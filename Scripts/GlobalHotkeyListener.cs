using Godot;
using System;
using System.Runtime.InteropServices;
using System.Threading;
using System.ComponentModel;
using System.Collections.Concurrent;
using static User32;

public partial class GlobalHotkeyListener : Node, IDisposable
{
	private uint _threadId;
	private Thread _messageLoopThread;
	private bool _isRunning;
	private readonly ManualResetEvent _threadReadyEvent = new ManualResetEvent(false);
	private readonly ManualResetEvent _messageLoopStartEvent = new ManualResetEvent(false);
	
	private readonly ConcurrentQueue<(int id, uint modifiers, uint virtualKeyCode)> _registrationQueue = new ConcurrentQueue<(int, uint, uint)>();
	private readonly ConcurrentQueue<int> _unregistrationQueue = new ConcurrentQueue<int>();
	
	private readonly ManualResetEvent _registrationCompleteEvent = new ManualResetEvent(false);
	
	[Signal]
	public delegate void HotkeyTriggeredEventHandler(int hotkeyId);
	
	public override void _Ready()
	{
		GD.Print("[GlobalHotkeyListener] _Ready() called.");
		_isRunning = true;
		_messageLoopThread = new Thread(MessageLoop);
		_messageLoopThread.IsBackground = true;
		_messageLoopThread.Start();
	}
	
	private void MessageLoop()
	{
		_threadId = User32.GetCurrentThreadId();
		GD.Print($"[GlobalHotkeyListener] Hotkey listener: Message loop thread started. ID: {_threadId}");
		
		_threadReadyEvent.Set();
		
		GD.Print("[GlobalHotkeyListener] Waiting for message loop start signal...");
		bool started = _messageLoopStartEvent.WaitOne();
		GD.Print($"[GlobalHotkeyListener] Message loop start signal WaitOne returned: {started}.");
		if (!started)
		{
			GD.PrintErr("[GlobalHotkeyListener] Message loop start signal not received. Exiting.");
			_isRunning = false;
			return;
		}
		GD.Print("[GlobalHotkeyListener] Message loop start signal received. Entering message loop.");
		
		MSG msg;
		while (_isRunning)
		{
			ProcessRegistrationQueue();
			ProcessUnregistrationQueue();
			
			bool messageAvailable = User32.PeekMessage(out msg, IntPtr.Zero, 0, 0, User32.PM_REMOVE);
			
			if (messageAvailable)
			{
				if (msg.message == User32.WM_QUIT)
				{
					GD.Print($"[GlobalHotKeyListener] WM_QUIT received, but will continue for debug.");
				} else if (msg.message == User32.WM_HOTKEY)
				{
					int id = (int)msg.wParam;
					GD.Print($"[GlobalHotkeyListener] === WM_HOTKEY received! Hotkey ID: {id} (raw wParam: 0x{msg.wParam:X8}) ===");
					Godot.Callable.From(() =>
					{
						EmitSignal(SignalName.HotkeyTriggered, id);
					}).CallDeferred();
				}
				
				User32.TranslateMessage(ref msg);
				User32.DispatchMessage(ref msg);
			}
			else
			{
				Thread.Sleep(1);
			}
		}
		
		GD.Print($"[GlobalHotkeyListener] Message loop thread exiting.");
	}
	
	public bool RequestRegisterHotkey(int id, uint fsModifiers, uint vk)
	{
		if (!_threadReadyEvent.WaitOne(5000))
		{
			GD.PrintErr("Hotkey listener: Message loop thread not running or ready for registration request.");
			return false;
		}
		
		_registrationQueue.Enqueue((id, fsModifiers, vk));
		User32.PostThreadMessage(_threadId, User32.WM_USER + 1, IntPtr.Zero, IntPtr.Zero);
		return true;
	}
	
	private void ProcessRegistrationQueue()
	{
		while (_registrationQueue.TryDequeue(out var request))
		{
			GD.Print($"[GlobalHotkeyListener] Processing registration request for ID: {request.id}");
			bool result = User32.RegisterHotKey(IntPtr.Zero, request.id, request.modifiers, request.virtualKeyCode);
			if (!result)
			{
				int error = Marshal.GetLastWin32Error();
				GD.PrintErr($"[GlobalHotkeyListener] Failed to register hotkey with API (in loop). ID: {request.id}. Error: {error} ({new Win32Exception(error).Message})");
			}
			else
			{
				GD.Print($"[GlobalHotkeyListener] Hotkey ID {request.id} successfully registered in its own thread.");
			}
			_registrationCompleteEvent.Set();
		}
	}
	
	public bool RequestUnregisterHotkey(int id)
	{
		if (!_threadReadyEvent.WaitOne(5000))
		{
			GD.PrintErr("Hotkey listener: Message loop thread not running or ready for unregistration request.");
			return false;
		}
		_unregistrationQueue.Enqueue(id);
		User32.PostThreadMessage(_threadId, User32.WM_USER + 1, IntPtr.Zero, IntPtr.Zero);
		return true;
	}
	
	private void ProcessUnregistrationQueue()
	{
		while (_unregistrationQueue.TryDequeue(out var id))
		{
			GD.Print($"[GlobalHotkeyListener] Processing unregistration request for ID: {id}");
			bool result = User32.UnregisterHotKey(IntPtr.Zero, id);
			if (!result)
			{
				int error = Marshal.GetLastWin32Error();
				GD.PrintErr($"[GlobalHotkeyListener] Failed to unregister hotkey with API (in loop). ID: {id}. Error: {error} ({new Win32Exception(error).Message})");
			}
			else
			{
				GD.Print($"[GlobalHotkeyListener] Hotkey ID {id} successfully unregistered in its own thread.");
			}
		}
	}
	
	public bool IsThreadRunningAndReady()
	{
		return _threadReadyEvent.WaitOne(5000) && _isRunning;
	}
	
	public void StartMessageLoop()
	{
		_messageLoopStartEvent.Set();
	}
	
	public override void _ExitTree()
	{
		GD.PrintErr("[GlobalHotkeyListener] _ExitTree() called. Initialing dispose process.");
		Dispose();
	}
	
	public new void Dispose()
	{
		GD.PrintErr("[GlobalHotkeyListener] Dispose() called. Setting _isRunning to false.");
		_isRunning = false;
		_messageLoopStartEvent.Set();
		
		if (_messageLoopThread != null && _messageLoopThread.IsAlive)
		{
			if (_threadId != 0 && User32.PostThreadMessage(_threadId, User32.WM_QUIT, IntPtr.Zero, IntPtr.Zero))
			{
				_messageLoopThread.Join(500);
			}
			else
			{
				int error = Marshal.GetLastWin32Error();
				GD.PrintErr($"Hotkey Listener: Failed to post WM_QUIT message. Error: {error} ({new Win32Exception(error).Message})");
			}
		}
		_messageLoopThread = null;
		_threadReadyEvent.Dispose();
		_messageLoopStartEvent.Dispose();
		_registrationCompleteEvent.Dispose();
	}
}
