using System;

namespace CapeCode.DependencyInjection.Interfaces {
    public class InjectAsNewInstanceAttribute : InjectionRegistrationAttribute {

        public InjectAsNewInstanceAttribute( params Type[] registeredInterfaces )
            : base( registeredInterfaces ) { }

        public InjectAsNewInstanceAttribute() : base() { }
    }
}
