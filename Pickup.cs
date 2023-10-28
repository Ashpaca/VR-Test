using Godot;
using System;

public partial class Pickup : RigidBody3D
{
	Godot.Collections.Array meshbodys;
	Vector3 grabOffset;
	public override void _Ready()
	{
		meshbodys = new Godot.Collections.Array{};
		for (int i = GetChildCount() - 1; i >= 0; i--)
		{
			meshbodys.Add(GetChild(i));
		}
	}

	public void PickedUp(CharacterBody3D hand)
	{
		for (int i = 0; i < meshbodys.Count; i++)
		{
			((Node3D)meshbodys[i]).Reparent(hand);
		}
		grabOffset = GlobalPosition - hand.GlobalPosition;
	}

	public void PutDown(CharacterBody3D hand, Vector3 launchLinear, Vector3 launchAngular)
	{
		for (int i = 0; i < meshbodys.Count; i++)
		{
			Transform = hand.Transform; // do I need to set to the original offset? how can i use grabOffset to do this...
			LinearVelocity = Vector3.Zero;
			AngularVelocity = Vector3.Zero;
			((Node3D)meshbodys[i]).Reparent(this);
		}
		ApplyTorqueImpulse(launchAngular);
		ApplyCentralImpulse(launchLinear);
	}
}
