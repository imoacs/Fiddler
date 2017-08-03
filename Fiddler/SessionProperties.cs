namespace Fiddler
{
    using System;
    using System.Collections;
    using System.ComponentModel;
    using System.Drawing;
    using System.Globalization;
    using System.Windows.Forms;

    internal class SessionProperties : Form
    {
        private Container components;
        private Session mySession;
        private StatusBar sbStatus;
        private RichTextBox txtProperties;

        internal SessionProperties(Session oSession)
        {
            this.InitializeComponent();
            this.txtProperties.BackColor = CONFIG.colorDisabledEdit;
            this.txtProperties.Font = new Font(this.txtProperties.Font.FontFamily, CONFIG.flFontSize);
            this.mySession = oSession;
            this.RefreshInfo();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing && (this.components != null))
            {
                this.components.Dispose();
            }
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            ComponentResourceManager manager = new ComponentResourceManager(typeof(SessionProperties));
            this.sbStatus = new StatusBar();
            this.txtProperties = new RichTextBox();
            base.SuspendLayout();
            this.sbStatus.Location = new Point(0, 0x214);
            this.sbStatus.Name = "sbStatus";
            this.sbStatus.Size = new Size(0x204, 20);
            this.sbStatus.TabIndex = 1;
            this.sbStatus.Text = "Hit ESC to close, F5 to refresh.";
            this.txtProperties.BorderStyle = BorderStyle.None;
            this.txtProperties.DetectUrls = false;
            this.txtProperties.Dock = DockStyle.Fill;
            this.txtProperties.Font = new Font("Lucida Console", 8.25f, FontStyle.Regular, GraphicsUnit.Point, 0);
            this.txtProperties.Location = new Point(0, 0);
            this.txtProperties.Name = "txtProperties";
            this.txtProperties.ReadOnly = true;
            this.txtProperties.Size = new Size(0x204, 0x214);
            this.txtProperties.TabIndex = 2;
            this.txtProperties.Text = "";
            this.txtProperties.WordWrap = false;
            this.AutoScaleBaseSize = new Size(5, 13);
            base.ClientSize = new Size(0x204, 0x228);
            base.Controls.Add(this.txtProperties);
            base.Controls.Add(this.sbStatus);
            base.FormBorderStyle = FormBorderStyle.SizableToolWindow;
            base.Icon = (Icon)Properties.Resources.sbpCapture_Icon; //manager.GetObject("$this.Icon");
            base.KeyPreview = true;
            base.Name = "SessionProperties";
            base.ShowInTaskbar = false;
            base.StartPosition = FormStartPosition.Manual;
            this.Text = "Session Properties";
            base.KeyDown += new KeyEventHandler(this.SessionProperties_KeyDown);
            base.KeyUp += new KeyEventHandler(this.SessionProperties_KeyUp);
            base.ResumeLayout(false);
        }

        private void RefreshInfo()
        {
            try
            {
                this.Text = string.Concat(new object[] { "Session Properties (", this.mySession.id, ") ", this.mySession.url });
                this.txtProperties.Clear();
                this.txtProperties.AppendText(string.Format("SESSION STATE: {0}.\n", this.mySession.state.ToString("g")));
                if (this.mySession.state == SessionStates.ReadingResponse)
                {
                    long num;
                    this.txtProperties.AppendText(string.Format("DOWNLOAD PROGRESS: {0:N0}", this.mySession.oResponse._PeekDownloadProgress));
                    if ((this.mySession.oResponse["Content-Length"] != string.Empty) && long.TryParse(this.mySession.oResponse["Content-Length"], NumberStyles.Integer, NumberFormatInfo.InvariantInfo, out num))
                    {
                        this.txtProperties.AppendText(string.Format(" of {0:N0}", num));
                    }
                    this.txtProperties.AppendText(" bytes.\n\n");
                }
                if ((this.mySession.state >= SessionStates.SendingRequest) && this.mySession.isFlagSet(SessionFlags.SentToGateway))
                {
                    this.txtProperties.AppendText("The request was forwarded to the gateway.\n");
                }
                if ((this.mySession.requestBodyBytes != null) && (this.mySession.requestBodyBytes.Length > 0))
                {
                    this.txtProperties.AppendText(string.Format("Request Entity Size: {0:N0} bytes.\n", this.mySession.requestBodyBytes.LongLength.ToString()));
                }
                if (this.mySession.responseBodyBytes != null)
                {
                    this.txtProperties.AppendText(string.Format("Response Entity Size: {0:N0} bytes.\n", this.mySession.responseBodyBytes.LongLength.ToString()));
                }
                this.txtProperties.AppendText("\n== FLAGS ==================\n");
                this.txtProperties.AppendText(string.Format("BitFlags: [{0}] 0x{1}\n", this.mySession.BitFlags, ((int) this.mySession.BitFlags).ToString("x")));
                foreach (DictionaryEntry entry in this.mySession.oFlags)
                {
                    this.txtProperties.AppendText(string.Format("{0}: {1}\n", (entry.Key as string).ToUpper(), entry.Value as string));
                }
                this.txtProperties.AppendText("\n== TIMING INFO ============\n");
                this.txtProperties.AppendText(this.mySession.Timers.ToString(true));
                if (this.mySession.state >= SessionStates.ReadingResponse)
                {
                    this.txtProperties.AppendText(this.mySession.isFlagSet(SessionFlags.ResponseStreamed) ? "\nThe response was streamed to the client as it was received.\n" : "\nThe response was buffered before delivery to the client.\n");
                }
                this.txtProperties.AppendText("\n== WININET CACHE INFO ============\n");
                this.txtProperties.AppendText(WinINETCache.GetCacheItemInfo(this.mySession.fullUrl));
                this.txtProperties.AppendText("\n* Note: Data above shows WinINET's current cache state, not the state at the time of the request.\n");
                if (Environment.OSVersion.Version.Major > 5)
                {
                    this.txtProperties.AppendText("* Note: Data above shows WinINET's Medium Integrity (non-Protected Mode) cache only.\n");
                }
                this.txtProperties.AppendText("\n");
                this.txtProperties.Select(0, 0);
            }
            catch (Exception exception)
            {
                this.txtProperties.Clear();
                this.txtProperties.AppendText(exception.Message + "\r\n" + exception.StackTrace);
            }
        }

        private void SessionProperties_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.F5)
            {
                this.RefreshInfo();
            }
        }

        private void SessionProperties_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Escape)
            {
                e.SuppressKeyPress = true;
                base.Close();
            }
        }
    }
}

