using System;
using System.Linq;
using CapeCode.DependencyInjection.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace CapeCode.DependencyInjection {
    [InjectAsGlobalSingleton( typeof( IDependencyResolver ) )]
    public class DependencyResolver : IDependencyResolver {

        private readonly IServiceProvider _serviceProvider;
        private readonly IServiceCollection _serviceCollection;

        public DependencyResolver( IServiceProvider serviceProvider, IServiceCollection serviceCollection ) {
            _serviceProvider = serviceProvider;
            _serviceCollection = serviceCollection;
        }

        #region IDependencyResolver Members

        public bool IsRegistered( Type type ) {
            return _serviceCollection.Any( i => i.ServiceType == type );
        }

        public object Resolve( Type type ) {
            if ( type != _serviceProvider.GetType() ) {
                var instance = _serviceProvider.GetService( type );
                return instance;
            } else {
                return _serviceProvider;
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
