namespace Fiddler
{
    using Microsoft.JScript.Vsa;
    using Microsoft.Vsa;
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.IO;
    using System.Reflection;
    using System.Text;
    using System.Threading;
    using System.Windows.Forms;

    public class FiddlerScript : IDisposable
    {
        private IVsaEngine _engine;
        private MethodInfo _methodExecAction;
        private MethodInfo _methodOnBeforeRequest;
        private MethodInfo _methodOnBeforeResponse;
        private MethodInfo _methodOnPeekAtRequestHeaders;
        private MethodInfo _methodOnPeekAtResponseHeaders;
        private MethodInfo _methodOnReturningError;
        private static int _scriptCount = 0;
        private System.Type _typeScriptHandlers;
        private RulesAfterCompileHandler _AfterRulesCompile;
        private RulesBeforeCompileHandler _BeforeRulesCompile;
        private static Hashtable htMenuScripts = new Hashtable();
        private static int lastScriptLoadTickCount = 0;
        private List<PreferenceBag.PrefWatcher> listWeaklyHeldWatchers = new List<PreferenceBag.PrefWatcher>();
        private ScriptEngineSite objVSASite;
        private bool Ready;
        private RulesCompileFailedHandler _RulesCompileFailed;
        private FileSystemWatcher scriptsFolderWatcher;

        public event RulesAfterCompileHandler AfterRulesCompile
        {
            add
            {
                RulesAfterCompileHandler handler2;
                RulesAfterCompileHandler afterRulesCompile = this._AfterRulesCompile;
                do
                {
                    handler2 = afterRulesCompile;
                    RulesAfterCompileHandler handler3 = (RulesAfterCompileHandler) Delegate.Combine(handler2, value);
                    afterRulesCompile = Interlocked.CompareExchange<RulesAfterCompileHandler>(ref this._AfterRulesCompile, handler3, handler2);
                }
                while (afterRulesCompile != handler2);
            }
            remove
            {
                RulesAfterCompileHandler handler2;
                RulesAfterCompileHandler afterRulesCompile = this._AfterRulesCompile;
                do
                {
                    handler2 = afterRulesCompile;
                    RulesAfterCompileHandler handler3 = (RulesAfterCompileHandler) Delegate.Remove(handler2, value);
                    afterRulesCompile = Interlocked.CompareExchange<RulesAfterCompileHandler>(ref this._AfterRulesCompile, handler3, handler2);
                }
                while (afterRulesCompile != handler2);
            }
        }

        public event RulesBeforeCompileHandler BeforeRulesCompile
        {
            add
            {
                RulesBeforeCompileHandler handler2;
                RulesBeforeCompileHandler beforeRulesCompile = this._BeforeRulesCompile;
                do
                {
                    handler2 = beforeRulesCompile;
                    RulesBeforeCompileHandler handler3 = (RulesBeforeCompileHandler) Delegate.Combine(handler2, value);
                    beforeRulesCompile = Interlocked.CompareExchange<RulesBeforeCompileHandler>(ref this._BeforeRulesCompile, handler3, handler2);
                }
                while (beforeRulesCompile != handler2);
            }
            remove
            {
                RulesBeforeCompileHandler handler2;
                RulesBeforeCompileHandler beforeRulesCompile = this._BeforeRulesCompile;
                do
                {
                    handler2 = beforeRulesCompile;
                    RulesBeforeCompileHandler handler3 = (RulesBeforeCompileHandler) Delegate.Remove(handler2, value);
                    beforeRulesCompile = Interlocked.CompareExchange<RulesBeforeCompileHandler>(ref this._BeforeRulesCompile, handler3, handler2);
                }
                while (beforeRulesCompile != handler2);
            }
        }

