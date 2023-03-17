using Caliburn.Micro;
using FastBuild.Dashboard.Services.RemoteWorker;

namespace FastBuild.Dashboard.ViewModels.Broker;

internal class RemoteWorkerModel : PropertyChangedBase
{
    private string _cPUs;

    private string _hostName;

    private string _iPv4Address;

    private string _memory;

    private string _mode;

    private string _user;

    private string _version;

    public RemoteWorkerModel(IRemoteWorkerAgent workerAgent)
    {
        IsDirty = false;
        _version = workerAgent.Version;
        _user = workerAgent.User;
        _hostName = workerAgent.HostName;
        _iPv4Address = workerAgent.IPv4Address;
        _cPUs = workerAgent.CPUs;
        _memory = workerAgent.Memory;
        _mode = workerAgent.Mode;
    }

    public bool IsDirty { get; set; }

    public string Version
    {
        get => _version;
        set
        {
            _version = value;
            this.NotifyOfPropertyChange();
        }
    }

    public string User
    {
        get => _user;
        set
        {
            _user = value;
            this.NotifyOfPropertyChange();
        }
    }

    public string HostName
    {
        get => _hostName;
        set
        {
            _hostName = value;
            this.NotifyOfPropertyChange();
        }
    }

    public string IPv4Address
    {
        get => _iPv4Address;
        set
        {
            _iPv4Address = value;
            this.NotifyOfPropertyChange();
        }
    }

    public string CPUs
    {
        get => _cPUs;
        set
        {
            _cPUs = value;
            this.NotifyOfPropertyChange();
        }
    }

    public string Memory
    {
        get => _memory;
        set
        {
            _memory = value;
            this.NotifyOfPropertyChange();
        }
    }

    public string Mode
    {
        get => _mode;
        set
        {
            _mode = value;
            this.NotifyOfPropertyChange();
        }
    }

    public bool UpdateData(RemoteWorkerModel newWorkerData)
    {
        if (newWorkerData == null)
            return false;

        IsDirty = false;

        if (newWorkerData == this)
            return false;

        Version = newWorkerData.Version;
        User = newWorkerData.User;
        HostName = newWorkerData.HostName;
        IPv4Address = newWorkerData.IPv4Address;
        CPUs = newWorkerData.CPUs;
        Memory = newWorkerData.Memory;
        Mode = newWorkerData.Mode;

        return true;
    }

    /*
    public static bool operator !=(RemoteWorkerModel lhs, RemoteWorkerModel rhs)
    {
        return !(lhs == rhs);
    }
    public static bool operator ==(RemoteWorkerModel lhs, RemoteWorkerModel rhs)
    {
        if (lhs is null || rhs is null)
            return false;

        return (lhs.Version == rhs.Version
            && lhs.User == rhs.User
            && lhs.HostName == rhs.HostName
            && lhs.IPv4Address == rhs.IPv4Address
            && lhs.CPUs == rhs.CPUs
            && lhs.Memory == rhs.Memory
            && lhs.Mode == rhs.Mode);
    }

    public override bool Equals(object o)
    {
        try
        {
            return (this == (RemoteWorkerModel)o);
        }
        catch
        {
            return false;
        }

    }
    public override int GetHashCode()
    {
        return base.GetHashCode();
    }
    */
}