using Godot;
using System;

public partial class GameHand : CharacterBody3D
{
	Godot.Collections.Array meshbodys;
	Vector3 pHandOriginalPos;
	Vector3 sHandOriginalPos;
	Vector3 pHandOriginalRot;
	bool isPrimaryHand;

	public override void _Ready()
	{
		meshbodys = new Godot.Collections.Array{};
		for (int i = GetChildCount() - 1; i >= 0; i--)
		{
			meshbodys.Add(GetChild(i));
		}

		pHandOriginalPos = Vector3.Zero;
		sHandOriginalPos = Vector3.Zero;
		pHandOriginalRot = Vector3.Zero;
		isPrimaryHand = false;
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
		isPrimaryHand = true;
		pHandOriginalPos = GlobalPosition;
		sHandOriginalPos = secondaryHandPos;
		pHandOriginalRot = GlobalRotation;
	}

	public void UnSetPrimaryHand()
	{
		isPrimaryHand = false;
	}

	public void SetRotation(Vector3 rotation, Vector3 primaryPosition, Vector3 secondaryPosition)
	{
		if (!isPrimaryHand)
		{
			GlobalRotation = rotation;
		}
		else
		{
			/*GlobalRotation = new Vector3( 
				Mathf.Atan2(secondaryPosition.Z - primaryPosition.Z, secondaryPosition.Y - primaryPosition.Y) - Mathf.Atan2(sHandOriginalPos.Z - pHandOriginalPos.Z, sHandOriginalPos.Y - pHandOriginalPos.Y) + pHandOriginalRot.X,
				-Mathf.Atan2(secondaryPosition.Z - primaryPosition.Z, secondaryPosition.X - primaryPosition.X) - Mathf.Atan2(sHandOriginalPos.Z - pHandOriginalPos.Z, sHandOriginalPos.X - pHandOriginalPos.X) + pHandOriginalRot.Y,
				Mathf.Atan2(secondaryPosition.Y - primaryPosition.Y, secondaryPosition.X - primaryPosition.X) - Mathf.Atan2(sHandOriginalPos.Y - pHandOriginalPos.Y, sHandOriginalPos.X - pHandOriginalPos.X) + pHandOriginalRot.Z
				);*/
		}
	}
}
