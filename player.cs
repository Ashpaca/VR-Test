using Godot;
using System;

public partial class player : XROrigin3D
{
	float gravity = 9.8f;

	XRCamera3D CameraNode { get; set; }
	CharacterBody3D CharacterBody { get; set; }
	CollisionShape3D CharacterCollider { get; set; }
	Hand LeftHand { get; set; }
	Hand RightHand { get; set; }

	Vector2 MovementVector { get; set; }
	Vector3 PushedByHands { get; set; }

	public override void _Ready()
	{
		CameraNode = GetNode<XRCamera3D>("XRCamera3D");
		CharacterBody = GetNode<CharacterBody3D>("CharacterBody3D");
		CharacterCollider = CharacterBody.GetNode<CollisionShape3D>("CollisionShape3D");

		LeftHand = GetNode<Hand>("Left Hand");
		RightHand = GetNode<Hand>("Right Hand");

		MovementVector = Vector2.Zero;
		PushedByHands = Vector3.Zero;
	}
	
	public override void _PhysicsProcess(double delta)
	{
		//How high is the headset off of the ground?
		((CapsuleShape3D)CharacterCollider.Shape).Height = CameraNode.Position.Y + 0.2f;

		//Modify the height of the collider accordingly
		CharacterCollider.Position = new Vector3(CharacterCollider.Position.X, -CameraNode.Position.Y / 2 + 0.1f, CharacterCollider.Position.Z);

		//move the body to the head
		Vector3 headOffset = new Vector3(CameraNode.GlobalTransform.Basis.Z.Normalized().X, 0, CameraNode.GlobalTransform.Basis.Z.Normalized().Z).Normalized(); //could make this better when looking down
		Vector3 headDif =  CameraNode.GlobalPosition + headOffset * .22f - CharacterBody.GlobalPosition;
		CharacterBody.GlobalPosition += headDif;
		
		//Set the player's velocity based on movement and gravity
		CharacterBody.Velocity = PushedByHands.Y > .1
									? PushedByHands + new Vector3(MovementVector.X, 0, MovementVector.Y)
									: new Vector3(PushedByHands.X + MovementVector.X, CharacterBody.Velocity.Y, PushedByHands.Z + MovementVector.Y);
		
		if (!CharacterBody.IsOnFloor())
		{
			CharacterBody.Velocity -= new Vector3(0, gravity * (float)delta, 0);
		}

		//Kinematic body does its thing
		CharacterBody.MoveAndSlide();

		//Apply forces to all rigid bodies you hit
		for (int i = 0; i < CharacterBody.GetSlideCollisionCount(); i++)
		{
			KinematicCollision3D iCollision = CharacterBody.GetSlideCollision(i);
			if (iCollision.GetCollider() is RigidBody3D rigidB)
			{
				rigidB.ApplyCentralForce(-iCollision.GetNormal() * 4); //is 4 enough?
			}
		}

		//move the head to the body
		headDif = CharacterBody.GlobalPosition - CameraNode.GlobalPosition - headOffset * .22f;
		GlobalPosition += headDif;
		CharacterBody.GlobalPosition -= headDif;
	}
	
	public override void _Process(double delta)
	{	
		if (!HandsPushing())
		{
			//get the left joystick as movement
			MovementVector = ((Vector2)LeftHand.GetInput("primary")).Normalized();
			
			Vector3 forwardsV = -CameraNode.GlobalTransform.Basis.Z.Normalized()*MovementVector.Y;
			Vector3 sideV = CameraNode.GlobalTransform.Basis.X.Normalized()*MovementVector.X;
			MovementVector = new Vector2(forwardsV.X + sideV.X, forwardsV.Z + sideV.Z).Normalized();
		}

		LeftHand.setOtherHandLocation(RightHand.GlobalPosition);
		RightHand.setOtherHandLocation(LeftHand.GlobalPosition);
	}
	
	//Checks if the hands are grabbing anything, and if they are is the player moving themself via their hands
	//Should this always be happening though, or only when grabbing? not sure...
	public bool HandsPushing()
	{
		PushedByHands = LeftHand.HandPushSelfAmount();
		PushedByHands += RightHand.HandPushSelfAmount();
		return LeftHand.IsClimbing() || RightHand.IsClimbing();
	}
	
}
