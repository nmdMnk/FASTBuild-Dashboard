using FastBuild.Dashboard.Services.RemoteWorker;
using System;

namespace FastBuild.Dashboard.Services.Worker
{
    internal interface IWorkerAgent
    {
        event EventHandler<WorkerRunStateChangedEventArgs> WorkerRunStateChanged;
		bool IsRunning { get; }
		void SetCoreCount(int coreCount);
        void SetThresholdValue(int threshold);
        void SetWorkerMode(WorkerMode mode);
        void SetLocalWorker(IRemoteWorkerAgent worker);
		void Initialize();
		WorkerCoreStatus[] GetStatus();
	}
}
