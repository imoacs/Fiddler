namespace Fiddler
{
    using System;
    using System.Net.Sockets;
    using System.Security.Cryptography.X509Certificates;

    internal class ProxyExecuteParams
    {
        public X509Certificate2 oServerCert;
        public Socket oSocket;

        public ProxyExecuteParams(Socket oS, X509Certificate2 oC)
        {
            this.oSocket = oS;
            this.oServerCert = oC;
        }
    }
}

