using System;

namespace CapeCode.DependencyInjection.Interfaces {
    [System.AttributeUsage( System.AttributeTargets.Assembly, AllowMultiple = false, Inherited = false )]
    [Obsolete]
    public abstract class AssemblyInjectionTagAttribute : Attribute {
    }
}
