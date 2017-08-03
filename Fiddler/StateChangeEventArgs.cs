namespace Fiddler
{
    using System;

    public class StateChangeEventArgs : EventArgs
    {
        public readonly SessionStates newState;
        public readonly SessionStates oldState;

        public StateChangeEventArgs(SessionStates ssOld, SessionStates ssNew)
        {
            this.oldState = ssOld;
            this.newState = ssNew;
        }
    }
}

