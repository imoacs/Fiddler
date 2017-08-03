namespace Fiddler
{
    using System;
    using System.Collections.Generic;
    using System.Text.RegularExpressions;

    internal class ProxyBypassList
    {
        private List<string> _BypassList;
        private bool _BypassOnLocal;
        private Regex[] _RegExBypassList;

        public ProxyBypassList(string sBypassList)
        {
            if (!string.IsNullOrEmpty(sBypassList))
            {
                this.PrepareBypassList(sBypassList);
                this.PrepareBypassRegEx();
            }
        }

        public bool IsBypass(string sSchemeHostPort)
        {
            if (this._RegExBypassList != null)
            {
                for (int i = 0; i < this._RegExBypassList.Length; i++)
                {
                    if (this._RegExBypassList[i].IsMatch(sSchemeHostPort))
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        private void PrepareBypassList(string bypassListString)
        {
            char[] separator = new char[] { ';' };
            string[] strArray = bypassListString.Split(separator);
            this._BypassOnLocal = false;
            this._BypassList = null;
            if (strArray.Length != 0)
            {
                foreach (string str in strArray)
                {
                    if (str != null)
                    {
                        string a = str.Trim();
                        if (a.Length > 0)
                        {
                            if (string.Equals(a, "<local>", StringComparison.OrdinalIgnoreCase))
                            {
                                this._BypassOnLocal = true;
                            }
                            else
                            {
                                if (!a.Contains("://"))
                                {
                                    a = "*://" + a;
                                }
                                a = Utilities.RegExEscape(a, true, true);
                                if (this._BypassList == null)
                                {
                                    this._BypassList = new List<string>();
                                }
                                if (!this._BypassList.Contains(a))
                                {
                                    this._BypassList.Add(a);
                                }
                            }
                        }
                    }
                }
            }
        }

        private void PrepareBypassRegEx()
        {
            Regex[] regexArray = null;
            List<string> list = this._BypassList;
            try
            {
                if ((list != null) && (list.Count > 0))
                {
                    regexArray = new Regex[list.Count];
                    for (int i = 0; i < list.Count; i++)
                    {
                        regexArray[i] = new Regex(list[i], RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);
                    }
                }
            }
            catch
            {
                this._RegExBypassList = null;
                return;
            }
            this._RegExBypassList = regexArray;
        }

        public bool HasEntries
        {
            get
            {
                return ((this._RegExBypassList != null) && (this._RegExBypassList.Length > 0));
            }
        }
    }
}

