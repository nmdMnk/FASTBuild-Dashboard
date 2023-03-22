using System;
using System.Diagnostics;
using System.IO;
using System.Management;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using FastBuild.Dashboard.Configuration;
using FastBuild.Dashboard.Services.RemoteWorker;

namespace FastBuild.Dashboard.Services.Worker;

internal partial class ExternalWorkerAgent : IWorkerAgent
{
    private const string WorkerExecutablePath = @"FBuild\FBuildWorker.exe";

    private bool _hasAppExited;

    private IRemoteWorkerAgent _localWorker;
    private uint _workerProcessId;

    private IntPtr _workerWindowPtr;

    public ExternalWorkerAgent()
    {
        Application.Current.Exit += Application_Exit;
    }

    public bool IsRunning { get; private set; }

    public event EventHandler<WorkerRunStateChangedEventArgs> WorkerRunStateChanged;

    public void Initialize()
    {
        _workerWindowPtr = FindExistingWorkerWindow();
        if (_workerWindowPtr == IntPtr.Zero)
        {
            StartNewWorker();
        }
        else
        {
            InitializeWorker();
            OnWorkerStarted();
        }

        if (IsRunning) StartWorkerGuardian();
    }

    public WorkerCoreStatus[] GetStatus()
    {
        // here we have to walk into another process's (fbuildworker) territory to get some text from a list view control

        var listViewPtr = GetChildWindow(0, "SysListView32");
        var itemCount = WinAPI.SendMessage(listViewPtr, (int)WinAPI.ListViewMessages.LVM_GETITEMCOUNT, 0, IntPtr.Zero)
            .ToInt32();

        var result = new WorkerCoreStatus[itemCount];

        // open fbuildworker process
        var processId = 0u;
        WinAPI.GetWindowThreadProcessId(listViewPtr, ref processId);

        var processHandle = WinAPI.OpenProcess(
            WinAPI.ProcessAccessFlags.VirtualMemoryOperation
            | WinAPI.ProcessAccessFlags.VirtualMemoryRead
            | WinAPI.ProcessAccessFlags.VirtualMemoryWrite,
            false,
            processId);

        // allocate memory for text to read
        var textBufferPtr = WinAPI.VirtualAllocEx(
            processHandle,
            IntPtr.Zero,
            WinAPI.MAX_LVMSTRING,
            WinAPI.AllocationType.Commit,
            WinAPI.MemoryProtection.ReadWrite);

        var lvItem = new WinAPI.LVITEM
        {
            mask = (uint)WinAPI.ListViewItemFilters.LVIF_TEXT,
            cchTextMax = (int)WinAPI.MAX_LVMSTRING,
            pszText = textBufferPtr
        };

        // marshal the LVITEM structure into unmanaged memory so it can be written into textBufferPtr
        var lvItemSize = Marshal.SizeOf(lvItem);
        var lvItemBufferPtr = WinAPI.VirtualAllocEx(
            processHandle,
            IntPtr.Zero,
            (uint)lvItemSize,
            WinAPI.AllocationType.Commit,
            WinAPI.MemoryProtection.ReadWrite);

        var lvItemLocalPtr = Marshal.AllocHGlobal(lvItemSize);
        var localTextBuffer = new byte[WinAPI.MAX_LVMSTRING];

        string GetCellText(int itemId, int subItemId)
        {
            lvItem.iItem = itemId;
            lvItem.iSubItem = subItemId;

            // write the LVITEM structure to target process's memory
            Marshal.StructureToPtr(lvItem, lvItemLocalPtr, false);

            WinAPI.WriteProcessMemory(
                processHandle,
                lvItemBufferPtr,
                lvItemLocalPtr,
                (uint)lvItemSize,
                out var _);

            WinAPI.SendMessage(listViewPtr, (int)WinAPI.ListViewMessages.LVM_GETITEMTEXT, itemId, lvItemBufferPtr);

            // read the text
            WinAPI.ReadProcessMemory(
                processHandle,
                textBufferPtr,
                localTextBuffer,
                (int)WinAPI.MAX_LVMSTRING,
                out var _);

            var text = Encoding.Unicode.GetString(localTextBuffer);
            var zeroIndex = text.IndexOf('\0');
            return zeroIndex < 0 ? text : text.Substring(0, zeroIndex);
        }

        for (var i = 0; i < itemCount; ++i)
        {
            var host = GetCellText(i, 1);
            var status = GetCellText(i, 2);

            WorkerCoreState state;
            string workingItem = null;
            switch (status)
            {
                case "Idle":
                    state = WorkerCoreState.Idle;
                    break;
                case "(Disabled)":
                    state = WorkerCoreState.Disabled;
                    break;
                default:
                    state = WorkerCoreState.Working;
                    workingItem = status;
                    break;
            }

            result[i] = new WorkerCoreStatus(state, host, workingItem);
        }

        WinAPI.VirtualFreeEx(processHandle, textBufferPtr, 0, WinAPI.AllocationType.Release);
        WinAPI.VirtualFreeEx(processHandle, lvItemBufferPtr, 0, WinAPI.AllocationType.Release);
        Marshal.FreeHGlobal(lvItemLocalPtr);

        WinAPI.CloseHandle(processHandle);

        return result;
    }

