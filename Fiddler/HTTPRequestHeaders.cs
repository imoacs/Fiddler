namespace Fiddler
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Text;

    public class HTTPRequestHeaders : HTTPHeaders, ICloneable
    {
        private string _Path;
        private byte[] _RawPath;
        private string _UriScheme;
        internal string _uriUserInfo;
        [CodeDescription("HTTP Method or Verb from HTTP Request.")]
        public string HTTPMethod;

        public HTTPRequestHeaders()
        {
            this._UriScheme = "http";
            this.HTTPMethod = string.Empty;
            this._RawPath = new byte[0];
            this._Path = string.Empty;
        }

        public HTTPRequestHeaders(Encoding encodingForHeaders)
        {
            this._UriScheme = "http";
            this.HTTPMethod = string.Empty;
            this._RawPath = new byte[0];
            this._Path = string.Empty;
            base._HeaderEncoding = encodingForHeaders;
        }

        [CodeDescription("Replaces the current Request header set using a string representing the new HTTP headers.")]
        public bool AssignFromString(string sHeaders)
        {
            HTTPRequestHeaders headers = null;
            try
            {
                headers = Parser.ParseRequest(sHeaders);
            }
            catch (Exception)
            {
            }
            if (headers != null)
            {
                this.HTTPMethod = headers.HTTPMethod;
                this._Path = headers._Path;
                this._RawPath = headers._RawPath;
                this._UriScheme = headers._UriScheme;
                base.HTTPVersion = headers.HTTPVersion;
                this._uriUserInfo = headers._uriUserInfo;
                base.storage = headers.storage;
                return true;
            }
            return false;
        }

        public object Clone()
        {
            HTTPRequestHeaders headers = (HTTPRequestHeaders) base.MemberwiseClone();
            headers.storage = new List<HTTPHeaderItem>(base.storage.Count);
            foreach (HTTPHeaderItem item in base.storage)
            {
                headers.storage.Add((HTTPHeaderItem) item.Clone());
            }
            return headers;
        }

        [CodeDescription("Returns current Request Headers as a byte array.")]
        public byte[] ToByteArray(bool prependVerbLine, bool appendEmptyLine, bool includeProtocolInPath)
        {
            if (!prependVerbLine)
            {
                return base._HeaderEncoding.GetBytes(this.ToString(false, appendEmptyLine, false));
            }
            byte[] bytes = Encoding.ASCII.GetBytes(this.HTTPMethod);
            byte[] buffer = Encoding.ASCII.GetBytes(base.HTTPVersion);
            byte[] buffer3 = base._HeaderEncoding.GetBytes(this.ToString(false, appendEmptyLine, false));
            MemoryStream stream = new MemoryStream(0x200);
            stream.Write(bytes, 0, bytes.Length);
            stream.WriteByte(0x20);
            if (includeProtocolInPath && !this.HTTPMethod.Equals("CONNECT", StringComparison.OrdinalIgnoreCase))
            {
                byte[] buffer4 = base._HeaderEncoding.GetBytes(this._UriScheme + "://" + this._uriUserInfo + base["Host"]);
                stream.Write(buffer4, 0, buffer4.Length);
            }
            stream.Write(this._RawPath, 0, this._RawPath.Length);
            stream.WriteByte(0x20);
            stream.Write(buffer, 0, buffer.Length);
            stream.WriteByte(13);
            stream.WriteByte(10);
            stream.Write(buffer3, 0, buffer3.Length);
            return stream.ToArray();
        }

        [CodeDescription("Returns a string representing the HTTP Request.")]
        public override string ToString()
        {
            return this.ToString(true, false, false);
        }

        [CodeDescription("Returns a string representing the HTTP Request.")]
        public string ToString(bool prependVerbLine, bool appendEmptyLine)
        {
            return this.ToString(prependVerbLine, appendEmptyLine, false);
        }

        [CodeDescription("Returns current Request Headers as a string.")]
        public string ToString(bool prependVerbLine, bool appendEmptyLine, bool includeProtocolAndHostInPath)
        {
            StringBuilder builder = new StringBuilder(0x200);
            if (prependVerbLine)
            {
                if (includeProtocolAndHostInPath && !this.HTTPMethod.Equals("CONNECT", StringComparison.OrdinalIgnoreCase))
                {
                    builder.AppendFormat("{0} {1}://{2}{3}{4} {5}\r\n", new object[] { this.HTTPMethod, this._UriScheme, this._uriUserInfo, base["Host"], this.RequestPath, base.HTTPVersion });
                }
                else
                {
                    builder.AppendFormat("{0} {1} {2}\r\n", this.HTTPMethod, this.RequestPath, base.HTTPVersion);
                }
            }
            for (int i = 0; i < base.storage.Count; i++)
            {
                builder.Append(base.storage[i].Name + ": " + base.storage[i].Value + "\r\n");
            }
            if (appendEmptyLine)
            {
                builder.Append("\r\n");
            }
            return builder.ToString();
        }

        [CodeDescription("Byte array representing the HTTP Request path.")]
        public byte[] RawPath
        {
            get
            {
                return this._RawPath;
            }
            set
            {
                if (value == null)
                {
                    value = new byte[0];
                }
                this._RawPath = (byte[]) value.Clone();
                this._Path = base._HeaderEncoding.GetString(this._RawPath);
            }
        }

        [CodeDescription("String representing the HTTP Request path.")]
        public string RequestPath
        {
            get
            {
                return this._Path;
            }
            set
            {
                if (value == null)
                {
                    value = string.Empty;
                }
                this._Path = value;
                this._RawPath = base._HeaderEncoding.GetBytes(this._Path);
            }
        }

        [CodeDescription("URI Scheme for this HTTP Request; usually 'http' or 'https'")]
        public string UriScheme
        {
            get
            {
                return this._UriScheme;
            }
            set
            {
                this._UriScheme = value;
            }
        }

        [CodeDescription("For FTP URLs, returns either null or user:pass@")]
        public string UriUserInfo
        {
            get
            {
                return this._uriUserInfo;
            }
        }
    }
}

