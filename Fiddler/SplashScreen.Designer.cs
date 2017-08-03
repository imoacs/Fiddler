using System;
using System.Collections.Generic;
using System.Text;
using System.ComponentModel;
using System.Windows.Forms;
using System.Drawing;

namespace Fiddler
{
    partial class SplashScreen
    {
        private Container components=null;
        private Label lblProgress;
        private Label lblVersion;
        private PictureBox picSplash;

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
            //ComponentResourceManager manager =// new ComponentResourceManager(typeof(SplashScreen));
            this.lblProgress = new Label();
            this.picSplash = new PictureBox();
            this.lblVersion = new Label();
            ((ISupportInitialize)this.picSplash).BeginInit();
            base.SuspendLayout();
            this.lblProgress.BackColor = Color.Transparent;
            this.lblProgress.ForeColor = Color.FromArgb(0x40, 0x40, 0x40);
            this.lblProgress.ImeMode = ImeMode.NoControl;
            this.lblProgress.Location = new Point(130, 0x94);
            this.lblProgress.Name = "lblProgress";
            this.lblProgress.Size = new Size(0xac, 0x20);
            this.lblProgress.TabIndex = 0;
            this.lblProgress.Text = "Loading...";
            this.picSplash.BackColor = Color.Black;
            this.picSplash.BorderStyle = BorderStyle.FixedSingle;
            this.picSplash.Dock = DockStyle.Fill;
            this.picSplash.Image = (Image)Fiddler.Properties.Resources.picSplash_Image;//manager.GetObject("picSplash.Image");
            this.picSplash.ImeMode = ImeMode.NoControl;
            this.picSplash.Location = new Point(0, 0);
            this.picSplash.Name = "picSplash";
            this.picSplash.Size = new Size(400, 0xfc);
            this.picSplash.TabIndex = 1;
            this.picSplash.TabStop = false;
            this.lblVersion.ForeColor = Color.FromArgb(0x40, 0x40, 0x40);
            this.lblVersion.ImeMode = ImeMode.NoControl;
            this.lblVersion.Location = new Point(0xe0, 220);
            this.lblVersion.Name = "lblVersion";
            this.lblVersion.Size = new Size(0xac, 0x1c);
            this.lblVersion.TabIndex = 2;
            this.lblVersion.Text = "v0.0.0.0";
            this.lblVersion.TextAlign = ContentAlignment.BottomRight;
            base.AutoScaleMode = AutoScaleMode.None;
            this.BackColor = Color.White;
            base.ClientSize = new Size(400, 0xfc);
            base.ControlBox = false;
            base.Controls.Add(this.lblVersion);
            base.Controls.Add(this.lblProgress);
            base.Controls.Add(this.picSplash);
            this.Font = new Font("Tahoma", 8.25f);
            base.FormBorderStyle = FormBorderStyle.None;
            base.Icon = (Icon)Fiddler.Properties.Resources.Splash_Icon;
            base.Name = "SplashScreen";
            base.StartPosition = FormStartPosition.CenterScreen;
            this.Text = "Starting Fiddler...";
            ((ISupportInitialize)this.picSplash).EndInit();
            base.ResumeLayout(false);
        }
    }
}
