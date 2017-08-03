namespace Fiddler
{
    using System;
    using System.Diagnostics;
    using System.Windows.Forms;

    public abstract class Inspector2
    {
        protected Inspector2()
        {
        }

        public abstract void AddToTab(TabPage o);
        public abstract int GetOrder();
        public virtual int ScoreForContentType(string sMIMEType)
        {
            return 0;
        }

        public virtual void SetFontSize(float flSizeInPoints)
        {
        }

        public virtual void ShowAboutBox()
        {
            FiddlerApplication.DoNotifyUser(this.ToString() + "\n\n" + FileVersionInfo.GetVersionInfo(base.GetType().Assembly.Location).ToString(), "About Inspector", MessageBoxIcon.Asterisk);
        }
    }
}

