namespace Fiddler
{
    using Microsoft.Win32;
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Drawing;
    using System.Globalization;
    using System.IO;
    using System.IO.Compression;
    using System.Net;
    using System.Reflection;
    using System.Runtime.InteropServices;
    using System.Security;
    using System.Text;
    using System.Text.RegularExpressions;
    using System.Web;
    using System.Windows.Forms;
    using Xceed.Compression;
    using Xceed.Compression.Formats;
    using Xceed.FileSystem;
    using Xceed.Zip;

    public class Utilities
    {
        private const int EM_SETCUEBANNER = 0x1501;
        internal const int LVCF_ORDER = 0x20;
        internal const int LVM_FIRST = 0x1000;
        internal const int LVM_GETCOLUMN = 0x105f;
        internal const int LVM_SETCOLUMN = 0x1060;
        internal const int MOD_ALT = 1;
        internal const int MOD_CONTROL = 2;
        internal const int MOD_SHIFT = 4;
        internal const int MOD_WIN = 8;
        private static Encoding[] sniffableEncodings = new Encoding[] { Encoding.UTF32, Encoding.BigEndianUnicode, Encoding.Unicode, Encoding.UTF8 };
        internal const int SW_HIDE = 0;
        internal const int SW_RESTORE = 9;
        internal const int SW_SHOW = 5;
        internal const int WM_COPYDATA = 0x4a;
        internal const int WM_HOTKEY = 0x312;
        internal const int WM_SIZE = 5;

        private static void _WriteChunkSizeToStream(MemoryStream oMS, int iLen)
        {
            string s = iLen.ToString("x");
            byte[] bytes = Encoding.ASCII.GetBytes(s);
            oMS.Write(bytes, 0, bytes.Length);
        }

        private static void _WriteCRLFToStream(MemoryStream oMS)
        {
            oMS.WriteByte(13);
            oMS.WriteByte(10);
        }

        public static void AdjustFontSize(Control c, float flSize)
        {
            if (c.Font.Size != flSize)
            {
                c.Font = new Font(c.Font.FontFamily, flSize);
            }
        }

