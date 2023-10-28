using Godot;
using System;

public partial class Pickup : RigidBody3D
{
	Godot.Collections.Array Meshbodys { get; set; }
	Vector3 GrabOffset { get; set; }
	public override void _Ready()
	{
		Meshbodys = new Godot.Collections.Array{};
		for (int i = GetChildCount() - 1; i >= 0; i--)
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

	public void PutDown(CharacterBody3D hand, Vector3 launchLinear, Vector3 launchAngular)
	{
		for (int i = 0; i < Meshbodys.Count; i++)
		{
			Transform = hand.Transform; // do I need to set to the original offset? how can i use GrabOffset to do this...
			LinearVelocity = Vector3.Zero;
			AngularVelocity = Vector3.Zero;
			((Node3D)Meshbodys[i]).Reparent(this);
		}
		ApplyTorqueImpulse(launchAngular);
		ApplyCentralImpulse(launchLinear);
	}
}
