namespace Fiddler
{
    using System;

    public interface IHandleExecAction : IFiddlerExtension
    {
        bool OnExecAction(string sCommand);
    }
}

