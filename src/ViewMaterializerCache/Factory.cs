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
		public Factory(Dictionary<Type, (GetMethod getMethod, string parameterName)> dtoLookups)
		{
			foreach(var lookup in dtoLookups)
			{
				DtoLookups.TryAdd(lookup.Key, lookup.Value);
			}

		}




		/// <summary>
		/// Will the Factory run the Vm getters in parallel
		/// </summary>
		public bool ParallelGet { get; set; }

		/// <summary>
		/// Method declaration used for getters for the Vm properties
		/// </summary>
		/// <param name="obj"></param>
		/// <returns></returns>
		public delegate object GetMethod(object obj);

		protected ConcurrentDictionary<Type, (GetMethod getMethod,string parameterName)> DtoLookups { get; set; } = new ConcurrentDictionary<Type, (GetMethod getMethod, string parameterName)>();


		public void Register(Type type, (GetMethod getMethod, string parameterName) getInfo)
		{
			if (!DtoLookups.ContainsKey(type))
			{
				DtoLookups.TryAdd(type, getInfo);
			}
			else
			{
				throw new Exception($"Type {type.Name} has already been registered.");
			}
		}

		public void ClearLookups()
		{
			DtoLookups.Clear();
		}

		public bool Remove(Type type)
		{
			(GetMethod getMethod, string parameterName) outObj;
			return DtoLookups.TryRemove(type, out outObj);
		}


		private IEnumerable<IGrouping<Type, (PropertyInfo pInfo, PropertyLookupDtoAttribute attribute)>> GetGroupedProperties(Type vmType)
		{
			TypeInfo vmTypeInfo = vmType.GetTypeInfo();

			IEnumerable<PropertyInfo> vmProperties = vmTypeInfo.DeclaredProperties.Where(x => x.GetCustomAttribute<PropertyLookupDtoAttribute>() != null);

			vmProperties = vmProperties.Where(x => x.GetMethod != null && x.SetMethod != null);

			List<(PropertyInfo pInfo, PropertyLookupDtoAttribute attribute)> mappedList = vmProperties.Select(x => (x, x.GetCustomAttribute<PropertyLookupDtoAttribute>())).ToList();

			return mappedList.GroupBy(x => x.attribute.SourceDto);
		}





		public (bool valid, List<string> errors) IsValidVM<T>(bool throwErrors = true)
		{
			return IsValidVM(typeof(T), throwErrors);
		}
		public (bool valid, List<string> errors) IsValidVM(Type vmType, bool throwErrors = true)
		{
			List<string> errors = new List<string>();

			var groupedTypeList = GetGroupedProperties(vmType);
			foreach (var dtoType in groupedTypeList)
			{
				string parameterName = null;
				foreach(var tuples in dtoType)
				{
					if(parameterName == null)
					{
						parameterName = tuples.attribute.GetParameterName;
					}
					else if(!string.Equals(parameterName, tuples.attribute.GetParameterName,StringComparison.CurrentCultureIgnoreCase))
					{
						string error = $"Property {tuples.pInfo.Name} does not follow the set standard for GetParameterName for Type {dtoType.Key.Name} which is {parameterName}.";
						errors.Add(error);

						if (throwErrors)
						{
							throw new Exception(error);
						}
						return (false,errors);
					}
				}

				if (!DtoLookups.ContainsKey(dtoType.Key))
				{
					string error = $"Type {dtoType.Key.Name} does not have a lookup defined.";
					errors.Add(error);

					if (throwErrors)
					{
						throw new Exception(error);
					}
					return (false, errors);
				}

			}

			return (!errors.Any(), errors);
		}


		public object Build(Type type, params Tuple<string, object>[] paramters)
		{
			return BuildVM(type,paramters.ToDictionary(x => x.Item1, y => y.Item2));
		}
		public object Build(Type type, params KeyValuePair<string, object>[] paramters)
		{
			return BuildVM(type, paramters.ToDictionary(x => x.Key, y => y.Value));
		}
		public object Build(Type type, params (string key, object value)[] paramters)
		{
			return BuildVM(type, paramters.ToDictionary(x => x.key, y => y.value));
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

		private void GetDtoInfo<T>(Dictionary<string, object> paramters, IGrouping<Type, (PropertyInfo pInfo, PropertyLookupDtoAttribute attribute)> dtoType, T finalVM)
		{
			var lookupInfo = DtoLookups[dtoType.Key];

			if (!paramters.ContainsKey(lookupInfo.parameterName))
			{
				throw new Exception($"Parameter {lookupInfo.parameterName} was not provided");
			}

			object parameterValue = paramters[lookupInfo.parameterName];

			object returnValue = lookupInfo.getMethod.Invoke(parameterValue);

			if(returnValue.GetType() != dtoType.Key)
			{
				throw new Exception($"{dtoType.Key.Name} was expected as the output for {lookupInfo.getMethod.GetMethodInfo().Name}, but {returnValue.GetType().Name} was returned.");
			}

			foreach(var propGroup in dtoType)
			{
				var dtoProperty = dtoType.Key.GetTypeInfo().GetDeclaredProperty(propGroup.attribute.DtoPropertyName);

				if(dtoProperty == null)
				{
					throw new Exception($"Property {propGroup.attribute.DtoPropertyName} does not exist in type {dtoType.Key.Name}");
				}


				object value = dtoProperty.GetValue(returnValue);

				propGroup.pInfo.SetValue(finalVM, value);
			}

		}

	}
}
