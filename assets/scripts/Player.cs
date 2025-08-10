using Godot;
using System.Collections.Generic;
using System.Text.RegularExpressions;

public partial class Player : CharacterBody3D
{
    [Export] private PackedScene gui;
    public const float Speed = 3.5f;
	[Export]
	public float health = 100f;
	public float max_health = 100f;
	public const float JumpVelocity = 10.0f;
	public float death_line = -1000f;
	private Camera3D camera;
	private Node3D xBone;
	private Node3D yBone;
	private AnimationPlayer anims;
	private AudioStreamPlayer wind;
	private AudioStreamPlayer3D Emote;
	private Node3D playerModel;
	private Node3D PlrHead;
	private RichTextLabel chat;
	private LineEdit chatbox;
	private EmoteClass Emote1;
	private EmoteClass Emote2;
	public int windi = 0;
	private int emote = 0;
	public float min_zoom = 0.0f;
	public float max_zoom = 45.0f;
	public float zoom = 0.0f;
	public int shiftlock = 0;
	public int inUI = 0;
	private int jumps = 0;
	private string nc = "#000";
	// private string Name = "Voxopolis";
	// UI scenes
	private Chat chatScene;
	private Emote emoteScene;
	private ProgressBar Health_Bar;

	private float targetRotationY;
	private float rotationTime = 0.15f; // Time to complete the rotation
	private float rotationElapsed = 0f;
	// Multiplayer
	private Vector3 syncPos = new Vector3(0,0,0);
	private Vector3 syncRot = new Vector3(0,0,0);
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
		int rl = GetAlphabetIndex(input[0]);
		int bl = GetAlphabetIndex(input[len / 2]);
		int gl = GetAlphabetIndex(input[len - 1]);
		byte r = (byte)(len * (23.5+(rl*13)) % 256);
		byte g = (byte)(len * (23.5+(gl*13)) % 256);
		byte b = (byte)(len * (23.5+(bl*13)) % 256);

