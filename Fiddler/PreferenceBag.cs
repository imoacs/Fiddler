namespace Fiddler
{
    using Microsoft.Win32;
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Collections.Specialized;
    using System.Reflection;
    using System.Runtime.InteropServices;
    using System.Security.AccessControl;
    using System.Text;
    using System.Threading;

    public class PreferenceBag : IFiddlerPreferences
    {
        private static char[] _arrForbiddenChars = new char[] { '*', ' ', '$', '%', '@', '?', '!' };
        private readonly StringDictionary _dictPrefs = new StringDictionary();
        private readonly List<PrefWatcher> _listWatchers = new List<PrefWatcher>();
        private readonly ReaderWriterLock _RWLockPrefs = new ReaderWriterLock();
        private readonly ReaderWriterLock _RWLockWatchers = new ReaderWriterLock();
        private string _sCurrentProfile = ".default";
        private string _sRegistryPath;

        internal PreferenceBag(string sRegPath)
        {
            this._sRegistryPath = sRegPath;
            this.ReadRegistry();
        }

        private void _NotifyThreadExecute(object objThreadState)
        {
            PrefChangeEventArgs e = (PrefChangeEventArgs) objThreadState;
            string prefName = e.PrefName;
            List<EventHandler<PrefChangeEventArgs>> list = null;
            try
            {
                this._RWLockWatchers.AcquireReaderLock(-1);
                try
                {
                    foreach (PrefWatcher watcher in this._listWatchers)
                    {
                        if (prefName.StartsWith(watcher.sPrefixToWatch, StringComparison.Ordinal))
                        {
                            if (list == null)
                            {
                                list = new List<EventHandler<PrefChangeEventArgs>>();
                            }
                            list.Add(watcher.fnToNotify);
                        }
                    }
                }
                finally
                {
                    this._RWLockWatchers.ReleaseReaderLock();
                }
                if (list != null)
                {
                    foreach (EventHandler<PrefChangeEventArgs> handler in list)
                    {
                        try
                        {
                            handler(this, e);
                            continue;
                        }
                        catch (Exception exception)
                        {
                            FiddlerApplication.ReportException(exception);
                            continue;
                        }
                    }
                }
            }
            catch (Exception exception2)
            {
                FiddlerApplication.ReportException(exception2);
            }
        }

        public PrefWatcher AddWatcher(string sPrefixFilter, EventHandler<PrefChangeEventArgs> pcehHandler)
        {
            PrefWatcher item = new PrefWatcher(sPrefixFilter.ToLower(), pcehHandler);
            this._RWLockWatchers.AcquireWriterLock(-1);
            try
            {
                this._listWatchers.Add(item);
            }
            finally
            {
                this._RWLockWatchers.ReleaseWriterLock();
            }
            return item;
        }

        private void AsyncNotifyWatchers(PrefChangeEventArgs oNotifyArgs)
        {
            ThreadPool.UnsafeQueueUserWorkItem(new WaitCallback(this._NotifyThreadExecute), oNotifyArgs);
        }

        public void Close()
        {
            this._listWatchers.Clear();
            this.WriteRegistry();
        }

        internal string FindMatches(string sFilter)
        {
            StringBuilder builder = new StringBuilder(0x80);
            try
            {
                this._RWLockPrefs.AcquireReaderLock(-1);
                foreach (DictionaryEntry entry in this._dictPrefs)
                {
                    if (((string) entry.Key).IndexOf(sFilter, StringComparison.OrdinalIgnoreCase) > -1)
                    {
                        builder.AppendFormat("{0}:\t{1}\r\n", entry.Key, entry.Value);
                    }
                }
            }
            finally
            {
                this._RWLockPrefs.ReleaseReaderLock();
            }
            return builder.ToString();
        }

        public bool GetBoolPref(string sPrefName, bool bDefault)
        {
            bool flag;
            string str = this[sPrefName];
            if ((str != null) && bool.TryParse(str, out flag))
            {
                return flag;
            }
            return bDefault;
        }

        public int GetInt32Pref(string sPrefName, int iDefault)
        {
            int num;
            string s = this[sPrefName];
            if ((s != null) && int.TryParse(s, out num))
            {
                return num;
            }
            return iDefault;
        }

        public string[] GetPrefArray()
        {
            string[] strArray2;
            try
            {
                this._RWLockPrefs.AcquireReaderLock(-1);
                string[] array = new string[this._dictPrefs.Count];
                this._dictPrefs.Keys.CopyTo(array, 0);
                strArray2 = array;
            }
            finally
            {
                this._RWLockPrefs.ReleaseReaderLock();
            }
            return strArray2;
        }

        public string GetStringPref(string sPrefName, string sDefault)
        {
            string str = this[sPrefName];
            return (str ?? sDefault);
        }

        private bool isValidName(string sName)
        {
            return (((!string.IsNullOrEmpty(sName) && (0x100 > sName.Length)) && (0 > sName.IndexOf("internal", StringComparison.OrdinalIgnoreCase))) && (0 > sName.IndexOfAny(_arrForbiddenChars)));
        }

        private void ReadRegistry()
        {
            if (this._sRegistryPath != null)
            {
                RegistryKey key = Registry.CurrentUser.OpenSubKey(this._sRegistryPath + @"\" + this._sCurrentProfile, RegistryKeyPermissionCheck.ReadSubTree, RegistryRights.ExecuteKey);
                if (key != null)
                {
                    string[] valueNames = key.GetValueNames();
                    try
                    {
                        this._RWLockPrefs.AcquireWriterLock(-1);
                        foreach (string str in valueNames)
                        {
                            if ((str.Length >= 1) && !str.Contains("ephemeral"))
                            {
                                try
                                {
                                    this._dictPrefs[str] = (string) key.GetValue(str, string.Empty);
                                }
                                catch (Exception)
                                {
                                }
                            }
                        }
                    }
                    finally
                    {
                        this._RWLockPrefs.ReleaseWriterLock();
                        key.Close();
                    }
                }
            }
        }

        public void RemovePref(string sPrefName)
        {
            bool flag = false;
            try
            {
                this._RWLockPrefs.AcquireWriterLock(-1);
                flag = this._dictPrefs.ContainsKey(sPrefName);
                this._dictPrefs.Remove(sPrefName);
            }
            finally
            {
                this._RWLockPrefs.ReleaseWriterLock();
            }
            if (flag)
            {
                PrefChangeEventArgs oNotifyArgs = new PrefChangeEventArgs(sPrefName, string.Empty);
                this.AsyncNotifyWatchers(oNotifyArgs);
            }
        }

        public void RemoveWatcher(PrefWatcher wliToRemove)
        {
            this._RWLockWatchers.AcquireWriterLock(-1);
            try
            {
                this._listWatchers.Remove(wliToRemove);
            }
            finally
            {
                this._RWLockWatchers.ReleaseWriterLock();
            }
        }

        public void SetBoolPref(string sPrefName, bool bValue)
        {
            this[sPrefName] = bValue.ToString();
        }

        public void SetInt32Pref(string sPrefName, int iValue)
        {
            this[sPrefName] = iValue.ToString();
        }

        public void SetStringPref(string sPrefName, string sValue)
        {
            this[sPrefName] = sValue;
        }

        public override string ToString()
        {
            return this.ToString(true);
        }

        public string ToString(bool bVerbose)
        {
            StringBuilder builder = new StringBuilder(0x80);
            try
            {
                this._RWLockPrefs.AcquireReaderLock(-1);
                builder.AppendFormat("PreferenceBag [{0} Preferences. {1} Watchers.]", this._dictPrefs.Count, this._listWatchers.Count);
                if (bVerbose)
                {
                    builder.Append("\n");
                    foreach (DictionaryEntry entry in this._dictPrefs)
                    {
                        builder.AppendFormat("{0}:\t{1}\n", entry.Key, entry.Value);
                    }
                }
            }
            finally
            {
                this._RWLockPrefs.ReleaseReaderLock();
            }
            return builder.ToString();
        }

        private void WriteRegistry()
        {
            if (!CONFIG.bIsViewOnly && (this._sRegistryPath != null))
            {
                RegistryKey key = Registry.CurrentUser.CreateSubKey(this._sRegistryPath, RegistryKeyPermissionCheck.ReadWriteSubTree);
                if (key != null)
                {
                    try
                    {
                        this._RWLockPrefs.AcquireReaderLock(-1);
                        key.DeleteSubKey(this._sCurrentProfile, false);
                        if (this._dictPrefs.Count >= 1)
                        {
                            key = key.CreateSubKey(this._sCurrentProfile, RegistryKeyPermissionCheck.ReadWriteSubTree);
                            foreach (DictionaryEntry entry in this._dictPrefs)
                            {
                                string name = (string) entry.Key;
                                if (!name.Contains("ephemeral"))
                                {
                                    key.SetValue(name, entry.Value);
                                }
                            }
                        }
                    }
                    finally
                    {
                        this._RWLockPrefs.ReleaseReaderLock();
                        key.Close();
                    }
                }
            }
        }

        public string CurrentProfile
        {
            get
            {
                return this._sCurrentProfile;
            }
        }

        public string this[string sPrefName]
        {
            get
            {
                string str;
                try
                {
                    this._RWLockPrefs.AcquireReaderLock(-1);
                    str = this._dictPrefs[sPrefName];
                }
                finally
                {
                    this._RWLockPrefs.ReleaseReaderLock();
                }
                return str;
            }
            set
            {
                if (!this.isValidName(sPrefName))
                {
                    throw new ArgumentException(string.Format("Preference name must contain 1 or more characters from the set A-z0-9-_ and may not contain the word Internal.\nYou tried to set: \"{0}\"", sPrefName));
                }
                if (value == null)
                {
                    this.RemovePref(sPrefName);
                }
                else
                {
                    bool flag = false;
                    try
                    {
                        this._RWLockPrefs.AcquireWriterLock(-1);
                        flag = !this._dictPrefs.ContainsKey(sPrefName) || (this._dictPrefs[sPrefName] != value);
                        this._dictPrefs[sPrefName] = value;
                    }
                    finally
                    {
                        this._RWLockPrefs.ReleaseWriterLock();
                    }
                    if (flag)
                    {
                        PrefChangeEventArgs oNotifyArgs = new PrefChangeEventArgs(sPrefName, value);
                        this.AsyncNotifyWatchers(oNotifyArgs);
                    }
                }
            }
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct PrefWatcher
        {
            internal readonly EventHandler<PrefChangeEventArgs> fnToNotify;
            internal readonly string sPrefixToWatch;
            internal PrefWatcher(string sPrefixFilter, EventHandler<PrefChangeEventArgs> fnHandler)
            {
                this.sPrefixToWatch = sPrefixFilter;
                this.fnToNotify = fnHandler;
            }
        }
    }
}

