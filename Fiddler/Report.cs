namespace Fiddler
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Drawing;
    using System.Drawing.Drawing2D;
    using System.Windows.Forms;

    internal class Report : UserControl
    {
        private bool bLazyGraphicsReady;
        private SolidBrush brushBackground;
        private LinearGradientBrush brushCSS;
        private Brush[] brushes;
        private LinearGradientBrush brushGif;
        private LinearGradientBrush brushHeaders;
        private LinearGradientBrush brushHTML;
        private LinearGradientBrush brushJpeg;
        private LinearGradientBrush brushJScript;
        private LinearGradientBrush brushPng;
        private Container components;
        private Font fontArial;
        private Font fontTahoma;
        private LinkLabel lblShowChart;
        private LinkLabel lnkCopyChart;
        private PictureBox pbPie;
        private Panel pnlPies;
        private TextBox txtReport;

        internal Report()
        {
            this.InitializeComponent();
            this.txtReport.ForeColor = Color.FromKnownColor(KnownColor.WindowText);
            this.txtReport.BackColor = CONFIG.colorDisabledEdit;
            FiddlerApplication.CalculateReport += new CalculateReportHandler(this.FiddlerApplication_CalculateReport);
            string stringPref = FiddlerApplication.Prefs.GetStringPref("fiddler.welcomemsg", null);
            if (!string.IsNullOrEmpty(stringPref))
            {
                stringPref = stringPref.Replace("\n", "\r\n");
                this.txtReport.Text = stringPref + "\r\n\r\n" + this.txtReport.Text;
            }
            else
            {
                stringPref = "Thanks for using Fiddler! If you need help or have feedback to share, please use the Help menu.\r\n\r\n\t-Eric Lawrence\r\n";
            }
            if (CONFIG.IsMicrosoftMachine)
            {
                this.txtReport.Text = "Select one or more sessions in the Web Sessions list to view performance statistics.\r\n\r\n================================================================\r\n\r\n" + stringPref + "\r\n\r\nLearn more about Microsoft grassroots innovation @ http://garage. The C# class library known as \"FiddlerCore\" is available for integration into your applications; contact me for details.\r\n\r\n";
            }
            else
            {
                this.txtReport.Text = "Select one or more sessions in the Web Sessions list to view performance statistics.\r\n\r\n================================================================\r\n\r\n" + stringPref;
            }
        }

        public void Clear()
        {
            this.txtReport.Text = "Select one or more sessions in the Web Sessions list to view performance statistics.";
            this.pbPie.Image = null;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing && (this.components != null))
            {
                this.components.Dispose();
            }
            base.Dispose(disposing);
        }

        private Bitmap DrawContentTypePie(Dictionary<string, long> dictSizeForContentType, long cTotal)
        {
            if (cTotal < 1L)
            {
                return null;
            }
            if (!this.bLazyGraphicsReady)
            {
                this.LazyInitGraphics();
            }
            int width = Math.Min(this.pbPie.Width, this.pbPie.Height - 20);
            int height = Math.Min(this.pbPie.Width, this.pbPie.Height - 20);
            Bitmap image = new Bitmap(width, height);
            Graphics graphics = Graphics.FromImage(image);
            graphics.SmoothingMode = SmoothingMode.AntiAlias;
            graphics.FillRectangle(this.brushBackground, -1, -1, width + 1, height + 1);
            float startAngle = 0f;
            int num4 = 0;
            foreach (string str in dictSizeForContentType.Keys)
            {
                float num5 = (float) dictSizeForContentType[str];
                float sweepAngle = 360f * (num5 / ((float) cTotal));
                switch (str)
                {
                    case "~headers~":
                        graphics.FillPie(this.brushHeaders, 0f, 0f, (float) width, (float) height, startAngle, sweepAngle);
                        break;

                    case "text/html":
                        graphics.FillPie(this.brushHTML, 0f, 0f, (float) width, (float) height, startAngle, sweepAngle);
                        break;

                    case "text/css":
                        graphics.FillPie(this.brushCSS, 0f, 0f, (float) width, (float) height, startAngle, sweepAngle);
                        break;

                    case "application/x-javascript":
                    case "application/javascript":
                    case "text/javascript":
                        graphics.FillPie(this.brushJScript, 0f, 0f, (float) width, (float) height, startAngle, sweepAngle);
                        break;

                    case "image/jpeg":
                    case "image/jpg":
                        graphics.FillPie(this.brushJpeg, 0f, 0f, (float) width, (float) height, startAngle, sweepAngle);
                        break;

                    case "image/gif":
                        graphics.FillPie(this.brushGif, 0f, 0f, (float) width, (float) height, startAngle, sweepAngle);
                        break;

                    case "image/png":
                        graphics.FillPie(this.brushPng, 0f, 0f, (float) width, (float) height, startAngle, sweepAngle);
                        break;

                    default:
                        graphics.FillPie(this.brushes[num4 % this.brushes.Length], 0f, 0f, (float) width, (float) height, startAngle, sweepAngle);
                        num4++;
                        break;
                }
                startAngle += sweepAngle;
            }
            Point point = new Point(width / 2, height / 2);
            graphics.TranslateTransform((float) point.X, (float) point.Y);
            startAngle = 0f;
            float x = 0f;
            foreach (string str2 in dictSizeForContentType.Keys)
            {
                float num8 = (float) dictSizeForContentType[str2];
                float angle = 360f * (num8 / ((float) cTotal));
                if (angle >= 5.0)
                {
                    graphics.RotateTransform(angle);
                    StringFormat format = new StringFormat(StringFormatFlags.NoClip) {
                        Alignment = StringAlignment.Near,
                        LineAlignment = StringAlignment.Far
                    };
                    string text = Utilities.TrimBefore(str2, '/');
                    if ((text == null) || (text.Length < 1))
                    {
                        text = "?";
                    }
                    Font fontArial = this.fontArial;
                    if ((text.Length > 12) || (angle < 12f))
                    {
                        fontArial = this.fontTahoma;
                    }
                    SizeF ef = graphics.MeasureString(text, fontArial, 150, format);
                    x = (width / 2) - ef.Width;
                    graphics.DrawString(text, fontArial, Brushes.White, x, 0f, format);
                }
            }
            graphics.Dispose();
            return image;
        }

        private void FiddlerApplication_CalculateReport(Session[] _arrSessions)
        {
            if (FiddlerApplication._frmMain.tabsViews.SelectedTab == FiddlerApplication._frmMain.pageStatistics)
            {
                try
                {
                    if (_arrSessions.Length < 1)
                    {
                        this.Clear();
                    }
                    else
                    {
                        Dictionary<string, long> dictionary;
                        long num;
                        string str = BasicAnalysis.ComputeBasicStatistics(_arrSessions, true, out dictionary, out num);
                        this.txtReport.Text = string.Format("{0}\r\n================\r\nLearn more about HTTP performance at " + CONFIG.GetUrl("REDIR") + "HTTPPERF", str);
                        if (this.pbPie.Height > 100)
                        {
                            this.pbPie.Image = this.DrawContentTypePie(dictionary, num);
                        }
                    }
                }
                catch (Exception)
                {
                }
            }
        }

        private void InitializeComponent()
        {
            this.txtReport = new TextBox();
            this.pnlPies = new Panel();
            this.lnkCopyChart = new LinkLabel();
            this.pbPie = new PictureBox();
            this.lblShowChart = new LinkLabel();
            this.pnlPies.SuspendLayout();
            ((ISupportInitialize) this.pbPie).BeginInit();
            base.SuspendLayout();
            this.txtReport.AcceptsReturn = true;
            this.txtReport.BackColor = Color.White;
            this.txtReport.BorderStyle = BorderStyle.None;
            this.txtReport.Dock = DockStyle.Fill;
            this.txtReport.Font = new Font("Lucida Console", 8.25f, FontStyle.Regular, GraphicsUnit.Point, 0);
            this.txtReport.Location = new Point(0, 0);
            this.txtReport.Multiline = true;
            this.txtReport.Name = "txtReport";
            this.txtReport.ReadOnly = true;
            this.txtReport.ScrollBars = ScrollBars.Both;
            this.txtReport.Size = new Size(0x25c, 0x1de);
            this.txtReport.TabIndex = 8;
            this.pnlPies.BackColor = SystemColors.Control;
            this.pnlPies.Controls.Add(this.lnkCopyChart);
            this.pnlPies.Controls.Add(this.pbPie);
            this.pnlPies.Controls.Add(this.lblShowChart);
            this.pnlPies.Dock = DockStyle.Bottom;
            this.pnlPies.Location = new Point(0, 0x1de);
            this.pnlPies.Name = "pnlPies";
            this.pnlPies.Size = new Size(0x25c, 0x12);
            this.pnlPies.TabIndex = 9;
            this.lnkCopyChart.Anchor = AnchorStyles.Left | AnchorStyles.Bottom;
            this.lnkCopyChart.AutoSize = true;
            this.lnkCopyChart.BackColor = Color.White;
            this.lnkCopyChart.Font = new Font("Tahoma", 8f);
            this.lnkCopyChart.LinkBehavior = LinkBehavior.HoverUnderline;
            this.lnkCopyChart.LinkColor = Color.Blue;
            this.lnkCopyChart.Location = new Point(2, 2);
            this.lnkCopyChart.Name = "lnkCopyChart";
            this.lnkCopyChart.Size = new Size(80, 13);
            this.lnkCopyChart.TabIndex = 2;
            this.lnkCopyChart.TabStop = true;
            this.lnkCopyChart.Text = "Copy this chart";
            this.lnkCopyChart.TextAlign = ContentAlignment.MiddleLeft;
            this.lnkCopyChart.Visible = false;
            this.lnkCopyChart.Click += new EventHandler(this.lnkCopyChart_Click);
            this.pbPie.BackColor = Color.White;
            this.pbPie.Dock = DockStyle.Fill;
            this.pbPie.Location = new Point(0, 20);
            this.pbPie.Name = "pbPie";
            this.pbPie.Size = new Size(0x25c, 0);
            this.pbPie.SizeMode = PictureBoxSizeMode.CenterImage;
            this.pbPie.TabIndex = 0;
            this.pbPie.TabStop = false;
            this.lblShowChart.Dock = DockStyle.Top;
            this.lblShowChart.Font = new Font("Tahoma", 8f);
            this.lblShowChart.LinkBehavior = LinkBehavior.HoverUnderline;
            this.lblShowChart.Location = new Point(0, 0);
            this.lblShowChart.Name = "lblShowChart";
            this.lblShowChart.Size = new Size(0x25c, 20);
            this.lblShowChart.TabIndex = 1;
            this.lblShowChart.TabStop = true;
            this.lblShowChart.Text = "Show Chart";
            this.lblShowChart.TextAlign = ContentAlignment.MiddleCenter;
            this.lblShowChart.LinkClicked += new LinkLabelLinkClickedEventHandler(this.lblShowChart_LinkClicked);
            base.Controls.Add(this.txtReport);
            base.Controls.Add(this.pnlPies);
            this.Font = new Font("Tahoma", 8.25f, FontStyle.Regular, GraphicsUnit.Point, 0);
            base.Name = "Report";
            base.Size = new Size(0x25c, 0x1f0);
            this.pnlPies.ResumeLayout(false);
            this.pnlPies.PerformLayout();
            ((ISupportInitialize) this.pbPie).EndInit();
            base.ResumeLayout(false);
            base.PerformLayout();
        }

        private void LazyInitGraphics()
        {
            this.brushes = new Brush[7];
            this.fontArial = new Font("Arial", 16f);
            this.fontTahoma = new Font("Tahoma", 10f);
            this.brushBackground = new SolidBrush(Color.White);
            this.brushHTML = new LinearGradientBrush(new Point(0, 0), new Point(400, 400), Color.LightSteelBlue, Color.Blue);
            this.brushHeaders = new LinearGradientBrush(new Point(0, 0), new Point(400, 400), Color.White, Color.Gray);
            this.brushCSS = new LinearGradientBrush(new Point(0, 0), new Point(400, 400), Color.Violet, Color.Purple);
            this.brushJScript = new LinearGradientBrush(new Point(0, 0), new Point(400, 400), Color.Green, Color.Aquamarine);
            this.brushJpeg = new LinearGradientBrush(new Point(0, 0), new Point(400, 400), Color.White, Color.LimeGreen);
            this.brushGif = new LinearGradientBrush(new Point(0, 0), new Point(400, 400), Color.White, Color.DarkGreen);
            this.brushPng = new LinearGradientBrush(new Point(0, 0), new Point(400, 400), Color.White, Color.SeaGreen);
            this.brushes[0] = new SolidBrush(Color.Coral);
            this.brushes[1] = new SolidBrush(Color.Crimson);
            this.brushes[2] = new SolidBrush(Color.MediumAquamarine);
            this.brushes[3] = new SolidBrush(Color.OliveDrab);
            this.brushes[4] = new SolidBrush(Color.Goldenrod);
            this.brushes[5] = new SolidBrush(Color.DarkSeaGreen);
            this.brushes[6] = new SolidBrush(Color.SpringGreen);
            this.bLazyGraphicsReady = true;
        }

        private void lblShowChart_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            if (this.pnlPies.Height < 100)
            {
                this.pnlPies.Height = 300;
                this.lblShowChart.BackColor = Color.White;
                this.lblShowChart.Text = "Collapse Chart";
                this.lnkCopyChart.Visible = true;
                FiddlerApplication._frmMain.actReportStatistics(true);
            }
            else
            {
                this.pnlPies.Height = 20;
                this.lblShowChart.BackColor = Color.FromKnownColor(KnownColor.Control);
                this.lblShowChart.Text = "Show Chart";
                this.lnkCopyChart.Visible = false;
            }
        }

        private void lnkCopyChart_Click(object sender, EventArgs e)
        {
            if (this.pbPie.Image != null)
            {
                DataObject oData = new DataObject(this.pbPie.Image);
                Utilities.CopyToClipboard(oData);
            }
        }

        public float FontSize
        {
            set
            {
                this.txtReport.Font = new Font(this.txtReport.Font.FontFamily, value);
            }
        }
    }
}

