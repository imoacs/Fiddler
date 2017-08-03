namespace Fiddler
{
    using System;

    [Flags]
    public enum SessionFlags
    {
        ClientPipeReused = 8,
        ImportedFromOtherTool = 0x400,
        IsBlindTunnel = 0x1000,
        IsDecryptingTunnel = 0x2000,
        IsFTP = 2,
        IsHTTPS = 1,
        LoadedFromSAZ = 0x200,
        None = 0,
        ProtocolViolationInRequest = 0x8000,
        ProtocolViolationInResponse = 0x10000,
        RequestGeneratedByFiddler = 0x80,
        RESERVED32 = 0x20,
        RESERVED4 = 4,
        ResponseBodyDropped = 0x20000,
        ResponseGeneratedByFiddler = 0x100,
        ResponseStreamed = 0x40,
        SentToGateway = 0x800,
        ServedFromCache = 0x4000,
        ServerPipeReused = 0x10
    }
}

