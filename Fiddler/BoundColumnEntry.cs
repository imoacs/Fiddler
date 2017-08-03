namespace Fiddler
{
    using System;

    internal class BoundColumnEntry
    {
        internal getColumnStringDelegate _delFn;
        internal int _iColNum;
        internal string _sSessionFlagName;

        internal BoundColumnEntry(getColumnStringDelegate delFn, int iColNum)
        {
            this._delFn = delFn;
            this._iColNum = iColNum;
        }

        internal BoundColumnEntry(string sSessionFlagName, int iColNum)
        {
            this._sSessionFlagName = sSessionFlagName;
            this._iColNum = iColNum;
        }
    }
}

