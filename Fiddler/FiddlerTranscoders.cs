namespace Fiddler
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Reflection;
    using System.Security.Policy;

    public class FiddlerTranscoders : IDisposable
    {
        internal Dictionary<string, TranscoderTuple> m_Exporters = new Dictionary<string, TranscoderTuple>();
        internal Dictionary<string, TranscoderTuple> m_Importers = new Dictionary<string, TranscoderTuple>();

        internal FiddlerTranscoders()
        {
        }

        private bool AddToImportOrExportCollection(Dictionary<string, TranscoderTuple> oCollection, Type t)
        {
            bool flag = false;
            ProfferFormatAttribute[] customAttributes = (ProfferFormatAttribute[]) Attribute.GetCustomAttributes(t, typeof(ProfferFormatAttribute));
            if ((customAttributes != null) && (customAttributes.Length > 0))
            {
                flag = true;
                foreach (ProfferFormatAttribute attribute in customAttributes)
                {
                    if (!oCollection.ContainsKey(attribute.FormatName))
                    {
                        oCollection.Add(attribute.FormatName, new TranscoderTuple(attribute.FormatDescription, t));
                    }
                }
            }
            return flag;
        }

        public void Dispose()
        {
            if (this.m_Exporters != null)
            {
                this.m_Exporters.Clear();
            }
            if (this.m_Importers != null)
            {
                this.m_Importers.Clear();
            }
            this.m_Importers = (Dictionary<string, TranscoderTuple>) (this.m_Exporters = null);
        }

        private void EnsureTranscoders()
        {
            if (((this.m_Importers == null) || (this.m_Exporters == null)) || ((this.m_Importers.Count == 0) || (this.m_Exporters.Count == 0)))
            {
                this.ScanPathForTranscoders(CONFIG.GetPath("Transcoders_Machine"));
                this.ScanPathForTranscoders(CONFIG.GetPath("Transcoders_User"));
            }
        }

        public TranscoderTuple GetExporter(string sExportFormat)
        {
            TranscoderTuple tuple;
            this.EnsureTranscoders();
            if (this.m_Exporters == null)
            {
                return null;
            }
            if (!this.m_Exporters.TryGetValue(sExportFormat, out tuple))
            {
                return null;
            }
            return tuple;
        }

        internal string[] getExportFormats()
        {
            this.EnsureTranscoders();
            if (!this.hasExporters)
            {
                return new string[0];
            }
            string[] array = new string[this.m_Exporters.Count];
            this.m_Exporters.Keys.CopyTo(array, 0);
            return array;
        }

        public TranscoderTuple GetImporter(string sImportFormat)
        {
            TranscoderTuple tuple;
            this.EnsureTranscoders();
            if (this.m_Importers == null)
            {
                return null;
            }
            if (!this.m_Importers.TryGetValue(sImportFormat, out tuple))
            {
                return null;
            }
            return tuple;
        }

        internal string[] getImportFormats()
        {
            this.EnsureTranscoders();
            if (!this.hasImporters)
            {
                return new string[0];
            }
            string[] array = new string[this.m_Importers.Count];
            this.m_Importers.Keys.CopyTo(array, 0);
            return array;
        }

        private bool ScanAssemblyForTranscoders(Assembly assemblyInput)
        {
            bool flag = false;
            try
            {
                if (!Utilities.FiddlerMeetsVersionRequirement(assemblyInput, "Importers and Exporters"))
                {
                    FiddlerApplication.Log.LogFormat("Assembly {0} did not specify a RequiredVersionAttribute. Aborting load of transcoders.", new object[] { assemblyInput.CodeBase });
                    return false;
                }
                foreach (Type type in assemblyInput.GetExportedTypes())
                {
                    if ((!type.IsAbstract && type.IsPublic) && type.IsClass)
                    {
                        if (typeof(ISessionImporter).IsAssignableFrom(type))
                        {
                            try
                            {
                                if (!this.AddToImportOrExportCollection(this.m_Importers, type))
                                {
                                    FiddlerApplication.Log.LogFormat("WARNING: SessionImporter {0} from {1} failed to specify any ImportExportFormat attributes.", new object[] { type.Name, assemblyInput.CodeBase });
                                }
                                else
                                {
                                    flag = true;
                                }
                            }
                            catch (Exception exception)
                            {
                                FiddlerApplication.DoNotifyUser(string.Format("[Fiddler] Failure loading {0} SessionImporter from {1}: {2}\n\n{3}\n\n{4}", new object[] { type.Name, assemblyInput.CodeBase, exception.Message, exception.StackTrace, exception.InnerException }), "Extension Load Error");
                            }
                        }
                        if (typeof(ISessionExporter).IsAssignableFrom(type))
                        {
                            try
                            {
                                if (!this.AddToImportOrExportCollection(this.m_Exporters, type))
                                {
                                    FiddlerApplication.Log.LogFormat("WARNING: SessionExporter {0} from {1} failed to specify any ImportExportFormat attributes.", new object[] { type.Name, assemblyInput.CodeBase });
                                }
                                else
                                {
                                    flag = true;
                                }
                            }
                            catch (Exception exception2)
                            {
                                FiddlerApplication.DoNotifyUser(string.Format("[Fiddler] Failure loading {0} SessionExporter from {1}: {2}\n\n{3}\n\n{4}", new object[] { type.Name, assemblyInput.CodeBase, exception2.Message, exception2.StackTrace, exception2.InnerException }), "Extension Load Error");
                            }
                        }
                    }
                }
            }
            catch (Exception exception3)
            {
                FiddlerApplication.DoNotifyUser(string.Format("[Fiddler] Failure loading Importer/Exporter from {0}: {1}", assemblyInput.CodeBase, exception3.Message), "Extension Load Error");
                return false;
            }
            return flag;
        }

        private void ScanPathForTranscoders(string sPath)
        {
            try
            {
                if (Directory.Exists(sPath))
                {
                    Evidence securityEvidence = Assembly.GetExecutingAssembly().Evidence;
                    FileInfo[] files = new DirectoryInfo(sPath).GetFiles();
                    bool boolPref = FiddlerApplication.Prefs.GetBoolPref("fiddler.debug.extensions.verbose", false);
                    if (boolPref)
                    {
                        FiddlerApplication.Log.LogFormat("Searching for Transcoders under {0}", new object[] { sPath });
                    }
                    foreach (FileInfo info in files)
                    {
                        if (info.FullName.EndsWith(".dll", StringComparison.OrdinalIgnoreCase) && !info.FullName.StartsWith("_", StringComparison.OrdinalIgnoreCase))
                        {
                            Assembly assembly;
                            if (boolPref)
                            {
                                FiddlerApplication.Log.LogFormat("Looking for Transcoders inside {0}", new object[] { info.FullName.ToString() });
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
                                goto Label_0105;
                            }
                            this.ScanAssemblyForTranscoders(assembly);
                        Label_0105:;
                        }
                    }
                }
            }
            catch (Exception exception2)
            {
                FiddlerApplication.DoNotifyUser(string.Format("[Fiddler] Failure loading Transcoders: {0}", exception2.Message), "Transcoders Load Error");
            }
        }

        internal bool hasExporters
        {
            get
            {
                return ((this.m_Exporters != null) && (this.m_Exporters.Count > 0));
            }
        }

        internal bool hasImporters
        {
            get
            {
                return ((this.m_Importers != null) && (this.m_Importers.Count > 0));
            }
        }
    }
}

