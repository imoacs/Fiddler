namespace Fiddler
{
    using System;

    [AttributeUsage(AttributeTargets.Method, Inherited=false)]
    public sealed class BindUIColumn : Attribute
    {
        internal string _colName;
        internal int _iColWidth;

        public BindUIColumn(string colName) : this(colName, 50)
        {
        }

        public BindUIColumn(string colName, int iColWidth)
        {
            this._colName = colName;
            this._iColWidth = iColWidth;
        }
    }
}

