namespace Fiddler
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Threading;

    public class Logger
    {
        private EventHandler<LogEventArgs> _OnLogString;
        private List<string> queueStartupMessages;

        public event EventHandler<LogEventArgs> OnLogString
        {
            add
            {
                EventHandler<LogEventArgs> handler2;
                EventHandler<LogEventArgs> onLogString = this._OnLogString;
                do
                {
                    handler2 = onLogString;
                    EventHandler<LogEventArgs> handler3 = (EventHandler<LogEventArgs>) Delegate.Combine(handler2, value);
                    onLogString = Interlocked.CompareExchange<EventHandler<LogEventArgs>>(ref this._OnLogString, handler3, handler2);
                }
                while (onLogString != handler2);
            }
            remove
            {
                EventHandler<LogEventArgs> handler2;
                EventHandler<LogEventArgs> onLogString = this._OnLogString;
                do
                {
                    handler2 = onLogString;
                    EventHandler<LogEventArgs> handler3 = (EventHandler<LogEventArgs>) Delegate.Remove(handler2, value);
                    onLogString = Interlocked.CompareExchange<EventHandler<LogEventArgs>>(ref this._OnLogString, handler3, handler2);
                }
                while (onLogString != handler2);
            }
        }

        public Logger(bool bQueueStartup)
        {
            if (bQueueStartup)
            {
                this.queueStartupMessages = new List<string>();
            }
            else
            {
                this.queueStartupMessages = null;
            }
        }

        internal void FlushStartupMessages()
        {
            if ((this._OnLogString != null) && (this.queueStartupMessages != null))
            {
                List<string> queueStartupMessages = this.queueStartupMessages;
                this.queueStartupMessages = null;
                foreach (string str in queueStartupMessages)
                {
                    LogEventArgs e = new LogEventArgs(str);
                    this._OnLogString(this, e);
                }
            }
            else
            {
                this.queueStartupMessages = null;
            }
        }

        public void LogFormat(string format, params object[] args)
        {
            this.LogString(string.Format(format, args));
        }

        public void LogString(string sMsg)
        {
            if (CONFIG.bDebugSpew)
            {
                Trace.WriteLine(sMsg);
            }
            if (this.queueStartupMessages != null)
            {
                lock (this.queueStartupMessages)
                {
                    this.queueStartupMessages.Add(sMsg);
                }
            }
            else if (this._OnLogString != null)
            {
                LogEventArgs e = new LogEventArgs(sMsg);
                this._OnLogString(this, e);
            }
        }
    }
}

