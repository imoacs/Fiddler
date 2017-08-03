namespace Fiddler
{
    using System;
    using System.Collections.Generic;

    public interface ISessionExporter : IDisposable
    {
        bool ExportSessions(string sExportFormat, Session[] oSessions, Dictionary<string, object> dictOptions, EventHandler<ProgressCallbackEventArgs> evtProgressNotifications);
    }
}

