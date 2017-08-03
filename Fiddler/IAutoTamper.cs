namespace Fiddler
{
    using System;

    public interface IAutoTamper : IFiddlerExtension
    {
        void AutoTamperRequestAfter(Session oSession);
        void AutoTamperRequestBefore(Session oSession);
        void AutoTamperResponseAfter(Session oSession);
        void AutoTamperResponseBefore(Session oSession);
        void OnBeforeReturningError(Session oSession);
    }
}

