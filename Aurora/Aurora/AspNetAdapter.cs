//
// AspNetAdapter - A thin wrapper around the ASP.NET request and response 
//								 objects.
//
// Updated On: 27 November 2014
//
// Description: 
//
//	A thin wrapper around the ASP.NET Request/Reponse objects to make it easier
//	to disconnect applications from the intrinsics of the HttpContext.
//
//  NOTE: Apps aren't totally disconnected because I've elected to expose the 
//				ASP.NET Session, Application and Cache stores through callbacks. I 
//				think this is a fair trade off for now. Aurora relies heavily on 
//				these to store state between requests.
//
// Requirements: .NET 4.5
//
// Contact Info:
//
//  Frank Hale - <frankhale@gmail.com> 
//
// An attempt to abstract away some of the common bits of the ASP.NET 
// HttpContext.
//
// I initially looked at OWIN to provide the abstraction that I wanted but I 
// found it to be a bit more complex than I was hoping for. What I was looking
// for was something a bit easier to strap in, something that exposed some 
// simple types and was as braindead easy as I could come up with.
//
// GNU GPLv3 license <http://www.gnu.org/licenses/gpl-3.0.html>
//
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
//
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
//
// You should have received a copy of the GNU General Public License
// along with this program.  If not, see <http://www.gnu.org/licenses/>.
//

using HtmlAgilityPack;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using System.Web.Caching;
using System.Web.SessionState;
using Newtonsoft.Json;

namespace AspNetAdapter
{
	#region ASP.NET HOOKS - IHTTPHANDLER / IHTTPMODULE
	public sealed class AspNetAdapterHandler : IHttpAsyncHandler, IRequiresSessionState
	{
		public IAsyncResult BeginProcessRequest(HttpContext context, AsyncCallback cb, object extraData)
		{
			var frameworkCompletionSource = new TaskCompletionSource<bool>();
			var httpContextAdapter = new HttpContextAdapter();

			httpContextAdapter.OnComplete += (o, e) =>
			{
				var args = (e as ResponseEventArgs);

				if (args.Redirect)
				{
					var redirectInfo = args.Data as RedirectInfo;
					if (redirectInfo != null)
						httpContextAdapter.SendRedirectResponse(redirectInfo.Path, redirectInfo.Headers);
				}
				else
				{
					var response = args.Data as Dictionary<string, object>;
					httpContextAdapter.SendResponse(response);
				}

				frameworkCompletionSource.SetResult(true);
			};

			httpContextAdapter.Init(context);

			var frameworkTask = frameworkCompletionSource.Task;

			if (cb != null)
				frameworkTask.ContinueWith(x => cb(x), TaskContinuationOptions.ExecuteSynchronously);

			return frameworkTask;
		}

		public void EndProcessRequest(IAsyncResult result)
		{
			((Task)result).Wait();
		}

		public bool IsReusable { get { return true; } }

		public void ProcessRequest(HttpContext context) { }
	}

	public sealed class AspNetAdapterModule : IHttpModule
	{
		public void Dispose() { }

		public void Init(HttpApplication app)
		{
			app.Error += new EventHandler(app_Error);
		}

		private void app_Error(object sender, EventArgs eventArgs)
		{
			var httpContextAdapter = new HttpContextAdapter();

			httpContextAdapter.OnComplete += (o, e) =>
			{
				var args = (e as ResponseEventArgs);

				if (args.Redirect)
				{
					var redirectInfo = args.Data as RedirectInfo;
					if (redirectInfo != null)
						httpContextAdapter.SendRedirectResponse(redirectInfo.Path, redirectInfo.Headers);
				}
				else
				{
					var response = args.Data as Dictionary<string, object>;
					httpContextAdapter.SendResponse(response);
				}
			};

			httpContextAdapter.Init(HttpContext.Current);
		}
	}
	#endregion

	#region WEB.JSON
	public class TypeInfo
	{
		public string Name { get; set; }
		public string Type { get; set; }
	}

	public class WebJsonInfo
	{
		public TypeInfo ApplicationTypeInfo { get; set; }
		public List<TypeInfo> MiddlewareTypeInfo { get; set; }
		public Dictionary<string,string> MimeTypes { get; set; }
		public Dictionary<string, string> AppSettings { get; set; }
		public List<string> ViewRoots { get; set; }
		public string AllowedFilePattern { get; set; }
	}

	public class WebJson
	{
		private static string error = "There is a problem with the web.json path or file and it cannot be deserialized.";
		private static string webJsonFileName = "web.json";
		private readonly string webJsonFilePath;

