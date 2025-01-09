using Godot;
using System;

public partial class DayNight : Node3D
{
	private DirectionalLight3D Sun;
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		Sun = GetNode<DirectionalLight3D>("DirectionalLight3D");
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
		// Rotate the Sun around its X-axis
		Vector3 currentRotation = Sun.Rotation;
		currentRotation.X -= (float)delta*0.015f; // Convert delta to float
		currentRotation.Y -= (float)delta*0.0075f; // Convert delta to float
		Sun.Rotation = currentRotation;   // Set the updated rotation
	}
}
