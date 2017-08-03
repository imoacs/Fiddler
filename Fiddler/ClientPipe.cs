namespace Fiddler
{
    using System;
    using System.Net;
    using System.Net.Security;
    using System.Net.Sockets;
    using System.Security.Cryptography.X509Certificates;

    public class ClientPipe : BasePipe
    {
        private byte[] _arrReceivedAndPutBack;
        private static bool _bWantClientCert = FiddlerApplication.Prefs.GetBoolPref("fiddler.network.https.requestclientcertificate", false);
        private int _iProcessID;
        private string _sProcessName;
        internal static int _timeoutReceiveInitial = FiddlerApplication.Prefs.GetInt32Pref("fiddler.network.timeouts.clientpipe.receive.initial", 0xea60);
        internal static int _timeoutReceiveReused = FiddlerApplication.Prefs.GetInt32Pref("fiddler.network.timeouts.clientpipe.receive.reuse", 0x7530);

        internal ClientPipe(Socket oSocket) : base(oSocket, "C")
        {
            try
            {
                oSocket.NoDelay = true;
                if (CONFIG.bMapSocketToProcess)
                {
                    this._iProcessID = Winsock.MapLocalPortToProcessId(((IPEndPoint) oSocket.RemoteEndPoint).Port);
                    if (this._iProcessID > 0)
                    {
                        this._sProcessName = ProcessHelper.GetProcessName(this._iProcessID);
                    }
                }
            }
            catch
            {
            }
        }

        internal void putBackSomeBytes(byte[] toPutback)
        {
            this._arrReceivedAndPutBack = new byte[toPutback.Length];
            Buffer.BlockCopy(toPutback, 0, this._arrReceivedAndPutBack, 0, toPutback.Length);
        }

        internal int Receive(byte[] arrBuffer)
        {
            if (this._arrReceivedAndPutBack == null)
            {
                return base.Receive(arrBuffer);
            }
            int length = this._arrReceivedAndPutBack.Length;
            Buffer.BlockCopy(this._arrReceivedAndPutBack, 0, arrBuffer, 0, length);
            this._arrReceivedAndPutBack = null;
            return length;
        }

        internal bool SecureClientPipe(string sHostname, HTTPResponseHeaders oHeaders)
        {
            X509Certificate2 certificate;
            try
            {
                certificate = CertMaker.FindCert(sHostname, true);
            }
            catch (Exception exception)
            {
                FiddlerApplication.Log.LogFormat("fiddler.https> Failed to obtain certificate for {0} due to {1}", new object[] { sHostname, exception.Message });
                certificate = null;
            }
            try
            {
                if (certificate == null)
                {
                    FiddlerApplication.DoNotifyUser("Unable to find Certificate for " + sHostname, "HTTPS Interception Failure");
                    oHeaders.HTTPResponseCode = 0x1f6;
                    oHeaders.HTTPResponseStatus = "502 Fiddler unable to generate certificate";
                }
                if (CONFIG.bDebugSpew)
                {
                    FiddlerApplication.DebugSpew("SecureClientPipe for: " + this.ToString() + " sending data to client:\n" + Utilities.ByteArrayToHexView(oHeaders.ToByteArray(true, true), 0x20));
                }
                base.Send(oHeaders.ToByteArray(true, true));
                if (oHeaders.HTTPResponseCode != 200)
                {
                    FiddlerApplication.DebugSpew("SecureClientPipe returning FALSE because HTTPResponseCode != 200");
                    return false;
                }
                base._httpsStream = new SslStream(new NetworkStream(base._baseSocket, false), false);
                base._httpsStream.AuthenticateAsServer(certificate, _bWantClientCert, CONFIG.oAcceptedClientHTTPSProtocols, false);
                return true;
            }
            catch (Exception exception2)
            {
                FiddlerApplication.Log.LogFormat("Secure client pipe failed: {0}{1}.", new object[] { exception2.Message, (exception2.InnerException == null) ? string.Empty : (" InnerException: " + exception2.InnerException.Message) });
                FiddlerApplication.DebugSpew("Secure client pipe failed: " + exception2.Message);
                try
                {
                    base.End();
                }
                catch (Exception)
                {
                }
            }
            return false;
        }

        internal bool SecureClientPipeDirect(X509Certificate2 certServer)
        {
            try
            {
                base._httpsStream = new SslStream(new NetworkStream(base._baseSocket, false), false);
                base._httpsStream.AuthenticateAsServer(certServer, _bWantClientCert, CONFIG.oAcceptedClientHTTPSProtocols, false);
                return true;
            }
            catch (Exception exception)
            {
                FiddlerApplication.Log.LogFormat("Secure client pipe failed: {0}{1}.", new object[] { exception.Message, (exception.InnerException == null) ? string.Empty : (" InnerException: " + exception.InnerException.Message) });
                FiddlerApplication.DebugSpew("Secure client pipe failed: " + exception.Message);
                try
                {
                    base.End();
                }
                catch (Exception)
                {
                }
            }
            return false;
        }

        internal void setReceiveTimeout()
        {
            try
            {
                base._baseSocket.ReceiveTimeout = (base.iUseCount < 2) ? _timeoutReceiveInitial : _timeoutReceiveReused;
            }
            catch
            {
            }
        }

        public override string ToString()
        {
            return string.Format("[ClientPipe: {0}:{1}; UseCnt: {2}; Port: {3}; {4}]", new object[] { this._sProcessName, this._iProcessID, base.iUseCount, base.Port, base.bIsSecured ? "SECURE" : "PLAINTTEXT" });
        }

        public int LocalProcessID
        {
            get
            {
                return this._iProcessID;
            }
        }

        public string LocalProcessName
        {
            get
            {
                return (this._sProcessName ?? string.Empty);
            }
        }
    }
}

