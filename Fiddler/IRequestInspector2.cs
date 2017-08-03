namespace Fiddler
{
    using System;

    public interface IRequestInspector2 : IBaseInspector2
    {
        HTTPRequestHeaders headers { get; set; }
    }
}

