extends Panel

var dragging = false
var drag_offset = Vector2.ZERO

var IPAddress
var PORT
var AudioVolume

func _gui_input(event: InputEvent):
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

func _ready():
	if !FileAccess.file_exists("user://config.json"):
		var config_data = {
			"ip_address": "127.0.0.1",
			"port": "5000",
			"volume": 0.0
		}
		var f = FileAccess.open("user://config.json", FileAccess.WRITE)
		f.store_string(JSON.stringify(config_data, " ", false))
		f.flush()
		f.close()
		IPAddress = "127.0.0.1"
		PORT = "5000"
		AudioVolume = 0.0
		update_display_percentage(AudioVolume)
	else:
		load_config()

func load_config():
	var f = FileAccess.open("user://config.json", FileAccess.READ)
	var data = f.get_as_text()
	f.close()
	var config_data = JSON.parse_string(data)
	IPAddress = config_data['ip_address']
	PORT = config_data['port']
	AudioVolume = config_data['volume']
	update_display_percentage(AudioVolume)
	$IPAddress/IPAddress_Edit.text = IPAddress
	$PORT/PORT_Edit.text = PORT

func _on_close_pressed():
	self.visible = false


func _on_save_pressed() -> void:
	$Save.disabled = true
	DirAccess.remove_absolute("user://config.json")
	var config_data = {
		"ip_address": $IPAddress/IPAddress_Edit.text,
		"port": $PORT/PORT_Edit.text,
		"volume": $AudioLevel/Volume.value
	}
	var f = FileAccess.open("user://config.json", FileAccess.WRITE)
	f.store_string(JSON.stringify(config_data, " ", false))
	f.flush()
	f.close()
	load_config()


func _on_ip_address_edit_text_changed(new_text: String) -> void:
	if new_text != "":
		$Save.disabled = false
	else:
		$Save.disabled = true
		


func _on_port_edit_text_changed(new_text: String) -> void:
	if new_text != "":
		$Save.disabled = false
	else:
		$Save.disabled = true


func _on_volume_value_changed(value: float) -> void:
	$Save.disabled = false
	update_display_percentage(value)
	
func update_display_percentage(value: float):
	var min_val = -80
	var max_val = 24
	var range_size = max_val - min_val
	var value_from_min = value - min_val
	var percentage = value_from_min / range_size
	var display_percentage = round(percentage * 100)
	$AudioLevel/VolumePercent.text = str(display_percentage) + "%"
