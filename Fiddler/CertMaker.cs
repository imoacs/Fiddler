namespace Fiddler
{
    using System;
    using System.IO;
    using System.Reflection;
    using System.Security.Cryptography.X509Certificates;
    using System.Threading;

    internal class CertMaker
    {
        private static ICertificateProvider oCertProvider = null;
        private static readonly object oEECertCreationLock = new object();
        private static readonly object oRootCertCreationLock = new object();

        static CertMaker()
        {
            oCertProvider = LoadOverrideCertProvider();
        }

        private static bool CreateCert(string sHostname, bool isRoot)
        {
            if (!isRoot && !rootCertExists())
            {
                lock (oRootCertCreationLock)
                {
                    if ((FindCert(CONFIG.sMakeCertRootCN, false) == null) && !createRootCert())
                    {
                        FiddlerApplication.DoNotifyUser("Creation of the root certificate was not successful.", "Certificate Error");
                        return false;
                    }
                }
            }
            if (sHostname.IndexOfAny(new char[] { '"', '\r', '\n' }) == -1)
            {
                int num;
                string str3;
                string path = CONFIG.GetPath("MakeCert");
                if (!File.Exists(path))
                {
                    FiddlerApplication.DoNotifyUser("Cannot locate:\n\t\"" + path + "\"\n\nPlease move makecert.exe to the Fiddler installation directory.", "MakeCert.exe not found");
                    throw new FileNotFoundException("Cannot locate: " + path + ". Please move makecert.exe to the Fiddler installation directory.");
                }
                string sParams = string.Format(isRoot ? CONFIG.sMakeCertParamsRoot : CONFIG.sMakeCertParamsEE, sHostname, CONFIG.sMakeCertSubjectO, CONFIG.sMakeCertRootCN);
                lock (oEECertCreationLock)
                {
                    if (FindCert(sHostname, false) == null)
                    {
                        str3 = Utilities.GetExecutableOutput(path, sParams, out num);
                        if (CONFIG.bDebugCertificateGeneration)
                        {
                            FiddlerApplication.Log.LogFormat("/Fiddler.CertMaker>{3}-CreateCert({0}) => ({1}){2}", new object[] { sHostname, num, (num == 0) ? "." : ("\r\n" + str3), Thread.CurrentThread.ManagedThreadId });
                        }
                        if (num == 0)
                        {
                            Thread.Sleep(150);
                        }
                    }
                    else
                    {
                        if (CONFIG.bDebugCertificateGeneration)
                        {
                            FiddlerApplication.Log.LogFormat("/Fiddler.CertMaker>{1} A racing thread already successfully CreatedCert({0})", new object[] { sHostname, Thread.CurrentThread.ManagedThreadId });
                        }
                        return true;
                    }
                }
                if (num == 0)
                {
                    return true;
                }
                string sMessage = string.Format("Creation of the interception certificate failed.\n\nmakecert.exe returned {0}.\n\n{1}", num, str3);
                FiddlerApplication.Log.LogFormat("Fiddler.CertMaker> [{0}{1}] Returned Error: {2} ", new object[] { path, sParams, sMessage });
                if (CONFIG.bDebugCertificateGeneration)
                {
                    FiddlerApplication.DoNotifyUser(sMessage, "Unable to Generate Certificate");
                }
            }
            return false;
        }

        internal static bool createRootCert()
        {
            if (oCertProvider != null)
            {
                return oCertProvider.CreateRootCertificate();
            }
            return CreateCert(CONFIG.sMakeCertRootCN, true);
        }

        internal static bool exportRootToDesktop()
        {
            try
            {
                byte[] bytes = getRootCertBytes();
                if (bytes != null)
                {
                    File.WriteAllBytes(Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory) + @"\FiddlerRoot.cer", bytes);
                    return true;
                }
            }
            catch (Exception exception)
            {
                FiddlerApplication.ReportException(exception);
                return false;
            }
            return false;
        }

        internal static X509Certificate2 FindCert(string sHostname, bool allowCreate)
        {
            if (oCertProvider != null)
            {
                return oCertProvider.GetCertificateForHost(sHostname);
            }
            X509Store store = new X509Store(StoreName.My, StoreLocation.CurrentUser);
            store.Open(OpenFlags.ReadOnly);
            string b = string.Format("CN={0}{1}", sHostname, CONFIG.sMakeCertSubjectO);
            X509Certificate2Enumerator enumerator = store.Certificates.GetEnumerator();
            while (enumerator.MoveNext())
            {
                X509Certificate2 current = enumerator.Current;
                if (string.Equals(current.Subject, b, StringComparison.OrdinalIgnoreCase))
                {
                    store.Close();
                    return current;
                }
            }
            store.Close();
            if (!allowCreate)
            {
                return null;
            }
            bool flag = CreateCert(sHostname, false);
            X509Certificate2 certificate2 = FindCert(sHostname, false);
            if (certificate2 == null)
            {
                FiddlerApplication.Log.LogFormat("!Fiddler.CertMaker> Tried to create cert for {0}, got {1}, but can't find it from thread {2}!", new object[] { sHostname, flag.ToString(), Thread.CurrentThread.ManagedThreadId });
            }
            return certificate2;
        }

        private static X509Certificate2Collection FindCertsByIssuer(StoreName storeName, string sFullIssuerSubject)
        {
            X509Store store = new X509Store(storeName, StoreLocation.CurrentUser);
            store.Open(OpenFlags.OpenExistingOnly);
            X509Certificate2Collection certificates = store.Certificates.Find(X509FindType.FindByIssuerDistinguishedName, sFullIssuerSubject, false);
            store.Close();
            return certificates;
        }

        private static X509Certificate2Collection FindCertsBySubject(StoreName storeName, StoreLocation storeLocation, string sFullSubject)
        {
            X509Store store = new X509Store(storeName, storeLocation);
            store.Open(OpenFlags.OpenExistingOnly);
            X509Certificate2Collection certificates = store.Certificates.Find(X509FindType.FindBySubjectDistinguishedName, sFullSubject, false);
            store.Close();
            return certificates;
        }

        internal static byte[] getRootCertBytes()
        {
            X509Certificate2 rootCertificate = GetRootCertificate();
            if (rootCertificate == null)
            {
                return null;
            }
            return rootCertificate.Export(X509ContentType.Cert);
        }

        public static X509Certificate2 GetRootCertificate()
        {
            return ((oCertProvider != null) ? oCertProvider.GetRootCertificate() : FindCert(CONFIG.sMakeCertRootCN, false));
        }

        private static ICertificateProvider LoadOverrideCertProvider()
        {
            string stringPref = FiddlerApplication.Prefs.GetStringPref("fiddler.certmaker.assembly", CONFIG.GetPath("App") + "CertMaker.dll");
            if (File.Exists(stringPref))
            {
                Assembly assembly;
                try
                {
                    assembly = Assembly.LoadFrom(stringPref);
                    if (!Utilities.FiddlerMeetsVersionRequirement(assembly, "Certificate Maker"))
                    {
                        FiddlerApplication.Log.LogFormat("Assembly {0} did not specify a RequiredVersionAttribute. Aborting load of Certificate Generation module.", new object[] { assembly.CodeBase });
                        return null;
                    }
                }
                catch (Exception exception)
                {
                    FiddlerApplication.LogAddonException(exception, "Failed to load CertMaker" + stringPref);
                    return null;
                }
                foreach (Type type in assembly.GetExportedTypes())
                {
                    if ((!type.IsAbstract && type.IsPublic) && (type.IsClass && typeof(ICertificateProvider).IsAssignableFrom(type)))
                    {
                        try
                        {
                            return (ICertificateProvider) Activator.CreateInstance(type);
                        }
                        catch (Exception exception2)
                        {
                            FiddlerApplication.DoNotifyUser(string.Format("[Fiddler] Failure loading {0} CertMaker from {1}: {2}\n\n{3}\n\n{4}", new object[] { type.Name, assembly.CodeBase, exception2.Message, exception2.StackTrace, exception2.InnerException }), "Load Error");
                        }
                    }
                }
            }
            return null;
        }

        internal static void removeFiddlerGeneratedCerts()
        {
            if (oCertProvider != null)
            {
                oCertProvider.ClearCertificateCache();
            }
            else
            {
                lock (oRootCertCreationLock)
                {
                    string sFullSubject = string.Format("CN={0}{1}", CONFIG.sMakeCertRootCN, CONFIG.sMakeCertSubjectO);
                    X509Certificate2Collection certificates = FindCertsBySubject(StoreName.Root, StoreLocation.CurrentUser, sFullSubject);
                    if (certificates.Count > 0)
                    {
                        X509Store store = new X509Store(StoreName.Root, StoreLocation.CurrentUser);
                        store.Open(OpenFlags.OpenExistingOnly | OpenFlags.ReadWrite);
                        try
                        {
                            store.RemoveRange(certificates);
                        }
                        catch
                        {
                        }
                        store.Close();
                    }
                    certificates = FindCertsByIssuer(StoreName.My, sFullSubject);
                    if (certificates.Count > 0)
                    {
                        X509Store store2 = new X509Store(StoreName.My, StoreLocation.CurrentUser);
                        store2.Open(OpenFlags.OpenExistingOnly | OpenFlags.ReadWrite);
                        try
                        {
                            store2.RemoveRange(certificates);
                        }
                        catch
                        {
                        }
                        store2.Close();
                    }
                }
            }
        }

        internal static bool rootCertExists()
        {
            try
            {
                X509Certificate2 rootCertificate = GetRootCertificate();
                return (null != rootCertificate);
            }
            catch (Exception)
            {
                return false;
            }
        }

        internal static bool rootCertIsMachineTrusted()
        {
            if (oCertProvider != null)
            {
                bool flag;
                bool flag2;
                oCertProvider.rootCertIsTrusted(out flag, out flag2);
                return flag2;
            }
            return (FindCertsBySubject(StoreName.Root, StoreLocation.LocalMachine, string.Format("CN={0}{1}", CONFIG.sMakeCertRootCN, CONFIG.sMakeCertSubjectO)).Count > 0);
        }

        internal static bool rootCertIsTrusted()
        {
            if (oCertProvider != null)
            {
                bool flag;
                bool flag2;
                oCertProvider.rootCertIsTrusted(out flag, out flag2);
                return flag;
            }
            return (FindCertsBySubject(StoreName.Root, StoreLocation.CurrentUser, string.Format("CN={0}{1}", CONFIG.sMakeCertRootCN, CONFIG.sMakeCertSubjectO)).Count > 0);
        }

        internal static bool trustRootCert()
        {
            if (oCertProvider != null)
            {
                return oCertProvider.TrustRootCertificate();
            }
            X509Certificate2 certificate = FindCert(CONFIG.sMakeCertRootCN, false);
            if (certificate == null)
            {
                return false;
            }
            try
            {
                X509Store store = new X509Store(StoreName.Root, StoreLocation.CurrentUser);
                store.Open(OpenFlags.ReadWrite);
                try
                {
                    store.Add(certificate);
                }
                finally
                {
                    store.Close();
                }
                return true;
            }
            catch (Exception exception)
            {
                FiddlerApplication.Log.LogFormat("!Fiddler.CertMaker> Unable to auto-trust root: {0}", new object[] { exception });
                return false;
            }
        }
    }
}

