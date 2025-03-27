﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using FastBuild.Dashboard.Configuration;
using FastBuild.Dashboard.Services.RemoteWorker;
using NLog;
using NLog.Fluent;

namespace FastBuild.Dashboard.Services.Worker;

internal partial class ExternalWorkerAgent : IWorkerAgent
{
    private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

    private readonly string _workerExecutablePath = 
        Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "FBuild", "FBuildWorker.exe");

    private bool _hasAppExited;

    private IRemoteWorkerAgent _localWorker;

    private WorkerSettings _settings;
    private Process _workerProcess;

    public ExternalWorkerAgent()
    {
        Application.Current.Exit += Application_Exit;
        _settings = new WorkerSettings(_workerExecutablePath);
        Logger.Info("Created");
    }

    public bool IsRunning => !_isStartingWorker && _workerProcess != null && !_workerProcess.HasExited;

    private bool _initialized = false;
    private bool _isStartingWorker = true;
    private bool _workerHidden = false;

    public event EventHandler<WorkerRunStateChangedEventArgs> WorkerRunStateChanged;

    public void Initialize()
    {
        if (_initialized)
        {
            Logger.Warn("Unable to initialize worker as it is already initialized!");
            return;
        }

        if (IsRunning)
        {
            Logger.Warn("Unable to initialize worker as it is already running!");
            return;
        }
            

        Logger.Info("Initializing external worker");
        if (FindExistingWorker())
        {
            Logger.Info("Killing external worker process");
            _workerProcess.Kill(); // Could be any instance but we want to make sure its spawned by us
        }

        StartNewWorker();

        Logger.Info("Starting worker watchdog");
        Task.Factory.StartNew(WorkerWatchdog);
        _initialized = true;
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

    public WorkerSettings GetSettings()
    {
        return _settings;
    }

    public void SetCoreCount(uint coreCount)
    {
        _settings.NumCPUsToUse = coreCount;
    }

    public void SetThresholdValue(uint threshold)
    {
        _settings.IdleThresholdPercent = threshold;
    }

    public void SetWorkerMode(WorkerSettings.WorkerModeSetting mode)
    {
        _settings.WorkerMode = mode;
    }

    public void SetLocalWorker(IRemoteWorkerAgent worker)
    {
        if (_localWorker != null)
            return;

        _localWorker = worker;
    }

    public void SetMinimumFreeMemoryMiB(uint memory)
    {
        _settings.MinimumFreeMemoryMiB = memory;
        _settings.Save();
    }
    public void SetPreferHostname(bool preferHostname)
    {
        _settings.PreferHostname = preferHostname;
        _settings.Save();
    }

    public void RestartWorker()
    {
        Logger.Info("Restarting worker...");

        _workerProcess?.Kill();
        OnWorkerErrorOccurred("Worker restarting");

        if (FindExistingWorker())
        {
            Logger.Info("Killing existing worker process.");
            try
            {
                _workerProcess?.Kill(); // Could be any instance but we want to make sure its spawned by us
            }
            catch (Exception)
            {
                // ignored
            }
        }

        _workerProcess = null;
        StartNewWorker();
    }

    private void Application_Exit(object sender, ExitEventArgs e)
    {
        _hasAppExited = true;

        _workerProcess?.Kill();
        if (FindExistingWorker())
            _workerProcess?.Kill();

        if (_localWorker != null && File.Exists(_localWorker.FilePath))
        {
            try
            {
                File.Delete(_localWorker.FilePath);
            }
            catch (Exception)
            {
                // Ignore exceptions as the file might be deleted already by FBuild Worker
            }
        }
    }

    private void WorkerWatchdog()
    {
        while (!_hasAppExited)
        {
            if (!IsRunning)
            {
                OnWorkerErrorOccurred("Worker stopped unexpectedly, restarting");
                StartNewWorker();
            }
            else
            {
                if (!_workerHidden)
                    HideWorkerVisuals(); // Worker might need a moment to start, so window handle is not accessible directly

                CheckForRestartToReloadSettings();
            }

            Thread.Sleep(500);
        }
    }

    private void CheckForRestartToReloadSettings()
    {
        if (_settings == null)
            return;

        if (!_settings.SettingsAreDirty)
            return;

        var anyWorking = GetStatus().Any(c => c.State == WorkerCoreState.Working);
        if (anyWorking)
            return;
        
        Logger.Info("Restartng worker due to settings update");
        RestartWorker();
    }

    private void OnWorkerStarted()
    {
        Logger.Info("Worker Started");
        WorkerRunStateChanged?.Invoke(this, new WorkerRunStateChangedEventArgs(true, null));
    }

    private void OnWorkerErrorOccurred(string message)
    {
        Logger.Error($"Worker Error: {message}");
        _isStartingWorker = false;
        WorkerRunStateChanged?.Invoke(this, new WorkerRunStateChangedEventArgs(false, message));
    }

    private bool FindExistingWorker()
    {
        List<Process> processes = Process.GetProcessesByName("FBuildWorker.exe").ToList();
        processes.AddRange(Process.GetProcessesByName("FBuildWorker"));
        processes.AddRange(Process.GetProcessesByName("FBuildWorker.exe.copy"));

        if (processes.Count <= 0)
            return false;

        // There can be only one FBuildWorker anyways, so there never should be more than one
        _workerProcess = processes[0];
        return true;
    }

    private void StartNewWorker()
    {
        Logger.Info("Trying to start new worker");

        _isStartingWorker = true;
        var executablePath = _workerExecutablePath;

        if (!File.Exists(executablePath))
        {
            // If worker isn't found in working directory, try it relative to running binary.
            executablePath = Path.Combine(Path.GetDirectoryName(Assembly.GetEntryAssembly().Location),
                _workerExecutablePath);

            if (!File.Exists(executablePath))
            {
                OnWorkerErrorOccurred($"Worker executable not found at {_workerExecutablePath}");
                return;
            }
        }

        var startInfo = new ProcessStartInfo(executablePath)
        {
            Arguments = "-nosubprocess"
        };

        if (AppSettings.Default.PreferHostname)
            startInfo.Arguments += " -preferhostname";

        startInfo.Arguments += $" -minfreememory={AppSettings.Default.WorkerMinFreeMemoryMiB}";

        try
        {
            _workerProcess = new Process { StartInfo = startInfo, EnableRaisingEvents = true };
            _workerProcess.Exited += WorkerProcessOnExited;
            _workerProcess.Start();
            _workerHidden = false;
        }
        catch (Exception ex)
        {
            OnWorkerErrorOccurred($"Failed to start worker, exception occurred.\n\nMessage:{ex.Message}");
            return;
        }

        if (_workerProcess == null || _workerProcess.HasExited)
        {
            OnWorkerErrorOccurred("Failed to start worker, worker window not found");
            return;
        }

        _settings = new WorkerSettings(_workerExecutablePath);
        HideWorkerVisuals();
        OnWorkerStarted();
        _isStartingWorker = false;
    }

    private void WorkerProcessOnExited(object sender, EventArgs e)
    {
        if (_workerProcess != null)
            _workerProcess.Exited -= WorkerProcessOnExited;

        _workerProcess = null;
    }
}