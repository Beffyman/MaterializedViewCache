using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace MaterializedViewCache
{
	/// <summary>
	/// Interface for dependency injection
	/// </summary>
	public interface IViewCacheService
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
		/// Disposes of the service cache and makes it unusable
		/// </summary>
		void Dispose();
	}

	/// <summary>
	/// A service that can build and cache view models based on what parameters they were provided to be built with.
	/// </summary>
	public sealed class ViewCacheService : IViewCacheService, IDisposable
	{

		internal Factory ViewFactory;
		internal ConcurrentDictionary<Type, ConcurrentList<CachedView>> _cachedVMs;

		/// <summary>
		/// Default Constructor, use dependency injection if possible
		/// </summary>
		public ViewCacheService()
		{
			ViewFactory = new Factory();
			_cachedVMs = new ConcurrentDictionary<Type, ConcurrentList<CachedView>>();
		}
		/// <summary>
		/// Register a method and caller that will be mapped to the methods return type when using PropertyLookupDtoAttribute.
		/// </summary>
		/// <param name="method"></param>
		/// <param name="methodCaller"></param>
		public void Register(MethodInfo method, object methodCaller = null)
		{
			ViewFactory.Register(method, methodCaller);
		}

		internal CachedView GetView(Type type, Dictionary<string, object> Parameters)
		{
			if (_cachedVMs.ContainsKey(type))
			{
				var existingVM = _cachedVMs[type].SingleOrDefault(x => x.Parameters.DictEqual(Parameters));
				if (existingVM != null)
				{
					return existingVM;
				}
			}
			return null;
		}
		/// <summary>
		/// Get a cached VM of type with parameters.
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="Parameters"></param>
		/// <returns></returns>
		public T Get<T>(Dictionary<string, object> Parameters)
		{
			return (T)Get(typeof(T), Parameters);
		}
		/// <summary>
		/// Get a cached VM of type with parameters.
		/// </summary>
		/// <param name="type"></param>
		/// <param name="Parameters"></param>
		/// <returns></returns>
		public object Get(Type type, Dictionary<string, object> Parameters)
		{
			var cachedVm = GetView(type, Parameters);

			if(cachedVm != null)
			{
				return cachedVm.CachedVM;
			}
			else
			{
				var vm = ViewFactory.Build(type, Parameters);
				Cache(vm, type, Parameters);
				return vm;
			}
		}

		/// <summary>
		/// A VM of type with parameters has already been cached.
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="Parameters"></param>
		/// <returns></returns>
		public bool Exists<T>(Dictionary<string, object> Parameters)
		{
			return Exists(typeof(T), Parameters);
		}
		/// <summary>
		/// A VM of type with parameters has already been cached.
		/// </summary>
		/// <param name="type"></param>
		/// <param name="Parameters"></param>
		/// <returns></returns>
		public bool Exists(Type type, Dictionary<string, object> Parameters)
		{
			return Get(type, Parameters) != null;
		}


		/// <summary>
		/// Expires all VMs of type
		/// </summary>
		/// <typeparam name="T"></typeparam>
		public void ExpireVM<T>()
		{
			ExpireVM(typeof(T));
		}
		/// <summary>
		/// Expires all VMs of type
		/// </summary>
		/// <param name="type"></param>
		public void ExpireVM(Type type)
		{
			if (_cachedVMs.ContainsKey(type))
			{
				_cachedVMs[type].Clear();
			}
		}
		/// <summary>
		/// Expires a specific VM based on the parameters provided
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="Parameters"></param>
		public void ExpireVM<T>(Dictionary<string, object> Parameters)
		{
			ExpireVM(typeof(T), Parameters);
		}

		/// <summary>
		/// Expires a specific VM based on the parameters provided
		/// </summary>
		/// <param name="type"></param>
		/// <param name="Parameters"></param>
		public void ExpireVM(Type type, Dictionary<string, object> Parameters)
		{
			var vm = GetView(type, Parameters);
			if(vm != null)
			{
				_cachedVMs[type].Remove(vm);
			}
		}

		private void Cache(object vm, Type type, Dictionary<string, object> Parameters)
		{
			if (!_cachedVMs.ContainsKey(type))
			{
				_cachedVMs.TryAdd(type, new ConcurrentList<CachedView>());
			}

			_cachedVMs[type].Add(new CachedView
			{
				CachedType = type,
				CachedVM = vm,
				Parameters = Parameters
			});
		}
		/// <summary>
		/// Disposes of the service cache and makes it unusable
		/// </summary>
		public void Dispose()
		{
			ViewFactory.Dispose();
			ViewFactory = null;

			_cachedVMs.Clear();
			_cachedVMs = null;
		}
	}
}
