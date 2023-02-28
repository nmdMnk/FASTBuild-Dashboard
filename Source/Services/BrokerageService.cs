using Caliburn.Micro;
using FastBuild.Dashboard.Services.RemoteWorker;
using FastBuild.Dashboard.Services.Worker;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Timers;

namespace FastBuild.Dashboard.Services
{
    public class WorkerListChangedEventArgs : EventArgs
    {
		public HashSet<IRemoteWorkerAgent> RemoteWorkers { get; set; }
    }

    internal class BrokerageService : IBrokerageService
	{
		private const string WorkerPoolRelativePath = @"main\22.windows";

		private string[] _workerNames;

		public string[] WorkerNames
		{
			get => _workerNames;
			private set
			{
				var oldCount = _workerNames.Length;
				_workerNames = value;

				if (oldCount != _workerNames.Length)
				{
					this.WorkerCountChanged?.Invoke(this, EventArgs.Empty);
				}
			}
		}

		private bool _isUpdatingWorkers;

		public string BrokeragePath
		{
			get => Environment.GetEnvironmentVariable("FASTBUILD_BROKERAGE_PATH");
			set => Environment.SetEnvironmentVariable("FASTBUILD_BROKERAGE_PATH", value);
		}

		public event EventHandler WorkerCountChanged;
        public event EventHandler<WorkerListChangedEventArgs> WorkerListChanged;

        public BrokerageService()
		{
			_workerNames = new string[0];

			var checkTimer = new Timer(5000);
			checkTimer.Elapsed += this.CheckTimer_Elapsed;
			checkTimer.AutoReset = true;
			checkTimer.Enabled = true;
			this.UpdateWorkers();
		}

		private void CheckTimer_Elapsed(object sender, ElapsedEventArgs e) => this.UpdateWorkers();

		private void UpdateWorkers()
		{
			if (_isUpdatingWorkers)
				return;

			_isUpdatingWorkers = true;
			HashSet<IRemoteWorkerAgent> remoteWorkers = new HashSet<IRemoteWorkerAgent>();

			try
			{
				var brokeragePath = this.BrokeragePath;
				if (string.IsNullOrEmpty(brokeragePath))
				{
					remoteWorkers = new HashSet<IRemoteWorkerAgent>();
					this.WorkerNames = new string[0];
					return;
				}

				try
				{
					this.WorkerNames = Directory.GetFiles(Path.Combine(brokeragePath, WorkerPoolRelativePath))
						.Select(Path.GetFullPath)
						.ToArray();

                    foreach (string workerFile in WorkerNames)
                    {
                        IRemoteWorkerAgent worker = RemoteWorkerAgent.CreateFromFile(workerFile);
						if (worker == null)
							continue;

						remoteWorkers.Add(worker);

						if (worker.IsLocal)
                        {
							IoC.Get<IWorkerAgentService>().SetLocalWorker(worker);
                        }
					}
                }
                catch (IOException)
                {
                    remoteWorkers = new HashSet<IRemoteWorkerAgent>();
					this.WorkerNames = new string[0];
				}
			}
			finally
            {
				WorkerListChangedEventArgs args = new WorkerListChangedEventArgs();
				args.RemoteWorkers = remoteWorkers;

				this.WorkerListChanged?.Invoke(this, args);

                _isUpdatingWorkers = false;
			}
		}
	}
}
