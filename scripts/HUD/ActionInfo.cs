using Godot;
using System;

public partial class ActionInfo : Panel
{
    [Export] private RichTextLabel nameLabel;
    [Export] private RichTextLabel descriptionLabel;

    public void SetContent(string name, string description)
    {
        nameLabel.Text = name;
        descriptionLabel.Text = description;
    }
}
