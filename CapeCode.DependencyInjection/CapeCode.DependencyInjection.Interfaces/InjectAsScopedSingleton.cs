using System;

namespace CapeCode.DependencyInjection.Interfaces {
    [Obsolete]
    public class InjectAsScopedSingletonAttribute : InjectionRegistrationAttribute {
        public Type ScopeRelatedTo { get; set; }

        public InjectAsScopedSingletonAttribute( Type scopeRelatedTo, params Type[] registeredInterfaces )
            : base( registeredInterfaces ) {
            ScopeRelatedTo = scopeRelatedTo;
        }

        public InjectAsScopedSingletonAttribute( Type scopeRelatedTo )
            : base() {
            ScopeRelatedTo = scopeRelatedTo;
        }
    }
}