		return new Color(r / 255.0f, g / 255.0f, b / 255.0f);
	}
	public string ColorToHex(Color color)
	{
		return $"{(int)(color.R * 255):X2}{(int)(color.G * 255):X2}{(int)(color.B * 255):X2}";
	}
	public override void _Input(InputEvent @event)
	{
		if(GetNode<MultiplayerSynchronizer>("MultiplayerSynchronizer").GetMultiplayerAuthority() == Multiplayer.GetUniqueId()){
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
	}

	public override void _Ready()
	{
		GetNode<MultiplayerSynchronizer>("MultiplayerSynchronizer").SetMultiplayerAuthority(int.Parse(Name));
		health = max_health;
		nc = ColorToHex(StringToColorFunction(Name));
		camera = GetNode<Camera3D>("x/y/Camera3D");
		xBone = GetNode<Node3D>("x");
		yBone = GetNode<Node3D>("x/y");
		wind = GetNode<AudioStreamPlayer>("AudioStreamPlayer");
		Emote = GetNode<AudioStreamPlayer3D>("Model/Middle/ROOT/T/EmoteSFX");
		Input.SetCustomMouseCursor((Texture2D)GD.Load("res://cursors/finger.png"), Input.CursorShape.PointingHand, new Vector2(5,0));
		Input.SetCustomMouseCursor((Texture2D)GD.Load("res://cursors/finger.png"), Input.CursorShape.Ibeam, new Vector2(5,0));
		playerModel = GetNode<Node3D>("Model");
		anims = GetNode<AnimationPlayer>("AnimationPlayer");
		PlrHead = GetNode<Node3D>("Model/Middle/ROOT/H");
        //camera.AddChild(gui.Instantiate());
        chatScene = camera.GetNode<GUI>("CanvasLayer").Chat;
        emoteScene = camera.GetNode<GUI>("CanvasLayer").Emote;
        Health_Bar = camera.GetNode<GUI>("CanvasLayer").Health;
        chat = chatScene.chat;
        chatbox = chatScene.chatbox;
        Emote1 = emoteScene.Emote1;
        Emote2 = emoteScene.Emote2;
        if (chatbox != null)
		{
			chatbox.Connect("text_submitted", new Callable(this, nameof(_on_line_edit_text_submitted)));
			chatbox.Connect("focus_entered", new Callable(this, nameof(_on_focus_entered)));
			chatbox.Connect("focus_exited", new Callable(this, nameof(_on_focus_exited)));
		}
		if (Emote1 != null)
		{
			Emote1.Pressed += () => StartEmoting(Emote1);
			Emote2.Pressed += () => StartEmoting(Emote2);
		}
	}
	private void StartEmoting(EmoteClass EmoteButton)
	{
		Emote.Stop();
		anims.Stop();
		emoteScene.Visible = false;
		emote = 1;
		anims.Play(EmoteButton.Name);
		if (EmoteButton.Music)
		{
			Emote.Stream = ResourceLoader.Load<AudioStream>($"res://assets/sfx/dances/{EmoteButton.Name}.mp3");
			Emote.Play();
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
			RpcId(1,"SendMessage",new_text,Name,nc);
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

		if(GetNode<MultiplayerSynchronizer>("MultiplayerSynchronizer").GetMultiplayerAuthority() == Multiplayer.GetUniqueId()){

			if (Input.IsActionJustPressed("chat") && inUI == 0)
			{
				chatbox.GrabFocus();
			}
			//Node focusOwner = GetTree().GetFocused();
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
                zoom += 0.5f + (Input.IsActionJustPressed("ZO") ? 1f : 0f);
                if (zoom >= max_zoom)
				{
					zoom = max_zoom;
				}
			}
			if (Input.IsActionJustPressed("ZI") || Input.IsActionPressed("ZI1") && inUI == 0)
			{
				zoom -= 0.5f + (Input.IsActionJustPressed("ZI") ? 1f : 0f);
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
				emote = 0;
				Emote.Stop();
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
						emote = 0;
						Emote.Stop();
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
			if (emote == 0){
				velocity.X += direction.X * Speed;
				velocity.Z += direction.Z * Speed;
			} else {
				velocity.X += direction.X * (Speed / 2.2f);
				velocity.Z += direction.Z * (Speed / 2.2f);
			}
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
			if (inputDir != Vector2.Zero && anims.CurrentAnimation != "walk" && IsOnFloor() && emote == 0)
			{
				anims.Play("walk");
			} else if (inputDir == Vector2.Zero && IsOnFloor() && emote == 0)
			{
				anims.Play("idle");
			}
			
			// Smoothly interpolate the rotation
			if (rotationElapsed < rotationTime)
			{
				rotationElapsed += (float)delta;
				float t;
				if (emote == 0) {
					t = Mathf.Clamp(rotationElapsed / rotationTime, 0, 1);
				} else {
					t = Mathf.Clamp(rotationElapsed / 0.5f, 0, 1);
				}
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
			if (Input.IsActionJustPressed("Emote") && inUI == 0)
			{
				emoteScene.Visible = true;
				Emote1.Animation.Play();
				Emote2.Animation.Play();
			}
			if (Input.IsActionJustReleased("Emote") && inUI == 0)
			{
				emoteScene.Visible = false;
				Emote1.Animation.Stop();
				Emote2.Animation.Stop();
			}
			MoveAndSlide();
			syncPos = GlobalPosition;
			syncRot = GetNode<Node3D>("Model").GlobalRotation;
			camera.Current = true;
		} else{
			GlobalPosition = GlobalPosition.Lerp(syncPos, .1f);
			GetNode<Node3D>("Model").GlobalRotation = GetNode<Node3D>("Model").GlobalRotation.Lerp(syncRot, .1f);
		}
	}
	public static string CloseUnclosedBBTags(string message)
	{
		Regex tagRegex = new Regex(@"\[(/?)(\w+)(=[^\]]+)?\]");
		Stack<string> tagStack = new Stack<string>();
		var matches = tagRegex.Matches(message);
		foreach (Match match in matches)
		{
			bool isClosingTag = match.Groups[1].Value == "/";
			string tagName = match.Groups[2].Value;

			if (isClosingTag)
			{
				if (tagStack.Count > 0 && tagStack.Peek() == tagName)
				{
					tagStack.Pop();
				}
			}
			else
			{
				tagStack.Push(tagName);
			}
		}
		while (tagStack.Count > 0)
		{
			string unclosedTag = tagStack.Pop();
			message += $"[/{unclosedTag}]";
		}

		return message;
	}

    [Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = true)] 
    public void SendMessage(string message, string senderName, string color)
    {
        if (Multiplayer.IsServer())
        {
            GD.Print($"[SERVER] {senderName} says: {message}");
            Rpc(nameof(ReceiveMessage), $"[color={color}]{senderName}: [color=fff]{CloseUnclosedBBTags(message)}\n");
        }
    }
 
	[Rpc]
    public void ReceiveMessage(string message)
    {
		chat.Text += message;
    }
	public void SetUpPlayer(string name){
		GetNode<Label3D>("Label").Text = name;
	}

}
