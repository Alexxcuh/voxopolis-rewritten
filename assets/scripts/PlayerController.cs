using Godot;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

public partial class PlayerController : CharacterBody3D
{
	public const float Speed = 3.5f;
	public float health = 100f;
	public float max_health = 100f;
	public const float JumpVelocity = 13.0f;
	public float death_line = -1000f;
	private Camera3D camera;
	private Node3D xBone;
	private Node3D yBone;
	private AnimationPlayer anims;
	private AnimationTree anim3;
	private AudioStreamPlayer wind;
	private Node3D playerModel;
	private Node3D PlrHead;
	private RichTextLabel chat;
	private LineEdit chatbox;
	public int windi = 0;
	public float min_zoom = 0.0f;
	public float max_zoom = 45.0f;
	public float zoom = 0.0f;
	public int shiftlock = 0;
	public int inUI = 0;
	private int jumps = 0;
	private string nc = "#000";
	private string Name = "AlexXD";
	// UI scenes
	private Panel chatScene;
	private ProgressBar Health_Bar;
	
	private float targetRotationY;
	private float rotationTime = 0.15f; // Time to complete the rotation
	private float rotationElapsed = 0f;

	public Color StringToColorFunction(string input)
	{
		if (input.Length < 3)
		{
			GD.Print("String must be at least 3 characters long.");
			return new Color(0, 0, 0); // Return black if the string is too short
		}

		// Ensure all letters are in uppercase
		input = input.ToUpper();

		// Get alphabet index (0-25)
		int GetAlphabetIndex(char c) => c - 'A';

		// Calculate R, G, B based on the string
		int len = input.Length;
		int firstIndex = GetAlphabetIndex(input[0]);
		int middleIndex = GetAlphabetIndex(input[len / 2]);
		int lastIndex = GetAlphabetIndex(input[len - 1]);

		// Calculate RGB values
		float r = (float)(firstIndex / 26.0) * 255.0f;
		float g = (float)(middleIndex / 26.0) * 255.0f;
		float b = (float)(lastIndex  / 26.0) * 255.0f;

		// Clamp the values to ensure they are between 0 and 255
		r = Mathf.Clamp(r-(len/3.14f), 0, 255);
		g = Mathf.Clamp(g+(len/3.14f), 0, 255);
		b = Mathf.Clamp(b, 0, 255);

		return new Color(r / 255.0f, g / 255.0f, b / 255.0f);
	}
	public string ColorToHex(Color color)
	{
		return $"{(int)(color.R * 255):X2}{(int)(color.G * 255):X2}{(int)(color.B * 255):X2}";
	}
	public static string CloseUnclosedBBTags(string message)
	{
		// Regex to match opening and valid closing BBCode tags
		Regex tagRegex = new Regex(@"\[(/?)(\w+)(=[^\]]+)?\]");
		
		// Stack to keep track of unclosed tags
		Stack<string> tagStack = new Stack<string>();

		// Match all BBCode tags in the message
		var matches = tagRegex.Matches(message);

		// Iterate over all matches
		foreach (Match match in matches)
		{
			bool isClosingTag = match.Groups[1].Value == "/"; // Check if it's a closing tag
			string tagName = match.Groups[2].Value; // Extract tag name

			if (isClosingTag) // If it's a closing tag
			{
				if (tagStack.Count > 0 && tagStack.Peek() == tagName)
				{
					tagStack.Pop(); // Properly close the tag by removing it from the stack
				}
				// Else: Ignore invalid closing tags (e.g., mismatched or malformed)
			}
			else // It's an opening tag
			{
				tagStack.Push(tagName);
			}
		}

		// Append unclosed tags to the message
		while (tagStack.Count > 0)
		{
			string unclosedTag = tagStack.Pop();
			message += $"[/{unclosedTag}]";
		}

		return message;
	}

	public override void _Input(InputEvent @event)
	{
		if (@event is InputEventMouseMotion mouseEvent)
		{	
			if (Input.IsActionPressed("RC") && inUI == 0 || zoom <= 0.5f && inUI == 0)
			{
				Input.MouseMode = Input.MouseModeEnum.Captured;
				Vector2 rotationAmount = mouseEvent.Relative * 2.5f;
				xBone.RotateY(Mathf.DegToRad(-rotationAmount.X/2.75f));
				yBone.RotateX(Mathf.DegToRad(-rotationAmount.Y/2.75f));
			} else
			{
				Input.MouseMode = Input.MouseModeEnum.Visible;
			}
		}
		
		//if (@event.IsActionPressed("crawl") && inUI == 0)
		//{
			//shiftlock = 1 - shiftlock; // Toggle shiftlock between 0 and 1
		//}
	}

