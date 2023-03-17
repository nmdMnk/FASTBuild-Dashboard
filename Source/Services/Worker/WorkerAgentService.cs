using System;
using FastBuild.Dashboard.Configuration;
using FastBuild.Dashboard.Services.RemoteWorker;

namespace FastBuild.Dashboard.Services.Worker;

internal class WorkerAgentService : IWorkerAgentService
{
    private readonly IWorkerAgent _workerAgent;

    public WorkerAgentService()
    {
        _workerAgent = new ExternalWorkerAgent();
        _workerAgent.WorkerRunStateChanged += WorkerAgent_WorkerRunStateChanged;
    }

    public int WorkerCores
    {
        get
        {
            var cores = AppSettings.Default.WorkerCores;
            if (cores <= 0) cores = Environment.ProcessorCount;

            return cores;
        }
        set
        {
            AppSettings.Default.WorkerCores = value;
            AppSettings.Default.Save();

            if (_workerAgent.IsRunning) _workerAgent.SetCoreCount(WorkerCores);
        }
    }

    public int WorkerThreshold
    {
        get => AppSettings.Default.WorkerThreshold;
        set
        {
            AppSettings.Default.WorkerThreshold = value;
            AppSettings.Default.Save();

            if (_workerAgent.IsRunning) _workerAgent.SetThresholdValue(WorkerThreshold);
        }
    }

    public WorkerMode WorkerMode
    {
        get => (WorkerMode)AppSettings.Default.WorkerMode;
        set
        {
            AppSettings.Default.WorkerMode = (int)value;
            AppSettings.Default.Save();

            if (_workerAgent.IsRunning) _workerAgent.SetWorkerMode(WorkerMode);
        }
    }

    public bool IsRunning => _workerAgent.IsRunning;
    public event EventHandler<WorkerRunStateChangedEventArgs> WorkerRunStateChanged;

    public void Initialize()
    {
        _workerAgent.Initialize();
        if (_workerAgent.IsRunning)
        {
            _workerAgent.SetCoreCount(WorkerCores);
            _workerAgent.SetWorkerMode(WorkerMode);
        }
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