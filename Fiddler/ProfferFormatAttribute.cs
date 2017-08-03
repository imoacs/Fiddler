namespace Fiddler
{
    using System;

    [AttributeUsage(AttributeTargets.Class, Inherited=false, AllowMultiple=true)]
    public sealed class ProfferFormatAttribute : Attribute
    {
        private string _sFormatDesc;
        private string _sFormatName;

        public ProfferFormatAttribute(string sFormatName, string sDescription)
        {
            this._sFormatName = sFormatName;
            this._sFormatDesc = sDescription;
        }

        public string FormatDescription
        {
            get
            {
                return this._sFormatDesc;
            }
        }

        public string FormatName
        {
            get
            {
                return this._sFormatName;
            }
        }
    }
}

