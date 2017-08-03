namespace Fiddler
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Drawing;
    using System.IO;
    using System.Reflection;
    using System.Security.Policy;
    using System.Windows.Forms;
    using System.ComponentModel;

    public class Inspectors : IDisposable
    {
        private Hashtable m_RequestInspectors;
        private Hashtable m_ResponseInspectors;
        private TabControl m_tabsRequest;
        private TabControl m_tabsResponse;
        private frmViewer m_Viewer;

        internal Inspectors(TabControl oRequestTabs, TabControl oResponseTabs, frmViewer oMain)
        {
            this.m_tabsRequest = oRequestTabs;
            this.m_tabsResponse = oResponseTabs;
            this.m_Viewer = oMain;
            this.m_RequestInspectors = new Hashtable();
            this.m_ResponseInspectors = new Hashtable();
            if (CONFIG.bLoadInspectors)
            {
                this.ScanInspectors();
            }
        }

        private void _CreateSyntaxViewAd()
        {
            Label label=new Label();
            TabPage oAdPage = new TabPage("Get SyntaxView");
            label = new Label {
                Font = new Font(label.Font.FontFamily, 11f),
                Height = 50,
                Text = "The SyntaxView Inspector displays syntax-highlighted html, script, css, and xml. If you're a web developer, you'll want this add-on. ",
                Padding = new Padding(8)
            };
            oAdPage.Controls.Add(label);
            label.Dock = DockStyle.Top;
            LinkLabel label2 = new LinkLabel {
                AutoSize = true,
                Location = new Point(20, 60),
                Text = "Download and Install SyntaxView now...",
                LinkBehavior = LinkBehavior.HoverUnderline
            };
            label2.LinkClicked += delegate (object sender, LinkLabelLinkClickedEventArgs e) {
                Utilities.LaunchHyperlink(CONFIG.GetUrl("REDIR") + "SYNTAXVIEWINSTALL");
            };
            oAdPage.Controls.Add(label2);
            LinkLabel label3 = new LinkLabel {
                AutoSize = true,
                Text = "Learn more about SyntaxView and other Inspector add-ons...",
                Location = new Point(20, 80),
                LinkBehavior = LinkBehavior.HoverUnderline
            };
            label3.LinkClicked += delegate (object sender, LinkLabelLinkClickedEventArgs e) {
                Utilities.LaunchHyperlink(CONFIG.GetUrl("REDIR") + "SYNTAXVIEWLEARN");
            };
            oAdPage.Controls.Add(label3);
            LinkLabel label4 = new LinkLabel {
                AutoSize = true,
                Text = "Remove this page",
                Location = new Point(20, 100),
                LinkBehavior = LinkBehavior.HoverUnderline
            };
            label4.LinkClicked += delegate (object sender, LinkLabelLinkClickedEventArgs e) {
                FiddlerApplication.Prefs.SetBoolPref("fiddler.inspectors.response.AdvertiseSyntaxView", false);
                oAdPage.Dispose();
            };
            oAdPage.Controls.Add(label4);
            this.m_tabsResponse.TabPages.Add(oAdPage);
        }

        internal void AddResponseInspectorsToTabControl(TabControl oTC)
        {
            List<TabPage> list = new List<TabPage>();
            foreach (DictionaryEntry entry in this.m_ResponseInspectors)
            {
                try
                {
                    TabPage page=new TabPage();
                    Inspector2 inspector = (Inspector2) Activator.CreateInstance((entry.Value as Inspector2).GetType());
                    page = new TabPage {
                        Font = new Font(page.Font.FontFamily, CONFIG.flFontSize),
                        Tag = inspector
                    };
                    inspector.AddToTab(page);
                    list.Add(page);
                    continue;
                }
                catch (Exception exception)
                {
                    FiddlerApplication.Log.LogFormat("[Fiddler] Failure initializing Response Inspector:  {0}\n{1}", new object[] { exception.Message, exception.StackTrace });
                    continue;
                }
            }
            oTC.TabPages.AddRange(list.ToArray());
        }

        internal void AnnounceFontSizeChange(float flSizeInPoints)
        {
            foreach (Inspector2 inspector in this.m_RequestInspectors.Values)
            {
                inspector.SetFontSize(flSizeInPoints);
            }
            foreach (Inspector2 inspector2 in this.m_ResponseInspectors.Values)
            {
                inspector2.SetFontSize(flSizeInPoints);
            }
        }

        public void Dispose()
        {
            foreach (TabPage page in this.m_RequestInspectors.Keys)
            {
                page.Tag = null;
                if (page.Parent != null)
                {
                    ((TabControl) page.Parent).TabPages.Remove(page);
                }
                page.Dispose();
            }
            foreach (TabPage page2 in this.m_ResponseInspectors.Keys)
            {
                page2.Tag = null;
                if (page2.Parent != null)
                {
                    ((TabControl) page2.Parent).TabPages.Remove(page2);
                }
                page2.Dispose();
            }
            this.m_RequestInspectors.Clear();
            this.m_ResponseInspectors.Clear();
        }

        public DictionaryEntry FindBestRequestInspectorForContentType(string sContentType)
        {
            int index = sContentType.IndexOf(';');
            if (index > -1)
            {
                sContentType = sContentType.Substring(0, index);
            }
            DictionaryEntry entry = new DictionaryEntry(null, null);
            int num2 = -1;
            foreach (DictionaryEntry entry2 in this.m_RequestInspectors)
            {
                int num3 = ((Inspector2) entry2.Value).ScoreForContentType(sContentType);
                if (num3 > num2)
                {
                    entry = entry2;
                    num2 = num3;
                }
            }
            return entry;
        }

        public DictionaryEntry FindBestResponseInspectorForContentType(string sContentType)
        {
            int index = sContentType.IndexOf(';');
            if (index > -1)
            {
                sContentType = sContentType.Substring(0, index);
            }
            DictionaryEntry entry = new DictionaryEntry(null, null);
            int num2 = -1;
            foreach (DictionaryEntry entry2 in this.m_ResponseInspectors)
            {
                int num3 = ((Inspector2) entry2.Value).ScoreForContentType(sContentType);
                if (num3 > num2)
                {
                    entry = entry2;
                    num2 = num3;
                }
            }
            return entry;
        }

        internal void ScanInspectors()
        {
            string path = CONFIG.GetPath("Inspectors");
            try
            {
                TabPage key;
                Evidence securityEvidence = Assembly.GetExecutingAssembly().Evidence;
                foreach (FileInfo info in new DirectoryInfo(path).GetFiles())
                {
                    if (info.FullName.EndsWith(".dll", StringComparison.OrdinalIgnoreCase) && !info.FullName.StartsWith("_", StringComparison.OrdinalIgnoreCase))
                    {
                        Assembly assembly;
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
                            goto Label_0283;
                        }
                        try
                        {
                            if (assembly.IsDefined(typeof(RequiredVersionAttribute), false))
                            {
                                RequiredVersionAttribute customAttribute = (RequiredVersionAttribute) Attribute.GetCustomAttribute(assembly, typeof(RequiredVersionAttribute));
                                int num = Utilities.CompareVersions(customAttribute.RequiredVersion, CONFIG.FiddlerVersionInfo);
                                if (num > 0)
                                {
                                    FiddlerApplication.DoNotifyUser(string.Format("The Inspectors in {0} require Fiddler v{1} or later. (You have v{2})\n\nPlease install the latest version of Fiddler from http://www.fiddler2.com.\n\nCode: {3}", new object[] { info.FullName, customAttribute.RequiredVersion, CONFIG.FiddlerVersionInfo, num }), "Inspector Not Loaded");
                                }
                                else
                                {
                                    foreach (System.Type type in assembly.GetExportedTypes())
                                    {
                                        if ((!type.IsAbstract && !type.IsInterface) && (type.IsPublic && type.IsSubclassOf(typeof(Inspector2))))
                                        {
                                            try
                                            {
                                                TabPage page=new TabPage();
                                                Inspector2 inspector = (Inspector2) Activator.CreateInstance(type);
                                                page = new TabPage {
                                                    Font = new Font(page.Font.FontFamily, CONFIG.flFontSize),
                                                    Tag = inspector
                                                };
                                                if (inspector is IRequestInspector2)
                                                {
                                                    this.m_RequestInspectors.Add(page, inspector);
                                                }
                                                else if (inspector is IResponseInspector2)
                                                {
                                                    this.m_ResponseInspectors.Add(page, inspector);
                                                }
                                            }
                                            catch (Exception exception2)
                                            {
                                                FiddlerApplication.DoNotifyUser(string.Format("[Fiddler] Failure loading {0} inspector from {1}: {2}\n\n{3}", new object[] { type.Name, info.FullName.ToString(), exception2.Message, exception2.StackTrace }), "Inspector Failed");
                                            }
                                        }
                                    }
                                }
                            }
                        }
                        catch (Exception exception3)
                        {
                            FiddlerApplication.DebugSpew(string.Format("[Fiddler] Failure loading inspectors from {0}: {1}", info.FullName.ToString(), exception3.Message));
                        }
                    Label_0283:;
                    }
                }
                List<TabPage> list = new List<TabPage>();
                List<TabPage> list2 = new List<TabPage>();
                foreach (DictionaryEntry entry in this.m_RequestInspectors)
                {
                    try
                    {
                        key = (TabPage) entry.Key;
                        ((Inspector2) entry.Value).AddToTab(key);
                        list.Add(key);
                        key.Validating += new CancelEventHandler(this.m_Viewer.actValidateRequest);
                        key.CausesValidation = true;
                        continue;
                    }
                    catch (Exception exception4)
                    {
                        FiddlerApplication.DoNotifyUser(string.Format("[Fiddler] Failure initializing Request Inspector:  {0}\n{1}", exception4.Message, exception4.StackTrace), "Initialization Error");
                        continue;
                    }
                }
                bool flag = false;
                foreach (DictionaryEntry entry2 in this.m_ResponseInspectors)
                {
                    try
                    {
                        key = (TabPage) entry2.Key;
                        ((Inspector2) entry2.Value).AddToTab(key);
                        if (key.Text.Contains("SyntaxView"))
                        {
                            flag = true;
                        }
                        list2.Add(key);
                        key.Validating += new CancelEventHandler(this.m_Viewer.actValidateResponse);
                        key.CausesValidation = true;
                        continue;
                    }
                    catch (Exception exception5)
                    {
                        FiddlerApplication.DoNotifyUser(string.Format("[Fiddler] Failure initializing Response Inspector:  {0}\n{1}", exception5.Message, exception5.StackTrace), "Initialization Error");
                        continue;
                    }
                }
                if (!flag && FiddlerApplication.Prefs.GetBoolPref("fiddler.inspectors.response.AdvertiseSyntaxView", true))
                {
                    this._CreateSyntaxViewAd();
                }
                InspectorComparer comparer = new InspectorComparer(this.m_RequestInspectors);
                list.Sort(comparer);
                comparer = new InspectorComparer(this.m_ResponseInspectors);
                list2.Sort(comparer);
                this.m_tabsRequest.TabPages.AddRange(list.ToArray());
                this.m_tabsResponse.TabPages.AddRange(list2.ToArray());
            }
            catch (Exception exception6)
            {
                FiddlerApplication.DoNotifyUser(string.Format("Failure loading inspectors: {0}", exception6.Message), "Error");
            }
        }

        public Hashtable RequestInspectors
        {
            get
            {
                return this.m_RequestInspectors;
            }
        }

        public Hashtable ResponseInspectors
        {
            get
            {
                return this.m_ResponseInspectors;
            }
        }
    }
}

