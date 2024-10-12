using Godot;
using System;
using System.Collections.Generic;
using System.Diagnostics;

public partial class Hand : XRController3D
{
	GameHand GameHandObject { get; set; }
	Hand SecondHand { get; set; }
	Area3D RealHand { get; set; }
	Godot.Collections.Array<Rid> Hands { get; set; }
	Transform3D HandGrabbedPos { get; set; }
	Vector3 HoldeePos { get; set; }

	public bool IsGrabbing { get; set; }
	public bool IsTriggerPressing { get; set; }
	double TimeSinceGrabbed { get; set; }
	double TimeSinceTriggerPressed { get; set; }
	double TIME_FOR_GRAB = 0.2;
	public Node3D Grabee { get; set; }
	[Export] 
	private string throwNodeName = "";
	[Export] 
	private string GameHandName = "";
	Node3D ThrowNode { get; set; }
	Vector3 ThrowDist { get; set; }
	Vector3 ThrowTorque { get; set; }

	Vector3 DistanceTo { get; set; }
	int NumOfCollisions { get; set; }
	int NumOfGrabbables { get; set; }

	Basis oldBasis;

	PackedScene justBowScene;
	PackedScene justArrowScene;
	PackedScene bowAndArrowScene;

	int[] numberOfBows;
	int[] numberOfArrows;

	public override void _Ready()
	{
		justBowScene = GD.Load<PackedScene>("res://just_bow.tscn");
		justArrowScene = GD.Load<PackedScene>("res://just_arrow.tscn");
		bowAndArrowScene = GD.Load<PackedScene>("res://bow_and_arrow.tscn");

		GameHandObject = GetParent().GetNode<GameHand>(GameHandName);
		RealHand = GetNode<Area3D>("Area3D");

		Hands = new Godot.Collections.Array<Rid>
        {
            GameHandObject.GetRid(),
            RealHand.GetRid()
        };

		HandGrabbedPos = GameHandObject.GlobalTransform;

		Grabee = null;
		IsGrabbing = false;
		IsTriggerPressing = false;
		TimeSinceGrabbed = 0;
		TimeSinceTriggerPressed = 0;
	
		ThrowNode = GetParent().GetNode<Node3D>(throwNodeName);
		ThrowDist = Vector3.Zero;
		ThrowTorque = Vector3.Zero;

		DistanceTo = Vector3.Zero;
		NumOfCollisions = 0;
		NumOfGrabbables = 0;

		oldBasis = Basis;
	}

	public override void _PhysicsProcess(double delta)
	{
		MoveGameHands();
		CalculateHandVelocityAndTorque(delta);
		ApplyForces();

		ReturnBowAndArrows();
		PickupsAndClimbing();
		GrabSpawnable();
		CombineBowAndArrow();
	}

	public override void _Process(double delta)
	{
		if ((float)GetInput("grip") > 0.6f)
		{
			IsGrabbing = true;
			TimeSinceGrabbed += delta;
			GameHandObject.Grabbing(true);
		}
		else if ((float)GetInput("grip") < 0.3f)
		{
			IsGrabbing = false;
			TimeSinceGrabbed = 0;
			GameHandObject.Grabbing(false);
		}
		if ((float)GetInput("trigger") > 0.6f)
		{
			IsTriggerPressing = true;
			TimeSinceTriggerPressed += delta;
		}
		else if ((float)GetInput("trigger") < 0.3f)
		{
			IsTriggerPressing = false;
			TimeSinceTriggerPressed = 0;
		}
	}

	private void ReturnBowAndArrows()
	{
		Godot.Collections.Array<Area3D> handZones = GameHandObject.HandZones();

		if (!IsGrabbing && Grabee != null && handZones.Count > 0)
		{
			for (int i = 0; i < handZones.Count; i++)
			{
				if (handZones[i] is SpawnArea spawner)
				{
					Pickup spawnee = spawner.SpawnPickup();

					if (Grabee is Bow && spawnee is Bow)
					{
						numberOfBows[0]++;
						((Pickup)Grabee).PutDown(GameHandObject, Vector3.Zero, Vector3.Zero);
						Grabee.QueueFree();
						GameHandObject.ReleasingPickup();
						return;
					}

					if (Grabee is Arrow && spawnee is Arrow)
					{
						numberOfArrows[0]++;
						((Pickup)Grabee).PutDown(GameHandObject, Vector3.Zero, Vector3.Zero);
						Grabee.QueueFree();
						GameHandObject.ReleasingPickup();
						return;
					}
				}
			}
		}
	}
	
