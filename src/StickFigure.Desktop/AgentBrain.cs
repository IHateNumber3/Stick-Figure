using System;
using System.Collections.Generic;
using System.Linq;

namespace StickFigure;

public enum AgentAction
{
    Wander, Socialize, Draw, Rest, FollowCursor, PlayWithProp, Jump, Inspect
}

public sealed class AgentTraits
{
    public double Curiosity { get; init; }
    public double Sociability { get; init; }
    public double Creativity { get; init; }
    public double Mischief { get; init; }
    public double Calmness { get; init; }

    public static AgentTraits Generate(Random random) => new()
    {
        Curiosity = 0.25 + random.NextDouble() * 0.75,
        Sociability = 0.15 + random.NextDouble() * 0.85,
        Creativity = 0.2 + random.NextDouble() * 0.8,
        Mischief = random.NextDouble(),
        Calmness = 0.2 + random.NextDouble() * 0.8
    };
}

public sealed class AgentNeeds
{
    public double Boredom { get; private set; } = 0.45;
    public double Social { get; private set; } = 0.3;
    public double Creative { get; private set; } = 0.35;
    public double Rest { get; private set; } = 0.2;
    public void Update(double dt, AgentAction action)
    {
        Boredom = Math.Clamp(Boredom + dt * 0.018, 0, 1);
        Social = Math.Clamp(Social + dt * 0.009, 0, 1);
        Creative = Math.Clamp(Creative + dt * 0.007, 0, 1);
        Rest = Math.Clamp(Rest + dt * (action == AgentAction.Jump ? 0.035 : 0.003), 0, 1);
    }
    public void Satisfy(AgentAction action)
    {
        switch (action)
        {
            case AgentAction.Wander: Boredom = 0.12; break;
            case AgentAction.Socialize: Social = 0.05; break;
            case AgentAction.Draw: Creative = 0.05; Boredom = 0.2; break;
            case AgentAction.Rest: Rest = 0.05; break;
            case AgentAction.FollowCursor: Boredom = 0.1; break;
            case AgentAction.PlayWithProp: Boredom = 0.08; break;
            case AgentAction.Jump: Boredom = 0.15; break;
            case AgentAction.Inspect: Boredom = 0.15; break;
        }
    }
}

public sealed class AgentMemory
{
    private readonly Queue<AgentAction> _recent = new();
    private readonly Dictionary<AgentAction, double> _reward = Enum.GetValues<AgentAction>().ToDictionary(a => a, _ => 1.0);
    private readonly Queue<double> _visitedX = new();
    public int Decisions { get; private set; }
    public AgentAction LastAction { get; private set; } = AgentAction.Wander;

    public double Repetition(AgentAction action) => _recent.Count(a => a == action) * 0.22;
    public double Preference(AgentAction action) => _reward[action];

    public void RememberDecision(AgentAction action)
    {
        LastAction = action;
        Decisions++;
        _recent.Enqueue(action);
        while (_recent.Count > 6) _recent.Dequeue();
    }

    public void RememberPosition(double x)
    {
        _visitedX.Enqueue(x);
        while (_visitedX.Count > 8) _visitedX.Dequeue();
    }

    public double Familiarity(double x) =>
        _visitedX.Count == 0 ? 0 : _visitedX.Count(v => Math.Abs(v - x) < 100) / (double)_visitedX.Count;

    public void Learn(AgentAction action, bool success)
    {
        _reward[action] = Math.Clamp(_reward[action] + (success ? 0.08 : -0.06), 0.55, 1.65);
    }
}

public sealed record AgentPerception(
    double ScreenWidth,
    double ScreenHeight,
    double SelfX,
    StickmanAgent? NearestAgent,
    double AgentDistance,
    double AgentDeltaX,
    PhysicsProp? NearestProp,
    double PropDistance,
    double PropDeltaX,
    double CursorX,
    double CursorY,
    bool CursorAvailable,
    DesktopSurface? NearestSurface);

public sealed record AgentGoal(AgentAction Action, double TargetX, StickmanAgent? TargetAgent = null, PhysicsProp? TargetProp = null);

/// <summary>Scores possible intentions from needs, personality, memory and current surroundings.</summary>
public sealed class AgentBrain
{
    private readonly Random _random;
    public AgentBrain(int seed) => _random = new Random(seed);

    public AgentGoal Choose(StickmanAgent agent, AgentPerception p)
    {
        var t = agent.Traits;
        var n = agent.Needs;
        var m = agent.Memory;
        var scores = new Dictionary<AgentAction, double>
        {
            [AgentAction.Wander] = (0.15 + n.Boredom) * t.Curiosity,
            [AgentAction.Socialize] = p.NearestAgent is null ? 0.01 : (0.15 + n.Social) * t.Sociability * Proximity(p.AgentDistance, 500),
            [AgentAction.Draw] = (0.15 + n.Creative) * t.Creativity,
            [AgentAction.Rest] = n.Rest * t.Calmness,
            [AgentAction.FollowCursor] = p.CursorAvailable ? (0.08 + t.Mischief * 0.5) * Proximity(Math.Abs(p.CursorX - p.SelfX), 700) : 0.01,
            [AgentAction.PlayWithProp] = p.NearestProp is null ? 0.01 : (0.2 + n.Boredom) * (0.4 + t.Mischief) * Proximity(p.PropDistance, 420),
            [AgentAction.Jump] = (0.05 + n.Boredom * 0.35) * (0.3 + t.Mischief),
            [AgentAction.Inspect] = (0.05 + n.Boredom * 0.25 + (p.NearestSurface is null ? 0 : 0.35)) * t.Curiosity
        };

        foreach (var action in Enum.GetValues<AgentAction>())
            scores[action] = Math.Max(0.005, scores[action] * m.Preference(action) - m.Repetition(action) + _random.NextDouble() * 0.035);

        AgentAction chosen = scores.OrderByDescending(pair => pair.Value).First().Key;
        double target = chosen switch
        {
            AgentAction.Wander => RandomTarget(p.ScreenWidth, m),
            AgentAction.Socialize when p.NearestAgent is not null => p.NearestAgent.X,
            AgentAction.PlayWithProp when p.NearestProp is not null => p.NearestProp.CenterX,
            AgentAction.FollowCursor when p.CursorAvailable => p.CursorX,
            AgentAction.Inspect when p.NearestSurface is not null => p.NearestSurface.CenterX,
            AgentAction.Inspect => _random.Next(2) == 0 ? 24 : p.ScreenWidth - 24,
            _ => p.SelfX
        };
        m.RememberDecision(chosen);
        return new AgentGoal(chosen, target, p.NearestAgent, p.NearestProp);
    }

    private static double Proximity(double distance, double range) =>
        double.IsFinite(distance) ? Math.Clamp(1 - distance / range, 0.08, 1) : 0.08;

    private double RandomTarget(double width, AgentMemory memory)
    {
        double target = 40 + _random.NextDouble() * Math.Max(1, width - 80);
        for (int i = 0; i < 4 && memory.Familiarity(target) > 0.35; i++)
            target = 40 + _random.NextDouble() * Math.Max(1, width - 80);
        return target;
    }
}
