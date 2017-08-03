namespace Fiddler
{
    using System;
    using System.Runtime.InteropServices;

    internal class WinHTTPNative
    {
        internal const int ERROR_WINHTTP_AUTODETECTION_FAILED = 0x2f94;
        internal const int ERROR_WINHTTP_BAD_AUTO_PROXY_SCRIPT = 0x2f86;
        internal const int ERROR_WINHTTP_LOGIN_FAILURE = 0x2eef;
        internal const int ERROR_WINHTTP_UNABLE_TO_DOWNLOAD_SCRIPT = 0x2f87;
        internal const int ERROR_WINHTTP_UNRECOGNIZED_SCHEME = 0x2ee6;
        internal const int WINHTTP_ACCESS_TYPE_DEFAULT_PROXY = 0;
        internal const int WINHTTP_ACCESS_TYPE_NAMED_PROXY = 3;
        internal const int WINHTTP_ACCESS_TYPE_NO_PROXY = 1;
        internal const int WINHTTP_AUTO_DETECT_TYPE_DHCP = 1;
        internal const int WINHTTP_AUTO_DETECT_TYPE_DNS_A = 2;
        internal const int WINHTTP_AUTOPROXY_AUTO_DETECT = 1;
        internal const int WINHTTP_AUTOPROXY_CONFIG_URL = 2;
        internal const int WINHTTP_AUTOPROXY_RUN_INPROCESS = 0x10000;
        internal const int WINHTTP_AUTOPROXY_RUN_OUTPROCESS_ONLY = 0x20000;

        [return: MarshalAs(UnmanagedType.Bool)]
        [DllImport("winhttp.dll", CharSet=CharSet.Unicode, SetLastError=true)]
        internal static extern bool WinHttpCloseHandle(IntPtr hInternet);
        [return: MarshalAs(UnmanagedType.Bool)]
        [DllImport("winhttp.dll", CharSet=CharSet.Unicode, SetLastError=true)]
        internal static extern bool WinHttpDetectAutoProxyConfigUrl([MarshalAs(UnmanagedType.U4)] int dwAutoDetectFlags, out IntPtr ppwszAutoConfigUrl);
        [return: MarshalAs(UnmanagedType.Bool)]
        [DllImport("winhttp.dll", CharSet=CharSet.Unicode, SetLastError=true)]
        internal static extern bool WinHttpGetProxyForUrl(IntPtr hSession, string lpcwszUrl, [In] ref WINHTTP_AUTOPROXY_OPTIONS pAutoProxyOptions, out WINHTTP_PROXY_INFO pProxyInfo);
        [DllImport("winhttp.dll", CharSet=CharSet.Unicode, SetLastError=true)]
        internal static extern IntPtr WinHttpOpen(string pwszUserAgent, int dwAccessType, IntPtr pwszProxyName, IntPtr pwszProxyBypass, int dwFlags);

        [StructLayout(LayoutKind.Sequential, CharSet=CharSet.Unicode)]
        internal struct WINHTTP_AUTOPROXY_OPTIONS
        {
            [MarshalAs(UnmanagedType.U4)]
            public int dwFlags;
            [MarshalAs(UnmanagedType.U4)]
            public int dwAutoDetectFlags;
            [MarshalAs(UnmanagedType.LPWStr)]
            public string lpszAutoConfigUrl;
            public IntPtr lpvReserved;
            [MarshalAs(UnmanagedType.U4)]
            public int dwReserved;
            [MarshalAs(UnmanagedType.Bool)]
            public bool fAutoLoginIfChallenged;
        }

        [StructLayout(LayoutKind.Sequential, CharSet=CharSet.Unicode)]
        internal struct WINHTTP_PROXY_INFO
        {
            [MarshalAs(UnmanagedType.U4)]
            public int dwAccessType;
            public IntPtr lpszProxy;
            public IntPtr lpszProxyBypass;
        }
    }
}

