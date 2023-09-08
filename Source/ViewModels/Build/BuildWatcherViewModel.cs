using System;
using System.Timers;
using System.Windows.Shell;
using Caliburn.Micro;
using FastBuild.Dashboard.Communication;
using FastBuild.Dashboard.Communication.Events;

namespace FastBuild.Dashboard.ViewModels.Build;

internal sealed class BuildWatcherViewModel : Conductor<BuildSessionViewModel>.Collection.OneActive, IMainPage
{
    private readonly BuildWatcher _watcher;
    private BuildSessionViewModel _currentSession;
    private TaskbarItemProgressState _taskbarProgressState;
    private double _taskbarProgressValue;
    
    public BuildWatcherViewModel()
    {
        DisplayName = "Build";

        _watcher = new BuildWatcher();
        _watcher.HistoryRestorationStarted += Watcher_HistoryRestorationStarted;
        _watcher.HistoryRestorationEnded += Watcher_HistoryRestorationEnded;
        _watcher.JobStarted += Watcher_JobStarted;
        _watcher.JobFinished += Watcher_JobFinished;
        _watcher.ReportCounter += Watcher_ReportCounter;
        _watcher.ReportProgress += Watcher_ReportProgress;
        _watcher.SessionStopped += Watcher_SessionStopped;
        _watcher.SessionStarted += Watcher_SessionStarted;
        _watcher.Start();

        var tickTimer = new Timer(100);
        tickTimer.Elapsed += TickTimer_Elapsed;
        tickTimer.Start();
    }

    public BuildSessionViewModel CurrentSession
    {
        get => _currentSession;
        private set
        {
            if (Equals(value, _currentSession)) 
                return;

            _currentSession = value;
            NotifyOfPropertyChange();
        }
    }

    public TaskbarItemProgressState TaskbarProgressState
    {
        get => _taskbarProgressState;
        private set
        {
            if (value == _taskbarProgressState) 
                return;

            _taskbarProgressState = value;
            NotifyOfPropertyChange();
        }
    }

    public double TaskbarProgressValue
    {
        get => _taskbarProgressValue;
        private set
        {
            if (value.Equals(_taskbarProgressValue)) 
                return;

            _taskbarProgressValue = value;
            NotifyOfPropertyChange();
        }
    }

    public string Icon => "HeartPulse";

    public event EventHandler<bool> WorkingStateChanged;

    private void TickTimer_Elapsed(object sender, ElapsedEventArgs e)
    {
        // ReSharper disable once UseNullPropagation
        if (CurrentSession == null) 
            return;

        // note we only need to tick current session

        // for sessions which are restoring history, don't use the real time so very long job graphs can be prevented
        CurrentSession.Tick(CurrentSession.IsRestoringHistory ? _watcher.LastMessageTime : DateTime.Now);
    }

    private void Watcher_HistoryRestorationEnded(object sender, EventArgs e)
    {
        if (CurrentSession != null) 
            CurrentSession.IsRestoringHistory = false;
    }

    private void Watcher_HistoryRestorationStarted(object sender, EventArgs e)
    {
        if (CurrentSession != null) 
            CurrentSession.IsRestoringHistory = true;
    }

    private void EnsureCurrentSession()
    {
        if (CurrentSession != null) 
            return;

        CurrentSession = new BuildSessionViewModel
        {
            IsRestoringHistory = _watcher.IsRestoringHistory
        };

        // called from log watcher thread
        lock (Items)
        {
            Items.Add(CurrentSession);
        }
    }

    private void Watcher_SessionStarted(object sender, StartBuildEventArgs e)
    {
        CurrentSession?.OnStopped(DateTime.Now);

        CurrentSession = new BuildSessionViewModel(e)
        {
            IsRestoringHistory = _watcher.IsRestoringHistory
        };

        Items.Add(CurrentSession);
        ActivateItemAsync(CurrentSession);

        TaskbarProgressState = TaskbarItemProgressState.Indeterminate;
        WorkingStateChanged?.Invoke(this, true);
    }

    private void Watcher_SessionStopped(object sender, StopBuildEventArgs e)
    {
        CurrentSession?.OnStopped(e);
        TaskbarProgressState = TaskbarItemProgressState.None;
        WorkingStateChanged?.Invoke(this, false);
    }

    private void Watcher_ReportProgress(object sender, ReportProgressEventArgs e)
    {
        EnsureCurrentSession();
        CurrentSession.ReportProgress(e);
        TaskbarProgressState = TaskbarItemProgressState.Normal;
        TaskbarProgressValue = e.Progress / 100.0;
    }

    private void Watcher_ReportCounter(object sender, ReportCounterEventArgs e)
    {
        EnsureCurrentSession();
        CurrentSession.ReportCounter(e);
    }

    private void Watcher_JobFinished(object sender, FinishJobEventArgs e)
    {
        EnsureCurrentSession();
        CurrentSession.OnJobFinished(e);
    }

    private void Watcher_JobStarted(object sender, StartJobEventArgs e)
    {
        EnsureCurrentSession();
        CurrentSession.OnJobStarted(e);
    }
}