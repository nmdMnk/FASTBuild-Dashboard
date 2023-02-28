using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Reflection;

namespace FastBuild.Dashboard.Services.RemoteWorker
{
    internal class RemoteWorkerAgent : IRemoteWorkerAgent
    {
        public string FilePath { get; private set; }
        public bool IsLocal { get; private set; }
        public string Version { get; private set; }
        public string User { get; private set; }
        public string HostName { get; private set; }
        public string IPv4Address { get; private set; }
        public string CPUs { get; private set; }
        public string Memory { get; private set; }
        public string Mode { get; private set; }

        private RemoteWorkerAgent() { }

        public static RemoteWorkerAgent CreateFromFile(string filePath)
        {
            if (!File.Exists(filePath))
                return null;

            RemoteWorkerAgent worker = new RemoteWorkerAgent();
            worker.FilePath = filePath;

            foreach (string line in File.ReadAllLines(filePath))
            {
                try
                {
                    string[] data = line.Split(':');
                    string propertyName = data[0].Trim().Replace(" ", "");
                    string propertyValue = data[1].Trim();

                    PropertyInfo property = typeof(RemoteWorkerAgent).GetProperty(propertyName);
                    property.SetValue(worker, propertyValue);
                }
                catch
                {
                    return null;
                }
            }

            if (worker.HostName == Dns.GetHostName())
                worker.IsLocal = true;
            else
                worker.IsLocal = false;

            return worker;
        }
    }
}
