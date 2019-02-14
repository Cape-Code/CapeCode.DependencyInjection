using System;
using System.Collections.Generic;
using System.Reflection;

namespace CapeCode.DependencyInjection.Interfaces {
    [Obsolete]
    public interface IAssembliesCache {
        IList<Assembly> Assemblies { get; }
    }
}