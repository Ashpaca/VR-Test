using Godot;

public partial class GameHand : CharacterBody3D
{
	Godot.Collections.Array meshbodys;
	Vector3 PrimaryOriginalPos { get; set; }
	Vector3 SecondaryOriginalPos  { get; set; }
	Basis PrimaryOriginalBasis  { get; set; }
	bool IsPrimaryHand { get; set; }

	Area3D GrabZone { get; set; }

	Node3D OpenHandMeshes { get; set; }
	Node3D ClosedHandMeshes { get; set; }

	Vector3 originalHandVector;

	public override void _Ready()
	{
		meshbodys = new Godot.Collections.Array{};
		for (int i = 0; i < GetChildCount()-1; i++)
		{
			meshbodys.Add(GetChild(i));
		}
		GrabZone = GetNode<Area3D>("Grab Area");

		OpenHandMeshes = GetNode("Meshes").GetNode<Node3D>("Open Hand");
		ClosedHandMeshes = GetNode("Meshes").GetNode<Node3D>("Closed Hand");

		PrimaryOriginalPos = Vector3.Zero;
		SecondaryOriginalPos = Vector3.Zero;
		PrimaryOriginalBasis = Basis.Identity;
		IsPrimaryHand = false;
		
		originalHandVector = Vector3.Zero;
	}

	public void HoldingPickup()
	{
		((CollisionShape3D)meshbodys[0]).Disabled = true;
	}

	public void ReleasingPickup()
	{
		((CollisionShape3D)meshbodys[0]).Disabled = false;
	}

	public void Grabbing(bool isIt)
	{
		ClosedHandMeshes.Visible = isIt;
		OpenHandMeshes.Visible = !isIt;
	}

	public void GrabedAsSecondaryHand(GameHand hand, Vector3 secondHandPos)
	{
		for (int i = 0; i < meshbodys.Count; i++)
		{
			((Node3D)meshbodys[i]).Reparent(hand);
		}
		hand.SetPrimaryHand(secondHandPos);
	}

	public void ReleaseHand(GameHand hand, Transform3D originalLocation)
	{
		GlobalTransform = originalLocation;
		for (int i = 0; i < meshbodys.Count; i++)
		{
			((Node3D)meshbodys[i]).Reparent(this);
			((Node3D)meshbodys[i]).GlobalTransform = originalLocation;
		}
		hand.UnSetPrimaryHand();
	}

	private void SetPrimaryHand(Vector3 secondaryHandPos)
	{
		IsPrimaryHand = true;
		PrimaryOriginalPos = GlobalPosition;
		SecondaryOriginalPos = secondaryHandPos;
		PrimaryOriginalBasis = Transform.Basis;
		originalHandVector = SecondaryOriginalPos - PrimaryOriginalPos;
	}

	private void UnSetPrimaryHand()
	{
		IsPrimaryHand = false;
	}

	public bool IsTwoHanded()
	{
		return IsPrimaryHand;
	}

	public Godot.Collections.Array<Node3D> Grabbables()
	{
		return GrabZone.GetOverlappingBodies();
	}

	public Godot.Collections.Array<Area3D> HandZones()
	{
		return GrabZone.GetOverlappingAreas();
	}

	public void SetRotation(Vector3 primaryCurrentPos, Vector3 secondaryCurrentPos, Basis primaryCurrentBasis)
	{
		// Set current rotation as a starting point
		Transform = new Transform3D(primaryCurrentBasis, Transform.Origin);
		
		
		// If you're not 2 handed then you're done 
		if (!IsPrimaryHand)
		{
			return;
		}

		// Rotate towards the second hand
		Vector3 originalHandVectorBC = PrimaryOriginalBasis.Inverse() * originalHandVector;
		Vector3 currentHandVectorBC = Basis.Inverse() * (secondaryCurrentPos - primaryCurrentPos);
		Vector3 secondaryHandRotationAxis = Basis * originalHandVectorBC.Normalized().Cross(currentHandVectorBC.Normalized());
		float secondaryHandRotationAngle = originalHandVectorBC.Normalized().AngleTo(currentHandVectorBC.Normalized());
		Basis = Basis.Rotated(secondaryHandRotationAxis.Normalized(), secondaryHandRotationAngle).Orthonormalized();
	}
}
