using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
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

        DataContextChanged += OnDataContextChanged;
        Loaded += OnLoaded;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        if (App.Current.StartMinimized) Hide();
    }

    protected override void OnClosing(CancelEventArgs e)
    {
        if (_isClosingFromXButton)
        {
            e.Cancel = true;

            Hide();
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
        if (vm == null) 
            return;

        vm.BuildWatcherPage.WorkingStateChanged += BuildWatcherPage_WorkingStateChanged;
        vm.SettingsPage.WorkerModeChanged += SettingsPage_WorkerModeChanged;
        _viewModel = vm;
    }

    private void BuildWatcherPage_WorkingStateChanged(object sender, bool isWorking)
    {
        _isWorking = isWorking;
        UpdateTrayIcon();
    }

    private void SettingsPage_WorkerModeChanged(object sender, WorkerSettings.WorkerModeSetting mode)
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
            Left = Profile.Default.WindowLeft;
            Top = Profile.Default.WindowTop;
        }

        Width = Profile.Default.WindowWidth;
        Height = Profile.Default.WindowHeight;

        if (App.Current.StartMinimized)
        {
            WindowState = WindowState.Minimized;
            Hide();
        }
        else
        {
            WindowState = Profile.Default.WindowState;
        }

        LocationChanged += MainWindowView_LocationChanged;
        SizeChanged += MainWindowView_SizeChanged;
        StateChanged += MainWindowView_StateChanged;

        _delayUpdateProfileTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(500)
        };

        _delayUpdateProfileTimer.Tick += DelayUpdateProfileTimer_Tick;
    }

    private void DelayUpdateProfileTimer_Tick(object sender, EventArgs e)
    {
        _delayUpdateProfileTimer.Stop();

        Profile.Default.WindowLeft = (int)Left;
        Profile.Default.WindowTop = (int)Top;
        if (WindowState != WindowState.Minimized) Profile.Default.WindowState = WindowState;
        Profile.Default.WindowWidth = (int)Width;
        Profile.Default.WindowHeight = (int)Height;
        Profile.Default.Save();
    }

    private void MainWindowView_StateChanged(object sender, EventArgs e)
    {
        StartDelayedProfileUpdate();
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

    private void ListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        MenuToggleButton.IsChecked = false;
    }

    public void CloseApplication()
    {
        _isClosingFromXButton = false;
        Close();
    }

    public void ShowAndActivate()
    {
        Show();
        if (WindowState == WindowState.Minimized)
            WindowState = Profile.Default.WindowState;

        Activate();
    }

    public void ChangeWorkerMode(int workerMode)
    {
        if (_viewModel != null)
            _viewModel.SettingsPage.WorkerMode = workerMode;
    }

    private bool IsWorkerEnabled()
    {
        return IoC.Get<IWorkerAgentService>().WorkerMode != WorkerSettings.WorkerModeSetting.Disabled;
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