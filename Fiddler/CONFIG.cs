namespace Fiddler
{
    using Microsoft.Win32;
    using System;
    using System.Diagnostics;
    using System.Drawing;
    using System.Globalization;
    using System.IO;
    using System.Net.NetworkInformation;
    using System.Reflection;
    using System.Runtime.CompilerServices;
    using System.Security.Authentication;
    using System.Text;
    using System.Windows.Forms;

    public class CONFIG
    {
        internal static bool bAlwaysShowTrayIcon = false;
        public static bool bAttachOnBoot = true;
        public static bool bAutoLoadScript = true;
        public static bool bAutoProxyLogon = false;
        public static bool bAutoScroll = true;
        public static bool bBreakOnImages = false;
        public static bool bCaptureCONNECT = true;
        public static bool bCaptureFTP = false;
        internal static bool bCheckCompressionIntegrity = false;
        public static bool bDebugCertificateGeneration = true;
        internal static bool bDebugSpew = false;
        public static bool bEnableIPv6 = (Environment.OSVersion.Version.Major > 5);
        public static bool bForwardToGateway = true;
        public static bool bHideOnMinimize = false;
        internal static bool bHookAllConnections = true;
        internal static bool bHookWithPAC = false;
        private static bool bIgnoreServerCertErrors = false;
        internal static bool bIsBeta = false;
        internal static bool bIsViewOnly = false;
        public static bool bLoadExtensions = true;
        public static bool bLoadInspectors = true;
        public static bool bLoadScript = true;
        public static bool bMapSocketToProcess = true;
        public static bool bMITM_HTTPS = false;
        private static bool bQuietMode = !Environment.UserInteractive;
        public static bool bReportHTTPErrors = true;
        public static bool bResetCounterOnClear = true;
        public static bool bReuseClientSockets = true;
        public static bool bReuseServerSockets = true;
        internal static bool bRunningOnCLRv4 = (Environment.Version.Major > 3);
        public static bool bSearchUnmarkOldHits = true;
        internal static bool bShowDefaultClientCertificateNeededPrompt = true;
        public static bool bSmartScroll = true;
        public static bool bStackedLayout = false;
        internal static bool bStreamAudioVideo = true;
        public static bool bUseAESForSAZ = false;
        public static bool bUseEventLogForExceptions = false;
        internal static bool bUseXceedDecompressForDeflate = true;
        internal static bool bUseXceedDecompressForGZIP = false;
        internal static bool bUsingPortOverride = false;
        public static bool bVersionCheck = true;
        internal static bool bVersionCheckBlocked;
        public static System.Drawing.Color colorDisabledEdit = System.Drawing.Color.AliceBlue;
        public static Version FiddlerVersionInfo = Assembly.GetExecutingAssembly().GetName().Version;
        public static float flFontSize = 8.25f;
        internal const int I_MAX_CONNECTION_QUEUE = 50;
        public static int iHotkey = 70;
        public static int iHotkeyMod = 3;
        public static int iReporterUpdateInterval = 500;
        public static int iScriptReloadInterval = 0xbb8;
        internal static ProcessFilterCategories iShowProcessFilter;
        internal static uint iStartupCount = 0;
        private static bool m_bAllowRemoteConnections;
        private static bool m_bCheckForISA = true;
        private static bool m_bForceExclusivePort;
        private static string m_CompareTool = "windiff.exe";
        private static string m_JSEditor;
        private static int m_ListenPort = 0x22b8;
        public static string m_sAdditionalScriptReferences;
        private static string m_sHostsThatBypassFiddler;
        private static string m_TextEditor = "notepad.exe";
        public static SslProtocols oAcceptedClientHTTPSProtocols = (SslProtocols.Default | SslProtocols.Ssl2);
        public static SslProtocols oAcceptedServerHTTPSProtocols = SslProtocols.Default;
        public static Encoding oHeaderEncoding = Encoding.UTF8;
        internal static string sFiddlerListenHostPort = "127.0.0.1:8888";
        public static string sGatewayPassword;
        public static string sGatewayUsername;
        internal static string sHookConnectionNamed = "DefaultLAN";
        internal static string[] slDecryptBypassList = null;
        internal static string sMachineNameLowerCase = string.Empty;
        internal static string sMakeCertParamsEE = "-pe -ss my -n \"CN={0}{1}\" -sky exchange -in {2} -is my -eku 1.3.6.1.5.5.7.3.1 -cy end -a sha1 -m 120";
        internal static string sMakeCertParamsRoot = "-r -ss my -n \"CN={0}{1}\" -sky signature -eku 1.3.6.1.5.5.7.3.1 -h 1 -cy authority -a sha1 -m 120";
        internal static string sMakeCertRootCN = "DO_NOT_TRUST_FiddlerRoot";
        internal static string sMakeCertSubjectO = ", O=DO_NOT_TRUST, OU=Created by http://www.fiddler2.com";
        internal static string sReverseProxyHostname = "localhost";
        internal static string sRootKey = @"SOFTWARE\Microsoft\Fiddler2\";
        private static string sRootUrl = "http://www.fiddler2.com/fiddler2/";
        private static string sScriptPath = (sUserPath + @"Scripts\CustomRules.js");
        private static string sSecureRootUrl = "https://www.fiddler2.com/";
        private static string sUserPath = (GetPath("MyDocs") + @"\Fiddler2\");

        static CONFIG()
        {
            try
            {
                IPGlobalProperties iPGlobalProperties = IPGlobalProperties.GetIPGlobalProperties();
                string domainName = iPGlobalProperties.DomainName;
                IsMicrosoftMachine = !string.IsNullOrEmpty(domainName) && domainName.EndsWith("microsoft.com", StringComparison.OrdinalIgnoreCase);
                sMachineNameLowerCase = iPGlobalProperties.HostName.ToLower();
                iPGlobalProperties = null;
            }
            catch (Exception)
            {
                IsMicrosoftMachine = false;
            }
            RegistryKey oReg = Registry.LocalMachine.OpenSubKey(GetRegPath("LMIsBeta"));
            if (oReg != null)
            {
                bIsBeta = Utilities.GetRegistryBool(oReg, "IsBeta", bIsBeta);
                bVersionCheckBlocked = Utilities.GetRegistryBool(oReg, "BlockUpdateCheck", bVersionCheckBlocked);
                if (Utilities.GetRegistryBool(oReg, "ForceViewerMode", false))
                {
                    bIsViewOnly = true;
                }
                if (bVersionCheckBlocked)
                {
                    bVersionCheck = false;
                }
                oReg.Close();
            }
            oReg = Registry.CurrentUser.OpenSubKey(sRootKey);
            if (oReg != null)
            {
                m_bForceExclusivePort = Utilities.GetRegistryBool(oReg, "ExclusivePort", m_bForceExclusivePort);
                bUseEventLogForExceptions = Utilities.GetRegistryBool(oReg, "UseEventLogForExceptions", bUseEventLogForExceptions);
                m_bCheckForISA = Utilities.GetRegistryBool(oReg, "CheckForISA", m_bCheckForISA);
                m_TextEditor = (string) oReg.GetValue("TextEditor", m_TextEditor);
                m_CompareTool = (string) oReg.GetValue("CompareTool", m_CompareTool);
                bBreakOnImages = Utilities.GetRegistryBool(oReg, "BreakOnImages", bBreakOnImages);
                sHostsThatBypassFiddler = (string) oReg.GetValue("FiddlerBypass", string.Empty);
                sGatewayUsername = (string) oReg.GetValue("GatewayUsername", sGatewayUsername);
                sGatewayPassword = (string) oReg.GetValue("GatewayPassword", sGatewayPassword);
                sMakeCertParamsRoot = (string) oReg.GetValue("MakeCertParamsRoot", sMakeCertParamsRoot);
                sMakeCertParamsEE = (string) oReg.GetValue("MakeCertParamsEE", sMakeCertParamsEE);
                sMakeCertRootCN = (string) oReg.GetValue("MakeCertRootCN", sMakeCertRootCN);
                sMakeCertSubjectO = (string) oReg.GetValue("MakeCertSubjectO", sMakeCertSubjectO);
                m_JSEditor = (string) oReg.GetValue("JSEditor", m_JSEditor);
                ListenPort = Utilities.GetRegistryInt(oReg, "ListenPort", m_ListenPort);
                bLoadScript = Utilities.GetRegistryBool(oReg, "LoadScript", bLoadScript);
                bLoadInspectors = Utilities.GetRegistryBool(oReg, "LoadInspectors", bLoadInspectors);
                bLoadExtensions = Utilities.GetRegistryBool(oReg, "LoadExtensions", bLoadExtensions);
                foreach (string str2 in Environment.GetCommandLineArgs())
                {
                    if (str2.IndexOf("port:", StringComparison.OrdinalIgnoreCase) > -1)
                    {
                        int result = 0;
                        if (int.TryParse(str2.Substring(str2.IndexOf("port:", StringComparison.OrdinalIgnoreCase) + 5), NumberStyles.Integer, NumberFormatInfo.InvariantInfo, out result))
                        {
                            ListenPort = result;
                            bUsingPortOverride = true;
                        }
                    }
                    else if (str2.IndexOf("quiet", StringComparison.OrdinalIgnoreCase) == 1)
                    {
                        bQuietMode = true;
                        if (IsMicrosoftMachine)
                        {
                            FiddlerApplication.logSelfHost(110);
                        }
                    }
                    else if (str2.IndexOf("extoff", StringComparison.OrdinalIgnoreCase) == 1)
                    {
                        bLoadExtensions = bLoadInspectors = false;
                    }
                    else if (str2.IndexOf("noscript", StringComparison.OrdinalIgnoreCase) == 1)
                    {
                        bLoadScript = false;
                    }
                    else if (str2.IndexOf("viewer", StringComparison.OrdinalIgnoreCase) == 1)
                    {
                        bIsViewOnly = true;
                    }
                }
                iHotkeyMod = Utilities.GetRegistryInt(oReg, "HotkeyMod", iHotkeyMod);
                iHotkey = Utilities.GetRegistryInt(oReg, "Hotkey", iHotkey);
                flFontSize = Utilities.GetRegistryFloat(oReg, "FontSize", flFontSize);
                flFontSize = Math.Min(flFontSize, 24f);
                flFontSize = Math.Max(flFontSize, 4f);
                int argb = Utilities.GetRegistryInt(oReg, "colorDisabledEdit", -1);
                if (argb != -1)
                {
                    colorDisabledEdit = System.Drawing.Color.FromArgb(argb);
                }
                bAttachOnBoot = Utilities.GetRegistryBool(oReg, "AttachOnBoot", bAttachOnBoot);
                iStartupCount = (uint) (1 + Utilities.GetRegistryInt(oReg, "StartupCount", 0));
                bAutoLoadScript = Utilities.GetRegistryBool(oReg, "AutoReloadScript", bAutoLoadScript);
                m_bAllowRemoteConnections = Utilities.GetRegistryBool(oReg, "AllowRemote", m_bAllowRemoteConnections);
                bReuseServerSockets = Utilities.GetRegistryBool(oReg, "ReuseServerSockets", bReuseServerSockets);
                bReuseClientSockets = Utilities.GetRegistryBool(oReg, "ReuseClientSockets", bReuseClientSockets);
                bAutoProxyLogon = Utilities.GetRegistryBool(oReg, "AutoProxyLogon", bAutoProxyLogon);
                bDebugSpew = Utilities.GetRegistryBool(oReg, "DebugSpew", bDebugSpew);
                bReportHTTPErrors = Utilities.GetRegistryBool(oReg, "ReportHTTPErrors", bReportHTTPErrors);
                if (bDebugSpew)
                {
                    FiddlerApplication.Log.LogString("Fiddler DebugSpew is enabled.");
                    Trace.WriteLine("Fiddler DebugSpew is enabled.");
                }
                bHideOnMinimize = Utilities.GetRegistryBool(oReg, "HideOnMinimize", bHideOnMinimize);
                bAlwaysShowTrayIcon = Utilities.GetRegistryBool(oReg, "AlwaysShowTrayIcon", bAlwaysShowTrayIcon);
                bForwardToGateway = Utilities.GetRegistryBool(oReg, "UseGateway", bForwardToGateway);
                bEnableIPv6 = Utilities.GetRegistryBool(oReg, "EnableIPv6", bEnableIPv6);
                bCaptureCONNECT = Utilities.GetRegistryBool(oReg, "CaptureCONNECT", bCaptureCONNECT);
                bCaptureFTP = Utilities.GetRegistryBool(oReg, "CaptureFTP", bCaptureFTP);
                bMapSocketToProcess = Utilities.GetRegistryBool(oReg, "MapSocketToProcess", bMapSocketToProcess);
                bUseXceedDecompressForGZIP = Utilities.GetRegistryBool(oReg, "UseXceedDecompressForGZIP", bUseXceedDecompressForGZIP);
                bUseXceedDecompressForDeflate = Utilities.GetRegistryBool(oReg, "UseXceedDecompressForDeflate", bUseXceedDecompressForDeflate);
                bUseAESForSAZ = Utilities.GetRegistryBool(oReg, "UseAESForSAZ", bUseAESForSAZ);
                bStreamAudioVideo = Utilities.GetRegistryBool(oReg, "AutoStreamAudioVideo", bStreamAudioVideo);
                bShowDefaultClientCertificateNeededPrompt = Utilities.GetRegistryBool(oReg, "ShowDefaultClientCertificateNeededPrompt", bShowDefaultClientCertificateNeededPrompt);
                bMITM_HTTPS = Utilities.GetRegistryBool(oReg, "CaptureHTTPS", bMITM_HTTPS);
                bIgnoreServerCertErrors = Utilities.GetRegistryBool(oReg, "IgnoreServerCertErrors", bIgnoreServerCertErrors);
                iReverseProxyForPort = Utilities.GetRegistryInt(oReg, "ReverseProxyForPort", iReverseProxyForPort);
                sReverseProxyHostname = (string) oReg.GetValue("ReverseProxyHostname", sReverseProxyHostname);
                bVersionCheck = Utilities.GetRegistryBool(oReg, "CheckForUpdates", bVersionCheck);
                iShowProcessFilter = (ProcessFilterCategories) Utilities.GetRegistryInt(oReg, "ShowProcessFilter", (int) iShowProcessFilter);
                m_sAdditionalScriptReferences = (string) oReg.GetValue("ScriptReferences", string.Empty);
                sHookConnectionNamed = (string) oReg.GetValue("HookConnectionNamed", sHookConnectionNamed);
                bHookAllConnections = Utilities.GetRegistryBool(oReg, "HookAllConnections", bHookAllConnections);
                bHookWithPAC = Utilities.GetRegistryBool(oReg, "HookWithPAC", bHookWithPAC);
                if (oReg.GetValue("HeaderEncoding") != null)
                {
                    try
                    {
                        oHeaderEncoding = Encoding.GetEncoding((string) oReg.GetValue("HeaderEncoding"));
                    }
                    catch (Exception exception)
                    {
                        FiddlerApplication.DoNotifyUser(exception.Message, "Invalid HeaderEncoding specified in registry");
                        oHeaderEncoding = Encoding.UTF8;
                    }
                }
                sUserPath = (string) oReg.GetValue("UserPath", sUserPath);
                if (!sUserPath.EndsWith(Path.DirectorySeparatorChar.ToString()))
                {
                    sUserPath = sUserPath + Path.DirectorySeparatorChar;
                }
                sScriptPath = (string) oReg.GetValue("ScriptFullPath", sUserPath + @"Scripts\CustomRules.js");
                oReg.Close();
            }
            if ((Environment.OSVersion.Version.Major < 6) && (Environment.OSVersion.Version.Minor < 1))
            {
                bMapSocketToProcess = false;
            }
        }

        internal static void EnsureFoldersExist()
        {
            try
            {
                if (!Directory.Exists(GetPath("Captures")))
                {
                    Directory.CreateDirectory(GetPath("Captures"));
                }
                if (!Directory.Exists(GetPath("Requests")))
                {
                    Directory.CreateDirectory(GetPath("Requests"));
                }
                if (!Directory.Exists(GetPath("Responses")))
                {
                    Directory.CreateDirectory(GetPath("Responses"));
                }
                if (!Directory.Exists(GetPath("Scripts")))
                {
                    Directory.CreateDirectory(GetPath("Scripts"));
                }
            }
            catch (Exception exception)
            {
                FiddlerApplication.DoNotifyUser(exception.ToString(), "Folder Creation Failed");
            }
            try
            {
                if ((!FiddlerApplication.Prefs.GetBoolPref("fiddler.script.delaycreate", true) && !File.Exists(GetPath("CustomRules"))) && File.Exists(GetPath("SampleRules")))
                {
                    File.Copy(GetPath("SampleRules"), GetPath("CustomRules"));
                }
            }
            catch (Exception exception2)
            {
                FiddlerApplication.DoNotifyUser(exception2.ToString(), "Initial file copies failed");
            }
        }

        [CodeDescription("Return a filesystem path. Contact EricLaw for constants.")]
        public static string GetPath(string sWhatPath)
        {
            string folderPath;
            switch (sWhatPath)
            {
                case "App":
                    return (Path.GetDirectoryName(Application.ExecutablePath) + @"\");

                case "AutoFiddlers_Machine":
                    return (Path.GetDirectoryName(Application.ExecutablePath) + @"\Scripts\");

                case "AutoFiddlers_User":
                    return (sUserPath + @"Scripts\");

                case "AutoResponderDefaultRules":
                    return (sUserPath + @"\AutoResponder.xml");

                case "Captures":
                    return FiddlerApplication.Prefs.GetStringPref("fiddler.config.path.captures", sUserPath + @"Captures\");

                case "CustomRules":
                    return sScriptPath;

                case "DefaultClientCertificate":
                    return FiddlerApplication.Prefs.GetStringPref("fiddler.config.path.defaultclientcert", sUserPath + "ClientCertificate.cer");

                case "FiddlerRootCert":
                    return (sUserPath + "DO_NOT_TRUST_FiddlerRoot.cer");

                case "Inspectors":
                    return (Path.GetDirectoryName(Application.ExecutablePath) + @"\Inspectors\");

                case "PerUser-ISA-Config":
                    folderPath = "C:";
                    try
                    {
                        folderPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                    }
                    catch (Exception)
                    {
                    }
                    return (folderPath + @"\microsoft\firewall client 2004\management.ini");

                case "PerMachine-ISA-Config":
                    folderPath = "C:";
                    try
                    {
                        folderPath = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData);
                    }
                    catch (Exception)
                    {
                    }
                    return (folderPath + @"\microsoft\firewall client 2004\management.ini");

                case "MakeCert":
                    folderPath = FiddlerApplication.Prefs.GetStringPref("fiddler.config.path.makecert", Path.GetDirectoryName(Application.ExecutablePath) + @"\MakeCert.exe");
                    if (!File.Exists(folderPath))
                    {
                        folderPath = "MakeCert.exe";
                    }
                    return folderPath;

                case "MyDocs":
                    folderPath = "C:";
                    try
                    {
                        folderPath = Environment.GetFolderPath(Environment.SpecialFolder.Personal);
                    }
                    catch (Exception exception)
                    {
                        FiddlerApplication.DoNotifyUser("Initialization Error", "Failed to retrieve path to your My Documents folder.\nThis generally means you have a relative environment variable.\nDefaulting to C:\\\n\n" + exception.Message);
                    }
                    return folderPath;

                case "Pac":
                    return FiddlerApplication.Prefs.GetStringPref("fiddler.config.path.pac", sUserPath + @"Scripts\BrowserPAC.js");

                case "Requests":
                    return FiddlerApplication.Prefs.GetStringPref("fiddler.config.path.requests", sUserPath + @"Captures\Requests\");

                case "Responses":
                    return FiddlerApplication.Prefs.GetStringPref("fiddler.config.path.responses", sUserPath + @"Captures\Responses\");

                case "Root":
                    return sUserPath;

                case "SafeTemp":
                    folderPath = "C:";
                    try
                    {
                        folderPath = Environment.GetFolderPath(Environment.SpecialFolder.InternetCache);
                    }
                    catch (Exception exception2)
                    {
                        FiddlerApplication.DoNotifyUser("Failed to retrieve path to your Internet Cache folder.\nThis generally means you have a relative environment variable.\nDefaulting to C:\\\n\n" + exception2.Message, "GetPath(SafeTemp) Failed");
                    }
                    return folderPath;

                case "SampleRules":
                    return (Path.GetDirectoryName(Application.ExecutablePath) + @"\Scripts\SampleRules.js");

                case "Scripts":
                    return (sUserPath + @"Scripts\");

                case "TemplateResponses":
                    return FiddlerApplication.Prefs.GetStringPref("fiddler.config.path.templateresponses", Path.GetDirectoryName(Application.ExecutablePath) + @"\ResponseTemplates\");

                case "TextEditor":
                    return FiddlerApplication.Prefs.GetStringPref("fiddler.config.path.texteditor", m_TextEditor);

                case "Transcoders_Machine":
                    return (Path.GetDirectoryName(Application.ExecutablePath) + @"\ImportExport\");

                case "Transcoders_User":
                    return (sUserPath + @"ImportExport\");

                case "WINDIFF":
                {
                    string stringPref = FiddlerApplication.Prefs.GetStringPref("fiddler.config.path.comparetool", null);
                    if (string.IsNullOrEmpty(stringPref))
                    {
                        if (m_CompareTool == "windiff.exe")
                        {
                            if (File.Exists(GetPath("App") + "windiff.exe"))
                            {
                                return (GetPath("App") + "windiff.exe");
                            }
                            RegistryKey key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\App Paths\WinMerge.exe", false);
                            if (key != null)
                            {
                                string str3 = (string) key.GetValue(string.Empty, null);
                                key.Close();
                                if (str3 != null)
                                {
                                    return str3;
                                }
                            }
                        }
                        return m_CompareTool;
                    }
                    return stringPref;
                }
            }
            return "C:";
        }

        public static string GetRegPath(string sWhatPath)
        {
            switch (sWhatPath)
            {
                case "Root":
                    return sRootKey;

                case "LMIsBeta":
                    return sRootKey;

                case "MenuExt":
                    return (sRootKey + @"MenuExt\");

                case "UI":
                    return (sRootKey + @"UI\");

                case "Dynamic":
                    return (sRootKey + @"Dynamic\");

                case "Prefs":
                    return (sRootKey + @"Prefs\");
            }
            return sRootKey;
        }

        [CodeDescription("Return a special Url. Contact EricLaw for constants.")]
        public static string GetUrl(string sWhatUrl)
        {
            switch (sWhatUrl)
            {
                case "AutoResponderHelp":
                    return (sRootUrl + "help/AutoResponder.asp");

                case "ChangeList":
                    return (sSecureRootUrl + "fiddler2/version.asp?ver=");

                case "FiltersHelp":
                    return (sRootUrl + "help/Filters.asp");

                case "HelpContents":
                    return (sRootUrl + "help/?ver=");

                case "REDIR":
                    return "http://www.fiddler2.com/redir/?id=";

                case "VerCheck":
                    return (sRootUrl + "updatecheck.asp?isBeta=");

                case "InstallLatest":
                    if (bIsBeta)
                    {
                        return (sSecureRootUrl + "redir/?id=GetFiddler2Beta");
                    }
                    return (sSecureRootUrl + "redir/?id=GetFiddler2");

                case "ShopAmazon":
                    return "http://www.amazon.com/exec/obidos/redirect-home/baydensystems";
            }
            return sRootUrl;
        }

        internal static void PerformISAFirewallCheck()
        {
            if (m_bCheckForISA && !bQuietMode)
            {
                try
                {
                    if (File.Exists(GetPath("PerUser-ISA-Config")))
                    {
                        string str = File.OpenText(GetPath("PerUser-ISA-Config")).ReadToEnd();
                        if (str.Contains("EnableWebProxyAutoConfig=1"))
                        {
                            ShowISAFirewallWarning();
                            return;
                        }
                        if (str.Contains("EnableWebProxyAutoConfig=0"))
                        {
                            return;
                        }
                    }
                    if (File.Exists(GetPath("PerMachine-ISA-Config")) && File.OpenText(GetPath("PerMachine-ISA-Config")).ReadToEnd().Contains("EnableWebProxyAutoConfig=1"))
                    {
                        ShowISAFirewallWarning();
                    }
                }
                catch (Exception)
                {
                }
            }
        }

        internal static void PerformProxySettingsPerUserCheck()
        {
            try
            {
                RegistryKey oReg = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Policies\Microsoft\Windows\CurrentVersion\Internet Settings", false);
                if ((oReg != null) && (Utilities.GetRegistryInt(oReg, "ProxySettingsPerUser", 1) == 0))
                {
                    FiddlerApplication.Log.LogString("!WARNING: Fiddler has detected that system policy ProxySettingsPerUser = 0. Unless run as Administrator, Fiddler may not be able to capture traffic from Internet Explorer and other programs.");
                    oReg.Close();
                }
            }
            catch (Exception)
            {
            }
        }

        internal static void RetrieveFormSettings(frmViewer f)
        {
            if (Utilities.GetAsyncKeyState(0x10) < 0)
            {
                FiddlerApplication._frmSplash.Visible = false;
                if (DialogResult.Yes == MessageBox.Show("SHIFT Key is down. Start with default form layout?", "Safe mode", MessageBoxButtons.YesNo, MessageBoxIcon.Question))
                {
                    bRevertToDefaultLayout = true;
                    FiddlerApplication._frmSplash.Visible = true;
                    return;
                }
                FiddlerApplication._frmSplash.Visible = true;
            }
            try
            {
                RegistryKey oReg = Registry.CurrentUser.OpenSubKey(GetRegPath("UI"));
                if (oReg != null)
                {
                    bStackedLayout = Utilities.GetRegistryBool(oReg, "StackedLayout", false);
                    bAutoScroll = Utilities.GetRegistryBool(oReg, "AutoScroll", bSmartScroll);
                    bSearchUnmarkOldHits = Utilities.GetRegistryBool(oReg, "SearchUnmarkOldHits", bSearchUnmarkOldHits);
                    bSmartScroll = Utilities.GetRegistryBool(oReg, "SmartScroll", bSmartScroll);
                    bResetCounterOnClear = Utilities.GetRegistryBool(oReg, "ResetCounterOnClear", bResetCounterOnClear);
                    f.miSessionListScroll.Checked = bAutoScroll;
                    f.miViewAutoScroll.Checked = bAutoScroll;
                    f.Bounds = new Rectangle(Utilities.GetRegistryInt(oReg, f.Name + "_Left", f.Left), Utilities.GetRegistryInt(oReg, f.Name + "_Top", f.Top), Utilities.GetRegistryInt(oReg, f.Name + "_Width", f.Width), Utilities.GetRegistryInt(oReg, f.Name + "_Height", f.Height));
                    f.pnlSessions.Width = (int) oReg.GetValue("pnlSessions_Width", f.pnlSessions.Width);
                    f.tabsRequest.Height = (int) oReg.GetValue("tabsRequest_Height", f.tabsRequest.Height);
                }
                new Point(f.Left, f.Top);
                Screen screen = Screen.FromRectangle(f.DesktopBounds);
                Rectangle rectangle = Rectangle.Intersect(f.DesktopBounds, screen.WorkingArea);
                if (rectangle.IsEmpty || ((rectangle.Width * rectangle.Height) < ((0.2 * f.Width) * f.Height)))
                {
                    f.SetDesktopLocation(screen.WorkingArea.Left + 20, screen.WorkingArea.Top + 20);
                }
                if (oReg != null)
                {
                    FormWindowState state = (FormWindowState) oReg.GetValue(f.Name + "_WState", f.WindowState);
                    if (state != FormWindowState.Minimized)
                    {
                        f.WindowState = state;
                    }
                    if (f.pnlSessions.Width > (f.Width - 50))
                    {
                        f.pnlSessions.Width = f.Width - 0x2d;
                    }
                    if (f.tabsRequest.Height > (f.Height - 250))
                    {
                        f.tabsRequest.Height = f.Height - 0xf5;
                    }
                    if (f.pnlSessions.Width < 0x19)
                    {
                        f.pnlSessions.Width = 0x19;
                    }
                    if (f.tabsRequest.Height < 0x19)
                    {
                        f.tabsRequest.Height = 0x19;
                    }
                    string[] strArray = null;
                    string str = (string) oReg.GetValue("lvSessions_widths");
                    if (str != null)
                    {
                        strArray = str.Split(new char[] { ',' });
                        if ((strArray != null) && (strArray.Length < f.lvSessions.Columns.Count))
                        {
                            strArray = null;
                        }
                    }
                    str = (string) oReg.GetValue("lvSessions_order");
                    if (str != null)
                    {
                        string[] strArray2 = str.Split(new char[] { ',' });
                        int[] numArray = new int[strArray2.Length];
                        for (int i = 0; i < strArray2.Length; i++)
                        {
                            numArray[i] = int.Parse(strArray2[i]);
                        }
                        if (numArray.Length >= f.lvSessions.Columns.Count)
                        {
                            for (int j = 0; j < f.lvSessions.Columns.Count; j++)
                            {
                                Utilities.LV_COLUMN lParam = new Utilities.LV_COLUMN {
                                    mask = 0x20,
                                    iOrder = numArray[j]
                                };
                                Utilities.SendLVMessage(f.lvSessions.Handle, 0x1060, (IntPtr) j, ref lParam);
                                if (strArray != null)
                                {
                                    f.lvSessions.Columns[j].Width = int.Parse(strArray[j]);
                                }
                            }
                        }
                    }
                    oReg.Close();
                }
                if (bStackedLayout)
                {
                    f.actToggleStackedLayout(true);
                }
            }
            catch (Exception)
            {
            }
        }

        internal static void SaveSettings(frmViewer f)
        {
            if (!bIsViewOnly)
            {
                RegistryKey oReg = Registry.CurrentUser.CreateSubKey(GetRegPath("UI"));
                oReg.SetValue("StackedLayout", bStackedLayout);
                oReg.SetValue("AutoScroll", bAutoScroll);
                oReg.SetValue("SmartScroll", bSmartScroll);
                oReg.SetValue("SearchUnmarkOldHits", bSearchUnmarkOldHits);
                oReg.SetValue("ResetCounterOnClear", bResetCounterOnClear);
                oReg.SetValue(f.Name + "_WState", (int) f.WindowState);
                if (f.WindowState != FormWindowState.Normal)
                {
                    if (!f.Visible)
                    {
                        f.Visible = true;
                    }
                    f.WindowState = FormWindowState.Normal;
                }
                oReg.SetValue(f.Name + "_Top", f.Top);
                oReg.SetValue(f.Name + "_Left", f.Left);
                oReg.SetValue(f.Name + "_Height", f.Height);
                oReg.SetValue(f.Name + "_Width", f.Width);
                if (!f.miViewStacked.Checked)
                {
                    oReg.SetValue("pnlSessions_Width", f.pnlSessions.Width);
                }
                oReg.SetValue("tabsRequest_Height", f.tabsRequest.Height);
                string str = string.Empty;
                foreach (ColumnHeader header in f.lvSessions.Columns)
                {
                    Utilities.LV_COLUMN lParam = new Utilities.LV_COLUMN {
                        mask = 0x20
                    };
                    Utilities.SendLVMessage(f.lvSessions.Handle, 0x105f, (IntPtr) header.Index, ref lParam);
                    str = str + lParam.iOrder.ToString() + ",";
                }
                oReg.SetValue("lvSessions_order", str.Substring(0, str.Length - 1));
                str = string.Empty;
                foreach (ColumnHeader header2 in f.lvSessions.Columns)
                {
                    str = str + header2.Width.ToString() + ",";
                }
                oReg.SetValue("lvSessions_widths", str.Substring(0, str.Length - 1));
                oReg.Close();
                oReg = Registry.CurrentUser.CreateSubKey(sRootKey);
                oReg.SetValue("StartupCount", iStartupCount);
                Utilities.SetRegistryString(oReg, "FontSize", flFontSize.ToString(CultureInfo.InvariantCulture));
                oReg.SetValue("colorDisabledEdit", colorDisabledEdit.ToArgb());
                oReg.SetValue("AttachOnBoot", bAttachOnBoot);
                oReg.SetValue("AllowRemote", m_bAllowRemoteConnections);
                oReg.SetValue("ReuseServerSockets", bReuseServerSockets);
                oReg.SetValue("ReuseClientSockets", bReuseClientSockets);
                Utilities.SetRegistryString(oReg, "FiddlerBypass", sHostsThatBypassFiddler);
                oReg.SetValue("ReportHTTPErrors", bReportHTTPErrors);
                oReg.SetValue("UseGateway", bForwardToGateway);
                oReg.SetValue("CaptureCONNECT", bCaptureCONNECT);
                oReg.SetValue("CaptureFTP", bCaptureFTP);
                oReg.SetValue("EnableIPv6", bEnableIPv6);
                oReg.SetValue("BreakOnImages", bBreakOnImages);
                oReg.SetValue("MapSocketToProcess", bMapSocketToProcess);
                oReg.SetValue("UseAESForSaz", bUseAESForSAZ);
                oReg.SetValue("AutoStreamAudioVideo", bStreamAudioVideo);
                oReg.SetValue("CaptureHTTPS", bMITM_HTTPS);
                oReg.SetValue("IgnoreServerCertErrors", bIgnoreServerCertErrors);
                oReg.SetValue("CheckForUpdates", bVersionCheck);
                oReg.SetValue("HookAllConnections", bHookAllConnections);
                oReg.SetValue("HookWithPAC", bHookWithPAC);
                oReg.SetValue("ShowProcessFilter", (int) iShowProcessFilter);
                oReg.SetValue("AutoReloadScript", bAutoLoadScript);
                oReg.SetValue("HideOnMinimize", bHideOnMinimize);
                oReg.SetValue("AlwaysShowTrayIcon", bAlwaysShowTrayIcon);
                if (!bUsingPortOverride)
                {
                    oReg.SetValue("ListenPort", m_ListenPort);
                }
                oReg.SetValue("HotkeyMod", iHotkeyMod);
                oReg.SetValue("Hotkey", iHotkey);
                oReg.SetValue("CheckForISA", m_bCheckForISA);
                Utilities.SetRegistryString(oReg, "ScriptReferences", m_sAdditionalScriptReferences);
                Utilities.SetRegistryString(oReg, "JSEditor", m_JSEditor);
            }
        }

        internal static void SetNoDecryptList(string sNewList)
        {
            if (sNewList == null)
            {
                slDecryptBypassList = null;
            }
            else
            {
                slDecryptBypassList = sNewList.Trim().ToLower().Split(new char[] { ',', ';', '\t', ' ', '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
                if (slDecryptBypassList.Length < 1)
                {
                    slDecryptBypassList = null;
                }
            }
        }

        internal static void ShowISAFirewallWarning()
        {
            DialogResult no;
            if (bQuietMode)
            {
                no = DialogResult.No;
            }
            else
            {
                no = MessageBox.Show("Fiddler has detected that you may be running Microsoft Firewall client\nin Web Browser Automatic Configuration mode. This may cause\nFiddler to detach from Internet Explorer unexpectedly.\n\nWould you like to learn more?\n\nTo disable this warning, click 'Cancel'.", "Possible Conflict Detected", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Asterisk);
            }
            switch (no)
            {
                case DialogResult.Yes:
                    Utilities.LaunchHyperlink(GetUrl("REDIR") + "ISA");
                    return;

                case DialogResult.Cancel:
                    m_bCheckForISA = false;
                    break;
            }
        }

        [CodeDescription("Returns true if Fiddler is configured to accept remote clients.")]
        public static bool bAllowRemoteConnections
        {
            get
            {
                return m_bAllowRemoteConnections;
            }
            internal set
            {
                m_bAllowRemoteConnections = value;
            }
        }

        public static bool bRevertToDefaultLayout
        {
            [CompilerGenerated]
            get;
            //{
            //    return _bRevertToDefaultLayout_k__BackingField;
            //}
            [CompilerGenerated]
            private set;
            //{
            //    _bRevertToDefaultLayout_k__BackingField = value;
            //}
        }

        public static bool ForceExclusivePort
        {
            get
            {
                return m_bForceExclusivePort;
            }
            internal set
            {
                m_bForceExclusivePort = value;
            }
        }

        public static bool IgnoreServerCertErrors
        {
            get
            {
                return bIgnoreServerCertErrors;
            }
            set
            {
                bIgnoreServerCertErrors = value;
            }
        }

        public static int iReverseProxyForPort
        {
            [CompilerGenerated]
            get;
            //{
            //    return _iReverseProxyForPort_k__BackingField;
            //}
            [CompilerGenerated]
            set;
            //{
            //    _iReverseProxyForPort_k__BackingField = value;
            //}
        }

        internal static bool IsMicrosoftMachine
        {
            [CompilerGenerated]
            get;
            //{
            //    return _IsMicrosoftMachine_k__BackingField;
            //}
            [CompilerGenerated]
            set;
            //{
            //    _IsMicrosoftMachine_k__BackingField = value;
            //}
        }

        [CodeDescription("Return path to user's FiddlerScript editor.")]
        public static string JSEditor
        {
            get
            {
                if ((m_JSEditor == null) || (m_JSEditor.Length < 1))
                {
                    m_JSEditor = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles) + @"\Microsoft Office\office11\mse7.exe";
                    if (!File.Exists(m_JSEditor))
                    {
                        m_JSEditor = "notepad.exe";
                    }
                }
                return m_JSEditor;
            }
            set
            {
                m_JSEditor = value;
            }
        }

        public static int ListenPort
        {
            get
            {
                return m_ListenPort;
            }
            internal set
            {
                if ((value > 0) && (value < 0x10000))
                {
                    m_ListenPort = value;
                    sFiddlerListenHostPort = Utilities.TrimAfter(sFiddlerListenHostPort, ':') + ":" + m_ListenPort.ToString();
                }
            }
        }

        public static bool QuietMode
        {
            get
            {
                return bQuietMode;
            }
            set
            {
                bQuietMode = value;
            }
        }

        public static string sHostsThatBypassFiddler
        {
            get
            {
                return m_sHostsThatBypassFiddler;
            }
            set
            {
                string str = value;
                if (str == null)
                {
                    str = string.Empty;
                }
                if ((str.IndexOf("<-loopback>", StringComparison.OrdinalIgnoreCase) < 0) && (str.IndexOf("<loopback>", StringComparison.OrdinalIgnoreCase) < 0))
                {
                    str = "<-loopback>;" + str;
                }
                m_sHostsThatBypassFiddler = str;
            }
        }

        public static string sScriptReferences
        {
            get
            {
                return ("mscorlib.dll;system.dll;system.windows.forms.dll;" + Assembly.GetExecutingAssembly().Location + ";" + m_sAdditionalScriptReferences);
            }
        }
    }
}

