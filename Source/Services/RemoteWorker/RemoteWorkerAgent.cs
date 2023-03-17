using System.IO;
using System.Net;

namespace FastBuild.Dashboard.Services.RemoteWorker;

internal class RemoteWorkerAgent : IRemoteWorkerAgent
{
    private RemoteWorkerAgent()
    {
    }

    public string FilePath { get; private set; }
    public bool IsLocal { get; private set; }
    public string Version { get; private set; }
    public string User { get; private set; }
    public string HostName { get; private set; }
    public string IPv4Address { get; private set; }
    public string DomainName { get; private set; }
    public string FQDN { get; private set; }
    public string CPUs { get; private set; }
    public string Memory { get; private set; }
    public string Mode { get; private set; }

    public static RemoteWorkerAgent CreateFromFile(string filePath)
    {
        if (!File.Exists(filePath))
            return null;

        var worker = new RemoteWorkerAgent();
        worker.FilePath = filePath;

        try
        {
            foreach (var line in File.ReadAllLines(filePath))
            {
                var propertyName = "";
                var propertyValue = "";

                try
                {
                    var data = line.Split(':');
                    propertyName = data[0].Trim().Replace(" ", "");
                    propertyValue = data[1].Trim();

                    var property = typeof(RemoteWorkerAgent).GetProperty(propertyName);
                    property.SetValue(worker, propertyValue);
                }
                catch
                {
                    //Console.WriteLine($"WARNING: {filePath} has invalid values (Property: {propertyName} - Value: {propertyValue}).");
                    return null;
                }
            }
        }
        catch
        {
            //Console.WriteLine($"WARNING: {filePath} is not valid.");
            return null;
        }

        if (worker.HostName == Dns.GetHostName())
            worker.IsLocal = true;
        else
            worker.IsLocal = false;

        return worker;
    }
}