        public static bool areOriginsEquivalent(string sHost1, string sHost2, int iDefaultPort)
        {
            string str;
            string str3;
            if (string.Equals(sHost1, sHost2, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
            int iPort = iDefaultPort;
            CrackHostAndPort(sHost1, out str, ref iPort);
            string a = str + ":" + iPort.ToString();
            iPort = iDefaultPort;
            CrackHostAndPort(sHost2, out str3, ref iPort);
            string b = str3 + ":" + iPort.ToString();
            return string.Equals(a, b, StringComparison.OrdinalIgnoreCase);
        }

        [CodeDescription("Returns a string representing a Hex view of a byte array. Slow.")]
        public static string ByteArrayToHexView(byte[] inArr, int iBytesPerLine)
        {
            return ByteArrayToHexView(inArr, iBytesPerLine, inArr.Length);
        }

        [CodeDescription("Returns a string representing a Hex view of a byte array. PERF: Slow.")]
        public static string ByteArrayToHexView(byte[] inArr, int iBytesPerLine, int iMaxByteCount)
        {
            if ((inArr == null) || (inArr.Length == 0))
            {
                return string.Empty;
            }
            if ((iBytesPerLine < 1) || (iMaxByteCount < 1))
            {
                throw new ArgumentOutOfRangeException("iBytesPerLine", "iBytesPerLine and iMaxByteCount must be >0");
            }
            iMaxByteCount = Math.Min(iMaxByteCount, inArr.Length);
            StringBuilder builder = new StringBuilder(iMaxByteCount * 5);
            int num = 0;
            bool flag = false;
            while (num < iMaxByteCount)
            {
                int num2 = Math.Min(iBytesPerLine, iMaxByteCount - num);
                flag = num2 < iBytesPerLine;
                for (int i = 0; i < num2; i++)
                {
                    builder.Append(inArr[num + i].ToString("X2"));
                    builder.Append(" ");
                }
                if (flag)
                {
                    builder.Append(new string(' ', 3 * (iBytesPerLine - num2)));
                }
                builder.Append(" ");
                for (int j = 0; j < num2; j++)
                {
                    if (inArr[num + j] < 0x20)
                    {
                        builder.Append(".");
                    }
                    else
                    {
                        builder.Append((char) inArr[num + j]);
                    }
                }
                if (flag)
                {
                    builder.Append(new string(' ', iBytesPerLine - num2));
                }
                builder.Append("\r\n");
                num += iBytesPerLine;
            }
            return builder.ToString();
        }

        [CodeDescription("Returns a string representing a Hex stream of a byte array. Slow.")]
        public static string ByteArrayToString(byte[] inArr)
        {
            if (inArr == null)
            {
                return "null";
            }
            if (inArr.Length == 0)
            {
                return "empty";
            }
            StringBuilder builder = new StringBuilder(inArr.Length * 3);
            for (int i = 0; i < inArr.Length; i++)
            {
                builder.Append(inArr[i].ToString("X2") + ' ');
            }
            return builder.ToString();
        }

        [CodeDescription("Returns a byte[] representing the bzip2'd representation of writeData[]")]
        public static byte[] bzip2Compress(byte[] writeData)
        {
            if ((writeData == null) || (writeData.Length == 0))
            {
                return new byte[0];
            }
            try
            {
                MemoryStream inner = new MemoryStream();
                using (ZLibCompressedStream stream2 = new ZLibCompressedStream(inner, CompressionMethod.BZip2, CompressionLevel.Highest))
                {
                    stream2.Write(writeData, 0, writeData.Length);
                }
                return inner.ToArray();
            }
            catch (Exception exception)
            {
                FiddlerApplication.DoNotifyUser("The content could not be compressed.\n\n" + exception.Message, "Fiddler: BZIP Compression failed");
                return writeData;
            }
        }

        public static byte[] bzip2Expand(byte[] compressedData)
        {
            if ((compressedData == null) || (compressedData.Length == 0))
            {
                return new byte[0];
            }
            try
            {
                MemoryStream inner = new MemoryStream(compressedData);
                MemoryStream stream2 = new MemoryStream(compressedData.Length);
                using (ZLibCompressedStream stream3 = new ZLibCompressedStream(inner, CompressionMethod.BZip2, CompressionLevel.Highest))
                {
                    byte[] buffer = new byte[0x8000];
                    int count = 0;
                    while ((count = stream3.Read(buffer, 0, buffer.Length)) > 0)
                    {
                        stream2.Write(buffer, 0, count);
                    }
                }
                return stream2.ToArray();
            }
            catch (Exception exception)
            {
                FiddlerApplication.DoNotifyUser("The content could not be decompressed.\n\n" + exception.Message, "Fiddler: Inflation failed");
                return new byte[0];
            }
        }

        [CodeDescription("Convert a full path into one that uses environment variables, e.g. %SYSTEM%")]
        public static string CollapsePath(string sPath)
        {
            StringBuilder pszBuf = new StringBuilder(0x103);
            if (PathUnExpandEnvStrings(sPath, pszBuf, pszBuf.Capacity))
            {
                return pszBuf.ToString();
            }
            return sPath;
        }

        public static int CompareVersions(string sRequiredVersion, Version verTest)
        {
            string[] strArray = sRequiredVersion.Split(new char[] { '.' });
            if (strArray.Length != 4)
            {
                return 5;
            }
            VersionStruct struct2 = new VersionStruct();
            if ((!int.TryParse(strArray[0], NumberStyles.Integer, NumberFormatInfo.InvariantInfo, out struct2.Major) || !int.TryParse(strArray[1], NumberStyles.Integer, NumberFormatInfo.InvariantInfo, out struct2.Minor)) || (!int.TryParse(strArray[2], NumberStyles.Integer, NumberFormatInfo.InvariantInfo, out struct2.Build) || !int.TryParse(strArray[3], NumberStyles.Integer, NumberFormatInfo.InvariantInfo, out struct2.Private)))
            {
                return 6;
            }
            if (struct2.Major > verTest.Major)
            {
                return 4;
            }
            if (verTest.Major > struct2.Major)
            {
                return -4;
            }
            if (struct2.Minor > verTest.Minor)
            {
                return 3;
            }
            if (verTest.Minor > struct2.Minor)
            {
                return -3;
            }
            if (struct2.Build > verTest.Build)
            {
                return 2;
            }
            if (verTest.Build > struct2.Build)
            {
                return -2;
            }
            if (struct2.Private > verTest.Revision)
            {
                return 1;
            }
            if (verTest.Revision > struct2.Private)
            {
                return -1;
            }
            return 0;
        }

        internal static string ContentTypeForFileExtension(string sExtension)
        {
            if ((sExtension == null) || (sExtension.Length < 1))
            {
                return null;
            }
            if (sExtension == ".js")
            {
                return "text/javascript";
            }
            if (sExtension == ".css")
            {
                return "text/css";
            }
            string str = null;
            try
            {
                RegistryKey key = Registry.ClassesRoot.OpenSubKey(sExtension, false);
                if (key != null)
                {
                    str = (string) key.GetValue("Content Type");
                    key.Close();
                }
            }
            catch (SecurityException)
            {
            }
            return str;
        }

        [CodeDescription("Copy a string to the clipboard, with exception handling.")]
        public static bool CopyToClipboard(string sText)
        {
            DataObject oData = new DataObject();
            oData.SetData(DataFormats.Text, sText);
            return CopyToClipboard(oData);
        }

        public static bool CopyToClipboard(DataObject oData)
        {
            try
            {
                Clipboard.SetDataObject(oData, true);
                return true;
            }
            catch (Exception exception)
            {
                FiddlerApplication.DoNotifyUser("Please disable any clipboard monitoring tools and try again.\n\n" + exception.Message, ".NET Framework Bug");
                return true;
            }
        }

        [CodeDescription("This function cracks the Host/Port combo, removing IPV6 brackets if needed.")]
        public static void CrackHostAndPort(string sHostPort, out string sHostname, ref int iPort)
        {
            int length = sHostPort.LastIndexOf(':');
            if ((length > -1) && (length > sHostPort.LastIndexOf(']')))
            {
                if (!int.TryParse(sHostPort.Substring(length + 1), NumberStyles.Integer, NumberFormatInfo.InvariantInfo, out iPort))
                {
                    iPort = -1;
                }
                sHostname = sHostPort.Substring(0, length);
            }
            else
            {
                sHostname = sHostPort;
            }
            if (sHostname.StartsWith("[", StringComparison.Ordinal) && sHostname.EndsWith("]", StringComparison.Ordinal))
            {
                sHostname = sHostname.Substring(1, sHostname.Length - 2);
            }
        }

        public static void CreateBundle(string sOutputFile, string[] sInputFiles, string sComment)
        {
            if (System.IO.File.Exists(sOutputFile))
            {
                System.IO.File.Delete(sOutputFile);
            }
            ZipArchive archive = new ZipArchive(new DiskFile(sOutputFile));
            MemoryFolder folder = new MemoryFolder();
            archive.TempFolder = folder;
            archive.BeginUpdate();
            if (!string.IsNullOrEmpty(sComment))
            {
                archive.Comment = sComment;
            }
            foreach (string str in sInputFiles)
            {
                if (System.IO.File.Exists(str))
                {
                    new DiskFile(str).CopyTo(archive.RootFolder, true);
                }
            }
            archive.EndUpdate();
        }

        [CodeDescription("Returns a byte[] containing a DEFLATE'd copy of writeData[]")]
        public static byte[] DeflaterCompress(byte[] writeData)
        {
            if ((writeData == null) || (writeData.Length == 0))
            {
                return new byte[0];
            }
            try
            {
                return QuickCompression.Compress(writeData, CompressionMethod.Deflated, CompressionLevel.Normal);
            }
            catch (Exception exception)
            {
                FiddlerApplication.DoNotifyUser("The content could not be compressed.\n\n" + exception.Message, "Fiddler: Deflation failed");
                return writeData;
            }
        }

        [CodeDescription("Returns a byte[] representing the INFLATE'd representation of compressedData[]")]
        public static byte[] DeflaterExpand(byte[] compressedData)
        {
            try
            {
                return DeflaterExpandInternal(CONFIG.bUseXceedDecompressForDeflate, compressedData);
            }
            catch (Exception exception)
            {
                FiddlerApplication.DoNotifyUser("The content could not be decompressed.\n\n" + exception.Message, "Fiddler: Inflation failed");
                return new byte[0];
            }
        }

        public static byte[] DeflaterExpandInternal(bool bUseXceed, byte[] compressedData)
        {
            if ((compressedData == null) || (compressedData.Length == 0))
            {
                return new byte[0];
            }
            int offset = 0;
            if (((compressedData.Length > 2) && (compressedData[0] == 120)) && (compressedData[1] == 0x9c))
            {
                offset = 2;
            }
            if (bUseXceed)
            {
                return QuickCompression.Decompress(compressedData, offset, compressedData.Length - offset, CompressionMethod.Deflated, CONFIG.bCheckCompressionIntegrity);
            }
            MemoryStream stream = new MemoryStream(compressedData, offset, compressedData.Length - offset);
            MemoryStream stream2 = new MemoryStream(compressedData.Length);
            using (DeflateStream stream3 = new DeflateStream(stream, CompressionMode.Decompress))
            {
                byte[] buffer = new byte[0x8000];
                int count = 0;
                while ((count = stream3.Read(buffer, 0, buffer.Length)) > 0)
                {
                    stream2.Write(buffer, 0, count);
                }
            }
            return stream2.ToArray();
        }

        public static byte[] doChunk(byte[] writeData, int iSuggestedChunkCount)
        {
            if ((writeData == null) || (writeData.Length < 1))
            {
                return Encoding.ASCII.GetBytes("0\r\n\r\n");
            }
            if (iSuggestedChunkCount < 1)
            {
                iSuggestedChunkCount = 1;
            }
            if (iSuggestedChunkCount > writeData.Length)
            {
                iSuggestedChunkCount = writeData.Length;
            }
            MemoryStream oMS = new MemoryStream(writeData.Length + (10 * iSuggestedChunkCount));
            int offset = 0;
            do
            {
                int num2 = writeData.Length - offset;
                int num3 = num2 / iSuggestedChunkCount;
                num3 = Math.Max(1, num3);
                num3 = Math.Min(num2, num3);
                _WriteChunkSizeToStream(oMS, num3);
                _WriteCRLFToStream(oMS);
                oMS.Write(writeData, offset, num3);
                _WriteCRLFToStream(oMS);
                offset += num3;
                iSuggestedChunkCount--;
                if (iSuggestedChunkCount < 1)
                {
                    iSuggestedChunkCount = 1;
                }
            }
            while (offset < writeData.Length);
            _WriteChunkSizeToStream(oMS, 0);
            _WriteCRLFToStream(oMS);
            return oMS.ToArray();
        }

        public static void DoFlash(IntPtr hWnd)
        {
            FLASHWINFO flashwinfo=new FLASHWINFO();
            flashwinfo = new FLASHWINFO {
                cbSize = Marshal.SizeOf(flashwinfo),
                hwnd = hWnd,
                dwFlags = FlashWInfo.FLASHW_TIMERNOFG | FlashWInfo.FLASHW_TRAY,
                uCount = 0,
                dwTimeout = 0
            };
            FlashWindowEx(ref flashwinfo);
        }

        public static byte[] doUnchunk(byte[] writeData)
        {
            if ((writeData == null) || (writeData.Length == 0))
            {
                return new byte[0];
            }
            MemoryStream stream = new MemoryStream(writeData.Length);
            int index = 0;
            bool flag = false;
            while (!flag && (index <= (writeData.Length - 3)))
            {
                string s = Encoding.ASCII.GetString(writeData, index, Math.Min(0x20, writeData.Length - index));
                int length = s.IndexOf("\r\n", StringComparison.Ordinal);
                if (length <= 0)
                {
                    throw new InvalidDataException("HTTP Error: The chunked entity body is corrupt. Cannot find Chunk-Length in expected location. Offset: " + index.ToString());
                }
                index += length + 2;
                s = s.Substring(0, length);
                length = s.IndexOf(';');
                if (length > 0)
                {
                    s = s.Substring(0, length);
                }
                int count = int.Parse(s, NumberStyles.HexNumber);
                if (count == 0)
                {
                    flag = true;
                }
                else
                {
                    if (writeData.Length < (count + index))
                    {
                        throw new InvalidDataException("HTTP Error: The chunked entity body is corrupt. The final chunk length is greater than the number of bytes remaining.");
                    }
                    stream.Write(writeData, index, count);
                    index += count + 2;
                }
            }
            if ((!flag && CONFIG.bReportHTTPErrors) && !CONFIG.QuietMode)
            {
                FiddlerApplication.DoNotifyUser("Chunked body did not terminate properly with 0-sized chunk.", "HTTP Protocol Violation");
            }
            byte[] buffer = new byte[stream.Length];
            stream.Position = 0L;
            stream.Read(buffer, 0, (int) stream.Length);
            return buffer;
        }

        public static void EnsureOverwritable(string sFilename)
        {
            if (!Directory.Exists(Path.GetDirectoryName(sFilename)))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(sFilename));
            }
            if (System.IO.File.Exists(sFilename))
            {
                FileAttributes attributes = System.IO.File.GetAttributes(sFilename);
                System.IO.File.SetAttributes(sFilename, attributes & ~(FileAttributes.System | FileAttributes.Hidden | FileAttributes.ReadOnly));
            }
        }

