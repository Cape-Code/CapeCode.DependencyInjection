using System;

namespace CapeCode.DependencyInjection.Interfaces {
    [System.AttributeUsage( System.AttributeTargets.Class, AllowMultiple = false, Inherited = false )]
    public class InjectInListAttribute : Attribute {

        public bool RemoveSubtypesFromList { get; private set; }
        public Type[] RegisteredInterfaces { get; private set; }

        public InjectInListAttribute( bool removeSubtypesFromList, params Type[] registeredInterfaces ) {
            RemoveSubtypesFromList = removeSubtypesFromList;
            this.RegisteredInterfaces = registeredInterfaces;
        }

        public InjectInListAttribute( params Type[] registeredInterfaces ) {
            this.RegisteredInterfaces = registeredInterfaces;
        }

        public InjectInListAttribute() {
            this.RegisteredInterfaces = new Type[] { };
        }
    }
}
