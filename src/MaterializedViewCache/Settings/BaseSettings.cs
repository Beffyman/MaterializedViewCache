using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace MaterializedViewCache.Settings
{
	/// <summary>
	/// Abstract settings class
	/// </summary>
    public abstract class BaseSettings
    {
		/// <summary>
		/// Should Get methods for building a VM be run in parallel?
		/// </summary>
		public bool ParallelGet { get; set; }

		/// <summary>
		/// Settings for Json Serialization and Deserialization.
		/// </summary>
		public JsonSerializerSettings JsonSettings { get; set; }

    }
}
