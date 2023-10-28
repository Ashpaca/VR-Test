using Godot;
using System;
using System.Diagnostics;

public partial class Hand : XRController3D
{
	GameHand GameHand { get; set; }
	Area3D RealHand { get; set; }
	Godot.Collections.Array<Rid> Hands { get; set; }
	Transform3D HandGrabbedPos { get; set; }
	Vector3 OtherHandPosition { get; set; }

	public bool IsGrabbing { get; set; }
	Node3D Grabee { get; set; }
	[Export] 
	private string throwNodeName = "";
	[Export] 
	private string GameHandName = "";
	Node3D ThrowNode { get; set; }
	Vector3 ThrowDist { get; set; }
	Vector3 ThrowTorque { get; set; }

	Vector3 DistanceTo { get; set; }
	int NumOfCollisions { get; set; }

	public override void _Ready()
	{
		GameHand = GetParent().GetNode<GameHand>(GameHandName);
		RealHand = GetNode<Area3D>("Area3D");

		Hands = new Godot.Collections.Array<Rid>();
		Hands.Add(GameHand.GetRid());
		Hands.Add(RealHand.GetRid());

		HandGrabbedPos = GameHand.GlobalTransform;
		OtherHandPosition = GameHand.GlobalPosition;

		Grabee = null;
		IsGrabbing = false;
		ThrowNode = GetParent().GetNode<Node3D>(throwNodeName);
		ThrowDist = Vector3.Zero;
		ThrowTorque = Vector3.Zero;

		DistanceTo = Vector3.Zero;
		NumOfCollisions = 0;
	}

	public override void _PhysicsProcess(double delta)
	{
		NumOfCollisions = MoveGameHands();
		CalculateHandVelocityAndTorque(delta);
		ApplyForces();

		PickupsAndClimbing();
	}

	public override void _Process(double delta)
	{
		if ((float)GetInput("grip_force") > 0.5f)
		{
			IsGrabbing = true;
		}
		else if ((float)GetInput("grip_force") < 0.1f)
		{
			IsGrabbing = false;
		}
	}

	private void PickupsAndClimbing()
	{
		if (IsGrabbing && Grabee == null && NumOfCollisions > 0)
		{
			for (int i = 0; i < NumOfCollisions; i++)
			{
				if (GameHand.GetSlideCollision(i).GetCollider() is Pickup grabeee)
				{
					Grabee = grabeee;
					grabeee.PickedUp(GameHand);
					return;
				}
				else if (GameHand.GetSlideCollision(i).GetCollider() is Climbable holdeee)
				{
					Grabee = holdeee;
					return;
				}
				else if (GameHand.GetSlideCollision(i).GetCollider() is GameHand handeee)
				{
					Grabee = handeee;
					GameHand.GrabedAsSecondaryHand(handeee);
					return;
				}
			}
		}
		else if (!IsGrabbing && Grabee != null)
		{
			if (Grabee is Pickup grabeee)
			{
				grabeee.PutDown(GameHand, ThrowDist * 20, ThrowTorque * 10);
			}
			else if (Grabee is GameHand handeee)
			{
				GameHand.ReleaseSecondaryHand(handeee, this.GlobalTransform);
			}
			Grabee = null;
		}
		else if (Grabee is Climbable)
		{
			GameHand.GlobalTransform = HandGrabbedPos;
		}
		else
		{
			HandGrabbedPos = GameHand.GlobalTransform;
		}
		
	}

	private int MoveGameHands()
	{
		//What is the distance between my real hand and the ingame hand? Is it non-zero
		DistanceTo = GlobalPosition - GameHand.GlobalPosition;
		if (DistanceTo.Length() > 0.001)
		{
			GameHand.Velocity = DistanceTo * 50;
			if (DistanceTo.Length() > .1)
			{
				GameHand.Velocity = DistanceTo.Normalized() * 2.5f;
			}
			//GameHand.GlobalTransform = new Transform3D(GameHand.GlobalTransform.Basis, lastTransform.Origin);

			//Can you draw an uninterrupted line from my head to my real life hand. If so then my in game hand should teleport there
			PhysicsDirectSpaceState3D space = GetViewport().World3D.DirectSpaceState;
			PhysicsRayQueryParameters3D parameters = PhysicsRayQueryParameters3D.Create(GlobalPosition, GetParent().GetNode<XRCamera3D>("XRCamera3D").GlobalPosition, 1,Hands);
			Godot.Collections.Dictionary result = space.IntersectRay(parameters);
			if (!RealHand.HasOverlappingBodies() && result.Count == 0 && !IsGrabbing)
			{
				GameHand.GlobalTransform = GlobalTransform;
				GameHand.Velocity = Vector3.Zero;
			}
			GameHand.SetRotation(GlobalRotation, GlobalPosition, OtherHandPosition);
		}
		else
		{
			//If it isn't far away then just follow like normal, no extra velocity
			GameHand.Velocity = Vector3.Zero;
		}

		//Kinematic body does its thing
		GameHand.MoveAndSlide();
		return GameHand.GetSlideCollisionCount();
	}

	private void CalculateHandVelocityAndTorque(double delta)
	{
		ThrowNode.GlobalPosition = ThrowNode.GlobalPosition.MoveToward(GlobalPosition, (float)delta * 2);
		ThrowDist = GlobalPosition - ThrowNode.GlobalPosition;
		ThrowTorque = ThrowNode.GlobalTransform.Basis.Z.Cross(GlobalTransform.Basis.Z);

		ThrowNode.GlobalTransform = new Transform3D(ThrowNode.GlobalTransform.Basis.Slerp(GlobalTransform.Basis, 0.3f), ThrowNode.GlobalTransform.Origin);
	}

	private void ApplyForces()
	{
		for (int i = 0; i < NumOfCollisions; i++)
		{
			KinematicCollision3D theCollision = GameHand.GetSlideCollision(i);
			if (theCollision.GetCollider() is RigidBody3D rigidB)
			{
				float hitForce = ThrowDist.Length() * 500 + ThrowTorque.Length() * (rigidB.GlobalPosition-GameHand.GlobalPosition).Length() * 100 + 10;
				rigidB.ApplyCentralForce(-theCollision.GetNormal() * hitForce);
			}
		}
	}

	//returns the distance between the in game and real life hand
	private Vector3 HandDistance()
	{
		if (DistanceTo.Length() > 0.001)
		{
			return DistanceTo;
		}
		return Vector3.Zero;
	}

	public bool IsClimbing()
	{
		return Grabee is Climbable;
	}

	public bool HasPickup()
	{
		return Grabee is Pickup;
	}

	public Vector3 HandPushSelfAmount()
	{ 
		if (IsGrabbing && NumOfCollisions > 0)
		{
			return -HandDistance() * 5;
		}
		return Vector3.Zero;
	}

	public void setOtherHandLocation(Vector3 otherPosition)
	{
		OtherHandPosition = otherPosition;
	}
}
