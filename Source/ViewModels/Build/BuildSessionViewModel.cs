using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Timers;
using Caliburn.Micro;
using FastBuild.Dashboard.Communication;
using FastBuild.Dashboard.Communication.Events;
using FastBuild.Dashboard.Services;

namespace FastBuild.Dashboard.ViewModels.Build;

internal partial class BuildSessionViewModel : Screen
{
    private readonly Dictionary<string, BuildWorkerViewModel> _workerMap = new();
    private DateTime _currentTime;
    private Process _fbuildProcess;

    private BuildSessionViewModel(DateTime startTime, int? processId, int? logVersion)
    {
        StartTime = startTime;
        CurrentTime = startTime;
        ProcessId = processId;
        LogVersion = logVersion;
        IsRunning = true;

        // ReSharper disable once VirtualMemberCallInConstructor
        DisplayName = startTime.ToString(CultureInfo.CurrentCulture);

        var brokerageService = IoC.Get<IBrokerageService>();
        PoolWorkerNames = brokerageService.WorkerNames;
        brokerageService.WorkerCountChanged += BrokerageService_WorkerCountChanged;

        InitiatorProcess = new BuildInitiatorProcessViewModel(processId);

        WatchBuildProcess();
    }

    public BuildSessionViewModel()
        : this(DateTime.Now, null, null)
    {
        IsRunning = true;
    }

    public BuildSessionViewModel(StartBuildEventArgs e)
        : this(e.Time, e.ProcessId, e.LogVersion)
    {
        IsRunning = true;
    }

    public DateTime StartTime { get; }

    // current time, this could be a historical time if we are restoring history, otherwise should be 
    // a time a bit before now (decided by Tick)
    public DateTime CurrentTime
    {
        get => _currentTime;
        private set
        {
            if (value.Equals(_currentTime)) return;

            _currentTime = value;
            NotifyOfPropertyChange();

            NotifyOfPropertyChange(nameof(ElapsedTime));
            NotifyOfPropertyChange(nameof(DisplayElapsedTime));
        }
    }

    public int? ProcessId { get; }
    public int? LogVersion { get; }

    public TimeSpan ElapsedTime => _currentTime - StartTime;
    public string DisplayElapsedTime => ElapsedTime.ToString(@"hh\:mm\:ss\.f");

    public BindableCollection<BuildWorkerViewModel> Workers { get; } = new BindableCollection<BuildWorkerViewModel>();

    public BuildSessionJobManager JobManager { get; } = new();
    public BuildInitiatorProcessViewModel InitiatorProcess { get; }

    public event EventHandler<double> Ticked;


    private void WatchBuildProcess()
    {
        if (ProcessId == null) return;

        if (!BuildInitiatorProcessViewModel.GetIsProcessAccessible(ProcessId.Value))
            // process not accessible, it's either exited (historical build)
            // or running by an account with higher privilege
            return;

        try
        {
            var process = Process.GetProcessById(ProcessId.Value);

            if (process.StartTime > StartTime)
                // fbuild process already terminated, this is a fake one with its ID reused
                return;

            _fbuildProcess = process;
        }
        catch (ArgumentException)
        {
            // process already terminated, may be a historical build
            return;
        }

        var timer = new Timer(100);

        void TimerTick(object sender, ElapsedEventArgs e)
        {
            if (_fbuildProcess != null && _fbuildProcess.HasExited)
            {
                _fbuildProcess = null;
                OnStopped(DateTime.Now);
                // ReSharper disable AccessToDisposedClosure
                timer.Elapsed -= TimerTick;
                timer.Stop();
                timer.Dispose();
                // ReSharper restore AccessToDisposedClosure
            }
        }

        timer.Elapsed += TimerTick;
        timer.Start();
    }


    public void OnStopped(StopBuildEventArgs e)
    {
        OnStopped(e.Time);
    }

