using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

public partial class MonsterBrain : Brain
{
    [Export] public float looseDistance = 150;
    [Export] public float minEngageDistance = 32;
    [Export] public float speed = 55;
    [Export] int baseDamageOverride = 2;
    [Export] AnimatedSprite2D bodyAnimation;
    [Export] AnimationPlayer animationPlayer;
    [Export] NavigationAgent2D navAgent;
    [Export] AiVision vision;
    [Export] VisibleOnScreenNotifier2D visibilityChecker;
    Node2D killer;
    bool isVisibleToAnyClient = true;
    Vector2 wanderPosition = new Vector2(0,0);
    Vector2 lastStuckPos = new Vector2(0,0);
	private Vector2 syncPosition = new Vector2(0,0);
    public Timer wanderTimer;

    public override void _Ready()
    {
        health = GetNode<Health>("Health");
        movement = GetNode<Movement>("Movement");
        multiSync = GetNode<MultiplayerSynchronizer>("MultiplayerSynchronizer");

        if (!Multiplayer.IsServer())
        {
            navAgent.QueueFree();
            GetNode<Timer>("PathfindToTarget").Stop();
            GetNode<Timer>("PathfindToTarget").QueueFree();
            GetNode<Timer>("MultiSyncCheck").Stop();
            GetNode<Timer>("MultiSyncCheck").QueueFree();
            GetNode<Timer>("AutoHeal").Stop();
            GetNode<Timer>("AutoHeal").QueueFree();
            GetNode<Area2D>("VisionCircle").QueueFree();

            visibilityChecker.ScreenEntered += NodeEnteredTheCamera;
            visibilityChecker.ScreenExited += NodeLeftTheCamera;
            HandleAnimationChange(0);
            return;
        }

        idleState = new Idle(this, GetNode<Timer>("StateDecisionTimer"));
        combatState = new Combat(this, GetNode<Timer>("PathfindToTarget"), GetNode<Timer>("AttackTimer"));

        team = 2;

        base._Ready();

        vision = GetNode<AiVision>("Vision");
        home = GetTree().Root.GetNode("World").GetNode<Node2D>("Home");

        vision.VisibleTargetChanged += OnTargetChanged;
        BrainActionChanged += HandleBrainActionChange;
        health.Died += HandleDeath;

        SetBrainAction = BrainAction.idle;
    }
    public override void Spawn(Vector2 _spawnPos, Node2D _home, CollisionShape2D _homeZone)
    {
        base.Spawn(_spawnPos, _home, _homeZone);
    }
    public override void SetName(string newName)
    {
        characterName = newName;
    }

    private void HandleBrainActionChange(BrainAction action)
    {
        if (brainAction == BrainAction.die)
            return;

        brainAction = action;

        HandleAnimationChange((int)action);
        Rpc("HandleAnimationChange", (int)action);
    }

    [Rpc(MultiplayerApi.RpcMode.AnyPeer)]
    private void HandleAnimationChange(int actionID)
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
                if (bodyAnimation.FlipH)
                    animationPlayer.Play("attackAnimL");
                else
                    animationPlayer.Play("attackAnimR");
                break;
            }
            case BrainAction.die: {
                movement.SetPhysicsProcess(false);
                //bodyAnimation.Play("Hit");
                animationPlayer.Stop();
                animationPlayer.Play("death");
                break;    
            }
            default: {
                break;
            }
        }
    }

    public void OnCharacterClick(Node viewPort, InputEvent inputEvent, int ShapeIndex)
    {
        //DEBUG CODE, FOR NOW
        if (inputEvent is InputEventMouseButton eventKey) 
            if (eventKey.Pressed && Input.IsKeyPressed(Key.Ctrl))
                base.OnCharacterClick(this);
    }

    public void TryAttack()
    {
        if (brainAction != BrainAction.idle)
            return;
        
        SetBrainAction = BrainAction.attacking;
    }
    
    public void TryApplyDamage()
    {
        if (!Multiplayer.IsServer())
            return;

        if (target == null || !IsInstanceValid(target))
            return;

        if (GlobalPosition.DistanceTo(target.GlobalPosition) < (combatState.minEngageDistance * 1.2))
			target.GetNode<Health>("Health").ApplyDamage(baseDamageOverride, this);
    }

    public void ResetAttack()
    {
        if (brainAction == BrainAction.attacking)
            SetBrainAction = BrainAction.idle;
    }

    [Rpc(MultiplayerApi.RpcMode.AnyPeer)]
    public void HandleSpriteFlip(bool state)
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
        RpcId(1, "RpcServerVisibleToClient");
        isVisibleToAnyClient = true;
    }

    private void NodeLeftTheCamera()
    {
        isVisibleToAnyClient = false;
    }

    private void HandleDeath(Node2D _killer)
    {
        SetBrainAction = BrainAction.die;
        killer = _killer;
    }

    private void StartDeathProcess()
    {
        if (!Multiplayer.IsServer())
            return;

        GiveLootToKiller();
        
        DeleteMonster();
        Rpc("DeleteMonster");
    }

    [Rpc(MultiplayerApi.RpcMode.AnyPeer)]
    private void DeleteMonster()
    {
        QueueFree();
    }

    private void GiveLootToKiller()
    {
        foreach (var item in GetNode<Inventory>("Inventory").ConvertedDropTable)
        {
            //Randomize loot count, one guaranteed
            int lootCount = GD.RandRange(1, item.Value);

            killer.GetNode<Inventory>("Inventory").AddLoot(new KeyValuePair<Item, int>(item.Key, lootCount));
        }
    }
}