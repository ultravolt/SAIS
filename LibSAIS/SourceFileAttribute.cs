using System;

namespace LibSAIS
{
    internal class SourceFileAttribute : Attribute
    {
        public string Value { get; set; }

        public SourceFileAttribute(string value)
        {
            this.Value = value;
        }
    }
}