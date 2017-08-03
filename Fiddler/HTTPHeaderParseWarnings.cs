namespace Fiddler
{
    using System;

    [Flags]
    public enum HTTPHeaderParseWarnings
    {
        EndedWithLFCRLF = 2,
        EndedWithLFLF = 1,
        Malformed = 4,
        None = 0
    }
}

