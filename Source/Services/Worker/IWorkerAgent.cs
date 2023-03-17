using System;
using FastBuild.Dashboard.Services.RemoteWorker;

namespace FastBuild.Dashboard.Services.Worker;

internal interface IWorkerAgent
{
    bool IsRunning { get; }
    event EventHandler<WorkerRunStateChangedEventArgs> WorkerRunStateChanged;
    void SetCoreCount(int coreCount);
    void SetThresholdValue(int threshold);
    void SetWorkerMode(WorkerMode mode);
    void SetLocalWorker(IRemoteWorkerAgent worker);
    void Initialize();
    WorkerCoreStatus[] GetStatus();
}