using Godot;
using System;

public partial class UnitInfo : Panel
{
	[Export] private TextureRect picture;
    [Export] private RichTextLabel nameLabel;
    [Export] private RichTextLabel descriptionLabel;

    public void SetContent(string name, string description, Texture2D texture)
    {
        nameLabel.Text = name;
        descriptionLabel.Text = description;
        picture.Texture = texture;
    }
}
