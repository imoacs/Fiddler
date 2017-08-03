namespace Fiddler
{
    using System;

    public class HTTPHeaderItem : ICloneable
    {
        [CodeDescription("String name of the HTTP header.")]
        public string Name;
        [CodeDescription("String value of the HTTP header.")]
        public string Value;

        public HTTPHeaderItem(string sName, string sValue)
        {
            this.Name = sName;
            this.Value = sValue;
        }

        public object Clone()
        {
            return base.MemberwiseClone();
        }

        public override string ToString()
        {
            return string.Format("{0}: {1}", this.Name, this.Value);
        }
    }
}

