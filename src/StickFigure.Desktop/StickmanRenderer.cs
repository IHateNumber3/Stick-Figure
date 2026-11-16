using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;

namespace StickFigure;

/// <summary>Pure presentation layer. It never chooses actions or owns simulation time.</summary>
public sealed class StickmanRenderer : IDisposable
{
    private readonly Canvas _canvas;
    private readonly Brush _ink;
    private readonly Ellipse _head;
    private readonly Line[] _bones;
    private readonly Ellipse[] _joints;
    private Polyline? _drawing;
    private bool _disposed;

    public StickmanRenderer(Canvas canvas, Color color)
    {
        _canvas = canvas;
        _ink = new SolidColorBrush(color);
        _head = new Ellipse { Width = 28, Height = 28, Stroke = _ink, StrokeThickness = 3, Fill = Brushes.Transparent };
        _bones = new Line[9];
        for (int i = 0; i < _bones.Length; i++)
            _bones[i] = new Line { Stroke = _ink, StrokeThickness = i < 4 ? 3.1 : 2.8, StrokeStartLineCap = PenLineCap.Round, StrokeEndLineCap = PenLineCap.Round };
        _joints = new Ellipse[5];
        for (int i = 0; i < _joints.Length; i++)
            _joints[i] = new Ellipse { Width = 6, Height = 6, Fill = _ink };
        foreach (var bone in _bones) _canvas.Children.Add(bone);
        foreach (var joint in _joints) _canvas.Children.Add(joint);
        _canvas.Children.Add(_head);
    }

    public void BeginDrawing()
    {
        _drawing = new Polyline
        {
            Stroke = _ink, StrokeThickness = 2.2,
            StrokeStartLineCap = PenLineCap.Round, StrokeEndLineCap = PenLineCap.Round,
            StrokeLineJoin = PenLineJoin.Round
        };
        _canvas.Children.Add(_drawing);
    }

    public void AddDrawingPoint(double x, double y)
    {
        if (_drawing is null || _disposed) return;
        if (_drawing.Points.Count == 0 || (_drawing.Points[^1] - new Point(x, y)).Length > 2.5)
            _drawing.Points.Add(new Point(x, y));
    }

    public void Render(StickmanAgent agent, double dt)
    {
        if (_disposed) return;
        double x = agent.X;
        double ground = agent.GroundY + agent.VerticalOffset;
        double bob = agent.Pose.BodyBob;
        double face = agent.Facing;
        var head = new Point(x, ground - 158 - bob);
        var shoulder = new Point(x, ground - 128 - bob);
        var hip = new Point(x + agent.Pose.TorsoLean * 20 * face, ground - 76 - bob);
        var neck = new Point(x, ground - 142 - bob);

        double leg = agent.Pose.LegSwing;
        var kneeL = new Point(x - 12 + leg, ground - 43 - bob);
        var kneeR = new Point(x + 12 - leg, ground - 43 - bob);
        var footL = new Point(x - 14 + leg * 1.7, ground - (agent.Action == AgentAction.Rest ? 8 : 4) - bob);
        var footR = new Point(x + 14 - leg * 1.7, ground - (agent.Action == AgentAction.Rest ? 8 : 4) - bob);

        double swing = agent.Pose.ArmSwing;
        var elbowL = new Point(x - 18 + swing * 0.3, ground - 101 - bob);
        var handL = new Point(x - 28 + swing, ground - 82 - bob);
        var elbowR = new Point(x + 18 - swing * 0.3, ground - 101 - bob);
        var handR = new Point(x + 28 - swing, ground - 82 - bob);

        if (agent.Action == AgentAction.Socialize)
        {
            if (face > 0) { elbowR = new Point(x + 24, ground - 115); handR = new Point(x + 36, ground - 112 - agent.Pose.ArmRaise * 36 - Math.Sin(agent.Pose.GesturePhase * 2.4) * 9); }
            else { elbowL = new Point(x - 24, ground - 115); handL = new Point(x - 36, ground - 112 - agent.Pose.ArmRaise * 36 - Math.Sin(agent.Pose.GesturePhase * 2.4) * 9); }
        }
        else if (agent.Action == AgentAction.Draw)
        {
            Point tip = _drawing is { Points.Count: > 0 } ? _drawing.Points[^1] : new Point(x + face * 54, ground - 78);
            if (face > 0) { elbowR = new Point(x + 24, ground - 105); handR = tip; }
            else { elbowL = new Point(x - 24, ground - 105); handL = tip; }
        }
        else if (agent.Action == AgentAction.PlayWithProp)
        {
            if (face > 0) { elbowR = new Point(x + 27, ground - 92); handR = new Point(x + 45, ground - 48); }
            else { elbowL = new Point(x - 27, ground - 92); handL = new Point(x - 45, ground - 48); }
        }

        Canvas.SetLeft(_head, head.X - 14);
        Canvas.SetTop(_head, head.Y - 14);
        Set(_bones[0], neck, hip);
        Set(_bones[1], shoulder, elbowL); Set(_bones[2], elbowL, handL);
        Set(_bones[3], shoulder, elbowR); Set(_bones[4], elbowR, handR);
        Set(_bones[5], hip, kneeL); Set(_bones[6], kneeL, footL);
        Set(_bones[7], hip, kneeR); Set(_bones[8], kneeR, footR);
        Place(_joints[0], shoulder); Place(_joints[1], elbowL); Place(_joints[2], elbowR); Place(_joints[3], kneeL); Place(_joints[4], kneeR);
    }

    private static void Set(Line line, Point a, Point b)
    { line.X1 = a.X; line.Y1 = a.Y; line.X2 = b.X; line.Y2 = b.Y; }
    private static void Place(Shape shape, Point p)
    { Canvas.SetLeft(shape, p.X - 3); Canvas.SetTop(shape, p.Y - 3); }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _canvas.Children.Remove(_head);
        foreach (var bone in _bones) _canvas.Children.Remove(bone);
        foreach (var joint in _joints) _canvas.Children.Remove(joint);
    }
}
