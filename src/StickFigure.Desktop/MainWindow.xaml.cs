using System.Collections.Generic;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;

namespace StickFigure;

public partial class MainWindow : Window
{
    private Polyline? _activeStroke;
    private bool _isDrawing;
    private ProceduralStickman? _stickman;
    private readonly List<PhysicsProp> _props = new();
    private readonly List<PhysicsOrb> _orbs = new();
    private readonly AgentWorld _agentWorld = new();
    private DesktopOverlayWindow? _overlay;

    public MainWindow()
    {
        InitializeComponent();
        SizeChanged += (_, _) => _stickman?.Render();
    }

    private void DrawingCanvas_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.OriginalSource is Shape) return;
        _isDrawing = true;
        DrawingCanvas.CaptureMouse();
        EmptyHint.Visibility = Visibility.Collapsed;
        var point = e.GetPosition(DrawingCanvas);
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
        if (!_isDrawing || _activeStroke is null || e.LeftButton != MouseButtonState.Pressed) return;
        var point = e.GetPosition(DrawingCanvas);
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
                    StatusText.Text = "SHAPE RECOGNIZED  •  PHYSICS CRATE";
                }
                else
                {
                    var orb = new PhysicsOrb(DrawingCanvas,
                        bounds.Left + bounds.Width / 2,
                        bounds.Top + bounds.Height / 2,
                        Math.Min(bounds.Width, bounds.Height) * 0.82);
                    _orbs.Add(orb);
                    StatusText.Text = "SHAPE RECOGNIZED  •  BOUNCING ORB";
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
        _stickman?.Dispose();
        _stickman = new ProceduralStickman(DrawingCanvas, _props, world: _agentWorld);
        EmptyHint.Visibility = Visibility.Collapsed;
        StatusText.Text = "AGENT  •  GOAL-DRIVEN BEHAVIOUR + MEMORY";
    }

    private void PropButton_Click(object sender, RoutedEventArgs e)
    {
        var prop = new PhysicsProp(DrawingCanvas, 150 + (_props.Count % 5) * 65, 40);
        _props.Add(prop);
        EmptyHint.Visibility = Visibility.Collapsed;
        StatusText.Text = "CRATE ADDED  •  DRAG IT OR LET THE STICKMAN PUSH IT";
    }

    private void ClearButton_Click(object sender, RoutedEventArgs e)
    {
        _stickman?.Dispose();
        _stickman = null;
        foreach (var prop in _props) prop.Dispose();
        _props.Clear();
        foreach (var orb in _orbs) orb.Dispose();
        _orbs.Clear();
        DrawingCanvas.Children.Clear();
        EmptyHint.Visibility = Visibility.Visible;
        StatusText.Text = "READY  •  CANVAS CLEARED";
    }

    private void DesktopButton_Click(object sender, RoutedEventArgs e)
    {
        if (_overlay is not null)
        {
            _overlay.Close();
            _overlay = null;
            DesktopButton.Content = "🖥 Desktop mode";
            StatusText.Text = "DESKTOP COMPANION STOPPED";
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
        StatusText.Text = "DESKTOP COMPANION RUNNING  •  Ctrl+Shift+F12 TO CLOSE";
    }

    private void PenButton_Click(object sender, RoutedEventArgs e)
    {
        StatusText.Text = "DRAW MODE  •  LEFT MOUSE BUTTON";
    }
}
