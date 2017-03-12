using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using ViewMaterializerCache.Attributes;

namespace ViewMaterializerCache
{
	public class Factory
	{
		public Factory()
		{

		}


		/// <summary>
		/// Will the Factory run the Vm getters in parallel
		/// </summary>
		public bool ParallelGet { get; set; }


		internal ConcurrentDictionary<Type, GetMethodDelegate> DtoLookups { get; set; } = new ConcurrentDictionary<Type, GetMethodDelegate>();


		public void Register(MethodInfo method,object methodCaller = null)
		{

			Type returnType = method.ReturnType;

			if(returnType == typeof(void))
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
				MethodCaller = methodCaller
			};

			DtoLookups.TryAdd(returnType, getDelegate);
		}

		public void ClearLookups()
		{
			DtoLookups.Clear();
		}

		public bool Remove(Type type)
		{
			return DtoLookups.TryRemove(type, out GetMethodDelegate outObj);
		}


		private IEnumerable<IGrouping<Type, PropertyLookupInfo>> GetGroupedProperties(Type vmType)
		{
			TypeInfo vmTypeInfo = vmType.GetTypeInfo();

			IEnumerable<PropertyInfo> vmProperties = vmTypeInfo.DeclaredProperties.Where(x => x.GetCustomAttribute<PropertyLookupDtoAttribute>() != null);

			vmProperties = vmProperties.Where(x => x.GetMethod != null && x.SetMethod != null);

			List<PropertyLookupInfo> mappedList = vmProperties.Select(x => new PropertyLookupInfo
			{
				propertyInfo = x,
				attribute = x.GetCustomAttribute<PropertyLookupDtoAttribute>()
			}).ToList();

			return mappedList.GroupBy(x => x.attribute.SourceDto);
		}

		public object Build(Type type, params Tuple<string, object>[] paramters)
		{
			return BuildVM(type,paramters.ToDictionary(x => x.Item1, y => y.Item2));
		}
		public object Build(Type type, params KeyValuePair<string, object>[] paramters)
		{
			return BuildVM(type, paramters.ToDictionary(x => x.Key, y => y.Value));
		}
		public object Build(Type type, Dictionary<string, object> paramters)
		{
			return BuildVM(type, paramters);
		}
		public object Build(Type type)
		{
			return BuildVM(type,null);
		}

		public T Build<T>(params Tuple<string,object>[] paramters)
		{
			return (T)BuildVM(typeof(T),paramters.ToDictionary(x => x.Item1, y => y.Item2));
		}
		public T Build<T>(params KeyValuePair<string, object>[] paramters)
		{
			return (T)BuildVM(typeof(T), paramters.ToDictionary(x => x.Key, y => y.Value));
		}
		public T Build<T>(params (string key, object value)[] paramters)
		{
			return (T)BuildVM(typeof(T), paramters.ToDictionary(x => x.key, y => y.value));
		}
		public T Build<T>(Dictionary<string, object> paramters)
		{
			return (T)BuildVM(typeof(T), paramters);
		}
		public T Build<T>()
		{
			return (T)BuildVM(typeof(T), null);
		}

		/// <summary>
		///
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="paramters">parameter name/values</param>
		/// <returns></returns>
		private object BuildVM(Type vmType, Dictionary<string, object> paramters)
		{

			var groupedTypeList = GetGroupedProperties(vmType);

			object finalVM = Activator.CreateInstance(vmType);

			if (ParallelGet)
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

		private void GetDtoInfo<T>(Dictionary<string, object> parameters, IGrouping<Type, PropertyLookupInfo> dtoType, T finalVM)
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

			var returnValue = lookupInfo.Method.Invoke(lookupInfo.MethodCaller, inputParameters.ToArray());


			if (returnValue.GetType() != dtoType.Key)
			{
				throw new Exception($"{dtoType.Key.Name} was expected as the output for {lookupInfo.Method.Name}, but {returnValue.GetType().Name} was returned.");
			}

			foreach (var propGroup in dtoType)
			{
				var dtoProperty = dtoType.Key.GetTypeInfo().GetDeclaredProperty(propGroup.attribute.DtoPropertyName);

				if (dtoProperty == null)
				{
					throw new Exception($"Property {propGroup.attribute.DtoPropertyName} does not exist in type {dtoType.Key.Name}");
				}


				object value = dtoProperty.GetValue(returnValue);

				propGroup.propertyInfo.SetValue(finalVM, value);
			}

		}

	}
}
