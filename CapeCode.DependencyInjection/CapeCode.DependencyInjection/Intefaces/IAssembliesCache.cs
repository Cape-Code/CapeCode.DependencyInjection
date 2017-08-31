using System.Collections.Generic;
using System.Reflection;

namespace CapeCode.DependencyInjection.Interfaces {
    public interface IAssembliesCache {
        IList<Assembly> Assemblies { get; }
    }
}