		public WebJson(string appRootPath)
		{
			appRootPath.ThrowIfArgumentNull(error);

			if (Directory.Exists(appRootPath))
			{
				webJsonFilePath = string.Format("{0}{1}{2}", 
					appRootPath, 
					Path.DirectorySeparatorChar,
					webJsonFileName);

				if (!File.Exists(webJsonFilePath))
					throw new Exception(error);
			}
			else
				throw new Exception(error);
		}

		public WebJsonInfo Load()
		{
			try
			{
				return JsonConvert.DeserializeObject<WebJsonInfo>(File.ReadAllText(webJsonFilePath));
			}
			catch (Exception ex)
			{
				// Yeah we are going to lose the original exception but that's okay. If 
				// the JSON is borked then it's probably pretty apparent from looking at
				// it what needs to be fixed.
				throw new Exception(error);
			}
		}
	}
	#endregion

	#region MIDDLEWARE
	public interface IAspNetAdapterMiddleware
	{
		MiddlewareResult Transform(Dictionary<string, object> app,
															 Dictionary<string, object> request);
	}

	public class MiddlewareResult
	{
		public Dictionary<string, object> App { get; set; }
		public Dictionary<string, object> Request { get; set; }
	}

	#region WEB.CONFIG SECTION
	// For reference: http://net.tutsplus.com/tutorials/asp-net/how-to-add-custom-configuration-settings-for-your-asp-net-application/
	public class AspNetAdapterMiddlewareConfigurationElement : ConfigurationElement
	{
		[ConfigurationProperty("name", IsKey = true, IsRequired = true)]
		public string Name
		{
			get
			{
				return this["name"] as string;
			}
		}

		[ConfigurationProperty("type", IsRequired = true)]
		public string Type
		{
			get
			{
				return this["type"] as string;
			}
		}
	}

	public class AspNetAdapterApplicationConfigurationElement : ConfigurationElement
	{
		[ConfigurationProperty("name", IsKey = true, IsRequired = true)]
		public string Name
		{
			get
			{
				return this["name"] as string;
			}
		}

		[ConfigurationProperty("type", IsRequired = true)]
		public string Type
		{
			get
			{
				return this["type"] as string;
			}
		}
	}

	public class AspNetAdapterMiddlewareConfigurationCollection : ConfigurationElementCollection
	{
		public AspNetAdapterMiddlewareConfigurationElement this[int index]
		{
			get
			{
				return base.BaseGet(index) as AspNetAdapterMiddlewareConfigurationElement;
			}

			set
			{
				if (base.BaseGet(index) != null)
				{
					base.BaseRemoveAt(index);
				}

				this.BaseAdd(index, value);
			}
		}

		protected override ConfigurationElement CreateNewElement()
		{
			return new AspNetAdapterMiddlewareConfigurationElement();
		}

		protected override object GetElementKey(ConfigurationElement element)
		{
			return ((AspNetAdapterMiddlewareConfigurationElement)element).Type;
		}
	}

	public class AspNetAdapterWebConfig : ConfigurationSection
	{
		[ConfigurationProperty("middleware", IsRequired = false)]
		public AspNetAdapterMiddlewareConfigurationCollection AspNetAdapterMiddlewareCollection
		{
			get
			{
				return this["middleware"] as AspNetAdapterMiddlewareConfigurationCollection;
			}
		}

		[ConfigurationProperty("application", IsRequired = false)]
		public AspNetAdapterApplicationConfigurationElement AspNetAdapterApplication
		{
			get
			{
				return this["application"] as AspNetAdapterApplicationConfigurationElement;
			}
		}
	}
	#endregion

	#endregion

	#region ASP.NET ADAPTER
	// An application that wants to hook into ASP.NET and be sent the goodies that 
	// the HttpContextAdapter has to offer
	public interface IAspNetAdapterApplication
	{
		void Init(Dictionary<string, object> app,
							Dictionary<string, object> request,
							Action<Dictionary<string, object>> response);
	}

	public sealed class PostedFile
	{
		public string ContentType { get; set; }
		public string FileName { get; set; }
		public byte[] FileBytes { get; set; }
	}

	public static class HttpAdapterConstants
	{
		#region MISCELLANEOUS
		public static readonly string ServerError = "ServerError";
		public static readonly string ServerErrorStackTrace = "StackTrace";
		public static readonly string ServerVariables = "ServerVariables";
		public static readonly string RewritePathCallback = "RewritePathCallback";
		public static readonly string User = "User";
		public static readonly string SessionId = "SessionID";
		public static readonly string DebugModeAssembly = "DebugModeAssembly";
		public static readonly string DebugModeASPNET = "DebugModeASPNET";
		#endregion

