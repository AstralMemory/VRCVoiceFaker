using Godot;
using System;
using System.Collections.Generic;

public partial class HotkeyManager : Node
{
	private GlobalHotkeyListener _hotkeyListener;
	private static int _nextHotkeyId = 1;
	private readonly Dictionary<int, string> _registeredHotkeys = new Dictionary<int, string>();
	
	[Signal]
	public delegate void HotkeyTriggeredEventHandler(string hotkeyId);
	
	public GlobalHotkeyListener HotkeyListenerInstance => _hotkeyListener;
	
	public override void _Ready()
	{
		GD.Print("[HotkeyManager] _Ready() called.");
		_hotkeyListener = new GlobalHotkeyListener();
		AddChild(_hotkeyListener);
		
		_hotkeyListener.Connect(GlobalHotkeyListener.SignalName.HotkeyTriggered, Callable.From((int id) =>
		{
			OnHotkeyTriggerTriggeredFromListener(id);
		}));
	}
	
	public bool RegisterHotkey(string hotkeyId, uint modifiers, uint virtualKeyCode)
	{
		if (!_hotkeyListener.IsThreadRunningAndReady())
		{
			GD.PrintErr($"HotkeyManager: Hotkey Listener is not ready for registration. Cannot register hotkey '{hotkeyId}'.");
			return false;
		}
		
		if (_registeredHotkeys.ContainsValue(hotkeyId))
		{
			GD.PrintErr($"Hotkey ID '{hotkeyId}' already registered.");
			return false;
		}
		
		int id = _nextHotkeyId++;
		if (_hotkeyListener.RequestRegisterHotkey(id, modifiers, virtualKeyCode))
		{
			_registeredHotkeys.Add(id, hotkeyId);
			GD.Print($"Hotkey '{hotkeyId}' (ID: {id}) registered successfully.");
			return true;
		}
		else
		{
			GD.PrintErr($"Failed to register hotkey '{hotkeyId}'.");
			return false;
		}
	}
	
	public bool UnregisterHotkey(string hotkeyId)
	{
		if (!_registeredHotkeys.ContainsValue(hotkeyId))
		{
			GD.PrintErr($"Hotkey ID '{hotkeyId}' not registered.");
			return false;
		}
		
		int idToRemove = -1;
		foreach (var entry in _registeredHotkeys)
		{
			if (entry.Value == hotkeyId)
			{
				idToRemove = entry.Key;
				break;
			}
		}
		
		if (idToRemove != -1)
		{
			if (_hotkeyListener.RequestUnregisterHotkey(idToRemove))
			{
				_registeredHotkeys.Remove(idToRemove);
				GD.Print($"Hotkey '{hotkeyId}' (ID: {idToRemove}) unregistered successfully.");
				return true;
			}
			else
			{
				GD.PrintErr("Failed to unregister hotkey '{hotkeyId}'.");
				return false;
			}
		}
		return false;
	}
	
	private void OnHotkeyTriggerTriggeredFromListener(int id)
	{
		GD.Print($"[HotkeyManager] Received raw hotkey ID from listener: {id}");
		
		if (_registeredHotkeys.TryGetValue(id, out string hotkeyIdString))
		{
			EmitSignal(SignalName.HotkeyTriggered, hotkeyIdString);
		}
		else
		{
			GD.PrintErr($"[HotkeyManager] Received unknown hotkey ID: {id}");
		}
	}
	
	public override void _ExitTree()
	{
		GD.PrintErr("[HotkeyManager] _ExitTree() called. Disposing hotkeys.");
		foreach (var id in _registeredHotkeys.Keys)
		{
			_hotkeyListener.RequestUnregisterHotkey(id);
		}
		_registeredHotkeys.Clear();
	}
}
