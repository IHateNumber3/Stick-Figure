namespace StickFigure;

public sealed class AnimationController
{
    public AgentAction Action { get; private set; } = AgentAction.Explore;
    public double Phase { get; private set; }
    public bool IsWalking { get; private set; }
    public bool IsDrawing => Action == AgentAction.Draw;
    public bool IsSocializing => Action == AgentAction.Socialize;
    public bool IsResting => Action == AgentAction.Rest;

    public void SetAction(AgentAction action) => Action = action;

    public void Update(double dt, bool moving)
    {
        IsWalking = moving && Action is AgentAction.Explore or AgentAction.Socialize or AgentAction.PlayWithProp;
        Phase += dt * (IsWalking ? 8.5 : IsSocializing ? 2.8 : IsDrawing ? 2.1 : 0.8);
    }
}
