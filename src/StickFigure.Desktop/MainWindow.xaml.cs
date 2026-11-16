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
        _activeStroke = null;
        StatusText.Text = $"READY  •  {DrawingCanvas.Children.Count} ELEMENTS";
    }

    private void SpawnButton_Click(object sender, RoutedEventArgs e)
    {
        _stickman?.Dispose();
        _stickman = new ProceduralStickman(DrawingCanvas, _props);
        EmptyHint.Visibility = Visibility.Collapsed;
        StatusText.Text = "STICKMAN  •  AUTONOMOUS WALK + OBJECT INTERACTION";
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
        DrawingCanvas.Children.Clear();
        EmptyHint.Visibility = Visibility.Visible;
        StatusText.Text = "READY  •  CANVAS CLEARED";
    }

    private void PenButton_Click(object sender, RoutedEventArgs e)
    {
        StatusText.Text = "DRAW MODE  •  LEFT MOUSE BUTTON";
    }
}
