using System;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace StickFigure;

/// <summary>
/// A vector stick figure whose joints are recalculated every frame.
/// This is a first procedural-animation prototype, not a sprite animation.
/// </summary>
public sealed class ProceduralStickman : IDisposable
{
    private readonly Canvas _canvas;
    private readonly Ellipse _head;
    private readonly Line _body, _leftArm, _rightArm, _leftLeg, _rightLeg;
    private readonly DispatcherTimer _timer;
    private double _time;

    public ProceduralStickman(Canvas canvas)
    {
        _canvas = canvas;
        _head = new Ellipse { Width = 31, Height = 31, Stroke = Brush(), StrokeThickness = 3, Fill = Brushes.Transparent };
        _body = Bone(); _leftArm = Bone(); _rightArm = Bone(); _leftLeg = Bone(); _rightLeg = Bone();
        _canvas.Children.Add(_leftLeg);
        _canvas.Children.Add(_rightLeg);
        _canvas.Children.Add(_body);
        _canvas.Children.Add(_leftArm);
        _canvas.Children.Add(_rightArm);
        _canvas.Children.Add(_head);

        _timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(16) };
        _timer.Tick += (_, _) => { _time += 0.045; Render(); };
        _timer.Start();
        Render();
    }

    private static SolidColorBrush Brush() => new(Color.FromRgb(232, 242, 250));
    private static Line Bone() => new()
    {
        Stroke = Brush(), StrokeThickness = 3.2,
        StrokeStartLineCap = PenLineCap.Round, StrokeEndLineCap = PenLineCap.Round
    };

    public void Render()
    {
        double cx = Math.Max(100, _canvas.ActualWidth / 2);
        double cy = Math.Max(150, _canvas.ActualHeight / 2);
        double bob = Math.Sin(_time * 2) * 3.0;
        double sway = Math.Sin(_time) * 5.0;

        Point2D head = new(cx + sway, cy - 83 + bob);
        Point2D neck = new(cx + sway, cy - 61 + bob);
        Point2D hip = new(cx, cy + 8 + bob);
        Point2D leftShoulder = new(cx + sway, cy - 52 + bob);
        Point2D rightShoulder = leftShoulder;
        Point2D leftElbow = new(cx - 29 + Math.Sin(_time * 1.4) * 5, cy - 27 + bob);
        Point2D rightElbow = new(cx + 29 - Math.Sin(_time * 1.4) * 5, cy - 29 + bob);
        Point2D leftHand = new(cx - 35 + Math.Sin(_time * 1.2) * 7, cy - 4 + bob);
        Point2D rightHand = new(cx + 35 - Math.Sin(_time * 1.2) * 7, cy - 5 + bob);
        Point2D leftKnee = new(cx - 17 + Math.Sin(_time) * 3, cy + 51 + bob);
        Point2D rightKnee = new(cx + 17 - Math.Sin(_time) * 3, cy + 51 + bob);
        Point2D leftFoot = new(cx - 23 + Math.Sin(_time) * 5, cy + 94 + bob);
        Point2D rightFoot = new(cx + 23 - Math.Sin(_time) * 5, cy + 94 + bob);

        Set(_head, head.X - 15.5, head.Y - 15.5);
        Segment(_body, neck, hip);
        Segment(_leftArm, leftShoulder, leftHand);
        Segment(_rightArm, rightShoulder, rightHand);
        // Legs use two-segment joints; knee points are kept visible as natural bends.
        Segment(_leftLeg, hip, leftKnee);
        Segment(_rightLeg, hip, rightKnee);
        AddOrUpdateFoot(leftKnee, leftFoot, true);
        AddOrUpdateFoot(rightKnee, rightFoot, false);
    }

    private Line? _leftShin, _rightShin;
    private void AddOrUpdateFoot(Point2D knee, Point2D foot, bool left)
    {
        if (left) _leftShin ??= AddBone();
        else _rightShin ??= AddBone();
        Segment(left ? _leftShin! : _rightShin!, knee, foot);
    }
    private Line AddBone() { var line = Bone(); _canvas.Children.Add(line); return line; }
    private static void Segment(Line line, Point2D a, Point2D b)
    { line.X1 = a.X; line.Y1 = a.Y; line.X2 = b.X; line.Y2 = b.Y; }
    private static void Set(Shape shape, double x, double y)
    { Canvas.SetLeft(shape, x); Canvas.SetTop(shape, y); }

    public void Dispose()
    {
        _timer.Stop();
        _canvas.Children.Remove(_head);
        _canvas.Children.Remove(_body);
        _canvas.Children.Remove(_leftArm);
        _canvas.Children.Remove(_rightArm);
        _canvas.Children.Remove(_leftLeg);
        _canvas.Children.Remove(_rightLeg);
        if (_leftShin is not null) _canvas.Children.Remove(_leftShin);
        if (_rightShin is not null) _canvas.Children.Remove(_rightShin);
    }

    private readonly record struct Point2D(double X, double Y);
}
