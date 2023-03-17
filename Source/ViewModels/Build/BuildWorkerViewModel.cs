using System;
using System.Diagnostics;
using System.Linq;
using Caliburn.Micro;
using FastBuild.Dashboard.Communication.Events;

namespace FastBuild.Dashboard.ViewModels.Build;

[DebuggerDisplay("Worker:{" + nameof(HostName) + "}")]
internal class BuildWorkerViewModel : PropertyChangedBase
{
    public BuildWorkerViewModel(string hostName, bool isLocal, BuildSessionViewModel ownerSession)
    {
        HostName = hostName;
        IsLocal = isLocal;
        OwnerSession = ownerSession;
    }

    public string HostName { get; }
    public bool IsLocal { get; }
    public BuildSessionViewModel OwnerSession { get; }
    public BindableCollection<BuildCoreViewModel> Cores { get; } = new BindableCollection<BuildCoreViewModel>();

    public int ActiveCoreCount { get; private set; }

    public BuildJobViewModel OnJobFinished(FinishJobEventArgs e)
    {
        var core = Cores.FirstOrDefault(c => c.CurrentJob != null && c.CurrentJob.EventName == e.EventName);
        var job = core?.OnJobFinished(e);

        UpdateActiveCoreCount();

        return job;
    }

    public BuildJobViewModel OnJobStarted(StartJobEventArgs e, DateTime sessionStartTime)
    {
        var core = Cores.FirstOrDefault(c => !c.IsBusy);
        if (core == null)
        {
            core = new BuildCoreViewModel(Cores.Count, this);

            // called from log watcher thread
            lock (Cores)
            {
                Cores.Add(core);
            }
        }

        var job = core.OnJobStarted(e, sessionStartTime);

        UpdateActiveCoreCount();

        return job;
    }

    private void UpdateActiveCoreCount()
    {
        ActiveCoreCount = Cores.Count(c => c.IsBusy);
    }

    public void OnSessionStopped(double currentTimeOffset)
    {
        foreach (var core in Cores) core.OnSessionStopped(currentTimeOffset);

        ActiveCoreCount = 0;
    }

    public void Tick(double currentTimeOffset)
    {
        // called from tick thread
        lock (Cores)
        {
            foreach (var core in Cores) core.Tick(currentTimeOffset);
        }
    }
}