namespace Fiddler
{
    using System;
    using System.Diagnostics;
    using System.Runtime.InteropServices;
    using System.Runtime.InteropServices.ComTypes;

    public class WinINETCache
    {
        private const int CACHEGROUP_FLAG_FLUSHURL_ONDELETE = 2;
        private const int CACHEGROUP_SEARCH_ALL = 0;
        private const int ERROR_FILE_NOT_FOUND = 2;
        private const int ERROR_INSUFFICENT_BUFFER = 0x7a;
        private const int ERROR_NO_MORE_ITEMS = 0x103;

        public static void ClearCacheItems(bool bClearFiles, bool bClearCookies)
        {
            if (!bClearCookies && !bClearFiles)
            {
                throw new ArgumentException("You must call ClearCacheItems with at least one target");
            }
            if (Environment.OSVersion.Version.Major > 5)
            {
                VistaClearTracks(bClearFiles, bClearCookies);
            }
            else
            {
                if (bClearCookies)
                {
                    ClearCookiesForHost("*");
                }
                if (bClearFiles)
                {
                    long lpGroupId = 0L;
                    int lpdwFirstCacheEntryInfoBufferSize = 0;
                    int cb = 0;
                    IntPtr zero = IntPtr.Zero;
                    IntPtr hFind = IntPtr.Zero;
                    bool flag = false;
                    hFind = FindFirstUrlCacheGroup(0, 0, IntPtr.Zero, 0, ref lpGroupId, IntPtr.Zero);
                    int num4 = Marshal.GetLastWin32Error();
                    if (((hFind != IntPtr.Zero) && (0x103 != num4)) && (2 != num4))
                    {
                        do
                        {
                            flag = DeleteUrlCacheGroup(lpGroupId, 2, IntPtr.Zero);
                            num4 = Marshal.GetLastWin32Error();
                            if (!flag && (2 == num4))
                            {
                                flag = FindNextUrlCacheGroup(hFind, ref lpGroupId, IntPtr.Zero);
                                num4 = Marshal.GetLastWin32Error();
                            }
                        }
                        while (flag || ((0x103 != num4) && (2 != num4)));
                    }
                    hFind = FindFirstUrlCacheEntryEx(null, 0, WININETCACHEENTRYTYPE.ALL, 0L, IntPtr.Zero, ref lpdwFirstCacheEntryInfoBufferSize, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero);
                    num4 = Marshal.GetLastWin32Error();
                    if ((IntPtr.Zero != hFind) || (0x103 != num4))
                    {
                        cb = lpdwFirstCacheEntryInfoBufferSize;
                        zero = Marshal.AllocHGlobal(cb);
                        hFind = FindFirstUrlCacheEntryEx(null, 0, WININETCACHEENTRYTYPE.ALL, 0L, zero, ref lpdwFirstCacheEntryInfoBufferSize, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero);
                        num4 = Marshal.GetLastWin32Error();
                        do
                        {
                            INTERNET_CACHE_ENTRY_INFOA internet_cache_entry_infoa = (INTERNET_CACHE_ENTRY_INFOA) Marshal.PtrToStructure(zero, typeof(INTERNET_CACHE_ENTRY_INFOA));
                            lpdwFirstCacheEntryInfoBufferSize = cb;
                            if (WININETCACHEENTRYTYPE.COOKIE_CACHE_ENTRY != (internet_cache_entry_infoa.CacheEntryType & WININETCACHEENTRYTYPE.COOKIE_CACHE_ENTRY))
                            {
                                flag = DeleteUrlCacheEntry(internet_cache_entry_infoa.lpszSourceUrlName);
                            }
                            flag = FindNextUrlCacheEntryEx(hFind, zero, ref lpdwFirstCacheEntryInfoBufferSize, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero);
                            num4 = Marshal.GetLastWin32Error();
                            if (!flag && (0x103 == num4))
                            {
                                break;
                            }
                            if (!flag && (lpdwFirstCacheEntryInfoBufferSize > cb))
                            {
                                cb = lpdwFirstCacheEntryInfoBufferSize;
                                zero = Marshal.ReAllocHGlobal(zero, (IntPtr) cb);
                                flag = FindNextUrlCacheEntryEx(hFind, zero, ref lpdwFirstCacheEntryInfoBufferSize, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero);
                            }
                        }
                        while (flag);
                        Marshal.FreeHGlobal(zero);
                    }
                }
            }
        }

        public static void ClearCookies()
        {
            ClearCacheItems(false, true);
        }

