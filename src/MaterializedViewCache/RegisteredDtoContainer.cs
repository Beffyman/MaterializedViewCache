using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using MaterializedViewCache.Attributes;
using MaterializedViewCache.Settings;

namespace MaterializedViewCache
{
	/// <summary>
	/// Used in a implementation of IMaterializedViewCacheService to call the getters and register methods
	/// </summary>
	public sealed class RegisteredDtoContainer : IDisposable
	{
		/// <summary>
		/// Default Constructor
		/// </summary>
		public RegisteredDtoContainer()
		{

		}


		private BaseSettings Settings
		{
			get
			{
				return Configuration.Settings;
			}
		}


		internal ConcurrentDictionary<Type, GetMethodDelegate> DtoLookups { get; set; } = new ConcurrentDictionary<Type, GetMethodDelegate>();

		/// <summary>
		/// Registers the method and its caller to the lookup list.
		/// </summary>
		/// <param name="method"></param>
		/// <param name="methodCaller"></param>
		/// <returns></returns>
		public RegisteredDtoContainer Register(MethodInfo method,object methodCaller = null)
		{
			return Register(method, methodCaller, null);
		}

		/// <summary>
		/// Registers the method and the method used to get its caller to the lookup list.
		/// </summary>
		/// <param name="method"></param>
		/// <param name="methodCallerGetter"></param>
		/// <returns></returns>
		public RegisteredDtoContainer Register(MethodInfo method, Func<object> methodCallerGetter)
		{
			return Register(method, null, methodCallerGetter);
		}

		private RegisteredDtoContainer Register(MethodInfo method, object methodCaller, Func<object> methodCallerGetter)
		{
			Type returnType = method.ReturnType;

			if (returnType == typeof(void))
			{
				throw new Exception($"Method {method.Name} has a return type of void. You cannot register a method with a return type of void.");
			}

			if (DtoLookups.ContainsKey(returnType))
			{
				throw new Exception($"Type {returnType.Name} has already been registered.");
			}

			var getDelegate = new GetMethodDelegate
			{
				Method = method,
				MethodCaller = methodCaller,
				MethodCallerGetter = methodCallerGetter
			};

			DtoLookups.TryAdd(returnType, getDelegate);

			return this;
		}

		/// <summary>
		/// Clear all registered lookups
		/// </summary>
		public void ClearLookups()
		{
			DtoLookups.Clear();
		}

		/// <summary>
		/// Remove the lookups of type
		/// </summary>
		/// <param name="type"></param>
		/// <returns></returns>
		public bool Remove(Type type)
		{
			return DtoLookups.TryRemove(type, out GetMethodDelegate outObj);
		}


		private IEnumerable<IGrouping<Type, MemberLookupInfo>> GetGroupedProperties(Type vmType)
		{
			TypeInfo vmTypeInfo = vmType.GetTypeInfo();

			IEnumerable<MemberInfo> vmProperties = vmTypeInfo.GetVariableMembers().Where(x => x.GetCustomAttribute<MemberLookupDtoAttribute>() != null);

			//vmProperties = vmProperties.Where(x => x.GetMethod != null && x.SetMethod != null);

			List<MemberLookupInfo> mappedList = vmProperties.Select(x => new MemberLookupInfo
			{
				memberInfo = x,
				attribute = x.GetCustomAttribute<MemberLookupDtoAttribute>()
			}).ToList();

			return mappedList.GroupBy(x => x.attribute.SourceDto);
		}

