namespace Fiddler
{
    using System;

    [AttributeUsage(AttributeTargets.Method, Inherited=false)]
    public sealed class ToolsAction : Attribute
    {
        private string myName;

        public ToolsAction(string name)
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

