namespace Fiddler
{
    using System;
    using System.ComponentModel;
    using System.Drawing;
    using System.Text;
    using System.Threading;
    using System.Windows.Forms;

    internal class RequestBuilder : UserControl
    {
        private Button btnBuilderExecute;
        private Button btnTearOff;
        private CheckBox cbAutoAuthenticate;
        private CheckBox cbFixContentLength;
        private CheckBox cbFollowRedirects;
        private CheckBox cbSelectBuilderResult;
        private ComboBox cbxBuilderHTTPVersion;
        private ComboBox cbxBuilderMethod;
        private IContainer components;
        private Form frmFloatingBuilder;
        private GroupBox gbRequestOptions;
        private GroupBox gbUIOptions;
        private Label lblBuilderRequestBody;
        private Label lblBuilderRequestHeaders;
        private Label lblHintBuilder;
        private Label lblTearoff;
        private LinkLabel lnkBuilderHelp;
        private static RequestBuilder oRequestBuilder;
        private TabPage pageOptions;
        private TabPage pageParsed;
        private TabPage pageRaw;
        private TabControl tabsBuilder;
        private TextBox txtBuilderRequestBody;
        private TextBox txtBuilderRequestHeaders;
        private TextBox txtBuilderURL;
        private TextBox txtRaw;

        private RequestBuilder()
        {
            this.InitializeComponent();
            this.cbxBuilderHTTPVersion.SelectedIndex = 2;
            this.cbxBuilderMethod.SelectedIndex = 0;
            this.AllowDrop = true;
            base.DragEnter += new DragEventHandler(this.oRequestBuilder_DragEnter);
            base.DragLeave += new EventHandler(this.RequestBuilder_DragLeave);
            base.DragOver += new DragEventHandler(this.RequestBuilder_DragOver);
            base.DragDrop += new DragEventHandler(this.RequestBuilder_DragDrop);
            this.cbFollowRedirects.Checked = FiddlerApplication.Prefs.GetBoolPref("fiddler.requestbuilder.followredirects", true);
            this.cbAutoAuthenticate.Checked = FiddlerApplication.Prefs.GetBoolPref("fiddler.requestbuilder.autoauth", false);
            this.cbSelectBuilderResult.Checked = FiddlerApplication.Prefs.GetBoolPref("fiddler.requestbuilder.inspectsession", false);
        }

        private bool actSendRawRequest()
        {
            try
            {
                FiddlerApplication.oProxy.InjectCustomRequest(this.txtRaw.Text);
                return true;
            }
            catch (Exception exception)
            {
                FiddlerApplication.DoNotifyUser(exception.Message, "Custom Request Failed");
                return false;
            }
        }

