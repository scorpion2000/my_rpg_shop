using Godot;
using System;

public partial class Health : Node
{
	[Export] public int maxHealth = 10;
	private int health = 1;
	[Export] Inventory inventory;

	public event Action Damaged;
	public event Action Healed;
	public event Action<Node2D> Died;

	public int GetHealth { get { return health; } }

    public override void _Ready()
    {
		health = maxHealth;
    }

    public void ApplyDamage(int value, Node2D damageDealer)
	{
		health = Mathf.Max(health - (value - inventory.EquippedArmor.GetDamageProtection), 0);
		Rpc("RpcClientHealthSync", health);
		Damaged?.Invoke();

		if (health <= 0)
			Died?.Invoke(damageDealer);
		
		inventory.EquippedArmor.DegradeDurability(2, 1);
	}

	public void ApplyHealing(int value)
	{
		health = Mathf.Min(health + value, maxHealth);
		Rpc("RpcClientHealthSync", health);
		Healed?.Invoke();
	}

	[Rpc(MultiplayerApi.RpcMode.AnyPeer)]
	private void RpcClientHealthSync(int syncValue)
	{
		//Cheeky check. Just in case a client somehow Rpcs, the server will ignore
		//This might lead to the health being out of sync (although no client should Rpc this)
		//But at least the server will always stay true
		if (Multiplayer.IsServer())
			return;
		
		health = syncValue;
		Healed?.Invoke();
	}
	
	public void OnAutoHealCountdown()
    {
        ApplyHealing((int)MathF.Round(maxHealth / 10));
    }
}
