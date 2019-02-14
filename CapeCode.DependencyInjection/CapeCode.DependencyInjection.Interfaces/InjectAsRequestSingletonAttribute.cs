using System;

namespace CapeCode.DependencyInjection.Interfaces {
    public class InjectAsRequestSingletonAttribute : InjectionRegistrationAttribute {
        public InjectAsRequestSingletonAttribute( params Type[] registeredInterfaces )
            : base( registeredInterfaces ) { }

        public InjectAsRequestSingletonAttribute() : base() { }
    }
}
