using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace StickFigure;

/// <summary>A draggable crate with lightweight gravity and a grab/throw state.</summary>
public sealed class PhysicsProp : IDisposable
{
    private readonly Canvas _canvas;
    private readonly Rectangle _shape;
    private readonly DispatcherTimer _timer;
    private bool _dragging;
    private bool _held;
    private Vector _dragOffset;
    private bool _disposed;
    private double _x, _y, _vx, _vy;

    public double CenterX => _x + Width / 2;
    public double CenterY => _y + Height / 2;
    public bool IsHeld => _held;
    public const double Width = 48;
    public const double Height = 42;

    public PhysicsProp(Canvas canvas, double x, double y)
    {
        _canvas = canvas; _x = x; _y = y;
        _shape = new Rectangle
        {
            Width = Width, Height = Height, RadiusX = 5, RadiusY = 5,
            Fill = new SolidColorBrush(Color.FromRgb(30, 91, 112)),
            Stroke = new SolidColorBrush(Color.FromRgb(0, 212, 255)),
            StrokeThickness = 2,
            Cursor = Cursors.SizeAll
        };
        _shape.MouseLeftButtonDown += BeginDrag;
        _shape.MouseMove += Drag;
        _shape.MouseLeftButtonUp += EndDrag;
        _canvas.Children.Add(_shape);
        _timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(16) };
        _timer.Tick += (_, _) => Step();
        _timer.Start();
        Render();
    }

    private void BeginDrag(object sender, MouseButtonEventArgs e)
    {
        if (_held) return;
        _dragging = true;
        _dragOffset = e.GetPosition(_canvas) - new Point(_x, _y);
        _shape.CaptureMouse();
        _vx = _vy = 0;
        e.Handled = true;
    }

    private void Drag(object sender, MouseEventArgs e)
    {
        if (!_dragging || e.LeftButton != MouseButtonState.Pressed) return;
        Point p = e.GetPosition(_canvas) - _dragOffset;
        _x = Math.Clamp(p.X, 0, Math.Max(0, _canvas.ActualWidth - Width));
        _y = Math.Clamp(p.Y, 0, Math.Max(0, _canvas.ActualHeight - Height));
        Render();
    }

    private void EndDrag(object sender, MouseButtonEventArgs e)
    {
        _dragging = false;
        _shape.ReleaseMouseCapture();
        e.Handled = true;
    }

    public bool TryGrab()
    {
        if (_disposed || _dragging || _held) return false;
        _held = true;
        _vx = _vy = 0;
        return true;
    }

    public void MoveWhileHeld(double centerX, double centerY)
    {
        if (!_held || _disposed) return;
        _x = centerX - Width / 2;
        _y = centerY - Height / 2;
        Render();
    }

    public void Throw(double velocityX, double velocityY = -180)
    {
        if (!_held || _disposed) return;
        _held = false;
        _vx = velocityX;
        _vy = velocityY;
    }

    public void Drop()
    {
        if (!_held) return;
        _held = false;
        _vx = 0;
        _vy = 0;
    }

    public void Push(double direction, double strength = 165)
    {
        if (_dragging || _held || _disposed) return;
        _vx += Math.Sign(direction) * strength;
        _vy -= 28;
    }

    private void Step()
    {
        if (_disposed || _dragging || _held) return;
        const double dt = 1.0 / 60.0;
        _vy += 820 * dt;
        _x += _vx * dt;
        _y += _vy * dt;
        double floor = Math.Max(0, _canvas.ActualHeight - Height);
        if (_y >= floor) { _y = floor; _vy = 0; _vx *= 0.82; }
        if (_x < 0) { _x = 0; _vx = Math.Abs(_vx) * 0.45; }
        double right = Math.Max(0, _canvas.ActualWidth - Width);
        if (_x > right) { _x = right; _vx = -Math.Abs(_vx) * 0.45; }
        _vx *= 0.992;
        Render();
    }

    private void Render()
    { Canvas.SetLeft(_shape, _x); Canvas.SetTop(_shape, _y); }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _timer.Stop();
        _canvas.Children.Remove(_shape);
    }
}