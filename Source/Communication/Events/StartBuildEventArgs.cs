namespace FastBuild.Dashboard.Communication.Events;

internal class StartBuildEventArgs : BuildEventArgs
{
    public const string StartBuildEventName = "START_BUILD";

    public int LogVersion { get; private set; }
    public int ProcessId { get; private set; }

    public static StartBuildEventArgs Parse(string[] tokens)
    {
        var args = new StartBuildEventArgs();
        ParseBase(tokens, args);
        
        if (int.TryParse(tokens[EventArgStartIndex], out int LogVersion))
        {
            args.LogVersion = LogVersion;
        }
        if (int.TryParse(tokens[EventArgStartIndex + 1], out int ProcessId))
        {
            args.ProcessId = ProcessId;
        }
        else if (long.TryParse(tokens[0], out long ProcessId2))
        {
            args.ProcessId = (int)ProcessId2;
        }
        else
        {
            args.ProcessId = 0;
        }
        
        return args;
    }
}