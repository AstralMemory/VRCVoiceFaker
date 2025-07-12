extends Panel

var dragging = false
var drag_offset = Vector2.ZERO

var osc: bool = false
var legacy: bool = false
var hotkey_manager = null

@onready var ChatText = $Chat
@onready var Voice: AudioStreamPlayer = $Voice
@onready var god_osc = $OSCClient
@onready var config = get_node("/root/Control/ConfigPanel")

func _ready():
	hotkey_manager = $HotkeyManager
	if hotkey_manager != null:
		hotkey_manager.connect("HotkeyTriggered", Callable(self, "_on_hotkey_triggered"))
		if not hotkey_manager.HotkeyListenerInstance.IsThreadRunningAndReady():
			push_error("Hotkey listener thread is not ready. Cannot proceed with hotkey registration.")
			return
		hotkey_manager.RegisterHotkey("TogglePanel", 0x0002, 0x50) #Ctrl+P
		hotkey_manager.RegisterHotkey("QuitApp", 0x0002 | 0x0001, 0x43) #Ctrl+Alt+C
		hotkey_manager.HotkeyListenerInstance.StartMessageLoop()
	else:
		push_error("singleton not found! Please check Project Settings -> AutoLoad.")

func _gui_input(event: InputEvent) -> void:
	if event is InputEventMouseButton:
		if event.button_index == MOUSE_BUTTON_LEFT:
			if event.pressed:
				dragging = true
				drag_offset = get_global_mouse_position() - global_position
			else:
				dragging = false
	elif event is InputEventMouseMotion:
		if dragging:
			global_position = get_global_mouse_position() - drag_offset
			accept_event()

func _on_send_pressed() -> void:
	var IPAddress = config.IPAddress
	var PORT = config.PORT
	$Send.disabled = true
	ChatText.editable = false
	if len(ChatText.text) <= 100 or ChatText.text != "":
		if legacy:
			DisplayServer.tts_stop()
			var voices = DisplayServer.tts_get_voices_for_language("ja")
			var voice_id = voices[0]
			DisplayServer.tts_speak(ChatText.text, voice_id)
			ChatText.text = ""
			$Send.disabled = false
			ChatText.editable = true
		else:
			var http_request = HTTPRequest.new()
			http_request.name = "VoiceRequest"
			http_request.connect("request_completed", Callable(self, "_on_request_completed"))
			add_child(http_request)
			var url = "http://" + IPAddress + ":" + PORT + "/voice?text=" + ChatText.text.uri_encode()
			var header = ["accept: audio/wav"]
			http_request.request(url, header, HTTPClient.METHOD_GET, "")
	else:
		var ErrorDialog = AcceptDialog.new()
		ErrorDialog.title = "エラー"
		ErrorDialog.dialog_text = "文字数が100を超えてます。"
		ErrorDialog.popup_centered()

func _on_request_completed(_result, responce_code, _headers, body):
	get_node("/root/Control/Main/VoiceRequest").queue_free()
	if responce_code == 200:
		var audio = AudioStreamWAV.load_from_buffer(body)
		Voice.stream = audio
		Voice.volume_db = config.AudioVolume
		Voice.play()
		if osc:
			var address = "/chatbox/typing"
			var args = [false]
			god_osc.send_message(address, args)
			address = "/chatbox/input"
			args = [ChatText.text, true, false]
			god_osc.send_message(address, args)
		$Send.disabled = false
		ChatText.text = ""
		ChatText.editable = true
		await Voice.finished
		Voice.stream = null
	else:
		var ErrorDialog = AcceptDialog.new()
		ErrorDialog.title = "エラー"
		ErrorDialog.dialog_text = "APIサーバーにつながりませんでした。"
		ErrorDialog.popup_centered()

func _on_osc_toggle_toggled(toggled_on: bool) -> void:
	if toggled_on:
		osc = true
	else:
		osc = false

func _on_legacy_mode_toggled(toggled_on: bool) -> void:
	if toggled_on:
		legacy = true
	else:
		legacy = false

func _on_chat_text_changed() -> void:
	if ChatText.text != "":
		$Send.disabled = false
		if osc:
			var address = "/chatbox/typing"
			var args = [true]
			god_osc.send_message(address, args)
		if Input.is_action_pressed("SendText"):
			_on_send_pressed()
	else:
		$Send.disabled = true
		if osc:
			var address = "/chatbox/typing"
			var args = [false]
			god_osc.send_message(address, args)

func _on_hotkey_triggered(hotkey_id: String):
	match hotkey_id:
		"TogglePanel":
			self.visible = !self.visible
		"QuitApp":
			get_tree().quit()

func _exit_tree():
	if hotkey_manager != null and hotkey_manager.is_connected("HotkeyTriggered", Callable(self, "_on_hotkey_triggered")):
		hotkey_manager.disconnect("HotkeyTriggered", Callable(self, "_hotkey_triggered"))


func _on_config_pressed() -> void:
	config.visible = true
