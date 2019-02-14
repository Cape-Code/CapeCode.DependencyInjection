using System;

namespace CapeCode.DependencyInjection.Interfaces {
    public class InjectAsRequestSingletonAttribute : InjectionRegistrationAttribute {
        public InjectAsRequestSingleton( params Type[] registeredInterfaces )
            : base( registeredInterfaces ) { }

        public InjectAsRequestSingleton() : base() { }
    }
}