	private void CombineBowAndArrow()
	{
		if (!IsTriggerPressing || TimeSinceTriggerPressed >= TIME_FOR_GRAB || SecondHand.Grabee is not Bow || Grabee is not Arrow)
		{
			return;
		}

		Godot.Collections.Array<Area3D> handZones = GameHandObject.HandZones();
		
		for (int i = 0; i < handZones.Count; i++)
		{
			if (((Node)handZones[i]).IsInGroup("bow"))
			{
				((Pickup)Grabee).PutDown(GameHandObject, Vector3.Zero, Vector3.Zero);
				Grabee.QueueFree();
				((Pickup)SecondHand.Grabee).PutDown(SecondHand.GameHandObject, Vector3.Zero, Vector3.Zero);
				SecondHand.Grabee.QueueFree();
				
				//add bow and arrow twohanded object
				BowAndArrow bna = bowAndArrowScene.Instantiate<BowAndArrow>();
				GetTree().Root.AddChild(bna);
				bna.GlobalTransform = SecondHand.GameHandObject.GlobalTransform;
				bna.SpecialGrab(SecondHand.GameHandName == "Left Game Hand");
				SecondHand.Grabee = bna;
				bna.PickedUp(SecondHand.GameHandObject);
				SecondHand.GameHandObject.HoldingPickup();

				//attach the second hand
				SecondaryGrab(SecondHand.GameHandObject, SecondHand.GameHandObject.GlobalPosition + SecondHand.Basis.Y);
				return;
			}
		}
	}

	private void GrabSpawnable()
	{
		Godot.Collections.Array<Area3D> handZones = GameHandObject.HandZones();

		if (IsGrabbing && TimeSinceGrabbed < TIME_FOR_GRAB && Grabee == null && handZones.Count > 0)
		{
			for (int i = 0; i < handZones.Count; i++)
			{
				if (handZones[i] is SpawnArea spawner)
				{
					Pickup spawnee = spawner.SpawnPickup();
					//Check if you have a limited item like a bow or an arrow
					bool isBow = spawnee is Bow;
					bool isArrow = spawnee is Arrow;
					if ((!isBow || numberOfBows[0] >= 1) && (!isArrow || numberOfArrows[0] >= 1))
					{
						if (isBow)
						{
							numberOfBows[0]--;
						}
						else if (isArrow)
						{
							numberOfArrows[0]--;
						}
						GetTree().Root.AddChild(spawnee);
						spawnee.GlobalTransform = GameHandObject.GlobalTransform;
						spawnee.SpecialGrab(GameHandName == "Left Game Hand");
						Grabee = spawnee;
						spawnee.PickedUp(GameHandObject);
						GameHandObject.HoldingPickup();
						return;
					}
				}
			}
		}
	}

	public void SecondaryGrab(GameHand primaryHand, Vector3 secondHandPos)
	{
		Grabee = primaryHand;
		GameHandObject.GrabedAsSecondaryHand(primaryHand, secondHandPos);
		GameHandObject.HoldingPickup();
	}

