using Godot;
using System;

public partial class StateMachine : CharacterBody2D
{
	public BaseState currentState;
	public event Action StateChanged;

	public override void _Ready()
	{
		if (currentState != null)
            currentState.Enter();
	}

	public override void _Process(double delta)
	{
		if (currentState != null)
            currentState.UpdateLogic();
	}

	public void ChangeState(BaseState newState)
	{
		if (newState == null)
		{
			GD.PrintErr("New AI state is null");
			return;
		}
		if (currentState != null)
            currentState.Exit();

        currentState = newState;
        currentState.Enter();
		StateChanged?.Invoke();
	}

	protected virtual BaseState GetInitialState()
	{
		return null;
	}
}
