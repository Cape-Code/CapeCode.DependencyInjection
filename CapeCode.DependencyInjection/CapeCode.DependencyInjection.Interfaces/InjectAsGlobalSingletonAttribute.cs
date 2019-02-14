using System;

namespace CapeCode.DependencyInjection.Interfaces {
    public class InjectAsGlobalSingletonAttribute : InjectionRegistrationAttribute {

        public InjectAsGlobalSingleton( params Type[] registeredInterfaces )
            : base( registeredInterfaces ) { }

        public InjectAsGlobalSingleton() : base() { }
    }
}
