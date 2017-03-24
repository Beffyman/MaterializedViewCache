using MaterializedViewCache.Settings;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace MaterializedViewCache
{
    internal static class Extensions
    {
		public static bool DictEqual<T,K>(this Dictionary<T,K> dict1, Dictionary<T, K> dict2)
		{
			return dict1.Count == dict2.Count && !dict1.Except(dict2).Any();
		}


		public static ulong Hash<T,K>(this Dictionary<T,K> dict, Type type)
		{
			ulong hash = (ulong)dict.Count + 1;

			hash *= hash + (uint)type.GetHashCode();

			foreach (var keyval in dict)
			{
				hash *= hash + (uint)keyval.Key.GetHashCode();
				hash *= hash + (uint)keyval.Value.GetHashCode();
			}

			return hash;
		}


		public static MemberInfo[] GetVariableMembers(this Type type)
		{
			return type.GetFields().Cast<MemberInfo>()
					.Concat(type.GetProperties()).ToArray();
		}

		public static MemberInfo[] GetVariableMembers(this TypeInfo type)
		{
			return type.DeclaredFields.Cast<MemberInfo>()
					.Concat(type.DeclaredProperties).ToArray();
		}

		public static string Serialize(this object obj)
		{
			return JsonConvert.SerializeObject(obj, Configuration.Settings.JsonSettings);
		}

		public static T Deserialize<T>(this string str)
		{
			return JsonConvert.DeserializeObject<T>(str, Configuration.Settings.JsonSettings);
		}
		public static object Deserialize(this string str, Type type)
		{
			return JsonConvert.DeserializeObject(str, type,Configuration.Settings.JsonSettings);
		}
	}
}
