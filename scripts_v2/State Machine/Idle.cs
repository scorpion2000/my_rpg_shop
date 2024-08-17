using System;
using System.Collections.Generic;
using Godot;

public partial class Idle : BaseState
{
    private Brain brainSM;
    private Timer idleDecisionTimer;

    public Idle(Brain stateMachine, Timer timer) : base("Idle", stateMachine)
    {
        brainSM = stateMachine;
        idleDecisionTimer = timer;
    }

    public override void Enter()
    {
        base.Enter();

        brainSM.listenToIdleCallback = true;
        if (idleDecisionTimer != null)
            idleDecisionTimer.Timeout += IdleDecision;
    }

    public override void UpdateLogic()
    {
        base.UpdateLogic();

        if (brainSM.target != null || IsInstanceValid(brainSM.target))
            brainSM.ChangeState(brainSM.combatState);
    }

    public override void Exit()
    {
        base.Exit();

        brainSM.listenToIdleCallback = false;
        if (idleDecisionTimer != null)
            idleDecisionTimer.Timeout -= IdleDecision;
    }

    private void IdleDecision()
    {
        if (brainSM.shoppingState != null && brainSM.GetNode<Inventory>("Inventory").MobInventory.Count > 0)
        {
            brainSM.ChangeState(brainSM.shoppingState);
            return;
        }

        if (brainSM.adventureState != null && ((float)brainSM.health.GetHealth / (float)brainSM.health.maxHealth) > 0.5f)
        {
            if (MultiplayerGameManager.SpawnedLairs.Count > 0)
            {
                brainSM.ChangeState(brainSM.adventureState);
                return;
            }
        }

        brainSM.Wander();
    }
}
