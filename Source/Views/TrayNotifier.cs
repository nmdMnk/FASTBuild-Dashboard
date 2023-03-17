using System;
using System.Windows.Threading;
using Application = System.Windows.Application;
using WinForms = System.Windows.Forms;

namespace FastBuild.Dashboard.Views;

internal class TrayNotifier
{
    private readonly MainWindowView _owner;
    private readonly WinForms.NotifyIcon _trayNotifier;
    private readonly DispatcherTimer _workingIconTimer;

    private int _workingIconStage;

    public TrayNotifier(MainWindowView owner)
    {
        _owner = owner;
        _trayNotifier = new WinForms.NotifyIcon();
        _trayNotifier.DoubleClick += TrayNotifier_DoubleClick;
        _trayNotifier.Text = "FASTBuild Dashboard is running. Double click to show window.";
        _trayNotifier.Visible = true;

        _workingIconTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(0.3)
        };

        _workingIconTimer.Tick += WorkingIconTimer_Tick;

        _trayNotifier.ContextMenuStrip = new WinForms.ContextMenuStrip();
        _trayNotifier.ContextMenuStrip.Items.Add("Show", GetImage("/Resources/Icons/tray_normal_16.ico"),
            TrayNotifier_DoubleClick);
        _trayNotifier.ContextMenuStrip.Items.Add(new WinForms.ToolStripSeparator());
        _trayNotifier.ContextMenuStrip.Items.Add("Work Proportional",
            GetImage("/Resources/Icons/tray_working_1_16.ico"), (sender, args) => MenuChangeWorkerMode(3));
        _trayNotifier.ContextMenuStrip.Items.Add("Work Always", GetImage("/Resources/Icons/tray_working_all_16.ico"),
            (sender, args) => MenuChangeWorkerMode(2));
        _trayNotifier.ContextMenuStrip.Items.Add("Work When Idle", GetImage("/Resources/Icons/tray_normal_16.ico"),
            (sender, args) => MenuChangeWorkerMode(1));
        _trayNotifier.ContextMenuStrip.Items.Add("Disabled", GetImage("/Resources/Icons/tray_disabled_16.ico"),
            (sender, args) => MenuChangeWorkerMode(0));
        _trayNotifier.ContextMenuStrip.Items.Add(new WinForms.ToolStripSeparator());
        _trayNotifier.ContextMenuStrip.Items.Add("Exit", null, TrayNotifier_Exit);

        UseNormalIcon();
    }

    private void TrayNotifier_DoubleClick(object sender, EventArgs e)
    {
        _owner.ShowAndActivate();
    }

    private void TrayNotifier_Exit(object sender, EventArgs e)
    {
        _owner.CloseApplication();
    }

    private void WorkingIconTimer_Tick(object sender, EventArgs e)
    {
        ShiftWorkingIcon();
    }

    public void UseNormalIcon()
    {
        _workingIconTimer.Stop();
        SetTrayIcon("/Resources/Icons/tray_normal_16.ico");
    }

    public void UseDisabledIcon()
    {
        _workingIconTimer.Stop();
        SetTrayIcon("/Resources/Icons/tray_disabled_16.ico");
    }

    public void UseWorkingIcon()
    {
        ShiftWorkingIcon();
        _workingIconTimer.Start();
    }

    private void ShiftWorkingIcon()
    {
        SetTrayIcon($"/Resources/Icons/tray_working_{_workingIconStage}_16.ico");
        _workingIconStage = (_workingIconStage + 1) % 3;
    }

    private System.Drawing.Image GetImage(string resourcePath)
    {
        var iconInfo = Application.GetResourceStream(new Uri(resourcePath, UriKind.Relative));
        if (iconInfo != null)
            using (var iconStream = iconInfo.Stream)
            {
                return System.Drawing.Image.FromStream(iconStream);
            }

        return null;
    }

    private void SetTrayIcon(string resourcePath)
    {
        var iconInfo = Application.GetResourceStream(new Uri(resourcePath, UriKind.Relative));
        if (iconInfo != null)
            using (var iconStream = iconInfo.Stream)
            {
                _trayNotifier.Icon = new System.Drawing.Icon(iconStream);
            }
    }

    public void Close()
    {
        _trayNotifier.Visible = false;
        _trayNotifier.Dispose();
    }

    private void MenuChangeWorkerMode(int workerMode)
    {
        _owner.ChangeWorkerMode(workerMode);
    }
}