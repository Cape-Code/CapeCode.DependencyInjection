using System;

namespace CapeCode.DependencyInjection.Interfaces {
    [Obsolete]
    public class InjectAsThreadSingleton : InjectionRegistrationAttribute {
        public InjectAsThreadSingleton( params Type[] registeredInterfaces )
            : base( registeredInterfaces ) { }

        public InjectAsThreadSingleton() : base() { }
    }
}
