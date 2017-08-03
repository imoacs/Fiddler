namespace Fiddler
{
    using System;

    public interface IBaseInspector2
    {
        void Clear();

        bool bDirty { get; }

        byte[] body { get; set; }

        bool bReadOnly { get; set; }
    }
}

