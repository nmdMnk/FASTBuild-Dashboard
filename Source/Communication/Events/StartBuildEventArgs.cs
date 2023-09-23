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

        args.LogVersion = 0;
        args.ProcessId = 0;
        if (int.TryParse(tokens[EventArgStartIndex], out int parsed))
        {
            args.LogVersion = parsed;
        }
        
        if (int.TryParse(tokens[EventArgStartIndex + 1], out int parsed2))
        {
            args.ProcessId = parsed2;
        }
        
        return args;
    }
}