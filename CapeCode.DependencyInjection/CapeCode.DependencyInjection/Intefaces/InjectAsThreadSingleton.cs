using System;

namespace CapeCode.DependencyInjection.Interfaces {
    public class InjectAsThreadSingleton : InjectionRegistrationAttribute {
        public InjectAsThreadSingleton( params Type[] registeredInterfaces )
            : base( registeredInterfaces ) { }

        public InjectAsThreadSingleton() : base() { }
    }
}