	public override void _Ready()
	{
		health = max_health;
		nc = ColorToHex(StringToColorFunction(Name));
		camera = GetNode<Camera3D>("x/y/Camera3D");
		xBone = GetNode<Node3D>("x");
		yBone = GetNode<Node3D>("x/y");
		wind = GetNode<AudioStreamPlayer>("AudioStreamPlayer");
		Input.SetCustomMouseCursor((Texture2D)GD.Load("res://cursors/finger.png"), Input.CursorShape.PointingHand, new Vector2(5,0));
		playerModel = GetNode<Node3D>("Model");
		anims = GetNode<AnimationPlayer>("AnimationPlayer");
		anim3 = GetNode<AnimationTree>("AnimationTree");
		PlrHead = GetNode<Node3D>("Model/ROOT/H");
		chatScene = camera.GetNode<CanvasLayer>("CanvasLayer").GetNode<Button>("mewing").GetNode<Panel>("Chat");
		Health_Bar = camera.GetNode<CanvasLayer>("CanvasLayer").GetNode<Button>("mewing").GetNode<ProgressBar>("Health");
		chat = chatScene.GetNode<RichTextLabel>("Text");
		chatbox = chatScene.GetNode<LineEdit>("LineEdit");

		if (chatbox != null)
		{
			chatbox.Connect("text_submitted", new Callable(this, nameof(_on_line_edit_text_submitted)));
			chatbox.Connect("focus_entered", new Callable(this, nameof(_on_focus_entered)));
			chatbox.Connect("focus_exited", new Callable(this, nameof(_on_focus_exited)));
		}
	}
	
	public void _on_line_edit_text_submitted(string new_text)
	{
		if (new_text != "" && new_text != null && new_text != " ")
		{
			if (new_text == "/clear")
			{
				chat.Text = "";
				chatbox.Text = "";
				return;
			}
			string bla = CloseUnclosedBBTags(new_text);
			chat.Text += $"[color={nc}]{Name}: [color=fff]{bla}\n";
			chatbox.Text = "";
		} else {
			chatbox.ReleaseFocus();
		}
		
	}
	public void _on_focus_entered()
	{
		inUI += 1;
	}
	public void _on_focus_exited()
	{
		inUI -= 1;
	}
	
