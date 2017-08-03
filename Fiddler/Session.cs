namespace Fiddler
{
    using System;
    using System.Collections.Specialized;
    using System.Diagnostics;
    using System.Globalization;
    using System.IO;
    using System.Net;
    using System.Net.Sockets;
    using System.Reflection;
    using System.Text;
    using System.Threading;
    using System.Windows.Forms;
    using System.Xml;

    [DebuggerDisplay("Session #{m_requestID}, {m_state}, {fullUrl}, [{BitFlags}]")]
    public class Session
    {
        private WebRequest __WebRequestForAuth;
        private bool _bAllowClientPipeReuse;
        private SessionFlags _bitFlags;
        private bool _bypassGateway;
        private int _LocalProcessID;
        public bool bBufferResponse;
        private static int cRequests;
        public bool isTunnel;
        [CodeDescription("IP Address of the client for this session.")]
        public string m_clientIP;
        [CodeDescription("Client port attached to Fiddler.")]
        public int m_clientPort;
        [CodeDescription("IP Address of the server for this session.")]
        public string m_hostIP;
        private int m_requestID;
        private SessionStates m_state;
        private Session nextSession;
        [CodeDescription("Fiddler-internal flags set on the session.")]
        public readonly StringDictionary oFlags;
        private EventHandler<StateChangeEventArgs> _OnStateChanged;
        [CodeDescription("Object representing the HTTP Request.")]
        public ClientChatter oRequest;
        [CodeDescription("Object representing the HTTP Response.")]
        public ServerChatter oResponse;
        private AutoResetEvent oSyncEvent;
        [CodeDescription("Contains the bytes of the request body.")]
        public byte[] requestBodyBytes;
        [CodeDescription("Contains the bytes of the response body.")]
        public byte[] responseBodyBytes;
        public SessionTimers Timers;
        [CodeDescription("ListViewItem object associated with this session in the Session list.")]
        public ListViewItem ViewItem;

        public event EventHandler<StateChangeEventArgs> OnStateChanged
        {
            add
            {
                EventHandler<StateChangeEventArgs> handler2;
                EventHandler<StateChangeEventArgs> onStateChanged = this._OnStateChanged;
                do
                {
                    handler2 = onStateChanged;
                    EventHandler<StateChangeEventArgs> handler3 = (EventHandler<StateChangeEventArgs>) Delegate.Combine(handler2, value);
                    onStateChanged = Interlocked.CompareExchange<EventHandler<StateChangeEventArgs>>(ref this._OnStateChanged, handler3, handler2);
                }
                while (onStateChanged != handler2);
            }
            remove
            {
                EventHandler<StateChangeEventArgs> handler2;
                EventHandler<StateChangeEventArgs> onStateChanged = this._OnStateChanged;
                do
                {
                    handler2 = onStateChanged;
                    EventHandler<StateChangeEventArgs> handler3 = (EventHandler<StateChangeEventArgs>) Delegate.Remove(handler2, value);
                    onStateChanged = Interlocked.CompareExchange<EventHandler<StateChangeEventArgs>>(ref this._OnStateChanged, handler3, handler2);
                }
                while (onStateChanged != handler2);
            }
        }

        internal Session(ClientPipe clientPipe, ServerPipe serverPipe)
        {
            EventHandler<StateChangeEventArgs> handler = null;
            this.bBufferResponse = FiddlerApplication.Prefs.GetBoolPref("fiddler.ui.rules.bufferresponses", true);
            this.Timers = new SessionTimers();
            this._bAllowClientPipeReuse = true;
            this.oFlags = new StringDictionary();
            if (CONFIG.bDebugSpew)
            {
                if (handler == null)
                {
                    handler = delegate (object s, StateChangeEventArgs ea) {
                        FiddlerApplication.DebugSpew(string.Format("onstatechange>#{0} moving from state '{1}' to '{2}' {3}", new object[] { this.id.ToString(), ea.oldState, ea.newState, Environment.StackTrace }));
                    };
                }
                this._OnStateChanged += handler;
            }
            this.Timers.ClientConnected = DateTime.Now;
            if (clientPipe != null)
            {
                this.m_clientIP = (clientPipe.Address == null) ? null : clientPipe.Address.ToString();
                this.m_clientPort = clientPipe.Port;
                this.oFlags["x-clientIP"] = this.m_clientIP;
                this.oFlags["x-clientport"] = this.m_clientPort.ToString();
                if (clientPipe.LocalProcessID != 0)
                {
                    this._LocalProcessID = clientPipe.LocalProcessID;
                    this.oFlags["x-ProcessInfo"] = string.Format("{0}:{1}", clientPipe.LocalProcessName, this._LocalProcessID);
                }
            }
            this.oResponse = new ServerChatter(this);
            this.oRequest = new ClientChatter(this);
            this.oRequest.pipeClient = clientPipe;
            this.oResponse.pipeServer = serverPipe;
        }

        public Session(byte[] arrRequest, byte[] arrResponse) : this(arrRequest, arrResponse, SessionFlags.None)
        {
        }

        public Session(HTTPRequestHeaders oRequestHeaders, byte[] arrRequestBody)
        {
            EventHandler<StateChangeEventArgs> handler = null;
            this.bBufferResponse = FiddlerApplication.Prefs.GetBoolPref("fiddler.ui.rules.bufferresponses", true);
            this.Timers = new SessionTimers();
            this._bAllowClientPipeReuse = true;
            this.oFlags = new StringDictionary();
            if (oRequestHeaders == null)
            {
                throw new ArgumentNullException("oRequestHeaders", "oRequestHeaders must not be null when creating a new Session.");
            }
            if (CONFIG.bDebugSpew)
            {
                if (handler == null)
                {
                    handler = delegate (object s, StateChangeEventArgs ea) {
                        FiddlerApplication.DebugSpew(string.Format("onstatechange>#{0} moving from state '{1}' to '{2}' {3}", new object[] { this.id.ToString(), ea.oldState, ea.newState, Environment.StackTrace }));
                    };
                }
                this._OnStateChanged += handler;
            }
            this.Timers.ClientConnected = this.Timers.ClientBeginRequest = DateTime.Now;
            this.m_clientIP = null;
            this.m_clientPort = 0;
            this.oFlags["x-clientIP"] = this.m_clientIP;
            this.oFlags["x-clientport"] = this.m_clientPort.ToString();
            this.oResponse = new ServerChatter(this);
            this.oRequest = new ClientChatter(this);
            this.oRequest.pipeClient = null;
            this.oResponse.pipeServer = null;
            this.oRequest.headers = oRequestHeaders;
            this.requestBodyBytes = arrRequestBody;
            this.m_state = SessionStates.AutoTamperRequestBefore;
        }

        public Session(byte[] arrRequest, byte[] arrResponse, SessionFlags oSF)
        {
            HTTPHeaderParseWarnings warnings;
            int num;
            int num2;
            int num3;
            int num4;
            this.bBufferResponse = FiddlerApplication.Prefs.GetBoolPref("fiddler.ui.rules.bufferresponses", true);
            this.Timers = new SessionTimers();
            this._bAllowClientPipeReuse = true;
            this.oFlags = new StringDictionary();
            if ((arrRequest == null) || (arrRequest.Length < 1))
            {
                throw new ArgumentException("Missing request data for session");
            }
            if ((arrResponse == null) || (arrResponse.Length < 1))
            {
                arrResponse = Encoding.ASCII.GetBytes("HTTP/1.1 0 FIDDLER GENERATED - RESPONSE DATA WAS MISSING\r\n\r\n");
            }
            this.state = SessionStates.Done;
            this.m_requestID = Interlocked.Increment(ref cRequests);
            this.BitFlags = oSF;
            if (!Parser.FindEntityBodyOffsetFromArray(arrRequest, out num, out num2, out warnings))
            {
                throw new InvalidDataException("Request corrupt, unable to find end of headers.");
            }
            if (!Parser.FindEntityBodyOffsetFromArray(arrResponse, out num3, out num4, out warnings))
            {
                throw new InvalidDataException("Response corrupt, unable to find end of headers.");
            }
            this.requestBodyBytes = new byte[arrRequest.Length - num2];
            this.responseBodyBytes = new byte[arrResponse.Length - num4];
            System.Buffer.BlockCopy(arrRequest, num2, this.requestBodyBytes, 0, this.requestBodyBytes.Length);
            System.Buffer.BlockCopy(arrResponse, num4, this.responseBodyBytes, 0, this.responseBodyBytes.Length);
            string sData = CONFIG.oHeaderEncoding.GetString(arrRequest, 0, num) + "\r\n\r\n";
            string sHeaders = CONFIG.oHeaderEncoding.GetString(arrResponse, 0, num3) + "\r\n\r\n";
            this.oRequest = new ClientChatter(this, sData);
            this.oResponse = new ServerChatter(this, sHeaders);
        }

        internal void _AssignID()
        {
            this.m_requestID = Interlocked.Increment(ref cRequests);
        }

        private void _createNextSession(bool bForceClientServerPipeAffinity)
        {
            if (((this.oResponse != null) && (this.oResponse.pipeServer != null)) && ((bForceClientServerPipeAffinity || (this.oResponse.pipeServer.ReusePolicy == PipeReusePolicy.MarriedToClientPipe)) || this.oFlags.ContainsKey("X-ClientServerPipeAffinity")))
            {
                this.nextSession = new Session(this.oRequest.pipeClient, this.oResponse.pipeServer);
                this.oResponse.pipeServer = null;
            }
            else
            {
                this.nextSession = new Session(this.oRequest.pipeClient, null);
            }
        }

        private bool _executeObtainRequest()
        {
            if (this.state > SessionStates.ReadingRequest)
            {
                this.Timers.ClientDoneRequest = DateTime.Now;
                this._AssignID();
            }
            else
            {
                this.state = SessionStates.ReadingRequest;
                if (!this.oRequest.ReadRequest())
                {
                    if (this.oResponse != null)
                    {
                        this.oResponse._detachServerPipe();
                    }
                    this.CloseSessionPipes(true);
                    this.state = SessionStates.Aborted;
                    return false;
                }
                this.Timers.ClientDoneRequest = DateTime.Now;
                if (CONFIG.bDebugSpew)
                {
                    FiddlerApplication.DebugSpew(string.Format("Session ID #{0} for request read from {1}.", this.m_requestID, this.oRequest.pipeClient));
                }
                this.requestBodyBytes = this.oRequest.TakeEntity();
            }
            this._replaceVirtualHostnames();
            if (((this.requestBodyBytes == null) || (this.requestBodyBytes.LongLength < 1L)) && (("GET" != this.oRequest.headers.HTTPMethod) && Utilities.HTTPMethodRequiresBody(this.oRequest.headers.HTTPMethod)))
            {
                FiddlerApplication.HandleHTTPError(this, SessionFlags.ProtocolViolationInRequest, true, false, "This HTTP method requires a request body.");
            }
            if (this.oFlags.ContainsKey("X-Original-Host"))
            {
                string str = this.oFlags["X-Original-Host"];
                if (string.Empty == str)
                {
                    FiddlerApplication.HandleHTTPError(this, SessionFlags.ProtocolViolationInRequest, false, false, "HTTP/1.1 Request was missing the required HOST header.");
                }
                else
                {
                    FiddlerApplication.HandleHTTPError(this, SessionFlags.ProtocolViolationInRequest, false, false, string.Format("Request's Host header does not match Host specified in URI.\n\nURL Host:\t{0}\nHeader Host:\t{1}", str, this.oRequest.headers["Host"]));
                }
            }
            this.state = SessionStates.AutoTamperRequestBefore;
            this.ExecuteBasicRequestManipulations();
            FiddlerApplication.DoBeforeRequest(this);
            FiddlerApplication.oExtensions.DoAutoTamperRequestBefore(this);
            if (FiddlerApplication._AutoResponder.IsEnabled)
            {
                FiddlerApplication._AutoResponder.DoMatchBeforeRequestTampering(this);
            }
            if (!this.ShouldBeHidden())
            {
                FiddlerApplication._frmMain.BeginInvoke(new updateUIDelegate(FiddlerApplication._frmMain.addSession), new object[] { this });
            }
            if (this.m_state >= SessionStates.Done)
            {
                this.FinishUISession();
                return false;
            }
            this._handTamperRequest();
            if (this.m_state < SessionStates.AutoTamperRequestAfter)
            {
                this.state = SessionStates.AutoTamperRequestAfter;
            }
            FiddlerApplication.oExtensions.DoAutoTamperRequestAfter(this);
            if (this.m_state >= SessionStates.Done)
            {
                return false;
            }
            if (FiddlerApplication._AutoResponder.IsEnabled)
            {
                FiddlerApplication._AutoResponder.DoMatchAfterRequestTampering(this);
            }
            return true;
        }

        private bool _handleHTTPSConnect()
        {
            string str;
            if ((!CONFIG.bMITM_HTTPS || this.oFlags.ContainsKey("x-no-decrypt")) || Utilities.IsHostInList(CONFIG.slDecryptBypassList, this.PathAndQuery))
            {
                HTTPSTunnel.CreateTunnel(this);
                this.state = SessionStates.Done;
                FiddlerApplication.DoAfterSessionComplete(this);
                return true;
            }
            this.SetBitFlag(SessionFlags.IsDecryptingTunnel, true);
            int iPort = 80;
            Utilities.CrackHostAndPort(this.oFlags["x-overrideHost"] ?? this.PathAndQuery, out str, ref iPort);
            try
            {
                if (this.oFlags.ContainsKey("x-replywithtunnel"))
                {
                    this._ReturnSelfGeneratedCONNECTTunnel(str);
                    return true;
                }
                IPEndPoint point = null;
                if (this.oFlags["x-overrideGateway"] != null)
                {
                    if (!string.Equals("DIRECT", this.oFlags["x-overrideGateway"], StringComparison.OrdinalIgnoreCase))
                    {
                        return false;
                    }
                    this.bypassGateway = true;
                }
                else if (!this.bypassGateway)
                {
                    int tickCount = Environment.TickCount;
                    point = FiddlerApplication.oProxy.FindGatewayForOrigin("https", this.PathAndQuery);
                    this.Timers.GatewayDeterminationTime = Environment.TickCount - tickCount;
                }
                if (point != null)
                {
                    return false;
                }
                ServerPipe serverPipe = null;
                serverPipe = Proxy.htServerPipePool.DequeuePipe("HTTPS:" + this.PathAndQuery, this.LocalProcessID, this.id);
                if (serverPipe != null)
                {
                    StringDictionary dictionary;
                    this.Timers.TCPConnectTime = 0;
                    (dictionary = this.oFlags)["x-securepipe"] = dictionary["x-securepipe"] + "REUSE " + serverPipe._sPipeName;
                }
                else
                {
                    IPAddress[] addressArray;
                    try
                    {
                        addressArray = DNSResolver.GetIPAddressList(str, true, this.Timers);
                    }
                    catch
                    {
                        this.oRequest.FailSession(0x1f6, "Fiddler - DNS Lookup Failed", "Fiddler: DNS Lookup for " + Utilities.HtmlEncode(str) + " failed.");
                        return true;
                    }
                    serverPipe = new ServerPipe("Tunnel#" + this.id.ToString(), false);
                    bool flag = false;
                    string str2 = string.Empty;
                    try
                    {
                        Socket oSocket = ServerChatter.CreateConnectedSocket(addressArray, iPort, this);
                        flag = serverPipe.WrapSocketInPipe(this, oSocket, true, false, str, this.oFlags["https-Client-Certificate"], "HTTPS:" + this.PathAndQuery, ref this.Timers.HTTPSHandshakeTime);
                    }
                    catch (Exception exception)
                    {
                        str2 = exception.ToString();
                        flag = false;
                    }
                    if (flag)
                    {
                        this.Timers.ServerConnected = serverPipe.dtConnected;
                    }
                    else
                    {
                        this.oResponse.headers = new HTTPResponseHeaders();
                        this.oResponse.headers.HTTPResponseCode = 0x1f6;
                        this.oResponse.headers.HTTPResponseStatus = "502 Connection failed";
                        this.oResponse.headers.Add("Connection", "close");
                        this.oResponse.headers.Add("Timestamp", DateTime.Now.ToString("HH:mm:ss.fff"));
                        this.responseBodyBytes = Encoding.UTF8.GetBytes("HTTPS connection failed.\n\n" + str2);
                        FiddlerApplication.DoBeforeReturningError(this);
                        if (this.oRequest.pipeClient != null)
                        {
                            try
                            {
                                this.oRequest.pipeClient.Send(this.oResponse.headers.ToByteArray(true, true));
                                this.oRequest.pipeClient.Send(Encoding.ASCII.GetBytes("HTTPS Connection Failed.".PadRight(0x201, ' ')));
                            }
                            catch
                            {
                            }
                        }
                        if ((this.ViewItem != null) || !this.ShouldBeHidden())
                        {
                            FiddlerApplication._frmMain.BeginInvoke(new updateUIDelegate(FiddlerApplication._frmMain.finishSession), new object[] { this });
                        }
                        this.state = SessionStates.Done;
                        FiddlerApplication.DoAfterSessionComplete(this);
                        this.CloseSessionPipes(true);
                        return true;
                    }
                }
                if ((serverPipe != null) && (serverPipe.Address != null))
                {
                    this.oFlags["x-EgressPort"] = serverPipe.LocalPort.ToString();
                    this.m_hostIP = serverPipe.Address.ToString();
                    this.oFlags["x-hostIP"] = this.m_hostIP;
                }
                this.oResponse._bWasForwarded = serverPipe.isConnectedToGateway;
                this.SetBitFlag(SessionFlags.SentToGateway, this.oResponse._bWasForwarded);
                this.oResponse.headers = new HTTPResponseHeaders();
                this.oResponse.headers.HTTPResponseCode = 200;
                this.oResponse.headers.HTTPResponseStatus = "200 DecryptTunnel Established";
                this.oResponse.headers.Add("Timestamp", DateTime.Now.ToString("HH:mm:ss.fff"));
                this.oResponse.headers.Add("FiddlerGateway", "Direct");
                this.responseBodyBytes = Encoding.UTF8.GetBytes("This is a HTTPS CONNECT Tunnel. Secure traffic flows through this connection.\n\n" + serverPipe.DescribeConnectionSecurity());
                FiddlerApplication.DoBeforeResponse(this);
                FiddlerApplication.oExtensions.DoAutoTamperResponseBefore(this);
                FiddlerApplication.oExtensions.DoAutoTamperResponseAfter(this);
                this.state = SessionStates.Done;
                FiddlerApplication.DoAfterSessionComplete(this);
                if ((this.oRequest.pipeClient == null) || !this.oRequest.pipeClient.SecureClientPipe(this.oFlags["x-OverrideCertCN"] ?? str, this.oResponse.headers))
                {
                    FiddlerApplication._frmMain.BeginInvoke(new updateUIDelegate(FiddlerApplication._frmMain.finishSession), new object[] { this });
                    this.CloseSessionPipes(true);
                    return true;
                }
                FiddlerApplication._frmMain.BeginInvoke(new updateUIDelegate(FiddlerApplication._frmMain.finishSession), new object[] { this });
                Session session = new Session(this.oRequest.pipeClient, serverPipe);
                this.oRequest.pipeClient = null;
                session.oFlags["x-serversocket"] = this.oFlags["x-securepipe"];
                if ((serverPipe != null) && (serverPipe.Address != null))
                {
                    session.m_hostIP = serverPipe.Address.ToString();
                    session.oFlags["x-hostIP"] = session.m_hostIP;
                    session.oFlags["x-EgressPort"] = serverPipe.LocalPort.ToString();
                }
                session.Execute(null);
            }
            catch (Exception exception2)
            {
                FiddlerApplication.ReportException(exception2, "HTTPS Interception failure");
                this.oRequest.pipeClient = null;
            }
            return true;
        }

        private void _handTamperRequest()
        {
            if (this.oFlags.ContainsKey("x-breakrequest") && !this.oFlags.ContainsKey("ui-hide"))
            {
                this.state = SessionStates.HandTamperRequest;
                FiddlerApplication._frmMain.BeginInvoke(new updateUIDelegate(FiddlerApplication._frmMain.updateSession), new object[] { this });
                Utilities.DoFlash(FiddlerApplication._frmMain.Handle);
                FiddlerApplication._frmMain.notifyIcon.ShowBalloonTip(0x1388, "Fiddler Breakpoint", "Fiddler is paused at a request breakpoint", ToolTipIcon.Warning);
                this.ThreadPause();
            }
        }

        private bool _innerReplaceInResponse(string sSearchFor, string sReplaceWith, bool bReplaceAll, bool bCaseSensitive)
        {
            Encoding encoding;
            string str;
            string str2;
            if ((this.responseBodyBytes != null) && (this.responseBodyBytes.LongLength != 0L))
            {
                encoding = Utilities.getResponseBodyEncoding(this);
                str = encoding.GetString(this.responseBodyBytes);
                if (bReplaceAll)
                {
                    str2 = str.Replace(sSearchFor, sReplaceWith);
                    goto Label_0088;
                }
                int index = str.IndexOf(sSearchFor, bCaseSensitive ? StringComparison.InvariantCulture : StringComparison.InvariantCultureIgnoreCase);
                if (index == 0)
                {
                    str2 = sReplaceWith + str.Substring(sSearchFor.Length);
                    goto Label_0088;
                }
                if (index > 0)
                {
                    str2 = str.Substring(0, index) + sReplaceWith + str.Substring(index + sSearchFor.Length);
                    goto Label_0088;
                }
            }
            return false;
        Label_0088:
            if (str != str2)
            {
                this.responseBodyBytes = encoding.GetBytes(str2);
                this.oResponse["Content-Length"] = this.responseBodyBytes.LongLength.ToString();
                return true;
            }
            return false;
        }

        private bool _isDirectRequestToFiddler()
        {
            if (this.port != CONFIG.ListenPort)
            {
                return false;
            }
            string str = this.hostname.ToLower();
            if ((!(str == "127.0.0.1") && !(str == "localhost")) && (!(str == "localhost.") && !(str == CONFIG.sMachineNameLowerCase)))
            {
                return (str == "[::1]");
            }
            return true;
        }

        private bool _isNTLMType2()
        {
            if (!this.oFlags.ContainsKey("x-SuppressProxySupportHeader"))
            {
                this.oResponse.headers["Proxy-Support"] = "Session-Based-Authentication";
            }
            if (0x197 == this.oResponse.headers.HTTPResponseCode)
            {
                if (string.Empty == this.oRequest.headers["Proxy-Authorization"])
                {
                    return false;
                }
                if (!this.oResponse.headers.Exists("Proxy-Authenticate") || (this.oResponse.headers["Proxy-Authenticate"].Length < 6))
                {
                    return false;
                }
            }
            else
            {
                if (string.Empty == this.oRequest.headers["Authorization"])
                {
                    return false;
                }
                if (!this.oResponse.headers.Exists("WWW-Authenticate") || (this.oResponse.headers["WWW-Authenticate"].Length < 6))
                {
                    return false;
                }
            }
            return true;
        }

        private bool _isResponseMultiStageAuthChallenge()
        {
            return (((0x191 == this.oResponse.headers.HTTPResponseCode) && this.oResponse.headers["WWW-Authenticate"].StartsWith("N", StringComparison.OrdinalIgnoreCase)) || ((0x197 == this.oResponse.headers.HTTPResponseCode) && this.oResponse.headers["Proxy-Authenticate"].StartsWith("N", StringComparison.OrdinalIgnoreCase)));
        }

        private string _MakeSafeFilename(string sFilename)
        {
            char[] invalidFileNameChars = Path.GetInvalidFileNameChars();
            if (sFilename.IndexOfAny(invalidFileNameChars) < 0)
            {
                return Utilities.TrimAfter(sFilename, 160);
            }
            StringBuilder builder = new StringBuilder(sFilename);
            for (int i = 0; i < builder.Length; i++)
            {
                if (Array.IndexOf<char>(invalidFileNameChars, sFilename[i]) > -1)
                {
                    builder[i] = '-';
                }
            }
            return Utilities.TrimAfter(builder.ToString(), 160);
        }

        private bool _MayReuseMyClientPipe()
        {
            if (((!CONFIG.bReuseClientSockets || !this._bAllowClientPipeReuse) || (this.oResponse.headers.ExistsAndEquals("Connection", "close") || this.oRequest.headers.ExistsAndEquals("Connection", "close"))) || this.oRequest.headers.ExistsAndEquals("Proxy-Connection", "close"))
            {
                return false;
            }
            if (!(this.oResponse.headers.HTTPVersion == "HTTP/1.1"))
            {
                return this.oResponse.headers.ExistsAndContains("Connection", "Keep-Alive");
            }
            return true;
        }

        private void _replaceVirtualHostnames()
        {
            if (!this.hostname.EndsWith(".fiddler", StringComparison.OrdinalIgnoreCase))
            {
                return;
            }
            string str = this.hostname.ToLower();
            string str2 = str;
            if (str2 == null)
            {
                return;
            }
            if (!(str2 == "ipv4.fiddler"))
            {
                if (!(str2 == "localhost.fiddler"))
                {
                    if (!(str2 == "ipv6.fiddler"))
                    {
                        return;
                    }
                    this.hostname = "[::1]";
                    goto Label_0075;
                }
            }
            else
            {
                this.hostname = "127.0.0.1";
                goto Label_0075;
            }
            this.hostname = "localhost";
        Label_0075:
            this.oFlags["x-UsedVirtualHost"] = str;
            this.bypassGateway = true;
            if (this.HTTPMethodIs("CONNECT"))
            {
                this.oFlags["x-OverrideCertCN"] = str;
            }
        }

        private void _returnEchoServiceResponse()
        {
            StringBuilder builder = new StringBuilder("<HTML><HEAD><META http-equiv=\"Content-Type\" content=\"text/html; charset=UTF-8\"><TITLE>Fiddler Echo Service</TITLE></HEAD><BODY STYLE=\"font-family: arial,sans-serif;\"><PRE>" + Utilities.HtmlEncode(this.oRequest.headers.ToString(true, true)));
            if ((this.requestBodyBytes != null) && (this.requestBodyBytes.LongLength > 0L))
            {
                builder.Append(Utilities.HtmlEncode(Encoding.UTF8.GetString(this.requestBodyBytes)));
            }
            builder.Append("</PRE><BR><HR><UL><LI>If you'd like to configure Fiddler as a reverse proxy instead, see <A HREF='" + CONFIG.GetUrl("REDIR") + "REVERSEPROXY'>Reverse Proxy Setup</A><LI>You can download the <a href=\"FiddlerRoot.cer\">FiddlerRoot certificate</A></UL></BODY></HTML>");
            this.oRequest.FailSession(200, "Fiddler - Ok", builder.ToString());
            this.state = SessionStates.Aborted;
        }

        private void _returnPACFileResponse()
        {
            this.SetBitFlag(SessionFlags.ResponseGeneratedByFiddler, true);
            this.utilCreateResponseAndBypassServer();
            this.oResponse.headers["Content-Type"] = "application/x-ns-proxy-autoconfig";
            this.oResponse.headers["Connection"] = "close";
            this.utilSetResponseBody(FiddlerApplication.oProxy._GetPACScriptText(FiddlerApplication.oProxy.IsAttached));
            this.state = SessionStates.Aborted;
            this.ReturnResponse(false);
        }

        private void _returnRootCert()
        {
            this.SetBitFlag(SessionFlags.ResponseGeneratedByFiddler, true);
            this.utilCreateResponseAndBypassServer();
            this.oResponse.headers["Connection"] = "close";
            this.oResponse.headers["Cache-Control"] = "max-age=0";
            byte[] buffer = CertMaker.getRootCertBytes();
            if (buffer != null)
            {
                this.oResponse.headers["Content-Type"] = "application/x-x509-ca-cert";
                this.responseBodyBytes = buffer;
                this.oResponse.headers["Content-Length"] = this.responseBodyBytes.Length.ToString();
            }
            else
            {
                this.responseCode = 0x194;
                this.oResponse.headers["Content-Type"] = "text/html; charset=UTF-8";
                this.utilSetResponseBody("No root certificate was found. Have you enabled HTTPS traffic decryption in Fiddler yet?".PadRight(0x200, ' '));
            }
            this.state = SessionStates.Done;
            this.ReturnResponse(false);
        }

        private void _ReturnSelfGeneratedCONNECTTunnel(string sHostname)
        {
            this.SetBitFlag(SessionFlags.ResponseGeneratedByFiddler, true);
            this.oResponse.headers = new HTTPResponseHeaders();
            this.oResponse.headers.HTTPResponseCode = 200;
            this.oResponse.headers.HTTPResponseStatus = "200 DecryptEndpoint Created";
            this.oResponse.headers.Add("Timestamp", DateTime.Now.ToString("HH:mm:ss.fff"));
            this.oResponse.headers.Add("FiddlerGateway", "AutoResponder");
            this.responseBodyBytes = Encoding.UTF8.GetBytes("This is a Fiddler-generated response to the client's request for a CONNECT tunnel.\n\n");
            this.oFlags["ui-backcolor"] = "Lavender";
            FiddlerApplication.DoBeforeResponse(this);
            FiddlerApplication.oExtensions.DoAutoTamperResponseBefore(this);
            FiddlerApplication.oExtensions.DoAutoTamperResponseAfter(this);
            this.state = SessionStates.Done;
            FiddlerApplication.DoAfterSessionComplete(this);
            if ((this.oRequest.pipeClient == null) || !this.oRequest.pipeClient.SecureClientPipe(this.oFlags["x-OverrideCertCN"] ?? sHostname, this.oResponse.headers))
            {
                FiddlerApplication._frmMain.BeginInvoke(new updateUIDelegate(FiddlerApplication._frmMain.finishSession), new object[] { this });
                this.CloseSessionPipes(false);
            }
            else
            {
                FiddlerApplication._frmMain.BeginInvoke(new updateUIDelegate(FiddlerApplication._frmMain.finishSession), new object[] { this });
                Session session = new Session(this.oRequest.pipeClient, null);
                this.oRequest.pipeClient = null;
                session.oFlags["x-serversocket"] = "AUTO-RESPONDER-GENERATED";
                session.Execute(null);
            }
        }

        internal void Abort()
        {
            try
            {
                if (this.m_state < SessionStates.Done)
                {
                    this.PoisonClientPipe();
                    this.PoisonServerPipe();
                    this.CloseSessionPipes(true);
                    this.oFlags["x-Fiddler-Aborted"] = "true";
                    this.state = SessionStates.Aborted;
                    this.ThreadResume();
                }
            }
            catch (Exception)
            {
            }
        }

        private void CloseSessionPipes(bool bNullThemToo)
        {
            if (CONFIG.bDebugSpew)
            {
                FiddlerApplication.DebugSpew(string.Format("CloseSessionPipes() for Session #{0}", this.id));
            }
            if ((this.oRequest != null) && (this.oRequest.pipeClient != null))
            {
                this.oRequest.pipeClient.End();
                if (bNullThemToo)
                {
                    this.oRequest.pipeClient = null;
                }
            }
            if ((this.oResponse != null) && (this.oResponse.pipeServer != null))
            {
                this.oResponse.pipeServer.End();
                if (bNullThemToo)
                {
                    this.oResponse.pipeServer = null;
                }
            }
        }

        public bool COMETPeek()
        {
            if (this.state != SessionStates.ReadingResponse)
            {
                return false;
            }
            this.responseBodyBytes = this.oResponse._PeekAtBody();
            return true;
        }

        internal static void CreateAndExecute(object oParams)
        {
            ClientPipe clientPipe = new ClientPipe(((ProxyExecuteParams) oParams).oSocket);
            Session session = new Session(clientPipe, null);
            if ((((ProxyExecuteParams) oParams).oServerCert != null) && !clientPipe.SecureClientPipeDirect(((ProxyExecuteParams) oParams).oServerCert))
            {
                FiddlerApplication.Log.LogString("Failed to secure client connection when acting as Secure Endpoint.");
            }
            else
            {
                session.Execute(null);
            }
        }

        internal void Execute(object objThreadState)
        {
            try
            {
                this.InnerExecute();
            }
            catch (Exception exception)
            {
                FiddlerApplication.ReportException(exception, "Uncaught Exception in Session #" + this.id.ToString());
            }
        }

        private void ExecuteBasicRequestManipulations()
        {
            if (!FiddlerApplication.isClosing && (FiddlerApplication._frmMain != null))
            {
                if (((this._LocalProcessID > 0) && (FiddlerApplication._iShowOnlyPID != 0)) && (FiddlerApplication._iShowOnlyPID != this._LocalProcessID))
                {
                    this.oFlags["ui-hide"] = "TB-PROCESSFILTER";
                }
                if (CONFIG.iShowProcessFilter != ProcessFilterCategories.All)
                {
                    if (CONFIG.iShowProcessFilter != ProcessFilterCategories.HideAll)
                    {
                        string str = this.oFlags["x-ProcessInfo"];
                        if (!string.IsNullOrEmpty(str))
                        {
                            bool flag = ((str.StartsWith("ie", StringComparison.OrdinalIgnoreCase) || str.StartsWith("firefox", StringComparison.OrdinalIgnoreCase)) || (str.StartsWith("chrome", StringComparison.OrdinalIgnoreCase) || str.StartsWith("opera", StringComparison.OrdinalIgnoreCase))) || str.StartsWith("safari", StringComparison.OrdinalIgnoreCase);
                            if (((CONFIG.iShowProcessFilter == ProcessFilterCategories.Browsers) && !flag) || ((CONFIG.iShowProcessFilter == ProcessFilterCategories.NonBrowsers) && flag))
                            {
                                this.oFlags["ui-hide"] = "PROCESSFILTER";
                            }
                        }
                    }
                    else
                    {
                        this.oFlags["ui-hide"] = "HIDEALL";
                    }
                }
                if (FiddlerApplication.Prefs.GetBoolPref("fiddler.ui.rules.hideimages", false))
                {
                    string str2 = this.url.ToLower();
                    if ((str2.EndsWith(".gif") || str2.EndsWith(".jpg")) || ((str2.EndsWith(".jpeg") || str2.EndsWith(".png")) || str2.EndsWith(".ico")))
                    {
                        this.oFlags["ui-hide"] = "IMAGESKIP";
                    }
                }
                if (this.HTTPMethodIs("CONNECT") && FiddlerApplication.Prefs.GetBoolPref("fiddler.ui.rules.hideconnects", false))
                {
                    this.oFlags["ui-hide"] = "CONNECTSKIP";
                }
                if (FiddlerApplication.Prefs.GetBoolPref("fiddler.ui.rules.removeencoding", false))
                {
                    this.utilDecodeRequest();
                }
                if (FiddlerApplication.Prefs.GetBoolPref("fiddler.ui.ephemeral.rules.breakonrequest", false))
                {
                    string str3 = this.url.ToLower();
                    if (CONFIG.bBreakOnImages || (((!str3.EndsWith(".gif") && !str3.EndsWith(".jpg")) && (!str3.EndsWith(".jpeg") && !str3.EndsWith(".png"))) && !str3.EndsWith(".ico")))
                    {
                        this.oFlags["x-breakrequest"] = "SINGLESTEP";
                    }
                }
                if ((!this.oRequest.headers.ExistsAndContains("Proxy-Authorization", "Basic MTox") && FiddlerApplication.Prefs.GetBoolPref("fiddler.ui.ephemeral.rules.requireproxyauth", false)) && ((this.oRequest.pipeClient != null) && !this.oRequest.pipeClient.bIsSecured))
                {
                    this.responseBodyBytes = Encoding.ASCII.GetBytes("<html><body>[Fiddler] Proxy Authentication Required.<BR>".PadRight(0x200, ' ') + "</body></html>");
                    this.oResponse.headers = new HTTPResponseHeaders(CONFIG.oHeaderEncoding);
                    this.oResponse.headers.HTTPResponseCode = 0x197;
                    this.oResponse.headers.HTTPResponseStatus = "407 Proxy Auth Required";
                    this.oResponse.headers.Add("Connection", "close");
                    this.oResponse.headers.Add("Proxy-Authenticate", "Basic realm=\"FiddlerProxy (username: 1, password: 1)\"");
                    this.oResponse.headers.Add("Content-Type", "text/html");
                    FiddlerApplication._frmMain.BeginInvoke(new updateUIDelegate(FiddlerApplication._frmMain.addSession), new object[] { this });
                    this.state = SessionStates.SendingResponse;
                    if (this.ReturnResponse(false))
                    {
                        this.state = SessionStates.Done;
                    }
                }
            }
        }

        private void ExecuteBasicResponseManipulations()
        {
            if (!FiddlerApplication.isClosing)
            {
                if (FiddlerApplication.Prefs.GetBoolPref("fiddler.ui.rules.removeencoding", false))
                {
                    this.utilDecodeResponse();
                }
                if ((FiddlerApplication.Prefs.GetBoolPref("fiddler.ui.ephemeral.rules.forcegzip", false) && this.oRequest.headers.ExistsAndContains("Accept-Encoding", "gzip")) && !this.oResponse.headers.ExistsAndContains("Content-Type", "image/"))
                {
                    this.utilGZIPResponse();
                }
            }
        }

        internal void ExecuteBasicResponseManipulationsUsingHeadersOnly()
        {
            if (!FiddlerApplication.isClosing)
            {
                if (FiddlerApplication.Prefs.GetBoolPref("fiddler.ui.rules.hideimages", false) && this.oResponse.headers.ExistsAndContains("Content-Type", "image/"))
                {
                    this.oFlags["ui-hide"] = "IMAGESKIP";
                }
                if (FiddlerApplication.Prefs.GetBoolPref("fiddler.ui.ephemeral.rules.breakonresponse", false) && (this.responseCode != 0x191))
                {
                    string str = this.url.ToLower();
                    if (CONFIG.bBreakOnImages || (((!this.oResponse.headers.ExistsAndContains("Content-Type", "image/") && !str.EndsWith(".gif")) && (!str.EndsWith(".jpg") && !str.EndsWith(".jpeg"))) && (!str.EndsWith(".png") && !str.EndsWith(".ico"))))
                    {
                        this.oFlags["x-breakresponse"] = "SINGLESTEP";
                        this.bBufferResponse = true;
                    }
                }
            }
        }

        internal void FinishUISession()
        {
            this.FinishUISession(false);
        }

        internal void FinishUISession(bool bSynchronous)
        {
            if ((!FiddlerApplication.isClosing && (FiddlerApplication._frmMain != null)) && ((this.ViewItem != null) || !this.ShouldBeHidden()))
            {
                if (bSynchronous)
                {
                    if (FiddlerApplication._frmMain.InvokeRequired)
                    {
                        FiddlerApplication._frmMain.Invoke(new updateUIDelegate(FiddlerApplication._frmMain.finishSession), new object[] { this });
                    }
                    else
                    {
                        FiddlerApplication._frmMain.finishSession(this);
                    }
                }
                else
                {
                    FiddlerApplication._frmMain.BeginInvoke(new updateUIDelegate(FiddlerApplication._frmMain.finishSession), new object[] { this });
                }
            }
        }

        [CodeDescription("Return a string generated from the request body, decoding it and converting from a codepage if needed. Possibly Expensive.")]
        public string GetRequestBodyAsString()
        {
            if (((this.requestBodyBytes == null) || (this.requestBodyBytes.Length == 0)) || ((this.oRequest == null) || (this.oRequest.headers == null)))
            {
                return string.Empty;
            }
            byte[] arrBody = (byte[]) this.requestBodyBytes.Clone();
            Utilities.utilDecodeHTTPBody(this.oRequest.headers, ref arrBody);
            return Utilities.getEntityBodyEncoding(this.oRequest.headers, arrBody).GetString(arrBody);
        }

        [CodeDescription("Returns the Encoding of the requestBodyBytes")]
        public Encoding GetRequestBodyEncoding()
        {
            return Utilities.getEntityBodyEncoding(this.oRequest.headers, this.requestBodyBytes);
        }

        [CodeDescription("Return a string generated from the response body, decoding it and converting from a codepage if needed. Possibly Expensive.")]
        public string GetResponseBodyAsString()
        {
            if ((this.responseBodyBytes == null) || (this.responseBodyBytes.Length == 0))
            {
                return string.Empty;
            }
            byte[] arrBody = (byte[]) this.responseBodyBytes.Clone();
            Utilities.utilDecodeHTTPBody(this.oResponse.headers, ref arrBody);
            return Utilities.getEntityBodyEncoding(this.oResponse.headers, arrBody).GetString(arrBody);
        }

        [CodeDescription("Returns the Encoding of the responseBodyBytes")]
        public Encoding GetResponseBodyEncoding()
        {
            return Utilities.getResponseBodyEncoding(this);
        }

        [CodeDescription("Returns TRUE if the Session's target hostname (no port) matches sTestHost (case-insensitively).")]
        public bool HostnameIs(string sTestHost)
        {
            if (this.oRequest == null)
            {
                return false;
            }
            int length = this.oRequest.host.LastIndexOf(':');
            if ((length > -1) && (length > this.oRequest.host.LastIndexOf(']')))
            {
                return (0 == string.Compare(this.oRequest.host, 0, sTestHost, 0, length, StringComparison.OrdinalIgnoreCase));
            }
            return string.Equals(this.oRequest.host, sTestHost, StringComparison.OrdinalIgnoreCase);
        }

        [CodeDescription("Returns TRUE if the Session's HTTP Method is available and matches the target method.")]
        public bool HTTPMethodIs(string sTestFor)
        {
            return (((this.oRequest != null) && (this.oRequest.headers != null)) && string.Equals(this.oRequest.headers.HTTPMethod, sTestFor, StringComparison.OrdinalIgnoreCase));
        }

        private void InnerExecute()
        {
            if ((this.oRequest == null) || (this.oResponse == null))
            {
                return;
            }
            if (!this._executeObtainRequest())
            {
                return;
            }
            if (this.m_state >= SessionStates.ReadingResponse)
            {
                goto Label_0346;
            }
            if (this.oFlags.ContainsKey("x-replywithfile"))
            {
                this.oResponse = new ServerChatter(this, "HTTP/1.1 200 OK\r\nServer: Fiddler\r\n\r\n");
                this.LoadResponseFromFile(this.oFlags["x-replywithfile"]);
                this.oFlags["x-repliedwithfile"] = this.oFlags["x-replywithfile"];
                this.oFlags.Remove("x-replywithfile");
                goto Label_0346;
            }
            if ((this.port < 0) || (this.port > 0xffff))
            {
                FiddlerApplication.HandleHTTPError(this, SessionFlags.ProtocolViolationInRequest, true, false, "HTTP Request specified an invalid port number.");
            }
            if (this._isDirectRequestToFiddler())
            {
                if (CONFIG.bHookWithPAC && this.oRequest.headers.RequestPath.EndsWith("/proxy.pac"))
                {
                    this._returnPACFileResponse();
                    return;
                }
                if (this.oRequest.headers.RequestPath.EndsWith("/fiddlerroot.cer", StringComparison.OrdinalIgnoreCase))
                {
                    this._returnRootCert();
                    return;
                }
                if (CONFIG.iReverseProxyForPort == 0)
                {
                    this._returnEchoServiceResponse();
                    return;
                }
                this.oFlags.Add("X-ReverseProxy", "1");
                this.host = CONFIG.sReverseProxyHostname + ":" + CONFIG.iReverseProxyForPort.ToString();
            }
            if (this.HTTPMethodIs("CONNECT"))
            {
                this.isTunnel = true;
                if (this._handleHTTPSConnect())
                {
                    return;
                }
            }
        Label_0179:
            this.state = SessionStates.SendingRequest;
            if (!this.oResponse.ResendRequest())
            {
                this.CloseSessionPipes(true);
                this.state = SessionStates.Aborted;
                return;
            }
            this.Timers.ServerGotRequest = DateTime.Now;
            this.state = SessionStates.ReadingResponse;
            if ((this.ViewItem != null) || !this.ShouldBeHidden())
            {
                FiddlerApplication._frmMain.BeginInvoke(new updateUIDelegate(FiddlerApplication._frmMain.updateSession), new object[] { this });
            }
            if (!this.oResponse.ReadResponse())
            {
                if (!this.oResponse.bServerSocketReused || (this.state == SessionStates.Aborted))
                {
                    FiddlerApplication.DebugSpew("Failed to read server response. Aborting.");
                    if (this.state != SessionStates.Aborted)
                    {
                        this.oRequest.FailSession(0x1f8, "Fiddler - Receive Failure", "ReadResponse() failed: The server did not return a response for this request.");
                    }
                    this.CloseSessionPipes(true);
                    this.state = SessionStates.Aborted;
                    return;
                }
                FiddlerApplication.DebugSpew("[" + this.id.ToString() + "] ServerSocket Reuse failed. Restarting fresh.");
                this.oResponse.Initialize(true);
                goto Label_0179;
            }
            if (this.isAnyFlagSet(SessionFlags.ResponseBodyDropped))
            {
                this.responseBodyBytes = new byte[0];
                this.oResponse.FreeResponseDataBuffer();
            }
            else
            {
                long num;
                this.responseBodyBytes = this.oResponse.TakeEntity();
                if ((this.oResponse.headers.Exists("Content-Length") && !this.HTTPMethodIs("HEAD")) && (long.TryParse(this.oResponse.headers["Content-Length"], NumberStyles.Integer, NumberFormatInfo.InvariantInfo, out num) && (num != this.responseBodyBytes.LongLength)))
                {
                    FiddlerApplication.HandleHTTPError(this, SessionFlags.ProtocolViolationInResponse, true, true, string.Format("Content-Length mismatch: Response Header indicated {0:N0} bytes, but server sent {1:N0} bytes.", num, this.responseBodyBytes.LongLength));
                }
            }
        Label_0346:
            this.oFlags["x-ResponseBodyTransferLength"] = (this.responseBodyBytes == null) ? "0" : this.responseBodyBytes.LongLength.ToString();
            this.state = SessionStates.AutoTamperResponseBefore;
            this.ExecuteBasicResponseManipulations();
            FiddlerApplication.DoBeforeResponse(this);
            FiddlerApplication.oExtensions.DoAutoTamperResponseBefore(this);
            if ((((this.oResponse.headers.HTTPResponseCode > 0x12b) && (this.oResponse.headers.HTTPResponseCode < 0x134)) && (this.oFlags.ContainsKey("x-Builder-MaxRedir") && this.oResponse.headers.Exists("Location"))) && this.isValidAutoRedir(this.fullUrl, this.oResponse["Location"]))
            {
                int num2;
                this.nextSession = new Session(this.oRequest.pipeClient, null);
                this.nextSession.oRequest.headers = (HTTPRequestHeaders) this.oRequest.headers.Clone();
                string sString = this.oResponse.headers["Location"];
                sString = Utilities.TrimAfter(sString, '#');
                if ((!sString.StartsWith("/") && sString.Contains("://")) && (sString.IndexOf("://") < sString.IndexOf("/")))
                {
                    this.nextSession.fullUrl = new Uri(sString).AbsoluteUri;
                }
                else
                {
                    Uri baseUri = new Uri(this.fullUrl);
                    Uri uri2 = new Uri(baseUri, sString);
                    this.nextSession.fullUrl = uri2.AbsoluteUri;
                }
                if (this.oResponse.headers.HTTPResponseCode == 0x133)
                {
                    this.nextSession.requestBodyBytes = this.requestBodyBytes;
                }
                else
                {
                    this.nextSession.oRequest.headers.HTTPMethod = "GET";
                    this.nextSession.oRequest.headers.Remove("Content-Length");
                    this.nextSession.oRequest.headers.Remove("Transfer-Encoding");
                    this.nextSession.requestBodyBytes = new byte[0];
                }
                this.nextSession.SetBitFlag(SessionFlags.RequestGeneratedByFiddler, true);
                this.nextSession["x-From-Builder"] = "Redir";
                if (this.oFlags.ContainsKey("x-AutoAuth"))
                {
                    this.nextSession.oFlags["x-AutoAuth"] = this.oFlags["x-AutoAuth"];
                }
                if (this.oFlags.ContainsKey("x-Builder-Inspect"))
                {
                    this.nextSession.oFlags["x-Builder-Inspect"] = "1";
                }
                if (int.TryParse(this.oFlags["x-Builder-MaxRedir"], out num2))
                {
                    num2--;
                    if (num2 > 0)
                    {
                        this.nextSession.oFlags["x-Builder-MaxRedir"] = num2.ToString();
                    }
                }
                this.nextSession.state = SessionStates.AutoTamperRequestBefore;
                this.state = SessionStates.Done;
                this.FinishUISession(!this.bBufferResponse);
            }
            else
            {
                if ((this._isResponseMultiStageAuthChallenge() && this.oFlags.ContainsKey("x-AutoAuth")) && !this.oFlags.ContainsKey("x-AutoAuth-Failed"))
                {
                    bool flag2 = 0x197 == this.oResponse.headers.HTTPResponseCode;
                    if (this.__WebRequestForAuth == null)
                    {
                        this.__WebRequestForAuth = WebRequest.Create(this.fullUrl);
                    }
                    Uri uri3 = new Uri(this.fullUrl);
                    try
                    {
                        ICredentials defaultCredentials;
                        int num3;
                        System.Type type = this.__WebRequestForAuth.GetType();
                        type.InvokeMember("Async", BindingFlags.SetProperty | BindingFlags.NonPublic | BindingFlags.Instance, null, this.__WebRequestForAuth, new object[] { false });
                        object target = type.InvokeMember("ServerAuthenticationState", BindingFlags.GetProperty | BindingFlags.NonPublic | BindingFlags.Instance, null, this.__WebRequestForAuth, new object[0]);
                        if (target == null)
                        {
                            throw new ApplicationException("Auth state is null");
                        }
                        target.GetType().InvokeMember("ChallengedUri", BindingFlags.SetField | BindingFlags.NonPublic | BindingFlags.Instance, null, target, new object[] { uri3 });
                        string challenge = flag2 ? this.oResponse["Proxy-Authenticate"] : this.oResponse["WWW-Authenticate"];
                        if (this.oFlags["x-AutoAuth"].Contains(":"))
                        {
                            string str3 = Utilities.TrimAfter(this.oFlags["x-AutoAuth"], ':');
                            string domain = null;
                            if (str3.Contains(@"\"))
                            {
                                domain = Utilities.TrimAfter(str3, '\\');
                                defaultCredentials = new NetworkCredential(Utilities.TrimBefore(str3, '\\'), Utilities.TrimBefore(this.oFlags["x-AutoAuth"], ':'), domain);
                            }
                            else
                            {
                                defaultCredentials = new NetworkCredential(str3, Utilities.TrimBefore(this.oFlags["x-AutoAuth"], ':'));
                            }
                        }
                        else
                        {
                            defaultCredentials = CredentialCache.DefaultCredentials;
                        }
                        Authorization authorization = AuthenticationManager.Authenticate(challenge, this.__WebRequestForAuth, defaultCredentials);
                        if (authorization == null)
                        {
                            throw new Exception("AuthenticationManager.Authenticate returned null.");
                        }
                        string message = authorization.Message;
                        this.nextSession = new Session(this.oRequest.pipeClient, this.oResponse.pipeServer);
                        if (!authorization.Complete)
                        {
                            this.nextSession.__WebRequestForAuth = this.__WebRequestForAuth;
                        }
                        this.__WebRequestForAuth = null;
                        this.nextSession.requestBodyBytes = this.requestBodyBytes;
                        this.nextSession.oRequest.headers = (HTTPRequestHeaders) this.oRequest.headers.Clone();
                        this.nextSession.oRequest.headers[flag2 ? "Proxy-Authorization" : "Authorization"] = message;
                        this.nextSession.SetBitFlag(SessionFlags.RequestGeneratedByFiddler, true);
                        this.nextSession.oFlags["x-From-Builder"] = "Stage2";
                        if (int.TryParse(this.oFlags["x-AutoAuth-Retries"], out num3))
                        {
                            num3--;
                            if (num3 > 0)
                            {
                                this.nextSession.oFlags["x-AutoAuth"] = this.oFlags["x-AutoAuth"];
                                this.nextSession.oFlags["x-AutoAuth-Retries"] = num3.ToString();
                            }
                            else
                            {
                                this.nextSession.oFlags["x-AutoAuth-Failed"] = "true";
                            }
                        }
                        else
                        {
                            this.nextSession.oFlags["x-AutoAuth-Retries"] = "5";
                            this.nextSession.oFlags["x-AutoAuth"] = this.oFlags["x-AutoAuth"];
                        }
                        if (this.oFlags.ContainsKey("x-Builder-Inspect"))
                        {
                            this.nextSession.oFlags["x-Builder-Inspect"] = "1";
                        }
                        if (this.oFlags.ContainsKey("x-Builder-MaxRedir"))
                        {
                            this.nextSession.oFlags["x-Builder-MaxRedir"] = this.oFlags["x-Builder-MaxRedir"];
                        }
                        this.state = SessionStates.Done;
                        this.nextSession.state = SessionStates.AutoTamperRequestBefore;
                        this.FinishUISession(!this.bBufferResponse);
                        goto Label_0E82;
                    }
                    catch (Exception exception)
                    {
                        FiddlerApplication.Log.LogFormat("Automatic authentication was unsuccessful. {0}", new object[] { exception.Message });
                    }
                }
                bool flag3 = false;
                if ((this.m_state >= SessionStates.Done) || this.isFlagSet(SessionFlags.ResponseStreamed))
                {
                    this.FinishUISession(this.isFlagSet(SessionFlags.ResponseStreamed));
                    if (this.isFlagSet(SessionFlags.ResponseStreamed) && this.oFlags.ContainsKey("log-drop-response-body"))
                    {
                        this.SetBitFlag(SessionFlags.ResponseBodyDropped, true);
                        this.responseBodyBytes = new byte[0];
                    }
                    flag3 = true;
                }
                if (flag3)
                {
                    this.m_state = SessionStates.Done;
                    FiddlerApplication.DoAfterSessionComplete(this);
                }
                else
                {
                    if (this.oFlags.ContainsKey("x-replywithfile"))
                    {
                        this.LoadResponseFromFile(this.oFlags["x-replywithfile"]);
                        this.oFlags["x-replacedwithfile"] = this.oFlags["x-replywithfile"];
                        this.oFlags.Remove("x-replywithfile");
                    }
                    if (this.oFlags.ContainsKey("x-breakresponse") && !this.oFlags.ContainsKey("ui-hide"))
                    {
                        this.state = SessionStates.HandTamperResponse;
                        FiddlerApplication._frmMain.BeginInvoke(new updateUIDelegate(FiddlerApplication._frmMain.updateSession), new object[] { this });
                        Utilities.DoFlash(FiddlerApplication._frmMain.Handle);
                        FiddlerApplication._frmMain.notifyIcon.ShowBalloonTip(0x1388, "Fiddler Breakpoint", "Fiddler is paused at a response breakpoint", ToolTipIcon.Warning);
                        this.ThreadPause();
                        if (this.m_state >= SessionStates.Done)
                        {
                            return;
                        }
                    }
                    this.state = SessionStates.AutoTamperResponseAfter;
                    FiddlerApplication.oExtensions.DoAutoTamperResponseAfter(this);
                }
                bool bForceClientServerPipeAffinity = false;
                if (this._isResponseMultiStageAuthChallenge())
                {
                    bForceClientServerPipeAffinity = this._isNTLMType2();
                }
                if (this.m_state >= SessionStates.Done)
                {
                    this.FinishUISession();
                    flag3 = true;
                }
                if (!flag3)
                {
                    if (this.HTTPMethodIs("CONNECT") && (this.responseCode == 200))
                    {
                        try
                        {
                            if (this.oResponse.pipeServer == null)
                            {
                                if (!this.isAnyFlagSet(SessionFlags.ResponseGeneratedByFiddler))
                                {
                                    throw new InvalidDataException("ServerPipe is null.");
                                }
                            }
                            else if (!this.oResponse.pipeServer.SecureExistingConnection(this, this.hostname, this.oFlags["https-Client-Certificate"], "GATEWAY:HTTPS:" + this.PathAndQuery, ref this.Timers.HTTPSHandshakeTime))
                            {
                                throw new InvalidDataException("Unable to secure gateway connection.");
                            }
                            if (!this.oRequest.pipeClient.SecureClientPipe(this.oFlags["x-OverrideCertCN"] ?? this.hostname, this.oResponse.headers))
                            {
                                throw new InvalidDataException("Unable to secure client connection.");
                            }
                        }
                        catch (Exception exception2)
                        {
                            FiddlerApplication.Log.LogFormat("fiddler.network>Establishment of HTTPS tunnel failed for session {0}. {1}", new object[] { this.id, exception2.Message });
                            this.CloseSessionPipes(true);
                            this.oFlags["x-HTTPS-Tunnel-Failed"] = exception2.Message;
                            this.oResponse.headers.HTTPResponseStatus = "502 Unable to Secure Connection";
                            this.oResponse.headers.HTTPResponseCode = 0x1f6;
                            this.state = SessionStates.Aborted;
                            return;
                        }
                        this.Timers.ClientDoneResponse = DateTime.Now;
                        this.nextSession = new Session(this.oRequest.pipeClient, this.oResponse.pipeServer);
                        this.oRequest.pipeClient = null;
                        this.oResponse.pipeServer = null;
                        this.state = SessionStates.Done;
                        this.FinishUISession();
                        goto Label_0E82;
                    }
                    this.state = SessionStates.SendingResponse;
                    if (this.ReturnResponse(bForceClientServerPipeAffinity))
                    {
                        this.state = SessionStates.Done;
                    }
                    else
                    {
                        this.state = SessionStates.Aborted;
                    }
                }
                if (flag3 && (this.oRequest.pipeClient != null))
                {
                    if (bForceClientServerPipeAffinity || this._MayReuseMyClientPipe())
                    {
                        this._createNextSession(bForceClientServerPipeAffinity);
                    }
                    else
                    {
                        this.oRequest.pipeClient.End();
                    }
                    this.oRequest.pipeClient = null;
                }
                this.oResponse.releaseServerPipe();
            }
        Label_0E82:
            if (this.nextSession != null)
            {
                ThreadPool.UnsafeQueueUserWorkItem(new WaitCallback(this.nextSession.Execute), null);
                this.nextSession = null;
            }
        }

        public bool isAnyFlagSet(SessionFlags FlagsToTest)
        {
            return (SessionFlags.None != (this._bitFlags & FlagsToTest));
        }

        public bool isFlagSet(SessionFlags FlagsToTest)
        {
            return (FlagsToTest == (this._bitFlags & FlagsToTest));
        }

        private bool isValidAutoRedir(string sBase, string sTarget)
        {
            if (!sTarget.Contains("://"))
            {
                Uri baseUri = new Uri(sBase);
                sTarget = new Uri(baseUri, sTarget).ToString();
            }
            if (!sTarget.StartsWith("http://", StringComparison.OrdinalIgnoreCase) && !sTarget.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            {
                return sTarget.StartsWith("ftp://", StringComparison.OrdinalIgnoreCase);
            }
            return true;
        }

        public bool LoadMetadata(Stream strmMetadata)
        {
            string str = XmlConvert.ToString(true);
            SessionFlags none = SessionFlags.None;
            try
            {
                XmlTextReader reader = new XmlTextReader(strmMetadata);
                while (reader.Read())
                {
                    string str3;
                    if ((reader.NodeType == XmlNodeType.Element) && ((str3 = reader.Name) != null))
                    {
                        if (!(str3 == "Session"))
                        {
                            if (str3 == "SessionFlag")
                            {
                                goto Label_00BB;
                            }
                            if (str3 == "SessionTimers")
                            {
                                goto Label_00E1;
                            }
                            if (str3 == "PipeInfo")
                            {
                                goto Label_0292;
                            }
                        }
                        else
                        {
                            if (reader.GetAttribute("Aborted") != null)
                            {
                                this.m_state = SessionStates.Aborted;
                            }
                            if (reader.GetAttribute("BitFlags") != null)
                            {
                                this.BitFlags = (SessionFlags) uint.Parse(reader.GetAttribute("BitFlags"), NumberStyles.HexNumber);
                            }
                        }
                    }
                    continue;
                Label_00BB:
                    this.oFlags.Add(reader.GetAttribute("N"), reader.GetAttribute("V"));
                    continue;
                Label_00E1:
                    this.Timers.ClientConnected = XmlConvert.ToDateTime(reader.GetAttribute("ClientConnected"), XmlDateTimeSerializationMode.RoundtripKind);
                    string attribute = reader.GetAttribute("ClientBeginRequest");
                    if (attribute != null)
                    {
                        this.Timers.ClientBeginRequest = XmlConvert.ToDateTime(attribute, XmlDateTimeSerializationMode.RoundtripKind);
                    }
                    this.Timers.ClientDoneRequest = XmlConvert.ToDateTime(reader.GetAttribute("ClientDoneRequest"), XmlDateTimeSerializationMode.RoundtripKind);
                    attribute = reader.GetAttribute("GatewayTime");
                    if (attribute != null)
                    {
                        this.Timers.GatewayDeterminationTime = XmlConvert.ToInt32(attribute);
                    }
                    attribute = reader.GetAttribute("DNSTime");
                    if (attribute != null)
                    {
                        this.Timers.DNSTime = XmlConvert.ToInt32(attribute);
                    }
                    attribute = reader.GetAttribute("TCPConnectTime");
                    if (attribute != null)
                    {
                        this.Timers.TCPConnectTime = XmlConvert.ToInt32(attribute);
                    }
                    attribute = reader.GetAttribute("HTTPSHandshakeTime");
                    if (attribute != null)
                    {
                        this.Timers.HTTPSHandshakeTime = XmlConvert.ToInt32(attribute);
                    }
                    attribute = reader.GetAttribute("ServerConnected");
                    if (attribute != null)
                    {
                        this.Timers.ServerConnected = XmlConvert.ToDateTime(attribute, XmlDateTimeSerializationMode.RoundtripKind);
                    }
                    attribute = reader.GetAttribute("FiddlerBeginRequest");
                    if (attribute != null)
                    {
                        this.Timers.FiddlerBeginRequest = XmlConvert.ToDateTime(attribute, XmlDateTimeSerializationMode.RoundtripKind);
                    }
                    this.Timers.ServerGotRequest = XmlConvert.ToDateTime(reader.GetAttribute("ServerGotRequest"), XmlDateTimeSerializationMode.RoundtripKind);
                    attribute = reader.GetAttribute("ServerBeginResponse");
                    if (attribute != null)
                    {
                        this.Timers.ServerBeginResponse = XmlConvert.ToDateTime(attribute, XmlDateTimeSerializationMode.RoundtripKind);
                    }
                    this.Timers.ServerDoneResponse = XmlConvert.ToDateTime(reader.GetAttribute("ServerDoneResponse"), XmlDateTimeSerializationMode.RoundtripKind);
                    this.Timers.ClientBeginResponse = XmlConvert.ToDateTime(reader.GetAttribute("ClientBeginResponse"), XmlDateTimeSerializationMode.RoundtripKind);
                    this.Timers.ClientDoneResponse = XmlConvert.ToDateTime(reader.GetAttribute("ClientDoneResponse"), XmlDateTimeSerializationMode.RoundtripKind);
                    continue;
                Label_0292:
                    this.bBufferResponse = str != reader.GetAttribute("Streamed");
                    if (!this.bBufferResponse)
                    {
                        none |= SessionFlags.ResponseStreamed;
                    }
                    if (str == reader.GetAttribute("CltReuse"))
                    {
                        none |= SessionFlags.ClientPipeReused;
                    }
                    if (str == reader.GetAttribute("Reused"))
                    {
                        none |= SessionFlags.ServerPipeReused;
                    }
                    if (this.oResponse != null)
                    {
                        this.oResponse._bWasForwarded = str == reader.GetAttribute("Forwarded");
                        if (this.oResponse._bWasForwarded)
                        {
                            none |= SessionFlags.SentToGateway;
                        }
                    }
                }
                if (this.BitFlags == SessionFlags.None)
                {
                    this.BitFlags = none;
                }
                if (this.Timers.ClientBeginRequest.Ticks < 1L)
                {
                    this.Timers.ClientBeginRequest = this.Timers.ClientConnected;
                }
                if (this.Timers.FiddlerBeginRequest.Ticks < 1L)
                {
                    this.Timers.FiddlerBeginRequest = this.Timers.ServerGotRequest;
                }
                if ((this.m_clientPort == 0) && this.oFlags.ContainsKey("X-ClientPort"))
                {
                    int.TryParse(this.oFlags["X-ClientPort"], out this.m_clientPort);
                }
                reader.Close();
                return true;
            }
            catch (Exception exception)
            {
                FiddlerApplication.ReportException(exception);
                return false;
            }
        }

        [CodeDescription("Replace HTTP request headers and body using the specified file.")]
        public bool LoadRequestBodyFromFile(string sFilename)
        {
            if ((this.oRequest == null) || (this.oRequest.headers == null))
            {
                return false;
            }
            try
            {
                if (!Path.IsPathRooted(sFilename))
                {
                    sFilename = CONFIG.GetPath("Requests") + sFilename;
                }
            }
            catch (Exception)
            {
            }
            return this.oRequest.ReadRequestBodyFromFile(sFilename);
        }

        [CodeDescription("Replace HTTP response headers and body using the specified file.")]
        public bool LoadResponseFromFile(string sFilename)
        {
            if ((this.oResponse == null) || (this.oResponse.headers == null))
            {
                return false;
            }
            try
            {
                if (!Path.IsPathRooted(sFilename))
                {
                    string str = sFilename;
                    sFilename = CONFIG.GetPath("TemplateResponses") + str;
                    if (!System.IO.File.Exists(sFilename))
                    {
                        sFilename = CONFIG.GetPath("Responses") + str;
                    }
                }
            }
            catch (Exception)
            {
            }
            this.bBufferResponse = true;
            this.BitFlags |= SessionFlags.ResponseGeneratedByFiddler;
            bool flag = this.oResponse.ReadResponseFromFile(sFilename);
            if (flag && this.HTTPMethodIs("HEAD"))
            {
                this.responseBodyBytes = new byte[0];
            }
            return flag;
        }

        public void PoisonClientPipe()
        {
            this._bAllowClientPipeReuse = false;
        }

        public void PoisonServerPipe()
        {
            if (this.oResponse != null)
            {
                this.oResponse._PoisonPipe();
            }
        }

        private void RefreshMyInspectors()
        {
            if (FiddlerApplication._frmMain.InvokeRequired)
            {
                FiddlerApplication._frmMain.BeginInvoke(new updateUIDelegate(FiddlerApplication._frmMain.actRefreshInspectorsIfNeeded), new object[] { this });
            }
            else
            {
                FiddlerApplication._frmMain.actRefreshInspectorsIfNeeded(this);
            }
        }

        [CodeDescription("Update the currently displayed session information in the HTTP Sessions List.")]
        public void RefreshUI()
        {
            if (!FiddlerApplication.isClosing && (FiddlerApplication._frmMain != null))
            {
                FiddlerApplication._frmMain.BeginInvoke(new updateUIDelegate(FiddlerApplication._frmMain.finishSession), new object[] { this });
            }
        }

        [CodeDescription("Reset the SessionID counter to 0. This method can lead to confusing UI, so use sparingly.")]
        internal static void ResetSessionCounter()
        {
            Interlocked.Exchange(ref cRequests, 0);
        }

        internal bool ReturnResponse(bool bForceClientServerPipeAffinity)
        {
            bool flag = false;
            this.Timers.ClientBeginResponse = this.Timers.ClientDoneResponse = DateTime.Now;
            try
            {
                if ((this.oRequest.pipeClient != null) && this.oRequest.pipeClient.Connected)
                {
                    if (this.oFlags.ContainsKey("response-trickle-delay"))
                    {
                        int num = int.Parse(this.oFlags["response-trickle-delay"]);
                        this.oRequest.pipeClient.TransmitDelay = num;
                    }
                    this.oRequest.pipeClient.Send(this.oResponse.headers.ToByteArray(true, true));
                    this.oRequest.pipeClient.Send(this.responseBodyBytes);
                    this.Timers.ClientDoneResponse = DateTime.Now;
                    if (bForceClientServerPipeAffinity || this._MayReuseMyClientPipe())
                    {
                        this._createNextSession(bForceClientServerPipeAffinity);
                        flag = true;
                    }
                    else
                    {
                        if (CONFIG.bDebugSpew)
                        {
                            FiddlerApplication.DebugSpew(string.Format("fiddler.network.clientpipereuse> Closing client socket since bReuseClientSocket was false after returning [{0}]", this.url));
                        }
                        this.oRequest.pipeClient.End();
                        flag = true;
                    }
                }
                else
                {
                    this.state = SessionStates.Done;
                }
            }
            catch (Exception exception)
            {
                if (CONFIG.bDebugSpew)
                {
                    FiddlerApplication.DebugSpew(string.Format("Write to client failed for Session #{0}; exception was {1}", this.id, exception.ToString()));
                }
                this.state = SessionStates.Aborted;
            }
            this.oRequest.pipeClient = null;
            try
            {
                this.FinishUISession(false);
            }
            catch (Exception)
            {
            }
            FiddlerApplication.DoAfterSessionComplete(this);
            if (this.oFlags.ContainsKey("log-drop-response-body"))
            {
                this.oFlags["x-ResponseBodyFinalLength"] = (this.responseBodyBytes != null) ? this.responseBodyBytes.LongLength.ToString() : "0";
                this.SetBitFlag(SessionFlags.ResponseBodyDropped, true);
                this.responseBodyBytes = new byte[0];
            }
            return flag;
        }

        public bool SaveMetadata(string sFilename)
        {
            try
            {
                FileStream strmMetadata = new FileStream(sFilename, FileMode.Create, FileAccess.Write);
                this.WriteMetadataToStream(strmMetadata);
                strmMetadata.Close();
                return true;
            }
            catch (Exception exception)
            {
                FiddlerApplication.ReportException(exception);
                return false;
            }
        }

        public void SaveRequest(string sFilename, bool bHeadersOnly)
        {
            this.SaveRequest(sFilename, bHeadersOnly, false);
        }

        public void SaveRequest(string sFilename, bool bHeadersOnly, bool bIncludeSchemeAndHostInPath)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(sFilename));
            FileStream stream = new FileStream(sFilename, FileMode.Create, FileAccess.Write);
            if (this.oRequest.headers != null)
            {
                byte[] buffer = this.oRequest.headers.ToByteArray(true, true, bIncludeSchemeAndHostInPath);
                stream.Write(buffer, 0, buffer.Length);
                if (!bHeadersOnly && (this.requestBodyBytes != null))
                {
                    stream.Write(this.requestBodyBytes, 0, this.requestBodyBytes.Length);
                }
            }
            stream.Close();
        }

        [CodeDescription("Save HTTP request body to specified location.")]
        public bool SaveRequestBody(string sFilename)
        {
            try
            {
                Utilities.WriteArrayToFile(sFilename, this.requestBodyBytes);
                return true;
            }
            catch (Exception exception)
            {
                FiddlerApplication.DoNotifyUser(exception.Message + "\n\n" + sFilename, "Save Failed");
                return false;
            }
        }

        public void SaveResponse(string sFilename, bool bHeadersOnly)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(sFilename));
            FileStream stream = new FileStream(sFilename, FileMode.Create, FileAccess.Write);
            if (this.oResponse.headers != null)
            {
                byte[] buffer = this.oResponse.headers.ToByteArray(true, true);
                stream.Write(buffer, 0, buffer.Length);
                if (!bHeadersOnly && (this.responseBodyBytes != null))
                {
                    stream.Write(this.responseBodyBytes, 0, this.responseBodyBytes.Length);
                }
            }
            stream.Close();
        }

        [CodeDescription("Save HTTP response body to Fiddler Captures folder.")]
        public bool SaveResponseBody()
        {
            string path = CONFIG.GetPath("Captures");
            StringBuilder builder = new StringBuilder();
            builder.Append(this.SuggestedFilename);
            while (System.IO.File.Exists(path + builder.ToString()))
            {
                builder.Insert(0, this.id.ToString() + "_");
            }
            builder.Insert(0, path);
            return this.SaveResponseBody(builder.ToString());
        }

        [CodeDescription("Save HTTP response body to specified location.")]
        public bool SaveResponseBody(string sFilename)
        {
            try
            {
                Utilities.WriteArrayToFile(sFilename, this.responseBodyBytes);
                return true;
            }
            catch (Exception exception)
            {
                FiddlerApplication.DoNotifyUser(exception.Message + "\n\n" + sFilename, "Save Failed");
                return false;
            }
        }

        public void SaveSession(string sFilename, bool bHeadersOnly)
        {
            Utilities.EnsureOverwritable(sFilename);
            FileStream oFS = new FileStream(sFilename, FileMode.Create, FileAccess.Write);
            this.WriteToStream(oFS, bHeadersOnly);
            oFS.Close();
        }

        internal void SetBitFlag(SessionFlags FlagsToSet, bool b)
        {
            if (b)
            {
                this.BitFlags = this._bitFlags | FlagsToSet;
            }
            else
            {
                this.BitFlags = this._bitFlags & ~FlagsToSet;
            }
        }

        internal bool ShouldBeHidden()
        {
            if (!this.oFlags.ContainsKey("ui-hide"))
            {
                return false;
            }
            if (CONFIG.bReportHTTPErrors)
            {
                return !this.oFlags.ContainsKey("x-HTTPProtocol-Violation");
            }
            return true;
        }

        private void ThreadPause()
        {
            if (this.oSyncEvent == null)
            {
                this.oSyncEvent = new AutoResetEvent(false);
            }
            this.oSyncEvent.WaitOne();
        }

        public void ThreadResume()
        {
            if (this.oSyncEvent != null)
            {
                this.oSyncEvent.Set();
            }
        }

        public string ToHTMLFragment(bool HeadersOnly)
        {
            string str;
            if (!HeadersOnly)
            {
                string sInput = this.oRequest.headers.ToString(true, true);
                if (this.requestBodyBytes != null)
                {
                    sInput = sInput + Encoding.UTF8.GetString(this.requestBodyBytes);
                }
                str = "<SPAN CLASS='REQUEST'>" + Utilities.HtmlEncode(sInput).Replace("\r\n", "<BR>") + "</SPAN><BR>";
                if ((this.oResponse == null) || (this.oResponse.headers == null))
                {
                    return str;
                }
                string str3 = this.oResponse.headers.ToString(true, true);
                if (this.responseBodyBytes != null)
                {
                    Encoding encoding = Utilities.getResponseBodyEncoding(this);
                    str3 = str3 + encoding.GetString(this.responseBodyBytes);
                }
                return (str + "<SPAN CLASS='RESPONSE'>" + Utilities.HtmlEncode(str3).Replace("\r\n", "<BR>") + "</SPAN>");
            }
            str = "<SPAN CLASS='REQUEST'>" + Utilities.HtmlEncode(this.oRequest.headers.ToString()).Replace("\r\n", "<BR>") + "</SPAN><BR>";
            if ((this.oResponse != null) && (this.oResponse.headers != null))
            {
                str = str + "<SPAN CLASS='RESPONSE'>" + Utilities.HtmlEncode(this.oResponse.headers.ToString()).Replace("\r\n", "<BR>") + "</SPAN>";
            }
            return str;
        }

        public override string ToString()
        {
            return this.ToString(false);
        }

        public string ToString(bool HeadersOnly)
        {
            string str;
            if (!HeadersOnly)
            {
                str = this.oRequest.headers.ToString(true, true);
                if (this.requestBodyBytes != null)
                {
                    str = str + Encoding.UTF8.GetString(this.requestBodyBytes);
                }
                if ((this.oResponse != null) && (this.oResponse.headers != null))
                {
                    str = str + "\r\n" + this.oResponse.headers.ToString(true, true);
                    if (this.responseBodyBytes != null)
                    {
                        Encoding encoding = Utilities.getResponseBodyEncoding(this);
                        str = str + encoding.GetString(this.responseBodyBytes);
                    }
                }
                return str;
            }
            str = this.oRequest.headers.ToString();
            if ((this.oResponse != null) && (this.oResponse.headers != null))
            {
                str = str + "\r\n" + this.oResponse.headers.ToString();
            }
            return str;
        }

        [CodeDescription("Returns true if request URI contains the specified string. Case-insensitive.")]
        public bool uriContains(string sLookfor)
        {
            return (this.fullUrl.IndexOf(sLookfor, StringComparison.OrdinalIgnoreCase) > -1);
        }

        [CodeDescription("Use BZIP2 to compress the response body. Throws exceptions to caller.")]
        public bool utilBZIP2Response()
        {
            if (((this.responseBodyBytes != null) && (this.responseBodyBytes.LongLength > 0L)) && (!this.oResponse.headers.Exists("Content-Encoding") && !this.oResponse.headers.Exists("Transfer-Encoding")))
            {
                this.responseBodyBytes = Utilities.bzip2Compress(this.responseBodyBytes);
                this.oResponse.headers["Content-Encoding"] = "bzip2";
                this.oResponse.headers["Content-Length"] = (this.responseBodyBytes == null) ? "0" : this.responseBodyBytes.LongLength.ToString();
                return true;
            }
            return false;
        }

        [CodeDescription("Apply Transfer-Encoding: chunked to the response, if possible.")]
        public bool utilChunkResponse(int iSuggestedChunkCount)
        {
            if ((((((this.oRequest == null) || (this.oRequest.headers == null)) || (!this.oRequest.headers.HTTPVersion.Equals("HTTP/1.1", StringComparison.OrdinalIgnoreCase) || this.HTTPMethodIs("HEAD"))) || ((this.HTTPMethodIs("CONNECT") || (this.oResponse == null)) || ((this.oResponse.headers == null) || (this.oResponse.headers.HTTPResponseCode == 0x130)))) || ((this.oResponse.headers.HTTPResponseCode == 0xcc) || ((this.responseBodyBytes != null) && (this.responseBodyBytes.LongLength > 0x7fffffffL)))) || this.oResponse.headers.Exists("Transfer-Encoding"))
            {
                return false;
            }
            this.responseBodyBytes = Utilities.doChunk(this.responseBodyBytes, iSuggestedChunkCount);
            this.oResponse.headers.Remove("Content-Length");
            this.oResponse.headers["Transfer-Encoding"] = "chunked";
            return true;
        }

        [CodeDescription("Call inside OnBeforeRequest to create a response object and bypass the server.")]
        public void utilCreateResponseAndBypassServer()
        {
            if (this.state > SessionStates.SendingRequest)
            {
                throw new InvalidOperationException("Too late, we're already talking to the server.");
            }
            this.oResponse = new ServerChatter(this, "HTTP/1.1 200 OK\r\nContent-Length: 0\r\n\r\n");
            this.responseBodyBytes = new byte[0];
            this.oFlags["x-Fiddler-Generated"] = "utilCreateResponseAndBypassServer";
            this.BitFlags |= SessionFlags.ResponseGeneratedByFiddler;
            this.bBufferResponse = true;
            this.state = SessionStates.AutoTamperResponseBefore;
        }

        [CodeDescription("Removes chunking and HTTP Compression from the Request. Adds or updates Content-Length header.")]
        public bool utilDecodeRequest()
        {
            if (((this.oRequest == null) || (this.oRequest.headers == null)) || (!this.oRequest.headers.Exists("Transfer-Encoding") && !this.oRequest.headers.Exists("Content-Encoding")))
            {
                return false;
            }
            try
            {
                Utilities.utilDecodeHTTPBody(this.oRequest.headers, ref this.requestBodyBytes);
                this.oRequest.headers.Remove("Transfer-Encoding");
                this.oRequest.headers.Remove("Content-Encoding");
                this.oRequest.headers["Content-Length"] = (this.requestBodyBytes == null) ? "0" : this.requestBodyBytes.LongLength.ToString();
            }
            catch (Exception exception)
            {
                FiddlerApplication.ReportException(exception, "utilDecodeRequest failed for Session #" + this.id.ToString());
                return false;
            }
            return true;
        }

        [CodeDescription("Removes chunking and HTTP Compression from the response. Adds or updates Content-Length header.")]
        public bool utilDecodeResponse()
        {
            if (((this.oResponse == null) || (this.oResponse.headers == null)) || (!this.oResponse.headers.Exists("Transfer-Encoding") && !this.oResponse.headers.Exists("Content-Encoding")))
            {
                return false;
            }
            try
            {
                Utilities.utilDecodeHTTPBody(this.oResponse.headers, ref this.responseBodyBytes);
                this.oResponse.headers.Remove("Transfer-Encoding");
                this.oResponse.headers.Remove("Content-Encoding");
                this.oResponse.headers["Content-Length"] = (this.responseBodyBytes == null) ? "0" : this.responseBodyBytes.LongLength.ToString();
            }
            catch (Exception exception)
            {
                FiddlerApplication.ReportException(exception, "utilDecodeResponse failed for Session #" + this.id.ToString());
                return false;
            }
            return true;
        }

        [CodeDescription("Use DEFLATE to compress the response body. Throws exceptions to caller.")]
        public bool utilDeflateResponse()
        {
            if (((this.responseBodyBytes != null) && (this.responseBodyBytes.LongLength > 0L)) && (!this.oResponse.headers.Exists("Content-Encoding") && !this.oResponse.headers.Exists("Transfer-Encoding")))
            {
                this.responseBodyBytes = Utilities.DeflaterCompress(this.responseBodyBytes);
                this.oResponse.headers["Content-Encoding"] = "deflate";
                this.oResponse.headers["Content-Length"] = (this.responseBodyBytes == null) ? "0" : this.responseBodyBytes.LongLength.ToString();
                return true;
            }
            return false;
        }

        [CodeDescription("Find a string in the request body. Return its index or -1.")]
        public int utilFindInRequest(string sSearchFor, bool bCaseSensitive)
        {
            if ((this.requestBodyBytes == null) || (this.requestBodyBytes.LongLength == 0L))
            {
                return -1;
            }
            return Utilities.getEntityBodyEncoding(this.oRequest.headers, this.requestBodyBytes).GetString(this.requestBodyBytes).IndexOf(sSearchFor, bCaseSensitive ? StringComparison.InvariantCulture : StringComparison.InvariantCultureIgnoreCase);
        }

        [CodeDescription("Find a string in the response body. Return its index or -1. Note, you should call utilDecodeResponse first!")]
        public int utilFindInResponse(string sSearchFor, bool bCaseSensitive)
        {
            if ((this.responseBodyBytes == null) || (this.responseBodyBytes.LongLength == 0L))
            {
                return -1;
            }
            return Utilities.getResponseBodyEncoding(this).GetString(this.responseBodyBytes).IndexOf(sSearchFor, bCaseSensitive ? StringComparison.InvariantCulture : StringComparison.InvariantCultureIgnoreCase);
        }

        [CodeDescription("Use GZIP to compress the response body. Throws exceptions to caller.")]
        public bool utilGZIPResponse()
        {
            if (((this.responseBodyBytes != null) && (this.responseBodyBytes.LongLength > 0L)) && (!this.oResponse.headers.Exists("Content-Encoding") && !this.oResponse.headers.Exists("Transfer-Encoding")))
            {
                this.responseBodyBytes = Utilities.GzipCompress(this.responseBodyBytes);
                this.oResponse.headers["Content-Encoding"] = "gzip";
                this.oResponse["Content-Length"] = (this.responseBodyBytes == null) ? "0" : this.responseBodyBytes.LongLength.ToString();
                return true;
            }
            return false;
        }

        [CodeDescription("Prepend a string to the response body. Updates Content-Length header. Note, you should call utilDecodeResponse first!")]
        public void utilPrependToResponseBody(string sString)
        {
            if (this.responseBodyBytes == null)
            {
                this.responseBodyBytes = new byte[0];
            }
            byte[] bytes = Utilities.getResponseBodyEncoding(this).GetBytes(sString);
            byte[] dst = new byte[bytes.Length + this.responseBodyBytes.LongLength];
            System.Buffer.BlockCopy(bytes, 0, dst, 0, bytes.Length);
            System.Buffer.BlockCopy(this.responseBodyBytes, 0, dst, bytes.Length, this.responseBodyBytes.Length);
            this.responseBodyBytes = dst;
            this.oResponse["Content-Length"] = this.responseBodyBytes.LongLength.ToString();
        }

        [CodeDescription("Perform a case-sensitive string replacement on the request body (not URL!). Updates Content-Length header. Returns TRUE if replacements occur.")]
        public bool utilReplaceInRequest(string sSearchFor, string sReplaceWith)
        {
            if ((this.requestBodyBytes != null) && (this.requestBodyBytes.LongLength != 0L))
            {
                string str = Encoding.UTF8.GetString(this.requestBodyBytes);
                string s = str.Replace(sSearchFor, sReplaceWith);
                if (str != s)
                {
                    this.requestBodyBytes = Encoding.UTF8.GetBytes(s);
                    this.oRequest["Content-Length"] = this.requestBodyBytes.LongLength.ToString();
                    return true;
                }
            }
            return false;
        }

        [CodeDescription("Perform a case-sensitive string replacement on the response body. Updates Content-Length header. Note, you should call utilDecodeResponse first!  Returns TRUE if replacements occur.")]
        public bool utilReplaceInResponse(string sSearchFor, string sReplaceWith)
        {
            return this._innerReplaceInResponse(sSearchFor, sReplaceWith, true, true);
        }

        [CodeDescription("Perform a single case-sensitive string replacement on the response body. Updates Content-Length header. Note, you should call utilDecodeResponse first!  Returns TRUE if replacements occur.")]
        public bool utilReplaceOnceInResponse(string sSearchFor, string sReplaceWith, bool bCaseSensitive)
        {
            return this._innerReplaceInResponse(sSearchFor, sReplaceWith, false, bCaseSensitive);
        }

        [CodeDescription("Replaces the request body with sString as UTF8. Sets Content-Length header & removes Transfer-Encoding/Content-Encoding")]
        public void utilSetRequestBody(string sString)
        {
            this.oRequest.headers.Remove("Transfer-Encoding");
            this.oRequest.headers.Remove("Content-Encoding");
            this.requestBodyBytes = Encoding.UTF8.GetBytes(sString);
            this.oRequest["Content-Length"] = this.requestBodyBytes.LongLength.ToString();
        }

        [CodeDescription("Replaces the response body with sString. Sets Content-Length header & removes Transfer-Encoding/Content-Encoding")]
        public void utilSetResponseBody(string sString)
        {
            this.oResponse.headers.Remove("Transfer-Encoding");
            this.oResponse.headers.Remove("Content-Encoding");
            this.responseBodyBytes = Utilities.getResponseBodyEncoding(this).GetBytes(sString);
            this.oResponse["Content-Length"] = this.responseBodyBytes.LongLength.ToString();
        }

        public void WriteMetadataToStream(Stream strmMetadata)
        {
            XmlTextWriter writer = new XmlTextWriter(strmMetadata, Encoding.UTF8) {
                Formatting = Formatting.Indented
            };
            writer.WriteStartDocument();
            writer.WriteStartElement("Session");
            writer.WriteAttributeString("SID", this.id.ToString());
            writer.WriteAttributeString("BitFlags", ((uint) this.BitFlags).ToString("x"));
            if (this.m_state == SessionStates.Aborted)
            {
                writer.WriteAttributeString("Aborted", XmlConvert.ToString(true));
            }
            writer.WriteStartElement("SessionTimers");
            writer.WriteAttributeString("ClientConnected", XmlConvert.ToString(this.Timers.ClientConnected, XmlDateTimeSerializationMode.RoundtripKind));
            writer.WriteAttributeString("ClientBeginRequest", XmlConvert.ToString(this.Timers.ClientBeginRequest, XmlDateTimeSerializationMode.RoundtripKind));
            writer.WriteAttributeString("ClientDoneRequest", XmlConvert.ToString(this.Timers.ClientDoneRequest, XmlDateTimeSerializationMode.RoundtripKind));
            writer.WriteAttributeString("GatewayTime", XmlConvert.ToString(this.Timers.GatewayDeterminationTime));
            writer.WriteAttributeString("DNSTime", XmlConvert.ToString(this.Timers.DNSTime));
            writer.WriteAttributeString("TCPConnectTime", XmlConvert.ToString(this.Timers.TCPConnectTime));
            writer.WriteAttributeString("HTTPSHandshakeTime", XmlConvert.ToString(this.Timers.HTTPSHandshakeTime));
            writer.WriteAttributeString("ServerConnected", XmlConvert.ToString(this.Timers.ServerConnected, XmlDateTimeSerializationMode.RoundtripKind));
            writer.WriteAttributeString("FiddlerBeginRequest", XmlConvert.ToString(this.Timers.FiddlerBeginRequest, XmlDateTimeSerializationMode.RoundtripKind));
            writer.WriteAttributeString("ServerGotRequest", XmlConvert.ToString(this.Timers.ServerGotRequest, XmlDateTimeSerializationMode.RoundtripKind));
            writer.WriteAttributeString("ServerBeginResponse", XmlConvert.ToString(this.Timers.ServerBeginResponse, XmlDateTimeSerializationMode.RoundtripKind));
            writer.WriteAttributeString("ServerDoneResponse", XmlConvert.ToString(this.Timers.ServerDoneResponse, XmlDateTimeSerializationMode.RoundtripKind));
            writer.WriteAttributeString("ClientBeginResponse", XmlConvert.ToString(this.Timers.ClientBeginResponse, XmlDateTimeSerializationMode.RoundtripKind));
            writer.WriteAttributeString("ClientDoneResponse", XmlConvert.ToString(this.Timers.ClientDoneResponse, XmlDateTimeSerializationMode.RoundtripKind));
            writer.WriteEndElement();
            writer.WriteStartElement("PipeInfo");
            if (!this.bBufferResponse)
            {
                writer.WriteAttributeString("Streamed", XmlConvert.ToString(true));
            }
            if ((this.oRequest != null) && this.oRequest.bClientSocketReused)
            {
                writer.WriteAttributeString("CltReuse", XmlConvert.ToString(true));
            }
            if (this.oResponse != null)
            {
                if (this.oResponse.bServerSocketReused)
                {
                    writer.WriteAttributeString("Reused", XmlConvert.ToString(true));
                }
                if (this.oResponse.bWasForwarded)
                {
                    writer.WriteAttributeString("Forwarded", XmlConvert.ToString(true));
                }
            }
            writer.WriteEndElement();
            writer.WriteStartElement("SessionFlags");
            foreach (string str in this.oFlags.Keys)
            {
                writer.WriteStartElement("SessionFlag");
                writer.WriteAttributeString("N", str);
                writer.WriteAttributeString("V", this.oFlags[str]);
                writer.WriteEndElement();
            }
            writer.WriteEndElement();
            writer.WriteEndElement();
            writer.WriteEndDocument();
            writer.Flush();
        }

        public bool WriteRequestToStream(bool bHeadersOnly, bool bIncludeProtocolAndHostWithPath, Stream oFS)
        {
            if (this.oRequest.headers == null)
            {
                return false;
            }
            byte[] buffer = this.oRequest.headers.ToByteArray(true, true, bIncludeProtocolAndHostWithPath);
            oFS.Write(buffer, 0, buffer.Length);
            if (!bHeadersOnly && (this.requestBodyBytes != null))
            {
                oFS.Write(this.requestBodyBytes, 0, this.requestBodyBytes.Length);
            }
            return true;
        }

        public bool WriteResponseToStream(Stream oFS, bool bHeadersOnly)
        {
            if (this.oResponse.headers == null)
            {
                return false;
            }
            byte[] buffer = this.oResponse.headers.ToByteArray(true, true);
            oFS.Write(buffer, 0, buffer.Length);
            if (!bHeadersOnly && (this.responseBodyBytes != null))
            {
                oFS.Write(this.responseBodyBytes, 0, this.responseBodyBytes.Length);
            }
            return true;
        }

        [CodeDescription("Write the session (or session headers) to the specified stream")]
        public bool WriteToStream(Stream oFS, bool bHeadersOnly)
        {
            try
            {
                this.WriteRequestToStream(bHeadersOnly, true, oFS);
                oFS.WriteByte(13);
                oFS.WriteByte(10);
                this.WriteResponseToStream(oFS, bHeadersOnly);
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        [CodeDescription("Returns TRUE if this session state>readingresponse and oResponse not null.")]
        public bool bHasResponse
        {
            get
            {
                return ((this.state > SessionStates.ReadingResponse) && (null != this.oResponse));
            }
        }

        public SessionFlags BitFlags
        {
            get
            {
                return this._bitFlags;
            }
            internal set
            {
                if (CONFIG.bDebugSpew && (value != this._bitFlags))
                {
                    FiddlerApplication.DebugSpew(string.Format("Session #{0} bitflags adjusted from {1} to {2} @ {3}", new object[] { this.id, this._bitFlags, value, Environment.StackTrace }));
                }
                this._bitFlags = value;
            }
        }

        [CodeDescription("Set to true in OnBeforeRequest if this request should bypass the gateway")]
        public bool bypassGateway
        {
            get
            {
                return this._bypassGateway;
            }
            set
            {
                this._bypassGateway = value;
            }
        }

        [CodeDescription("Returns the Address used by the client to communicate to Fiddler.")]
        public string clientIP
        {
            get
            {
                if (this.m_clientIP != null)
                {
                    return this.m_clientIP;
                }
                return "0.0.0.0";
            }
        }

        [CodeDescription("Returns the port used by the client to communicate to Fiddler.")]
        public int clientPort
        {
            get
            {
                return this.m_clientPort;
            }
        }

        [CodeDescription("Retrieves the complete URI, including protocol/scheme, in the form http://www.host.com/filepath?query.")]
        public string fullUrl
        {
            get
            {
                if (this.oRequest.headers != null)
                {
                    return string.Format("{0}://{1}", this.oRequest.headers.UriScheme, this.url);
                }
                return string.Empty;
            }
            set
            {
                if (string.IsNullOrEmpty(value))
                {
                    throw new ArgumentException("Must specify a complete URI");
                }
                string str = Utilities.TrimAfter(value, "://").ToLower();
                string str2 = Utilities.TrimBefore(value, "://");
                if (((str != "http") && (str != "https")) && (str != "ftp"))
                {
                    throw new ArgumentException("URI scheme must be http, https, or ftp");
                }
                this.oRequest.headers.UriScheme = str;
                this.url = str2;
            }
        }

        [CodeDescription("Gets/Sets the host to which this request is targeted. MAY include a trailing port#.")]
        public string host
        {
            get
            {
                if (this.oRequest == null)
                {
                    return string.Empty;
                }
                return this.oRequest.host;
            }
            set
            {
                if (this.oRequest != null)
                {
                    this.oRequest.host = value;
                }
            }
        }

        [CodeDescription("Gets/Sets the hostname to which this request is targeted; does NOT include any port# but will include IPv6-literal brackets for IPv6 literals.")]
        public string hostname
        {
            get
            {
                if (((this.oRequest.headers == null) || (this.oRequest.host == null)) || (this.oRequest.host.Length <= 0))
                {
                    return string.Empty;
                }
                int length = this.oRequest.host.LastIndexOf(':');
                if ((length > -1) && (length > this.oRequest.host.LastIndexOf(']')))
                {
                    return this.oRequest.host.Substring(0, length);
                }
                return this.oRequest.host;
            }
            set
            {
                int startIndex = value.LastIndexOf(':');
                if ((startIndex > -1) && (startIndex > value.LastIndexOf(']')))
                {
                    throw new ArgumentException("Do not specify a port when setting hostname; use host property instead.");
                }
                string str = this.HTTPMethodIs("CONNECT") ? this.PathAndQuery : this.host;
                startIndex = str.LastIndexOf(':');
                if ((startIndex > -1) && (startIndex > str.LastIndexOf(']')))
                {
                    this.host = value + str.Substring(startIndex);
                }
                else
                {
                    this.host = value;
                }
            }
        }

        [CodeDescription("Returns the sequential number of this request.")]
        public int id
        {
            get
            {
                return this.m_requestID;
            }
        }

        [CodeDescription("When true, this session was conducted using the FTPS protocol.")]
        public bool isFTP
        {
            get
            {
                return (((this.oRequest != null) && (this.oRequest.headers != null)) && string.Equals(this.oRequest.headers.UriScheme, "FTP", StringComparison.OrdinalIgnoreCase));
            }
        }

        [CodeDescription("When true, this session was conducted using the HTTPS protocol.")]
        public bool isHTTPS
        {
            get
            {
                return (((this.oRequest != null) && (this.oRequest.headers != null)) && string.Equals(this.oRequest.headers.UriScheme, "HTTPS", StringComparison.OrdinalIgnoreCase));
            }
        }

        [CodeDescription("Indexer property into SESSION flags, REQUEST headers, and RESPONSE headers. e.g. oSession[\"Request\", \"Host\"] returns string value for the Request host header. If null, returns String.Empty")]
        public string this[string sCollection, string sName]
        {
            get
            {
                if (string.Equals(sCollection, "SESSION", StringComparison.OrdinalIgnoreCase))
                {
                    string str = this.oFlags[sName];
                    return (str ?? string.Empty);
                }
                if (string.Equals(sCollection, "REQUEST", StringComparison.OrdinalIgnoreCase))
                {
                    if ((this.oRequest != null) && (this.oRequest.headers != null))
                    {
                        return this.oRequest[sName];
                    }
                    return string.Empty;
                }
                if (!string.Equals(sCollection, "RESPONSE", StringComparison.OrdinalIgnoreCase))
                {
                    return "undefined";
                }
                if ((this.oResponse != null) && (this.oResponse.headers != null))
                {
                    return this.oResponse[sName];
                }
                return string.Empty;
            }
        }

        [CodeDescription("Indexer property into session flags collection. oSession[\"Flagname\"] returns string value, or null if missing.")]
        public string this[string sFlag]
        {
            get
            {
                return this.oFlags[sFlag];
            }
            set
            {
                if (value == null)
                {
                    this.oFlags.Remove(sFlag);
                }
                else
                {
                    this.oFlags[sFlag] = value;
                }
            }
        }

        [CodeDescription("Get the process ID of the application which made this request, or 0 if it cannot be determined.")]
        public int LocalProcessID
        {
            get
            {
                return this._LocalProcessID;
            }
        }

        [CodeDescription("Returns the path and query part of the URL. (For a CONNECT request, returns the host:port to be connected.)")]
        public string PathAndQuery
        {
            get
            {
                if (this.oRequest.headers != null)
                {
                    return this.oRequest.headers.RequestPath;
                }
                return string.Empty;
            }
            set
            {
                this.oRequest.headers.RequestPath = value;
            }
        }

        [CodeDescription("Returns the server port to which this request is targeted.")]
        public int port
        {
            get
            {
                string str;
                string requestPath;
                if (this.HTTPMethodIs("CONNECT"))
                {
                    requestPath = this.oRequest.headers.RequestPath;
                }
                else
                {
                    requestPath = this.oRequest.host;
                }
                int iPort = this.isHTTPS ? 0x1bb : (this.isFTP ? 0x15 : 80);
                Utilities.CrackHostAndPort(requestPath, out str, ref iPort);
                return iPort;
            }
            set
            {
                if ((value < 0) || (value > 0xffff))
                {
                    throw new ArgumentException("A valid target port value (0-65535) must be specified.");
                }
                this.host = this.hostname + ":" + value.ToString();
            }
        }

        [CodeDescription("Gets or Sets the HTTP Status code of the server's response")]
        public int responseCode
        {
            get
            {
                if ((this.oResponse != null) && (this.oResponse.headers != null))
                {
                    return this.oResponse.headers.HTTPResponseCode;
                }
                return 0;
            }
            set
            {
                if ((this.oResponse != null) && (this.oResponse.headers != null))
                {
                    this.oResponse.headers.HTTPResponseCode = value;
                    this.oResponse.headers.HTTPResponseStatus = value.ToString() + " Fiddled";
                }
            }
        }

        [CodeDescription("Enumerated state of the current session.")]
        public SessionStates state
        {
            get
            {
                return this.m_state;
            }
            set
            {
                SessionStates state = this.m_state;
                this.m_state = value;
                if (this.m_state == SessionStates.Aborted)
                {
                    this.FinishUISession(true);
                }
                else if (((state == SessionStates.HandTamperRequest) || (state == SessionStates.HandTamperResponse)) || ((this.m_state == SessionStates.HandTamperRequest) || (this.m_state == SessionStates.HandTamperResponse)))
                {
                    this.RefreshMyInspectors();
                }
                if (this._OnStateChanged != null)
                {
                    StateChangeEventArgs e = new StateChangeEventArgs(state, value);
                    this._OnStateChanged(this, e);
                    if (this.m_state >= SessionStates.Done)
                    {
                        this._OnStateChanged = null;
                    }
                }
            }
        }

        [CodeDescription("Gets a path-less filename suitable for the Response entity. Uses Content-Disposition if available.")]
        public string SuggestedFilename
        {
            get
            {
                if ((this.oResponse == null) || (this.oResponse.headers == null))
                {
                    return (this.id.ToString() + ".txt");
                }
                if ((this.responseBodyBytes == null) || (0L == this.responseBodyBytes.LongLength))
                {
                    string format = "{0}_Status{1}.txt";
                    return string.Format(format, this.id.ToString(), this.responseCode.ToString());
                }
                string tokenValue = this.oResponse.headers.GetTokenValue("Content-Disposition", "filename");
                if (tokenValue != null)
                {
                    return this._MakeSafeFilename(tokenValue);
                }
                string sFilename = Utilities.TrimBeforeLast(Utilities.TrimAfter(this.url, '?'), '/');
                if (((sFilename.Length > 0) && (sFilename.Length < 0x40)) && (sFilename.Contains(".") && (sFilename.LastIndexOf('.') == sFilename.IndexOf('.'))))
                {
                    string str4 = this._MakeSafeFilename(sFilename);
                    string str5 = string.Empty;
                    if (((this.url.Contains("?") || (str4.Length < 1)) || (str4.EndsWith(".php") || str4.EndsWith(".aspx"))) || ((str4.EndsWith(".asp") || str4.EndsWith(".asmx")) || str4.EndsWith(".cgi")))
                    {
                        str5 = Utilities.FileExtensionForMIMEType(this.oResponse.MIMEType);
                        if (str4.EndsWith(str5, StringComparison.OrdinalIgnoreCase))
                        {
                            str5 = string.Empty;
                        }
                    }
                    string str6 = FiddlerApplication.Prefs.GetBoolPref("fiddler.session.prependIDtosuggestedfilename", false) ? "{0}_{1}{2}" : "{1}{2}";
                    return string.Format(str6, this.id.ToString(), str4, str5);
                }
                StringBuilder builder = new StringBuilder(0x20);
                builder.Append(this.id);
                builder.Append("_");
                string mIMEType = this.oResponse.MIMEType;
                builder.Append(Utilities.FileExtensionForMIMEType(mIMEType));
                return builder.ToString();
            }
        }

        [CodeDescription("Gets or sets the URL (without protocol) being requested from the server, in the form www.host.com/filepath?query.")]
        public string url
        {
            get
            {
                if (this.HTTPMethodIs("CONNECT"))
                {
                    return this.PathAndQuery;
                }
                return (this.host + this.PathAndQuery);
            }
            set
            {
                int length = value.IndexOfAny(new char[] { '/', '?' });
                if (length > -1)
                {
                    this.host = value.Substring(0, length);
                    this.PathAndQuery = value.Substring(length);
                }
                else
                {
                    this.host = value;
                    this.PathAndQuery = "/";
                }
            }
        }
    }
}

