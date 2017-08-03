namespace Fiddler
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Net;
    using System.Net.Sockets;
    using System.Text;

    internal class DNSResolver
    {
        private static readonly Dictionary<string, DNSCacheEntry> dictAddresses = new Dictionary<string, DNSCacheEntry>();
        private static int MSEC_DNS_CACHE_LIFETIME = FiddlerApplication.Prefs.GetInt32Pref("fiddler.network.timeouts.dnscache", 0x249f0);

        static DNSResolver()
        {
            FiddlerApplication.Janitor.assignWork(new SimpleEventHandler(DNSResolver.ScavengeCache), 0x7530);
        }

        internal static void ClearCache()
        {
            lock (dictAddresses)
            {
                dictAddresses.Clear();
            }
        }

        public static IPAddress GetIPAddress(string sRemoteHost, bool bCheckCache)
        {
            return GetIPAddressList(sRemoteHost, bCheckCache, null)[0];
        }

        public static IPAddress[] GetIPAddressList(string sRemoteHost, bool bCheckCache, SessionTimers oTimers)
        {
            IPAddress[] arrResult = null;
            DNSCacheEntry entry;
            Stopwatch stopwatch = Stopwatch.StartNew();
            IPAddress address = Utilities.IPFromString(sRemoteHost);
            if (address != null)
            {
                arrResult = new IPAddress[] { address };
                if (oTimers != null)
                {
                    oTimers.DNSTime = (int) stopwatch.ElapsedMilliseconds;
                }
                return arrResult;
            }
            if (bCheckCache && dictAddresses.TryGetValue(sRemoteHost, out entry))
            {
                if (entry.iLastLookup > (Environment.TickCount - MSEC_DNS_CACHE_LIFETIME))
                {
                    arrResult = entry.arrAddressList;
                }
                else
                {
                    lock (dictAddresses)
                    {
                        dictAddresses.Remove(sRemoteHost);
                    }
                }
            }
            if (arrResult == null)
            {
                try
                {
                    arrResult = Dns.GetHostAddresses(sRemoteHost);
                }
                catch
                {
                    if (oTimers != null)
                    {
                        oTimers.DNSTime = (int) stopwatch.ElapsedMilliseconds;
                    }
                    throw;
                }
                arrResult = trimAddressList(arrResult);
                if (arrResult.Length < 1)
                {
                    throw new Exception("No valid IPv4 addresses were found for this host.");
                }
                if (arrResult.Length > 0)
                {
                    lock (dictAddresses)
                    {
                        if (!dictAddresses.ContainsKey(sRemoteHost))
                        {
                            dictAddresses.Add(sRemoteHost, new DNSCacheEntry(arrResult));
                        }
                    }
                }
            }
            if (oTimers != null)
            {
                oTimers.DNSTime = (int) stopwatch.ElapsedMilliseconds;
            }
            return arrResult;
        }

        public static string InspectCache()
        {
            StringBuilder builder = new StringBuilder(0x2000);
            builder.AppendFormat("DNSResolver Cache\nfiddler.network.timeouts.dnscache: {0}ms\nContents\n--------\n", MSEC_DNS_CACHE_LIFETIME);
            new List<string>();
            lock (dictAddresses)
            {
                foreach (KeyValuePair<string, DNSCacheEntry> pair in dictAddresses)
                {
                    StringBuilder builder2 = new StringBuilder();
                    builder2.Append(" [");
                    foreach (IPAddress address in pair.Value.arrAddressList)
                    {
                        builder2.Append(address.ToString());
                        builder2.Append(", ");
                    }
                    builder2.Remove(builder2.Length - 2, 2);
                    builder2.Append("]");
                    builder.AppendFormat("\tHostName: {0}, Age: {1}ms, AddressList:{2}\n", pair.Key, Environment.TickCount - pair.Value.iLastLookup, builder2.ToString());
                }
            }
            builder.Append("--------\n");
            return builder.ToString();
        }

        public static void ScavengeCache()
        {
            if (dictAddresses.Count >= 1)
            {
                List<string> list = new List<string>();
                lock (dictAddresses)
                {
                    foreach (KeyValuePair<string, DNSCacheEntry> pair in dictAddresses)
                    {
                        if (pair.Value.iLastLookup < (Environment.TickCount - MSEC_DNS_CACHE_LIFETIME))
                        {
                            list.Add(pair.Key);
                        }
                    }
                    foreach (string str in list)
                    {
                        dictAddresses.Remove(str);
                    }
                }
            }
        }

        private static IPAddress[] trimAddressList(IPAddress[] arrResult)
        {
            List<IPAddress> list = new List<IPAddress>();
            for (int i = 0; i < arrResult.Length; i++)
            {
                if (!list.Contains(arrResult[i]) && (CONFIG.bEnableIPv6 || (arrResult[i].AddressFamily == AddressFamily.InterNetwork)))
                {
                    list.Add(arrResult[i]);
                }
                if (list.Count == 5)
                {
                    break;
                }
            }
            return list.ToArray();
        }

        private class DNSCacheEntry
        {
            public IPAddress[] arrAddressList;
            public int iLastLookup = Environment.TickCount;

            public DNSCacheEntry(IPAddress[] arrIPs)
            {
                this.arrAddressList = arrIPs;
            }
        }
    }
}

