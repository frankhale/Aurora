//
// AspNetAdapter 
//
// Description: 
//
//	A thin wrapper around the ASP.NET Request/Reponse objects to make it easier
//	to disconnect applications from the intrinsics of the HttpContext.
//
// Date: 4 March 2013
//
// Contact Info:
//
//  Frank Hale - <frankhale@gmail.com> 
//               <http://about.me/frank.hale>
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
using System.Web;
using System.Web.SessionState;
using Microsoft.Web.Infrastructure.DynamicValidationHelper;

//#if LIBRARY
//using System.Runtime.InteropServices;

//[assembly: AssemblyTitle("AspNetAdapter")]
//[assembly: AssemblyDescription("An ASP.NET HttpContext Adapter")]
//[assembly: AssemblyCompany("Frank Hale")]
//[assembly: AssemblyProduct("AspNetAdapter")]
//[assembly: AssemblyCopyright("Copyright © 2012-2013 | LICENSE GNU GPLv3")]
//[assembly: ComVisible(false)]
//[assembly: CLSCompliant(true)]
//[assembly: AssemblyVersion("0.0.15.0")]
//#endif

namespace AspNetAdapter
{
	#region ASP.NET HOOKS - IHTTPHANDLER / IHTTPMODULE
	public sealed class AspNetAdapterHandler : IHttpHandler, IRequiresSessionState
	{
		public bool IsReusable
		{
			get
			{
				return false;
			}
		}

		public void ProcessRequest(HttpContext context)
		{
			new HttpContextAdapter(context);
		}
	}

	public sealed class AspNetAdapterModule : IHttpModule
	{
		public void Dispose() { }

		public void Init(HttpApplication context)
		{
			context.Error += new EventHandler(app_Error);
		}

