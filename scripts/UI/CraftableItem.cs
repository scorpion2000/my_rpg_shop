using System;
using Godot;

[Obsolete]
public partial class CraftableItem : Button
{
    int craftableID;
    public event Action<int> CraftableItemSelection;

    public void ButtonSetup(Item item)
    {
        Text = item.ItemName;
        Icon = (Texture2D)item.Sprite;
        craftableID = item.ID;
    }

    public override void _Pressed()
    {
        CraftableItemSelection?.Invoke(craftableID);
    }
}