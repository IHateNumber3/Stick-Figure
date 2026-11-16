using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;

namespace StickFigure;

public partial class DesktopOverlayWindow : Window
{
    private ProceduralStickman? _stickman;
    private HwndSource? _source;
    private const int HotkeyId = 0x51F1;
    private const int WmHotkey = 0x0312;
    private const uint ModControl = 0x0002, ModShift = 0x0004;
    private const uint VkF12 = 0x7B;
    private const int GwlExStyle = -20;
    private const int WsExTransparent = 0x20, WsExToolWindow = 0x80, WsExLayered = 0x80000;

    [DllImport("user32.dll")] private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint modifiers, uint vk);
    [DllImport("user32.dll")] private static extern bool UnregisterHotKey(IntPtr hWnd, int id);
    [DllImport("user32.dll", EntryPoint = "GetWindowLongPtr")] private static extern IntPtr GetWindowLongPtr(IntPtr hWnd, int index);
    [DllImport("user32.dll", EntryPoint = "SetWindowLongPtr")] private static extern IntPtr SetWindowLongPtr(IntPtr hWnd, int index, IntPtr value);

    public DesktopOverlayWindow()
    {
        InitializeComponent();
        Left = SystemParameters.VirtualScreenLeft;
        Top = SystemParameters.VirtualScreenTop;
        Width = SystemParameters.VirtualScreenWidth;
        Height = SystemParameters.VirtualScreenHeight;
        SourceInitialized += OnSourceInitialized;
        Closed += (_, _) => _stickman?.Dispose();
    }

    private void OnSourceInitialized(object? sender, EventArgs e)
    {
        var handle = new WindowInteropHelper(this).Handle;
        var style = GetWindowLongPtr(handle, GwlExStyle).ToInt64();
        SetWindowLongPtr(handle, GwlExStyle, new IntPtr(style | WsExTransparent | WsExToolWindow | WsExLayered));
        _source = HwndSource.FromHwnd(handle);
        _source?.AddHook(WindowProc);
        RegisterHotKey(handle, HotkeyId, ModControl | ModShift, VkF12);
        _stickman = new ProceduralStickman(OverlayCanvas);
    }

    private IntPtr WindowProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (msg == WmHotkey && wParam.ToInt32() == HotkeyId)
        {
            handled = true;
            Close();
        }
        return IntPtr.Zero;
    }

    protected override void OnClosed(EventArgs e)
    {
        var handle = new WindowInteropHelper(this).Handle;
        UnregisterHotKey(handle, HotkeyId);
        _source?.RemoveHook(WindowProc);
        base.OnClosed(e);
    }
}