
using System;
using System.Collections;
using System.Collections.Generic;
using Microsoft.Extensions.DependencyInjection;

namespace CapeCode.DependencyInjection.Core {
    public class ListInjectionProxy<T> : IEnumerable<T> {

        private readonly IEnumerable<T> _instances = new HashSet<T>();

        public ListInjectionProxy( IServiceProvider serviceProvider ) {
            var listInjectionRegistrationManager = serviceProvider.GetService<ListInjectionRegistrationManager>();
            var types = listInjectionRegistrationManager.GetRegisteredTypesForListInterfaceType( typeof( T ) );
            var typeInstances = new HashSet<T>();
            if ( types != null ) {
                foreach ( var type in types ) {
                    typeInstances.Add( ( T ) serviceProvider.GetService( type ) );
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
