using System;

namespace CapeCode.DependencyInjection.Interfaces {
    [Serializable]
    public class InjectionRegistrationException : Exception {
        public Type RegisteredForType { get; private set; }
        public Type RegisteredToType { get; private set; }

        public InjectionRegistrationException( Type registeredForType, Type registeredToType, string message, Exception innerException )
            : base( message, innerException ) {
            this.RegisteredForType = registeredForType;
            this.RegisteredToType = registeredToType;
        }

        public InjectionRegistrationException( Type registeredForType, Type registeredToType, string message )
            : base( message ) {
            this.RegisteredForType = registeredForType;
            this.RegisteredToType = registeredToType;
        }

        public InjectionRegistrationException( Type registeredForType, Type registeredToType )
            : base() {
            this.RegisteredForType = registeredForType;
            this.RegisteredToType = registeredToType;
        }
    }
}
