using System;
using System.Diagnostics.CodeAnalysis;
using FastBuild.Dashboard.Configuration;

namespace FastBuild.Dashboard.Services.Build;

internal class BuildViewportService : IBuildViewportService
{
    private const double StandardScaling = 50;
    private const double MinimumScaling = 0.4;
    private const double MaximumScaling = 1024;

    private double _scaling = StandardScaling;

    public double Scaling
    {
        get => _scaling;
        set
        {
            // ReSharper disable once CompareOfFloatsByEqualityOperator
            if (_scaling == value) return;

            _scaling = Math.Min(Math.Max(value, MinimumScaling), MaximumScaling);
            ScalingChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    public double ViewStartTimeOffsetSeconds { get; private set; }
    public double ViewEndTimeOffsetSeconds { get; private set; }
    public double ViewTop { get; private set; }
    public double ViewBottom { get; private set; }

    public BuildJobDisplayMode BuildJobDisplayMode
    {
        get => (BuildJobDisplayMode)Profile.Default.BuildJobDisplayMode;
        private set
        {
            Profile.Default.BuildJobDisplayMode = (int)value;
            Profile.Default.Save();
        }
    }

    public event EventHandler ScalingChanged;
    public event EventHandler ViewTimeRangeChanged;
    public event EventHandler VerticalViewRangeChanged;
    public event EventHandler BuildJobDisplayModeChanged;


    [SuppressMessage("ReSharper", "CompareOfFloatsByEqualityOperator")]
    public void SetViewTimeRange(double startTime, double endTime)
    {
        if (ViewStartTimeOffsetSeconds == startTime && ViewEndTimeOffsetSeconds == endTime) return;

        ViewStartTimeOffsetSeconds = startTime;
        ViewEndTimeOffsetSeconds = endTime;
        ViewTimeRangeChanged?.Invoke(this, EventArgs.Empty);
    }


    [SuppressMessage("ReSharper", "CompareOfFloatsByEqualityOperator")]
    public void SetVerticalViewRange(double top, double bottom)
    {
        if (ViewTop == top && ViewBottom == bottom) return;

        ViewTop = top;
        ViewBottom = bottom;
        VerticalViewRangeChanged?.Invoke(this, EventArgs.Empty);
    }

    public void SetBuildJobDisplayMode(BuildJobDisplayMode mode)
    {
        BuildJobDisplayMode = mode;
        BuildJobDisplayModeChanged?.Invoke(this, EventArgs.Empty);
    }
}