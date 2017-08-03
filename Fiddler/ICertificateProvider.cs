namespace Fiddler
{
    using System;
    using System.Runtime.InteropServices;
    using System.Security.Cryptography.X509Certificates;

    public interface ICertificateProvider
    {
        bool ClearCertificateCache();
        bool CreateRootCertificate();
        X509Certificate2 GetCertificateForHost(string sHostname);
        X509Certificate2 GetRootCertificate();
        bool rootCertIsTrusted(out bool bUserTrusted, out bool bMachineTrusted);
        bool TrustRootCertificate();
    }
}

