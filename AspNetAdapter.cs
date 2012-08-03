// AspNetAdapter - A thin generic wrapper that exposes some ASP.NET stuff in a
//                 nice simple way.
//
// Updated On: 2 August 2012
//
// Contact Info:
//
//  Frank Hale - <frankhale@gmail.com> 
//               <http://about.me/frank.hale>
//
// An attempt to abstract away some of the common bits of the ASP.NET 
// HttpContext.
//
// I initially looked at OWIN to provide the abstraction that I wanted but I 
// found it to be a bit more complex than I was hoping for. What I was looking
// for was something a bit easier to strap in, something that exposed some 
// simple types and was as braindead easy as I could come up with.
//
// Usage:
// 
//  Write a class that implements IAspNetAdapterApplication. And use the 
//  following web.config.
//
//  <?xml version="1.0"?>
//  <configuration>
//   <system.web>
//     <compilation debug="false" targetFramework="4.0" />
//     <customErrors mode="On"></customErrors>
//     <httpHandlers>
//       <add verb="*" path="*" validate="false" 
//																	 type="AspNetAdapter.AspNetAdapterHandler"/>
//
//		<httpModules>
//			<add type="AspNetAdapter.AspNetAdapterModule" 
//																								 name="AspNetAdapterModule" />
//		</httpModules>
//     </httpHandlers>
//   </system.web>
//  </configuration>
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

#region USING STATEMENTS
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Web;
using System.Web.Caching;
using System.Web.SessionState;
using Microsoft.Web.Infrastructure.DynamicValidationHelper;
using System.Diagnostics;
#endregion

#region ASSEMBLY INFORMATION
#if LIBRARY
using System.Runtime.InteropServices;

[assembly: AssemblyTitle("AspNetAdapter")]
[assembly: AssemblyDescription("An ASP.NET HttpContext Adapter")]
[assembly: AssemblyCompany("Frank Hale")]
[assembly: AssemblyProduct("Aurora")]
[assembly: AssemblyCopyright("(GNU GPLv3) Copyleft © 2012")]
[assembly: ComVisible(false)]
[assembly: CLSCompliant(true)]
[assembly: AssemblyVersion("0.0.4.0")]
#endif
#endregion

namespace AspNetAdapter
{
	#region ASP.NET HOOKS - IHTTPHANDLER / IHTTPMODULE

	#region HTTP HANDLER
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
	#endregion

	#region HTTP MODULE
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
		public const string ServerError = "ServerError";
		public const string ServerVariables = "ServerVariables";
		public const string RewritePathCallback = "RewritePathCallback";
		public const string User = "User";
		public const string SessionID = "SessionID";
		public const string DebugMode = "DebugMode";
		#endregion

		#region APPLICATION CALLBACKS
		public const string ApplicationSessionStoreAddCallback = "ApplicationSessionStoreAddCallback";
		public const string ApplicationSessionStoreRemoveCallback = "ApplicationSessionStoreRemoveCallback";
		public const string ApplicationSessionStoreGetCallback = "ApplicationSessionStoreGetCallback";

		public const string UserSessionStoreAddCallback = "UserSessionStoreAddCallback";
		public const string UserSessionStoreRemoveCallback = "UserSessionStoreRemoveCallback";
		public const string UserSessionStoreGetCallback = "UserSessionStoreGetCallback";
		public const string UserSessionStoreAbandonCallback = "UserSessionStoreAbandonCallback";
		#endregion

		#region REQUEST
		public const string RequestScheme = "RequestScheme";
		public const string RequestMethod = "RequestMethod";
		public const string RequestPathBase = "RequestPathBase";
		public const string RequestPath = "RequestPath";
		public const string RequestQueryString = "RequestQueryString";
		public const string RequestQueryStringCallback = "RequestQueryStringCallback";
		public const string RequestHeaders = "RequestHeaders";
		public const string RequestBody = "RequestBody";
		public const string RequestCookie = "RequestCookie";
		public const string RequestIsSecure = "RequestIsSecure";
		public const string RequestIPAddress = "RequestIPAddress";
		public const string RequestForm = "RequestForm";
		public const string RequestFormCallback = "RequestFormCallback";
		public const string RequestClientCertificate = "RequestClientCertificate";
		public const string RequestFiles = "RequestFiles";
		public const string RequestUrl = "RequestUrl";
		#endregion

