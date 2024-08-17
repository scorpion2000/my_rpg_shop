using System;
using Godot;

public partial class Brain : StateMachine
{
    public enum BrainAction
    {
        idle = 0,
        moving = 1,
        attacking = 2,
        die = 3,
        enterShop = 4
    }
    public BrainAction brainAction;
    public Idle idleState;
    public Combat combatState;
    public Fleeing fleeingState;
    public Adventure adventureState;
    public Shopping shoppingState;
    public Health health;
    public Node2D target;
    public Node2D targetedBy;
    public Node2D home;
    public CollisionShape2D homeZone;
    public MultiplayerSynchronizer multiSync;
    public Movement movement;
    public int creationID;
    public string characterName;
    public int team;
    public int entityID;
    public bool listenToIdleCallback = false;
    public event Action<Brain> DebugClick;
    public event Action<Node2D> TargetChanged;
    public event Action<BrainAction> BrainActionChanged;
    public BrainAction SetBrainAction { set { BrainActionChanged?.Invoke(value);} }

    public override void _Ready()
    {
        base._Ready();
        ChangeState(idleState);
    }
    public virtual void EntitySetup(Mob entity, int _entityID)
    {
        entityID = _entityID;
        characterName = entity.MobName;   //Temporary
        GetNode<AiVision>("Vision").looseDistance = entity.LooseDistance;
        GetNode<Health>("Health").maxHealth = entity.Health;
        GetNode<Movement>("Movement").SetMaxSpeed = entity.MovementSpeed;
        AnimatedSprite2D anim = GetNode<AnimatedSprite2D>("AnimatedSprite2D");

        anim.SpriteFrames.ClearAll();
        anim.SpriteFrames.AddAnimation("Idle");
        anim.SpriteFrames.SetAnimationSpeed("Idle",11f);
        for (int i = 0; i < 32; i++)
        {
            string name = $"res://{entity.IdleAnimFolder}/{i}.png";
            if (Godot.FileAccess.FileExists(name))
                anim.SpriteFrames.AddFrame("Idle", GD.Load<Texture2D>(name));
            else
                break;
        }

        anim.SpriteFrames.AddAnimation("Run");
        anim.SpriteFrames.SetAnimationSpeed("Run",11f);
        for (int i = 0; i < 32; i++)
        {
            string name = $"res://{entity.RunAnimFolder}/{i}.png";
            if (Godot.FileAccess.FileExists(name))
                anim.SpriteFrames.AddFrame("Run", GD.Load<Texture2D>(name));
            else
                break;
        }

        anim.Play("Idle");
    }
    public virtual void Spawn(Vector2 _spawnPos, Node2D _home, CollisionShape2D _homeZone)
    {
        GlobalPosition = _spawnPos;

        home = _home;
        homeZone = _homeZone;

        movement.syncPosition = _spawnPos;
    }
    public virtual void SetName(string newName) {}
    public virtual void OnCharacterClick(Brain brain) { DebugClick?.Invoke(brain); }
    public virtual void OnTargetChanged(Node2D newTarget)
    {
        if (target != null || IsInstanceValid(target))
            return;

        target = newTarget;
        TargetChanged?.Invoke(newTarget);
    }
    [Rpc(MultiplayerApi.RpcMode.AnyPeer)]
    public virtual void ToggleVisibility()
    {
        if (Visible)
            Visible = false;
        else
            Visible = true;

        if (Multiplayer.IsServer())
            Rpc("ToggleVisibility");
    }
    public virtual void Wander()
    {
        float minX = -(homeZone.Shape.GetRect().Size.X/2), maxX = (homeZone.Shape.GetRect().Size.X/2);
        float minY = -(homeZone.Shape.GetRect().Size.Y/2), maxY = (homeZone.Shape.GetRect().Size.Y/2);
        float x = (float)GD.RandRange(minX, maxX);
        float y = (float)GD.RandRange(minY, maxY);
        Vector2 wanderPos = new Vector2(homeZone.GlobalPosition.X + x, homeZone.GlobalPosition.Y + y);

        movement.MoveTo(wanderPos);
    }
}