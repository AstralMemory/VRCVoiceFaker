using Godot;
using System;
using static User32;

public partial class MainScene : Node
{
	private HotkeyManager _hotkeyManager;
	
	public override void _Ready()
	{
		_hotkeyManager = GetNode<HotkeyManager>("/root/Main/HotkeyManager");
		
		if (_hotkeyManager != null)
		{
			_hotkeyManager.HotkeyTriggered += OnHotkeyTriggerTriggered;
			
			bool registered1 = _hotkeyManager.RegisterHotkey("ToggleSomething", MOD_CONTROL | MOD_ALT, (uint)VKeys.VK_A);
			if (registered1)
			{
				GD.Print("Ctrl+Alt+A Hotkey registered.");
			}
			
			bool registered2 = _hotkeyManager.RegisterHotkey("ShowDebugInfo", MOD_SHIFT, (uint)VKeys.VK_Z);
			if (registered2)
			{
				GD.Print("Shift+Z Hotkey registered.");
			}
			
			bool registered3 = _hotkeyManager.RegisterHotkey("PerformActionF10", 0, (uint)VKeys.VK_F10);
			if (registered3)
			{
				GD.Print("F10 Hotkey registered.");
			}
			_hotkeyManager.HotkeyListenerInstance.StartMessageLoop();
		}
		else
		{
			GD.PrintErr("HotkeyManager not found!");
		}
		GD.Print("[MainScene] _Ready() finished.");
	}
	
	private void OnHotkeyTriggerTriggered(string hotkeyId)
	{
		GD.Print($"Godot received hotkey: {hotkeyId}");
		switch (hotkeyId)
		{
			case "ToggleSomething":
				GD.Print("Ctrl+Alt+A was pressed! Toggling something...");
				break;
			case "ShowDebugInfo":
				GD.Print("Shift+Z was pressed! Displaying debug information...");
				break;
			case "PerformActionF10":
				GD.Print("F10 was pressed! Performing action...");
				break;
			default:
				GD.Print("Unknown hotkey triggered.");
				break;
		}
	}
	
	public override void _ExitTree()
	{
		if (_hotkeyManager != null)
		{
			_hotkeyManager.HotkeyTriggered -= OnHotkeyTriggerTriggered;
		}
	}
}