        public event RulesCompileFailedHandler RulesCompileFailed
        {
            add
            {
                RulesCompileFailedHandler handler2;
                RulesCompileFailedHandler rulesCompileFailed = this._RulesCompileFailed;
                do
                {
                    handler2 = rulesCompileFailed;
                    RulesCompileFailedHandler handler3 = (RulesCompileFailedHandler) Delegate.Combine(handler2, value);
                    rulesCompileFailed = Interlocked.CompareExchange<RulesCompileFailedHandler>(ref this._RulesCompileFailed, handler3, handler2);
                }
                while (rulesCompileFailed != handler2);
            }
            remove
            {
                RulesCompileFailedHandler handler2;
                RulesCompileFailedHandler rulesCompileFailed = this._RulesCompileFailed;
                do
                {
                    handler2 = rulesCompileFailed;
                    RulesCompileFailedHandler handler3 = (RulesCompileFailedHandler) Delegate.Remove(handler2, value);
                    rulesCompileFailed = Interlocked.CompareExchange<RulesCompileFailedHandler>(ref this._RulesCompileFailed, handler3, handler2);
                }
                while (rulesCompileFailed != handler2);
            }
        }

        public FiddlerScript()
        {
            this.objVSASite = new ScriptEngineSite(this);
            this.scriptsFolderWatcher = new FileSystemWatcher();
            this.scriptsFolderWatcher.Filter = "CustomRules.js";
            this.scriptsFolderWatcher.NotifyFilter = NotifyFilters.LastWrite;
            this.scriptsFolderWatcher.SynchronizingObject = FiddlerApplication.UI;
            this.scriptsFolderWatcher.Deleted += new FileSystemEventHandler(this.scriptsFolderWatcher_Notify);
            this.scriptsFolderWatcher.Created += new FileSystemEventHandler(this.scriptsFolderWatcher_Notify);
            this.scriptsFolderWatcher.Changed += new FileSystemEventHandler(this.scriptsFolderWatcher_Notify);
            try
            {
                this.scriptsFolderWatcher.Path = CONFIG.GetPath("Scripts");
                this.scriptsFolderWatcher.EnableRaisingEvents = true;
            }
            catch (Exception exception)
            {
                FiddlerApplication.ReportException(exception, "ScriptWatcher Failure");
            }
        }

        private void _ClearCachedMethodPointers()
        {
            this._methodOnBeforeRequest = this._methodOnBeforeResponse = this._methodOnPeekAtRequestHeaders = this._methodOnPeekAtResponseHeaders = this._methodOnReturningError = (MethodInfo) (this._methodExecAction = null);
        }

        private void _ExtractMethodPointersFromScript()
        {
            this._methodOnBeforeRequest = this._typeScriptHandlers.GetMethod("OnBeforeRequest");
            this._methodOnBeforeResponse = this._typeScriptHandlers.GetMethod("OnBeforeResponse");
            this._methodOnReturningError = this._typeScriptHandlers.GetMethod("OnReturningError");
            this._methodOnPeekAtResponseHeaders = this._typeScriptHandlers.GetMethod("OnPeekAtResponseHeaders");
            this._methodOnPeekAtRequestHeaders = this._typeScriptHandlers.GetMethod("OnPeekAtRequestHeaders");
            this._methodExecAction = this._typeScriptHandlers.GetMethod("OnExecAction");
        }

