namespace Fiddler
{
    using System;

    public class LogEventArgs : EventArgs
    {
        private readonly string _sMessage;

        internal LogEventArgs(string sMsg)
        {
            this._sMessage = sMsg;
        }

        public string LogString
        {
            get
            {
                return this._sMessage;
            }
        }
    }
}

