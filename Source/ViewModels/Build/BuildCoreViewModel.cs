using System;
using System.Diagnostics;
using Caliburn.Micro;
using FastBuild.Dashboard.Communication.Events;
using FastBuild.Dashboard.Services.Build;

namespace FastBuild.Dashboard.ViewModels.Build;
#if DEBUG
[DebuggerDisplay("{" + nameof(DebuggerDisplay) + "}")]
#endif
internal class BuildCoreViewModel : PropertyChangedBase
{
    private BuildJobViewModel _currentJob;

    private bool _isBusy;
    private double _uiJobsTotalWidth;

    public BuildCoreViewModel(int id, BuildWorkerViewModel ownerWorker)
    {
        Id = id;
        OwnerWorker = ownerWorker;
        IoC.Get<IBuildViewportService>().ScalingChanged
            += ViewTransformServicePreScalingChanged;
    }
#if DEBUG
    private string DebuggerDisplay => $"Core:{OwnerWorker.HostName} #{Id}";
#endif
    public int Id { get; }
    public BuildWorkerViewModel OwnerWorker { get; }

    public BindableCollection<BuildJobViewModel> Jobs { get; } = new BindableCollection<BuildJobViewModel>();

    public BuildJobViewModel CurrentJob
    {
        get => _currentJob;
        private set
        {
            if (Equals(value, _currentJob)) return;

            _currentJob = value;
            this.NotifyOfPropertyChange();
        }
    }

    public bool IsBusy
    {
        get => _isBusy;
        private set
        {
            if (value == _isBusy) return;

            _isBusy = value;
            this.NotifyOfPropertyChange();
        }
    }

    public double UIJobsTotalWidth
    {
        get => _uiJobsTotalWidth;
        private set
        {
            if (value.Equals(_uiJobsTotalWidth)) return;

            _uiJobsTotalWidth = value;
            this.NotifyOfPropertyChange();
        }
    }

    private void ViewTransformServicePreScalingChanged(object sender, EventArgs e)
    {
        UpdateUIJobsTotalWidth();
    }

    public BuildJobViewModel OnJobFinished(FinishJobEventArgs e)
    {
        IsBusy = false;
        if (CurrentJob != null)
        {
            var job = CurrentJob;
            CurrentJob.OnFinished(e);
            CurrentJob = null;
            return job;
        }

        return null;
    }

    public BuildJobViewModel OnJobStarted(StartJobEventArgs e, DateTime sessionStartTime)
    {
        IsBusy = true;

        CurrentJob = new BuildJobViewModel(this, e, Jobs.Count == 0 ? null : Jobs[Jobs.Count - 1]);

        // called from log watcher thread
        lock (Jobs)
        {
            Jobs.Add(CurrentJob);
        }

        return CurrentJob;
    }

    public void OnSessionStopped(double currentTimeOffset)
    {
        foreach (var job in Jobs) job.OnSessionStopped(currentTimeOffset);
    }

    public void Tick(double currentTimeOffset)
    {
        // called from tick thread
        lock (Jobs)
        {
            foreach (var job in Jobs) job.Tick(currentTimeOffset);
        }

        UpdateUIJobsTotalWidth();
    }

    private void UpdateUIJobsTotalWidth()
    {
        UIJobsTotalWidth = IoC.Get<IBuildViewportService>().Scaling *
                           OwnerWorker.OwnerSession.ElapsedTime.TotalSeconds;
    }
}