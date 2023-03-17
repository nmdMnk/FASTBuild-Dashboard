using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Threading;
using Caliburn.Micro;
using FastBuild.Dashboard.Configuration;
using FastBuild.Dashboard.Services.Worker;
using FastBuild.Dashboard.ViewModels;

namespace FastBuild.Dashboard.Views;

public partial class MainWindowView
{
    private readonly TrayNotifier _trayNotifier;
    private DispatcherTimer _delayUpdateProfileTimer;
    private bool _isClosingFromXButton = true;
    private bool _isWorking;
    private MainWindowViewModel _viewModel;

    public MainWindowView()
    {
        InitializeComponent();
        InitializeWindowDimensions();
        _trayNotifier = new TrayNotifier(this);
        UpdateTrayIcon();

        this.DataContextChanged += OnDataContextChanged;
        this.Loaded += OnLoaded;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        if (App.Current.StartMinimized) this.Hide();
    }

    protected override void OnClosing(CancelEventArgs e)
    {
        if (_isClosingFromXButton)
        {
            e.Cancel = true;

            this.Hide();
        }

        _isClosingFromXButton = true;
        base.OnClosing(e);
    }

    protected override void OnClosed(EventArgs e)
    {
        _trayNotifier.Close();
        base.OnClosed(e);
    }

    private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        var vm = e.NewValue as MainWindowViewModel;
        if (vm == null) return;

        vm.BuildWatcherPage.WorkingStateChanged += BuildWatcherPage_WorkingStateChanged;
        vm.SettingsPage.WorkerModeChanged += SettingsPage_WorkerModeChanged;
        _viewModel = vm;
    }

    private void BuildWatcherPage_WorkingStateChanged(object sender, bool isWorking)
    {
        _isWorking = isWorking;
        UpdateTrayIcon();
    }

    private void SettingsPage_WorkerModeChanged(object sender, WorkerMode mode)
    {
        UpdateTrayIcon();
    }

    private void InitializeWindowDimensions()
    {
        if (Profile.Default.IsFirstRun)
        {
            Profile.Default.IsFirstRun = false;
            Profile.Default.Save();
        }
        else
        {
            this.Left = Profile.Default.WindowLeft;
            this.Top = Profile.Default.WindowTop;
        }

        this.Width = Profile.Default.WindowWidth;
        this.Height = Profile.Default.WindowHeight;

        if (App.Current.StartMinimized)
        {
            this.WindowState = WindowState.Minimized;
            this.Hide();
        }
        else
        {
            this.WindowState = Profile.Default.WindowState;
        }

        this.LocationChanged += MainWindowView_LocationChanged;
        this.SizeChanged += MainWindowView_SizeChanged;
        this.StateChanged += MainWindowView_StateChanged;

        _delayUpdateProfileTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(500)
        };

        _delayUpdateProfileTimer.Tick += DelayUpdateProfileTimer_Tick;
    }

    private void DelayUpdateProfileTimer_Tick(object sender, EventArgs e)
    {
        _delayUpdateProfileTimer.Stop();

        Profile.Default.WindowLeft = (int)this.Left;
        Profile.Default.WindowTop = (int)this.Top;
        if (this.WindowState != WindowState.Minimized) Profile.Default.WindowState = this.WindowState;
        Profile.Default.WindowWidth = (int)this.Width;
        Profile.Default.WindowHeight = (int)this.Height;
        Profile.Default.Save();
    }

    private void MainWindowView_StateChanged(object sender, EventArgs e)
    {
        StartDelayedProfileUpdate();

        /*
        if (this.WindowState == WindowState.Minimized)
        {
            this.Hide();
        }
        */
    }

    private void StartDelayedProfileUpdate()
    {
        _delayUpdateProfileTimer.Stop();
        _delayUpdateProfileTimer.Start();
    }

    private void MainWindowView_LocationChanged(object sender, EventArgs e)
    {
        StartDelayedProfileUpdate();
    }

    private void MainWindowView_SizeChanged(object sender, SizeChangedEventArgs e)
    {
        StartDelayedProfileUpdate();
    }

    private void ListBox_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
    {
        MenuToggleButton.IsChecked = false;
    }

    public void CloseApplication()
    {
        _isClosingFromXButton = false;
        this.Close();
    }

    public void ShowAndActivate()
    {
        this.Show();
        if (this.WindowState == WindowState.Minimized) this.WindowState = Profile.Default.WindowState;

        this.Activate();
    }

    public void ChangeWorkerMode(int workerMode)
    {
        if (_viewModel != null) _viewModel.SettingsPage.WorkerMode = workerMode;
    }

    private bool IsWorkerEnabled()
    {
        if (IoC.Get<IWorkerAgentService>().WorkerMode == WorkerMode.Disabled)
            return false;
        return true;
    }

    private void UpdateTrayIcon()
    {
        if (_isWorking)
        {
            _trayNotifier.UseWorkingIcon();
        }
        else
        {
            if (IsWorkerEnabled())
                _trayNotifier.UseNormalIcon();
            else
                _trayNotifier.UseDisabledIcon();
        }
    }
}