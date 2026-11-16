using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;

namespace StickFigure;

public partial class MainWindow : Window
{
    private Polyline? _activeStroke;
    private bool _isDrawing;
    private readonly List<PhysicsProp> _props = new();
    private readonly List<PhysicsOrb> _orbs = new();
    private readonly AgentWorld _world;
    private DesktopOverlayWindow? _overlay;

    public MainWindow()
    {
        InitializeComponent();
        _world = new AgentWorld(DrawingCanvas);
        Closed += (_, _) => { _world.Dispose(); foreach (var p in _props) p.Dispose(); foreach (var o in _orbs) o.Dispose(); };
    }

    private void DrawingCanvas_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.OriginalSource is Shape) return;
        _isDrawing = true;
        DrawingCanvas.CaptureMouse();
        EmptyHint.Visibility = Visibility.Collapsed;
        var point = e.GetPosition(DrawingCanvas);
        _world.SetCursor(point.X, point.Y);
        _activeStroke = new Polyline
        {
            Stroke = new SolidColorBrush(Color.FromRgb(235, 243, 250)),
            StrokeThickness = 3,
            StrokeStartLineCap = PenLineCap.Round,
            StrokeEndLineCap = PenLineCap.Round,
            StrokeLineJoin = PenLineJoin.Round
        };
        _activeStroke.Points.Add(point);
        DrawingCanvas.Children.Add(_activeStroke);
        StatusText.Text = "DRAWING  •  FREEHAND STROKE";
    }

    private void DrawingCanvas_MouseMove(object sender, MouseEventArgs e)
    {
        var point = e.GetPosition(DrawingCanvas);
        _world.SetCursor(point.X, point.Y);
        if (!_isDrawing || _activeStroke is null || e.LeftButton != MouseButtonState.Pressed) return;
        if ((point - _activeStroke.Points[^1]).Length >= 1.2)
            _activeStroke.Points.Add(point);
    }

    private void DrawingCanvas_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        if (!_isDrawing) return;
        _isDrawing = false;
        DrawingCanvas.ReleaseMouseCapture();
        if (_activeStroke is not null)
        {
            var points = _activeStroke.Points.ToList();
            var bounds = GetBounds(points);
            var recognized = ShapeRecognizer.Recognize(points);
            if (recognized != RecognizedShape.None)
            {
                DrawingCanvas.Children.Remove(_activeStroke);
                if (recognized == RecognizedShape.Rectangle)
                {
                    var prop = new PhysicsProp(DrawingCanvas,
                        bounds.Left + (bounds.Width - PhysicsProp.Width) / 2,
                        bounds.Top + (bounds.Height - PhysicsProp.Height) / 2);
                    _props.Add(prop);
                    _world.RegisterProp(prop);
                    StatusText.Text = "OBJECT CREATED  •  AGENTS CAN INTERACT";
                }
                else
                {
                    var orb = new PhysicsOrb(DrawingCanvas,
                        bounds.Left + bounds.Width / 2,
                        bounds.Top + bounds.Height / 2,
                        Math.Min(bounds.Width, bounds.Height) * 0.82);
                    _orbs.Add(orb);
                    StatusText.Text = "BOUNCING ORB CREATED";
                }
            }
            else StatusText.Text = $"FREEHAND  •  {DrawingCanvas.Children.Count} ELEMENTS";
        }
        _activeStroke = null;
    }

    private static Rect GetBounds(IReadOnlyList<Point> points)
    {
        double minX = points.Min(p => p.X), maxX = points.Max(p => p.X);
        double minY = points.Min(p => p.Y), maxY = points.Max(p => p.Y);
        return new Rect(new Point(minX, minY), new Point(maxX, maxY));
    }

    private void SpawnButton_Click(object sender, RoutedEventArgs e)
    {
        var agent = _world.AddAgent();
        EmptyHint.Visibility = Visibility.Collapsed;
        StatusText.Text = $"{agent.Name.ToUpperInvariant()} JOINED  •  {_world.Agents.Count} AGENTS";
    }

    private void PropButton_Click(object sender, RoutedEventArgs e)
    {
        var prop = new PhysicsProp(DrawingCanvas, 150 + (_props.Count % 5) * 65, 40);
        _props.Add(prop);
        _world.RegisterProp(prop);
        EmptyHint.Visibility = Visibility.Collapsed;
        StatusText.Text = "OBJECT ADDED  •  AGENTS CAN NOTICE AND PUSH IT";
    }

    private void ClearButton_Click(object sender, RoutedEventArgs e)
    {
        _world.ClearAgents();
        foreach (var prop in _props) prop.Dispose();
        _props.Clear();
        foreach (var orb in _orbs) orb.Dispose();
        _orbs.Clear();
        DrawingCanvas.Children.Clear();
        EmptyHint.Visibility = Visibility.Visible;
        StatusText.Text = "WORLD RESET";
    }

    private void DesktopButton_Click(object sender, RoutedEventArgs e)
    {
        if (_overlay is not null)
        {
            _overlay.Close();
            _overlay = null;
            DesktopButton.Content = "🖥 Desktop mode";
            StatusText.Text = "DESKTOP WORLD STOPPED";
            return;
        }

        _overlay = new DesktopOverlayWindow();
        _overlay.Closed += (_, _) =>
        {
            _overlay = null;
            DesktopButton.Content = "🖥 Desktop mode";
        };
        _overlay.Show();
        DesktopButton.Content = "✕ Stop desktop";
        StatusText.Text = "DESKTOP WORLD RUNNING  •  Ctrl+Shift+F12 TO CLOSE";
    }

    private void PenButton_Click(object sender, RoutedEventArgs e)
    {
        StatusText.Text = "DRAW MODE  •  LEFT MOUSE BUTTON";
    }
}
