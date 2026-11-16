using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;

namespace StickFigure;

public partial class DesktopOverlayWindow : Window
{
    private readonly List<ProceduralStickman> _stickmen = new();
    private readonly AgentWorld _world = new();
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
        Loaded += OnOverlayLoaded;
        Closed += (_, _) => { foreach (var stickman in _stickmen) stickman.Dispose(); _stickmen.Clear(); };
    }

    private void OnSourceInitialized(object? sender, EventArgs e)
    {
        var handle = new WindowInteropHelper(this).Handle;
        var style = GetWindowLongPtr(handle, GwlExStyle).ToInt64();
        SetWindowLongPtr(handle, GwlExStyle, new IntPtr(style | WsExTransparent | WsExToolWindow | WsExLayered));
        _source = HwndSource.FromHwnd(handle);
        _source?.AddHook(WindowProc);
        RegisterHotKey(handle, HotkeyId, ModControl | ModShift, VkF12);
    }

    private void OnOverlayLoaded(object sender, RoutedEventArgs e)
    {
        var colors = new[] { Color.FromRgb(235,243,250), Color.FromRgb(0,212,255), Color.FromRgb(0,255,153), Color.FromRgb(255,190,90) };
        var width = OverlayCanvas.ActualWidth;
        for (int i = 0; i < 4; i++)
        {
            var x = width * (i + 1) / 5.0;
            _stickmen.Add(new ProceduralStickman(OverlayCanvas, startX: x, walkSpeed: 48 + i * 17, personalityOffset: i, color: colors[i], world: _world));
        }
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