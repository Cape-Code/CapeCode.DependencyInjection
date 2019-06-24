using System;
using System.Collections.Generic;
using System.Linq;
using CapeCode.DependencyInjection.Interfaces;
using Microsoft.Extensions.DependencyInjection.Core;

namespace CapeCode.DependencyInjection.Core {
    [InjectAsGlobalSingleton]
    class ListInjectionRegistrationManager {

        private readonly IDictionary<Type, HashSet<Type>> _registeredTypesByListInterfaceType = new Dictionary<Type, HashSet<Type>>();

        public IEnumerable<Type> GetRegisteredTypesForListInterfaceType( Type listInterfaceType ) {
            if ( listInterfaceType != null && _registeredTypesByListInterfaceType.ContainsKey( listInterfaceType ) ) {
                return _registeredTypesByListInterfaceType[ listInterfaceType ].ToList().AsEnumerable();
            } else {
                return null;
            }
        }

        public void RegisterTypeForListInterfaceType( Type registeredType, Type listInterfaceType, bool replaceParentTypes ) {
            if ( listInterfaceType != null ) {
                if ( !_registeredTypesByListInterfaceType.ContainsKey( listInterfaceType ) ) {
                    _registeredTypesByListInterfaceType[ listInterfaceType ] = new HashSet<Type>();
                }
                if ( !_registeredTypesByListInterfaceType[ listInterfaceType ].Contains( registeredType ) ) {

                    var isReplacedByRegisteredChildTypes = _registeredTypesByListInterfaceType[ listInterfaceType ]
                        .Any( t => t.IsSubclassOf( registeredType )
                            && t.GetCustomAttribute<InjectInListAttribute>()
                                .Any( a => a.RegisteredInterfaces.Contains( listInterfaceType )
                                    && a.RemoveSubtypesFromList ) );

                    if ( !isReplacedByRegisteredChildTypes ) {
                        _registeredTypesByListInterfaceType[ listInterfaceType ].Add( registeredType );
                    }
                }
                if ( replaceParentTypes ) {
                    foreach ( var oldRegistration in _registeredTypesByListInterfaceType[ listInterfaceType ].ToList() ) {
                        if ( registeredType.IsSubclassOf( oldRegistration ) ) {
                            _registeredTypesByListInterfaceType[ listInterfaceType ].Remove( oldRegistration );
                        }
                    }
                }
            }
        }

    }
}
