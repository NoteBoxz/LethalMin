using System;

namespace LethalMin
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
    public class CompatClassAttribute : Attribute
    {
        public string ModGUID { get; private set; }

        public CompatClassAttribute(string modGUID)
        {
            ModGUID = modGUID;
        }
    }
}