namespace Fiddler
{
    using System;
    using System.ComponentModel;
    using System.Diagnostics;
    using System.Drawing;
    using System.Globalization;
    using System.Text;
    using System.Web;
    using System.Windows.Forms;

    internal class frmOptions : Form
    {
        private Button btnApply;
        private Button btnCancel;
        private Button btnChooseEditor;
        private Button btnCleanCertificateStore;
        private Button btnExportRoot;
        private Button btnSetBGColor;
        private CheckBox cbAllowRemote;
        private CheckBox cbAlwaysShowTrayIcon;
        private CheckBox cbAttachOnStartup;
        private CheckBox cbAutoCheckForUpdates;
        private CheckBox cbAutoReloadScript;
        private CheckBox cbCaptureCONNECT;
        private CheckBox cbDecryptHTTPS;
        private CheckBox cbEnableIPv6;
        private CheckBox cbHideOnMinimize;
        private CheckBox cbHookAllConnections;
        private CheckBox cbHookWithPAC;
        private CheckBox cbIgnoreServerCertErrors;
        private CheckBox cbMapSocketToProcess;
        private CheckBox cbReportHTTPErrors;
        private CheckBox cbResetSessionIDOnListClear;
        private CheckBox cbReuseClientSockets;
        private CheckBox cbReuseServerSockets;
        private CheckBox cbStreamVideo;
        private CheckBox cbUseAES;
        private CheckBox cbUseGateway;
        private CheckBox cbUseSmartScroll;
        private ComboBox cbxFontSize;
        private ComboBox cbxHotkeyMod;
        private CheckedListBox clbConnections;
        private IContainer components;
        private OpenFileDialog dlgChooseEditor;
        private ColorDialog dlgPickColor;
        private GroupBox gbAutoFiddles;
        private GroupBox gbScript;
        private Label label1;
        private Label label2;
        private Label label3;
        private Label label5;
        private Label lblAssemblyReferences;
        private Label lblConns;
        private Label lblExplainConn;
        private Label lblFiddlerBypass;
        private Label lblFontSize;
        private Label lblHTTPSDescription;
        private Label lblRequiresRestart;
        private Label lblSkipDecryption;
        private LinkLabel lnkCopyPACURL;
        private LinkLabel lnkFindExtensions;
        private LinkLabel lnkHookup;
        private LinkLabel lnkHTTPSIntercept;
        private LinkLabel lnkOptionsHelp;
        private LinkLabel lnkShowGatewayInfo;
        private Panel pnlOptionsFooter;
        private TabPage tabAppearance;
        private TabPage tabConnections;
        private TabPage tabExtensions;
        private TabPage tabGeneral;
        private TabPage tabHTTPS;
        private TabControl tabsOptions;
        private ToolTip toolTip1;
        private RichTextBox txtExtensions;
        private TextBox txtFiddlerBypass;
        private TextBox txtHotkey;
        private TextBox txtListenPort;
        private TextBox txtScriptEditor;
        private TextBox txtScriptReferences;
        private TextBox txtSkipDecryption;

        internal frmOptions()
        {
            this.InitializeComponent();
            this.txtExtensions.BackColor = CONFIG.colorDisabledEdit;
        }

        private void actApplyChanges()
        {
            try
            {
                int num;
                CONFIG.bAttachOnBoot = this.cbAttachOnStartup.Checked;
                CONFIG.bVersionCheck = this.cbAutoCheckForUpdates.Checked;
                CONFIG.bAutoLoadScript = this.cbAutoReloadScript.Checked;
                CONFIG.bReuseServerSockets = this.cbReuseServerSockets.Checked;
                CONFIG.bReuseClientSockets = this.cbReuseClientSockets.Checked;
                CONFIG.bHideOnMinimize = this.cbHideOnMinimize.Checked;
                CONFIG.bResetCounterOnClear = this.cbResetSessionIDOnListClear.Checked;
                CONFIG.bReportHTTPErrors = this.cbReportHTTPErrors.Checked;
                CONFIG.JSEditor = this.txtScriptEditor.Text;
                CONFIG.bAllowRemoteConnections = this.cbAllowRemote.Checked;
                CONFIG.bHookWithPAC = this.cbHookWithPAC.Checked;
                CONFIG.m_sAdditionalScriptReferences = this.txtScriptReferences.Text;
                if ((int.TryParse(this.txtListenPort.Text, out num) && (num > 0)) && (num < 0xffff))
                {
                    CONFIG.ListenPort = num;
                }
                CONFIG.bForwardToGateway = this.cbUseGateway.Checked;
                CONFIG.bCaptureCONNECT = this.cbCaptureCONNECT.Checked;
                CONFIG.bEnableIPv6 = this.cbEnableIPv6.Checked;
                CONFIG.sHostsThatBypassFiddler = this.txtFiddlerBypass.Text.Trim();
                CONFIG.bHookAllConnections = this.cbHookAllConnections.Checked;
                if (this.txtHotkey.Text.Length == 1)
                {
                    int num2 = this.txtHotkey.Text[0];
                    if (CONFIG.iHotkey != num2)
                    {
                        MessageBox.Show("New hotkey will take effect after Fiddler is restarted.", "Restart Required");
                    }
                    CONFIG.iHotkey = num2;
                }
                switch (this.cbxHotkeyMod.SelectedIndex)
                {
                    case 0:
                        CONFIG.iHotkeyMod = 8;
                        break;

                    case 2:
                        CONFIG.iHotkeyMod = 6;
                        break;

                    default:
                        CONFIG.iHotkeyMod = 3;
                        break;
                }
                CONFIG.bMITM_HTTPS = this.cbDecryptHTTPS.Checked;
                CONFIG.IgnoreServerCertErrors = this.cbIgnoreServerCertErrors.Checked;
                FiddlerApplication.Prefs.SetStringPref("fiddler.network.https.NoDecryptionHosts", this.txtSkipDecryption.Text);
                CONFIG.bUseAESForSAZ = this.cbUseAES.Checked;
                CONFIG.bAlwaysShowTrayIcon = this.cbAlwaysShowTrayIcon.Checked;
                CONFIG.bSmartScroll = this.cbUseSmartScroll.Checked;
                CONFIG.bStreamAudioVideo = this.cbStreamVideo.Checked;
                CONFIG.bMapSocketToProcess = this.cbMapSocketToProcess.Checked;
                if ((this.cbxFontSize.SelectedIndex > -1) && (CONFIG.flFontSize != float.Parse(this.cbxFontSize.Text, NumberStyles.Float, CultureInfo.InvariantCulture)))
                {
                    CONFIG.flFontSize = float.Parse(this.cbxFontSize.Text, NumberStyles.Float, CultureInfo.InvariantCulture);
                    FiddlerApplication._frmMain.actSetFontSize(CONFIG.flFontSize);
                }
            }
            catch (Exception exception)
            {
                FiddlerApplication.ReportException(exception, "ApplySettings Failure");
            }
            base.Close();
        }

        private void btnApply_Click(object sender, EventArgs e)
        {
            this.actApplyChanges();
        }

        private void btnCancel_Click(object sender, EventArgs e)
        {
            base.Close();
        }

        private void btnChooseEditor_Click(object sender, EventArgs e)
        {
            if (this.dlgChooseEditor.ShowDialog(this) == DialogResult.OK)
            {
                this.txtScriptEditor.Text = this.dlgChooseEditor.FileName;
            }
        }

        private void btnCleanCertificateStore_Click(object sender, EventArgs e)
        {
            if (CertMaker.rootCertIsMachineTrusted())
            {
                Utilities.RunExecutableAndWait(CONFIG.GetPath("App") + "TrustCert.exe", string.Format("-u \"CN={0}{1}\"", CONFIG.sMakeCertRootCN, CONFIG.sMakeCertSubjectO));
            }
            CertMaker.removeFiddlerGeneratedCerts();
            MessageBox.Show("Fiddler-generated certificates have been removed from your User certificate store.", "Success");
        }

