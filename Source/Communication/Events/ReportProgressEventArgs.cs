using System.Globalization;

namespace FastBuild.Dashboard.Communication.Events;

internal class ReportProgressEventArgs : BuildEventArgs
{
    public const string ReportProgressEventName = "PROGRESS_STATUS";

    public double Progress { get; private set; }

    public static ReportProgressEventArgs Parse(string[] tokens)
    {
        var args = new ReportProgressEventArgs();
        ParseBase(tokens, args);
        args.Progress = float.Parse(tokens[EventArgStartIndex], CultureInfo.InvariantCulture);
        return args;
    }
}