using Godot;
using System;

public partial class World : Node3D
{
	//This code is apparently needed to use VR for the start of a scene
	XRInterface TheInterface { get; set; }
	public override void _Ready()
	{
		TheInterface = XRServer.FindInterface("OpenXR");
		if (TheInterface != null && TheInterface.IsInitialized())
		{
			DisplayServer.WindowSetVsyncMode(DisplayServer.VSyncMode.Disabled);
			GetViewport().UseXR = true;
		}
	}
}
