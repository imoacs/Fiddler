namespace Fiddler
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Runtime.InteropServices;

    internal static class ProcessHelper
    {
        private static readonly Dictionary<int, ProcessNameCacheEntry> dictProcessNames = new Dictionary<int, ProcessNameCacheEntry>();
        private const int MSEC_PROCESSNAME_CACHE_LIFETIME = 0x7530;

        static ProcessHelper()
        {
            FiddlerApplication.Janitor.assignWork(new SimpleEventHandler(ProcessHelper.ScavengeCache), 0xea60);
        }

        internal static string GetProcessName(int iPID)
        {
            try
            {
                ProcessNameCacheEntry entry;
                if (dictProcessNames.TryGetValue(iPID, out entry))
                {
                    if (entry.iLastLookup > (Environment.TickCount - 0x7530))
                    {
                        return entry.sProcessName;
                    }
                    lock (dictProcessNames)
                    {
                        dictProcessNames.Remove(iPID);
                    }
                }
                string str = Process.GetProcessById(iPID).ProcessName.ToLower();
                if (string.IsNullOrEmpty(str))
                {
                    return string.Empty;
                }
                lock (dictProcessNames)
                {
                    if (!dictProcessNames.ContainsKey(iPID))
                    {
                        dictProcessNames.Add(iPID, new ProcessNameCacheEntry(str));
                    }
                }
                return str;
            }
            catch (Exception)
            {
                return string.Empty;
            }
        }

        internal static void ScavengeCache()
        {
            lock (dictProcessNames)
            {
                List<int> list = new List<int>();
                foreach (KeyValuePair<int, ProcessNameCacheEntry> pair in dictProcessNames)
                {
                    if (pair.Value.iLastLookup < (Environment.TickCount - 0x7530))
                    {
                        list.Add(pair.Key);
                    }
                }
                foreach (int num in list)
                {
                    dictProcessNames.Remove(num);
                }
            }
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct ProcessNameCacheEntry
        {
            public readonly int iLastLookup;
            public readonly string sProcessName;
            public ProcessNameCacheEntry(string _sProcessName)
            {
                this.iLastLookup = Environment.TickCount;
                this.sProcessName = _sProcessName;
            }
        }
    }
}

