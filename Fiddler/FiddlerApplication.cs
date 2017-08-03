namespace Fiddler
{
    using System;
    using System.Collections.Generic;
    using System.Configuration;
    using System.Diagnostics;
    using System.Net;
    using System.Net.Security;
    using System.Reflection;
    using System.Runtime.CompilerServices;
    using System.Runtime.InteropServices;
    using System.Security.Cryptography.X509Certificates;
    using System.Threading;
    using System.Windows.Forms;
    using Xceed.Compression.Formats;
    using Xceed.Zip;

    public class FiddlerApplication
    {
        internal static AutoResponder _AutoResponder;
        private static bool _bSuppressReportUpdates = false;
        internal static frmViewer _frmMain;
        internal static SplashScreen _frmSplash;
        internal static int _iShowOnlyPID;
        internal static Logger _Log = new Logger(true);
        internal static PreferenceBag _Prefs = new PreferenceBag(CONFIG.GetRegPath("Prefs"));
        private static SessionStateHandler _AfterSessionComplete;
        private static SessionStateHandler _BeforeRequest;
        private static SessionStateHandler _BeforeResponse;
        private static SessionStateHandler _BeforeReturningError;
        private static CalculateReportHandler _CalculateReport;
        private static SimpleEventHandler _FiddlerAttach;
        private static SimpleEventHandler _FiddlerBoot;
        private static SimpleEventHandler _FiddlerDetach;
        private static SimpleEventHandler _FiddlerShutdown;
        internal static bool isClosing;
        internal static readonly PeriodicWorker Janitor = new PeriodicWorker();
        public static X509Certificate oDefaultClientCertificate;
        [CodeDescription("Fiddler's loaded extensions.")]
        public static FiddlerExtensions oExtensions;
        internal static Inspectors oInspectors;
        private static EventHandler<NotificationEventArgs> _OnNotification;
        [CodeDescription("Fiddler's core proxy engine.")]
        public static Proxy oProxy;
        public static FiddlerTranscoders oTranscoders = new FiddlerTranscoders();
        private static OverrideCertificatePolicyHandler _OverrideServerCertificateValidation;
        internal static Report Reporter;
        private static SessionStateHandler _RequestHeadersAvailable;
        private static SessionStateHandler _ResponseHeadersAvailable;
        public static FiddlerScript scriptRules;

        public static  event SessionStateHandler AfterSessionComplete
        {
            add
            {
                SessionStateHandler handler2;
                SessionStateHandler afterSessionComplete = _AfterSessionComplete;
                do
                {
                    handler2 = afterSessionComplete;
                    SessionStateHandler handler3 = (SessionStateHandler) Delegate.Combine(handler2, value);
                    afterSessionComplete = Interlocked.CompareExchange<SessionStateHandler>(ref _AfterSessionComplete, handler3, handler2);
                }
                while (afterSessionComplete != handler2);
            }
            remove
            {
                SessionStateHandler handler2;
                SessionStateHandler afterSessionComplete = _AfterSessionComplete;
                do
                {
                    handler2 = afterSessionComplete;
                    SessionStateHandler handler3 = (SessionStateHandler) Delegate.Remove(handler2, value);
                    afterSessionComplete = Interlocked.CompareExchange<SessionStateHandler>(ref _AfterSessionComplete, handler3, handler2);
                }
                while (afterSessionComplete != handler2);
            }
        }

        public static  event SessionStateHandler BeforeRequest
        {
            add
            {
                SessionStateHandler handler2;
                SessionStateHandler beforeRequest = _BeforeRequest;
                do
                {
                    handler2 = beforeRequest;
                    SessionStateHandler handler3 = (SessionStateHandler) Delegate.Combine(handler2, value);
                    beforeRequest = Interlocked.CompareExchange<SessionStateHandler>(ref _BeforeRequest, handler3, handler2);
                }
                while (beforeRequest != handler2);
            }
            remove
            {
                SessionStateHandler handler2;
                SessionStateHandler beforeRequest = _BeforeRequest;
                do
                {
                    handler2 = beforeRequest;
                    SessionStateHandler handler3 = (SessionStateHandler) Delegate.Remove(handler2, value);
                    beforeRequest = Interlocked.CompareExchange<SessionStateHandler>(ref _BeforeRequest, handler3, handler2);
                }
                while (beforeRequest != handler2);
            }
        }

        public static  event SessionStateHandler BeforeResponse
        {
            add
            {
                SessionStateHandler handler2;
                SessionStateHandler beforeResponse = _BeforeResponse;
                do
                {
                    handler2 = beforeResponse;
                    SessionStateHandler handler3 = (SessionStateHandler) Delegate.Combine(handler2, value);
                    beforeResponse = Interlocked.CompareExchange<SessionStateHandler>(ref _BeforeResponse, handler3, handler2);
                }
                while (beforeResponse != handler2);
            }
            remove
            {
                SessionStateHandler handler2;
                SessionStateHandler beforeResponse = _BeforeResponse;
                do
                {
                    handler2 = beforeResponse;
                    SessionStateHandler handler3 = (SessionStateHandler) Delegate.Remove(handler2, value);
                    beforeResponse = Interlocked.CompareExchange<SessionStateHandler>(ref _BeforeResponse, handler3, handler2);
                }
                while (beforeResponse != handler2);
            }
        }

        public static  event SessionStateHandler BeforeReturningError
        {
            add
            {
                SessionStateHandler handler2;
                SessionStateHandler beforeReturningError = _BeforeReturningError;
                do
                {
                    handler2 = beforeReturningError;
                    SessionStateHandler handler3 = (SessionStateHandler) Delegate.Combine(handler2, value);
                    beforeReturningError = Interlocked.CompareExchange<SessionStateHandler>(ref _BeforeReturningError, handler3, handler2);
                }
                while (beforeReturningError != handler2);
            }
            remove
            {
                SessionStateHandler handler2;
                SessionStateHandler beforeReturningError = _BeforeReturningError;
                do
                {
                    handler2 = beforeReturningError;
                    SessionStateHandler handler3 = (SessionStateHandler) Delegate.Remove(handler2, value);
                    beforeReturningError = Interlocked.CompareExchange<SessionStateHandler>(ref _BeforeReturningError, handler3, handler2);
                }
                while (beforeReturningError != handler2);
            }
        }

        [CodeDescription("Sync this event to capture the CalculateReport event, summarizing the selected sessions.")]
        public static  event CalculateReportHandler CalculateReport
        {
            add
            {
                CalculateReportHandler handler2;
                CalculateReportHandler calculateReport = _CalculateReport;
                do
                {
                    handler2 = calculateReport;
                    CalculateReportHandler handler3 = (CalculateReportHandler) Delegate.Combine(handler2, value);
                    calculateReport = Interlocked.CompareExchange<CalculateReportHandler>(ref _CalculateReport, handler3, handler2);
                }
                while (calculateReport != handler2);
            }
            remove
            {
                CalculateReportHandler handler2;
                CalculateReportHandler calculateReport = _CalculateReport;
                do
                {
                    handler2 = calculateReport;
                    CalculateReportHandler handler3 = (CalculateReportHandler) Delegate.Remove(handler2, value);
                    calculateReport = Interlocked.CompareExchange<CalculateReportHandler>(ref _CalculateReport, handler3, handler2);
                }
                while (calculateReport != handler2);
            }
        }

        [CodeDescription("Sync this event to be notified when FiddlerCore has attached as the system proxy.")]
        public static  event SimpleEventHandler FiddlerAttach
        {
            add
            {
                SimpleEventHandler handler2;
                SimpleEventHandler fiddlerAttach = _FiddlerAttach;
                do
                {
                    handler2 = fiddlerAttach;
                    SimpleEventHandler handler3 = (SimpleEventHandler) Delegate.Combine(handler2, value);
                    fiddlerAttach = Interlocked.CompareExchange<SimpleEventHandler>(ref _FiddlerAttach, handler3, handler2);
                }
                while (fiddlerAttach != handler2);
            }
            remove
            {
                SimpleEventHandler handler2;
                SimpleEventHandler fiddlerAttach = _FiddlerAttach;
                do
                {
                    handler2 = fiddlerAttach;
                    SimpleEventHandler handler3 = (SimpleEventHandler) Delegate.Remove(handler2, value);
                    fiddlerAttach = Interlocked.CompareExchange<SimpleEventHandler>(ref _FiddlerAttach, handler3, handler2);
                }
                while (fiddlerAttach != handler2);
            }
        }

        [CodeDescription("Sync this event to be notified when Fiddler has completed startup.")]
        public static  event SimpleEventHandler FiddlerBoot
        {
            add
            {
                SimpleEventHandler handler2;
                SimpleEventHandler fiddlerBoot = _FiddlerBoot;
                do
                {
                    handler2 = fiddlerBoot;
                    SimpleEventHandler handler3 = (SimpleEventHandler) Delegate.Combine(handler2, value);
                    fiddlerBoot = Interlocked.CompareExchange<SimpleEventHandler>(ref _FiddlerBoot, handler3, handler2);
                }
                while (fiddlerBoot != handler2);
            }
            remove
            {
                SimpleEventHandler handler2;
                SimpleEventHandler fiddlerBoot = _FiddlerBoot;
                do
                {
                    handler2 = fiddlerBoot;
                    SimpleEventHandler handler3 = (SimpleEventHandler) Delegate.Remove(handler2, value);
                    fiddlerBoot = Interlocked.CompareExchange<SimpleEventHandler>(ref _FiddlerBoot, handler3, handler2);
                }
                while (fiddlerBoot != handler2);
            }
        }

        [CodeDescription("Sync this event to be notified when FiddlerCore has detached as the system proxy.")]
        public static  event SimpleEventHandler FiddlerDetach
        {
            add
            {
                SimpleEventHandler handler2;
                SimpleEventHandler fiddlerDetach = _FiddlerDetach;
                do
                {
                    handler2 = fiddlerDetach;
                    SimpleEventHandler handler3 = (SimpleEventHandler) Delegate.Combine(handler2, value);
                    fiddlerDetach = Interlocked.CompareExchange<SimpleEventHandler>(ref _FiddlerDetach, handler3, handler2);
                }
                while (fiddlerDetach != handler2);
            }
            remove
            {
                SimpleEventHandler handler2;
                SimpleEventHandler fiddlerDetach = _FiddlerDetach;
                do
                {
                    handler2 = fiddlerDetach;
                    SimpleEventHandler handler3 = (SimpleEventHandler) Delegate.Remove(handler2, value);
                    fiddlerDetach = Interlocked.CompareExchange<SimpleEventHandler>(ref _FiddlerDetach, handler3, handler2);
                }
                while (fiddlerDetach != handler2);
            }
        }

        [CodeDescription("Sync this event to be notified when Fiddler shuts down.")]
        public static  event SimpleEventHandler FiddlerShutdown
        {
            add
            {
                SimpleEventHandler handler2;
                SimpleEventHandler fiddlerShutdown = _FiddlerShutdown;
                do
                {
                    handler2 = fiddlerShutdown;
                    SimpleEventHandler handler3 = (SimpleEventHandler) Delegate.Combine(handler2, value);
                    fiddlerShutdown = Interlocked.CompareExchange<SimpleEventHandler>(ref _FiddlerShutdown, handler3, handler2);
                }
                while (fiddlerShutdown != handler2);
            }
            remove
            {
                SimpleEventHandler handler2;
                SimpleEventHandler fiddlerShutdown = _FiddlerShutdown;
                do
                {
                    handler2 = fiddlerShutdown;
                    SimpleEventHandler handler3 = (SimpleEventHandler) Delegate.Remove(handler2, value);
                    fiddlerShutdown = Interlocked.CompareExchange<SimpleEventHandler>(ref _FiddlerShutdown, handler3, handler2);
                }
                while (fiddlerShutdown != handler2);
            }
        }

        public static  event EventHandler<NotificationEventArgs> OnNotification
        {
            add
            {
                EventHandler<NotificationEventArgs> handler2;
                EventHandler<NotificationEventArgs> onNotification = _OnNotification;
                do
                {
                    handler2 = onNotification;
                    EventHandler<NotificationEventArgs> handler3 = (EventHandler<NotificationEventArgs>) Delegate.Combine(handler2, value);
                    onNotification = Interlocked.CompareExchange<EventHandler<NotificationEventArgs>>(ref _OnNotification, handler3, handler2);
                }
                while (onNotification != handler2);
            }
            remove
            {
                EventHandler<NotificationEventArgs> handler2;
                EventHandler<NotificationEventArgs> onNotification = _OnNotification;
                do
                {
                    handler2 = onNotification;
                    EventHandler<NotificationEventArgs> handler3 = (EventHandler<NotificationEventArgs>) Delegate.Remove(handler2, value);
                    onNotification = Interlocked.CompareExchange<EventHandler<NotificationEventArgs>>(ref _OnNotification, handler3, handler2);
                }
                while (onNotification != handler2);
            }
        }

        public static  event OverrideCertificatePolicyHandler OverrideServerCertificateValidation
        {
            add
            {
                OverrideCertificatePolicyHandler handler2;
                OverrideCertificatePolicyHandler overrideServerCertificateValidation = _OverrideServerCertificateValidation;
                do
                {
                    handler2 = overrideServerCertificateValidation;
                    OverrideCertificatePolicyHandler handler3 = (OverrideCertificatePolicyHandler) Delegate.Combine(handler2, value);
                    overrideServerCertificateValidation = Interlocked.CompareExchange<OverrideCertificatePolicyHandler>(ref _OverrideServerCertificateValidation, handler3, handler2);
                }
                while (overrideServerCertificateValidation != handler2);
            }
            remove
            {
                OverrideCertificatePolicyHandler handler2;
                OverrideCertificatePolicyHandler overrideServerCertificateValidation = _OverrideServerCertificateValidation;
                do
                {
                    handler2 = overrideServerCertificateValidation;
                    OverrideCertificatePolicyHandler handler3 = (OverrideCertificatePolicyHandler) Delegate.Remove(handler2, value);
                    overrideServerCertificateValidation = Interlocked.CompareExchange<OverrideCertificatePolicyHandler>(ref _OverrideServerCertificateValidation, handler3, handler2);
                }
                while (overrideServerCertificateValidation != handler2);
            }
        }

        public static  event SessionStateHandler RequestHeadersAvailable
        {
            add
            {
                SessionStateHandler handler2;
                SessionStateHandler requestHeadersAvailable = _RequestHeadersAvailable;
                do
                {
                    handler2 = requestHeadersAvailable;
                    SessionStateHandler handler3 = (SessionStateHandler) Delegate.Combine(handler2, value);
                    requestHeadersAvailable = Interlocked.CompareExchange<SessionStateHandler>(ref _RequestHeadersAvailable, handler3, handler2);
                }
                while (requestHeadersAvailable != handler2);
            }
            remove
            {
                SessionStateHandler handler2;
                SessionStateHandler requestHeadersAvailable = _RequestHeadersAvailable;
                do
                {
                    handler2 = requestHeadersAvailable;
                    SessionStateHandler handler3 = (SessionStateHandler) Delegate.Remove(handler2, value);
                    requestHeadersAvailable = Interlocked.CompareExchange<SessionStateHandler>(ref _RequestHeadersAvailable, handler3, handler2);
                }
                while (requestHeadersAvailable != handler2);
            }
        }

        public static  event SessionStateHandler ResponseHeadersAvailable
        {
            add
            {
                SessionStateHandler handler2;
                SessionStateHandler responseHeadersAvailable = _ResponseHeadersAvailable;
                do
                {
                    handler2 = responseHeadersAvailable;
                    SessionStateHandler handler3 = (SessionStateHandler) Delegate.Combine(handler2, value);
                    responseHeadersAvailable = Interlocked.CompareExchange<SessionStateHandler>(ref _ResponseHeadersAvailable, handler3, handler2);
                }
                while (responseHeadersAvailable != handler2);
            }
            remove
            {
                SessionStateHandler handler2;
                SessionStateHandler responseHeadersAvailable = _ResponseHeadersAvailable;
                do
                {
                    handler2 = responseHeadersAvailable;
                    SessionStateHandler handler3 = (SessionStateHandler) Delegate.Remove(handler2, value);
                    responseHeadersAvailable = Interlocked.CompareExchange<SessionStateHandler>(ref _ResponseHeadersAvailable, handler3, handler2);
                }
                while (responseHeadersAvailable != handler2);
            }
        }

        static FiddlerApplication()
        {
            if (CONFIG.IsMicrosoftMachine && Prefs.GetBoolPref("microsoft.selfhost.optout", false))
            {
                CONFIG.IsMicrosoftMachine = false;
            }
        }

        private FiddlerApplication()
        {
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void _SetXceedLicenseKeys()
        {
            Xceed.Zip.Licenser.LicenseKey = "ZIN51GEWHTK4XMKZNXA";
            Xceed.Compression.Formats.Licenser.LicenseKey = "FTN51TEWHTA4ZCY3NXA";
        }

        [CodeDescription("Notify the user that an event has occured; shows a balloon tip if Fiddler is minimized to the system tray.")]
        public static void AlertUser(string sTitle, string sMessage)
        {
            if ((_frmMain != null) && (_frmMain.notifyIcon != null))
            {
                _frmMain.notifyIcon.ShowBalloonTip(0x3e8, sTitle, sMessage, ToolTipIcon.Info);
            }
        }

        internal static bool CheckOverrideCertificatePolicy(Session oS, string sExpectedCN, X509Certificate ServerCertificate, X509Chain ServerCertificateChain, SslPolicyErrors sslPolicyErrors, out bool bTreatCertificateAsValid)
        {
            if (_OverrideServerCertificateValidation != null)
            {
                return _OverrideServerCertificateValidation(oS, sExpectedCN, ServerCertificate, ServerCertificateChain, sslPolicyErrors, out bTreatCertificateAsValid);
            }
            bTreatCertificateAsValid = false;
            return false;
        }

        internal static void DebugSpew(string sMessage)
        {
            if (CONFIG.bDebugSpew)
            {
                Trace.WriteLine(sMessage);
            }
        }

        internal static void DoAfterSessionComplete(Session oSession)
        {
            if (_AfterSessionComplete != null)
            {
                _AfterSessionComplete(oSession);
            }
        }

        internal static void DoBeforeRequest(Session oSession)
        {
            if (_BeforeRequest != null)
            {
                _BeforeRequest(oSession);
            }
        }

        internal static void DoBeforeResponse(Session oSession)
        {
            if (_BeforeResponse != null)
            {
                _BeforeResponse(oSession);
            }
        }

        internal static void DoBeforeReturningError(Session oSession)
        {
            if (_BeforeReturningError != null)
            {
                _BeforeReturningError(oSession);
            }
            oExtensions.DoBeforeReturningError(oSession);
        }

        public static bool DoExport(string sExportFormat, Session[] oSessions, Dictionary<string, object> dictOptions, EventHandler<ProgressCallbackEventArgs> ehPCEA)
        {
            if (string.IsNullOrEmpty(sExportFormat))
            {
                return false;
            }
            TranscoderTuple tuple = oTranscoders.GetExporter(sExportFormat);
            if (tuple == null)
            {
                return false;
            }
            bool flag = false;
            try
            {
                if (CONFIG.IsMicrosoftMachine)
                {
                    logSelfHost(130);
                }
                UI.UseWaitCursor = true;
                ISessionExporter exporter = (ISessionExporter) Activator.CreateInstance(tuple.typeFormatter);
                if (ehPCEA == null)
                {
                    ehPCEA = delegate (object sender, ProgressCallbackEventArgs oPCE) {
                        string str = (oPCE.PercentComplete > 0) ? ("Export is " + oPCE.PercentComplete + "% complete; ") : string.Empty;
                        Log.LogFormat("{0}{1}", new object[] { str, oPCE.ProgressText });
                        Application.DoEvents();
                    };
                }
                flag = exporter.ExportSessions(sExportFormat, oSessions, dictOptions, ehPCEA);
                exporter.Dispose();
            }
            catch (Exception exception)
            {
                LogAddonException(exception, "Exporter for " + sExportFormat + " failed.");
                flag = false;
            }
            UI.UseWaitCursor = false;
            return flag;
        }

        public static Session[] DoImport(string sImportFormat, bool bAddToSessionList, Dictionary<string, object> dictOptions, EventHandler<ProgressCallbackEventArgs> ehPCEA)
        {
            Session[] sessionArray;
            if (string.IsNullOrEmpty(sImportFormat))
            {
                return null;
            }
            TranscoderTuple tuple = oTranscoders.GetImporter(sImportFormat);
            if (tuple == null)
            {
                return null;
            }
            try
            {
                if (CONFIG.IsMicrosoftMachine)
                {
                    logSelfHost(120);
                }
                UI.UseWaitCursor = true;
                ISessionImporter importer = (ISessionImporter) Activator.CreateInstance(tuple.typeFormatter);
                if (ehPCEA == null)
                {
                    ehPCEA = delegate (object sender, ProgressCallbackEventArgs oPCE) {
                        string str = (oPCE.PercentComplete > 0) ? ("Import is " + oPCE.PercentComplete + "% complete; ") : string.Empty;
                        Log.LogFormat("{0}{1}", new object[] { str, oPCE.ProgressText });
                        Application.DoEvents();
                    };
                }
                sessionArray = importer.ImportSessions(sImportFormat, dictOptions, ehPCEA);
                importer.Dispose();
                if (sessionArray == null)
                {
                    UI.UseWaitCursor = false;
                    return null;
                }
                if (bAddToSessionList)
                {
                    try
                    {
                        UI.lvSessions.BeginUpdate();
                        foreach (Session session in sessionArray)
                        {
                            session.SetBitFlag(SessionFlags.ImportedFromOtherTool, true);
                            UI.addSession(session);
                            UI.finishSession(session);
                        }
                    }
                    finally
                    {
                        UI.lvSessions.EndUpdate();
                    }
                }
            }
            catch (Exception exception)
            {
                LogAddonException(exception, "Importer for " + sImportFormat + " failed.");
                sessionArray = null;
            }
            UI.UseWaitCursor = false;
            return sessionArray;
        }

        internal static void DoNotifyUser(string sMessage, string sTitle)
        {
            DoNotifyUser(sMessage, sTitle, MessageBoxIcon.None);
        }

        internal static void DoNotifyUser(string sMessage, string sTitle, MessageBoxIcon oIcon)
        {
            if (_OnNotification != null)
            {
                NotificationEventArgs e = new NotificationEventArgs(string.Format("{0} - {1}", sTitle, sMessage));
                _OnNotification(null, e);
            }
            if (!CONFIG.QuietMode)
            {
                MessageBox.Show(sMessage, sTitle, MessageBoxButtons.OK, oIcon);
            }
        }

        internal static void DoRequestHeadersAvailable(Session oSession)
        {
            if (_RequestHeadersAvailable != null)
            {
                _RequestHeadersAvailable(oSession);
            }
            oExtensions.DoPeekAtRequestHeaders(oSession);
        }

        internal static void DoResponseHeadersAvailable(Session oSession)
        {
            if (_ResponseHeadersAvailable != null)
            {
                _ResponseHeadersAvailable(oSession);
            }
            oExtensions.DoPeekAtResponseHeaders(oSession);
        }

        public static string GetDetailedInfo()
        {
            string str2 = string.Empty;
            string str = str2 + "\nRunning on: " + CONFIG.sMachineNameLowerCase + ":" + oProxy.ListenPort.ToString() + "\n";
            if (CONFIG.bHookAllConnections)
            {
                str = str + "Listening to: All Adapters\n";
            }
            else
            {
                str = str + "Listening to: " + (CONFIG.sHookConnectionNamed ?? "Default LAN") + "\n";
            }
            if (CONFIG.iReverseProxyForPort > 0)
            {
                object obj2 = str;
                str = string.Concat(new object[] { obj2, "Acting as reverse proxy for port #", CONFIG.iReverseProxyForPort, "\n" });
            }
            if (oProxy.oAutoProxy != null)
            {
                str = str + "Gateway: Using Script\n" + oProxy.oAutoProxy.ToString();
            }
            else
            {
                IPEndPoint point = oProxy.FindGatewayForOrigin("http", "www.fiddler2.com");
                if (point != null)
                {
                    string str3 = str;
                    str = str3 + "Gateway: " + point.Address.ToString() + ":" + point.Port.ToString();
                }
                else
                {
                    str = str + "Gateway: No Gateway";
                }
            }
            return string.Format("Fiddler Web Debugger ({0})\n{1}, VM: {2:N2}mb, WS: {3:N2}mb\n{4}\n{5}\n\nYou've started Fiddler: {6:N0} times.\n{7}\n", new object[] { CONFIG.bIsBeta ? string.Format("v{0} beta", Application.ProductVersion) : string.Format("v{0}", Application.ProductVersion), (8 == IntPtr.Size) ? "64-bit" : "32-bit", Process.GetCurrentProcess().PagedMemorySize64 / 0x100000L, Process.GetCurrentProcess().WorkingSet64 / 0x100000L, ".NET " + Environment.Version, Environment.OSVersion.VersionString, CONFIG.iStartupCount, str });
        }

        public static string GetVersionString()
        {
            FileVersionInfo versionInfo = FileVersionInfo.GetVersionInfo(Assembly.GetExecutingAssembly().Location);
            string str = " (+SAZ)";
            string str2 = "Fiddler";
            return string.Format("{0}/{1}.{2}.{3}.{4}{5}", new object[] { str2, versionInfo.FileMajorPart, versionInfo.FileMinorPart, versionInfo.FileBuildPart, versionInfo.FilePrivatePart, str });
        }

        internal static void HandleHTTPError(Session oSession, SessionFlags flagViolation, bool bPoisonClientConnection, bool bPoisonServerConnection, string sMessage)
        {
            if (bPoisonClientConnection)
            {
                oSession.PoisonClientPipe();
            }
            if (bPoisonServerConnection)
            {
                oSession.PoisonServerPipe();
            }
            oSession.SetBitFlag(flagViolation, true);
            if ((CONFIG.bReportHTTPErrors && !CONFIG.QuietMode) && !oSession.oFlags.ContainsKey("x-HTTPProtocol-Violation"))
            {
                oSession.oFlags.Remove("ui-hide");
                frmAlert alert = new frmAlert("HTTP Protocol Violation", "Fiddler has detected a protocol violation in session #" + oSession.id.ToString() + ".\n\n" + sMessage, "Note: You can disable this message using Tools | Fiddler Options");
                _frmMain.BeginInvoke(new alerterDelegate(_frmMain.ShowAlert), new object[] { alert });
            }
            Log.LogFormat("{0} - [#{1}] {2}", new object[] { "Fiddler.Network.ProtocolViolation", oSession.id.ToString(), sMessage });
            sMessage = "[ProtocolViolation] " + sMessage;
            if ((oSession["x-HTTPProtocol-Violation"] == null) || !oSession["x-HTTPProtocol-Violation"].Contains(sMessage))
            {
                Session session;
                (session = oSession)["x-HTTPProtocol-Violation"] = session["x-HTTPProtocol-Violation"] + sMessage;
            }
        }

        internal static void LogAddonException(Exception eX, string sTitle)
        {
            if (Prefs.GetBoolPref("fiddler.debug.extensions.showerrors", false) || Prefs.GetBoolPref("fiddler.debug.extensions.verbose", false))
            {
                ReportException(eX, sTitle);
            }
        }

        internal static void logSelfHost(int iUsedFeature)
        {
            string sValue = string.Format("{0},{1}", iUsedFeature, Prefs.GetStringPref("microsoft.selfhost.featurelist", ","));
            Prefs.SetStringPref("microsoft.selfhost.featurelist", sValue);
        }

        internal static void logSelfHostOnePerSession(int iUsedFeatureFlag)
        {
            if (!Prefs.GetStringPref("microsoft.selfhost.featurelist", string.Empty).Contains("," + iUsedFeatureFlag.ToString() + ","))
            {
                logSelfHost(iUsedFeatureFlag);
            }
        }

        internal static void OnCalculateReport(Session[] _arrSessions)
        {
            if ((!_bSuppressReportUpdates && !isClosing) && (_CalculateReport != null))
            {
                _CalculateReport(_arrSessions);
            }
        }

        internal static void OnFiddlerAttach()
        {
            _frmMain.miCaptureEnabled.Checked = true;
            _frmMain.miNotifyCapturing.Checked = true;
            if (scriptRules != null)
            {
                scriptRules.DoOnAttach();
            }
            if (_FiddlerAttach != null)
            {
                _FiddlerAttach();
            }
        }

        internal static void OnFiddlerBoot()
        {
            if (scriptRules != null)
            {
                scriptRules.DoOnBoot();
            }
            if (_FiddlerBoot != null)
            {
                _FiddlerBoot();
            }
            _Log.FlushStartupMessages();
        }

        internal static void OnFiddlerDetach()
        {
            _frmMain.miCaptureEnabled.Checked = false;
            _frmMain.miNotifyCapturing.Checked = false;
            if (scriptRules != null)
            {
                scriptRules.DoOnDetach();
            }
            if (_FiddlerDetach != null)
            {
                _FiddlerDetach();
            }
        }

        internal static void OnFiddlerShutdown()
        {
            if (scriptRules != null)
            {
                scriptRules.DoOnShutdown();
            }
            if (_FiddlerShutdown != null)
            {
                _FiddlerShutdown();
            }
        }

        internal static void ReportException(Exception eX)
        {
            ReportException(eX, "Sorry, you may have found a bug...");
        }

        public static void ReportException(Exception eX, string sTitle)
        {
            if (eX is ConfigurationErrorsException)
            {
                DoNotifyUser(string.Concat(new object[] { 
                    "Your Microsoft .NET Configuration file is corrupt and contains invalid data. You can often correct this error by installing updates from WindowsUpdate and/or reinstalling the .NET Framework.\r\n", eX.Message, "\n\nType: ", eX.GetType().ToString(), "\nSource: ", eX.Source, "\n", eX.StackTrace, "\n\n", eX.InnerException, "\nFiddler v", Application.ProductVersion, (8 == IntPtr.Size) ? " (x64) " : " (x86) ", " [.NET ", Environment.Version, " on ", 
                    Environment.OSVersion.VersionString, "] "
                 }), sTitle, MessageBoxIcon.Hand);
            }
            else
            {
                string str;
                if (eX is OutOfMemoryException)
                {
                    sTitle = "Out of Memory Error";
                    str = "An out-of-memory exception was encountered. To help avoid out-of-memory conditions, please see: " + CONFIG.GetUrl("REDIR") + "FIDDLEROOM\n\n";
                }
                else
                {
                    str = "Fiddler has encountered an unexpected problem. If you believe this is a bug in Fiddler, please copy this message by hitting CTRL+C, and submit a bug report using the Help | Send Feedback menu.\n\n";
                }
                DoNotifyUser(string.Concat(new object[] { 
                    str, eX.Message, "\n\nType: ", eX.GetType().ToString(), "\nSource: ", eX.Source, "\n", eX.StackTrace, "\n\n", eX.InnerException, "\nFiddler v", Application.ProductVersion, (8 == IntPtr.Size) ? " (x64) " : " (x86) ", " [.NET ", Environment.Version, " on ", 
                    Environment.OSVersion.VersionString, "] "
                 }), sTitle, MessageBoxIcon.Hand);
            }
            if (CONFIG.IsMicrosoftMachine)
            {
                logSelfHost(100);
            }
            Trace.Write(string.Concat(new object[] { eX.Message, "\n", eX.StackTrace, "\n", eX.InnerException }));
        }

        [CodeDescription("Reset the SessionID counter to 0. This method can lead to confusing UI, so call sparingly.")]
        public static void ResetSessionCounter()
        {
            Session.ResetSessionCounter();
        }

        internal static void UIInvoke(MethodInvoker target)
        {
            if (UI.InvokeRequired)
            {
                UI.Invoke(target);
            }
            else
            {
                target();
            }
        }

        [CodeDescription("Fiddler's logging subsystem; displayed on the LOG tab by default.")]
        public static Logger Log
        {
            get
            {
                return _Log;
            }
        }

        [CodeDescription("Fiddler's AutoResponder object.")]
        public static AutoResponder oAutoResponder
        {
            get
            {
                return _AutoResponder;
            }
        }

        [CodeDescription("Fiddler's Preferences collection. http://fiddler.wikidot.com/prefs")]
        public static IFiddlerPreferences Prefs
        {
            get
            {
                return _Prefs;
            }
        }

        internal static bool SuppressReportUpdates
        {
            get
            {
                return _bSuppressReportUpdates;
            }
            set
            {
                _bSuppressReportUpdates = value;
                if (!_bSuppressReportUpdates)
                {
                    _frmMain.actReportStatistics(true);
                }
            }
        }

        [CodeDescription("Fiddler's main form.")]
        public static frmViewer UI
        {
            get
            {
                return _frmMain;
            }
        }
    }
}

