using Godot;
using System;
using System.Runtime.Serialization;

public partial class player : XROrigin3D
{
	float gravity = 9.8f;

	XRCamera3D CameraNode { get; set; }
	CharacterBody3D CharacterBody { get; set; }
	CollisionShape3D CharacterCollider { get; set; }
	Hand LeftHand { get; set; }
	Hand RightHand { get; set; }
	bool IsClimbing = false;

	Vector2 MovementVector { get; set; }
	Vector3 PushedByHands { get; set; }
	Vector2 ThumbstickRotation { get; set; }
	bool AlreadyRotated = false;

	Area3D RightShoulderGrabZone { get; set; }

	int[] numberOfBows = new int[1]{1};
	int[] numberOfArrows = new int[1]{10};

	public override void _Ready()
	{
		CameraNode = GetNode<XRCamera3D>("XRCamera3D");
		CharacterBody = GetNode<CharacterBody3D>("CharacterBody3D");
		CharacterCollider = CharacterBody.GetNode<CollisionShape3D>("CollisionShape3D");
		RightShoulderGrabZone = CameraNode.GetNode<Area3D>("Right Shoulder Grab");

		LeftHand = GetNode<Hand>("Left Hand");
		RightHand = GetNode<Hand>("Right Hand");
		LeftHand.SecondHandReference(RightHand);
		RightHand.SecondHandReference(LeftHand);

		MovementVector = Vector2.Zero;
		PushedByHands = Vector3.Zero;
		ThumbstickRotation = Vector2.Zero;

		LeftHand.SetUpBowAndArrowCounts(numberOfBows, numberOfArrows);
		RightHand.SetUpBowAndArrowCounts(numberOfBows, numberOfArrows);
	}
	
	public override void _PhysicsProcess(double delta)
	{
		//How high is the headset off of the ground?
		((CapsuleShape3D)CharacterCollider.Shape).Height = MathF.Max(CameraNode.Position.Y, .1f) + 0.2f;

		//Modify the height of the collider accordingly
		CharacterCollider.Position = new Vector3(CharacterCollider.Position.X, -CameraNode.Position.Y / 2 + 0.1f, CharacterCollider.Position.Z);

		//move the body to the head
		Vector3 headOffset = new Vector3(CameraNode.GlobalTransform.Basis.Z.Normalized().X, 0, CameraNode.GlobalTransform.Basis.Z.Normalized().Z).Normalized(); //could make this better when looking down
		Vector3 headDif =  CameraNode.GlobalPosition + headOffset * .22f - CharacterBody.GlobalPosition;
		CharacterBody.GlobalPosition += headDif;
		
		float MysteryNum = -9.8f;

		IsClimbing = HandsPushing();
		//Set the player's velocity based on movement and gravity
		if (PushedByHands.Y > 0.1f)
		{
			CharacterBody.Velocity = PushedByHands + new Vector3(MovementVector.X, MysteryNum, MovementVector.Y);
		}
		else
		{
			CharacterBody.Velocity = new Vector3(PushedByHands.X + MovementVector.X, CharacterBody.Velocity.Y, PushedByHands.Z + MovementVector.Y);

			if (!CharacterBody.IsOnFloor())
			{
				CharacterBody.Velocity -= new Vector3(0, gravity * (float)delta, 0);
				if  (CharacterBody.Velocity.Y < -30)
				{
					CharacterBody.Velocity = new Vector3(CharacterBody.Velocity.X, -30, CharacterBody.Velocity.Z);
				}
			}
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

		if (!AlreadyRotated && ThumbstickRotation.X != 0)
		{
			
			AlreadyRotated = true;
			//this is not good. Two handed interactions don't work correctly and it rotates around the playspace
			GlobalRotation += Vector3.Up * ThumbstickRotation.X * Mathf.Pi / 2;
		}
	}
	
	public override void _Process(double delta)
	{	
		if (!IsClimbing)
		{
			//get the left joystick as movement
			MovementVector = ((Vector2)LeftHand.GetInput("primary")).Normalized();
			
			Vector3 forwardsV = -CameraNode.GlobalTransform.Basis.Z.Normalized()*MovementVector.Y;
			Vector3 sideV = CameraNode.GlobalTransform.Basis.X.Normalized()*MovementVector.X;
			MovementVector = new Vector2(forwardsV.X + sideV.X, forwardsV.Z + sideV.Z).Normalized();

			//handle Joystick rotation
			ThumbstickRotation = (Vector2)RightHand.GetInput("primary");
			if (Mathf.Abs(ThumbstickRotation.X) > 0.8)
			{
				ThumbstickRotation = new Vector2(Mathf.Sign(ThumbstickRotation.X), ThumbstickRotation.Y);
			}
			else
			{
				ThumbstickRotation = new Vector2(0, ThumbstickRotation.Y);
				AlreadyRotated = false;
			}
		}
	}
	
	//need to add a different amount of force depending on if climbing or picking up
	private bool HandsPushing()
	{
		Vector3 lHandAmount = LeftHand.HandPushSelfAmount();
		if (lHandAmount.LengthSquared() > 0.0144f && lHandAmount.LengthSquared() < 0.001f)
		{
			lHandAmount = lHandAmount.Normalized() * 12;
		}
		else
		{
			lHandAmount *= 100;
		}
		if(!LeftHand.IsClimbing())
		{
			lHandAmount /= 1.5f;
			if (LeftHand.Grabee == null)
			{
				lHandAmount = Vector3.Up * lHandAmount.Y;
			}
		}

		Vector3 rHandAmount = RightHand.HandPushSelfAmount();
		if (rHandAmount.LengthSquared() > 0.0144f && rHandAmount.LengthSquared() < 0.001f)
		{
			rHandAmount = rHandAmount.Normalized() * 12;
		}
		else
		{
			rHandAmount *= 100;
		}
		if(!RightHand.IsClimbing())
		{
			rHandAmount /= 1.5f; //something like this to reduce the bounce when lifting with hands
			if (RightHand.Grabee == null)
			{
				rHandAmount = Vector3.Up * rHandAmount.Y;
			}
		}

		PushedByHands = rHandAmount + lHandAmount;
		if (PushedByHands.LengthSquared() > 256)
		{
			PushedByHands = PushedByHands.Normalized() * 16;
		}
		
		return LeftHand.IsClimbing() || RightHand.IsClimbing();
	}
	
}
