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
using CapeCode.ExtensionMethods;
using Microsoft.Practices.Unity;

namespace CapeCode.DependencyInjection {
    public class InjectionRegistrationController {

        public IUnityContainer MainContainer { get; private set; }

        public InjectionRegistrationController( IList<object> enumRestrictions = null, Type requestScopeType = null ) {
            _enumRestrictions = enumRestrictions ?? new List<object>();
            _requestScopeType = requestScopeType;
            MainContainer = new UnityContainer();
            ServiceContracts = new List<Type>();
            MainContainer.RegisterInstance<InjectionRegistrationController>( this, new ExternallyControlledLifetimeManager() );
            MainContainer.RegisterInstance<IAssembliesCache>( _assembliesCache );
            RegisterAllClasses( this.GetType().Assembly );
        }

        public IList<Type> ServiceContracts { get; private set; }

        private readonly IDictionary<Type, IList<Type>> _registrationsForDataContracts = new Dictionary<Type, IList<Type>>();

        private readonly IAssembliesCache _assembliesCache = new AssembliesCache();

        private readonly IList<object> _enumRestrictions;
        private readonly Type _requestScopeType;

        // Manages the registration for Interfaces that inject a singelton depending on a given scope object.
        // First Type ist the type of the scope object; Second Type is the type of the interface
        private readonly IDictionary<Type, IDictionary<Type, InstanceDependendScopeRegistration>> _registrationsForInterfacesForScopeTypes = new Dictionary<Type, IDictionary<Type, InstanceDependendScopeRegistration>>();

        // Manages the LifetimeManagers for Interfaces depending on a scope object
        // The object is the object that stores are registered for; second is a registration for a type that depends on the instance
        private readonly IDictionary<object, IDictionary<InstanceDependendScopeRegistration, LifetimeManager>> _lifetimeManagersForRegistrationsForScopeObjects = new ConcurrentDictionary<object, IDictionary<InstanceDependendScopeRegistration, LifetimeManager>>();

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
            foreach ( var type in types ) {
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

                        foreach ( var interfaceType in interfaceTypes ) {
                            // TODO check via name, namespace, assembly and generic type arguments (via interfaces) if the interface types is equal to the type
                            if ( !interfaceType.GetGenericArguments().Any() && !interfaceType.IsAssignableFrom( type ) ) {
                                // A class may only be registered for type is declares. There might be an interface declared in the attribute which does not fit to the class.
                                throw new InjectionRegistrationException( interfaceType, type, "Type " + interfaceType.FullName + " of " + type.FullName + " can not be registered, since it does not implement this type." );
                            }


                            if ( interfaceType.IsInterface && interfaceType.IsDefined( typeof( ServiceContractAttribute ), false ) ) {
                                if ( !ServiceContracts.Contains( interfaceType ) ) {
                                    bool serviceAlreadyRegistered = false;
                                    foreach ( var serviceContract in ServiceContracts ) {
                                        if ( interfaceType.IsAssignableFrom( serviceContract ) ) {
                                            serviceAlreadyRegistered = true;
                                            break;
                                        }
                                    }
                                    if ( !serviceAlreadyRegistered ) {
                                        foreach ( var serviceContract in ServiceContracts.ToList() ) {
                                            if ( serviceContract.IsAssignableFrom( interfaceType ) ) {
                                                ServiceContracts.Remove( serviceContract );
                                            }
                                        }
                                        ServiceContracts.Add( interfaceType );
                                    }
                                }
                            }

                            // Selects the type to which the interface is mapped currently
                            var registeredType = MainContainer.Registrations.ToList().Where( reg => reg.RegisteredType == interfaceType ).Select( reg => reg.MappedToType ).FirstOrDefault();

                            Type registeredForType = null;

                            // Search all scoped registration for a mapping.
                            foreach ( var scopeType in this._registrationsForInterfacesForScopeTypes.Keys ) {
                                var scopingedRegistration = this._registrationsForInterfacesForScopeTypes[ scopeType ];
                                if ( scopingedRegistration.ContainsKey( interfaceType ) ) {
                                    if ( registeredType == null ) {
                                        registeredType = scopingedRegistration[ interfaceType ].RegisteredToClass;
                                        registeredForType = scopeType;
                                    } else {
                                        throw new InjectionRegistrationException( interfaceType, type, "Reflected interface " + interfaceType.FullName + " is already registered to multiple tyes." );
                                    }
                                }
                            }

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

                                // Check what kind of injection is desired.
                                if ( registrationAttribute.GetType() == typeof( InjectAsScopedSingletonAttribute ) ) {
                                    var scopedSingeltonAttribute = ( InjectAsScopedSingletonAttribute ) registrationAttribute;
                                    var scopeType = scopedSingeltonAttribute.ScopeRelatedTo;
                                    RegisterTypeForScope( type, interfaceType, registeredType, registeredForType, scopeType );
                                } else if ( registrationAttribute.GetType() == typeof( InjectAsRequestSingletonAttribute ) ) {
                                    RegisterTypeForScope( type, interfaceType, registeredType, registeredForType, _requestScopeType );
                                } else if ( registrationAttribute.GetType() == typeof( InjectAsGlobalSingletonAttribute ) ) {
                                    MainContainer.RegisterType( interfaceType, type, new ContainerControlledLifetimeManager() );
                                } else if ( registrationAttribute.GetType() == typeof( InjectAsThreadSingletonAttribute ) ) {
                                    MainContainer.RegisterType( interfaceType, type, new PerThreadLifetimeManager() );
                                } else if ( registrationAttribute.GetType() == typeof( InjectAsNewInstancePerResolveAttribute ) ) {
                                    MainContainer.RegisterType( interfaceType, type, new PerResolveLifetimeManager() );
                                } else if ( registrationAttribute.GetType() == typeof( InjectAsNewInstanceAttribute ) ) {
                                    MainContainer.RegisterType( interfaceType, type );
                                } else {
                                    // An implementation of IInjcetionScopeAttribute was given, but is not supported.
                                    throw new InjectionRegistrationException( interfaceType, type, "Reflected interface " + interfaceType.FullName + " of " + type.FullName + " can not be registered, since IInjcetionScopeAttribute was invalid." );
                                }
                            }
                        }

