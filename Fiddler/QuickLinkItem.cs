namespace Fiddler
{
    using System;

    [AttributeUsage(AttributeTargets.Method, Inherited=false, AllowMultiple=true)]
    public sealed class QuickLinkItem : Attribute, IComparable<QuickLinkItem>
    {
        private string _myAction;
        private string _myMenuText;

        public QuickLinkItem(string sMenuText, string sValue)
        {
            this._myMenuText = sMenuText;
            this._myAction = sValue;
        }

        public int CompareTo(QuickLinkItem other)
        {
            return string.Compare(this._myMenuText, other._myMenuText);
        }

        public string Action
        {
            get
            {
                return this._myAction;
            }
        }

        public string MenuText
        {
            get
            {
                return this._myMenuText;
            }
        }
    }
}