        private bool actSendRequestFromWizard(bool bBreakRequest)
        {
            this.txtBuilderURL.Text = this.txtBuilderURL.Text.Trim();
            if (!this.txtBuilderURL.Text.StartsWith("http", StringComparison.OrdinalIgnoreCase))
            {
                if (this.txtBuilderURL.Text.Contains("://"))
                {
                    MessageBox.Show("Only HTTP:// and HTTPS:// URLs are supported.\n\nInvalid URI: " + this.txtBuilderURL.Text, "Invalid URI");
                    return false;
                }
                this.txtBuilderURL.Text = "http://" + this.txtBuilderURL.Text;
            }
            bool flag = this.txtBuilderURL.Text.Contains("#");
            int result = 0;
            int num2 = 10;
            if (flag)
            {
                string s = string.Empty;
                string str = frmPrompt.GetUserString("Sequential Requests Starting At", "All instances of the # character will be replaced with a consecutive integer starting at: ", "0", true);
                if (str == null)
                {
                    flag = false;
                }
                if (flag)
                {
                    s = frmPrompt.GetUserString("Sequential Requests Ending At", "End at: ", "10", true);
                    if (s == null)
                    {
                        flag = false;
                    }
                }
                if (flag && (!int.TryParse(str, out result) || !int.TryParse(s, out num2)))
                {
                    flag = false;
                }
            }
            try
            {
                StringBuilder builder;
            Label_0108:
                builder = new StringBuilder(0x400);
                string text = this.txtBuilderURL.Text;
                if (flag)
                {
                    text = text.Replace("#", result.ToString());
                }
                builder.AppendFormat("{0} {1} {2}\r\n", this.cbxBuilderMethod.Text, text, this.cbxBuilderHTTPVersion.Text);
                builder.Append(this.txtBuilderRequestHeaders.Text.Trim());
                builder.Append("\r\n\r\n");
                HTTPRequestHeaders oHeaders = Parser.ParseRequest(builder.ToString());
                builder = null;
                byte[] bytes = Utilities.getEntityBodyEncoding(oHeaders, null).GetBytes(this.txtBuilderRequestBody.Text);
                string str4 = this.txtBuilderURL.Text;
                int index = str4.IndexOf("//", StringComparison.Ordinal);
                if (index > -1)
                {
                    str4 = str4.Substring(index + 2);
                }
                int length = str4.IndexOfAny(new char[] { '/', '?' });
                if (length > -1)
                {
                    str4 = str4.Substring(0, length).ToLower();
                }
                oHeaders["Host"] = str4;
                if (flag && oHeaders.ExistsAndContains("Host", "#"))
                {
                    oHeaders["Host"] = oHeaders["Host"].Replace("#", result.ToString());
                }
                if ((this.cbFixContentLength.Checked && (oHeaders.Exists("Content-Length") || (bytes.Length > 0))) && !oHeaders.ExistsAndContains("Transfer-Encoding", "chunked"))
                {
                    oHeaders["Content-Length"] = bytes.Length.ToString();
                }
                this.txtBuilderRequestHeaders.Text = oHeaders.ToString(false, false);
                Session session = new Session((HTTPRequestHeaders) oHeaders.Clone(), bytes);
                session.SetBitFlag(SessionFlags.RequestGeneratedByFiddler, true);
                session.oFlags["x-From-Builder"] = "true";
                bool flag2 = this.cbSelectBuilderResult.Checked;
                if (bBreakRequest)
                {
                    session.oFlags["x-breakrequest"] = "Builder";
                    flag2 = true;
                }
                if (flag)
                {
                    if (session.oRequest.headers.ExistsAndContains("Referer", "#"))
                    {
                        session.oRequest["Referer"] = session.oRequest["Referer"].Replace("#", result.ToString());
                    }
                }
                else if (flag2)
                {
                    session.oFlags["x-Builder-Inspect"] = "1";
                }
                if (this.cbAutoAuthenticate.Checked)
                {
                    session.oFlags["x-AutoAuth"] = "(default)";
                }
                if (this.cbFollowRedirects.Checked)
                {
                    session.oFlags["x-Builder-MaxRedir"] = FiddlerApplication.Prefs.GetInt32Pref("fiddler.requestbuilder.followredirects.max", 10).ToString();
                }
                ThreadPool.UnsafeQueueUserWorkItem(new WaitCallback(session.Execute), null);
                if (flag && (result < num2))
                {
                    result++;
                    goto Label_0108;
                }
                return true;
            }
            catch (Exception exception)
            {
                FiddlerApplication.DoNotifyUser(exception.Message, "Custom Request Failed");
                return false;
            }
        }

        private void btnBuilderExecute_Click(object sender, EventArgs e)
        {
            if (this.tabsBuilder.SelectedTab == this.pageRaw)
            {
                if (CONFIG.IsMicrosoftMachine)
                {
                    FiddlerApplication.logSelfHost(40);
                }
                this.actSendRawRequest();
            }
            else
            {
                if (CONFIG.IsMicrosoftMachine)
                {
                    FiddlerApplication.logSelfHost(0x29);
                }
                this.actSendRequestFromWizard(Utilities.GetAsyncKeyState(0x10) < 0);
            }
        }

