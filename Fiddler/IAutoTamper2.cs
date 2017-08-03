namespace Fiddler
{
    using System;

    public interface IAutoTamper2 : IAutoTamper, IFiddlerExtension
    {
        void OnPeekAtResponseHeaders(Session oSession);
    }
}