		#region APPLICATION CALLBACKS
		public static readonly string ApplicationSessionStoreAddCallback = "ApplicationSessionStoreAddCallback";
		public static readonly string ApplicationSessionStoreRemoveCallback = "ApplicationSessionStoreRemoveCallback";
		public static readonly string ApplicationSessionStoreGetCallback = "ApplicationSessionStoreGetCallback";
		public static readonly string UserSessionStoreAddCallback = "UserSessionStoreAddCallback";
		public static readonly string UserSessionStoreRemoveCallback = "UserSessionStoreRemoveCallback";
		public static readonly string UserSessionStoreGetCallback = "UserSessionStoreGetCallback";
		public static readonly string UserSessionStoreAbandonCallback = "UserSessionStoreAbandonCallback";
		public static readonly string CacheAddCallback = "CacheAddCallback";
		public static readonly string CacheGetCallback = "CacheGetCallback";
		public static readonly string CacheRemoveCallback = "CacheRemoveCallback";
		public static readonly string CookieAddCallback = "CookieAddCallback";
		public static readonly string CookieGetCallback = "CookieGetCallback";
		public static readonly string CookieRemoveCallback = "CookieRemoveCallback";
		#endregion

		#region REQUEST
		public static readonly string RequestScheme = "RequestScheme";
		public static readonly string RequestMethod = "RequestMethod";
		public static readonly string RequestPathBase = "RequestPathBase";
		public static readonly string RequestPath = "RequestPath";
		public static readonly string RequestPathSegments = "RequestPathSegments";
		public static readonly string RequestQueryString = "RequestQueryString";
		public static readonly string RequestQueryStringCallback = "RequestQueryStringCallback";
		public static readonly string RequestHeaders = "RequestHeaders";
		public static readonly string RequestBody = "RequestBody";
		public static readonly string RequestCookie = "RequestCookie";
		public static readonly string RequestIsSecure = "RequestIsSecure";
		public static readonly string RequestIpAddress = "RequestIPAddress";
		public static readonly string RequestForm = "RequestForm";
		public static readonly string RequestFormCallback = "RequestFormCallback";
		public static readonly string RequestClientCertificate = "RequestClientCertificate";
		public static readonly string RequestFiles = "RequestFiles";
		public static readonly string RequestUrl = "RequestUrl";
		public static readonly string RequestUrlAuthority = "RequestUrlAuthority";
		public static readonly string RequestIdentity = "RequestIdentity";
		#endregion

		#region RESPONSE
		public static readonly string ResponseCache = "ResponseCache";
		public static readonly string ResponseCacheabilityOption = "ResponseCacheabilityOption";
		public static readonly string ResponseCacheExpiry = "ResponseCacheExpiry";
		public static readonly string ResponseCacheMaxAge = "ResponseCacheMaxAge";
		public static readonly string ResponseCookie = "ResponseCookie";
		public static readonly string ResponseStatus = "ResponseStatus";
		public static readonly string ResponseContentType = "ResponseContentType";
		public static readonly string ResponseHeaders = "ResponseHeaders";
		public static readonly string ResponseBody = "ResponseBody";
		public static readonly string ResponseRedirectCallback = "ResponseRedirectCallback";
		public static readonly string ResponseErrorCallback = "ResponseErrorCallback";
		#endregion

		public static readonly Dictionary<string, string> MimeTypes = new Dictionary<string, string>()
		{
			{ ".js",  "application/x-javascript" },  
			{ ".css", "text/css" },
			{ ".png", "image/png" },
			{ ".jpg", "image/jpg" },
			{ ".gif", "image/gif" },
			{ ".ico", "image/x-icon" },
			{ ".csv", "text/plain"},
			{ ".txt", "text/plain"},
			{ ".xlsx", "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet"},
			{ ".pptx", "application/vnd.openxmlformats-officedocument.presentationml.presentation"}
		};
	}

	public class RedirectInfo
	{
		public string Path { get; private set; }
		public Dictionary<string, string> Headers { get; private set; }

		public RedirectInfo(string path, Dictionary<string, string> headers)
		{
			Path = path;
			Headers = headers;
		}
	}

	public class ResponseEventArgs : EventArgs
	{
		public bool Redirect { get; private set; }
		public object Data { get; private set; }

		public ResponseEventArgs(bool redirect, object data)
		{
			Redirect = redirect;
			Data = data;
		}
	}

