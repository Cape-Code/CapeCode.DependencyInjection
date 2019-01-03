using System;
using System.Collections.Generic;
using System.Linq;
using CapeCode.DependencyInjection.Interfaces;

namespace CapeCode.DependencyInjection {
    //[InjectAsGlobalSingleton]
    class ListInjectionRegistrationManager {

        private readonly IDictionary<Type, HashSet<Type>> _registeredTypesByListInterfaceType = new Dictionary<Type, HashSet<Type>>();

        public IEnumerable<Type> GetRegisteredTypesForListInterfaceType( Type listInterfaceType ) {
            if ( listInterfaceType != null && _registeredTypesByListInterfaceType.ContainsKey( listInterfaceType ) ) {
                return _registeredTypesByListInterfaceType[ listInterfaceType ].ToList().AsEnumerable();
            } else {
                return null;
            }
        }

        public void RegisterTypeForListInterfaceType( Type registeredType, Type listInterfaceType ) {
            if ( listInterfaceType != null ) {
                if ( !_registeredTypesByListInterfaceType.ContainsKey( listInterfaceType ) ) {
                    _registeredTypesByListInterfaceType[ listInterfaceType ] = new HashSet<Type>();
                }
                if ( !_registeredTypesByListInterfaceType[ listInterfaceType ].Contains( registeredType ) ) {
                    _registeredTypesByListInterfaceType[ listInterfaceType ].Add( registeredType );
                }
            }
        }

    }
}
