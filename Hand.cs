using Godot;
using System;
using System.Diagnostics;

public partial class Hand : XRController3D
{
	GameHand gameHand;
	Area3D realHand;
	Godot.Collections.Array<Rid> hands;
	Transform3D handGrabbedPos;
	Vector3 otherHandPosition;

	public bool isGrabbing;
	Node3D grabee;
	[Export] 
	private string throwNodeName = "";
	[Export] 
	private string gameHandName = "";
	Node3D throwNode;
	Vector3 throwDist;
	Vector3 throwTorque;

	Vector3 distanceTo;
	int numOfCollisions;

	public override void _Ready()
	{
		gameHand = GetParent().GetNode<GameHand>(gameHandName);
		realHand = GetNode<Area3D>("Area3D");

		hands = new Godot.Collections.Array<Rid>();
		hands.Add(gameHand.GetRid());
		hands.Add(realHand.GetRid());

		handGrabbedPos = gameHand.GlobalTransform;
		otherHandPosition = gameHand.GlobalPosition;

		grabee = null;
		isGrabbing = false;
		throwNode = GetParent().GetNode<Node3D>(throwNodeName);
		throwDist = Vector3.Zero;
		throwTorque = Vector3.Zero;

		distanceTo = Vector3.Zero;
		numOfCollisions = 0;
	}

	public override void _PhysicsProcess(double delta)
	{
		numOfCollisions = MoveGameHands();
		CalculateHandVelocityAndTorque(delta);
		ApplyForces();

		PickupsAndClimbing();
	}

	public override void _Process(double delta)
	{
		if ((float)GetInput("grip_force") > 0.5f)
		{
			isGrabbing = true;
		}
		else if ((float)GetInput("grip_force") < 0.1f)
		{
			isGrabbing = false;
		}
	}

	private void PickupsAndClimbing()
	{
		if (isGrabbing && grabee == null && numOfCollisions > 0)
		{
			for (int i = 0; i < numOfCollisions; i++)
			{
				if (gameHand.GetSlideCollision(i).GetCollider() is Pickup grabeee)
				{
					grabee = grabeee;
					grabeee.PickedUp(gameHand);
					return;
				}
				else if (gameHand.GetSlideCollision(i).GetCollider() is Climbable holdeee)
				{
					grabee = holdeee;
					return;
				}
				else if (gameHand.GetSlideCollision(i).GetCollider() is GameHand handeee)
				{
					grabee = handeee;
					gameHand.GrabedAsSecondaryHand(handeee);
					return;
				}
			}
		}
		else if (!isGrabbing && grabee != null)
		{
			if (grabee is Pickup grabeee)
			{
				grabeee.PutDown(gameHand, throwDist * 20, throwTorque * 10);
			}
			else if (grabee is GameHand handeee)
			{
				gameHand.ReleaseSecondaryHand(handeee, this.GlobalTransform);
			}
			grabee = null;
		}
		else if (grabee is Climbable)
		{
			gameHand.GlobalTransform = handGrabbedPos;
		}
		else
		{
			handGrabbedPos = gameHand.GlobalTransform;
		}
		
	}

	private int MoveGameHands()
	{
		//What is the distance between my real hand and the ingame hand? Is it non-zero
		distanceTo = GlobalPosition - gameHand.GlobalPosition;
		if (distanceTo.Length() > 0.001)
		{
			gameHand.Velocity = distanceTo * 50;
			if (distanceTo.Length() > .1)
			{
				gameHand.Velocity = distanceTo.Normalized() * 2.5f;
			}
			//gameHand.GlobalTransform = new Transform3D(gameHand.GlobalTransform.Basis, lastTransform.Origin);

			//Can you draw an uninterrupted line from my head to my real life hand. If so then my in game hand should teleport there
			PhysicsDirectSpaceState3D space = GetViewport().World3D.DirectSpaceState;
			PhysicsRayQueryParameters3D parameters = PhysicsRayQueryParameters3D.Create(GlobalPosition, GetParent().GetNode<XRCamera3D>("XRCamera3D").GlobalPosition, 1,hands);
			Godot.Collections.Dictionary result = space.IntersectRay(parameters);
			if (!realHand.HasOverlappingBodies() && result.Count == 0 && !isGrabbing)
			{
				gameHand.GlobalTransform = GlobalTransform;
				gameHand.Velocity = Vector3.Zero;
			}
			gameHand.SetRotation(GlobalRotation, GlobalPosition, otherHandPosition);
		}
		else
		{
			//If it isn't far away then just follow like normal, no extra velocity
			gameHand.Velocity = Vector3.Zero;
		}

		//Kinematic body does its thing
		gameHand.MoveAndSlide();
		return gameHand.GetSlideCollisionCount();
	}

	private void CalculateHandVelocityAndTorque(double delta)
	{
		throwNode.GlobalPosition = throwNode.GlobalPosition.MoveToward(GlobalPosition, (float)delta * 2);
		throwDist = GlobalPosition - throwNode.GlobalPosition;
		throwTorque = throwNode.GlobalTransform.Basis.Z.Cross(GlobalTransform.Basis.Z);

		throwNode.GlobalTransform = new Transform3D(throwNode.GlobalTransform.Basis.Slerp(GlobalTransform.Basis, 0.3f), throwNode.GlobalTransform.Origin);
	}

	private void ApplyForces()
	{
		for (int i = 0; i < numOfCollisions; i++)
		{
			KinematicCollision3D theCollision = gameHand.GetSlideCollision(i);
			if (theCollision.GetCollider() is RigidBody3D rigidB)
			{
				float hitForce = throwDist.Length() * 500 + throwTorque.Length() * (rigidB.GlobalPosition-gameHand.GlobalPosition).Length() * 100 + 10;
				rigidB.ApplyCentralForce(-theCollision.GetNormal() * hitForce);
			}
		}
	}

	//returns the distance between the in game and real life hand
	private Vector3 HandDistance()
	{
		if (distanceTo.Length() > 0.001)
		{
			return distanceTo;
		}
		return Vector3.Zero;
	}

	public bool IsClimbing()
	{
		return grabee is Climbable;
	}

	public bool HasPickup()
	{
		return grabee is Pickup;
	}

	public Vector3 HandPushSelfAmount()
	{ 
		if (isGrabbing && numOfCollisions > 0)
		{
			return -HandDistance() * 5;
		}
		return Vector3.Zero;
	}

	public void setOtherHandLocation(Vector3 otherPosition)
	{
		otherHandPosition = otherPosition;
	}
}
