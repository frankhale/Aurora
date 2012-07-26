//
// Aurora - An MVC web framework for .NET
//
// Updated On: 25 July 2012
//
// Contact Info:
//
//  Frank Hale - <frankhale@gmail.com> 
//               <http://about.me/frank.hale>
//
// --------------------
// --- Feature List ---
// --------------------
//
// - Model View Controller based 
// - Front controller aware (optional)
// - Simple tag based view engine with master pages and partial views as well as
//   fragments 
// - URL parameters bind to action method parameters automatically 
// - Posted forms binds to post models or action parameters automatically 
// - Actions can have bound parameters that are bound to actions at runtime
// - Actions can be segregated based on HttpGet, HttpPost, HttpPut and 
//   HttpDelete attributes and you can secure them with the Secure named 
//   parameter. Actions without a designation will not be invoked from a URL.  
// - Actions can have filters with optional filter results that bind to action
//   parameters.
// - Actions can have aliases. Aliases can also be added dynamically at runtime
//   along with default parameters.
// - Actions can be invoked on a special basis if they are designated with the
//   [FromRedirectOnly] attribute.
// - Bundling/Minifying of Javascript and CSS.
//
// Aurora.Extra 
//
// - My fork of Massive ORM
// - My fork of the Gravatar URL generator
// - HTML helpers
// - Plugin support (can be used by apps but is not integrated at all into the
//   framework pipeline.)
// - OpenID authentication which is as easy as calling two methods. One 
//   to initiate the login with the provider and then one to finalize 
//   authentication.
// - Active Directory querying so you can authenticate your user against an 
//   Active Directory user. Typically for use in client certificate 
//   authentication.
//
// Aurora.Misc
//
// - My fork of the Gravatar URL generato
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
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Dynamic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Web;
using AspNetAdapter;
using HtmlAgilityPack;
using MarkdownSharp;
using Newtonsoft.Json;
using Yahoo.Yui.Compressor;
using System.Web.Caching;
#endregion

#region ASSEMBLY INFORMATION
#if LIBRARY
[assembly: AssemblyTitle("Aurora")]
[assembly: AssemblyDescription("An MVC web framework for .NET")]
[assembly: AssemblyCompany("Frank Hale")]
[assembly: AssemblyProduct("Aurora")]
[assembly: AssemblyCopyright("(GNU GPLv3) Copyleft © 2011-2012")]
[assembly: ComVisible(false)]
[assembly: CLSCompliant(true)]
[assembly: AssemblyVersion("2.0.2.0")]
#endif
#endregion

namespace Aurora
{
	#region CONFIGURATION
	public static class Config
	{
		static Config()
		{
			MimeTypes = new Dictionary<string, string>()
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

		public static Regex AllowedFilePattern = new Regex(@"\.(js|css|png|jpg|gif|ico|pptx|xlsx|csv)$", RegexOptions.Compiled);
		public static string SharedResourceFolderPath = "/Resources";
		public static Dictionary<string, string> MimeTypes;
	}
	#endregion

	#region ATTRIBUTES
	public enum ActionSecurity
	{
		Secure,
		None
	}

	#region HTTP REQUEST
	public abstract class RequestTypeAttribute : Attribute
	{
		public bool RequireAntiForgeryToken { get; set; }
		public string RedirectWithoutAuthorizationTo { get; set; }
		public ActionSecurity SecurityType { get; set; }
		public string RouteAlias { get; set; }
		public string Roles { get; set; }
		public bool HttpsOnly { get; set; }
		public string RequestType { get; private set; }

		protected RequestTypeAttribute(string requestType)
		{
			SecurityType = ActionSecurity.None;
			RequestType = requestType;
			Roles = string.Empty;
		}
	}

	[AttributeUsage(AttributeTargets.Method)]
	public sealed class FromRedirectOnlyAttribute : RequestTypeAttribute
	{
		public FromRedirectOnlyAttribute(string routeAlias)
			: base("GET")
		{
			RouteAlias = routeAlias;
		}
	}

	[AttributeUsage(AttributeTargets.Method)]
	public sealed class HttpGetAttribute : RequestTypeAttribute
	{
		public HttpGetAttribute() : base("GET") { }
		public HttpGetAttribute(ActionSecurity sec) : this(string.Empty, sec) { }

		public HttpGetAttribute(string routeAlias)
			: base("GET")
		{
			RouteAlias = routeAlias;
		}
		
		public HttpGetAttribute(string routeAlias, ActionSecurity sec)
			: base("GET")
		{
			SecurityType = sec;
			RouteAlias = routeAlias;
		}
	}

	[AttributeUsage(AttributeTargets.Method)]
	public sealed class HttpPostAttribute : RequestTypeAttribute
	{
		public HttpPostAttribute() : base("POST") { }
		public HttpPostAttribute(ActionSecurity sec) : this(string.Empty, sec) { }

		public HttpPostAttribute(string routeAlias)
			: base("POST")
		{
			RouteAlias = routeAlias;
			RequireAntiForgeryToken = true;
		}

		public HttpPostAttribute(string routeAlias, ActionSecurity sec)
			: base("POST")
		{
			SecurityType = sec;
			RouteAlias = routeAlias;
		}
	}

	[AttributeUsage(AttributeTargets.Method)]
	public sealed class HttpPutAttribute : RequestTypeAttribute
	{
		public HttpPutAttribute() : base("PUT") { }
		public HttpPutAttribute(ActionSecurity sec) : this(string.Empty, sec) { }

		public HttpPutAttribute(string routeAlias)
			: base("PUT")
		{
			RouteAlias = routeAlias;
			RequireAntiForgeryToken = true;
		}

		public HttpPutAttribute(string routeAlias, ActionSecurity sec)
			: base("PUT")
		{
			SecurityType = sec;
			RouteAlias = routeAlias;
		}
	}

	[AttributeUsage(AttributeTargets.Method)]
	public sealed class HttpDeleteAttribute : RequestTypeAttribute
	{
		public HttpDeleteAttribute() : base("DELETE") { }
		public HttpDeleteAttribute(ActionSecurity sec) : this(string.Empty, sec) { }

		public HttpDeleteAttribute(string routeAlias)
			: base("DELETE")
		{
			RouteAlias = routeAlias;
			RequireAntiForgeryToken = true;
		}

		public HttpDeleteAttribute(string routeAlias, ActionSecurity sec)
			: base("DELETE")
		{
			SecurityType = sec;
			RouteAlias = routeAlias;
		}
	}
	#endregion

