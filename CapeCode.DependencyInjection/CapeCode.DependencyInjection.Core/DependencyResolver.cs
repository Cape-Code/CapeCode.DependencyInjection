using System;
using System.Linq;
using CapeCode.DependencyInjection.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Core;

namespace CapeCode.DependencyInjection.Core {
    [InjectAsGlobalSingleton( typeof( IDependencyResolver ) )]
    public class DependencyResolver : IDependencyResolver {

        private readonly IServiceProvider _serviceProvider;

        public DependencyResolver( IServiceProvider serviceProvider ) {
            _serviceProvider = serviceProvider;
        }

        #region IDependencyResolver Members

        public bool IsRegistered( Type type ) {
            return _serviceProvider.GetService( type ) != null;
        }

        public object Resolve( Type type ) {
            return _serviceProvider.GetRequiredService( type );
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
