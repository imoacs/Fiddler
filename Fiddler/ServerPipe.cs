namespace Fiddler
{
    using System;
    using System.Collections.Specialized;
    using System.Diagnostics;
    using System.IO;
    using System.Net;
    using System.Net.Security;
    using System.Net.Sockets;
    using System.Security.Cryptography.X509Certificates;
    using System.Text;
    using System.Windows.Forms;

    public class ServerPipe : BasePipe
    {
        protected bool _bIsConnectedToGateway;
        private int _iMarriedToPID;
        private bool _isAuthenticated;
        private PipeReusePolicy _reusePolicy;
        protected string _sPoolKey;
        internal DateTime dtConnected;
        internal int iLastPooled;
        private static StringCollection slAcceptableBadCertificates;

        internal ServerPipe(string sName, bool WillConnectToGateway) : base(null, sName)
        {
            this._bIsConnectedToGateway = WillConnectToGateway;
        }

        private X509Certificate _GetDefaultCertificate()
        {
            if (FiddlerApplication.oDefaultClientCertificate != null)
            {
                return FiddlerApplication.oDefaultClientCertificate;
            }
            X509Certificate certificate = null;
            if (System.IO.File.Exists(CONFIG.GetPath("DefaultClientCertificate")))
            {
                certificate = X509Certificate.CreateFromCertFile(CONFIG.GetPath("DefaultClientCertificate"));
                if ((certificate != null) && FiddlerApplication.Prefs.GetBoolPref("fiddler.network.https.cacheclientcert", true))
                {
                    FiddlerApplication.oDefaultClientCertificate = certificate;
                }
            }
            return certificate;
        }

        private X509Certificate AttachClientCertificate(Session oS, object sender, string targetHost, X509CertificateCollection localCertificates, X509Certificate remoteCertificate, string[] acceptableIssuers)
        {
            if (localCertificates.Count > 0)
            {
                this.MarkAsAuthenticated(oS.LocalProcessID);
                oS.oFlags["x-client-cert"] = localCertificates[0].Subject + " Serial#" + localCertificates[0].GetSerialNumberString();
                return localCertificates[0];
            }
            if ((remoteCertificate != null) || (acceptableIssuers.Length >= 1))
            {
                X509Certificate certificate = this._GetDefaultCertificate();
                if (certificate != null)
                {
                    this.MarkAsAuthenticated(oS.LocalProcessID);
                    oS.oFlags["x-client-cert"] = certificate.Subject + " Serial#" + certificate.GetSerialNumberString();
                    return certificate;
                }
                if (CONFIG.bShowDefaultClientCertificateNeededPrompt && FiddlerApplication.Prefs.GetBoolPref("fiddler.network.https.clientcertificate.ephemeral.prompt-for-missing", true))
                {
                    FiddlerApplication.DoNotifyUser("The server [" + targetHost + "] requests a client certificate.\nPlease save a client certificate in the following location:\n\n" + CONFIG.GetPath("DefaultClientCertificate"), "Client Certificate Requested");
                    FiddlerApplication.Prefs.SetBoolPref("fiddler.network.https.clientcertificate.ephemeral.prompt-for-missing", false);
                }
                FiddlerApplication.Log.LogFormat("The server [{0}] requested a client certificate, but no client certificate was available.", new object[] { targetHost });
            }
            return null;
        }

        private static bool ConfirmServerCertificate(Session oS, string sExpectedCN, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors)
        {
            bool flag;
            if (FiddlerApplication.CheckOverrideCertificatePolicy(oS, sExpectedCN, certificate, chain, sslPolicyErrors, out flag))
            {
                return flag;
            }
            if ((sslPolicyErrors == SslPolicyErrors.None) || CONFIG.IgnoreServerCertErrors)
            {
                return true;
            }
            if ((slAcceptableBadCertificates != null) && slAcceptableBadCertificates.Contains(certificate.GetSerialNumberString()))
            {
                return true;
            }
            if (CONFIG.QuietMode)
            {
                return false;
            }
            frmAlert alert = new frmAlert("Ignore remote certificate error?", string.Format("Session #{5}: The remote server ({0}) presented a certificate that did not validate, due to {1}.\r\n\r\nSUBJECT: {2}\r\nISSUER: {3}\r\nEXPIRES: {4}\r\n\r\n(This warning can be disabled by clicking Tools | Fiddler Options.)", new object[] { sExpectedCN, sslPolicyErrors, certificate.Subject, certificate.Issuer, certificate.GetExpirationDateString(), oS.id }), "Ignore errors and proceed anyway?", MessageBoxButtons.YesNo, MessageBoxDefaultButton.Button2) {
                TopMost = true,
                StartPosition = FormStartPosition.CenterScreen
            };
            DialogResult result = (DialogResult) FiddlerApplication._frmMain.Invoke(new getDecisionDelegate(FiddlerApplication._frmMain.GetDecision), new object[] { alert });
            if (DialogResult.Yes == result)
            {
                if (slAcceptableBadCertificates == null)
                {
                    slAcceptableBadCertificates = new StringCollection();
                }
                slAcceptableBadCertificates.Add(certificate.GetSerialNumberString());
            }
            return (DialogResult.Yes == result);
        }

        public string DescribeConnectionSecurity()
        {
            if (base._httpsStream == null)
            {
                return "No connection security";
            }
            string str = string.Empty;
            if (base._httpsStream.IsMutuallyAuthenticated)
            {
                str = "== Client Certificate ==========\nUnknown.\n";
            }
            if (base._httpsStream.LocalCertificate != null)
            {
                str = "\n== Client Certificate ==========\n" + base._httpsStream.LocalCertificate.ToString(true) + "\n";
            }
            return ("Secure Protocol: " + base._httpsStream.SslProtocol.ToString() + "\nCipher: " + base._httpsStream.CipherAlgorithm.ToString() + " " + base._httpsStream.CipherStrength.ToString() + "bits\nHash Algorithm: " + base._httpsStream.HashAlgorithm.ToString() + " " + base._httpsStream.HashStrength.ToString() + "bits\nKey Exchange: " + base._httpsStream.KeyExchangeAlgorithm.ToString() + " " + base._httpsStream.KeyExchangeStrength.ToString() + "bits\n" + str + "\n== Server Certificate ==========\n" + base._httpsStream.RemoteCertificate.ToString(true) + "\n");
        }

        internal X509CertificateCollection GetCertificateCollectionFromFile(string sClientCertificateFilename)
        {
            X509CertificateCollection certificates = null;
            if (!string.IsNullOrEmpty(sClientCertificateFilename))
            {
                try
                {
                    if (!Path.IsPathRooted(sClientCertificateFilename))
                    {
                        sClientCertificateFilename = CONFIG.GetPath("Root") + sClientCertificateFilename;
                    }
                }
                catch (Exception)
                {
                }
                if (System.IO.File.Exists(sClientCertificateFilename))
                {
                    certificates = new X509CertificateCollection();
                    certificates.Add(X509Certificate.CreateFromCertFile(sClientCertificateFilename));
                }
            }
            return certificates;
        }

        internal void MarkAsAuthenticated(int clientPID)
        {
            this._isAuthenticated = true;
            int num = FiddlerApplication.Prefs.GetInt32Pref("fiddler.network.auth.reusemode", 0);
            if ((num == 0) && (clientPID == 0))
            {
                num = 1;
            }
            if (num == 0)
            {
                this.ReusePolicy = PipeReusePolicy.MarriedToClientProcess;
                this._iMarriedToPID = clientPID;
                this.sPoolKey = string.Format("PID{0}*{1}", clientPID, this.sPoolKey);
            }
            else if (num == 1)
            {
                this.ReusePolicy = PipeReusePolicy.MarriedToClientPipe;
            }
        }

        internal bool SecureExistingConnection(Session oS, string sCertCN, string sClientCertificateFilename, string sPoolingKey, ref int iHandshakeTime)
        {
            RemoteCertificateValidationCallback userCertificateValidationCallback = null;
            LocalCertificateSelectionCallback userCertificateSelectionCallback = null;
            Stopwatch stopwatch = Stopwatch.StartNew();
            try
            {
                this.sPoolKey = sPoolingKey;
                X509CertificateCollection certificateCollectionFromFile = this.GetCertificateCollectionFromFile(sClientCertificateFilename);
                if (userCertificateValidationCallback == null)
                {
                    userCertificateValidationCallback = (sender, certificate, chain, sslPolicyErrors) => ConfirmServerCertificate(oS, sCertCN, certificate, chain, sslPolicyErrors);
                }
                if (userCertificateSelectionCallback == null)
                {
                    userCertificateSelectionCallback = (sender, targetHost, localCertificates, remoteCertificate, acceptableIssuers) => this.AttachClientCertificate(oS, sender, targetHost, localCertificates, remoteCertificate, acceptableIssuers);
                }
                base._httpsStream = new SslStream(new NetworkStream(base._baseSocket, false), false, userCertificateValidationCallback, userCertificateSelectionCallback);
                base._httpsStream.AuthenticateAsClient(sCertCN, certificateCollectionFromFile, CONFIG.oAcceptedServerHTTPSProtocols, false);
                iHandshakeTime = (int) stopwatch.ElapsedMilliseconds;
            }
            catch (Exception exception)
            {
                iHandshakeTime = (int) stopwatch.ElapsedMilliseconds;
                FiddlerApplication.DebugSpew(exception.StackTrace + "\n" + exception.Message);
                FiddlerApplication.Log.LogFormat("fiddler.network.https> Failed to secure existing connection for {0}. {1}{2}", new object[] { sCertCN, exception.Message, (exception.InnerException != null) ? (" InnerException: " + exception.InnerException.ToString()) : "." });
                return false;
            }
            return true;
        }

        public override string ToString()
        {
            return string.Format("{0}[Key: {1}; UseCnt: {2} [{3}]; {4}; {5} (:{6} to {7}:{8} {9}) {10}]", new object[] { base._sPipeName, this._sPoolKey, base.iUseCount, base._sHackSessionList, base.bIsSecured ? "Secure" : "PlainText", this._isAuthenticated ? "Authenticated" : "Anonymous", base.LocalPort, base.Address, base.Port, this.isConnectedToGateway ? "Gateway" : "Direct", this._reusePolicy });
        }

        internal bool WrapSocketInPipe(Session oS, Socket oSocket, bool bSecureTheSocket, bool bCreateConnectTunnel, string sCertCN, string sClientCertificateFilename, string sPoolingKey, ref int iHTTPSHandshakeTime)
        {
            RemoteCertificateValidationCallback userCertificateValidationCallback = null;
            LocalCertificateSelectionCallback userCertificateSelectionCallback = null;
            this.sPoolKey = sPoolingKey;
            base._baseSocket = oSocket;
            this.dtConnected = DateTime.Now;
            if (bCreateConnectTunnel)
            {
                this._bIsConnectedToGateway = true;
                base._baseSocket.Send(Encoding.ASCII.GetBytes("CONNECT " + Utilities.TrimBefore(sPoolingKey, ":") + " HTTP/1.1\r\nConnection: close\r\n\r\n"));
                byte[] buffer = new byte[0x2000];
                int num = base._baseSocket.Receive(buffer);
                if ((num <= 12) || !Utilities.isHTTP200Array(buffer))
                {
                    FiddlerApplication.Log.LogFormat("fiddler.network.connect2> Unexpected response from upstream gateway {0}", new object[] { Encoding.UTF8.GetString(buffer, 0, Math.Min(num, 0x100)) });
                    return false;
                }
                this.sPoolKey = "GATEWAY:HTTPS:" + sPoolingKey;
                this._bIsConnectedToGateway = true;
            }
            if (bSecureTheSocket)
            {
                X509CertificateCollection certificateCollectionFromFile = this.GetCertificateCollectionFromFile(sClientCertificateFilename);
                Stopwatch stopwatch = Stopwatch.StartNew();
                if (userCertificateValidationCallback == null)
                {
                    userCertificateValidationCallback = (sender, certificate, chain, sslPolicyErrors) => ConfirmServerCertificate(oS, sCertCN, certificate, chain, sslPolicyErrors);
                }
                if (userCertificateSelectionCallback == null)
                {
                    userCertificateSelectionCallback = (sender, targetHost, localCertificates, remoteCertificate, acceptableIssuers) => this.AttachClientCertificate(oS, sender, targetHost, localCertificates, remoteCertificate, acceptableIssuers);
                }
                base._httpsStream = new SslStream(new NetworkStream(base._baseSocket, false), false, userCertificateValidationCallback, userCertificateSelectionCallback);
                base._httpsStream.AuthenticateAsClient(sCertCN, certificateCollectionFromFile, CONFIG.oAcceptedServerHTTPSProtocols, false);
                iHTTPSHandshakeTime = (int) stopwatch.ElapsedMilliseconds;
            }
            return true;
        }

        internal bool isAuthenticated
        {
            get
            {
                return this._isAuthenticated;
            }
        }

        internal bool isClientCertAttached
        {
            get
            {
                return ((base._httpsStream != null) && base._httpsStream.IsMutuallyAuthenticated);
            }
        }

        public bool isConnectedToGateway
        {
            get
            {
                return this._bIsConnectedToGateway;
            }
        }

        public IPEndPoint RemoteEndPoint
        {
            get
            {
                if (base._baseSocket == null)
                {
                    return null;
                }
                return (base._baseSocket.RemoteEndPoint as IPEndPoint);
            }
        }

        public PipeReusePolicy ReusePolicy
        {
            get
            {
                return this._reusePolicy;
            }
            set
            {
                this._reusePolicy = value;
            }
        }

        public string sPoolKey
        {
            get
            {
                return this._sPoolKey;
            }
            private set
            {
                this._sPoolKey = value;
            }
        }
    }
}

