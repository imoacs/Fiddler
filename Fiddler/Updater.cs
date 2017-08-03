namespace Fiddler
{
    using Microsoft.Win32;
    using System;
    using System.IO;
    using System.Net;
    using System.Text;
    using System.Windows.Forms;

    internal class Updater
    {
        public void CheckForUpdates(bool bVerbose)
        {
            try
            {
                VersionStruct verServer = new VersionStruct();
                verServer = this.GetLatestVersion(CONFIG.bIsBeta);
                FiddlerApplication.Prefs.SetStringPref("fiddler.welcomemsg", verServer.sWelcomeMsg);
                int num = CompareVersions(ref verServer, CONFIG.FiddlerVersionInfo);
                if ((num > 1) || (bVerbose && (num > 0)))
                {
                    verServer.sWhatIsNew = verServer.sWhatIsNew + string.Format("\r\n\r\n------------------------------\r\nWould you like to learn more?\r\n{0}{1}&IsBeta={2}", CONFIG.GetUrl("ChangeList"), Application.ProductVersion, CONFIG.bIsBeta.ToString());
                    frmUpdate oUpdatePrompt = new frmUpdate("Update Announcement (from v" + Application.ProductVersion + " to v" + verServer.Major.ToString() + "." + verServer.Minor.ToString() + "." + verServer.Build.ToString() + "." + verServer.Private.ToString() + ")", "Good news! An improved version of Fiddler is now available.\r\n\r\n" + verServer.sWhatIsNew, !verServer.bMustCleanInstall, bVerbose ? MessageBoxDefaultButton.Button1 : MessageBoxDefaultButton.Button2) {
                        StartPosition = FormStartPosition.CenterScreen
                    };
                    DialogResult updateDecision = FiddlerApplication.UI.GetUpdateDecision(oUpdatePrompt);
                    if (DialogResult.Yes == updateDecision)
                    {
                        Utilities.LaunchHyperlink(CONFIG.GetUrl("InstallLatest"));
                    }
                    else if (DialogResult.Retry == updateDecision)
                    {
                        try
                        {
                            RegistryKey oReg = Registry.CurrentUser.OpenSubKey(CONFIG.GetRegPath("Root"), true);
                            if (oReg != null)
                            {
                                Utilities.SetRegistryString(oReg, "UpdatePending", "True");
                                oReg.Close();
                            }
                        }
                        catch
                        {
                        }
                    }
                }
                else if (bVerbose)
                {
                    FiddlerApplication.DoNotifyUser("Your version of Fiddler is up-to-date.\n\nThanks for checking!", "FiddlerUpdate");
                }
            }
            catch (Exception exception)
            {
                if (bVerbose)
                {
                    MessageBox.Show("There was an error retrieving version information. You can check for updates manually by visiting https://www.fiddler2.com/\n\n" + exception.ToString(), "Error retrieving version information");
                }
            }
        }

        private static int CompareVersions(ref VersionStruct verServer, Version verClient)
        {
            if (verServer.Major > verClient.Major)
            {
                return 4;
            }
            if (verClient.Major > verServer.Major)
            {
                return -4;
            }
            if (verServer.Minor > verClient.Minor)
            {
                return 3;
            }
            if (verClient.Minor > verServer.Minor)
            {
                return -3;
            }
            if (verServer.Build > verClient.Build)
            {
                return 2;
            }
            if (verClient.Build > verServer.Build)
            {
                return -2;
            }
            if (verServer.Private > verClient.Revision)
            {
                return 1;
            }
            if (verClient.Revision > verServer.Private)
            {
                return -1;
            }
            return 0;
        }

        private VersionStruct GetLatestVersion(bool isBeta)
        {
            HttpWebRequest request = (HttpWebRequest) WebRequest.Create(CONFIG.GetUrl("VerCheck") + isBeta.ToString());
            if ((CONFIG.sGatewayUsername != null) && (CONFIG.sGatewayPassword != null))
            {
                request.Proxy.Credentials = new NetworkCredential(CONFIG.sGatewayUsername, CONFIG.sGatewayPassword);
            }
            else
            {
                request.Proxy.Credentials = CredentialCache.DefaultCredentials;
            }
            request.UserAgent = string.Format("Fiddler/{0}{1} (.NET {2}; {3})", new object[] { Application.ProductVersion, CONFIG.bIsBeta ? " beta" : string.Empty, Environment.Version.ToString(), Environment.OSVersion.VersionString });
            request.Headers.Add("Pragma: no-cache");
            request.Referer = string.Format("http://fiddler2.com/client/{0}{1}", CONFIG.IsMicrosoftMachine ? "MSFT/" : string.Empty, CONFIG.FiddlerVersionInfo.ToString());
            request.AllowAutoRedirect = true;
            request.KeepAlive = false;
            HttpWebResponse response = (HttpWebResponse) request.GetResponse();
            VersionStruct struct2 = new VersionStruct();
            if (response.StatusCode == HttpStatusCode.OK)
            {
                StreamReader reader = new StreamReader(response.GetResponseStream(), Encoding.UTF8);
                struct2.Major = int.Parse(reader.ReadLine());
                struct2.Minor = int.Parse(reader.ReadLine());
                struct2.Build = int.Parse(reader.ReadLine());
                struct2.Private = int.Parse(reader.ReadLine());
                struct2.sWhatIsNew = reader.ReadToEnd().Trim();
                struct2.bMustCleanInstall = struct2.sWhatIsNew.Contains("CleanInstall");
                int index = struct2.sWhatIsNew.IndexOf("@WELCOME@");
                if (index > 0)
                {
                    struct2.sWelcomeMsg = struct2.sWhatIsNew.Substring(index + 9).Trim();
                    struct2.sWhatIsNew = struct2.sWhatIsNew.Substring(0, index).Trim();
                }
            }
            response.Close();
            return struct2;
        }
    }
}

