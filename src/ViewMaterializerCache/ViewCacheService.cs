using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace ViewMaterializerCache
{
	public interface IViewCacheService
	{

	}


	public sealed class ViewCacheService
	{

		private Factory ViewFactory;
		internal readonly ConcurrentDictionary<Type, ConcurrentList<CachedView>> _cachedVMs;


		public ViewCacheService()
		{
			ViewFactory = new Factory();
			_cachedVMs = new ConcurrentDictionary<Type, ConcurrentList<CachedView>>();
		}

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

		public T Get<T>(Dictionary<string, object> Parameters)
		{
			return (T)Get(typeof(T), Parameters);
		}
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


		public bool Exists<T>(Dictionary<string, object> Parameters)
		{
			return Exists(typeof(T), Parameters);
		}
		public bool Exists(Type type, Dictionary<string, object> Parameters)
		{
			return Get(type, Parameters) != null;
		}



		public void ExpireVM<T>()
		{
			ExpireVM(typeof(T));
		}
		public void ExpireVM(Type type)
		{
			if (_cachedVMs.ContainsKey(type))
			{
				_cachedVMs[type].Clear();
			}
		}
		public void ExpireVM<T>(Dictionary<string, object> Parameters)
		{
			ExpireVM(typeof(T), Parameters);
		}

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

	}
}
