using System;
using System.Collections.Generic;
using System.Linq;

namespace StickFigure;

public enum AgentAction { Explore, Socialize, Draw, Rest, PlayWithProp }
public enum AgentMood { Curious, Social, Creative, Calm }

public sealed record AgentGoal(AgentAction Action, double TargetX = double.NaN, ProceduralStickman? TargetAgent = null, PhysicsProp? TargetProp = null);

public sealed class AgentPerception
{
    public required double X { get; init; }
    public required double ScreenWidth { get; init; }
    public required double EdgeDistance { get; init; }
    public ProceduralStickman? NearestAgent { get; init; }
    public double AgentDistance { get; init; } = double.PositiveInfinity;
    public double AgentDeltaX { get; init; }
    public PhysicsProp? NearestProp { get; init; }
    public double PropDistance { get; init; } = double.PositiveInfinity;
    public double PropDeltaX { get; init; }
    public int NearbyAgents { get; init; }
    public bool HasInterestingProp => NearestProp is not null && PropDistance < 360;
}

public sealed class AgentMemory
{
    private readonly Queue<AgentAction> _recentActions = new();
    private readonly Dictionary<AgentAction, double> _preferences = Enum.GetValues<AgentAction>().ToDictionary(a => a, _ => 1.0);
    public double Boredom { get; private set; } = 0.25;
    public double SocialNeed { get; private set; } = 0.4;
    public double CreativityNeed { get; private set; } = 0.35;
    public AgentAction LastAction { get; private set; } = AgentAction.Explore;
    public int DecisionsMade { get; private set; }

    public double RepetitionPenalty(AgentAction action)
    {
        int repeats = _recentActions.Count(a => a == action);
        return repeats * 0.24;
    }

    public void RecordDecision(AgentAction action)
    {
        LastAction = action;
        DecisionsMade++;
        _recentActions.Enqueue(action);
        while (_recentActions.Count > 5) _recentActions.Dequeue();
        Boredom = Math.Clamp(Boredom + 0.13, 0, 1);
        SocialNeed = Math.Clamp(SocialNeed + 0.06, 0, 1);
        CreativityNeed = Math.Clamp(CreativityNeed + 0.05, 0, 1);
    }

    public void RecordOutcome(AgentAction action, bool successful)
    {
        double change = successful ? 0.12 : -0.08;
        _preferences[action] = Math.Clamp(_preferences[action] + change, 0.55, 1.8);
        switch (action)
        {
            case AgentAction.Socialize: SocialNeed = successful ? 0.08 : Math.Min(1, SocialNeed + 0.12); break;
            case AgentAction.Draw: CreativityNeed = successful ? 0.05 : Math.Min(1, CreativityNeed + 0.1); break;
            case AgentAction.Rest: Boredom = successful ? 0.05 : Math.Min(1, Boredom + 0.1); break;
            default: Boredom = successful ? 0.15 : Math.Min(1, Boredom + 0.1); break;
        }
    }

    public double Preference(AgentAction action) => _preferences[action];
}

public sealed class AgentBrain
{
    private readonly double _socialTrait, _creativeTrait, _curiosityTrait, _calmTrait;
    private readonly Random _random;
    private AgentGoal _currentGoal = new(AgentAction.Explore);
    public AgentGoal CurrentGoal => _currentGoal;
    public int DecisionsMade { get; private set; }

    public AgentBrain(int personalitySeed)
    {
        _random = new Random(unchecked(Environment.TickCount * 31 + personalitySeed * 7919));
        _socialTrait = 0.25 + _random.NextDouble() * 0.75;
        _creativeTrait = 0.25 + _random.NextDouble() * 0.75;
        _curiosityTrait = 0.25 + _random.NextDouble() * 0.75;
        _calmTrait = 0.25 + _random.NextDouble() * 0.75;
    }

