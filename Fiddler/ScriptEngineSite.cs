namespace Fiddler
{
    using Microsoft.Vsa;
    using System;
    using System.Runtime.InteropServices;

    internal class ScriptEngineSite : IVsaSite
    {
        private FiddlerScript _myOwner;

        public ScriptEngineSite(FiddlerScript myOwner)
        {
            this._myOwner = myOwner;
        }

        void IVsaSite.GetCompiledState(out byte[] pe, out byte[] debugInfo)
        {
            pe = (byte[]) (debugInfo = null);
        }

        object IVsaSite.GetEventSourceInstance(string itemName, string eventSourceName)
        {
            throw new VsaException(VsaError.EventSourceInvalid);
        }

        object IVsaSite.GetGlobalInstance(string name)
        {
            string str;
            if (((str = name) == null) || (!(str == "FiddlerObject") && !(str == "FiddlerScript")))
            {
                return null;
            }
            return this._myOwner;
        }

        void IVsaSite.Notify(string sNotify, object oInfo)
        {
        }

        bool IVsaSite.OnCompilerError(IVsaError e)
        {
            this._myOwner.NotifyOfCompilerError(e);
            return false;
        }
    }
}

