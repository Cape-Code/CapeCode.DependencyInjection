using System;

namespace CapeCode.DependencyInjection.Interfaces {
    public interface IDependencyResolver {
        bool IsRegistered( Type type );
        object Resolve( Type type );
        bool IsRegistered<TType>();
        TType Resolve<TType>();

        bool IsGenericTypeRegistered( Type generictype, params Type[] typeArguments );
        object ResolveGenericType( Type generictype, params Type[] typeArguments );
        TType ResolveGenericType<TType>( Type generictype, params Type[] typeArguments );
    }
}
