using System;
using FastBuild.Dashboard.Services.RemoteWorker;

namespace FastBuild.Dashboard.Services.Worker;

internal interface IWorkerAgent
{
    bool IsRunning { get; }
    event EventHandler<WorkerRunStateChangedEventArgs> WorkerRunStateChanged;
    void SetCoreCount(uint coreCount);
    void SetThresholdValue(uint threshold);
    void SetWorkerMode(WorkerSettings.WorkerModeSetting mode);
    void SetLocalWorker(IRemoteWorkerAgent worker);
    void SetMinimumFreeMemoryMiB(uint memory);
    void RestartWorker();
    void Initialize();
    WorkerCoreStatus[] GetStatus();
    WorkerSettings GetSettings();
}