        private void btnTearOff_Click(object sender, EventArgs e)
        {
            this.frmFloatingBuilder = new Form();
            this.frmFloatingBuilder.FormBorderStyle = FormBorderStyle.SizableToolWindow;
            this.frmFloatingBuilder.Width = 0x1d8;
            this.frmFloatingBuilder.Height = 500;
            this.frmFloatingBuilder.StartPosition = FormStartPosition.Manual;
            this.frmFloatingBuilder.Left = (base.Left + base.Width) - 500;
            this.frmFloatingBuilder.Top = base.Top + 100;
            this.frmFloatingBuilder.Text = "Request Builder";
            FiddlerApplication.UI.tabsViews.TabPages.Remove(FiddlerApplication.UI.pageBuilder);
            this.frmFloatingBuilder.Controls.Add(this);
            this.cbSelectBuilderResult.Checked = true;
            this.gbUIOptions.Visible = false;
            this.frmFloatingBuilder.Icon = FiddlerApplication.UI.Icon;
            this.frmFloatingBuilder.FormClosing += new FormClosingEventHandler(this.frmFloatingBuilder_FormClosing);
            this.frmFloatingBuilder.Show(FiddlerApplication.UI);
        }

        private void Builder_BodyOrMethodChanged(object sender, EventArgs e)
        {
            if (Utilities.HTTPMethodAllowsBody(this.cbxBuilderMethod.Text.ToUpper()))
            {
                this.txtBuilderRequestBody.BackColor = Color.FromKnownColor(KnownColor.Window);
            }
            else
            {
                this.txtBuilderRequestBody.BackColor = (this.txtBuilderRequestBody.Text.Length > 0) ? Color.Red : Color.FromKnownColor(KnownColor.Control);
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

        internal static void EnsureReady()
        {
            oRequestBuilder = new RequestBuilder();
            oRequestBuilder.Parent = FiddlerApplication.UI.pageBuilder;
            oRequestBuilder.Dock = DockStyle.Fill;
        }

        private void frmFloatingBuilder_FormClosing(object sender, FormClosingEventArgs e)
        {
            FiddlerApplication.UI.tabsViews.Controls.Add(FiddlerApplication.UI.pageBuilder);
            FiddlerApplication.UI.pageBuilder.Dock = DockStyle.Fill;
            oRequestBuilder.Parent = FiddlerApplication.UI.pageBuilder;
            oRequestBuilder.Dock = DockStyle.Fill;
            this.gbUIOptions.Visible = true;
            this.cbSelectBuilderResult.Checked = false;
        }

        private void HandleSelectAllKeydown(object sender, KeyEventArgs e)
        {
            if (e.Control && (e.KeyCode == Keys.A))
            {
                (sender as TextBox).SelectAll();
                e.SuppressKeyPress = true;
            }
        }

        private void InitializeComponent()
        {
            this.btnBuilderExecute = new Button();
            this.lblHintBuilder = new Label();
            this.tabsBuilder = new TabControl();
            this.pageParsed = new TabPage();
            this.lnkBuilderHelp = new LinkLabel();
            this.txtBuilderRequestBody = new TextBox();
            this.txtBuilderRequestHeaders = new TextBox();
            this.lblBuilderRequestBody = new Label();
            this.lblBuilderRequestHeaders = new Label();
            this.cbxBuilderHTTPVersion = new ComboBox();
            this.cbxBuilderMethod = new ComboBox();
            this.txtBuilderURL = new TextBox();
            this.pageRaw = new TabPage();
            this.txtRaw = new TextBox();
            this.pageOptions = new TabPage();
            this.gbRequestOptions = new GroupBox();
            this.cbAutoAuthenticate = new CheckBox();
            this.cbFollowRedirects = new CheckBox();
            this.cbFixContentLength = new CheckBox();
            this.cbSelectBuilderResult = new CheckBox();
            this.gbUIOptions = new GroupBox();
            this.lblTearoff = new Label();
            this.btnTearOff = new Button();
            this.tabsBuilder.SuspendLayout();
            this.pageParsed.SuspendLayout();
            this.pageRaw.SuspendLayout();
            this.pageOptions.SuspendLayout();
            this.gbRequestOptions.SuspendLayout();
            this.gbUIOptions.SuspendLayout();
            base.SuspendLayout();
            this.btnBuilderExecute.Anchor = AnchorStyles.Right | AnchorStyles.Top;
            this.btnBuilderExecute.Location = new Point(540, 3);
            this.btnBuilderExecute.Name = "btnBuilderExecute";
            this.btnBuilderExecute.Size = new Size(0x3e, 0x26);
            this.btnBuilderExecute.TabIndex = 14;
            this.btnBuilderExecute.Text = "E&xecute";
            this.btnBuilderExecute.Click += new EventHandler(this.btnBuilderExecute_Click);
            this.lblHintBuilder.Anchor = AnchorStyles.Right | AnchorStyles.Left | AnchorStyles.Top;
            this.lblHintBuilder.BackColor = Color.Gray;
            this.lblHintBuilder.Font = new Font("Tahoma", 8.25f, FontStyle.Regular, GraphicsUnit.Point, 0);
            this.lblHintBuilder.ForeColor = Color.White;
            this.lblHintBuilder.Location = new Point(3, 3);
            this.lblHintBuilder.Name = "lblHintBuilder";
            this.lblHintBuilder.Padding = new Padding(4);
            this.lblHintBuilder.Size = new Size(0x217, 0x26);
            this.lblHintBuilder.TabIndex = 0x11;
            this.lblHintBuilder.Text = "Use this page to handcraft a HTTP Request. You can clone a prior request by dragging and dropping a session from the Web Sessions list.";
            this.lblHintBuilder.TextAlign = ContentAlignment.MiddleLeft;
            this.tabsBuilder.Anchor = AnchorStyles.Right | AnchorStyles.Left | AnchorStyles.Bottom | AnchorStyles.Top;
            this.tabsBuilder.Controls.Add(this.pageParsed);
            this.tabsBuilder.Controls.Add(this.pageRaw);
            this.tabsBuilder.Controls.Add(this.pageOptions);
            this.tabsBuilder.Location = new Point(3, 0x2c);
            this.tabsBuilder.Multiline = true;
            this.tabsBuilder.Name = "tabsBuilder";
            this.tabsBuilder.SelectedIndex = 0;
            this.tabsBuilder.Size = new Size(600, 0x1bb);
            this.tabsBuilder.TabIndex = 0;
            this.tabsBuilder.SelectedIndexChanged += new EventHandler(this.tabsBuilder_SelectedIndexChanged);
            this.pageParsed.Controls.Add(this.txtBuilderRequestHeaders);
            this.pageParsed.Controls.Add(this.lnkBuilderHelp);
            this.pageParsed.Controls.Add(this.txtBuilderRequestBody);
            this.pageParsed.Controls.Add(this.lblBuilderRequestBody);
            this.pageParsed.Controls.Add(this.lblBuilderRequestHeaders);
            this.pageParsed.Controls.Add(this.cbxBuilderHTTPVersion);
            this.pageParsed.Controls.Add(this.cbxBuilderMethod);
            this.pageParsed.Controls.Add(this.txtBuilderURL);
            this.pageParsed.Location = new Point(4, 0x16);
            this.pageParsed.Name = "pageParsed";
            this.pageParsed.Padding = new Padding(3);
            this.pageParsed.Size = new Size(0x250, 0x1a1);
            this.pageParsed.TabIndex = 0;
            this.pageParsed.Text = "Parsed";
            this.pageParsed.UseVisualStyleBackColor = true;
            this.lnkBuilderHelp.Anchor = AnchorStyles.Right | AnchorStyles.Top;
            this.lnkBuilderHelp.AutoSize = true;
            this.lnkBuilderHelp.LinkBehavior = LinkBehavior.HoverUnderline;
            this.lnkBuilderHelp.Location = new Point(0x222, 0x20);
            this.lnkBuilderHelp.Name = "lnkBuilderHelp";
            this.lnkBuilderHelp.Size = new Size(40, 13);
            this.lnkBuilderHelp.TabIndex = 30;
            this.lnkBuilderHelp.TabStop = true;
            this.lnkBuilderHelp.Text = "Help...";
            this.lnkBuilderHelp.TextAlign = ContentAlignment.MiddleRight;
            this.lnkBuilderHelp.LinkClicked += new LinkLabelLinkClickedEventHandler(this.lnkBuilderHelp_LinkClicked);
            this.txtBuilderRequestBody.Anchor = AnchorStyles.Right | AnchorStyles.Left | AnchorStyles.Bottom | AnchorStyles.Top;
            this.txtBuilderRequestBody.BackColor = SystemColors.Control;
            this.txtBuilderRequestBody.Location = new Point(7, 0x12e);
            this.txtBuilderRequestBody.MaxLength = 0x1000000;
            this.txtBuilderRequestBody.Multiline = true;
            this.txtBuilderRequestBody.Name = "txtBuilderRequestBody";
            this.txtBuilderRequestBody.ScrollBars = ScrollBars.Both;
            this.txtBuilderRequestBody.Size = new Size(0x242, 0x6b);
            this.txtBuilderRequestBody.TabIndex = 6;
            this.txtBuilderRequestBody.WordWrap = false;
            this.txtBuilderRequestBody.TextChanged += new EventHandler(this.Builder_BodyOrMethodChanged);
            this.txtBuilderRequestBody.KeyDown += new KeyEventHandler(this.HandleSelectAllKeydown);
            this.txtBuilderRequestHeaders.AcceptsReturn = true;
            this.txtBuilderRequestHeaders.Anchor = AnchorStyles.Right | AnchorStyles.Left | AnchorStyles.Top;
            this.txtBuilderRequestHeaders.BackColor = SystemColors.Window;
            this.txtBuilderRequestHeaders.Location = new Point(7, 0x34);
            this.txtBuilderRequestHeaders.MaxLength = 0x400000;
            this.txtBuilderRequestHeaders.Multiline = true;
            this.txtBuilderRequestHeaders.Name = "txtBuilderRequestHeaders";
            this.txtBuilderRequestHeaders.ScrollBars = ScrollBars.Both;
            this.txtBuilderRequestHeaders.Size = new Size(0x242, 0xe9);
            this.txtBuilderRequestHeaders.TabIndex = 4;
            this.txtBuilderRequestHeaders.Text = "User-Agent: Fiddler";
            this.txtBuilderRequestHeaders.WordWrap = false;
            this.txtBuilderRequestHeaders.KeyDown += new KeyEventHandler(this.HandleSelectAllKeydown);
            this.lblBuilderRequestBody.Font = new Font("Tahoma", 8.25f, FontStyle.Regular, GraphicsUnit.Point, 0);
            this.lblBuilderRequestBody.Location = new Point(7, 0x120);
            this.lblBuilderRequestBody.Name = "lblBuilderRequestBody";
            this.lblBuilderRequestBody.Size = new Size(0x88, 14);
            this.lblBuilderRequestBody.TabIndex = 5;
            this.lblBuilderRequestBody.Text = "Request Body";
            this.lblBuilderRequestHeaders.Font = new Font("Tahoma", 8.25f, FontStyle.Regular, GraphicsUnit.Point, 0);
            this.lblBuilderRequestHeaders.Location = new Point(7, 0x26);
            this.lblBuilderRequestHeaders.Name = "lblBuilderRequestHeaders";
            this.lblBuilderRequestHeaders.Size = new Size(0x88, 0x10);
            this.lblBuilderRequestHeaders.TabIndex = 3;
            this.lblBuilderRequestHeaders.Text = "Request Headers";
            this.cbxBuilderHTTPVersion.Anchor = AnchorStyles.Right | AnchorStyles.Top;
            this.cbxBuilderHTTPVersion.Items.AddRange(new object[] { "HTTP/2.0", "HTTP/1.2", "HTTP/1.1", "HTTP/1.0", "HTTP/0.9" });
            this.cbxBuilderHTTPVersion.Location = new Point(0x1e4, 8);
            this.cbxBuilderHTTPVersion.Name = "cbxBuilderHTTPVersion";
            this.cbxBuilderHTTPVersion.Size = new Size(0x65, 0x15);
            this.cbxBuilderHTTPVersion.TabIndex = 2;
            this.cbxBuilderMethod.Items.AddRange(new object[] { "GET", "POST", "PUT", "HEAD", "TRACE", "DELETE", "SEARCH", "CONNECT", "PROPFIND", "PROPPATCH", "MKCOL", "COPY", "MOVE", "LOCK", "UNLOCK", "OPTIONS" });
            this.cbxBuilderMethod.Location = new Point(7, 8);
            this.cbxBuilderMethod.Name = "cbxBuilderMethod";
            this.cbxBuilderMethod.Size = new Size(0x56, 0x15);
            this.cbxBuilderMethod.TabIndex = 0;
            this.cbxBuilderMethod.TextChanged += new EventHandler(this.Builder_BodyOrMethodChanged);
            this.txtBuilderURL.Anchor = AnchorStyles.Right | AnchorStyles.Left | AnchorStyles.Top;
            this.txtBuilderURL.Location = new Point(0x5d, 8);
            this.txtBuilderURL.Name = "txtBuilderURL";
            this.txtBuilderURL.Size = new Size(0x185, 0x15);
            this.txtBuilderURL.TabIndex = 1;
            this.txtBuilderURL.Text = "http://www.example.com/";
            this.txtBuilderURL.KeyDown += new KeyEventHandler(this.txtBuilderURL_KeyDown);
            this.txtBuilderURL.KeyPress += new KeyPressEventHandler(this.txtBuilderURL_KeyPress);
            this.pageRaw.Controls.Add(this.txtRaw);
            this.pageRaw.Location = new Point(4, 0x16);
            this.pageRaw.Name = "pageRaw";
            this.pageRaw.Padding = new Padding(3);
            this.pageRaw.Size = new Size(0x250, 0x1a1);
            this.pageRaw.TabIndex = 1;
            this.pageRaw.Text = "Raw";
            this.pageRaw.UseVisualStyleBackColor = true;
            this.txtRaw.AcceptsReturn = true;
            this.txtRaw.BackColor = SystemColors.Window;
            this.txtRaw.Dock = DockStyle.Fill;
            this.txtRaw.Location = new Point(3, 3);
            this.txtRaw.MaxLength = 0x400000;
            this.txtRaw.Multiline = true;
            this.txtRaw.Name = "txtRaw";
            this.txtRaw.ScrollBars = ScrollBars.Both;
            this.txtRaw.Size = new Size(0x24a, 0x19b);
            this.txtRaw.TabIndex = 0x1c;
            this.txtRaw.WordWrap = false;
            this.pageOptions.Controls.Add(this.gbRequestOptions);
            this.pageOptions.Controls.Add(this.gbUIOptions);
            this.pageOptions.Location = new Point(4, 0x16);
            this.pageOptions.Name = "pageOptions";
            this.pageOptions.Padding = new Padding(3);
            this.pageOptions.Size = new Size(0x250, 0x1a1);
            this.pageOptions.TabIndex = 2;
            this.pageOptions.Text = "Options";
            this.pageOptions.UseVisualStyleBackColor = true;
            this.gbRequestOptions.Controls.Add(this.cbAutoAuthenticate);
            this.gbRequestOptions.Controls.Add(this.cbFollowRedirects);
            this.gbRequestOptions.Controls.Add(this.cbFixContentLength);
            this.gbRequestOptions.Controls.Add(this.cbSelectBuilderResult);
            this.gbRequestOptions.Location = new Point(0x12, 0x10);
            this.gbRequestOptions.Name = "gbRequestOptions";
            this.gbRequestOptions.Size = new Size(0x184, 0x7e);
            this.gbRequestOptions.TabIndex = 0;
            this.gbRequestOptions.TabStop = false;
            this.gbRequestOptions.Text = "Request Options";
            this.cbAutoAuthenticate.AutoSize = true;
            this.cbAutoAuthenticate.Location = new Point(9, 0x59);
            this.cbAutoAuthenticate.Name = "cbAutoAuthenticate";
            this.cbAutoAuthenticate.Size = new Size(0x97, 0x11);
            this.cbAutoAuthenticate.TabIndex = 3;
            this.cbAutoAuthenticate.Text = "Automatically Authenticate";
            this.cbAutoAuthenticate.UseVisualStyleBackColor = true;
            this.cbFollowRedirects.AutoSize = true;
            this.cbFollowRedirects.Checked = true;
            this.cbFollowRedirects.CheckState = CheckState.Checked;
            this.cbFollowRedirects.Location = new Point(9, 0x42);
            this.cbFollowRedirects.Name = "cbFollowRedirects";
            this.cbFollowRedirects.Size = new Size(0x68, 0x11);
            this.cbFollowRedirects.TabIndex = 2;
            this.cbFollowRedirects.Text = "Follow Redirects";
            this.cbFollowRedirects.UseVisualStyleBackColor = true;
            this.cbFixContentLength.AutoSize = true;
            this.cbFixContentLength.Checked = true;
            this.cbFixContentLength.CheckState = CheckState.Checked;
            this.cbFixContentLength.Location = new Point(9, 0x2b);
            this.cbFixContentLength.Name = "cbFixContentLength";
            this.cbFixContentLength.Size = new Size(0x97, 0x11);
            this.cbFixContentLength.TabIndex = 1;
            this.cbFixContentLength.Text = "Fix Content-Length header";
            this.cbFixContentLength.UseVisualStyleBackColor = true;
            this.cbSelectBuilderResult.AutoSize = true;
            this.cbSelectBuilderResult.Location = new Point(9, 20);
            this.cbSelectBuilderResult.Name = "cbSelectBuilderResult";
            this.cbSelectBuilderResult.Size = new Size(0x65, 0x11);
            this.cbSelectBuilderResult.TabIndex = 0;
            this.cbSelectBuilderResult.Text = "Inspect Session";
            this.cbSelectBuilderResult.UseVisualStyleBackColor = true;
            this.gbUIOptions.Controls.Add(this.lblTearoff);
            this.gbUIOptions.Controls.Add(this.btnTearOff);
            this.gbUIOptions.Location = new Point(0x12, 0x94);
            this.gbUIOptions.Name = "gbUIOptions";
            this.gbUIOptions.Size = new Size(0x184, 100);
            this.gbUIOptions.TabIndex = 1;
            this.gbUIOptions.TabStop = false;
            this.gbUIOptions.Text = "UI Options";
            this.lblTearoff.AutoSize = true;
            this.lblTearoff.Location = new Point(6, 0x1a);
            this.lblTearoff.Name = "lblTearoff";
            this.lblTearoff.Size = new Size(0x12e, 13);
            this.lblTearoff.TabIndex = 0;
            this.lblTearoff.Text = "You can \"tear off\" the Request Builder into a floating window.";
            this.btnTearOff.Location = new Point(9, 0x33);
            this.btnTearOff.Name = "btnTearOff";
            this.btnTearOff.Size = new Size(0x4b, 0x17);
            this.btnTearOff.TabIndex = 1;
            this.btnTearOff.Text = "Tear off";
            this.btnTearOff.UseVisualStyleBackColor = true;
            this.btnTearOff.Click += new EventHandler(this.btnTearOff_Click);
            base.AutoScaleDimensions = new SizeF(6f, 13f);
            base.AutoScaleMode = AutoScaleMode.Font;
            base.Controls.Add(this.tabsBuilder);
            base.Controls.Add(this.btnBuilderExecute);
            base.Controls.Add(this.lblHintBuilder);
            this.Font = new Font("Tahoma", 8.25f);
            base.Name = "RequestBuilder";
            base.Size = new Size(0x25d, 490);
            this.tabsBuilder.ResumeLayout(false);
            this.pageParsed.ResumeLayout(false);
            this.pageParsed.PerformLayout();
            this.pageRaw.ResumeLayout(false);
            this.pageRaw.PerformLayout();
            this.pageOptions.ResumeLayout(false);
            this.gbRequestOptions.ResumeLayout(false);
            this.gbRequestOptions.PerformLayout();
            this.gbUIOptions.ResumeLayout(false);
            this.gbUIOptions.PerformLayout();
            base.ResumeLayout(false);
        }

        private void lnkBuilderHelp_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            Utilities.LaunchHyperlink(CONFIG.GetUrl("REDIR") + "REQUESTBUILDER");
        }

        private void oRequestBuilder_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent("Fiddler.Session[]"))
            {
                e.Effect = DragDropEffects.Copy;
                this.BackColor = Color.Lime;
            }
            else
            {
                e.Effect = DragDropEffects.None;
            }
        }

