using System;
using System.Runtime.InteropServices;

public static class User32
{
	
	public const uint MOD_ALT = 0x0001;
	public const uint MOD_CONTROL = 0x0002;
	public const uint MOD_SHIFT = 0x0004;
	public const uint MOD_WIN = 0x0008;
	public const uint MOD_NOREPEAT = 0x4000;
	
	public enum VKeys : uint
	{
		VK_A = 0x41,
		VK_Z = 0x5A,
		VK_F10 = 0x79,
	}
	
	public const int WM_HOTKEY = 0x0312;
	public const int WM_QUIT = 0x0012;
	public const uint WM_USER = 0x0400;
	
	public const uint PM_NOREMOVE = 0x0000;
	public const uint PM_REMOVE = 0x0001;
	public const uint PM_NOYIELD = 0x0002;
	
	[StructLayout(LayoutKind.Sequential)]
	public struct MSG
	{
		public IntPtr hwnd;
		public uint message;
		public IntPtr wParam;
		public IntPtr lParam;
		public uint time;
		public POINT pt;
	}
	
	[StructLayout(LayoutKind.Sequential)]
	public struct POINT
	{
		public int x;
		public int y;
	}
	
	[DllImport("user32.dll", SetLastError = true)]
	public static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);
	
	[DllImport("user32.dll", SetLastError = true)]
	public static extern bool UnregisterHotKey(IntPtr hWnd, int id);
	
	[DllImport("user32.dll")]
	public static extern int GetMessage(out MSG lpMsg, IntPtr hWnd, uint wMsgFilterMin, uint wMsgFilterMax);
	
	[DllImport("user32.dll")]
	public static extern IntPtr DispatchMessage(ref MSG lpmsg);
	
	[DllImport("user32.dll")]
	public static extern bool TranslateMessage(ref MSG lpMsg);
	
	[DllImport("user32.dll", SetLastError = true)]
	public static extern bool PeekMessage(out MSG lpMsg, IntPtr hWnd, uint wMsgFilterMin, uint wMsgFilterMax, uint uRemoveMsg);
	
	[DllImport("user32.dll", SetLastError = true)]
	public static extern bool PostThreadMessage(uint idThread, uint Msg, IntPtr wParam, IntPtr lParam);
	
	[DllImport("kernel32.dll")]
	public static extern uint GetCurrentThreadId();
}
