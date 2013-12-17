//
// AspNetAdapter 
//
// Description: 
//
//	A thin wrapper around the ASP.NET Request/Reponse objects to make it easier
//	to disconnect applications from the intrinsics of the HttpContext.
//
// Requirements: .NET 4 or higher
//
// Date: 17 December 2013
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

using System;
using System.Collections.Generic;
using System.Collections.Specialized;
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
using HtmlAgilityPack;
//using Microsoft.Web.Infrastructure.DynamicValidationHelper;

//#if LIBRARY
//using System.Runtime.InteropServices;

//[assembly: AssemblyTitle("AspNetAdapter")]
//[assembly: AssemblyDescription("An ASP.NET HttpContext Adapter")]
//[assembly: AssemblyCompany("Frank Hale")]
//[assembly: AssemblyProduct("AspNetAdapter")]
//[assembly: AssemblyCopyright("Copyright © 2012-2013 | LICENSE GNU GPLv3")]
//[assembly: ComVisible(false)]
//[assembly: CLSCompliant(true)]
//[assembly: AssemblyVersion("0.0.22.0")]
//#endif

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
				ResponseEventArgs args = (e as ResponseEventArgs);

				if (args.Redirect)
				{
					var redirectInfo = args.Data as RedirectInfo;
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
			{
				frameworkTask.ContinueWith(x => cb(x), TaskContinuationOptions.ExecuteSynchronously);
			}

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
				ResponseEventArgs args = (e as ResponseEventArgs);

				if (args.Redirect)
				{
					var redirectInfo = args.Data as RedirectInfo;
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
		public static readonly string SessionID = "SessionID";
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
		public static readonly string RequestIPAddress = "RequestIPAddress";
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
		private static object syncInitLock = new object();

		private Stopwatch timer;
		private HttpContext context;
		private bool firstRun, debugMode;
		private static string AspNetApplicationTypeSessionName = "__AspNetApplicationType";

		public event EventHandler<ResponseEventArgs> OnComplete;

		public HttpContextAdapter() { }

		public void Init(HttpContext ctx)
		{
			timer = new Stopwatch();
			timer.Start();

			context = ctx;

			firstRun = Convert.ToBoolean(context.Application["__SyncInitLock"]);

			Type adapterApp = null;

			if (context.Application[AspNetApplicationTypeSessionName] == null)
			{
				context.Application["__SyncInitLock"] = true;

                try
                {

                    // Look for a class inside the executing assembly that implements IAspNetAdapterApplication
                    var apps = AppDomain.CurrentDomain
                                                            .GetAssemblies()
                                                            .AsParallel()
// DotNetOpenAuth depends on System.Web.Mvc which is not referenced, this will fail if we don't eliminate it
														.Where(x => x.GetName().Name != "DotNetOpenAuth") 
.SelectMany(x => x.GetTypes()
                                                                                                .Where(y => y.GetInterfaces()
                                                                                                                         .FirstOrDefault(i => i.IsInterface && i.UnderlyingSystemType == typeof(IAspNetAdapterApplication)) != null));

                    if (apps != null)
                    {
                        if (apps.Count() > 1)
                            throw new Exception("The executing assembly can contain only one application utilizing AspNetAdapter");

                        adapterApp = apps.FirstOrDefault() as Type;

                        context.Application.Lock();
                        context.Application[AspNetApplicationTypeSessionName] = adapterApp;
                        context.Application.UnLock();
                    }
                    else
                        throw new Exception("Could not find any apps utilizing AspNetAdapter");
                }
                catch
                {
                    throw;
                }
			}
			else
				adapterApp = ctx.Application[AspNetApplicationTypeSessionName] as Type;

			debugMode = IsAssemblyDebugBuild(adapterApp.Assembly);

			Dictionary<string, object> app = InitializeApplicationDictionary();
			Dictionary<string, object> request = InitializeRequestDictionary();

			IAspNetAdapterApplication _appInstance = (IAspNetAdapterApplication)Activator.CreateInstance(adapterApp);

			if (firstRun)
				lock (syncInitLock) _appInstance.Init(app, request, ResponseCallback);
			else
				_appInstance.Init(app, request, ResponseCallback);
		}

		#region REQUEST/APPLICATION DICTIONARY INITIALIZATION
		private Dictionary<string, object> InitializeRequestDictionary()
		{
			Dictionary<string, object> request = new Dictionary<string, object>();

			request[HttpAdapterConstants.SessionID] = (context.Session != null) ? context.Session.SessionID : null;
			request[HttpAdapterConstants.ServerVariables] = NameValueCollectionToDictionary(context.Request.ServerVariables);
			request[HttpAdapterConstants.RequestScheme] = context.Request.Url.Scheme;
			request[HttpAdapterConstants.RequestIsSecure] = context.Request.IsSecureConnection;
			request[HttpAdapterConstants.RequestHeaders] = StringToDictionary(context.Request.ServerVariables["ALL_HTTP"], '\n', ':');
			request[HttpAdapterConstants.RequestMethod] = context.Request.HttpMethod;
			request[HttpAdapterConstants.RequestPathBase] = context.Request.PhysicalApplicationPath.TrimEnd('\\');
			request[HttpAdapterConstants.RequestPath] = (string.IsNullOrEmpty(context.Request.Path)) ? "/" : (context.Request.Path.Length > 1) ? context.Request.Path.TrimEnd('/') : context.Request.Path;
			request[HttpAdapterConstants.RequestPathSegments] = SplitPathSegments(context.Request.Path);
            request[HttpAdapterConstants.RequestQueryString] = NameValueCollectionToDictionary(context.Request.Unvalidated.QueryString);
			request[HttpAdapterConstants.RequestQueryStringCallback] = new Func<string, bool, string>(RequestQueryStringGetCallback);
			request[HttpAdapterConstants.RequestCookie] = StringToDictionary(context.Request.ServerVariables["HTTP_COOKIE"], ';', '=');
			try
			{
				request[HttpAdapterConstants.RequestBody] = (context.Request.InputStream != null) ? StringToDictionary(new StreamReader(context.Request.InputStream).ReadToEnd(), '&', '=') : null;
			}
			catch
			{
				request[HttpAdapterConstants.RequestBody] = null;
			}
            request[HttpAdapterConstants.RequestForm] = NameValueCollectionToDictionary(context.Request.Unvalidated.Form);
			request[HttpAdapterConstants.RequestFormCallback] = new Func<string, bool, string>(RequestFormGetCallback);
			request[HttpAdapterConstants.RequestIPAddress] = GetIPAddress();
			request[HttpAdapterConstants.RequestClientCertificate] = (context.Request.ClientCertificate != null) ? new X509Certificate2(context.Request.ClientCertificate.Certificate) : null;
			request[HttpAdapterConstants.RequestFiles] = GetRequestFiles();
			request[HttpAdapterConstants.RequestUrl] = context.Request.Url;
			request[HttpAdapterConstants.RequestUrlAuthority] = context.Request.Url.Authority;
			request[HttpAdapterConstants.RequestIdentity] = (context.User != null) ? context.User.Identity.Name : null;

			return request;
		}

		private Dictionary<string, object> InitializeApplicationDictionary()
		{
			Dictionary<string, object> application = new Dictionary<string, object>();

			Exception serverError = context.Server.GetLastError();

			if (serverError != null)
				context.Server.ClearError();

			application[HttpAdapterConstants.DebugMode] = debugMode;
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
		private string[] SplitPathSegments(string path)
		{
			string _path = (string.IsNullOrEmpty(path)) ? "/" : path;
			string[] segments = null;

			if (_path.Length > 1)
			{
				_path = _path.Trim('/');
				segments = _path.Split(new char[] { '/' }, StringSplitOptions.RemoveEmptyEntries);
			}
			else
				segments = new string[] { "/" };

			return segments;
		}

		// Adapted from: http://ardalis.com/determine-whether-an-assembly-was-compiled-in-debug-mode
		private bool IsAssemblyDebugBuild(Assembly assembly)
		{
			return (assembly.GetCustomAttributes(typeof(DebuggableAttribute), false)
											.FirstOrDefault() as DebuggableAttribute)
											.IsJITTrackingEnabled;
		}

		private Dictionary<string, string> NameValueCollectionToDictionary(NameValueCollection nvc)
		{
			Dictionary<string, string> result = new Dictionary<string, string>();

			foreach (string key in nvc.AllKeys)
				if (!result.ContainsKey(key))
					result[key] = nvc.Get(key);

			return result;
		}

		private Dictionary<string, string> StringToDictionary(string value, char splitOn, char delimiter)
		{
			Dictionary<string, string> result = new Dictionary<string, string>();

			if (!string.IsNullOrEmpty(value))
			{
				foreach (string[] arr in value.Split(new char[] { splitOn }, StringSplitOptions.RemoveEmptyEntries).Select(x => x.Split(delimiter)))
					if (!result.ContainsKey(arr[0]))
						result.Add(arr[0].Trim(), arr[1].Trim());
			}

			return result;
		}

		private string GetIPAddress()
		{
			// This method is based on the following example at StackOverflow:
			// http://stackoverflow.com/questions/735350/how-to-get-a-users-client-ip-address-in-asp-net
			string ip = context.Request.ServerVariables["HTTP_X_FORWARDED_FOR"];

			return (string.IsNullOrEmpty(ip)) ? context.Request.ServerVariables["REMOTE_ADDR"] : ip.Split(',')[0];
		}

		private List<PostedFile> GetRequestFiles()
		{
			List<PostedFile> postedFiles = new List<PostedFile>();

			foreach (HttpPostedFileBase pf in context.Request.Files)
			{
				postedFiles.Add(new PostedFile()
				{
					ContentType = pf.ContentType,
					FileName = pf.FileName,
					FileBytes = ReadStream(pf.InputStream)
				});
			}

			return (postedFiles.Count > 0) ? postedFiles : null;
		}

		// It's likely I grabbed this from Stackoverflow but I cannot remember the
		// exact source.
		public byte[] ReadStream(Stream stream)
		{
			int length = (int)stream.Length;
			byte[] buffer = new byte[length];
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
				foreach (KeyValuePair<string, string> kvp in (response[HttpAdapterConstants.ResponseHeaders] as Dictionary<string, string>))
					context.Response.AddHeader(kvp.Key, kvp.Value);
			}

			try
			{
				if (response[HttpAdapterConstants.ResponseBody] is string)
				{
					string result = (string)response[HttpAdapterConstants.ResponseBody];

					if (response[HttpAdapterConstants.ResponseContentType].ToString() == "text/html")
					{
						double seconds = (double)timer.ElapsedTicks / Stopwatch.Frequency;

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
				else if (response[HttpAdapterConstants.ResponseBody] is byte[])
					context.Response.BinaryWrite((byte[])response[HttpAdapterConstants.ResponseBody]);
			}
			catch (Exception e)
			{
				if (!(e is ThreadAbortException))
				{
					if (response.ContainsKey(HttpAdapterConstants.ResponseErrorCallback) &&
							response[HttpAdapterConstants.ResponseErrorCallback] != null)
						(response[HttpAdapterConstants.ResponseErrorCallback] as Action<Exception>)(e);
				}
			}

			context.Response.End();
		}

		public void SendRedirectResponse(string path, Dictionary<string, string> headers)
		{
			if (headers != null)
			{
				foreach (KeyValuePair<string, string> kvp in headers)
					context.Response.AddHeader(kvp.Key, kvp.Value);
			}

			context.Response.Redirect(path, true);
		}

		#region CALLBACKS
		private void ResponseCallback(Dictionary<string, object> response)
		{
			if (OnComplete != null)
			{
				OnComplete(this, new ResponseEventArgs(false, response));
				OnComplete = null;
			}
		}

		private void ResponseRedirectCallback(string path, Dictionary<string, string> headers)
		{
			if (OnComplete != null)
			{
				OnComplete(this, new ResponseEventArgs(true, new RedirectInfo(path, headers)));
				OnComplete = null;
			}
		}

		#region APPLICATION STATE
		private void ApplicationSessionStoreAddCallback(string key, object value)
		{
			if (!string.IsNullOrEmpty(key))
			{
				context.Application.Lock();
				context.Application[key] = value;
				context.Application.UnLock();
			}
		}

		private void ApplicationSessionStoreRemoveCallback(string key)
		{
			if (!string.IsNullOrEmpty(key) && context.Application.AllKeys.Contains(key))
			{
				context.Application.Lock();
				context.Application.Remove(key);
				context.Application.UnLock();
			}
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
			try
			{
				if (!string.IsNullOrEmpty(key))
					context.Session[key] = value;
			}
			catch { }
		}

		private void UserSessionStoreRemoveCallback(string key)
		{
			try
			{
				if (!string.IsNullOrEmpty(key))
					context.Session.Remove(key);
			}
			catch { }
		}

		private object UserSessionStoreGetCallback(string key)
		{
			try
			{
				return context.Session[key];
			}
			catch
			{
				return null;
			}
		}

		private void UserSessionStoreAbandonCallback()
		{
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
			if (!string.IsNullOrEmpty(key))
				return context.Cache.Get(key);

			return null;
		}

		private void CacheRemoveCallback(string key)
		{
			if (!string.IsNullOrEmpty(key))
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
			if (context.Request.Cookies.AllKeys.Contains(name))
				return context.Request.Cookies[name];

			return null;
		}

		private void CookieRemoveCallback(string name)
		{
			if (!string.IsNullOrEmpty(name))
				context.Response.Cookies.Remove(name);
		}
		#endregion
		#endregion
	}
	#endregion

	#region EXTENSION METHODS
	internal static class ExtensionMethods
	{
		public static void ThrowIfArgumentNull<T>(this T t, string message = null)
		{
			string argName = t.GetType().Name;

			if (t == null)
				throw new ArgumentNullException(argName, message);
			else if ((t is string) && (t as string) == string.Empty)
				throw new ArgumentException(argName, message);
		}

		public static string GetStackTrace(this Exception exception)
		{
			StringBuilder stacktraceBuilder = new StringBuilder();

			var trace = new System.Diagnostics.StackTrace((exception.InnerException != null) ? exception.InnerException : exception, true);

			foreach (StackFrame sf in trace.GetFrames())
				if (!string.IsNullOrEmpty(sf.GetFileName()))
					stacktraceBuilder.AppendFormat("<b>method:</b> {0} <b>file:</b> {1}<br />", sf.GetMethod().Name, Path.GetFileName(sf.GetFileName()));

			return stacktraceBuilder.ToString();
		}
	}
	#endregion
}