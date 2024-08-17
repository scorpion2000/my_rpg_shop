using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

public partial class HeroBrain : Brain
{
    [Export] public float minEngageDistance = 32;
    [Export] Timer pathfindToTarget;
    [Export] Timer attackTimer;
    [Export] Inventory inventory;
    [Export] VisibleOnScreenNotifier2D visibilityChecker;
    [Export] AnimatedSprite2D bodyAnimation;
    [Export] AnimationPlayer animationPlayer;
    [Export] NavigationAgent2D navAgent;
    AiVision vision;
    Shop shopToCheck;
    bool isVisibleToAnyClient = true;
    Vector2 lastStuckPos = new Vector2(0,0);
    Vector2 globalShopPosition;
    Vector2 globalFleePosition;
    public Timer wanderTimer;

    public override void _Ready()
    {
        health = GetNode<Health>("Health");
        movement = GetNode<Movement>("Movement");
        multiSync = GetNode<MultiplayerSynchronizer>("MultiplayerSynchronizer");

        if (!Multiplayer.IsServer())
        {
            navAgent.QueueFree();
            GetNode<Timer>("VisionTimer").Stop();
            GetNode<Timer>("VisionTimer").QueueFree();
            GetNode<Timer>("MultiSyncCheck").Stop();
            GetNode<Timer>("MultiSyncCheck").QueueFree();
            GetNode<Timer>("AutoHeal").Stop();
            GetNode<Timer>("AutoHeal").QueueFree();
            GetNode<Area2D>("VisionCircle").QueueFree();

            visibilityChecker.ScreenEntered += NodeEnteredTheCamera;
            visibilityChecker.ScreenExited += NodeLeftTheCamera;
            RpcAnyHandleBrainActionChange(0);
            return;
        }

        idleState = new Idle(this, GetNode<Timer>("StateDecisionTimer"));
        combatState = new Combat(this, pathfindToTarget, attackTimer);
        fleeingState = new Fleeing(this, GetTree().Root.GetNode("World").GetNode<Node2D>("GlobalFleePosition").GlobalPosition);
        adventureState = new Adventure(this);
        shoppingState = new Shopping(this, GetNode<Timer>("ShoppingTimer"));

        team = 1;

        base._Ready();

        vision = GetNode<AiVision>("Vision");
        globalShopPosition = GetTree().Root.GetNode("World").GetNode<Node2D>("GlobalMarketPos").GlobalPosition;

        shoppingState.StopShopping += ResetShop;
        vision.VisibleTargetChanged += OnTargetChanged;
        vision.ShopsSeen += HandleShopsSeen;
        BrainActionChanged += HandleBrainActionChange;

        SetBrainAction = BrainAction.idle;
    }

    public override void Wander()
    {
        base.Wander();
    }

    public override void Spawn(Vector2 _spawnPos, Node2D _home, CollisionShape2D _homeZone)
    {
        base.Spawn(_spawnPos, _home, _homeZone);
    }
    public override void SetName(string newName)
    {
        characterName = newName;
    }

    public override void OnTargetChanged(Node2D newTarget)
    {
        if (currentState != fleeingState)
            base.OnTargetChanged(newTarget);
    }
    
    public void TryApplyDamage()
    {
        if (!Multiplayer.IsServer())
            return;

        if (target == null || !IsInstanceValid(target))
            return;

        if (GlobalPosition.DistanceTo(target.GlobalPosition) < (combatState.minEngageDistance * 1.2))
			target.GetNode<Health>("Health").ApplyDamage(inventory.EquippedWeapon.GetBaseDamage, this);
    }

    public void OnCharacterClick(Node viewPort, InputEvent inputEvent, int ShapeIndex)
    {
        //DEBUG CODE, FOR NOW
        if (inputEvent is InputEventMouseButton eventKey) 
            if (eventKey.Pressed && Input.IsKeyPressed(Key.Ctrl))
                base.OnCharacterClick(this);
    }

    public void ResetAttack()
    {
        if (brainAction == BrainAction.attacking)
            SetBrainAction = BrainAction.idle;
    }

    private void HandleBrainActionChange(BrainAction action)
    {
        brainAction = action;
        
        RpcAnyHandleBrainActionChange((int)action);
        Rpc("RpcAnyHandleBrainActionChange", (int)action);
    }

    [Rpc(MultiplayerApi.RpcMode.AnyPeer)]
    private void RpcAnyHandleBrainActionChange(int actionID)
    {
        if (!bodyAnimation.SpriteFrames.HasAnimation("Idle"))
            return;
        BrainAction action = (BrainAction)actionID;
        switch (action)
        {
            case BrainAction.idle: {
                bodyAnimation.Play("Idle");
                break;
            }
            case BrainAction.moving: {
                bodyAnimation.Play("Run");
                break;
            }
            case BrainAction.attacking: {
                animationPlayer.Play("attackAnimR");
                break;
            }
            case BrainAction.enterShop: {
                bodyAnimation.Play("Run");
                break;
            }
            default: {
                break;
            }
        }
    }

    [Rpc(MultiplayerApi.RpcMode.AnyPeer)]
    public void RpcAnyHandleSpriteFlip(bool state)
    {
        bodyAnimation.FlipH = state;
    }

    [Rpc(MultiplayerApi.RpcMode.AnyPeer)]
    private void RpcServerVisibleToClient()
    {
        if (isVisibleToAnyClient)
            return;

        isVisibleToAnyClient = true;

        multiSync.ReplicationInterval = 0.1;
        multiSync.DeltaInterval = 0.1;
    }

    [Rpc(MultiplayerApi.RpcMode.AnyPeer)]
    private void RpcServerVisibilityResponse()
    {
        RpcServerVisibleToClient(); //Server calling this to itself locally
    }

    [Rpc(MultiplayerApi.RpcMode.AnyPeer)]
    private void RpcClientVisibilityCheck()
    {
        if (isVisibleToAnyClient)
            RpcId(1, "RpcServerVisibilityResponse");
    }

    private void OnSyncCheckup()
    {
        if (!isVisibleToAnyClient)
        {
            multiSync.ReplicationInterval = 1;
            multiSync.DeltaInterval = 1;
        }

        isVisibleToAnyClient = false;

        Rpc("RpcClientVisibilityCheck");
    }

    private void NodeEnteredTheCamera()
    {
        isVisibleToAnyClient = true;
    }

    private void NodeLeftTheCamera()
    {
        isVisibleToAnyClient = false;
    }

    private void HandleShopsSeen(List<Node2D> shopsSeen)
    {
        if (currentState != shoppingState)
            return;
        
        foreach (Shop shop in shopsSeen)
            shoppingState.BrainFoundShop(shop);
    }

    private void ResetShop()
    {
        shopToCheck = null;
    }
}