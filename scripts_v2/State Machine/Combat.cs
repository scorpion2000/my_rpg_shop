using Godot;

public partial class Combat : BaseState
{
    private Brain brainSM;
    private Timer pathfindToTarget;
    private Timer attackTimer;
    public int minEngageDistance = 32;
    public float looseDistance = 150;
    public double SetAttackTimer { set { attackTimer.WaitTime = value; } }

    public Combat(Brain stateMachine, Timer _pathfindToTarget, Timer _attackTimer) : base("Combat", stateMachine)
    {
        brainSM = stateMachine;
        pathfindToTarget = _pathfindToTarget;
        attackTimer = _attackTimer;
        attackTimer.Timeout += TryAttack;
        pathfindToTarget.Timeout += RepathToTarget;
    }

    public override void Enter()
    {
        base.Enter();

        MoveToTarget();
        brainSM.movement.PathFinished += EngageTarget;
    }

    public override void UpdateLogic()
    {
        base.UpdateLogic();

        if (brainSM.target == null || !IsInstanceValid(brainSM.target) || ((Brain)brainSM.target).health.GetHealth <= 0)
            {brainSM.target = null; brainSM.ChangeState(brainSM.idleState);}

        ShouldFlee();
    }

    public override void Exit()
    {
        base.Exit();

        Disengage();
        pathfindToTarget.Stop();
        brainSM.movement.PathFinished -= EngageTarget;
    }

    private void MoveToTarget()
    {
        pathfindToTarget.Start();
        RepathToTarget();
    }

    private void EngageTarget()
    {
        attackTimer.Start();
        pathfindToTarget.Stop();
        TryAttack();
    }
    
    private void Disengage()
    {
        attackTimer.Stop();
    }

    public void TryAttack()
    {
        if (brainSM.GlobalPosition.DistanceTo(brainSM.target.GlobalPosition) < minEngageDistance)
            brainSM.SetBrainAction = Brain.BrainAction.attacking;
        else
            MoveToTarget();
    }

    private void RepathToTarget()
    {
        if (brainSM.GlobalPosition.DistanceTo(brainSM.target.GlobalPosition) > looseDistance)
        {
            brainSM.target = null;
            Disengage();
            return;
        }

        brainSM.movement.MoveTo(brainSM.target.GlobalPosition);
    }

    private void ShouldFlee()
    {
        if (brainSM.fleeingState == null)
            return;

        if (((float)brainSM.health.GetHealth / (float)brainSM.health.maxHealth) <= 0.2f)    //20% Health or below
            brainSM.ChangeState(brainSM.fleeingState);
    }
}
