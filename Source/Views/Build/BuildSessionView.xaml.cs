using System;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;
using Caliburn.Micro;
using FastBuild.Dashboard.Services.Build;

namespace FastBuild.Dashboard.Views.Build;

public partial class BuildSessionView
{
    private bool _isAutoScrollingContent;
    private double _previousHorizontalScrollOffset;

    public BuildSessionView()
    {
        InitializeComponent();
        _isAutoScrollingContent = true;
        _previousHorizontalScrollOffset = ContentScrollViewer.ScrollableWidth;
        BuildViewportService.BuildJobDisplayModeChanged += BuildViewportService_BuildJobDisplayModeChanged;
        NotifyVerticalViewRangeChanged();
    }

    private static IBuildViewportService BuildViewportService => IoC.Get<IBuildViewportService>();
    private double HeaderViewWidth => App.CachedResource<double>.GetResource("HeaderViewWidth");

    private void BuildViewportService_BuildJobDisplayModeChanged(object sender, EventArgs e)
    {
        // when this event is triggered, the layout of header/background is not updated immediately.
        // we use a one-shot timer to delay a synchronization among scroll viewers

        var t = new DispatcherTimer();

        void Callback(object callbackSender, EventArgs callbackArgs)
        {
            SynchronizeScrollViewers();
            t.Tick -= Callback;
            t.IsEnabled = false;
        }

        t.Tick += Callback;

        t.IsEnabled = true;
    }

    private void ContentScrollViewer_ScrollChanged(object sender, ScrollChangedEventArgs e)
    {
        var horizontalOffset = ContentScrollViewer.HorizontalOffset;
        if (_isAutoScrollingContent)
            // ReSharper disable once CompareOfFloatsByEqualityOperator
            if (horizontalOffset ==
                _previousHorizontalScrollOffset) // which means the scroll is not actually changed, but content size is changed
            {
                ContentScrollViewer.ScrollToRightEnd();
                horizontalOffset = ContentScrollViewer.ScrollableWidth;
            }

        // if we are scrolled to the right end, turn on auto scrolling
        _isAutoScrollingContent = Math.Abs(horizontalOffset - ContentScrollViewer.ScrollableWidth) < 1;

        _previousHorizontalScrollOffset = horizontalOffset;

        SynchronizeScrollViewers();

        NotifyVerticalViewRangeChanged();

        var startTime = (ContentScrollViewer.HorizontalOffset - HeaderViewWidth) / BuildViewportService.Scaling;
        var duration = ContentScrollViewer.ActualWidth / BuildViewportService.Scaling;
        var endTime = startTime + duration;
        BuildViewportService.SetViewTimeRange(startTime, endTime);
    }

    private void SynchronizeScrollViewers()
    {
        if (ContentScrollViewer.ScrollableHeight > 0)
        {
            var offset = ContentScrollViewer.VerticalOffset * HeaderScrollViewer.ScrollableHeight /
                         ContentScrollViewer.ScrollableHeight;
            HeaderScrollViewer.ScrollToVerticalOffset(offset);
            BackgroundScrollViewer.ScrollToVerticalOffset(offset);
        }
        else
        {
            HeaderScrollViewer.ScrollToVerticalOffset(0);
            BackgroundScrollViewer.ScrollToVerticalOffset(0);
        }
    }

    private void NotifyVerticalViewRangeChanged()
    {
        BuildViewportService.SetVerticalViewRange(ContentScrollViewer.VerticalOffset,
            ContentScrollViewer.VerticalOffset + ContentScrollViewer.ViewportHeight);
    }

    private void UserControl_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
    {
        if (Keyboard.Modifiers == ModifierKeys.Control)
        {
            var buildViewportService = IoC.Get<IBuildViewportService>();

            var relativePosition = e.GetPosition(this);
            var proportion = relativePosition.X / this.ActualWidth;

            var fixedTime = buildViewportService.ViewStartTimeOffsetSeconds
                            + (buildViewportService.ViewEndTimeOffsetSeconds -
                               buildViewportService.ViewStartTimeOffsetSeconds)
                            * proportion;

            buildViewportService.Scaling = buildViewportService.Scaling * (1 + e.Delta / 1200.0);

            var startTime = fixedTime - ContentScrollViewer.ActualWidth / buildViewportService.Scaling * proportion;

            ContentScrollViewer.ScrollToHorizontalOffset(startTime * buildViewportService.Scaling + HeaderViewWidth);

            e.Handled = true;
        }
    }

    private void BuildJobsView_SizeChanged(object sender, System.Windows.SizeChangedEventArgs e)
    {
        NotifyVerticalViewRangeChanged();
    }
}