//
// AspNetAdapter 
//
// Description: 
//
//	A thin wrapper around the ASP.NET Request/Reponse objects to make it easier
//	to disconnect applications from the intrinsics of the HttpContext.
//
// Requirements: .NET 4.5
//
// Date: 30 March 2014
//
// Contact Info:
//
//  Frank Hale - <frankhale@gmail.com> 
//
// An attempt to abstract away some of the common bits of the ASP.NET HttpContext.
//
// I initially looked at OWIN to provide the abstraction that I wanted but I 
// found it to be a bit more complex than I was hoping for. What I was looking
// for was something a bit easier to strap in, something that exposed some 
// simple types and was as braindead easy as I could come up with.
//
// Usage:
// 
//  Write a class that implements IAspNetAdapterApplication and add in the bits
//  to set the ASP.NET Adapter handler and module in your web.config.
//
//	<system.web>
//		<httpHandlers>
//			<add verb="*" path="*" validate="false" type="AspNetAdapter.AspNetAdapterHandler"/>
//		</httpHandlers>
//		<httpModules>
//			<add type="AspNetAdapter.AspNetAdapterModule" name="AspNetAdapterModule" />
//		</httpModules>
//	</system.web>
//
// The IAspNetAdapterApplication provides the following method:
//
//  void Init(Dictionary<string, object> app, 
//            Dictionary<string, object> request, 
//            Action<Dictionary<string, object>> response);
//
// The Init() method gets an app and request dictionary. The app dictionary 
// contains callbacks for things like adding to the Session or getting something
// from the Session. The request dictionary contains the information you'd 
// expect from an http request and finally the response callback takes a 
// dictionary with response values. All of the dictionary keys can be found in
// the HttpAdapterConstants class.
//

