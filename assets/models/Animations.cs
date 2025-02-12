using Godot;
using System;

public partial class Animations : Node3D
{
	private AnimationPlayer anims;
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		anims = GetNode<AnimationPlayer>("AnimationPlayer");
		anims.Play("Kazotsky");
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
	}
}
