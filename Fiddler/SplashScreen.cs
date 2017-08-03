namespace Fiddler
{
    using System;
    using System.ComponentModel;
    using System.Drawing;
    using System.Windows.Forms;

    public partial class SplashScreen : Form
    {
     
        public SplashScreen()
        {
            try
            {
                this.InitializeComponent();
            }
            catch (ArgumentException exception)
            {
                FiddlerApplication.DoNotifyUser("It appears that one of your computer's fonts is missing, or your\nMicrosoft .NET Framework installation is corrupt. If you see the Font file in\nthe c:\\windows\\fonts folder, then try reinstalling the .NET Framework and\nall updates from WindowsUpdate.\n\n" + exception.Message + "\n" + exception.StackTrace, "Fiddler is unable to start");
            }
            this.lblVersion.Text = string.Format("v{0}{1}", Application.ProductVersion, CONFIG.bIsBeta ? " beta" : string.Empty);
        }

      

        internal void IndicateProgress(string sWhatIsHappening)
        {
            this.lblProgress.Text = sWhatIsHappening;
            Application.DoEvents();
        }

     
    }
}

