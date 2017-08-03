namespace Fiddler
{
    using System;
    using System.IO;
    using System.Text;
    using System.Windows.Forms;

    public class ResponderRule
    {
        internal byte[] _arrResponseBodyBytes;
        private bool _bEnabled;
        private int _MSLatency;
        internal UIARRuleEditor _oEditor;
        internal HTTPResponseHeaders _oResponseHeaders;
        private string _sAction;
        private string _sMatch;
        internal ListViewItem ViewItem;

        internal ResponderRule(string strMatch, string strAction, bool bEnabled) : this(strMatch, null, null, strAction, 0, bEnabled)
        {
        }

        internal ResponderRule(string strMatch, HTTPResponseHeaders oResponseHeaders, byte[] arrResponseBytes, string strDescription, int iLatencyMS, bool bEnabled)
        {
            this._bEnabled = true;
            this.sMatch = strMatch;
            this.sAction = strDescription;
            this.iLatency = iLatencyMS;
            this._oResponseHeaders = oResponseHeaders;
            this._arrResponseBodyBytes = arrResponseBytes;
            if ((this._oResponseHeaders != null) && (this._arrResponseBodyBytes == null))
            {
                this._arrResponseBodyBytes = new byte[0];
            }
            this._bEnabled = bEnabled;
        }

        private string _MakeSafeFilename(string sFilename)
        {
            char[] invalidFileNameChars = Path.GetInvalidFileNameChars();
            if (sFilename.IndexOfAny(invalidFileNameChars) < 0)
            {
                return Utilities.TrimAfter(sFilename, 160);
            }
            StringBuilder builder = new StringBuilder(sFilename);
            for (int i = 0; i < builder.Length; i++)
            {
                if (Array.IndexOf<char>(invalidFileNameChars, sFilename[i]) > -1)
                {
                    builder[i] = '-';
                }
            }
            return Utilities.TrimAfter(builder.ToString(), 160);
        }

        internal bool ConvertToFileBackedRule()
        {
            if (this._oResponseHeaders == null)
            {
                return false;
            }
            string path = Environment.GetFolderPath(Environment.SpecialFolder.Desktop) + @"\" + this._MakeSafeFilename(this._sAction);
            string str3 = Utilities.FileExtensionForMIMEType(Utilities.TrimAfter(this._oResponseHeaders["Content-Type"], ";"));
            path = path + str3;
            FileStream stream = File.Create(path);
            bool flag = true;
            if (this._oResponseHeaders.HTTPResponseCode == 200)
            {
                string str2 = this._oResponseHeaders["Content-Type"];
                if (str2.StartsWith("image/"))
                {
                    flag = false;
                }
            }
            if (flag)
            {
                byte[] buffer = this._oResponseHeaders.ToByteArray(true, true);
                stream.Write(buffer, 0, buffer.Length);
            }
            if (this._arrResponseBodyBytes != null)
            {
                stream.Write(this._arrResponseBodyBytes, 0, this._arrResponseBodyBytes.Length);
            }
            stream.Close();
            this._oResponseHeaders = null;
            this._arrResponseBodyBytes = null;
            this._sAction = path;
            this.ViewItem.SubItems[1].Text = this._sAction;
            return true;
        }

        internal bool HasImportedResponse
        {
            get
            {
                return (null != this._oResponseHeaders);
            }
        }

        public int iLatency
        {
            get
            {
                return this._MSLatency;
            }
            set
            {
                if (value > 0)
                {
                    this._MSLatency = value;
                }
                else
                {
                    this._MSLatency = 0;
                }
            }
        }

        internal bool IsEnabled
        {
            get
            {
                return this._bEnabled;
            }
            set
            {
                this._bEnabled = value;
            }
        }

        public string sAction
        {
            get
            {
                return this._sAction;
            }
            internal set
            {
                if (value == null)
                {
                    this._sAction = string.Empty;
                }
                else
                {
                    this._sAction = value.Trim();
                }
            }
        }

        public string sMatch
        {
            get
            {
                return this._sMatch;
            }
            internal set
            {
                if ((value == null) || (value.Trim().Length < 1))
                {
                    this._sMatch = "*";
                }
                else
                {
                    this._sMatch = value.Trim();
                }
            }
        }
    }
}