	public override void _PhysicsProcess(double delta)
	{

		if (Input.IsActionJustPressed("chat") && inUI == 0)
		{
			chatbox.GrabFocus();
		}
		//Node focusOwner = GetTree().GetFocused();
//
		//if (focusOwner != null)
		//{
			//// GUI is focused, ignore input
			//GD.Print("Focus is on GUI, ignoring input.");
		//}
		//else
		//{
			//// GUI is not focused, process player input
			//GD.Print("hello weed");
		//}
		
		Vector3 velocity = Velocity;
		if (Input.IsActionJustPressed("ZO") || Input.IsActionPressed("ZO1") && inUI == 0)
		{
			zoom += 0.5f;
			if (zoom >= max_zoom)
			{
				zoom = max_zoom;
			}
		}
		if (Input.IsActionJustPressed("ZI") || Input.IsActionPressed("ZI1") && inUI == 0)
		{
			zoom -= 0.5f;
			if (zoom <= min_zoom)
			{
				zoom = min_zoom;
			}
		}
		Vector3 sigma = camera.Position; // Create a copy of the Position vector
		sigma.Z = Mathf.Lerp(camera.Position.Z, zoom, 0.15f); // Modify the Z value
		camera.Position = sigma; // Reassign the modified position
		// Add the gravity.
		if (!IsOnFloor())
		{
			velocity += GetGravity() * (float)delta*3.0f;
			anims.Play("jump");
			//GD.Print(velocity.Y);
			if (velocity.Y <= -180f)
			{
				wind.VolumeDb = Mathf.Clamp(Mathf.Lerp(0f, -50f, Mathf.InverseLerp(-1000f, -180f, velocity.Y)), -50f, 0f);
				//GD.Print(wind.VolumeDb);
				if (windi == 0)
				{
					windi = 1;
					wind.Play();
					
				}
				RandomNumberGenerator rng = new RandomNumberGenerator();
				yBone.Position = new Vector3((float)rng.RandfRange(velocity.Y, -velocity.Y),(float)rng.RandfRange(velocity.Y, -velocity.Y),(float)rng.RandfRange(velocity.Y, -velocity.Y))/2000f;
			} else
			{
				windi = 0;
				wind.Stop();
				yBone.Position = new Vector3(0,0,0);
			}
		}

		if (IsOnFloor() && jumps == 1)
		{
			jumps = 0;
			//GD.Print(jumps);
		}

		// Handle Jump.
		if (Input.IsActionPressed("Jump") && inUI == 0)
		{
			if (IsOnFloor())
			{
				velocity.Y = JumpVelocity*1.5f;
				jumps = 1;
				//GD.Print(jumps);
			} else {
				if (jumps == 1 && Input.IsActionJustPressed("Jump") && inUI == 0)
				{
					velocity.Y = JumpVelocity*1.5f;
					jumps = 0;
					anims.Play("double_jump");
					//GD.Print(jumps);
				}
			}
		}
		// Get the input direction and handle the movement/deceleration.
		// As good practice, you should replace UI actions with custom gameplay actions.
		Vector2 inputDir = Input.GetVector("A", "D", "W", "S");
		Vector3 forwardDir = xBone.GlobalTransform.Basis.Z;
		forwardDir.Y = 0; // Ignore vertical component
		forwardDir = forwardDir.Normalized();
		
		Vector3 rightDir = xBone.GlobalTransform.Basis.X;
		rightDir.Y = 0; // Ignore vertical component
		rightDir = rightDir.Normalized();

		Vector3 direction = (forwardDir * inputDir.Y + rightDir * inputDir.X).Normalized();
		if (inUI != 0)
		{
			direction = Vector3.Zero;
			inputDir = Vector2.Zero;
		}
		velocity.X += direction.X * Speed;
		velocity.Z += direction.Z * Speed;
		velocity.X *= 0.75f;
		velocity.Z *= 0.75f;
		Velocity = velocity;

		if (shiftlock == 0 && inputDir != Vector2.Zero && inUI == 0)
		{
			targetRotationY = Mathf.Atan2(direction.X, direction.Z);
			rotationElapsed = 0f; // Reset rotation elapsed time
		}
		else if (zoom == 0.0f|| shiftlock == 1 && inUI == 0)
		{
			// Ensure player model faces the back direction of xBone
			targetRotationY = xBone.Rotation.Y + Mathf.Pi;
			rotationElapsed = 0f; // Reset rotation elapsed time
		}
		if (zoom == 0.0f)
		{
			playerModel.Visible = false;
		} else {
			playerModel.Visible = true;
		}
		if (inputDir != Vector2.Zero && anims.CurrentAnimation != "walk" && IsOnFloor())
		{
			anims.Play("walk");
		} else if (inputDir == Vector2.Zero && IsOnFloor())
		{
			anims.Play("idle");
		} else if (!IsOnFloor())
		{
			//anims.Play("jump");
		}
		
		// Smoothly interpolate the rotation
		if (rotationElapsed < rotationTime)
		{
			rotationElapsed += (float)delta;
			float t = Mathf.Clamp(rotationElapsed / rotationTime, 0, 1);
			float smoothRotation = Mathf.LerpAngle(playerModel.Rotation.Y, targetRotationY, t);
			playerModel.Rotation = new Vector3(playerModel.Rotation.X, smoothRotation, playerModel.Rotation.Z);
		}
		//PlrHead.LookAt(new Vector3(0,15,0)-camera.GlobalPosition*2f);
		//PlrHead.Rotation /= 2f;
		if ( Position.Y <= death_line )
		{
			health = 0f;
		}
		
		if (health <= 0f)
		{
			health = max_health;
			Position = new Vector3(0,5,0);
			Velocity *= 0;
			velocity = Velocity;
		}
		Health_Bar.Value = health;
		//GD.Print(inUI);
		MoveAndSlide();
	}
}
