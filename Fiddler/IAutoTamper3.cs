namespace Fiddler
{
    using System;

    public interface IAutoTamper3 : IAutoTamper2, IAutoTamper, IFiddlerExtension
    {
        void OnPeekAtRequestHeaders(Session oSession);
    }
}

