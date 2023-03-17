using System;
using System.Drawing;
using Caliburn.Micro;
using FastBuild.Dashboard.Services.Worker;

namespace FastBuild.Dashboard.ViewModels.Worker;

internal class WorkerCoreStatusViewModel : PropertyChangedBase
{
    private WorkerCoreStatus _status;

    public WorkerCoreStatusViewModel(int coreId)
    {
        CoreId = coreId;
    }

    public int CoreId { get; }
    public string HostHelping => _status.HostHelping;
    public string WorkingItem => _status.WorkingItem;
    public bool IsWorking => _status.State == WorkerCoreState.Working;

    public string DisplayState
    {
        get
        {
            switch (_status.State)
            {
                case WorkerCoreState.Disabled:
                    return "Disabled";
                case WorkerCoreState.Idle:
                    return "Idle";
                case WorkerCoreState.Working:
                    return "Working";
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }

    public Brush UIBulbBorderColor
    {
        get
        {
            switch (_status.State)
            {
                case WorkerCoreState.Disabled:
                    return Brushes.Gray;
                case WorkerCoreState.Idle:
                case WorkerCoreState.Working:
                    return Brushes.DarkGreen;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }

    public Brush UIBulbFillColor
    {
        get
        {
            switch (_status.State)
            {
                case WorkerCoreState.Disabled:
                case WorkerCoreState.Idle:
                    return Brushes.Transparent;
                case WorkerCoreState.Working:
                    return Brushes.Green;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }

    public Brush UIBulbForeground
    {
        get
        {
            switch (_status.State)
            {
                case WorkerCoreState.Disabled:
                    return Brushes.Gray;
                case WorkerCoreState.Idle:
                    return Brushes.Green;
                case WorkerCoreState.Working:
                    return Brushes.White;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }

    public void UpdateStatus(WorkerCoreStatus status)
    {
        try
        {
            _status = status;
            this.NotifyOfPropertyChange(nameof(HostHelping));
            this.NotifyOfPropertyChange(nameof(WorkingItem));
            this.NotifyOfPropertyChange(nameof(UIBulbBorderColor));
            this.NotifyOfPropertyChange(nameof(UIBulbFillColor));
            this.NotifyOfPropertyChange(nameof(UIBulbForeground));
            this.NotifyOfPropertyChange(nameof(IsWorking));
            this.NotifyOfPropertyChange(nameof(DisplayState));
        }
        catch
        {
        }
    }
}