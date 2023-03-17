namespace FastBuild.Dashboard.Communication.Events;

internal class StartJobEventArgs : BuildEventArgs
{
    public const string StartJobEventName = "START_JOB";

    public string HostName { get; private set; }
    public string EventName { get; private set; }

    public static StartJobEventArgs Parse(string[] tokens)
    {
        var args = new StartJobEventArgs();
        ParseBase(tokens, args);
        args.HostName = tokens[EventArgStartIndex];
        args.EventName = tokens[EventArgStartIndex + 1];
        return args;
    }
}