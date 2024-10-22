using System;
namespace LethalMin
{
    public interface IDebuggable
    {
        [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, Inherited = true, AllowMultiple = false)]
        public class DebugAttribute : Attribute { }
    }
}