        internal static bool FiddlerMeetsVersionRequirement(Assembly assemblyInput, string sWhatType)
        {
            if (!assemblyInput.IsDefined(typeof(RequiredVersionAttribute), false))
            {
                return false;
            }
            RequiredVersionAttribute customAttribute = (RequiredVersionAttribute) Attribute.GetCustomAttribute(assemblyInput, typeof(RequiredVersionAttribute));
            int num = CompareVersions(customAttribute.RequiredVersion, CONFIG.FiddlerVersionInfo);
            if (num > 0)
            {
                FiddlerApplication.DoNotifyUser(string.Format("The {0} in {1} require Fiddler v{2} or later. (You have v{3})\n\nPlease install the latest version of Fiddler from http://www.fiddler2.com.\n\nCode: {4}", new object[] { sWhatType, assemblyInput.CodeBase, customAttribute.RequiredVersion, CONFIG.FiddlerVersionInfo, num }), "Extension Not Loaded");
                return false;
            }
            return true;
        }

        internal static string FileExtensionForMIMEType(string sMIME)
        {
            sMIME = sMIME.ToLower();
            switch (sMIME)
            {
                case "text/css":
                    return ".css";

                case "text/html":
                    return ".htm";

                case "text/javascript":
                case "application/javascript":
                case "application/x-javascript":
                    return ".js";

                case "image/jpg":
                case "image/jpeg":
                    return ".jpg";

                case "image/gif":
                    return ".gif";

                case "image/png":
                    return ".png";

                case "image/x-icon":
                    return ".ico";

                case "text/xml":
                    return ".xml";

                case "video/x-flv":
                    return ".flv";

                case "video/mp4":
                    return ".mp4";
            }
            return ".txt";
        }

        [DllImport("user32.dll")]
        internal static extern IntPtr FindWindow(string lpClassName, string lpWindowName);
        [return: MarshalAs(UnmanagedType.Bool)]
        [DllImport("user32.dll")]
        private static extern bool FlashWindowEx(ref FLASHWINFO pwfi);
        [DllImport("user32.dll")]
        internal static extern short GetAsyncKeyState(int vKey);
        internal static System.Drawing.Color GetColorFromString(string sColor)
        {
            if ((sColor.Length == 7) && (sColor[0] == '#'))
            {
                return System.Drawing.Color.FromArgb(Convert.ToInt32(sColor.Substring(1, 2), 0x10), Convert.ToInt32(sColor.Substring(3, 2), 0x10), Convert.ToInt32(sColor.Substring(5, 2), 0x10));
            }
            return System.Drawing.Color.FromName(sColor);
        }

        private static COMBOBOXINFO GetComboBoxInfo(Control control)
        {
            COMBOBOXINFO comboboxinfo=new COMBOBOXINFO();
            comboboxinfo = new COMBOBOXINFO {
                cbSize = Marshal.SizeOf(comboboxinfo)
            };
            if (!GetComboBoxInfo(control.Handle, ref comboboxinfo))
            {
                Marshal.GetLastWin32Error();
            }
            return comboboxinfo;
        }

        [DllImport("user32.dll", SetLastError=true)]
        private static extern bool GetComboBoxInfo(IntPtr hwnd, ref COMBOBOXINFO pcbi);
        public static string GetCommaTokenValue(string sString, string sTokenName)
        {
            string str = null;
            if ((sString != null) && (sString.Length > 0))
            {
                System.Text.RegularExpressions.Match match = new Regex(sTokenName + "\\s?=?\\s?[\"]?(?<TokenValue>[^\";,]*)").Match(sString);
                if (match.Success && (match.Groups["TokenValue"] != null))
                {
                    str = match.Groups["TokenValue"].Value;
                }
            }
            return str;
        }

        [CodeDescription("Gets (via Headers or Sniff) the provided body's text Encoding. Returns CONFIG.oHeaderEncoding (usually UTF-8) if unknown. Potentially slow.")]
        public static Encoding getEntityBodyEncoding(HTTPHeaders oHeaders, byte[] oBody)
        {
            if (oHeaders == null)
            {
                return CONFIG.oHeaderEncoding;
            }
            string tokenValue = oHeaders.GetTokenValue("Content-Type", "charset");
            if (tokenValue != null)
            {
                try
                {
                    return Encoding.GetEncoding(tokenValue);
                }
                catch (Exception)
                {
                }
            }
            Encoding oHeaderEncoding = CONFIG.oHeaderEncoding;
            if ((oBody != null) && (oBody.Length >= 2))
            {
                foreach (Encoding encoding2 in sniffableEncodings)
                {
                    byte[] preamble = encoding2.GetPreamble();
                    if (oBody.Length >= preamble.Length)
                    {
                        bool flag = preamble.Length > 0;
                        for (int i = 0; i < preamble.Length; i++)
                        {
                            if (preamble[i] != oBody[i])
                            {
                                flag = false;
                                break;
                            }
                        }
                        if (flag)
                        {
                            oHeaderEncoding = encoding2;
                            break;
                        }
                    }
                }
                if (oHeaders.ExistsAndContains("Content-Type", "html"))
                {
                    string input = oHeaderEncoding.GetString(oBody, 0, Math.Min(0x1000, oBody.Length));
                    MatchCollection matchs = new Regex("<meta.*charset\\s*=\\s*(?<thecharset>[^\\\"]*)", RegexOptions.IgnoreCase).Matches(input);
                    if ((matchs.Count <= 0) || (matchs[0].Groups.Count <= 0))
                    {
                        return oHeaderEncoding;
                    }
                    try
                    {
                        oHeaderEncoding = Encoding.GetEncoding(matchs[0].Groups[1].Value);
                    }
                    catch (Exception)
                    {
                    }
                }
            }
            return oHeaderEncoding;
        }

