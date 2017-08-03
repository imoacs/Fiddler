namespace Fiddler
{
    using System;

    [AttributeUsage(AttributeTargets.Event | AttributeTargets.Field | AttributeTargets.Property | AttributeTargets.Method, Inherited=false, AllowMultiple=false)]
    public sealed class CodeDescription : Attribute
    {
        private string sDesc;

        public CodeDescription(string desc)
        {
            this.sDesc = desc;
        }

        public string Description
        {
            get
            {
                return this.sDesc;
            }
        }
    }
}

