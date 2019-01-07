using System.Collections;
using System.Collections.Generic;
using CapeCode.DependencyInjection.Interfaces;

namespace CapeCode.DependencyInjection {
    public class ListInjectionProxy<T> : IEnumerable<T> {

        private readonly IEnumerable<T> _instances = new HashSet<T>();

        public ListInjectionProxy( IDependencyResolver dependencyResolver ) {
            var listInjectionRegistrationManager = dependencyResolver.Resolve<ListInjectionRegistrationManager>();
            var types = listInjectionRegistrationManager.GetRegisteredTypesForListInterfaceType( typeof( T ) );
            var typeInstances = new HashSet<T>();
            if ( types != null ) {
                foreach ( var type in types ) {
                    typeInstances.Add( ( T ) dependencyResolver.Resolve( type ) );
                }
            }
            _instances = typeInstances;
        }

        #region IEnumerable<T> Member

        public IEnumerator<T> GetEnumerator() {
            return _instances.GetEnumerator();
        }

        #endregion

        #region IEnumerable Member

        IEnumerator IEnumerable.GetEnumerator() {
            return _instances.GetEnumerator();
        }

        #endregion
    }
}
