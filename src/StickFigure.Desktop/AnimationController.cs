using System;

namespace StickFigure;

public readonly record struct AgentPose(
    double BodyBob, double LegSwing, double ArmSwing,
    double TorsoLean, double ArmRaise, double HeadTilt);

/// <summary>Maps intent and motion state to a procedural hand-drawn pose.</summary>
public sealed class AnimationController
{
    private double _phase;
    public AgentPose Evaluate(AgentAction action, double speed, double verticalVelocity, double dt)
    {
        _phase += dt * (speed > 4 ? 8.5 : 2.1);
        double moving = Math.Clamp(speed / 110, 0, 1);
        double walk = Math.Sin(_phase);
        double bob = moving * Math.Abs(Math.Sin(_phase * 2)) * 4;
        double legs = walk * 17 * moving;
        double arms = -walk * 19 * moving;
        double lean = moving * 0.12;

        switch (action)
        {
            case AgentAction.Socialize:
                arms = 0;
                break;
            case AgentAction.Draw:
                arms = 0;
                break;
            case AgentAction.Rest:
                bob = Math.Sin(_phase) * 0.7;
                legs = 0;
                arms = 2;
                break;
            case AgentAction.Jump:
                bob = Math.Abs(verticalVelocity) * 0.012;
                legs = -7;
                arms = -12;
                break;
            case AgentAction.FollowCursor:
                arms = -walk * 24;
                break;
            case AgentAction.PlayWithProp:
                arms = 8;
                break;
        }

        double raise = action switch
        {
            AgentAction.Socialize => 0.8 + Math.Sin(_phase * 2.4) * 0.12,
            AgentAction.Draw => 0.65,
            AgentAction.Jump => 0.9,
            AgentAction.PlayWithProp => 0.35,
            _ => 0
        };
        return new AgentPose(bob, legs, arms, lean, raise, action == AgentAction.Rest ? 0.12 : 0);
    }
}
