//
// Aurora - An MVC web framework for .NET
//
// Updated On: 17 September 2012
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
// - Actions can be segregated based on Get, Post, Put and 
//   Delete action type and you can secure them with the Secure named parameter.
//   Actions without a designation will not be invoked from a URL.
// - Actions can have filters with optional filter results that bind to action
//   parameters.  
// - Actions can have aliases. Aliases can also be added dynamically at runtime
//   along with default parameters.
// - Bundling/Minifying of Javascript and CSS.
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
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Dynamic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Web;
using AspNetAdapter;
using MarkdownSharp;
using Newtonsoft.Json;
using Yahoo.Yui.Compressor;

#if LIBRARY
using System.Runtime.InteropServices;

[assembly: AssemblyTitle("Aurora")]
[assembly: AssemblyDescription("An MVC web framework for .NET")]
[assembly: AssemblyCompany("Frank Hale")]
[assembly: AssemblyProduct("Aurora")]
[assembly: AssemblyCopyright("Copyright © 2011-2012 | LICENSE GNU GPLv3")]
[assembly: ComVisible(false)]
[assembly: CLSCompliant(true)]
[assembly: AssemblyVersion("2.0.14.0")]
#endif

namespace Aurora
{
	#region ATTRIBUTES
	public enum ActionSecurity
	{
		Secure,
		None
	}

	#region HTTP REQUEST
	public enum ActionType
	{
		Get,
		Post,
		Put,
		Delete,
		FromRedirectOnly
	}

	public class HttpAttribute : Attribute
	{
		public bool RequireAntiForgeryToken { get; set; }
		public string RedirectWithoutAuthorizationTo { get; set; }
		public ActionSecurity SecurityType { get; set; }
		public string RouteAlias { get; set; }
		public string Roles { get; set; }
		public bool HttpsOnly { get; set; }
		public ActionType ActionType { get; private set; }

		public HttpAttribute(ActionType actionType) : this(actionType, string.Empty, ActionSecurity.None) { }

		public HttpAttribute(ActionType actionType, string alias) : this(actionType, alias, ActionSecurity.None) { }

		public HttpAttribute(ActionType actionType, string alias, ActionSecurity actionSecurity)
		{
			SecurityType = actionSecurity;
			RouteAlias = alias;
			ActionType = actionType;
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
	[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
	public abstract class ModelValidationBaseAttribute : Attribute
	{
		public string ErrorMessage { get; set; }

		protected ModelValidationBaseAttribute(string errorMessage)
		{
			ErrorMessage = errorMessage;
		}
	}

	public sealed class RequiredAttribute : ModelValidationBaseAttribute
	{
		public RequiredAttribute(string errorMessage) : base(errorMessage) { }
	}

	public sealed class RequiredLengthAttribute : ModelValidationBaseAttribute
	{
		public int Length { get; private set; }

		public RequiredLengthAttribute(int length, string errorMessage)
			: base(errorMessage)
		{
			Length = length;
		}
	}

	public sealed class RegularExpressionAttribute : ModelValidationBaseAttribute
	{
		public Regex Pattern { get; set; }

		public RegularExpressionAttribute(string pattern, string errorMessage)
			: base(errorMessage)
		{
			Pattern = new Regex(pattern);
		}
	}

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
		private static string viewCacheFilePathSessionName = "__ViewCacheFilePath";
		#endregion

		#region ASP.NET ADAPTER STUFF
		private Dictionary<string, object> app;
		internal Dictionary<string, object> request;
    private Dictionary<string, string> queryString, cookies, form, payload;
		private Action<Dictionary<string, object>> response;
		private List<PostedFile> files;
		private Exception serverError;
		internal X509Certificate2 clientCertificate { get; private set; }
    private string IPAddress, path, requestType, appRoot, viewRoot, sessionID;
		private bool fromRedirectOnly;
		#endregion

		#region MISCELLANEOUS VARIABLES
		private static Regex allowedFilePattern = new Regex(@"\.(js|css|png|jpg|gif|ico|pptx|xlsx|csv)$", RegexOptions.Compiled);
		private static string sharedResourceFolderPath = "/Resources";
		private static string compiledViewsCacheFolderPath = "/Views/Cache";
		private static string compiledViewsCacheFileName = "viewsCache.json";
		private static string antiForgeryTokenName = "AntiForgeryToken";
		private bool debugMode;
		internal ViewEngine ViewEngine;
		private FrontController frontController;
		private List<Controller> controllers;
		internal List<RouteInfo> routeInfos;
		private List<string> antiForgeryTokens;
		private List<User> users;
		private List<Type> models;
		private Dictionary<string, Dictionary<string, List<object>>> actionBindings;
		private Dictionary<string, Tuple<List<string>, string>> bundles;
		private Dictionary<string, string> protectedFiles;
		private Dictionary<string, object> controllersSession;
		internal User currentUser;
		string cachePath;
		string cacheFilePath;
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
			debugMode = Convert.ToBoolean(app[HttpAdapterConstants.DebugMode]);
			serverError = app[HttpAdapterConstants.ServerError] as Exception;
			int httpStatus = 200;
			ViewResponse viewResponse = null;
			clientCertificate = request[HttpAdapterConstants.RequestClientCertificate] as X509Certificate2;
			#endregion

			#region GET OBJECTS FROM APPLICATION SESSION STORE
			ViewEngine = GetApplication(viewEngineSessionName) as ViewEngine;
			controllers = GetSession(controllerInstancesSessionName) as List<Controller>;
			frontController = GetSession(frontControllerInstanceSessionName) as FrontController;
			routeInfos = GetSession(routeInfosSessionName) as List<RouteInfo>;
			actionBindings = GetSession(actionBindingsSessionName) as Dictionary<string, Dictionary<string, List<object>>>;
			users = GetApplication(usersSessionName) as List<User>;
			antiForgeryTokens = GetApplication(antiForgeryTokensSessionName) as List<string>;
			bundles = GetSession(bundlesTokenSessionName) as Dictionary<string, Tuple<List<string>, string>>;
			models = GetApplication(modelsSessionName) as List<Type>;
			fromRedirectOnly = Convert.ToBoolean(GetSession(fromRedirectOnlySessionName));
			protectedFiles = GetApplication(protectedFilesSessionName) as Dictionary<string, string>;
			controllersSession = GetApplication(controllersSessionSessionName) as Dictionary<string, object>;
			cacheFilePath = GetApplication(viewCacheFilePathSessionName) as string;
			#endregion

			#region INITIALIZE MISCELLANEOUS
			cachePath = MapPath(compiledViewsCacheFolderPath);

			if (routeInfos == null)
				routeInfos = new List<RouteInfo>();
			#endregion

			#region INITIALIZE USERS
			if (users == null)
			{
				users = new List<User>();
				AddApplication(usersSessionName, users);
			}
			else
				currentUser = users.FirstOrDefault(x => x.SessionId == sessionID);
			#endregion

			#region INITIALIZE ANTIFORGERYTOKENS
			if (antiForgeryTokens == null)
			{
				antiForgeryTokens = new List<string>();
				AddApplication(antiForgeryTokensSessionName, antiForgeryTokens);
			}
			#endregion

			#region INITIALIZE MODELS
			if (models == null)
			{
				models = GetTypeList(typeof(Model));
				AddApplication(modelsSessionName, models);
			}
			#endregion

			#region INITIALIZE CONTROLLERS SESSION
			if (controllersSession == null)
			{
				controllersSession = new Dictionary<string, object>();
				AddApplication(controllersSessionSessionName, controllersSession);
			}
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
				AddSession(actionBindingsSessionName, actionBindings);
			}
			#endregion

			#region INITIALIZE BUNDLES
			if (bundles == null)
			{
				bundles = new Dictionary<string, Tuple<List<string>, string>>();
				AddSession(bundlesTokenSessionName, bundles);
			}
			#endregion

			#region INTIALIZE FRONT CONTROLLER
			if (frontController == null)
			{
				frontController = GetFrontControllerInstance();
				AddSession(frontControllerInstanceSessionName, frontController);
			}
			else
				frontController.Refresh(this);
			#endregion

			#region INITIALIZE CONTROLLER INSTANCES
			if (controllers == null)
			{
				controllers = GetControllerInstances();
				AddSession(controllerInstancesSessionName, controllers);
			}
			else
				controllers.ForEach(c => c.Refresh(this));
			#endregion

			#region RUN ALL CONTROLLER ONINIT METHODS
			if (frontController != null)
				frontController.RaiseEvent(EventType.OnInit);

			controllers.ForEach(x => x.RaiseEvent(EventType.OnInit));
			#endregion

			#region INITIALIZE ROUTEINFOS
			if (GetSession(routeInfosSessionName) == null)
			{
				routeInfos.AddRange(GetRouteInfos());
				AddSession(routeInfosSessionName, routeInfos);
			}
			#endregion

			#region INITIALIZE VIEW ENGINE

			//FIXME: If the ViewEngine is not null and there is no cache force the view engine to reload everything!

			if (ViewEngine == null)
			{
				List<IViewCompilerDirectiveHandler> dirHandlers = new List<IViewCompilerDirectiveHandler>();
				List<IViewCompilerSubstitutionHandler> substitutionHandlers = new List<IViewCompilerSubstitutionHandler>();

				dirHandlers.Add(new MasterPageDirective());
				dirHandlers.Add(new PlaceHolderDirective());
				dirHandlers.Add(new PartialPageDirective());
				dirHandlers.Add(new BundleDirective(debugMode, sharedResourceFolderPath, GetBundleFiles));
				substitutionHandlers.Add(new AntiForgeryTokenSubstitution(CreateAntiForgeryToken));
				substitutionHandlers.Add(new HeadSubstitution());

				if (string.IsNullOrEmpty(cacheFilePath))
				{
					cacheFilePath = string.Join("/", cachePath, compiledViewsCacheFileName);
					AddApplication(viewCacheFilePathSessionName, cacheFilePath);
				}

				string viewCache = null;

				if (!Directory.Exists(cachePath))
				{
					try { Directory.CreateDirectory(cachePath); }
					catch { /* Silently ignore failure */ }
				}
				else if (File.Exists(cacheFilePath))
					viewCache = File.ReadAllText(cacheFilePath);

				ViewEngine = new ViewEngine(appRoot, GetViewRoots(), dirHandlers, substitutionHandlers, viewCache);

				if (string.IsNullOrEmpty(viewCache))
					UpdateCache();

				AddApplication(viewEngineSessionName, ViewEngine);
			}
			else if (ViewEngine.UpdateCache || !Directory.Exists(cachePath) || !File.Exists(cacheFilePath))
				UpdateCache();
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
				viewResponse = GetErrorViewResponse((serverError.InnerException != null) ? serverError.InnerException.Message : serverError.Message, app[HttpAdapterConstants.ServerErrorStackTrace].ToString());
			}

