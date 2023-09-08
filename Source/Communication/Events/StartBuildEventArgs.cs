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
        
        //need to be defensive here
        if (tokens.Length > 0)
        {
            ParseBase(tokens, args);
            if (int.TryParse(tokens[EventArgStartIndex], out int logVersion))
            {
                args.LogVersion = logVersion;
            }
            
            if (int.TryParse(tokens[EventArgStartIndex + 1], out int parsed))
            {
                args.ProcessId = parsed;
            }
            else if (long.TryParse(tokens[0], out long processId))
            {
                args.ProcessId = (int)processId;
            }
            else
            {
                //not ideal but gets things going
                args.ProcessId = 0;
            }
        }
        return args;
    }
}