using Godot;
using System;

public partial class Arrow : Pickup
{
	Area3D pointyBit;
	PackedScene stuckArrowScene;
	bool wasShot = false;

	public override void _Ready()
	{
		base._Ready();
		pointyBit = GetNode<Area3D>("Pointy Bit");
		stuckArrowScene = GD.Load<PackedScene>("res://stuck_arrow.tscn");
	}

	public override void _PhysicsProcess(double delta)
	{
		if (!wasShot)
		{
			return;
		}
		if (LinearVelocity.LengthSquared() > 10 && !(GlobalPosition + LinearVelocity).IsEqualApprox(Vector3.Up))
		{
			// need to make it aim where it is moving if it is moving fast. Change to use forces
			LookAt(GlobalPosition + LinearVelocity, Vector3.Up);
			RotateObjectLocal(Vector3.Right, Mathf.Pi / 2.0f);
		}
		
		if (LinearVelocity.LengthSquared() > 100)
		{
			if (pointyBit.HasOverlappingBodies() && pointyBit.GetOverlappingBodies()[0] is not Pickup)
			{
				
				QueueFree();
				Climbable stuckArrow = stuckArrowScene.Instantiate<Climbable>();
				GetTree().Root.GetChild(0).GetChild(0).AddChild(stuckArrow);
				stuckArrow.GlobalTransform = GlobalTransform;
			}
			else if (pointyBit.HasOverlappingBodies() && pointyBit.GetOverlappingBodies()[0] is Pickup hitObj)
			{
				foreach(Node body in Meshbodys)
				{
					hitObj.Meshbodys.Add(body);
					body.Reparent(hitObj);
				}
				hitObj.ApplyCentralImpulse(LinearVelocity);
				QueueFree();
			}
		}

		if (wasShot && pointyBit.HasOverlappingBodies())
		{
			wasShot = false;
		}
	}

	public void Shoot()
	{
		wasShot = true;
	}
}