        private void btnExportRoot_Click(object sender, EventArgs e)
        {
            if (CertMaker.exportRootToDesktop())
            {
                MessageBox.Show("Fiddler's Root Certificate has been exported to your desktop.", "Success");
            }
            else
            {
                MessageBox.Show("Unable to export Fiddler's Root Certificate.", "Failed");
            }
        }

        private void btnSetBGColor_Click(object sender, EventArgs e)
        {
            this.dlgPickColor.FullOpen = true;
            this.dlgPickColor.Color = CONFIG.colorDisabledEdit;
            if (DialogResult.OK == this.dlgPickColor.ShowDialog(this))
            {
                if (this.dlgPickColor.Color.ToArgb() == -1)
                {
                    MessageBox.Show("Sorry, you cannot set the disabled control color to white.", "Invalid Selection");
                }
                else
                {
                    CONFIG.colorDisabledEdit = this.dlgPickColor.Color;
                    this.btnSetBGColor.BackColor = CONFIG.colorDisabledEdit;
                }
            }
        }

        private void cbAlwaysShowTrayIcon_CheckedChanged(object sender, EventArgs e)
        {
            FiddlerApplication.UI.notifyIcon.Visible = this.cbAlwaysShowTrayIcon.Checked;
        }

        private void cbCaptureCONNECT_CheckedChanged(object sender, EventArgs e)
        {
            if (!this.cbCaptureCONNECT.Checked)
            {
                this.cbDecryptHTTPS.Enabled = this.cbDecryptHTTPS.Checked = this.cbIgnoreServerCertErrors.Enabled = this.cbIgnoreServerCertErrors.Checked = false;
            }
            else
            {
                this.cbDecryptHTTPS.Enabled = true;
            }
        }

        private void cbDecryptHTTPS_CheckedChanged(object sender, EventArgs e)
        {
            if (!this.cbDecryptHTTPS.Checked)
            {
                this.lblSkipDecryption.Visible = this.txtSkipDecryption.Visible = this.cbIgnoreServerCertErrors.Enabled = this.cbIgnoreServerCertErrors.Checked = false;
            }
            else
            {
                this.lblSkipDecryption.Visible = this.txtSkipDecryption.Visible = this.cbIgnoreServerCertErrors.Enabled = true;
            }
            this.btnCleanCertificateStore.Enabled = !this.cbDecryptHTTPS.Checked;
            this.btnExportRoot.Enabled = this.cbDecryptHTTPS.Checked;
        }

        private void cbDecryptHTTPS_Click(object sender, EventArgs e)
        {
            if (this.cbDecryptHTTPS.Checked)
            {
                if ((CertMaker.rootCertExists() || CertMaker.createRootCert()) && !CertMaker.rootCertIsTrusted())
                {
                    frmAlert alert = new frmAlert("WARNING: Sharp Edges! Read Carefully!", "Fiddler generates a unique root CA certificate to intercept HTTPS traffic.\n\nYou may choose to have Windows trust this root certificate to avoid\nsecurity warnings about the untrusted root certificate. You should\nONLY click 'Yes' on a computer used exclusively for TEST purposes.\n\nYou should click 'No' unless you're very very sure.", "Trust the Fiddler Root certificate?", MessageBoxButtons.YesNo, MessageBoxDefaultButton.Button2) {
                        TopMost = true,
                        StartPosition = FormStartPosition.CenterScreen
                    };
                    DialogResult result = alert.ShowDialog();
                    alert.Dispose();
                    if (((DialogResult.Yes == result) && CertMaker.trustRootCert()) && FiddlerApplication.Prefs.GetBoolPref("fiddler.CertMaker.OfferMachineTrust", (Environment.OSVersion.Version.Major > 6) || ((Environment.OSVersion.Version.Major == 6) && (Environment.OSVersion.Version.Minor > 1))))
                    {
                        Utilities.RunExecutable(CONFIG.GetPath("App") + "TrustCert.exe", string.Format("\"CN={0}{1}\"", CONFIG.sMakeCertRootCN, CONFIG.sMakeCertSubjectO));
                    }
                }
            }
            else
            {
                CONFIG.bMITM_HTTPS = false;
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing && (this.components != null))
            {
                this.components.Dispose();
            }
            base.Dispose(disposing);
        }

        private void frmOptions_Load(object sender, EventArgs e)
        {
            try
            {
                this.cbAttachOnStartup.Checked = CONFIG.bAttachOnBoot;
                this.cbAutoCheckForUpdates.Checked = CONFIG.bVersionCheck;
                this.cbAutoReloadScript.Checked = CONFIG.bAutoLoadScript;
                this.cbHideOnMinimize.Checked = CONFIG.bHideOnMinimize;
                this.cbResetSessionIDOnListClear.Checked = CONFIG.bResetCounterOnClear;
                this.txtScriptEditor.Text = CONFIG.JSEditor;
                this.cbAllowRemote.Checked = CONFIG.bAllowRemoteConnections;
                this.cbHookWithPAC.Checked = CONFIG.bHookWithPAC;
                this.cbCaptureCONNECT.Checked = CONFIG.bCaptureCONNECT;
                this.cbEnableIPv6.Checked = CONFIG.bEnableIPv6;
                this.cbDecryptHTTPS.Checked = CONFIG.bMITM_HTTPS;
                this.txtSkipDecryption.Text = FiddlerApplication.Prefs.GetStringPref("fiddler.network.https.NoDecryptionHosts", string.Empty);
                this.cbIgnoreServerCertErrors.Checked = CONFIG.IgnoreServerCertErrors;
                this.cbDecryptHTTPS.Enabled = this.cbCaptureCONNECT.Checked;
                this.cbIgnoreServerCertErrors.Enabled = this.cbDecryptHTTPS.Checked;
                this.cbUseAES.Checked = CONFIG.bUseAESForSAZ;
                this.cbUseSmartScroll.Checked = CONFIG.bSmartScroll;
                this.cbStreamVideo.Checked = CONFIG.bStreamAudioVideo;
                this.cbMapSocketToProcess.Checked = CONFIG.bMapSocketToProcess;
                this.cbReuseServerSockets.Checked = CONFIG.bReuseServerSockets;
                this.cbReuseClientSockets.Checked = CONFIG.bReuseClientSockets;
                this.cbReportHTTPErrors.Checked = CONFIG.bReportHTTPErrors;
                this.txtScriptReferences.Text = CONFIG.m_sAdditionalScriptReferences;
                this.txtListenPort.Text = CONFIG.ListenPort.ToString();
                this.txtListenPort.Enabled = !CONFIG.bUsingPortOverride;
                this.txtFiddlerBypass.Text = CONFIG.sHostsThatBypassFiddler;
                this.cbUseGateway.Checked = CONFIG.bForwardToGateway;
                this.cbHookAllConnections.Checked = CONFIG.bHookAllConnections;
                this.btnSetBGColor.BackColor = CONFIG.colorDisabledEdit;
                this.cbAlwaysShowTrayIcon.Checked = CONFIG.bAlwaysShowTrayIcon;
                this.cbxFontSize.Text = CONFIG.flFontSize.ToString("##.##");
                this.txtHotkey.Text = string.Empty + ((char) CONFIG.iHotkey);
                switch (CONFIG.iHotkeyMod)
                {
                    case 6:
                        this.cbxHotkeyMod.SelectedIndex = 2;
                        break;

                    case 8:
                        this.cbxHotkeyMod.SelectedIndex = 0;
                        break;

                    default:
                        this.cbxHotkeyMod.SelectedIndex = 1;
                        break;
                }
                this.lnkShowGatewayInfo.Enabled = FiddlerApplication.oProxy.piPrior != null;
                this.txtExtensions.Text = "None loaded.";
                if ((FiddlerApplication.oExtensions != null) && (FiddlerApplication.oExtensions.Extensions.Count > 0))
                {
                    StringBuilder builder = new StringBuilder();
                    foreach (IFiddlerExtension extension in FiddlerApplication.oExtensions.Extensions.Values)
                    {
                        FileVersionInfo versionInfo = FileVersionInfo.GetVersionInfo(extension.GetType().Assembly.Location);
                        string str = string.Format("(v{0}.{1}.{2}.{3})\tfrom {4}", new object[] { versionInfo.FileMajorPart, versionInfo.FileMinorPart, versionInfo.FileBuildPart, versionInfo.FilePrivatePart, Utilities.CollapsePath(extension.GetType().Assembly.Location) });
                        builder.AppendFormat("{0} {1}\r\n", extension.ToString(), str);
                    }
                    this.txtExtensions.Text = builder.ToString();
                }
                if (FiddlerApplication.oProxy.oAllConnectoids != null)
                {
                    foreach (WinINETConnectoid connectoid in FiddlerApplication.oProxy.oAllConnectoids._oConnectoids.Values)
                    {
                        this.clbConnections.Items.Add(connectoid.sConnectionName, connectoid.bIsHooked);
                    }
                }
                else
                {
                    this.clbConnections.Visible = false;
                }
            }
            catch (Exception exception)
            {
                FiddlerApplication.ReportException(exception, "LoadSettings Failure");
            }
        }

