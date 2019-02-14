using System;

namespace CapeCode.DependencyInjection.Interfaces {
    [Obsolete]
    public class InjectAsScopedSingleton : InjectionRegistrationAttribute {
        public Type ScopeRelatedTo { get; set; }

        public InjectAsScopedSingleton( Type scopeRelatedTo, params Type[] registeredInterfaces )
            : base( registeredInterfaces ) {
            ScopeRelatedTo = scopeRelatedTo;
        }

        public InjectAsScopedSingleton( Type scopeRelatedTo )
            : base() {
            ScopeRelatedTo = scopeRelatedTo;
        }
    }
}
