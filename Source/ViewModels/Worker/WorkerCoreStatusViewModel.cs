using System;
using System.Windows;
using System.Windows.Media;
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
                    return (SolidColorBrush)Application.Current.FindResource("JobBorderBrush_Building");
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
                    return (SolidColorBrush)Application.Current.FindResource("JobBorderBrush_Building");
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
                    return (SolidColorBrush)Application.Current.FindResource("JobBackgroundBrush_Success");
                case WorkerCoreState.Working:
                    return (SolidColorBrush)Application.Current.FindResource("JobBackgroundBrush_Building");
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
            NotifyOfPropertyChange(nameof(HostHelping));
            NotifyOfPropertyChange(nameof(WorkingItem));
            NotifyOfPropertyChange(nameof(UIBulbBorderColor));
            NotifyOfPropertyChange(nameof(UIBulbFillColor));
            NotifyOfPropertyChange(nameof(UIBulbForeground));
            NotifyOfPropertyChange(nameof(IsWorking));
            NotifyOfPropertyChange(nameof(DisplayState));
        }
        catch (Exception)
        {
            // ignored
        }
    }
}