        private void InitializeComponent()
        {
            this.components = new Container();
            ComponentResourceManager manager = new ComponentResourceManager(typeof(frmOptions));
            this.cbAutoCheckForUpdates = new CheckBox();
            this.cbAutoReloadScript = new CheckBox();
            this.txtScriptEditor = new TextBox();
            this.label1 = new Label();
            this.btnApply = new Button();
            this.btnCancel = new Button();
            this.btnChooseEditor = new Button();
            this.txtScriptReferences = new TextBox();
            this.label2 = new Label();
            this.cbEnableIPv6 = new CheckBox();
            this.gbScript = new GroupBox();
            this.lblAssemblyReferences = new Label();
            this.cbReportHTTPErrors = new CheckBox();
            this.txtHotkey = new TextBox();
            this.cbxHotkeyMod = new ComboBox();
            this.label5 = new Label();
            this.dlgChooseEditor = new OpenFileDialog();
            this.toolTip1 = new ToolTip(this.components);
            this.cbIgnoreServerCertErrors = new CheckBox();
            this.cbDecryptHTTPS = new CheckBox();
            this.cbCaptureCONNECT = new CheckBox();
            this.cbAttachOnStartup = new CheckBox();
            this.cbUseGateway = new CheckBox();
            this.cbxFontSize = new ComboBox();
            this.cbHideOnMinimize = new CheckBox();
            this.cbMapSocketToProcess = new CheckBox();
            this.cbUseAES = new CheckBox();
            this.cbUseSmartScroll = new CheckBox();
            this.cbStreamVideo = new CheckBox();
            this.txtListenPort = new TextBox();
            this.cbResetSessionIDOnListClear = new CheckBox();
            this.txtFiddlerBypass = new TextBox();
            this.lblFiddlerBypass = new Label();
            this.cbHookAllConnections = new CheckBox();
            this.cbAlwaysShowTrayIcon = new CheckBox();
            this.lnkCopyPACURL = new LinkLabel();
            this.cbReuseClientSockets = new CheckBox();
            this.cbReuseServerSockets = new CheckBox();
            this.cbAllowRemote = new CheckBox();
            this.cbHookWithPAC = new CheckBox();
            this.btnCleanCertificateStore = new Button();
            this.btnExportRoot = new Button();
            this.txtSkipDecryption = new TextBox();
            this.tabsOptions = new TabControl();
            this.tabGeneral = new TabPage();
            this.tabHTTPS = new TabPage();
            this.lblSkipDecryption = new Label();
            this.lblHTTPSDescription = new Label();
            this.lnkHTTPSIntercept = new LinkLabel();
            this.tabExtensions = new TabPage();
            this.gbAutoFiddles = new GroupBox();
            this.lnkFindExtensions = new LinkLabel();
            this.txtExtensions = new RichTextBox();
            this.tabConnections = new TabPage();
            this.lnkShowGatewayInfo = new LinkLabel();
            this.label3 = new Label();
            this.lnkHookup = new LinkLabel();
            this.lblExplainConn = new Label();
            this.clbConnections = new CheckedListBox();
            this.lblConns = new Label();
            this.tabAppearance = new TabPage();
            this.btnSetBGColor = new Button();
            this.lblFontSize = new Label();
            this.pnlOptionsFooter = new Panel();
            this.lnkOptionsHelp = new LinkLabel();
            this.lblRequiresRestart = new Label();
            this.dlgPickColor = new ColorDialog();
            this.gbScript.SuspendLayout();
            this.tabsOptions.SuspendLayout();
            this.tabGeneral.SuspendLayout();
            this.tabHTTPS.SuspendLayout();
            this.tabExtensions.SuspendLayout();
            this.gbAutoFiddles.SuspendLayout();
            this.tabConnections.SuspendLayout();
            this.tabAppearance.SuspendLayout();
            this.pnlOptionsFooter.SuspendLayout();
            base.SuspendLayout();
            this.cbAutoCheckForUpdates.Location = new Point(8, 0x11);
            this.cbAutoCheckForUpdates.Name = "cbAutoCheckForUpdates";
            this.cbAutoCheckForUpdates.Size = new Size(0x1fc, 0x10);
            this.cbAutoCheckForUpdates.TabIndex = 0;
            this.cbAutoCheckForUpdates.Text = "Check for &updates on startup";
            this.toolTip1.SetToolTip(this.cbAutoCheckForUpdates, "Fiddler will hit a webservice at www.fiddler2.com to check for new versions.");
            this.cbAutoReloadScript.Location = new Point(10, 20);
            this.cbAutoReloadScript.Name = "cbAutoReloadScript";
            this.cbAutoReloadScript.Size = new Size(0x1fd, 0x17);
            this.cbAutoReloadScript.TabIndex = 0;
            this.cbAutoReloadScript.Text = "Automatically reload &script when changed";
            this.toolTip1.SetToolTip(this.cbAutoReloadScript, "Fiddler will automatically reload the script when it detects that the script file has been changed.");
            this.txtScriptEditor.Anchor = AnchorStyles.Right | AnchorStyles.Left | AnchorStyles.Top;
            this.txtScriptEditor.Location = new Point(0x80, 0x2c);
            this.txtScriptEditor.Name = "txtScriptEditor";
            this.txtScriptEditor.Size = new Size(0x16f, 0x15);
            this.txtScriptEditor.TabIndex = 2;
            this.toolTip1.SetToolTip(this.txtScriptEditor, "Choose the editing environment you'd like to use to edit your FiddlerScript.\nA syntax-highlighting editor is available from Fiddler2.com.");
            this.label1.Location = new Point(10, 0x2e);
            this.label1.Name = "label1";
            this.label1.Size = new Size(0x72, 0x12);
            this.label1.TabIndex = 1;
            this.label1.Text = "&Editor:";
            this.label1.TextAlign = ContentAlignment.MiddleRight;
            this.btnApply.Anchor = AnchorStyles.Right | AnchorStyles.Bottom;
            this.btnApply.Location = new Point(0x174, 8);
            this.btnApply.Name = "btnApply";
            this.btnApply.Size = new Size(0x4e, 0x17);
            this.btnApply.TabIndex = 0;
            this.btnApply.Text = "&OK";
            this.btnApply.Click += new EventHandler(this.btnApply_Click);
            this.btnCancel.Anchor = AnchorStyles.Right | AnchorStyles.Bottom;
            this.btnCancel.DialogResult = DialogResult.Cancel;
            this.btnCancel.Location = new Point(0x1ca, 8);
            this.btnCancel.Name = "btnCancel";
            this.btnCancel.Size = new Size(0x4e, 0x17);
            this.btnCancel.TabIndex = 1;
            this.btnCancel.Text = "&Cancel";
            this.btnCancel.Click += new EventHandler(this.btnCancel_Click);
            this.btnChooseEditor.Anchor = AnchorStyles.Right | AnchorStyles.Top;
            this.btnChooseEditor.Location = new Point(0x1f1, 0x2c);
            this.btnChooseEditor.Name = "btnChooseEditor";
            this.btnChooseEditor.Size = new Size(0x16, 20);
            this.btnChooseEditor.TabIndex = 3;
            this.btnChooseEditor.Text = "...";
            this.toolTip1.SetToolTip(this.btnChooseEditor, "Click to browse for an editor.");
            this.btnChooseEditor.Click += new EventHandler(this.btnChooseEditor_Click);
            this.txtScriptReferences.Anchor = AnchorStyles.Right | AnchorStyles.Left | AnchorStyles.Top;
            this.txtScriptReferences.Location = new Point(0x80, 0x42);
            this.txtScriptReferences.Name = "txtScriptReferences";
            this.txtScriptReferences.Size = new Size(0x189, 0x15);
            this.txtScriptReferences.TabIndex = 5;
            this.toolTip1.SetToolTip(this.txtScriptReferences, "List any additional assemblies to reference within FiddlerScript.");
            this.label2.Location = new Point(10, 0x42);
            this.label2.Name = "label2";
            this.label2.Size = new Size(0x72, 0x12);
            this.label2.TabIndex = 4;
            this.label2.Text = "Re&ferences:";
            this.label2.TextAlign = ContentAlignment.MiddleRight;
            this.cbEnableIPv6.Location = new Point(8, 0x3d);
            this.cbEnableIPv6.Name = "cbEnableIPv6";
            this.cbEnableIPv6.Size = new Size(0x1fc, 0x10);
            this.cbEnableIPv6.TabIndex = 2;
            this.cbEnableIPv6.Text = "Enable IPv&6 (if available)";
            this.toolTip1.SetToolTip(this.cbEnableIPv6, "Bind Fiddler's listener to the IPv6 adapter on the local host.");
            this.gbScript.Anchor = AnchorStyles.Right | AnchorStyles.Left | AnchorStyles.Top;
            this.gbScript.Controls.Add(this.txtScriptEditor);
            this.gbScript.Controls.Add(this.txtScriptReferences);
            this.gbScript.Controls.Add(this.lblAssemblyReferences);
            this.gbScript.Controls.Add(this.label1);
            this.gbScript.Controls.Add(this.label2);
            this.gbScript.Controls.Add(this.btnChooseEditor);
            this.gbScript.Controls.Add(this.cbAutoReloadScript);
            this.gbScript.Location = new Point(3, 8);
            this.gbScript.Name = "gbScript";
            this.gbScript.Size = new Size(0x20f, 0x6a);
            this.gbScript.TabIndex = 0;
            this.gbScript.TabStop = false;
            this.gbScript.Text = "Scripting";
            this.lblAssemblyReferences.Anchor = AnchorStyles.Right | AnchorStyles.Left | AnchorStyles.Top;
            this.lblAssemblyReferences.ForeColor = SystemColors.ControlDarkDark;
            this.lblAssemblyReferences.Location = new Point(0x6b, 0x56);
            this.lblAssemblyReferences.Name = "lblAssemblyReferences";
            this.lblAssemblyReferences.Size = new Size(0x19e, 14);
            this.lblAssemblyReferences.TabIndex = 6;
            this.lblAssemblyReferences.Text = "e.g. system.data.dll; system.xml.dll; myco.myassembly.dll; myco.myapp.exe";
            this.cbReportHTTPErrors.Location = new Point(8, 0x27);
            this.cbReportHTTPErrors.Name = "cbReportHTTPErrors";
            this.cbReportHTTPErrors.Size = new Size(0x1fc, 0x10);
            this.cbReportHTTPErrors.TabIndex = 1;
            this.cbReportHTTPErrors.Text = "Show a message when HTTP protocol &violations encountered";
            this.toolTip1.SetToolTip(this.cbReportHTTPErrors, "Show a message when either a request or a response is illegal under the HTTP RFCs.");
            this.txtHotkey.CharacterCasing = CharacterCasing.Upper;
            this.txtHotkey.Location = new Point(0xf3, 0xa5);
            this.txtHotkey.MaxLength = 1;
            this.txtHotkey.Name = "txtHotkey";
            this.txtHotkey.Size = new Size(0x1a, 0x15);
            this.txtHotkey.TabIndex = 8;
            this.txtHotkey.Text = "F";
            this.cbxHotkeyMod.DropDownStyle = ComboBoxStyle.DropDownList;
            this.cbxHotkeyMod.Items.AddRange(new object[] { "WIN +", "CTRL + ALT +", "CTRL + SHIFT +" });
            this.cbxHotkeyMod.Location = new Point(0x7d, 0xa5);
            this.cbxHotkeyMod.Name = "cbxHotkeyMod";
            this.cbxHotkeyMod.Size = new Size(0x70, 0x15);
            this.cbxHotkeyMod.TabIndex = 7;
            this.label5.Location = new Point(5, 0xa5);
            this.label5.Name = "label5";
            this.label5.Size = new Size(0x72, 0x12);
            this.label5.TabIndex = 6;
            this.label5.Text = "Systemwide &Hotkey:";
            this.label5.TextAlign = ContentAlignment.MiddleRight;
            this.dlgChooseEditor.DefaultExt = "exe";
            this.dlgChooseEditor.Filter = "Executables (*.exe)|*.exe|All Files (*.*)|*.*";
            this.cbIgnoreServerCertErrors.Location = new Point(0x18, 0x5b);
            this.cbIgnoreServerCertErrors.Name = "cbIgnoreServerCertErrors";
            this.cbIgnoreServerCertErrors.Size = new Size(0xb8, 20);
            this.cbIgnoreServerCertErrors.TabIndex = 3;
            this.cbIgnoreServerCertErrors.Text = "&Ignore server certificate errors";
            this.toolTip1.SetToolTip(this.cbIgnoreServerCertErrors, "Check to suppress the warning shown when a remote server presents an invalid certificate.");
            this.cbDecryptHTTPS.Location = new Point(0x18, 0x41);
            this.cbDecryptHTTPS.Name = "cbDecryptHTTPS";
            this.cbDecryptHTTPS.Size = new Size(0x8b, 20);
            this.cbDecryptHTTPS.TabIndex = 2;
            this.cbDecryptHTTPS.Text = "D&ecrypt HTTPS traffic";
            this.toolTip1.SetToolTip(this.cbDecryptHTTPS, "Show HTTPS traffic in plaintext (see Learn more link for details)");
            this.cbDecryptHTTPS.CheckedChanged += new EventHandler(this.cbDecryptHTTPS_CheckedChanged);
            this.cbDecryptHTTPS.Click += new EventHandler(this.cbDecryptHTTPS_Click);
            this.cbCaptureCONNECT.Location = new Point(9, 0x27);
            this.cbCaptureCONNECT.Name = "cbCaptureCONNECT";
            this.cbCaptureCONNECT.Size = new Size(200, 20);
            this.cbCaptureCONNECT.TabIndex = 1;
            this.cbCaptureCONNECT.Text = "Capture &HTTPS CONNECTs";
            this.toolTip1.SetToolTip(this.cbCaptureCONNECT, "Check to intercept HTTPS Tunneling requests.");
            this.cbCaptureCONNECT.CheckedChanged += new EventHandler(this.cbCaptureCONNECT_CheckedChanged);
            this.cbAttachOnStartup.Location = new Point(0xfb, 0x45);
            this.cbAttachOnStartup.Name = "cbAttachOnStartup";
            this.cbAttachOnStartup.Size = new Size(240, 20);
            this.cbAttachOnStartup.TabIndex = 10;
            this.cbAttachOnStartup.Text = "&Act as system proxy on startup";
            this.toolTip1.SetToolTip(this.cbAttachOnStartup, "When checked, Fiddler will automatically attach to WinINET/IE on startup.");
            this.cbUseGateway.Location = new Point(0x10, 0xd0);
            this.cbUseGateway.Name = "cbUseGateway";
            this.cbUseGateway.Size = new Size(0xc2, 20);
            this.cbUseGateway.TabIndex = 7;
            this.cbUseGateway.Text = "Chain to &upstream gateway proxy";
            this.toolTip1.SetToolTip(this.cbUseGateway, "Check to forward traffic from Fiddler through an upstream proxy.");
            this.cbxFontSize.DropDownStyle = ComboBoxStyle.DropDownList;
            this.cbxFontSize.Items.AddRange(new object[] { "8.25", "10", "11", "12", "14", "16", "18" });
            this.cbxFontSize.Location = new Point(0x44, 0x10);
            this.cbxFontSize.Name = "cbxFontSize";
            this.cbxFontSize.Size = new Size(60, 0x15);
            this.cbxFontSize.TabIndex = 1;
            this.toolTip1.SetToolTip(this.cbxFontSize, "Font size used in Fiddler. (Requires restart for full effect)");
            this.cbHideOnMinimize.Location = new Point(8, 0x2e);
            this.cbHideOnMinimize.Name = "cbHideOnMinimize";
            this.cbHideOnMinimize.Size = new Size(0x206, 0x10);
            this.cbHideOnMinimize.TabIndex = 3;
            this.cbHideOnMinimize.Text = "&Hide Fiddler when minimized";
            this.toolTip1.SetToolTip(this.cbHideOnMinimize, "Hides Fiddler in the System tray when minimized");
            this.cbMapSocketToProcess.Location = new Point(8, 0x53);
            this.cbMapSocketToProcess.Name = "cbMapSocketToProcess";
            this.cbMapSocketToProcess.Size = new Size(0x1fc, 0x10);
            this.cbMapSocketToProcess.TabIndex = 3;
            this.cbMapSocketToProcess.Text = "&Map socket to originating application";
            this.toolTip1.SetToolTip(this.cbMapSocketToProcess, "Fiddler will attempt to determine which local process owns the HTTP session.");
            this.cbUseAES.Location = new Point(8, 0x69);
            this.cbUseAES.Name = "cbUseAES";
            this.cbUseAES.Size = new Size(0x1fc, 0x10);
            this.cbUseAES.TabIndex = 4;
            this.cbUseAES.Text = "&Encrypt using AES256 when saving password-protected SAZ files (slow)";
            this.toolTip1.SetToolTip(this.cbUseAES, "Fiddler will encrypt Password-Protected SAZ files using 256bit AES.");
            this.cbUseSmartScroll.Location = new Point(8, 0x5b);
            this.cbUseSmartScroll.Name = "cbUseSmartScroll";
            this.cbUseSmartScroll.Size = new Size(0x206, 0x10);
            this.cbUseSmartScroll.TabIndex = 5;
            this.cbUseSmartScroll.Text = "Use &SmartScroll in Session List";
            this.toolTip1.SetToolTip(this.cbUseSmartScroll, "Disables session list auto-scrolling when session list is scrolled");
            this.cbStreamVideo.Location = new Point(8, 0x7f);
            this.cbStreamVideo.Name = "cbStreamVideo";
            this.cbStreamVideo.Size = new Size(0x1fc, 0x10);
            this.cbStreamVideo.TabIndex = 5;
            this.cbStreamVideo.Text = "Automatically &stream audio && video";
            this.toolTip1.SetToolTip(this.cbStreamVideo, "Allow audio and video to stream automatically");
            this.txtListenPort.Location = new Point(0x90, 0x44);
            this.txtListenPort.MaxLength = 5;
            this.txtListenPort.Name = "txtListenPort";
            this.txtListenPort.Size = new Size(0x42, 0x15);
            this.txtListenPort.TabIndex = 2;
            this.toolTip1.SetToolTip(this.txtListenPort, "Select the local port on which Fiddler should accept connections.");
            this.cbResetSessionIDOnListClear.Location = new Point(8, 0x71);
            this.cbResetSessionIDOnListClear.Name = "cbResetSessionIDOnListClear";
            this.cbResetSessionIDOnListClear.Size = new Size(0x206, 0x10);
            this.cbResetSessionIDOnListClear.TabIndex = 6;
            this.cbResetSessionIDOnListClear.Text = "&Reset Session ID counter on CTRL+X";
            this.toolTip1.SetToolTip(this.cbResetSessionIDOnListClear, "When CTRL+X is used to clear the session list, the session identifier counter restarts from 0.");
            this.txtFiddlerBypass.Anchor = AnchorStyles.Right | AnchorStyles.Left | AnchorStyles.Bottom | AnchorStyles.Top;
            this.txtFiddlerBypass.Location = new Point(0xfb, 0xd0);
            this.txtFiddlerBypass.Multiline = true;
            this.txtFiddlerBypass.Name = "txtFiddlerBypass";
            this.txtFiddlerBypass.ScrollBars = ScrollBars.Both;
            this.txtFiddlerBypass.Size = new Size(270, 0x3d);
            this.txtFiddlerBypass.TabIndex = 15;
            this.toolTip1.SetToolTip(this.txtFiddlerBypass, "Internet Explorer-style Proxy Bypass list. Semicolon-delimited. Requires restart. E.g.  *example.*:8081;https://*.fiddler2.com");
            this.lblFiddlerBypass.AutoSize = true;
            this.lblFiddlerBypass.Location = new Point(0xf8, 190);
            this.lblFiddlerBypass.Name = "lblFiddlerBypass";
            this.lblFiddlerBypass.Size = new Size(0xf3, 13);
            this.lblFiddlerBypass.TabIndex = 14;
            this.lblFiddlerBypass.Text = "&IE should bypass Fiddler for URLs that start with:";
            this.toolTip1.SetToolTip(this.lblFiddlerBypass, "Internet Explorer-style Proxy Bypass list. Semicolon-delimited. Requires restart. E.g.  *example.*:8081;https://*.fiddler2.com");
            this.cbHookAllConnections.Location = new Point(0xfb, 0x5d);
            this.cbHookAllConnections.Name = "cbHookAllConnections";
            this.cbHookAllConnections.Size = new Size(160, 20);
            this.cbHookAllConnections.TabIndex = 11;
            this.cbHookAllConnections.Text = "&Monitor all connections";
            this.toolTip1.SetToolTip(this.cbHookAllConnections, "When checked, Fiddler will attach to all WinINET connections.");
            this.cbAlwaysShowTrayIcon.Location = new Point(8, 0x44);
            this.cbAlwaysShowTrayIcon.Name = "cbAlwaysShowTrayIcon";
            this.cbAlwaysShowTrayIcon.Size = new Size(0x206, 0x10);
            this.cbAlwaysShowTrayIcon.TabIndex = 4;
            this.cbAlwaysShowTrayIcon.Text = "&Always show tray icon";
            this.toolTip1.SetToolTip(this.cbAlwaysShowTrayIcon, "Show Fiddler in the System tray at all times");
            this.cbAlwaysShowTrayIcon.CheckedChanged += new EventHandler(this.cbAlwaysShowTrayIcon_CheckedChanged);
            this.lnkCopyPACURL.AutoSize = true;
            this.lnkCopyPACURL.LinkBehavior = LinkBehavior.HoverUnderline;
            this.lnkCopyPACURL.Location = new Point(15, 0x60);
            this.lnkCopyPACURL.Name = "lnkCopyPACURL";
            this.lnkCopyPACURL.Size = new Size(0xc3, 13);
            this.lnkCopyPACURL.TabIndex = 3;
            this.lnkCopyPACURL.TabStop = true;
            this.lnkCopyPACURL.Text = "Copy Browser Proxy Configuration URL";
            this.toolTip1.SetToolTip(this.lnkCopyPACURL, "When a browser (e.g. Firefox) is configured to point at this URL, it will use Fiddler when Fiddler is running, and go direct otherwise.");
            this.lnkCopyPACURL.LinkClicked += new LinkLabelLinkClickedEventHandler(this.lnkCopyPACURL_LinkClicked);
            this.cbReuseClientSockets.Location = new Point(0x10, 0x9c);
            this.cbReuseClientSockets.Name = "cbReuseClientSockets";
            this.cbReuseClientSockets.Size = new Size(0xad, 20);
            this.cbReuseClientSockets.TabIndex = 5;
            this.cbReuseClientSockets.Text = "Reuse client co&nnections";
            this.toolTip1.SetToolTip(this.cbReuseClientSockets, "Allow more than one request from a given client socket.");
            this.cbReuseServerSockets.Location = new Point(0x10, 0xb6);
            this.cbReuseServerSockets.Name = "cbReuseServerSockets";
            this.cbReuseServerSockets.Size = new Size(0xb5, 20);
            this.cbReuseServerSockets.TabIndex = 6;
            this.cbReuseServerSockets.Text = "&Reuse connections to servers";
            this.toolTip1.SetToolTip(this.cbReuseServerSockets, "Enable socket reuse when connecting to remote HTTP servers.");
            this.cbAllowRemote.Location = new Point(0x10, 0x80);
            this.cbAllowRemote.Name = "cbAllowRemote";
            this.cbAllowRemote.Size = new Size(210, 0x16);
            this.cbAllowRemote.TabIndex = 4;
            this.cbAllowRemote.Text = "Allo&w remote computers to connect";
            this.toolTip1.SetToolTip(this.cbAllowRemote, "Check to permit remote computers to route traffic through Fiddler.");
            this.cbHookWithPAC.Location = new Point(0x19f, 0x5c);
            this.cbHookWithPAC.Name = "cbHookWithPAC";
            this.cbHookWithPAC.Size = new Size(0x6a, 0x16);
            this.cbHookWithPAC.TabIndex = 12;
            this.cbHookWithPAC.Text = "Use &PAC Script";
            this.toolTip1.SetToolTip(this.cbHookWithPAC, "Registers with WinINET as a ProxyAutoConfig script, allowing for simpler Localhost debugging");
            this.btnCleanCertificateStore.Location = new Point(0x127, 0xd3);
            this.btnCleanCertificateStore.Name = "btnCleanCertificateStore";
            this.btnCleanCertificateStore.Size = new Size(210, 0x17);
            this.btnCleanCertificateStore.TabIndex = 7;
            this.btnCleanCertificateStore.Text = "&Remove Interception Certificates";
            this.toolTip1.SetToolTip(this.btnCleanCertificateStore, "Removes all Fiddler-generated interception certificates from your certificate stores.");
            this.btnCleanCertificateStore.UseVisualStyleBackColor = true;
            this.btnCleanCertificateStore.Click += new EventHandler(this.btnCleanCertificateStore_Click);
            this.btnExportRoot.Enabled = false;
            this.btnExportRoot.Location = new Point(0x18, 0xd3);
            this.btnExportRoot.Name = "btnExportRoot";
            this.btnExportRoot.Size = new Size(0xf5, 0x17);
            this.btnExportRoot.TabIndex = 6;
            this.btnExportRoot.Text = "E&xport Fiddler Root Certificate to Desktop";
            this.toolTip1.SetToolTip(this.btnExportRoot, "Export's Fiddler's Root Certificate to the desktop. Useful for importing into some browsers.");
            this.btnExportRoot.UseVisualStyleBackColor = true;
            this.btnExportRoot.Click += new EventHandler(this.btnExportRoot_Click);
            this.txtSkipDecryption.Location = new Point(0x18, 0x8f);
            this.txtSkipDecryption.Multiline = true;
            this.txtSkipDecryption.Name = "txtSkipDecryption";
            this.txtSkipDecryption.Size = new Size(0x1e1, 60);
            this.txtSkipDecryption.TabIndex = 5;
            this.toolTip1.SetToolTip(this.txtSkipDecryption, "Semi-colon delimited list of hostnames for which decryption should not occur.");
            this.tabsOptions.Controls.Add(this.tabGeneral);
            this.tabsOptions.Controls.Add(this.tabHTTPS);
            this.tabsOptions.Controls.Add(this.tabExtensions);
            this.tabsOptions.Controls.Add(this.tabConnections);
            this.tabsOptions.Controls.Add(this.tabAppearance);
            this.tabsOptions.Dock = DockStyle.Fill;
            this.tabsOptions.Location = new Point(0, 0);
            this.tabsOptions.Name = "tabsOptions";
            this.tabsOptions.SelectedIndex = 0;
            this.tabsOptions.Size = new Size(0x21e, 0x12d);
            this.tabsOptions.TabIndex = 0;
            this.tabsOptions.HelpRequested += new HelpEventHandler(this.tabsOptions_HelpRequested);
            this.tabGeneral.BackColor = SystemColors.Window;
            this.tabGeneral.Controls.Add(this.cbStreamVideo);
            this.tabGeneral.Controls.Add(this.cbUseAES);
            this.tabGeneral.Controls.Add(this.cbMapSocketToProcess);
            this.tabGeneral.Controls.Add(this.cbEnableIPv6);
            this.tabGeneral.Controls.Add(this.cbxHotkeyMod);
            this.tabGeneral.Controls.Add(this.label5);
            this.tabGeneral.Controls.Add(this.cbAutoCheckForUpdates);
            this.tabGeneral.Controls.Add(this.cbReportHTTPErrors);
            this.tabGeneral.Controls.Add(this.txtHotkey);
            this.tabGeneral.Location = new Point(4, 0x16);
            this.tabGeneral.Name = "tabGeneral";
            this.tabGeneral.Size = new Size(0x216, 0x113);
            this.tabGeneral.TabIndex = 0;
            this.tabGeneral.Text = "General";
            this.tabHTTPS.BackColor = SystemColors.Window;
            this.tabHTTPS.Controls.Add(this.lblSkipDecryption);
            this.tabHTTPS.Controls.Add(this.txtSkipDecryption);
            this.tabHTTPS.Controls.Add(this.btnExportRoot);
            this.tabHTTPS.Controls.Add(this.btnCleanCertificateStore);
            this.tabHTTPS.Controls.Add(this.lblHTTPSDescription);
            this.tabHTTPS.Controls.Add(this.lnkHTTPSIntercept);
            this.tabHTTPS.Controls.Add(this.cbIgnoreServerCertErrors);
            this.tabHTTPS.Controls.Add(this.cbDecryptHTTPS);
            this.tabHTTPS.Controls.Add(this.cbCaptureCONNECT);
            this.tabHTTPS.Location = new Point(4, 0x16);
            this.tabHTTPS.Name = "tabHTTPS";
            this.tabHTTPS.Padding = new Padding(3);
            this.tabHTTPS.Size = new Size(0x216, 0x113);
            this.tabHTTPS.TabIndex = 3;
            this.tabHTTPS.Text = "HTTPS";
            this.lblSkipDecryption.AutoSize = true;
            this.lblSkipDecryption.Location = new Point(0x15, 0x7c);
            this.lblSkipDecryption.Name = "lblSkipDecryption";
            this.lblSkipDecryption.Size = new Size(0xc2, 13);
            this.lblSkipDecryption.TabIndex = 4;
            this.lblSkipDecryption.Text = "&Skip decryption for the following hosts:";
            this.lblHTTPSDescription.Anchor = AnchorStyles.Right | AnchorStyles.Left | AnchorStyles.Top;
            this.lblHTTPSDescription.Location = new Point(6, 3);
            this.lblHTTPSDescription.Name = "lblHTTPSDescription";
            this.lblHTTPSDescription.Size = new Size(520, 0x15);
            this.lblHTTPSDescription.TabIndex = 0;
            this.lblHTTPSDescription.Text = "Fiddler is able to decrypt HTTPS sessions by re-signing traffic using a self-generated certificate.";
            this.lblHTTPSDescription.TextAlign = ContentAlignment.MiddleLeft;
            this.lnkHTTPSIntercept.AutoSize = true;
            this.lnkHTTPSIntercept.LinkBehavior = LinkBehavior.HoverUnderline;
            this.lnkHTTPSIntercept.Location = new Point(0xf3, 0x44);
            this.lnkHTTPSIntercept.Name = "lnkHTTPSIntercept";
            this.lnkHTTPSIntercept.Size = new Size(0x106, 13);
            this.lnkHTTPSIntercept.TabIndex = 8;
            this.lnkHTTPSIntercept.TabStop = true;
            this.lnkHTTPSIntercept.Text = "Learn more about decryption and certificate errors...";
            this.lnkHTTPSIntercept.LinkClicked += new LinkLabelLinkClickedEventHandler(this.lnkHTTPSIntercept_LinkClicked);
            this.tabExtensions.BackColor = SystemColors.Window;
            this.tabExtensions.Controls.Add(this.gbAutoFiddles);
            this.tabExtensions.Controls.Add(this.gbScript);
            this.tabExtensions.Location = new Point(4, 0x16);
            this.tabExtensions.Name = "tabExtensions";
            this.tabExtensions.Size = new Size(0x216, 0x113);
            this.tabExtensions.TabIndex = 1;
            this.tabExtensions.Text = "Extensions";
            this.gbAutoFiddles.Anchor = AnchorStyles.Right | AnchorStyles.Left | AnchorStyles.Bottom | AnchorStyles.Top;
            this.gbAutoFiddles.Controls.Add(this.lnkFindExtensions);
            this.gbAutoFiddles.Controls.Add(this.txtExtensions);
            this.gbAutoFiddles.Location = new Point(3, 120);
            this.gbAutoFiddles.Name = "gbAutoFiddles";
            this.gbAutoFiddles.Size = new Size(0x211, 0x98);
            this.gbAutoFiddles.TabIndex = 1;
            this.gbAutoFiddles.TabStop = false;
            this.gbAutoFiddles.Text = "Extensions";
            this.lnkFindExtensions.Anchor = AnchorStyles.Right | AnchorStyles.Bottom;
            this.lnkFindExtensions.AutoSize = true;
            this.lnkFindExtensions.LinkBehavior = LinkBehavior.HoverUnderline;
            this.lnkFindExtensions.Location = new Point(0x192, 130);
            this.lnkFindExtensions.Name = "lnkFindExtensions";
            this.lnkFindExtensions.Size = new Size(0x79, 13);
            this.lnkFindExtensions.TabIndex = 1;
            this.lnkFindExtensions.TabStop = true;
            this.lnkFindExtensions.Text = "Find more extensions...";
            this.lnkFindExtensions.TextAlign = ContentAlignment.TopRight;
            this.lnkFindExtensions.LinkClicked += new LinkLabelLinkClickedEventHandler(this.lnkFindExtensions_LinkClicked);
            this.txtExtensions.Anchor = AnchorStyles.Right | AnchorStyles.Left | AnchorStyles.Bottom | AnchorStyles.Top;
            this.txtExtensions.BorderStyle = BorderStyle.FixedSingle;
            this.txtExtensions.Font = new Font("Tahoma", 6.75f, FontStyle.Regular, GraphicsUnit.Point, 0);
            this.txtExtensions.Location = new Point(5, 14);
            this.txtExtensions.Name = "txtExtensions";
            this.txtExtensions.ReadOnly = true;
            this.txtExtensions.Size = new Size(0x206, 0x74);
            this.txtExtensions.TabIndex = 0;
            this.txtExtensions.Text = "";
            this.txtExtensions.WordWrap = false;
            this.tabConnections.BackColor = SystemColors.Window;
            this.tabConnections.Controls.Add(this.cbHookWithPAC);
            this.tabConnections.Controls.Add(this.cbAllowRemote);
            this.tabConnections.Controls.Add(this.cbReuseClientSockets);
            this.tabConnections.Controls.Add(this.cbReuseServerSockets);
            this.tabConnections.Controls.Add(this.lnkCopyPACURL);
            this.tabConnections.Controls.Add(this.cbHookAllConnections);
            this.tabConnections.Controls.Add(this.txtFiddlerBypass);
            this.tabConnections.Controls.Add(this.lblFiddlerBypass);
            this.tabConnections.Controls.Add(this.lnkShowGatewayInfo);
            this.tabConnections.Controls.Add(this.txtListenPort);
            this.tabConnections.Controls.Add(this.label3);
            this.tabConnections.Controls.Add(this.cbAttachOnStartup);
            this.tabConnections.Controls.Add(this.cbUseGateway);
            this.tabConnections.Controls.Add(this.lnkHookup);
            this.tabConnections.Controls.Add(this.lblExplainConn);
            this.tabConnections.Controls.Add(this.clbConnections);
            this.tabConnections.Controls.Add(this.lblConns);
            this.tabConnections.Location = new Point(4, 0x16);
            this.tabConnections.Name = "tabConnections";
            this.tabConnections.Padding = new Padding(3);
            this.tabConnections.Size = new Size(0x216, 0x113);
            this.tabConnections.TabIndex = 2;
            this.tabConnections.Text = "Connections";
            this.lnkShowGatewayInfo.AutoSize = true;
            this.lnkShowGatewayInfo.LinkBehavior = LinkBehavior.HoverUnderline;
            this.lnkShowGatewayInfo.Location = new Point(0x23, 0xe7);
            this.lnkShowGatewayInfo.Name = "lnkShowGatewayInfo";
            this.lnkShowGatewayInfo.Size = new Size(0x66, 13);
            this.lnkShowGatewayInfo.TabIndex = 8;
            this.lnkShowGatewayInfo.TabStop = true;
            this.lnkShowGatewayInfo.Text = "Show Gateway Info";
            this.lnkShowGatewayInfo.LinkClicked += new LinkLabelLinkClickedEventHandler(this.lnkShowGatewayInfo_LinkClicked);
            this.label3.Location = new Point(13, 0x47);
            this.label3.Name = "label3";
            this.label3.Size = new Size(0x7d, 14);
            this.label3.TabIndex = 1;
            this.label3.Text = "Fiddler &listens on port:";
            this.label3.TextAlign = ContentAlignment.MiddleRight;
            this.lnkHookup.Anchor = AnchorStyles.Right | AnchorStyles.Top;
            this.lnkHookup.LinkBehavior = LinkBehavior.HoverUnderline;
            this.lnkHookup.Location = new Point(0x1b2, 0x20);
            this.lnkHookup.Name = "lnkHookup";
            this.lnkHookup.Size = new Size(0x62, 0x10);
            this.lnkHookup.TabIndex = 0x10;
            this.lnkHookup.TabStop = true;
            this.lnkHookup.Text = "Learn more...";
            this.lnkHookup.TextAlign = ContentAlignment.TopRight;
            this.lnkHookup.LinkClicked += new LinkLabelLinkClickedEventHandler(this.lnkHookup_LinkClicked);
            this.lblExplainConn.Anchor = AnchorStyles.Right | AnchorStyles.Left | AnchorStyles.Top;
            this.lblExplainConn.Location = new Point(3, 3);
            this.lblExplainConn.Name = "lblExplainConn";
            this.lblExplainConn.Size = new Size(520, 0x1d);
            this.lblExplainConn.TabIndex = 0;
            this.lblExplainConn.Text = "Fiddler can debug traffic from any application that accepts a HTTP Proxy. All WinINET traffic is routed through Fiddler when \"File > Capture Traffic\" is checked.";
            this.lblExplainConn.TextAlign = ContentAlignment.MiddleLeft;
            this.clbConnections.Anchor = AnchorStyles.Right | AnchorStyles.Left | AnchorStyles.Top;
            this.clbConnections.CheckOnClick = true;
            this.clbConnections.Enabled = false;
            this.clbConnections.FormattingEnabled = true;
            this.clbConnections.Location = new Point(0xfb, 0x77);
            this.clbConnections.Name = "clbConnections";
            this.clbConnections.Size = new Size(270, 0x44);
            this.clbConnections.TabIndex = 13;
            this.lblConns.AutoSize = true;
            this.lblConns.BackColor = Color.Transparent;
            this.lblConns.Font = new Font("Tahoma", 8.25f, FontStyle.Bold);
            this.lblConns.Location = new Point(0xf8, 0x33);
            this.lblConns.Name = "lblConns";
            this.lblConns.Size = new Size(0x7d, 13);
            this.lblConns.TabIndex = 9;
            this.lblConns.Text = "WinINET Connections";
            this.tabAppearance.BackColor = SystemColors.Window;
            this.tabAppearance.Controls.Add(this.btnSetBGColor);
            this.tabAppearance.Controls.Add(this.cbAlwaysShowTrayIcon);
            this.tabAppearance.Controls.Add(this.cbResetSessionIDOnListClear);
            this.tabAppearance.Controls.Add(this.cbUseSmartScroll);
            this.tabAppearance.Controls.Add(this.cbxFontSize);
            this.tabAppearance.Controls.Add(this.cbHideOnMinimize);
            this.tabAppearance.Controls.Add(this.lblFontSize);
            this.tabAppearance.Location = new Point(4, 0x16);
            this.tabAppearance.Name = "tabAppearance";
            this.tabAppearance.Padding = new Padding(3);
            this.tabAppearance.Size = new Size(0x216, 0x113);
            this.tabAppearance.TabIndex = 4;
            this.tabAppearance.Text = "Appearance";
            this.btnSetBGColor.Location = new Point(0x9d, 14);
            this.btnSetBGColor.Name = "btnSetBGColor";
            this.btnSetBGColor.Size = new Size(0xb2, 0x17);
            this.btnSetBGColor.TabIndex = 2;
            this.btnSetBGColor.Text = "Set Readonly &Color";
            this.btnSetBGColor.UseVisualStyleBackColor = true;
            this.btnSetBGColor.Click += new EventHandler(this.btnSetBGColor_Click);
            this.lblFontSize.Location = new Point(6, 0x13);
            this.lblFontSize.Name = "lblFontSize";
            this.lblFontSize.Size = new Size(0x38, 0x12);
            this.lblFontSize.TabIndex = 0;
            this.lblFontSize.Text = "&Font size:";
            this.lblFontSize.TextAlign = ContentAlignment.MiddleRight;
            this.pnlOptionsFooter.Controls.Add(this.lnkOptionsHelp);
            this.pnlOptionsFooter.Controls.Add(this.btnCancel);
            this.pnlOptionsFooter.Controls.Add(this.btnApply);
            this.pnlOptionsFooter.Controls.Add(this.lblRequiresRestart);
            this.pnlOptionsFooter.Dock = DockStyle.Bottom;
            this.pnlOptionsFooter.Location = new Point(0, 0x12d);
            this.pnlOptionsFooter.Name = "pnlOptionsFooter";
            this.pnlOptionsFooter.Size = new Size(0x21e, 0x24);
            this.pnlOptionsFooter.TabIndex = 1;
            this.lnkOptionsHelp.Anchor = AnchorStyles.Left | AnchorStyles.Bottom;
            this.lnkOptionsHelp.LinkBehavior = LinkBehavior.HoverUnderline;
            this.lnkOptionsHelp.Location = new Point(3, 3);
            this.lnkOptionsHelp.Name = "lnkOptionsHelp";
            this.lnkOptionsHelp.Size = new Size(0x27, 0x21);
            this.lnkOptionsHelp.TabIndex = 3;
            this.lnkOptionsHelp.TabStop = true;
            this.lnkOptionsHelp.Text = "Help";
            this.lnkOptionsHelp.TextAlign = ContentAlignment.MiddleCenter;
            this.lnkOptionsHelp.LinkClicked += new LinkLabelLinkClickedEventHandler(this.lnkOptionsHelp_LinkClicked);
            this.lblRequiresRestart.Anchor = AnchorStyles.Right | AnchorStyles.Left | AnchorStyles.Top;
            this.lblRequiresRestart.ForeColor = Color.DimGray;
            this.lblRequiresRestart.Location = new Point(0x3b, 3);
            this.lblRequiresRestart.Name = "lblRequiresRestart";
            this.lblRequiresRestart.Size = new Size(0x1d7, 0x21);
            this.lblRequiresRestart.TabIndex = 2;
            this.lblRequiresRestart.Text = "Note: Changes may not take effect until Fiddler is restarted.";
            this.lblRequiresRestart.TextAlign = ContentAlignment.MiddleLeft;
            this.AutoScaleBaseSize = new Size(5, 14);
            base.CancelButton = this.btnCancel;
            base.ClientSize = new Size(0x21e, 0x151);
            base.Controls.Add(this.tabsOptions);
            base.Controls.Add(this.pnlOptionsFooter);
            this.Font = new Font("Tahoma", 8.25f);
            base.FormBorderStyle = FormBorderStyle.SizableToolWindow;
            base.HelpButton = true;
            base.Icon = (Icon) manager.GetObject("$this.Icon");
            this.MinimumSize = new Size(550, 350);
            base.Name = "frmOptions";
            base.StartPosition = FormStartPosition.CenterParent;
            this.Text = "Fiddler Options";
            base.Load += new EventHandler(this.frmOptions_Load);
            this.gbScript.ResumeLayout(false);
            this.gbScript.PerformLayout();
            this.tabsOptions.ResumeLayout(false);
            this.tabGeneral.ResumeLayout(false);
            this.tabGeneral.PerformLayout();
            this.tabHTTPS.ResumeLayout(false);
            this.tabHTTPS.PerformLayout();
            this.tabExtensions.ResumeLayout(false);
            this.gbAutoFiddles.ResumeLayout(false);
            this.gbAutoFiddles.PerformLayout();
            this.tabConnections.ResumeLayout(false);
            this.tabConnections.PerformLayout();
            this.tabAppearance.ResumeLayout(false);
            this.pnlOptionsFooter.ResumeLayout(false);
            base.ResumeLayout(false);
        }