        private void RequestBuilder_DragDrop(object sender, DragEventArgs e)
        {
            this.BackColor = Color.FromKnownColor(KnownColor.Control);
            Session[] data = (Session[]) e.Data.GetData("Fiddler.Session[]");
            if ((data != null) && (data.Length >= 1))
            {
                Session oSession = data[0];
                data = null;
                this.cbxBuilderHTTPVersion.Text = oSession.oRequest.headers.HTTPVersion;
                this.cbxBuilderMethod.Text = oSession.oRequest.headers.HTTPMethod;
                this.txtBuilderURL.Text = oSession.fullUrl;
                if (oSession.oRequest.headers != null)
                {
                    this.txtBuilderRequestHeaders.Text = oSession.oRequest.headers.ToString(false, false);
                    this.txtRaw.Text = oSession.oRequest.headers.ToString(true, true);
                }
                else
                {
                    this.txtBuilderRequestHeaders.Clear();
                    this.txtRaw.Clear();
                }
                if (oSession.requestBodyBytes != null)
                {
                    this.txtBuilderRequestBody.Text = Utilities.GetStringFromArrayRemovingBOM(oSession.requestBodyBytes, Utilities.getResponseBodyEncoding(oSession));
                    this.txtRaw.Text = this.txtRaw.Text + this.txtBuilderRequestBody.Text;
                }
                else
                {
                    this.txtBuilderRequestBody.Clear();
                }
            }
        }

