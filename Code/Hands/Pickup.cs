using System.Collections.Generic;
using Godot;
using Godot.Collections;

public partial class Pickup : RigidBody3D
{
	public List<Node> Meshbodys { get; set; }
	public Vector3 GrabOffset { get; set; }
	[Export]
	public Vector3 SpawnOffset { get; set; }

	public override void _Ready()
	{
		Meshbodys = new List<Node>{};
		for (int i = 0; i < GetChildCount(); i++)
		{
			Meshbodys.Add(GetChild(i));
		}
	}

	public void PickedUp(CharacterBody3D hand)
	{
		for (int i = 0; i < Meshbodys.Count; i++)
		{
			((Node3D)Meshbodys[i]).Reparent(hand);
		}
		GrabOffset = GlobalPosition - hand.GlobalPosition;
	}

	public void SwapHand(CharacterBody3D newHand, Transform3D handLocation)
	{
		for (int i = 0; i < Meshbodys.Count; i++)
		{
			((Node3D)Meshbodys[i]).Reparent(newHand);
		}
	}

	public void PutDown(CharacterBody3D hand, Vector3 launchLinear, Vector3 launchAngular)
	{
		GlobalTransform = hand.GlobalTransform;
		for (int i = 0; i < Meshbodys.Count; i++)
		{
			((Node3D)Meshbodys[i]).Reparent(this);
		}
		
		LinearVelocity = Vector3.Zero;
		AngularVelocity = Vector3.Zero;
		ApplyTorqueImpulse(launchAngular);
		ApplyCentralImpulse(launchLinear);
	}

	public void SpecialGrab(bool left)
	{
		if (left)
		{
			GlobalPosition += Basis.X.Normalized() * - SpawnOffset.X + Basis.Y.Normalized() * SpawnOffset.Y + Basis.Z.Normalized() * - SpawnOffset.Z;
		}
		else
		{
			GlobalPosition += Basis.X.Normalized() * SpawnOffset.X + Basis.Y.Normalized() * SpawnOffset.Y + Basis.Z.Normalized() * SpawnOffset.Z;
		}
	}
}
