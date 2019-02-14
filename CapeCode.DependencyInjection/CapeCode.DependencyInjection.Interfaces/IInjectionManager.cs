using System;
using System.Collections.Generic;

namespace CapeCode.DependencyInjection.Interfaces {
    [Obsolete]
    public interface IInjectionManager {

        object Resolve( Type type );
        TType Resolve<TType>();

        bool IsRegistered( Type type );
        bool IsRegistered<TType>();

        object BuildUp( object instance );
        TType BuildUp<TType>( TType instance );

        IList<Type> ResolveKnownType( Type type );
        IList<Type> ResolveKnownType<TType>();

        bool IsScopedFor<ScopeType>();
        bool IsScopedFor( Type scopeType );


        void AddScope<ScopeType>( ScopeType instance );
        void AddScope( Type scopeType, object instance );

        void RemoveScope<ScopeType>();
        void RemoveScope( Type scopeType );

        void RemoveScopeRegistrationsForInstance<ScopeType>();
        void RemoveScopeRegistrationsForInstance( Type scopeType );
    }
}
