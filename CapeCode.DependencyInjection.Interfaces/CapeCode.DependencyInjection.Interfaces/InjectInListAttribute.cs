using System;

namespace CapeCode.DependencyInjection.Interfaces {
    [System.AttributeUsage( System.AttributeTargets.Class, AllowMultiple = false, Inherited = false )]
    public class InjectInListAttribute : Attribute {

        public Type[] RegisteredInterfaces { get; private set; }

        public InjectInListAttribute( params Type[] registeredInterfaces ) {
            this.RegisteredInterfaces = registeredInterfaces;
        }

        public InjectInListAttribute() {
            this.RegisteredInterfaces = new Type[] { };
        }
    }
}
