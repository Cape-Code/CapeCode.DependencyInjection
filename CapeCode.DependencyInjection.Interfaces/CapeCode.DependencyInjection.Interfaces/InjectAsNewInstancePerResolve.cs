using System;

namespace CapeCode.DependencyInjection.Interfaces {
    public class InjectAsNewInstancePerResolve : InjectionRegistrationAttribute {

        public InjectAsNewInstancePerResolve( params Type[] registeredInterfaces )
            : base( registeredInterfaces ) { }

        public InjectAsNewInstancePerResolve() : base() { }
    }
}
