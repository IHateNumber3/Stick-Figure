using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;

namespace StickFigure;

/// <summary>
/// Owns the shared simulation clock and world. Every agent is updated from one
/// fixed-step loop; agents do not create timers or directly control one another.
/// </summary>
public sealed class AgentWorld : IDisposable
{
    private readonly Canvas _canvas;
    private readonly DispatcherTimer _timer;
    private readonly Stopwatch _clock = Stopwatch.StartNew();
    private readonly List<StickmanAgent> _agents = new();
    private readonly List<PhysicsProp> _props = new();
    private readonly Stopwatch _environmentRefreshClock = Stopwatch.StartNew();
    public DesktopEnvironment Desktop { get; } = new();
    private TimeSpan _lastTick;
    private int _nextName = 1;
    private int _seed = Environment.TickCount;
    private static readonly Color[] Palette =
    {
        Color.FromRgb(235, 243, 250), Color.FromRgb(0, 212, 255),
        Color.FromRgb(0, 255, 153), Color.FromRgb(255, 190, 90),
        Color.FromRgb(255, 112, 150), Color.FromRgb(180, 145, 255),
        Color.FromRgb(255, 225, 120)
    };

    public IReadOnlyList<StickmanAgent> Agents => _agents;
    public double Width => Math.Max(100, _canvas.ActualWidth);
    public double Height => Math.Max(100, _canvas.ActualHeight);
    public bool CursorAvailable { get; private set; }
    public double CursorX { get; private set; }
    public double CursorY { get; private set; }

    public AgentWorld(Canvas canvas)
    {
        _canvas = canvas;
        _timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(16) };
        _timer.Tick += OnTick;
        _lastTick = _clock.Elapsed;
        Desktop.Refresh();
        _timer.Start();
    }

    public StickmanAgent AddAgent(double? x = null)
    {
        var random = new Random(unchecked(++_seed * 397));
        double startX = x ?? (40 + random.NextDouble() * Math.Max(1, Width - 80));
        Color color = Palette[(_nextName - 1) % Palette.Length];
        var agent = new StickmanAgent(_canvas, $"Stickman {_nextName++}", color, startX, _seed);
        agent.SetWorldWidth(Width);
        _agents.Add(agent);
        return agent;
    }

    public void RegisterProp(PhysicsProp prop)
    {
        if (!_props.Contains(prop)) _props.Add(prop);
    }

    public void UnregisterProp(PhysicsProp prop) => _props.Remove(prop);
    public void ClearProps() => _props.Clear();

    public void SetCursor(double x, double y, bool available = true)
    {
        CursorX = x;
        CursorY = y;
        CursorAvailable = available;
    }

    public void ClearAgents()
    {
        foreach (var agent in _agents) agent.Dispose();
        _agents.Clear();
    }

    public AgentPerception Perceive(StickmanAgent self)
    {
        var nearest = _agents.Where(a => !ReferenceEquals(a, self) && !a.IsDisposed)
            .Select(a => (Agent: a, Delta: a.X - self.X))
            .OrderBy(v => Math.Abs(v.Delta)).FirstOrDefault();
        var prop = _props.Where(p => !p.IsHeld)
            .Select(p => (Prop: p, Delta: p.CenterX - self.X))
            .OrderBy(v => Math.Abs(v.Delta)).FirstOrDefault();

        return new AgentPerception(
            Width, Height, self.X,
            nearest.Agent,
            nearest.Agent is null ? double.PositiveInfinity : Math.Abs(nearest.Delta),
            nearest.Agent is null ? 0 : nearest.Delta,
            prop.Prop,
            prop.Prop is null ? double.PositiveInfinity : Math.Abs(prop.Delta),
            prop.Prop is null ? 0 : prop.Delta,
            CursorX, CursorY, CursorAvailable,
            Desktop.FindNearestSurface(self.X));
    }

    private void OnTick(object? sender, EventArgs e)
    {
        var now = _clock.Elapsed;
        if (_environmentRefreshClock.ElapsedMilliseconds >= 650)
        {
            Desktop.Refresh();
            _environmentRefreshClock.Restart();
        }
        double dt = Math.Clamp((now - _lastTick).TotalSeconds, 0.001, 0.05);
        _lastTick = now;
        foreach (var agent in _agents)
        {
            agent.SetWorldWidth(Width);
            agent.Update(this, dt);
        }
    }

    public void Dispose()
    {
        _timer.Stop();
        ClearAgents();
    }
}