        private bool _LoadScript(string sScriptFilename)
        {
            if (!CONFIG.bLoadScript)
            {
                return false;
            }
            if (string.IsNullOrEmpty(sScriptFilename))
            {
                return false;
            }
            if (!File.Exists(sScriptFilename))
            {
                return false;
            }
            _scriptCount++;
            if (this._BeforeRulesCompile != null)
            {
                this._BeforeRulesCompile(sScriptFilename);
            }
            foreach (object obj2 in htMenuScripts.Keys)
            {
                ((MenuItem) obj2).Parent.MenuItems.RemoveAt(((MenuItem) obj2).Index);
            }
            htMenuScripts.Clear();
            foreach (PreferenceBag.PrefWatcher watcher in this.listWeaklyHeldWatchers)
            {
                FiddlerApplication.Prefs.RemoveWatcher(watcher);
            }
            this.listWeaklyHeldWatchers.Clear();
            StreamReader reader = new StreamReader(new FileStream(sScriptFilename, FileMode.Open, FileAccess.Read, FileShare.ReadWrite), Encoding.UTF8, true);
            string str = reader.ReadToEnd();
            reader.Close();
            try
            {
                this._engine = new VsaEngine();
                this._engine.RootMoniker = "fiddler://script/" + _scriptCount;
                this._engine.Site = this.objVSASite;
                this._engine.InitNew();
                this._engine.RootNamespace = "Fiddler.ScriptNamespace";
                this._engine.GenerateDebugInfo = false;
            }
            catch (EntryPointNotFoundException)
            {
                FiddlerApplication.DoNotifyUser("Unable to initialize FiddlerScript. This typically indicates that you are attempting to run Fiddler on .NET Framework v4.\n\nYour machine may have the unsupported OnlyUseLatestCLR registry key set.", "Unsupported configuration", MessageBoxIcon.Hand);
                this.Ready = false;
                return false;
            }
            IVsaItems items = this._engine.Items;
            foreach (string str2 in CONFIG.sScriptReferences.Split(new char[] { ';' }))
            {
                if (str2.Trim().Length > 0)
                {
                    IVsaReferenceItem item = (IVsaReferenceItem) items.CreateItem(str2, VsaItemType.Reference, VsaItemFlag.None);
                    item.AssemblyName = str2;
                }
            }
            IVsaGlobalItem item2 = (IVsaGlobalItem) items.CreateItem("FiddlerObject", VsaItemType.AppGlobal, VsaItemFlag.None);
            item2.TypeString = "Fiddler.FiddlerScript";
            item2 = (IVsaGlobalItem) items.CreateItem("FiddlerScript", VsaItemType.AppGlobal, VsaItemFlag.None);
            item2.TypeString = "Fiddler.FiddlerScript";
            IVsaCodeItem item3 = (IVsaCodeItem) items.CreateItem("Handlers", VsaItemType.Code, VsaItemFlag.None);
            item3.SourceText = str;
            if (!this._engine.Compile())
            {
                this.Ready = false;
                return false;
            }
            this._engine.Run();
            this.Ready = true;
            this._typeScriptHandlers = this._engine.Assembly.GetType("Fiddler.ScriptNamespace.Handlers");
            if (this._typeScriptHandlers == null)
            {
                this._ClearCachedMethodPointers();
            }
            else
            {
                this._ExtractMethodPointersFromScript();
                foreach (FieldInfo info in this._typeScriptHandlers.GetFields(BindingFlags.Public | BindingFlags.Static))
                {
                    if (info.FieldType == typeof(bool))
                    {
                        RulesOption customAttribute = (RulesOption) Attribute.GetCustomAttribute(info, typeof(RulesOption));
                        if (customAttribute != null)
                        {
                            this.CreateRulesMenuItem(info, customAttribute);
                        }
                    }
                    else if (info.FieldType == typeof(string))
                    {
                        RulesString oRule = (RulesString) Attribute.GetCustomAttribute(info, typeof(RulesString));
                        if (oRule != null)
                        {
                            RulesStringValue[] customAttributes = (RulesStringValue[]) Attribute.GetCustomAttributes(info, typeof(RulesStringValue));
                            if ((customAttributes != null) && (customAttributes.Length > 0))
                            {
                                Array.Sort<RulesStringValue>(customAttributes);
                                this.CreateRulesMenuForStrings(info, oRule, customAttributes);
                            }
                        }
                    }
                }
                bool flag = true;
                foreach (MethodInfo info2 in this._typeScriptHandlers.GetMethods())
                {
                    MenuItem item4;
                    ToolsAction action = (ToolsAction) Attribute.GetCustomAttribute(info2, typeof(ToolsAction));
                    if (action != null)
                    {
                        item4 = new MenuItem(action.Name);
                        htMenuScripts.Add(item4, info2);
                        item4.Click += new EventHandler(this.HandleScriptToolsClick);
                        FiddlerApplication._frmMain.mnuTools.MenuItems.Add(item4);
                    }
                    ContextAction action2 = (ContextAction) Attribute.GetCustomAttribute(info2, typeof(ContextAction));
                    if (action2 != null)
                    {
                        if (flag)
                        {
                            item4 = new MenuItem("-");
                            htMenuScripts.Add(item4, null);
                            FiddlerApplication._frmMain.mnuSessionContext.MenuItems.Add(0, item4);
                            flag = false;
                        }
                        item4 = new MenuItem(action2.Name);
                        htMenuScripts.Add(item4, info2);
                        item4.Click += new EventHandler(this.HandleScriptToolsClick);
                        FiddlerApplication._frmMain.mnuSessionContext.MenuItems.Add(0, item4);
                    }
                    BindUIColumn column = (BindUIColumn) Attribute.GetCustomAttribute(info2, typeof(BindUIColumn));
                    if (column != null)
                    {
                        getColumnStringDelegate delFn = (getColumnStringDelegate) Delegate.CreateDelegate(typeof(getColumnStringDelegate), info2);
                        FiddlerApplication._frmMain.lvSessions.AddBoundColumn(column._colName, column._iColWidth, delFn);
                    }
                    QuickLinkMenu menu = (QuickLinkMenu) Attribute.GetCustomAttribute(info2, typeof(QuickLinkMenu));
                    if (menu != null)
                    {
                        QuickLinkItem[] array = (QuickLinkItem[]) Attribute.GetCustomAttributes(info2, typeof(QuickLinkItem));
                        if ((array != null) && (array.Length > 0))
                        {
                            Array.Sort<QuickLinkItem>(array);
                            this.CreateQuickLinkMenu(menu.Name, info2, menu.Name, array);
                        }
                    }
                }
                MenuExt.ReadRegistry(FiddlerApplication._frmMain.mnuTools, htMenuScripts);
                try
                {
                    MethodInfo method = this._typeScriptHandlers.GetMethod("Main");
                    if (method != null)
                    {
                        method.Invoke(null, null);
                    }
                }
                catch (Exception exception)
                {
                    FiddlerApplication.DoNotifyUser(string.Concat(new object[] { "There was a problem with your FiddlerScript.\n\n", exception.Message, "\n", exception.StackTrace, "\n\n", exception.InnerException }), "JScript main() failed.");
                }
            }
            if (this._AfterRulesCompile != null)
            {
                this._AfterRulesCompile();
            }
            return true;
        }

