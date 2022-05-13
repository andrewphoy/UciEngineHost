﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace UciEngineHost {
    public static class WinApi {
        [DllImport("user32")]
        public static extern int RegisterWindowMessage(string message);

        internal static int RegisterWindowMessage(string format, params object[] args) {
            string message = String.Format(format, args);
            return RegisterWindowMessage(message);
        }

        internal const int HWND_BROADCAST = 0xffff;
        internal const int SW_SHOWNORMAL = 1;

        [DllImport("user32")]
        public static extern bool PostMessage(IntPtr hwnd, int msg, IntPtr wparam, IntPtr lparam);

        [DllImportAttribute("user32.dll")]
        public static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        [DllImportAttribute("user32.dll")]
        public static extern bool SetForegroundWindow(IntPtr hWnd);

        internal static void ShowToFront(IntPtr window) {
            ShowWindow(window, SW_SHOWNORMAL);
            SetForegroundWindow(window);
        }
    }
}
