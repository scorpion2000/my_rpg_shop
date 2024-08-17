using System.Collections;
using System.Collections.Generic;
using Godot;

public partial class Fleeing : BaseState
{
    private Brain brainSM;
    private Vector2 fleePosition;
    int minDistanceToHome = 32;

    public Fleeing(Brain stateMachine, Vector2 _fleePosition) : base("Fleeing", stateMachine)
    {
        brainSM = stateMachine;
        fleePosition = _fleePosition;
    }

    public override void Enter()
    {
        base.Enter();

        brainSM.target = null;
        Flee();
    }

    public override void UpdateLogic()
    {
        base.UpdateLogic();

        if (brainSM.GlobalPosition.DistanceTo(fleePosition) < minDistanceToHome)
            brainSM.ChangeState(brainSM.idleState);
    }

    public override void Exit()
    {
        base.Exit();
    }

    private void Flee()
    {
        brainSM.movement.MoveTo(fleePosition);
        //SetBrainAction = BrainAction.fleeing;
    }
}
