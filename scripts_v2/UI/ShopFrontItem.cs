using System.Linq;
using Godot;
using System.Text.RegularExpressions;
using System;

public partial class ShopFrontItem : Control
{
    [Export] TextureRect sprite;
    [Export] RichTextLabel itemDisplayName;
    [Export] LineEdit Volume;
    [Export] LineEdit Price;
    [Export] CheckBox ignoreVolume;
    [Export] Button deleteButton;
    [Export] Button addVolume;
    [Export] Button subVolume;
    [Export] Button addPrice;
    [Export] Button subPrice;
    public string ItemName;
    public bool isPurchase;
    private int itemID;
    private ShopFront parent;
    int pos;

    public override void _Ready()
    {
        deleteButton.Pressed += HandleDeletion;
        Volume.TextChanged += s => {SimpleNumberInputFilter(s, Volume);};
        Price.TextChanged += s => {SimpleNumberInputFilter(s, Price);};

        Volume.FocusExited += delegate{HandleConfirm();};
        Volume.TextSubmitted += delegate{HandleConfirm();};
        Price.FocusExited += delegate{HandleConfirm();};
        Price.TextSubmitted += delegate{HandleConfirm();};

        addVolume.Pressed += delegate{VolumeButtonChange(true, false);};
        subVolume.Pressed += delegate{VolumeButtonChange(true, true);};
        addPrice.Pressed += delegate{VolumeButtonChange(false, false);};
        subPrice.Pressed += delegate{VolumeButtonChange(false, true);};

        sprite.MouseEntered += delegate{DisplayItemName(true);};
        sprite.MouseExited += delegate{DisplayItemName(false);};
    }

    public void Setup(Item item, ShopFront parent, bool _isPurchase)
    {
        this.parent = parent;
        ItemName = item.ItemName;
        itemID = item.ID;
        sprite.Texture = (Texture2D)item.Sprite;
        itemDisplayName.Text = "[center]" + item.ItemName;
        isPurchase = _isPurchase;
    }

    private void VolumeButtonChange(bool volumeChange, bool sub)
    {
        int value = volumeChange ? (Volume.Text == "") ? 0 : Volume.Text.ToInt() : (Price.Text == "") ? 0 : Price.Text.ToInt();
        int change = 1;
        if (Input.IsKeyPressed(Key.Shift))
            change = 10;
        if (Input.IsKeyPressed(Key.Ctrl))
            change = 100;

        if (!sub)
            value += change;
        else
            value -= change;
            
        if (volumeChange)
            Volume.Text = value.ToString();
        else
            Price.Text = value.ToString();

        HandleConfirm();
    }

    private void HandleDeletion()
    {
        parent.HandleItemDeletion(this, isPurchase, itemID);
        QueueFree();
    }

    private void HandleConfirm()
    {
        if (Volume.Text == "" || Price.Text == "")
            return;
        if (Volume.Text.ToInt() < 0)
            Volume.Text = "0";
        if (Price.Text.ToInt() < 0)
            Price.Text = "0";

        GD.Print(isPurchase);
        parent.HandleItemEdit(itemID, Volume.Text.ToInt(), Price.Text.ToInt(), isPurchase);
    }

    private void DisplayItemName(bool hover)
    {
        itemDisplayName.Visible = hover;
    }

    public void SimpleNumberInputFilter(string input, LineEdit lineEdit)
	{
        if (input.Any(char.IsLetter))
            pos = -1;
        else
            return;

		string result = Regex.Replace(input, "[^0123456789]", "");
        pos += lineEdit.CaretColumn;
        lineEdit.Text = result;
        lineEdit.CaretColumn = pos;
	}
}