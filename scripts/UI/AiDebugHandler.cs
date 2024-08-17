using Godot;
using System;
using System.Collections.Generic;

[Obsolete]
public partial class AiDebugHandler : GridContainer
{
	Label target;
	Label characterName;
	Label state;
	Label health;
	Label brainAction;
	Label lair;
	Label inventory;
	Label gold;

	Brain activeBrain = null;
	JipSyncronyzer jipSyncronyzer;
	public override void _Ready()
	{
		target = GetNode<Label>("Target");
		characterName = GetNode<Label>("CharacterName");
		state = GetNode<Label>("State");
		health = GetNode<Label>("Health");
		brainAction = GetNode<Label>("BrainAction");
		lair = GetNode<Label>("Lair");
		inventory = GetNode<Label>("Inventory");
		gold = GetNode<Label>("Gold");
		jipSyncronyzer = GetTree().Root.GetNode("World").GetNode<JipSyncronyzer>("JipSyncHandler");

		Visible = false;
		
		jipSyncronyzer.MobSpawned += RegisterHero;
	}

	public void OnBrainSelection(Brain brain)
	{
		//Unsub from old
		if (activeBrain != null && IsInstanceValid(activeBrain))
		{
			if (activeBrain.GetNode<Health>("Health") is Health oldHealth)
			{
				oldHealth.Healed -= HandleHealthChange;
				oldHealth.Damaged -= HandleHealthChange;
			}
			activeBrain.StateChanged -= HandleStateChange;
			activeBrain.TargetChanged -= HandleTargetChange;
			activeBrain.BrainActionChanged -= HandleBrainActionChange;
			if (activeBrain.GetNode<Inventory>("Inventory") is Inventory oldMobInventory)
				oldMobInventory.InventoryUpdated -= HandleInventoryChange;
		}

		activeBrain = brain;

		if (activeBrain == null)
		{
			Visible = false;
			return;
		}
		//Sub to new
		if (activeBrain.GetNode<Health>("Health") is Health newHealth)
		{
			newHealth.Healed += HandleHealthChange;
			newHealth.Damaged += HandleHealthChange;
		}
		activeBrain.StateChanged += HandleStateChange;
		activeBrain.TargetChanged += HandleTargetChange;
		activeBrain.BrainActionChanged += HandleBrainActionChange;
		if (activeBrain.GetNode<Inventory>("Inventory") is Inventory mobInventory)
			mobInventory.InventoryUpdated += HandleInventoryChange;

		Visible = true;

		GrabBrainInfo();
	}

	private void GrabBrainInfo()
	{
		characterName.Text = "Name: " +  activeBrain.Name + " |";
		HandleHealthChange();
		HandleStateChange();
		HandleTargetChange(activeBrain.target);
		HandleBrainActionChange(activeBrain.brainAction);
		HandleInventoryChange(activeBrain.GetNode<Inventory>("Inventory").MobInventory, activeBrain.GetNode<Inventory>("Inventory").Gold);
	}

	private void HandleHealthChange()
	{
		health.Text = "Health: " + activeBrain.GetNode<Health>("Health").GetHealth.ToString() + " |";
	}

	private void HandleStateChange()
	{
		if (activeBrain.currentState != null)
			state.Text = "State: " + activeBrain.currentState.stateName + " |";
		else 
			state.Text = "State: " + " |";

		GetLairInfo();
	}

	private void HandleTargetChange(Node2D newTarget)
	{
		if (activeBrain.target != null)
			target.Text = "Target: " + newTarget.Name + " |";
		else
			target.Text = "Target: " + " |";
	}

	private void HandleBrainActionChange(Brain.BrainAction action)
	{
		brainAction.Text = "Action: " + action + " |";
	}

	private void HandleInventoryChange(Dictionary<Item, int> mobInventory, int _gold)
	{
		string inventoryText = "Inventory: ";
		foreach (var item in mobInventory)
		{
			inventoryText = inventoryText + item.Key.Name + ":" + item.Value.ToString() + " ; ";
		}
		inventory.Text = inventoryText + " |";

		gold.Text = "Gold: " + _gold.ToString() + " |";
	}

	private void GetLairInfo()
	{
		/*if (IsInstanceValid(activeBrain.targetLair))
			lair.Text = "Lair: " + activeBrain.targetLair.Name + " |";
		else
			lair.Text = "Lair: |";*/
	}

	public void OnDebugClose()
	{
		OnBrainSelection(null);
		Visible = false;
	}

	private void RegisterHero(Brain brain)
	{
		brain.DebugClick += OnBrainSelection;
	}
}
