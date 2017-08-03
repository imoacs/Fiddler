namespace Fiddler
{
    using System;

    [AttributeUsage(AttributeTargets.Method, Inherited=false, AllowMultiple=false)]
    public sealed class QuickLinkMenu : Attribute
    {
        private string myMenuName;

        public QuickLinkMenu(string sName)
        {
            this.myMenuName = sName;
        }

        public string Name
        {
            get
            {
                return this.myMenuName;
            }
        }
    }
}

