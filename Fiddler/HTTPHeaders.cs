namespace Fiddler
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Reflection;
    using System.Text;
    using System.Text.RegularExpressions;

    public class HTTPHeaders
    {
        protected Encoding _HeaderEncoding = CONFIG.oHeaderEncoding;
        [CodeDescription("HTTP version (e.g. HTTP/1.1).")]
        public string HTTPVersion = "HTTP/1.1";
        protected List<HTTPHeaderItem> storage = new List<HTTPHeaderItem>();

        [CodeDescription("Add a new header containing the specified name and value.")]
        public HTTPHeaderItem Add(string sHeaderName, string sValue)
        {
            HTTPHeaderItem item = new HTTPHeaderItem(sHeaderName, sValue);
            this.storage.Add(item);
            return item;
        }

        public int ByteCount()
        {
            return this.ToString().Length;
        }

        [CodeDescription("Returns an integer representing the number of headers.")]
        public int Count()
        {
            return this.storage.Count;
        }

        [CodeDescription("Returns true if the Headers collection contains a header of the specified (case-insensitive) name.")]
        public bool Exists(string sHeaderName)
        {
            for (int i = 0; i < this.storage.Count; i++)
            {
                if (string.Equals(this.storage[i].Name, sHeaderName, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }
            return false;
        }

        [CodeDescription("Returns true if the collection contains a header of the specified (case-insensitive) name, and sHeaderValue (case-insensitive) is part of the Header's value.")]
        public bool ExistsAndContains(string sHeaderName, string sHeaderValue)
        {
            for (int i = 0; i < this.storage.Count; i++)
            {
                if (string.Equals(this.storage[i].Name, sHeaderName, StringComparison.OrdinalIgnoreCase) && (this.storage[i].Value.IndexOf(sHeaderValue, StringComparison.OrdinalIgnoreCase) > -1))
                {
                    return true;
                }
            }
            return false;
        }

        [CodeDescription("Returns true if the collection contains a header of the specified (case-insensitive) name, with value sHeaderValue (case-insensitive).")]
        public bool ExistsAndEquals(string sHeaderName, string sHeaderValue)
        {
            for (int i = 0; i < this.storage.Count; i++)
            {
                if (string.Equals(this.storage[i].Name, sHeaderName, StringComparison.OrdinalIgnoreCase) && string.Equals(this.storage[i].Value, sHeaderValue, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }
            return false;
        }

        public IEnumerator GetEnumerator()
        {
            return this.storage.GetEnumerator();
        }

        [CodeDescription("Returns a string representing the value of the named token within the named header.")]
        public string GetTokenValue(string sHeaderName, string sTokenName)
        {
            string str = null;
            string input = this[sHeaderName];
            if ((input != null) && (input.Length > 0))
            {
                Match match = new Regex(sTokenName + "\\s?=\\s?[\"]?(?<TokenValue>[^\";]*)").Match(input);
                if (match.Success && (match.Groups["TokenValue"] != null))
                {
                    str = match.Groups["TokenValue"].Value;
                }
            }
            return str;
        }

        public void Remove(HTTPHeaderItem oRemove)
        {
            this.storage.Remove(oRemove);
        }

        [CodeDescription("Removes ALL headers from the header collection which have the specified (case-insensitive) name.")]
        public void Remove(string sHeaderName)
        {
            for (int i = this.storage.Count - 1; i >= 0; i--)
            {
                if (string.Equals(this.storage[i].Name, sHeaderName, StringComparison.OrdinalIgnoreCase))
                {
                    this.storage.RemoveAt(i);
                }
            }
        }

        [CodeDescription("Renames ALL headers in the header collection which have the specified (case-insensitive) name.")]
        public bool RenameHeaderItems(string sOldHeaderName, string sNewHeaderName)
        {
            bool flag = false;
            for (int i = 0; i < this.storage.Count; i++)
            {
                if (string.Equals(this.storage[i].Name, sOldHeaderName, StringComparison.OrdinalIgnoreCase))
                {
                    this.storage[i].Name = sNewHeaderName;
                    flag = true;
                }
            }
            return flag;
        }

        [CodeDescription("Indexer property. Returns HTTPHeaderItem by index.")]
        public HTTPHeaderItem this[int iHeaderNumber]
        {
            get
            {
                return this.storage[iHeaderNumber];
            }
            set
            {
                this.storage[iHeaderNumber] = value;
            }
        }

        [CodeDescription("Indexer property. Gets or sets the value of a header. In the case of Gets, the value of the FIRST header of that name is returned.\nIf the header does not exist, returns null.\nIn the case of Sets, the value of the FIRST header of that name is updated.\nIf the header does not exist, it is added.")]
        public string this[string HeaderName]
        {
            get
            {
                for (int i = 0; i < this.storage.Count; i++)
                {
                    if (string.Equals(this.storage[i].Name, HeaderName, StringComparison.OrdinalIgnoreCase))
                    {
                        return this.storage[i].Value;
                    }
                }
                return string.Empty;
            }
            set
            {
                for (int i = 0; i < this.storage.Count; i++)
                {
                    if (string.Equals(this.storage[i].Name, HeaderName, StringComparison.OrdinalIgnoreCase))
                    {
                        this.storage[i].Value = value;
                        return;
                    }
                }
                this.Add(HeaderName, value);
            }
        }
    }
}