        private void _NotifyUserOfError(string sTitle, Exception eX)
        {
            FiddlerApplication.DoNotifyUser(string.Concat(new object[] { "There was a problem with your FiddlerScript.\n\n", eX.Message, "\n", eX.StackTrace, "\n\n", eX.InnerException }), sTitle);
        }

        [CodeDescription("Show a simple dialog with string sMessage.")]
        public void alert(string sMessage)
        {
            MessageBox.Show(sMessage, "FiddlerScript", MessageBoxButtons.OK, MessageBoxIcon.Asterisk);
        }

        public void ClearScript()
        {
            this.Ready = false;
            this._ClearCachedMethodPointers();
            if (this._engine != null)
            {
                try
                {
                    this._engine.Close();
                }
                catch (Exception)
                {
                }
                this._engine = null;
            }
        }

        [CodeDescription("Creates a simple Dictionary<string, object>, used to pass option strings into the DoImport or DoExport methods on the FiddlerApplication object.")]
        public Dictionary<string, object> createDictionary()
        {
            return new Dictionary<string, object>();
        }

        private void CreateQuickLinkMenu(string sMenuText, MethodInfo oMethod, string p, QuickLinkItem[] oValues)
        {
            MenuItem key = new MenuItem(sMenuText);
            htMenuScripts.Add(key, null);
            FiddlerApplication.UI.Menu.MenuItems.Add(key);
            foreach (QuickLinkItem item3 in oValues)
            {
                MenuItem item2 = new MenuItem(item3.MenuText) {
                    Tag = item3.Action
                };
                item2.Click += new EventHandler(this.HandleScriptQuickLinkClick);
                htMenuScripts.Add(item2, oMethod);
                key.MenuItems.Add(item2);
            }
        }

