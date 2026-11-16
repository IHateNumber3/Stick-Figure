using System;
using System.Windows.Media;

namespace StickFigure;

/// <summary>One simulated creature: identity, traits, needs, memory, goal and physical state.</summary>
public sealed class StickmanAgent
{
    private readonly AgentBrain _brain;
    private readonly AnimationController _animation = new();
    private readonly Random _random;
    private double _goalElapsed;
    private double _goalDuration;
    private double _verticalVelocity;
    private double _drawPhase;
    private double _socialElapsed;

    public Guid Id { get; } = Guid.NewGuid();
    public string Name { get; }
    public Color Color { get; }
    public AgentTraits Traits { get; }
    public AgentNeeds Needs { get; } = new();
    public AgentMemory Memory { get; } = new();
    public AgentAction Action { get; private set; } = AgentAction.Wander;
    public AgentGoal? Goal { get; private set; }
    public double X { get; private set; }
    public double GroundY { get; private set; }
    public double VerticalOffset { get; private set; }
    public double Facing { get; private set; } = 1;
    public double Speed { get; private set; }
    public AgentPose Pose { get; private set; }
    public bool IsDisposed { get; private set; }
    public StickmanRenderer Renderer { get; }

    public StickmanAgent(System.Windows.Controls.Canvas canvas, string name, Color color, double x, int seed)
    {
        Name = name;
        Color = color;
        X = x;
        _random = new Random(seed);
        Traits = AgentTraits.Generate(_random);
        _brain = new AgentBrain(seed * 7919);
        Renderer = new StickmanRenderer(canvas, color);
    }

    public void Update(AgentWorld world, double dt)
    {
        if (IsDisposed) return;
        GroundY = Math.Max(100, world.Height - 18);
        Needs.Update(dt, Action);

        _goalElapsed += dt;
        if (Goal is null || _goalElapsed >= _goalDuration || GoalFinished(world))
            ChooseGoal(world);

        Speed = 0;
        switch (Action)
        {
            case AgentAction.Wander:
            case AgentAction.Inspect:
            case AgentAction.FollowCursor:
                MoveToward(Goal?.TargetX ?? X, dt, Action == AgentAction.FollowCursor ? 145 : 88);
                break;
            case AgentAction.Socialize:
                UpdateSocial(world, dt);
                break;
            case AgentAction.Draw:
                UpdateDrawing(dt);
                break;
            case AgentAction.PlayWithProp:
                UpdatePropPlay(dt);
                break;
            case AgentAction.Rest:
                break;
            case AgentAction.Jump:
                if (VerticalOffset >= 0 && _verticalVelocity == 0) _verticalVelocity = -350;
                break;
        }

        if (Action == AgentAction.Jump || VerticalOffset < 0)
        {
            _verticalVelocity += 950 * dt;
            VerticalOffset += _verticalVelocity * dt;
            if (VerticalOffset >= 0) { VerticalOffset = 0; _verticalVelocity = 0; }
        }

        Pose = _animation.Evaluate(Action, Speed, _verticalVelocity, dt);
        Renderer.Render(this, dt);
    }

    private void ChooseGoal(AgentWorld world)
    {
        if (Goal is not null) Memory.Learn(Action, _goalElapsed >= _goalDuration * 0.65);
        var perception = world.Perceive(this);
        Goal = _brain.Choose(this, perception);
        Action = Goal.Action;
        _goalElapsed = 0;
        _goalDuration = Action switch
        {
            AgentAction.Wander => 2.0 + _random.NextDouble() * 3.0,
            AgentAction.Socialize => 2.5 + _random.NextDouble() * 2.0,
            AgentAction.Draw => 2.2 + _random.NextDouble() * 2.0,
            AgentAction.Rest => 1.4 + _random.NextDouble() * 2.2,
            AgentAction.FollowCursor => 1.2 + _random.NextDouble() * 1.5,
            AgentAction.PlayWithProp => 3.0,
            AgentAction.Jump => 0.9,
            AgentAction.Inspect => 2.0,
            _ => 2
        };
        _socialElapsed = 0;
        _drawPhase = _random.NextDouble() * Math.PI * 2;
        if (Action == AgentAction.Draw) Renderer.BeginDrawing();
        if (Action == AgentAction.Jump && VerticalOffset == 0) _verticalVelocity = -350;
        if (Action == AgentAction.Wander || Action == AgentAction.Inspect)
            Memory.RememberPosition(Goal.TargetX);
    }

    private bool GoalFinished(AgentWorld world)
    {
        if (Goal is null) return true;
        return Action switch
        {
            AgentAction.Wander or AgentAction.Inspect or AgentAction.FollowCursor => Math.Abs(Goal.TargetX - X) < 18,
            AgentAction.Socialize => Goal.TargetAgent is null || Goal.TargetAgent.IsDisposed || _goalElapsed > _goalDuration,
            AgentAction.PlayWithProp => Goal.TargetProp is null || Goal.TargetProp.IsHeld || _goalElapsed > _goalDuration,
            AgentAction.Jump => VerticalOffset == 0 && _goalElapsed > 0.45,
            _ => _goalElapsed >= _goalDuration
        };
    }

    private void MoveToward(double target, double dt, double speed)
    {
        double delta = target - X;
        if (Math.Abs(delta) < 12) return;
        Facing = Math.Sign(delta);
        X += Facing * speed * dt;
        X = Math.Clamp(X, 24, Math.Max(24,  worldWidth - 24));
        Speed = speed;
    }

    private double worldWidth = 1920;
    internal void SetWorldWidth(double width) => worldWidth = Math.Max(100, width);

    private void UpdateSocial(AgentWorld world, double dt)
    {
        var other = Goal?.TargetAgent;
        if (other is null || other.IsDisposed) return;
        double delta = other.X - X;
        if (Math.Abs(delta) > 76) MoveToward(other.X - Math.Sign(delta) * 64, dt, 92);
        else { Facing = Math.Sign(delta); _socialElapsed += dt; }
    }

    private void UpdateDrawing(double dt)
    {
        _drawPhase += dt * 3.2;
        double radius = 20 + Math.Sin(_drawPhase * 0.7) * 8;
        double px = X + Facing * (54 + Math.Cos(_drawPhase) * radius);
        double py = GroundY - 78 + Math.Sin(_drawPhase * 1.35) * radius;
        Renderer.AddDrawingPoint(px, py);
    }

    private void UpdatePropPlay(double dt)
    {
        var prop = Goal?.TargetProp;
        if (prop is null || prop.IsHeld) return;
        double dx = prop.CenterX - X;
        if (Math.Abs(dx) > 60) MoveToward(prop.CenterX - Math.Sign(dx) * 48, dt, 86);
        else
        {
            Facing = Math.Sign(dx);
            if (_goalElapsed > 0.8 && _random.NextDouble() < 0.08)
                prop.Push(Facing, 130 + Traits.Mischief * 100);
        }
    }

    public void Dispose()
    {
        if (IsDisposed) return;
        IsDisposed = true;
        Renderer.Dispose();
    }
}