    public AgentGoal Decide(AgentPerception p, AgentMemory memory)
    {
        var scores = new Dictionary<AgentAction, double>
        {
            [AgentAction.Explore] = _curiosityTrait * (0.35 + memory.Boredom) * memory.Preference(AgentAction.Explore),
            [AgentAction.Socialize] = p.NearestAgent is null ? 0.02 : _socialTrait * (0.25 + memory.SocialNeed) * (p.AgentDistance < 520 ? 1.2 : 0.45) * memory.Preference(AgentAction.Socialize),
            [AgentAction.Draw] = _creativeTrait * (0.2 + memory.CreativityNeed) * memory.Preference(AgentAction.Draw),
            [AgentAction.Rest] = _calmTrait * (0.15 + (1 - memory.Boredom) * 0.35) * memory.Preference(AgentAction.Rest),
            [AgentAction.PlayWithProp] = p.HasInterestingProp ? _curiosityTrait * (p.PropDistance < 220 ? 1.35 : 0.8) : 0.01
        };

        foreach (var action in Enum.GetValues<AgentAction>())
            scores[action] = Math.Max(0.01, scores[action] - memory.RepetitionPenalty(action) + _random.NextDouble() * 0.12);

        // Prefer leaving a crowded edge and avoid choosing a peer target from across the whole screen.
        if (p.EdgeDistance < 90) scores[AgentAction.Explore] += 0.55;
        if (p.NearestAgent is not null && p.AgentDistance < 150) scores[AgentAction.Socialize] += 0.35;

        var chosen = scores.OrderByDescending(pair => pair.Value).First().Key;
        _currentGoal = chosen switch
        {
            AgentAction.Socialize when p.NearestAgent is not null => new AgentGoal(chosen, p.NearestAgent.AgentX, p.NearestAgent),
            AgentAction.PlayWithProp when p.NearestProp is not null => new AgentGoal(chosen, p.NearestProp.CenterX, TargetProp: p.NearestProp),
            AgentAction.Explore => new AgentGoal(chosen, RandomTarget(p)),
            _ => new AgentGoal(chosen)
        };
        memory.RecordDecision(chosen);
        DecisionsMade++;
        return _currentGoal;
    }

    private double RandomTarget(AgentPerception p)
    {
        double margin = Math.Min(120, p.ScreenWidth * 0.12);
        return margin + _random.NextDouble() * Math.Max(1, p.ScreenWidth - margin * 2);
    }

    public void ReportOutcome(AgentMemory memory, bool successful) => memory.RecordOutcome(_currentGoal.Action, successful);
}

public sealed class AgentWorld
{
    private readonly List<ProceduralStickman> _agents = new();
    public void Register(ProceduralStickman agent) { if (!_agents.Contains(agent)) _agents.Add(agent); }
    public void Unregister(ProceduralStickman agent) => _agents.Remove(agent);

    public AgentPerception Perceive(ProceduralStickman self, double x, double width, IReadOnlyList<PhysicsProp> props)
    {
        var peers = _agents.Where(a => !ReferenceEquals(a, self) && !a.IsDisposed)
            .Select(a => (Agent: a, Delta: a.AgentX - x))
            .OrderBy(item => Math.Abs(item.Delta)).ToArray();
        var prop = props.Where(p => !p.IsHeld)
            .Select(p => (Prop: p, Delta: p.CenterX - x))
            .OrderBy(item => Math.Abs(item.Delta)).FirstOrDefault();
        return new AgentPerception
        {
            X = x, ScreenWidth = width, EdgeDistance = Math.Min(x, width - x),
            NearestAgent = peers.Length > 0 ? peers[0].Agent : null,
            AgentDistance = peers.Length > 0 ? Math.Abs(peers[0].Delta) : double.PositiveInfinity,
            AgentDeltaX = peers.Length > 0 ? peers[0].Delta : 0,
            NearbyAgents = peers.Count(item => Math.Abs(item.Delta) < 420),
            NearestProp = prop.Prop,
            PropDistance = prop.Prop is null ? double.PositiveInfinity : Math.Abs(prop.Delta),
            PropDeltaX = prop.Prop is null ? 0 : prop.Delta
        };
    }
}
