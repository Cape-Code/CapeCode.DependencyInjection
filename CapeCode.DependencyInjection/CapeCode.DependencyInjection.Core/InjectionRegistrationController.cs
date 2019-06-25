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
        private IList<DependencyInjectionRegistration> _registrations = new List<DependencyInjectionRegistration>();

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

                    var registrationAttributes = type.GetCustomDerivedAttribute<InjectionRegistrationAttribute>()
                        .OrderByDescending( i => i.RegisteredInterfaces.Count() )
                        .ToList();

                    if ( registrationAttributes.Any() && isEnvironmentCorrect ) {
                        var injectInListAttributes = type.GetSingleCustomAttribute<InjectInListAttribute>();
                        var alreadyRegisteredTypes = new HashSet<Type>();

                        foreach ( var registrationAttribute in registrationAttributes ) {

                            IList<Type> interfaceTypes = registrationAttribute.RegisteredInterfaces;
                            if ( interfaceTypes.Any() ) {
                                var repeatedlyRegisteredTypes = interfaceTypes.Where( i => alreadyRegisteredTypes.Contains( i ) ).ToList();
                                if ( repeatedlyRegisteredTypes.Any() ) {
                                    throw new InjectionRegistrationException( repeatedlyRegisteredTypes.First(), type, $"Reflected interfaces [{string.Join( ", ", repeatedlyRegisteredTypes.Select( i => i.FullName ) )}] of {type.FullName} cannot be registered, since they were already registered for the same type with another attribute." );
                                }
                                //    if ( !registrationAttribute.RegisteredInterfaces.Contains( type ) ) {
                                //        interfaceTypes = new Type[ registrationAttribute.RegisteredInterfaces.Count() + 1 ];
                                //        interfaceTypes[ 0 ] = type;
                                //        registrationAttribute.RegisteredInterfaces.CopyTo( interfaceTypes, 1 );
                                //    } else {
                                //        interfaceTypes = registrationAttribute.RegisteredInterfaces;
                                //    }
                            } else {
                                interfaceTypes = type.GetInterfaces().Where( i => !alreadyRegisteredTypes.Contains( i ) ).ToList();
                                if ( !interfaceTypes.Contains( type ) ) {
                                    interfaceTypes.Insert( 0, type );
                                }
                                interfaceTypes = interfaceTypes.Where( i => !alreadyRegisteredTypes.Contains( i ) ).ToList();
                            }

                            var explicitOverriddenTypes = new HashSet<Type>( type.GetCustomAttribute<InjectionExplicitOverrideAttribute>().SelectMany( a => a.OverriddenTypes ) );
                            DependencyInjectionRegistration firstRegistration = null;

                            foreach ( var interfaceType in interfaceTypes ) {
                                CheckInterfaceType( interfaceType, type );

                                // Selects the type to which the interface is mapped currently
                                var existingRegistration = _registrations.SingleOrDefault( i => i.RegisteredInterface == interfaceType );
                                if ( existingRegistration != null && !type.IsSubclassOf( existingRegistration.RegisteredToType ) && !explicitOverriddenTypes.Contains( existingRegistration.RegisteredToType ) ) {
                                    // Check whether this type was overridden explicitly
                                    var isExplicitlyOverridden = existingRegistration.RegisteredToType.EvaluateCustomAttributePredicate<InjectionExplicitOverrideAttribute>( eoa => eoa.OverriddenTypes.Any( t => t.Equals( type ) ) );

                                    if ( existingRegistration.RegisteredToType != type && !existingRegistration.RegisteredToType.IsSubclassOf( type ) && !isExplicitlyOverridden ) {
                                        // Only registrations of inherited types may be overwritten. An alternative branch to an already registered type may not be registered.
                                        throw new InjectionRegistrationException( interfaceType, type, $"Reflected interface {interfaceType.FullName} of {type.FullName} cannot be registered, since it is already registered to {existingRegistration.RegisteredToType.FullName}, which is not a superclass." );
                                    }
                                    if ( firstRegistration == null ) {
                                        firstRegistration = existingRegistration;
                                    }
                                } else {
                                    var registration = new DependencyInjectionRegistration {
                                        RegisteredInterface = interfaceType,
                                        RegisteredToType = type,
                                        UsedOtherRegistration = firstRegistration
                                    };
                                    _registrations.Add( registration );

                                    switch ( registrationAttribute ) {
                                        case InjectAsGlobalSingletonAttribute _:
                                            registration.Lifetime = ServiceLifetime.Singleton;
                                            if ( firstRegistration == null ) {
                                                serviceCollection.AddSingleton( interfaceType, type );
                                            } else {
                                                serviceCollection.AddSingleton( interfaceType, sp => sp.GetService( firstRegistration.RegisteredInterface ) );
                                            }
                                            break;
                                        case InjectAsRequestSingletonAttribute _:
                                            registration.Lifetime = ServiceLifetime.Scoped;
                                            if ( firstRegistration == null ) {
                                                serviceCollection.AddScoped( interfaceType, type );
                                            } else {
                                                serviceCollection.AddScoped( interfaceType, sp => sp.GetService( firstRegistration.RegisteredInterface ) );
                                            }
                                            break;
                                        case InjectAsNewInstanceAttribute _:
                                            registration.Lifetime = ServiceLifetime.Transient;
                                            if ( firstRegistration == null ) {
                                                serviceCollection.AddTransient( interfaceType, type );
                                            } else {
                                                serviceCollection.AddTransient( interfaceType, sp => sp.GetService( firstRegistration.RegisteredInterface ) );
                                            }
                                            break;
                                        default:
                                            // An implementation of IInjcetionScopeAttribute was given, but is not supported.
                                            throw new InjectionRegistrationException( interfaceType, type, $"Reflected interface {interfaceType.FullName} of {type.FullName} cannot be registered, since InjectionRegistrationAttribute was invalid." );
                                    }

                                    if ( existingRegistration != null && ( type.IsSubclassOf( existingRegistration.RegisteredToType ) || explicitOverriddenTypes.Contains( existingRegistration.RegisteredToType ) ) ) {
                                        var existingRegistrationChildren = existingRegistration.UsedByOtherRegistrations.ToList();
                                        foreach ( var existingRegistrationChild in existingRegistrationChildren ) {
                                            CheckInterfaceType( existingRegistrationChild.RegisteredInterface, type );

                                            existingRegistrationChild.RegisteredToType = type;
                                            existingRegistrationChild.UsedOtherRegistration = registration;
                                            registration.UsedByOtherRegistrations.Add( existingRegistrationChild );
                                        }
                                        _registrations.Remove( existingRegistration );
                                    }

                                    if ( firstRegistration == null ) {
                                        firstRegistration = registration;
                                    } else {
                                        firstRegistration.UsedByOtherRegistrations.Add( registration );
                                    }
                                }
                            }

                            if ( injectInListAttributes.Count == 1 ) {
                                var injectInListAttribute = injectInListAttributes.First();

                                var listInterfaceTypes = injectInListAttribute.RegisteredInterfaces;

                                if ( !serviceCollection.Any( reg => reg.ServiceType == typeof( ListInjectionRegistrationManager ) ) ) {
                                    serviceCollection.AddSingleton( _listInjectionRegistrationManager );
                                }

                                foreach ( var interfaceType in listInterfaceTypes ) {
                                    CheckInterfaceType( interfaceType, type );

                                    _listInjectionRegistrationManager.RegisterTypeForListInterfaceType( type, interfaceType, injectInListAttribute.RemoveSubtypesFromList );

                                    var listInjectionType = typeof( IEnumerable<> ).MakeGenericType( interfaceType );
                                    if ( !serviceCollection.Any( reg => reg.ServiceType == listInjectionType ) ) {
                                        var listInjectionProxyType = typeof( ListInjectionProxy<> ).MakeGenericType( interfaceType );
                                        serviceCollection.AddTransient( listInjectionType, listInjectionProxyType );
                                    }
                                }
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

        private class DependencyInjectionRegistration {
            public Type RegisteredInterface { get; set; }
            public Type RegisteredToType { get; set; }
            public ServiceLifetime Lifetime { get; set; }
            public DependencyInjectionRegistration UsedOtherRegistration { get; set; }
            public IList<DependencyInjectionRegistration> UsedByOtherRegistrations { get; private set; } = new List<DependencyInjectionRegistration>();

        }
    }
}