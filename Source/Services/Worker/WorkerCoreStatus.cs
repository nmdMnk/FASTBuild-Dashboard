namespace FastBuild.Dashboard.Services.Worker;

internal class WorkerCoreStatus
{
    public WorkerCoreStatus(WorkerCoreState state, string hostHelping = null, string workingItem = null)
    {
        State = state;
        HostHelping = hostHelping;
        WorkingItem = workingItem;
    }

    public WorkerCoreState State { get; }
    public string HostHelping { get; }
    public string WorkingItem { get; }
}