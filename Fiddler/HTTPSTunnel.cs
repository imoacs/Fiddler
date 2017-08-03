namespace Fiddler
{
    using System;
    using System.IO;
    using System.Net;
    using System.Net.Sockets;
    using System.Text;
    using System.Threading;

    internal class HTTPSTunnel
    {
        private Session _mySession;
        private byte[] arrRequestBytes = new byte[0x4000];
        private byte[] arrResponseBytes = new byte[0x4000];
        private AutoResetEvent oKeepTunnelAlive;
        private Socket socketClient;
        private Socket socketRemote;

        private HTTPSTunnel(Session oSess, Socket oFrom)
        {
            this._mySession = oSess;
            this.socketClient = oFrom;
            this._mySession.SetBitFlag(SessionFlags.IsBlindTunnel, true);
        }

        private void _indicateTunnelFailure(int iResponseCode, string sErr)
        {
            try
            {
                this._mySession.oResponse.headers = new HTTPResponseHeaders();
                this._mySession.oResponse.headers.HTTPVersion = this._mySession.oRequest.headers.HTTPVersion;
                this._mySession.oResponse.headers.HTTPResponseCode = iResponseCode;
                this._mySession.oResponse.headers.HTTPResponseStatus = (iResponseCode == 0x1f6) ? "502 Gateway Connection failure" : "504 Connection Failed";
                this._mySession.oResponse.headers.Add("Connection", "close");
                this._mySession.responseBodyBytes = Encoding.UTF8.GetBytes("[Fiddler] " + this._mySession.oResponse.headers.HTTPResponseStatus + ": " + sErr + "<BR>Timestamp: " + DateTime.Now.ToString("HH:mm:ss.fff"));
                FiddlerApplication.DoBeforeReturningError(this._mySession);
                FiddlerApplication._frmMain.BeginInvoke(new updateUIDelegate(FiddlerApplication._frmMain.finishSession), new object[] { this._mySession });
                this.socketClient.Send(this._mySession.oResponse.headers.ToByteArray(true, true));
                this.socketClient.Send(this._mySession.responseBodyBytes);
                this.socketClient.Shutdown(SocketShutdown.Both);
                if (this.socketRemote != null)
                {
                    this.socketRemote.Close();
                }
            }
            catch (Exception)
            {
            }
        }

        private void CloseTunnel()
        {
            try
            {
                if (this.socketClient != null)
                {
                    if (this.socketClient.Connected)
                    {
                        this.socketClient.Shutdown(SocketShutdown.Both);
                    }
                    this.socketClient.Close();
                }
            }
            catch (Exception)
            {
            }
            try
            {
                if (this.socketRemote != null)
                {
                    if (this.socketRemote.Connected)
                    {
                        this.socketRemote.Shutdown(SocketShutdown.Both);
                    }
                    this.socketRemote.Close();
                }
            }
            catch (Exception)
            {
            }
            try
            {
                if (this.oKeepTunnelAlive != null)
                {
                    this.oKeepTunnelAlive.Set();
                }
            }
            catch (Exception)
            {
            }
        }

        internal static void CreateTunnel(Session oSession)
        {
            if (((oSession != null) && (oSession.oRequest != null)) && (((oSession.oRequest.headers != null) && (oSession.oRequest.pipeClient != null)) && (oSession.oResponse != null)))
            {
                Socket rawSocket = oSession.oRequest.pipeClient.GetRawSocket();
                if (rawSocket != null)
                {
                    oSession.oRequest.pipeClient = null;
                    HTTPSTunnel tunnel = new HTTPSTunnel(oSession, rawSocket);
                    new Thread(new ThreadStart(tunnel.RunTunnel)) { IsBackground = true }.Start();
                }
            }
        }

        protected void OnClientReceive(IAsyncResult ar)
        {
            try
            {
                int count = this.socketClient.EndReceive(ar);
                if (count > 0)
                {
                    if ((this._mySession.requestBodyBytes == null) || (this._mySession.requestBodyBytes.LongLength == 0L))
                    {
                        try
                        {
                            HTTPSClientHello hello = new HTTPSClientHello();
                            if (hello.LoadFromStream(new MemoryStream(this.arrRequestBytes, 0, count, false)))
                            {
                                this._mySession.requestBodyBytes = Encoding.UTF8.GetBytes(hello.ToString() + "\n");
                                this._mySession["https-Client-SessionID"] = hello.SessionID;
                            }
                        }
                        catch (Exception exception)
                        {
                            this._mySession.requestBodyBytes = Encoding.UTF8.GetBytes("HTTPSParse Failed: " + exception.Message);
                        }
                    }
                    this.socketRemote.BeginSend(this.arrRequestBytes, 0, count, SocketFlags.None, new AsyncCallback(this.OnRemoteSent), this.socketRemote);
                }
                else
                {
                    this.CloseTunnel();
                }
            }
            catch (Exception)
            {
                this.CloseTunnel();
            }
        }

        protected void OnClientSent(IAsyncResult ar)
        {
            try
            {
                if (this.socketClient.EndSend(ar) > 0)
                {
                    this.socketRemote.BeginReceive(this.arrResponseBytes, 0, this.arrResponseBytes.Length, SocketFlags.None, new AsyncCallback(this.OnRemoteReceive), this.socketRemote);
                }
            }
            catch (Exception)
            {
            }
        }

        protected void OnRemoteReceive(IAsyncResult ar)
        {
            try
            {
                int count = this.socketRemote.EndReceive(ar);
                if (count > 0)
                {
                    if ((this._mySession.responseBodyBytes == null) || (this._mySession.responseBodyBytes.LongLength == 0L))
                    {
                        try
                        {
                            HTTPSServerHello hello = new HTTPSServerHello();
                            if (hello.LoadFromStream(new MemoryStream(this.arrResponseBytes, 0, count, false)))
                            {
                                this._mySession.responseBodyBytes = Encoding.UTF8.GetBytes("This is a CONNECT tunnel, through which encrypted HTTPS traffic flows. To view the encrypted sessions inside this tunnel, ensure that the Tools > Fiddler Options > HTTPS > Decrypt HTTPS traffic option is checked.\n\n" + hello.ToString() + "\n");
                                this._mySession["https-Server-SessionID"] = hello.SessionID;
                            }
                        }
                        catch (Exception exception)
                        {
                            this._mySession.requestBodyBytes = Encoding.UTF8.GetBytes("HTTPSParse Failed: " + exception.Message);
                        }
                    }
                    this.socketClient.BeginSend(this.arrResponseBytes, 0, count, SocketFlags.None, new AsyncCallback(this.OnClientSent), this.socketClient);
                }
                else
                {
                    this.CloseTunnel();
                }
            }
            catch (Exception)
            {
                this.CloseTunnel();
            }
        }

        protected void OnRemoteSent(IAsyncResult ar)
        {
            try
            {
                if (this.socketRemote.EndSend(ar) > 0)
                {
                    this.socketClient.BeginReceive(this.arrRequestBytes, 0, this.arrRequestBytes.Length, SocketFlags.None, new AsyncCallback(this.OnClientReceive), this.socketClient);
                }
            }
            catch (Exception)
            {
            }
        }

        private void RunTunnel()
        {
            if (FiddlerApplication.oProxy != null)
            {
                try
                {
                    IPEndPoint ipepForwardHTTPS = null;
                    if (this._mySession.oFlags["x-overrideGateway"] != null)
                    {
                        if (string.Equals("DIRECT", this._mySession.oFlags["x-overrideGateway"], StringComparison.OrdinalIgnoreCase))
                        {
                            this._mySession.bypassGateway = true;
                        }
                        else
                        {
                            ipepForwardHTTPS = Utilities.IPEndPointFromHostPortString(this._mySession.oFlags["x-overrideGateway"]);
                        }
                    }
                    else if (!this._mySession.bypassGateway)
                    {
                        int tickCount = Environment.TickCount;
                        ipepForwardHTTPS = FiddlerApplication.oProxy.FindGatewayForOrigin("https", this._mySession.oFlags["x-overrideHost"] ?? this._mySession.PathAndQuery);
                        this._mySession.Timers.GatewayDeterminationTime = Environment.TickCount - tickCount;
                    }
                    if (ipepForwardHTTPS != null)
                    {
                        this.TunnelToGateway(ipepForwardHTTPS);
                    }
                    else
                    {
                        this.TunnelDirectly();
                    }
                }
                catch (Exception exception)
                {
                    FiddlerApplication.ReportException(exception, "Uncaught Exception in Tunnel; Session #" + this._mySession.id.ToString());
                }
            }
        }

        private void TunnelDirectly()
        {
            string str;
            this._mySession.SetBitFlag(SessionFlags.SentToGateway, false);
            int iPort = 0x1bb;
            string sHostPort = this._mySession.oFlags["x-overrideHost"];
            if (sHostPort == null)
            {
                sHostPort = this._mySession.PathAndQuery;
            }
            Utilities.CrackHostAndPort(sHostPort, out str, ref iPort);
            try
            {
                IPAddress[] arrDestIPs = DNSResolver.GetIPAddressList(str, true, this._mySession.Timers);
                this.socketRemote = ServerChatter.CreateConnectedSocket(arrDestIPs, iPort, this._mySession);
            }
            catch (Exception exception)
            {
                this._indicateTunnelFailure(0x1f8, exception.Message);
                return;
            }
            try
            {
                this._mySession.oResponse.headers = new HTTPResponseHeaders();
                this._mySession.oResponse.headers.HTTPVersion = this._mySession.oRequest.headers.HTTPVersion;
                this._mySession.oResponse.headers.HTTPResponseCode = 200;
                this._mySession.oResponse.headers.HTTPResponseStatus = "200 Blind-Connection Established";
                this._mySession.oResponse.headers.Add("FiddlerGateway", "Direct");
                this._mySession.oResponse.headers.Add("StartTime", DateTime.Now.ToString("HH:mm:ss.fff"));
                this.socketClient.Send(this._mySession.oResponse.headers.ToByteArray(true, true));
                this._mySession.oFlags["x-EgressPort"] = (this.socketRemote.LocalEndPoint as IPEndPoint).Port.ToString();
                this.socketClient.BeginReceive(this.arrRequestBytes, 0, this.arrRequestBytes.Length, SocketFlags.None, new AsyncCallback(this.OnClientReceive), this.socketClient);
                this.socketRemote.BeginReceive(this.arrResponseBytes, 0, this.arrResponseBytes.Length, SocketFlags.None, new AsyncCallback(this.OnRemoteReceive), this.socketRemote);
                FiddlerApplication._frmMain.BeginInvoke(new updateUIDelegate(FiddlerApplication._frmMain.finishSession), new object[] { this._mySession });
                this.WaitForCompletion();
            }
            catch (Exception)
            {
                try
                {
                    this.socketRemote.Close();
                    this.socketClient.Close();
                }
                catch (Exception)
                {
                }
            }
        }

        private void TunnelToGateway(IPEndPoint ipepForwardHTTPS)
        {
            try
            {
                this._mySession.oResponse._bWasForwarded = true;
                this._mySession.SetBitFlag(SessionFlags.SentToGateway, true);
                IPAddress[] arrDestIPs = new IPAddress[] { ipepForwardHTTPS.Address };
                this.socketRemote = ServerChatter.CreateConnectedSocket(arrDestIPs, ipepForwardHTTPS.Port, this._mySession);
                this.socketRemote.Send(this._mySession.oRequest.headers.ToByteArray(true, true, false));
                this._mySession.oFlags["x-EgressPort"] = (this.socketRemote.LocalEndPoint as IPEndPoint).Port.ToString();
                this.socketClient.BeginReceive(this.arrRequestBytes, 0, this.arrRequestBytes.Length, SocketFlags.None, new AsyncCallback(this.OnClientReceive), this.socketClient);
                this.socketRemote.BeginReceive(this.arrResponseBytes, 0, this.arrResponseBytes.Length, SocketFlags.None, new AsyncCallback(this.OnRemoteReceive), this.socketRemote);
                this._mySession.oResponse.headers = new HTTPResponseHeaders();
                this._mySession.oResponse.headers.HTTPResponseCode = 0;
                this._mySession.oResponse.headers.HTTPResponseStatus = "0 Connection passed to Gateway - Result unknown";
                FiddlerApplication._frmMain.BeginInvoke(new updateUIDelegate(FiddlerApplication._frmMain.finishSession), new object[] { this._mySession });
                this.WaitForCompletion();
            }
            catch (Exception exception)
            {
                this._indicateTunnelFailure(0x1f6, exception.Message);
            }
        }

        private void WaitForCompletion()
        {
            AutoResetEvent oKeepTunnelAlive = this.oKeepTunnelAlive;
            this.oKeepTunnelAlive = new AutoResetEvent(false);
            this.oKeepTunnelAlive.WaitOne();
            this.oKeepTunnelAlive.Close();
            this.oKeepTunnelAlive = null;
            this.arrRequestBytes = (byte[]) (this.arrResponseBytes = null);
            this.socketClient = (Socket) (this.socketRemote = null);
            if ((this._mySession.oResponse != null) && (this._mySession.oResponse.headers != null))
            {
                this._mySession.oResponse.headers.Add("EndTime", DateTime.Now.ToString("HH:mm:ss.fff"));
            }
            this._mySession.Timers.ServerDoneResponse = this._mySession.Timers.ClientBeginResponse = this._mySession.Timers.ClientDoneResponse = DateTime.Now;
            this._mySession = null;
        }
    }
}

