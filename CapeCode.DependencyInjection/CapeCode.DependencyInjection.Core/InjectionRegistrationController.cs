using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.ServiceModel;
using CapeCode.DependencyInjection.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace CapeCode.DependencyInjection {
    public class InjectionRegistrationController {

        public IServiceCollection ServiceCollection { get; private set; }

        public InjectionRegistrationController( IServiceCollection serviceCollection, IList<object> enumRestrictions = null ) {
            _enumRestrictions = enumRestrictions ?? new List<object>();
            ServiceCollection = serviceCollection;
            ServiceContracts = new List<Type>();
            ServiceCollection.AddSingleton<InjectionRegistrationController>( this );
            ServiceCollection.AddSingleton<ListInjectionRegistrationManager>( _listInjectionRegistrationManager );
            ServiceCollection.AddSingleton<IAssembliesCache>( _assembliesCache );
            ServiceCollection.AddSingleton<IServiceCollection>( serviceCollection );
            RegisterAllClasses( this.GetType().Assembly );
        }

        public IList<Type> ServiceContracts { get; private set; }

        private readonly IDictionary<Type, IList<Type>> _registrationsForDataContracts = new Dictionary<Type, IList<Type>>();

        private readonly ListInjectionRegistrationManager _listInjectionRegistrationManager = new ListInjectionRegistrationManager();

        private readonly IAssembliesCache _assembliesCache = new AssembliesCache();

        private readonly IList<object> _enumRestrictions;
        

        public void RegisterAllAssembliesInCurrentDirectory( params AssemblyInjectionTagAttribute[] requiredAssemblyInjectionTags ) {
            string localDirectory = Path.GetDirectoryName( Assembly.GetExecutingAssembly().Location );// Environment.CurrentDirectory;
            RegisterAllAssembliesInDirectory( localDirectory, requiredAssemblyInjectionTags );
        }

        public void RegisterAllAssembliesInDirectory( string directory, params AssemblyInjectionTagAttribute[] requiredAssemblyInjectionTags ) {
            Trace.WriteLine( "Used Directory: " + directory );
            Trace.WriteLine( "Environment.CurrentDirectory: " + Environment.CurrentDirectory );
            Trace.WriteLine( "Assembly.GetExecutingAssembly().Location: " + Path.GetDirectoryName( Assembly.GetExecutingAssembly().Location ) );
            var files = Directory.GetFiles( directory, "*.dll" ).Concat( Directory.GetFiles( directory, "*.exe" ) ).Select( Path.GetFullPath ).ToList();

            // HACK: the new Microsoft Team Foundation Server 11 libraries from VS 2012 add the "Microsoft.WITDataStore.dll" library to the bin folder which is not 64bit and therefore causes a BadImageFormat exception when being loaded...
            files = files.Where( f => !f.EndsWith( "Microsoft.WITDataStore.dll" ) ).ToList();

            var allAssemblies = new List<Assembly>();
            foreach ( var file in files ) {
                Trace.WriteLine( string.Format( "Loading Assembly {0}", file ) );
                try {
                    var assembly = Assembly.LoadFile( file );
                    allAssemblies.Add( assembly );
                } catch ( Exception ex ) {
                    throw new Exception( string.Format( "Used Directory: {0}, Current Directory:{1}, Executing Assembly Directory Location: {2} - Failed to load assembly {3}:{4}", directory, Environment.CurrentDirectory, Path.GetDirectoryName( Assembly.GetExecutingAssembly().Location ), file, ex.ToString() ) );
                }
            }
            RegisterAllAssemblies( allAssemblies, requiredAssemblyInjectionTags );
        }

        public void RegisterAllAssemblies( IList<Assembly> assemblies, params AssemblyInjectionTagAttribute[] requiredAssemblyInjectionTags ) {
            var requiredAssemblyInjectionTagTypeHash = requiredAssemblyInjectionTags != null && requiredAssemblyInjectionTags.Any() ? requiredAssemblyInjectionTags.Select( ait => ait.GetType() ).ToList() : null;

            foreach ( var assembly in assemblies ) {
                RegisterAllClasses( assembly, requiredAssemblyInjectionTagTypeHash );
            }
        }

        public void RegisterAllClasses( Assembly assembly, IEnumerable<Type> requiredAssemblyInjectionTagTypeHash = null ) {

            var isAssemblyRequired = true;
            if ( requiredAssemblyInjectionTagTypeHash != null ) {
                isAssemblyRequired = false;
                var assemblyInjectionTags = assembly.GetCustomAttributes( false ).Where( attr => attr.GetType().IsSubclassOf( typeof( AssemblyInjectionTagAttribute ) ) ).Cast<AssemblyInjectionTagAttribute>().ToList<AssemblyInjectionTagAttribute>();

                if ( assemblyInjectionTags.Any( ait => requiredAssemblyInjectionTagTypeHash.Contains( ait.GetType() ) ) ) {
                    isAssemblyRequired = true;
                }
            }

            if ( isAssemblyRequired && !_assembliesCache.Assemblies.Select( a => a.FullName ).Contains( assembly.FullName ) ) {
                _assembliesCache.Assemblies.Add( assembly );
                IList<Type> types;
                try {
                    types = assembly.GetTypes();
                } catch ( ReflectionTypeLoadException ex ) {
                    throw new Exception( string.Format( @"Loading the types {0} failed with: \n {1}", ex.Types != null ? string.Join( ", ", ex.Types.Where( t => t != null ).Select( t => t.FullName ) ) : "'NULL'", ex.LoaderExceptions != null ? string.Join( @"\n\n", ex.LoaderExceptions.Where( e => e != null ).Select( e => e.ToString() ) ) : "'NULL'" ), ex );
                }
                RegisterTypes( types );
            }
        }

        public void RegisterTypes( IList<Type> types ) {
            foreach ( Type type in types ) {
                RegisterType( type );
            }
        }

        public void RegisterType( Type type ) {
            if ( !type.IsInterface ) {
                if ( type.IsClass && !type.IsAbstract ) {
                    bool isEnvironmentCorrect = true;
                    IList<InjectionEnumRestrictionAttribute> enumRestrictionAttributes = type.GetCustomAttributes( false ).Where( attr => attr.GetType().IsSubclassOf( typeof( InjectionEnumRestrictionAttribute ) ) ).Cast<InjectionEnumRestrictionAttribute>().ToList<InjectionEnumRestrictionAttribute>();
                    // Filter attributes to have only attributes for arbitrary environments and attributes fitting to the current environment.
                    if ( enumRestrictionAttributes.Any() ) {
                        isEnvironmentCorrect = enumRestrictionAttributes.All( era => era.ValidEnumValues.Any( vev => _enumRestrictions.Any( ev => Equals( ev, vev ) ) ) );
                    }


                    IList<InjectionRegistrationAttribute> registrationAttributes = type.GetCustomAttributes( false ).Where( attr => attr.GetType().IsSubclassOf( typeof( InjectionRegistrationAttribute ) ) ).Cast<InjectionRegistrationAttribute>().ToList<InjectionRegistrationAttribute>();
                    if ( registrationAttributes.Count > 1 ) {
                        // Only one InjectAs... attribute is alowed.
                        throw new InjectionRegistrationException( null, type, "Type " + type.FullName + " can not be registered, since more than one InjectAs... attribute was found." );
                    }

                    if ( registrationAttributes.Count == 1 && isEnvironmentCorrect ) {

                        IList<InjectInListAttribute> injectInListAttributes = type.GetCustomAttributes( false ).Where( attr => attr is InjectInListAttribute ).Cast<InjectInListAttribute>().ToList<InjectInListAttribute>();
                        if ( registrationAttributes.Count > 1 ) {
                            // Only one InjectInList attribute is allowed.
                            throw new InjectionRegistrationException( null, type, "Type " + type.FullName + " can not be registered, since more than one InjectInList. attribute was found." );
                        }

                        InjectionRegistrationAttribute registrationAttribute = registrationAttributes.First();
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

                        var explicitOverriddenTypes = new HashSet<Type>();
                        {
                            IList<InjectionExplicitOverrideAttribute> explicitOverrideAttributesAttributes = type.GetCustomAttributes( false ).Where( attr => attr.GetType() == typeof( InjectionExplicitOverrideAttribute ) ).Cast<InjectionExplicitOverrideAttribute>().ToList<InjectionExplicitOverrideAttribute>();
                            if ( explicitOverrideAttributesAttributes.Count >= 1 ) {
                                foreach ( var explicitOverrideAttributesAttribute in explicitOverrideAttributesAttributes ) {
                                    foreach ( var overriddenType in explicitOverrideAttributesAttribute.OverriddenTypes ) {
                                        explicitOverriddenTypes.Add( overriddenType );
                                    }
                                }
                            }
                        }

                        foreach ( Type interfaceType in interfaceTypes ) {
                            // TODO check via name, namespace, assembly and generic type arguments (via interfaces) if the interface types is equal to the type
                            if ( !interfaceType.GetGenericArguments().Any() && !interfaceType.IsAssignableFrom( type ) ) {
                                // A class may only be registered for type is declares. There might be an interface declared in the attribute which does not fit to the class.
                                throw new InjectionRegistrationException( interfaceType, type, "Type " + interfaceType.FullName + " of " + type.FullName + " can not be registered, since it does not implement this type." );
                            }


                            //if ( interfaceType.IsInterface && interfaceType.IsDefined( typeof( ServiceContractAttribute ), false ) ) {
                            //    if ( !ServiceContracts.Contains( interfaceType ) ) {
                            //        bool serviceAlreadyRegistered = false;
                            //        foreach ( Type serviceContract in ServiceContracts ) {
                            //            if ( interfaceType.IsAssignableFrom( serviceContract ) ) {
                            //                serviceAlreadyRegistered = true;
                            //                break;
                            //            }
                            //        }
                            //        if ( !serviceAlreadyRegistered ) {
                            //            foreach ( Type serviceContract in ServiceContracts.ToList() ) {
                            //                if ( serviceContract.IsAssignableFrom( interfaceType ) ) {
                            //                    ServiceContracts.Remove( serviceContract );
                            //                }
                            //            }
                            //            ServiceContracts.Add( interfaceType );
                            //        }
                            //    }
                            //}

                            // Selects the type to which the interface is mapped currently
                            Type registeredType = ServiceCollection.Where( reg => reg.ServiceType == interfaceType ).Select( reg => reg.ImplementationType ).FirstOrDefault();

                            if ( registeredType != null && !type.IsSubclassOf( registeredType ) && !explicitOverriddenTypes.Contains( registeredType ) ) {

                                // Check whether this type was overridden explicitly
                                var isExplicitlyOverridden = false;
                                IList<InjectionExplicitOverrideAttribute> registeredTypeExplicitOverrideAttributesAttributes = registeredType.GetCustomAttributes( false ).Where( attr => attr.GetType() == typeof( InjectionExplicitOverrideAttribute ) ).Cast<InjectionExplicitOverrideAttribute>().ToList<InjectionExplicitOverrideAttribute>();
                                if ( registeredTypeExplicitOverrideAttributesAttributes.Count >= 1 ) {
                                    isExplicitlyOverridden = registeredTypeExplicitOverrideAttributesAttributes.Any( eoa => eoa.OverriddenTypes.Any( t => t.Equals( type ) ) );
                                }

                                if ( registeredType != type && !registeredType.IsSubclassOf( type ) && !isExplicitlyOverridden ) {
                                    // Only registrations of inherited types may be overwritten. An alternative branch to an already registered type may not be registered.
                                    throw new InjectionRegistrationException( interfaceType, type, "Reflected interface " + interfaceType.FullName + " of " + type.FullName + " can not be registered, since it is already registered to " + registeredType.FullName + ", which is not a superclass." );
                                }
                            } else {

                                if ( registrationAttribute.GetType() == typeof( InjectAsScopedSingleton ) ) {
                                    throw new NotImplementedException( ".net Core Dependency Injection does not support child containers." );
                                } else if ( registrationAttribute.GetType() == typeof( InjectAsThreadSingleton ) ) {
                                    throw new NotImplementedException( ".net Core Dependency Injection does not keep track of threads. Try using RequestScope based injection instead." );
                                } else if ( registrationAttribute.GetType() == typeof( InjectAsNewInstancePerResolve ) ) {
                                    throw new NotImplementedException( ".net Core Dependency Injection does not keep track of resolves. Try using RequestScope based injection instead." );
                                }

                                // Check what kind of injection is desired.
                                if ( registrationAttribute.GetType() == typeof( InjectAsGlobalSingleton ) ) {
                                    ServiceCollection.AddSingleton( interfaceType, type );
                                } else if ( registrationAttribute.GetType() == typeof( InjectAsRequestSingleton ) ) {
                                    ServiceCollection.AddScoped( interfaceType, type );
                                } else if ( registrationAttribute.GetType() == typeof( InjectAsNewInstance ) ) {
                                    ServiceCollection.AddTransient( interfaceType, type );
                                } else {
                                    // An implementation of IInjcetionScopeAttribute was given, but is not supported.
                                    throw new InjectionRegistrationException( interfaceType, type, "Reflected interface " + interfaceType.FullName + " of " + type.FullName + " can not be registered, since IInjcetionScopeAttribute was invalid." );
                                }
                            }
                        }

                        if ( injectInListAttributes.Count == 1 ) {
                            var injectInListAttribute = injectInListAttributes.First();

                            var listInterfaceTypes = injectInListAttribute.RegisteredInterfaces;

                            foreach ( Type interfaceType in listInterfaceTypes ) {
                                // TODO check via name, namespace, assembly and generic type arguments (via interfaces) if the interface types is equal to the type
                                if ( !interfaceType.GetGenericArguments().Any() && !interfaceType.IsAssignableFrom( type ) ) {
                                    // A class may only be registered for type is declares. There might be an interface declared in the attribute which does not fit to the class.
                                    throw new InjectionRegistrationException( interfaceType, type, "Type " + interfaceType.FullName + " of " + type.FullName + " can not be registered for list injection, since it does not implement this type." );
                                }

                                var listInjectionRegistrationManager = ServiceCollection.BuildServiceProvider().GetService<ListInjectionRegistrationManager>();
                                listInjectionRegistrationManager.RegisterTypeForListInterfaceType( type, interfaceType );


                                Type listInjectionType = typeof( IEnumerable<> ).MakeGenericType( interfaceType );
                                Type listInjectionProxyType = typeof( ListInjectionProxy<> ).MakeGenericType( interfaceType );
                                if ( !ServiceCollection.Any( sd => sd.ServiceType == listInjectionType ) ) {
                                    ServiceCollection.AddTransient( listInjectionType, listInjectionProxyType );
                                }
                            }
                        }
                    }
                }

                RegisterDataContracts( type );
            }
        }

        private void RegisterDataContracts( Type type, HashSet<Type> alreadyDone = null ) {
            if ( alreadyDone == null ) {
                alreadyDone = new HashSet<Type>();
            }

            if ( !alreadyDone.Contains( type ) ) {
                alreadyDone.Add( type );
                if ( type.IsDefined( typeof( DataContractAttribute ), false ) ) {

                    RegisterTypesImplementingTypes( type );

                    IList<KnownTypeAttribute> knownTypeAttributes = type.GetCustomAttributes( false ).Where( attr => attr.GetType().IsAssignableFrom( typeof( KnownTypeAttribute ) ) ).Cast<KnownTypeAttribute>().ToList<KnownTypeAttribute>();
                    foreach ( var knownTypeAttribute in knownTypeAttributes ) {
                        RegisterDataContracts( knownTypeAttribute.Type, alreadyDone );
                    }
                } else if ( type.IsDefined( typeof( SerializableAttribute ), false ) ) {
                    RegisterTypesImplementingTypes( type );
                }

            }
        }

        private void RegisterTypesImplementingTypes( Type type ) {
            IList<Type> interfaceTypes = type.GetInterfaces().ToList();
            Type inheritedType = type;
            while ( inheritedType != null && inheritedType != typeof( object ) ) {
                inheritedType = inheritedType.BaseType;
                interfaceTypes.Add( inheritedType );
            }

            foreach ( Type interfaceType in interfaceTypes ) {
                // TODO check via name, namespace, assembly and generic type arguments (via interfaces) if the interface types is equal to the type
                if ( !type.ContainsGenericParameters ) {


                    if ( !this._registrationsForDataContracts.ContainsKey( interfaceType ) ) {
                        this._registrationsForDataContracts[ interfaceType ] = new List<Type>();
                    }

                    if ( !this._registrationsForDataContracts[ interfaceType ].Contains( type ) ) {
                        this._registrationsForDataContracts[ interfaceType ].Add( type );
                    }
                }
            }
        }

        public IDependencyResolver GetDependencyResolver() {
            return ServiceCollection.BuildServiceProvider().GetService<IDependencyResolver>();
        }

        internal IDictionary<Type, IList<Type>> GetKnownTypeRegistrations() {
            return new Dictionary<Type, IList<Type>>( this._registrationsForDataContracts );
        }
    }
}
