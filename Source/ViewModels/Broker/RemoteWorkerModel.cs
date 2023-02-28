using Caliburn.Micro;
using FastBuild.Dashboard.Services.RemoteWorker;

namespace FastBuild.Dashboard.ViewModels.Broker
{
    internal class RemoteWorkerModel : PropertyChangedBase
    {
        public bool IsDirty { get; set; }

        private string _version;
        public string Version
        {
            get => _version;
            set
            {
                _version = value;
                this.NotifyOfPropertyChange();
            }
        }

        private string _user;
        public string User
        {
            get => _user;
            set
            {
                _user = value;
                this.NotifyOfPropertyChange();
            }
        }

        private string _hostName;
        public string HostName
        {
            get => _hostName;
            set
            {
                _hostName = value;
                this.NotifyOfPropertyChange();
            }
        }

        private string _iPv4Address;
        public string IPv4Address
        {
            get => _iPv4Address;
            set
            {
                _iPv4Address = value;
                this.NotifyOfPropertyChange();
            }
        }

        private string _cPUs;
        public string CPUs
        {
            get => _cPUs;
            set
            {
                _cPUs = value;
                this.NotifyOfPropertyChange();
            }
        }

        private string _memory;
        public string Memory
        {
            get => _memory;
            set
            {
                _memory = value;
                this.NotifyOfPropertyChange();
            }
        }

        private string _mode;
        public string Mode
        {
            get => _mode;
            set
            {
                _mode = value;
                this.NotifyOfPropertyChange();
            }
        }

        public RemoteWorkerModel(IRemoteWorkerAgent workerAgent)
        {
            this.IsDirty = false;
            this._version = workerAgent.Version;
            this._user = workerAgent.User;
            this._hostName = workerAgent.HostName;
            this._iPv4Address = workerAgent.IPv4Address;
            this._cPUs = workerAgent.CPUs;
            this._memory = workerAgent.Memory;
            this._mode = workerAgent.Mode;
        }
        public bool UpdateData(RemoteWorkerModel newWorkerData)
        {
            if (newWorkerData == null)
                return false;

            this.IsDirty = false;

            if ((RemoteWorkerModel)newWorkerData == this)
                return false;

            this.Version = newWorkerData.Version;
            this.User = newWorkerData.User;
            this.HostName = newWorkerData.HostName;
            this.IPv4Address = newWorkerData.IPv4Address;
            this.CPUs = newWorkerData.CPUs;
            this.Memory = newWorkerData.Memory;
            this.Mode = newWorkerData.Mode;

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
}
