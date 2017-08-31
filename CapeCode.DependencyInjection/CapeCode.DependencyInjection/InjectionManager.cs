using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using CapeCode.DependencyInjection.Interfaces;
using Microsoft.Practices.Unity;

namespace CapeCode.DependencyInjection {
    [InjectAsThreadSingleton( typeof( IInjectionManager ) )]
    public class InjectionManager : IInjectionManager {
        private IUnityContainer container;
        private InjectionRegistrationController injectionController;
        private Stack<KeyValuePair<Type, IUnityContainer>> scopeContainerHistoryStack;
        private IDictionary<Type, IList<Type>> knownTypesByDataContract;

        [InjectionConstructor]
        public InjectionManager( InjectionRegistrationController injectionController ) {
            this.injectionController = injectionController;
            this.container = injectionController.CreateContainer();
            this.scopeContainerHistoryStack = new Stack<KeyValuePair<Type, IUnityContainer>>();
            this.knownTypesByDataContract = injectionController.GetKnownTypeRegistrations();
        }

        #region IInjectionManager Members

        // Mapping IsRegistered to current container.
        public bool IsRegistered( Type type ) {
            return container.IsRegistered( type );
        }

        // Mapping typesave IsRegistered to current container.
        public bool IsRegistered<TType>() {
            return container.IsRegistered<TType>();
        }

        // Mapping resolve to current container.
        public object Resolve( Type type ) {
            return container.Resolve( type );
        }

        // Mapping typesave resolve to current container.
        public TType Resolve<TType>() {
            return container.Resolve<TType>();
        }

        // Mapping build up to current container.
        public object BuildUp( object instance ) {
            return container.BuildUp( instance.GetType(), instance );
        }


        // Mapping typesave build up to current container.
        public TType BuildUp<TType>( TType instance ) {
            return container.BuildUp<TType>( instance );
        }

        // A typesave call for AddScope. Add a scope to the current manager.
        public void AddScope<ScopeType>( ScopeType instance ) {
            AddScope( typeof( ScopeType ), instance );
        }

        // Mapping resolve known type to known type dictionary.
        public IList<Type> ResolveKnownType( Type type ) {
            if ( knownTypesByDataContract.ContainsKey( type ) ) {
                return knownTypesByDataContract[ type ];
            } else {
                return new List<Type>();
            }
        }

        // Mapping typesave resolve known type to known type dictionary.
        public IList<Type> ResolveKnownType<TType>() {
            return ResolveKnownType( typeof( TType ) );
        }

        public bool IsScopedFor<ScopeType>() {
            return IsScopedFor( typeof( ScopeType ) );
        }

        public bool IsScopedFor( Type scopeType ) {
            return scopeContainerHistoryStack.Any( kvp => kvp.Key == scopeType );
        }

        // Add a scope to the current manager.
        public void AddScope( Type scopeType, object instance ) {
            //var logger = this.Resolve<ILogger>();
            //logger.ParentType = GetType();
            //logger.LogTrace( "Add scope " + scopeType.Name + " for thread " + Thread.CurrentThread.ManagedThreadId + "." );

            // Check whether an istance is already added as scope for this type.
            if ( scopeContainerHistoryStack.Any( kvp => kvp.Key == scopeType ) ) {
                throw new InjectionScopeException( "An instance was already added as scope for this type in this manager." );
            } else {
                // The previous container is saved to be able to return to the previous state.
                scopeContainerHistoryStack.Push( new KeyValuePair<Type, IUnityContainer>( scopeType, container ) );
                // A child container is created. It containes all previous registrations.
                container = this.container.CreateChildContainer();
                // Registering the instance as a singleton for this container.
                container.RegisterInstance( scopeType, instance, new ExternallyControlledLifetimeManager() );
                // Let the injection registration controller register all the mappings related to the instance.
                injectionController.RegisterInstanceForScope( scopeType, instance, container );
            }
        }

        // A typesave call for RemoveScope. Remove a scope that was added before.
        public void RemoveScope<ScopeType>() {
            RemoveScope( typeof( ScopeType ) );
        }

        // Remove a scope that was added before.
        public void RemoveScope( Type scopeType ) {
            //var logger = this.Resolve<ILogger>();
            //logger.ParentType = GetType();
            //logger.LogTrace( "Remove scope " + scopeType.Name + " for thread " + Thread.CurrentThread.ManagedThreadId + ".", "RemoveScope" );

            // Check whether the istance was added as scope before.
            if ( !scopeContainerHistoryStack.Any( kvp => kvp.Key == scopeType ) ) {
                throw new InjectionScopeException( "Instance was not added as scope in this manager." );
            } else {
                // Remove all scopes that were added after the scope that is to be removed.
                Type lastScopeType;
                do {
                    // Dispose the current container.
                    container.Dispose();
                    // Pop the scope that was added last.
                    KeyValuePair<Type, IUnityContainer> lastScope = scopeContainerHistoryStack.Pop();
                    lastScopeType = lastScope.Key;
                    // Revert to the container before this scope was added.
                    container = lastScope.Value;
                    // Until the desired scope is found.
                } while ( lastScopeType != scopeType );
            }
        }

        // A typesave call for RemoveScopeRegistrationsForInstance. Remove a scope that was added before and remove all its registrations.
        public void RemoveScopeRegistrationsForInstance<ScopeType>() {
            RemoveScopeRegistrationsForInstance( typeof( ScopeType ) );
        }

        // Remove a scope that was added before and remove all its registrations.
        public void RemoveScopeRegistrationsForInstance( Type scopeType ) {
            //var logger = this.Resolve<ILogger>();
            //logger.ParentType = GetType();
            //logger.LogTrace( "Remove scope " + scopeType.Name + " and registrations for thread " + Thread.CurrentThread.ManagedThreadId + "." );

            // Check whether the istance was added as scope before.
            if ( !scopeContainerHistoryStack.Any( kvp => kvp.Key == scopeType ) ) {
                throw new InjectionScopeException( "Instance was not added as scope in this manager." );
            } else {
                // Remove and unregister all scopes that were added after the scope that is to be removed.
                Type lastScopeType;
                do {
                    // Dispose the current container.
                    container.Dispose();
                    // Pop the scope that was added last.
                    KeyValuePair<Type, IUnityContainer> lastScope = scopeContainerHistoryStack.Pop();
                    lastScopeType = lastScope.Key;
                    // Resolving the instance that was mapped to this type.
                    object lastScopeInstance = container.Resolve( lastScopeType );
                    // Revert to the container before this scope was added.
                    container = lastScope.Value;
                    // Remove all registrations for this scope.
                    injectionController.UnregisterInstanceForScope( lastScopeType, lastScopeInstance );
                    // Until the desired scope is found.
                } while ( lastScopeType != scopeType );
            }
        }

        #endregion
    }
}