		#region RESPONSE
		public const string ResponseCache = "ResponseCache";
		public const string ResponseCacheabilityOption = "ResponseCacheabilityOption";
		public const string ResponseCacheExpiry = "ResponseCacheExpiry";
		public const string ResponseCacheMaxAge = "ResponseCacheMaxAge";
		public const string ResponseCookie = "ResponseCookie";
		public const string ResponseStatus = "ResponseStatus";
		public const string ResponseContentType = "ResponseContentType";
		public const string ResponseHeaders = "ResponseHeaders";
		public const string ResponseBody = "ResponseBody";
		public const string ResponseRedirectCallback = "ResponseRedirectCallback";
		public const string ResponseErrorCallback = "ResponseErrorCallback";
		#endregion

		public static Dictionary<string, string> MimeTypes = new Dictionary<string, string>()
			{
				{ ".js",  "application/x-javascript" },  
				{ ".css", "text/css" },
				{ ".png", "image/png" },
				{ ".jpg", "image/jpg" },
				{ ".gif", "image/gif" },
				{ ".ico", "image/x-icon" },
				{ ".csv", "test/plain"},
				{ ".xlsx", "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet"},
				{ ".pptx", "application/vnd.openxmlformats-officedocument.presentationml.presentation"}
			};
	}

	// An application that wants to hook into ASP.NET and be sent the goodies that the HttpContextAdapter has to offer
	public interface IAspNetAdapterApplication
	{
		void Init(Dictionary<string, object> app, Dictionary<string, object> request, Action<Dictionary<string, object>> response);
	}

	public sealed class HttpContextAdapter
	{
		private static object syncInitLock = new object();

		private bool firstRun;
		private readonly HttpContext context;
		private bool debugMode;
		private static string AspNetApplicationTypeSessionName = "__AspNetApplicationType";
		private NameValueCollection unvalidatedForm;
		private NameValueCollection unvalidatedQueryString;

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
				var apps = (from assembly in AppDomain.CurrentDomain.GetAssemblies().Where(x => x.GetName().Name != "DotNetOpenAuth")
										from type in assembly.GetTypes().Where(x => x.GetInterfaces().FirstOrDefault(i => i.UnderlyingSystemType == typeof(IAspNetAdapterApplication)) != null)
										select type);

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
			{
				lock (syncInitLock)
				{
					_appInstance.Init(app, request, ResponseCallback);
				}
			}
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
			request[HttpAdapterConstants.RequestPath] = (context.Request.Path.Length > 1) ? context.Request.Path.TrimEnd('/') : context.Request.Path;
			request[HttpAdapterConstants.RequestQueryString] = NameValueCollectionToDictionary(context.Request.QueryString);
			request[HttpAdapterConstants.RequestQueryStringCallback] = new Func<string, bool, string>(RequestQueryStringGetCallback);
			request[HttpAdapterConstants.RequestCookie] = StringToDictionary(context.Request.ServerVariables["HTTP_COOKIE"], ';', '=');
			request[HttpAdapterConstants.RequestBody] = StringToDictionary(new StreamReader(context.Request.InputStream).ReadToEnd(), '&', '=');
			request[HttpAdapterConstants.RequestForm] = NameValueCollectionToDictionary(unvalidatedForm);
			request[HttpAdapterConstants.RequestFormCallback] = new Func<string, bool, string>(RequestFormGetCallback);
			request[HttpAdapterConstants.RequestIPAddress] = GetIPAddress();
			request[HttpAdapterConstants.RequestClientCertificate] = (context.Request.ClientCertificate != null) ? new X509Certificate2(context.Request.ClientCertificate.Certificate) : null;
			request[HttpAdapterConstants.RequestFiles] = GetRequestFiles();
			request[HttpAdapterConstants.RequestUrl] = context.Request.Url.Authority;

			return request;
		}

		private Dictionary<string, object> InitializeApplicationDictionary()
		{
			Dictionary<string, object> application = new Dictionary<string, object>();

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
			application[HttpAdapterConstants.ServerError] = context.Server.GetLastError();

			return application;
		}
		#endregion

		#region MISCELLANEOUS
		// Adapted from: http://ardalis.com/determine-whether-an-assembly-was-compiled-in-debug-mode
		private bool IsAssemblyDebugBuild(Assembly assembly)
		{
			return (assembly.GetCustomAttributes(typeof(DebuggableAttribute), false).FirstOrDefault() as DebuggableAttribute).IsJITTrackingEnabled;
		}

		private Dictionary<string, string> NameValueCollectionToDictionary(NameValueCollection nvc)
		{
			Dictionary<string, string> result = new Dictionary<string, string>();

			foreach (string key in nvc.AllKeys)
			{
				if (!result.ContainsKey(key))
					result[key] = nvc.Get(key);
			}

			return result;
		}

		private Dictionary<string, string> StringToDictionary(string value, char splitOn, char delimiter)
		{
			Dictionary<string, string> result = new Dictionary<string, string>();

			if (!string.IsNullOrEmpty(value))
			{
				foreach (string[] arr in value.Split(new char[] { splitOn }, StringSplitOptions.RemoveEmptyEntries).Select(x => x.Split(delimiter)))
				{
					if (!result.ContainsKey(arr[0]))
						result.Add(arr[0].Trim(), arr[1].Trim());
				}
			}

			return result;
		}

		private string GetIPAddress()
		{
			// This method is based on the following example at StackOverflow:
			//
			// http://stackoverflow.com/questions/735350/how-to-get-a-users-client-ip-address-in-asp-net
			string ip = context.Request.ServerVariables["HTTP_X_FORWARDED_FOR"];

			if (string.IsNullOrEmpty(ip))
				return context.Request.ServerVariables["REMOTE_ADDR"];
			else
				return ip.Split(',')[0];
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

			if (postedFiles.Count > 0)
				return postedFiles;

			return null;
		}

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

			Func<NameValueCollection> _unvalidatedForm;
			Func<NameValueCollection> _unvalidatedQueryString;

			ValidationUtility.GetUnvalidatedCollections(context.ApplicationInstance.Context, out _unvalidatedForm, out _unvalidatedQueryString);

			unvalidatedForm = _unvalidatedForm();
			unvalidatedQueryString = _unvalidatedQueryString();
		}
		#endregion

		#region CALLBACKS

		private void ResponseCallback(Dictionary<string, object> response)
		{
			response.ThrowIfArgumentNull();

			if (!(response.ContainsKey(HttpAdapterConstants.ResponseStatus) ||
					response.ContainsKey(HttpAdapterConstants.ResponseContentType) ||
					response.ContainsKey(HttpAdapterConstants.ResponseBody)))
				throw new Exception("The response dictionary must include an http status code, content type and content");

			context.Response.AddHeader("X_FRAME_OPTIONS", "SAMEORIGIN");
			
			context.Response.StatusCode = (int)response[HttpAdapterConstants.ResponseStatus];
			context.Response.ContentType = response[HttpAdapterConstants.ResponseContentType].ToString();

			if (response[HttpAdapterConstants.ResponseHeaders] != null)
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
					if (response[HttpAdapterConstants.ResponseErrorCallback] != null)
						(response[HttpAdapterConstants.ResponseErrorCallback] as Action<Exception>)(e);
				}
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
			if (!string.IsNullOrEmpty(key))
				return context.Session[key];

			return null;
		}

		private void UserSessionStoreAbandonCallback()
		{
			context.Session.Abandon();
		}
		#endregion

		#region FORM / QUERYSTRING (FOR OBTAINING VALIDATED VALUES)
		private string RequestFormGetCallback(string key, bool validated)
		{
			if (validated)
				return context.Request.Form[key];
			else
				return unvalidatedForm[key];
		}

		private string RequestQueryStringGetCallback(string key, bool validated)
		{
			if (validated)
				return context.Request.QueryString[key];
			else
				return unvalidatedQueryString[key];
		}
		#endregion

		#region MISCELLANEOUS
		private void ResponseRedirectCallback(string path, Dictionary<string, string> headers)
		{
			if (headers != null)
			{
				foreach (KeyValuePair<string, string> kvp in headers)
					context.Response.AddHeader(kvp.Key, kvp.Value);
			}

			context.Response.Redirect(path);
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
	}
	#endregion
}