namespace Fiddler
{
    using System;
    using System.Collections.Generic;
    using System.Runtime.InteropServices;
    using System.Text;

    public class BasicAnalysis
    {
        [CodeDescription("Get a multi-line string describing the provided sessions.")]
        public static string ComputeBasicStatistics(Session[] _arrSessions, bool bTimeEstimates)
        {
            Dictionary<string, long> dictionary;
            long num;
            return ComputeBasicStatistics(_arrSessions, bTimeEstimates, out dictionary, out num);
        }

        public static string ComputeBasicStatistics(Session[] _arrSessions, bool bEstimates, out Dictionary<string, long> dictResponseSizeByContentType, out long cBytesRecv)
        {
            long num = 0L;
            long num2 = 0L;
            long num3 = 0L;
            long num4 = 0L;
            dictResponseSizeByContentType = new Dictionary<string, long>();
            int length = _arrSessions.Length;
            int num6 = 0;
            int num7 = 0;
            int num8 = 0;
            DateTime time = new DateTime();
            StringBuilder builder = new StringBuilder();
            Dictionary<int, int> collection = new Dictionary<int, int>();
            DateTime maxValue = DateTime.MaxValue;
            DateTime minValue = DateTime.MinValue;
            int num9 = 0;
            cBytesRecv = 0L;
            builder.AppendFormat("Request Count: \t{0:N0}\r\n", _arrSessions.Length);
            foreach (Session session in _arrSessions)
            {
                num6 += session.Timers.DNSTime;
                num7 += session.Timers.TCPConnectTime;
                num8 += session.Timers.HTTPSHandshakeTime;
                if ((session.Timers.ClientBeginRequest.Ticks > 0L) && (session.Timers.ClientBeginRequest < maxValue))
                {
                    maxValue = session.Timers.ClientBeginRequest;
                }
                if ((session.Timers.ClientDoneResponse.Ticks > 0L) && (session.Timers.ClientDoneResponse > minValue))
                {
                    minValue = session.Timers.ClientDoneResponse;
                }
                if ((session.Timers.ClientBeginRequest.Ticks > 0L) && (session.Timers.ClientDoneResponse.Ticks > 0L))
                {
                    time += session.Timers.ClientDoneResponse - session.Timers.ClientBeginRequest;
                }
                if (((session.oResponse != null) && (session.oResponse.headers != null)) && (session.responseBodyBytes != null))
                {
                    string key = Utilities.TrimAfter(session.oResponse["Content-Type"], ';').ToLower();
                    if (key.Length < 1)
                    {
                        key = "?";
                    }
                    num9 = Math.Max(num9, key.Length);
                    long longLength = session.responseBodyBytes.LongLength;
                    if (longLength > 0L)
                    {
                        if (!dictResponseSizeByContentType.ContainsKey(key))
                        {
                            dictResponseSizeByContentType.Add(key, longLength);
                        }
                        else
                        {
                            dictResponseSizeByContentType[key] += longLength;
                        }
                    }
                }
                if (!collection.ContainsKey(session.responseCode))
                {
                    collection.Add(session.responseCode, 1);
                }
                else
                {
                    collection[session.responseCode] += 1;
                }
                if (session.requestBodyBytes != null)
                {
                    num2 += session.requestBodyBytes.LongLength;
                }
                if ((session.oRequest != null) && (session.oRequest.headers != null))
                {
                    num += 2 + session.oRequest.headers.ByteCount();
                }
                if (session.responseBodyBytes != null)
                {
                    num4 += session.responseBodyBytes.Length;
                }
                if ((session.oResponse != null) && (session.oResponse.headers != null))
                {
                    num3 += 2 + session.oResponse.headers.ByteCount();
                }
            }
            long num11 = num + num2;
            builder.AppendFormat("Bytes Sent: \t{0:N0}\t(headers:{1}; body:{2})\r\n", num11, num, num2);
            cBytesRecv = num3 + num4;
            builder.AppendFormat("Bytes Received: {0:N0}\t(headers:{1}; body:{2})\r\n", (long) cBytesRecv, num3, num4);
            builder.Append("\r\nACTUAL PERFORMANCE\r\n--------------\r\n");
            if (_arrSessions.Length == 1)
            {
                builder.Append(_arrSessions[0].Timers.ToString(true));
            }
            else
            {
                TimeSpan span = (TimeSpan) (minValue - maxValue);
                if (span.Ticks > 0L)
                {
                    builder.AppendFormat("Requests started at:\t{0:HH:mm:ss.fff}\r\nResponses completed at:\t{1:HH:mm:ss.fff}\r\nAggregate Session time:\t{2:HH:mm:ss.fff}\r\nSequence (clock) time:\t{3:hh\\:mm\\:ss\\.fff}\r\n", new object[] { maxValue, minValue, time, (TimeSpan) (minValue - maxValue) });
                    if (num6 > 0)
                    {
                        builder.AppendFormat("DNS Lookup time:\t{0:N0}ms\r\n", num6);
                    }
                    if (num7 > 0)
                    {
                        builder.AppendFormat("TCP/IP Connect time:\t{0:N0}ms\r\n", num7);
                    }
                    if (num8 > 0)
                    {
                        builder.AppendFormat("HTTPS Handshake time\t{0:N0}ms\r\n", num8);
                    }
                }
            }
            builder.AppendFormat("\r\nRESPONSE CODES\r\n--------------\r\n", new object[0]);
            List<KeyValuePair<int, int>> list = new List<KeyValuePair<int, int>>(collection);
            list.Sort((Comparison<KeyValuePair<int, int>>) ((firstPair, nextPair) => -firstPair.Value.CompareTo(nextPair.Value)));
            foreach (KeyValuePair<int, int> pair in list)
            {
                builder.AppendFormat("HTTP/{0,3:N0}: \t{1:N0}\r\n", pair.Key, pair.Value);
            }
            dictResponseSizeByContentType.Add("~headers~", num3);
            builder.AppendFormat("\r\nRESPONSE BYTES (by Content-Type)\r\n--------------\r\n", new object[0]);
            List<KeyValuePair<string, long>> list2 = new List<KeyValuePair<string, long>>(dictResponseSizeByContentType);
            list2.Sort((Comparison<KeyValuePair<string, long>>) ((firstPair, nextPair) => -firstPair.Value.CompareTo(nextPair.Value)));
            foreach (KeyValuePair<string, long> pair2 in list2)
            {
                builder.AppendFormat("{0," + num9.ToString() + "}:\t{1:N0}\r\n", pair2.Key, pair2.Value);
            }
            if (bEstimates)
            {
                builder.Append("\r\nESTIMATED WORLDWIDE PERFORMANCE\r\n--------------\r\n");
                builder.Append("The following are VERY rough estimates of download times when hitting servers based in WA, USA.\r\n\r\n");
                builder.Append("\r\nUS West Coast (Modem - 6KB/sec)\r\n");
                builder.Append("---------------\r\n");
                builder.AppendFormat("Round trip cost: {0:N2}s\r\n", length * 0.1);
                builder.AppendFormat("Elapsed Time:\t {0:N2}s\r\n\r\n", (length * 0.1) + ((num11 + cBytesRecv) / 0x1770L));
                builder.Append("\r\nJapan / Northern Europe (Modem)\r\n");
                builder.Append("---------------\r\n");
                builder.AppendFormat("Round trip cost: {0:N2}s\r\n", length * 0.15);
                builder.AppendFormat("Elapsed Time:\t {0:N2}s\r\n\r\n", (length * 0.15) + ((num11 + cBytesRecv) / 0x1770L));
                builder.Append("\r\nChina (Modem)\r\n");
                builder.Append("---------------\r\n");
                builder.Append(string.Format("Round trip cost: {0:N2}s\r\n", length * 0.45));
                builder.AppendFormat("Elapsed Time:\t {0:N2}s\r\n\r\n", (length * 0.45) + ((num11 + cBytesRecv) / 0x1770L));
                builder.Append("\r\nUS West Coast (DSL - 30KB/sec)\r\n");
                builder.Append("---------------\r\n");
                builder.AppendFormat("Round trip cost: {0:N2}s\r\n", length * 0.1);
                builder.AppendFormat("Elapsed Time:\t {0:N2}s\r\n\r\n", (length * 0.1) + ((num11 + cBytesRecv) / 0x7530L));
                builder.Append("\r\nJapan / Northern Europe (DSL)\r\n");
                builder.Append("---------------\r\n");
                builder.AppendFormat("Round trip cost: {0:N2}s\r\n", length * 0.15);
                builder.AppendFormat("Elapsed Time:\t {0:N2}s\r\n\r\n", (length * 0.15) + ((num11 + cBytesRecv) / 0x7530L));
                builder.Append("\r\nChina (DSL)\r\n");
                builder.Append("---------------\r\n");
                builder.AppendFormat("Round trip cost: {0:N2}s\r\n", length * 0.45);
                builder.AppendFormat("Elapsed Time:\t {0:N2}s\r\n\r\n", (length * 0.45) + ((num11 + cBytesRecv) / 0x7530L));
            }
            return builder.ToString();
        }
    }
}

