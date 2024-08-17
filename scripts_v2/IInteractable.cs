using System;
using Godot;

interface IInteractable
{
    public bool GetCanPlayerInteract { get; }
    public void ToggleInteractionAnim(bool toggle) {}
    public void Interact() {}
    public void InteractUI(PlayerInfo playerInfo, Node2D interactionNode = null)
    {

    }
}