        private void lnkCopyPACURL_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            Utilities.CopyToClipboard("file:///" + HttpUtility.UrlPathEncode(CONFIG.GetPath("Pac").Replace('\\', '/')));
        }

        private void lnkFindExtensions_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            Utilities.LaunchHyperlink(CONFIG.GetUrl("REDIR") + "FIDDLEREXTENSIONS");
        }

        private void lnkHookup_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            Utilities.LaunchHyperlink(CONFIG.GetUrl("REDIR") + "HOOKUP");
        }

        private void lnkHTTPSIntercept_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            Utilities.LaunchHyperlink(CONFIG.GetUrl("REDIR") + "HTTPSDECRYPTION");
        }

        private void lnkOptionsHelp_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            Utilities.LaunchHyperlink(CONFIG.GetUrl("REDIR") + "OPTIONSHELP");
        }

        private void lnkShowGatewayInfo_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            if (FiddlerApplication.oProxy.piPrior != null)
            {
                MessageBox.Show(FiddlerApplication.oProxy.piPrior.bUseManualProxies ? FiddlerApplication.oProxy.piPrior.ToString() : ((FiddlerApplication.oProxy.oAutoProxy != null) ? FiddlerApplication.oProxy.oAutoProxy.ToString() : "No upstream gateway proxy was configured."), "System Default Proxy");
            }
        }

        private void tabsOptions_HelpRequested(object sender, HelpEventArgs hlpevent)
        {
            Utilities.LaunchHyperlink(CONFIG.GetUrl("REDIR") + "OPTIONSHELP");
        }
    }
}

