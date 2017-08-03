namespace Fiddler
{
    using System;

    public interface IResponseInspector2 : IBaseInspector2
    {
        HTTPResponseHeaders headers { get; set; }
    }
}

