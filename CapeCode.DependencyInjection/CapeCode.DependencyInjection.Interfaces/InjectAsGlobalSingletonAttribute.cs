using System;

namespace CapeCode.DependencyInjection.Interfaces {
    public class InjectAsGlobalSingletonAttribute : InjectionRegistrationAttribute {

        public InjectAsGlobalSingletonAttribute( params Type[] registeredInterfaces )
            : base( registeredInterfaces ) { }

        public InjectAsGlobalSingletonAttribute() : base() { }
    }
}
