namespace Fiddler
{
    using System;
    using System.ComponentModel;
    using System.Drawing;
    using System.Resources;
    using System.Windows.Forms;

    public partial class frmAlert : Form
    {
       

        public frmAlert()
        {
            this.InitializeComponent();
            this.Font = new Font(this.Font.FontFamily, CONFIG.flFontSize);
        }

        public frmAlert(string sTitle, string sMessage, string sHint) : this()
        {
            this.Text = " " + sTitle;
            this.txtMessage.Text = sMessage;
            this.lblHint.Text = sHint;
        }

        public frmAlert(string sTitle, string sMessage, string sHint, MessageBoxButtons mbButtons, MessageBoxDefaultButton mbDefault) : this(sTitle, sMessage, sHint)
        {
            if (mbButtons == MessageBoxButtons.YesNo)
            {
                this.lblHint.Width = 260;
                this.lblHint.ForeColor = Color.FromKnownColor(KnownColor.WindowText);
                this.lblHint.TextAlign = ContentAlignment.MiddleRight;
                this.lblHint.Font = new Font(this.lblHint.Font, FontStyle.Bold);
                this.btnOk.Visible = false;
                this.btnNo.Visible = true;
                this.btnYes.Visible = true;
                if (mbDefault == MessageBoxDefaultButton.Button1)
                {
                    base.ActiveControl = this.btnYes;
                }
                else
                {
                    base.ActiveControl = this.btnNo;
                }
            }
        }

        private void btnOk_Click(object sender, EventArgs e)
        {
            base.Dispose();
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
                if (this.btnYes.Visible)
                {
                    this.btnYes.PerformClick();
                }
                else if (this.btnOk.Visible)
                {
                    this.btnOk.PerformClick();
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

       

        private void txtMessage_LinkClicked(object sender, LinkClickedEventArgs e)
        {
            Utilities.LaunchHyperlink(e.LinkText);
        }
    }
}