	private void PickupsAndClimbing()
	{
		if (IsGrabbing && TimeSinceGrabbed < TIME_FOR_GRAB && Grabee == null && NumOfGrabbables > 0)
		{
			for (int i = 0; i < NumOfGrabbables; i++)
			{
				if (GameHandObject.Grabbables()[i] is Pickup grabeee)
				{
					Grabee = grabeee;
					grabeee.PickedUp(GameHandObject);
					GameHandObject.HoldingPickup();
					return;
				}
				else if (GameHandObject.Grabbables()[i] is Climbable holdeee)
				{
					Grabee = holdeee;
					HoldeePos = Grabee.GlobalPosition;
					return;
				}
				else if (GameHandObject.Grabbables()[i] is GameHand handeee)
				{
					SecondaryGrab(handeee, GameHandObject.GlobalPosition);
					return;
				}
			}
		}
		else if (!IsGrabbing && Grabee != null)
		{
			if (Grabee is Pickup grabeee)
			{
				if (Grabee is BowAndArrow)
				{
					Transform3D bowLoc = GameHandObject.GlobalTransform;
					//delete bow and arrow
					SecondHand.GameHandObject.ReleaseHand(GameHandObject, SecondHand.GlobalTransform);
					((Pickup)Grabee).PutDown(GameHandObject, Vector3.Zero, Vector3.Zero);
					Grabee.QueueFree();

					//spawn bow, then bow.putdown
					Bow droppedBow = justBowScene.Instantiate<Bow>();
					GetTree().Root.AddChild(droppedBow);
					droppedBow.PutDown(GameHandObject, ThrowDist * 20, ThrowTorque * 10);
					droppedBow.GlobalTransform = bowLoc;

					//spawn arrow in second hand
					Arrow heldArrow = justArrowScene.Instantiate<Arrow>();
					GetTree().Root.AddChild(heldArrow);
					heldArrow.GlobalTransform = SecondHand.GameHandObject.GlobalTransform;
					heldArrow.SpecialGrab(SecondHand.GameHandName == "Left Game Hand");
					SecondHand.Grabee = heldArrow;
					heldArrow.PickedUp(SecondHand.GameHandObject);
				}
				else if (GameHandObject.IsTwoHanded())
				{
					SecondHand.Grabee = grabeee;
					SecondHand.GameHandObject.ReleaseHand(GameHandObject, SecondHand.GlobalTransform);
					grabeee.SwapHand(SecondHand.GameHandObject, SecondHand.GlobalTransform);
				}
				else
				{
					GameHandObject.GlobalTransform = GlobalTransform;
					grabeee.PutDown(GameHandObject, ThrowDist * 20, ThrowTorque * 10);
				}
				GameHandObject.ReleasingPickup();
			}
			else if (Grabee is GameHand handeee)
			{
				if (SecondHand.Grabee is BowAndArrow baaee)
				{
					Transform3D bowLoc = SecondHand.GameHandObject.GlobalTransform;
					double power = baaee.GetPowerPercentage();
					//delete bow and arrow
					GameHandObject.ReleaseHand(SecondHand.GameHandObject, GlobalTransform);
					baaee.PutDown(SecondHand.GameHandObject, Vector3.Zero, Vector3.Zero);
					baaee.QueueFree();
					GameHandObject.ReleasingPickup();

					//second hand holds a spawned bow
					Bow heldBow = justBowScene.Instantiate<Bow>();
					GetTree().Root.AddChild(heldBow);
					heldBow.GlobalTransform = SecondHand.GameHandObject.GlobalTransform;
					heldBow.SpecialGrab(SecondHand.GameHandName == "Left Game Hand");
					SecondHand.Grabee = heldBow;
					heldBow.PickedUp(SecondHand.GameHandObject);

					//spawn arrow that shoots forward based on hand distance
					Arrow droppedArrow = justArrowScene.Instantiate<Arrow>();
					GetTree().Root.AddChild(droppedArrow);
					GameHandObject.GlobalTransform = bowLoc;
					droppedArrow.PutDown(GameHandObject, -GameHandObject.Basis.Y * (float) power * 60, Vector3.Zero);
					droppedArrow.Shoot();
				}
				else
				{
					GameHandObject.ReleaseHand(handeee, GlobalTransform);
					GameHandObject.ReleasingPickup();
				}
			}
			Grabee = null;
		}
		else if (Grabee is Climbable)
		{
			GameHandObject.GlobalTransform = HandGrabbedPos;
			GameHandObject.GlobalPosition += Grabee.GlobalPosition - HoldeePos;
		}
		else if (Grabee is Pickup)
		{
			if (Grabee is BowAndArrow boweee)
			{
				if (GameHandObject.IsTwoHanded())
				{
					boweee.PullBackString((GlobalPosition - SecondHand.GlobalPosition).Length());
				}
				else
				{
					boweee.PullBackString(0);
				}
			}
		}
		else if (Grabee is GameHand && !IsTriggerPressing && SecondHand.Grabee is BowAndArrow)
		{
			//delete bow and arrow
			GameHandObject.ReleaseHand(SecondHand.GameHandObject, GlobalTransform);
			((Pickup)SecondHand.Grabee).PutDown(SecondHand.GameHandObject, Vector3.Zero, Vector3.Zero);
			SecondHand.Grabee.QueueFree();

			//this hand holds a spawned in arrow
			Arrow heldArrow = justArrowScene.Instantiate<Arrow>();
			GetTree().Root.AddChild(heldArrow);
			heldArrow.GlobalTransform = GameHandObject.GlobalTransform;
			heldArrow.SpecialGrab(GameHandName == "Left Game Hand");
			Grabee = heldArrow;
			heldArrow.PickedUp(GameHandObject);

			//second hand holds a spawned in bow
			Bow heldBow = justBowScene.Instantiate<Bow>();
			GetTree().Root.AddChild(heldBow);
			heldBow.GlobalTransform = SecondHand.GameHandObject.GlobalTransform;
			heldBow.SpecialGrab(SecondHand.GameHandName == "Left Game Hand");
			SecondHand.Grabee = heldBow;
			heldBow.PickedUp(SecondHand.GameHandObject);
		}
		else
		{
			// Should this always run? or sometimes? or only when nothing else is happening? What does this do?
		 	//something about climbing rope
			HandGrabbedPos = GameHandObject.GlobalTransform;
		}
	}