        private void CreateRulesMenuForStrings(FieldInfo oField, RulesString oRule, RulesStringValue[] oValues)
        {
            MenuItem item2;
            MenuItem key = new MenuItem(oRule.Name);
            htMenuScripts.Add(key, null);
            FiddlerApplication._frmMain.mnuRules.MenuItems.Add(key);
            bool flag = false;
            foreach (RulesStringValue value2 in oValues)
            {
                item2 = new MenuItem(value2.MenuText) {
                    Tag = value2.Value,
                    RadioCheck = true
                };
                if (value2.IsDefault)
                {
                    flag = true;
                    item2.Checked = true;
                    if (value2.Value != "%CUSTOM%")
                    {
                        oField.SetValue(null, value2.Value);
                    }
                }
                item2.Click += new EventHandler(this.HandleScriptRulesStringClick);
                htMenuScripts.Add(item2, oField);
                key.MenuItems.Add(item2);
            }
            if (oRule.ShowDisabledOption)
            {
                htMenuScripts.Add(key.MenuItems.Add("-"), null);
                item2 = new MenuItem("Disabled") {
                    Tag = null,
                    RadioCheck = true
                };
                if (!flag)
                {
                    item2.Checked = true;
                }
                htMenuScripts.Add(item2, oField);
                item2.Click += new EventHandler(this.HandleScriptRulesStringClick);
                key.MenuItems.Add(item2);
            }
        }

        private void CreateRulesMenuItem(FieldInfo oField, RulesOption oRule)
        {
            MenuItem key = new MenuItem(oRule.Name);
            htMenuScripts.Add(key, oField);
            key.Click += new EventHandler(this.HandleScriptRulesClick);
            if ((bool) oField.GetValue(null))
            {
                key.Checked = true;
            }
            if (oRule.SubMenu == null)
            {
                FiddlerApplication._frmMain.mnuRules.MenuItems.Add(key);
            }
            else
            {
                if (oRule.IsExclusive)
                {
                    key.RadioCheck = true;
                }
                MenuItem item2 = this.FindSubMenu(FiddlerApplication._frmMain.mnuRules.MenuItems, oRule.SubMenu);
                if (item2 == null)
                {
                    item2 = FiddlerApplication._frmMain.mnuRules.MenuItems.Add(oRule.SubMenu);
                    htMenuScripts.Add(item2, null);
                }
                item2.MenuItems.Add(key);
                if (oRule.followingSplitter)
                {
                    htMenuScripts.Add(item2.MenuItems.Add("-"), null);
                }
            }
        }

        internal void DoBeforeRequest(Session oSession)
        {
            if (this.Ready && (this._methodOnBeforeRequest != null))
            {
                try
                {
                    this._methodOnBeforeRequest.Invoke(null, new object[] { oSession });
                }
                catch (Exception exception)
                {
                    this._NotifyUserOfError("FiddlerScript OnBeforeRequest() failed.", exception);
                }
            }
        }

        internal void DoBeforeResponse(Session oSession)
        {
            if (this.Ready && (this._methodOnBeforeResponse != null))
            {
                try
                {
                    this._methodOnBeforeResponse.Invoke(null, new object[] { oSession });
                }
                catch (Exception exception)
                {
                    this._NotifyUserOfError("FiddlerScript OnBeforeResponse() failed.", exception);
                }
            }
        }

