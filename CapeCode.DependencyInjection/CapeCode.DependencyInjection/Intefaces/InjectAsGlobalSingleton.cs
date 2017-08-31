using System;

namespace CapeCode.DependencyInjection.Interfaces {
    public class InjectAsGlobalSingleton : InjectionRegistrationAttribute {

        public InjectAsGlobalSingleton( params Type[] registeredInterfaces )
            : base( registeredInterfaces ) { }

        public InjectAsGlobalSingleton() : base() { }
    }
}
