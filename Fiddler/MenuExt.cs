namespace Fiddler
{
    using Microsoft.Win32;
    using System;
    using System.Collections;
    using System.Windows.Forms;

    internal class MenuExt
    {
        private static void HandleItemClick(object sender, EventArgs e)
        {
            RegistryKey key = Registry.CurrentUser.OpenSubKey(CONFIG.GetRegPath("MenuExt") + (sender as MenuItem).Text);
            if (key != null)
            {
                string sExecute = (string) key.GetValue("Command");
                string sParams = (string) key.GetValue("Parameters");
                string sMethodName = (string) key.GetValue("ScriptMethod");
                key.Close();
                if ((sMethodName != null) && ((FiddlerApplication.scriptRules == null) || !FiddlerApplication.scriptRules.DoMethod(sMethodName)))
                {
                    FiddlerApplication.DoNotifyUser(string.Format("Failed to execute script method: {0}\n(Case sensitive. Ensure your script includes a function of that name.)", sMethodName), "Scripting Error");
                }
                if (sExecute != null)
                {
                    Utilities.RunExecutable(sExecute, sParams);
                }
            }
        }

        internal static void ReadRegistry(MenuItem miAddTo, Hashtable htMapping)
        {
            RegistryKey key = Registry.CurrentUser.OpenSubKey(CONFIG.GetRegPath("MenuExt"));
            if (key != null)
            {
                bool flag = true;
                foreach (string str in key.GetSubKeyNames())
                {
                    MenuItem item;
                    if (flag)
                    {
                        item = new MenuItem("-");
                        htMapping.Add(item, null);
                        miAddTo.MenuItems.Add(item);
                        flag = false;
                    }
                    item = new MenuItem(str);
                    htMapping.Add(item, null);
                    item.Click += new EventHandler(MenuExt.HandleItemClick);
                    miAddTo.MenuItems.Add(item);
                }
                key.Close();
            }
        }
    }
}

