namespace Fiddler
{
    using System;

    public enum PipeReusePolicy
    {
        NoRestrictions,
        MarriedToClientProcess,
        MarriedToClientPipe,
        NoReuse
    }
}