        [CodeDescription("Delete all permanent WinINET cookies for sHost; won't clear memory-only session cookies. Supports hostnames with an optional leading wildcard, e.g. *example.com. NOTE: Will not work on VistaIE Protected Mode cookies.")]
        public static void ClearCookiesForHost(string sHost)
        {
            string str;
            INTERNET_CACHE_ENTRY_INFOA internet_cache_entry_infoa;
            bool flag;
            sHost = sHost.Trim();
            if (sHost.Length < 1)
            {
                return;
            }
            if (sHost == "*")
            {
                str = string.Empty;
                if (Environment.OSVersion.Version.Major > 5)
                {
                    VistaClearTracks(false, true);
                    return;
                }
            }
            else
            {
                str = sHost.StartsWith("*") ? sHost.Substring(1).ToLower() : ("@" + sHost.ToLower());
            }
            int lpdwFirstCacheEntryInfoBufferSize = 0;
            int cb = 0;
            IntPtr zero = IntPtr.Zero;
            IntPtr hFind = IntPtr.Zero;
            if ((FindFirstUrlCacheEntry("cookie:", IntPtr.Zero, ref lpdwFirstCacheEntryInfoBufferSize) == IntPtr.Zero) && (0x103 == Marshal.GetLastWin32Error()))
            {
                return;
            }
            cb = lpdwFirstCacheEntryInfoBufferSize;
            zero = Marshal.AllocHGlobal(cb);
            hFind = FindFirstUrlCacheEntry("cookie:", zero, ref lpdwFirstCacheEntryInfoBufferSize);
        Label_00C2:
            internet_cache_entry_infoa = (INTERNET_CACHE_ENTRY_INFOA) Marshal.PtrToStructure(zero, typeof(INTERNET_CACHE_ENTRY_INFOA));
            lpdwFirstCacheEntryInfoBufferSize = cb;
            if (WININETCACHEENTRYTYPE.COOKIE_CACHE_ENTRY == (internet_cache_entry_infoa.CacheEntryType & WININETCACHEENTRYTYPE.COOKIE_CACHE_ENTRY))
            {
                bool flag2;
                if (str.Length == 0)
                {
                    flag2 = true;
                }
                else
                {
                    string str2 = Marshal.PtrToStringAnsi(internet_cache_entry_infoa.lpszSourceUrlName);
                    int index = str2.IndexOf('/');
                    if (index > 0)
                    {
                        str2 = str2.Remove(index);
                    }
                    flag2 = str2.ToLower().EndsWith(str);
                }
                if (flag2)
                {
                    flag = DeleteUrlCacheEntry(internet_cache_entry_infoa.lpszSourceUrlName);
                }
            }
        Label_014A:
            flag = FindNextUrlCacheEntry(hFind, zero, ref lpdwFirstCacheEntryInfoBufferSize);
            if (flag || (0x103 != Marshal.GetLastWin32Error()))
            {
                if (flag || (lpdwFirstCacheEntryInfoBufferSize <= cb))
                {
                    goto Label_00C2;
                }
                cb = lpdwFirstCacheEntryInfoBufferSize;
                zero = Marshal.ReAllocHGlobal(zero, (IntPtr) cb);
                goto Label_014A;
            }
            Marshal.FreeHGlobal(zero);
        }

        public static void ClearFiles()
        {
            ClearCacheItems(true, false);
        }

