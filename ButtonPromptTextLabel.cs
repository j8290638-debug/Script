using Godot;
using Godot.Collections;
using System;

public partial class ButtonPromptTextLabel : Node
{
	public PlayerController Player;
	[Export(PropertyHint.MultilineText)] public string Text = "";
	[Export] public ColorRect ColorRect;
	[Export] public RichTextLabel RichTextLabel;
	[Export] public OnePlayerUI PlayerUI;


	[Export] public Dictionary<string, string> ControllerButtonToImageMapping = new Dictionary<string, string>();
	public override void _Ready()
	{
		//RichTextLabel.Visible = false;
		if (PlayerUI != null)
		{
			Player = PlayerController.Instances[PlayerUI.PlayerID];
			Player.PlayerInput.OnInputTypeChange += HandleInputTag;
        }
	}

    public override void _ExitTree()
    {
        if (Player != null)
        {
            Player.PlayerInput.OnInputTypeChange -= HandleInputTag;
        }
    }

	public void OnBodyEnter(Node3D body)
	{
		var player = body.GetNodeOrNull<PlayerController>(".");
		if (player != null && !player.NpcPartnerControl.IsNpc)
		{
			this.Player = player;
			RichTextLabel.Visible = true;

			HandleInputTag();
			if (ColorRect != null)
			{
				ColorRect.Size = RichTextLabel.Size;
				ColorRect.GlobalPosition = RichTextLabel.GlobalPosition;
				ColorRect.SetAnchorsPreset(RichTextLabel.LayoutPreset.FullRect, true);
			}
		}

	}

	public void OnBodyExit(Node3D body)
	{
		var player = body.GetNodeOrNull<PlayerController>(".");
		if (player == this.Player)
		{
			this.Player = null;
			RichTextLabel.Visible = false;
		}

	}

	public void HandleInputTag()
	{
		string startTag = "[input]";
		string endTag = "[/input]";
		string newText = this.Text;
		RichTextLabel.Text = "";
		if (this.Text.Length == 0) return;
		while (true)
		{
			var startTagPos = newText.FindN(startTag);
			var endTagPos = newText.FindN(endTag);
			if (startTagPos == -1 && endTagPos == -1)
			{
				break;
			}
			if (startTagPos != -1 && endTagPos == -1)
			{
				throw new ArgumentException("Unterminated [input] tag");
			}
			if (startTagPos == -1 && endTagPos != -1)
			{
				throw new ArgumentException("Found closing [/input] tag without opening [input]");
			}
			var preText = newText.Substring(0, startTagPos);
			var insideText = newText.Substring(startTagPos + startTag.Length, endTagPos - (startTagPos + startTag.Length));
			var postText = newText.Substring(endTagPos + endTag.Length);


			string inputText = GetInputText(insideText);

			newText = preText + inputText + postText;
		}
		RichTextLabel.Text = newText;
	}

