using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UciEngineHost {
    public static class SingleInstance {

        public static readonly int WM_SHOWFIRSTINSTANCE =
            WinApi.RegisterWindowMessage("WM_SHOWFIRSTINSTANCE|{0}", ProgramInfo.AssemblyGuid);
        private static Mutex? mutex;

        public static bool Start() {
            string nameMutex = string.Format("Local\\{0}", ProgramInfo.AssemblyGuid);

            mutex = new Mutex(true, nameMutex, out bool createdNew);
            return createdNew;
        }

        public static void ShowFirstInstance() {
            WinApi.PostMessage(
                (IntPtr)WinApi.HWND_BROADCAST,
                WM_SHOWFIRSTINSTANCE,
                IntPtr.Zero,
                IntPtr.Zero);
        }

        public static void Stop() {
            mutex?.ReleaseMutex();
        }
    }
}
