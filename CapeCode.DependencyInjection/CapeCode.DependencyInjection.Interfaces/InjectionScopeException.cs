using System;

namespace CapeCode.DependencyInjection.Interfaces {
    [Obsolete]
    public class InjectionScopeException : Exception {
        public InjectionScopeException( string message, Exception innerException )
            : base( message, innerException ) {
        }

        public InjectionScopeException( string message )
            : base( message ) {
        }

        public InjectionScopeException()
            : base() {
        }
    }
}