	#region MISCELLANEOUS
	[AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
	public sealed class AliasAttribute : Attribute
	{
		public string Alias { get; private set; }

		public AliasAttribute(string alias)
		{
			Alias = alias;
		}
	}

	[AttributeUsage(AttributeTargets.Class)]
	public sealed class PartitionAttribute : Attribute
	{
		public string Name { get; private set; }

		public PartitionAttribute(string name)
		{
			Name = name;
		}
	}

	[AttributeUsage(AttributeTargets.Property)]
	public sealed class HiddenAttribute : Attribute
	{
	}

	[AttributeUsage(AttributeTargets.Property)]
	public sealed class NotRequiredAttribute : Attribute
	{
	}

	[AttributeUsage(AttributeTargets.Property)]
	internal sealed class ExcludeFromBindingAttribute : Attribute
	{
	}

	[AttributeUsage(AttributeTargets.Property | AttributeTargets.Parameter)]
	public sealed class UnsafeAttribute : Attribute
	{
	}

	[AttributeUsage(AttributeTargets.Parameter)]
	public sealed class ActionParameterTransformAttribute : Attribute
	{
		public string TransformName { get; private set; }

		public ActionParameterTransformAttribute(string transformName)
		{
			TransformName = transformName;
		}
	}
	#endregion

	#region MODEL VALIDATION
	public abstract class ModelValidationBaseAttribute : Attribute
	{
		public string ErrorMessage { get; set; }

		protected ModelValidationBaseAttribute(string errorMessage)
		{
			ErrorMessage = errorMessage;
		}
	}

	[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
	public sealed class RequiredAttribute : ModelValidationBaseAttribute
	{
		public RequiredAttribute(string errorMessage)
			: base(errorMessage)
		{
		}
	}

	[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
	public sealed class RequiredLengthAttribute : ModelValidationBaseAttribute
	{
		public int Length { get; private set; }

		public RequiredLengthAttribute(int length, string errorMessage)
			: base(errorMessage)
		{
			Length = length;
		}
	}

	[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
	public sealed class RegularExpressionAttribute : ModelValidationBaseAttribute
	{
		public Regex Pattern { get; set; }

		public RegularExpressionAttribute(string pattern, string errorMessage)
			: base(errorMessage)
		{
			Pattern = new Regex(pattern);
		}
	}

	[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
	public sealed class RangeAttribute : ModelValidationBaseAttribute
	{
		public int Min { get; private set; }
		public int Max { get; private set; }

		public RangeAttribute(int min, int max, string errorMessage)
			: base(errorMessage)
		{
			Min = min;
			Max = max;
		}
	}
	#endregion

	#endregion

	#region FRAMEWORK ENGINE
	internal class Engine : IAspNetAdapterApplication
	{
		#region ASP.NET ADAPTER STUFF
		private Dictionary<string, object> app;
		private Dictionary<string, object> request;
		private Dictionary<string, string> queryString;
		private Dictionary<string, string> cookies;
		private Dictionary<string, string> form;
		private Dictionary<string, string> payload;
		private Action<Dictionary<string, object>> response;
		private List<PostedFile> files;
		private Exception serverError;
		internal X509Certificate2 clientCertificate { get; private set; }
		internal string IPAddress { get; private set; }
		private string path;
		private string requestType;
		private string appRoot;
		private string viewRoot;
		private string sessionID;
		private bool fromRedirectOnly;
		private bool isSecureConnection;
		#endregion

		#region SESSION NAMES
		private static string viewEngineSessionName = "__ViewEngine";
		private static string fromRedirectOnlySessionName = "__FromRedirectOnly";
		private static string controllerInstancesSessionName = "__Controllers";
		private static string frontControllerInstanceSessionName = "__FrontController";
		private static string routeInfosSessionName = "__RouteInfos";
		private static string actionBindingsSessionName = "__ActionBindings";
		private static string usersSessionName = "__Users";
		private static string currentUserSessionName = "__CurrentUser";
		private static string antiForgeryTokensSessionName = "__AntiForgeryTokens";
		private static string bundlesTokenSessionName = "__Bundles";
		private static string modelsSessionName = "__Models";
		private static string protectedFilesSessionName = "__ProtectedFiles";
		private static string controllersSessionSessionName = "__ControllersSession";
		#endregion

		#region MISCELLANEOUS VARIABLES
		private bool debugMode;
		internal ViewEngine ViewEngine;
		private FrontController frontController;
		private List<Controller> controllers;
		internal List<RouteInfo> routeInfos;
		private List<string> antiForgeryTokens;
		private List<User> users;
		private Dictionary<string, Dictionary<string, List<object>>> actionBindings;
		private Dictionary<string, Tuple<List<string>, string>> bundles;
		internal User currentUser;
		private List<Type> models;
		private Dictionary<string, string> protectedFiles;
		private Dictionary<string, object> controllersSession;
		private static string antiForgeryTokenName = "AntiForgeryToken";
		#endregion

		#region FRAMEWORK METHODS
		public void Init(Dictionary<string, object> app, Dictionary<string, object> request, Action<Dictionary<string, object>> response)
		{
			#region INITIALIZE LOCALS FROM APP/REQUEST AND MISC
			this.request = request;
			this.app = app;
			this.response = response;

			requestType = request[HttpAdapterConstants.RequestMethod].ToString();
			appRoot = request[HttpAdapterConstants.RequestPathBase].ToString();
			viewRoot = string.Format(@"{0}\Views", appRoot);
			IPAddress = request[HttpAdapterConstants.RequestIPAddress].ToString();
			sessionID = request[HttpAdapterConstants.SessionID].ToString();
			path = request[HttpAdapterConstants.RequestPath].ToString();
			cookies = request[HttpAdapterConstants.RequestCookie] as Dictionary<string, string>;
			form = request[HttpAdapterConstants.RequestForm] as Dictionary<string, string>;
			payload = request[HttpAdapterConstants.RequestBody] as Dictionary<string, string>;
			files = request[HttpAdapterConstants.RequestFiles] as List<PostedFile>;
			queryString = request[HttpAdapterConstants.RequestQueryString] as Dictionary<string, string>;
			fromRedirectOnly = Convert.ToBoolean(GetSession(fromRedirectOnlySessionName));
			isSecureConnection = Convert.ToBoolean(request[HttpAdapterConstants.RequestIsSecure]);
			debugMode = Convert.ToBoolean(app[HttpAdapterConstants.DebugMode]);
			serverError = app[HttpAdapterConstants.ServerError] as Exception;
			int httpStatus = 200;
			ViewResponse viewResponse = null;
			clientCertificate = request[HttpAdapterConstants.RequestClientCertificate] as X509Certificate2;
			#endregion

			#region GET OBJECTS FROM APPLICATION SESSION STORE
			ViewEngine = GetApplication(viewEngineSessionName) as ViewEngine;
			controllers = GetApplication(controllerInstancesSessionName) as List<Controller>;
			frontController = GetApplication(frontControllerInstanceSessionName) as FrontController;
			routeInfos = GetApplication(routeInfosSessionName) as List<RouteInfo>;
			actionBindings = GetApplication(actionBindingsSessionName) as Dictionary<string, Dictionary<string, List<object>>>;
			users = GetApplication(usersSessionName) as List<User>;
			antiForgeryTokens = GetApplication(antiForgeryTokensSessionName) as List<string>;
			bundles = GetApplication(bundlesTokenSessionName) as Dictionary<string, Tuple<List<string>, string>>;
			models = GetApplication(modelsSessionName) as List<Type>;
			fromRedirectOnly = Convert.ToBoolean(GetSession(fromRedirectOnlySessionName));
			protectedFiles = GetSession(protectedFilesSessionName) as Dictionary<string, string>;
			controllersSession = GetSession(controllersSessionSessionName) as Dictionary<string, object>;

			if (routeInfos == null)
				routeInfos = new List<RouteInfo>();
			#endregion

			#region INITIALIZE CONTROLLERS SESSION STORE
			controllersSession = new Dictionary<string, object>();

			AddSession(controllersSessionSessionName, controllersSession);
			#endregion

			#region INITIALIZE PROTECTED FILES
			if (protectedFiles == null)
			{
				protectedFiles = new Dictionary<string, string>();

				AddApplication(protectedFilesSessionName, protectedFiles);
			}
			#endregion

			#region INITIALIZE ACTION BINDINGS
			if (actionBindings == null)
			{
				actionBindings = new Dictionary<string, Dictionary<string, List<object>>>();

				AddApplication(actionBindingsSessionName, actionBindings);
			}
			#endregion

			#region INTIALIZE FRONT CONTROLLER INSTANCE
			if (frontController == null)
			{
				frontController = GetFrontControllerInstance();

				AddApplication(frontControllerInstanceSessionName, frontController);
			}
			else
				frontController.Refresh(this);
			#endregion

			#region INITIALIZE CONTROLLER INSTANCES
			if (controllers == null)
			{
				controllers = GetControllerInstances();

				AddApplication(controllerInstancesSessionName, controllers);
			}
			else
				controllers.ForEach(c => c.Refresh(this));
			#endregion

			#region INITIALIZE BUNDLES
			if (bundles == null)
			{
				bundles = new Dictionary<string, Tuple<List<string>, string>>();

				AddApplication(bundlesTokenSessionName, bundles);
			}
			#endregion

			#region INITIALIZE ANTIFORGERYTOKENS
			if (antiForgeryTokens == null)
			{
				antiForgeryTokens = new List<string>();

				AddApplication(antiForgeryTokensSessionName, antiForgeryTokens);
			}
			#endregion

			#region INITIALIZE USERS
			if (users == null)
			{
				users = new List<User>();

				AddApplication(usersSessionName, users);
			}
			else
				currentUser = GetSession(currentUserSessionName) as User;
			#endregion

			#region INITIALIZE MODELS
			if (models == null)
			{
				models = GetTypeList(typeof(Model));

				AddApplication(modelsSessionName, models);
			}
			#endregion

			#region RUN CONTROLLER ONINIT METHODS
			if (!frontController.OnInitComplete)
				frontController.InvokeOnInit();

			foreach (Controller c in controllers)
			{
				if (!c.OnInitComplete)
					c.InvokeOnInit();
			}
			#endregion

			#region INITIALIZE ROUTEINFOS
			if (GetApplication(routeInfosSessionName) == null)
			{
				routeInfos.AddRange(GetRouteInfos());

				AddApplication(routeInfosSessionName, routeInfos);
			}
			#endregion

			#region INITIALIZE VIEW ENGINE
			if (ViewEngine == null)
			{
				List<IViewCompilerDirectiveHandler> dirHandlers = new List<IViewCompilerDirectiveHandler>();
				List<IViewCompilerSubstitutionHandler> substitutionHandlers = new List<IViewCompilerSubstitutionHandler>();

				dirHandlers.Add(new MasterPageDirective());
				dirHandlers.Add(new PlaceHolderDirective());
				dirHandlers.Add(new PartialPageDirective());
				dirHandlers.Add(new BundleDirective(debugMode, GetBundleFiles));
				substitutionHandlers.Add(new AntiForgeryTokenSubstitution(CreateAntiForgeryToken));
				substitutionHandlers.Add(new HeadSubstitution());

				ViewEngine = new ViewEngine(appRoot, GetViewRoots(), dirHandlers, substitutionHandlers);

				AddApplication(viewEngineSessionName, ViewEngine);
			}
			#endregion

			#region PROCESS REQUEST / RENDER RESPONSE
			if (serverError == null)
			{
				viewResponse = ProcessRequest();

				if (viewResponse == null)
				{
					httpStatus = 404;
					viewResponse = GetErrorViewResponse(string.Format("Http 404 - Page Not Found : {0}", path), null);
				}
			}
			else
			{
				httpStatus = 503;
				viewResponse = GetErrorViewResponse(serverError.Message, serverError.StackTrace);
			}

			RenderResponse(viewResponse, httpStatus);
			#endregion
		}

		private ViewResponse ProcessRequest()
		{
			RouteInfo routeInfo = null;
			IViewResult viewResult = null;
			ViewResponse viewResponse = null;

			if (path == "/" || path == "/default.aspx" || path == "~/") path = "/Index";

			if (Config.AllowedFilePattern.IsMatch(path))
			{
				#region FILE RESPONSE
				RaiseEventOnFrontController(RouteHandlerEventType.Static, path, null, null);

				if (path.StartsWith(Config.SharedResourceFolderPath) || path.EndsWith(".ico"))
				{
					string filePath = MapPath(path);

					if (CanAccessFile(filePath))
					{
						if (File.Exists(filePath))
							viewResponse = new FileResult(filePath).Render();
						else
						{
							string fileName = Path.GetFileName(path);

							if (bundles.ContainsKey(fileName))
								viewResponse = new FileResult(fileName, bundles[fileName].Item2).Render();
						}
					}
				}
				#endregion
			}
			else
			{
				#region ACTION RESPONSE
				RaiseEventOnFrontController(RouteHandlerEventType.PreRoute, path, null, null);

				routeInfo = FindRoute(path);

				RaiseEventOnFrontController(RouteHandlerEventType.PostRoute, path, null, null);

				if (routeInfo == null)
					routeInfo = RaiseEventOnFrontController(RouteHandlerEventType.MissingRoute, path, null, null);

				if (routeInfo != null)
				{
					if (routeInfo.RequestTypeAttribute is FromRedirectOnlyAttribute && !fromRedirectOnly)
						return null;

					if (requestType == "POST" || requestType == "PUT" || requestType == "DELETE")
					{
						if (routeInfo.RequestTypeAttribute.RequireAntiForgeryToken)
						{
							if (!(form.ContainsKey(antiForgeryTokenName) || payload.ContainsKey(antiForgeryTokenName)))
								return null;
							else
							{
								antiForgeryTokens.Remove(form[antiForgeryTokenName]);
								antiForgeryTokens.Remove(payload[antiForgeryTokenName]);
							}
						}
					}

					if (routeInfo.RequestTypeAttribute.SecurityType == ActionSecurity.Secure && currentUser == null ||
							routeInfo.RequestTypeAttribute.SecurityType == ActionSecurity.Secure && !(currentUser.Roles.Intersect(routeInfo.RequestTypeAttribute.Roles.Split('|')).Count() > 0) &&
							routeInfo.Controller.InvokeCheckRoles(routeInfo))
					{
						RaiseEventOnFrontController(RouteHandlerEventType.FailedSecurity, path, routeInfo, null);

						if (!string.IsNullOrEmpty(routeInfo.RequestTypeAttribute.RedirectWithoutAuthorizationTo))
						{
							viewResponse = new ViewResponse()
							{
								RedirectTo = routeInfo.RequestTypeAttribute.RedirectWithoutAuthorizationTo
							};
						}
					}
					else
					{
						RaiseEventOnFrontController(RouteHandlerEventType.PassedSecurity, path, routeInfo, null);
						RaiseEventOnFrontController(RouteHandlerEventType.Pre, path, routeInfo, null);
						routeInfo.Controller.RaiseEvent(RouteHandlerEventType.Pre, path, routeInfo);

						if (routeInfo.RequestTypeAttribute is FromRedirectOnlyAttribute && fromRedirectOnly)
							RemoveSession(fromRedirectOnlySessionName);

						foreach (IBoundToAction bta in routeInfo.IBoundToActionParams)
							bta.Initialize(routeInfo.Controller);

						if (routeInfo.ActionParamTransforms != null)
						{
							foreach (var apt in routeInfo.ActionParamTransforms)
							{
								Tuple<MethodInfo, object> transformMethod = routeInfo.CachedActionParamTransformInstances[apt.Item1.TransformName] as Tuple<MethodInfo, object>;
								object transformedParam = transformMethod.Item1.Invoke(transformMethod.Item2, new object[] { routeInfo.ActionParams[apt.Item2] });

								routeInfo.ActionParams[apt.Item2] = transformedParam;
							}
						}

						var filterResults = ProcessAnyActionFilters(routeInfo);

						if (filterResults.Count() > 0)
						{
							object[] actionParams = routeInfo.ActionParams;
							Array.Resize(ref actionParams, actionParams.Count() + filterResults.Count());
							filterResults.CopyTo(actionParams, actionBindings.Count());
							routeInfo.ActionParams = actionParams;
						}

						if (routeInfo.Action.ReturnType.GetInterface("IViewResult") != null)
							viewResult = (IViewResult)routeInfo.Action.Invoke(routeInfo.Controller, routeInfo.ActionParams);
						else
							routeInfo.Action.Invoke(routeInfo.Controller, routeInfo.ActionParams);

						if (viewResult != null)
							viewResponse = viewResult.Render();

						if (viewResponse == null)
							RaiseEventOnFrontController(RouteHandlerEventType.Error, path, routeInfo, null);

						RaiseEventOnFrontController(RouteHandlerEventType.Post, path, routeInfo, null);
						routeInfo.Controller.RaiseEvent(RouteHandlerEventType.Post, path, routeInfo);
					}
				}
				#endregion
			}

			return viewResponse;
		}

		private ViewResponse GetErrorViewResponse(string error, string stackTrace)
		{
			if (!string.IsNullOrEmpty(stackTrace))
				stackTrace = string.Format("<hr/><pre>{0}</pre>", stackTrace);

			string errorView = ViewEngine.LoadView("Views", "Shared", "Error", ViewTemplateType.Shared, new Dictionary<string, string>()
					{
						{"error", error },
						{"stacktrace", stackTrace }
					});

			ViewResponse viewResponse = new ViewResponse()
			{
				ContentType = "text/html",
				Content = !string.IsNullOrEmpty(errorView) ? errorView : string.Format("<html><body>{0} : {1} {2}</body></html>", path, error, stackTrace)
			};

			return viewResponse;
		}

		private void RenderResponse(ViewResponse viewResponse, int httpStatus)
		{
			if (string.IsNullOrEmpty(viewResponse.RedirectTo))
			{
				response(new Dictionary<string, object>
				{
					{HttpAdapterConstants.ResponseBody, viewResponse.Content},
					{HttpAdapterConstants.ResponseContentType, viewResponse.ContentType},
					{HttpAdapterConstants.ResponseHeaders, viewResponse.Headers},
					{HttpAdapterConstants.ResponseStatus, httpStatus}
				});
			}
			else
				ResponseRedirect(viewResponse.RedirectTo, false);
		}

		private string[] GetViewRoots()
		{
			List<string> viewRoots = new List<string>()
			{
				viewRoot
			};

			viewRoots.AddRange(controllers.SelectMany(x => x.GetType().GetCustomAttributes(typeof(PartitionAttribute), false)).Select(x => string.Format(@"{0}\{1}", appRoot, (x as PartitionAttribute).Name)));

			return viewRoots.ToArray();
		}

		internal void ProtectFile(string path, string roles)
		{
			path.ThrowIfArgumentNull();
			roles.ThrowIfArgumentNull();

			protectedFiles[string.Format(@"{0}\{1}", appRoot, path)] = roles;
		}

		internal bool CanAccessFile(string path)
		{
			path.ThrowIfArgumentNull();

			if (protectedFiles.ContainsKey(path))
				return (currentUser != null && currentUser.Roles.Intersect(protectedFiles[path].Split('|')).Count() > 0) ? true : false;

			return true;
		}

		internal void AddBundle(string name, string[] paths)
		{
			string compressedFile = null;
			StringBuilder combinedFiles = new StringBuilder();

			paths = paths.Where(x => File.Exists(appRoot + x.Replace('/', '\\')) &&
															 Path.GetExtension(x) == ".css" ||
															 Path.GetExtension(x) == ".js").ToArray();

			if (!debugMode)
			{
				foreach (string p in paths)
					combinedFiles.AppendLine(File.ReadAllText(appRoot + p));

				if (Path.GetExtension(name) == ".js")
					compressedFile = new JavaScriptCompressor().Compress(combinedFiles.ToString());
				else if (Path.GetExtension(name) == ".css")
					compressedFile = new CssCompressor().Compress(combinedFiles.ToString());
			}

			bundles[name] = new Tuple<List<string>, string>(paths.ToList(), compressedFile);
		}

		internal string[] GetBundleFiles(string name)
		{
			if (bundles.ContainsKey(name))
				return bundles[name].Item1.ToArray();

			return null;
		}

		internal void AddBinding(string controllerName, string actionName, object bindInstance)
		{
			if (!actionBindings.ContainsKey(controllerName))
				actionBindings[controllerName] = new Dictionary<string, List<object>>();

			if (!actionBindings[controllerName].ContainsKey(actionName))
				actionBindings[controllerName][actionName] = new List<object>();

			if (!actionBindings[controllerName][actionName].Contains(bindInstance))
				actionBindings[controllerName][actionName].Add(bindInstance);
		}

		internal void AddBinding(string controllerName, string[] actionNames, object bindInstance)
		{
			foreach (string actionName in actionNames)
				AddBinding(controllerName, actionName, bindInstance);
		}

		internal void AddBinding(string controllerName, string[] actionNames, object[] bindInstances)
		{
			foreach (string actionName in actionNames)
				foreach (object bindInstance in bindInstances)
					AddBinding(controllerName, actionName, bindInstance);
		}

		internal void AddBindingForAllActions(string controllerName, object bindInstance)
		{
			foreach (string actionName in GetControllerActionNames(controllerName))
				AddBinding(controllerName, actionName, bindInstance);
		}

		internal void AddBindingsForAllActions(string controllerName, object[] bindInstances)
		{
			foreach (string actionName in GetControllerActionNames(controllerName))
				foreach (object bindInstance in bindInstances)
					AddBinding(controllerName, actionName, bindInstance);
		}

		internal RouteInfo FindRoute(string path)
		{
			var routeSlice = routeInfos.SelectMany(routeInfo => routeInfo.Aliases, (routeInfo, alias) => new { routeInfo, alias })
																 .Where(x => path.StartsWith(x.alias)).ToList();
			
			if (routeSlice.Count() > 0)
			{
				object model = null;
				List<object> allParams = new List<object>();

				object[] urlParams = path.Replace(routeSlice[0].alias, string.Empty).Split('/').Where(x => !string.IsNullOrEmpty(x)).Select(x => HttpUtility.UrlEncode(x)).ToArray().ToObjectArray();

				allParams.AddRange(routeSlice[0].routeInfo.BoundParams);
				allParams.AddRange(urlParams);
				allParams.AddRange(routeSlice[0].routeInfo.DefaultParams);

				if (requestType == "POST")
				{
					model = PayloadToModel(form);
					object[] formParams = (model != null) ? new object[] { model } : form.Values.ToArray().ToObjectArray();
					allParams.AddRange(formParams);
				}
				else if (requestType == "PUT" || requestType == "DELETE")
				{
					model = PayloadToModel(payload);
					object[] payloadParams = (model != null) ? new object[] { model } : payload.Values.ToArray().ToObjectArray();
					allParams.AddRange(payloadParams);
				}

				object[] finalParams = allParams.ToArray();

				foreach (RouteInfo routeInfo in routeSlice.Where(x => x.routeInfo.Action.GetParameters().Count() >= finalParams.Count()).Select(x => x.routeInfo))
				{
					Type[] finalParamTypes = finalParams.Select(x => x.GetType()).ToArray();
					Type[] actionParamTypes = routeInfo.Action.GetParameters()
						// ActionFilterResults aren't known at this point
						.Where(x => x.ParameterType.GetInterface("IActionFilterResult") == null)
						.Select(x => x.ParameterType).ToArray();

					if (routeInfo.ActionParamTransforms != null)
						foreach (var apt in routeInfo.ActionParamTransforms)
							finalParamTypes[apt.Item2] = actionParamTypes[apt.Item2];

					for (int i = 0; i < routeSlice[0].routeInfo.BoundParams.Count(); i++)
					{
						if (actionParamTypes[i].IsInterface)
							if (finalParamTypes[i].GetInterface(actionParamTypes[i].Name) != null)
							{
								finalParamTypes[i] = actionParamTypes[i];
								break;
							}
					}

					if (finalParamTypes.SequenceEqual(actionParamTypes))
					{
						routeInfo.ActionParams = finalParams;

						return routeInfo;
					}
				}
			}

			return null;
		}

		private object PayloadToModel(Dictionary<string, string> payload)
		{
			object result = null;
			Type model = null;
			List<string> payloadNames = payload.Keys.Where(x => x != "AntiForgeryToken").ToList();

			foreach (Type m in models)
			{
				List<string> props = Model.GetPropertiesWithExclusions(m, true).Select(x => x.Name).ToList();

				if (props.Intersect(payloadNames).Count() == props.Union(payloadNames).Count())
					model = m;
				else
				{
					props = Model.GetPropertiesNotRequiredToPost(m).Select(x => x.Name).ToList();

					if (props.Intersect(payloadNames).Count() == props.Union(payloadNames).Count())
						model = m;
				}
			}

			if (model != null)
			{
				result = Activator.CreateInstance(model);

				foreach (PropertyInfo p in Model.GetPropertiesWithExclusions(model, true))
				{
					UnsafeAttribute skipValidationAttrib = (UnsafeAttribute)p.GetCustomAttributes(typeof(UnsafeAttribute), false).FirstOrDefault();
					NotRequiredAttribute notRequiredAttrib = (NotRequiredAttribute)p.GetCustomAttributes(typeof(NotRequiredAttribute), false).FirstOrDefault();

					if (notRequiredAttrib != null && !payload.ContainsKey(p.Name)) continue;

					string propertyValue = payload[p.Name];

					if (skipValidationAttrib == null)
						propertyValue = GetValidatedFormValue(p.Name);

					if (p.PropertyType == typeof(int) ||
							p.PropertyType == typeof(int?))
					{
						if (propertyValue.IsInt32())
							p.SetValue(result, Convert.ToInt32(propertyValue, CultureInfo.InvariantCulture), null);
					}
					else if (p.PropertyType == typeof(string))
					{
						p.SetValue(result, propertyValue, null);
					}
					else if (p.PropertyType == typeof(bool))
					{
						if (propertyValue.IsBool())
							p.SetValue(result, Convert.ToBoolean(propertyValue, CultureInfo.InvariantCulture), null);
					}
					else if (p.PropertyType == typeof(DateTime?))
					{
						DateTime? dt = null;

						propertyValue.IsDate(out dt);

						p.SetValue(result, dt, null);
					}
					else if (p.PropertyType == typeof(PostedFile))
					{
						if (files.Count() > 0)
							p.SetValue(result, files[0], null);
					}
					else if (p.PropertyType == typeof(List<PostedFile>))
					{
						p.SetValue(result, files, null);
					}
				}

				(result as Model).Validate(payload);
			}

			return result;
		}

		private List<Type> GetTypeList(Type t)
		{
			t.ThrowIfArgumentNull();

			var types = (from assembly in AppDomain.CurrentDomain.GetAssemblies().Where(x => x.GetName().Name != "DotNetOpenAuth")
									 from type in assembly.GetTypes().Where(x => x.BaseType == t)
									 select type);

			if (types != null)
				return types.ToList();

			return null;
		}

		private List<string> GetControllerActionNames(string controllerName)
		{
			controllerName.ThrowIfArgumentNull();

			return controllers.FirstOrDefault(x => x.GetType().Name == controllerName).GetType().GetMethods()
																							 .Where(x => x.GetCustomAttributes(typeof(RequestTypeAttribute), false).Count() > 0)
																							 .Select(x => x.Name).ToList();
		}

		private List<Controller> GetControllerInstances()
		{
			List<Controller> instances = new List<Controller>();

			foreach (Type c in GetTypeList(typeof(Controller)))
				instances.Add(Controller.CreateInstance(c, this));

			return instances;
		}

		private FrontController GetFrontControllerInstance()
		{
			FrontController fc = null;
			List<Type> frontController = GetTypeList(typeof(FrontController));

			if (frontController != null && frontController.Count > 0)
				fc = FrontController.CreateInstance(frontController[0], this);

			return fc;
		}

		internal ActionParameterInfo GetActionParameterTransforms(ParameterInfo[] actionParams, List<object> bindings)
		{
			ActionParameterInfo actionParameterInfo = new ActionParameterInfo();
			Dictionary<string, object> cachedActionParamTransformInstances = new Dictionary<string, object>();

			List<Tuple<ActionParameterTransformAttribute, int>> actionParameterTransforms = actionParams
				.Select((x, i) => new Tuple<ActionParameterTransformAttribute, int>((ActionParameterTransformAttribute)x.GetCustomAttributes(typeof(ActionParameterTransformAttribute), false).FirstOrDefault(), i))
				.Where(x => x.Item1 != null).ToList();

			foreach (var apt in actionParameterTransforms)
			{
				Type actionTransformClassType = (from assembly in AppDomain.CurrentDomain.GetAssemblies().Where(x => x.GetName().Name != "DotNetOpenAuth")
																				 from type in assembly.GetTypes().Where(x => x.GetInterface(typeof(IActionParamTransform<,>).Name) != null && x.Name == apt.Item1.TransformName)
																				 select type).FirstOrDefault();

				if (actionTransformClassType != null)
				{
					object instance = Activator.CreateInstance(actionTransformClassType, (bindings != null) ? bindings.ToArray() : null);
					MethodInfo transformMethod = actionTransformClassType.GetMethod("Transform");

					cachedActionParamTransformInstances[apt.Item1.TransformName] = new Tuple<MethodInfo, object>(transformMethod, instance);
				}
			}

			if (!(cachedActionParamTransformInstances.Count() > 0))
				cachedActionParamTransformInstances = null;

			actionParameterInfo.ActionParamTransforms = actionParameterTransforms;
			actionParameterInfo.ActionParamTransformInstances = cachedActionParamTransformInstances;

			return actionParameterInfo;
		}

		internal IActionFilterResult[] ProcessAnyActionFilters(RouteInfo routeInfo)
		{
			List<IActionFilterResult> results = new List<IActionFilterResult>();
			List<ActionFilterAttribute> actionFilterAttributes =
				routeInfo.Action.GetCustomAttributes(typeof(ActionFilterAttribute), false).Cast<ActionFilterAttribute>().ToList();

			foreach (ActionFilterAttribute afa in actionFilterAttributes)
			{
				afa.Init(this);
				afa.Controller = routeInfo.Controller;
				afa.OnFilter(routeInfo);

				if (afa.FilterResult != null)
					results.Add(afa.FilterResult);
			}

			return results.ToArray();
		}

		internal void RemoveRoute(string alias)
		{
			RouteInfo routeInfo = routeInfos.FirstOrDefault(x => x.Aliases.FirstOrDefault(a => a == alias) != null);

			if (routeInfo != null && routeInfo.Dynamic)
				routeInfos.Remove(routeInfo);
		}

		internal void AddRoute(List<RouteInfo> routeInfos, Controller c, MethodInfo action, List<string> aliases, string defaultParams)
		{
			//if (routeInfos.Where(x => x.Aliases.Intersect(aliases).Count() > 0).Count() > 0)
			//	return;

			if (action != null)
			{
				List<object> bindings = null;
				ActionParameterInfo actionParameterInfo = null;
				Dictionary<string, object> cachedActionParamTransformInstances = new Dictionary<string, object>();
				RequestTypeAttribute rta = (RequestTypeAttribute)action.GetCustomAttributes(typeof(RequestTypeAttribute), false).FirstOrDefault();

				if (actionBindings.ContainsKey(c.GetType().Name))
					if (actionBindings[c.GetType().Name].ContainsKey(action.Name))
						bindings = actionBindings[c.GetType().Name][action.Name];

				actionParameterInfo = GetActionParameterTransforms(action.GetParameters(), bindings);

				routeInfos.Add(new RouteInfo()
				{
					Aliases = aliases,
					Action = action,
					Controller = c,
					RequestTypeAttribute = rta,
					BoundParams = (bindings != null) ? bindings.ToArray() : new object[] { },
					IBoundToActionParams = (bindings != null) ? bindings.Where(x => x.GetType().GetInterface("IBoundToAction") != null).Cast<IBoundToAction>().ToArray() : null,
					DefaultParams = (!string.IsNullOrEmpty(defaultParams)) ? defaultParams.Split('/').ToObjectArray() : new object[] { },
					ActionParamTransforms = (actionParameterInfo.ActionParamTransforms.Count() > 0) ? actionParameterInfo.ActionParamTransforms : null,
					CachedActionParamTransformInstances = actionParameterInfo.ActionParamTransformInstances,
					Dynamic = string.IsNullOrEmpty(defaultParams) ? true : false
				});
			}
		}

		internal void AddRoute(List<RouteInfo> routeInfos, string alias, string controllerName, string actionName, string defaultParams)
		{
			alias.ThrowIfArgumentNull();
			controllerName.ThrowIfArgumentNull();
			actionName.ThrowIfArgumentNull();

			Controller c = controllers.FirstOrDefault(x => x.GetType().Name == controllerName);

			if (c != null)
			{
				MethodInfo action = c.GetType().GetMethods().FirstOrDefault(x => x.GetCustomAttributes(typeof(RequestTypeAttribute), false).Count() > 0 && x.Name == actionName);

				AddRoute(routeInfos, c, action, new List<string> { alias }, defaultParams);
			}
		}

		private List<RouteInfo> GetRouteInfos()
		{
			List<RouteInfo> routeInfos = new List<RouteInfo>();

			foreach (Controller c in controllers)
			{
				var actions = c.GetType().GetMethods().Where(x => x.GetCustomAttributes(typeof(RequestTypeAttribute), false).Count()>0);

				foreach (MethodInfo action in actions)
				{
					List<string> aliases = action.GetCustomAttributes(typeof(AliasAttribute), false).Select(x => (x as AliasAttribute).Alias).ToList();
					RequestTypeAttribute rta = (RequestTypeAttribute)action.GetCustomAttributes(typeof(RequestTypeAttribute), false).FirstOrDefault();

					if (string.IsNullOrEmpty(rta.RouteAlias))
						aliases.Add(string.Format("/{0}/{1}", c.GetType().Name, action.Name));
					else
						aliases.Add(rta.RouteAlias);

					AddRoute(routeInfos, c, action, aliases, null);
				}
			}

			return routeInfos;
		}

		internal List<string> GetAllRouteAliases()
		{
			return routeInfos.SelectMany(x => x.Aliases).ToList();
		}

		private RouteInfo RaiseEventOnFrontController(RouteHandlerEventType eventType, string path, RouteInfo routeInfo, object data)
		{
			if (frontController != null)
				return frontController.RaiseEvent(eventType, path, routeInfo, data);

			return null;
		}

		private string CreateToken()
		{
			return (Guid.NewGuid().ToString() + Guid.NewGuid().ToString()).Replace("-", string.Empty);
		}

		internal string CreateAntiForgeryToken()
		{
			string token = CreateToken();

			antiForgeryTokens.Add(token);

			return token;
		}

		internal void LogOn(string id, string[] roles)
		{
			id.ThrowIfArgumentNull();
			roles.ThrowIfArgumentNull();

			if (currentUser != null)
				return;

			string authToken = CreateToken();
			DateTime expiration = DateTime.Now.Add(TimeSpan.FromHours(8));

			AuthCookie authCookie = new AuthCookie()
			{
				ID = id,
				AuthToken = authToken,
				Expiration = expiration
			};

			User u = new User()
			{
				AuthenticationCookie = authCookie,
				SessionId = sessionID,
				//ClientCertificate = request
				IPAddress = IPAddress,
				LogOnDate = DateTime.Now,
				Name = id,
				Roles = roles.ToList()
			};

			users.Add(u);

			currentUser = u;

			AddSession(currentUserSessionName, u);
		}

		internal bool LogOff()
		{
			if (currentUser != null && users.Remove(currentUser))
			{
				currentUser = null;
				RemoveSession(currentUserSessionName);

				return true;
			}

			return false;
		}

		internal void AddControllerSession(string key, object value)
		{
			controllersSession[key] = value;
		}

		internal object GetControllerSession(string key)
		{
			if (controllersSession.ContainsKey(key))
				return controllersSession[key];

			return null;
		}

		internal string MapPath(string path)
		{
			return appRoot + path.Replace('/', '\\');
		}
		#endregion

		#region ASP.NET ADAPTER CALLBACKS
		public object GetApplication(string key)
		{
			if (app.ContainsKey(HttpAdapterConstants.ApplicationSessionStoreGetCallback) &&
					app[HttpAdapterConstants.ApplicationSessionStoreGetCallback] is Func<string, object>)
			{
				return (app[HttpAdapterConstants.ApplicationSessionStoreGetCallback] as Func<string, object>)(key);
			}

			return null;
		}

		public void AddApplication(string key, object value)
		{
			if (app.ContainsKey(HttpAdapterConstants.ApplicationSessionStoreAddCallback) &&
					app[HttpAdapterConstants.ApplicationSessionStoreAddCallback] is Action<string, object>)
			{
				(app[HttpAdapterConstants.ApplicationSessionStoreAddCallback] as Action<string, object>)(key, value);
			}
		}

		public object GetSession(string key)
		{
			if (app.ContainsKey(HttpAdapterConstants.UserSessionStoreGetCallback) &&
					app[HttpAdapterConstants.UserSessionStoreGetCallback] is Func<string, object>)
			{
				return (app[HttpAdapterConstants.UserSessionStoreGetCallback] as Func<string, object>)(key);
			}

			return null;
		}

		public void AddSession(string key, object value)
		{
			if (app.ContainsKey(HttpAdapterConstants.UserSessionStoreAddCallback) &&
					app[HttpAdapterConstants.UserSessionStoreAddCallback] is Action<string, object>)
			{
				(app[HttpAdapterConstants.UserSessionStoreAddCallback] as Action<string, object>)(key, value);
			}
		}

		public void RemoveSession(string key)
		{
			if (app.ContainsKey(HttpAdapterConstants.UserSessionStoreRemoveCallback) &&
					app[HttpAdapterConstants.UserSessionStoreRemoveCallback] is Action<string>)
			{
				(app[HttpAdapterConstants.UserSessionStoreRemoveCallback] as Action<string>)(key);
			}
		}

		public void AbandonSession()
		{
			if (app.ContainsKey(HttpAdapterConstants.UserSessionStoreAbandonCallback) &&
					app[HttpAdapterConstants.UserSessionStoreAbandonCallback] is Action)
			{
				(app[HttpAdapterConstants.UserSessionStoreAbandonCallback] as Action)();
			}
		}

		public void ResponseRedirect(string path, bool fromRedirectOnly)
		{
			if (app.ContainsKey(HttpAdapterConstants.ResponseRedirectCallback) &&
				app[HttpAdapterConstants.ResponseRedirectCallback] is Action<string, Dictionary<string, string>>)
			{
				if (fromRedirectOnly)
					AddSession(fromRedirectOnlySessionName, fromRedirectOnly);

				(app[HttpAdapterConstants.ResponseRedirectCallback] as Action<string, Dictionary<string, string>>)(path, null);
			}
		}

		public string GetValidatedFormValue(string key)
		{
			if (request.ContainsKey(HttpAdapterConstants.RequestFormCallback) &&
					request[HttpAdapterConstants.RequestFormCallback] is Func<string, bool, string>)
			{
				return (request[HttpAdapterConstants.RequestFormCallback] as Func<string, bool, string>)(key, true);
			}

			return null;
		}
		#endregion
	}
	#endregion

	#region ROUTE INFO
	public class RouteInfo
	{
		public List<string> Aliases { get; internal set; }
		public MethodInfo Action { get; internal set; }
		public Controller Controller { get; internal set; }
		public RequestTypeAttribute RequestTypeAttribute { get; internal set; }
		public object[] ActionParams { get; internal set; }
		public object[] BoundParams { get; internal set; }
		public IBoundToAction[] IBoundToActionParams { get; internal set; }
		public object[] DefaultParams { get; internal set; }
		public List<Tuple<ActionParameterTransformAttribute, int>> ActionParamTransforms { get; internal set; }
		public Dictionary<string, object> CachedActionParamTransformInstances { get; internal set; }
		public bool Dynamic { get; internal set; }
	}
	#endregion

	#region ACTION PARAMETER TRANSFORM
	public interface IActionParamTransform<T, V>
	{
		T Transform(V value);
	}

	internal class ActionParameterInfo
	{
		public Dictionary<string, object> ActionParamTransformInstances { get; set; }
		public List<Tuple<ActionParameterTransformAttribute, int>> ActionParamTransforms { get; set; }
	}
	#endregion

	#region ACTION FILTER
	public interface IActionFilterResult { }

	[AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
	public abstract class ActionFilterAttribute : Attribute
	{
		private Engine engine;
		internal Controller Controller { get; set; }
		public IActionFilterResult FilterResult { get; set; }
		public abstract void OnFilter(RouteInfo routeInfo);

		internal void Init(Engine engine)
		{
			this.engine = engine;
		}

		public void Redirect(string alias)
		{
			engine.ResponseRedirect(alias, false);
		}

		public void RedirectOnly(string alias)
		{
			engine.ResponseRedirect(alias, true);
		}
	}
	#endregion

	#region ACTION BINDINGS
	public interface IBoundToAction
	{
		void Initialize(Controller c);
	}
	#endregion

	#region MODEL
	public abstract class Model
	{
		[Hidden]
		public bool IsValid { get; private set; }
		[Hidden]
		public string Error { get; private set; }

		private bool ValidateRequiredLengthAttribute(RequiredLengthAttribute requiredLengthAttribute, PropertyInfo property, object value, out string error)
		{
			bool result = false;
			error = string.Empty;

			if (requiredLengthAttribute != null)
			{
				string sValue = value as string;

				if (!string.IsNullOrEmpty(sValue))
				{
					if (sValue.Length >= requiredLengthAttribute.Length)
						result = true;
					else
						result = false;
				}
			}

			if(!result)
				error = string.Format("{0} has a required length that was not met", property.Name);

			return result;
		}

		private bool ValidateRequiredAttribute(RequiredAttribute requiredAttribute, PropertyInfo property, object value, out string error)
		{
			bool result = false;
			error = string.Empty;

			if (requiredAttribute != null)
			{
				if (value is string)
				{
					if (!string.IsNullOrEmpty(value as string))
						result = true;
					else
						result = false;
				}
				else if (value != null)
					result = true;
				else
					result = false;
			}

			if (!result)
				error = string.Format("{0} is a required field", property.Name);

			return result;
		}

		private bool ValidateRegularExpressionAttribute(RegularExpressionAttribute regularExpressionAttribute, PropertyInfo property, object value, out string error)
		{
			bool result = false;
			error = string.Empty;

			if (regularExpressionAttribute != null)
			{
				string sValue = value as string;

				if (!string.IsNullOrEmpty(sValue))
				{
					if (regularExpressionAttribute.Pattern.IsMatch(sValue))
						result = true;
					else
						result = false;
				}
				else
					result = false;
			}

			if(!result)
				error = string.Format("{0} did not pass regular expression validation", property.Name);

			return result;
		}

		private bool ValidateRangeAttribute(RangeAttribute rangeAttribute, PropertyInfo property, object value, out string error)
		{
			bool result = false;
			error = string.Empty;

			if (rangeAttribute != null)
			{
				if (value.GetType().IsAssignableFrom(typeof(Int64)))
				{
					if (((Int64)value).InRange(rangeAttribute.Min, rangeAttribute.Max))
						result = true;
					else
						result = false;
				}
				else
					result = false;
			}

			if(!result)
				error = string.Format("{0} was not within the range specified", property.Name);

			return result;
		}

		internal void Validate(Dictionary<string, string> form)
		{
			List<bool> results = new List<bool>();
			StringBuilder errors = new StringBuilder();

			foreach (PropertyInfo pi in GetPropertiesWithExclusions(GetType(), false))
			{
				bool requiredResult = false;
				bool requiredLengthResult = false;
				bool regularExpressionResult = false;
				bool rangeResult = false;

				RequiredAttribute requiredAttribute = (RequiredAttribute)pi.GetCustomAttributes(false).FirstOrDefault(x => x is RequiredAttribute);
				RequiredLengthAttribute requiredLengthAttribute = (RequiredLengthAttribute)pi.GetCustomAttributes(false).FirstOrDefault(x => x is RequiredLengthAttribute);
				RegularExpressionAttribute regularExpressionAttribute = (RegularExpressionAttribute)pi.GetCustomAttributes(false).FirstOrDefault(x => x is RegularExpressionAttribute);
				RangeAttribute rangeAttribute = (RangeAttribute)pi.GetCustomAttributes(false).FirstOrDefault(x => x is RangeAttribute);

				object value = pi.GetValue(this, null);

				#region REQUIRED
				if (requiredAttribute != null)
				{
					if (form.Keys.FirstOrDefault(x => x == pi.Name) != null)
					{
						string error;

						requiredResult = ValidateRequiredAttribute(requiredAttribute, pi, value, out error);

						if (requiredResult)
							results.Add(true);
						else
							results.Add(false);

						if (!string.IsNullOrEmpty(error))
							errors.AppendLine(error);
					}
				}
				#endregion

				#region REQUIRED LENGTH
				if (requiredLengthAttribute != null)
				{
					string error;

					requiredLengthResult = ValidateRequiredLengthAttribute(requiredLengthAttribute, pi, value, out error);

					if (requiredLengthResult)
						results.Add(true);
					else
						results.Add(false);

					if (!string.IsNullOrEmpty(error))
						errors.AppendLine(error);
				}
				#endregion

				#region REGULAR EXPRESSION
				if (regularExpressionAttribute != null)
				{
					string error;

					regularExpressionResult = ValidateRegularExpressionAttribute(regularExpressionAttribute, pi, value, out error);

					if (regularExpressionResult)
						results.Add(true);
					else
						results.Add(false);

					if (!string.IsNullOrEmpty(error))
						errors.AppendLine(error);
				}
				#endregion

				#region RANGE
				if (rangeAttribute != null)
				{
					string error;

					rangeResult = ValidateRangeAttribute(rangeAttribute, pi, value, out error);

					if (rangeResult)
						results.Add(true);
					else
						results.Add(false);

					if (!string.IsNullOrEmpty(error))
						errors.AppendLine(error);
				}
				#endregion
			}

			if (errors.Length > 0)
				Error = errors.ToString();

			var finalResult = results.Where(x => x == false);

			IsValid = (finalResult.Count()>0) ? false : true;
		}

		internal static List<PropertyInfo> GetPropertiesNotRequiredToPost(Type t)
		{
			if (t.BaseType == typeof(Model))
			{
				var props = GetPropertiesWithExclusions(t, true).Where(x => x.GetCustomAttributes(false).FirstOrDefault(y => y is NotRequiredAttribute) == null);

				if (props != null)
					return props.ToList();
			}

			return null;
		}

		internal static List<PropertyInfo> GetPropertiesWithExclusions(Type t, bool postedFormBinding)
		{
			if (t.BaseType == typeof(Model))
			{
				var props = t.GetProperties().Where(x => x.GetCustomAttributes(false).FirstOrDefault(y => y is HiddenAttribute) == null);

				if (postedFormBinding)
					props = props.Where(x => x.GetCustomAttributes(false).FirstOrDefault(y => y is ExcludeFromBindingAttribute) == null);

				if (props != null)
					return props.ToList();
			}

			return null;
		}
	}
	#endregion

	#region CONTROLLERS

	#region ACTION HANDLER
	internal enum RouteHandlerEventType
	{
		Pre,
		Post,
		PreRoute,
		PostRoute,
		Static,
		CachedViewResult,
		PassedSecurity,
		FailedSecurity,
		MissingRoute,
		Error
	}

	public class RouteHandlerEventArgs : EventArgs
	{
		public string Path { get; set; }
		public RouteInfo RouteInfo { get; set; }
		public object Data { get; set; }
	}
	#endregion

	public abstract class BaseController
	{
		internal Engine engine;
		
		internal bool OnInitComplete { get; set; }
		public string IPAddress { get { return engine.IPAddress; } }
		public User CurrentUser { get { return engine.currentUser; } }
		public X509Certificate2 ClientCertificate { get { return engine.clientCertificate; } }

		protected virtual void OnInit() { }
		protected virtual bool CheckRoles(RouteInfo routeInfo) { return true; }

		internal void InvokeOnInit()
		{
			OnInit();
			OnInitComplete = true;
		}

		internal bool InvokeCheckRoles(RouteInfo routeInfo) { return CheckRoles(routeInfo); }

		internal void Refresh(Engine engine)
		{
			this.engine = engine;
		}

		protected RouteInfo FindRoute(string path)
		{
			return engine.FindRoute(path);
		}

		protected void AddRoute(string alias, string actionName, string defaultParams)
		{
			engine.AddRoute(engine.routeInfos, alias, this.GetType().Name, actionName, defaultParams);
		}

		protected void RemoveRoute(string alias)
		{
			engine.RemoveRoute(alias);
		}

		protected void AddBinding(string actionName, object bindInstance)
		{
			engine.AddBinding(this.GetType().Name, actionName, bindInstance);
		}

		protected void AddBinding(string[] actionNames, object bindInstance)
		{
			engine.AddBinding(this.GetType().Name, actionNames, bindInstance);
		}

		protected void AddBinding(string[] actionNames, object[] bindInstances)
		{
			engine.AddBinding(this.GetType().Name, actionNames, bindInstances);
		}

		protected void AddBindingForAllActions(string controllerName, object bindInstance)
		{
			engine.AddBindingForAllActions(controllerName, bindInstance);
		}

		protected void AddBindingsForAllActions(string controllerName, object[] bindInstances)
		{
			engine.AddBindingsForAllActions(controllerName, bindInstances);
		}

		protected void AddBindingForAllActions(object bindInstance)
		{
			engine.AddBindingForAllActions(this.GetType().Name, bindInstance);
		}

		protected void AddBindingsForAllActions(object[] bindInstances)
		{
			engine.AddBindingsForAllActions(this.GetType().Name, bindInstances);
		}

		protected void AddBundle(string name, string[] paths)
		{
			engine.AddBundle(name, paths);
		}

		protected void LogOn(string id, string[] roles)
		{
			engine.LogOn(id, roles);
		}

		protected void LogOff()
		{
			engine.LogOff();
		}

		protected List<string> GetAllRouteAliases()
		{
			return engine.GetAllRouteAliases();
		}

		protected void Redirect(string path)
		{
			engine.ResponseRedirect(path, false);
		}

		protected void Redirect(string alias, params string[] parameters)
		{
			engine.ResponseRedirect(string.Format("{0}/{1}", alias, string.Join("/", parameters)), false);
		}

		protected void RedirectOnly(string path)
		{
			engine.ResponseRedirect(path, true);
		}

		protected void ProtectFile(string path, string roles)
		{
			engine.ProtectFile(path, roles);
		}

		protected void AddSession(string key, object value)
		{
			engine.AddControllerSession(key, value);
		}

		protected object GetSession(string key)
		{
			return engine.GetControllerSession(key);
		}

		protected string MapPath(string path)
		{
			return engine.MapPath(path);
		}
	}

	public abstract class FrontController : BaseController
	{
		public event EventHandler<RouteHandlerEventArgs> PreActionEvent = (sender, args) => { };
		public event EventHandler<RouteHandlerEventArgs> PostActionEvent = (sender, args) => { };
		public event EventHandler<RouteHandlerEventArgs> StaticRouteEvent = (sender, args) => { };
		public event EventHandler<RouteHandlerEventArgs> CachedViewResultEvent = (sender, args) => { };
		public event EventHandler<RouteHandlerEventArgs> PreRouteDeterminationEvent = (sender, args) => { };
		public event EventHandler<RouteHandlerEventArgs> PostRouteDeterminationEvent = (sender, args) => { };
		public event EventHandler<RouteHandlerEventArgs> PassedSecurityEvent = (sender, args) => { };
		public event EventHandler<RouteHandlerEventArgs> FailedSecurityEvent = (sender, args) => { };
		public event EventHandler<RouteHandlerEventArgs> MissingRouteEvent = (sender, args) => { };
		public event EventHandler<RouteHandlerEventArgs> ErrorEvent = (sender, args) => { };

		internal static FrontController CreateInstance(Type type, Engine engine)
		{
			FrontController controller = (FrontController)Activator.CreateInstance(type);

			controller.engine = engine;

			return controller;
		}

		internal RouteInfo RaiseEvent(RouteHandlerEventType type, string path, RouteInfo routeInfo, object data = null)
		{
			RouteInfo route = routeInfo;

			RouteHandlerEventArgs args = new RouteHandlerEventArgs()
			{
				Path = path,
				RouteInfo = routeInfo,
				Data = data
			};

			switch (type)
			{
				case RouteHandlerEventType.Pre:
					PreActionEvent(this, args);
					break;

				case RouteHandlerEventType.Post:
					PostActionEvent(this, args);
					break;

				case RouteHandlerEventType.PreRoute:
					PreRouteDeterminationEvent(this, args);
					break;

				case RouteHandlerEventType.PostRoute:
					PostRouteDeterminationEvent(this, args);
					break;

				case RouteHandlerEventType.Static:
					StaticRouteEvent(this, args);
					break;

				case RouteHandlerEventType.CachedViewResult:
					CachedViewResultEvent(this, args);
					break;

				case RouteHandlerEventType.PassedSecurity:
					PassedSecurityEvent(this, args);
					break;

				case RouteHandlerEventType.FailedSecurity:
					FailedSecurityEvent(this, args);
					break;

				case RouteHandlerEventType.MissingRoute:
					MissingRouteEvent(this, args);

					if (args.RouteInfo != null)
						route = args.RouteInfo;
					break;

				case RouteHandlerEventType.Error:
					ErrorEvent(this, args);
					break;
			}

			return route;
		}
	}

	public abstract class Controller : BaseController
	{
		private string partitionName;

		protected Dictionary<string, string> ViewTags { get; private set; }
		protected Dictionary<string, Dictionary<string, string>> FragTags { get; private set; }
		protected dynamic DViewTags { get; private set; }
		protected dynamic DFragTags { get; private set; }

		public event EventHandler<RouteHandlerEventArgs> PreActionEvent = (sender, args) => { };
		public event EventHandler<RouteHandlerEventArgs> PostActionEvent = (sender, args) => { };

		protected Controller()
		{
			initializeViewTags();

			partitionName = GetPartitionName();
		}

		private void initializeViewTags()
		{
			ViewTags = new Dictionary<string, string>();
			FragTags = new Dictionary<string, Dictionary<string, string>>();
			DFragTags = new DynamicDictionary();
			DViewTags = new DynamicDictionary();
		}

		internal static Controller CreateInstance(Type type, Engine engine)
		{
			Controller controller = (Controller)Activator.CreateInstance(type);

			controller.engine = engine;

			return controller;
		}

		private Dictionary<string, string> GetViewTagsDictionary()
		{
			Dictionary<string, string> viewTags = ViewTags;

			if (!DViewTags.IsEmpty())
			{
				Dictionary<string, object> _viewTags = DViewTags.GetDynamicDictionary();

				if (_viewTags != null)
					viewTags = _viewTags.ToDictionary(k => k.Key, k => (k.Value != null) ? k.Value.ToString() : string.Empty);
			}

			return viewTags;
		}

		internal void RaiseEvent(RouteHandlerEventType type, string path, RouteInfo routeInfo)
		{
			RouteHandlerEventArgs args = new RouteHandlerEventArgs()
			{
				Path = path,
				RouteInfo = routeInfo,
				Data = null
			};

			switch (type)
			{
				case RouteHandlerEventType.Pre:
					PreActionEvent(this, args);
					break;

				case RouteHandlerEventType.Post:
					PostActionEvent(this, args);
					break;
			}
		}

		private string GetPartitionName()
		{
			string partitionName = null;

			PartitionAttribute partitionAttrib = (PartitionAttribute)this.GetType().GetCustomAttributes(false).FirstOrDefault(x => x.GetType() == typeof(PartitionAttribute));

			if (partitionAttrib != null)
				partitionName = partitionAttrib.Name;

			return partitionName;
		}

		#region RENDER FRAGMENT
		public string RenderFragment(string fragmentName)
		{
			Dictionary<string, string> fragTags = null;

			if (!DFragTags.IsEmpty())
			{
				Dictionary<string, object> _fragTags = DFragTags.GetDynamicDictionary(fragmentName);

				if (_fragTags != null)
					fragTags = _fragTags.ToDictionary(k => k.Key, k => k.Value.ToString());
			}
			else if (FragTags.ContainsKey(fragmentName))
					fragTags = FragTags[fragmentName];

			return RenderFragment(fragmentName, fragTags);
		}

		public string RenderFragment(string fragmentName, Dictionary<string, string> fragTags)
		{
			return engine.ViewEngine.LoadView(partitionName, this.GetType().Name, fragmentName, ViewTemplateType.Fragment, fragTags);
		}
		#endregion

		#region VIEW
		public ViewResult View()
		{
			StackFrame stackFrame = new StackFrame(1);

			return View(this.GetType().Name, stackFrame.GetMethod().Name);
		}

		public ViewResult View(string name)
		{
			return View(this.GetType().Name, name);
		}

		public ViewResult View(string controllerName, string actionName)
		{
			Dictionary<string, string> viewTags = GetViewTagsDictionary();

			initializeViewTags();

			return new ViewResult(engine.ViewEngine, partitionName, controllerName, actionName, viewTags);
		}

		public FileResult View(string fileName, byte[] fileBytes, string contentType)
		{
			return new FileResult(fileName, fileBytes, contentType);
		}
		#endregion
	}
	#endregion

	#region SECURITY
	public class AuthCookie
	{
		public string ID { get; set; }
		public string AuthToken { get; set; }
		public DateTime Expiration { get; set; }
	}

	public class User
	{
		public string Name { get; internal set; }
		public AuthCookie AuthenticationCookie { get; internal set; }
		public string SessionId { get; internal set; }
		public string IPAddress { get; internal set; }
		public DateTime LogOnDate { get; internal set; }
		public List<string> Roles { get; internal set; }
		public X509Certificate2 ClientCertificate { get; internal set; }

		public bool IsInRole(string role)
		{
			if (Roles != null)
				return Roles.Contains(role);

			return false;
		}
	}
	#endregion

	#region VIEW RESULTS
	public class ViewResponse
	{
		public Dictionary<string, string> Headers { get; set; }
		public string ContentType { get; set; }
		public object Content { get; set; }
		public string RedirectTo { get; set; }
	}

	public interface IViewResult
	{
		ViewResponse Render();
	}

	public class ViewResult : IViewResult
	{
		private IViewEngine viewEngine;
		private string partitionName;
		private string controllerName;
		private string viewName;
		private Dictionary<string, string> viewTags;

		public ViewResult(IViewEngine viewEngine, string partitionName, string controllerName, string viewName, Dictionary<string, string> viewTags)
		{
			this.viewEngine = viewEngine;
			this.partitionName = partitionName;
			this.controllerName = controllerName;
			this.viewName = viewName;
			this.viewTags = viewTags;
		}

		public ViewResponse Render()
		{
			string view = viewEngine.LoadView(partitionName, controllerName, viewName, ViewTemplateType.Action, viewTags);

			if (string.IsNullOrEmpty(view))
				return null;

			return new ViewResponse()
			{
				ContentType = "text/html",
				Content = view,
				Headers = new Dictionary<string, string>()
			};
		}
	}

	public class FileResult : IViewResult
	{
		private string path;
		private byte[] file;
		private string contentType;
		private Dictionary<string, string> headers;

		public FileResult(string name, string data)
			: this(name, ASCIIEncoding.UTF8.GetBytes(data), null) { }

		public FileResult(string name, byte[] data, string contentType)
		{
			string fileExtension = Path.GetExtension(name);

			if (!string.IsNullOrEmpty(contentType) || Config.MimeTypes.ContainsKey(fileExtension))
				contentType = Config.MimeTypes[fileExtension];

			this.contentType = contentType;
			file = data;
		}

		public FileResult(string path)
		{
			this.path = path;

			if (File.Exists(path) && Config.AllowedFilePattern.IsMatch(path))
			{
				string fileExtension = Path.GetExtension(path);

				if (Config.MimeTypes.ContainsKey(fileExtension))
				{
					contentType = Config.MimeTypes[fileExtension];
					file = File.ReadAllBytes(path);
				}
			}
		}

		public ViewResponse Render()
		{
			if (file == null)
				return null;

			headers = new Dictionary<string, string>();
			headers["Cache-Control"] = string.Format("public, max-age={0}", 600);
			headers["Expires"] = DateTime.Now.Add(new TimeSpan(0, 0, 10, 0, 0)).ToUniversalTime().ToString("r");

			return new ViewResponse()
			{
				Content = file,
				ContentType = contentType,
				Headers = headers
			};
		}
	}

	public class JsonResult : IViewResult
	{
		private string json;

		public JsonResult(object data)
		{
			json = JsonConvert.SerializeObject(data);
		}

		public ViewResponse Render()
		{
			return new ViewResponse()
			{
				Content = null,
				ContentType = "application/json",
				Headers = new Dictionary<string, string>()
			};
		}
	}
	#endregion

	#region VIEW ENGINE
	public enum ViewTemplateType
	{
		Fragment,
		Shared,
		Action
	}

	internal class ViewTemplate
	{
		public string Name { get; set; }
		public string FullName { get; set; }
		public string Partition { get; set; }
		public string Controller { get; set; }
		public string Path { get; set; }
		public string Template { get; set; }
		public string MD5sum { get; set; }

		public ViewTemplateType TemplateType { get; set; }
	}

	internal class CompiledView
	{
		public string Name { get; set; }
		public string FullName { get; set; }
		public string Render { get; set; }
		public string Template { get; set; }
		public string CompiledTemplate { get; set; }
		public string TemplateMD5sum { get; set; }
	}

	internal interface IViewTemplateLoader
	{
		List<ViewTemplate> Load();
		ViewTemplate Load(string path);
	}

	internal class ViewTemplateLoader : IViewTemplateLoader
	{
		private string appRoot;
		private string[] viewRoots;
		private string sharedHint = @"shared\";
		private string fragmentsHint = @"fragments\";
		private static Regex commentBlockRE = new Regex(@"\@\@(?<block>[\s\w\p{P}\p{S}]+?)\@\@");

		public ViewTemplateLoader(string appRoot, string[] viewRoots)
		{
			if (string.IsNullOrEmpty(appRoot))
				throw new ArgumentNullException("applicationRoot");

			this.appRoot = appRoot;
			this.viewRoots = viewRoots;
		}

		public List<ViewTemplate> Load()
		{
			List<ViewTemplate> templates = new List<ViewTemplate>();

			foreach (string viewRoot in viewRoots)
			{
				if (Directory.Exists(viewRoot))
					foreach (FileInfo fi in new DirectoryInfo(viewRoot).GetAllFiles().Where(x => x.Extension == ".html"))
						templates.Add(Load(fi));
			}

			return templates;
		}

		public ViewTemplate Load(FileInfo fi)
		{
			string viewRoot = viewRoots.FirstOrDefault(x => fi.FullName.StartsWith(x));

			if (string.IsNullOrEmpty(viewRoot)) return null;

			DirectoryInfo rootDir = new DirectoryInfo(viewRoot);

			StringBuilder templateBuilder;
			string templateName = fi.Name.Replace(fi.Extension, string.Empty);
			string templateKeyName = fi.FullName.Replace(rootDir.Parent.FullName, string.Empty)
																					.Replace(appRoot, string.Empty)
																					.Replace(fi.Extension, string.Empty)
																					.Replace("\\", "/").TrimStart('/');

			using (StreamReader sr = new StreamReader(fi.OpenRead()))
				templateBuilder = new StringBuilder(sr.ReadToEnd());

			#region STRIP COMMENT SECTIONS
			MatchCollection comments = commentBlockRE.Matches(templateBuilder.ToString());

			if (comments.Count > 0)
			{
				foreach (Match comment in comments)
					templateBuilder.Replace(comment.Value, string.Empty);
			}
			#endregion

			ViewTemplateType templateType = ViewTemplateType.Action;

			if (fi.FullName.ToLower().Contains(sharedHint))
				templateType = ViewTemplateType.Shared;
			else if (fi.FullName.ToLower().Contains(fragmentsHint))
				templateType = ViewTemplateType.Fragment;

			string partition = null;
			string controller = null;

			if (templateType == ViewTemplateType.Action ||
					templateType == ViewTemplateType.Shared)
			{
				string[] keyParts = templateKeyName.Split('/');

				if (keyParts.Length > 2)
				{
					partition = keyParts[0];
					controller = keyParts[1];
				}
				else
				{
					partition = null;
					controller = keyParts[0];
				}
			}

			string template = templateBuilder.ToString();

			return new ViewTemplate()
			{
				MD5sum = template.CalculateMD5sum(),
				Partition = partition,
				Controller = controller,
				FullName = templateKeyName,
				Name = templateName,
				Path = fi.FullName,
				Template = template,
				TemplateType = templateType
			};
		}

		public ViewTemplate Load(string path)
		{
			if (File.Exists(path))
				return Load(new FileInfo(path));

			return null;
		}
	}

	internal enum DirectiveProcessType
	{
		Compile,
		AfterCompile,
		Render
	}

	internal interface IViewCompilerDirectiveHandler
	{
		DirectiveProcessType Type { get; }

		StringBuilder Process(ViewCompilerDirectiveInfo directiveInfo);
	}

	internal interface IViewCompilerSubstitutionHandler
	{
		DirectiveProcessType Type { get; }

		StringBuilder Process(StringBuilder content);
	}

	internal interface IViewCompiler
	{
		List<CompiledView> CompileAll();
		CompiledView Compile(string partitionName, string controllerName, string viewName, ViewTemplateType viewType);
		CompiledView Render(string fullName, Dictionary<string, string> tags);
	}

	internal class ViewCompilerDirectiveInfo
	{
		public Match Match { get; set; }
		public string Directive { get; set; }
		public string Value { get; set; }
		public StringBuilder Content { get; set; }
		public List<ViewTemplate> ViewTemplates { get; set; }
		public Func<string, string> DetermineKeyName { get; set; }
		public Action<string> AddPageDependency { get; set; }
	}

	internal class HeadSubstitution : IViewCompilerSubstitutionHandler
	{
		private static Regex headBlockRE = new Regex(@"\[\[(?<block>[\s\w\p{P}\p{S}]+)\]\]", RegexOptions.Compiled);
		private static string headDirective = "%%Head%%";

		public DirectiveProcessType Type { get; private set; }

		public HeadSubstitution()
		{
			Type = DirectiveProcessType.Compile;
		}

		public StringBuilder Process(StringBuilder content)
		{
			MatchCollection heads = headBlockRE.Matches(content.ToString());

			if (heads.Count > 0)
			{
				StringBuilder headSubstitutions = new StringBuilder();

				foreach (Match head in heads)
				{
					headSubstitutions.Append(Regex.Replace(head.Groups["block"].Value, @"^(\s+)", string.Empty, RegexOptions.Multiline));
					content.Replace(head.Value, string.Empty);
				}

				content.Replace(headDirective, headSubstitutions.ToString());
			}

			content.Replace(headDirective, string.Empty);

			return content;
		}
	}

	internal class AntiForgeryTokenSubstitution : IViewCompilerSubstitutionHandler
	{
		private Func<string> createAntiForgeryToken;

		public DirectiveProcessType Type { get; private set; }

		public AntiForgeryTokenSubstitution(Func<string> createAntiForgeryToken)
		{
			this.createAntiForgeryToken = createAntiForgeryToken;

			Type = DirectiveProcessType.Render;
		}

		public StringBuilder Process(StringBuilder content)
		{
			string tokenName = "%%AntiForgeryToken%%";

			var tokens = Regex.Matches(content.ToString(), tokenName)
									.Cast<Match>()
									.Select(m => new { Start = m.Index, End = m.Length })
									.Reverse();

			foreach (var t in tokens)
				content.Replace(tokenName, createAntiForgeryToken(), t.Start, t.End);

			return content;
		}
	}

	internal class MasterPageDirective : IViewCompilerDirectiveHandler
	{
		public DirectiveProcessType Type { get; private set; }

		public MasterPageDirective()
		{
			Type = DirectiveProcessType.Compile;
		}

		public StringBuilder Process(ViewCompilerDirectiveInfo directiveInfo)
		{
			if (directiveInfo.Directive == "Master")
			{
				StringBuilder finalPage = new StringBuilder();

				string masterPageName = directiveInfo.DetermineKeyName(directiveInfo.Value);
				string masterPageTemplate = directiveInfo.ViewTemplates.FirstOrDefault(x => x.FullName == masterPageName).Template;

				directiveInfo.AddPageDependency(masterPageName);

				finalPage.Append(masterPageTemplate);
				finalPage.Replace("%%View%%", directiveInfo.Content.ToString());

				finalPage.Replace(directiveInfo.Match.Groups[0].Value, string.Empty);

				return finalPage;
			}

			return directiveInfo.Content;
		}
	}

	internal class PartialPageDirective : IViewCompilerDirectiveHandler
	{
		public DirectiveProcessType Type { get; private set; }

		public PartialPageDirective()
		{
			Type = DirectiveProcessType.AfterCompile;
		}

		public StringBuilder Process(ViewCompilerDirectiveInfo directiveInfo)
		{
			if (directiveInfo.Directive == "Partial")
			{
				string partialPageName = directiveInfo.DetermineKeyName(directiveInfo.Value);
				string partialPageTemplate = directiveInfo.ViewTemplates.FirstOrDefault(x => x.FullName == partialPageName).Template;

				directiveInfo.Content.Replace(directiveInfo.Match.Groups[0].Value, partialPageTemplate);
			}

			return directiveInfo.Content;
		}
	}

	internal class BundleDirective : IViewCompilerDirectiveHandler
	{
		private bool debugMode;
		private Func<string, string[]> getBundleFiles;

		private static string cssIncludeTag = @"<link href=""{0}"" rel=""stylesheet"" type=""text/css"" />";
		private static string jsIncludeTag = @"<script src=""{0}"" type=""text/javascript""></script>";

		public DirectiveProcessType Type { get; private set; }

		public BundleDirective(bool debugMode, Func<string, string[]> getBundleFiles)
		{
			this.debugMode = debugMode;
			this.getBundleFiles = getBundleFiles;

			Type = DirectiveProcessType.AfterCompile;
		}

		public string ProcessBundleLink(string bundlePath)
		{
			string tag = string.Empty;
			string extension = Path.GetExtension(bundlePath);
			bool isAPath = bundlePath.Contains('/') ? true : false;

			string modifiedBundlePath = bundlePath;

			if (!isAPath)
				modifiedBundlePath = string.Format("{0}/{1}/{2}", Config.SharedResourceFolderPath, extension.TrimStart('.'), bundlePath);

			switch (extension)
			{
				case ".css":
					tag = string.Format(cssIncludeTag, modifiedBundlePath);
					break;

				case ".js":
					tag = string.Format(jsIncludeTag, modifiedBundlePath);
					break;
			}

			return tag;
		}

		public StringBuilder Process(ViewCompilerDirectiveInfo directiveInfo)
		{
			if (directiveInfo.Directive == "Bundle")
			{
				StringBuilder fileLinkBuilder = new StringBuilder();

				string bundleName = directiveInfo.Value;

				if (!string.IsNullOrEmpty(bundleName))
				{
					if (debugMode)
					{
						foreach (string bundlePath in getBundleFiles(bundleName))
							fileLinkBuilder.AppendLine(ProcessBundleLink(bundlePath));
					}
					else
						fileLinkBuilder.AppendLine(ProcessBundleLink(bundleName));
				}

				directiveInfo.Content.Replace(directiveInfo.Match.Groups[0].Value, fileLinkBuilder.ToString());
			}

			return directiveInfo.Content;
		}
	}

	internal class PlaceHolderDirective : IViewCompilerDirectiveHandler
	{
		public DirectiveProcessType Type { get; private set; }

		public PlaceHolderDirective()
		{
			Type = DirectiveProcessType.AfterCompile;
		}

		public StringBuilder Process(ViewCompilerDirectiveInfo directiveInfo)
		{
			if (directiveInfo.Directive == "Placeholder")
			{
				Regex placeholderRE = new Regex(string.Format(@"\[{0}\](?<block>[\s\S]+?)\[/{0}\]", directiveInfo.Value));

				Match placeholderMatch = placeholderRE.Match(directiveInfo.Content.ToString());

				if (placeholderMatch.Success)
				{
					directiveInfo.Content.Replace(directiveInfo.Match.Groups[0].Value, placeholderMatch.Groups["block"].Value);
					directiveInfo.Content.Replace(placeholderMatch.Groups[0].Value, string.Empty);
				}
			}

			return directiveInfo.Content;
		}
	}

	internal class ViewCompiler : IViewCompiler
	{
		private List<IViewCompilerDirectiveHandler> directiveHandlers;
		private List<IViewCompilerSubstitutionHandler> substitutionHandlers;

		private static Markdown Markdown = new Markdown();
		private List<ViewTemplate> viewTemplates;
		private List<CompiledView> compiledViews;
		private Dictionary<string, List<string>> viewDependencies;
		private Dictionary<string, List<string>> templateKeyNames;

		private static string partitionControllerScopeActionKeyName = "{0}/{1}/{2}";
		private static string controllerScopeActionKeyName = "{0}/{1}";
		private static string partitionRootScopeSharedKeyName = "{0}/Views/Shared/{1}";
		private static string partitionControllerScopeSharedKeyName = "{0}/{1}/Shared/{2}";
		private static string controllerScopeSharedKeyName = "{0}/Shared/{1}";
		private static string globalScopeSharedKeyName = "Views/Shared/{0}";
		private static string partitionControllerScopeFragmentKeyName = "{0}/{1}/Fragments/{2}";
		private static string partitionRootScopeFragmentsKeyName = "{0}/Views/Fragments/{1}";
		private static string controllerScopeFragmentKeyName = "{0}/Fragments/{1}";
		private static string globalScopeFragmentKeyName = "Views/Fragments/{0}";

		private static Regex directiveTokenRE = new Regex(@"(\%\%(?<directive>[a-zA-Z0-9]+)=(?<value>(\S|\.)+)\%\%)", RegexOptions.Compiled);
		private static Regex tagRE = new Regex(@"{({|\||\!)([\w]+)(}|\!|\|)}", RegexOptions.Compiled);
		private static string tagFormatPattern = @"({{({{|\||\!){0}(\||\!|}})}})";
		private static string tagEncodingHint = "{|";
		private static string markdownEncodingHint = "{!";
		private static string unencodedTagHint = "{{";

		private StringBuilder directive = new StringBuilder();
		private StringBuilder value = new StringBuilder();

		public ViewCompiler(List<ViewTemplate> viewTemplates,
												List<CompiledView> compiledViews,
												Dictionary<string, List<string>> viewDependencies,
												List<IViewCompilerDirectiveHandler> directiveHandlers,
												List<IViewCompilerSubstitutionHandler> substitutionHandlers)
		{
			this.viewTemplates = viewTemplates;
			this.compiledViews = compiledViews;
			this.viewDependencies = viewDependencies;
			this.directiveHandlers = directiveHandlers;
			this.substitutionHandlers = substitutionHandlers;

			templateKeyNames = new Dictionary<string, List<string>>();
		}

		public List<CompiledView> CompileAll()
		{
			foreach (ViewTemplate vt in viewTemplates)
			{
				if (vt.TemplateType != ViewTemplateType.Fragment)
					Compile(vt.Partition, vt.Controller, vt.Name, vt.TemplateType);
				else
				{
					compiledViews.Add(new CompiledView()
					{
						FullName = vt.FullName,
						Name = vt.Name,
						CompiledTemplate = vt.Template,
						Template = vt.Template,
						Render = string.Empty,
						TemplateMD5sum = vt.MD5sum
					});
				}
			}

			if (compiledViews.Count > 0)
				return compiledViews;

			return null;
		}

		public CompiledView Compile(string partitionName, string controllerName, string viewName, ViewTemplateType viewType)
		{
			string keyName = DetermineKeyName(partitionName, controllerName, viewName, viewType);

			if (!string.IsNullOrEmpty(keyName))
			{
				ViewTemplate viewTemplate = viewTemplates.FirstOrDefault(x => x.FullName == keyName);

				if (viewTemplate != null)
				{
					StringBuilder rawView = new StringBuilder(viewTemplate.Template);
					StringBuilder compiledView = new StringBuilder();

					if (viewTemplate.TemplateType != ViewTemplateType.Fragment)
						compiledView = ProcessDirectives(keyName, partitionName, controllerName, viewType, rawView);

					if (string.IsNullOrEmpty(compiledView.ToString()))
						compiledView = rawView;

					compiledView.Replace(compiledView.ToString(), Regex.Replace(compiledView.ToString(), @"^\s*$\n", string.Empty, RegexOptions.Multiline));

					CompiledView view = new CompiledView()
					{
						FullName = keyName,
						Name = viewName,
						Template = viewTemplate.Template,
						CompiledTemplate = compiledView.ToString(),
						Render = string.Empty,
						TemplateMD5sum = viewTemplate.MD5sum
					};

					CompiledView previouslyCompiled = compiledViews.FirstOrDefault(x => x.FullName == viewTemplate.FullName);

					if (previouslyCompiled != null)
						compiledViews.Remove(previouslyCompiled);

					compiledViews.Add(view);

					return view;
				}
			}

			throw new FileNotFoundException(string.Format(CultureInfo.CurrentCulture, "Cannot find view : {0}", viewName));
		}

		public CompiledView Render(string fullName, Dictionary<string, string> tags)
		{
			CompiledView compiledView = compiledViews.FirstOrDefault(x => x.FullName == fullName);

			if (compiledView != null)
			{
				StringBuilder compiledViewSB = new StringBuilder(compiledView.CompiledTemplate);

				foreach (IViewCompilerSubstitutionHandler sub in substitutionHandlers.Where(x => x.Type == DirectiveProcessType.Render))
					compiledViewSB = sub.Process(compiledViewSB);

				if (tags != null)
				{
					StringBuilder tagSB = new StringBuilder();

					foreach (KeyValuePair<string, string> tag in tags)
					{
						tagSB.Length = 0;
						tagSB.Insert(0, string.Format(CultureInfo.InvariantCulture, tagFormatPattern, tag.Key));

						Regex tempTagRE = new Regex(tagSB.ToString());

						MatchCollection tagMatches = tempTagRE.Matches(compiledViewSB.ToString());

						if (tagMatches != null)
						{
							foreach (Match m in tagMatches)
							{
								if (!string.IsNullOrEmpty(tag.Value))
								{
									if (m.Value.StartsWith(unencodedTagHint, StringComparison.Ordinal))
										compiledViewSB.Replace(m.Value, tag.Value.Trim());
									else if (m.Value.StartsWith(tagEncodingHint, StringComparison.Ordinal))
										compiledViewSB.Replace(m.Value, HttpUtility.HtmlEncode(tag.Value.Trim()));
									else if (m.Value.StartsWith(markdownEncodingHint, StringComparison.Ordinal))
										compiledViewSB.Replace(m.Value, Markdown.Transform(tag.Value.Trim()));
								}
							}
						}
					}

					MatchCollection leftoverMatches = tagRE.Matches(compiledViewSB.ToString());

					if (leftoverMatches != null)
					{
						foreach (Match match in leftoverMatches)
							compiledViewSB.Replace(match.Value, string.Empty);
					}
				}

				compiledView.Render = compiledViewSB.ToString();

				return compiledView;
			}

			return null;
		}

		private StringBuilder ProcessDirectives(string fullViewName, string partitionName, string controllerName, ViewTemplateType viewType, StringBuilder rawView)
		{
			StringBuilder pageContent = new StringBuilder(rawView.ToString());

			if (!viewDependencies.ContainsKey(fullViewName))
				viewDependencies[fullViewName] = new List<string>();

			Func<string, string> determineKeyName = new Func<string, string>((x) =>
			{
				return DetermineKeyName(partitionName, controllerName, x, ViewTemplateType.Shared);
			});

			Action<string> addPageDependency = new Action<string>((x) =>
			{
				viewDependencies[fullViewName].Add(x);
			});

			Action<IEnumerable<IViewCompilerDirectiveHandler>> performCompilerPass =
				new Action<IEnumerable<IViewCompilerDirectiveHandler>>((x) =>
				{
					MatchCollection dirMatches = directiveTokenRE.Matches(pageContent.ToString());

					foreach (Match match in dirMatches)
					{
						directive.Length = 0;
						directive.Insert(0, match.Groups["directive"].Value);

						value.Length = 0;
						value.Insert(0, match.Groups["value"].Value);

						// process directive handlers
						foreach (IViewCompilerDirectiveHandler handler in x)
						{
							pageContent.Replace(pageContent.ToString(),
								handler.Process(new ViewCompilerDirectiveInfo()
								{
									Match = match,
									Directive = directive.ToString(),
									Value = value.ToString(),
									Content = pageContent,
									ViewTemplates = viewTemplates,
									DetermineKeyName = determineKeyName,
									AddPageDependency = addPageDependency
								}).ToString());
						}
					}
				});

			#region PROCESS DIRECTIVES (1ST PASS COMPILE)
			performCompilerPass(directiveHandlers.Where(x => x.Type == DirectiveProcessType.Compile));
			#endregion

			#region PROCESS SUBSTITUTIONS
			foreach (IViewCompilerSubstitutionHandler sub in substitutionHandlers.Where(x => x.Type == DirectiveProcessType.Compile))
				pageContent = sub.Process(pageContent);
			#endregion

			#region PROCESS DIRECTIVES (2ND PASS COMPILE)
			performCompilerPass(directiveHandlers.Where(x => x.Type == DirectiveProcessType.AfterCompile));
			#endregion

			return pageContent;
		}

		public string DetermineKeyName(string partitionName, string controllerName, string viewName, ViewTemplateType viewType)
		{
			List<string> keyTypes = new List<string>();
			string lookupKeyName = string.Empty;
			string viewDesignation = string.Empty;

			if (string.IsNullOrEmpty(partitionName))
				partitionName = "Views";

			if (string.IsNullOrEmpty(controllerName))
				controllerName = "Shared";

			if (viewType == ViewTemplateType.Shared)
				viewDesignation = "Shared/";
			else if (viewType == ViewTemplateType.Fragment)
				viewDesignation = "Fragments/";

			lookupKeyName = string.Format("{0}/{1}/{2}{3}", partitionName, controllerName, viewDesignation, viewName);

			if (!templateKeyNames.ContainsKey(lookupKeyName))
			{
				switch (viewType)
				{
					case ViewTemplateType.Action:
						keyTypes.Add(string.Format(partitionControllerScopeActionKeyName, partitionName, controllerName, viewName));
						keyTypes.Add(string.Format(controllerScopeActionKeyName, controllerName, viewName));
						break;

					case ViewTemplateType.Shared:
						keyTypes.Add(string.Format(partitionRootScopeSharedKeyName, partitionName, viewName));
						keyTypes.Add(string.Format(partitionControllerScopeSharedKeyName, partitionName, controllerName, viewName));
						keyTypes.Add(string.Format(controllerScopeSharedKeyName, controllerName, viewName));
						keyTypes.Add(string.Format(globalScopeSharedKeyName, viewName));
						break;

					case ViewTemplateType.Fragment:
						keyTypes.Add(string.Format(partitionControllerScopeFragmentKeyName, partitionName, controllerName, viewName));
						keyTypes.Add(string.Format(partitionRootScopeFragmentsKeyName, partitionName, viewName));
						keyTypes.Add(string.Format(controllerScopeFragmentKeyName, controllerName, viewName));
						keyTypes.Add(string.Format(globalScopeFragmentKeyName, viewName));
						break;
				}

				templateKeyNames[lookupKeyName] = keyTypes;
			}
			else
				keyTypes = templateKeyNames[lookupKeyName];

			return keyTypes.Intersect(viewTemplates.Select(x => x.FullName)).FirstOrDefault();
		}

		public void RecompileDependencies(string fullViewName, string partitionName, string controllerName)
		{
			var deps = viewDependencies.Where(x => x.Value.FirstOrDefault(y => y == fullViewName) != null);

			foreach (KeyValuePair<string, List<string>> view in deps)
			{
				var template = viewTemplates.FirstOrDefault(x => x.FullName == view.Key);

				if (template != null && template.TemplateType != ViewTemplateType.Fragment)
					Compile(partitionName, controllerName, template.Name, template.TemplateType);
			}
		}
	}

	public interface IViewEngine
	{
		string LoadView(string partitionName, string controllerName, string viewName, ViewTemplateType viewType, Dictionary<string, string> tags);
	}

	internal class ViewEngine : IViewEngine
	{
		private List<IViewCompilerDirectiveHandler> dirHandlers;
		private List<IViewCompilerSubstitutionHandler> substitutionHandlers;
		private List<ViewTemplate> viewTemplates;
		private List<CompiledView> compiledViews;
		private Dictionary<string, List<string>> viewDependencies;

		private ViewTemplateLoader viewTemplateLoader;
		private ViewCompiler viewCompiler;

		public ViewEngine(string _appRoot, string[] _viewRoots, List<IViewCompilerDirectiveHandler> dirHandlers, List<IViewCompilerSubstitutionHandler> substitutionHandlers)
		{
			string appRoot = _appRoot;
			string[] viewRoots = _viewRoots;

			this.dirHandlers = dirHandlers;
			this.substitutionHandlers = substitutionHandlers;

			viewTemplateLoader = new ViewTemplateLoader(appRoot, viewRoots);

			FileSystemWatcher watcher = new FileSystemWatcher(appRoot, "*.html");

			watcher.NotifyFilter = NotifyFilters.LastWrite;
			watcher.Changed += new FileSystemEventHandler(OnChanged);
			watcher.IncludeSubdirectories = true;
			watcher.EnableRaisingEvents = true;

			// We'd either new up these or look for a cached copy in the application store
			viewTemplates = new List<ViewTemplate>();
			compiledViews = new List<CompiledView>();
			viewDependencies = new Dictionary<string, List<string>>();

			if (!(viewRoots.Count() >= 1))
				throw new ArgumentException("At least one view root is required to load view templates from.");

			viewTemplates = viewTemplateLoader.Load();

			if (!(viewTemplates.Count() > 0))
				throw new Exception("Failed to load any view templates.");

			viewCompiler = new ViewCompiler(viewTemplates, compiledViews, viewDependencies, dirHandlers, substitutionHandlers);

			if (!(compiledViews.Count() > 0))
				compiledViews = viewCompiler.CompileAll();
		}

		private void OnChanged(object sender, FileSystemEventArgs e)
		{
			FileSystemWatcher fsw = sender as FileSystemWatcher;

			try
			{
				fsw.EnableRaisingEvents = false;

				while (GetExclusiveAccess(e.FullPath) == false)
					Thread.Sleep(1000);

				ViewTemplate changedTemplate = viewTemplateLoader.Load(e.FullPath);
				viewTemplates.Remove(viewTemplates.Find(x => x.FullName == changedTemplate.FullName));
				viewTemplates.Add(changedTemplate);

				CompiledView cv = compiledViews.FirstOrDefault(x => x.FullName == changedTemplate.FullName && x.TemplateMD5sum != changedTemplate.MD5sum);

				if (cv != null)
				{
					if (changedTemplate.TemplateType == ViewTemplateType.Fragment)
					{
						cv.TemplateMD5sum = changedTemplate.MD5sum;
						cv.Template = changedTemplate.Template;
						cv.CompiledTemplate = changedTemplate.Template;
						cv.Render = string.Empty;
					}
					else
					{
						viewCompiler = new ViewCompiler(viewTemplates, compiledViews, viewDependencies, dirHandlers, substitutionHandlers);
						viewCompiler.RecompileDependencies(changedTemplate.FullName, changedTemplate.Partition, changedTemplate.Controller);
						viewCompiler.Compile(changedTemplate.Partition, changedTemplate.Controller, changedTemplate.Name, changedTemplate.TemplateType);
					}
				}
			}
			finally
			{
				fsw.EnableRaisingEvents = true;
			}
		}

		// This method is borrowed from:
		// http://stackoverflow.com/a/8218033/170217
		private static bool GetExclusiveAccess(string filePath)
		{
			try
			{
				FileStream file = new FileStream(filePath, FileMode.Append, FileAccess.Write);
				file.Close();
				return true;
			}
			catch (IOException)
			{
				return false;
			}
		}

		public string LoadView(string partitionName, string controllerName, string viewName, ViewTemplateType viewType, Dictionary<string, string> tags)
		{
			string keyName = viewCompiler.DetermineKeyName(partitionName, controllerName, viewName, viewType);

			if (!string.IsNullOrEmpty(keyName))
			{
				CompiledView renderedView = viewCompiler.Render(keyName, tags);

				if (renderedView != null)
					return renderedView.Render;
			}

			return null;
		}
	}
	#endregion

	#region EXTENSION METHODS / ENCRYPTION / DYNAMIC DICTIONARY

	#region EXTENSION METHODS
	public static class ExtensionMethods
	{
		public static void ThrowIfArgumentNull<T>(this T t, string message = null)
		{
			string argName = t.GetType().Name;
			bool isNull = false;
			bool isEmptyString = false;

			if (t == null)
				isNull = true;
			else if (t is string)
			{
				if ((t as string) == string.Empty)
					isEmptyString = true;
			}

			if (isNull)
				throw new ArgumentNullException(argName, message);
			else if (isEmptyString)
				throw new ArgumentException(argName, message);
		}

		/// <summary>
		/// Calculates the MD5sum of a string
		/// <remarks>
		/// from http://blogs.msdn.com/b/csharpfaq/archive/2006/10/09/how-do-i-calculate-a-md5-hash-from-a-string_3f00_.aspx
		/// </remarks>
		/// </summary>
		/// <param name="input">The input string</param>
		/// <returns>An MD5sum</returns>
		public static string CalculateMD5sum(this string input)
		{
			MD5 md5 = System.Security.Cryptography.MD5.Create();
			byte[] inputBytes = System.Text.Encoding.UTF8.GetBytes(input);
			byte[] hash = md5.ComputeHash(inputBytes);

			StringBuilder sb = new StringBuilder();

			for (int i = 0; i < hash.Length; i++)
				sb.Append(hash[i].ToString("X2"));

			return sb.ToString();
		}

		public static object[] ToObjectArray(this string[] parms)
		{
			DateTime? dt = null;

			if (parms != null)
			{
				object[] _parms = new object[parms.Length];

				for (int i = 0; i < parms.Length; i++)
				{
					if (parms[i].IsInt32())
						_parms[i] = Convert.ToInt32(parms[i], CultureInfo.InvariantCulture);
					else if (parms[i].IsLong())
						_parms[i] = Convert.ToInt64(parms[i], CultureInfo.InvariantCulture);
					else if (parms[i].IsDouble())
						_parms[i] = Convert.ToDouble(parms[i], CultureInfo.InvariantCulture);
					else if (parms[i].ToLowerInvariant() == "true" ||
									parms[i].ToLowerInvariant() == "false" ||
									parms[i].ToLowerInvariant() == "on" || // HTML checkbox value
									parms[i].ToLowerInvariant() == "off" || // HTML checkbox value
									parms[i].ToLowerInvariant() == "checked") // HTML checkbox value
					{
						if (parms[i].ToLower() == "on" || parms[i].ToLower() == "checked")
							parms[i] = "true";
						else if (parms[i].ToLower() == "off")
							parms[i] = "false";

						_parms[i] = Convert.ToBoolean(parms[i]);
					}
					else if (parms[i].IsDate(out dt))
						_parms[i] = dt.Value;
					else
						_parms[i] = parms[i];
				}

				return _parms;
			}

			return null;
		}

		public static string NewLinesToBR(this string value)
		{
			return value.Trim().Replace("\n", "<br />");
		}

		public static string StripHtml(this string value)
		{
			HtmlDocument htmlDoc = new HtmlDocument();
			htmlDoc.LoadHtml(value);

			if (htmlDoc == null)
				return value;

			StringBuilder sanitizedString = new StringBuilder();

			foreach (var node in htmlDoc.DocumentNode.ChildNodes)
				sanitizedString.Append(node.InnerText);

			return sanitizedString.ToString();
		}

		public static string ToMarkdown(this string value)
		{
			return new Markdown().Transform(value);
		}

		public static string ToURLEncodedString(this string value)
		{
			return HttpUtility.UrlEncode(value);
		}

		public static string ToURLDecodedString(this string value)
		{
			return HttpUtility.UrlDecode(value);
		}

		public static string ToHtmlEncodedString(this string value)
		{
			return HttpUtility.HtmlEncode(value);
		}

		public static string ToHtmlDecodedString(this string value)
		{
			return HttpUtility.HtmlDecode(value);
		}

		public static bool InRange(this int value, int min, int max)
		{
			return value <= max && value >= min;
		}

		public static bool InRange(this long value, int min, int max)
		{
			return value <= max && value >= min;
		}

		public static bool IsDate(this string value, out DateTime? dt)
		{
			DateTime x;
			dt = null;

			if (DateTime.TryParse(value, out x))
			{
				dt = x;

				return true;
			}

			return false;
		}

		public static bool IsLong(this string value)
		{
			long x = 0;

			return long.TryParse(value, out x);
		}

		public static bool IsInt32(this string value)
		{
			int x = 0;

			return int.TryParse(value, out x);
		}

		public static bool IsInt64(this string value)
		{
			long x = 0;

			if (Int64.TryParse(value, out x))
				return true;

			return false;
		}

		public static bool IsDouble(this string value)
		{
			double x = 0;

			return double.TryParse(value, out x);
		}

		public static bool IsBool(this string value)
		{
			bool x = false;

			return bool.TryParse(value, out x);
		}

		// This code was adapted to work with FileInfo/DirectoryInfo but was originally from the following question on SO:
		//
		// http://stackoverflow.com/questions/929276/how-to-recursively-list-all-the-files-in-a-directory-in-c
		public static IEnumerable<FileInfo> GetAllFiles(this DirectoryInfo dirInfo)
		{
			string path = dirInfo.FullName;

			Queue<string> queue = new Queue<string>();
			queue.Enqueue(path);

			while (queue.Count > 0)
			{
				path = queue.Dequeue();

				try
				{
					foreach (string subDir in Directory.GetDirectories(path))
						queue.Enqueue(subDir);
				}
				catch (Exception ex)
				{
					Console.Error.WriteLine(ex);
				}

				FileInfo[] fileInfos = null;

				try
				{
					fileInfos = new DirectoryInfo(path).GetFiles();
				}
				catch
				{
					throw;
				}

				if (fileInfos != null)
				{
					for (int i = 0; i < fileInfos.Length; i++)
						yield return fileInfos[i];
				}
			}
		}
	}
	#endregion

	#region DYNAMIC DICTIONARY
	public class DynamicDictionary : DynamicObject
	{
		private Dictionary<string, object> _members = new Dictionary<string, object>();

		public bool IsEmpty()
		{
			if (_members.Keys.Count() > 0)
				return false;

			return true;
		}

		public override IEnumerable<string> GetDynamicMemberNames()
		{
			return _members.Keys;
		}

		public IEnumerable<string> GetDynamicMemberNames(string key)
		{
			if (_members.ContainsKey(key))
				if (_members[key] is DynamicDictionary)
					return (_members[key] as DynamicDictionary)._members.Keys;

			return null;
		}

		public Dictionary<string, object> GetDynamicDictionary()
		{
			return _members;
		}

		public Dictionary<string, object> GetDynamicDictionary(string key)
		{
			if (_members.ContainsKey(key))
				if (_members[key] is DynamicDictionary)
					return (_members[key] as DynamicDictionary)._members;

			return null;
		}

		public override bool TrySetMember(SetMemberBinder binder, object value)
		{
			if (!_members.ContainsKey(binder.Name))
				_members.Add(binder.Name, value);
			else
				_members[binder.Name] = value;

			return true;
		}

		public override bool TryGetMember(GetMemberBinder binder, out object result)
		{
			if (_members.ContainsKey(binder.Name))
				result = _members[binder.Name];
			else
				result = _members[binder.Name] = new DynamicDictionary();

			return true;
		}
	}
	#endregion

	#endregion
}