	public sealed class HttpContextAdapter
	{
		private static readonly object SyncInitLock = new object();
		private static readonly AspNetAdapterWebConfig webConfig = ConfigurationManager.GetSection("aspNetAdapter") as AspNetAdapterWebConfig;
		private static WebJson webJson;
		private WebJsonInfo webJsonInfo;
		private Stopwatch timer;
		private HttpContext context;
		private bool firstRun, debugModeAssembly;
		private const string AspNetApplicationTypeSessionName = "__AspNetApplicationType";
		private const string AspNetMiddlewareSessionName = "__AspNetMiddleware";

		public event EventHandler<ResponseEventArgs> OnComplete;

		public void Init(HttpContext ctx)
		{
			context = ctx;
			webJson = new WebJson(ctx.Request.PhysicalApplicationPath.TrimEnd('\\'));
			webJsonInfo = webJson.Load();
			timer = new Stopwatch();
			timer.Start();
			firstRun = Convert.ToBoolean(context.Application["__SyncInitLock"]);

			Type adapterApp = null;

			if (context.Application[AspNetApplicationTypeSessionName] == null)
			{
				context.Application["__SyncInitLock"] = true;

				List<Type> apps = null;

				if (webConfig.AspNetAdapterApplication == null)
				{
					apps = Utility.GetAssemblies()
												.SelectMany(x => x.GetLoadableTypes()
												.Where(y => y.GetInterfaces().FirstOrDefault(i => i.IsInterface && i.UnderlyingSystemType == typeof(IAspNetAdapterApplication)) != null))
												.ToList();
				}
				else
				{
					apps = Utility.GetAssemblies()
												.SelectMany(x => x.GetLoadableTypes())
												.Where(y => y.Name == webConfig.AspNetAdapterApplication.Name &&
																		y.FullName == webConfig.AspNetAdapterApplication.Type &&
																		y.GetInterfaces().FirstOrDefault(i => i.IsInterface && i.UnderlyingSystemType == typeof(IAspNetAdapterApplication)) != null)
												.ToList();
				}

				if (apps == null)
					throw new Exception("Failed to find an assembly the IAspNetAdapterApplication interface.");
				else if (apps.Count() > 1)
					throw new Exception("You can only have one IAspNetAdapterApplication interface.");

				adapterApp = apps.FirstOrDefault();

				context.Application.Lock();
				context.Application[AspNetApplicationTypeSessionName] = adapterApp;
				context.Application.UnLock();

			}
			else
				adapterApp = ctx.Application[AspNetApplicationTypeSessionName] as Type;

			if (adapterApp == null) return;

			debugModeAssembly = IsAssemblyDebugBuild(adapterApp.Assembly);

			var appDictionary = InitializeApplicationDictionary();
			var requestDictionary = InitializeRequestDictionary();
			var appInstance = (IAspNetAdapterApplication)Activator.CreateInstance(adapterApp);

			ProcessMiddleware(appDictionary, requestDictionary);
			InitializeApplication(appInstance, appDictionary, requestDictionary);
		}

		private void ProcessMiddleware(Dictionary<string, object> appDictionary, Dictionary<string, object> requestDictionary)
		{
			// Middleware in the context of AspNetAdapter is simply a class implementing the IAspNetMiddleware interface that
			// has the ability to modify the 'app' and/or 'request' dictionaries. 
			if (webConfig == null) return;

			var middlewareDict = new Dictionary<string, Type>();

			if (context.Application[AspNetMiddlewareSessionName] == null)
			{
				var middleware = Utility.GetAssemblies()
					.SelectMany(x => x.GetLoadableTypes().Where(y => y.GetInterfaces()
						.FirstOrDefault(i => i.IsInterface &&
																 i.UnderlyingSystemType == typeof(IAspNetAdapterMiddleware)) != null));

				foreach (AspNetAdapterMiddlewareConfigurationElement mw in webConfig.AspNetAdapterMiddlewareCollection)
				{
					var m = middleware.FirstOrDefault(x => x.FullName == mw.Type);

					if (m != null)
						middlewareDict[m.FullName] = m;
				}

				context.Application.Lock();
				context.Application[AspNetMiddlewareSessionName] = middlewareDict;
				context.Application.UnLock();
			}
			else
				middlewareDict = context.Application[AspNetMiddlewareSessionName] as Dictionary<string, Type>;

			if (middlewareDict == null) return;

			foreach (AspNetAdapterMiddlewareConfigurationElement mw in webConfig.AspNetAdapterMiddlewareCollection)
			{
				var m = middlewareDict[mw.Type];
				if (m == null) continue;

				// The big question is, should this instance be cached?
				var min = (IAspNetAdapterMiddleware)Activator.CreateInstance(m);
				var result = min.Transform(appDictionary, requestDictionary);

				if (result == null) continue;

				appDictionary = result.App;
				requestDictionary = result.Request;
			}
		}

