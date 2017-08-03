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

    public class ServerChatter
    {
        private bool _bLeakedHeaders;
        internal bool _bWasForwarded;
        internal static int _cbServerReadBuffer = 0x8000;
        private int _iBodySeekProgress;
        private long _lngLastChunkInfoOffset;
        private long _lngLeakedOffset;
        private int iEntityBodyOffset;
        private HTTPResponseHeaders m_inHeaders;
        private MemoryStream m_responseData;
        private long m_responseTotalDataCount;
        private Session m_session;
        public ServerPipe pipeServer;

        internal ServerChatter(Session oSession)
        {
            this._lngLastChunkInfoOffset = -1L;
            this.m_session = oSession;
            this.m_responseData = new MemoryStream(0x4000);
        }

        internal ServerChatter(Session oSession, string sHeaders)
        {
            this._lngLastChunkInfoOffset = -1L;
            this.m_session = oSession;
            this.m_inHeaders = Parser.ParseResponse(sHeaders);
        }

        private void _deleteInformationalMessage()
        {
            this.m_inHeaders = null;
            byte[] buffer = new byte[this.m_responseData.Length - this.iEntityBodyOffset];
            this.m_responseData.Position = this.iEntityBodyOffset;
            this.m_responseData.Read(buffer, 0, buffer.Length);
            this.m_responseData.Dispose();
            this.m_responseData = new MemoryStream(buffer.Length);
            this.m_responseData.Write(buffer, 0, buffer.Length);
            this.m_responseTotalDataCount = 0L;
            this.iEntityBodyOffset = this._iBodySeekProgress = 0;
        }

        internal void _detachServerPipe()
        {
            if (this.pipeServer != null)
            {
                if (((this.pipeServer.ReusePolicy != PipeReusePolicy.NoReuse) && (this.pipeServer.ReusePolicy != PipeReusePolicy.MarriedToClientPipe)) && (this.pipeServer.isClientCertAttached && !this.pipeServer.isAuthenticated))
                {
                    this.pipeServer.MarkAsAuthenticated(this.m_session.LocalProcessID);
                }
                if ((this.pipeServer.ReusePolicy == PipeReusePolicy.NoReuse) || (this.pipeServer.ReusePolicy == PipeReusePolicy.MarriedToClientPipe))
                {
                    if (this.pipeServer.Connected)
                    {
                        this.pipeServer.End();
                    }
                }
                else
                {
                    Proxy.htServerPipePool.EnqueuePipe(this.pipeServer);
                }
                this.pipeServer = null;
            }
        }

        internal byte[] _PeekAtBody()
        {
            if (((this.iEntityBodyOffset < 1) || (this.m_responseData == null)) || (this.m_responseData.Length < 1L))
            {
                return new byte[0];
            }
            int count = ((int) this.m_responseData.Length) - this.iEntityBodyOffset;
            if (count < 1)
            {
                return new byte[0];
            }
            byte[] dst = new byte[count];
            Buffer.BlockCopy(this.m_responseData.GetBuffer(), this.iEntityBodyOffset, dst, 0, count);
            return dst;
        }

        internal void _PoisonPipe()
        {
            if (this.pipeServer != null)
            {
                this.pipeServer.ReusePolicy = PipeReusePolicy.NoReuse;
            }
        }

        private bool ConnectToHost()
        {
            IPEndPoint point = null;
            string str2;
            string str3;
            IPAddress[] addressArray;
            string sHostAndPort = this.m_session.oFlags["x-overrideHost"];
            if (sHostAndPort == null)
            {
                sHostAndPort = this.m_session.host;
            }
            if (this.m_session.oFlags["x-overrideGateway"] != null)
            {
                if (string.Equals("DIRECT", this.m_session.oFlags["x-overrideGateway"], StringComparison.OrdinalIgnoreCase))
                {
                    this.m_session.bypassGateway = true;
                }
                else
                {
                    point = Utilities.IPEndPointFromHostPortString(this.m_session.oFlags["x-overrideGateway"]);
                }
            }
            else if (!this.m_session.bypassGateway)
            {
                int tickCount = Environment.TickCount;
                point = FiddlerApplication.oProxy.FindGatewayForOrigin(this.m_session.oRequest.headers.UriScheme, sHostAndPort);
                this.m_session.Timers.GatewayDeterminationTime = Environment.TickCount - tickCount;
            }
            if (point != null)
            {
                this._bWasForwarded = true;
            }
            else if (this.m_session.isFTP)
            {
                this.m_session.oRequest.FailSession(0x1f6, "Fiddler - FTP Connection Failed", "[Fiddler] Fiddler does not support proxying FTP traffic without an upstream HTTP->FTP proxy.");
                return false;
            }
            int iPort = this.m_session.isHTTPS ? 0x1bb : (this.m_session.isFTP ? 0x15 : 80);
            Utilities.CrackHostAndPort(sHostAndPort, out str2, ref iPort);
            if (point != null)
            {
                if (this.m_session.isHTTPS)
                {
                    str3 = "GATEWAY:HTTPS:" + str2 + ":" + iPort.ToString();
                }
                else
                {
                    str3 = "GW:" + point.ToString();
                }
            }
            else
            {
                str3 = (this.m_session.isHTTPS ? "HTTPS:" : "") + str2 + ":" + iPort.ToString();
            }
            if (((this.pipeServer != null) && !this.m_session.oFlags.ContainsKey("X-ServerPipe-Marriage-Trumps-All")) && !this.SIDsMatch(this.m_session.LocalProcessID, str3, this.pipeServer.sPoolKey))
            {
                this._detachServerPipe();
            }
            if ((this.pipeServer == null) && !this.m_session.oFlags.ContainsKey("X-Bypass-ServerPipe-Reuse-Pool"))
            {
                this.pipeServer = Proxy.htServerPipePool.DequeuePipe(str3, this.m_session.LocalProcessID, this.m_session.id);
            }
            if (this.pipeServer != null)
            {
                StringDictionary dictionary;
                this.m_session.Timers.ServerConnected = this.pipeServer.dtConnected;
                (dictionary = this.m_session.oFlags)["x-serversocket"] = dictionary["x-serversocket"] + "REUSE " + this.pipeServer._sPipeName;
                if ((this.pipeServer.Address != null) && !this.pipeServer.isConnectedToGateway)
                {
                    this.m_session.m_hostIP = this.pipeServer.Address.ToString();
                    this.m_session.oFlags["x-hostIP"] = this.m_session.m_hostIP;
                }
                return true;
            }
            int port = iPort;
            if (point != null)
            {
                addressArray = new IPAddress[] { point.Address };
                port = point.Port;
            }
            else
            {
                try
                {
                    addressArray = DNSResolver.GetIPAddressList(str2, true, this.m_session.Timers);
                }
                catch (Exception exception)
                {
                    this.m_session.oRequest.FailSession(0x1f6, "Fiddler - DNS Lookup Failed", "Fiddler: DNS Lookup for " + Utilities.HtmlEncode(str2) + " failed. " + exception.Message);
                    return false;
                }
                if ((port < 0) || (port > 0xffff))
                {
                    this.m_session.oRequest.FailSession(0x1f6, "Invalid Request", "HTTP Request specified an invalid port number.");
                    return false;
                }
            }
            try
            {
                this.pipeServer = new ServerPipe("ServerPipe#" + this.m_session.id.ToString(), this._bWasForwarded);
                Socket oSocket = CreateConnectedSocket(addressArray, port, this.m_session);
                if (this._bWasForwarded)
                {
                    if (!this.m_session.isHTTPS)
                    {
                        this.pipeServer.WrapSocketInPipe(this.m_session, oSocket, false, false, str2, this.m_session.oFlags["https-Client-Certificate"], "GW:" + point.ToString(), ref this.m_session.Timers.HTTPSHandshakeTime);
                    }
                    else
                    {
                        this.m_session.oFlags["x-CreatedTunnel"] = "Fiddler-Created-A-CONNECT-Tunnel";
                        this.pipeServer.WrapSocketInPipe(this.m_session, oSocket, true, true, str2, this.m_session.oFlags["https-Client-Certificate"], "HTTPS:" + str2 + ":" + iPort.ToString(), ref this.m_session.Timers.HTTPSHandshakeTime);
                    }
                }
                else
                {
                    this.pipeServer.WrapSocketInPipe(this.m_session, oSocket, this.m_session.isHTTPS, false, str2, this.m_session.oFlags["https-Client-Certificate"], (this.m_session.isHTTPS ? "HTTPS:" : "") + str2 + ":" + iPort.ToString(), ref this.m_session.Timers.HTTPSHandshakeTime);
                }
                return true;
            }
            catch (Exception exception2)
            {
                if (this._bWasForwarded)
                {
                    this.m_session.oRequest.FailSession(0x1f6, "Fiddler - Gateway Connection Failed", "[Fiddler] Connection to Gateway failed.<BR>Exception Text: " + exception2.Message);
                }
                else
                {
                    this.m_session.oRequest.FailSession(0x1f6, "Fiddler - Connection Failed", "[Fiddler] Connection to " + Utilities.HtmlEncode(str2) + " failed.<BR>Exception Text: " + exception2.Message);
                }
                return false;
            }
        }

        internal static Socket CreateConnectedSocket(IPAddress[] arrDestIPs, int iPort, Session _oSession)
        {
            Socket socket = null;
            bool flag = false;
            Stopwatch stopwatch = Stopwatch.StartNew();
            Exception exception = null;
            foreach (IPAddress address in arrDestIPs)
            {
                try
                {
                    socket = new Socket(address.AddressFamily, SocketType.Stream, ProtocolType.Tcp) {
                        NoDelay = true
                    };
                    if (FiddlerApplication.oProxy._DefaultEgressEndPoint != null)
                    {
                        socket.Bind(FiddlerApplication.oProxy._DefaultEgressEndPoint);
                    }
                    socket.Connect(address, iPort);
                    _oSession.m_hostIP = address.ToString();
                    _oSession.oFlags["x-hostIP"] = _oSession.m_hostIP;
                    flag = true;
                    break;
                }
                catch (Exception exception2)
                {
                    exception = exception2;
                    if (!FiddlerApplication.Prefs.GetBoolPref("fiddler.network.dns.fallback", true))
                    {
                        break;
                    }
                    _oSession.oFlags["x-DNS-Failover"] = _oSession.oFlags["x-DNS-Failover"] + "+1";
                }
            }
            _oSession.Timers.ServerConnected = DateTime.Now;
            _oSession.Timers.TCPConnectTime = (int) stopwatch.ElapsedMilliseconds;
            if (!flag)
            {
                throw exception;
            }
            return socket;
        }

        internal static Socket CreateSOCKSSocket(IPEndPoint ipepSOCKS, string sTargetHost, int iPort, Session _oSession)
        {
            Socket socket = null;
            bool flag = false;
            Stopwatch stopwatch = Stopwatch.StartNew();
            Exception exception = null;
            try
            {
                socket = new Socket(ipepSOCKS.AddressFamily, SocketType.Stream, ProtocolType.Tcp) {
                    NoDelay = true
                };
                if (FiddlerApplication.oProxy._DefaultEgressEndPoint != null)
                {
                    socket.Bind(FiddlerApplication.oProxy._DefaultEgressEndPoint);
                }
                socket.Connect(ipepSOCKS.Address, ipepSOCKS.Port);
                byte[] buffer = GetSOCKS4ConnectToTarget(sTargetHost, iPort);
                socket.Send(buffer);
                byte[] buffer2 = new byte[0x40];
                if (((socket.Receive(buffer2) > 1) && (buffer2[0] == 0)) && (buffer2[1] == 90))
                {
                    flag = true;
                }
                if (!flag)
                {
                    socket = null;
                    throw new InvalidDataException("SOCKS server returned failure");
                }
            }
            catch (Exception exception2)
            {
                exception = exception2;
            }
            _oSession.Timers.ServerConnected = DateTime.Now;
            _oSession.Timers.TCPConnectTime = (int) stopwatch.ElapsedMilliseconds;
            if (!flag)
            {
                throw exception;
            }
            return socket;
        }

        internal void FreeResponseDataBuffer()
        {
            this.m_responseData.Dispose();
            this.m_responseData = null;
        }

        private bool GetHeaders()
        {
            if (!this.HeadersAvailable())
            {
                return false;
            }
            if (!this.ParseResponseForHeaders())
            {
                string str;
                this.m_session.SetBitFlag(SessionFlags.ProtocolViolationInResponse, true);
                this._PoisonPipe();
                if (this.m_responseData != null)
                {
                    str = "<plaintext>\n" + Utilities.ByteArrayToHexView(this.m_responseData.GetBuffer(), 0x18, (int) Math.Min(this.m_responseData.Length, 0x800L));
                }
                else
                {
                    str = "{Fiddler:no data}";
                }
                this.m_session.oRequest.FailSession(500, "Fiddler - Bad Response", string.Format("[Fiddler] Response Header parsing failed.\n{0}Response Data:\n{1}", this.m_session.isFlagSet(SessionFlags.ServerPipeReused) ? "This can be caused by an illegal HTTP response earlier on this reused server socket-- for instance, a HTTP/304 response which illegally contains a body.\n" : string.Empty, str));
                return true;
            }
            if ((this.m_inHeaders.HTTPResponseCode <= 0x63) || (this.m_inHeaders.HTTPResponseCode >= 200))
            {
                return true;
            }
            if (this.m_inHeaders.Exists("Content-Length") && ("0" != this.m_inHeaders["Content-Length"].Trim()))
            {
                FiddlerApplication.HandleHTTPError(this.m_session, SessionFlags.ProtocolViolationInResponse, false, true, "HTTP/1xx responses MUST NOT contain a body, but a non-zero content-length was returned.");
            }
            if (FiddlerApplication.Prefs.GetBoolPref("fiddler.network.streaming.leakhttp1xx", true) && (this.m_session.oRequest.pipeClient != null))
            {
                try
                {
                    StringDictionary dictionary;
                    this.m_session.oRequest.pipeClient.Send(this.m_inHeaders.ToByteArray(true, true));
                    (dictionary = this.m_session.oFlags)["x-fiddler-Stream1xx"] = dictionary["x-fiddler-Stream1xx"] + "Returned a HTTP/" + this.m_inHeaders.HTTPResponseCode.ToString() + " message from the server.";
                }
                catch (Exception exception)
                {
                    if (FiddlerApplication.Prefs.GetBoolPref("fiddler.network.streaming.abortifclientaborts", false))
                    {
                        throw new Exception("Leaking HTTP/1xx response to client failed", exception);
                    }
                    FiddlerApplication.Log.LogFormat("fiddler.network.streaming> Streaming of HTTP/1xx headers from #{0} to client failed: {1}", new object[] { this.m_session.id, exception.Message });
                }
            }
            else
            {
                StringDictionary dictionary2;
                (dictionary2 = this.m_session.oFlags)["x-fiddler-streaming"] = dictionary2["x-fiddler-streaming"] + "Eating a HTTP/" + this.m_inHeaders.HTTPResponseCode.ToString() + " message from the stream.";
            }
            this._deleteInformationalMessage();
            return this.GetHeaders();
        }

        private static byte[] GetSOCKS4ConnectToTarget(string sTargetHost, int iPort)
        {
            MemoryStream stream = new MemoryStream();
            byte[] bytes = Encoding.ASCII.GetBytes(sTargetHost);
            stream.WriteByte(4);
            stream.WriteByte(1);
            stream.WriteByte((byte) (iPort >> 8));
            stream.WriteByte((byte) (iPort & 0xff));
            stream.WriteByte(0);
            stream.WriteByte(0);
            stream.WriteByte(0);
            stream.WriteByte(0x7f);
            stream.WriteByte(0);
            stream.Write(bytes, 0, bytes.Length);
            stream.WriteByte(0);
            return stream.ToArray();
        }

        private static byte[] GetSOCKS5ConnectToTarget(string sTargetHost, int iPort)
        {
            MemoryStream stream = new MemoryStream();
            byte[] bytes = Encoding.ASCII.GetBytes(sTargetHost);
            stream.WriteByte(5);
            stream.WriteByte(1);
            stream.WriteByte(0);
            stream.WriteByte(3);
            stream.WriteByte((byte) bytes.Length);
            stream.Write(bytes, 0, bytes.Length);
            stream.WriteByte((byte) (iPort >> 8));
            stream.WriteByte((byte) (iPort & 0xff));
            return stream.ToArray();
        }

        private static byte[] GetSOCKS5Handshake()
        {
            return new byte[] { 5, 2, 0, 2 };
        }

        private bool HeadersAvailable()
        {
            if (this.iEntityBodyOffset <= 0)
            {
                HTTPHeaderParseWarnings warnings;
                if (this.m_responseData == null)
                {
                    return false;
                }
                if (!Parser.FindEndOfHeaders(this.m_responseData.GetBuffer(), ref this._iBodySeekProgress, this.m_responseData.Length, out warnings))
                {
                    return false;
                }
                this.iEntityBodyOffset = this._iBodySeekProgress + 1;
                switch (warnings)
                {
                    case HTTPHeaderParseWarnings.EndedWithLFLF:
                        FiddlerApplication.HandleHTTPError(this.m_session, SessionFlags.ProtocolViolationInResponse, false, true, "The Server did not return properly formatted HTTP Headers. HTTP headers\nshould be terminated with CRLFCRLF. These were terminated with LFLF.");
                        break;

                    case HTTPHeaderParseWarnings.EndedWithLFCRLF:
                        FiddlerApplication.HandleHTTPError(this.m_session, SessionFlags.ProtocolViolationInResponse, false, true, "The Server did not return properly formatted HTTP Headers. HTTP headers\nshould be terminated with CRLFCRLF. These were terminated with LFCRLF.");
                        break;
                }
            }
            return true;
        }

        internal void Initialize(bool bAlloc)
        {
            if (bAlloc)
            {
                this.m_responseData = new MemoryStream(0x4000);
            }
            else
            {
                this.m_responseData = null;
            }
            this._lngLeakedOffset = this._iBodySeekProgress = this.iEntityBodyOffset = 0;
            this._lngLastChunkInfoOffset = -1L;
            this.m_inHeaders = null;
            this._bLeakedHeaders = false;
            this.pipeServer = null;
            this._bWasForwarded = false;
            this.m_session.SetBitFlag(SessionFlags.ServerPipeReused, false);
        }

        private bool isResponseBodyComplete()
        {
            if (this.m_session.HTTPMethodIs("HEAD"))
            {
                return true;
            }
            if (this.m_session.HTTPMethodIs("CONNECT") && (this.m_inHeaders.HTTPResponseCode == 200))
            {
                return true;
            }
            if (((this.m_inHeaders.HTTPResponseCode == 0xcc) || (this.m_inHeaders.HTTPResponseCode == 0xcd)) || (this.m_inHeaders.HTTPResponseCode == 0x130))
            {
                if (this.m_inHeaders.Exists("Content-Length") && ("0" != this.m_inHeaders["Content-Length"].Trim()))
                {
                    FiddlerApplication.HandleHTTPError(this.m_session, SessionFlags.ProtocolViolationInResponse, false, true, "This type of HTTP response MUST NOT contain a body, but a non-zero content-length was returned.");
                    return true;
                }
                return true;
            }
            if (this.m_inHeaders.ExistsAndEquals("Transfer-Encoding", "chunked"))
            {
                long num;
                if (this._lngLastChunkInfoOffset < this.iEntityBodyOffset)
                {
                    this._lngLastChunkInfoOffset = this.iEntityBodyOffset;
                }
                return Utilities.IsChunkedBodyComplete(this.m_session, this.m_responseData, this._lngLastChunkInfoOffset, out this._lngLastChunkInfoOffset, out num);
            }
            if (this.m_inHeaders.Exists("Content-Length"))
            {
                long num2;
                if (long.TryParse(this.m_inHeaders["Content-Length"], NumberStyles.Integer, NumberFormatInfo.InvariantInfo, out num2) && (num2 >= 0L))
                {
                    return (this.m_responseTotalDataCount >= (this.iEntityBodyOffset + num2));
                }
                FiddlerApplication.HandleHTTPError(this.m_session, SessionFlags.ProtocolViolationInResponse, true, true, "Content-Length response header is not a valid unsigned integer.\nContent-Length: " + this.m_inHeaders["Content-Length"]);
                return true;
            }
            if ((!this.m_inHeaders.ExistsAndEquals("Connection", "close") && !this.m_inHeaders.ExistsAndEquals("Proxy-Connection", "close")) && ((this.m_inHeaders.HTTPVersion != "HTTP/1.0") || this.m_inHeaders.ExistsAndContains("Connection", "Keep-Alive")))
            {
                FiddlerApplication.HandleHTTPError(this.m_session, SessionFlags.ProtocolViolationInResponse, true, true, "No Connection: close, no Content-Length. No way to tell if the response is complete.");
            }
            return false;
        }

        private bool LeakResponseBytes()
        {
            try
            {
                if (this.m_session.oRequest.pipeClient == null)
                {
                    return false;
                }
                if (!this._bLeakedHeaders)
                {
                    if (((0x191 == this.m_inHeaders.HTTPResponseCode) && this.m_inHeaders["WWW-Authenticate"].StartsWith("N", StringComparison.OrdinalIgnoreCase)) || ((0x197 == this.m_inHeaders.HTTPResponseCode) && this.m_inHeaders["Proxy-Authenticate"].StartsWith("N", StringComparison.OrdinalIgnoreCase)))
                    {
                        this.m_inHeaders["Proxy-Support"] = "Session-Based-Authentication";
                    }
                    this.m_session.Timers.ClientBeginResponse = DateTime.Now;
                    this._bLeakedHeaders = true;
                    this.m_session.oRequest.pipeClient.Send(this.m_inHeaders.ToByteArray(true, true));
                    this._lngLeakedOffset = this.iEntityBodyOffset;
                }
                this.m_session.oRequest.pipeClient.Send(this.m_responseData.GetBuffer(), (int) this._lngLeakedOffset, (int) (this.m_responseData.Length - this._lngLeakedOffset));
                this._lngLeakedOffset = this.m_responseData.Length;
                return true;
            }
            catch (Exception exception)
            {
                this.m_session.PoisonClientPipe();
                if (FiddlerApplication.Prefs.GetBoolPref("fiddler.network.streaming.abortifclientaborts", false))
                {
                    throw new OperationCanceledException("Leaking response to client failed", exception);
                }
                FiddlerApplication.Log.LogFormat("fiddler.network.streaming> Streaming of response #{0} to client failed: {1}", new object[] { this.m_session.id, exception.Message });
                return false;
            }
        }

        private bool ParseResponseForHeaders()
        {
            if ((this.m_responseData != null) && (this.iEntityBodyOffset >= 4))
            {
                this.m_inHeaders = new HTTPResponseHeaders(CONFIG.oHeaderEncoding);
                byte[] bytes = this.m_responseData.GetBuffer();
                string str = CONFIG.oHeaderEncoding.GetString(bytes, 0, this.iEntityBodyOffset).Trim();
                if ((str == null) || (str.Length < 1))
                {
                    this.m_inHeaders = null;
                    return false;
                }
                string[] sHeaderLines = str.Replace("\r\n", "\n").Split(new char[] { '\n' });
                if (sHeaderLines.Length >= 1)
                {
                    int index = sHeaderLines[0].IndexOf(' ');
                    if (index > 0)
                    {
                        this.m_inHeaders.HTTPVersion = sHeaderLines[0].Substring(0, index).ToUpper();
                        sHeaderLines[0] = sHeaderLines[0].Substring(index + 1).Trim();
                        if (!this.m_inHeaders.HTTPVersion.StartsWith("HTTP/", StringComparison.OrdinalIgnoreCase))
                        {
                            FiddlerApplication.HandleHTTPError(this.m_session, SessionFlags.ProtocolViolationInResponse, false, true, "Response does not start with HTTP. Data:\n\n\t" + sHeaderLines[0]);
                            return false;
                        }
                        this.m_inHeaders.HTTPResponseStatus = sHeaderLines[0];
                        bool flag = false;
                        index = sHeaderLines[0].IndexOf(' ');
                        if (index > 0)
                        {
                            flag = int.TryParse(sHeaderLines[0].Substring(0, index).Trim(), NumberStyles.Integer, NumberFormatInfo.InvariantInfo, out this.m_inHeaders.HTTPResponseCode);
                        }
                        else
                        {
                            flag = int.TryParse(sHeaderLines[0].Trim(), NumberStyles.Integer, NumberFormatInfo.InvariantInfo, out this.m_inHeaders.HTTPResponseCode);
                        }
                        if (!flag)
                        {
                            FiddlerApplication.HandleHTTPError(this.m_session, SessionFlags.ProtocolViolationInResponse, false, true, "Response headers did not contain a status code. Data:\n\n\t" + sHeaderLines[0]);
                            return false;
                        }
                        string sErrors = string.Empty;
                        if (!Parser.ParseNVPHeaders(this.m_inHeaders, sHeaderLines, 1, ref sErrors))
                        {
                            FiddlerApplication.HandleHTTPError(this.m_session, SessionFlags.ProtocolViolationInResponse, false, true, "Incorrectly formed response headers.\n" + sErrors);
                        }
                        return true;
                    }
                    FiddlerApplication.HandleHTTPError(this.m_session, SessionFlags.ProtocolViolationInResponse, false, true, "Cannot parse HTTP response; Status line contains no spaces. Data:\n\n\t" + sHeaderLines[0]);
                }
            }
            return false;
        }

        internal bool ReadResponse()
        {
            int iMaxByteCount = 0;
            bool flag = false;
            bool flag2 = false;
            bool flag3 = false;
            byte[] arrBuffer = new byte[_cbServerReadBuffer];
            do
            {
                try
                {
                    iMaxByteCount = this.pipeServer.Receive(arrBuffer);
                    if (0L == this.m_session.Timers.ServerBeginResponse.Ticks)
                    {
                        this.m_session.Timers.ServerBeginResponse = DateTime.Now;
                    }
                    if (iMaxByteCount <= 0)
                    {
                        flag = true;
                    }
                    else
                    {
                        if (CONFIG.bDebugSpew)
                        {
                            FiddlerApplication.DebugSpew(Utilities.ByteArrayToHexView(arrBuffer, 0x20, iMaxByteCount));
                        }
                        this.m_responseData.Write(arrBuffer, 0, iMaxByteCount);
                        this.m_responseTotalDataCount += iMaxByteCount;
                        if ((this.m_inHeaders == null) && this.GetHeaders())
                        {
                            if ((this.m_session.state == SessionStates.Aborted) && this.m_session.isAnyFlagSet(SessionFlags.ProtocolViolationInResponse))
                            {
                                return false;
                            }
                            FiddlerApplication.DoResponseHeadersAvailable(this.m_session);
                            if (CONFIG.bStreamAudioVideo)
                            {
                                string str = this.m_inHeaders["Content-Type"];
                                if ((str.StartsWith("video/", StringComparison.OrdinalIgnoreCase) || str.StartsWith("audio/", StringComparison.OrdinalIgnoreCase)) || str.StartsWith("application/x-mms-framed", StringComparison.OrdinalIgnoreCase))
                                {
                                    this.m_session.bBufferResponse = false;
                                }
                            }
                            if (!this.m_session.bBufferResponse)
                            {
                                this.m_session.bBufferResponse = this.m_session.HTTPMethodIs("CONNECT");
                            }
                            if (!this.m_session.bBufferResponse && (this.m_session.oRequest.pipeClient == null))
                            {
                                this.m_session.bBufferResponse = true;
                            }
                            if ((!this.m_session.bBufferResponse && ((0x191 == this.m_inHeaders.HTTPResponseCode) || (0x197 == this.m_inHeaders.HTTPResponseCode))) && this.m_session.oFlags.ContainsKey("x-AutoAuth"))
                            {
                                this.m_session.bBufferResponse = true;
                            }
                            this.m_session.ExecuteBasicResponseManipulationsUsingHeadersOnly();
                            this.m_session.SetBitFlag(SessionFlags.ResponseStreamed, !this.m_session.bBufferResponse);
                            if (!this.m_session.bBufferResponse)
                            {
                                if (this.m_session.oFlags.ContainsKey("response-trickle-delay"))
                                {
                                    int num2 = int.Parse(this.m_session.oFlags["response-trickle-delay"]);
                                    this.m_session.oRequest.pipeClient.TransmitDelay = num2;
                                }
                                if (this.m_session.oFlags.ContainsKey("log-drop-response-body") || FiddlerApplication.Prefs.GetBoolPref("fiddler.network.streaming.ForgetStreamedData", false))
                                {
                                    flag3 = true;
                                }
                            }
                        }
                        if ((this.m_inHeaders != null) && this.m_session.isFlagSet(SessionFlags.ResponseStreamed))
                        {
                            this.LeakResponseBytes();
                            if (flag3)
                            {
                                this.m_session.SetBitFlag(SessionFlags.ResponseBodyDropped, true);
                                if (this._lngLastChunkInfoOffset > -1L)
                                {
                                    this.ReleaseStreamedChunkedData();
                                }
                                else if (this.m_inHeaders.ExistsAndContains("Transfer-Encoding", "chunked"))
                                {
                                    this.ReleaseStreamedChunkedData();
                                }
                                else
                                {
                                    this.ReleaseStreamedData();
                                }
                            }
                        }
                    }
                }
                catch (Exception exception)
                {
                    flag2 = true;
                    if (exception is OperationCanceledException)
                    {
                        this.m_session.state = SessionStates.Aborted;
                        FiddlerApplication.Log.LogFormat("fiddler.network.readresponse.failure> Session #{0} was aborted {1}", new object[] { this.m_session.id, exception.Message });
                    }
                    else if (exception is OutOfMemoryException)
                    {
                        FiddlerApplication.ReportException(exception);
                        this.m_session.state = SessionStates.Aborted;
                        FiddlerApplication.Log.LogFormat("fiddler.network.readresponse.failure> Session #{0} Out of Memory", new object[] { this.m_session.id });
                    }
                    else
                    {
                        FiddlerApplication.Log.LogFormat("fiddler.network.readresponse.failure> Session #{0} raised exception {1}", new object[] { this.m_session.id, exception.Message });
                    }
                }
            }
            while ((!flag && !flag2) && ((this.m_inHeaders == null) || !this.isResponseBodyComplete()));
            this.m_session.Timers.ServerDoneResponse = DateTime.Now;
            if (this.m_session.isFlagSet(SessionFlags.ResponseStreamed))
            {
                this.m_session.Timers.ClientDoneResponse = this.m_session.Timers.ServerDoneResponse;
            }
            if ((this.m_responseTotalDataCount == 0L) && (this.m_inHeaders == null))
            {
                flag2 = true;
            }
            arrBuffer = null;
            if (flag2)
            {
                this.m_responseData.Dispose();
                this.m_responseData = null;
                return false;
            }
            if (this.m_inHeaders == null)
            {
                FiddlerApplication.HandleHTTPError(this.m_session, SessionFlags.ProtocolViolationInResponse, true, true, "The Server did not return properly formatted HTTP Headers. Maybe missing altogether (e.g. HTTP/0.9), maybe only \\r\\r instead of \\r\\n\\r\\n?\n");
                this.m_session.SetBitFlag(SessionFlags.ResponseStreamed, false);
                this.m_inHeaders = new HTTPResponseHeaders(CONFIG.oHeaderEncoding);
                this.m_inHeaders.HTTPVersion = "HTTP/1.0";
                this.m_inHeaders.HTTPResponseCode = 200;
                this.m_inHeaders.HTTPResponseStatus = "200 This buggy server did not return headers";
                this.iEntityBodyOffset = 0;
                return true;
            }
            return true;
        }

        internal bool ReadResponseFromFile(string sFilename)
        {
            string str2;
            if (System.IO.File.Exists(sFilename))
            {
                FileStream oStream = System.IO.File.OpenRead(sFilename);
                byte[] arrBytes = new byte[oStream.Length];
                Utilities.ReadEntireStream(oStream, arrBytes);
                oStream.Close();
                this.Initialize(true);
                int length = arrBytes.Length;
                int index = 0;
                bool flag = (((arrBytes.Length > 3) && (arrBytes[0] == 0xef)) && (arrBytes[1] == 0xbb)) && (arrBytes[2] == 0xbf);
                if (flag)
                {
                    index = 3;
                    length -= 3;
                }
                bool flag2 = ((((arrBytes.Length > (5 + index)) && (arrBytes[index] == 0x48)) && ((arrBytes[index + 1] == 0x54) && (arrBytes[index + 2] == 0x54))) && (arrBytes[index + 3] == 80)) && (arrBytes[index + 4] == 0x2f);
                if (flag && !flag2)
                {
                    length += 3;
                    index = 0;
                }
                this.m_responseData.Write(arrBytes, index, length);
                if ((flag2 && this.HeadersAvailable()) && this.ParseResponseForHeaders())
                {
                    this.m_session.responseBodyBytes = this.TakeEntity();
                }
                else
                {
                    this.Initialize(false);
                    this.m_inHeaders = new HTTPResponseHeaders(CONFIG.oHeaderEncoding);
                    this.m_inHeaders.HTTPResponseCode = 200;
                    this.m_inHeaders.HTTPResponseStatus = "200 OK with automatic headers";
                    this.m_inHeaders["Content-Length"] = arrBytes.LongLength.ToString();
                    this.m_inHeaders["Cache-Control"] = "max-age=0, must-revalidate";
                    string str = Utilities.ContentTypeForFileExtension(Path.GetExtension(sFilename));
                    if (str != null)
                    {
                        this.m_inHeaders["Content-Type"] = str;
                    }
                    this.m_session.responseBodyBytes = arrBytes;
                }
                return true;
            }
            this.Initialize(false);
            if ((this.m_session.LocalProcessID > 0) || this.m_session.isFlagSet(SessionFlags.RequestGeneratedByFiddler))
            {
                str2 = "Fiddler - The file '" + sFilename + "' was not found.";
            }
            else
            {
                str2 = "Fiddler - The requested file was not found.";
            }
            str2 = str2.PadRight(0x200, ' ');
            this.m_session.responseBodyBytes = Encoding.UTF8.GetBytes(str2);
            this.m_inHeaders = new HTTPResponseHeaders(CONFIG.oHeaderEncoding);
            this.m_inHeaders.HTTPResponseCode = 0x194;
            this.m_inHeaders.HTTPResponseStatus = "404 Not Found";
            this.m_inHeaders.Add("Content-Length", this.m_session.responseBodyBytes.Length.ToString());
            this.m_inHeaders.Add("Cache-Control", "max-age=0, must-revalidate");
            return false;
        }

        internal void releaseServerPipe()
        {
            if (this.pipeServer != null)
            {
                if (((this.headers.ExistsAndEquals("Connection", "close") || this.headers.ExistsAndEquals("Proxy-Connection", "close")) || ((this.headers.HTTPVersion != "HTTP/1.1") && !this.headers.ExistsAndContains("Connection", "Keep-Alive"))) || !this.pipeServer.Connected)
                {
                    this.pipeServer.ReusePolicy = PipeReusePolicy.NoReuse;
                }
                this._detachServerPipe();
            }
        }

        private void ReleaseStreamedChunkedData()
        {
            long num;
            if (this.iEntityBodyOffset > this._lngLastChunkInfoOffset)
            {
                this._lngLastChunkInfoOffset = this.iEntityBodyOffset;
            }
            Utilities.IsChunkedBodyComplete(this.m_session, this.m_responseData, this._lngLastChunkInfoOffset, out this._lngLastChunkInfoOffset, out num);
            int capacity = (int) (this.m_responseData.Length - this._lngLastChunkInfoOffset);
            MemoryStream stream = new MemoryStream(capacity);
            stream.Write(this.m_responseData.GetBuffer(), (int) this._lngLastChunkInfoOffset, capacity);
            this.m_responseData = stream;
            this._lngLeakedOffset = capacity;
            this._lngLastChunkInfoOffset = 0L;
            this.iEntityBodyOffset = 0;
        }

        private void ReleaseStreamedData()
        {
            this.m_responseData = new MemoryStream();
            this._lngLeakedOffset = 0L;
            if (this.iEntityBodyOffset > 0)
            {
                this.m_responseTotalDataCount -= this.iEntityBodyOffset;
                this.iEntityBodyOffset = 0;
            }
        }

        internal bool ResendRequest()
        {
            bool b = this.pipeServer != null;
            if (!this.ConnectToHost())
            {
                FiddlerApplication.DebugSpew("ConnectToHost returned null. Bailing...");
                this.m_session.SetBitFlag(SessionFlags.ServerPipeReused, b);
                return false;
            }
            try
            {
                this.pipeServer.IncrementUse(this.m_session.id);
                this.m_session.Timers.ServerConnected = this.pipeServer.dtConnected;
                this._bWasForwarded = this.pipeServer.isConnectedToGateway;
                this.m_session.SetBitFlag(SessionFlags.ServerPipeReused, this.pipeServer.iUseCount > 1);
                this.m_session.SetBitFlag(SessionFlags.SentToGateway, this._bWasForwarded);
                if (!this._bWasForwarded && !this.m_session.isHTTPS)
                {
                    this.m_session.oRequest.headers.RenameHeaderItems("Proxy-Connection", "Connection");
                }
                if (!this.pipeServer.isAuthenticated)
                {
                    string str = this.m_session.oRequest.headers["Authorization"];
                    if ((str != null) && str.StartsWith("N"))
                    {
                        this.pipeServer.MarkAsAuthenticated(this.m_session.LocalProcessID);
                    }
                }
                this.m_session.Timers.FiddlerBeginRequest = DateTime.Now;
                if (this.m_session.oFlags.ContainsKey("request-trickle-delay"))
                {
                    int num = int.Parse(this.m_session.oFlags["request-trickle-delay"]);
                    this.pipeServer.TransmitDelay = num;
                }
                this.pipeServer.Send(this.m_session.oRequest.headers.ToByteArray(true, true, this._bWasForwarded && !this.m_session.isHTTPS));
                this.pipeServer.Send(this.m_session.requestBodyBytes);
            }
            catch (Exception exception)
            {
                if (this.bServerSocketReused && (this.m_session.state != SessionStates.Aborted))
                {
                    this.pipeServer = null;
                    return this.ResendRequest();
                }
                FiddlerApplication.DebugSpew("ResendRequest() failed: " + exception.Message);
                this.m_session.oRequest.FailSession(0x1f8, "Fiddler - Send Failure", "ResendRequest() failed: " + exception.Message);
                return false;
            }
            this.m_session.oFlags["x-EgressPort"] = this.pipeServer.LocalPort.ToString();
            if (this.m_session.oFlags.ContainsKey("log-drop-request-body"))
            {
                this.m_session.oFlags["x-RequestBodyLength"] = (this.m_session.requestBodyBytes != null) ? this.m_session.requestBodyBytes.Length.ToString() : "0";
                this.m_session.requestBodyBytes = new byte[0];
            }
            return true;
        }

        private bool SIDsMatch(int iPID, string sIDSession, string sIDPipe)
        {
            return (string.Equals(sIDSession, sIDPipe, StringComparison.Ordinal) || ((iPID != 0) && string.Equals(string.Format("PID{0}*{1}", iPID, sIDSession), sIDPipe, StringComparison.Ordinal)));
        }

        internal byte[] TakeEntity()
        {
            byte[] bytes;
            try
            {
                bytes = new byte[this.m_responseData.Length - this.iEntityBodyOffset];
                this.m_responseData.Position = this.iEntityBodyOffset;
                this.m_responseData.Read(bytes, 0, bytes.Length);
            }
            catch (OutOfMemoryException exception)
            {
                FiddlerApplication.ReportException(exception, "HTTP Response Too Large");
                bytes = Encoding.ASCII.GetBytes("Fiddler: Out of memory");
                this.m_session.PoisonServerPipe();
            }
            this.FreeResponseDataBuffer();
            return bytes;
        }

        internal long _PeekDownloadProgress
        {
            get
            {
                if (this.m_responseData != null)
                {
                    return this.m_responseTotalDataCount;
                }
                return -1L;
            }
        }

        public bool bServerSocketReused
        {
            get
            {
                return this.m_session.isFlagSet(SessionFlags.ServerPipeReused);
            }
        }

        public bool bWasForwarded
        {
            get
            {
                return this._bWasForwarded;
            }
        }

        public HTTPResponseHeaders headers
        {
            get
            {
                return this.m_inHeaders;
            }
            set
            {
                if (value != null)
                {
                    this.m_inHeaders = value;
                }
            }
        }

        public string this[string sHeader]
        {
            get
            {
                if (this.m_inHeaders != null)
                {
                    return this.m_inHeaders[sHeader];
                }
                return string.Empty;
            }
            set
            {
                if (this.m_inHeaders == null)
                {
                    throw new InvalidDataException("Response Headers object does not exist");
                }
                this.m_inHeaders[sHeader] = value;
            }
        }

        public int iTTFB
        {
            get
            {
                TimeSpan span = (TimeSpan) (this.m_session.Timers.ServerBeginResponse - this.m_session.Timers.FiddlerBeginRequest);
                int totalMilliseconds = (int) span.TotalMilliseconds;
                if (totalMilliseconds <= 0)
                {
                    return 0;
                }
                return totalMilliseconds;
            }
        }

        public int iTTLB
        {
            get
            {
                TimeSpan span = (TimeSpan) (this.m_session.Timers.ServerDoneResponse - this.m_session.Timers.FiddlerBeginRequest);
                int totalMilliseconds = (int) span.TotalMilliseconds;
                if (totalMilliseconds <= 0)
                {
                    return 0;
                }
                return totalMilliseconds;
            }
        }

        public string MIMEType
        {
            get
            {
                if (this.headers == null)
                {
                    return string.Empty;
                }
                string sString = this.headers["Content-Type"];
                if (sString.Length > 0)
                {
                    sString = Utilities.TrimAfter(sString, ';').Trim();
                }
                return sString;
            }
        }
    }
}

