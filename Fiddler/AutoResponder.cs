namespace Fiddler
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Runtime.CompilerServices;
    using System.Text;
    using System.Text.RegularExpressions;
    using System.Threading;
    using System.Web;
    using System.Windows.Forms;
    using System.Xml;

    public class AutoResponder
    {
        private List<ResponderRule> _alRules = new List<ResponderRule>();
        private bool _bEnabled;
        private bool _bPermitFallthrough;
        private bool _bRuleListIsDirty;
        private ReaderWriterLock _RWLockRules = new ReaderWriterLock();
        private UIAutoResponder oAutoResponderUI = new UIAutoResponder();

        internal AutoResponder()
        {
        }

        internal void AddActionToUI(string sAction)
        {
            this.oAutoResponderUI.cbxRuleAction.Items.Add(sAction);
        }

        public ResponderRule AddRule(string sRule, string sAction, bool bIsEnabled)
        {
            return this.AddRule(sRule, null, null, sAction, 0, bIsEnabled);
        }

        public ResponderRule AddRule(string sRule, Session oImportedSession, string sDescription, bool bEnabled)
        {
            if (oImportedSession != null)
            {
                return this.AddRule(sRule, oImportedSession.oResponse.headers, oImportedSession.responseBodyBytes, sDescription, 0, bEnabled);
            }
            return this.AddRule(sRule, null, null, sDescription, 0, bEnabled);
        }

        public ResponderRule AddRule(string sRule, Session oImportedSession, string sDescription, int iLatencyMS, bool bEnabled)
        {
            return this.AddRule(sRule, oImportedSession.oResponse.headers, oImportedSession.responseBodyBytes, sDescription, iLatencyMS, bEnabled);
        }

        public ResponderRule AddRule(string sRule, HTTPResponseHeaders oRH, byte[] arrResponseBody, string sDescription, int iLatencyMS, bool bEnabled)
        {
            try
            {
                ResponderRule item = new ResponderRule(sRule, oRH, arrResponseBody, sDescription, iLatencyMS, bEnabled);
                try
                {
                    this._RWLockRules.AcquireWriterLock(-1);
                    this._alRules.Add(item);
                }
                finally
                {
                    this._RWLockRules.ReleaseWriterLock();
                }
                this._bRuleListIsDirty = true;
                this.CreateViewItem(item);
                return item;
            }
            catch (Exception)
            {
                return null;
            }
        }

        internal void AddToUI()
        {
            FiddlerApplication.UI.pageResponder.Controls.Add(this.oAutoResponderUI);
            this.oAutoResponderUI.Parent = FiddlerApplication.UI.pageResponder;
            this.oAutoResponderUI.Dock = DockStyle.Fill;
        }

        private bool CheckMatch(string sURI, ResponderRule oCandidate)
        {
            if (!oCandidate.IsEnabled)
            {
                return false;
            }
            string sMatch = oCandidate.sMatch;
            if ((sMatch.Length > 6) && sMatch.StartsWith("REGEX:", StringComparison.OrdinalIgnoreCase))
            {
                string pattern = sMatch.Substring(6);
                try
                {
                    Regex regex = new Regex(pattern);
                    if (regex.Match(sURI).Success)
                    {
                        return true;
                    }
                }
                catch
                {
                }
                return false;
            }
            if ((sMatch.Length > 6) && sMatch.StartsWith("EXACT:", StringComparison.OrdinalIgnoreCase))
            {
                return sMatch.Substring(6).Equals(sURI, StringComparison.Ordinal);
            }
            if ((sMatch.Length > 4) && sMatch.StartsWith("NOT:", StringComparison.OrdinalIgnoreCase))
            {
                string str4 = sMatch.Substring(4);
                return (sURI.IndexOf(str4, StringComparison.OrdinalIgnoreCase) < 0);
            }
            return ((sMatch == "*") || (-1 < sURI.IndexOf(sMatch, StringComparison.OrdinalIgnoreCase)));
        }

        internal void ClearActionsFromUI()
        {
            this.oAutoResponderUI.cbxRuleAction.Items.Clear();
        }

        public void ClearRules()
        {
            try
            {
                this._RWLockRules.AcquireWriterLock(-1);
                this._alRules.Clear();
            }
            finally
            {
                this._RWLockRules.ReleaseWriterLock();
            }
            this.oAutoResponderUI.lvRespondRules.Items.Clear();
            this._bRuleListIsDirty = true;
        }

        internal bool CreateRuleForFile(string sFilename, string sRelativeTo)
        {
            if ((sFilename == null) || !File.Exists(sFilename))
            {
                return false;
            }
            try
            {
                string fileName = null;
                if (string.IsNullOrEmpty(sRelativeTo))
                {
                    fileName = Path.GetFileName(sFilename);
                }
                else
                {
                    fileName = sFilename.Substring(sRelativeTo.Length).Replace('\\', '/');
                }
                fileName = HttpUtility.UrlPathEncode(fileName);
                string sRule = "REGEX:(?insx).*" + Utilities.RegExEscape(fileName, false, true);
                string sAction = sFilename;
                this.AddRule(sRule, sAction, true);
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        internal void CreateRulesForFolder(string sFolderName)
        {
            DirectoryInfo info = new DirectoryInfo(sFolderName);
            foreach (FileInfo info2 in info.GetFiles("*", SearchOption.AllDirectories))
            {
                this.CreateRuleForFile(info2.FullName, info.Parent.FullName);
            }
        }

        private void CreateViewItem(ResponderRule oRule)
        {
            if (oRule != null)
            {
                ListViewItem item = this.oAutoResponderUI.lvRespondRules.Items.Add(oRule.sMatch);
                item.SubItems.Add(oRule.sAction);
                item.SubItems.Add(oRule.iLatency.ToString());
                item.Checked = oRule.IsEnabled;
                item.Tag = oRule;
                oRule.ViewItem = item;
            }
        }

        internal bool DemoteRule(ResponderRule oRule)
        {
            bool flag;
            try
            {
                this._RWLockRules.AcquireWriterLock(-1);
                int index = this._alRules.IndexOf(oRule);
                if ((index > -1) && (index < (this._alRules.Count - 1)))
                {
                    this._alRules.Reverse(index, 2);
                    this._bRuleListIsDirty = true;
                    return true;
                }
                flag = false;
            }
            finally
            {
                this._RWLockRules.ReleaseWriterLock();
            }
            return flag;
        }

        private void DoDelay(ResponderRule oMatch)
        {
            if (FiddlerApplication.oAutoResponder.UseLatency && (oMatch.iLatency > 0))
            {
                Thread.Sleep(oMatch.iLatency);
            }
        }

        internal void DoMatchAfterRequestTampering(Session oSession)
        {
            try
            {
                this._RWLockRules.AcquireReaderLock(-1);
                foreach (ResponderRule rule in this._alRules)
                {
                    if ((!(string.Empty == rule.sAction) && !rule.sAction.Equals("*bpu", StringComparison.OrdinalIgnoreCase)) && (this.CheckMatch(oSession.fullUrl, rule) && this.HandleMatch(oSession, rule)))
                    {
                        return;
                    }
                }
            }
            finally
            {
                this._RWLockRules.ReleaseReaderLock();
            }
            if (!this._bPermitFallthrough)
            {
                oSession["ui-backcolor"] = "Lavender";
                oSession.SetBitFlag(SessionFlags.ResponseGeneratedByFiddler, true);
                if (!oSession.HTTPMethodIs("CONNECT"))
                {
                    if (oSession.state < SessionStates.SendingRequest)
                    {
                        oSession.utilCreateResponseAndBypassServer();
                    }
                    else
                    {
                        oSession.bBufferResponse = true;
                        oSession.responseBodyBytes = new byte[0];
                    }
                    oSession.state = SessionStates.ReadingResponse;
                    if (oSession.oRequest.headers.Exists("If-Modified-Since") || oSession.oRequest.headers.Exists("If-None-Match"))
                    {
                        oSession.responseCode = 0x130;
                    }
                    else
                    {
                        oSession.responseCode = 0x194;
                        oSession.oResponse["Cache-Control"] = "max-age=0, must-revalidate";
                        if (!oSession.HTTPMethodIs("HEAD"))
                        {
                            oSession.utilSetResponseBody("[Fiddler] No matching AutoResponder rule and fallthrough not permitted.".PadRight(0x200, ' '));
                        }
                    }
                }
                else
                {
                    oSession.oFlags["x-replywithtunnel"] = "AutoResponderWithNoFallthrough";
                }
            }
        }

        internal void DoMatchBeforeRequestTampering(Session oSession)
        {
            try
            {
                this._RWLockRules.AcquireReaderLock(-1);
                foreach (ResponderRule rule in this._alRules)
                {
                    if (rule.sAction.Equals("*bpu", StringComparison.OrdinalIgnoreCase) && this.CheckMatch(oSession.fullUrl, rule))
                    {
                        oSession["x-breakrequest"] = "AutoResponder";
                        return;
                    }
                }
            }
            finally
            {
                this._RWLockRules.ReleaseReaderLock();
            }
        }

        internal bool ExportFARX(string sFilename)
        {
            return this.SaveRules(sFilename);
        }

        private bool HandleMatch(Session oSession, ResponderRule oMatch)
        {
            bool flag = oSession.HTTPMethodIs("CONNECT");
            if (oMatch.sAction.StartsWith("*"))
            {
                if (oMatch.sAction.Equals("*drop", StringComparison.OrdinalIgnoreCase))
                {
                    this.DoDelay(oMatch);
                    if ((oSession.oRequest != null) && (oSession.oRequest.pipeClient != null))
                    {
                        oSession.oRequest.pipeClient.End();
                    }
                    oSession.utilCreateResponseAndBypassServer();
                    oSession.oResponse.headers.HTTPResponseCode = 0;
                    oSession.oResponse.headers.HTTPResponseStatus = "0 Aborted";
                    oSession.state = SessionStates.Aborted;
                    return true;
                }
                if (oMatch.sAction.StartsWith("*delay:", StringComparison.OrdinalIgnoreCase))
                {
                    int result = 0;
                    if (int.TryParse(Utilities.TrimBefore(oMatch.sAction, ':'), out result))
                    {
                        Thread.Sleep(result);
                    }
                    return false;
                }
                if (oMatch.sAction.Equals("*bpafter", StringComparison.OrdinalIgnoreCase))
                {
                    oSession["x-breakresponse"] = "AutoResponder";
                    oSession.bBufferResponse = true;
                    return false;
                }
                if (oMatch.sAction.StartsWith("*redir:", StringComparison.OrdinalIgnoreCase) && !flag)
                {
                    this.DoDelay(oMatch);
                    oSession.utilCreateResponseAndBypassServer();
                    oSession.oResponse.headers.HTTPResponseCode = 0x133;
                    oSession.oResponse.headers.HTTPResponseStatus = "307 AutoRedir";
                    oSession.oResponse.headers["Location"] = oMatch.sAction.Substring(7);
                    oSession.oResponse.headers["Cache-Control"] = "max-age=0, must-revalidate";
                    return true;
                }
                if (oMatch.sAction.Equals("*exit", StringComparison.OrdinalIgnoreCase))
                {
                    this.DoDelay(oMatch);
                    return true;
                }
            }
            if (oMatch.HasImportedResponse && !flag)
            {
                if (oSession.state < SessionStates.SendingRequest)
                {
                    oSession.utilCreateResponseAndBypassServer();
                }
                else
                {
                    FiddlerApplication.Log.LogFormat("fiddler.autoresponder.error> AutoResponder will not respond to a request which is already in-flight; Session #{0} is at state: {1}", new object[] { oSession.id, oSession.state });
                    return true;
                }
                if ((oMatch._arrResponseBodyBytes == null) || (oMatch._oResponseHeaders == null))
                {
                    FiddlerApplication.Log.LogString("fiddler.autoresponder.error> Response data from imported session is missing.");
                    return true;
                }
                this.DoDelay(oMatch);
                if (oSession.HTTPMethodIs("HEAD"))
                {
                    oSession.responseBodyBytes = new byte[0];
                }
                else
                {
                    oSession.responseBodyBytes = oMatch._arrResponseBodyBytes;
                }
                oSession.oResponse.headers = (HTTPResponseHeaders) oMatch._oResponseHeaders.Clone();
                oSession.state = SessionStates.AutoTamperResponseBefore;
                oSession["x-AutoResponder"] = "Matched: " + oMatch.sMatch + ", sent: " + oMatch.sAction;
                oSession["ui-backcolor"] = "Lavender";
                return true;
            }
            if (!flag && (oMatch.sAction.StartsWith("http://", StringComparison.OrdinalIgnoreCase) || oMatch.sAction.StartsWith("https://", StringComparison.OrdinalIgnoreCase)))
            {
                this.DoDelay(oMatch);
                oSession.oFlags["X-OriginalURL"] = oSession.fullUrl;
                oSession.fullUrl = oMatch.sAction;
                return true;
            }
            this.DoDelay(oMatch);
            oSession["x-replywithfile"] = oMatch.sAction;
            oSession["ui-backcolor"] = "Lavender";
            return true;
        }

        internal bool ImportFARX(string sFilename)
        {
            return this.LoadRules(sFilename, false);
        }

        public bool ImportSAZ(string sFilename)
        {
            Session[] oSessions = Utilities.ReadSessionArchive(sFilename, true);
            if (oSessions == null)
            {
                return false;
            }
            if (sFilename.StartsWith(CONFIG.GetPath("Captures"), StringComparison.OrdinalIgnoreCase))
            {
                sFilename = sFilename.Substring(CONFIG.GetPath("Captures").Length);
            }
            else
            {
                sFilename = Utilities.CollapsePath(sFilename);
            }
            return this.ImportSessions(oSessions, sFilename);
        }

        public bool ImportSessions(Session[] oSessions)
        {
            return this.ImportSessions(oSessions, null);
        }

        private bool ImportSessions(Session[] oSessions, string sAnnotation)
        {
            if (oSessions == null)
            {
                return false;
            }
            foreach (Session session in oSessions)
            {
                if ((!session.HTTPMethodIs("CONNECT") && session.bHasResponse) && (session.oResponse != null))
                {
                    string sRule = "EXACT:" + session.fullUrl;
                    string sDescription = string.Format("*{0}-{1}", session.responseCode, (sAnnotation == null) ? ("SESSION_" + session.id.ToString()) : (sAnnotation + "#" + session.oFlags["x-LoadedFrom"]));
                    int iLatencyMS = 0;
                    if (session.Timers != null)
                    {
                        TimeSpan span = (TimeSpan) (session.Timers.ServerBeginResponse - session.Timers.ClientDoneRequest);
                        iLatencyMS = (int) span.TotalMilliseconds;
                    }
                    byte[] arrResponseBody = (byte[]) session.responseBodyBytes.Clone();
                    HTTPResponseHeaders oRH = (HTTPResponseHeaders) session.oResponse.headers.Clone();
                    this.AddRule(sRule, oRH, arrResponseBody, sDescription, iLatencyMS, true);
                }
            }
            this._bRuleListIsDirty = true;
            return true;
        }

        internal void LoadRules()
        {
            this.LoadRules(CONFIG.GetPath("AutoResponderDefaultRules"), true);
        }

        public bool LoadRules(string sFilename)
        {
            return this.LoadRules(sFilename, true);
        }

        public bool LoadRules(string sFilename, bool bIsDefaultRuleFile)
        {
            if (bIsDefaultRuleFile)
            {
                this.ClearRules();
            }
            try
            {
                if (!File.Exists(sFilename) || (new FileInfo(sFilename).Length < 0x8fL))
                {
                    return false;
                }
                FileStream input = new FileStream(sFilename, FileMode.Open, FileAccess.Read, FileShare.Read);
                XmlTextReader reader = new XmlTextReader(input);
                while (reader.Read())
                {
                    string str6;
                    if ((reader.NodeType == XmlNodeType.Element) && ((str6 = reader.Name) != null))
                    {
                        if (!(str6 == "State"))
                        {
                            if (str6 == "ResponseRule")
                            {
                                goto Label_00B8;
                            }
                        }
                        else if (bIsDefaultRuleFile)
                        {
                            this.IsEnabled = XmlConvert.ToBoolean(reader.GetAttribute("Enabled"));
                            this.PermitFallthrough = XmlConvert.ToBoolean(reader.GetAttribute("Fallthrough"));
                        }
                    }
                    continue;
                Label_00B8:
                    try
                    {
                        string attribute = reader.GetAttribute("Match");
                        string sAction = reader.GetAttribute("Action");
                        int iLatencyMS = 0;
                        string s = reader.GetAttribute("Latency");
                        if (s != null)
                        {
                            iLatencyMS = XmlConvert.ToInt32(s);
                        }
                        bool bIsEnabled = "false" != reader.GetAttribute("Enabled");
                        string str4 = reader.GetAttribute("Headers");
                        if (string.IsNullOrEmpty(str4))
                        {
                            this.AddRule(attribute, sAction, bIsEnabled);
                        }
                        else
                        {
                            byte[] buffer;
                            HTTPResponseHeaders oRH = new HTTPResponseHeaders();
                            str4 = Encoding.UTF8.GetString(Convert.FromBase64String(str4));
                            oRH.AssignFromString(str4);
                            string str5 = reader.GetAttribute("DeflatedBody");
                            if (!string.IsNullOrEmpty(str5))
                            {
                                buffer = Utilities.DeflaterExpand(Convert.FromBase64String(str5));
                            }
                            else
                            {
                                str5 = reader.GetAttribute("Body");
                                if (!string.IsNullOrEmpty(str5))
                                {
                                    buffer = Convert.FromBase64String(str5);
                                }
                                else
                                {
                                    buffer = new byte[0];
                                }
                            }
                            this.AddRule(attribute, oRH, buffer, sAction, iLatencyMS, bIsEnabled);
                        }
                        continue;
                    }
                    catch
                    {
                        continue;
                    }
                }
                reader.Close();
                if (bIsDefaultRuleFile && (this._alRules.Count < 1))
                {
                    this.IsEnabled = false;
                }
                if (bIsDefaultRuleFile)
                {
                    this._bRuleListIsDirty = false;
                }
                return true;
            }
            catch (Exception exception)
            {
                FiddlerApplication.ReportException(exception, "Failed to load AutoResponder settings from " + sFilename);
                if (bIsDefaultRuleFile)
                {
                    this.IsEnabled = false;
                }
                return false;
            }
        }

        internal bool PromoteRule(ResponderRule oRule)
        {
            bool flag;
            try
            {
                this._RWLockRules.AcquireWriterLock(-1);
                int index = this._alRules.IndexOf(oRule);
                if (index > 0)
                {
                    this._alRules.Reverse(index - 1, 2);
                    this._bRuleListIsDirty = true;
                    return true;
                }
                flag = false;
            }
            finally
            {
                this._RWLockRules.ReleaseWriterLock();
            }
            return flag;
        }

        public bool RemoveRule(ResponderRule oRule)
        {
            try
            {
                try
                {
                    this._RWLockRules.AcquireWriterLock(-1);
                    this._alRules.Remove(oRule);
                }
                finally
                {
                    this._RWLockRules.ReleaseWriterLock();
                }
                this._bRuleListIsDirty = true;
                if (oRule.ViewItem != null)
                {
                    oRule.ViewItem.Remove();
                    oRule.ViewItem = null;
                }
                if (oRule._oEditor != null)
                {
                    oRule._oEditor.Dispose();
                    oRule._oEditor = null;
                }
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        internal void SaveDefaultRules()
        {
            if (this._bRuleListIsDirty)
            {
                this.SaveRules(CONFIG.GetPath("AutoResponderDefaultRules"));
                this._bRuleListIsDirty = false;
            }
        }

        public bool SaveRules(string sFilename)
        {
            try
            {
                Utilities.EnsureOverwritable(sFilename);
                XmlTextWriter writer = new XmlTextWriter(sFilename, Encoding.UTF8) {
                    Formatting = Formatting.Indented
                };
                writer.WriteStartDocument();
                writer.WriteStartElement("AutoResponder");
                writer.WriteAttributeString("LastSave", XmlConvert.ToString(DateTime.Now, XmlDateTimeSerializationMode.RoundtripKind));
                writer.WriteAttributeString("FiddlerVersion", Application.ProductVersion);
                writer.WriteStartElement("State");
                writer.WriteAttributeString("Enabled", XmlConvert.ToString(this._bEnabled));
                writer.WriteAttributeString("Fallthrough", XmlConvert.ToString(this._bPermitFallthrough));
                try
                {
                    this._RWLockRules.AcquireReaderLock(-1);
                    foreach (ResponderRule rule in this._alRules)
                    {
                        writer.WriteStartElement("ResponseRule");
                        writer.WriteAttributeString("Match", rule.sMatch);
                        writer.WriteAttributeString("Action", rule.sAction);
                        if (rule.iLatency > 0)
                        {
                            writer.WriteAttributeString("Latency", rule.iLatency.ToString());
                        }
                        writer.WriteAttributeString("Enabled", XmlConvert.ToString(rule.IsEnabled));
                        if (rule.HasImportedResponse)
                        {
                            byte[] buffer = rule._oResponseHeaders.ToByteArray(true, true);
                            writer.WriteStartAttribute("Headers");
                            writer.WriteBase64(buffer, 0, buffer.Length);
                            writer.WriteEndAttribute();
                            byte[] writeData = rule._arrResponseBodyBytes;
                            if ((writeData != null) && (writeData.Length > 0))
                            {
                                if (writeData.Length > 0x800)
                                {
                                    byte[] buffer3 = Utilities.DeflaterCompress(writeData);
                                    if (buffer3.Length < (0.9 * writeData.Length))
                                    {
                                        writer.WriteStartAttribute("DeflatedBody");
                                        writer.WriteBase64(buffer3, 0, buffer3.Length);
                                        writer.WriteEndAttribute();
                                        writeData = null;
                                    }
                                }
                                if (writeData != null)
                                {
                                    writer.WriteStartAttribute("Body");
                                    writer.WriteBase64(writeData, 0, writeData.Length);
                                    writer.WriteEndAttribute();
                                }
                            }
                        }
                        writer.WriteEndElement();
                    }
                }
                finally
                {
                    this._RWLockRules.ReleaseReaderLock();
                }
                writer.WriteEndElement();
                writer.WriteEndElement();
                writer.WriteEndDocument();
                writer.Close();
                return true;
            }
            catch (Exception exception)
            {
                FiddlerApplication.ReportException(exception, "Failed to save AutoResponder Rules");
                return false;
            }
        }

        public override string ToString()
        {
            StringBuilder builder = new StringBuilder();
            try
            {
                this._RWLockRules.AcquireReaderLock(-1);
                builder.AppendFormat("The AutoResponder list contains {0} rules.\r\n", this._alRules.Count);
                foreach (ResponderRule rule in this._alRules)
                {
                    builder.AppendFormat("\t{0}\t->\t{1}\r\n", rule.sMatch, rule.sAction);
                }
            }
            finally
            {
                this._RWLockRules.ReleaseReaderLock();
            }
            return builder.ToString();
        }

        public bool IsEnabled
        {
            get
            {
                return this._bEnabled;
            }
            set
            {
                this.oAutoResponderUI.cbAutoRespond.Checked = this._bEnabled = value;
                this._bRuleListIsDirty = true;
            }
        }

        public bool IsRuleListDirty
        {
            get
            {
                return this._bRuleListIsDirty;
            }
            set
            {
                this._bRuleListIsDirty = value;
            }
        }

        public bool PermitFallthrough
        {
            get
            {
                return this._bPermitFallthrough;
            }
            set
            {
                this.oAutoResponderUI.cbRespondPassthrough.Checked = this._bPermitFallthrough = value;
                this._bRuleListIsDirty = true;
            }
        }

        public bool UseLatency { get; set; }
    }
}

