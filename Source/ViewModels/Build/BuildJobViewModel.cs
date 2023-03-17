using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows.Media;
using Caliburn.Micro;
using FastBuild.Dashboard.Communication;
using FastBuild.Dashboard.Communication.Events;

namespace FastBuild.Dashboard.ViewModels.Build;

[DebuggerDisplay("Job:{" + nameof(DisplayName) + "}")]
internal class BuildJobViewModel : PropertyChangedBase, IBuildJobViewModel
{
    private double _elapsedSeconds;
    private IEnumerable<BuildErrorGroup> _errorGroups;
    private string _message;


    private BuildJobStatus _status;
    private Brush _uiBackground;
    private Brush _uiBorderBrush;
    private Pen _uiBorderPen;
    private Brush _uiForeground;

    public BuildJobViewModel(BuildCoreViewModel ownerCore, StartJobEventArgs e, BuildJobViewModel previousJob = null)
    {
        OwnerCore = ownerCore;
        PreviousJob = previousJob;
        if (previousJob != null) previousJob.NextJob = this;

        EventName = e.EventName;
        DisplayName = GenerateDisplayName(EventName);
        StartTime = e.Time;
        StartTimeOffset = (e.Time - ownerCore.OwnerWorker.OwnerSession.StartTime).TotalSeconds;
        Status = BuildJobStatus.Building;
        UpdateUIBrushes();
    }

    // double linked-list structure
    public BuildJobViewModel PreviousJob { get; }
    public IBuildJobViewModel NextJob { get; private set; }
    public DateTime StartTime { get; }
    public string DisplayStartTime => $"Started: {StartTime}";

    public DateTime EndTime => StartTime.AddSeconds(ElapsedSeconds);

    public double ElapsedSeconds
    {
        get => _elapsedSeconds;
        private set
        {
            if (value.Equals(_elapsedSeconds)) return;

            _elapsedSeconds = value;
            this.NotifyOfPropertyChange();
            this.NotifyOfPropertyChange(nameof(EndTime));
            this.NotifyOfPropertyChange(nameof(EndTimeOffset));
            this.NotifyOfPropertyChange(nameof(DisplayElapsedSeconds));
        }
    }

    public string DisplayElapsedSeconds => $"{ElapsedSeconds:0.#} seconds elapsed";

    public Pen UIBorderPen
    {
        get => _uiBorderPen;
        private set
        {
            if (object.Equals(value, _uiBorderPen)) return;

            _uiBorderPen = value;
            this.NotifyOfPropertyChange();
        }
    }

    public bool IsFinished => Status != BuildJobStatus.Building;
    public string EventName { get; }

    public string Message
    {
        get => _message;
        private set
        {
            if (value == _message) return;

            _message = value;
            this.NotifyOfPropertyChange();
            this.NotifyOfPropertyChange(nameof(HasError));
            this.NotifyOfPropertyChange(nameof(ShouldShowErrorMessage));
        }
    }

    public IEnumerable<BuildErrorGroup> ErrorGroups
    {
        get => _errorGroups;
        private set
        {
            if (Equals(value, _errorGroups)) return;

            _errorGroups = value;
            this.NotifyOfPropertyChange();
        }
    }

    public BuildJobStatus Status
    {
        get => _status;
        private set
        {
            if (value == _status) return;

            _status = value;
            this.NotifyOfPropertyChange();
            this.NotifyOfPropertyChange(nameof(IsFinished));
            this.NotifyOfPropertyChange(nameof(ElapsedSeconds));
            this.NotifyOfPropertyChange(nameof(DisplayStatus));
            this.NotifyOfPropertyChange(nameof(HasError));
            this.NotifyOfPropertyChange(nameof(ShouldShowErrorMessage));
            UpdateUIBrushes();
        }
    }

    public bool HasError => Status == BuildJobStatus.Error
                            || Status == BuildJobStatus.Failed;

    public bool ShouldShowErrorMessage => HasError
                                          && !string.IsNullOrEmpty(Message);

