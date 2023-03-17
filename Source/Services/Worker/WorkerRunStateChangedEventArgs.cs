using System;

namespace FastBuild.Dashboard.Services.Worker;

internal class WorkerRunStateChangedEventArgs : EventArgs
{
    public WorkerRunStateChangedEventArgs(bool isRunning, string errorMessage)
    {
        IsRunning = isRunning;
        ErrorMessage = errorMessage;
    }

    public bool IsRunning { get; }
    public string ErrorMessage { get; }
}