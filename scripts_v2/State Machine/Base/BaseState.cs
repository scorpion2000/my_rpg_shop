using Godot;
using System;

public partial class BaseState : StateMachine
{
	public string stateName;
	protected StateMachine stateMachine;

	public BaseState(string name, StateMachine stateMachine)
	{
		this.stateName = name;
		this.stateMachine = stateMachine;
	}

	public virtual void Enter() {}
	public virtual void UpdateLogic() {}
	public virtual void Exit() {}
}
