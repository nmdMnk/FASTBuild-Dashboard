using System;
using Caliburn.Micro;

namespace FastBuild.Dashboard.Services.Build;

internal class BuildViewportServiceXamlSupport : PropertyChangedBase
{
    static BuildViewportServiceXamlSupport()
    {
        Instance = new BuildViewportServiceXamlSupport();
    }

    public BuildViewportServiceXamlSupport()
    {
        var viewportService = IoC.Get<IBuildViewportService>();
        viewportService.BuildJobDisplayModeChanged += ViewportService_BuildJobDisplayModeChanged;
    }

    public static BuildViewportServiceXamlSupport Instance { get; }


    public BuildJobDisplayMode JobDisplayMode => IoC.Get<IBuildViewportService>().BuildJobDisplayMode;

    public bool IsCompactDisplayMode
    {
        get => JobDisplayMode == BuildJobDisplayMode.Compact;
        set => IoC.Get<IBuildViewportService>()
            .SetBuildJobDisplayMode(value ? BuildJobDisplayMode.Compact : BuildJobDisplayMode.Standard);
    }

    private void ViewportService_BuildJobDisplayModeChanged(object sender, EventArgs e)
    {
        this.NotifyOfPropertyChange(nameof(JobDisplayMode));
        this.NotifyOfPropertyChange(nameof(IsCompactDisplayMode));
    }
}