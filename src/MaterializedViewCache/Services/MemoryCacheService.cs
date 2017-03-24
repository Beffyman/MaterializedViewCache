using MaterializedViewCache.Services.Memory;
using MaterializedViewCache.Settings;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace MaterializedViewCache.Services
{
	/// <summary>
	/// A service that can build and cache view models based on what parameters they were provided to be built with.
	/// </summary>
	public sealed class MemoryCacheService : IMaterializedViewCacheService, IDisposable
	{
		private ConcurrentDictionary<Type, ConcurrentList<CachedView>> _cachedVms { get; set; }

		/// <summary>
		/// Default Constructor, use dependency injection if possible
		/// </summary>
		public MemoryCacheService()
		{
			_cachedVms = new ConcurrentDictionary<Type, ConcurrentList<CachedView>>();
		}


		private MemoryCacheSettings Settings
		{
			get
			{
				return Configuration.Settings as MemoryCacheSettings;
			}
		}


		/// <summary>
		/// Register a method and caller that will be mapped to the methods return type when using PropertyLookupDtoAttribute.
		/// </summary>
		/// <param name="method"></param>
		/// <param name="methodCaller"></param>
		public void Register(MethodInfo method, object methodCaller = null)
		{
			Configuration.Container.Register(method, methodCaller);
		}

		private CachedView GetView(Type type, Dictionary<string, object> Parameters)
		{
			if (_cachedVms.ContainsKey(type))
			{
				var existingVM = _cachedVms[type].SingleOrDefault(x => x.Parameters.DictEqual(Parameters));
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
				var vm = Configuration.Container.Build(type, Parameters);
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
			if (_cachedVms.ContainsKey(type))
			{
				_cachedVms[type].Clear();
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
				_cachedVms[type].Remove(vm);
			}
		}

		private void Cache(object vm, Type type, Dictionary<string, object> Parameters)
		{
			if (!_cachedVms.ContainsKey(type))
			{
				_cachedVms.TryAdd(type, new ConcurrentList<CachedView>());
			}

			_cachedVms[type].Add(new CachedView
			{
				CachedType = type,
				CachedVM = vm,
				Parameters = Parameters
			});
		}

		/// <summary>
		/// Cleans the cache in memory
		/// </summary>
		public void Clean()
		{
			foreach(var type in _cachedVms)
			{
				type.Value.Clear();
			}
		}

		/// <summary>
		/// Disposes of the service cache and makes it unusable
		/// </summary>
		public void Dispose()
		{
			_cachedVms.Clear();
			_cachedVms = null;
		}
	}
}
