using System.Collections.Generic;
using System.Linq;
using Godot;

public partial class PlayerController : CharacterBody2D
{
    public enum PlayerAction
    {
        idle = 0,
        moving = 1,
        attacking = 2
    }
	[Export] Area2D visionArea;
    [Export] AnimatedSprite2D bodyAnimation;
    [Export] AnimationPlayer animationPlayer;
    [Export] MultiplayerSynchronizer multiSync;

    Camera2D camera;
	PlayerInfo playerInfo;
	PlayerAction action = PlayerAction.idle;
	Node2D interactingWith;
	Node2D lastInteractedWith;

    bool canControl = false;
	Vector2 dir;
	float speed = 100;
	Vector2 syncPosition = new Vector2(0,0);
	public PlayerInfo GetPlayerInfo { get { return playerInfo; } }

    public override void _Ready()
    {
		RpcAnimationChange(0);

		GetNode<MultiplayerSynchronizer>("MultiplayerSynchronizer").SetMultiplayerAuthority(int.Parse(Name));
		if (GetNode<MultiplayerSynchronizer>("MultiplayerSynchronizer").GetMultiplayerAuthority() != Multiplayer.GetUniqueId())
		{
			GetNode<Timer>("VisionTimer").Stop();
			GetNode("VisionTimer").QueueFree();
			return;
		}

		canControl = true;
		camera = GetNode<Camera2D>("Camera2D");
    }

    public override void _Process(double delta)
    {
		if (canControl && !MultiplayerGameManager.UiOpen)
		{
			dir.X = Input.GetAxis("MoveLeft", "MoveRight");
			dir.Y = Input.GetAxis("MoveUp", "MoveDown");
			dir = dir.Normalized();

			if (dir.X < 0 && !bodyAnimation.FlipH)
				{ HandleSpriteFlip(true); Rpc("HandleSpriteFlip", true); }
			else if (dir.X > 0 && bodyAnimation.FlipH)
				{ HandleSpriteFlip(false); Rpc("HandleSpriteFlip", false); }

			
            if (dir.X == 0 && dir.Y == 0)
                { HandleAnimationChange((int)PlayerAction.idle);}
            else
                { HandleAnimationChange((int)PlayerAction.moving);}

			Velocity = dir * speed;
			MoveAndSlide();
			syncPosition = GlobalPosition;
		} else {
			GlobalPosition = GlobalPosition.Lerp(syncPosition, 0.1f);
		}
    }
	
	public void SetPlayerInfo(PlayerInfo _playerInfo)
	{
		playerInfo = _playerInfo;
	}

    [Rpc(MultiplayerApi.RpcMode.AnyPeer)]
	private void HandleSpriteFlip(bool state)
	{
		bodyAnimation.FlipH = state;
	}

	private void HandleAnimationChange(int actionID)
	{
		if ((int)action != actionID)
		{
			action = (PlayerAction)actionID;
			RpcAnimationChange((int)PlayerAction.moving);
			Rpc("RpcAnimationChange", (int)PlayerAction.moving);
		}
	}

	[Rpc(MultiplayerApi.RpcMode.AnyPeer)]
	private void RpcAnimationChange(int actionID)
	{
		switch (action)
		{
			case PlayerAction.idle: {
                bodyAnimation.Play("Idle");
                break;
            }
            case PlayerAction.moving: {
                bodyAnimation.Play("Run");
                break;
            }
            default: {
                break;
            }
		}
	}

	public void LookForInteractable()
	{
		List<Node2D> physicsBodies = visionArea.GetOverlappingBodies().ToList();
		if (physicsBodies.Contains(visionArea.GetParent<Node2D>()))
			physicsBodies.Remove(visionArea.GetParent<Node2D>());

		physicsBodies = physicsBodies.Where(body => body is IInteractable).ToList();
		physicsBodies = physicsBodies.Where(body => ((IInteractable)body).GetCanPlayerInteract).ToList();
		physicsBodies = physicsBodies.OrderBy(body => body.GlobalPosition.DistanceTo(visionArea.GlobalPosition)).ToList();

		if (physicsBodies.Count == 0)
		{
			if (interactingWith != null)
			{
				((IInteractable)interactingWith).ToggleInteractionAnim(false);
				interactingWith = null;
			}
			return;
		}

		if (physicsBodies[0] != interactingWith)
		{
			if (interactingWith != null)
				((IInteractable)interactingWith).ToggleInteractionAnim(false);
			
			((IInteractable)physicsBodies[0]).ToggleInteractionAnim(true);
			interactingWith = physicsBodies[0];
		}
	}

    public override void _Input(InputEvent @event)
    {
		if (@event.IsActionReleased("InteractableInteraction") && !MultiplayerGameManager.UiOpen)
			if (interactingWith != null)
				((IInteractable)interactingWith).Interact();
    }
}