using Godot;
using System;

public partial class BowAndArrow : Pickup
{
	AnimationPlayer bowAnimation;
	double animationLength = 4.1667;
	double handDistanceRest = 0.2; //estimate
	double drawDistance = 0.8; //estimate

	double drawPercentage = 0;

	public override void _Ready()
	{
		base._Ready();
		bowAnimation = GetNode("Bow_and_Arrow").GetNode<AnimationPlayer>("AnimationPlayer");
		bowAnimation.Play("Use");
	}

	public void PullBackString(double HandDistance)
	{
		drawPercentage = Math.Clamp((HandDistance - handDistanceRest) / drawDistance, 0, 1);
		double animationFrame = drawPercentage * animationLength;
		bowAnimation.Seek(animationFrame, true);
		bowAnimation.Pause();
	}

	public double GetPowerPercentage()
	{
		return drawPercentage;
	}
}
