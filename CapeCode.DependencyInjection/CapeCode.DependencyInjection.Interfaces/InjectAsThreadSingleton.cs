using System;

namespace CapeCode.DependencyInjection.Interfaces {
    [Obsolete]
    public class InjectAsThreadSingletonAttribute : InjectionRegistrationAttribute {
        public InjectAsThreadSingletonAttribute( params Type[] registeredInterfaces )
            : base( registeredInterfaces ) { }

        public InjectAsThreadSingletonAttribute() : base() { }
    }
}