	private void MoveGameHands()
	{
		//What is the distance between my real hand and the ingame hand? Is it non-zero
		DistanceTo = GlobalPosition - GameHandObject.GlobalPosition;
		if (DistanceTo.Length() > 0.001)
		{
			GameHandObject.Velocity = DistanceTo * 50;
			if (DistanceTo.Length() > .1)
			{
				GameHandObject.Velocity = DistanceTo.Normalized() * 2.5f;
			}

			//Can you draw an uninterrupted line from my head to my real life hand. If so then my in game hand should teleport there
			PhysicsDirectSpaceState3D space = GetViewport().World3D.DirectSpaceState;
			PhysicsRayQueryParameters3D parameters = PhysicsRayQueryParameters3D.Create(GlobalPosition, GetParent().GetNode<XRCamera3D>("XRCamera3D").GlobalPosition, 1,Hands);
			Godot.Collections.Dictionary result = space.IntersectRay(parameters);
			if (!RealHand.HasOverlappingBodies() && result.Count == 0 && !IsGrabbing)
			{
				GameHandObject.GlobalTransform = GlobalTransform;
				GameHandObject.Velocity = Vector3.Zero;
			}
		}
		else
		{
			//If it isn't far away then just follow like normal, no extra velocity
			GameHandObject.Velocity = Vector3.Zero;
		}

		GameHandObject.SetRotation(GlobalPosition, SecondHand.GlobalPosition, Basis);

		//Kinematic body does its thing
		GameHandObject.MoveAndSlide();
		NumOfCollisions = GameHandObject.GetSlideCollisionCount();
		NumOfGrabbables = GameHandObject.Grabbables().Count;
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
			KinematicCollision3D theCollision = GameHandObject.GetSlideCollision(i);
			if (theCollision.GetCollider() is RigidBody3D rigidB)
			{
				float hitForce = ThrowDist.Length() * 500 + ThrowTorque.Length() * (rigidB.GlobalPosition-GameHandObject.GlobalPosition).Length() * 100 + 10;
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
		if (IsGrabbing && (NumOfCollisions > 0 || IsClimbing()))
		{
			return -HandDistance();
		}
		return Vector3.Zero;
	}

	public void SecondHandReference(Hand otherHand)
	{
		SecondHand = otherHand;
	}

	public void SetUpBowAndArrowCounts(int[] numBows, int[] numArrows)
	{
		numberOfBows = numBows;
		numberOfArrows = numArrows;
	}
}