        internal bool DoExecAction(string strData)
        {
            if (!this.Ready || (this._methodExecAction == null))
            {
                return false;
            }
            try
            {
                object[] parameters = new object[] { Utilities.Parameterize(strData) };
                if (parameters.Length > 0)
                {
                    this._methodExecAction.Invoke(null, parameters);
                    return true;
                }
                return false;
            }
            catch (Exception exception)
            {
                this._NotifyUserOfError("FiddlerScript DoExecAction() failed.", exception);
                return false;
            }
        }

        public bool DoMethod(string sMethodName)
        {
            return this.DoMethod(sMethodName, null);
        }

        public bool DoMethod(string sMethodName, object[] oParams)
        {
            if (!this.Ready)
            {
                return false;
            }
            if (this._typeScriptHandlers == null)
            {
                return false;
            }
            try
            {
                MethodInfo method = this._typeScriptHandlers.GetMethod(sMethodName);
                if (method != null)
                {
                    method.Invoke(null, oParams);
                }
                else
                {
                    return false;
                }
            }
            catch (Exception exception)
            {
                FiddlerApplication.DoNotifyUser(string.Concat(new object[] { "There was a problem with your FiddlerScript.\n\n", exception.Message, "\n", exception.StackTrace, "\n\n", exception.InnerException }), "FiddlerScript Error invoking " + sMethodName);
                return false;
            }
            return true;
        }

        internal void DoOnAttach()
        {
            this.DoMethod("OnAttach");
        }

        internal void DoOnBoot()
        {
            this.DoMethod("OnBoot");
        }

        internal void DoOnDetach()
        {
            this.DoMethod("OnDetach");
        }

        internal void DoOnShutdown()
        {
            this.DoMethod("OnShutdown");
        }

        internal void DoPeekAtRequestHeaders(Session oSession)
        {
            if (this.Ready && (this._methodOnPeekAtRequestHeaders != null))
            {
                try
                {
                    this._methodOnPeekAtRequestHeaders.Invoke(null, new object[] { oSession });
                }
                catch (Exception exception)
                {
                    this._NotifyUserOfError("FiddlerScript OnPeekAtRequestHeaders() failed.", exception);
                }
            }
        }

        internal void DoPeekAtResponseHeaders(Session oSession)
        {
            if (this.Ready && (this._methodOnPeekAtResponseHeaders != null))
            {
                try
                {
                    this._methodOnPeekAtResponseHeaders.Invoke(null, new object[] { oSession });
                }
                catch (Exception exception)
                {
                    this._NotifyUserOfError("FiddlerScript OnPeekAtResponseHeaders() failed.", exception);
                }
            }
        }

        internal void DoReturningError(Session oSession)
        {
            if (this.Ready && (this._methodOnReturningError != null))
            {
                try
                {
                    this._methodOnReturningError.Invoke(null, new object[] { oSession });
                }
                catch (Exception exception)
                {
                    this._NotifyUserOfError("FiddlerScript OnReturningError() failed.", exception);
                }
            }
        }

        private MenuItem FindSubMenu(Menu.MenuItemCollection oItems, string sItemName)
        {
            foreach (MenuItem item in oItems)
            {
                if (item.Text == sItemName)
                {
                    return item;
                }
            }
            return null;
        }

        [CodeDescription("Flash Fiddler's taskbar icon if Fiddler is not the active window.")]
        public void flashWindow()
        {
            if (FiddlerApplication._frmMain != null)
            {
                Utilities.DoFlash(FiddlerApplication._frmMain.Handle);
            }
        }

        private void HandleScriptQuickLinkClick(object sender, EventArgs e)
        {
            try
            {
                MethodInfo info = (MethodInfo) htMenuScripts[sender];
                if (((info.GetParameters().Length != 2) || (info.GetParameters()[0].ParameterType != typeof(string))) || (info.GetParameters()[1].ParameterType != typeof(string)))
                {
                    throw new InvalidDataException("Handler function was incorrectly defined. Should be function(string,string);");
                }
                info.Invoke(null, new object[] { (sender as MenuItem).Text, (sender as MenuItem).Tag as string });
            }
            catch (Exception exception)
            {
                FiddlerApplication.DoNotifyUser(exception.Message, "Script Error");
            }
        }

