extends Node

var hotkey_manager_cs = null

func _ready():
	print("[MainScene.gd] _ready() called.")
	
	hotkey_manager_cs = get_node("/root/Main/HotkeyManager")
	
	if hotkey_manager_cs != null:
		print("[MainScene.gd] HotkeyManager (C#) singleton found.")
		
		hotkey_manager_cs.connect("HotkeyTriggered", Callable(self, "_on_hotkey_triggered"))
		print("[MainScene.gd] Connected to HotkeyManager HotkeyTriggered signal.")
		
		if not hotkey_manager_cs.HotkeyListenerInstance.IsThreadRunningAndReady():
			push_error("MainScene.gd: Hotkey listener thread is not ready. Cannot proceed with hotkey registration.")
			return
			
		print("MainScene.gd: Hotkey listener thread is ready.")
		
		hotkey_manager_cs.RegisterHotkey("TestKey", 0x0002 | 0x0001, 0x50)
		hotkey_manager_cs.HotkeyListenerInstance.StartMessageLoop()
		print("MainScene.gd: All hotkeys registered. Message loop initiated.")
	else:
		push_error("HotkeyManager (C#) singleton not found! Please check Project Settings -> AutoLoad.")
		
	print("[MainScene.gd] _ready() finished.")

func _on_hotkey_triggered(hotkey_id: String):
	print("Godot (GDScript) received hotkey: " + hotkey_id)
	match hotkey_id:
		"TestKey":
			print("test")
		_:
			print("Unknown hotkey triggered (from GDScript).")
			
func _exit_tree():
	print("[MainScene.gd] _exit_tree() called.")
	if hotkey_manager_cs != null and hotkey_manager_cs.is_connected("HotkeyTriggered", Callable(self, "_on_hotkey_triggered")):
		hotkey_manager_cs.disconnect("HotkeyTriggered", Callable(self, "_on_hotkey_triggered"))
		print("[MainScene.gd] Disconnected from HotkeyManager HotkeyTriggered signal.")
