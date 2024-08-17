using System;
using Godot;

public partial class Adventure : BaseState
{
    private Brain brainSM;
    private Node2D targetLair;

    public Adventure(Brain stateMachine) : base("Adventure", stateMachine)
    {
        brainSM = stateMachine;
    }

    public override void Enter()
    {
        base.Enter();

        LookForLair();
        brainSM.movement.PathFinished += MakePathToTargetLair;
    }

    public override void UpdateLogic()
    {
        base.UpdateLogic();

        if (MultiplayerGameManager.SpawnedLairs.Count == 0)
            brainSM.ChangeState(brainSM.idleState);

        if (brainSM.target != null || IsInstanceValid(brainSM.target))
            brainSM.ChangeState(brainSM.combatState);
        
        ShouldFlee();
    }

    public override void Exit()
    {
        base.Exit();
        brainSM.movement.PathFinished -= MakePathToTargetLair;
    }

    private void ShouldFlee()
    {
        if (brainSM.fleeingState == null)
            return;

        if (((float)brainSM.health.GetHealth / (float)brainSM.health.maxHealth) <= 0.2f)
            brainSM.ChangeState(brainSM.fleeingState);
    }

    private void LookForLair()
    {
        if (MultiplayerGameManager.SpawnedLairs.Count == 0)
            return;
        Node2D lair = MultiplayerGameManager.SpawnedLairs[GD.RandRange(0, MultiplayerGameManager.SpawnedLairs.Count - 1)];
        targetLair = lair;

        MakePathToTargetLair();
    }

    private void MakePathToTargetLair()
    {
        if (targetLair == null || !IsInstanceValid(targetLair))
        {
            LookForLair();
            return;
        }

        CollisionShape2D lairArea = targetLair.GetNode("SpawnArea").GetNode<CollisionShape2D>("CollisionShape2D");
        var lairAreaPos = lairArea.GlobalPosition;
		var lairAreaEnd = lairArea.Shape.GetRect().End;

		Random rnd = new Random();
        Vector2 rndLairPos = new Vector2(
			rnd.Next((int)lairAreaPos.X - (int)lairAreaEnd.X, (int)lairAreaPos.X + (int)lairAreaEnd.X), 
			rnd.Next((int)lairAreaPos.Y - (int)lairAreaEnd.Y, (int)lairAreaPos.Y + (int)lairAreaEnd.Y)
		);

        brainSM.movement.MoveTo(rndLairPos);
    }
}
