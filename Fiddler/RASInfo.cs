namespace Fiddler
{
    using System;
    using System.Runtime.InteropServices;

    internal class RASInfo
    {
        private const int MAX_PATH = 260;
        private const int RAS_MaxEntryName = 0x100;

        private static string GetConnectedState()
        {
            InternetConnectionState lpdwFlags = 0;
            if (InternetGetConnectedState(ref lpdwFlags, 0))
            {
                return ("CONNECTED (" + lpdwFlags.ToString() + ")");
            }
            return ("NOT_CONNECTED (" + lpdwFlags.ToString() + ")");
        }

        internal static string[] GetConnectionNames()
        {
            if (CONFIG.bDebugSpew)
            {
                FiddlerApplication.DebugSpew("WinINET indicates connectivity is via: " + GetConnectedState());
            }
            int lpcb = Marshal.SizeOf(typeof(RASENTRYNAME));
            int lpcEntries = 0;
            RASENTRYNAME[] lprasentryname = new RASENTRYNAME[1];
            lprasentryname[0].dwSize = lpcb;
            uint num3 = RasEnumEntries(IntPtr.Zero, IntPtr.Zero, lprasentryname, ref lpcb, ref lpcEntries);
            if ((num3 != 0) && (0x25b != num3))
            {
                lpcEntries = 0;
            }
            string[] strArray = new string[lpcEntries + 1];
            strArray[0] = "DefaultLAN";
            if (lpcEntries != 0)
            {
                lprasentryname = new RASENTRYNAME[lpcEntries];
                for (int i = 0; i < lpcEntries; i++)
                {
                    lprasentryname[i].dwSize = Marshal.SizeOf(typeof(RASENTRYNAME));
                }
                if (RasEnumEntries(IntPtr.Zero, IntPtr.Zero, lprasentryname, ref lpcb, ref lpcEntries) != 0)
                {
                    return strArray;
                }
                for (int j = 0; j < lpcEntries; j++)
                {
                    strArray[j + 1] = lprasentryname[j].szEntryName;
                }
            }
            return strArray;
        }

        [return: MarshalAs(UnmanagedType.Bool)]
        [DllImport("wininet.dll", CharSet=CharSet.Unicode)]
        internal static extern bool InternetGetConnectedState(ref InternetConnectionState lpdwFlags, int dwReserved);
        [DllImport("rasapi32.dll", CharSet=CharSet.Auto, SetLastError=true)]
        private static extern uint RasEnumEntries(IntPtr reserved, IntPtr lpszPhonebook, [In, Out] RASENTRYNAME[] lprasentryname, ref int lpcb, ref int lpcEntries);

        [Flags]
        internal enum InternetConnectionState
        {
            INTERNET_CONNECTION_CONFIGURED = 0x40,
            INTERNET_CONNECTION_LAN = 2,
            INTERNET_CONNECTION_MODEM = 1,
            INTERNET_CONNECTION_OFFLINE = 0x20,
            INTERNET_CONNECTION_PROXY = 4,
            INTERNET_RAS_INSTALLED = 0x10
        }

        [StructLayout(LayoutKind.Sequential, CharSet=CharSet.Auto)]
        private struct RASENTRYNAME
        {
            public int dwSize;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst=0x101)]
            public string szEntryName;
            public int dwFlags;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst=0x105)]
            public string szPhonebook;
        }
    }
}