    public void OnStopped(DateTime time)
    {
        // give components a last chance to tick
        // we don't do UpdateTimeFromEvent here because Tick will do the same thing
        Tick(time);
        IsRunning = false;

        var currentTimeOffset = ElapsedTime.TotalSeconds;

        // do this before notifying workers, so give JobManager a chance to raise
        // job finish events
        JobManager.NotifySessionStopped();

        foreach (var worker in Workers) 
            worker.OnSessionStopped(currentTimeOffset);

        InProgressJobCount = 0;

        UpdateActiveWorkerAndCoreCount();
    }

    public void ReportProgress(ReportProgressEventArgs e)
    {
        UpdateTimeFromEvent(e);

        Progress = e.Progress;
    }

    public void ReportCounter(ReportCounterEventArgs e)
    {
        UpdateTimeFromEvent(e);
    }

    private BuildWorkerViewModel EnsureWorker(string hostName)
    {
        if (!_workerMap.TryGetValue(hostName, out var worker))
        {
            worker = new BuildWorkerViewModel(hostName, _workerMap.Count == 0, this);
            _workerMap.Add(hostName, worker);

            // called from log watcher thread
            lock (Workers)
            {
                Workers.Add(worker);
            }
        }

        return worker;
    }

    public void OnJobFinished(FinishJobEventArgs e)
    {
        UpdateTimeFromEvent(e);

        var job = EnsureWorker(e.HostName).OnJobFinished(e);

        if (job != null)
        {
            var racedJob = JobManager.GetJobPotentiallyWonByLocalRace(job);
            if (racedJob != null)
                OnJobFinished(FinishJobEventArgs.MakeRacedOut(e.Time, racedJob.OwnerCore.OwnerWorker.HostName,
                    racedJob.EventName));

            JobManager.NotifyJobFinished(job);
        }

        switch (e.Result)
        {
            case BuildJobStatus.Success:
                ++SuccessfulJobCount;
                break;
            case BuildJobStatus.SuccessCached:
                ++SuccessfulJobCount;
                ++CacheHitCount;
                break;
            case BuildJobStatus.Failed:
            case BuildJobStatus.Error:
                ++FailedJobCount;
                break;
        }

        --InProgressJobCount;

        UpdateActiveWorkerAndCoreCount();
    }

    public void OnJobStarted(StartJobEventArgs e)
    {
        UpdateTimeFromEvent(e);

        var job = EnsureWorker(e.HostName).OnJobStarted(e, StartTime);
        JobManager.Add(job);
        ++InProgressJobCount;

        if (!IsRunning)
        {
            // because of the async nature, this could happen even after the build process 
            // is terminated
            var finishJobEventArgs = FinishJobEventArgs.MakeStopped(_currentTime, e.HostName, e.EventName);
            OnJobFinished(finishJobEventArgs);
        }

        UpdateActiveWorkerAndCoreCount();
    }


    private void UpdateTimeFromEvent(BuildEventArgs e)
    {
        // this is important for history restoring to keep track of time
        CurrentTime = e.Time;
    }

    public void Tick(DateTime now)
    {
        if (!IsRunning || IsRestoringHistory)
            return;

        CurrentTime = now;

        var timeOffset = ElapsedTime.TotalSeconds;

        JobManager.Tick(timeOffset);

        // called from tick thread
        lock (Workers)
        {
            foreach (var worker in Workers)
                worker.Tick(timeOffset);
        }

        Ticked?.Invoke(this, timeOffset);
    }

    private void DetectDebris()
    {
        // historical build could be interrupted (due to unexpected termination of fbuild process)
        // so no StopBuild event will be triggered. we detect this kind of 'debris' here.

        // because fbuild will output build state routinely (500ms IIRC), so we won't need to worry
        // about long jobs being mishandled in this situation.

        if ((DateTime.Now - CurrentTime).TotalSeconds > 10)
            OnStopped(CurrentTime);
    }
}