        private void HandleScriptRulesClick(object sender, EventArgs e)
        {
            MenuItem item = sender as MenuItem;
            item.Checked = !item.Checked;
            FieldInfo info = (FieldInfo) htMenuScripts[item];
            info.SetValue(null, item.Checked);
            if (item.RadioCheck)
            {
                foreach (MenuItem item2 in item.Parent.MenuItems)
                {
                    if ((item != item2) && item2.RadioCheck)
                    {
                        item2.Checked = false;
                        ((FieldInfo) htMenuScripts[item2]).SetValue(null, false);
                    }
                }
            }
        }

        private void HandleScriptRulesStringClick(object sender, EventArgs e)
        {
            MenuItem item = sender as MenuItem;
            FieldInfo info = (FieldInfo) htMenuScripts[item];
            string tag = (string) item.Tag;
            if ((tag != null) && tag.StartsWith("%CUSTOM%", StringComparison.OrdinalIgnoreCase))
            {
                tag = frmPrompt.GetUserString("Input custom value", "Input the custom value", (string) info.GetValue(null));
                item.Checked = true;
            }
            else
            {
                if (item.Checked)
                {
                    return;
                }
                item.Checked = !item.Checked;
            }
            info.SetValue(null, tag);
            foreach (MenuItem item2 in item.Parent.MenuItems)
            {
                if (item != item2)
                {
                    item2.Checked = false;
                }
            }
        }

        private void HandleScriptToolsClick(object sender, EventArgs e)
        {
            try
            {
                MethodInfo info = (MethodInfo) htMenuScripts[sender];
                if ((info.GetParameters().Length == 1) && (info.GetParameters()[0].ParameterType == typeof(Session[])))
                {
                    info.Invoke(null, new object[] { FiddlerApplication._frmMain.GetSelectedSessions() });
                }
                else
                {
                    info.Invoke(null, null);
                }
            }
            catch (Exception exception)
            {
                FiddlerApplication.DoNotifyUser(exception.Message, "Script Error");
            }
        }

        public bool LoadRulesScript()
        {
            bool flag = false;
            try
            {
                string path = CONFIG.GetPath("CustomRules");
                if (FiddlerApplication.Prefs.GetBoolPref("fiddler.script.delaycreate", true) && !File.Exists(path))
                {
                    path = CONFIG.GetPath("SampleRules");
                }
                flag = this._LoadScript(path);
            }
            catch (Exception exception)
            {
                FiddlerApplication.DoNotifyUser(string.Concat(new object[] { "Failed to load script.\n ", exception.Message, "\n", exception.InnerException, exception.StackTrace }), "Script Failure", MessageBoxIcon.Hand);
                this.ClearScript();
            }
            return flag;
        }

        [CodeDescription("Log a simple string.")]
        public void log(string sMessage)
        {
            FiddlerApplication.Log.LogString(sMessage);
        }

        internal void NotifyOfCompilerError(IVsaError e)
        {
            if (this._RulesCompileFailed == null)
            {
                string lineText;
                string[] strArray = e.LineText.Split(new char[] { '\n' });
                if ((e.Line >= 2) && (e.Line < strArray.Length))
                {
                    lineText = "\t\t" + strArray[e.Line - 2].Trim() + "\nERROR LINE -->\t" + strArray[e.Line - 1].Trim() + "\n\t\t" + strArray[e.Line].Trim();
                }
                else
                {
                    lineText = e.LineText;
                }
                FiddlerApplication.DoNotifyUser(string.Format("FiddlerScript compilation failed on line {0}:\n\n----------------------  SOURCE  -------------------------------\n\n{1}\n\n-------------------------------------------------------------------\n\n{2}", e.Line, lineText, e.Description), "FiddlerScript Error", MessageBoxIcon.Hand);
            }
            else
            {
                this._RulesCompileFailed(e.Description, e.Line, e.StartColumn, e.EndColumn);
            }
        }

