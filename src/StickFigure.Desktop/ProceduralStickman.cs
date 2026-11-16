using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace StickFigure;

/// <summary>
/// Procedural stick figure with an autonomous walk cycle and simple crate
/// interaction: approach, grab, carry briefly, then throw.
/// </summary>
public sealed class ProceduralStickman : IDisposable
{
    private readonly Canvas _canvas;
    private readonly IReadOnlyList<PhysicsProp> _props;
    private readonly Ellipse _head;
    private readonly Ellipse[] _joints;
    private readonly Line _torso, _upperArmL, _lowerArmL, _upperArmR, _lowerArmR;
    private readonly Line _upperLegL, _lowerLegL, _upperLegR, _lowerLegR;
    private readonly DispatcherTimer _timer;

    private double _x, _y, _velocityY, _phase;
    private double _direction = 1;
    private double _decisionTime = 1.5;
    private double _carryTime;
    private bool _walking = true;
    private bool _disposed;
    private PhysicsProp? _heldProp;
    private Polyline? _doodleStroke;
    private double _doodleProgress = -1;
    private double _doodleCooldown = 4;
    private readonly double _walkSpeed;
    private readonly double _personalityOffset;
    private readonly Brush _ink;
    private readonly AgentWorld? _world;
    private readonly AgentBrain _brain;
    private readonly AgentMemory _memory = new();
    private readonly AnimationController _animation = new();
    private double _goalTime;
    private double _targetX;
    private double _socialGestureTime;
    public double AgentX => _x;
    public bool IsDisposed => _disposed;
    public AgentAction CurrentAction => _animation.Action;
    public int DecisionsMade => _brain.DecisionsMade;

    private const double FigureHeight = 178;
    private const double GroundMargin = 12;
    
    private const double Gravity = 900;

