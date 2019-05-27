using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using CapeCode.DependencyInjection.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Core;

namespace CapeCode.DependencyInjection.Core {
    public class InjectionRegistrationController {

        public InjectionRegistrationController( IList<object> enumRestrictions ) {
            _enumRestrictions = enumRestrictions;
        }

        private readonly IList<object> _enumRestrictions;

        private ListInjectionRegistrationManager _listInjectionRegistrationManager = new ListInjectionRegistrationManager();

        public void RegisterAllClasses( IServiceCollection serviceCollection, Assembly assembly ) {
            IList<Type> types;
            try {
                types = assembly.GetTypes();
            } catch ( ReflectionTypeLoadException ex ) {
                throw new Exception( $@"Loading the types {( ex.Types != null ? string.Join( ", ", ex.Types.Where( t => t != null ).Select( t => t.FullName ) ) : "'NULL'" )} failed with: \n {( ex.LoaderExceptions != null ? string.Join( @"\n\n", ex.LoaderExceptions.Where( e => e != null ).Select( e => e.ToString() ) ) : "'NULL'" )}", ex );
            }

            foreach ( Type type in types ) {
                RegisterType( serviceCollection, type );
            }
        }

        public void RegisterType( IServiceCollection serviceCollection, Type type ) {
            if ( !type.IsInterface ) {
                if ( type.IsClass && !type.IsAbstract ) {
                    // Filter attributes to have only attributes for arbitrary environments and attributes fitting to the current environment.
                    var isEnvironmentCorrect = type.EvaluateDerivedCustomAttributePredicate<InjectionEnumRestrictionAttribute>( era => era.ValidEnumValues.Any( vev => _enumRestrictions.Any( ev => Equals( ev, vev ) ) ), trueIfEmpty: true );

                    var registrationAttributes = type.GetSingleCustomDerivedAttribute<InjectionRegistrationAttribute>();

                    if ( registrationAttributes.Any() && isEnvironmentCorrect ) {
                        var injectInListAttributes = type.GetSingleCustomAttribute<InjectInListAttribute>();

                        var registrationAttribute = registrationAttributes.First();
                        Type[] interfaceTypes;
                        if ( registrationAttribute.RegisteredInterfaces.Length > 0 ) {
                            if ( !registrationAttribute.RegisteredInterfaces.Contains( type ) ) {
                                interfaceTypes = new Type[ registrationAttribute.RegisteredInterfaces.Count() + 1 ];
                                interfaceTypes[ 0 ] = type;
                                registrationAttribute.RegisteredInterfaces.CopyTo( interfaceTypes, 1 );
                            } else {
                                interfaceTypes = registrationAttribute.RegisteredInterfaces;
                            }
                        } else {
                            if ( !type.GetInterfaces().Contains( type ) ) {
                                interfaceTypes = new Type[ type.GetInterfaces().Count() + 1 ];
                                interfaceTypes[ 0 ] = type;
                                type.GetInterfaces().CopyTo( interfaceTypes, 1 );
                            } else {
                                interfaceTypes = type.GetInterfaces();
                            }
                        }

                        var explicitOverriddenTypes = new HashSet<Type>( type.GetCustomAttribute<InjectionExplicitOverrideAttribute>().SelectMany( a => a.OverriddenTypes ) );

                        foreach ( var interfaceType in interfaceTypes ) {
                            CheckInterfaceType( interfaceType, type );

                            // Selects the type to which the interface is mapped currently
                            var registeredType = serviceCollection.FirstOrDefault( reg => reg.ServiceType == interfaceType )?.ImplementationType;

                            if ( registeredType != null && !type.IsSubclassOf( registeredType ) && !explicitOverriddenTypes.Contains( registeredType ) ) {

                                // Check whether this type was overridden explicitly
                                var isExplicitlyOverridden = registeredType.EvaluateCustomAttributePredicate<InjectionExplicitOverrideAttribute>( eoa => eoa.OverriddenTypes.Any( t => t.Equals( type ) ) );

                                if ( registeredType != type && !registeredType.IsSubclassOf( type ) && !isExplicitlyOverridden ) {
                                    // Only registrations of inherited types may be overwritten. An alternative branch to an already registered type may not be registered.
                                    throw new InjectionRegistrationException( interfaceType, type, $"Reflected interface {interfaceType.FullName} of {type.FullName} can not be registered, since it is already registered to {registeredType.FullName}, which is not a superclass." );
                                }
                            } else {
                                var listInterfaceTypes = injectInListAttributes.FirstOrDefault()?.RegisteredInterfaces ?? new Type[] { };

                                //foreach ( var listInterfaceType in listInterfaceTypes )
                                //	CheckInterfaceType( listInterfaceType, type );

                                switch ( registrationAttribute ) {
                                    case InjectAsGlobalSingletonAttribute _:
                                        serviceCollection.AddSingleton( interfaceType, type );

                                        //foreach ( var listInterfaceType in listInterfaceTypes )
                                        //	serviceCollection.AddSingleton( listInterfaceType, type );
                                        break;
                                    case InjectAsRequestSingletonAttribute _:
                                        serviceCollection.AddScoped( interfaceType, type );

                                        //foreach ( var listInterfaceType in listInterfaceTypes )
                                        //	serviceCollection.AddScoped( listInterfaceType, type );
                                        break;
                                    case InjectAsNewInstanceAttribute _:
                                        serviceCollection.AddTransient( interfaceType, type );

                                        //foreach ( var listInterfaceType in listInterfaceTypes )
                                        //	serviceCollection.AddTransient( listInterfaceType, type );
                                        break;
                                    default:
                                        // An implementation of IInjcetionScopeAttribute was given, but is not supported.
                                        throw new InjectionRegistrationException( interfaceType, type, $"Reflected interface {interfaceType.FullName} of {type.FullName} can not be registered, since InjectionRegistrationAttribute was invalid." );
                                }
                            }
                        }

                        if ( injectInListAttributes.Count == 1 ) {
                            var injectInListAttribute = injectInListAttributes.First();

                            var listInterfaceTypes = injectInListAttribute.RegisteredInterfaces;

                            foreach ( var interfaceType in listInterfaceTypes ) {
                                CheckInterfaceType( interfaceType, type );

                                _listInjectionRegistrationManager.RegisterTypeForListInterfaceType( type, interfaceType, injectInListAttribute.RemoveSubtypesFromList );
                                serviceCollection.AddSingleton<ListInjectionRegistrationManager>( _listInjectionRegistrationManager );


                                var listInjectionType = typeof( IEnumerable<> ).MakeGenericType( interfaceType );
                                var listInjectionProxyType = typeof( ListInjectionProxy<> ).MakeGenericType( interfaceType );
                                //if ( !MainContainer.IsRegistered( listInjectionType ) ) {
                                serviceCollection.AddTransient( listInjectionType, listInjectionProxyType );
                                //}
                            }
                        }
                    }
                }
            }
        }

        private void CheckInterfaceType( Type interfaceType, Type type ) {
            // TODO check via name, namespace, assembly and generic type arguments (via interfaces) if the interface types is equal to the type
            if ( !interfaceType.GetGenericArguments().Any() && !interfaceType.IsAssignableFrom( type ) ) {
                // A class may only be registered for type is declares. There might be an interface declared in the attribute which does not fit to the class.
                throw new InjectionRegistrationException( interfaceType, type, $"Type {interfaceType.FullName} of {type.FullName} can not be registered, since it does not implement this type." );
            }
        }
    }
}