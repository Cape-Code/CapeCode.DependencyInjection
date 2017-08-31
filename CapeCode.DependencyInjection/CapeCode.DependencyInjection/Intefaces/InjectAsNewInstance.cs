using System;

namespace CapeCode.DependencyInjection.Interfaces {
    public class InjectAsNewInstance : InjectionRegistrationAttribute {

        public InjectAsNewInstance( params Type[] registeredInterfaces )
            : base( registeredInterfaces ) { }

        public InjectAsNewInstance() : base() { }
    }
}
