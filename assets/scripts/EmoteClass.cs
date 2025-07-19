using Godot;
using System;

public partial class EmoteClass : Button
{
	[Export]
	public string Name = "Example";
	[Export]
	public bool Music = false;
    [Export] public AnimatedSprite2D Animation;
}