        [return: MarshalAs(UnmanagedType.Bool)]
        [DllImport("wininet.dll", EntryPoint="DeleteUrlCacheEntryA", CallingConvention=CallingConvention.StdCall, CharSet=CharSet.Ansi, SetLastError=true, ExactSpelling=true)]
        private static extern bool DeleteUrlCacheEntry(IntPtr lpszUrlName);
        [return: MarshalAs(UnmanagedType.Bool)]
        [DllImport("wininet.dll", CallingConvention=CallingConvention.StdCall, CharSet=CharSet.Unicode, SetLastError=true)]
        private static extern bool DeleteUrlCacheGroup(long GroupId, int dwFlags, IntPtr lpReserved);
        [DllImport("wininet.dll", EntryPoint="FindFirstUrlCacheEntryA", CallingConvention=CallingConvention.StdCall, CharSet=CharSet.Ansi, SetLastError=true, ExactSpelling=true)]
        private static extern IntPtr FindFirstUrlCacheEntry([MarshalAs(UnmanagedType.LPTStr)] string lpszUrlSearchPattern, IntPtr lpFirstCacheEntryInfo, ref int lpdwFirstCacheEntryInfoBufferSize);
        [DllImport("wininet.dll", EntryPoint="FindFirstUrlCacheEntryExA", CallingConvention=CallingConvention.StdCall, CharSet=CharSet.Ansi, SetLastError=true, ExactSpelling=true)]
        private static extern IntPtr FindFirstUrlCacheEntryEx([MarshalAs(UnmanagedType.LPTStr)] string lpszUrlSearchPattern, int dwFlags, WININETCACHEENTRYTYPE dwFilter, long GroupId, IntPtr lpFirstCacheEntryInfo, ref int lpdwFirstCacheEntryInfoBufferSize, IntPtr lpReserved, IntPtr pcbReserved2, IntPtr lpReserved3);
        [DllImport("wininet.dll", CallingConvention=CallingConvention.StdCall, CharSet=CharSet.Unicode, SetLastError=true)]
        private static extern IntPtr FindFirstUrlCacheGroup(int dwFlags, int dwFilter, IntPtr lpSearchCondition, int dwSearchCondition, ref long lpGroupId, IntPtr lpReserved);
        [return: MarshalAs(UnmanagedType.Bool)]
        [DllImport("wininet.dll", EntryPoint="FindNextUrlCacheEntryA", CallingConvention=CallingConvention.StdCall, CharSet=CharSet.Ansi, SetLastError=true, ExactSpelling=true)]
        private static extern bool FindNextUrlCacheEntry(IntPtr hFind, IntPtr lpNextCacheEntryInfo, ref int lpdwNextCacheEntryInfoBufferSize);
        [return: MarshalAs(UnmanagedType.Bool)]
        [DllImport("wininet.dll", EntryPoint="FindNextUrlCacheEntryExA", CallingConvention=CallingConvention.StdCall, CharSet=CharSet.Ansi, SetLastError=true, ExactSpelling=true)]
        private static extern bool FindNextUrlCacheEntryEx(IntPtr hEnumHandle, IntPtr lpNextCacheEntryInfo, ref int lpdwNextCacheEntryInfoBufferSize, IntPtr lpReserved, IntPtr pcbReserved2, IntPtr lpReserved3);
        [return: MarshalAs(UnmanagedType.Bool)]
        [DllImport("wininet.dll", CallingConvention=CallingConvention.StdCall, CharSet=CharSet.Unicode, SetLastError=true)]
        private static extern bool FindNextUrlCacheGroup(IntPtr hFind, ref long lpGroupId, IntPtr lpReserved);
        internal static string GetCacheItemInfo(string sURL)
        {
            int lpdwCacheEntryInfoBufferSize = 0;
            int cb = 0;
            IntPtr zero = IntPtr.Zero;
            bool flag = GetUrlCacheEntryInfoA(sURL, zero, ref lpdwCacheEntryInfoBufferSize);
            int num = Marshal.GetLastWin32Error();
            if (flag || (num != 0x7a))
            {
                return string.Format("This URL is not present in the WinINET cache. [Code: {0}]", num);
            }
            cb = lpdwCacheEntryInfoBufferSize;
            zero = Marshal.AllocHGlobal(cb);
            flag = GetUrlCacheEntryInfoA(sURL, zero, ref lpdwCacheEntryInfoBufferSize);
            num = Marshal.GetLastWin32Error();
            if (!flag)
            {
                Marshal.FreeHGlobal(zero);
                return ("GetUrlCacheEntryInfoA with buffer failed. 2=filenotfound 122=insufficient buffer, 259=nomoreitems. Last error: " + num.ToString() + "\n");
            }
            INTERNET_CACHE_ENTRY_INFOA internet_cache_entry_infoa = (INTERNET_CACHE_ENTRY_INFOA) Marshal.PtrToStructure(zero, typeof(INTERNET_CACHE_ENTRY_INFOA));
            lpdwCacheEntryInfoBufferSize = cb;
            long fileTime = (internet_cache_entry_infoa.LastModifiedTime.dwHighDateTime << 0x20) | ((long) ((ulong) internet_cache_entry_infoa.LastModifiedTime.dwLowDateTime));
            long num5 = (internet_cache_entry_infoa.LastAccessTime.dwHighDateTime << 0x20) | ((long) ((ulong) internet_cache_entry_infoa.LastAccessTime.dwLowDateTime));
            long num6 = (internet_cache_entry_infoa.LastSyncTime.dwHighDateTime << 0x20) | ((long) ((ulong) internet_cache_entry_infoa.LastSyncTime.dwLowDateTime));
            long num7 = (internet_cache_entry_infoa.ExpireTime.dwHighDateTime << 0x20) | ((long) ((ulong) internet_cache_entry_infoa.ExpireTime.dwLowDateTime));
            string[] strArray = new string[] { 
                "Url:\t\t", Marshal.PtrToStringAnsi(internet_cache_entry_infoa.lpszSourceUrlName), "\nCache File:\t", Marshal.PtrToStringAnsi(internet_cache_entry_infoa.lpszLocalFileName), "\nSize:\t\t", ((ulong) ((internet_cache_entry_infoa.dwSizeHigh << 0x20) + internet_cache_entry_infoa.dwSizeLow)).ToString("0,0"), " bytes\nFile Extension:\t", Marshal.PtrToStringAnsi(internet_cache_entry_infoa.lpszFileExtension), "\nHit Rate:\t", internet_cache_entry_infoa.dwHitRate.ToString(), "\nUse Count:\t", internet_cache_entry_infoa.dwUseCount.ToString(), "\nDon't Scavenge for:\t", internet_cache_entry_infoa._Union.dwExemptDelta.ToString(), " seconds\nLast Modified:\t", DateTime.FromFileTime(fileTime).ToString(), 
                "\nLast Accessed:\t", DateTime.FromFileTime(num5).ToString(), "\nLast Synced:  \t", DateTime.FromFileTime(num6).ToString(), "\nEntry Expires:\t", DateTime.FromFileTime(num7).ToString(), "\n"
             };
            string str = string.Concat(strArray);
            Marshal.FreeHGlobal(zero);
            return str;
        }

