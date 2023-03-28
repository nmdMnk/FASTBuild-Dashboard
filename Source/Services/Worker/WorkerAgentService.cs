using System;
using FastBuild.Dashboard.Services.RemoteWorker;

namespace FastBuild.Dashboard.Services.Worker;

internal class WorkerAgentService : IWorkerAgentService
{
    private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();
    private readonly IWorkerAgent _workerAgent;

    public WorkerAgentService()
    {
        _workerAgent = new ExternalWorkerAgent();
        _workerAgent.WorkerRunStateChanged += WorkerAgent_WorkerRunStateChanged;
        Logger.Info("Created");
    }

    public uint WorkerCores
    {
        get => _workerAgent.GetSettings().NumCPUsToUse;
        set
        {
            value = Math.Min(value, (uint)Environment.ProcessorCount);
            _workerAgent.SetCoreCount(value);
        }
    }

    public uint WorkerThreshold
    {
        get => _workerAgent.GetSettings().IdleThresholdPercent;
        set
        {
            value = Math.Min(value, 100);
            _workerAgent.SetThresholdValue(value);
        }
    }

    public WorkerSettings.WorkerModeSetting WorkerMode
    {
        get => _workerAgent.GetSettings().WorkerMode;
        set => _workerAgent.SetWorkerMode(value);
    }

    public uint MinFreeMemoryMiB
    {
        get => _workerAgent.GetSettings().MinimumFreeMemoryMiB;
        set => _workerAgent.SetMinimumFreeMemoryMiB(value);
    }

    public bool IsRunning => _workerAgent.IsRunning;
    public bool IsPendingRestart => _workerAgent.IsRunning && _workerAgent.GetSettings().SettingsAreDirty;
    public event EventHandler<WorkerRunStateChangedEventArgs> WorkerRunStateChanged;

    public void Initialize()
    {
        Logger.Info("Initialize");
        _workerAgent.Initialize();
    }

    public WorkerCoreStatus[] GetStatus()
    {
        return _workerAgent.GetStatus();
    }

    public void SetLocalWorker(IRemoteWorkerAgent worker)
    {
        _workerAgent.SetLocalWorker(worker);
    }

    private void WorkerAgent_WorkerRunStateChanged(object sender, WorkerRunStateChangedEventArgs e)
    {
        WorkerRunStateChanged?.Invoke(this, e);
    }
}