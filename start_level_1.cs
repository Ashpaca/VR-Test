using Godot;
using System;
using System.Collections.Generic;

public partial class start_level_1 : Node3D
{
	public DirectionalLight3D TheSun { get; set; }
	public List<Pickup> Targets { get; set; }
	public List<int> TargetsMeshCount { get; set; }
	public bool LevelClear { get; set; }
	public override void _Ready()
	{
		TheSun = GetNode<DirectionalLight3D>("The_Sun");
		Node3D NPCs = GetNode<Node3D>("NPCs");
		Targets = new List<Pickup>{};
		TargetsMeshCount = new List<int>{};
		for (int i = 0; i < NPCs.GetChildCount(); i++)
		{
			Targets.Add(NPCs.GetChild<Pickup>(i));
			TargetsMeshCount.Add(NPCs.GetChild<Pickup>(i).Meshbodys.Count);
		}
	}

	public override void _Process(double delta)
	{
		if (LevelClear) { return; }
		for (int i = 0; i < Targets.Count; i++)
		{
			if (Targets[i].Meshbodys.Count == TargetsMeshCount[i])
			{
				return;
			}
		}
		LevelClear = true;
		TheSun.Visible = true;
	}
}
