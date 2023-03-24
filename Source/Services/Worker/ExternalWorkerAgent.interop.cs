using System;

namespace FastBuild.Dashboard.Services.Worker;

internal partial class ExternalWorkerAgent
{
    // ReSharper disable once InconsistentNaming
    private const int ID_TRAY_APP_ICON = 5000;

    private IntPtr FindExistingWorkerWindow()
    {
        var existingWindowPtr = IntPtr.Zero;
        WinAPI.EnumWindows((hWnd, lParam) =>
        {
            if (!WinAPIUtils.GetWindowText(hWnd).StartsWith("FBuildWorker")) 
                return true;

            if (!WinAPIUtils.GetWindowClass(hWnd).StartsWith("windowClass_")) 
                return true;

            existingWindowPtr = hWnd;
            return false;
        }, IntPtr.Zero);

        return existingWindowPtr;
    }

    private IntPtr GetChildWindow(int index, string assertedClass, bool recursive = false)
    {
        var workerWindowPtr = FindExistingWorkerWindow();
        var childWindowPtr = IntPtr.Zero;
        var currentIndex = 0;
        WinAPI.EnumChildWindows(workerWindowPtr, (hWnd, lParam) =>
        {
            if (!recursive && WinAPI.GetParent(hWnd) != workerWindowPtr) 
                return true;

            if (currentIndex == index)
            {
                childWindowPtr = hWnd;
                return false;
            }

            ++currentIndex;

            return true;
        }, IntPtr.Zero);

        if (childWindowPtr != IntPtr.Zero && !string.IsNullOrEmpty(assertedClass))
        {
            if (WinAPIUtils.GetWindowClass(childWindowPtr) != assertedClass)
                return IntPtr.Zero;
        }

        return childWindowPtr;
    }

    private void HideWorkerVisuals()
    {
        var workerWindowPtr = _workerProcess.MainWindowHandle;

        var data = new WinAPI.NOTIFYICONDATAA
        {
            hWnd = workerWindowPtr,
            uID = ID_TRAY_APP_ICON
        };
        _workerHidden = WinAPI.Shell_NotifyIconA(WinAPI.NotifyIconMessages.NIM_DELETE, data);
        if (_workerHidden)
            WinAPI.ShowWindow(_workerProcess.MainWindowHandle, 0);
    }
}