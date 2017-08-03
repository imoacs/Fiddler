namespace Fiddler
{
    using System;

    public class NotificationEventArgs : EventArgs
    {
        private readonly string _sMessage;

        internal NotificationEventArgs(string sMsg)
        {
            this._sMessage = sMsg;
        }

        public string NotifyString
        {
            get
            {
                return this._sMessage;
            }
        }
    }
}

