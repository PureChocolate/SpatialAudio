using System.Runtime.InteropServices;
using System.Text;

namespace SpatialAudio
{
    struct RECT
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;

        public override string ToString()
        {
            return $"[L: {Left}, T: {Top}, R: {Right}, B: {Bottom}]";
        }
    }

    struct MONITORINFO
    {
        public uint cbSize;
        public RECT rcMonitor;
        public RECT rcWork;
        public uint dwFlags;
    }

    internal static class WindowTracker
    {
        private static RECT VirtualDesktop;
        public delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);
        public delegate bool MonitorEnumProc(IntPtr hMonitor, IntPtr hdc, ref RECT lprcMonitor, IntPtr lParam);

        [DllImport("user32.dll")]
        static extern bool EnumWindows(EnumWindowsProc callback, IntPtr lParam);

        [DllImport("user32.dll")]
        static extern bool GetWindowRect(IntPtr hWnd, out RECT rect);

        [DllImport("user32.dll")]
        static extern bool IsWindowVisible(IntPtr hWnd);

        [DllImport ("user32.dll", CharSet= CharSet.Unicode)]
        static extern int GetWindowText(IntPtr hWnd, StringBuilder text, int maxCount);

        [DllImport("user32.dll")]
        static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint pid);

        [DllImport("user32.dll")]
        static extern bool SetProcessDpiAwarenessContext(IntPtr value);

        [DllImport("user32.dll")]
        static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll")]
        static extern bool EnumDisplayMonitors(IntPtr hdc, IntPtr lprcClip, MonitorEnumProc lpfnEnum, IntPtr dwData);

        [DllImport("user32.dll")]
        static extern bool GetMonitorInfo(IntPtr hMonitor, ref MONITORINFO lpmi);

        public static void PrintFocusedWindow()
        {
            IntPtr fWindow = GetForegroundWindow();
            if (!IsWindowVisible(fWindow)) return;
            GetWindowRect(fWindow, out RECT rect);

            StringBuilder title = new StringBuilder(256);
            int chars = GetWindowText(fWindow, title, 256);
            string t = chars == 0 ? "(no title)" : title.ToString();
            Console.Write($"FOCUSED: {t,-60} | {rect.Left},{rect.Top},{rect.Right},{rect.Bottom}".PadRight(110));
        }

        public static void EnablePerMonitorDpiAwareness()
        {
            SetProcessDpiAwarenessContext(-4);
        }

        private static bool WindowCallBack(IntPtr hWnd, IntPtr lParam)
        {
            if (!IsWindowVisible(hWnd)) return true;
            GetWindowRect(hWnd, out RECT rect);

            StringBuilder title = new StringBuilder(256);

            int chars = GetWindowText(hWnd, title, 256);
            if (chars == 0) {
                Console.WriteLine("(no title)");
            }
            GetWindowThreadProcessId(hWnd, out uint pid);
            Console.WriteLine($"hwnd: {hWnd}, pid: {pid}, title: {title}, {rect.Left}, {rect.Top}, {rect.Right}, {rect.Bottom}");

            return true;
            
        }

        public static void ListWindows()
        {
            EnumWindows(WindowCallBack, IntPtr.Zero);
        }

        private static bool MonitorCallBack(IntPtr hMonitor, IntPtr hdc, ref RECT lprcMonitor, IntPtr lParam)
        {
            MONITORINFO monitor = new MONITORINFO();
            monitor.cbSize = (uint)Marshal.SizeOf<MONITORINFO>();
            if (GetMonitorInfo(hMonitor, ref monitor))
            {
                VirtualDesktop.Left = Math.Min(VirtualDesktop.Left, monitor.rcMonitor.Left);
                VirtualDesktop.Top = Math.Min(VirtualDesktop.Top, monitor.rcMonitor.Top);
                VirtualDesktop.Right = Math.Max(VirtualDesktop.Right, monitor.rcMonitor.Right);
                VirtualDesktop.Bottom = Math.Max(VirtualDesktop.Bottom, monitor.rcMonitor.Bottom);
            }
            return true;
        }

        public static void ListMonitors()
        {
            VirtualDesktop = new RECT { Left = int.MaxValue, Top = int.MaxValue, Right = int.MinValue, Bottom = int.MinValue };
            EnumDisplayMonitors(IntPtr.Zero, IntPtr.Zero, MonitorCallBack, IntPtr.Zero);
        }

        public static (float azimuth, float distance, string title) GetFocusedInfo()
        {
            IntPtr fWindow = GetForegroundWindow();
            if (IsWindowVisible(fWindow))
            {
                GetWindowRect(fWindow, out RECT rect);

                StringBuilder title = new StringBuilder(256);
                int chars = GetWindowText(fWindow, title, 256);
                string t = chars == 0 ? "(no title)" : title.ToString();

                float cx = rect.Left + MathF.Abs(rect.Right - rect.Left) / 2;
                float cy = rect.Top + MathF.Abs(rect.Bottom - rect.Top) / 2;
                float lx = VirtualDesktop.Left + MathF.Abs(VirtualDesktop.Right - VirtualDesktop.Left) / 2;
                float ly = VirtualDesktop.Top + MathF.Abs(VirtualDesktop.Bottom - VirtualDesktop.Top) / 2;

                float dx = cx - lx;
                float dy = cy - ly;

                float halfWidth = (VirtualDesktop.Right - VirtualDesktop.Left) / 2f;
                float eyeDistance = halfWidth / MathF.Tan(70f * MathF.PI / 180f);
                float azimuth = MathF.Atan2(dx, eyeDistance) * 180.0f / MathF.PI;
                float distance = MathF.Sqrt(dx * dx + dy * dy);

                return (azimuth, distance, title.ToString());
            }else { return (0, 0, "Not visible"); }
        }
    }
}
