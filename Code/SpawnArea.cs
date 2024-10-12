using Godot;
using System;

public partial class SpawnArea : Area3D
{
	[Export]
	String PathToSpawnee { get; set; }
	PackedScene Spawnee { get; set; }
	
	public override void _Ready()
	{
		Spawnee = GD.Load<PackedScene>(PathToSpawnee);
	}

	public Pickup SpawnPickup()
	{
		return Spawnee.Instantiate<Pickup>();
	}
}