        private void RequestBuilder_DragLeave(object sender, EventArgs e)
        {
            this.BackColor = Color.FromKnownColor(KnownColor.Control);
        }

        private void RequestBuilder_DragOver(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent("Fiddler.Session[]"))
            {
                e.Effect = DragDropEffects.Copy;
            }
            else
            {
                e.Effect = DragDropEffects.None;
            }
        }

        private void SavePreferences()
        {
            FiddlerApplication.Prefs.SetBoolPref("fiddler.requestbuilder.followredirects", this.cbFollowRedirects.Checked);
            FiddlerApplication.Prefs.SetBoolPref("fiddler.requestbuilder.autoauth", this.cbAutoAuthenticate.Checked);
            FiddlerApplication.Prefs.SetBoolPref("fiddler.requestbuilder.inspectsession", this.cbSelectBuilderResult.Checked);
        }

        internal static void SavePrefs()
        {
            if (oRequestBuilder != null)
            {
                oRequestBuilder.SavePreferences();
            }
        }

        private void tabsBuilder_SelectedIndexChanged(object sender, EventArgs e)
        {
            TabPage pageRaw = this.pageRaw;
            TabPage selectedTab = this.tabsBuilder.SelectedTab;
        }

        private void txtBuilderURL_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Control && (e.KeyCode == Keys.A))
            {
                this.txtBuilderURL.SelectAll();
                e.SuppressKeyPress = true;
            }
        }

        private void txtBuilderURL_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar == '\r')
            {
                e.Handled = true;
                this.btnBuilderExecute_Click(this, null);
            }
        }
    }
}

