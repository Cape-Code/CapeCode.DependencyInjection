using System;
using CapeCode.DependencyInjection.Interfaces;

namespace CapeCode.DependencyInjection {
    [InjectAsGlobalSingleton( typeof( IDependencyResolver ) )]
    public class DependencyResolver : IDependencyResolver {

        private readonly IInjectionManager _injectionManager;
        private readonly Type _injectionManagerType;

        public DependencyResolver( IInjectionManager injectionManager ) {
            _injectionManager = injectionManager;
            _injectionManagerType = typeof( InjectionManager );
        }

        #region IDependencyResolver Members

        public bool IsRegistered( Type type ) {
            var threadbasedInjectionManager = _injectionManager.Resolve<IInjectionManager>();

            return threadbasedInjectionManager.IsRegistered( type );
        }

        public object Resolve( Type type ) {
            var threadbasedInjectionManager = _injectionManager.Resolve<IInjectionManager>();

            if ( type != _injectionManagerType ) {
                var instance = threadbasedInjectionManager.Resolve( type );
                return instance;
            } else {
                return threadbasedInjectionManager;
            }
        }

        public bool IsRegistered<TType>() {
            return IsRegistered( typeof( TType ) );
        }

        public TType Resolve<TType>() {
            return ( TType ) Resolve( typeof( TType ) );
        }

        public bool IsGenericTypeRegistered( Type generictype, params Type[] typeArguments ) {
            Type constructedType = generictype.MakeGenericType( typeArguments );
            return IsRegistered( constructedType );
        }

        public object ResolveGenericType( Type generictype, params Type[] typeArguments ) {
            Type constructedType = generictype.MakeGenericType( typeArguments );
            return Resolve( constructedType );
        }

        public TType ResolveGenericType<TType>( Type generictype, params Type[] typeArguments ) {
            return ( TType ) ResolveGenericType( generictype, typeArguments );
        }

        #endregion
    }
}
