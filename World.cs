using Godot;
using System;

public partial class World : Node3D
{
	//This code is apparently needed to use VR for the start of a scene
	XRInterface theInterface;
	public override void _Ready()
	{
		theInterface = XRServer.FindInterface("OpenXR");
		if (theInterface != null && theInterface.IsInitialized())
		{
			DisplayServer.WindowSetVsyncMode(DisplayServer.VSyncMode.Disabled);
			GetViewport().UseXR = true;
		}
	}
}
