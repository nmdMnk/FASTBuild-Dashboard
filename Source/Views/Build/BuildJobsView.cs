using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Caliburn.Micro;
using FastBuild.Dashboard.Services.Build;
using FastBuild.Dashboard.ViewModels.Build;
using Action = System.Action;

namespace FastBuild.Dashboard.Views.Build;

public partial class BuildJobsView : Control
{
    private readonly IBuildViewportService _buildViewportService;

    // a set that stores all the cores visible to current viewport
    private readonly HashSet<BuildCoreViewModel> _visibleCores
        = new HashSet<BuildCoreViewModel>();

    private double _currentTimeOffset;
    private double _endTimeOffset;

    private BuildSessionJobManager _jobManager;
    private BuildSessionViewModel _sessionViewModel;

    private double _startTimeOffset;
    private bool _wasNowInTimeFrame;


    public BuildJobsView()
    {
        _buildViewportService = IoC.Get<IBuildViewportService>();

        this.DataContextChanged += FastBuildJobsView_DataContextChanged;

        this.Background = Brushes.Transparent;

        InitializeLayoutPart();
        InitializeRenderPart();
        InitializeTooltipPart();
    }

    private void FastBuildJobsView_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        if (_sessionViewModel != null)
        {
            _sessionViewModel.Ticked -= OnTicked;
            _sessionViewModel = null;

            _jobManager.OnJobFinished -= JobManager_OnJobFinished;
            _jobManager.OnJobStarted -= JobManager_OnJobStarted;
            _jobManager = null;

            Clear();
        }

        var vm = this.DataContext as BuildSessionViewModel;
        if (vm == null) return;

        _sessionViewModel = vm;
        _sessionViewModel.Ticked += OnTicked;

        _jobManager = vm.JobManager;
        _jobManager.OnJobStarted += JobManager_OnJobStarted;
        _jobManager.OnJobFinished += JobManager_OnJobFinished;

        UpdateTimeFrame();

        InvalidateCores();
        InvalidateJobs();
    }

    private void JobManager_OnJobFinished(object sender, BuildJobViewModel e)
    {
        this.Dispatcher.BeginInvoke(new Action(this.InvalidateVisual));
    }

    private void JobManager_OnJobStarted(object sender, BuildJobViewModel job)
    {
        this.Dispatcher.BeginInvoke(new Action(() =>
        {
            InvalidateCores();

            if (job.StartTimeOffset <= _endTimeOffset && job.EndTimeOffset >= _startTimeOffset) TryAddJob(job);

            this.InvalidateVisual();
        }));
    }


    private void OnTicked(object sender, double timeOffset)
    {
        this.Dispatcher.BeginInvoke(new Action(() =>
        {
            _currentTimeOffset = timeOffset;

            var isNowInTimeFrame = _endTimeOffset >= _currentTimeOffset && _startTimeOffset <= _currentTimeOffset;
            if (isNowInTimeFrame)
                if (!_wasNowInTimeFrame)
                    // "now" has come into current time frame, add all active jobs
                    foreach (var job in _jobManager.GetAllJobs().Where(j => !j.IsFinished))
                        TryAddJob(job);

            this.InvalidateMeasure();

            _wasNowInTimeFrame = isNowInTimeFrame;
        }));
    }
}