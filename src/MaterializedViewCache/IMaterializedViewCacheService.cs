using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace MaterializedViewCache
{
	/// <summary>
	/// Interface for dependency injection
	/// </summary>
	public interface IMaterializedViewCacheService
	{
		/// <summary>
		/// Register a method and caller that will be mapped to the methods return type when using PropertyLookupDtoAttribute.
		/// </summary>
		/// <param name="method"></param>
		/// <param name="methodCaller"></param>
		void Register(MethodInfo method, object methodCaller = null);

		/// <summary>
		/// Get a cached VM of type with parameters.
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="Parameters"></param>
		/// <returns></returns>
		T Get<T>(Dictionary<string, object> Parameters);
		/// <summary>
		/// Get a cached VM of type with parameters.
		/// </summary>
		/// <param name="type"></param>
		/// <param name="Parameters"></param>
		/// <returns></returns>
		object Get(Type type, Dictionary<string, object> Parameters);

		/// <summary>
		/// A VM of type with parameters has already been cached.
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="Parameters"></param>
		/// <returns></returns>
		bool Exists<T>(Dictionary<string, object> Parameters);
		/// <summary>
		/// A VM of type with parameters has already been cached.
		/// </summary>
		/// <param name="type"></param>
		/// <param name="Parameters"></param>
		/// <returns></returns>
		bool Exists(Type type, Dictionary<string, object> Parameters);

		/// <summary>
		/// Expires all VMs of type
		/// </summary>
		/// <typeparam name="T"></typeparam>
		void ExpireVM<T>();
		/// <summary>
		/// Expires all VMs of type
		/// </summary>
		/// <param name="type"></param>
		void ExpireVM(Type type);

		/// <summary>
		/// Expires a specific VM based on the parameters provided
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="Parameters"></param>
		void ExpireVM<T>(Dictionary<string, object> Parameters);
		/// <summary>
		/// Expires a specific VM based on the parameters provided
		/// </summary>
		/// <param name="type"></param>
		/// <param name="Parameters"></param>
		void ExpireVM(Type type, Dictionary<string, object> Parameters);

		/// <summary>
		/// Completely wipes out all stored view data, keeps registered methods
		/// </summary>
		void Clean();

		/// <summary>
		/// Disposes of the service cache and makes it unusable
		/// </summary>
		void Dispose();
	}
}