		/// <summary>
		/// Builds the object of type with the parameters provided.
		/// </summary>
		/// <param name="type"></param>
		/// <param name="paramters"></param>
		/// <returns></returns>
		public object Build(Type type, params Tuple<string, object>[] paramters)
		{
			return BuildVM(type,paramters.ToDictionary(x => x.Item1, y => y.Item2));
		}
		/// <summary>
		/// Builds the object of type with the parameters provided.
		/// </summary>
		/// <param name="type"></param>
		/// <param name="paramters"></param>
		/// <returns></returns>
		public object Build(Type type, params KeyValuePair<string, object>[] paramters)
		{
			return BuildVM(type, paramters.ToDictionary(x => x.Key, y => y.Value));
		}
		/// <summary>
		/// Builds the object of type with the parameters provided.
		/// </summary>
		/// <param name="type"></param>
		/// <param name="paramters"></param>
		/// <returns></returns>
		public object Build(Type type, Dictionary<string, object> paramters)
		{
			return BuildVM(type, paramters);
		}
		/// <summary>
		/// Builds the object of type.
		/// </summary>
		/// <param name="type"></param>
		/// <returns></returns>
		public object Build(Type type)
		{
			return BuildVM(type,null);
		}
		/// <summary>
		/// Builds the object of type with the parameters provided.
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="paramters"></param>
		/// <returns></returns>
		public T Build<T>(params Tuple<string,object>[] paramters)
		{
			return (T)BuildVM(typeof(T),paramters.ToDictionary(x => x.Item1, y => y.Item2));
		}
		/// <summary>
		/// Builds the object of type with the parameters provided.
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="paramters"></param>
		/// <returns></returns>
		public T Build<T>(params KeyValuePair<string, object>[] paramters)
		{
			return (T)BuildVM(typeof(T), paramters.ToDictionary(x => x.Key, y => y.Value));
		}
		/// <summary>
		/// Builds the object of type with the parameters provided.
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="paramters"></param>
		/// <returns></returns>
		public T Build<T>(Dictionary<string, object> paramters)
		{
			return (T)BuildVM(typeof(T), paramters);
		}
		/// <summary>
		/// Builds the object of type.
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <returns></returns>
		public T Build<T>()
		{
			return (T)BuildVM(typeof(T), null);
		}

		private object BuildVM(Type vmType, Dictionary<string, object> paramters)
		{

			var groupedTypeList = GetGroupedProperties(vmType);

			object finalVM = Activator.CreateInstance(vmType);

			if (Settings.ParallelGet)
			{
				Parallel.ForEach(groupedTypeList, dtoType =>
				{
					GetDtoInfo(paramters, dtoType, finalVM);
				});
			}
			else
			{
				foreach (var dtoType in groupedTypeList)
				{
					GetDtoInfo(paramters, dtoType, finalVM);
				}
			}

			return finalVM;
		}

		private void GetDtoInfo<T>(Dictionary<string, object> parameters, IGrouping<Type, MemberLookupInfo> dtoType, T finalVM)
		{
			var lookupInfo = DtoLookups[dtoType.Key];

			//Lookup any missing parameters
			if (lookupInfo.Method.GetParameters().Any(x => !parameters.ContainsKey(x.Name)))
			{
				var invalidParameters = lookupInfo.Method.GetParameters().Where(x => !parameters.ContainsKey(x.Name));

				throw new Exception($"Parameters {string.Join(", ", invalidParameters.Select(x => x.Name))} were not provided");
			}

			IOrderedEnumerable<ParameterInfo> orderedParameters = lookupInfo.Method.GetParameters().OrderBy(x => x.Position);

			List<object> inputParameters = new List<object>();

			foreach(var param in orderedParameters)
			{
				//already established that all parameters are provided
				var matchingKey = parameters.Keys.SingleOrDefault(x => x.Equals(param.Name, StringComparison.CurrentCultureIgnoreCase));

				object matchingParam = parameters[matchingKey];

				inputParameters.Add(matchingParam);
			}

			object caller = lookupInfo.MethodCaller;

			if (caller == null && lookupInfo.MethodCallerGetter != null)
			{
				caller = lookupInfo.MethodCallerGetter();
			}

			var returnValue = lookupInfo.Method.Invoke(caller, inputParameters.ToArray());


			if (returnValue.GetType() != dtoType.Key)
			{
				throw new Exception($"{dtoType.Key.Name} was expected as the output for {lookupInfo.Method.Name}, but {returnValue.GetType().Name} was returned.");
			}

			foreach (var propGroup in dtoType)
			{
				var dtoProperty = dtoType.Key.GetTypeInfo().GetDeclaredProperty(propGroup.attribute.DtoMemberName);

				if (dtoProperty == null)
				{
					throw new Exception($"Property {propGroup.attribute.DtoMemberName} does not exist in type {dtoType.Key.Name}");
				}


				object value = dtoProperty.GetValue(returnValue);

				if(propGroup.memberInfo is FieldInfo fi)
				{
					fi.SetValue(finalVM, value);
				}
				else if (propGroup.memberInfo is PropertyInfo pi)
				{
					pi.SetValue(finalVM, value);
				}
			}

		}
		/// <summary>
		/// Cleans up the object
		/// </summary>
		public void Dispose()
		{
			ClearLookups();
			DtoLookups = null;
		}
	}
}