		private void InitializeApplication(IAspNetAdapterApplication appInstance, Dictionary<string, object> appDictionary, Dictionary<string, object> requestDictionary)
		{
			if (firstRun)
				lock (SyncInitLock) appInstance.Init(appDictionary, requestDictionary, ResponseCallback);
			else
				appInstance.Init(appDictionary, requestDictionary, ResponseCallback);
		}

		#region REQUEST/APPLICATION DICTIONARY INITIALIZATION
		private Dictionary<string, object> InitializeRequestDictionary()
		{
			var request = new Dictionary<string, object>();

			request[HttpAdapterConstants.SessionId] = (context.Session != null) ? context.Session.SessionID : null;
			request[HttpAdapterConstants.ServerVariables] = NameValueCollectionToDictionary(context.Request.ServerVariables);
			request[HttpAdapterConstants.RequestScheme] = context.Request.Url.Scheme;
			request[HttpAdapterConstants.RequestIsSecure] = context.Request.IsSecureConnection;
			request[HttpAdapterConstants.RequestHeaders] = StringToDictionary(context.Request.ServerVariables["ALL_HTTP"], '\n', ':');
			request[HttpAdapterConstants.RequestMethod] = context.Request.HttpMethod;

			if (context.Request.PhysicalApplicationPath != null)
				request[HttpAdapterConstants.RequestPathBase] = context.Request.PhysicalApplicationPath.TrimEnd('\\');

			request[HttpAdapterConstants.RequestPath] = (string.IsNullOrEmpty(context.Request.Path)) ? "/" : (context.Request.Path.Length > 1) ? context.Request.Path.TrimEnd('/') : context.Request.Path;
			request[HttpAdapterConstants.RequestPathSegments] = SplitPathSegments(context.Request.Path);
			request[HttpAdapterConstants.RequestQueryString] = NameValueCollectionToDictionary(context.Request.Unvalidated.QueryString);
			request[HttpAdapterConstants.RequestQueryStringCallback] = new Func<string, bool, string>(RequestQueryStringGetCallback);
			request[HttpAdapterConstants.RequestCookie] = StringToDictionary(context.Request.ServerVariables["HTTP_COOKIE"], ';', '=');

			try
			{
				request[HttpAdapterConstants.RequestBody] = StringToDictionary(new StreamReader(context.Request.InputStream).ReadToEnd(), '&', '=');
			}
			catch
			{
				request[HttpAdapterConstants.RequestBody] = null;
			}

			request[HttpAdapterConstants.RequestForm] = NameValueCollectionToDictionary(context.Request.Unvalidated.Form);
			request[HttpAdapterConstants.RequestFormCallback] = new Func<string, bool, string>(RequestFormGetCallback);
			request[HttpAdapterConstants.RequestIpAddress] = GetIpAddress();
			request[HttpAdapterConstants.RequestClientCertificate] = new X509Certificate2(context.Request.ClientCertificate.Certificate);
			request[HttpAdapterConstants.RequestFiles] = GetRequestFiles();
			request[HttpAdapterConstants.RequestUrl] = context.Request.Url;
			request[HttpAdapterConstants.RequestUrlAuthority] = context.Request.Url.Authority;
			request[HttpAdapterConstants.RequestIdentity] = (context.User != null) ? context.User.Identity.Name : null;

			return request;
		}