        [CodeDescription("Run an executable, wait for it to exit, and return its output as a string.")]
        public static string GetExecutableOutput(string sExecute, string sParams, out int iExitCode)
        {
            iExitCode = -999;
            StringBuilder builder = new StringBuilder();
            builder.Append("Results from " + sExecute + " " + sParams + "\r\n\r\n");
            try
            {
                string str;
                Process process = new Process {
                    StartInfo = { UseShellExecute = false, RedirectStandardOutput = true, RedirectStandardError = false, CreateNoWindow = true, FileName = sExecute, Arguments = sParams }
                };
                process.Start();
                while ((str = process.StandardOutput.ReadLine()) != null)
                {
                    str = str.TrimEnd(new char[0]);
                    if (str != string.Empty)
                    {
                        builder.Append(str + "\r\n");
                    }
                }
                iExitCode = process.ExitCode;
                process.Dispose();
            }
            catch (Exception exception)
            {
                builder.Append("Exception thrown: " + exception.ToString() + "\r\n" + exception.StackTrace.ToString());
            }
            builder.Append("-------------------------------------------\r\n");
            return builder.ToString();
        }

        [CodeDescription("Returns an bool from the registry, or bDefault if the registry key is missing or cannot be used as an bool.")]
        public static bool GetRegistryBool(RegistryKey oReg, string sName, bool bDefault)
        {
            bool flag = bDefault;
            object obj2 = oReg.GetValue(sName);
            if (obj2 is int)
            {
                return (1 == ((int) obj2));
            }
            if (obj2 is string)
            {
                flag = string.Equals(obj2 as string, "true", StringComparison.OrdinalIgnoreCase);
            }
            return flag;
        }

        [CodeDescription("Returns an float from the registry, or flDefault if the registry key is missing or cannot be used as an float.")]
        public static float GetRegistryFloat(RegistryKey oReg, string sName, float flDefault)
        {
            float result = flDefault;
            object obj2 = oReg.GetValue(sName);
            if (obj2 is int)
            {
                return (float) obj2;
            }
            if ((obj2 is string) && !float.TryParse((string) obj2, NumberStyles.Float, CultureInfo.InvariantCulture, out result))
            {
                result = flDefault;
            }
            return result;
        }

        [CodeDescription("Returns an integer from the registry, or iDefault if the registry key is missing or cannot be used as an integer.")]
        public static int GetRegistryInt(RegistryKey oReg, string sName, int iDefault)
        {
            int result = iDefault;
            object obj2 = oReg.GetValue(sName);
            if (obj2 is int)
            {
                return (int) obj2;
            }
            if ((obj2 is string) && !int.TryParse((string) obj2, out result))
            {
                return iDefault;
            }
            return result;
        }

        [CodeDescription("Gets (via Headers or Sniff) the Response Text Encoding. Returns CONFIG.oHeaderEncoding (usually UTF-8) if unknown. Potentially slow.")]
        public static Encoding getResponseBodyEncoding(Session oSession)
        {
            if ((oSession != null) && (oSession.bHasResponse && (oSession.oResponse.headers != null)))
            {
                return getEntityBodyEncoding(oSession.oResponse.headers, oSession.responseBodyBytes);
            }
            return CONFIG.oHeaderEncoding;
        }

        [CodeDescription("Gets a string from a byte-array, stripping a BOM if present.")]
        public static string GetStringFromArrayRemovingBOM(byte[] arrInput, Encoding oDefaultEncoding)
        {
            if ((arrInput != null) && (arrInput.Length >= 2))
            {
                foreach (Encoding encoding in sniffableEncodings)
                {
                    byte[] preamble = encoding.GetPreamble();
                    if (arrInput.Length >= preamble.Length)
                    {
                        bool flag = preamble.Length > 0;
                        for (int i = 0; i < preamble.Length; i++)
                        {
                            if (preamble[i] != arrInput[i])
                            {
                                flag = false;
                                break;
                            }
                        }
                        if (flag)
                        {
                            int length = encoding.GetPreamble().Length;
                            return encoding.GetString(arrInput, length, arrInput.Length - length);
                        }
                    }
                }
            }
            return oDefaultEncoding.GetString(arrInput);
        }

        internal static string GetStringFromColor(System.Drawing.Color c)
        {
            if (c.IsNamedColor)
            {
                return c.Name;
            }
            return string.Format("#{0:x2}{0:x2}{0:x2}", c.R, c.G, c.B);
        }

        [DllImport("kernel32.dll", SetLastError=true)]
        internal static extern IntPtr GlobalFree(IntPtr hMem);
        internal static void GlobalFreeIfNonZero(IntPtr hMem)
        {
            if (IntPtr.Zero != hMem)
            {
                GlobalFree(hMem);
            }
        }

        [CodeDescription("Returns a byte[] containing a gzip-compressed copy of writeData[]")]
        public static byte[] GzipCompress(byte[] writeData)
        {
            try
            {
                MemoryStream inner = new MemoryStream();
                using (GZipCompressedStream stream2 = new GZipCompressedStream(inner))
                {
                    stream2.Write(writeData, 0, writeData.Length);
                }
                return inner.ToArray();
            }
            catch (Exception exception)
            {
                FiddlerApplication.DoNotifyUser("The content could not be compressed.\n\n" + exception.Message, "Fiddler: GZip failed");
                return writeData;
            }
        }

        [CodeDescription("Returns a byte[] containing an un-gzipped copy of compressedData[]")]
        public static byte[] GzipExpand(byte[] compressedData)
        {
            try
            {
                return GzipExpandInternal(CONFIG.bUseXceedDecompressForGZIP, compressedData);
            }
            catch (Exception exception)
            {
                FiddlerApplication.DoNotifyUser("The content could not be decompressed.\n\n" + exception.Message, "Fiddler: UnGZip failed");
                return new byte[0];
            }
        }

        public static byte[] GzipExpandInternal(bool bUseXceed, byte[] compressedData)
        {
            if ((compressedData == null) || (compressedData.Length == 0))
            {
                return new byte[0];
            }
            MemoryStream inner = new MemoryStream(compressedData);
            MemoryStream stream2 = new MemoryStream(compressedData.Length);
            if (bUseXceed)
            {
                using (GZipCompressedStream stream3 = new GZipCompressedStream(inner, CompressionLevel.Normal, false, CONFIG.bCheckCompressionIntegrity))
                {
                    byte[] buffer = new byte[0x8000];
                    int count = 0;
                    while ((count = stream3.Read(buffer, 0, buffer.Length)) > 0)
                    {
                        stream2.Write(buffer, 0, count);
                    }
                    goto Label_00AE;
                }
            }
            using (GZipStream stream4 = new GZipStream(inner, CompressionMode.Decompress))
            {
                byte[] buffer2 = new byte[0x8000];
                int num2 = 0;
                while ((num2 = stream4.Read(buffer2, 0, buffer2.Length)) > 0)
                {
                    stream2.Write(buffer2, 0, num2);
                }
            }
        Label_00AE:
            return stream2.ToArray();
        }

        internal static string HtmlEncode(string sInput)
        {
            if (sInput == null)
            {
                return null;
            }
            return HttpUtility.HtmlEncode(sInput);
        }

