using Godot;
using System;

public partial class StepsPlayer : Node
{
	private PlayerSkinController PlayerSkinController;
	[Export] public AudioStreamPlayer3D AudioStreamPlayer;

	[Export] public AudioStream LeftStepDefaultSound;
	[Export] public AudioStream RightStepDefaultSound;

	[Export] public AudioStream LeftStepWaterSound;
	[Export] public AudioStream RightStepWaterSound;

	private WaterInteraction WaterInteraction;

	public override void _Ready()
	{
		if (PlayerSkinController == null)
		{
			PlayerSkinController = this.GetNodeOrNull<PlayerSkinController>("..");
		}
		if (PlayerSkinController != null && WaterInteraction == null)
		{
			WaterInteraction = PlayerSkinController.PlayerController.GetNodeOrNull<WaterInteraction>("./PlayerControl/WaterInteraction");
		}
	}

	public void PlayLeftStep()
	{
		AudioStreamPlayer.Stream = GetLeftStepSound();
		AudioStreamPlayer.Play(0);
	}
	public void PlayRightStep()
	{
		AudioStreamPlayer.Stream = GetRightStepSound();
		AudioStreamPlayer.Play(0);
	}

	private AudioStream GetLeftStepSound()
	{
		if (WaterInteraction != null && WaterInteraction.WaterFeetCounter > 0)
		{
			return LeftStepWaterSound;
		}
		return LeftStepDefaultSound;
	}

	private AudioStream GetRightStepSound()
	{
		if (WaterInteraction != null && WaterInteraction.WaterFeetCounter > 0)
		{
			return RightStepWaterSound;
		}
		return RightStepDefaultSound;
	}
}
