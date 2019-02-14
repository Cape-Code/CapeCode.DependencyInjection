using System.Reflection;
using CapeCode.DependencyInjection;

namespace Microsoft.Extensions.DependencyInjection.Core {
    public static class DependencyInjectionExtension {
		public static IEnumerable<T> GetCustomAttribute<T>( this Type t ) where T : Attribute {
			return t.GetCustomAttributes( false ).Where( attr => attr.GetType() == typeof( T ) ).Cast<T>();
		}

		public static IEnumerable<T> GetCustomDerivedAttribute<T>( this Type t ) where T : Attribute {
			return t.GetCustomAttributes( false ).Where( attr => attr.GetType().IsSubclassOf( typeof( T ) ) ).Cast<T>();
		}

		public static IList<T> GetSingleCustomAttribute<T>( this Type t ) where T : Attribute {
			var attributes = t.GetCustomAttribute<T>().ToList();

			ValidateAttributeList( attributes, t );

			return attributes;
		}

		public static IList<T> GetSingleCustomDerivedAttribute<T>( this Type t ) where T : Attribute {
			var attributes = t.GetCustomDerivedAttribute<T>().ToList();

			ValidateAttributeList( attributes, t );

			return attributes;
		}

		private static void ValidateAttributeList<T>( IList<T> attributes, Type t ) where T : Attribute {
			if ( attributes.Count > 1 ) {
				throw new InjectionRegistrationException( null, t, $"Type {t.FullName} can not be registered, since more than one {typeof( T ).Name} attribute was found." );
			}
		}

		public static bool EvaluateCustomAttributePredicate<T>( this Type t, Predicate<T> predicate, bool trueIfEmpty = false ) where T : Attribute {
			var list = t.GetCustomAttribute<T>().ToList();

			return trueIfEmpty && !list.Any() || list.Any() && list.All( a => predicate( a ) );
		}

		public static bool EvaluateDerivedCustomAttributePredicate<T>( this Type t, Predicate<T> predicate, bool trueIfEmpty = false ) where T : Attribute {
			var list = t.GetCustomDerivedAttribute<T>().ToList();

			return trueIfEmpty && !list.Any() || list.Any() && list.All( a => predicate( a ) );
		}

		public static void AddDependencyInjectionAssemblies( this IServiceCollection services, IList<object> enumRestrictions, params Assembly[] assemblies ) {
			var injectionRegistrationController = new InjectionRegistrationController( enumRestrictions );

			foreach ( var assembly in assemblies )
				injectionRegistrationController.RegisterAllClasses( services, assembly );
		}
    }
}