    public ProceduralStickman(Canvas canvas, IReadOnlyList<PhysicsProp>? props = null, double startX = double.NaN, double walkSpeed = 74, double personalityOffset = 0, Color? color = null, AgentWorld? world = null)
    {
        _canvas = canvas;
        _props = props ?? Array.Empty<PhysicsProp>();
        _walkSpeed = walkSpeed;
        _personalityOffset = personalityOffset;
        _world = world;
        _brain = new AgentBrain((int)(personalityOffset * 101) + (int)(double.IsNaN(startX) ? 0 : startX));
        _world?.Register(this);
        _ink = new SolidColorBrush(color ?? Color.FromRgb(235, 243, 250));
        _x = double.IsNaN(startX) ? Math.Max(100, canvas.ActualWidth / 2) : startX;
        _direction = personalityOffset % 2 == 0 ? 1 : -1;
        _decisionTime = 0.4 + Math.Abs(personalityOffset) * 0.31;
        _doodleCooldown = 1.5 + Math.Abs(personalityOffset) * 2.2;
        _head = new Ellipse
        {
            Width = 30, Height = 30, Stroke = Ink(), StrokeThickness = 3,
            Fill = Brushes.Transparent
        };
        _torso = Bone(3.4);
        _upperArmL = Bone(3); _lowerArmL = Bone(3);
        _upperArmR = Bone(3); _lowerArmR = Bone(3);
        _upperLegL = Bone(3.2); _lowerLegL = Bone(3.2);
        _upperLegR = Bone(3.2); _lowerLegR = Bone(3.2);
        _joints = new Ellipse[5];
        for (int i = 0; i < _joints.Length; i++)
            _joints[i] = new Ellipse { Width = 6, Height = 6, Fill = Ink() };

        foreach (var line in new[] { _upperLegL, _lowerLegL, _upperLegR, _lowerLegR,
                     _torso, _upperArmL, _lowerArmL, _upperArmR, _lowerArmR })
            _canvas.Children.Add(line);
        foreach (var joint in _joints) _canvas.Children.Add(joint);
        _canvas.Children.Add(_head);

        _timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(16) };
        _timer.Tick += Tick;
        _timer.Start();
        Render();
    }

    private Brush Ink() => _ink;
    private Line Bone(double thickness) => new()
    {
        Stroke = _ink, StrokeThickness = thickness,
        StrokeStartLineCap = PenLineCap.Round,
        StrokeEndLineCap = PenLineCap.Round,
        StrokeLineJoin = PenLineJoin.Round
    };

    private void Tick(object? sender, EventArgs e)
    {
        if (_disposed) return;
        const double dt = 1.0 / 60.0;
        double width = _canvas.ActualWidth, height = _canvas.ActualHeight;
        if (width < 80 || height < 100) return;

        _walking = false;
        if (_heldProp is not null)
        {
            _carryTime -= dt;
            if (_carryTime <= 0)
            {
                _heldProp.Throw(_direction * 245, -205);
                _heldProp = null;
                _brain.ReportOutcome(_memory, true);
                _goalTime = 0.2;
            }
        }
        else
        {
            _goalTime -= dt;
            if (_goalTime <= 0)
            {
                var perception = _world?.Perceive(this, _x, width, _props)
                    ?? new AgentPerception { X = _x, ScreenWidth = width, EdgeDistance = Math.Min(_x, width - _x) };
                var goal = _brain.Decide(perception, _memory);
                _animation.SetAction(goal.Action);
                _targetX = goal.TargetX;
                _goalTime = goal.Action switch
                {
                    AgentAction.Explore => 2.0 + Random.Shared.NextDouble() * 2.5,
                    AgentAction.Socialize => 2.4,
                    AgentAction.Draw => 2.8,
                    AgentAction.PlayWithProp => 3.5,
                    _ => 1.6 + Random.Shared.NextDouble() * 2.2
                };
                _socialGestureTime = 0;
                if (goal.Action == AgentAction.Draw && _doodleProgress < 0)
                {
                    _doodleProgress = 0;
                    _doodleStroke = new Polyline
                    {
                        Stroke = new SolidColorBrush(Color.FromRgb(0, 212, 255)),
                        StrokeThickness = 2.2,
                        StrokeStartLineCap = PenLineCap.Round,
                        StrokeEndLineCap = PenLineCap.Round,
                        StrokeLineJoin = PenLineJoin.Round
                    };
                    _canvas.Children.Add(_doodleStroke);
                }
            }

            switch (_animation.Action)
            {
                case AgentAction.Explore:
                    MoveToward(_targetX, dt);
                    if (Math.Abs(_targetX - _x) < 18) _brain.ReportOutcome(_memory, true);
                    break;
                case AgentAction.Socialize:
                    if (_world is not null)
                    {
                        var social = _world.Perceive(this, _x, width, _props);
                        if (social.NearestAgent is not null)
                        {
                            if (social.AgentDistance > 82) MoveToward(social.NearestAgent.AgentX, dt);
                            else
                            {
                                _direction = social.AgentDeltaX >= 0 ? 1 : -1;
                                _socialGestureTime += dt;
                                _phase += dt * 2.8;
                                if (_socialGestureTime > 1.2) _brain.ReportOutcome(_memory, true);
                            }
                        }
                        else _brain.ReportOutcome(_memory, false);
                    }
                    break;
                case AgentAction.Draw:
                    _doodleProgress += dt * 2.7;
                    if (_doodleProgress >= Math.PI * 2)
                    {
                        _doodleProgress = -1;
                        _doodleStroke = null;
                        _brain.ReportOutcome(_memory, true);
                    }
                    break;
                case AgentAction.PlayWithProp:
                    var targetProp = _brain.CurrentGoal.TargetProp;
                    if (targetProp is not null && !targetProp.IsHeld)
                    {
                        if (Math.Abs(targetProp.CenterX - _x) < 62 && Math.Abs(targetProp.CenterY - (_y + 153)) < 50 && targetProp.TryGrab())
                        {
                            _heldProp = targetProp;
                            _direction = targetProp.CenterX >= _x ? 1 : -1;
                            _carryTime = 1.15;
                        }
                        else MoveToward(targetProp.CenterX, dt);
                    }
                    break;
                case AgentAction.Rest:
                    _phase += dt * 0.7;
                    if (_goalTime < 0.15) _brain.ReportOutcome(_memory, true);
                    break;
            }
        }

        double halfWidth = 42;
        if (_x < halfWidth) { _x = halfWidth; _direction = 1; }
        if (_x > width - halfWidth) { _x = width - halfWidth; _direction = -1; }

        double groundY = height - GroundMargin - FigureHeight;
        _velocityY += Gravity * dt;
        _y += _velocityY * dt;
        if (_y >= groundY) { _y = groundY; _velocityY = 0; }

        _animation.Update(dt, _walking);
        _phase = _animation.Phase;
        Render();
    }

    private void MoveToward(double targetX, double dt)
    {
        if (double.IsNaN(targetX)) return;
        double delta = targetX - _x;
        if (Math.Abs(delta) < 16) return;
        _direction = delta > 0 ? 1 : -1;
        _x += _direction * _walkSpeed * dt;
        _walking = true;
    }

    public void Render()
    {
        if (_disposed) return;
        double bob = _walking ? Math.Abs(Math.Sin(_phase * 2)) * 4 : Math.Sin(_phase) * (_animation.IsResting ? 0.6 : 1.5);
        double stride = _walking ? Math.Sin(_phase) : 0;
        double armSwing = _walking ? Math.Sin(_phase + Math.PI) * 20 : _animation.IsSocializing ? Math.Sin(_phase * 3) * 13 : Math.Sin(_phase) * 3;

        P head = new(_x, _y + 16 - bob);
        P neck = new(_x, _y + 37 - bob);
        P shoulder = new(_x, _y + 43 - bob);
        P hip = new(_x, _y + 91 - bob);
        P elbowL = new(_x - 17 * _direction + armSwing * 0.35, _y + 66 - bob);
        P handL = new(_x - 25 * _direction + armSwing, _y + 84 - bob);
        P elbowR = new(_x + 17 * _direction - armSwing * 0.35, _y + 66 - bob);
        P handR = new(_x + 25 * _direction - armSwing, _y + 84 - bob);

        if ((_doodleProgress >= 0 || _animation.IsSocializing) && _heldProp is null)
        {
            double angle = _doodleProgress;
            P drawingHand = new(_x + _direction * (65 + Math.Cos(angle) * 19),
                                _y + 112 + Math.Sin(angle) * 19);
            if (_direction > 0) { elbowR = new(_x + 22, _y + 88); handR = drawingHand; }
            else { elbowL = new(_x - 22, _y + 88); handL = drawingHand; }
            _doodleStroke?.Points.Add(new Point(drawingHand.X, drawingHand.Y));
        }

        if (_heldProp is not null)
        {
            // The forward arm reaches down to the crate; the crate follows its hand.
            P heldHand = new(_x + _direction * 39, _y + 153 - bob);
            if (_direction > 0) { elbowR = new(_x + 22, _y + 88 - bob); handR = heldHand; }
            else { elbowL = new(_x - 22, _y + 88 - bob); handL = heldHand; }
            _heldProp.MoveWhileHeld(heldHand.X, heldHand.Y);
        }

        P kneeL = new(_x - 13 + stride * 16, _y + 126 - bob);
        P footL = new(_x - 17 + stride * 29, _y + 166 - bob);
        P kneeR = new(_x + 13 - stride * 16, _y + 126 - bob);
        P footR = new(_x + 17 - stride * 29, _y + 166 - bob);

        Place(_head, head.X - 15, head.Y - 15);
        Set(_torso, neck, hip);
        Set(_upperArmL, shoulder, elbowL); Set(_lowerArmL, elbowL, handL);
        Set(_upperArmR, shoulder, elbowR); Set(_lowerArmR, elbowR, handR);
        Set(_upperLegL, hip, kneeL); Set(_lowerLegL, kneeL, footL);
        Set(_upperLegR, hip, kneeR); Set(_lowerLegR, kneeR, footR);
        Place(_joints[0], shoulder.X - 3, shoulder.Y - 3);
        Place(_joints[1], elbowL.X - 3, elbowL.Y - 3);
        Place(_joints[2], elbowR.X - 3, elbowR.Y - 3);
        Place(_joints[3], kneeL.X - 3, kneeL.Y - 3);
        Place(_joints[4], kneeR.X - 3, kneeR.Y - 3);
    }

    private static void Set(Line line, P a, P b)
    { line.X1 = a.X; line.Y1 = a.Y; line.X2 = b.X; line.Y2 = b.Y; }
    private static void Place(Shape shape, double x, double y)
    { Canvas.SetLeft(shape, x); Canvas.SetTop(shape, y); }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _timer.Stop();
        _world?.Unregister(this);
        _heldProp?.Drop();
        _heldProp = null;
        _canvas.Children.Remove(_head);
        foreach (var line in new[] { _torso, _upperArmL, _lowerArmL, _upperArmR,
                     _lowerArmR, _upperLegL, _lowerLegL, _upperLegR, _lowerLegR })
            _canvas.Children.Remove(line);
        foreach (var joint in _joints) _canvas.Children.Remove(joint);
    }

    private readonly record struct P(double X, double Y);
}