                        if ( injectInListAttributes.Count == 1 ) {
                            var injectInListAttribute = injectInListAttributes.First();

                            var listInterfaceTypes = injectInListAttribute.RegisteredInterfaces;

                            foreach ( var interfaceType in listInterfaceTypes ) {
                                // TODO check via name, namespace, assembly and generic type arguments (via interfaces) if the interface types is equal to the type
                                if ( !interfaceType.GetGenericArguments().Any() && !interfaceType.IsAssignableFrom( type ) ) {
                                    // A class may only be registered for type is declares. There might be an interface declared in the attribute which does not fit to the class.
                                    throw new InjectionRegistrationException( interfaceType, type, "Type " + interfaceType.FullName + " of " + type.FullName + " can not be registered for list injection, since it does not implement this type." );
                                }

                                var listInjectionRegistrationManager = MainContainer.Resolve<ListInjectionRegistrationManager>();
                                listInjectionRegistrationManager.RegisterTypeForListInterfaceType( type, interfaceType, injectInListAttribute.RemoveSubtypesFromList );

                                var listInjectionType = typeof( IEnumerable<> ).MakeGenericType( interfaceType );
                                var listInjectionProxyType = typeof( ListInjectionProxy<> ).MakeGenericType( interfaceType );
                                if ( !MainContainer.IsRegistered( listInjectionType ) ) {
                                    MainContainer.RegisterType( listInjectionType, listInjectionProxyType, new PerResolveLifetimeManager() );
                                }
                            }
                        }
                    }
                }

                RegisterDataContracts( type );
            }
        }

        private void RegisterTypeForScope( Type type, Type interfaceType, Type registeredType, Type registeredForType, Type scopeType ) {
            // Interfaces may only be scoped for one type to prevent confusion
            if ( registeredForType != null && registeredForType != scopeType ) {
                // Only registrations of inherited types may be overwritten. An alternative branch to an already registered type may not be registered.
                throw new InjectionRegistrationException( interfaceType, type, "Reflected interface " + interfaceType.FullName + " of " + type.FullName + " can not be registered for scope " + scopeType + ", since it is already registered to " + registeredType.FullName + ", for scope " + registeredForType + "." );
            }
            if ( !this._registrationsForInterfacesForScopeTypes.ContainsKey( scopeType ) ) {
                this._registrationsForInterfacesForScopeTypes[ scopeType ] = new Dictionary<Type, InstanceDependendScopeRegistration>();
            }

            this._registrationsForInterfacesForScopeTypes[ scopeType ][ interfaceType ] = new InstanceDependendScopeRegistration( registeredInterface: interfaceType, registeredToClass: type );
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

            foreach ( var interfaceType in interfaceTypes ) {
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

        internal IUnityContainer CreateContainer() {
            return MainContainer.CreateChildContainer();
        }

        public IInjectionManager GetInjectionManager() {
            return MainContainer.Resolve<IInjectionManager>();
        }

        internal IDictionary<Type, IList<Type>> GetKnownTypeRegistrations() {
            return new Dictionary<Type, IList<Type>>( this._registrationsForDataContracts );
        }

        public void RegisterInstanceForScope<ScopeType>( ScopeType instance, IUnityContainer container ) {
            RegisterInstanceForScope( typeof( ScopeType ), instance, container );
        }

        public void RegisterInstanceForScope( Type scopeType, object instance, IUnityContainer container ) {
            if ( this._registrationsForInterfacesForScopeTypes.ContainsKey( scopeType ) ) {
                // Initialize the registration to lifetime manager dictionary for this instance if it was not added as scope before.
                if ( !this._lifetimeManagersForRegistrationsForScopeObjects.ContainsKey( instance ) ) {
                    this._lifetimeManagersForRegistrationsForScopeObjects[ instance ] = new ConcurrentDictionary<InstanceDependendScopeRegistration, LifetimeManager>();
                }
#if DEBUG
                var containerRegistrations = container.Registrations;

                var registrationsByNameAndType = containerRegistrations.Where( r => r.Name == null ).ToDictionaryDictionary( k => k.RegisteredType, k => k.Name, v => v.RegisteredType );
                var registrationsByType = containerRegistrations.Where( r => r.Name == null ).ToDictionary( k => k.RegisteredType, v => v.RegisteredType );
#endif

                // Add a registration to the container for each registered scoped type.
                foreach ( InstanceDependendScopeRegistration registration in this._registrationsForInterfacesForScopeTypes[ scopeType ].Values ) {
                    // Initialize the lifetime manager for the registration for this instance if it was not created for this instance as scope before.
                    if ( !this._lifetimeManagersForRegistrationsForScopeObjects[ instance ].ContainsKey( registration ) ) {
                        this._lifetimeManagersForRegistrationsForScopeObjects[ instance ][ registration ] = new ContainerControlledLifetimeManager();
                    }

                    if ( registration.RegisteredForName != null ) {
# if DEBUG
                        // Select the type to which the interface is mapped currently.
                        if ( registrationsByNameAndType.ContainsKey( registration.RegisteredInterface ) && registrationsByNameAndType[ registration.RegisteredInterface ].ContainsKey( registration.RegisteredForName ) ) {
                            // An interface may only be registered once per scope object.
                            throw new InjectionRegistrationException( registration.RegisteredInterface, registration.RegisteredToClass, "Reflected interface " + registration.RegisteredInterface.FullName + " can not be registered for scope, since it is already registered to " + registrationsByNameAndType[ registration.RegisteredInterface ][ registration.RegisteredForName ].FullName + " in this container." );
                        }

                        // remember the registration
                        if ( registrationsByNameAndType.ContainsKey( registration.RegisteredInterface ) ) {
                            registrationsByNameAndType.Add( registration.RegisteredInterface, new Dictionary<string, Type>() );
                        }
                        registrationsByNameAndType[ registration.RegisteredInterface ].Add( registration.RegisteredForName, registration.RegisteredToClass );
# endif
                        // Reuse the previously registered lifetime manager for this instance.
                        container.RegisterType( registration.RegisteredInterface, registration.RegisteredToClass, registration.RegisteredForName, new InstanceBasedScopeLifetimeManager( this._lifetimeManagersForRegistrationsForScopeObjects[ instance ][ registration ] ) );
                    } else {
# if DEBUG
                        // Select the type to which the interface is mapped currently.
                        if ( registrationsByType.ContainsKey( registration.RegisteredInterface ) ) {
                            // An interface may only be registered once per scope object.
                            throw new InjectionRegistrationException( registration.RegisteredInterface, registration.RegisteredToClass, "Reflected interface " + registration.RegisteredInterface.FullName + " can not be registered for scope, since it is already registered to " + registrationsByType[ registration.RegisteredInterface ].FullName + " in this container." );
                        }

                        // remember the registration
                        registrationsByType.Add( registration.RegisteredInterface, registration.RegisteredToClass );
# endif
                        // Reuse the previously registered lifetime manager for this instance.
                        container.RegisterType( registration.RegisteredInterface, registration.RegisteredToClass, new InstanceBasedScopeLifetimeManager( this._lifetimeManagersForRegistrationsForScopeObjects[ instance ][ registration ] ) );
                    }
                }
            }
        }

        public void UnregisterInstanceForScope<ScopeType>( ScopeType instance ) {
            UnregisterInstanceForScope( typeof( ScopeType ), instance );
        }

        public void UnregisterInstanceForScope( Type scopeType, object instance ) {
            if ( this._registrationsForInterfacesForScopeTypes.ContainsKey( scopeType ) ) {
                // Check whether any lifetime manager was registered.
                if ( this._lifetimeManagersForRegistrationsForScopeObjects.ContainsKey( instance ) ) {
                    // Remove the stored lifetime managers for each registered scoped type.
                    foreach ( InstanceDependendScopeRegistration registration in this._registrationsForInterfacesForScopeTypes[ scopeType ].Values ) {
                        if ( this._lifetimeManagersForRegistrationsForScopeObjects[ instance ].ContainsKey( registration ) ) {
                            // Remove the value of the lifetime manager first and then remove it from the dictionary.
                            this._lifetimeManagersForRegistrationsForScopeObjects[ instance ][ registration ].RemoveValue();
                            this._lifetimeManagersForRegistrationsForScopeObjects[ instance ].Remove( registration );
                        }
                    }
                }
                // Remove the entry for lifetime managers for the instance from the dictionary.
                this._lifetimeManagersForRegistrationsForScopeObjects.Remove( instance );
            }
        }

        // This class stores registrations needing a scope, since they cannot be inserted into the container directly.
        class InstanceDependendScopeRegistration {
            internal Type RegisteredInterface { get; set; }
            internal Type RegisteredToClass { get; set; }
            internal string RegisteredForName { get; set; }

            public InstanceDependendScopeRegistration( Type registeredInterface, Type registeredToClass, string registeredForName ) {
                RegisteredInterface = registeredInterface;
                RegisteredToClass = registeredToClass;
                RegisteredForName = registeredForName;
            }

            public InstanceDependendScopeRegistration( Type registeredInterface, Type registeredToClass ) {
                RegisteredInterface = registeredInterface;
                RegisteredToClass = registeredToClass;
                RegisteredForName = null;
            }
        }
    }

    // Each liftime manager can only be registered to one container, so a proxy lifetime manager is used.
    public class InstanceBasedScopeLifetimeManager : LifetimeManager {
        private LifetimeManager instanceStore;

        public InstanceBasedScopeLifetimeManager( LifetimeManager store ) {
            this.instanceStore = store;
        }

        public override object GetValue() {
            return instanceStore.GetValue();
        }

        public override void RemoveValue() {
            instanceStore.RemoveValue();
        }

        public override void SetValue( object newValue ) {
            instanceStore.SetValue( newValue );
        }
    }
}
