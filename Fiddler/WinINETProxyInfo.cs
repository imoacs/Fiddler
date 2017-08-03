namespace Fiddler
{
    using System;
    using System.Diagnostics;
    using System.Runtime.InteropServices;
    using System.Runtime.InteropServices.ComTypes;
    using System.Text;

    public class WinINETProxyInfo
    {
        private bool _bAutoDetect;
        private bool _bAutoDetectWasUserSet;
        private bool _bBypassIntranetHosts;
        private bool _bDirect;
        private bool _bProxiesSpecified;
        private bool _bUseConfigScript;
        private string _sFtpProxy;
        private string _sHostsThatBypass;
        private string _sHttpProxy;
        private string _sHttpsProxy;
        private string _sScriptURL;
        private string _sSocksProxy;
        private const int AUTO_PROXY_FLAG_USER_SET = 1;
        private const int ERROR_INVALID_PARAMETER = 0x57;
        private const int INTERNET_OPTION_PER_CONNECTION_OPTION = 0x4b;
        private const int INTERNET_OPTION_PROXY_SETTINGS_CHANGED = 0x5f;
        private const int INTERNET_PER_CONN_AUTOCONFIG_LAST_DETECT_TIME = 8;
        private const int INTERNET_PER_CONN_AUTOCONFIG_LAST_DETECT_URL = 9;
        private const int INTERNET_PER_CONN_AUTOCONFIG_RELOAD_DELAY_MINS = 7;
        private const int INTERNET_PER_CONN_AUTOCONFIG_SECONDARY_URL = 6;
        private const int INTERNET_PER_CONN_AUTOCONFIG_URL = 4;
        private const int INTERNET_PER_CONN_AUTODISCOVERY_FLAGS = 5;
        private const int INTERNET_PER_CONN_FLAGS = 1;
        private const int INTERNET_PER_CONN_FLAGS_UI = 10;
        private const int INTERNET_PER_CONN_PROXY_BYPASS = 3;
        private const int INTERNET_PER_CONN_PROXY_SERVER = 2;
        private const int PROXY_TYPE_AUTO_DETECT = 8;
        private const int PROXY_TYPE_AUTO_PROXY_URL = 4;
        private const int PROXY_TYPE_DIRECT = 1;
        private const int PROXY_TYPE_PROXY = 2;

        internal string CalculateProxyString()
        {
            if (((this._sHttpProxy == this._sHttpsProxy) && (this._sHttpProxy == this._sFtpProxy)) && (this._sHttpProxy == this._sSocksProxy))
            {
                if (this._sHttpProxy == string.Empty)
                {
                    return null;
                }
                return this._sHttpProxy;
            }
            string str = null;
            if (!string.IsNullOrEmpty(this._sHttpProxy))
            {
                str = "http=" + this._sHttpProxy + ";";
            }
            if (!string.IsNullOrEmpty(this._sHttpsProxy))
            {
                str = str + "https=" + this._sHttpsProxy + ";";
            }
            if (!string.IsNullOrEmpty(this._sFtpProxy))
            {
                str = str + "ftp=" + this._sFtpProxy + ";";
            }
            if (!string.IsNullOrEmpty(this._sSocksProxy))
            {
                str = str + "socks=" + this._sSocksProxy + ";";
            }
            return str;
        }

        private void Clear()
        {
            this._bAutoDetect = false;
            this._bBypassIntranetHosts = false;
            this._bDirect = false;
            this._bProxiesSpecified = false;
            this._bUseConfigScript = false;
            this._sHttpProxy = null;
            this._sHttpsProxy = null;
            this._sSocksProxy = null;
            this._sFtpProxy = null;
            this._sScriptURL = null;
            this._sHostsThatBypass = null;
        }

        public static WinINETProxyInfo CreateFromNamedConnection(string sConnectionName)
        {
            WinINETProxyInfo info = new WinINETProxyInfo();
            if (info.GetFromWinINET(sConnectionName))
            {
                return info;
            }
            return null;
        }

        public bool GetFromWinINET(string sConnectionName)
        {
            this.Clear();
            try
            {
                INTERNET_PER_CONN_OPTION_LIST structure = new INTERNET_PER_CONN_OPTION_LIST();
                INTERNET_PER_CONN_OPTION[] internet_per_conn_optionArray = new INTERNET_PER_CONN_OPTION[5];
                if (sConnectionName == "DefaultLAN")
                {
                    sConnectionName = null;
                }
                structure.Connection = sConnectionName;
                structure.OptionCount = internet_per_conn_optionArray.Length;
                structure.OptionError = 0;
                internet_per_conn_optionArray[0] = new INTERNET_PER_CONN_OPTION();
                internet_per_conn_optionArray[0].dwOption = 1;
                internet_per_conn_optionArray[1] = new INTERNET_PER_CONN_OPTION();
                internet_per_conn_optionArray[1].dwOption = 2;
                internet_per_conn_optionArray[2] = new INTERNET_PER_CONN_OPTION();
                internet_per_conn_optionArray[2].dwOption = 3;
                internet_per_conn_optionArray[3] = new INTERNET_PER_CONN_OPTION();
                internet_per_conn_optionArray[3].dwOption = 4;
                internet_per_conn_optionArray[4] = new INTERNET_PER_CONN_OPTION();
                internet_per_conn_optionArray[4].dwOption = 10;
                int cb = 0;
                for (int i = 0; i < internet_per_conn_optionArray.Length; i++)
                {
                    cb += Marshal.SizeOf(internet_per_conn_optionArray[i]);
                }
                IntPtr ptr = Marshal.AllocCoTaskMem(cb);
                IntPtr ptr2 = ptr;
                for (int j = 0; j < internet_per_conn_optionArray.Length; j++)
                {
                    Marshal.StructureToPtr(internet_per_conn_optionArray[j], ptr2, false);
                    ptr2 = (IntPtr) (((long) ptr2) + Marshal.SizeOf(internet_per_conn_optionArray[j]));
                }
                structure.pOptions = ptr;
                structure.Size = Marshal.SizeOf(structure);
                int size = structure.Size;
                bool flag = InternetQueryOptionList(IntPtr.Zero, 0x4b, ref structure, ref size);
                int num5 = Marshal.GetLastWin32Error();
                if (!flag && (0x57 == num5))
                {
                    internet_per_conn_optionArray[4].dwOption = 5;
                    ptr2 = ptr;
                    for (int k = 0; k < internet_per_conn_optionArray.Length; k++)
                    {
                        Marshal.StructureToPtr(internet_per_conn_optionArray[k], ptr2, false);
                        ptr2 = (IntPtr) (((long) ptr2) + Marshal.SizeOf(internet_per_conn_optionArray[k]));
                    }
                    structure.pOptions = ptr;
                    structure.Size = Marshal.SizeOf(structure);
                    size = structure.Size;
                    flag = InternetQueryOptionList(IntPtr.Zero, 0x4b, ref structure, ref size);
                    num5 = Marshal.GetLastWin32Error();
                }
                if (flag)
                {
                    ptr2 = ptr;
                    for (int m = 0; m < internet_per_conn_optionArray.Length; m++)
                    {
                        Marshal.PtrToStructure(ptr2, internet_per_conn_optionArray[m]);
                        ptr2 = (IntPtr) (((long) ptr2) + Marshal.SizeOf(internet_per_conn_optionArray[m]));
                    }
                    this._bDirect = 1 == (internet_per_conn_optionArray[0].Value.dwValue & 1);
                    this._bUseConfigScript = 4 == (internet_per_conn_optionArray[0].Value.dwValue & 4);
                    this._bAutoDetect = 8 == (internet_per_conn_optionArray[0].Value.dwValue & 8);
                    this._bProxiesSpecified = 2 == (internet_per_conn_optionArray[0].Value.dwValue & 2);
                    if (internet_per_conn_optionArray[4].dwOption == 10)
                    {
                        this._bAutoDetectWasUserSet = 8 == (internet_per_conn_optionArray[4].Value.dwValue & 8);
                    }
                    else
                    {
                        this._bAutoDetectWasUserSet = this._bAutoDetect && (1 == (internet_per_conn_optionArray[4].Value.dwValue & 1));
                    }
                    this._sScriptURL = Marshal.PtrToStringAnsi(internet_per_conn_optionArray[3].Value.pszValue);
                    Utilities.GlobalFree(internet_per_conn_optionArray[3].Value.pszValue);
                    if (internet_per_conn_optionArray[1].Value.pszValue != IntPtr.Zero)
                    {
                        string sProxyString = Marshal.PtrToStringAnsi(internet_per_conn_optionArray[1].Value.pszValue);
                        Utilities.GlobalFree(internet_per_conn_optionArray[1].Value.pszValue);
                        this.InitializeFromProxyString(sProxyString);
                    }
                    if (internet_per_conn_optionArray[2].Value.pszValue != IntPtr.Zero)
                    {
                        this._sHostsThatBypass = Marshal.PtrToStringAnsi(internet_per_conn_optionArray[2].Value.pszValue);
                        Utilities.GlobalFree(internet_per_conn_optionArray[2].Value.pszValue);
                        this._bBypassIntranetHosts = this._sHostsThatBypass.Contains("<local>");
                    }
                }
                Marshal.FreeCoTaskMem(ptr);
                return flag;
            }
            catch (Exception exception)
            {
                FiddlerApplication.ReportException(exception, "Unable to get proxy information for " + (sConnectionName ?? "DefaultLAN"));
                return false;
            }
        }

        private bool InitializeFromProxyString(string sProxyString)
        {
            this._sFtpProxy = this._sSocksProxy = this._sHttpProxy = (string) (this._sHttpsProxy = null);
            if (!string.IsNullOrEmpty(sProxyString))
            {
                sProxyString = sProxyString.ToLower();
                if (!sProxyString.Contains("="))
                {
                    string str6;
                    string str7;
                    this.sSocksProxy = str6 = sProxyString;
                    this.sHttpsProxy = str7 = str6;
                    this.sFtpProxy = this.sHttpProxy = str7;
                    return true;
                }
                foreach (string str in sProxyString.Split(new char[] { ';' }))
                {
                    if (str.IndexOf('=') >= 3)
                    {
                        string str2 = str.Substring(str.IndexOf('=') + 1).Trim();
                        if (str2.IndexOf('/') > 0)
                        {
                            str2 = str2.Substring(str2.LastIndexOf('/') + 1);
                        }
                        if (str.StartsWith("http="))
                        {
                            this._sHttpProxy = str2;
                        }
                        else if (str.StartsWith("https="))
                        {
                            this._sHttpsProxy = str2;
                        }
                        else if (str.StartsWith("ftp="))
                        {
                            this._sFtpProxy = str2;
                        }
                        else if (str.StartsWith("socks="))
                        {
                            this._sSocksProxy = str2;
                        }
                    }
                }
            }
            return true;
        }

        [return: MarshalAs(UnmanagedType.Bool)]
        [DllImport("wininet.dll", CharSet=CharSet.Ansi, SetLastError=true)]
        private static extern bool InternetQueryOption(IntPtr hInternet, int Option, byte[] OptionInfo, ref int size);
        [return: MarshalAs(UnmanagedType.Bool)]
        [DllImport("wininet.dll", EntryPoint="InternetQueryOption", CharSet=CharSet.Ansi, SetLastError=true)]
        private static extern bool InternetQueryOptionList(IntPtr hInternet, int Option, ref INTERNET_PER_CONN_OPTION_LIST OptionList, ref int size);
        [return: MarshalAs(UnmanagedType.Bool)]
        [DllImport("wininet.dll", CharSet=CharSet.Ansi, SetLastError=true)]
        private static extern bool InternetSetOption(IntPtr hInternet, int Option, [In] IntPtr buffer, int BufferLength);
        [return: MarshalAs(UnmanagedType.Bool)]
        [DllImport("wininet.dll", EntryPoint="InternetSetOption", CharSet=CharSet.Ansi, SetLastError=true)]
        private static extern bool InternetSetOptionList(IntPtr hInternet, int Option, ref INTERNET_PER_CONN_OPTION_LIST OptionList, int size);
        internal bool SetToWinINET(string sConnectionName)
        {
            try
            {
                INTERNET_PER_CONN_OPTION_LIST structure = new INTERNET_PER_CONN_OPTION_LIST();
                INTERNET_PER_CONN_OPTION[] internet_per_conn_optionArray = new INTERNET_PER_CONN_OPTION[5];
                if (sConnectionName == "DefaultLAN")
                {
                    sConnectionName = null;
                }
                structure.Connection = sConnectionName;
                structure.OptionCount = internet_per_conn_optionArray.Length;
                structure.OptionError = 0;
                int num = 0;
                if (this._bDirect)
                {
                    num |= 1;
                }
                if (this._bAutoDetect)
                {
                    num |= 8;
                }
                if (this._bAutoDetectWasUserSet)
                {
                    num |= 8;
                }
                if (this._bUseConfigScript)
                {
                    num |= 4;
                }
                if (this._bProxiesSpecified)
                {
                    num |= 2;
                }
                internet_per_conn_optionArray[0] = new INTERNET_PER_CONN_OPTION();
                internet_per_conn_optionArray[0].dwOption = 1;
                internet_per_conn_optionArray[0].Value.dwValue = num;
                internet_per_conn_optionArray[1] = new INTERNET_PER_CONN_OPTION();
                internet_per_conn_optionArray[1].dwOption = 2;
                internet_per_conn_optionArray[1].Value.pszValue = Marshal.StringToHGlobalAnsi(this.CalculateProxyString());
                internet_per_conn_optionArray[2] = new INTERNET_PER_CONN_OPTION();
                internet_per_conn_optionArray[2].dwOption = 3;
                internet_per_conn_optionArray[2].Value.pszValue = Marshal.StringToHGlobalAnsi(this._sHostsThatBypass);
                internet_per_conn_optionArray[3] = new INTERNET_PER_CONN_OPTION();
                internet_per_conn_optionArray[3].dwOption = 4;
                internet_per_conn_optionArray[3].Value.pszValue = Marshal.StringToHGlobalAnsi(this._sScriptURL);
                internet_per_conn_optionArray[4] = new INTERNET_PER_CONN_OPTION();
                internet_per_conn_optionArray[4].dwOption = 5;
                internet_per_conn_optionArray[4].Value.dwValue = 0;
                if (this._bAutoDetectWasUserSet)
                {
                    internet_per_conn_optionArray[4].Value.dwValue = 1;
                }
                int cb = 0;
                for (int i = 0; i < internet_per_conn_optionArray.Length; i++)
                {
                    cb += Marshal.SizeOf(internet_per_conn_optionArray[i]);
                }
                IntPtr ptr = Marshal.AllocCoTaskMem(cb);
                IntPtr ptr2 = ptr;
                for (int j = 0; j < internet_per_conn_optionArray.Length; j++)
                {
                    Marshal.StructureToPtr(internet_per_conn_optionArray[j], ptr2, false);
                    ptr2 = (IntPtr) (((long) ptr2) + Marshal.SizeOf(internet_per_conn_optionArray[j]));
                }
                structure.pOptions = ptr;
                structure.Size = Marshal.SizeOf(structure);
                int size = structure.Size;
                bool flag = InternetSetOptionList(IntPtr.Zero, 0x4b, ref structure, size);
                int num6 = Marshal.GetLastWin32Error();
                if (flag)
                {
                    InternetSetOption(IntPtr.Zero, 0x5f, IntPtr.Zero, 0);
                }
                else
                {
                    Trace.WriteLine("[Fiddler] SetProxy failed.  WinINET Error #" + num6.ToString("x"));
                }
                Marshal.FreeHGlobal(internet_per_conn_optionArray[0].Value.pszValue);
                Marshal.FreeHGlobal(internet_per_conn_optionArray[1].Value.pszValue);
                Marshal.FreeHGlobal(internet_per_conn_optionArray[2].Value.pszValue);
                Marshal.FreeCoTaskMem(ptr);
                return flag;
            }
            catch (Exception exception)
            {
                Trace.WriteLine("[Fiddler] SetProxy failed. " + exception.Message);
                return false;
            }
        }

        public override string ToString()
        {
            StringBuilder builder = new StringBuilder(0x200);
            builder.AppendFormat("HTTP=\t{0}\n", this._sHttpProxy);
            builder.AppendFormat("HTTPS=\t{0}\n", this._sHttpsProxy);
            builder.AppendFormat("FTP=\t{0}\n", this._sFtpProxy);
            builder.AppendFormat("SOCKS=\t{0}\n", this._sSocksProxy);
            builder.AppendFormat("Script=\t{0}\n", this._sScriptURL);
            builder.AppendFormat("Bypass=\t{0}\n", this._sHostsThatBypass);
            int num = 0;
            if (this._bDirect)
            {
                num |= 1;
            }
            if (this._bAutoDetect)
            {
                num |= 8;
            }
            if (this._bUseConfigScript)
            {
                num |= 4;
            }
            if (this._bProxiesSpecified)
            {
                num |= 2;
            }
            builder.AppendFormat("ProxyType:\t{0}\n", num.ToString());
            if (this._bAutoDetectWasUserSet)
            {
                builder.AppendLine("AutoProxyDetection was user-set.");
            }
            return builder.ToString();
        }

        public bool bAllowDirect
        {
            get
            {
                return this._bDirect;
            }
            set
            {
                this._bDirect = value;
            }
        }

        public bool bAutoDetect
        {
            get
            {
                return this._bAutoDetect;
            }
            set
            {
                this._bAutoDetect = value;
            }
        }

        public bool bBypassIntranetHosts
        {
            get
            {
                return this._bBypassIntranetHosts;
            }
        }

        public bool bUseManualProxies
        {
            get
            {
                return this._bProxiesSpecified;
            }
            set
            {
                this._bProxiesSpecified = value;
            }
        }

        public string sFtpProxy
        {
            get
            {
                return this._sFtpProxy;
            }
            set
            {
                this._sFtpProxy = value;
            }
        }

        public string sHostsThatBypass
        {
            get
            {
                return this._sHostsThatBypass;
            }
            set
            {
                this._sHostsThatBypass = value;
            }
        }

        public string sHttpProxy
        {
            get
            {
                return this._sHttpProxy;
            }
            set
            {
                this._sHttpProxy = value;
            }
        }

        public string sHttpsProxy
        {
            get
            {
                return this._sHttpsProxy;
            }
            set
            {
                this._sHttpsProxy = value;
            }
        }

        public string sPACScriptLocation
        {
            get
            {
                if (this._bUseConfigScript)
                {
                    return this._sScriptURL;
                }
                return null;
            }
            set
            {
                if (value == null)
                {
                    this._bUseConfigScript = false;
                    this._sScriptURL = null;
                }
                else
                {
                    this._bUseConfigScript = true;
                    this._sScriptURL = value;
                }
            }
        }

        public string sSocksProxy
        {
            get
            {
                return this._sSocksProxy;
            }
            set
            {
                this._sSocksProxy = value;
            }
        }

        [StructLayout(LayoutKind.Sequential)]
        private class INTERNET_PER_CONN_OPTION
        {
            public int dwOption;
            public WinINETProxyInfo.OptionUnion Value;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct INTERNET_PER_CONN_OPTION_LIST
        {
            public int Size;
            public string Connection;
            public int OptionCount;
            public int OptionError;
            public IntPtr pOptions;
        }

        [StructLayout(LayoutKind.Explicit)]
        private struct OptionUnion
        {
            [FieldOffset(0)]
            public int dwValue;
            [FieldOffset(0)]
            public System.Runtime.InteropServices.ComTypes.FILETIME ftValue;
            [FieldOffset(0)]
            public IntPtr pszValue;
        }
    }
}

