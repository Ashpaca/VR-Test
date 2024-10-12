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
		if (LinearVelocity.LengthSquared() > 10)
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
				GetTree().Root.AddChild(stuckArrow);
				stuckArrow.GlobalTransform = GlobalTransform;
				
				//for (int i = GetChildCount() - 1; i >= 0; i--)
				//{
				//	GetChild(i).Reparent(pointyBit.GetOverlappingBodies()[0]); 	// they also need to be added to that objects meshbodies list if it is a pick up
																				// if it isn't a pick up then it should probably use the comment out code instead
				//}
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
