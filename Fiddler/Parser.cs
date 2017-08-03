namespace Fiddler
{
    using System;
    using System.Globalization;
    using System.Runtime.InteropServices;

    public class Parser
    {
        internal static void CrackRequestLine(byte[] arrRequest, out int ixURIOffset, out int iURILen, out int ixHeaderNVPOffset)
        {
            int num2;
            ixHeaderNVPOffset = num2 = 0;
            ixURIOffset = iURILen = num2;
            int index = 0;
            do
            {
                if (arrRequest[index] == 0x20)
                {
                    if (ixURIOffset == 0)
                    {
                        ixURIOffset = index + 1;
                    }
                    else if (iURILen == 0)
                    {
                        iURILen = index - ixURIOffset;
                    }
                }
                else if (arrRequest[index] == 10)
                {
                    ixHeaderNVPOffset = index + 1;
                }
                index++;
            }
            while (ixHeaderNVPOffset == 0);
        }

        internal static bool FindEndOfHeaders(byte[] arrData, ref int iBodySeekProgress, long lngDataLen, out HTTPHeaderParseWarnings oWarnings)
        {
            bool flag;
            oWarnings = HTTPHeaderParseWarnings.None;
        Label_0003:
            flag = false;
            while (((long) iBodySeekProgress) < (lngDataLen - 1L))
            {
                iBodySeekProgress++;
                if (10 == arrData[iBodySeekProgress - 1])
                {
                    flag = true;
                    break;
                }
            }
            if (flag)
            {
                if ((13 != arrData[iBodySeekProgress]) && (10 != arrData[iBodySeekProgress]))
                {
                    iBodySeekProgress++;
                    goto Label_0003;
                }
                if (10 == arrData[iBodySeekProgress])
                {
                    oWarnings = HTTPHeaderParseWarnings.EndedWithLFLF;
                    return true;
                }
                iBodySeekProgress++;
                if ((((long) iBodySeekProgress) < lngDataLen) && (10 == arrData[iBodySeekProgress]))
                {
                    if (13 != arrData[iBodySeekProgress - 3])
                    {
                        oWarnings = HTTPHeaderParseWarnings.EndedWithLFCRLF;
                    }
                    return true;
                }
                if (iBodySeekProgress > 3)
                {
                    iBodySeekProgress -= 4;
                }
                else
                {
                    iBodySeekProgress = 0;
                }
            }
            return false;
        }

        public static bool FindEntityBodyOffsetFromArray(byte[] arrData, out int iHeadersLen, out int iEntityBodyOffset, out HTTPHeaderParseWarnings outWarnings)
        {
            if ((arrData != null) && (arrData.Length >= 2))
            {
                int iBodySeekProgress = 0;
                long length = arrData.Length;
                if (FindEndOfHeaders(arrData, ref iBodySeekProgress, length, out outWarnings))
                {
                    iEntityBodyOffset = iBodySeekProgress + 1;
                    switch (outWarnings)
                    {
                        case HTTPHeaderParseWarnings.None:
                            iHeadersLen = iBodySeekProgress - 3;
                            return true;

                        case HTTPHeaderParseWarnings.EndedWithLFLF:
                            iHeadersLen = iBodySeekProgress - 1;
                            return true;

                        case HTTPHeaderParseWarnings.EndedWithLFCRLF:
                            iHeadersLen = iBodySeekProgress - 2;
                            return true;
                    }
                }
            }
            iHeadersLen = iEntityBodyOffset = -1;
            outWarnings = HTTPHeaderParseWarnings.Malformed;
            return false;
        }

        internal static bool ParseNVPHeaders(HTTPHeaders oHeaders, string[] sHeaderLines, int iStartAt, ref string sErrors)
        {
            bool flag = true;
            for (int i = iStartAt; i < sHeaderLines.Length; i++)
            {
                int index = sHeaderLines[i].IndexOf(':');
                if (index > 0)
                {
                    oHeaders.Add(sHeaderLines[i].Substring(0, index), sHeaderLines[i].Substring(index + 1).Trim());
                }
                else
                {
                    sErrors = sErrors + string.Format("Missing colon in header #{0}, {1}\n", i - iStartAt, sHeaderLines[i]);
                    flag = false;
                }
            }
            return flag;
        }

        public static HTTPRequestHeaders ParseRequest(string sRequest)
        {
            HTTPRequestHeaders oHeaders = new HTTPRequestHeaders(CONFIG.oHeaderEncoding);
            string[] sHeaderLines = sRequest.Substring(0, sRequest.IndexOf("\r\n\r\n", StringComparison.Ordinal)).Replace("\r\n", "\n").Split(new char[] { '\n' });
            if (sHeaderLines.Length >= 1)
            {
                int index = sHeaderLines[0].IndexOf(' ');
                if (index > 0)
                {
                    oHeaders.HTTPMethod = sHeaderLines[0].Substring(0, index).ToUpper();
                    sHeaderLines[0] = sHeaderLines[0].Substring(index).Trim();
                }
                index = sHeaderLines[0].LastIndexOf(' ');
                if (index > 0)
                {
                    oHeaders.RequestPath = sHeaderLines[0].Substring(0, index);
                    oHeaders.HTTPVersion = sHeaderLines[0].Substring(index).Trim().ToUpper();
                    if (oHeaders.RequestPath.StartsWith("http://", StringComparison.OrdinalIgnoreCase))
                    {
                        oHeaders.UriScheme = "http";
                        index = oHeaders.RequestPath.IndexOfAny(new char[] { '/', '?' }, 7);
                        if (index == -1)
                        {
                            oHeaders.RequestPath = "/";
                        }
                        else
                        {
                            oHeaders.RequestPath = oHeaders.RequestPath.Substring(index);
                        }
                    }
                    else if (oHeaders.RequestPath.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
                    {
                        oHeaders.UriScheme = "https";
                        index = oHeaders.RequestPath.IndexOfAny(new char[] { '/', '?' }, 8);
                        if (index == -1)
                        {
                            oHeaders.RequestPath = "/";
                        }
                        else
                        {
                            oHeaders.RequestPath = oHeaders.RequestPath.Substring(index);
                        }
                    }
                    else if (oHeaders.RequestPath.StartsWith("ftp://", StringComparison.OrdinalIgnoreCase))
                    {
                        oHeaders.UriScheme = "ftp";
                        index = oHeaders.RequestPath.IndexOf('/', 6);
                        if (index == -1)
                        {
                            oHeaders.RequestPath = "/";
                        }
                        else
                        {
                            oHeaders.RequestPath = oHeaders.RequestPath.Substring(index);
                        }
                    }
                    string sErrors = string.Empty;
                    ParseNVPHeaders(oHeaders, sHeaderLines, 1, ref sErrors);
                    return oHeaders;
                }
            }
            return null;
        }

        public static HTTPResponseHeaders ParseResponse(string sResponse)
        {
            int index = sResponse.IndexOf("\r\n\r\n", StringComparison.Ordinal);
            if (index < 1)
            {
                index = sResponse.Length;
            }
            if (index >= 1)
            {
                string[] sHeaderLines = sResponse.Substring(0, index).Replace("\r\n", "\n").Split(new char[] { '\n' });
                if (sHeaderLines.Length < 1)
                {
                    return null;
                }
                HTTPResponseHeaders oHeaders = new HTTPResponseHeaders(CONFIG.oHeaderEncoding);
                int length = sHeaderLines[0].IndexOf(' ');
                if (length > 0)
                {
                    oHeaders.HTTPVersion = sHeaderLines[0].Substring(0, length).ToUpper();
                    sHeaderLines[0] = sHeaderLines[0].Substring(length + 1).Trim();
                    if (!oHeaders.HTTPVersion.StartsWith("HTTP/", StringComparison.OrdinalIgnoreCase))
                    {
                        return null;
                    }
                    oHeaders.HTTPResponseStatus = sHeaderLines[0];
                    bool flag = false;
                    length = sHeaderLines[0].IndexOf(' ');
                    if (length > 0)
                    {
                        flag = int.TryParse(sHeaderLines[0].Substring(0, length).Trim(), NumberStyles.Integer, NumberFormatInfo.InvariantInfo, out oHeaders.HTTPResponseCode);
                    }
                    else
                    {
                        flag = int.TryParse(sHeaderLines[0].Trim(), NumberStyles.Integer, NumberFormatInfo.InvariantInfo, out oHeaders.HTTPResponseCode);
                    }
                    if (!flag)
                    {
                        return null;
                    }
                    string sErrors = string.Empty;
                    ParseNVPHeaders(oHeaders, sHeaderLines, 1, ref sErrors);
                    return oHeaders;
                }
            }
            return null;
        }
    }
}

