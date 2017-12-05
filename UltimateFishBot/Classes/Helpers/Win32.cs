using System;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;

namespace UltimateFishBot.Classes.Helpers
{
    public class Win32
    {
        private struct Rect
        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct Point
        {
            public Int32 x;
            public Int32 y;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct CursorInfo
        {
            public Int32 cbSize;
            public Int32 flags;
            public IntPtr hCursor;
            public Point ptScreenPos;
        }

        public enum KeyState
        {
            Keydown = 0,
            Extendedkey = 1,
            Keyup = 2
        };

        [DllImport("user32.dll", EntryPoint = "FindWindow")]
        private static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

        [DllImport("user32.dll")]
        private static extern bool SetForegroundWindow(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern bool GetWindowRect(IntPtr hwnd, ref Rect rectangle);

        [DllImport("user32.dll")]
        private static extern bool SetCursorPos(int x, int y);

        [DllImport("user32.dll")]
        private static extern bool GetCursorInfo(out CursorInfo pci);

        [DllImport("user32.dll")]
        private static extern bool DrawIcon(IntPtr hDc, int x, int y, IntPtr hIcon);

        [DllImport("user32.dll")]
        private static extern bool keybd_event(byte bVk, byte bScan, uint dwFlags, UIntPtr dwExtraInfo);

        [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        private static extern bool SendNotifyMessage(IntPtr hWnd, uint msg, UIntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll")]
        public static extern bool RegisterHotKey(IntPtr hWnd, int id, int fsModifiers, int vk);

        [DllImport("user32.dll")]
        public static extern bool UnregisterHotKey(IntPtr hWnd, int id);
        
        private const uint WmRbuttondown = 516;
        private const uint WmRbuttonup = 517;

        public static Rectangle GetWowRectangle()
        {
            IntPtr wow = FindWindow("GxWindowClassD3d", "World Of Warcraft");
            Rect win32ApiRect = new Rect();
            GetWindowRect(wow, ref win32ApiRect);
            Rectangle myRect = new Rectangle();
            myRect.X = win32ApiRect.Left;
            myRect.Y = win32ApiRect.Top;
            myRect.Width = win32ApiRect.Right - win32ApiRect.Left;
            myRect.Height = win32ApiRect.Bottom - win32ApiRect.Top;
            return myRect;
        }

        public static Bitmap GetCursorIcon(CursorInfo actualCursor, int width = 35, int height = 35)
        {
            Bitmap actualCursorIcon = null;

            try
            {
                actualCursorIcon = new Bitmap(width, height);
                Graphics g = Graphics.FromImage(actualCursorIcon);
                Win32.DrawIcon(g.GetHdc(), 0, 0, actualCursor.hCursor);
                g.ReleaseHdc();
            }
            catch (Exception) { }

            return actualCursorIcon;
        }

        public static void ActivateWow()
        {
            ActivateApp(Properties.Settings.Default.ProcName);
            ActivateApp(Properties.Settings.Default.ProcName + "-64");
            ActivateApp("World Of Warcraft");
        }

        public static void ActivateApp(string processName)
        {
            Process[] p = Process.GetProcessesByName(processName);

            // Activate the first application we find with this name
            if (p.Any())
                SetForegroundWindow(p[0].MainWindowHandle);
        }

        public static void MoveMouse(int x, int y)
        {
            if (SetCursorPos(x, y))
            {
                _lastX = x;
                _lastY = y;
            }
        }

        public static CursorInfo GetNoFishCursor()
        {
            Rectangle woWRect = Win32.GetWowRectangle();
            Win32.MoveMouse(woWRect.X + 10, woWRect.Y + 45);
            _lastRectX = woWRect.X;
            _lastRectY = woWRect.Y;
            Thread.Sleep(15);
            CursorInfo myInfo = new CursorInfo();
            myInfo.cbSize = Marshal.SizeOf(myInfo);
            GetCursorInfo(out myInfo);
            return myInfo;
        }

        public static CursorInfo GetCurrentCursor()
        {
            CursorInfo myInfo = new CursorInfo();
            myInfo.cbSize = Marshal.SizeOf(myInfo);
            GetCursorInfo(out myInfo);
            return myInfo;
        }

        public static void SendKey(string sKeys)
        {
            if (sKeys != " ")
            {
                if (Properties.Settings.Default.UseAltKey)
                    sKeys = "%(" + sKeys + ")"; // %(X) : Use the alt key
                else
                    sKeys = "{" + sKeys + "}";  // {X} : Avoid UTF-8 errors (é, è, ...)
            }

            SendKeys.Send(sKeys);
        }

        public static void SendMouseClick()
        {
            IntPtr wow = FindWindow("GxWindowClassD3d", "World Of Warcraft");
            long dWord = MakeDWord(_lastX - _lastRectX, _lastY - _lastRectY);

            if (Properties.Settings.Default.ShiftLoot)
                SendKeyboardAction(16, KeyState.Keydown);

            SendNotifyMessage(wow, WmRbuttondown, (UIntPtr)1, (IntPtr)dWord);
            Thread.Sleep(100);
            SendNotifyMessage(wow, WmRbuttonup, (UIntPtr)1, (IntPtr)dWord);

            if (Properties.Settings.Default.ShiftLoot)
                SendKeyboardAction(16, KeyState.Keyup);
        }

        public static bool SendKeyboardAction(Keys key, KeyState state)
        {
            return SendKeyboardAction((byte)key.GetHashCode(), state);
        }

        public static bool SendKeyboardAction(byte key, KeyState state)
        {
            return keybd_event(key, 0, (uint)state, (UIntPtr)0);
        }

        private static long MakeDWord(int loWord, int hiWord)
        {
            return (hiWord << 16) | (loWord & 0xFFFF);
        }

        private static int _lastRectX;
        private static int _lastRectY;

        private static int _lastX;
        private static int _lastY;
    }
}
