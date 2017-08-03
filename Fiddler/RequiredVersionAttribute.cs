namespace Fiddler
{
    using System;

    [AttributeUsage(AttributeTargets.Assembly, Inherited=false, AllowMultiple=false)]
    public sealed class RequiredVersionAttribute : Attribute
    {
        private string _sVersion;

        public RequiredVersionAttribute(string sVersion)
        {
            this._sVersion = sVersion;
        }

        public string RequiredVersion
        {
            get
            {
                return this._sVersion;
            }
        }
    }
}

