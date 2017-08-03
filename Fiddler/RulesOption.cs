namespace Fiddler
{
    using System;

    [AttributeUsage(AttributeTargets.Field, Inherited=false, AllowMultiple=false)]
    public sealed class RulesOption : Attribute
    {
        internal bool followingSplitter;
        private bool isExclusive;
        private string myName;
        private string mySubMenu;

        public RulesOption(string name) : this(name, null, false, false)
        {
        }

        public RulesOption(string name, string subMenu) : this(name, subMenu, false, false)
        {
        }

        public RulesOption(string name, string subMenu, bool exclusive) : this(name, subMenu, exclusive, false)
        {
        }

        public RulesOption(string name, string subMenu, bool exclusive, bool hasSplitter)
        {
            this.myName = name;
            this.mySubMenu = subMenu;
            this.isExclusive = exclusive;
            this.followingSplitter = hasSplitter;
        }

        public bool IsExclusive
        {
            get
            {
                return this.isExclusive;
            }
        }

        public string Name
        {
            get
            {
                return this.myName;
            }
        }

        public string SubMenu
        {
            get
            {
                return this.mySubMenu;
            }
        }
    }
}