		private Dictionary<string, object> InitializeApplicationDictionary()
		{
			var application = new Dictionary<string, object>();
			var serverError = context.Server.GetLastError();

			if (serverError != null)
				context.Server.ClearError();

			// This debug mode tells us if the assembly is in debug mode, not if the web.config is.
			// TODO: We may want to be able to tell if the web.config is in debug mode as well if
			//       the assembly is.
			application[HttpAdapterConstants.DebugModeAssembly] = debugModeAssembly;
			application[HttpAdapterConstants.DebugModeASPNET] = context.IsDebuggingEnabled;
			application[HttpAdapterConstants.User] = context.User;
			application[HttpAdapterConstants.ApplicationSessionStoreAddCallback] = new Action<string, object>(ApplicationSessionStoreAddCallback);
			application[HttpAdapterConstants.ApplicationSessionStoreRemoveCallback] = new Action<string>(ApplicationSessionStoreRemoveCallback);
			application[HttpAdapterConstants.ApplicationSessionStoreGetCallback] = new Func<string, object>(ApplicationSessionStoreGetCallback);
			application[HttpAdapterConstants.UserSessionStoreAddCallback] = new Action<string, object>(UserSessionStoreAddCallback);
			application[HttpAdapterConstants.UserSessionStoreRemoveCallback] = new Action<string>(UserSessionStoreRemoveCallback);
			application[HttpAdapterConstants.UserSessionStoreGetCallback] = new Func<string, object>(UserSessionStoreGetCallback);
			application[HttpAdapterConstants.UserSessionStoreAbandonCallback] = new Action(UserSessionStoreAbandonCallback);
			application[HttpAdapterConstants.CacheAddCallback] = new Action<string, object, DateTime>(CacheAddCallback);
			application[HttpAdapterConstants.CacheGetCallback] = new Func<string, object>(CacheGetCallback);
			application[HttpAdapterConstants.CacheRemoveCallback] = new Action<string>(CacheRemoveCallback);
			application[HttpAdapterConstants.CookieAddCallback] = new Action<HttpCookie>(CookieAddCallback);
			application[HttpAdapterConstants.CookieGetCallback] = new Func<string, HttpCookie>(CookieGetCallback);
			application[HttpAdapterConstants.CookieRemoveCallback] = new Action<string>(CookieRemoveCallback);
			application[HttpAdapterConstants.ResponseRedirectCallback] = new Action<string, Dictionary<string, string>>(ResponseRedirectCallback);
			application[HttpAdapterConstants.ServerError] = serverError;
			application[HttpAdapterConstants.ServerErrorStackTrace] = (serverError != null) ? serverError.GetStackTrace() : null;

			return application;
		}
		#endregion

		#region MISCELLANEOUS
		private static string[] SplitPathSegments(string path)
		{
			var tpath = "/";

			if (!string.IsNullOrEmpty(path))
				tpath = path;

			string[] segments = null;

			if (tpath.Length > 1)
			{
				tpath = tpath.Trim('/');
				segments = tpath.Split(new char[] { '/' }, StringSplitOptions.RemoveEmptyEntries);
			}
			else
				segments = new string[] { "/" };

			return segments;
		}

		// Adapted from: http://ardalis.com/determine-whether-an-assembly-was-compiled-in-debug-mode
		private static bool IsAssemblyDebugBuild(Assembly assembly)
		{
			if (assembly == null) throw new ArgumentNullException("assembly");

			var debuggableAttribute = assembly.GetCustomAttributes(typeof(DebuggableAttribute), false)
																				.FirstOrDefault() as DebuggableAttribute;

			return debuggableAttribute != null && debuggableAttribute.IsJITTrackingEnabled;
		}

		private Dictionary<string, string> NameValueCollectionToDictionary(NameValueCollection nvc)
		{
			var result = new Dictionary<string, string>();

			foreach (var key in nvc.AllKeys.Where(key => !result.ContainsKey(key)))
				result[key] = nvc.Get(key);

			return result;
		}

		private Dictionary<string, string> StringToDictionary(string value, char splitOn, char delimiter)
		{
			var result = new Dictionary<string, string>();

			if (string.IsNullOrEmpty(value)) return result;

			foreach (var arr in value.Split(new char[] { splitOn }, StringSplitOptions.RemoveEmptyEntries)
															 .Select(x => x.Split(delimiter))
															 .Where(arr => !result.ContainsKey(arr[0])))
				result.Add(arr[0].Trim(), arr[1].Trim());

			return result;
		}

		private string GetIpAddress()
		{
			// This method is based on the following example at StackOverflow:
			// http://stackoverflow.com/questions/735350/how-to-get-a-users-client-ip-address-in-asp-net
			var ip = context.Request.ServerVariables["HTTP_X_FORWARDED_FOR"];

			return (string.IsNullOrEmpty(ip)) ? context.Request.ServerVariables["REMOTE_ADDR"] : ip.Split(',')[0];
		}

		private List<PostedFile> GetRequestFiles()
		{
			var postedFiles = context.Request.Files.Cast<HttpPostedFileBase>()
																.Select(x => new PostedFile
																{
																	ContentType = x.ContentType,
																	FileName = x.FileName,
																	FileBytes = ReadStream(x.InputStream)
																})
																.ToList();

			return (postedFiles.Count > 0) ? postedFiles : null;
		}

		// It's likely I grabbed this from Stackoverflow but I cannot remember the
		// exact source. It's been adapted (slightly modified).
		public byte[] ReadStream(Stream stream)
		{
			var length = (int)stream.Length;
			var buffer = new byte[length];
			int count, sum = 0;

			while ((count = stream.Read(buffer, sum, length - sum)) > 0)
				sum += count;

			return buffer;
		}
		#endregion

