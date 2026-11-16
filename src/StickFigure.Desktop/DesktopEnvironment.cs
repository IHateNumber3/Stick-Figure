using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;

namespace StickFigure;

/// <summary>
/// Read-only snapshot of visible top-level Windows. Coordinates are converted
/// to the overlay's virtual-screen coordinate space. No window content is read.
/// </summary>
public sealed class DesktopEnvironment
{
    private const int GwlExStyle = -20;
    private const long WsExToolWindow = 0x00000080L;
    private readonly int _ownProcessId = Environment.ProcessId;
    private readonly List<DesktopSurface> _surfaces = new();

    public IReadOnlyList<DesktopSurface> Surfaces => _surfaces;
    public DateTime LastRefreshUtc { get; private set; }

    public void Refresh()
    {
        _surfaces.Clear();
        var bounds = new NativeRect
        {
            Left = (int)System.Windows.SystemParameters.VirtualScreenLeft,
            Top = (int)System.Windows.SystemParameters.VirtualScreenTop,
            Right = (int)(System.Windows.SystemParameters.VirtualScreenLeft + System.Windows.SystemParameters.VirtualScreenWidth),
            Bottom = (int)(System.Windows.SystemParameters.VirtualScreenTop + System.Windows.SystemParameters.VirtualScreenHeight)
        };

        EnumWindows((hwnd, _) =>
        {
            if (!IsWindowVisible(hwnd) || IsIconic(hwnd)) return true;
            GetWindowThreadProcessId(hwnd, out uint processId);
            if (processId == _ownProcessId) return true;
            long style = GetWindowLongPtr(hwnd, GwlExStyle).ToInt64();
            if ((style & WsExToolWindow) != 0) return true;
            if (!GetWindowRect(hwnd, out var rect)) return true;

            rect.Left = Math.Max(rect.Left, bounds.Left);
            rect.Top = Math.Max(rect.Top, bounds.Top);
            rect.Right = Math.Min(rect.Right, bounds.Right);
            rect.Bottom = Math.Min(rect.Bottom, bounds.Bottom);
            if (rect.Right - rect.Left < 140 || rect.Bottom - rect.Top < 90) return true;

            var title = new StringBuilder(256);
            _ = GetWindowText(hwnd, title, title.Capacity);
            string processName = "Application";
            try { processName = Process.GetProcessById((int)processId).ProcessName; } catch { }

            _surfaces.Add(new DesktopSurface(
                hwnd,
                title.ToString(),
                processName,
                rect.Left - bounds.Left,
                rect.Top - bounds.Top,
                rect.Right - bounds.Left,
                rect.Bottom - bounds.Top));
            return true;
        }, IntPtr.Zero);

        LastRefreshUtc = DateTime.UtcNow;
    }

    public DesktopSurface? FindNearestSurface(double x, double maxDistance = 1000)
    {
        DesktopSurface? best = null;
        double bestDistance = maxDistance;
        foreach (var surface in _surfaces)
        {
            double distance = x < surface.Left ? surface.Left - x :
                x > surface.Right ? x - surface.Right : 0;
            // Prefer surfaces closer to the bottom of the desktop: they are
            // reachable first without requiring a multi-window traversal.
            distance += Math.Abs(surface.Top - surface.Bottom) * 0.0001;
            if (distance < bestDistance)
            {
                best = surface;
                bestDistance = distance;
            }
        }
        return best;
    }

    private delegate bool EnumWindowsProc(IntPtr hwnd, IntPtr lParam);

    [StructLayout(LayoutKind.Sequential)]
    private struct NativeRect
    {
        public int Left, Top, Right, Bottom;
    }

    [DllImport("user32.dll")] private static extern bool EnumWindows(EnumWindowsProc callback, IntPtr lParam);
    [DllImport("user32.dll")] private static extern bool IsWindowVisible(IntPtr hwnd);
    [DllImport("user32.dll")] private static extern bool IsIconic(IntPtr hwnd);
    [DllImport("user32.dll")] private static extern bool GetWindowRect(IntPtr hwnd, out NativeRect rect);
    [DllImport("user32.dll", CharSet = CharSet.Unicode)] private static extern int GetWindowText(IntPtr hwnd, StringBuilder text, int maxCount);
    [DllImport("user32.dll")] private static extern uint GetWindowThreadProcessId(IntPtr hwnd, out uint processId);
    [DllImport("user32.dll", EntryPoint = "GetWindowLongPtrW")] private static extern IntPtr GetWindowLongPtr(IntPtr hwnd, int index);
}

public sealed record DesktopSurface(
    IntPtr WindowHandle,
    string Title,
    string ProcessName,
    double Left,
    double Top,
    double Right,
    double Bottom)
{
    public double CenterX => (Left + Right) * 0.5;
    public double Width => Right - Left;
}
