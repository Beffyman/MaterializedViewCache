using System;
using System.Collections.Generic;
using System.Text;

namespace MaterializedViewCache.Settings
{
	/// <summary>
	/// Settings for when using RavenDbCacheService
	/// </summary>
	public class RavenDbCacheSettings : BaseSettings
	{
		/// <summary>
		/// Uri of the server hosting RavenDb
		/// </summary>
		public Uri ServerUrl { get; set; }
		/// <summary>
		/// ApiKey for RavenDB connection
		/// </summary>
		public string ApiKey { get; set; }

		/// <summary>
		/// Database to be used to store the Views
		/// </summary>
		public string CacheDatabaseName { get; set; }

		/// <summary>
		/// Optional function to compress views going into the db
		/// </summary>
		public Func<string, string> CompressionFunction { get; set; }
		/// <summary>
		/// Optional function to compress views going into the db
		/// </summary>
		public Func<string, string> DecompressionFunction { get; set; }
		/// <summary>
		/// Optional function to encrypt views going into the db
		/// </summary>
		public Func<string, string> EncryptionFunction { get; set; }
		/// <summary>
		/// Optional function to encrypt views going into the db
		/// </summary>
		public Func<string, string> DecryptionFunction { get; set; }
	}
}
