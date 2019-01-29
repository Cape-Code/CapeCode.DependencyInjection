using System.Reflection;
using CapeCode.DependencyInjection;

namespace Microsoft.Extensions.DependencyInjection {
    public static class DependencyInjectionExtension {
        public static void AddDependencyInjectionAssemblies( this IServiceCollection services, params Assembly[] assemblies ) {
            var injectionRegistrationController = new InjectionRegistrationController( services );

            foreach ( var assembly in assemblies )
                injectionRegistrationController.RegisterAllClasses( assembly );
        }
    }
}
