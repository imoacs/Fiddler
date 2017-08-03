namespace Fiddler
{
    using System;
    using System.Net;
    using System.Runtime.InteropServices;

    internal class WinHTTPAutoProxy : IDisposable
    {
        private readonly bool _bUseAutoDiscovery = true;
        private readonly IntPtr _hSession;
        private WinHTTPNative.WINHTTP_AUTOPROXY_OPTIONS _oAPO;
        private readonly string _sPACScriptLocation;
        internal int iAutoProxySuccessCount;

        public WinHTTPAutoProxy(bool bAutoDiscover, string sAutoConfigUrl)
        {
            this._bUseAutoDiscovery = bAutoDiscover;
            if (!string.IsNullOrEmpty(sAutoConfigUrl))
            {
                this._sPACScriptLocation = sAutoConfigUrl;
            }
            this._oAPO = GetAutoProxyOptionsStruct(this._sPACScriptLocation, this._bUseAutoDiscovery);
            this._hSession = WinHTTPNative.WinHttpOpen("Fiddler", 1, IntPtr.Zero, IntPtr.Zero, 0);
        }

        public void Dispose()
        {
            WinHTTPNative.WinHttpCloseHandle(this._hSession);
        }

        public bool GetAutoProxyForUrl(string sUrl, out IPEndPoint ipepResult)
        {
            WinHTTPNative.WINHTTP_PROXY_INFO winhttp_proxy_info;
            int num = 0;
            bool flag = WinHTTPNative.WinHttpGetProxyForUrl(this._hSession, sUrl, ref this._oAPO, out winhttp_proxy_info);
            if (!flag)
            {
                num = Marshal.GetLastWin32Error();
            }
            if (flag)
            {
                if (IntPtr.Zero != winhttp_proxy_info.lpszProxy)
                {
                    string sHostAndPort = Marshal.PtrToStringUni(winhttp_proxy_info.lpszProxy);
                    ipepResult = Utilities.IPEndPointFromHostPortString(sHostAndPort);
                    if (ipepResult == null)
                    {
                        FiddlerApplication.Log.LogFormat("Proxy Configuration Script specified an unreachable proxy: {0} for URL: {1}", new object[] { sHostAndPort, sUrl });
                    }
                }
                else
                {
                    ipepResult = null;
                }
                Utilities.GlobalFreeIfNonZero(winhttp_proxy_info.lpszProxy);
                Utilities.GlobalFreeIfNonZero(winhttp_proxy_info.lpszProxyBypass);
                return true;
            }
            int num2 = num;
            if (num2 <= 0x2eef)
            {
                switch (num2)
                {
                    case 0x2ee6:
                        FiddlerApplication._Log.LogString("Fiddler.Network.ProxyPAC> PAC Script download failure; Fiddler only supports HTTP/HTTPS for PAC script URLs.");
                        goto Label_013E;

                    case 0x2eef:
                        FiddlerApplication._Log.LogString("Fiddler.Network.ProxyPAC> PAC Script download failure; you must set the AutoProxyLogon registry key to TRUE.");
                        goto Label_013E;
                }
            }
            else
            {
                switch (num2)
                {
                    case 0x2f86:
                        FiddlerApplication._Log.LogString("Fiddler.Network.ProxyPAC> PAC Script contents were not valid.");
                        goto Label_013E;

                    case 0x2f87:
                        FiddlerApplication._Log.LogString("Fiddler.Network.ProxyPAC> PAC Script download failed.");
                        goto Label_013E;

                    default:
                        if (num2 != 0x2f94)
                        {
                            goto Label_0124;
                        }
                        FiddlerApplication._Log.LogString("Fiddler.Network.AutoProxy> AutoProxy Detection failed.");
                        goto Label_013E;
                }
            }
        Label_0124:
            FiddlerApplication._Log.LogString("Fiddler.Network.ProxyPAC> Proxy determination failed with error code: " + num);
        Label_013E:
            Utilities.GlobalFreeIfNonZero(winhttp_proxy_info.lpszProxy);
            Utilities.GlobalFreeIfNonZero(winhttp_proxy_info.lpszProxyBypass);
            ipepResult = null;
            return false;
        }

        private static WinHTTPNative.WINHTTP_AUTOPROXY_OPTIONS GetAutoProxyOptionsStruct(string sPAC, bool bUseAutoDetect)
        {
            WinHTTPNative.WINHTTP_AUTOPROXY_OPTIONS winhttp_autoproxy_options = new WinHTTPNative.WINHTTP_AUTOPROXY_OPTIONS();
            if (FiddlerApplication.Prefs.GetBoolPref("fiddler.network.gateway.DetermineInProcess", false))
            {
                winhttp_autoproxy_options.dwFlags = 0x10000;
            }
            else
            {
                winhttp_autoproxy_options.dwFlags = 0;
            }
            if (bUseAutoDetect)
            {
                winhttp_autoproxy_options.dwFlags |= 1;
                winhttp_autoproxy_options.dwAutoDetectFlags = 3;
            }
            if (sPAC != null)
            {
                winhttp_autoproxy_options.dwFlags |= 2;
                winhttp_autoproxy_options.lpszAutoConfigUrl = sPAC;
            }
            winhttp_autoproxy_options.fAutoLoginIfChallenged = CONFIG.bAutoProxyLogon;
            return winhttp_autoproxy_options;
        }

        private static string GetWPADUrl()
        {
            IntPtr ptr;
            string str;
            bool flag = WinHTTPNative.WinHttpDetectAutoProxyConfigUrl(3, out ptr);
            if (!flag)
            {
                Marshal.GetLastWin32Error();
            }
            if (flag && (IntPtr.Zero != ptr))
            {
                str = Marshal.PtrToStringUni(ptr);
            }
            else
            {
                str = string.Empty;
            }
            Utilities.GlobalFreeIfNonZero(ptr);
            return str;
        }

        public string ToShortString()
        {
            string str = string.Empty;
            if (this._bUseAutoDiscovery)
            {
                str = "WPAD: " + GetWPADUrl();
            }
            if (this._sPACScriptLocation == null)
            {
                return str;
            }
            if (str.Length > 0)
            {
                str = str + "; ";
            }
            return (str + "SCRIPT: " + this._sPACScriptLocation);
        }

        public override string ToString()
        {
            string str = null;
            if (this._bUseAutoDiscovery)
            {
                str = "\tDHCP/DNS url: " + GetWPADUrl() + " \n";
            }
            if (this._sPACScriptLocation != null)
            {
                str = str + "\tConfig script: " + this._sPACScriptLocation + "\n";
            }
            return (str ?? "\tDisabled");
        }
    }
}

