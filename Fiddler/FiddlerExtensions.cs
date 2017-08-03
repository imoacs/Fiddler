namespace Fiddler
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Reflection;
    using System.Security.Policy;

    public class FiddlerExtensions : IDisposable
    {
        private List<IAutoTamper> m_AutoFiddlers = new List<IAutoTamper>();
        private Dictionary<Guid, IFiddlerExtension> m_Extensions = new Dictionary<Guid, IFiddlerExtension>();

        internal FiddlerExtensions()
        {
            if (CONFIG.bLoadExtensions)
            {
                this.ScanExtensions();
            }
        }

        public void Dispose()
        {
            foreach (IFiddlerExtension extension in this.m_Extensions.Values)
            {
                try
                {
                    extension.OnBeforeUnload();
                    continue;
                }
                catch (Exception exception)
                {
                    FiddlerApplication.LogAddonException(exception, "Extension threw during OnBeforeUnload");
                    continue;
                }
            }
            this.m_AutoFiddlers.Clear();
            this.m_Extensions.Clear();
        }

        internal void DoAutoTamperRequestAfter(Session oSession)
        {
            foreach (IAutoTamper tamper in this.m_AutoFiddlers)
            {
                tamper.AutoTamperRequestAfter(oSession);
            }
        }

        internal void DoAutoTamperRequestBefore(Session oSession)
        {
            foreach (IAutoTamper tamper in this.m_AutoFiddlers)
            {
                tamper.AutoTamperRequestBefore(oSession);
            }
            if (FiddlerApplication.scriptRules != null)
            {
                FiddlerApplication.scriptRules.DoBeforeRequest(oSession);
            }
        }

        internal void DoAutoTamperResponseAfter(Session oSession)
        {
            foreach (IAutoTamper tamper in this.m_AutoFiddlers)
            {
                tamper.AutoTamperResponseAfter(oSession);
            }
        }

        internal void DoAutoTamperResponseBefore(Session oSession)
        {
            foreach (IAutoTamper tamper in this.m_AutoFiddlers)
            {
                tamper.AutoTamperResponseBefore(oSession);
            }
            if (FiddlerApplication.scriptRules != null)
            {
                FiddlerApplication.scriptRules.DoBeforeResponse(oSession);
            }
        }

        internal void DoBeforeReturningError(Session oSession)
        {
            foreach (IAutoTamper tamper in this.m_AutoFiddlers)
            {
                tamper.OnBeforeReturningError(oSession);
            }
            if (FiddlerApplication.scriptRules != null)
            {
                FiddlerApplication.scriptRules.DoReturningError(oSession);
            }
        }

        internal void DoOnLoad()
        {
            foreach (IFiddlerExtension extension in this.m_Extensions.Values)
            {
                try
                {
                    extension.OnLoad();
                    continue;
                }
                catch (Exception exception)
                {
                    FiddlerApplication.LogAddonException(exception, "Extension threw during OnLoad");
                    continue;
                }
            }
        }

        internal bool DoOnQuickExec(string sCommand)
        {
            foreach (IFiddlerExtension extension in this.m_Extensions.Values)
            {
                if (extension is IHandleExecAction)
                {
                    try
                    {
                        if ((extension as IHandleExecAction).OnExecAction(sCommand))
                        {
                            return true;
                        }
                        continue;
                    }
                    catch (Exception exception)
                    {
                        FiddlerApplication.LogAddonException(exception, "Extension threw during OnExecAction");
                        continue;
                    }
                }
            }
            if (FiddlerApplication.scriptRules == null)
            {
                return false;
            }
            return FiddlerApplication.scriptRules.DoExecAction(sCommand);
        }

        internal void DoPeekAtRequestHeaders(Session oSession)
        {
            foreach (IAutoTamper tamper in this.m_AutoFiddlers)
            {
                if (tamper is IAutoTamper3)
                {
                    (tamper as IAutoTamper3).OnPeekAtRequestHeaders(oSession);
                }
            }
            if (FiddlerApplication.scriptRules != null)
            {
                FiddlerApplication.scriptRules.DoPeekAtRequestHeaders(oSession);
            }
        }

        internal void DoPeekAtResponseHeaders(Session oSession)
        {
            foreach (IAutoTamper tamper in this.m_AutoFiddlers)
            {
                if (tamper is IAutoTamper2)
                {
                    (tamper as IAutoTamper2).OnPeekAtResponseHeaders(oSession);
                }
            }
            if (FiddlerApplication.scriptRules != null)
            {
                FiddlerApplication.scriptRules.DoPeekAtResponseHeaders(oSession);
            }
        }

        internal void ScanExtensions()
        {
            this.ScanPathForExtensions(CONFIG.GetPath("AutoFiddlers_User"));
            this.ScanPathForExtensions(CONFIG.GetPath("AutoFiddlers_Machine"));
            if (this.m_Extensions.Count > 0)
            {
                FiddlerApplication.FiddlerBoot += new SimpleEventHandler(this.DoOnLoad);
            }
            if (CONFIG.IsMicrosoftMachine && (this.m_Extensions.Count > 5))
            {
                FiddlerApplication.logSelfHostOnePerSession(60);
            }
        }

        private void ScanPathForExtensions(string sPath)
        {
            try
            {
                Evidence securityEvidence = Assembly.GetExecutingAssembly().Evidence;
                FileInfo[] files = new DirectoryInfo(sPath).GetFiles();
                bool boolPref = FiddlerApplication.Prefs.GetBoolPref("fiddler.debug.extensions.verbose", false);
                if (boolPref)
                {
                    FiddlerApplication.Log.LogFormat("Searching for FiddlerExtensions under {0}", new object[] { sPath });
                }
                foreach (FileInfo info in files)
                {
                    if (info.FullName.EndsWith(".dll", StringComparison.OrdinalIgnoreCase) && !info.FullName.StartsWith("_", StringComparison.OrdinalIgnoreCase))
                    {
                        Assembly assembly;
                        if (boolPref)
                        {
                            FiddlerApplication.Log.LogFormat("Looking for FiddlerExtensions inside {0}", new object[] { info.FullName.ToString() });
                        }
                        try
                        {
                            if (CONFIG.bRunningOnCLRv4)
                            {
                                assembly = Assembly.LoadFrom(info.FullName);
                            }
                            else
                            {
                                assembly = Assembly.LoadFrom(info.FullName, securityEvidence);
                            }
                        }
                        catch (Exception exception)
                        {
                            FiddlerApplication.LogAddonException(exception, "Failed to load " + info.FullName);
                            goto Label_024A;
                        }
                        try
                        {
                            if (Utilities.FiddlerMeetsVersionRequirement(assembly, "FiddlerExtensions"))
                            {
                                foreach (Type type in assembly.GetExportedTypes())
                                {
                                    if (((!type.IsAbstract && type.IsPublic) && (type.IsClass && typeof(IFiddlerExtension).IsAssignableFrom(type))) && !this.m_Extensions.ContainsKey(type.GUID))
                                    {
                                        try
                                        {
                                            IFiddlerExtension extension = (IFiddlerExtension) Activator.CreateInstance(type);
                                            this.m_Extensions.Add(type.GUID, extension);
                                            if (extension is IAutoTamper)
                                            {
                                                this.m_AutoFiddlers.Add(extension as IAutoTamper);
                                            }
                                        }
                                        catch (Exception exception2)
                                        {
                                            FiddlerApplication.DoNotifyUser(string.Format("[Fiddler] Failure loading {0} FiddlerExtensions from {1}: {2}\n\n{3}\n\n{4}", new object[] { type.Name, info.FullName.ToString(), exception2.Message, exception2.StackTrace, exception2.InnerException }), "Extension Load Error");
                                        }
                                    }
                                }
                            }
                        }
                        catch (Exception exception3)
                        {
                            FiddlerApplication.DoNotifyUser(string.Format("[Fiddler] Failure loading FiddlerExtensions from {0}: {1}", info.FullName.ToString(), exception3.Message), "Extension Load Error");
                        }
                    Label_024A:;
                    }
                }
            }
            catch (Exception exception4)
            {
                FiddlerApplication.DoNotifyUser(string.Format("[Fiddler] Failure loading FiddlerExtensions: {0}", exception4.Message), "Extension Load Error");
            }
        }

        internal Dictionary<Guid, IFiddlerExtension> Extensions
        {
            get
            {
                return this.m_Extensions;
            }
        }
    }
}

