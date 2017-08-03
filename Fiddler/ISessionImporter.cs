namespace Fiddler
{
    using System;
    using System.Collections.Generic;

    public interface ISessionImporter : IDisposable
    {
        Session[] ImportSessions(string sImportFormat, Dictionary<string, object> dictOptions, EventHandler<ProgressCallbackEventArgs> evtProgressNotifications);
    }
}

