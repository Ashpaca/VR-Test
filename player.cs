using Godot;
using System;

public partial class player : XROrigin3D
{
	float gravity = 9.8f;

	XRCamera3D cameraNode;
	CharacterBody3D characterBody;
	CollisionShape3D characterCollider;
	Hand leftHand;
	Hand rightHand;

	Vector2 movementVector;
	Vector3 PushedByHands { get; set; }

	public override void _Ready()
	{
		cameraNode = GetNode<XRCamera3D>("XRCamera3D");
		characterBody = GetNode<CharacterBody3D>("CharacterBody3D");
		characterCollider = characterBody.GetNode<CollisionShape3D>("CollisionShape3D");

		leftHand = GetNode<Hand>("Left Hand");
		rightHand = GetNode<Hand>("Right Hand");

		movementVector = Vector2.Zero;
		PushedByHands = Vector3.Zero;
	}
	
	public override void _PhysicsProcess(double delta)
	{
		//How high is the headset off of the ground?
		((CapsuleShape3D)characterCollider.Shape).Height = cameraNode.Position.Y + 0.2f;

		//Modify the height of the collider accordingly
		characterCollider.Position = new Vector3(characterCollider.Position.X, -cameraNode.Position.Y / 2 + 0.1f, characterCollider.Position.Z);

		//move the body to the head
		Vector3 headOffset = new Vector3(cameraNode.GlobalTransform.Basis.Z.Normalized().X, 0, cameraNode.GlobalTransform.Basis.Z.Normalized().Z).Normalized(); //could make this better when looking down
		Vector3 headDif =  cameraNode.GlobalPosition + headOffset * .22f - characterBody.GlobalPosition;
		characterBody.GlobalPosition += headDif;
		
		//Set the player's velocity based on movement and gravity
		characterBody.Velocity = PushedByHands.Y > .1
									? PushedByHands + new Vector3(movementVector.X, 0, movementVector.Y)
									: new Vector3(PushedByHands.X + movementVector.X, characterBody.Velocity.Y, PushedByHands.Z + movementVector.Y);
		
		if (!characterBody.IsOnFloor())
		{
			characterBody.Velocity -= new Vector3(0, gravity * (float)delta, 0);
		}

		//Kinematic body does its thing
		characterBody.MoveAndSlide();

		//Apply forces to all rigid bodies you hit
		for (int i = 0; i < characterBody.GetSlideCollisionCount(); i++)
		{
			KinematicCollision3D iCollision = characterBody.GetSlideCollision(i);
			if (iCollision.GetCollider() is RigidBody3D rigidB)
			{
				rigidB.ApplyCentralForce(-iCollision.GetNormal() * 4); //is 4 enough?
			}
		}

		//move the head to the body
		headDif = characterBody.GlobalPosition - cameraNode.GlobalPosition - headOffset * .22f;
		GlobalPosition += headDif;
		characterBody.GlobalPosition -= headDif;
	}
	
	public override void _Process(double delta)
	{	
		if (!HandsPushing())
		{
			//get the left joystick as movement
			movementVector = ((Vector2)leftHand.GetInput("primary")).Normalized();
			
			Vector3 forwardsV = -cameraNode.GlobalTransform.Basis.Z.Normalized()*movementVector.Y;
			Vector3 sideV = cameraNode.GlobalTransform.Basis.X.Normalized()*movementVector.X;
			movementVector = new Vector2(forwardsV.X + sideV.X, forwardsV.Z + sideV.Z).Normalized();
		}

		leftHand.setOtherHandLocation(rightHand.GlobalPosition);
		rightHand.setOtherHandLocation(leftHand.GlobalPosition);
	}
	
	//Checks if the hands are grabbing anything, and if they are is the player moving themself via their hands
	//Should this always be happening though, or only when grabbing? not sure...
	public bool HandsPushing()
	{
		PushedByHands = leftHand.HandPushSelfAmount();
		PushedByHands += rightHand.HandPushSelfAmount();
		return leftHand.IsClimbing() || rightHand.IsClimbing();
	}
	
}
