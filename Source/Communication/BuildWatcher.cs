using System;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using FastBuild.Dashboard.Communication.Events;
using System.Windows.Media;

namespace FastBuild.Dashboard.Communication;

internal class BuildWatcher
{
    private readonly LogWatcher _logWatcher;
    private bool _IsSessionRuning = false;

    public BuildWatcher()
    {
        _logWatcher = new LogWatcher();
        _logWatcher.HistoryRestorationStarted += LogWatcher_HistoryRestorationStarted;
        _logWatcher.HistoryRestorationEnded += LogWatcher_HistoryRestorationEnded;
        _logWatcher.LogReceived += LogWatcher_LogReceived;
        _logWatcher.LogReset += LogWatcher_LogReset;
    }

    public bool IsRestoringHistory => _logWatcher.IsRestoringHistory;

    public DateTime LastMessageTime { get; private set; }

    private static string[] Tokenize(string message)
    {
        return Regex.Matches(message, @"[\""].+?[\""]|[^ ]+")
            .Cast<Match>()
            .Select(m => m.Value.Trim('"'))
            .ToArray();
    }

    private static T ParseEventArgs<T>(string[] tokens)
        where T : BuildEventArgs
    {
        try
        {
            return (T)typeof(T).GetMethod("Parse", BindingFlags.Static | BindingFlags.Public)
                .Invoke(null, new object[] { tokens });
        }
        catch (Exception e)
        {
            throw new ParseException(string.Empty, e);
        }
    }

    public event EventHandler HistoryRestorationStarted;
    public event EventHandler HistoryRestorationEnded;
    public event EventHandler<StartBuildEventArgs> SessionStarted;
    public event EventHandler<StopBuildEventArgs> SessionStopped;
    public event EventHandler<ReportCounterEventArgs> ReportCounter;
    public event EventHandler<ReportProgressEventArgs> ReportProgress;
    public event EventHandler<StartJobEventArgs> JobStarted;
    public event EventHandler<FinishJobEventArgs> JobFinished;

    private void LogWatcher_LogReset(object sender, EventArgs e)
    {
    }

    private void LogWatcher_HistoryRestorationStarted(object sender, EventArgs e)
    {
        HistoryRestorationStarted?.Invoke(this, EventArgs.Empty);
    }

    private void LogWatcher_HistoryRestorationEnded(object sender, EventArgs e)
    {
        HistoryRestorationEnded?.Invoke(this, EventArgs.Empty);
    }

    public void Start()
    {
        _logWatcher.Start();
    }

    private void LogWatcher_LogReceived(object sender, string e)
    {
        ProcessLog(e);
    }

    private void IgnoreLog(string message)
    {
        // todo: log this message
    }

    private T ReceiveEvent<T>(string[] tokens)
        where T : BuildEventArgs
    {
        var args = ParseEventArgs<T>(tokens);
        LastMessageTime = args.Time;
        return args;
    }

    private void ProcessLog(string message)
    {
        var tokens = Tokenize(message);

        if (tokens.Length < 2)
        {
            IgnoreLog(message);
            return;
        }

        try
        {
            switch (tokens[BuildEventArgs.EventTypeArgIndex])
            {
                case StartBuildEventArgs.StartBuildEventName:
                    SessionStarted?.Invoke(this, ReceiveEvent<StartBuildEventArgs>(tokens));
                    _IsSessionRuning = true;
                    break;
                case StopBuildEventArgs.StopBuildEventName:
                    SessionStopped?.Invoke(this, ReceiveEvent<StopBuildEventArgs>(tokens));
                    _IsSessionRuning = false;
                    break;
                case StartJobEventArgs.StartJobEventName:
                    if (!_IsSessionRuning)
                    {
                        SessionStarted?.Invoke(this, ReceiveEvent<StartBuildEventArgs>(tokens));
                        _IsSessionRuning = true;
                    }

                    JobStarted?.Invoke(this, ReceiveEvent<StartJobEventArgs>(tokens));
                    break;
                case FinishJobEventArgs.FinishJobEventName:
                    JobFinished?.Invoke(this, ReceiveEvent<FinishJobEventArgs>(tokens));
                    break;
                case ReportProgressEventArgs.ReportProgressEventName:
                    ReportProgress?.Invoke(this, ReceiveEvent<ReportProgressEventArgs>(tokens));
                    break;
                case ReportCounterEventArgs.ReportCounterEventName:
                    ReportCounter?.Invoke(this, ReceiveEvent<ReportCounterEventArgs>(tokens));
                    break;
                default:
                    IgnoreLog(message);
                    break;
            }
        }
        catch (ParseException)
        {
            IgnoreLog(message);
        }
    }
}