        [return: MarshalAs(UnmanagedType.Bool)]
        [DllImport("wininet.dll", CharSet=CharSet.Ansi, SetLastError=true, ExactSpelling=true)]
        private static extern bool GetUrlCacheEntryInfoA(string lpszUrlName, IntPtr lpCacheEntryInfo, ref int lpdwCacheEntryInfoBufferSize);
        private static void VistaClearTracks(bool bClearFiles, bool bClearCookies)
        {
            int num = 0;
            if (bClearCookies)
            {
                num = 2;
            }
            if (bClearFiles)
            {
                num = 0x100c;
            }
            try
            {
                using (Process.Start("rundll32.exe", "inetcpl.cpl,ClearMyTracksByProcess " + num.ToString()))
                {
                }
            }
            catch (Exception exception)
            {
                FiddlerApplication.DoNotifyUser("Failed to launch ClearMyTracksByProcess.\n" + exception.Message, "Error");
            }
        }

        [StructLayout(LayoutKind.Sequential)]
        private class INTERNET_CACHE_ENTRY_INFOA
        {
            public uint dwStructureSize;
            public IntPtr lpszSourceUrlName;
            public IntPtr lpszLocalFileName;
            public WinINETCache.WININETCACHEENTRYTYPE CacheEntryType;
            public uint dwUseCount;
            public uint dwHitRate;
            public uint dwSizeLow;
            public uint dwSizeHigh;
            public System.Runtime.InteropServices.ComTypes.FILETIME LastModifiedTime;
            public System.Runtime.InteropServices.ComTypes.FILETIME ExpireTime;
            public System.Runtime.InteropServices.ComTypes.FILETIME LastAccessTime;
            public System.Runtime.InteropServices.ComTypes.FILETIME LastSyncTime;
            public IntPtr lpHeaderInfo;
            public uint dwHeaderInfoSize;
            public IntPtr lpszFileExtension;
            public WinINETCache.WININETCACHEENTRYINFOUNION _Union;
        }

        [StructLayout(LayoutKind.Explicit)]
        private struct WININETCACHEENTRYINFOUNION
        {
            [FieldOffset(0)]
            public uint dwExemptDelta;
            [FieldOffset(0)]
            public uint dwReserved;
        }

        private enum WININETCACHEENTRYTYPE
        {
            ALL = 0x31003d,
            COOKIE_CACHE_ENTRY = 0x100000,
            EDITED_CACHE_ENTRY = 8,
            None = 0,
            NORMAL_CACHE_ENTRY = 1,
            SPARSE_CACHE_ENTRY = 0x10000,
            STICKY_CACHE_ENTRY = 4,
            TRACK_OFFLINE_CACHE_ENTRY = 0x10,
            TRACK_ONLINE_CACHE_ENTRY = 0x20,
            URLHISTORY_CACHE_ENTRY = 0x200000
        }
    }
}

