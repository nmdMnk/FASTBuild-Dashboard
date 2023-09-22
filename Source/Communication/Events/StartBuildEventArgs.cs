using System;
namespace FastBuild.Dashboard.Communication.Events;

internal class StartBuildEventArgs : BuildEventArgs
{
    public const string StartBuildEventName = "START_BUILD";

    public int LogVersion { get; private set; }
    public int ProcessId { get; private set; }

    public static StartBuildEventArgs Parse(string[] tokens)
    {
        var args = new StartBuildEventArgs();
        if (tokens.Length > 0)
        {
            ParseBase(tokens, args);
            //args.LogVersion = int.Parse(tokens[EventArgStartIndex]);
            
            if (int.TryParse(tokens[3], out int parsed))
            {
                args.ProcessId = parsed;
            }
            else if (long.TryParse(tokens[0], out long parsedlong))
            {
                args.ProcessId = (int)parsedlong;
            }
            else
            {
                args.ProcessId = 0;
            }
        }
        return args;
    }
}