			RenderResponse(viewResponse, httpStatus);
			#endregion
		}

		#region PRIVATE METHODS
		private ViewResponse ProcessRequest()
		{
			RouteInfo routeInfo = null;
			IViewResult viewResult = null;
			ViewResponse viewResponse = null;

			if (path == "/" || path.ToLower() == "/default.aspx" || path == "~/") path = "/Index";

			if (allowedFilePattern.IsMatch(path))
			{
				#region FILE RESPONSE
				RaiseEventOnFrontController(RouteHandlerEventType.Static, path, null, null);

				if (path.StartsWith(sharedResourceFolderPath) || path.EndsWith(".ico"))
				{
					string filePath = MapPath(path);

					if (CanAccessFile(filePath))
					{
						if (File.Exists(filePath))
							viewResponse = new FileResult(allowedFilePattern, filePath).Render();
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
					if (routeInfo.RequestTypeAttribute.ActionType == ActionType.FromRedirectOnly && !fromRedirectOnly)
						return null;

					if (routeInfo.RequestTypeAttribute.ActionType == ActionType.Post ||
							routeInfo.RequestTypeAttribute.ActionType == ActionType.Put ||
							routeInfo.RequestTypeAttribute.ActionType == ActionType.Delete)
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

					if (routeInfo.RequestTypeAttribute.SecurityType == ActionSecurity.Secure &&
							routeInfo.RequestTypeAttribute.SecurityType == ActionSecurity.Secure && currentUser == null ||
							routeInfo.RequestTypeAttribute.SecurityType == ActionSecurity.Secure && routeInfo.RequestTypeAttribute.Roles == null ||
							routeInfo.RequestTypeAttribute.SecurityType == ActionSecurity.Secure && !(currentUser.Roles.Intersect(routeInfo.RequestTypeAttribute.Roles.Split('|')).Count() > 0) ||
							routeInfo.RequestTypeAttribute.SecurityType == ActionSecurity.Secure && routeInfo.Controller.RaiseCheckRoles(new CheckRolesHandlerEventArgs() { RouteInfo = routeInfo }))
					{
						RaiseEventOnFrontController(RouteHandlerEventType.FailedSecurity, path, routeInfo, null);

						if (!string.IsNullOrEmpty(routeInfo.RequestTypeAttribute.RedirectWithoutAuthorizationTo))
							viewResponse = new ViewResponse() { RedirectTo = routeInfo.RequestTypeAttribute.RedirectWithoutAuthorizationTo };
					}
					else
					{
						RaiseEventOnFrontController(RouteHandlerEventType.PassedSecurity, path, routeInfo, null);
						RaiseEventOnFrontController(RouteHandlerEventType.Pre, path, routeInfo, null);
						routeInfo.Controller.RaiseEvent(RouteHandlerEventType.Pre, path, routeInfo);

						if (routeInfo.RequestTypeAttribute.ActionType == ActionType.FromRedirectOnly && fromRedirectOnly)
							RemoveSession(fromRedirectOnlySessionName);

						if (routeInfo.IBoundToActionParams != null)
							foreach (IBoundToAction bta in routeInfo.IBoundToActionParams)
								bta.Initialize(routeInfo.Controller);

						if (routeInfo.ActionParamTransforms != null)
						{
							foreach (var apt in routeInfo.ActionParamTransforms)
							{
								Tuple<MethodInfo, object> transformMethod = routeInfo.CachedActionParamTransformInstances[apt.Item1.TransformName] as Tuple<MethodInfo, object>;

								routeInfo.ActionParams[apt.Item2] = transformMethod.Item1.Invoke(transformMethod.Item2, new object[] { routeInfo.ActionParams[apt.Item2] });
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

						viewResult = (IViewResult)routeInfo.Action.Invoke(routeInfo.Controller, routeInfo.ActionParams);

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

		private void UpdateCache()
		{
			try
			{
				if (Directory.Exists(Path.GetDirectoryName(cacheFilePath)))
					using (StreamWriter cacheWriter = new StreamWriter(cacheFilePath))
						cacheWriter.Write(ViewEngine.GetCache());
			}
			catch { /* Silently ignore any write failures */ }
		}

		private ViewResponse GetErrorViewResponse(string error, string stackTrace)
		{
			if (!string.IsNullOrEmpty(stackTrace))
				stackTrace = string.Format("<p><pre>{0}</pre></p>", stackTrace);

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
			{
				RouteInfo redirectRoute = FindRoute(viewResponse.RedirectTo);

				if (redirectRoute != null && redirectRoute.RequestTypeAttribute.ActionType == ActionType.FromRedirectOnly)
					ResponseRedirect(viewResponse.RedirectTo, true);
				else
					ResponseRedirect(viewResponse.RedirectTo, false);
			}
		}

		private string[] GetViewRoots()
		{
			List<string> viewRoots = new List<string>() { viewRoot };

			viewRoots.AddRange(controllers.Where(x => !string.IsNullOrEmpty(x.PartitionName)).Select(x => string.Format(@"{0}\{1}", appRoot, x.PartitionName)));

			return viewRoots.ToArray();
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

					if (p.PropertyType == typeof(int) || p.PropertyType == typeof(int?))
					{
						if (propertyValue.IsInt32())
							p.SetValue(result, Convert.ToInt32(propertyValue), null);
					}
					else if (p.PropertyType == typeof(string))
					{
						p.SetValue(result, propertyValue, null);
					}
					else if (p.PropertyType == typeof(bool))
					{
						if (propertyValue.IsBool())
							p.SetValue(result, Convert.ToBoolean(propertyValue), null);
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
						p.SetValue(result, files, null);
				}

				(result as Model).Validate(payload);
			}

			return result;
		}

		private List<Type> GetTypeList(Type t)
		{
			t.ThrowIfArgumentNull();

			return AppDomain.CurrentDomain.GetAssemblies()
                           // DotNetOpenAuth depends on System.Web.Mvc which is not referenced, this will fail if we don't eliminate it
													 .Where(x => x.GetName().Name != "DotNetOpenAuth") 
													 .SelectMany(x => x.GetTypes().Where(y => y.BaseType == t)).ToList();
		}

		private List<string> GetControllerActionNames(string controllerName)
		{
			controllerName.ThrowIfArgumentNull();

			return controllers.FirstOrDefault(x => x.GetType().Name == controllerName)
												.GetType()
												.GetMethods()
												.Where(x => x.GetCustomAttributes(typeof(HttpAttribute), false).Count() > 0)
												.Select(x => x.Name)
												.ToList();
		}

		private List<Controller> GetControllerInstances()
		{
			List<Controller> instances = new List<Controller>();

      GetTypeList(typeof(Controller)).ForEach(x => instances.Add(Controller.CreateInstance(x, this)));

			return instances;
		}

		private FrontController GetFrontControllerInstance()
		{
      FrontController fc = null;

      GetTypeList(typeof(FrontController)).ForEach(x => { fc = FrontController.CreateInstance(x, this); return; });

      return fc;
		}

		private List<RouteInfo> GetRouteInfos()
		{
			List<RouteInfo> routeInfos = new List<RouteInfo>();

			foreach (Controller c in controllers)
			{
				var actions = c.GetType().GetMethods()
					.Where(x => x.GetCustomAttributes(typeof(HttpAttribute), false).FirstOrDefault() != null)
					.Select(x =>
						new
						{
							Method = x,
							Attribute = (HttpAttribute)x.GetCustomAttributes(typeof(HttpAttribute), false).FirstOrDefault(),
							Aliases = x.GetCustomAttributes(typeof(AliasAttribute), false).Select(a => (a as AliasAttribute).Alias).ToList()
						});

				foreach (var action in actions)
				{
					if (string.IsNullOrEmpty(action.Attribute.RouteAlias))
						action.Aliases.Add(string.Format("/{0}/{1}", c.GetType().Name, action.Method.Name));
					else
						action.Aliases.Add(action.Attribute.RouteAlias);

					AddRoute(routeInfos, c, action.Method, action.Aliases, null, false);
				}
			}

			return routeInfos;
		}

		private ActionParameterInfo GetActionParameterTransforms(ParameterInfo[] actionParams, List<object> bindings)
		{
			ActionParameterInfo actionParameterInfo = new ActionParameterInfo();
			Dictionary<string, object> cachedActionParamTransformInstances = new Dictionary<string, object>();

			List<Tuple<ActionParameterTransformAttribute, int>> actionParameterTransforms = actionParams
					.Select((x, i) => new Tuple<ActionParameterTransformAttribute, int>((ActionParameterTransformAttribute)x.GetCustomAttributes(typeof(ActionParameterTransformAttribute), false).FirstOrDefault(), i))
					.Where(x => x.Item1 != null).ToList();

			foreach (var apt in actionParameterTransforms)
			{
				var actionTransformClassType = AppDomain.CurrentDomain.GetAssemblies().Where(x => x.GetName().Name != "DotNetOpenAuth") // DotNetOpenAuth depends on System.Web.Mvc which is not referenced, this will fail if we don't eliminate it
														.SelectMany(x => x.GetTypes().Where(y => y.GetInterface(typeof(IActionParamTransform<,>).Name) != null && y.Name == apt.Item1.TransformName))
														.FirstOrDefault();

				if (actionTransformClassType != null)
				{
					object instance = Activator.CreateInstance(actionTransformClassType, (bindings != null) ? bindings.ToArray() : null);
					MethodInfo transformMethod = actionTransformClassType.GetMethod("Transform");

					cachedActionParamTransformInstances[apt.Item1.TransformName] = new Tuple<MethodInfo, object>(transformMethod, instance);
				}
			}

			actionParameterInfo.ActionParamTransforms = actionParameterTransforms.Count() > 0 ? actionParameterTransforms : null;
			actionParameterInfo.ActionParamTransformInstances = cachedActionParamTransformInstances.Count() > 0 ? cachedActionParamTransformInstances : null;

			return actionParameterInfo;
		}

		private IActionFilterResult[] ProcessAnyActionFilters(RouteInfo routeInfo)
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
		#endregion

		#region INTERNAL METHODS
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
      name.ThrowIfArgumentNull();
      paths.ThrowIfArgumentNull();

			string compressedFile = null;
			StringBuilder combinedFiles = new StringBuilder();

			paths = paths.Where(x => File.Exists(appRoot + x.Replace('/', '\\')) &&
																											 Path.GetExtension(x) == ".css" ||
																											 Path.GetExtension(x) == ".js").ToArray();

			foreach (string p in paths)
				combinedFiles.AppendLine(File.ReadAllText(appRoot + p));

			if (Path.GetExtension(name) == ".js")
				compressedFile = new JavaScriptCompressor().Compress(combinedFiles.ToString());
			else if (Path.GetExtension(name) == ".css")
				compressedFile = new CssCompressor().Compress(combinedFiles.ToString());

			bundles[name] = new Tuple<List<string>, string>(paths.ToList(), compressedFile);
		}

		internal string[] GetBundleFiles(string name)
		{
			return (bundles.ContainsKey(name)) ? bundles[name].Item1.ToArray() : null;
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
			var routeSlice = routeInfos.SelectMany(routeInfo =>
					routeInfo.Aliases, (routeInfo, alias) =>
						new { routeInfo, alias }).OrderByDescending(x => x.alias.Length).Where(x => path.StartsWith(x.alias)).ToList();

			if (routeSlice.Count() > 0)
			{
				List<object> allParams = new List<object>()
					.Concat(routeSlice[0].routeInfo.BoundParams)
					.Concat(path.Replace(routeSlice[0].alias, string.Empty).Split('/').Where(x => !string.IsNullOrEmpty(x)).Select(x => HttpUtility.UrlEncode(x)).ToArray())
					.Concat(routeSlice[0].routeInfo.DefaultParams)
					.ToList();

				Func<Dictionary<string, string>, object[]> getModelOrParams =
					_payload =>
					{
						object model = PayloadToModel(_payload);
						object[] payloadParams = (model != null) ? new object[] { model } : _payload.Values.Where(x => !antiForgeryTokens.Contains(x)).ToArray();
						return payloadParams;
					};

				if (requestType == "POST")
					allParams.AddRange(getModelOrParams(form));
				else if (requestType == "PUT" || requestType == "DELETE")
					allParams.AddRange(getModelOrParams(payload));

				object[] finalParams = allParams.ToArray();

				foreach (RouteInfo routeInfo in routeSlice
					.Where(x => x.routeInfo.Action.GetParameters().Count() >= finalParams.Count()).Select(x => x.routeInfo))
				{
					Type[] finalParamTypes = finalParams.Select(x => x.GetType()).ToArray();
					Type[] actionParamTypes = routeInfo.Action.GetParameters()
																										.Where(x => x.ParameterType.GetInterface("IActionFilterResult") == null)
																										.Select(x => x.ParameterType).ToArray();

					if (routeInfo.ActionParamTransforms != null && finalParamTypes.Count() == actionParamTypes.Count())
						foreach (var apt in routeInfo.ActionParamTransforms)
							finalParamTypes[apt.Item2] = actionParamTypes[apt.Item2];

					for (int i = 0; i < routeSlice[0].routeInfo.BoundParams.Count(); i++)
						if (actionParamTypes[i].IsInterface && finalParamTypes[i].GetInterface(actionParamTypes[i].Name) != null)
							finalParamTypes[i] = actionParamTypes[i];

					if (finalParamTypes.Intersect(actionParamTypes).Count() < finalParamTypes.Count())
					{
						for (int i = 0; i < finalParamTypes.Count(); i++)
						{
							if (finalParamTypes[i] != actionParamTypes[i])
							{
								try
								{
									finalParams[i] = Convert.ChangeType(finalParams[i], actionParamTypes[i]);
									finalParamTypes[i] = actionParamTypes[i];
								}
								catch { }
							}
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

		internal void RemoveRoute(string alias)
		{
			RouteInfo routeInfo = routeInfos.FirstOrDefault(x => x.Aliases.FirstOrDefault(a => a == alias) != null);

			if (routeInfo.Dynamic)
				routeInfos.Remove(routeInfo);
		}

		internal void AddRoute(List<RouteInfo> routeInfos, Controller c, MethodInfo action, List<string> aliases, string defaultParams, bool dynamic)
		{
			if (action != null)
			{
				List<object> bindings = null;
				ActionParameterInfo actionParameterInfo = null;
				Dictionary<string, object> cachedActionParamTransformInstances = new Dictionary<string, object>();
				HttpAttribute rta = (HttpAttribute)action.GetCustomAttributes(typeof(HttpAttribute), false).FirstOrDefault();

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
					DefaultParams = (!string.IsNullOrEmpty(defaultParams)) ? defaultParams.Split('/').ConvertToObjectTypeArray() : new object[] { },
					ActionParamTransforms = actionParameterInfo.ActionParamTransforms,
					CachedActionParamTransformInstances = actionParameterInfo.ActionParamTransformInstances,
					Dynamic = dynamic
				});
			}
		}

		internal void AddRoute(List<RouteInfo> routeInfos, string alias, string controllerName, string actionName, string defaultParams, bool dynamic)
		{
			alias.ThrowIfArgumentNull();
			controllerName.ThrowIfArgumentNull();
			actionName.ThrowIfArgumentNull();

			Controller c = controllers.FirstOrDefault(x => x.GetType().Name == controllerName);
      
			if (c != null)
			{
				MethodInfo action = c.GetType().GetMethods().FirstOrDefault(x => x.GetCustomAttributes(typeof(HttpAttribute), false).Count() > 0 && x.Name == actionName);
				AddRoute(routeInfos, c, action, new List<string> { alias }, defaultParams, dynamic);
			}
		}

		internal List<string> GetAllRouteAliases()
		{
			return routeInfos.SelectMany(x => x.Aliases).ToList();
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

			if (currentUser != null && currentUser.SessionId == sessionID)
				return;

			User alreadyLoggedInWithDiffSession = users.FirstOrDefault(x => x.Name == id);

			if (alreadyLoggedInWithDiffSession != null)
				users.Remove(alreadyLoggedInWithDiffSession);

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
				ClientCertificate = clientCertificate,
				IPAddress = IPAddress,
				LogOnDate = DateTime.Now,
				Name = id,
				Roles = roles.ToList()
			};

			users.Add(u);

			AddSession("CURRENT_USER", u);

			currentUser = u;
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
			return (controllersSession.ContainsKey(key)) ? controllersSession[key] : null;
		}

		internal string MapPath(string path)
		{
			return appRoot + path.Replace('/', '\\');
		}
		#endregion
		#endregion

		#region ASP.NET ADAPTER CALLBACKS
		public object GetApplication(string key)
		{
			if (app.ContainsKey(HttpAdapterConstants.ApplicationSessionStoreGetCallback) &&
					app[HttpAdapterConstants.ApplicationSessionStoreGetCallback] is Func<string, object>)
				return (app[HttpAdapterConstants.ApplicationSessionStoreGetCallback] as Func<string, object>)(key);

			return null;
		}

		public void AddApplication(string key, object value)
		{
			if (app.ContainsKey(HttpAdapterConstants.ApplicationSessionStoreAddCallback) &&
					app[HttpAdapterConstants.ApplicationSessionStoreAddCallback] is Action<string, object>)
				(app[HttpAdapterConstants.ApplicationSessionStoreAddCallback] as Action<string, object>)(key, value);
		}

		public object GetSession(string key)
		{
			if (app.ContainsKey(HttpAdapterConstants.UserSessionStoreGetCallback) &&
					app[HttpAdapterConstants.UserSessionStoreGetCallback] is Func<string, object>)
				return (app[HttpAdapterConstants.UserSessionStoreGetCallback] as Func<string, object>)(key);

			return null;
		}

		public void AddSession(string key, object value)
		{
			if (app.ContainsKey(HttpAdapterConstants.UserSessionStoreAddCallback) &&
					app[HttpAdapterConstants.UserSessionStoreAddCallback] is Action<string, object>)
				(app[HttpAdapterConstants.UserSessionStoreAddCallback] as Action<string, object>)(key, value);
		}

		public void RemoveSession(string key)
		{
			if (app.ContainsKey(HttpAdapterConstants.UserSessionStoreRemoveCallback) &&
					app[HttpAdapterConstants.UserSessionStoreRemoveCallback] is Action<string>)
				(app[HttpAdapterConstants.UserSessionStoreRemoveCallback] as Action<string>)(key);
		}

		public void AbandonSession()
		{
			if (app.ContainsKey(HttpAdapterConstants.UserSessionStoreAbandonCallback) &&
					app[HttpAdapterConstants.UserSessionStoreAbandonCallback] is Action)
				(app[HttpAdapterConstants.UserSessionStoreAbandonCallback] as Action)();
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
				return (request[HttpAdapterConstants.RequestFormCallback] as Func<string, bool, string>)(key, true);

			return null;
		}

    public string GetQueryString(string key, bool validated)
    {
      string result = null;

      if (!validated)
      {
        if (request.ContainsKey(HttpAdapterConstants.RequestQueryString) &&
            request[HttpAdapterConstants.RequestQueryString] is Dictionary<string, string>)
          (request[HttpAdapterConstants.RequestQueryString] as Dictionary<string, string>).TryGetValue(key, out result);
      }
      else
      {
        if (request.ContainsKey(HttpAdapterConstants.RequestQueryStringCallback) &&
          request[HttpAdapterConstants.RequestQueryStringCallback] is Func<string, bool, string>)
          result = (request[HttpAdapterConstants.RequestQueryStringCallback] as Func<string, bool, string>)(key, true);
      }

      return result;
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
		public HttpAttribute RequestTypeAttribute { get; internal set; }
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
		public User CurrentUser { get { return engine.currentUser; } }
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

		public void LogOn(string id, string[] roles)
		{
			engine.LogOn(id, roles);
		}

		public void LogOff()
		{
			engine.LogOff();
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
					result = (sValue.Length >= requiredLengthAttribute.Length) ? true : false;
			}

			if (!result)
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
					result = (!string.IsNullOrEmpty(value as string)) ? true : false;
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
					result = (regularExpressionAttribute.Pattern.IsMatch(sValue)) ? true : false;
				else
					result = false;
			}

			if (!result)
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
					result = (((Int64)value).InRange(rangeAttribute.Min, rangeAttribute.Max)) ? true : false;
				else
					result = false;
			}

			if (!result)
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

			IsValid = (finalResult.Count() > 0) ? false : true;
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
	#region EVENT ASSETS
	internal enum EventType
	{
		OnInit,
		CheckRoles
	}

	internal enum RouteHandlerEventType
	{
		Pre,
		Post,
		PreRoute,
		PostRoute,
		Static,
		PassedSecurity,
		FailedSecurity,
		MissingRoute,
		Error
	}

	public class CheckRolesHandlerEventArgs : EventArgs
	{
		public bool Result { get; set; }
		public RouteInfo RouteInfo { get; set; }
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

		public Dictionary<string, object> Request { get { return engine.request.ToDictionary(x => x.Key, x => x.Value); } }
		public User CurrentUser { get { return engine.currentUser; } }
		public X509Certificate2 ClientCertificate { get { return engine.clientCertificate; } }

		protected event EventHandler OnInit;
		protected event EventHandler<CheckRolesHandlerEventArgs> OnCheckRoles;

		internal bool RaiseCheckRoles(CheckRolesHandlerEventArgs checkRolesHandlerEventArgs)
		{
			if (OnCheckRoles != null)
			{
				OnCheckRoles(this, checkRolesHandlerEventArgs);

				return checkRolesHandlerEventArgs.Result;
			}

			return true;
		}

		internal void RaiseEvent(EventType eventType)
		{
			if (eventType == EventType.OnInit && OnInit != null)
			{
				OnInit(this, null);
				OnInit = null;
			}
		}

		internal virtual void Refresh(Engine engine)
		{
			this.engine = engine;
		}

		protected RouteInfo FindRoute(string path)
		{
			return engine.FindRoute(path);
		}

		protected void AddRoute(string alias, string controllerName, string actionName, string defaultParams)
		{
			engine.AddRoute(engine.routeInfos, alias, controllerName, actionName, defaultParams, true);
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

		protected void AddApplication(string key, object value)
		{
			engine.AddApplication(key, value);
		}

		protected object GetApplication(string key)
		{
			return engine.GetApplication(key);
		}

		protected void AddSession(string key, object value)
		{
			engine.AddControllerSession(key, value);
		}

		protected object GetSession(string key)
		{
			return engine.GetControllerSession(key);
		}

		protected string GetQueryString(string key, bool validate)
		{
      return engine.GetQueryString(key, validate);
		}

		protected string MapPath(string path)
		{
			return engine.MapPath(path);
		}
	}

	public abstract class FrontController : BaseController
	{
		protected event EventHandler<RouteHandlerEventArgs> OnPreActionEvent,
			OnPostActionEvent, OnStaticRouteEvent, OnPreRouteDeterminationEvent, OnPostRouteDeterminationEvent,
			OnPassedSecurityEvent, OnFailedSecurityEvent, OnMissingRouteEvent, OnErrorEvent;

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
					if (OnPreActionEvent != null)
						OnPreActionEvent(this, args);
					break;

				case RouteHandlerEventType.Post:
					if (OnPostActionEvent != null)
						OnPostActionEvent(this, args);
					break;

				case RouteHandlerEventType.PreRoute:
					if (OnPreRouteDeterminationEvent != null)
						OnPreRouteDeterminationEvent(this, args);
					break;

				case RouteHandlerEventType.PostRoute:
					if (OnPostRouteDeterminationEvent != null)
						OnPostRouteDeterminationEvent(this, args);
					break;

				case RouteHandlerEventType.Static:
					if (OnStaticRouteEvent != null)
						OnStaticRouteEvent(this, args);
					break;

				case RouteHandlerEventType.PassedSecurity:
					if (OnPassedSecurityEvent != null)
						OnPassedSecurityEvent(this, args);
					break;

				case RouteHandlerEventType.FailedSecurity:
					if (OnFailedSecurityEvent != null)
						OnFailedSecurityEvent(this, args);
					break;

				case RouteHandlerEventType.MissingRoute:
					if (OnMissingRouteEvent != null)
					{
						OnMissingRouteEvent(this, args);

						route = args.RouteInfo;
					}
					break;

				case RouteHandlerEventType.Error:
					if (OnErrorEvent != null)
						OnErrorEvent(this, args);
					break;
			}

			return route;
		}
	}

	public abstract class Controller : BaseController
	{
		internal string PartitionName;

		protected Dictionary<string, string> ViewTags { get; private set; }
		protected Dictionary<string, Dictionary<string, string>> FragTags { get; private set; }
		protected dynamic ViewBag { get; private set; }
		protected dynamic FragBag { get; private set; }

		protected event EventHandler<RouteHandlerEventArgs> OnPreActionEvent, OnPostActionEvent;

		internal void initializeViewTags()
		{
			ViewTags = new Dictionary<string, string>();
			FragTags = new Dictionary<string, Dictionary<string, string>>();
			FragBag = new DynamicDictionary();
			ViewBag = new DynamicDictionary();
		}

		internal override void Refresh(Engine engine)
		{
			base.Refresh(engine);

			initializeViewTags();
		}

		internal void init(Engine engine)
		{
			this.engine = engine;

			initializeViewTags();

			PartitionAttribute partitionAttrib = (PartitionAttribute)GetType().GetCustomAttributes(false).FirstOrDefault(x => x.GetType() == typeof(PartitionAttribute));

			if (partitionAttrib != null)
				PartitionName = partitionAttrib.Name;
		}

		internal static Controller CreateInstance(Type type, Engine engine)
		{
			Controller controller = (Controller)Activator.CreateInstance(type);
			controller.init(engine);
			return controller;
		}

		private Dictionary<string, string> GetTagsDictionary(Dictionary<string, string> tags, dynamic tagBag, string subDict)
		{
			Dictionary<string, string> result = tags;

			if (!tagBag.IsEmpty())
			{
				Dictionary<string, object> bag = string.IsNullOrEmpty(subDict) ? tagBag.GetDynamicDictionary() : tagBag.GetDynamicDictionary(subDict);

				if (bag != null)
					result = bag.ToDictionary(k => k.Key, k => (k.Value != null) ? k.Value.ToString() : string.Empty);
			}

			return result;
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
					if (OnPreActionEvent != null)
						OnPreActionEvent(this, args);
					break;

				case RouteHandlerEventType.Post:
					if (OnPostActionEvent != null)
						OnPostActionEvent(this, args);
					break;
			}
		}

		#region RENDER FRAGMENT
		public string RenderFragment(string fragmentName)
		{
			Dictionary<string, string> tags = GetTagsDictionary(FragTags.ContainsKey(fragmentName) ? FragTags[fragmentName] : null, FragBag, fragmentName);

			return RenderFragment(fragmentName, tags);
		}

		public string RenderFragment(string fragmentName, Dictionary<string, string> fragTags)
		{
			return engine.ViewEngine.LoadView(PartitionName, this.GetType().Name, fragmentName, ViewTemplateType.Fragment, fragTags);
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
			return new ViewResult(engine.ViewEngine, PartitionName, controllerName, actionName, ViewTemplateType.Action, GetTagsDictionary(ViewTags, ViewBag, null));
		}

		public FileResult View(string fileName, byte[] fileBytes, string contentType)
		{
			return new FileResult(fileName, fileBytes, contentType);
		}

		public ViewResult Partial(string name)
		{
			return Partial(this.GetType().Name, name);
		}

		public ViewResult Partial(string controllerName, string actionName)
		{
			return new ViewResult(engine.ViewEngine, PartitionName, controllerName, actionName, ViewTemplateType.Shared, GetTagsDictionary(ViewTags, ViewBag, null));
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
		private string view;

		public ViewResult(IViewEngine viewEngine, string partitionName, string controllerName, string viewName, ViewTemplateType viewTemplateType, Dictionary<string, string> viewTags)
		{
			view = viewEngine.LoadView(partitionName, controllerName, viewName, viewTemplateType, viewTags);
		}

		public ViewResponse Render()
		{
			return (string.IsNullOrEmpty(view)) ? null : new ViewResponse() { ContentType = "text/html", Content = view };
		}
	}

	public class FileResult : IViewResult
	{
		private string path;
		private byte[] file;
		private string fileName;
		private string contentType;
		private Dictionary<string, string> headers = new Dictionary<string, string>();

		public FileResult(string name, string data)
			: this(name, ASCIIEncoding.UTF8.GetBytes(data), null) { }

		public FileResult(string name, byte[] data, string contentType)
		{
			string fileExtension = Path.GetExtension(name);

			if (!string.IsNullOrEmpty(contentType) || HttpAdapterConstants.MimeTypes.ContainsKey(fileExtension))
				contentType = HttpAdapterConstants.MimeTypes[fileExtension];

			this.contentType = contentType;
			fileName = name;
			file = data;

			headers["content-disposition"] = string.Format("attachment;filename=\"{0}\"", fileName);
		}

		public FileResult(Regex allowedFilePattern, string path)
		{
			this.path = path;
			fileName = Path.GetFileName(path);

			if (File.Exists(path) && allowedFilePattern.IsMatch(path))
			{
				string fileExtension = Path.GetExtension(path);

				if (HttpAdapterConstants.MimeTypes.ContainsKey(fileExtension))
				{
					contentType = HttpAdapterConstants.MimeTypes[fileExtension];
					file = File.ReadAllBytes(path);
				}
			}
		}

		public ViewResponse Render()
		{
			if (file == null)
				return null;

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
			return new ViewResponse() { Content = null, ContentType = "application/json" };
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
		private static Regex commentBlockRE = new Regex(@"\@\@(?<block>[\s\w\p{P}\p{S}]+?)\@\@", RegexOptions.Compiled);

		public ViewTemplateLoader(string appRoot, string[] viewRoots)
		{
			appRoot.ThrowIfArgumentNull();

			this.appRoot = appRoot;
			this.viewRoots = viewRoots;
		}

		public List<ViewTemplate> Load()
		{
			List<ViewTemplate> templates = new List<ViewTemplate>();

			foreach (string viewRoot in viewRoots)
			{
				if (Directory.Exists(viewRoot))
					foreach (FileInfo fi in GetAllFiles(new DirectoryInfo(viewRoot), "*.html"))
						templates.Add(Load(fi.FullName));
			}

			return templates;
		}

		// This code was adapted to work with FileInfo/DirectoryInfo but was originally from the following question on SO:
		// http://stackoverflow.com/questions/929276/how-to-recursively-list-all-the-files-in-a-directory-in-c
		public static IEnumerable<FileInfo> GetAllFiles(DirectoryInfo dirInfo, string searchPattern = "")
		{
			string path = dirInfo.FullName;

			Queue<string> queue = new Queue<string>();
			queue.Enqueue(path);

			while (queue.Count > 0)
			{
				path = queue.Dequeue();

				foreach (string subDir in Directory.GetDirectories(path))
					queue.Enqueue(subDir);

				FileInfo[] fileInfos = new DirectoryInfo(path).GetFiles(searchPattern);

				if (fileInfos != null)
					for (int i = 0; i < fileInfos.Length; i++)
						yield return fileInfos[i];
			}
		}

		public ViewTemplate Load(string path)
		{
			string viewRoot = viewRoots.FirstOrDefault(x => path.StartsWith(x));

			if (string.IsNullOrEmpty(viewRoot)) return null;

			DirectoryInfo rootDir = new DirectoryInfo(viewRoot);

			string extension = Path.GetExtension(path);
			string templateName = Path.GetFileNameWithoutExtension(path);
			string templateKeyName = path.Replace(rootDir.Parent.FullName, string.Empty)
																	 .Replace(appRoot, string.Empty)
																	 .Replace(extension, string.Empty)
																	 .Replace("\\", "/").TrimStart('/');

			string template = File.ReadAllText(path);

			commentBlockRE.Replace(template, string.Empty);

			ViewTemplateType templateType = ViewTemplateType.Action;

			if (path.ToLower().Contains(sharedHint))
				templateType = ViewTemplateType.Shared;
			else if (path.ToLower().Contains(fragmentsHint))
				templateType = ViewTemplateType.Fragment;

			string partition = null;
			string controller = null;

			if (templateType == ViewTemplateType.Action || templateType == ViewTemplateType.Shared)
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

			return new ViewTemplate()
			{
				MD5sum = template.CalculateMD5sum(),
				Partition = partition,
				Controller = controller,
				FullName = templateKeyName,
				Name = templateName,
				Path = path,
				Template = template,
				TemplateType = templateType
			};
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
		private static string tokenName = "%%AntiForgeryToken%%";
		private Func<string> createAntiForgeryToken;

		public DirectiveProcessType Type { get; private set; }

		public AntiForgeryTokenSubstitution(Func<string> createAntiForgeryToken)
		{
			this.createAntiForgeryToken = createAntiForgeryToken;

			Type = DirectiveProcessType.Render;
		}

		public StringBuilder Process(StringBuilder content)
		{
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
		private static string tokenName = "%%View%%";
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
				finalPage.Replace(tokenName, directiveInfo.Content.ToString());
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
		private string sharedResourceFolderPath;
		private Func<string, string[]> getBundleFiles;
		private Dictionary<string, string> bundleLinkResults;
		private static string cssIncludeTag = "<link href=\"{0}\" rel=\"stylesheet\" type=\"text/css\" />";
		private static string jsIncludeTag = "<script src=\"{0}\" type=\"text/javascript\"></script>";

		public DirectiveProcessType Type { get; private set; }

		public BundleDirective(bool debugMode, string sharedResourceFolderPath, Func<string, string[]> getBundleFiles)
		{
			this.debugMode = debugMode;
			this.sharedResourceFolderPath = sharedResourceFolderPath;
			this.getBundleFiles = getBundleFiles;

			bundleLinkResults = new Dictionary<string, string>();

			Type = DirectiveProcessType.AfterCompile;
		}

		public string ProcessBundleLink(string bundlePath)
		{
			string tag = string.Empty;
			string extension = Path.GetExtension(bundlePath).Substring(1).ToLower();
			bool isAPath = bundlePath.Contains('/') ? true : false;
			string modifiedBundlePath = bundlePath;

			if (!isAPath)
				modifiedBundlePath = string.Join("/", sharedResourceFolderPath, extension, bundlePath);

			if (extension == "css")
				tag = string.Format(cssIncludeTag, modifiedBundlePath);
			else if (extension == "js")
				tag = string.Format(jsIncludeTag, modifiedBundlePath);

			return tag;
		}

		public StringBuilder Process(ViewCompilerDirectiveInfo directiveInfo)
		{
			if (directiveInfo.Directive == "Bundle")
			{
				StringBuilder fileLinkBuilder = new StringBuilder();

				string bundleName = directiveInfo.Value;

				if (bundleLinkResults.ContainsKey(bundleName))
					fileLinkBuilder.AppendLine(bundleLinkResults[bundleName]);
				else
				{
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

					bundleLinkResults[bundleName] = fileLinkBuilder.ToString();
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

		private List<ViewTemplate> viewTemplates;
		private List<CompiledView> compiledViews;
		private Dictionary<string, List<string>> viewDependencies;
		private Dictionary<string, HashSet<string>> templateKeyNames;

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

			templateKeyNames = new Dictionary<string, HashSet<string>>();
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

			throw new FileNotFoundException(string.Format("Cannot find view : {0}", viewName));
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
						tagSB.Clear();
						tagSB.Insert(0, string.Format(tagFormatPattern, tag.Key));

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
										compiledViewSB.Replace(m.Value, new Markdown().Transform((tag.Value.Trim())));
								}
							}
						}
					}

					MatchCollection leftoverMatches = tagRE.Matches(compiledViewSB.ToString());

					if (leftoverMatches != null)
						foreach (Match match in leftoverMatches)
							compiledViewSB.Replace(match.Value, string.Empty);
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

			Func<string, string> determineKeyName = x =>
			{
				return DetermineKeyName(partitionName, controllerName, x, ViewTemplateType.Shared);
			};

			Action<string> addPageDependency = x =>
			{
				if (!viewDependencies[fullViewName].Contains(x))
					viewDependencies[fullViewName].Add(x);
			};

			Action<IEnumerable<IViewCompilerDirectiveHandler>> performCompilerPass = x =>
			{
				MatchCollection dirMatches = directiveTokenRE.Matches(pageContent.ToString());

				foreach (Match match in dirMatches)
				{
					directive.Clear();
					directive.Insert(0, match.Groups["directive"].Value);

					value.Clear();
					value.Insert(0, match.Groups["value"].Value);

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
			};

			performCompilerPass(directiveHandlers.Where(x => x.Type == DirectiveProcessType.Compile));

			foreach (IViewCompilerSubstitutionHandler sub in substitutionHandlers.Where(x => x.Type == DirectiveProcessType.Compile))
				pageContent = sub.Process(pageContent);

			performCompilerPass(directiveHandlers.Where(x => x.Type == DirectiveProcessType.AfterCompile));

			return pageContent;
		}

		public string DetermineKeyName(string partitionName, string controllerName, string viewName, ViewTemplateType viewType)
		{
			HashSet<string> keyTypes;

			string viewTypeName = string.Empty;
			string lookupKeyName = string.Empty;

			if (string.IsNullOrEmpty(partitionName)) partitionName = "Views";
			if (string.IsNullOrEmpty(controllerName)) controllerName = "Shared";
			if (viewType == ViewTemplateType.Shared) viewTypeName = "Shared/";
			else if (viewType == ViewTemplateType.Fragment) viewTypeName = "Fragments/";

			lookupKeyName = string.Format("{0}/{1}/{2}{3}", partitionName, controllerName, viewTypeName, viewName);

			if (!(templateKeyNames.TryGetValue(lookupKeyName, out keyTypes)))
			{
				keyTypes = new HashSet<string>();

				switch (viewType)
				{
					case ViewTemplateType.Action:
						keyTypes.Add(string.Format(partitionControllerScopeActionKeyName, partitionName, controllerName, viewName));
						keyTypes.Add(string.Format(controllerScopeActionKeyName, controllerName, viewName));
						break;

					case ViewTemplateType.Shared:
						if (partitionName != "Views")
							keyTypes.Add(string.Format(partitionRootScopeSharedKeyName, partitionName, viewName));

						if (partitionName != "Shared")
							keyTypes.Add(string.Format(partitionControllerScopeSharedKeyName, partitionName, controllerName, viewName));

						keyTypes.Add(string.Format(controllerScopeSharedKeyName, controllerName, viewName));
						keyTypes.Add(string.Format(globalScopeSharedKeyName, viewName));
						break;

					case ViewTemplateType.Fragment:
						if (controllerName != "Shared")
							keyTypes.Add(string.Format(partitionControllerScopeFragmentKeyName, partitionName, controllerName, viewName));

						if (partitionName != "Views")
							keyTypes.Add(string.Format(partitionRootScopeFragmentsKeyName, partitionName, viewName));

						keyTypes.Add(string.Format(controllerScopeFragmentKeyName, controllerName, viewName));
						keyTypes.Add(string.Format(globalScopeFragmentKeyName, viewName));
						break;
				}

				templateKeyNames[lookupKeyName] = keyTypes;
			}

			keyTypes.IntersectWith(viewTemplates.Select(x => x.FullName));

			return keyTypes.FirstOrDefault();
		}

		public void RecompileDependencies(string fullViewName, string partitionName, string controllerName)
		{
			var deps = viewDependencies.Where(x => x.Value.FirstOrDefault(y => y == fullViewName) != null);

			Action<string> compile = name =>
			{
				var template = viewTemplates.FirstOrDefault(x => x.FullName == name);

				if (template != null /*&& template.TemplateType != ViewTemplateType.Fragment*/)
					Compile(partitionName, controllerName, template.Name, template.TemplateType);
			};

			if (deps.Count() > 0)
			{
				foreach (KeyValuePair<string, List<string>> view in deps)
					compile(view.Key);
			}
			else
				compile(fullViewName);
		}
	}

	public interface IViewEngine
	{
		string LoadView(string partitionName, string controllerName, string viewName, ViewTemplateType viewType, Dictionary<string, string> tags);
		string GetCache();
		bool UpdateCache { get; }
	}

	internal class ViewCache
	{
		public List<ViewTemplate> ViewTemplates;
		public List<CompiledView> CompiledViews;
		public Dictionary<string, List<string>> ViewDependencies;
	}

	internal class ViewEngine : IViewEngine
	{
		private string appRoot;
		private List<IViewCompilerDirectiveHandler> dirHandlers;
		private List<IViewCompilerSubstitutionHandler> substitutionHandlers;
		private List<ViewTemplate> viewTemplates;
		private List<CompiledView> compiledViews;
		private Dictionary<string, List<string>> viewDependencies;

		private ViewTemplateLoader viewTemplateLoader;
		private ViewCompiler viewCompiler;

		public bool UpdateCache { get; private set; }

		public ViewEngine(string _appRoot, string[] _viewRoots, List<IViewCompilerDirectiveHandler> dirHandlers, List<IViewCompilerSubstitutionHandler> substitutionHandlers, string cache)
		{
			appRoot = _appRoot;
			string[] viewRoots = _viewRoots;

			this.dirHandlers = dirHandlers;
			this.substitutionHandlers = substitutionHandlers;

			viewTemplateLoader = new ViewTemplateLoader(appRoot, viewRoots);

			FileSystemWatcher watcher = new FileSystemWatcher(appRoot, "*.html");

			watcher.NotifyFilter = NotifyFilters.LastWrite;
			watcher.Changed += new FileSystemEventHandler(OnChanged);
			watcher.IncludeSubdirectories = true;
			watcher.EnableRaisingEvents = true;

			viewTemplates = new List<ViewTemplate>();
			compiledViews = new List<CompiledView>();
			viewDependencies = new Dictionary<string, List<string>>();

			if (!(viewRoots.Count() >= 1))
				throw new ArgumentException("At least one view root is required to load view templates from.");

			ViewCache viewCache = null;

			if (!string.IsNullOrEmpty(cache))
			{
				viewCache = JsonConvert.DeserializeObject<ViewCache>(cache);

				if (viewCache != null)
				{
					viewTemplates = viewCache.ViewTemplates;
					compiledViews = viewCache.CompiledViews;
					viewDependencies = viewCache.ViewDependencies;
				}
			}

			if (viewCache == null)
			{
				viewTemplates = viewTemplateLoader.Load();

				if (!(viewTemplates.Count() > 0))
					throw new Exception("Failed to load any view templates.");
			}

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

				while (CanOpenForRead(e.FullPath) == false)
					Thread.Sleep(1000);

				ViewTemplate changedTemplate = viewTemplateLoader.Load(e.FullPath);
				viewTemplates.Remove(viewTemplates.Find(x => x.FullName == changedTemplate.FullName));
				viewTemplates.Add(changedTemplate);

				CompiledView cv = compiledViews.FirstOrDefault(x => x.FullName == changedTemplate.FullName && x.TemplateMD5sum != changedTemplate.MD5sum);

				if (cv != null && changedTemplate.TemplateType == ViewTemplateType.Fragment)
				{
					cv.TemplateMD5sum = changedTemplate.MD5sum;
					cv.Template = changedTemplate.Template;
					cv.CompiledTemplate = changedTemplate.Template;
					cv.Render = string.Empty;
				}

				viewCompiler = new ViewCompiler(viewTemplates, compiledViews, viewDependencies, dirHandlers, substitutionHandlers);

				if (cv != null)
					viewCompiler.RecompileDependencies(changedTemplate.FullName, changedTemplate.Partition, changedTemplate.Controller);
				else
					viewCompiler.Compile(changedTemplate.Partition, changedTemplate.Controller, changedTemplate.Name, changedTemplate.TemplateType);

				UpdateCache = true;
			}
			finally
			{
				fsw.EnableRaisingEvents = true;
			}
		}

		public string GetCache()
		{
			if (UpdateCache) UpdateCache = false;

			return JsonConvert.SerializeObject(new ViewCache()
			{
				CompiledViews = compiledViews,
				ViewTemplates = viewTemplates,
				ViewDependencies = viewDependencies
			});
		}

		// adapted from: http://stackoverflow.com/a/8218033/170217
		private static bool CanOpenForRead(string filePath)
		{
			try
			{
				using (FileStream file = new FileStream(filePath, FileMode.Open, FileAccess.Read))
				{
					file.Close();
					return true;
				}
			}
			catch
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

	#region EXTENSION METHODS / DYNAMIC DICTIONARY
	#region EXTENSION METHODS
	public static class ExtensionMethods
	{
		public static void ThrowIfArgumentNull<T>(this T t, string message = null)
		{
			string argName = t.GetType().Name;

			if (t == null)
				throw new ArgumentNullException(argName, message);
			else if ((t is string) && (t as string) == string.Empty)
				throw new ArgumentException(argName, message);
		}

		// from: http://blogs.msdn.com/b/csharpfaq/archive/2006/10/09/how-do-i-calculate-a-md5-hash-from-a-string_3f00_.aspx
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

		public static object[] ConvertToObjectTypeArray(this string[] parms)
		{
			DateTime? dt = null;

			if (parms != null)
			{
				object[] _parms = new object[parms.Length];

				for (int i = 0; i < parms.Length; i++)
				{
					if (parms[i].IsInt32())
						_parms[i] = Convert.ToInt32(parms[i]);
					else if (parms[i].IsLong())
						_parms[i] = Convert.ToInt64(parms[i]);
					else if (parms[i].IsDouble())
						_parms[i] = Convert.ToDouble(parms[i]);
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
			if (_members.ContainsKey(key) && (_members[key] is DynamicDictionary))
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