		public void SendResponse(Dictionary<string, object> response)
		{
			response.ThrowIfArgumentNull();

			timer.Stop();

			if (!response.ContainsKey(HttpAdapterConstants.ResponseStatus))
				response[HttpAdapterConstants.ResponseStatus] = 200;

			if (!response.ContainsKey(HttpAdapterConstants.ResponseContentType))
				response[HttpAdapterConstants.ResponseContentType] = "text/html";

			context.Response.AddHeader("X_FRAME_OPTIONS", "SAMEORIGIN");
			context.Response.StatusCode = (int)response[HttpAdapterConstants.ResponseStatus];
			context.Response.ContentType = response[HttpAdapterConstants.ResponseContentType].ToString();

			if (response.ContainsKey(HttpAdapterConstants.ResponseHeaders) &&
					response[HttpAdapterConstants.ResponseHeaders] != null)
			{
				var dictionary = response[HttpAdapterConstants.ResponseHeaders] as Dictionary<string, string>;

				if (dictionary != null)
				{
					foreach (var kvp in dictionary)
						context.Response.AddHeader(kvp.Key, kvp.Value);
				}
			}

			try
			{
				var rb = response[HttpAdapterConstants.ResponseBody] as string;
				if (rb != null)
				{
					var result = rb;

					if (response[HttpAdapterConstants.ResponseContentType].ToString() == "text/html")
					{
						var seconds = (double)timer.ElapsedTicks / Stopwatch.Frequency;
						var doc = new HtmlDocument();
						doc.LoadHtml(result);
						var titleNode = doc.DocumentNode.SelectSingleNode("//title//text()") as HtmlTextNode;

						if (titleNode != null)
						{
							titleNode.Text = titleNode.Text + string.Format(" ({0:F4} sec)", seconds);
							result = doc.DocumentNode.InnerHtml;
						}
					}

					context.Response.Write(result);
				}
				else
				{
					var bytes = response[HttpAdapterConstants.ResponseBody] as byte[];
					if (bytes != null)
						context.Response.BinaryWrite(bytes);
				}
			}
			catch (Exception e)
			{
				if (!(e is ThreadAbortException))
				{
					if (response.ContainsKey(HttpAdapterConstants.ResponseErrorCallback) &&
							response[HttpAdapterConstants.ResponseErrorCallback] != null)
					{
						var action = response[HttpAdapterConstants.ResponseErrorCallback] as Action<Exception>;
						if (action != null)
							action(e);
					}
				}
			}

			context.Response.End();
		}

		public void SendRedirectResponse(string path, Dictionary<string, string> headers)
		{
			if (headers != null)
			{
				foreach (var kvp in headers)
					context.Response.AddHeader(kvp.Key, kvp.Value);
			}

			context.Response.Redirect(path, true);
		}

		#region CALLBACKS
		private void ResponseCallback(Dictionary<string, object> response)
		{
			if (OnComplete == null) return;

			OnComplete(this, new ResponseEventArgs(false, response));
			OnComplete = null;
		}

		private void ResponseRedirectCallback(string path, Dictionary<string, string> headers)
		{
			if (OnComplete == null) return;

			OnComplete(this, new ResponseEventArgs(true, new RedirectInfo(path, headers)));
			OnComplete = null;
		}

		#region APPLICATION STATE
		private void ApplicationSessionStoreAddCallback(string key, object value)
		{
			if (string.IsNullOrEmpty(key)) return;

			context.Application.Lock();
			context.Application[key] = value;
			context.Application.UnLock();
		}

		private void ApplicationSessionStoreRemoveCallback(string key)
		{
			if (string.IsNullOrEmpty(key) || !context.Application.AllKeys.Contains(key)) return;

			context.Application.Lock();
			context.Application.Remove(key);
			context.Application.UnLock();
		}

		private object ApplicationSessionStoreGetCallback(string key)
		{
			if (!string.IsNullOrEmpty(key) && context.Application.AllKeys.Contains(key))
				return context.Application[key];

			return null;
		}
		#endregion

		#region SESSION STATE
		private void UserSessionStoreAddCallback(string key, object value)
		{
			if (context.Session == null && string.IsNullOrEmpty(key)) return;

			// ReSharper disable once PossibleNullReferenceException
			context.Session[key] = value;
		}

		private void UserSessionStoreRemoveCallback(string key)
		{
			if (context.Session == null && string.IsNullOrEmpty(key)) return;

			// ReSharper disable once PossibleNullReferenceException
			context.Session.Remove(key);
		}

