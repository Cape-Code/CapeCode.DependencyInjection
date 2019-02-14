using System;

namespace CapeCode.DependencyInjection.Interfaces {
    [Obsolete]
    public class InjectAsNewInstancePerResolve : InjectionRegistrationAttribute {

        public InjectAsNewInstancePerResolve( params Type[] registeredInterfaces )
            : base( registeredInterfaces ) { }

        public InjectAsNewInstancePerResolve() : base() { }
    }
}
