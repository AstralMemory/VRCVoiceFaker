using Godot;
using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

public partial class MousePassThrough : Node
{
	// Windows API の winuser.h で定義されている関数や定数を利用して、
	// Godot のウィンドウに対して「マウスイベントを透過させる」などの特殊な挙動を実現するクラスです。

	// 現在アクティブなウィンドウのハンドル（HWND）を取得します。
	[DllImport("user32.dll")]
	private static extern IntPtr GetActiveWindow(); // GetActiveWindow() はウィンドウのハンドルを取得します。

	// ウィンドウの属性（スタイル）を変更します。ここでは拡張ウィンドウスタイルを設定するために使用します。
	[DllImport("user32.dll")]
	private static extern int SetWindowLong(IntPtr hWnd, int nIndex, uint dwNewLong);
	// SetWindowLong() はウィンドウに関連付けられた特定のフラグ値を変更します。
	// hWnd: 対象ウィンドウのハンドル
	// nIndex: 変更する属性のインデックス（ここでは GWL_EXSTYLE で拡張スタイル）
	// dwNewLong: 設定する新しい値（フラグの組み合わせ）

	// レイヤードウィンドウの属性（透過色や不透明度）を設定します。
	[DllImport("user32.dll")]
	static extern int SetLayeredWindowAttributes(IntPtr hWnd, uint crKey, byte bAlpha, uint dwFlags); 
	// crKey: 透過色として使う色（0 なら無効）
	// bAlpha: ウィンドウ全体の不透明度（0=完全透明, 255=完全不透明）
	// dwFlags: LWA_COLORKEY などのフラグ

	// ウィンドウの位置やサイズ、Zオーダー（前後関係）を変更します。
	[DllImport("user32.dll")]
	static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags); 
	// hWndInsertAfter: HWND_TOPMOST などで最前面に固定
	// uFlags: SWP_NOMOVE などで位置やサイズを変更しない指定も可能

	// 指定したウィンドウをフォアグラウンド（最前面）にします。
	[DllImport("user32.dll")]
	static extern bool SetForegroundWindow(IntPtr hWnd); 

	// --- 定数定義 ---
	// SetWindowLong で使う拡張ウィンドウスタイルのインデックス
	const int GWL_EXSTYLE = -20; // 拡張ウィンドウスタイルを設定します。

	// レイヤードウィンドウ（不透明度や透過色をサポートするウィンドウ）にするフラグ
	const int WS_EX_LAYERED = 0x00080000; // レイヤードウィンドウにする

	// ウィンドウをマウスイベント透過にするフラグ
	const int WS_EX_TRANSPARENT = 0x00000020; // マウスイベントを下のウィンドウに通す

	// ウィンドウを常に最前面にするフラグ
	const int WS_EX_TOPMOST = 0x000000008; // 常に最前面

	// SetLayeredWindowAttributes で使う透過色指定フラグ
	const int LWA_COLORKEY = 1; // crKey を透過色として使用

	// SetWindowPos で使う「最前面ウィンドウ」指定用のハンドル
	static readonly IntPtr HWND_TOPMOST = new IntPtr(-1); // 常に最前面

	// SetWindowPos で使う各種フラグ
	const UInt32 SWP_NOSIZE = 0x0001; // サイズを変更しない
	const UInt32 SWP_NOMOVE = 0x0002; // 位置を変更しない
	const UInt32 SWP_SHOWWINDOW = 0x0040; // ウィンドウを表示
	const UInt32 SWP_NOZORDER = 0x0004; // Zオーダーを変更しない
	const UInt32 SWP_FRAMECHANGED = 0x0020; // フレームスタイルの変更を反映

	// --- メンバー変数 ---
	IntPtr hWnd; // このウィンドウのハンドル

	// Godot のノード初期化時に呼ばれる関数
	public override void _Ready()
	{
		// 1. 現在アクティブなウィンドウのハンドルを取得
		hWnd = GetActiveWindow();

		// 2. ウィンドウの拡張スタイルを「レイヤードウィンドウ」に設定
		//    ※ここで WS_EX_TRANSPARENT を追加するとマウスイベント透過になる
		SetWindowLong(hWnd, GWL_EXSTYLE, WS_EX_LAYERED);

		// 3. レイヤードウィンドウの属性を設定（ここでは透過色キーを0に）
		//    bAlpha=0 なので完全透明、LWA_COLORKEY で色キー透過
		SetLayeredWindowAttributes(hWnd, 0, 0, LWA_COLORKEY);

		// 4. ウィンドウを最前面にし、位置・サイズは変更せず、フレームスタイルを反映
		SetWindowPos(hWnd, HWND_TOPMOST, 0, 0, 0, 0, 
			SWP_NOMOVE | SWP_NOSIZE | SWP_NOZORDER | SWP_FRAMECHANGED | SWP_SHOWWINDOW);
	}
}