        [CodeDescription("Returns TRUE if the HTTP Method MAY have a body.")]
        public static bool HTTPMethodAllowsBody(string sMethod)
        {
            if (((!("POST" == sMethod) && !("PUT" == sMethod)) && (!("PROPPATCH" == sMethod) && !("LOCK" == sMethod))) && !("PROPFIND" == sMethod))
            {
                return ("SEARCH" == sMethod);
            }
            return true;
        }

        [CodeDescription("Returns TRUE if the HTTP Method MUST have a body.")]
        public static bool HTTPMethodRequiresBody(string sMethod)
        {
            return ("PROPPATCH" == sMethod);
        }

        public static IPEndPoint IPEndPointFromHostPortString(string sHostAndPort)
        {
            if ((sHostAndPort == null) || sHostAndPort.Trim().Equals(string.Empty))
            {
                return null;
            }
            sHostAndPort = TrimAfter(sHostAndPort, ';');
            try
            {
                string str;
                int iPort = 80;
                CrackHostAndPort(sHostAndPort, out str, ref iPort);
                return new IPEndPoint(DNSResolver.GetIPAddress(str, true), iPort);
            }
            catch (Exception)
            {
                return null;
            }
        }

        [CodeDescription("This function attempts to be a ~fast~ way to return an IP from a hoststring that contains an IP-Literal. ")]
        public static IPAddress IPFromString(string sHost)
        {
            for (int i = 0; i < sHost.Length; i++)
            {
                if (((((sHost[i] != '.') && (sHost[i] != ':')) && ((sHost[i] < '0') || (sHost[i] > '9'))) && ((sHost[i] < 'A') || (sHost[i] > 'F'))) && ((sHost[i] < 'a') || (sHost[i] > 'f')))
                {
                    return null;
                }
            }
            if (sHost.EndsWith("."))
            {
                sHost = TrimBeforeLast(sHost, '.');
            }
            try
            {
                return IPAddress.Parse(sHost);
            }
            catch
            {
                return null;
            }
        }

        public static bool IsBinaryMIME(string sContentType)
        {
            if (string.IsNullOrEmpty(sContentType))
            {
                return false;
            }
            if (sContentType.StartsWith("text/", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }
            if (sContentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase))
            {
                return !sContentType.StartsWith("image/svg+xml", StringComparison.OrdinalIgnoreCase);
            }
            return (sContentType.StartsWith("application/octet", StringComparison.OrdinalIgnoreCase) || (sContentType.StartsWith("application/x-shockwave-flash", StringComparison.OrdinalIgnoreCase) || (sContentType.StartsWith("audio/", StringComparison.OrdinalIgnoreCase) || sContentType.StartsWith("video/", StringComparison.OrdinalIgnoreCase))));
        }

        internal static bool IsChunkedBodyComplete(Session m_session, MemoryStream oData, long iStartAtOffset, out long outStartOfLatestChunk, out long outEndOfEntity)
        {
            long num = iStartAtOffset;
            outStartOfLatestChunk = num;
            outEndOfEntity = -1L;
            while (num < oData.Length)
            {
                outStartOfLatestChunk = num;
                oData.Position = num;
                byte[] buffer = new byte[0x20];
                oData.Read(buffer, 0, buffer.Length);
                string sInput = Encoding.ASCII.GetString(buffer);
                int index = sInput.IndexOf("\r\n", StringComparison.Ordinal);
                if (index > -1)
                {
                    num += index + 2;
                    sInput = sInput.Substring(0, index);
                }
                else
                {
                    return false;
                }
                index = sInput.IndexOf(';');
                if (index > -1)
                {
                    sInput = sInput.Substring(0, index);
                }
                int iOutput = 0;
                if (!TryHexParse(sInput, out iOutput))
                {
                    FiddlerApplication.HandleHTTPError(m_session, SessionFlags.ProtocolViolationInResponse | SessionFlags.ProtocolViolationInRequest, true, true, "Illegal chunked encoding. '" + sInput + "' is not a hexadecimal number.");
                    FiddlerApplication.DebugSpew(ByteArrayToHexView(buffer, 0x20));
                    return true;
                }
                if (iOutput == 0)
                {
                    oData.Position = num;
                    bool flag = true;
                    bool flag2 = false;
                    for (int i = oData.ReadByte(); i != -1; i = oData.ReadByte())
                    {
                        int num5 = i;
                        if (num5 != 10)
                        {
                            if (num5 != 13)
                            {
                                goto Label_010C;
                            }
                            flag2 = true;
                        }
                        else if (flag2)
                        {
                            if (flag)
                            {
                                outEndOfEntity = oData.Position;
                                oData.Position = oData.Length;
                                return true;
                            }
                            flag = true;
                            flag2 = false;
                        }
                        else
                        {
                            flag2 = false;
                            flag = false;
                        }
                        continue;
                    Label_010C:
                        flag2 = false;
                        flag = false;
                    }
                    return false;
                }
                num += iOutput + 2;
            }
            oData.Position = oData.Length;
            return false;
        }

