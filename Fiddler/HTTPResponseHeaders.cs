namespace Fiddler
{
    using System;
    using System.Collections.Generic;
    using System.Text;

    public class HTTPResponseHeaders : HTTPHeaders, ICloneable
    {
        [CodeDescription("Status code from HTTP Response. If setting, also set HTTPResponseStatus too!")]
        public int HTTPResponseCode;
        [CodeDescription("Status text from HTTP Response (e.g. '200 OK').")]
        public string HTTPResponseStatus;

        public HTTPResponseHeaders()
        {
            this.HTTPResponseStatus = string.Empty;
        }

        public HTTPResponseHeaders(Encoding encodingForHeaders)
        {
            this.HTTPResponseStatus = string.Empty;
            base._HeaderEncoding = encodingForHeaders;
        }

        [CodeDescription("Replaces the current Response header set using a string representing the new HTTP headers.")]
        public bool AssignFromString(string sHeaders)
        {
            HTTPResponseHeaders headers = null;
            try
            {
                headers = Parser.ParseResponse(sHeaders);
            }
            catch (Exception)
            {
            }
            if (headers != null)
            {
                this.HTTPResponseCode = headers.HTTPResponseCode;
                this.HTTPResponseStatus = headers.HTTPResponseStatus;
                base.HTTPVersion = headers.HTTPVersion;
                base.storage = headers.storage;
                return true;
            }
            return false;
        }

        public object Clone()
        {
            HTTPResponseHeaders headers = (HTTPResponseHeaders) base.MemberwiseClone();
            headers.storage = new List<HTTPHeaderItem>(base.storage.Count);
            foreach (HTTPHeaderItem item in base.storage)
            {
                headers.storage.Add((HTTPHeaderItem) item.Clone());
            }
            return headers;
        }

        [CodeDescription("Returns a byte[] representing the HTTP headers.")]
        public byte[] ToByteArray(bool prependStatusLine, bool appendEmptyLine)
        {
            return base._HeaderEncoding.GetBytes(this.ToString(prependStatusLine, appendEmptyLine));
        }

        [CodeDescription("Returns a string containing the HTTP Response headers.")]
        public override string ToString()
        {
            return this.ToString(true, false);
        }

        [CodeDescription("Returns a string representing the HTTP headers.")]
        public string ToString(bool prependStatusLine, bool appendEmptyLine)
        {
            StringBuilder builder = new StringBuilder(0x200);
            if (prependStatusLine)
            {
                builder.AppendFormat("{0} {1}\r\n", base.HTTPVersion, this.HTTPResponseStatus);
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
    }
}

