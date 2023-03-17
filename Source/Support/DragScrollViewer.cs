using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace FastBuild.Dashboard.Support;

public class DragScrollViewer : ScrollViewer
{
    private const double DragPollingInterval = 1; // milliseconds
    private const double DefaultFriction = 0.2;
    private const double MinimumFriction = 0.0;
    private const double MaximumFriction = 1.0;

    private Point _beginPoint;
    private Point _currentPoint;
    private double _friction = DefaultFriction;
    private bool _isDragging;

    private bool _isListeningCompositionTargetRendering;

    private bool _mouseDown;
    private Point _previousPoint;
    private Point _previousPreviousPoint;

    public double Friction
    {
        get => _friction;
        set => _friction = Math.Min(Math.Max(value, MinimumFriction), MaximumFriction);
    }

    public double DragStartSensitivity { get; set; } = 2.0;


    private Vector Momentum { get; set; }
    private Vector Velocity => new Vector(_currentPoint.X - _previousPoint.X, _currentPoint.Y - _previousPoint.Y);

    // Using PreviousVelocity gives a smoother, better feeling as it leaves out any last frame momentum changes
    private Vector PreviousVelocity => new Vector(_previousPoint.X - _previousPreviousPoint.X,
        _previousPoint.Y - _previousPreviousPoint.Y);

    public event EventHandler DragBegin;
    public event EventHandler DragEnd;
    public event EventHandler SlideEnd;

    protected override void OnMouseLeave(MouseEventArgs e)
    {
        base.OnMouseLeave(e);
        CancelDrag(PreviousVelocity);
    }

    protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e)
    {
        _beginPoint = _currentPoint = _previousPoint = _previousPreviousPoint = e.GetPosition(this);
        Momentum = new Vector(0, 0);
        _mouseDown = true;
    }

    protected override void OnMouseMove(MouseEventArgs e)
    {
        _currentPoint = e.GetPosition(this);
        if (_mouseDown && !_isDragging)
            if ((_currentPoint - _beginPoint).Length >= DragStartSensitivity)
            {
                BeginDrag();
                e.Handled = true;

                _isDragging = true;
                DragScroll();
            }
    }

    protected override void OnMouseUp(MouseButtonEventArgs e)
    {
        if (_isDragging) e.Handled = true;

        CancelDrag(PreviousVelocity);
    }

    private void OnDragBegin()
    {
        DragBegin?.Invoke(this, new EventArgs());
    }

    private void BeginDrag()
    {
        this.Cursor = Cursors.ScrollAll;
        this.CaptureMouse();
        OnDragBegin();
    }

    private void OnDragEnd()
    {
        DragEnd?.Invoke(this, new EventArgs());
    }

    private void CancelDrag(Vector velocityToUse)
    {
        if (_isDragging) Momentum = velocityToUse;

        _isDragging = false;
        _mouseDown = false;
        this.Cursor = Cursors.Arrow;

        this.ReleaseMouseCapture();

        OnDragEnd();
    }


    protected void DragScroll()
    {
        if (!_isListeningCompositionTargetRendering)
        {
            CompositionTarget.Rendering += CompositionTarget_Rendering;
            _isListeningCompositionTargetRendering = true;
        }
    }

    private void OnSlideEnd()
    {
        SlideEnd?.Invoke(this, new EventArgs());
    }

    private void CompositionTarget_Rendering(object sender, EventArgs e)
    {
        if (_isDragging)
        {
            var generalTransform = this.TransformToVisual(this);
            var childToParentCoordinates = generalTransform.Transform(new Point(0, 0));
            var bounds = new Rect(childToParentCoordinates, this.RenderSize);

            if (bounds.Contains(_currentPoint)) PerformScroll(PreviousVelocity);

            if (!_mouseDown) CancelDrag(Velocity);
            _previousPreviousPoint = _previousPoint;
            _previousPoint = _currentPoint;
        }
        else if (Momentum.Length > 0.01)
        {
            Momentum *= 1.0 - _friction / 4.0;
            if (!PerformScroll(Momentum)) StopSlide();
        }
        else
        {
            StopSlide();
        }
    }


    private void StopSlide()
    {
        if (_isListeningCompositionTargetRendering)
        {
            CompositionTarget.Rendering -= CompositionTarget_Rendering;
            _isListeningCompositionTargetRendering = false;

            OnSlideEnd();
        }
    }


    private void CancelDrag()
    {
        _isDragging = false;
        Momentum = Velocity;
    }

    private static double CoerceOffset(double offset, double extent, double viewport)
    {
        if (offset > extent - viewport) offset = extent - viewport;

        if (offset < 0.0) offset = 0.0;

        return offset;
    }

    private void CoerceOffsets(ref double horizontalOffset, ref double verticalOffset)
    {
        horizontalOffset = CoerceOffset(horizontalOffset, this.ExtentWidth, this.ViewportWidth);
        verticalOffset = CoerceOffset(verticalOffset, this.ExtentHeight, this.ViewportHeight);
    }


    private bool PerformScroll(Vector displacement)
    {
        var horizontalOffset = this.HorizontalOffset - displacement.X;
        var verticalOffset = this.VerticalOffset - displacement.Y;

        this.CoerceOffsets(ref horizontalOffset, ref verticalOffset);

        this.ScrollToVerticalOffset(verticalOffset);
        this.ScrollToHorizontalOffset(horizontalOffset);

        return Math.Abs(horizontalOffset - this.HorizontalOffset) > 1.0
               || Math.Abs(verticalOffset - this.VerticalOffset) > 1.0;
    }
}