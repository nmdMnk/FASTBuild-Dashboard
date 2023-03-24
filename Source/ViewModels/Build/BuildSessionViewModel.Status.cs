using System;
using Caliburn.Micro;
using FastBuild.Dashboard.Services;

namespace FastBuild.Dashboard.ViewModels.Build;

internal partial class BuildSessionViewModel
{
    private int _activeCoreCount;
    private int _activeWorkerCount;
    private int _cacheHitCount;
    private int _failedJobCount;
    private int _inProgressJobCount;
    private bool _isRestoringHistory;
    private bool _isRunning;
    private string[] _poolWorkerNames;
    private double _progress;
    private int _successfulJobCount;

    public bool IsRestoringHistory
    {
        get => _isRestoringHistory;
        set
        {
            if (value == _isRestoringHistory) 
                return;

            _isRestoringHistory = value;
            NotifyOfPropertyChange();
            NotifyOfPropertyChange(nameof(IsSessionViewVisible));
            NotifyOfPropertyChange(nameof(StatusText));

            if (!IsRestoringHistory)
            {
                // refresh these values after restoring history because they are not updated during the process
                // in order to increase history restoration performance
                NotifyOfPropertyChange(nameof(SuccessfulJobCount));
                NotifyOfPropertyChange(nameof(CacheHitCount));
                NotifyOfPropertyChange(nameof(InProgressJobCount));
                NotifyOfPropertyChange(nameof(FailedJobCount));

                DetectDebris();
            }
        }
    }

    public bool IsSessionViewVisible => !IsRestoringHistory;

    public bool IsRunning
    {
        get => _isRunning;
        private set
        {
            if (value == _isRunning) 
                return;

            _isRunning = value;
            NotifyOfPropertyChange();
            NotifyOfPropertyChange(nameof(StatusText));
        }
    }

    public double Progress
    {
        get => _progress;
        private set
        {
            if (value.Equals(_progress)) 
                return;

            _progress = value;
            NotifyOfPropertyChange();
            if (IsRunning) NotifyOfPropertyChange(nameof(StatusText));
        }
    }

    public string StatusText
    {
        get
        {
            if (_isRestoringHistory) 
                return $"Loading ({Progress:0}%)";

            if (IsRunning) 
                return $"Building ({Progress:0}%)";

            return "Finished";
        }
    }

    public int InProgressJobCount
    {
        get => _inProgressJobCount;
        private set
        {
            if (value == _inProgressJobCount) 
                return;

            _inProgressJobCount = value;

            if (!IsRestoringHistory) NotifyOfPropertyChange();
        }
    }

    public int SuccessfulJobCount
    {
        get => _successfulJobCount;
        private set
        {
            if (value == _successfulJobCount) 
                return;

            _successfulJobCount = value;

            if (!IsRestoringHistory) NotifyOfPropertyChange();
        }
    }

    public int FailedJobCount
    {
        get => _failedJobCount;
        private set
        {
            if (value == _failedJobCount) 
                return;

            _failedJobCount = value;

            if (!IsRestoringHistory) NotifyOfPropertyChange();
        }
    }

    public int CacheHitCount
    {
        get => _cacheHitCount;
        private set
        {
            if (value == _cacheHitCount) 
                return;

            _cacheHitCount = value;

            if (!IsRestoringHistory) NotifyOfPropertyChange();
        }
    }

    public int ActiveWorkerCount
    {
        get => _activeWorkerCount;
        private set
        {
            if (value == _activeWorkerCount) 
                return;

            _activeWorkerCount = value;
            NotifyOfPropertyChange();
        }
    }

    public int ActiveCoreCount
    {
        get => _activeCoreCount;
        private set
        {
            if (value == _activeCoreCount) 
                return;

            _activeCoreCount = value;
            NotifyOfPropertyChange();
        }
    }

    public int PoolWorkerCount => PoolWorkerNames.Length;

    public string[] PoolWorkerNames
    {
        get => _poolWorkerNames;
        private set
        {
            _poolWorkerNames = value;
            NotifyOfPropertyChange();
            NotifyOfPropertyChange(nameof(PoolWorkerCount));
        }
    }

    private void UpdateActiveWorkerAndCoreCount()
    {
        if (IsRestoringHistory) 
            return;

        var activeWorkerCount = 0;
        var activeCoreCount = 0;
        foreach (var worker in Workers)
        {
            if (worker.ActiveCoreCount > 0)
            {
                ++activeWorkerCount;
                activeCoreCount += worker.ActiveCoreCount;
            }
        }

        ActiveWorkerCount = activeWorkerCount;
        ActiveCoreCount = activeCoreCount;
    }


    private void BrokerageService_WorkerCountChanged(object sender, EventArgs e)
    {
        PoolWorkerNames = IoC.Get<IBrokerageService>().WorkerNames;
    }
}