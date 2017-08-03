namespace Fiddler
{
    using System;

    [AttributeUsage(AttributeTargets.Method, Inherited=false)]
    public sealed class ContextAction : Attribute
    {
        private string myName;

        public ContextAction(string name)
        {
            this.myName = name;
        }

        public string Name
        {
            get
            {
                return this.myName;
            }
        }
    }
}