        [CodeDescription("Play the specified file or system sound.")]
        public void playSound(string sSoundname)
        {
            Utilities.PlaySound(sSoundname, IntPtr.Zero, Utilities.SoundFlags.SND_FILENAME | Utilities.SoundFlags.SND_NOWAIT | Utilities.SoundFlags.SND_ASYNC);
        }

        [CodeDescription("Retrieve a string from an input box labeled by sMessage.")]
        public string prompt(string sMessage)
        {
            return this.prompt(sMessage, string.Empty, "FiddlerScript Prompt");
        }

        [CodeDescription("Retrieve a string from an input box labeled by sMessage.")]
        public string prompt(string sMessage, string sDefaultValue)
        {
            return this.prompt(sMessage, sDefaultValue, "FiddlerScript Prompt");
        }

        [CodeDescription("Retrieve a string from an input box labeled by sMessage.")]
        public string prompt(string sMessage, string sDefaultValue, string sWindowTitle)
        {
            if (FiddlerApplication._frmMain != null)
            {
                Utilities.DoFlash(FiddlerApplication._frmMain.Handle);
            }
            return frmPrompt.GetUserString(sWindowTitle, sMessage, sDefaultValue);
        }

        [CodeDescription("Forces Fiddler to reload the rules script from disk.")]
        public void ReloadScript()
        {
            FiddlerApplication._frmMain.actLoadScripts();
        }

        private void scriptsFolderWatcher_Notify(object sender, FileSystemEventArgs e)
        {
            if ((CONFIG.bAutoLoadScript && CONFIG.bLoadScript) && string.Equals(e.Name, "CustomRules.js", StringComparison.OrdinalIgnoreCase))
            {
                if ((Environment.TickCount - lastScriptLoadTickCount) < CONFIG.iScriptReloadInterval)
                {
                    lastScriptLoadTickCount = Environment.TickCount;
                }
                else
                {
                    if (CONFIG.IsMicrosoftMachine)
                    {
                        FiddlerApplication.logSelfHost(50);
                    }
                    lastScriptLoadTickCount = Environment.TickCount;
                    Utilities.PlaySound(CONFIG.GetPath("App") + "LoadScript.wav", IntPtr.Zero, Utilities.SoundFlags.SND_FILENAME | Utilities.SoundFlags.SND_NOWAIT | Utilities.SoundFlags.SND_NODEFAULT | Utilities.SoundFlags.SND_ASYNC);
                    Thread.Sleep(250);
                    this.LoadRulesScript();
                }
            }
        }

        void IDisposable.Dispose()
        {
            if (this._engine != null)
            {
                this._engine.Close();
            }
            this.scriptsFolderWatcher.Dispose();
        }

        [CodeDescription("Sends a HTTP request; sRequest represents a full request. Warning: Method may throw exceptions.")]
        public void utilIssueRequest(string sRequest)
        {
            FiddlerApplication.oProxy.InjectCustomRequest(sRequest);
        }

        [CodeDescription("Enable notification of preference changes. For proper GC, use this function instead of Prefs.AddWatcher.")]
        public void WatchPreference(string sPref, EventHandler<PrefChangeEventArgs> oFN)
        {
            PreferenceBag.PrefWatcher item = FiddlerApplication.Prefs.AddWatcher(sPref, oFN);
            this.listWeaklyHeldWatchers.Add(item);
        }

        [CodeDescription("Set the text on Fiddler's status bar.")]
        public string StatusText
        {
            set
            {
                FiddlerApplication._frmMain.sbpInfo.Text = value;
                Application.DoEvents();
            }
        }

        public frmViewer UI
        {
            get
            {
                return FiddlerApplication._frmMain;
            }
        }
    }
}

