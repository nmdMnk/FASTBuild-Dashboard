using System.Collections.Generic;
using System.Linq;
using Caliburn.Micro;
using FastBuild.Dashboard.Services;

namespace FastBuild.Dashboard.ViewModels.Broker;

internal class BrokerViewModel : PropertyChangedBase, IMainPage
{
    private bool _isUpdatingWorkers;

    private BindableCollection<RemoteWorkerModel> _workerList;

    public BrokerViewModel()
    {
        WorkerList = new BindableCollection<RemoteWorkerModel>();

        var brokerageService = IoC.Get<IBrokerageService>();
        brokerageService.WorkerListChanged += BrokerageService_WorkerListChanged;
    }

    public BindableCollection<RemoteWorkerModel> WorkerList
    {
        get => _workerList;
        set
        {
            _workerList = value;
            this.NotifyOfPropertyChange();
        }
    }

    public string Icon => "AccountGroup";
    public string DisplayName => "Broker agents";

    private void BrokerageService_WorkerListChanged(object sender, WorkerListChangedEventArgs e)
    {
        if (_isUpdatingWorkers)
            return;

        _isUpdatingWorkers = true;

        lock (this)
        {
            var wasUpdated = false;
            WorkerList.ToList().ForEach(x => x.IsDirty = true);

            Stack<RemoteWorkerModel> workerList = new Stack<RemoteWorkerModel>();
            e.RemoteWorkers.ToList().ForEach(x => workerList.Push(new RemoteWorkerModel(x)));

            while (workerList.Count > 0)
            {
                RemoteWorkerModel newWorker = workerList.Pop();
                if (WorkerList.Any(x => x.HostName == newWorker.HostName))
                {
                    RemoteWorkerModel worker = WorkerList.Where(x => x.HostName == newWorker.HostName).FirstOrDefault();
                    if (worker != null)
                        if (worker.UpdateData(newWorker))
                            wasUpdated = true;
                }
                else
                {
                    WorkerList.Add(newWorker);
                    wasUpdated = true;
                }
            }

            for (var i = WorkerList.Count - 1; i >= 0; i--)
                if (WorkerList[i].IsDirty)
                {
                    WorkerList.RemoveAt(i);
                    wasUpdated = true;
                }

            if (wasUpdated)
                this.NotifyOfPropertyChange(nameof(WorkerList));
        }

        /*
        HashSet<RemoteWorkerModel> workerList = new HashSet<RemoteWorkerModel>();
        e.RemoteWorkers.ToList().ForEach(x => workerList.Add(new RemoteWorkerModel(x)));

        WorkerList = workerList;
        */
        /*
        bool wasUpdated = false;
        this._workerList.ToList().ForEach(x => x.IsDirty = true);

        Stack<RemoteWorkerModel> workerList = new Stack<RemoteWorkerModel>();
        e.RemoteWorkers.ToList().ForEach(x => workerList.Push(new RemoteWorkerModel(x)));

        while (workerList.Count > 0)
        {
            RemoteWorkerModel newWorker = workerList.Pop();
            if (_workerList.Any(x => x.HostName == newWorker.HostName))
            {
                RemoteWorkerModel worker = _workerList.Where(x => x.HostName == newWorker.HostName).FirstOrDefault();
                if (worker != null)
                { 
                    if (worker.UpdateData(newWorker))
                        wasUpdated = true;
                }
            }
            else
            {
                _workerList.Add(newWorker);
                wasUpdated = true;
            }
        }

        if (_workerList.RemoveWhere(x => x.IsDirty == true) > 0)
            wasUpdated = true;

        if (wasUpdated)
            this.NotifyOfPropertyChange(nameof(this.WorkerList));
        */
        _isUpdatingWorkers = false;
    }
}