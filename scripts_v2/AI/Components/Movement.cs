using System;
using Godot;

public partial class Movement : Node
{
    [Export] Brain brain;
    [Export] NavigationAgent2D navAgent;
    [Export] float speed = 55;
    [Export] int minDistanceToFinish = 16;
    [Export] Timer moveStuckTimer;
    [Export] AnimatedSprite2D bodyAnimation;
	[Export] public Vector2 syncPosition;
    int minDistance = 0;
    Vector2 lastStuckPos;
    Vector2 targetPosition;
    Vector2 setLastStuckPosition { set { if (lastStuckPos.DistanceTo(value) > 2) lastStuckPos = value; else FinishPath(); } }
    int minDistanceOverride { set { if (value > 0) minDistance = value; else minDistance = minDistanceToFinish; } }
    public float SetMaxSpeed { set { speed = value;}}
    public event Action PathFinished;

    public override void _PhysicsProcess(double delta)
    {
        if (!Multiplayer.IsServer())
        {
			brain.GlobalPosition = brain.GlobalPosition.Lerp(syncPosition, 0.1f);
            return;
        }
        
        base._Process(delta);

        if (targetPosition != Vector2.Zero)
        {
            if (moveStuckTimer.TimeLeft == 0)
                {moveStuckTimer.Start(); setLastStuckPosition = brain.GlobalPosition;}

            if (brain.GlobalPosition.DistanceTo(targetPosition) < minDistance)
                FinishPath();
            else
                MoveToTargetPos();
        }
        syncPosition = brain.GlobalPosition;
    }

    private void MoveToTargetPos()
	{
		Vector2 dir = brain.ToLocal(navAgent.GetNextPathPosition()).Normalized();
		if (dir.X < 0 && !bodyAnimation.FlipH)
            { RpcAnySpriteFlip(true); Rpc("RpcAnySpriteFlip", true); }
		else if (dir.X > 0 && bodyAnimation.FlipH)
            { RpcAnySpriteFlip(false); Rpc("RpcAnySpriteFlip", false); }
		brain.Velocity = dir * speed;
		brain.MoveAndSlide();
	}

    private void FinishPath()
    {
        targetPosition = Vector2.Zero;
        brain.SetBrainAction = Brain.BrainAction.idle;
        PathFinished?.Invoke();
    }

    public void MoveTo(Vector2 newTargetPos, int _minDistanceOverride = 0)
	{
        minDistanceOverride = _minDistanceOverride;

        lastStuckPos = Vector2.Zero;
        targetPosition = newTargetPos;
        if (brain.GlobalPosition.DistanceTo(targetPosition) > minDistance)
        {
		    navAgent.TargetPosition = targetPosition;
            brain.SetBrainAction = Brain.BrainAction.moving;
        }
	}

    [Rpc(MultiplayerApi.RpcMode.AnyPeer)]
    private void RpcAnySpriteFlip(bool flipH)
    {
        bodyAnimation.FlipH = flipH;
    }
}