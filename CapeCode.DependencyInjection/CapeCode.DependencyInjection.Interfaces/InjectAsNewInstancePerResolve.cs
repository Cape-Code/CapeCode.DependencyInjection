using System;

namespace CapeCode.DependencyInjection.Interfaces {
    [Obsolete]
    public class InjectAsNewInstancePerResolveAttribute : InjectionRegistrationAttribute {

        public InjectAsNewInstancePerResolveAttribute( params Type[] registeredInterfaces )
            : base( registeredInterfaces ) { }

        public InjectAsNewInstancePerResolveAttribute() : base() { }
    }
}