        internal static bool IsHostInList(string[] slHostList, string sHost)
        {
            if ((slHostList != null) && (slHostList.Length >= 0))
            {
                string str = TrimAfter(sHost, ':').ToLower();
                foreach (string str2 in slHostList)
                {
                    if (str == str2)
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        internal static bool isHTTP200Array(byte[] arrData)
        {
            return ((((((arrData.Length > 12) && (arrData[0] == 0x48)) && ((arrData[1] == 0x54) && (arrData[2] == 0x54))) && (((arrData[3] == 80) && (arrData[4] == 0x2f)) && ((arrData[5] == 0x31) && (arrData[6] == 0x2e)))) && ((arrData[9] == 50) && (arrData[10] == 0x30))) && (arrData[11] == 0x30));
        }

        [return: MarshalAs(UnmanagedType.Bool)]
        [DllImport("user32.dll")]
        internal static extern bool IsIconic(IntPtr hWnd);
        [CodeDescription("Returns true if True if the sHostAndPort's host is 127.0.0.1, 'localhost', or ::1. Note that list is not complete.")]
        public static bool isLocalhost(string sHostAndPort)
        {
            string str;
            int iPort = 0;
            CrackHostAndPort(sHostAndPort, out str, ref iPort);
            if ((!string.Equals(str, "localhost", StringComparison.OrdinalIgnoreCase) && !string.Equals(str, "localhost.", StringComparison.OrdinalIgnoreCase)) && !string.Equals(str, "127.0.0.1", StringComparison.OrdinalIgnoreCase))
            {
                return string.Equals(str, "::1", StringComparison.OrdinalIgnoreCase);
            }
            return true;
        }

        [CodeDescription("Returns false if Hostname contains any dots or colons.")]
        public static bool isPlainHostName(string sHostAndPort)
        {
            string str;
            int iPort = 0;
            CrackHostAndPort(sHostAndPort, out str, ref iPort);
            char[] anyOf = new char[] { '.', ':' };
            return (str.IndexOfAny(anyOf) < 0);
        }

        [CodeDescription("ShellExecutes the sURL.")]
        public static bool LaunchHyperlink(string sURL)
        {
            try
            {
                using (Process.Start(sURL))
                {
                }
                return true;
            }
            catch (Exception exception)
            {
                FiddlerApplication.DoNotifyUser("Your web browser is not correctly configured to launch hyperlinks.\n\nTo see this content, visit:\n\t" + sURL + "\n...in your web browser.\n\nError: " + exception.Message, "Error");
            }
            return false;
        }

        public static string ObtainOpenFilename(string sDialogTitle, string sFilter)
        {
            FileDialog dialog = new OpenFileDialog {
                Title = sDialogTitle,
                Filter = sFilter
            };
            dialog.CustomPlaces.Add(CONFIG.GetPath("Captures"));
            string fileName = null;
            if (DialogResult.OK == dialog.ShowDialog(FiddlerApplication.UI))
            {
                fileName = dialog.FileName;
            }
            dialog.Dispose();
            return fileName;
        }

        public static string ObtainSaveFilename(string sDialogTitle, string sFilter)
        {
            FileDialog dialog = new SaveFileDialog {
                Title = sDialogTitle,
                Filter = sFilter
            };
            dialog.CustomPlaces.Add(CONFIG.GetPath("Captures"));
            string fileName = null;
            if (DialogResult.OK == dialog.ShowDialog(FiddlerApplication.UI))
            {
                fileName = dialog.FileName;
            }
            dialog.Dispose();
            return fileName;
        }

        [CodeDescription("Tokenize a string into tokens. Delimits on whitespace; \" marks are dropped unless preceded by \\ characters.")]
        public static string[] Parameterize(string sInput)
        {
            List<string> list = new List<string>();
            bool flag = false;
            StringBuilder builder = new StringBuilder();
            for (int i = 0; i < sInput.Length; i++)
            {
                switch (sInput[i])
                {
                    case ' ':
                    case '\t':
                    {
                        if (flag)
                        {
                            goto Label_00A1;
                        }
                        if ((builder.Length > 0) || ((i > 0) && (sInput[i - 1] == '"')))
                        {
                            list.Add(builder.ToString());
                            builder.Length = 0;
                        }
                        continue;
                    }
                    case '"':
                    {
                        if ((i <= 0) || (sInput[i - 1] != '\\'))
                        {
                            break;
                        }
                        builder.Remove(builder.Length - 1, 1);
                        builder.Append('"');
                        continue;
                    }
                    default:
                        goto Label_00B1;
                }
                flag = !flag;
                continue;
            Label_00A1:
                builder.Append(sInput[i]);
                continue;
            Label_00B1:
                builder.Append(sInput[i]);
            }
            if (builder.Length > 0)
            {
                list.Add(builder.ToString());
            }
            return list.ToArray();
        }

        [return: MarshalAs(UnmanagedType.Bool)]
        [DllImport("shlwapi.dll", CharSet=CharSet.Auto)]
        internal static extern bool PathUnExpandEnvStrings(string pszPath, [Out] StringBuilder pszBuf, int cchBuf);
        [return: MarshalAs(UnmanagedType.Bool)]
        [DllImport("winmm.dll", SetLastError=true)]
        internal static extern bool PlaySound(string pszSound, IntPtr hMod, SoundFlags sf);
        [CodeDescription("Reads oStream until arrBytes is filled.")]
        public static int ReadEntireStream(Stream oStream, byte[] arrBytes)
        {
            int offset = 0;
            while (offset < arrBytes.LongLength)
            {
                offset += oStream.Read(arrBytes, offset, arrBytes.Length - offset);
            }
            return offset;
        }

        [CodeDescription("Load the specified .SAZ or .ZIP session archive")]
        public static Session[] ReadSessionArchive(string sFilename, bool bVerboseDialogs)
        {
            if (!System.IO.File.Exists(sFilename))
            {
                if (bVerboseDialogs)
                {
                    FiddlerApplication.DoNotifyUser("File " + sFilename + " does not exist.", "ReadSessionArchive Failed", MessageBoxIcon.Hand);
                }
                return null;
            }
            Application.DoEvents();
            ZipArchive archive = null;
            List<Session> list = new List<Session>();
            try
            {
                FileStream stream = System.IO.File.Open(sFilename, FileMode.Open, FileAccess.Read, FileShare.Read);
                if (((stream.Length < 0x40L) || (stream.ReadByte() != 80)) || (stream.ReadByte() != 0x4b))
                {
                    if (bVerboseDialogs)
                    {
                        FiddlerApplication.DoNotifyUser(sFilename + " is not a Fiddler-generated .SAZ archive of HTTP Sessions.", "ReadSessionArchive Failed", MessageBoxIcon.Hand);
                    }
                    stream.Close();
                    return null;
                }
                stream.Close();
                archive = new ZipArchive(new DiskFile(sFilename));
                archive.BeginUpdate();
                AbstractFolder folder = archive.GetFolder("raw");
                if (!folder.Exists)
                {
                    if (bVerboseDialogs)
                    {
                        FiddlerApplication.DoNotifyUser("The selected file is not a Fiddler-generated .SAZ archive of HTTP Sessions.", "Invalid Archive", MessageBoxIcon.Hand);
                    }
                    archive.EndUpdate();
                    return null;
                }
                foreach (AbstractFile file in folder.GetFiles(true, new object[] { "*_c.txt" }))
                {
                    try
                    {
                        Stream stream2;
                        byte[] arrBytes = new byte[file.Size];
                    Label_010F:
                        try
                        {
                            stream2 = file.OpenRead(FileShare.Read);
                        }
                        catch (InvalidDecryptionPasswordException)
                        {
                            string str = frmPrompt.GetUserString("Password-Protected Session Archive", "Enter the password (case-sensitive) that will be used to decrypt this session archive file. Leave blank to abort open.", string.Empty);
                            if (str != string.Empty)
                            {
                                archive.DefaultDecryptionPassword = str;
                                goto Label_010F;
                            }
                            return null;
                        }
                        ReadEntireStream(stream2, arrBytes);
                        stream2.Close();
                        AbstractFile file2 = folder.GetFile(file.Name.Replace("_c.txt", "_s.txt"));
                        if (!file2.Exists)
                        {
                            if (bVerboseDialogs)
                            {
                                FiddlerApplication.DoNotifyUser("Could not find a server response. Missing file:\n" + file2.FullName, "Archive Incomplete", MessageBoxIcon.Hand);
                            }
                        }
                        else
                        {
                            byte[] buffer2 = new byte[file2.Size];
                            stream2 = file2.OpenRead();
                            ReadEntireStream(stream2, buffer2);
                            stream2.Close();
                            file2 = folder.GetFile(file.Name.Replace("_c.txt", "_m.xml"));
                            Session item = new Session(arrBytes, buffer2);
                            if (file2.Exists)
                            {
                                item.LoadMetadata(file2.OpenRead());
                            }
                            item.oFlags["x-LoadedFrom"] = file.Name.Replace("_c.txt", "_s.txt");
                            list.Add(item);
                        }
                    }
                    catch (Exception exception)
                    {
                        if (bVerboseDialogs)
                        {
                            FiddlerApplication.DoNotifyUser("Invalid data was present in session: " + file.FullName + ".\n\n\n" + exception.Message + "\n" + exception.StackTrace, "Archive Incomplete", MessageBoxIcon.Hand);
                        }
                    }
                }
            }
            catch (Exception exception2)
            {
                FiddlerApplication.ReportException(exception2);
                return null;
            }
            if (archive != null)
            {
                archive.EndUpdate();
                archive = null;
            }
            return list.ToArray();
        }

        internal static string RegExEscape(string sString, bool bAddPrefixCaret, bool bAddSuffixDollarSign)
        {
            StringBuilder builder = new StringBuilder();
            if (bAddPrefixCaret)
            {
                builder.Append("^");
            }
            foreach (char ch in sString)
            {
                switch (ch)
                {
                    case '#':
                    case '$':
                    case '(':
                    case ')':
                    case '+':
                    case '.':
                    case '[':
                    case '\\':
                    case '^':
                    case '?':
                    case '{':
                    case '|':
                        builder.Append('\\');
                        break;

                    default:
                        if (ch == '*')
                        {
                            builder.Append('.');
                        }
                        break;
                }
                builder.Append(ch);
            }
            if (bAddSuffixDollarSign)
            {
                builder.Append('$');
            }
            return builder.ToString();
        }

        [return: MarshalAs(UnmanagedType.Bool)]
        [DllImport("user32.dll")]
        internal static extern bool RegisterHotKey(IntPtr hWnd, int id, int fsModifiers, int vlc);
        public static bool RunExecutable(string sExecute, string sParams)
        {
            try
            {
                using (Process.Start(sExecute, sParams))
                {
                }
                return true;
            }
            catch (Exception exception)
            {
                FiddlerApplication.DoNotifyUser(string.Format("Failed to execute: {0}\nwith parameters: {1}\r\n\r\n{2}\r\n{3}", new object[] { sExecute, sParams, exception.Message, exception.StackTrace.ToString() }), "ShellExecute Failed");
            }
            return false;
        }

        [CodeDescription("Run an executable and wait for it to exit.")]
        public static bool RunExecutableAndWait(string sExecute, string sParams)
        {
            try
            {
                Process process = new Process {
                    StartInfo = { CreateNoWindow = true, FileName = sExecute, Arguments = sParams }
                };
                process.Start();
                process.WaitForExit();
                process.Dispose();
                return true;
            }
            catch (Exception exception)
            {
                FiddlerApplication.DoNotifyUser("Fiddler Exception thrown: " + exception.ToString() + "\r\n" + exception.StackTrace.ToString(), "ShellExecute Failed");
                return false;
            }
        }

        [DllImport("user32.dll", EntryPoint="SendMessage")]
        private static extern IntPtr SendCueTextMessage(IntPtr hWnd, int msg, IntPtr wParam, [MarshalAs(UnmanagedType.LPWStr)] string lParam);
        [DllImport("user32.dll", EntryPoint="SendMessage")]
        internal static extern IntPtr SendLVMessage(IntPtr hWnd, uint msg, IntPtr wParam, ref LV_COLUMN lParam);
        [DllImport("user32.dll")]
        internal static extern IntPtr SendMessage(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);
        [DllImport("user32.dll", EntryPoint="SendMessage")]
        internal static extern IntPtr SendWMCopyMessage(IntPtr hWnd, int Msg, IntPtr wParam, ref SendDataStruct lParam);
        public static void SetCueText(Control control, string text)
        {
            IntPtr hwndItem;
            if (control is ComboBox)
            {
                hwndItem = GetComboBoxInfo(control).hwndItem;
            }
            else
            {
                hwndItem = control.Handle;
            }
            SendCueTextMessage(hwndItem, 0x1501, IntPtr.Zero, text);
        }

        [return: MarshalAs(UnmanagedType.Bool)]
        [DllImport("user32.dll")]
        internal static extern bool SetForegroundWindow(IntPtr hWnd);
        [CodeDescription("Save a string to the registry. Correctly handles null Value, saving as String.Empty.")]
        public static void SetRegistryString(RegistryKey oReg, string sName, string sValue)
        {
            if (sName != null)
            {
                if (sValue == null)
                {
                    sValue = string.Empty;
                }
                oReg.SetValue(sName, sValue);
            }
        }

        [return: MarshalAs(UnmanagedType.Bool)]
        [DllImport("user32.dll")]
        internal static extern bool ShowWindowAsync(IntPtr hWnd, int nCmdShow);
        internal static string StringToCF_HTML(string inStr)
        {
            string str = "<HTML><HEAD><STYLE>.REQUEST { font: 8pt Courier New; color: blue;} .RESPONSE { font: 8pt Courier New; color: green;}</STYLE></HEAD><BODY>" + inStr + "</BODY></HTML>";
            string format = "Version:1.0\r\nStartHTML:{0:00000000}\r\nEndHTML:{1:00000000}\r\nStartFragment:{0:00000000}\r\nEndFragment:{1:00000000}\r\n";
            return (string.Format(format, format.Length - 0x10, (str.Length + format.Length) - 0x10) + str);
        }

        [CodeDescription("Returns the part of a string up to (but NOT including) the first instance of specified delimiter. If delim not found, returns entire string.")]
        public static string TrimAfter(string sString, char chDelim)
        {
            if (sString == null)
            {
                return string.Empty;
            }
            int index = sString.IndexOf(chDelim);
            if (index < 0)
            {
                return sString;
            }
            return sString.Substring(0, index);
        }

        [CodeDescription("Returns sString truncated to iMaxLen.")]
        public static string TrimAfter(string sString, int iMaxLen)
        {
            if (sString == null)
            {
                return string.Empty;
            }
            if (iMaxLen >= sString.Length)
            {
                return sString;
            }
            return sString.Remove(iMaxLen + 1);
        }

        [CodeDescription("Returns the part of a string up to (but NOT including) the first instance of specified substring. If delim not found, returns entire string.")]
        public static string TrimAfter(string sString, string sDelim)
        {
            if (sString == null)
            {
                return string.Empty;
            }
            if (sDelim == null)
            {
                return sString;
            }
            int index = sString.IndexOf(sDelim);
            if (index < 0)
            {
                return sString;
            }
            return sString.Substring(0, index);
        }

        [CodeDescription("Returns the part of a string after (but NOT including) the first instance of specified delimiter. If delim not found, returns entire string.")]
        public static string TrimBefore(string sString, char chDelim)
        {
            if (sString == null)
            {
                return string.Empty;
            }
            int index = sString.IndexOf(chDelim);
            if (index < 0)
            {
                return sString;
            }
            return sString.Substring(index + 1);
        }

        [CodeDescription("Returns the part of a string after (but NOT including) the first instance of specified substring. If delim not found, returns entire string.")]
        public static string TrimBefore(string sString, string sDelim)
        {
            if (sString == null)
            {
                return string.Empty;
            }
            if (sDelim == null)
            {
                return sString;
            }
            int index = sString.IndexOf(sDelim);
            if (index < 0)
            {
                return sString;
            }
            return sString.Substring(index + sDelim.Length);
        }

        [CodeDescription("Returns the part of a string after (but not including) the last instance of specified delimiter. If delim not found, returns entire string.")]
        public static string TrimBeforeLast(string sString, char chDelim)
        {
            if (sString == null)
            {
                return string.Empty;
            }
            int num = sString.LastIndexOf(chDelim);
            if (num < 0)
            {
                return sString;
            }
            return sString.Substring(num + 1);
        }

        [CodeDescription("Returns the part of a string after (but not including) the last instance of specified substring. If delim not found, returns entire string.")]
        public static string TrimBeforeLast(string sString, string sDelim)
        {
            if (sString == null)
            {
                return string.Empty;
            }
            if (sDelim == null)
            {
                return sString;
            }
            int num = sString.LastIndexOf(sDelim);
            if (num < 0)
            {
                return sString;
            }
            return sString.Substring(num + sDelim.Length);
        }

        [CodeDescription("Returns the part of a string after (and including) the first instance of specified substring. If delim not found, returns entire string.")]
        public static string TrimUpTo(string sString, string sDelim)
        {
            if (sString == null)
            {
                return string.Empty;
            }
            if (sDelim == null)
            {
                return sString;
            }
            int index = sString.IndexOf(sDelim);
            if (index < 0)
            {
                return sString;
            }
            return sString.Substring(index);
        }

        [CodeDescription("Try parsing the string for a Hex-formatted int. If it fails, return false and 0 in iOutput.")]
        public static bool TryHexParse(string sInput, out int iOutput)
        {
            return int.TryParse(sInput, NumberStyles.HexNumber, NumberFormatInfo.InvariantInfo, out iOutput);
        }

        [return: MarshalAs(UnmanagedType.Bool)]
        [DllImport("user32.dll")]
        internal static extern bool UnregisterHotKey(IntPtr hWnd, int id);
        public static void utilDecodeHTTPBody(HTTPHeaders oHeaders, ref byte[] arrBody)
        {
            if ((arrBody != null) && (arrBody.LongLength > 0L))
            {
                if (oHeaders.ExistsAndContains("Transfer-Encoding", "chunked"))
                {
                    arrBody = doUnchunk(arrBody);
                }
                if (oHeaders.ExistsAndContains("Transfer-Encoding", "gzip") || oHeaders.ExistsAndContains("Content-Encoding", "gzip"))
                {
                    arrBody = GzipExpand(arrBody);
                }
                if (oHeaders.ExistsAndContains("Transfer-Encoding", "deflate") || oHeaders.ExistsAndContains("Content-Encoding", "deflate"))
                {
                    arrBody = DeflaterExpand(arrBody);
                }
                if (oHeaders.ExistsAndContains("Transfer-Encoding", "bzip2") || oHeaders.ExistsAndContains("Content-Encoding", "bzip2"))
                {
                    arrBody = bzip2Expand(arrBody);
                }
            }
        }

        [CodeDescription("Writes arrBytes to a file, overwriting if needed.")]
        public static void WriteArrayToFile(string sFilename, byte[] arrBytes)
        {
            EnsureOverwritable(sFilename);
            FileStream stream = new FileStream(sFilename, FileMode.Create, FileAccess.Write);
            if (arrBytes != null)
            {
                stream.Write(arrBytes, 0, arrBytes.Length);
            }
            stream.Close();
        }

        [CodeDescription("Save the specified .SAZ session archive")]
        public static bool WriteSessionArchive(string sFilename, Session[] arrSessions, string sPassword, bool bVerboseDialogs)
        {
            if ((arrSessions == null) || (arrSessions.Length < 1))
            {
                if (bVerboseDialogs)
                {
                    FiddlerApplication.DoNotifyUser("No sessions were provided to save to the archive.", "WriteSessionArchive - No Input");
                }
                return false;
            }
            try
            {
                if (System.IO.File.Exists(sFilename))
                {
                    System.IO.File.Delete(sFilename);
                }
                DiskFile zipFile = new DiskFile(sFilename);
                ZipArchive archive = new ZipArchive(zipFile) {
                    TempFolder = new MemoryFolder()
                };
                archive.BeginUpdate();
                ZippedFolder folder1 = (ZippedFolder) archive.CreateFolder("raw");
                if (!string.IsNullOrEmpty(sPassword))
                {
                    if (CONFIG.bUseAESForSAZ)
                    {
                        archive.DefaultEncryptionMethod = EncryptionMethod.WinZipAes;
                    }
                    archive.DefaultEncryptionPassword = sPassword;
                }
                archive.Comment = "Fiddler (v" + Application.ProductVersion + ") Session Archive. See http://www.fiddler2.com";
                int iFileNumber = 1;
                foreach (Session session in arrSessions)
                {
                    WriteSessionToSAZ(session, zipFile, iFileNumber, null, bVerboseDialogs);
                    iFileNumber++;
                }
                archive.EndUpdate();
                return true;
            }
            catch (Exception exception)
            {
                if (bVerboseDialogs)
                {
                    FiddlerApplication.DoNotifyUser("Failed to save Session Archive.\n\n" + exception.Message, "Save Failed");
                }
                return false;
            }
        }

        internal static void WriteSessionToSAZ(Session oSession, DiskFile odfZip, int iFileNumber, StringBuilder sbHTML, bool bVerboseDialogs)
        {
            string str = @"raw\" + iFileNumber.ToString("0000");
            string fileName = str + "_c.txt";
            string str3 = str + "_s.txt";
            string str4 = str + "_m.xml";
            try
            {
                Stream oFS = new ZippedFile(odfZip, fileName).CreateWrite(FileShare.None);
                oSession.WriteRequestToStream(false, true, oFS);
                oFS.Close();
            }
            catch (Exception exception)
            {
                if (bVerboseDialogs)
                {
                    FiddlerApplication.DoNotifyUser("Unable to add " + fileName + "\n\n" + exception.Message, "Archive Failure");
                }
            }
            try
            {
                Stream stream2 = new ZippedFile(odfZip, str3).CreateWrite(FileShare.None);
                oSession.WriteResponseToStream(stream2, false);
                stream2.Close();
            }
            catch (Exception exception2)
            {
                if (bVerboseDialogs)
                {
                    FiddlerApplication.DoNotifyUser("Unable to add " + str3 + "\n\n" + exception2.Message, "Archive Failure");
                }
            }
            try
            {
                Stream strmMetadata = new ZippedFile(odfZip, str4).CreateWrite(FileShare.None);
                oSession.WriteMetadataToStream(strmMetadata);
                strmMetadata.Close();
            }
            catch (Exception exception3)
            {
                if (bVerboseDialogs)
                {
                    FiddlerApplication.DoNotifyUser("Unable to add " + str4 + "\n\n" + exception3.Message, "Archive Failure");
                }
            }
            if (sbHTML != null)
            {
                sbHTML.Append("<tr>");
                sbHTML.Append("<TD><a href='" + fileName + "'>C</a>&nbsp;");
                sbHTML.Append("<a href='" + str3 + "'>S</a>&nbsp;");
                sbHTML.Append("<a href='" + str4 + "'>M</a></TD>");
                ListViewItem viewItem = oSession.ViewItem;
                if (viewItem != null)
                {
                    ListViewItem.ListViewSubItemCollection subItems = viewItem.SubItems;
                    if (subItems != null)
                    {
                        foreach (ListViewItem.ListViewSubItem item2 in subItems)
                        {
                            sbHTML.AppendFormat("<td>{0}</td>", HtmlEncode(item2.Text));
                        }
                    }
                }
                sbHTML.Append("</tr>");
            }
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct COMBOBOXINFO
        {
            public int cbSize;
            public FiddlerToolbar.RECT rcItem;
            public FiddlerToolbar.RECT rcButton;
            public int stateButton;
            public IntPtr hwndCombo;
            public IntPtr hwndItem;
            public IntPtr hwndList;
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct COPYDATASTRUCT
        {
            public IntPtr dwData;
            public int cbData;
            public IntPtr lpData;
        }

        [Flags]
        private enum FlashWInfo
        {
            FLASHW_ALL = 3,
            FLASHW_CAPTION = 1,
            FLASHW_STOP = 0,
            FLASHW_TIMER = 4,
            FLASHW_TIMERNOFG = 12,
            FLASHW_TRAY = 2
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct FLASHWINFO
        {
            public int cbSize;
            public IntPtr hwnd;
            public Utilities.FlashWInfo dwFlags;
            public int uCount;
            public int dwTimeout;
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct LV_COLUMN
        {
            public uint mask;
            public int fmt;
            public int cx;
            public string pszText;
            public int cchTextMax;
            public int iSubItem;
            public int iImage;
            public int iOrder;
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct SendDataStruct
        {
            public IntPtr dwData;
            public int cbData;
            public string strData;
        }

        [Flags]
        internal enum SoundFlags
        {
            SND_ALIAS = 0x10000,
            SND_ALIAS_ID = 0x110000,
            SND_ASYNC = 1,
            SND_FILENAME = 0x20000,
            SND_LOOP = 8,
            SND_MEMORY = 4,
            SND_NODEFAULT = 2,
            SND_NOSTOP = 0x10,
            SND_NOWAIT = 0x2000,
            SND_RESOURCE = 0x40004,
            SND_SYNC = 0
        }
    }
}

