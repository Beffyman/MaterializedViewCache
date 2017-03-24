using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using Raven.Client;
using Raven.Client.Document;
using MaterializedViewCache.Settings;
using MaterializedViewCache.Services.RavenDb;
using Raven.Client.Linq;
using System.Linq;

namespace MaterializedViewCache.Services
{
	/// <summary>
	/// Views are stored in a RavenDB no-sql database and the hashed parameters act as the key
	/// </summary>
	public sealed class RavenDbCacheService : IMaterializedViewCacheService, IDisposable
	{

		private IDocumentStore _documentStore { get; set; }

		/// <summary>
		/// Default Constuctor
		/// </summary>
		public RavenDbCacheService()
		{
			_documentStore = Connect();
		}

		private RavenDbCacheSettings Settings
		{
			get
			{
				return Configuration.Settings as RavenDbCacheSettings;
			}
		}


		private IDocumentStore Connect()
		{
			var ravenStore = new DocumentStore()
			{
				Url = Settings.ServerUrl.ToString(),
				DefaultDatabase = Settings.CacheDatabaseName
			};
			ravenStore.Initialize(true);

			return ravenStore;
		}
		private IDocumentSession OpenSession()
		{
			var session = _documentStore.OpenSession();

			session.Advanced.UseOptimisticConcurrency = !Settings.ParallelGet;

			return session;
		}

		/// <summary>
		/// Disposes of the RavenDB connection and registered getters
		/// </summary>
		public void Dispose()
		{
			_documentStore.Dispose();

			_documentStore = null;
		}

		/// <summary>
		/// Checks to see if the type with parameters exists in the server
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="Parameters"></param>
		/// <returns></returns>
		public bool Exists<T>(Dictionary<string, object> Parameters)
		{
			return Exists(typeof(T), Parameters);
		}

		/// <summary>
		/// Checks to see if the type with parameters exists in the server
		/// </summary>
		/// <param name="type"></param>
		/// <param name="Parameters"></param>
		/// <returns></returns>
		public bool Exists(Type type, Dictionary<string, object> Parameters)
		{
			using (var session = OpenSession())
			{
				var id = Parameters.Hash(type);
				var result = session.Query<ViewJson,ViewJson_ByAll>().SingleOrDefault(x => x.Id == id);

				return result != null;
			}
		}

		/// <summary>
		/// Deletes all occurances of views with the type from the server
		/// </summary>
		/// <typeparam name="T"></typeparam>
		public void ExpireVM<T>()
		{
			ExpireVM(typeof(T));
		}

		/// <summary>
		/// Deletes all occurances of views with the type from the server
		/// </summary>
		/// <param name="type"></param>
		public void ExpireVM(Type type)
		{
			using (var session = OpenSession())
			{
				var hash = type.GetHashCode();
				var values = session.Query<ViewJson, ViewJson_ByAll>().Where(x => x.TypeHash == hash).ToList();
				foreach (var val in values)
				{
					session.Delete(val);
				}
			}
		}

		/// <summary>
		/// Deletes the view of type with parameters from the server
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="Parameters"></param>
		public void ExpireVM<T>(Dictionary<string, object> Parameters)
		{
			ExpireVM(typeof(T), Parameters);
		}

		/// <summary>
		/// Deletes the view of type with parameters from the server
		/// </summary>
		/// <param name="type"></param>
		/// <param name="Parameters"></param>
		public void ExpireVM(Type type, Dictionary<string, object> Parameters)
		{
			using (var session = OpenSession())
			{
				var id = Parameters.Hash(type);
				var val = session.Query<ViewJson, ViewJson_ByAll>().SingleOrDefault(x => x.Id == id);
				if (val != null)
				{
					session.Delete(val);
				}
				else
				{
					throw new Exception($"View with Id of {id} does not exist");
				}
			}
		}

		/// <summary>
		/// Gets the view of type with the parameters provided.  If it does not exist in the DB, it will be generated and then stored.
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="Parameters"></param>
		/// <returns></returns>
		public T Get<T>(Dictionary<string, object> Parameters)
		{
			return (T)Get(typeof(T), Parameters);
		}

		/// <summary>
		/// Gets the view of type with the parameters provided.  If it does not exist in the DB, it will be generated and then stored.
		/// </summary>
		/// <param name="type"></param>
		/// <param name="Parameters"></param>
		/// <returns></returns>
		public object Get(Type type, Dictionary<string, object> Parameters)
		{
			using (var session = OpenSession())
			{
				var id = Parameters.Hash(type);
				var val = session.Query<ViewJson>().SingleOrDefault(x => x.Id == id);
				if (val != null)
				{
					string outJson = val.Json;
					if (Settings.EncryptionFunction != null && Settings.DecryptionFunction != null)
					{
						outJson = Settings.DecryptionFunction(outJson);
					}
					if (Settings.CompressionFunction != null && Settings.DecompressionFunction != null)
					{
						outJson = Settings.DecompressionFunction(outJson);
					}

					return outJson.Deserialize(type);
				}
			}

			//Failed to find in Raven, that means construct it.
			//No idea how long a Build could take so lets close the Session.

			return Store(type, Parameters);
		}

		private object Store(Type type, Dictionary<string, object> Parameters)
		{
			var obj = Configuration.Container.Build(type, Parameters);

			string inJson = obj.Serialize();

			if (Settings.CompressionFunction != null && Settings.DecompressionFunction != null)
			{
				inJson = Settings.CompressionFunction(inJson);
			}
			if (Settings.EncryptionFunction != null && Settings.DecryptionFunction != null)
			{
				inJson = Settings.EncryptionFunction(inJson);
			}

			ViewJson view = new ViewJson
			{
				Id = Parameters.Hash(type),
				TypeHash = type.GetHashCode(),
				Json = inJson
			};

			using (var session = OpenSession())
			{
				session.Store(view);
			}

			return obj;
		}

		/// <summary>
		/// Registers the
		/// </summary>
		/// <param name="method"></param>
		/// <param name="methodCaller"></param>
		public void Register(MethodInfo method, object methodCaller = null)
		{
			Configuration.Container.Register(method, methodCaller);
		}

		/// <summary>
		/// Wipe out all views stored in the server.
		/// </summary>
		public void Clean()
		{
			var docs = _documentStore.DatabaseCommands.GetDocuments(0, 1000, false);
			foreach (var doc in docs)
			{
				_documentStore.DatabaseCommands.Delete(doc.Key, null);
			}
		}
	}
}
