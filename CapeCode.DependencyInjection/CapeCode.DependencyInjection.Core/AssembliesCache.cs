using System.Collections.Generic;
using System.Reflection;
using CapeCode.DependencyInjection.Interfaces;

namespace CapeCode.DependencyInjection {
    public class AssembliesCache : IAssembliesCache {
        public IList<Assembly> Assemblies { get; private set; }

        public AssembliesCache( IList<Assembly> assemblies ) {
            this.Assemblies = assemblies;
        }

        public AssembliesCache()
            : this( new List<Assembly>() ) {
        }
    }
}
