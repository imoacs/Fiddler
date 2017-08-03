namespace Fiddler
{
    using System;

    [AttributeUsage(AttributeTargets.Field, Inherited=false, AllowMultiple=true)]
    public sealed class RulesStringValue : Attribute, IComparable<RulesStringValue>
    {
        private int _iPrecedence;
        private bool _myIsDefault;
        private string _myMenuText;
        private string _myValue;

        public RulesStringValue(string sMenuText, string sValue) : this(0, sMenuText, sValue, false)
        {
        }

        public RulesStringValue(int iPrecedence, string sMenuText, string sValue) : this(iPrecedence, sMenuText, sValue, false)
        {
        }

        public RulesStringValue(int iPrecedence, string sMenuText, string sValue, bool bIsDefault)
        {
            this._iPrecedence = iPrecedence;
            this._myMenuText = sMenuText;
            this._myValue = sValue;
            this._myIsDefault = bIsDefault;
        }

        public int CompareTo(RulesStringValue other)
        {
            if (this._iPrecedence < other._iPrecedence)
            {
                return -1;
            }
            if (this._iPrecedence > other._iPrecedence)
            {
                return 1;
            }
            return string.Compare(this._myMenuText, other._myMenuText);
        }

        public bool IsDefault
        {
            get
            {
                return this._myIsDefault;
            }
        }

        public string MenuText
        {
            get
            {
                return this._myMenuText;
            }
        }

        public string Value
        {
            get
            {
                return this._myValue;
            }
        }
    }
}