#region LICENSE - GPL version 3 <http://www.gnu.org/licenses/gpl-3.0.html>
//
// GNU GPLv3 quick guide: http://www.gnu.org/licenses/quick-guide-gplv3.html
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
#endregion

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

	#region MIDDLEWARE
	public class MiddlewareResult
	{
		public Dictionary<string, object> App { get; set; }
		public Dictionary<string, object> Request { get; set; }
	}

	public interface IAspNetAdapterMiddleware
	{
		MiddlewareResult Transform(Dictionary<string, object> app,
															 Dictionary<string, object> request);
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
	}
	#endregion

	#endregion

	#region ASP.NET ADAPTER
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
		public static readonly string DebugMode = "DebugMode";
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

	// An application that wants to hook into ASP.NET and be sent the goodies that the HttpContextAdapter has to offer
	public interface IAspNetAdapterApplication
	{
		void Init(Dictionary<string, object> app,
							Dictionary<string, object> request,
							Action<Dictionary<string, object>> response);
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
		private static readonly AspNetAdapterWebConfig _webConfig = ConfigurationManager.GetSection("aspNetAdapter") as AspNetAdapterWebConfig;
		private Stopwatch _timer;
		private HttpContext _context;
		private bool _firstRun, _debugMode;
		private const string AspNetApplicationTypeSessionName = "__AspNetApplicationType";
		private const string AspNetMiddlewareSessionName = "__AspNetMiddleware";

		public event EventHandler<ResponseEventArgs> OnComplete;

		public void Init(HttpContext ctx)
		{
			_context = ctx;
			_timer = new Stopwatch();
			_timer.Start();
			_context = ctx;
			_firstRun = Convert.ToBoolean(_context.Application["__SyncInitLock"]);

			Type adapterApp = null;

			if (_context.Application[AspNetApplicationTypeSessionName] == null)
			{
				_context.Application["__SyncInitLock"] = true;

				// Look for a class inside the executing assembly that implements IAspNetAdapterApplication
				var app = Utility.GetAssemblies(x => x.GetName().Name != "DotNetOpenAuth")
									.SelectMany(x => x.GetTypes()
													.Where(y => y.GetInterfaces().FirstOrDefault(i => i.IsInterface && i.UnderlyingSystemType == typeof(IAspNetAdapterApplication)) != null))
													.FirstOrDefault();

				if (app == null)
					throw new Exception("Failed to find an assembly the IAspNetAdapterApplication interface");

				adapterApp = app;

				_context.Application.Lock();
				_context.Application[AspNetApplicationTypeSessionName] = adapterApp;
				_context.Application.UnLock();

			}
			else
				adapterApp = ctx.Application[AspNetApplicationTypeSessionName] as Type;

			if (adapterApp == null) return;

			_debugMode = IsAssemblyDebugBuild(adapterApp.Assembly);

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
			if (_webConfig != null)
			{
				IEnumerable<Type> middleware = null;

				if (_context.Application[AspNetMiddlewareSessionName] == null)
				{
					middleware = Utility.GetAssemblies()
															.SelectMany(x => x.GetTypes().Where(y => y.GetInterfaces()
																																				.FirstOrDefault(i => i.IsInterface && i.UnderlyingSystemType == typeof(IAspNetAdapterMiddleware)) != null));
				}
				else
					middleware = _context.Application[AspNetMiddlewareSessionName] as IEnumerable<Type>;

				if (middleware == null) return;

				foreach (AspNetAdapterMiddlewareConfigurationElement mw in _webConfig.AspNetAdapterMiddlewareCollection)
				{
					var m = middleware.FirstOrDefault(x => x.FullName == mw.Type);

					if (m != null)
					{
						// The big question is, should this instance be cached?
						var min = (IAspNetAdapterMiddleware)Activator.CreateInstance(m);
						var result = min.Transform(appDictionary, requestDictionary);

						if (result != null)
						{
							appDictionary = result.App;
							requestDictionary = result.Request;
						}
					}
				}
			}
		}

		private void InitializeApplication(IAspNetAdapterApplication appInstance, Dictionary<string, object> appDictionary, Dictionary<string, object> requestDictionary)
		{
			if (_firstRun)
				lock (SyncInitLock) appInstance.Init(appDictionary, requestDictionary, ResponseCallback);
			else
				appInstance.Init(appDictionary, requestDictionary, ResponseCallback);
		}

		#region REQUEST/APPLICATION DICTIONARY INITIALIZATION
		private Dictionary<string, object> InitializeRequestDictionary()
		{
			var request = new Dictionary<string, object>();

			request[HttpAdapterConstants.SessionId] = (_context.Session != null) ? _context.Session.SessionID : null;
			request[HttpAdapterConstants.ServerVariables] = NameValueCollectionToDictionary(_context.Request.ServerVariables);
			request[HttpAdapterConstants.RequestScheme] = _context.Request.Url.Scheme;
			request[HttpAdapterConstants.RequestIsSecure] = _context.Request.IsSecureConnection;
			request[HttpAdapterConstants.RequestHeaders] = StringToDictionary(_context.Request.ServerVariables["ALL_HTTP"], '\n', ':');
			request[HttpAdapterConstants.RequestMethod] = _context.Request.HttpMethod;

			if (_context.Request.PhysicalApplicationPath != null)
				request[HttpAdapterConstants.RequestPathBase] = _context.Request.PhysicalApplicationPath.TrimEnd('\\');

			request[HttpAdapterConstants.RequestPath] = (string.IsNullOrEmpty(_context.Request.Path)) ? "/" : (_context.Request.Path.Length > 1) ? _context.Request.Path.TrimEnd('/') : _context.Request.Path;
			request[HttpAdapterConstants.RequestPathSegments] = SplitPathSegments(_context.Request.Path);
			request[HttpAdapterConstants.RequestQueryString] = NameValueCollectionToDictionary(_context.Request.Unvalidated.QueryString);
			request[HttpAdapterConstants.RequestQueryStringCallback] = new Func<string, bool, string>(RequestQueryStringGetCallback);
			request[HttpAdapterConstants.RequestCookie] = StringToDictionary(_context.Request.ServerVariables["HTTP_COOKIE"], ';', '=');

			try
			{
				request[HttpAdapterConstants.RequestBody] = StringToDictionary(new StreamReader(_context.Request.InputStream).ReadToEnd(), '&', '=');
				//request[HttpAdapterConstants.RequestBody] = (_context.Request.InputStream != null) ? StringToDictionary(new StreamReader(_context.Request.InputStream).ReadToEnd(), '&', '=') : null;
			}
			catch
			{
				request[HttpAdapterConstants.RequestBody] = null;
			}

			request[HttpAdapterConstants.RequestForm] = NameValueCollectionToDictionary(_context.Request.Unvalidated.Form);
			request[HttpAdapterConstants.RequestFormCallback] = new Func<string, bool, string>(RequestFormGetCallback);
			request[HttpAdapterConstants.RequestIpAddress] = GetIpAddress();
			request[HttpAdapterConstants.RequestClientCertificate] = new X509Certificate2(_context.Request.ClientCertificate.Certificate);
			request[HttpAdapterConstants.RequestFiles] = GetRequestFiles();
			request[HttpAdapterConstants.RequestUrl] = _context.Request.Url;
			request[HttpAdapterConstants.RequestUrlAuthority] = _context.Request.Url.Authority;
			request[HttpAdapterConstants.RequestIdentity] = (_context.User != null) ? _context.User.Identity.Name : null;

			return request;
		}

		private Dictionary<string, object> InitializeApplicationDictionary()
		{
			var application = new Dictionary<string, object>();
			var serverError = _context.Server.GetLastError();

			if (serverError != null)
				_context.Server.ClearError();

			application[HttpAdapterConstants.DebugMode] = _debugMode;
			application[HttpAdapterConstants.User] = _context.User;
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

			foreach (var arr in value.Split(new char[] { splitOn }, StringSplitOptions.RemoveEmptyEntries).Select(x => x.Split(delimiter)).Where(arr => !result.ContainsKey(arr[0])))
				result.Add(arr[0].Trim(), arr[1].Trim());

			return result;
		}

		private string GetIpAddress()
		{
			// This method is based on the following example at StackOverflow:
			// http://stackoverflow.com/questions/735350/how-to-get-a-users-client-ip-address-in-asp-net
			var ip = _context.Request.ServerVariables["HTTP_X_FORWARDED_FOR"];

			return (string.IsNullOrEmpty(ip)) ? _context.Request.ServerVariables["REMOTE_ADDR"] : ip.Split(',')[0];
		}

		private List<PostedFile> GetRequestFiles()
		{
			var postedFiles = (from HttpPostedFileBase pf in _context.Request.Files
												 select new PostedFile()
												 {
													 ContentType = pf.ContentType,
													 FileName = pf.FileName,
													 FileBytes = ReadStream(pf.InputStream)
												 }).ToList();

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

			_timer.Stop();

			if (!response.ContainsKey(HttpAdapterConstants.ResponseStatus))
				response[HttpAdapterConstants.ResponseStatus] = 200;

			if (!response.ContainsKey(HttpAdapterConstants.ResponseContentType))
				response[HttpAdapterConstants.ResponseContentType] = "text/html";

			_context.Response.AddHeader("X_FRAME_OPTIONS", "SAMEORIGIN");
			_context.Response.StatusCode = (int)response[HttpAdapterConstants.ResponseStatus];
			_context.Response.ContentType = response[HttpAdapterConstants.ResponseContentType].ToString();

			if (response.ContainsKey(HttpAdapterConstants.ResponseHeaders) &&
					response[HttpAdapterConstants.ResponseHeaders] != null)
			{
				var dictionary = response[HttpAdapterConstants.ResponseHeaders] as Dictionary<string, string>;

				if (dictionary != null)
				{
					foreach (var kvp in dictionary)
						_context.Response.AddHeader(kvp.Key, kvp.Value);
				}
			}

			try
			{
				if (response[HttpAdapterConstants.ResponseBody] is string)
				{
					var result = (string)response[HttpAdapterConstants.ResponseBody];

					if (response[HttpAdapterConstants.ResponseContentType].ToString() == "text/html")
					{
						var seconds = (double)_timer.ElapsedTicks / Stopwatch.Frequency;
						var doc = new HtmlDocument();
						doc.LoadHtml(result);
						var titleNode = doc.DocumentNode.SelectSingleNode("//title//text()") as HtmlTextNode;

						if (titleNode != null)
						{
							titleNode.Text = titleNode.Text + string.Format(" ({0:F4} sec)", seconds);
							result = doc.DocumentNode.InnerHtml;
						}
					}

					_context.Response.Write(result);
				}
				else
				{
					var bytes = response[HttpAdapterConstants.ResponseBody] as byte[];
					if (bytes != null)
						_context.Response.BinaryWrite(bytes);
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

			_context.Response.End();
		}

		public void SendRedirectResponse(string path, Dictionary<string, string> headers)
		{
			if (headers != null)
			{
				foreach (var kvp in headers)
					_context.Response.AddHeader(kvp.Key, kvp.Value);
			}

			_context.Response.Redirect(path, true);
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

			_context.Application.Lock();
			_context.Application[key] = value;
			_context.Application.UnLock();
		}

		private void ApplicationSessionStoreRemoveCallback(string key)
		{
			if (string.IsNullOrEmpty(key) || !_context.Application.AllKeys.Contains(key)) return;

			_context.Application.Lock();
			_context.Application.Remove(key);
			_context.Application.UnLock();
		}

		private object ApplicationSessionStoreGetCallback(string key)
		{
			if (!string.IsNullOrEmpty(key) && _context.Application.AllKeys.Contains(key))
				return _context.Application[key];

			return null;
		}
		#endregion

		#region SESSION STATE
		private void UserSessionStoreAddCallback(string key, object value)
		{
			if (_context.Session == null && string.IsNullOrEmpty(key)) return;

			// ReSharper disable once PossibleNullReferenceException
			_context.Session[key] = value;
		}

		private void UserSessionStoreRemoveCallback(string key)
		{
			if (_context.Session == null && string.IsNullOrEmpty(key)) return;

			// ReSharper disable once PossibleNullReferenceException
			_context.Session.Remove(key);
		}

		private object UserSessionStoreGetCallback(string key)
		{
			if (_context.Session == null && string.IsNullOrEmpty(key)) return null;

			// ReSharper disable once PossibleNullReferenceException
			return _context.Session[key];
		}

		private void UserSessionStoreAbandonCallback()
		{
			if (_context.Session == null) return;

			_context.Session.Abandon();
		}
		#endregion

		#region CACHE STATE
		private void CacheAddCallback(string key, object value, DateTime expiresOn)
		{
			if (!string.IsNullOrEmpty(key))
				_context.Cache.Insert(key, value, null, expiresOn, Cache.NoSlidingExpiration);
		}

		private object CacheGetCallback(string key)
		{
			return !string.IsNullOrEmpty(key) ? _context.Cache.Get(key) : null;
		}

		private void CacheRemoveCallback(string key)
		{
			if (string.IsNullOrEmpty(key)) return;

			_context.Cache.Remove(key);
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
					result = _context.Request.Form[key];
				}
				catch
				{
					// <httpRuntime encoderType="Microsoft.Security.Application.AntiXssEncoder, AntiXssLibrary"/>
					result = HttpUtility.HtmlEncode(_context.Request.Unvalidated.Form[key]);
				}
			}
			else
				result = _context.Request.Unvalidated.Form[key];

			return result;
		}

		private string RequestQueryStringGetCallback(string key, bool validated)
		{
			string result = null;

			if (validated)
			{
				try
				{
					result = _context.Request.QueryString[key];
				}
				catch
				{
					// <httpRuntime encoderType="Microsoft.Security.Application.AntiXssEncoder, AntiXssLibrary"/>
					result = HttpUtility.HtmlEncode(_context.Request.Unvalidated.QueryString[key]);
				}
			}
			else
				result = _context.Request.Unvalidated.QueryString[key];

			return result;
		}
		#endregion

		#region COOKIES
		private void CookieAddCallback(HttpCookie cookie)
		{
			if (cookie != null)
				_context.Response.Cookies.Add(cookie);
		}

		private HttpCookie CookieGetCallback(string name)
		{
			return _context.Request.Cookies.AllKeys.Contains(name) ? _context.Request.Cookies[name] : null;
		}

		private void CookieRemoveCallback(string name)
		{
			if (string.IsNullOrEmpty(name)) return;

			_context.Response.Cookies.Remove(name);
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
			//NOTE: DotNetOpenAuth depends on System.Web.Mvc 1.0 if you don't have MVC 1.0
			//      this is gonna barf and throw an exception...

			return AppDomain.CurrentDomain
							.GetAssemblies()
							.AsParallel()
 						  .Where(x => (predicate == null) || predicate(x));
		}
	}
}