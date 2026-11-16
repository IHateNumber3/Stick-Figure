using System;
using System.Collections.Generic;
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

    private const double FigureHeight = 178;
    private const double GroundMargin = 12;
    private const double WalkSpeed = 74;
    private const double Gravity = 900;

    public ProceduralStickman(Canvas canvas, IReadOnlyList<PhysicsProp>? props = null)
    {
        _canvas = canvas;
        _props = props ?? Array.Empty<PhysicsProp>();
        _x = Math.Max(100, canvas.ActualWidth / 2);
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

    private static SolidColorBrush Ink() => new(Color.FromRgb(235, 243, 250));
    private static Line Bone(double thickness) => new()
    {
        Stroke = Ink(), StrokeThickness = thickness,
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

        if (_heldProp is not null)
        {
            _carryTime -= dt;
            _phase += dt * 5;
            if (_carryTime <= 0)
            {
                _heldProp.Throw(_direction * 245, -205);
                _heldProp = null;
                _walking = false;
                _decisionTime = 0.45;
            }
        }
        else
        {
            _decisionTime -= dt;
            if (_decisionTime <= 0)
            {
                _walking = !_walking;
                _decisionTime = _walking ? 2.2 : 0.65;
                if (_walking && Random.Shared.NextDouble() < 0.35)
                    _direction *= -1;
            }

            if (_walking)
            {
                _x += _direction * WalkSpeed * dt;
                _phase += dt * 8.5;
            }
            else _phase += dt * 1.5;

            // Look for a crate at hand-reach distance and pick it up.
            foreach (var prop in _props)
            {
                if (prop.IsHeld) continue;
                double dx = prop.CenterX - _x;
                double dy = prop.CenterY - (_y + 153);
                if (Math.Abs(dx) < 62 && Math.Abs(dy) < 34 && prop.TryGrab())
                {
                    _heldProp = prop;
                    _direction = dx >= 0 ? 1 : -1;
                    _walking = false;
                    _carryTime = 1.35;
                    break;
                }
            }

            // If a crate is close but not yet within reach, walk toward it.
            if (_heldProp is null && _walking)
            {
                foreach (var prop in _props)
                {
                    if (prop.IsHeld) continue;
                    double dx = prop.CenterX - _x;
                    double dy = prop.CenterY - (_y + 153);
                    if (Math.Abs(dx) < 105 && Math.Abs(dy) < 45)
                    {
                        _direction = dx >= 0 ? 1 : -1;
                        break;
                    }
                }
            }
        }

        double halfWidth = 42;
        if (_x < halfWidth) { _x = halfWidth; _direction = 1; }
        if (_x > width - halfWidth) { _x = width - halfWidth; _direction = -1; }

        double groundY = height - GroundMargin - FigureHeight;
        _velocityY += Gravity * dt;
        _y += _velocityY * dt;
        if (_y >= groundY) { _y = groundY; _velocityY = 0; }

        Render();
    }

    public void Render()
    {
        if (_disposed) return;
        double bob = _walking ? Math.Abs(Math.Sin(_phase * 2)) * 4 : Math.Sin(_phase) * 1.5;
        double stride = _walking ? Math.Sin(_phase) : 0;
        double armSwing = _walking ? Math.Sin(_phase + Math.PI) * 20 : Math.Sin(_phase) * 3;

        P head = new(_x, _y + 16 - bob);
        P neck = new(_x, _y + 37 - bob);
        P shoulder = new(_x, _y + 43 - bob);
        P hip = new(_x, _y + 91 - bob);
        P elbowL = new(_x - 17 * _direction + armSwing * 0.35, _y + 66 - bob);
        P handL = new(_x - 25 * _direction + armSwing, _y + 84 - bob);
        P elbowR = new(_x + 17 * _direction - armSwing * 0.35, _y + 66 - bob);
        P handR = new(_x + 25 * _direction - armSwing, _y + 84 - bob);

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