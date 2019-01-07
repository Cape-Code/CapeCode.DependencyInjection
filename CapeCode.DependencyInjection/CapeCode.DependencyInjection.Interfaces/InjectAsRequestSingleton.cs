using System;

namespace CapeCode.DependencyInjection.Interfaces {
    public class InjectAsRequestSingleton : InjectionRegistrationAttribute {
        public InjectAsRequestSingleton( params Type[] registeredInterfaces )
            : base( registeredInterfaces ) { }

        public InjectAsRequestSingleton() : base() { }
    }
}
