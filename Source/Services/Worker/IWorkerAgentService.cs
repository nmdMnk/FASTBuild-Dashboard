using System;
using FastBuild.Dashboard.Services.RemoteWorker;

namespace FastBuild.Dashboard.Services.Worker;

internal interface IWorkerAgentService
{
    uint WorkerCores { get; set; }
    uint WorkerThreshold { get; set; }
    WorkerSettings.WorkerModeSetting WorkerMode { get; set; }
    uint MinFreeMemoryMiB { get; set; }
    bool IsRunning { get; }
    bool IsPendingRestart { get; }
    event EventHandler<WorkerRunStateChangedEventArgs> WorkerRunStateChanged;
    void Initialize();
    WorkerCoreStatus[] GetStatus();

    void SetLocalWorker(IRemoteWorkerAgent worker);
}