		private object UserSessionStoreGetCallback(string key)
		{
			if (context.Session == null && string.IsNullOrEmpty(key)) return null;

			// ReSharper disable once PossibleNullReferenceException
			return context.Session[key];
		}

		private void UserSessionStoreAbandonCallback()
		{
			if (context.Session == null) return;

			context.Session.Abandon();
		}
		#endregion

		#region CACHE STATE
		private void CacheAddCallback(string key, object value, DateTime expiresOn)
		{
			if (!string.IsNullOrEmpty(key))
				context.Cache.Insert(key, value, null, expiresOn, Cache.NoSlidingExpiration);
		}

		private object CacheGetCallback(string key)
		{
			return !string.IsNullOrEmpty(key) ? context.Cache.Get(key) : null;
		}

		private void CacheRemoveCallback(string key)
		{
			if (string.IsNullOrEmpty(key)) return;

			context.Cache.Remove(key);
		}
		#endregion

		#region FORM / QUERYSTRING (FOR OBTAINING VALIDATED VALUES)
		private string RequestFormGetCallback(string key, bool validated)
		{
			string result = null;

			if (validated)
			{
				try
				{
					result = context.Request.Form[key];
				}
				catch
				{
					// <httpRuntime encoderType="Microsoft.Security.Application.AntiXssEncoder, AntiXssLibrary"/>
					result = HttpUtility.HtmlEncode(context.Request.Unvalidated.Form[key]);
				}
			}
			else
				result = context.Request.Unvalidated.Form[key];

			return result;
		}

		private string RequestQueryStringGetCallback(string key, bool validated)
		{
			string result = null;

			if (validated)
			{
				try
				{
					result = context.Request.QueryString[key];
				}
				catch
				{
					// <httpRuntime encoderType="Microsoft.Security.Application.AntiXssEncoder, AntiXssLibrary"/>
					result = HttpUtility.HtmlEncode(context.Request.Unvalidated.QueryString[key]);
				}
			}
			else
				result = context.Request.Unvalidated.QueryString[key];

			return result;
		}
		#endregion

		#region COOKIES
		private void CookieAddCallback(HttpCookie cookie)
		{
			if (cookie != null)
				context.Response.Cookies.Add(cookie);
		}

		private HttpCookie CookieGetCallback(string name)
		{
			return context.Request.Cookies.AllKeys.Contains(name) ? context.Request.Cookies[name] : null;
		}

		private void CookieRemoveCallback(string name)
		{
			if (string.IsNullOrEmpty(name)) return;

			context.Response.Cookies.Remove(name);
		}
		#endregion
		#endregion
	}
	#endregion

	#region EXTENSION METHODS
	public static class ExtensionMethods
	{
		public static void ThrowIfArgumentNull<T>(this T t, string message = null)
		{
			var argName = t.GetType().Name;

			// ReSharper disable once CompareNonConstrainedGenericWithNull
			if (t is ValueType && t == null)
				throw new ArgumentNullException(argName, message);
			else if (t is string && string.IsNullOrEmpty(t as string))
				throw new ArgumentException(argName, message);
		}

		public static string GetStackTrace(this Exception exception)
		{
			var stacktraceBuilder = new StringBuilder();
			var trace = new System.Diagnostics.StackTrace(exception.InnerException ?? exception, true);
			var stackFrames = trace.GetFrames();

			if (stackFrames == null) return null;

			foreach (var sf in stackFrames.Where(sf => !string.IsNullOrEmpty(sf.GetFileName())))
			{
				stacktraceBuilder.AppendFormat("<b>method:</b> {0} <b>file:</b> {1}<br />", sf.GetMethod().Name,
					Path.GetFileName(sf.GetFileName()));
			}

			return stacktraceBuilder.ToString();
		}
	}
	#endregion

	public static class Utility
	{
		public static IEnumerable<Assembly> GetAssemblies(Predicate<Assembly> predicate = null)
		{
			return AppDomain.CurrentDomain
							.GetAssemblies()
							.AsParallel()
							.Where(x => (predicate == null) || predicate(x));
		}

		// Thanks to: http://stackoverflow.com/a/11915414/170217
		public static IEnumerable<Type> GetLoadableTypes(this Assembly assembly)
		{
			assembly.ThrowIfArgumentNull("assembly");

			try
			{
				return assembly.GetTypes();
			}
			catch (ReflectionTypeLoadException e)
			{
				return e.Types.Where(t => t != null);
			}
		}
	}
}