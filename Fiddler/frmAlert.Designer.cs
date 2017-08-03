

using System.Resources;
using System.Windows.Forms;
using System.ComponentModel;
using System.Drawing;
using System;
namespace Fiddler
{
    partial class frmAlert
    {
        ///// <summary>
        ///// 必需的设计器变量。
        ///// </summary>
        //private System.ComponentModel.IContainer components = null;

        ///// <summary>
        ///// 清理所有正在使用的资源。
        ///// </summary>
        ///// <param name="disposing">如果应释放托管资源，为 true；否则为 false。</param>
        //protected override void Dispose(bool disposing)
        //{
        //    if (disposing && (components != null))
        //    {
        //        components.Dispose();
        //    }
        //    base.Dispose(disposing);
        //}

        protected override void Dispose(bool disposing)
        {
            if (disposing && (this.components != null))
            {
                this.components.Dispose();
            }
            base.Dispose(disposing);
        }

        private Button btnNo;
        private Button btnOk;
        private Button btnYes;
        private Container components=null;
        internal Label lblHint;
        private PictureBox pbImage;
        internal RichTextBox txtMessage;

        private void InitializeComponent()
        {
            ResourceManager manager = new ResourceManager(typeof(frmAlert));
            this.pbImage = new PictureBox();
            this.btnOk = new Button();
            this.txtMessage = new RichTextBox();
            this.lblHint = new Label();
            this.btnYes = new Button();
            this.btnNo = new Button();
            base.SuspendLayout();
            this.pbImage.Image = (Image)Properties.Resources.pbImage_Image;//manager.GetObject("pbImage.Image");
            this.pbImage.Location = new Point(8, 8);
            this.pbImage.Name = "pbImage";
            this.pbImage.Size = new Size(0x20, 0x20);
            this.pbImage.SizeMode = PictureBoxSizeMode.CenterImage;
            this.pbImage.TabIndex = 0;
            this.pbImage.TabStop = false;
            this.btnOk.Anchor = AnchorStyles.Right | AnchorStyles.Bottom;
            this.btnOk.DialogResult = DialogResult.OK;
            this.btnOk.Location = new Point(360, 0x88);
            this.btnOk.Name = "btnOk";
            this.btnOk.TabIndex = 2;
            this.btnOk.Text = "OK";
            this.btnOk.Click += new EventHandler(this.btnOk_Click);
            this.txtMessage.Anchor = AnchorStyles.Right | AnchorStyles.Left | AnchorStyles.Bottom | AnchorStyles.Top;
            this.txtMessage.BackColor = SystemColors.Control;
            this.txtMessage.BorderStyle = BorderStyle.None;
            this.txtMessage.Location = new Point(0x30, 8);
            this.txtMessage.Name = "txtMessage";
            this.txtMessage.ReadOnly = true;
            this.txtMessage.Size = new Size(0x180, 120);
            this.txtMessage.TabIndex = 3;
            this.txtMessage.Text = "Codito Ergo Sum Codito Ergo Sum Codito Ergo Sum Codito Ergo Sum Codito Ergo Sum Codito Ergo Sum Codito Ergo Sum Codito Ergo Sum Codito Ergo Sum Codito Ergo Sum Codito Ergo Sum Codito Ergo Sum Codito Ergo Sum Codito Ergo Sum Codito Ergo Sum Codito Ergo Sum Codito Ergo Sum Codito Ergo Sum Codito Ergo Sum Codito Ergo Sum Codito Ergo Sum Codito Ergo Sum Codito Ergo Sum Codito Ergo Sum Codito Ergo Sum Codito Ergo Sum Codito Ergo Sum Codito Ergo Sum Codito Ergo Sum Codito Ergo Sum Codito Ergo Sum Codito Ergo Sum Codito Ergo Sum Codito Ergo Sum Codito Ergo Sum Codito Ergo Sum Codito Ergo Sum Codito Ergo Sum Codito Ergo Sum Codito Ergo Sum Codito Ergo Sum Codito Ergo Sum Codito Ergo Sum Codito Ergo Sum Codito Ergo Sum Codito Ergo Sum Codito Ergo Sum Codito Ergo Sum Codito Ergo Sum Codito Ergo Sum Codito Ergo Sum Codito Ergo Sum Codito Ergo Sum Codito Ergo Sum Codito Ergo Sum Codito Ergo Sum Codito Ergo Sum Codito Ergo Sum Codito Ergo Sum Codito Ergo Sum Codito Ergo Sum Codito Ergo Sum Codito E";
            this.txtMessage.LinkClicked += new LinkClickedEventHandler(this.txtMessage_LinkClicked);
            this.lblHint.Anchor = AnchorStyles.Right | AnchorStyles.Left | AnchorStyles.Bottom;
            this.lblHint.ForeColor = SystemColors.ControlDarkDark;
            this.lblHint.Location = new Point(8, 140);
            this.lblHint.Name = "lblHint";
            this.lblHint.Size = new Size(0x160, 0x10);
            this.lblHint.TabIndex = 4;
            this.lblHint.Text = "WWMgqWWMgqWMgqWMgqWWMgqWMgqWWMgqWMgqWWMgq";
            this.lblHint.TextAlign = ContentAlignment.MiddleLeft;
            this.btnYes.Anchor = AnchorStyles.Right | AnchorStyles.Bottom;
            this.btnYes.DialogResult = DialogResult.Yes;
            this.btnYes.Location = new Point(280, 0x88);
            this.btnYes.Name = "btnYes";
            this.btnYes.TabIndex = 5;
            this.btnYes.Text = "&Yes";
            this.btnYes.Visible = false;
            this.btnYes.Click += new EventHandler(this.btnOk_Click);
            this.btnNo.Anchor = AnchorStyles.Right | AnchorStyles.Bottom;
            this.btnNo.DialogResult = DialogResult.No;
            this.btnNo.Location = new Point(360, 0x88);
            this.btnNo.Name = "btnNo";
            this.btnNo.TabIndex = 6;
            this.btnNo.Text = "&No";
            this.btnNo.Visible = false;
            this.btnNo.Click += new EventHandler(this.btnOk_Click);
            this.AutoScaleBaseSize = new Size(5, 14);
            base.ClientSize = new Size(440, 0xa6);
            base.Controls.Add(this.btnNo);
            base.Controls.Add(this.btnYes);
            base.Controls.Add(this.lblHint);
            base.Controls.Add(this.txtMessage);
            base.Controls.Add(this.btnOk);
            base.Controls.Add(this.pbImage);
            this.Font = new Font("Tahoma", 8.25f, FontStyle.Regular, GraphicsUnit.Point, 0);
            base.FormBorderStyle = FormBorderStyle.SizableToolWindow;
            base.Icon = (Icon)Properties.Resources.notifyIcon_Icon;//manager.GetObject("$this.Icon");
            base.KeyPreview = true;
            base.Name = "frmAlert";
            base.SizeGripStyle = SizeGripStyle.Hide;
            base.StartPosition = FormStartPosition.Manual;
            this.Text = " Fiddler Alert";
            base.KeyDown += new KeyEventHandler(this.frmAlert_KeyDown);
            base.ResumeLayout(false);
        }
    }
}