    public string DisplayStatus
    {
        get
        {
            switch (Status)
            {
                case BuildJobStatus.Building:
                    return "Building";
                case BuildJobStatus.Success:
                    return "Successfully Built";
                case BuildJobStatus.SuccessCached:
                    return "Successfully (Cache Hit)";
                case BuildJobStatus.SuccessPreprocessed:
                    return "Successfully Preprocessed";
                case BuildJobStatus.Failed:
                    return "Failed";
                case BuildJobStatus.Error:
                    return "Error Occurred";
                case BuildJobStatus.Timeout:
                    return "Timed Out";
                case BuildJobStatus.RacedOut:
                    return "Deprecated by Local Race";
                case BuildJobStatus.Stopped:
                    return "Stopped";
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }


    public BuildCoreViewModel OwnerCore { get; }
    public double StartTimeOffset { get; }

    public double EndTimeOffset => StartTimeOffset + ElapsedSeconds;

    public Brush UIForeground
    {
        get => _uiForeground;
        private set
        {
            if (object.Equals(value, _uiForeground)) return;

            _uiForeground = value;
            this.NotifyOfPropertyChange();
        }
    }

    public Brush UIBackground
    {
        get => _uiBackground;
        private set
        {
            if (object.Equals(value, _uiBackground)) return;

            _uiBackground = value;
            this.NotifyOfPropertyChange();
        }
    }

    public Brush UIBorderBrush
    {
        get => _uiBorderBrush;
        private set
        {
            if (object.Equals(value, _uiBorderBrush)) return;

            _uiBorderBrush = value;
            this.NotifyOfPropertyChange();
        }
    }

    public string DisplayName { get; }

    private static string GenerateDisplayName(string eventName)
    {
        return Path.GetFileName(eventName) ?? eventName;
    }

    private void UpdateUIBrushes()
    {
        UIForeground = App.CachedResource<Brush>.GetResource($"JobForegroundBrush_{Status}");
        UIBackground = App.CachedResource<Brush>.GetResource($"JobBackgroundBrush_{Status}");
        UIBorderBrush = App.CachedResource<Brush>.GetResource($"JobBorderBrush_{Status}");
        UIBorderPen = App.CachedResource<Pen>.GetResource($"JobBorderPen_{Status}");
    }


    public void OnFinished(FinishJobEventArgs e)
    {
        // already raced out
        if (Status == BuildJobStatus.RacedOut) return;

        if (e.Message != null)
        {
            var message = e.Message.TrimEnd().Replace('\f', '\n');
            var matches = Regex.Matches(message, @"^(.+)\((\d+)\)\s*\: (.+)$", RegexOptions.Multiline);
            if (matches.Count > 0)
            {
                ErrorGroups = matches.Cast<Match>()
                    .Select(m => new BuildErrorInfo(
                        m.Groups[1].Value,
                        int.Parse(m.Groups[2].Value, CultureInfo.InvariantCulture),
                        m.Groups[3].Value,
                        OwnerCore.OwnerWorker.OwnerSession.InitiatorProcess))
                    .GroupBy(i => i.FilePath, (file, group) => new BuildErrorGroup(file, group))
                    .ToArray();

                Message = null; // we don't show both message and parsed errors
            }
            else
            {
                Message = message;
            }
        }

        ElapsedSeconds = (e.Time - StartTime).TotalSeconds;
        Status = e.Result;
    }


    public void InvalidateCurrentTime(double currentTimeOffset)
    {
        if (IsFinished) return;

        UpdateDuration(currentTimeOffset);
    }

    private void UpdateDuration(double currentTimeOffset)
    {
        ElapsedSeconds = currentTimeOffset - StartTimeOffset;
    }

    public void OnSessionStopped(double currentTimeOffset)
    {
        if (!IsFinished)
        {
            Status = BuildJobStatus.Stopped;
            ElapsedSeconds = currentTimeOffset - StartTimeOffset;
        }
    }

    public void Tick(double currentTimeOffset)
    {
        InvalidateCurrentTime(currentTimeOffset);
    }
}