		private void app_Error(object sender, EventArgs e)
		{
			HttpContext context = HttpContext.Current;
			new HttpContextAdapter(context);
			context.Server.ClearError();
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
		public static string ServerError = "ServerError";
		public static string ServerErrorStackTrace = "StackTrace";
		public static string ServerVariables = "ServerVariables";
		public static string RewritePathCallback = "RewritePathCallback";
		public static string User = "User";
		public static string SessionID = "SessionID";
		public static string DebugMode = "DebugMode";
		#endregion

		#region APPLICATION CALLBACKS
		public static string ApplicationSessionStoreAddCallback = "ApplicationSessionStoreAddCallback";
		public static string ApplicationSessionStoreRemoveCallback = "ApplicationSessionStoreRemoveCallback";
		public static string ApplicationSessionStoreGetCallback = "ApplicationSessionStoreGetCallback";
		public static string UserSessionStoreAddCallback = "UserSessionStoreAddCallback";
		public static string UserSessionStoreRemoveCallback = "UserSessionStoreRemoveCallback";
		public static string UserSessionStoreGetCallback = "UserSessionStoreGetCallback";
		public static string UserSessionStoreAbandonCallback = "UserSessionStoreAbandonCallback";
		#endregion

		#region REQUEST
		public static string RequestScheme = "RequestScheme";
		public static string RequestMethod = "RequestMethod";
		public static string RequestPathBase = "RequestPathBase";
		public static string RequestPath = "RequestPath";
		public static string RequestPathSegments = "RequestPathSegments";
		public static string RequestQueryString = "RequestQueryString";
		public static string RequestQueryStringCallback = "RequestQueryStringCallback";
		public static string RequestHeaders = "RequestHeaders";
		public static string RequestBody = "RequestBody";
		public static string RequestCookie = "RequestCookie";
		public static string RequestIsSecure = "RequestIsSecure";
		public static string RequestIPAddress = "RequestIPAddress";
		public static string RequestForm = "RequestForm";
		public static string RequestFormCallback = "RequestFormCallback";
		public static string RequestClientCertificate = "RequestClientCertificate";
		public static string RequestFiles = "RequestFiles";
		public static string RequestUrl = "RequestUrl";
		public static string RequestUrlAuthority = "RequestUrlAuthority";
		public static string RequestIdentity = "RequestIdentity";
		#endregion

		#region RESPONSE
		public static string ResponseCache = "ResponseCache";
		public static string ResponseCacheabilityOption = "ResponseCacheabilityOption";
		public static string ResponseCacheExpiry = "ResponseCacheExpiry";
		public static string ResponseCacheMaxAge = "ResponseCacheMaxAge";
		public static string ResponseCookie = "ResponseCookie";
		public static string ResponseStatus = "ResponseStatus";
		public static string ResponseContentType = "ResponseContentType";
		public static string ResponseHeaders = "ResponseHeaders";
		public static string ResponseBody = "ResponseBody";
		public static string ResponseRedirectCallback = "ResponseRedirectCallback";
		public static string ResponseErrorCallback = "ResponseErrorCallback";
		#endregion

		public static Dictionary<string, string> MimeTypes = new Dictionary<string, string>()
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

	public sealed class HttpContextAdapter
	{
		private static object syncInitLock = new object();

		private readonly HttpContext context;
		private bool firstRun, debugMode;
		private static string AspNetApplicationTypeSessionName = "__AspNetApplicationType";
		private NameValueCollection unvalidatedForm, unvalidatedQueryString;

		public HttpContextAdapter(HttpContext ctx)
		{
			context = ctx;

			firstRun = Convert.ToBoolean(context.Application["__SyncInitLock"]);

			SetupUnvalidatedFormAndQueryStringCollections();

			Type adapterApp = null;

			if (context.Application[AspNetApplicationTypeSessionName] == null)
			{
				context.Application["__SyncInitLock"] = true;

				// Look for a class inside the executing assembly that implements IAspNetAdapterApplication
				var apps = AppDomain.CurrentDomain
														.GetAssemblies()
														.AsParallel()
														.Where(x => x.GetName().Name != "DotNetOpenAuth") // DotNetOpenAuth depends on System.Web.Mvc which is not referenced, this will fail if we don't eliminate it
														.SelectMany(x => x.GetTypes()
																							.Where(y => y.GetInterfaces()
																													 .FirstOrDefault(i => i.UnderlyingSystemType == typeof(IAspNetAdapterApplication)) != null));

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

			request[HttpAdapterConstants.SessionID] = context.Session.SessionID;
			request[HttpAdapterConstants.ServerVariables] = NameValueCollectionToDictionary(context.Request.ServerVariables);
			request[HttpAdapterConstants.RequestScheme] = context.Request.Url.Scheme;
			request[HttpAdapterConstants.RequestIsSecure] = context.Request.IsSecureConnection;
			request[HttpAdapterConstants.RequestHeaders] = StringToDictionary(context.Request.ServerVariables["ALL_HTTP"], '\n', ':');
			request[HttpAdapterConstants.RequestMethod] = context.Request.HttpMethod;
			request[HttpAdapterConstants.RequestPathBase] = context.Request.PhysicalApplicationPath.TrimEnd('\\');
			request[HttpAdapterConstants.RequestPath] = (string.IsNullOrEmpty(context.Request.Path)) ? "/" : (context.Request.Path.Length > 1) ? context.Request.Path.TrimEnd('/') : context.Request.Path;
			request[HttpAdapterConstants.RequestPathSegments] = SplitPathSegments(context.Request.Path);
			request[HttpAdapterConstants.RequestQueryString] = NameValueCollectionToDictionary(unvalidatedQueryString);
			request[HttpAdapterConstants.RequestQueryStringCallback] = new Func<string, bool, string>(RequestQueryStringGetCallback);
			request[HttpAdapterConstants.RequestCookie] = StringToDictionary(context.Request.ServerVariables["HTTP_COOKIE"], ';', '=');
			request[HttpAdapterConstants.RequestBody] = StringToDictionary(new StreamReader(context.Request.InputStream).ReadToEnd(), '&', '=');
			request[HttpAdapterConstants.RequestForm] = NameValueCollectionToDictionary(unvalidatedForm);
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

			application[HttpAdapterConstants.DebugMode] = debugMode;
			application[HttpAdapterConstants.User] = context.User;
			application[HttpAdapterConstants.ApplicationSessionStoreAddCallback] = new Action<string, object>(ApplicationSessionStoreAddCallback);
			application[HttpAdapterConstants.ApplicationSessionStoreRemoveCallback] = new Action<string>(ApplicationSessionStoreRemoveCallback);
			application[HttpAdapterConstants.ApplicationSessionStoreGetCallback] = new Func<string, object>(ApplicationSessionStoreGetCallback);
			application[HttpAdapterConstants.UserSessionStoreAddCallback] = new Action<string, object>(UserSessionStoreAddCallback);
			application[HttpAdapterConstants.UserSessionStoreRemoveCallback] = new Action<string>(UserSessionStoreRemoveCallback);
			application[HttpAdapterConstants.UserSessionStoreGetCallback] = new Func<string, object>(UserSessionStoreGetCallback);
			application[HttpAdapterConstants.UserSessionStoreAbandonCallback] = new Action(UserSessionStoreAbandonCallback);
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

		private void SetupUnvalidatedFormAndQueryStringCollections()
		{
			ValidationUtility.EnableDynamicValidation(context.ApplicationInstance.Context);

			Func<NameValueCollection> _unvalidatedForm, _unvalidatedQueryString;

			ValidationUtility.GetUnvalidatedCollections(context.ApplicationInstance.Context, out _unvalidatedForm, out _unvalidatedQueryString);

			unvalidatedForm = _unvalidatedForm();
			unvalidatedQueryString = _unvalidatedQueryString();
		}
		#endregion

		#region CALLBACKS
		private void ResponseCallback(Dictionary<string, object> response)
		{
			response.ThrowIfArgumentNull();

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
					context.Response.Write((string)response[HttpAdapterConstants.ResponseBody]);
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
		}

		private void ResponseRedirectCallback(string path, Dictionary<string, string> headers)
		{
			if (headers != null)
			{
				foreach (KeyValuePair<string, string> kvp in headers)
					context.Response.AddHeader(kvp.Key, kvp.Value);
			}

			context.Response.Redirect(path);
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
			if (!string.IsNullOrEmpty(key))
				context.Session[key] = value;
		}

		private void UserSessionStoreRemoveCallback(string key)
		{
			if (!string.IsNullOrEmpty(key))
				context.Session.Remove(key);
		}

		private object UserSessionStoreGetCallback(string key)
		{
			return context.Session[key];
		}

		private void UserSessionStoreAbandonCallback()
		{
			context.Session.Abandon();
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
					result = HttpUtility.HtmlEncode(unvalidatedForm[key]);
				}
			}
			else
				result = unvalidatedForm[key];

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
					result = HttpUtility.HtmlEncode(unvalidatedQueryString[key]);
				}
			}
			else
				result = unvalidatedQueryString[key];

			return result;
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