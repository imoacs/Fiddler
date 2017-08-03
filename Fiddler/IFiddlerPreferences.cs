namespace Fiddler
{
    using System;
    using System.Reflection;

    public interface IFiddlerPreferences
    {
        PreferenceBag.PrefWatcher AddWatcher(string sPrefixFilter, EventHandler<PrefChangeEventArgs> pcehHandler);
        bool GetBoolPref(string sPrefName, bool bDefault);
        int GetInt32Pref(string sPrefName, int iDefault);
        string GetStringPref(string sPrefName, string sDefault);
        void RemovePref(string sPrefName);
        void RemoveWatcher(PreferenceBag.PrefWatcher wliToRemove);
        void SetBoolPref(string sPrefName, bool bValue);
        void SetInt32Pref(string sPrefName, int iValue);
        void SetStringPref(string sPrefName, string sValue);

        string this[string sName] { get; set; }
    }
}

