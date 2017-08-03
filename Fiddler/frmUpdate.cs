namespace Fiddler
{
    using System;
    using System.ComponentModel;
    using System.Drawing;
    using System.Windows.Forms;

    public class frmUpdate : Form
    {
        private Button btnNextTime;
        private Button btnNo;
        private Button btnNow;
        private Container components;
        internal Label lblHint;
        private PictureBox pbImage;
        internal RichTextBox txtMessage;

        public frmUpdate()
        {
            this.InitializeComponent();
            this.Font = new Font(this.Font.FontFamily, CONFIG.flFontSize);
        }

        public frmUpdate(string sTitle, string sMessage, bool bOfferNextTime, MessageBoxDefaultButton mbDefault) : this()
        {
            this.Text = sTitle;
            this.txtMessage.Text = sMessage;
            this.btnNextTime.Visible = bOfferNextTime;
            if ((mbDefault == MessageBoxDefaultButton.Button1) || !bOfferNextTime)
            {
                base.ActiveControl = this.btnNow;
            }
            else
            {
                base.ActiveControl = this.btnNextTime;
            }
        }

        private void btnOk_Click(object sender, EventArgs e)
        {
            base.Dispose();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing && (this.components != null))
            {
                this.components.Dispose();
            }
            base.Dispose(disposing);
        }

        private void frmAlert_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Escape)
            {
                base.DialogResult = DialogResult.Cancel;
                base.Dispose();
            }
            else if (e.KeyCode == Keys.Return)
            {
                if (this.btnNow.Visible)
                {
                    this.btnNow.PerformClick();
                }
                else if (this.btnNo.Visible)
                {
                    this.btnNo.PerformClick();
                }
                else
                {
                    base.Dispose();
                }
            }
            else if (e.Control && (e.KeyCode == Keys.C))
            {
                this.txtMessage.SelectAll();
                this.txtMessage.Copy();
            }
        }

        private void InitializeComponent()
        {
            ComponentResourceManager manager = new ComponentResourceManager(typeof(frmUpdate));
            this.pbImage = new PictureBox();
            this.btnNo = new Button();
            this.txtMessage = new RichTextBox();
            this.lblHint = new Label();
            this.btnNow = new Button();
            this.btnNextTime = new Button();
            ((ISupportInitialize) this.pbImage).BeginInit();
            base.SuspendLayout();
            this.pbImage.Image = (Image)Properties.Resources.pbImage_Image; //manager.GetObject("pbImage.Image");
            this.pbImage.Location = new Point(8, 8);
            this.pbImage.Name = "pbImage";
            this.pbImage.Size = new Size(0x20, 0x20);
            this.pbImage.SizeMode = PictureBoxSizeMode.CenterImage;
            this.pbImage.TabIndex = 0;
            this.pbImage.TabStop = false;
            this.btnNo.Anchor = AnchorStyles.Right | AnchorStyles.Bottom;
            this.btnNo.DialogResult = DialogResult.Cancel;
            this.btnNo.Location = new Point(0x152, 0xe0);
            this.btnNo.Name = "btnNo";
            this.btnNo.Size = new Size(0x5c, 0x17);
            this.btnNo.TabIndex = 2;
            this.btnNo.Text = "&No";
            this.btnNo.Click += new EventHandler(this.btnOk_Click);
            this.txtMessage.Anchor = AnchorStyles.Right | AnchorStyles.Left | AnchorStyles.Bottom | AnchorStyles.Top;
            this.txtMessage.BackColor = SystemColors.Control;
            this.txtMessage.BorderStyle = BorderStyle.None;
            this.txtMessage.Location = new Point(0x30, 8);
            this.txtMessage.Name = "txtMessage";
            this.txtMessage.ReadOnly = true;
            this.txtMessage.Size = new Size(0x180, 0xb8);
            this.txtMessage.TabIndex = 3;
            this.txtMessage.Text = manager.GetString("txtMessage.Text");
            this.txtMessage.LinkClicked += new LinkClickedEventHandler(this.txtMessage_LinkClicked);
            this.lblHint.Anchor = AnchorStyles.Right | AnchorStyles.Left | AnchorStyles.Bottom;
            this.lblHint.BackColor = SystemColors.Highlight;
            this.lblHint.Font = new Font("Tahoma", 8.25f, FontStyle.Bold, GraphicsUnit.Point, 0);
            this.lblHint.ForeColor = SystemColors.HighlightText;
            this.lblHint.Location = new Point(3, 0xc3);
            this.lblHint.Name = "lblHint";
            this.lblHint.Size = new Size(0x1ab, 0x1a);
            this.lblHint.TabIndex = 4;
            this.lblHint.Text = "Would you like to install this update?";
            this.lblHint.TextAlign = ContentAlignment.MiddleCenter;
            this.btnNow.Anchor = AnchorStyles.Right | AnchorStyles.Bottom;
            this.btnNow.DialogResult = DialogResult.Yes;
            this.btnNow.Location = new Point(0x30, 0xe0);
            this.btnNow.Name = "btnNow";
            this.btnNow.Size = new Size(0x88, 0x17);
            this.btnNow.TabIndex = 5;
            this.btnNow.Text = "&Yes";
            this.btnNow.Click += new EventHandler(this.btnOk_Click);
            this.btnNextTime.Anchor = AnchorStyles.Right | AnchorStyles.Bottom;
            this.btnNextTime.DialogResult = DialogResult.Retry;
            this.btnNextTime.Location = new Point(190, 0xe0);
            this.btnNextTime.Name = "btnNextTime";
            this.btnNextTime.Size = new Size(140, 0x17);
            this.btnNextTime.TabIndex = 6;
            this.btnNextTime.Text = "Next &Time";
            this.btnNextTime.Click += new EventHandler(this.btnOk_Click);
            this.AutoScaleBaseSize = new Size(5, 14);
            base.CancelButton = this.btnNo;
            base.ClientSize = new Size(440, 250);
            base.Controls.Add(this.btnNextTime);
            base.Controls.Add(this.btnNow);
            base.Controls.Add(this.lblHint);
            base.Controls.Add(this.txtMessage);
            base.Controls.Add(this.btnNo);
            base.Controls.Add(this.pbImage);
            this.Font = new Font("Tahoma", 8.25f, FontStyle.Regular, GraphicsUnit.Point, 0);
            base.FormBorderStyle = FormBorderStyle.SizableToolWindow;
            base.Icon = (Icon)Properties.Resources.sbpCapture_Icon;//manager.GetObject("$this.Icon");
            base.KeyPreview = true;
            this.MinimumSize = new Size(0x1a9, 150);
            base.Name = "frmUpdate";
            base.SizeGripStyle = SizeGripStyle.Hide;
            base.StartPosition = FormStartPosition.Manual;
            this.Text = " Fiddler Update";
            base.KeyDown += new KeyEventHandler(this.frmAlert_KeyDown);
            ((ISupportInitialize) this.pbImage).EndInit();
            base.ResumeLayout(false);
        }

        private void txtMessage_LinkClicked(object sender, LinkClickedEventArgs e)
        {
            Utilities.LaunchHyperlink(e.LinkText);
        }
    }
}

