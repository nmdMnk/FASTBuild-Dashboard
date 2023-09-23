using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Timers;
using Caliburn.Micro;
using FastBuild.Dashboard.Services.RemoteWorker;
using FastBuild.Dashboard.Services.Worker;

namespace FastBuild.Dashboard.Services;

public class WorkerListChangedEventArgs : EventArgs
{
    public HashSet<IRemoteWorkerAgent> RemoteWorkers { get; set; }
}

internal class BrokerageService : IBrokerageService
{
    private const string WorkerPoolRelativePath = @"main\22.windows";

    private bool _isUpdatingWorkers;

    private string[] _workerNames;

    public BrokerageService()
    {
        _workerNames = new string[0];

        var checkTimer = new Timer(5000);
        checkTimer.Elapsed += CheckTimer_Elapsed;
        checkTimer.AutoReset = true;
        checkTimer.Enabled = true;
        UpdateWorkers();
    }

    public string[] WorkerNames
    {
        get => _workerNames;
        private set
        {
            var oldCount = _workerNames.Length;
            _workerNames = value;

            if (oldCount != _workerNames.Length) WorkerCountChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    public string BrokeragePath
    {
        get => Environment.GetEnvironmentVariable("FASTBUILD_BROKERAGE_PATH");
        set => Environment.SetEnvironmentVariable("FASTBUILD_BROKERAGE_PATH", value);
    }

    public event EventHandler WorkerCountChanged;
    public event EventHandler<WorkerListChangedEventArgs> WorkerListChanged;

    private void CheckTimer_Elapsed(object sender, ElapsedEventArgs e)
    {
        UpdateWorkers();
    }

    private void UpdateWorkers()
    {
        if (_isUpdatingWorkers)
            return;

        _isUpdatingWorkers = true;
        HashSet<IRemoteWorkerAgent> remoteWorkers = new HashSet<IRemoteWorkerAgent>();

        try
        {
            var brokeragePath = BrokeragePath;
            if (string.IsNullOrEmpty(brokeragePath))
            {
                remoteWorkers = new HashSet<IRemoteWorkerAgent>();
                WorkerNames = new string[0];
                return;
            }

            try
            {
                WorkerNames = Directory.GetFiles(Path.Combine(brokeragePath, WorkerPoolRelativePath))
                    .Select(Path.GetFullPath)
                    .ToArray();

                foreach (var workerFile in WorkerNames)
                {
                    IRemoteWorkerAgent worker = RemoteWorkerAgent.CreateFromFile(workerFile);
                    if (worker == null)
                        continue;

                    remoteWorkers.Add(worker);

                    if (worker.IsLocal) IoC.Get<IWorkerAgentService>().SetLocalWorker(worker);
                }
            }
            catch (IOException)
            {
                remoteWorkers = new HashSet<IRemoteWorkerAgent>();
                WorkerNames = new string[0];
            }
        }
        finally
        {
            var args = new WorkerListChangedEventArgs();
            args.RemoteWorkers = remoteWorkers;

            WorkerListChanged?.Invoke(this, args);

            _isUpdatingWorkers = false;
        }
    }
}