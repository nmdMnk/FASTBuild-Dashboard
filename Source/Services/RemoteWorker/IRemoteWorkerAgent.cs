namespace FastBuild.Dashboard.Services.RemoteWorker;

public interface IRemoteWorkerAgent
{
    string FilePath { get; }
    bool IsLocal { get; }
    string Version { get; }
    string User { get; }
    string HostName { get; }
    string IPv4Address { get; }
    string DomainName { get; }
    string FQDN { get; }
    string CPUs { get; }
    string Memory { get; }
    string Mode { get; }
}