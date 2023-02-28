using FastBuild.Dashboard.Services.RemoteWorker;
using System;

namespace FastBuild.Dashboard.Services.Worker
{
    internal interface IWorkerAgentService
	{
		int WorkerCores { get; set; }
		int WorkerThreshold { get; set; }
		WorkerMode WorkerMode { get; set; }
		bool IsRunning { get; }
		event EventHandler<WorkerRunStateChangedEventArgs> WorkerRunStateChanged;
		void Initialize();
		WorkerCoreStatus[] GetStatus();

		void SetLocalWorker(IRemoteWorkerAgent worker);
	}
}
