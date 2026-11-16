using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;

namespace StickFigure;

public partial class MainWindow : Window
{
    private Polyline? _activeStroke;
    private bool _isDrawing;

    public MainWindow()
    {
        InitializeComponent();
    }

    private void DrawingCanvas_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        _isDrawing = true;
        DrawingCanvas.CaptureMouse();
        EmptyHint.Visibility = Visibility.Collapsed;

        var point = e.GetPosition(DrawingCanvas);
        _activeStroke = new Polyline
        {
            Stroke = new SolidColorBrush(Color.FromRgb(235, 243, 250)),
            StrokeThickness = 3.0,
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
        if (!_isDrawing || _activeStroke is null || e.LeftButton != MouseButtonState.Pressed)
            return;

        var point = e.GetPosition(DrawingCanvas);
        var previous = _activeStroke.Points[^1];

        // Ignore tiny pointer jitter while preserving the hand-drawn feel.
        if ((point - previous).Length >= 1.2)
            _activeStroke.Points.Add(point);
    }

    private void DrawingCanvas_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        if (!_isDrawing)
            return;

        _isDrawing = false;
        DrawingCanvas.ReleaseMouseCapture();
        _activeStroke = null;
        StatusText.Text = $"READY  •  {DrawingCanvas.Children.Count} STROKES";
    }

    private void ClearButton_Click(object sender, RoutedEventArgs e)
    {
        DrawingCanvas.Children.Clear();
        EmptyHint.Visibility = Visibility.Visible;
        StatusText.Text = "READY  •  CANVAS CLEARED";
    }

    private void PenButton_Click(object sender, RoutedEventArgs e)
    {
        StatusText.Text = "DRAW MODE  •  LEFT MOUSE BUTTON";
    }
}
