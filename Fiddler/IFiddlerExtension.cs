namespace Fiddler
{
    using System;

    public interface IFiddlerExtension
    {
        void OnBeforeUnload();
        void OnLoad();
    }
}