    public void SetCoreCount(int coreCount)
    {
        var comboBoxPtr = GetChildWindow(5, "ComboBox");

        if (comboBoxPtr == IntPtr.Zero)
        {
            OnWorkerErrorOccurred("An incompatible worker is running");
            return;
        }

        WinAPIUtils.SetComboBoxSelectedIndex(comboBoxPtr, coreCount - 1);
    }

    public void SetThresholdValue(int threshold)
    {
        var comboBoxPtr = GetChildWindow(3, "ComboBox");

        if (comboBoxPtr == IntPtr.Zero)
        {
            OnWorkerErrorOccurred("An incompatible worker is running");
            return;
        }

        WinAPIUtils.SetComboBoxSelectedIndex(comboBoxPtr, threshold - 1);
    }

    public void SetWorkerMode(WorkerMode mode)
    {
        var comboBoxPtr = GetChildWindow(1, "ComboBox");

        if (comboBoxPtr == IntPtr.Zero)
        {
            OnWorkerErrorOccurred("An incompatible worker is running");
            return;
        }

        WinAPIUtils.SetComboBoxSelectedIndex(comboBoxPtr, (int)mode);
    }

    public void SetLocalWorker(IRemoteWorkerAgent worker)
    {
        if (_localWorker != null)
            return;

        _localWorker = worker;
    }

    private void Application_Exit(object sender, ExitEventArgs e)
    {
        _hasAppExited = true;

        KillProcessAndChildren((int)_workerProcessId);

        if (_localWorker != null && File.Exists(_localWorker.FilePath))
            File.Delete(_localWorker.FilePath);
    }

    private void StartWorkerGuardian()
    {
        Task.Factory.StartNew(WorkerGuardian);
    }

    private void WorkerGuardian()
    {
        while (!_hasAppExited)
        {
            if (_workerWindowPtr == IntPtr.Zero || !WinAPI.IsWindow(_workerWindowPtr))
            {
                IsRunning = false;
            }
            else
            {
                var processId = 1u;
                WinAPI.GetWindowThreadProcessId(_workerWindowPtr, ref processId);
                if (processId != _workerProcessId) IsRunning = false;
            }

            if (!IsRunning)
            {
                OnWorkerErrorOccurred("Worker stopped unexpectedly, restarting");
                StartNewWorker();
            }

            Thread.Sleep(500);
        }
    }

    private void InitializeWorker()
    {
        WinAPI.ShowWindow(_workerWindowPtr, WinAPI.ShowWindowCommands.SW_HIDE);
        RemoveTrayIcon();

        _workerProcessId = 1u; // must not be NULL (0)
        WinAPI.GetWindowThreadProcessId(_workerWindowPtr, ref _workerProcessId);
    }

    private void OnWorkerStarted()
    {
        IsRunning = true;
        WorkerRunStateChanged?.Invoke(this, new WorkerRunStateChangedEventArgs(true, null));
    }

    private void OnWorkerErrorOccurred(string message)
    {
        IsRunning = false;
        WorkerRunStateChanged?.Invoke(this, new WorkerRunStateChangedEventArgs(false, message));
    }

    private void StartNewWorker()
    {
        var executablePath = WorkerExecutablePath;

        if (!File.Exists(executablePath))
        {
            // If worker isn't found in working directory, try it relative to running binary.
            executablePath = Path.Combine(Path.GetDirectoryName(Assembly.GetEntryAssembly().Location),
                WorkerExecutablePath);

            if (!File.Exists(executablePath))
            {
                OnWorkerErrorOccurred($"Worker executable not found at {WorkerExecutablePath}");
                return;
            }
        }

        var startInfo = new ProcessStartInfo(executablePath)
        {
            Arguments = "-nosubprocess",
            CreateNoWindow = true
        };
        
        startInfo.Arguments += $" -minfreememory={AppSettings.Default.WorkerMinFreeMemoryMiB}";

        Process process;

        try
        {
            process = Process.Start(startInfo);
        }
        catch (Exception ex)
        {
            OnWorkerErrorOccurred($"Failed to start worker, exception occurred.\n\nMessage:{ex.Message}");
            return;
        }

        while (true)
        {
            if (process == null || process.HasExited)
            {
                OnWorkerErrorOccurred("Failed to start worker, worker window not found");
                return;
            }

            _workerWindowPtr = FindExistingWorkerWindow();
            if (_workerWindowPtr != IntPtr.Zero) 
                break;

            Thread.Sleep(100);
        }

        InitializeWorker();
        OnWorkerStarted();
    }


    private static void KillProcessAndChildren(int pid)
    {
        // Cannot close 'system idle process'.
        if (pid == 0) return;
        ManagementObjectSearcher searcher = new ManagementObjectSearcher
            ("Select * From Win32_Process Where ParentProcessID=" + pid);
        ManagementObjectCollection moc = searcher.Get();
        foreach (ManagementObject mo in moc) KillProcessAndChildren(Convert.ToInt32(mo["ProcessID"]));
        try
        {
            Process proc = Process.GetProcessById(pid);
            proc.Kill();
        }
        catch (ArgumentException)
        {
            // Process already exited.
        }
    }
}