	string GetInputText(string action)
	{
		string keyboardText = "";
		string padText = "";
		string touchText = "";


		var identifier = Player.PlayerInput.PlayerIdentifier;
		if ((identifier == "any" || identifier.StartsWith("pad")) && Input.GetConnectedJoypads().Count > 0)
		{
			var events = InputMap.ActionGetEvents(identifier + "_" + action);
			if (events.Count > 0)
			{
				var ev = events[0];
				int padNum = 0;
				if (identifier.StartsWith("pad"))
				{
					var padNumStr = identifier.Substring(3).Split("_")[0];
					padNum = Int32.Parse(padNumStr);
				}

				var joyName = Input.GetJoyName(padNum).ToLower();
				if (joyName.Length == 0)
				{
					return action;  // How did this happen? Joypad without name? This value is a fallback.
				}

				var eventText = ev.AsText();
				var eventTextSplit = eventText.Split(new string[] { "(", ")", "," }, StringSplitOptions.TrimEntries);
                // Special case D-pad mappings because they don't follow the same naming convention, eg. "Joypad Button 13 (D-pad Left)"
                if (eventTextSplit.Length < 5)
				{
					eventTextSplit = new string[]{
                        eventTextSplit[0],	// Joypad Button 13
                        eventTextSplit[1],	// D-Pad Left
						eventTextSplit[1],	// D-Pad Left
						eventTextSplit[1],	// D-Pad Left
						eventTextSplit[1],	// D-Pad Left
					};
				}

				var numberName = eventTextSplit[0];
				var genericName = eventTextSplit[1];
				var playstationName = eventTextSplit[2];
				var xboxName = eventTextSplit[3];
				var nintendoName = eventTextSplit[4];


				// Please refer to https://github.com/mdqinc/SDL_GameControllerDB/blob/master/gamecontrollerdb.txt for controller names.
				if (joyName.Contains("xbox") || joyName.Contains("xinput"))
				{
					var xboxText = xboxName.Split(" ")[1];  // First part contains "Xbox"
					if (xboxName.Split(" ")[0].ToLower() == "d-pad")
					{
						xboxText = xboxName;
					}

					if (ControllerButtonToImageMapping.ContainsKey("Xbox_"+xboxText.ToUpper()))
					{
						padText = "[img]" + ControllerButtonToImageMapping["Xbox_" + xboxText.ToUpper()] + "[/img]";
					}
					else
					{
						switch (xboxText.ToUpper())
						{
							case "A":
								padText = "[color=green]A[/color]";
								break;
							case "B":
								padText = "[color=red]B[/color]";
								break;
							case "X":
								padText = "[color=blue]X[/color]";
								break;
							case "Y":
								padText = "[color=yellow]Y[/color]";
								break;
                            case "D-PAD LEFT":
                                padText = "🡄";
                                break;
                            case "D-PAD RIGHT":
                                padText = "🡆";
                                break;
                            case "D-PAD UP":
                                padText = "🡅";
                                break;
                            case "D-PAD DOWN":
                                padText = "🡇";
                                break;
                            default:
								padText = xboxText;
								break;
						}
					}
				}
				else if (joyName.Contains("sony") || joyName.Contains("dualshock") || joyName.Contains("playstation") || joyName.Contains("ps"))
				{
					var psName = playstationName.Split(" ")[1];  // First part contains "Sony";
                    if (playstationName.Split(" ")[0].ToLower() == "d-pad")
                    {
                        psName = playstationName;
                    }
                    if (ControllerButtonToImageMapping.ContainsKey("PS_" + psName.ToUpper()))
					{
						padText = "[img]" + ControllerButtonToImageMapping["PS_" + psName.ToUpper()] + "[/img]";
					}
					else
					{
						switch (psName.ToLower())
						{
							case "cross":
								padText = "[color=blue]×[/color]";
								break;
							case "triangle":
								padText = "[color=green]△[/color]";
								break;
							case "circle":
								padText = "[color=red]○[/color]";
								break;
							case "square":
								padText = "[color=pink]□[/color]";
								break;
                            case "D-PAD LEFT":
                                padText = "🡄";
                                break;
                            case "D-PAD RIGHT":
                                padText = "🡆";
                                break;
                            case "D-PAD UP":
                                padText = "🡅";
                                break;
                            case "D-PAD DOWN":
                                padText = "🡇";
                                break;
                            default:
								padText = psName;
								break;
						}
					}
				}
				else if (joyName.Contains("nintendo") || joyName.Contains("wii") || joyName.Contains("gamecube") || joyName.Contains("switch"))
				{
					padText = nintendoName.Split(" ")[1];  // First part contains "Nintendo";
				}
				else
				{
					padText = genericName.Split(" ")[0] + " Button (" + numberName.Split(" ", StringSplitOptions.TrimEntries)[2] + ")";   // Second part contains "Action"
				}
			}
		}
		if (identifier == "any" || identifier == "kb")
		{
			if (OS.GetName() == "Android" || OS.GetName() == "iOS")
			{
				if (ControllerButtonToImageMapping.ContainsKey("touch_" + action))
				{
					touchText = "[img]" + ControllerButtonToImageMapping["touch_" + action] + "[/img]";
				}
				else if (action.Contains("_actioncharaswap_"))
				{
					touchText = "";	// We're skipping it because the text itself is an icon
				}
			}
			else
			{
				keyboardText = OptionsMenu.Options.KeyboardInput["kb_" + action].ToString();
			}
		}

		//if (keyboardText.Length > 0 && padText.Length > 0)
		//{
		//	return keyboardText + "/" + padText;
		//}
		//else if (keyboardText.Length > 0)
		//{
		//	return keyboardText;
		//}
		//else if (padText.Length > 0)
		//{
		//	return padText;
		//}
		
		switch (Player.PlayerInput.LastInputType)
		{
			case PlayerInput.InputType.KEYBOARD_AND_MOUSE:
				return keyboardText;
			case PlayerInput.InputType.PAD:
				return padText;
			case PlayerInput.InputType.TOUCHSCREEN:
				return touchText;
		}

		// Something went wrong here. This means no input mapping.
		GD.PrintErr("Couldn't find mapping for action {" + action + "} for neither keyboard nor joypad. Returning action name as a fallback.");
		return action;
	}

}
