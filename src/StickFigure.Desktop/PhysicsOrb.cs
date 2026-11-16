using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace StickFigure;

public sealed class PhysicsOrb : IDisposable
{
    private readonly Canvas _canvas;
    private readonly Ellipse _shape;
    private readonly DispatcherTimer _timer;
    private double _x, _y, _vx, _vy;
    private bool _dragging, _disposed;
    private Vector _dragOffset;

    public PhysicsOrb(Canvas canvas, double centerX, double centerY, double diameter)
    {
        _canvas = canvas;
        diameter = Math.Clamp(diameter, 24, 110);
        _x = centerX - diameter / 2;
        _y = centerY - diameter / 2;
        _shape = new Ellipse {
            Width = diameter, Height = diameter,
            Fill = new SolidColorBrush(Color.FromArgb(65, 0, 212, 255)),
            Stroke = new SolidColorBrush(Color.FromRgb(0, 212, 255)),
            StrokeThickness = 3, Cursor = Cursors.SizeAll
        };
        _shape.MouseLeftButtonDown += BeginDrag;
        _shape.MouseMove += Drag;
        _shape.MouseLeftButtonUp += EndDrag;
        _canvas.Children.Add(_shape);
        _timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(16) };
        _timer.Tick += (_, _) => Step();
        _timer.Start();
        _vy = -90;
        Render();
    }

    private void BeginDrag(object sender, MouseButtonEventArgs e)
    {
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
        _x = Math.Clamp(p.X, 0, Math.Max(0, _canvas.ActualWidth - _shape.Width));
        _y = Math.Clamp(p.Y, 0, Math.Max(0, _canvas.ActualHeight - _shape.Height));
        Render();
    }

    private void EndDrag(object sender, MouseButtonEventArgs e)
    {
        _dragging = false;
        _shape.ReleaseMouseCapture();
        e.Handled = true;
    }

    private void Step()
    {
        if (_disposed || _dragging) return;
        const double dt = 1.0 / 60.0;
        _vy += 650 * dt;
        _x += _vx * dt;
        _y += _vy * dt;
        double floor = Math.Max(0, _canvas.ActualHeight - _shape.Height);
        if (_y >= floor) {
            _y = floor;
            _vy = -Math.Abs(_vy) * 0.72;
            if (Math.Abs(_vy) < 34) _vy = 0;
            _vx *= 0.94;
        }
        double right = Math.Max(0, _canvas.ActualWidth - _shape.Width);
        if (_x < 0) { _x = 0; _vx = Math.Abs(_vx) * 0.65; }
        if (_x > right) { _x = right; _vx = -Math.Abs(_vx) * 0.65; }
        _vx *= 0.999;
        Render();
    }

    private void Render()
    {
        Canvas.SetLeft(_shape, _x);
        Canvas.SetTop(_shape, _y);
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _timer.Stop();
        _canvas.Children.Remove(_shape);
    }
}
