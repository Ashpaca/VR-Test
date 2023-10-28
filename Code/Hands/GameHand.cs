using Godot;
using System;

public partial class GameHand : CharacterBody3D
{
	Godot.Collections.Array meshbodys;
	Vector3 PHandOriginalPos { get; set; }
	Vector3 SHandOriginalPos  { get; set; }
	Vector3 PHandOriginalRot  { get; set; }
	bool IsPrimaryHand { get; set; }

	public override void _Ready()
	{
		meshbodys = new Godot.Collections.Array{};
		for (int i = GetChildCount() - 1; i >= 0; i--)
		{
			meshbodys.Add(GetChild(i));
		}

		PHandOriginalPos = Vector3.Zero;
		SHandOriginalPos = Vector3.Zero;
		PHandOriginalRot = Vector3.Zero;
		IsPrimaryHand = false;
	}

	public void GrabedAsSecondaryHand(GameHand hand)
	{
		for (int i = 0; i < meshbodys.Count; i++)
		{
			((Node3D)meshbodys[i]).Reparent(hand);
		}
		hand.SetPrimaryHand(GlobalPosition);
	}

	public void ReleaseSecondaryHand(GameHand hand, Transform3D originalLocation)
	{
		GlobalTransform = originalLocation;
		for (int i = 0; i < meshbodys.Count; i++)
		{
			((Node3D)meshbodys[i]).Reparent(this);
			((Node3D)meshbodys[i]).GlobalTransform = originalLocation;;
		}
		hand.UnSetPrimaryHand();
	}

	public void SetPrimaryHand(Vector3 secondaryHandPos)
	{
		IsPrimaryHand = true;
		PHandOriginalPos = GlobalPosition;
		SHandOriginalPos = secondaryHandPos;
		PHandOriginalRot = GlobalRotation;
	}

	public void UnSetPrimaryHand()
	{
		IsPrimaryHand = false;
	}

	public void SetRotation(Vector3 rotation, Vector3 primaryPosition, Vector3 secondaryPosition)
	{
		if (!IsPrimaryHand)
		{
			GlobalRotation = rotation;
		}
		else
		{
			Vector3 handRotationAxis = (SHandOriginalPos - PHandOriginalPos).Cross(secondaryPosition - primaryPosition);
			float handRotationAngle = (SHandOriginalPos - PHandOriginalPos).AngleTo(secondaryPosition - primaryPosition);
			Transform = Transform.Rotated(handRotationAxis, handRotationAngle);
		}
	}
}
