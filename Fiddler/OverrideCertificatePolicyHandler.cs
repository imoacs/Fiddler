namespace Fiddler
{
    using System;
    using System.Net.Security;
    using System.Runtime.CompilerServices;
    using System.Runtime.InteropServices;
    using System.Security.Cryptography.X509Certificates;

    public delegate bool OverrideCertificatePolicyHandler(Session oSession, string sExpectedCN, X509Certificate ServerCertificate, X509Chain ServerCertificateChain, SslPolicyErrors sslPolicyErrors, out bool bTreatCertificateAsValid);
}

