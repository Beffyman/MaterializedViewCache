using System;
using System.Collections.Generic;
using System.Text;

namespace MaterializedViewCache.Settings
{
	/// <summary>
	/// Configuration manager that holds settings used by services that implement IMaterializedViewCacheService
	/// </summary>
	public static class Configuration
	{

		internal static BaseSettings Settings { get; set; }

		/// <summary>
		/// Container for Registering and Building Views
		/// </summary>
		public static RegisteredDtoContainer Container { get; set; }

		/// <summary>
		/// Call this on startup, provide it the settings type you want to use in your application
		/// </summary>
		/// <param name="settings"></param>
		/// <param name="forceAssign"></param>
		public static void Setup(BaseSettings settings, bool forceAssign = false)
		{
			if(Settings == null || forceAssign)
			{
				Settings = settings;
				Container = new RegisteredDtoContainer();
			}
			else
			{
				throw new Exception("Cannot reassign settings after they are already being used");
			}
		}

	}
}
