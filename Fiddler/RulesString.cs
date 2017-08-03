namespace Fiddler
{
    using System;

    [AttributeUsage(AttributeTargets.Field, Inherited=false, AllowMultiple=false)]
    public sealed class RulesString : Attribute
    {
        private bool _bShowDisabled;
        private string myRuleName;

        public RulesString(string sName, bool ShowDisabledOption)
        {
            this.myRuleName = sName;
            this._bShowDisabled = ShowDisabledOption;
        }

        public string Name
        {
            get
            {
                return this.myRuleName;
            }
        }

        public bool ShowDisabledOption
        {
            get
            {
                return this._bShowDisabled;
            }
        }
    }
}

