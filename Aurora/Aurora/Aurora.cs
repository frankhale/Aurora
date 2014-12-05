// 
// Aurora engine rewrite (WIP)
// 
// Frank Hale <frankhale@gmail.com>
// 4 December 2014
//
// --------------------
// --- Feature List ---
// --------------------
//
// - Model View Controller based 
// - Apps can have a Front controller to intercept various events and perform
//   arbitrary logic before actions are invoked.
// - Simple tag based view engine with master pages and partial views as well as
//   fragments. 
// - URL parameters bind to action method parameters automatically, no fiddling 
//	 with routes declarations. 
// - Posted forms binds to post models or action parameters automatically. 
// - Actions can have bound parameters that are bound to actions (dependency 
//	 injection)
// - Actions can be segregated based on Get, Post, GetOrPost, Put and Delete 
//   action type and you can secure them with the ActionSecurity named 
//	 parameter.
// - Actions can have filters with optional filter results that bind to action
//   parameters.  
// - Actions can have aliases. Aliases can also be added dynamically at runtime
//   along with default parameters.
// - Bundling/Minifying of Javascript and CSS.
// - Html Helpers
//
// LICENSE:
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

using AspNetAdapter;
using MarkdownSharp;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Web;
using System.Xml.Linq;

namespace Aurora
{
	using Aurora.Common;

	#region FRAMEWORK ENGINE
	internal class EngineAppState
	{
		private static readonly string EngineAppStateSessionName = "__ENGINE_APP_STATE__";

		private Dictionary<string, object> app;
		private Dictionary<string, object> request;

		public static readonly Regex AllowedFilePattern = new Regex(@"^.*\.(js|css|png|jpg|gif|ico|pptx|xlsx|csv|txt)$", RegexOptions.Compiled | RegexOptions.IgnoreCase);

		// This is used in the bundle code to figure out what files to operate on.
		// This should probably live there and not here since it's not going to 
		// change.
		public static readonly Regex CssOrJsExtPattern = new Regex(@"^.*\.(js|css)$", RegexOptions.Compiled | RegexOptions.IgnoreCase);

		public static readonly string SharedResourceFolderPath = "/Resources";
		public static readonly string CompiledViewsCacheFolderPath = "/Views/Cache";
		public static readonly string CompiledViewsCacheFileName = "viewsCache.json";

		// This really doesn't need to be here. I think this should probably live
		// inside the view engine.
		public static readonly string AntiForgeryTokenName = "AntiForgeryToken";

		private class AppState
		{
			public ViewEngine ViewEngine { get; set; }
			public List<User> Users { get; set; }
			public List<string> AntiForgeryTokens { get; set; }
			public Dictionary<string, string> ProtectedFiles { get; set; }
			public List<Type> Models { get; set; }
			public string CacheFilePath { get; set; }
			public string[] ViewRoots { get; set; }
			public List<IViewCompilerDirectiveHandler> ViewEngineDirectiveHandlers { get; set; }
			public List<IViewCompilerSubstitutionHandler> ViewEngineSubstitutionHandlers { get; set; }
			public Dictionary<string, Tuple<List<string>, string>> Bundles { get; set; }
		}

		private AppState appState { get; set; }

		// Yeah I'm doing it. This is crazy but it helps keep things simple when I
		// stuff the data into a session variable. This is a trade off. I want to
		// get one value out of the session and put one value in.

		public ViewEngine ViewEngine
		{
			get { return appState.ViewEngine; }
			set { appState.ViewEngine = value; }
		}
		public List<User> Users
		{
			get { return appState.Users; }
			set { appState.Users = value; }
		}
		public List<string> AntiForgeryTokens
		{
			get { return appState.AntiForgeryTokens; }
			set { appState.AntiForgeryTokens = value; }
		}
		public Dictionary<string, string> ProtectedFiles
		{
			get { return appState.ProtectedFiles; }
			set { appState.ProtectedFiles = value; }
		}
		public List<Type> Models
		{
			get { return appState.Models; }
			set { appState.Models = value; }
		}
		public string CacheFilePath
		{
			get { return appState.CacheFilePath; }
			set { appState.CacheFilePath = value; }
		}
		public string[] ViewRoots
		{
			get { return appState.ViewRoots; }
			set { appState.ViewRoots = value; }
		}
		public List<IViewCompilerDirectiveHandler> ViewEngineDirectiveHandlers
		{
			get { return appState.ViewEngineDirectiveHandlers; }
			set { appState.ViewEngineDirectiveHandlers = value; }
		}
		public List<IViewCompilerSubstitutionHandler> ViewEngineSubstitutionHandlers
		{
			get { return appState.ViewEngineSubstitutionHandlers; }
			set { appState.ViewEngineSubstitutionHandlers = value; }
		}
		public Dictionary<string, Tuple<List<string>, string>> Bundles
		{
			get { return appState.Bundles; }
			set { appState.Bundles = value; }
		}
		public string SessionId
		{
			get
			{
				return (request[HttpAdapterConstants.SessionId] != null) ?
					request[HttpAdapterConstants.SessionId].ToString() : null;
			}
		}
		public User CurrentUser
		{
			get { return appState.Users.FirstOrDefault(x => x.SessionId == SessionId); }
		}

		// TODO: Investigate and figure out how to resolve this:
		// It's now in app state and it's not going back to session state because 
		// that doesn't make sense.
		//
		// I've wanted to put this in app state but it's problematic because the 
		// OnInit method for a controller is ran for each new session and that is 
		// kind of where I want to add new bindings which means they will run each 
		// time an instance of a controller is created. I haven't figured out a 
		// good method to ignore this. So since adding bindings is something that 
		// happens per controller instance it is stuck here for now even though 
		// bindings won't change between instances. 
		public Dictionary<string, Dictionary<string, List<object>>> ActionBindings { get; set; }

		public EngineAppState(Dictionary<string, object> app, Dictionary<string, object> request, AspNetAdapterCallbacks callbacks)
		{
			this.app = app;
			this.request = request;

			// do the initialization or get the state from the Application store
			appState = callbacks.GetApplication(EngineAppStateSessionName) as AppState ?? new AppState();

			//if (appState.RouteInfos == null)
			//	appState.RouteInfos = new List<RouteInfo>();
			//else
			//	appState.RouteInfos.ForEach(x => x.Controller.Engine = this);

			if (appState.Users == null)
				appState.Users = new List<User>();

			if (appState.AntiForgeryTokens == null)
				appState.AntiForgeryTokens = new List<string>();

			if (appState.ProtectedFiles == null)
				appState.ProtectedFiles = new Dictionary<string, string>();

			if (appState.Models == null)
				appState.Models = ReflectionHelpers.GetTypeList(typeof(Model));

			//if (appState.ActionBindings == null)
			//	appState.ActionBindings = new Dictionary<string, Dictionary<string, List<object>>>();

			if (appState.Bundles == null)
				appState.Bundles = new Dictionary<string, Tuple<List<string>, string>>();

			callbacks.AddApplication(EngineAppStateSessionName, appState);
		}
	}

	internal class EngineSessionState
	{
		private static readonly string EngineSessionStateSessionName = "__ENGINE_SESSION_STATE__";

		private class SessionState
		{
			public FrontController FrontController { get; set; }
			public List<Controller> Controllers { get; set; }
			public Dictionary<string, object> ControllersSession { get; set; }
			// Helper bundles are impromptu bundles that are added by HTML Helpers
			public Dictionary<string, StringBuilder> HelperBundles { get; set; }
			public List<MethodInfo> ControllerActions { get; set; }
			public bool FromRedirectOnly { get; set; }
			public User CurrentUser { get; set; }
		}

		private SessionState sessionState;

		// Yeah I'm doing it. This is crazy but it helps keep things simple when I
		// stuff the data into a session variable. This is a trade off. I want to
		// get one value out of the session and put one value in.

		public FrontController FrontController
		{
			get { return sessionState.FrontController; }
			set { sessionState.FrontController = value; }
		}
		public List<Controller> Controllers
		{
			get { return sessionState.Controllers; }
			set { sessionState.Controllers = value; }
		}
		public Dictionary<string, object> ControllersSession
		{
			get { return sessionState.ControllersSession; }
			set { sessionState.ControllersSession = value; }
		}
		// Helper bundles are impromptu bundles that are added by HTML Helpers
		public Dictionary<string, StringBuilder> HelperBundles
		{
			get { return sessionState.HelperBundles; }
			set { sessionState.HelperBundles = value; }
		}
		public List<MethodInfo> ControllerActions
		{
			get { return sessionState.ControllerActions; }
			set { sessionState.ControllerActions = value; }
		}
		public bool FromRedirectOnly
		{
			get { return sessionState.FromRedirectOnly; }
			set { sessionState.FromRedirectOnly = value; }
		}
		public User CurrentUser
		{
			get { return sessionState.CurrentUser; }
			set { sessionState.CurrentUser = value; }
		}

		public EngineSessionState(Dictionary<string, object> app, Dictionary<string, object> request, AspNetAdapterCallbacks callbacks)
		{
			// do the initialization or get the state from the Session store
			sessionState = callbacks.GetSession(EngineSessionStateSessionName) as SessionState ?? new SessionState();

			if (sessionState.ControllersSession == null)
				sessionState.ControllersSession = new Dictionary<string, object>();

			if (sessionState.HelperBundles == null)
				sessionState.HelperBundles = new Dictionary<string, StringBuilder>();

			//if (sessionState.FrontController == null)
			//	sessionState.FrontController = GetFrontControllerInstance();
			//else
			//	sessionState.FrontController.Engine = this;

			if (sessionState.Controllers == null)
				sessionState.Controllers = new List<Controller>();

			if (sessionState.FrontController != null)
				sessionState.FrontController.RaiseEvent(EventType.OnInit);

			callbacks.AddSession(EngineSessionStateSessionName, sessionState);
		}
	}

	internal class Engine : IAspNetAdapterApplication
	{
		private EngineAppState engineAppState;
		private EngineSessionState engineSessionState;
		private AspNetAdapterCallbacks aspNetAdapterCallbacks;

		//private static readonly string FromRedirectOnlySessionName = "__FROM_REDIRECT_ONLY__";

		private Dictionary<string, object> app;
		private Dictionary<string, object> request;
		private Action<Dictionary<string, object>> response;

		//ViewRoot = string.Format(@"{0}\Views", AppRoot);

		//private RouteInfo currentRoute;
		//private User CurrentUser;

		//internal string IpAddress, Path, RequestType, AppRoot, ViewRoot, SessionId, Identity;

		public void Init(Dictionary<string, object> app,
										 Dictionary<string, object> request,
										 Action<Dictionary<string, object>> response)
		{
			aspNetAdapterCallbacks = new AspNetAdapterCallbacks(app, request);
			engineAppState = new EngineAppState(app, request, aspNetAdapterCallbacks);
			engineSessionState = new EngineSessionState(app, request, aspNetAdapterCallbacks);

			this.app = app;
			this.request = request;
			this.response = response;

			#region INITIALIZE VIEW ENGINE

			//if (!EngineAppState.AllowedFilePattern.IsMatch(Path) && (EngineAppState.ViewEngine == null || debugMode))
			//{
			//	string viewCache = null;

			//	if (EngineAppState.ViewEngineDirectiveHandlers == null && EngineAppState.ViewEngineSubstitutionHandlers == null)
			//	{
			//		EngineAppState.ViewEngineDirectiveHandlers = new List<IViewCompilerDirectiveHandler>();
			//		EngineAppState.ViewEngineSubstitutionHandlers = new List<IViewCompilerSubstitutionHandler>();

			//		EngineAppState.ViewEngineDirectiveHandlers.Add(new MasterPageDirective());
			//		EngineAppState.ViewEngineDirectiveHandlers.Add(new PlaceHolderDirective());
			//		EngineAppState.ViewEngineDirectiveHandlers.Add(new PartialPageDirective());
			//		EngineAppState.ViewEngineDirectiveHandlers.Add(new BundleDirective(debugMode,
			//			EngineAppState.SharedResourceFolderPath, GetBundleFiles));
			//		EngineAppState.ViewEngineSubstitutionHandlers.Add(new HelperBundleDirective(
			//			EngineAppState.SharedResourceFolderPath, GetHelperBundle));
			//		EngineAppState.ViewEngineSubstitutionHandlers.Add(new CommentSubstitution());
			//		EngineAppState.ViewEngineSubstitutionHandlers.Add(new AntiForgeryTokenSubstitution(CreateAntiForgeryToken));
			//		EngineAppState.ViewEngineSubstitutionHandlers.Add(new HeadSubstitution());
			//	}

			//	if (string.IsNullOrEmpty(EngineAppState.CacheFilePath))
			//		EngineAppState.CacheFilePath = System.IO.Path.Combine(MapPath(EngineAppState.CompiledViewsCacheFolderPath),
			//			EngineAppState.CompiledViewsCacheFileName);

			//	if (File.Exists(EngineAppState.CacheFilePath) && !debugMode)
			//		viewCache = File.ReadAllText(EngineAppState.CacheFilePath);

			//	if (EngineAppState.ViewRoots == null)
			//		EngineAppState.ViewRoots = GetViewRoots();

			//	EngineAppState.ViewEngine = new ViewEngine(AppRoot, EngineAppState.ViewRoots,
			//		EngineAppState.ViewEngineDirectiveHandlers, EngineAppState.ViewEngineSubstitutionHandlers, viewCache);

			//	if (string.IsNullOrEmpty(viewCache) || !debugMode)
			//		UpdateCache(EngineAppState.CacheFilePath);
			//}
			//else if (EngineAppState.ViewEngine.CacheUpdated ||
			//				 !Directory.Exists(System.IO.Path.GetDirectoryName(EngineAppState.CacheFilePath)) ||
			//				 !File.Exists(EngineAppState.CacheFilePath))
			//	UpdateCache(EngineAppState.CacheFilePath);

			#endregion

			#region PROCESS REQUEST / RENDER RESPONSE

			//ViewResponse viewResponse = null;
			//Exception exception = serverError;
			//int httpStatus = 200;

			//if (exception == null)
			//{
			//	try
			//	{
			//		viewResponse = ProcessRequest();
			//	}
			//	catch (Exception ex)
			//	{
			//		exception = ex;
			//	}
			//}

			//if (viewResponse == null && _currentRoute != null && _currentRoute.Action.ReturnType != typeof(void)) return;

			//if (viewResponse == null && exception == null)
			//{
			//	httpStatus = 404;
			//	viewResponse = GetErrorViewResponse("Http 404 - Page Not Found", null);
			//}
			//else if (exception != null)
			//{
			//	httpStatus = 503;
			//	if (debugMode)
			//	{
			//		viewResponse = GetErrorViewResponse(
			//			(exception.InnerException != null) ? exception.InnerException.Message : exception.Message,
			//			(exception.InnerException != null) ? exception.InnerException.StackTrace : exception.StackTrace
			//			);
			//	}
			//	else
			//		viewResponse = GetErrorViewResponse("A problem occurred trying to process this request.", null);
			//}

			//viewResponse.HttpStatus = httpStatus;

			//RenderResponse(viewResponse);

			#endregion
		}

		#region PRIVATE METHODS
		private ViewResponse ProcessRequest()
		{
			//ViewResponse viewResponse = null;

			//if (EngineAppState.AllowedFilePattern.IsMatch(Path))
			//{
			//	#region FILE RESPONSE
			//	RaiseEventOnFrontController(RouteHandlerEventType.Static, Path, null, null);

			//	if (Path.StartsWith(EngineAppState.SharedResourceFolderPath) || Path.EndsWith(".ico"))
			//	{
			//		var filePath = MapPath(Path);

			//		if (CanAccessFile(filePath))
			//		{
			//			if (File.Exists(filePath))
			//				viewResponse = new FileResult(EngineAppState.AllowedFilePattern, filePath).Render();
			//			else
			//			{
			//				var fileName = System.IO.Path.GetFileName(filePath);

			//				if (EngineAppState.Bundles.ContainsKey(fileName))
			//					viewResponse = new FileResult(fileName, EngineAppState.Bundles[fileName].Item2).Render();
			//				else if (EngineSessionState.HelperBundles.ContainsKey(fileName))
			//					viewResponse = new FileResult(fileName, EngineSessionState.HelperBundles[fileName].ToString()).Render();
			//			}
			//		}
			//	}
			//	#endregion
			//}
			//else
			//{
			//	#region ACTION RESPONSE
			//	RouteInfo routeInfo = null;

			//	if (Path == "/" || Path == "~/" || Path.ToLower() == "/default.aspx" || Path == "/Index")
			//	{
			//		Path = "/Index";
			//		pathSegments[0] = "Index";
			//	}

			//	RaiseEventOnFrontController(RouteHandlerEventType.PreRoute, Path, null, null);

			//	routeInfo = FindRoute(string.Concat("/", pathSegments[0]), pathSegments);

			//	RaiseEventOnFrontController(RouteHandlerEventType.PostRoute, Path, routeInfo, null);

			//	if (routeInfo == null)
			//		routeInfo = RaiseEventOnFrontController(RouteHandlerEventType.MissingRoute, Path, null, null);

			//	if (routeInfo != null)
			//	{
			//		_currentRoute = routeInfo;

			//		if (routeInfo.RequestTypeAttribute.ActionType == ActionType.FromRedirectOnly && !EngineSessionState.FromRedirectOnly)
			//			return null;

			//		if (routeInfo.RequestTypeAttribute.RequireAntiForgeryToken &&
			//				RequestType == "post" || RequestType == "put" || RequestType == "delete")
			//		{
			//			if (!(form.ContainsKey(EngineAppState.AntiForgeryTokenName) ||
			//						payload.ContainsKey(EngineAppState.AntiForgeryTokenName)))
			//			{
			//				// i
			//				return GetErrorViewResponse("AntiForgeryToken Required", "All forms require an AntiForgeryToken by default.");
			//			}
			//			else
			//			{
			//				if (EngineAppState.AntiForgeryTokens.Contains(form[EngineAppState.AntiForgeryTokenName]) ||
			//					EngineAppState.AntiForgeryTokens.Contains(payload[EngineAppState.AntiForgeryTokenName]))
			//				{
			//					EngineAppState.AntiForgeryTokens.Remove(form[EngineAppState.AntiForgeryTokenName]);
			//					EngineAppState.AntiForgeryTokens.Remove(payload[EngineAppState.AntiForgeryTokenName]);
			//				}
			//				else
			//				{
			//					return GetErrorViewResponse("AntiForgeryToken Required", "All forms require a valid AntiForgeryToken.");
			//				}
			//			}
			//		}

			//		if (routeInfo.RequestTypeAttribute.SecurityType == ActionSecurity.Secure &&
			//				routeInfo.RequestTypeAttribute.SecurityType == ActionSecurity.Secure && CurrentUser == null ||
			//				routeInfo.RequestTypeAttribute.SecurityType == ActionSecurity.Secure && routeInfo.RequestTypeAttribute.Roles == null ||
			//				routeInfo.RequestTypeAttribute.SecurityType == ActionSecurity.Secure && !(CurrentUser.Roles.Intersect(routeInfo.RequestTypeAttribute.Roles.Split('|')).Any()) ||
			//				routeInfo.RequestTypeAttribute.SecurityType == ActionSecurity.Secure && routeInfo.Controller.RaiseCheckRoles(new CheckRolesHandlerEventArgs() { RouteInfo = routeInfo }))
			//		{
			//			RaiseEventOnFrontController(RouteHandlerEventType.FailedSecurity, Path, routeInfo, null);

			//			if (!string.IsNullOrEmpty(routeInfo.RequestTypeAttribute.RedirectWithoutAuthorizationTo))
			//			{
			//				viewResponse = new ViewResponse() { RedirectTo = routeInfo.RequestTypeAttribute.RedirectWithoutAuthorizationTo };
			//			}
			//		}
			//		else
			//		{
			//			RaiseEventOnFrontController(RouteHandlerEventType.PassedSecurity, Path, routeInfo, null);
			//			RaiseEventOnFrontController(RouteHandlerEventType.PreAction, Path, routeInfo, null);
			//			routeInfo.Controller.RaiseEvent(RouteHandlerEventType.PreAction, Path, routeInfo);

			//			if (routeInfo.RequestTypeAttribute.ActionType == ActionType.FromRedirectOnly && EngineSessionState.FromRedirectOnly)
			//				RemoveSession(EngineAppState.FromRedirectOnlySessionName);

			//			if (routeInfo.BoundToActionParams != null)
			//			{
			//				foreach (var bta in routeInfo.BoundToActionParams)
			//					bta.Initialize(routeInfo);
			//			}

			//			if (routeInfo.BoundParams != null)
			//			{
			//				for (var i = 0; i < routeInfo.BoundParams.Count(); i++)
			//				{
			//					if (routeInfo.BoundParams[i].GetType().GetInterface(typeof(IBoundToAction).Name) == null)
			//						routeInfo.BoundParams[i] = Activator.CreateInstance(routeInfo.BoundParams[i].GetType(), null);
			//				}
			//			}

			//			if (routeInfo.ActionParamTransforms != null)
			//			{
			//				foreach (var apt in routeInfo.ActionParamTransforms)
			//				{
			//					var transformMethod = routeInfo.CachedActionParamTransformInstances[apt.Item1.TransformName] as Tuple<MethodInfo, object>;

			//					if (transformMethod != null)
			//					{
			//						var t = transformMethod.Item1.GetParameters()[0].ParameterType;
			//						var param = routeInfo.ActionUrlParams[apt.Item2];

			//						if (routeInfo.ActionUrlParams[apt.Item2] != null &&
			//								routeInfo.ActionUrlParams[apt.Item2].GetType() != t)
			//						{
			//							try
			//							{
			//								param = Convert.ChangeType(routeInfo.ActionUrlParams[apt.Item2], t);
			//							}
			//							catch
			//							{
			//								// Oops! We probably tried to convert a type to another type and it failed! 
			//								// In which case we'll pretend like nothing happened.
			//							}
			//						}

			//						try
			//						{
			//							routeInfo.ActionUrlParams[apt.Item2] =
			//								transformMethod.Item1.Invoke(transformMethod.Item2, new object[] { param });
			//						}
			//						catch
			//						{
			//							// Oops! We probably tried to invoke an action with incorrect types! 
			//							// In which case we'll pretend like nothing happened.
			//						}
			//					}
			//				}
			//			}

			//			var filterResults = ProcessAnyActionFilters(routeInfo);

			//			if (filterResults.Any())
			//			{
			//				var actionParams = routeInfo.ActionUrlParams;
			//				Array.Resize(ref actionParams, actionParams.Count() + filterResults.Count());
			//				filterResults.CopyTo(actionParams, EngineSessionState.ActionBindings.Count());
			//				routeInfo.ActionUrlParams = actionParams;
			//			}

			//			routeInfo.Controller.HttpAttribute = routeInfo.RequestTypeAttribute;

			//			IViewResult viewResult;
			//			try
			//			{
			//				viewResult = (IViewResult)routeInfo.Action.Invoke(routeInfo.Controller, routeInfo.ActionUrlParams);
			//			}
			//			catch (Exception ex)
			//			{
			//				// I'm not completely sure how I want to ultimately handle this condtion yet,
			//				// for now let's fire off the front controllers error event in case anyone is
			//				// looking for it. The usefulness of this is questionable, what are they going to
			//				// do with the information? Should we allow another action to be potentially 
			//				// invoked so that we can get a good viewResult? Thereby dropping the throw below
			//				// this call?!? 
			//				RaiseEventOnFrontController(RouteHandlerEventType.Error, Path, routeInfo, ex);

			//				throw;
			//			}

			//			if (routeInfo.Action.ReturnType != typeof(void))
			//			{
			//				if (viewResult != null)
			//					viewResponse = viewResult.Render();

			//				if (viewResponse == null)
			//					RaiseEventOnFrontController(RouteHandlerEventType.Error, Path, routeInfo,
			//						"A problem occurred trying to render the result causing it to be null. Please check to make sure you have a view for this action.");
			//			}

			//			RaiseEventOnFrontController(RouteHandlerEventType.PostAction, Path, routeInfo, viewResponse);
			//			routeInfo.Controller.RaiseEvent(RouteHandlerEventType.PostAction, Path, routeInfo);
			//		}
			//	}
			//	#endregion
			//}

			//return viewResponse;
			throw new NotImplementedException();
		}

		private void UpdateCache(string cacheFilePath)
		{
			//try
			//{
			//	var path = System.IO.Path.GetDirectoryName(cacheFilePath);

			//	if (!Directory.Exists(path))
			//	{
			//		try { Directory.CreateDirectory(path); }
			//		catch { /* Silently ignore failure */ }
			//	}

			//	if (!Directory.Exists(path)) return;

			//	using (var cacheWriter = new StreamWriter(cacheFilePath))
			//		cacheWriter.Write(EngineAppState.ViewEngine.GetCache());
			//}
			//catch { /* Silently ignore any write failures */ }
			throw new NotImplementedException();
		}

		private ViewResponse GetErrorViewResponse(string error, string stackTrace)
		{
			//if (!string.IsNullOrEmpty(stackTrace))
			//	stackTrace = string.Format("<p><pre>{0}</pre></p>", stackTrace);

			//Dictionary<string, string> tags;

			//if (_currentRoute != null)
			//{
			//	Dictionary<string, object> ttags = _currentRoute.Controller.ViewBag.AsDictionary();

			//	tags = ttags.ToDictionary(k => k.Key, k => k.Value.ToString());
			//}
			//else
			//	tags = new Dictionary<string, string>();

			//tags["error"] = error;
			//tags["stacktrace"] = stackTrace;

			//var errorView = EngineAppState.ViewEngine.LoadView("Views/Shared/Error", tags);

			//var viewResponse = new ViewResponse()
			//{
			//	ContentType = "text/html",
			//	Content = !string.IsNullOrEmpty(errorView) ? errorView : string.Format("<!DOCTYPE html><body>{0} : {1} {2}</body></html>", Path, error, stackTrace)
			//};

			//return viewResponse;
			throw new NotImplementedException();
		}

		private void RenderResponse(ViewResponse viewResponse)
		{
			//if (string.IsNullOrEmpty(viewResponse.RedirectTo))
			//{
			//	response(new Dictionary<string, object>
			//	{
			//		{HttpAdapterConstants.ResponseBody, viewResponse.Content},
			//		{HttpAdapterConstants.ResponseContentType, viewResponse.ContentType},
			//		{HttpAdapterConstants.ResponseHeaders, viewResponse.Headers},
			//		{HttpAdapterConstants.ResponseStatus, viewResponse.HttpStatus}
			//	});
			//}
			//else
			//{
			//	var redirectRoute = FindRoute(viewResponse.RedirectTo, new string[] { });

			//	if (redirectRoute != null)
			//		ResponseRedirect(viewResponse.RedirectTo, (redirectRoute.RequestTypeAttribute.ActionType == ActionType.FromRedirectOnly) ? true : false);
			//	else
			//		RenderResponse(GetErrorViewResponse("Unable to determine the route to redirect to.", null));
			//}
			throw new NotImplementedException();
		}

		private string[] GetViewRoots()
		{
			//var viewRoots = new List<string>() { ViewRoot };
			//var partitionAttributes = GetTypeList(typeof(Controller))
			//	.SelectMany(x => x.GetCustomAttributes(typeof(PartitionAttribute), false)
			//	.Cast<PartitionAttribute>());

			//viewRoots.AddRange(partitionAttributes.Select(x => string.Format(@"{0}\{1}", AppRoot, x.Name)));

			//return viewRoots.ToArray();
			throw new NotImplementedException();
		}

		private object PayloadToModel(Dictionary<string, string> payload)
		{
			//object result = null;
			//Type model = null;
			//var payloadNames = new HashSet<string>(payload.Keys.Where(x => x != "AntiForgeryToken"));

			//foreach (Type m in EngineAppState.Models)
			//{
			//	var props = new HashSet<string>(Model.GetPropertiesWithExclusions(m, true).Select(x => x.Name));

			//	if (props.Intersect(payloadNames).Count() == props.Union(payloadNames).Count())
			//		model = m;
			//	else
			//	{
			//		props = new HashSet<string>(Model.GetPropertiesNotRequiredToPost(m).Select(x => x.Name));

			//		if (props.IsSubsetOf(payloadNames))
			//			model = m;
			//	}
			//}

			//if (model != null)
			//{
			//	result = Activator.CreateInstance(model);

			//	foreach (PropertyInfo p in Model.GetPropertiesWithExclusions(model, true))
			//	{
			//		var skipValidationAttrib = (UnsafeAttribute)p.GetCustomAttributes(typeof(UnsafeAttribute), false).FirstOrDefault();
			//		var notRequiredAttrib = (NotRequiredAttribute)p.GetCustomAttributes(typeof(NotRequiredAttribute), false).FirstOrDefault();

			//		if (notRequiredAttrib != null && !payload.ContainsKey(p.Name)) continue;

			//		var propertyValue = payload[p.Name];

			//		if (skipValidationAttrib == null)
			//			propertyValue = GetValidatedFormValue(p.Name);

			//		if (p.PropertyType == typeof(int) || p.PropertyType == typeof(int?))
			//		{
			//			if (propertyValue.IsInt32())
			//				p.SetValue(result, Convert.ToInt32(propertyValue), null);
			//		}
			//		else if (p.PropertyType == typeof(string))
			//		{
			//			p.SetValue(result, propertyValue, null);
			//		}
			//		else if (p.PropertyType == typeof(bool))
			//		{
			//			if (propertyValue.IsBool())
			//				p.SetValue(result, Convert.ToBoolean(propertyValue), null);
			//		}
			//		else if (p.PropertyType == typeof(DateTime?))
			//		{
			//			DateTime? dt = null;

			//			propertyValue.IsDate(out dt);

			//			p.SetValue(result, dt, null);
			//		}
			//		else if (p.PropertyType == typeof(PostedFile))
			//		{
			//			if (files.Any())
			//				p.SetValue(result, files[0], null);
			//		}
			//		else if (p.PropertyType == typeof(List<PostedFile>))
			//			p.SetValue(result, files, null);
			//	}

			//	var model1 = result as Model;
			//	if (model1 != null) model1.Validate(payload);
			//}

			//return result;
			throw new NotImplementedException();
		}

		private static List<string> GetControllerActionNames(string controllerName)
		{
			//controllerName.ThrowIfArgumentNull();

			//var result = new List<string>();

			//var controller = GetTypeList(typeof(Controller)).FirstOrDefault(x =>
			//	x.Name == controllerName);

			//if (controller != null)
			//{
			//	result = controller.GetMethods()
			//										 .Where(x =>
			//											 x.GetCustomAttributes(typeof(HttpAttribute), false)
			//											 .Any() && x.IsPublic)
			//										 .Select(x => x.Name)
			//										 .ToList();
			//}

			//return result;
			throw new NotImplementedException();
		}

		private Controller GetControllerInstances(string controllerName)
		{
			//var ctrlInstance = EngineSessionState.Controllers.FirstOrDefault(x => x.GetType().Name == controllerName);

			//if (ctrlInstance == null)
			//{
			//	ctrlInstance = Controller.CreateInstance(GetTypeList(typeof(Controller))
			//													 .FirstOrDefault(x => x.Name == controllerName), this);
			//	EngineSessionState.Controllers.Add(ctrlInstance);
			//	ctrlInstance.RaiseEvent(EventType.OnInit);
			//}
			//else
			//	ctrlInstance.Engine = this;

			//return ctrlInstance;
			throw new NotImplementedException();
		}

		private FrontController GetFrontControllerInstance()
		{
			//FrontController result = null;
			//var fcType = GetTypeList(typeof(FrontController)).FirstOrDefault();

			//if (fcType != null)
			//	result = FrontController.CreateInstance(fcType, this);

			//return result;
			throw new NotImplementedException();
		}

		private ActionParameterInfo GetActionParameterTransforms(ParameterInfo[] actionParams, List<object> bindings)
		{
			//var cachedActionParamTransformInstances = new Dictionary<string, object>();

			//// Eventually I want to change this algorithm to not require the attribute and instead figure
			//// it out based on the type parameter and if there are any action parameter transform classes
			//// that implement the IActionParamTransform<T, V> with the type parameter. This may make it slower
			//// since it would have to check more action parameters in it's parameter list.

			//// The int in this is for the index of the parameter in the action parameter list.
			//var actionParameterTransforms = actionParams
			//		.Select((x, i) => new Tuple<ActionParameterTransformAttribute, int>
			//			((ActionParameterTransformAttribute)x.GetCustomAttributes(typeof(ActionParameterTransformAttribute), false).FirstOrDefault(), i))
			//		.Where(x => x.Item1 != null)
			//		.ToList();

			//if (actionParameterTransforms.Count > 0)
			//{
			//	foreach (var apt in actionParameterTransforms)
			//	{
			//		var actionTransformClassType = Utility.GetAssemblies()
			//												.SelectMany(x => x.GetLoadableTypes().Where(y => y.GetInterface(typeof(IActionParamTransform<,>).Name) != null && y.Name == apt.Item1.TransformName))
			//												.FirstOrDefault();

			//		if (actionTransformClassType != null)
			//		{
			//			try
			//			{
			//				var instance = Activator.CreateInstance(actionTransformClassType, (bindings != null) ? bindings.ToArray() : null);
			//				var transformMethod = actionTransformClassType.GetMethod("Transform");

			//				cachedActionParamTransformInstances[apt.Item1.TransformName] = new Tuple<MethodInfo, object>(transformMethod, instance);
			//			}
			//			catch
			//			{
			//				cachedActionParamTransformInstances[apt.Item1.TransformName] = null;
			//			}
			//		}
			//	}
			//}

			//return new ActionParameterInfo()
			//{
			//	ActionParamTransforms = actionParameterTransforms.Any() ? actionParameterTransforms : null,
			//	ActionParamTransformInstances = cachedActionParamTransformInstances.Any() ? cachedActionParamTransformInstances : null
			//};
			throw new NotImplementedException();
		}

		private IActionFilterResult[] ProcessAnyActionFilters(RouteInfo routeInfo)
		{
			//var results = new List<IActionFilterResult>();
			//var actionFilterAttributes =
			//		routeInfo.Action.GetCustomAttributes(typeof(ActionFilterAttribute), false).Cast<ActionFilterAttribute>().ToList();

			//foreach (var afa in actionFilterAttributes)
			//{
			//	afa.Init(this);
			//	afa.Controller = routeInfo.Controller;
			//	afa.OnFilter(routeInfo);

			//	if (afa.DivertRoute != null)
			//		routeInfo = afa.DivertRoute;

			//	if (afa.FilterResult != null)
			//		results.Add(afa.FilterResult);
			//}

			//return results.ToArray();
			throw new NotImplementedException();
		}

		private RouteInfo RaiseEventOnFrontController(RouteHandlerEventType eventType, string path, RouteInfo routeInfo, object data)
		{
			//if (EngineSessionState.FrontController != null)
			//	return EngineSessionState.FrontController.RaiseEvent(eventType, path, routeInfo, data);

			//return routeInfo;
			throw new NotImplementedException();
		}

		private static string CreateToken()
		{
			//return Guid.NewGuid().ToString().Replace("-", string.Empty);
			throw new NotImplementedException();
		}
		#endregion

		#region INTERNAL METHODS
		// Allows a file to be protected from download by users that are not logged in
		internal void ProtectFile(string path, string roles)
		{
			//path.ThrowIfArgumentNull();
			//roles.ThrowIfArgumentNull();

			//EngineAppState.ProtectedFiles[string.Format(@"{0}\{1}", AppRoot, path)] = roles;
			throw new NotImplementedException();
		}

		internal bool CanAccessFile(string path)
		{
			//path.ThrowIfArgumentNull();

			//if (EngineAppState.ProtectedFiles.ContainsKey(path))
			//	return (CurrentUser != null && CurrentUser.Roles.Intersect(EngineAppState.ProtectedFiles[path].Split('|')).Any()) ? true : false;

			//return true;
			throw new NotImplementedException();
		}

		internal void AddBundles(Dictionary<string, string[]> bundles)
		{
			//foreach (var bundle in bundles)
			//	AddBundle(bundle.Key, bundle.Value);
			throw new NotImplementedException();
		}

		internal void AddBundle(string name, string[] paths)
		{
			//name.ThrowIfArgumentNull();
			//paths.ThrowIfArgumentNull();

			//if (paths.Length == 0) return;

			//var extension = System.IO.Path.GetExtension(name);
			//string fileContentResult = null;
			//var combinedFiles = new StringBuilder();

			//foreach (var p in paths)
			//{
			//	var resourcePath = AppRoot + p.Replace('/', '\\');

			//	if (File.Exists(resourcePath) &&
			//			(System.IO.Path.GetExtension(p) == ".css" ||
			//			 System.IO.Path.GetExtension(p) == ".js"))
			//	{
			//		combinedFiles.AppendLine(File.ReadAllText(resourcePath));
			//	}
			//}

			//if (!debugMode)
			//{
			//	switch (extension)
			//	{
			//		case ".js":
			//			fileContentResult = new JavaScriptCompressor().Compress(combinedFiles.ToString());
			//			break;
			//		case ".css":
			//			fileContentResult = new CssCompressor().Compress(combinedFiles.ToString());
			//			break;
			//	}
			//}
			//else
			//	fileContentResult = combinedFiles.ToString();

			//EngineAppState.Bundles[name] = new Tuple<List<string>, string>(paths.ToList(), fileContentResult);
			throw new NotImplementedException();
		}

		// Helper bundles are a special mechanism that will allow HtmlHelpers to inject CSS or JS when
		// they are used. This also provides a nice way to package up controls that can be used over
		// and over.
		internal void AddHelperBundle(string name, string code)
		{
			//EngineSessionState.HelperBundles[name] = new StringBuilder(code);

			//if (!debugMode)
			//{
			//	var match = EngineAppState.CssorJsExtPattern.Match(name);

			//	if (match.Value.EndsWith(".js"))
			//	{
			//		if (!EngineSessionState.HelperBundles[name].ToString().Contains(code))
			//			EngineSessionState.HelperBundles[name].AppendLine(new JavaScriptCompressor().Compress(code));
			//	}
			//	else if (match.Value.EndsWith(".css"))
			//	{
			//		if (!EngineSessionState.HelperBundles[name].ToString().Contains(code))
			//			EngineSessionState.HelperBundles[name].AppendLine(new CssCompressor().Compress(code));
			//	}
			//}
			//else
			//{
			//	if (!EngineSessionState.HelperBundles[name].ToString().Contains(code))
			//		EngineSessionState.HelperBundles[name].AppendLine(code);
			//}
			throw new NotImplementedException();
		}

		// This is used to provide a way to get the file path so if we are in debug mode
		// we can ignore the bundles and instead place all of the real file includes in the
		// HTML head.
		internal string[] GetBundleFiles(string name)
		{
			//return (EngineAppState.Bundles.ContainsKey(name)) ? EngineAppState.Bundles[name].Item1.ToArray() : null;
			throw new NotImplementedException();
		}

		internal Dictionary<string, StringBuilder> GetHelperBundle()
		{
			//return EngineSessionState.HelperBundles;
			throw new NotImplementedException();
		}

		// Bindings are a poor mans IoC and even then not really. They just provide a mechanism
		// to predefine what parameters get used to invoke an action.
		internal void AddBinding(string controllerName, string actionName, object bindInstance)
		{
			//controllerName.ThrowIfArgumentNull();
			//actionName.ThrowIfArgumentNull();
			//bindInstance.ThrowIfArgumentNull();

			//if (!EngineSessionState.ActionBindings.ContainsKey(controllerName))
			//	EngineSessionState.ActionBindings[controllerName] = new Dictionary<string, List<object>>();

			//if (!EngineSessionState.ActionBindings[controllerName].ContainsKey(actionName))
			//	EngineSessionState.ActionBindings[controllerName][actionName] = new List<object>();

			//if (!EngineSessionState.ActionBindings[controllerName][actionName].Contains(bindInstance))
			//	EngineSessionState.ActionBindings[controllerName][actionName].Add(bindInstance);
			throw new NotImplementedException();
		}

		#region BINDINGS
		// Bindings can be a source of confusion. These methods tell the famework that you want
		// to pass certain objects in the parameter list of the action. If you forget to add these
		// bound parameters to your actions parameter list then upon navigating you'll receive an
		// HTTP 404.
		// 
		// The framework is not forgiving, the framework should probably spit out a message if
		// in debug mode that says "Hey, you reqested this action but we didn't find that and instead we found
		// this action that is similiar but your parameters are not correct."
		internal void AddBinding(string controllerName, string[] actionNames, object bindInstance)
		{
			//foreach (var actionName in actionNames)
			//	AddBinding(controllerName, actionName, bindInstance);
			throw new NotImplementedException();
		}

		internal void AddBinding(string controllerName, string[] actionNames, object[] bindInstances)
		{
			//foreach (var actionName in actionNames)
			//	foreach (var bindInstance in bindInstances)
			//		AddBinding(controllerName, actionName, bindInstance);
			throw new NotImplementedException();
		}

		internal void AddBindingForAllActions(string controllerName, object bindInstance)
		{
			//foreach (var actionName in GetControllerActionNames(controllerName))
			//	AddBinding(controllerName, actionName, bindInstance);
			throw new NotImplementedException();
		}

		internal void AddBindingsForAllActions(string controllerName, object[] bindInstances)
		{
			//foreach (var actionName in GetControllerActionNames(controllerName))
			//	foreach (var bindInstance in bindInstances)
			//		AddBinding(controllerName, actionName, bindInstance);
			throw new NotImplementedException();
		}

		internal List<object> GetBindings(string controllerName, string actionName, string alias, Type[] initializeTypes)
		{
			//var bindings = (EngineSessionState.ActionBindings.ContainsKey(controllerName) && EngineSessionState.ActionBindings[controllerName].ContainsKey(actionName)) ?
			//	EngineSessionState.ActionBindings[controllerName][actionName] : null;

			//if (bindings != null)
			//{
			//	var routeInfo = FindRoute(string.Format("/{0}", actionName), pathSegments);

			//	if (routeInfo != null && routeInfo.BoundToActionParams != null)
			//	{
			//		var boundActionParams = routeInfo.BoundToActionParams.Where(x => initializeTypes.Any(y => x.GetType() == y));

			//		foreach (var b in boundActionParams)
			//			b.Initialize(routeInfo);
			//	}
			//}

			//return bindings;
			throw new NotImplementedException();
		}
		#endregion

		internal RouteInfo FindRoute(string path)
		{
			//return FindRoute(path, pathSegments);
			throw new NotImplementedException();
		}

		internal RouteInfo FindRoute(string path, string[] urlParameters)
		{
			//path.ThrowIfArgumentNull();

			//RouteInfo result = null;

			//var routeSlice = GetRouteInfos(path).SelectMany(routeInfo => routeInfo.Aliases, (routeInfo, alias) =>
			//														new { routeInfo, alias }).Where(x => path == x.alias)
			//													 .OrderBy(x => x.routeInfo.Action.GetParameters().Length)
			//													 .ToList();

			//if (routeSlice.Any())
			//{
			//	var allParams = new List<object>()
			//		.Concat(routeSlice[0].routeInfo.BoundParams)
			//		.Concat(urlParameters.Skip(1))
			//		.Concat(routeSlice[0].routeInfo.DefaultParams)
			//		.ToList();

			//	Func<Dictionary<string, string>, object[]> getModelOrParams =
			//		pl =>
			//		{
			//			var model = PayloadToModel(pl);
			//			var payloadParams = (model != null) ? new object[] { model } : pl.Values.Where(x => !EngineAppState.AntiForgeryTokens.Contains(x)).ToArray();
			//			return payloadParams;
			//		};

			//	switch (RequestType)
			//	{
			//		case "post":
			//			allParams.AddRange(getModelOrParams(form));
			//			break;
			//		case "delete":
			//		case "put":
			//			allParams.AddRange(getModelOrParams(payload));
			//			break;
			//	}

			//	var finalParams = allParams.ToArray();

			//	// This loop is pretty horrible and needs to be revised!
			//	foreach (var routeInfo in routeSlice
			//		.Where(x => x.routeInfo.Action.GetParameters().Count() >= finalParams.Count()).Select(x => x.routeInfo))
			//	{
			//		var finalParamTypes = finalParams.Select(x => x.GetType()).ToArray();
			//		var actionParamTypes = routeInfo.Action.GetParameters()
			//																							.Where(x => x.ParameterType.GetInterface("IActionFilterResult") == null)
			//																							.Select(x => x.ParameterType).ToArray();

			//		if (routeInfo.ActionParamTransforms != null && finalParamTypes.Count() == actionParamTypes.Count())
			//			foreach (var apt in routeInfo.ActionParamTransforms)
			//				finalParamTypes[apt.Item2] = actionParamTypes[apt.Item2];

			//		for (var i = 0; i < routeInfo.BoundParams.Count(); i++)
			//			if (actionParamTypes[i].IsInterface && finalParamTypes[i].GetInterface(actionParamTypes[i].Name) != null)
			//				finalParamTypes[i] = actionParamTypes[i];

			//		if (finalParamTypes.Intersect(actionParamTypes).Count() < finalParamTypes.Count())
			//		{
			//			for (var i = 0; i < finalParamTypes.Count(); i++)
			//			{
			//				if (finalParamTypes[i] == actionParamTypes[i]) continue;

			//				finalParams[i] = Convert.ChangeType(finalParams[i], actionParamTypes[i]);
			//				finalParamTypes[i] = actionParamTypes[i];
			//			}
			//		}

			//		var intersection = finalParamTypes.Except(actionParamTypes);

			//		if (actionParamTypes.Except(finalParamTypes).Any())
			//		{
			//			finalParamTypes = actionParamTypes;
			//			Array.Resize(ref finalParams, finalParamTypes.Length);
			//		}

			//		if (!finalParamTypes.SequenceEqual(actionParamTypes)) continue;

			//		routeInfo.ActionUrlParams = finalParams;
			//		result = routeInfo;
			//		_currentRoute = routeInfo;
			//		break;
			//	}
			//}

			//return result;
			throw new NotImplementedException();
		}

		internal void RemoveRoute(string alias)
		{
			//var routeInfo = EngineAppState.RouteInfos.FirstOrDefault(x => x.Aliases.FirstOrDefault(a => a == alias) != null && x.Dynamic);

			//if (routeInfo != null)
			//	EngineAppState.RouteInfos.Remove(routeInfo);
			throw new NotImplementedException();
		}

		internal void AddRoute(List<RouteInfo> routeInfos, Controller c, MethodInfo action, List<string> aliases, string defaultParams, bool dynamic)
		{
			//if (EngineAppState.RouteInfos.FirstOrDefault(x => x.Action.GetParameters().Count() == action.GetParameters().Count() && x.Aliases.Except(aliases).Count() == 0) != null)
			//	return;

			//if (action != null)
			//{
			//	List<object> bindings = null;
			//	var rta = (HttpAttribute)action.GetCustomAttributes(typeof(HttpAttribute), false).FirstOrDefault();

			//	if (EngineSessionState.ActionBindings.ContainsKey(c.GetType().Name))
			//		if (EngineSessionState.ActionBindings[c.GetType().Name].ContainsKey(action.Name))
			//			bindings = EngineSessionState.ActionBindings[c.GetType().Name][action.Name];

			//	var actionParameterInfo = GetActionParameterTransforms(action.GetParameters(), bindings);

			//	routeInfos.Add(new RouteInfo()
			//	{
			//		Aliases = aliases,
			//		Action = action,
			//		Controller = c,
			//		RequestTypeAttribute = rta,
			//		BoundParams = (bindings != null) ? bindings.ToArray() : new object[] { },
			//		BoundToActionParams = (bindings != null) ? bindings.Where(x => x.GetType().GetInterface("IBoundToAction") != null).Cast<IBoundToAction>().ToArray() : null,
			//		DefaultParams = (!string.IsNullOrEmpty(defaultParams)) ? defaultParams.Split('/').ConvertToObjectTypeArray() : new object[] { },
			//		ActionParamTransforms = actionParameterInfo.ActionParamTransforms,
			//		CachedActionParamTransformInstances = actionParameterInfo.ActionParamTransformInstances,
			//		Dynamic = dynamic
			//	});
			//}
			throw new NotImplementedException();
		}

		internal void AddRoute(List<RouteInfo> routeInfos, string alias, string controllerName, string actionName, string defaultParams, bool dynamic)
		{
			//alias.ThrowIfArgumentNull();
			//controllerName.ThrowIfArgumentNull();
			//actionName.ThrowIfArgumentNull();

			//var c = EngineSessionState.Controllers.FirstOrDefault(x => x.GetType().Name == controllerName);

			//if (c != null)
			//{
			//	var action = c.GetType().GetMethods().FirstOrDefault(x => x.GetCustomAttributes(typeof(HttpAttribute), false).Any() && x.Name == actionName);

			//	if (action != null)
			//		AddRoute(routeInfos, c, action, new List<string> { alias }, defaultParams, dynamic);
			//}
			throw new NotImplementedException();
		}

		// If you are creating dynamic routes it may be useful to obtain a list of all of the
		// routes the framework knows about, especially for debugging purposes.
		internal List<string> GetAllRouteAliases()
		{
			//return EngineAppState.RouteInfos.SelectMany(x => x.Aliases).ToList();
			throw new NotImplementedException();
		}

		internal string CreateAntiForgeryToken()
		{
			//var token = CreateToken();
			//EngineAppState.AntiForgeryTokens.Add(token);

			//return token;
			throw new NotImplementedException();
		}

		// The framework per se doesn't really care how you determine who is able to login
		// the framework only cares about who *you* think should be logged in for the purposes
		// of keeping track of the users between requests.
		internal void LogOn(string id, string[] roles, object archeType = null)
		{
			//id.ThrowIfArgumentNull();
			//roles.ThrowIfArgumentNull();

			//if (CurrentUser != null && CurrentUser.SessionId == SessionId)
			//	return;

			//var alreadyLoggedInWithDiffSession = EngineAppState.Users.FirstOrDefault(x => x.Name == id);

			//if (alreadyLoggedInWithDiffSession != null)
			//	EngineAppState.Users.Remove(alreadyLoggedInWithDiffSession);

			//var authCookie = new AuthCookie()
			//{
			//	Id = id,
			//	AuthToken = CreateToken(),
			//	Expiration = DateTime.Now.Add(TimeSpan.FromHours(8))
			//};

			//var u = new User()
			//{
			//	AuthenticationCookie = authCookie,
			//	SessionId = SessionId,
			//	ClientCertificate = ClientCertificate,
			//	IpAddress = IpAddress,
			//	LogOnDate = DateTime.Now,
			//	Name = id,
			//	ArcheType = archeType,
			//	Roles = roles.ToList()
			//};

			//EngineAppState.Users.Add(u);
			//CurrentUser = u;
			throw new NotImplementedException();
		}

		internal bool LogOff()
		{
			//if (CurrentUser == null || !EngineAppState.Users.Remove(CurrentUser)) return false;

			//CurrentUser = null;

			//return true;
			throw new NotImplementedException();
		}

		// Sessions within the controller are sandboxed.
		internal void AddControllerSession(string key, object value)
		{
			//if (!string.IsNullOrEmpty(key))
			//	EngineSessionState.ControllersSession[key] = value;
			throw new NotImplementedException();
		}

		internal object GetControllerSession(string key)
		{
			//return (EngineSessionState.ControllersSession.ContainsKey(key)) ? EngineSessionState.ControllersSession[key] : null;
			throw new NotImplementedException();
		}

		internal void AbandonControllerSession()
		{
			//EngineSessionState.ControllersSession = null;
			throw new NotImplementedException();
		}

		internal string MapPath(string path)
		{
			//return AppRoot + path.Replace('/', '\\');
			throw new NotImplementedException();
		}
		#endregion
	}
	#endregion

	#region ACTION FILTER
	public interface IActionFilterResult { }

	// Action filters are special classes that subclass this attribute and are used by denoting 
	// a controller action with your subclassed attribute. Action filters can optionally pass
	// parameters to the action or redirect to other actions or even perform logon or logoff
	// capabilities.
	[AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
	public abstract class ActionFilterAttribute : Attribute
	{
		private Engine _engine;
		public RouteInfo DivertRoute { get; set; }
		internal Controller Controller { get; set; }
		public IActionFilterResult FilterResult { get; set; }
		//public User CurrentUser { get { return _engine.CurrentUser; } }
		public abstract void OnFilter(RouteInfo routeInfo);

		internal void Init(Engine e)
		{
			_engine = e;
		}

		#region WRAPPERS FOR ENGINE METHODS
		protected RouteInfo FindRoute(string path)
		{
			//return _engine.FindRoute(path);
			throw new NotImplementedException();
		}
		protected RouteInfo FindRoute(string path, string[] urlParameters)
		{
			//return _engine.FindRoute(path, urlParameters);
			throw new NotImplementedException();
		}
		protected void Redirect(string alias)
		{
			//_engine.ResponseRedirect(alias, false);
			throw new NotImplementedException();
		}
		protected void RedirectOnly(string alias)
		{
			//_engine.ResponseRedirect(alias, true);
			throw new NotImplementedException();
		}
		protected void LogOn(string id, string[] roles, object archeType = null)
		{
			//_engine.LogOn(id, roles, archeType);
			throw new NotImplementedException();
		}
		protected void LogOff()
		{
			//_engine.LogOff();
			throw new NotImplementedException();
		}
		protected void AddApplication(string key, object value)
		{
			//_engine.AddApplication(key, value);
			throw new NotImplementedException();
		}
		protected object GetApplication(string key)
		{
			//return _engine.GetApplication(key);
			throw new NotImplementedException();
		}
		protected void AddSession(string key, object value)
		{
			//_engine.AddControllerSession(key, value);
			throw new NotImplementedException();
		}
		protected object GetSession(string key)
		{
			//return _engine.GetControllerSession(key);
			throw new NotImplementedException();
		}
		protected void AddCache(string key, object value, DateTime expiresOn)
		{
			//_engine.AddCache(key, value, expiresOn);
			throw new NotImplementedException();
		}
		protected object GetCache(string key)
		{
			//return _engine.GetCache(key);
			throw new NotImplementedException();
		}
		protected void RemoveCache(string key)
		{
			//_engine.RemoveCache(key);
			throw new NotImplementedException();
		}
		protected void AbandonSession()
		{
			//_engine.AbandonControllerSession();
			throw new NotImplementedException();
		}
		protected string GetQueryString(string key, bool validate)
		{
			//return _engine.GetQueryString(key, validate);
			throw new NotImplementedException();
		}
		protected string MapPath(string path)
		{
			//return _engine.MapPath(path);
			throw new NotImplementedException();
		}
		protected void AddCookie(HttpCookie cookie)
		{
			//_engine.AddCookie(cookie);
			throw new NotImplementedException();
		}
		protected HttpCookie GetCookie(string name)
		{
			//return _engine.GetCookie(name);
			throw new NotImplementedException();
		}
		protected void RemoveCookie(string name)
		{
			//_engine.RemoveCookie(name);
			throw new NotImplementedException();
		}
		#endregion
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
		PreAction,
		PostAction,
		PreRoute,
		PostRoute,
		Static,
		PassedSecurity,
		FailedSecurity,
		MissingRoute,
		Error
	}

	// Because the framework needs to know who's logged in and what roles they have it may
	// be beneficial to add a handler for the check roles event so that before a secure action
	// is invoked we make sure that the frameworks list of roles for this user is actually
	// still inline with what roles the user actually has.
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

	// All of the base controller infrastructure is defined here. This is the starting point
	// for the Front Controller and the Controller classes.
	public abstract class BaseController
	{
		internal Engine Engine { get; set; }
		//public Dictionary<string, object> Request { get { return Engine.Request.ToDictionary(x => x.Key, x => x.Value); } }
		//public User CurrentUser { get { return Engine.CurrentUser; } }
		//public X509Certificate2 ClientCertificate { get { return Engine.ClientCertificate; } }
		//public Uri Url { get { return Engine.Url; } }
		//public string RequestType { get { return Engine.RequestType; } }
		//public string Identity { get { return Engine.Identity; } }

		protected event EventHandler OnInit;
		protected event EventHandler<CheckRolesHandlerEventArgs> OnCheckRoles;

		internal bool RaiseCheckRoles(CheckRolesHandlerEventArgs checkRolesHandlerEventArgs)
		{
			if (OnCheckRoles != null)
			{
				OnCheckRoles(this, checkRolesHandlerEventArgs);

				return checkRolesHandlerEventArgs.Result;
			}

			return false;
		}

		internal void RaiseEvent(EventType eventType)
		{
			if (eventType != EventType.OnInit || OnInit == null) return;

			OnInit(this, null);
			// we only want OnInit called once per controller instantiation
			OnInit = null;
		}

		#region WRAPPERS AROUND ENGINE METHS/PROPS
		protected RouteInfo FindRoute(string path)
		{
			//return Engine.FindRoute(path);
			throw new NotImplementedException();
		}
		protected RouteInfo FindRoute(string path, string[] urlParameters)
		{
			//return Engine.FindRoute(path, urlParameters);
			throw new NotImplementedException();
		}
		protected void AddRoute(string alias, string controllerName, string actionName, string defaultParams)
		{
			//Engine.AddRoute(Engine.EngineAppState.RouteInfos, alias, controllerName, actionName, defaultParams, true);
			throw new NotImplementedException();
		}
		protected void RemoveRoute(string alias)
		{
			//Engine.RemoveRoute(alias);
			throw new NotImplementedException();
		}
		protected void AddBinding(string actionName, object bindInstance)
		{
			//Engine.AddBinding(this.GetType().Name, actionName, bindInstance);
			throw new NotImplementedException();
		}
		protected void AddBinding(string[] actionNames, object bindInstance)
		{
			//Engine.AddBinding(this.GetType().Name, actionNames, bindInstance);
			throw new NotImplementedException();
		}
		protected void AddBinding(string[] actionNames, object[] bindInstances)
		{
			//Engine.AddBinding(this.GetType().Name, actionNames, bindInstances);
			throw new NotImplementedException();
		}
		protected void AddBindingForAllActions(string controllerName, object bindInstance)
		{
			//Engine.AddBindingForAllActions(controllerName, bindInstance);
			throw new NotImplementedException();
		}
		protected void AddBindingsForAllActions(string controllerName, object[] bindInstances)
		{
			//Engine.AddBindingsForAllActions(controllerName, bindInstances);
			throw new NotImplementedException();
		}
		protected void AddBindingForAllActions(object bindInstance)
		{
			//Engine.AddBindingForAllActions(this.GetType().Name, bindInstance);
			throw new NotImplementedException();
		}
		protected void AddBindingsForAllActions(object[] bindInstances)
		{
			//Engine.AddBindingsForAllActions(this.GetType().Name, bindInstances);
			throw new NotImplementedException();
		}
		protected void AddBundles(Dictionary<string, string[]> bundles)
		{
			//Engine.AddBundles(bundles);
			throw new NotImplementedException();
		}
		protected void AddBundle(string name, string[] paths)
		{
			//Engine.AddBundle(name, paths);
			throw new NotImplementedException();
		}
		public void AddHelperBundle(string name, string data)
		{
			//Engine.AddHelperBundle(name, data);
			throw new NotImplementedException();
		}
		protected void LogOn(string id, string[] roles, object archeType = null)
		{
			//Engine.LogOn(id, roles, archeType);
			throw new NotImplementedException();
		}
		protected void LogOff()
		{
			//Engine.LogOff();
			throw new NotImplementedException();
		}
		protected List<string> GetAllRouteAliases()
		{
			//return Engine.GetAllRouteAliases();
			throw new NotImplementedException();
		}
		protected void Redirect(string path)
		{
			//Engine.ResponseRedirect(path, false);
			throw new NotImplementedException();
		}
		protected void Redirect(string alias, params string[] parameters)
		{
			//Engine.ResponseRedirect(string.Format("{0}/{1}", alias, string.Join("/", parameters)), false);
			throw new NotImplementedException();
		}
		protected void RedirectOnly(string path)
		{
			//Engine.ResponseRedirect(path, true);
			throw new NotImplementedException();
		}
		protected void ProtectFile(string path, string roles)
		{
			//Engine.ProtectFile(path, roles);
			throw new NotImplementedException();
		}
		public void AddApplication(string key, object value)
		{
			//Engine.AddApplication(key, value);
			throw new NotImplementedException();
		}
		public object GetApplication(string key)
		{
			//return Engine.GetApplication(key);
			throw new NotImplementedException();
		}
		public void AddSession(string key, object value)
		{
			//Engine.AddControllerSession(key, value);
			throw new NotImplementedException();
		}
		public object GetSession(string key)
		{
			//return Engine.GetControllerSession(key);
			throw new NotImplementedException();
		}
		public void AddCache(string key, object value, DateTime expiresOn)
		{
			//Engine.AddCache(key, value, expiresOn);
			throw new NotImplementedException();
		}
		public object GetCache(string key)
		{
			//return Engine.GetCache(key);
			throw new NotImplementedException();
		}
		public void RemoveCache(string key)
		{
			//Engine.RemoveCache(key);
			throw new NotImplementedException();
		}
		public void AbandonSession()
		{
			//Engine.AbandonControllerSession();
			throw new NotImplementedException();
		}
		protected string GetQueryString(string key, bool validate)
		{
			//return Engine.GetQueryString(key, validate);
			throw new NotImplementedException();
		}
		protected string MapPath(string path)
		{
			//return Engine.MapPath(path);
			throw new NotImplementedException();
		}
		protected void AddCookie(HttpCookie cookie)
		{
			//Engine.AddCookie(cookie);
			throw new NotImplementedException();
		}
		protected HttpCookie GetCookie(string name)
		{
			//return Engine.GetCookie(name);
			throw new NotImplementedException();
		}
		protected void RemoveCookie(string name)
		{
			//Engine.RemoveCookie(name);
			throw new NotImplementedException();
		}
		protected string CreateAntiForgeryToken()
		{
			//return Engine.CreateAntiForgeryToken();
			throw new NotImplementedException();
		}
		#endregion
	}

	// The front controller is for all intents and purposes a master controller that can intercept
	// requests and perform various functions before a controller action is invoked. 
	public abstract class FrontController : BaseController
	{
		protected event EventHandler<RouteHandlerEventArgs> OnPreActionEvent,
			OnPostActionEvent, OnStaticRouteEvent, OnPreRouteDeterminationEvent, OnPostRouteDeterminationEvent,
			OnPassedSecurityEvent, OnFailedSecurityEvent, OnMissingRouteEvent, OnErrorEvent;

		internal static FrontController CreateInstance(Type type, Engine engine)
		{
			var controller = (FrontController)Activator.CreateInstance(type);
			controller.Engine = engine;

			return controller;
		}

		protected List<object> GetBindings(string controllerName, string actionName, string alias, Type[] initializeTypes)
		{
			//return Engine.GetBindings(controllerName, actionName, alias, initializeTypes);
			throw new NotImplementedException();
		}

		internal RouteInfo RaiseEvent(RouteHandlerEventType type, string path, RouteInfo routeInfo, object data = null)
		{
			var route = routeInfo;
			var args = new RouteHandlerEventArgs()
			{
				Path = path,
				RouteInfo = routeInfo,
				Data = data
			};

			switch (type)
			{
				case RouteHandlerEventType.PreAction:
					if (OnPreActionEvent != null)
						OnPreActionEvent(this, args);
					break;

				case RouteHandlerEventType.PostAction:
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

	public abstract class Controller : BaseController, IController
	{
		internal string PartitionName { get; set; }
		internal HttpAttribute HttpAttribute { get; set; }
		public Dictionary<string, string> ViewTags { get; private set; }
		public Dictionary<string, Dictionary<string, string>> FragTags { get; private set; }
		public dynamic ViewBag { get; private set; }
		public dynamic FragBag { get; private set; }

		protected event EventHandler<RouteHandlerEventArgs> OnPreAction, OnPostAction;

		internal void InitializeViewTags()
		{
			ViewTags = new Dictionary<string, string>();
			FragTags = new Dictionary<string, Dictionary<string, string>>();
			FragBag = new DynamicDictionary();
			ViewBag = new DynamicDictionary();
		}

		internal void Init(Engine engine)
		{
			this.Engine = engine;

			InitializeViewTags();

			var partitionAttrib = (PartitionAttribute)GetType().GetCustomAttributes(false).FirstOrDefault(x => x is PartitionAttribute);

			if (partitionAttrib != null)
				PartitionName = partitionAttrib.Name;
		}

		internal static Controller CreateInstance(Type type, Engine engine)
		{
			var controller = (Controller)Activator.CreateInstance(type);
			controller.Init(engine);

			if (string.IsNullOrEmpty(controller.PartitionName))
				controller.PartitionName = "Views";

			return controller;
		}

		private Dictionary<string, string> GetTagsDictionary(Dictionary<string, string> tags, dynamic tagBag, string subDict)
		{
			var result = tags;

			if (!tagBag.IsEmpty())
			{
				Dictionary<string, object> bag = string.IsNullOrEmpty(subDict) ? tagBag.AsDictionary() : tagBag.AsDictionary(subDict);

				if (bag != null)
					result = bag.ToDictionary(k => k.Key, k => (k.Value != null) ? k.Value.ToString() : string.Empty);
			}

			return result;
		}

		internal void RaiseEvent(RouteHandlerEventType type, string path, RouteInfo routeInfo)
		{
			var args = new RouteHandlerEventArgs()
			{
				Path = path,
				RouteInfo = routeInfo,
				Data = null
			};

			switch (type)
			{
				case RouteHandlerEventType.PreAction:
					if (OnPreAction != null)
						OnPreAction(this, args);
					break;

				case RouteHandlerEventType.PostAction:
					if (OnPostAction != null)
						OnPostAction(this, args);
					break;
			}
		}

		#region RENDER FRAGMENT
		public string RenderFragment(string fragmentName)
		{
			return RenderFragment(fragmentName, null, null, null, null);
		}

		public string RenderFragment(string fragmentName, DateTime expiresOn)
		{
			return RenderFragment(fragmentName, null, null, null, expiresOn);
		}

		public string RenderFragment(string fragmentName, Func<bool> canViewFragment)
		{
			return RenderFragment(fragmentName, null, null, canViewFragment, null);
		}

		public string RenderFragment(string fragmentName, string forRoles)
		{
			return RenderFragment(fragmentName, null, forRoles, null, null);
		}

		public string RenderFragment(string fragmentName, Dictionary<string, string> fragTags)
		{
			return RenderFragment(fragmentName, fragTags, null, null, null);
		}

		private string RenderFragment(string fragmentName, Dictionary<string, string> fragTags, string forRoles, Func<bool> canViewFragment, DateTime? expiresOn)
		{
			//if (expiresOn != null)
			//{
			//	var cr = Engine.GetCache(fragmentName);

			//	if (cr != null) return cr as string;
			//}

			//if (!string.IsNullOrEmpty(forRoles) && CurrentUser != null && !CurrentUser.IsInRole(forRoles))
			//	return string.Empty;

			//if (canViewFragment != null && !canViewFragment())
			//	return string.Empty;

			//if (fragTags == null)
			//	fragTags = GetTagsDictionary(FragTags.ContainsKey(fragmentName) ? FragTags[fragmentName] : null, FragBag, fragmentName);

			//var result = Engine.EngineAppState.ViewEngine.LoadView(string.Format("{0}/{1}/Fragments/{2}", PartitionName, this.GetType().Name, fragmentName), fragTags);

			//if (expiresOn != null)
			//	Engine.AddCache(fragmentName, result, expiresOn.Value);

			//return result;

			throw new NotImplementedException();
		}

		public string RenderPartial(string partialName)
		{
			return RenderPartial(partialName, null);
		}

		public string RenderPartial(string partialName, Dictionary<string, string> tags)
		{
			//return Engine.EngineAppState.ViewEngine.LoadView(string.Format("{0}/{1}/Shared/{3}", PartitionName, this.GetType().Name, partialName), tags ?? GetTagsDictionary(ViewTags, ViewBag, null));
			throw new NotImplementedException();
		}
		#endregion

		#region VIEW
		public HtmlStringResult HtmlView(string html)
		{
			var result = new HtmlStringResult(html);
			InitializeViewTags();
			return result;
		}

		public ViewResult View()
		{
			var view = HttpAttribute.View;
			var stackFrame = new StackFrame(1);
			var result = View(this.GetType().Name, (string.IsNullOrEmpty(view)) ? stackFrame.GetMethod().Name : view);

			InitializeViewTags();

			return result;
		}

		public ViewResult View(string name)
		{
			var result = View(this.GetType().Name, name);
			InitializeViewTags();
			return result;
		}

		public ViewResult View(string controllerName, string actionName)
		{
			//var result = new ViewResult(Engine.EngineAppState.ViewEngine, GetTagsDictionary(ViewTags, ViewBag, null), PartitionName, controllerName, actionName);
			//InitializeViewTags();
			//return result;

			throw new NotImplementedException();
		}

		public FileResult View(string fileName, byte[] fileBytes, string contentType)
		{
			var result = new FileResult(fileName, fileBytes, contentType);
			InitializeViewTags();
			return result;
		}

		public ViewResult Partial(string name)
		{
			var result = Partial(this.GetType().Name, name);
			InitializeViewTags();
			return result;
		}

		public ViewResult Partial(string controllerName, string actionName)
		{
			//var result = new ViewResult(Engine.EngineAppState.ViewEngine, GetTagsDictionary(ViewTags, ViewBag, null), PartitionName, controllerName, actionName, "Shared/");
			//InitializeViewTags();
			//return result;

			throw new NotImplementedException();
		}
		#endregion
	}
	#endregion

	#region VIEW ENGINE -> TO BE REWRITTEN

	#region VIEW RESULTS
	public class ViewResponse
	{
		public int HttpStatus { get; set; }
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
		private readonly string _view;
		private readonly Dictionary<string, string> _headers;

		public ViewResult(IViewEngine viewEngine,
											Dictionary<string, string> viewTags,
											string partitionName,
											string controllerName,
											string viewName,
											string typeHint = "")
		{
			_view = viewEngine.LoadView(string.Format("{0}/{1}/{2}{3}", partitionName, controllerName, typeHint, viewName), viewTags);
			_headers = new Dictionary<string, string>();
			_headers["Cache-Control"] = "no-cache";
			_headers["Pragma"] = "no-cache";
			_headers["Expires"] = "-1";
		}

		public ViewResponse Render()
		{
			return (string.IsNullOrEmpty(_view)) ? null :
				new ViewResponse() { ContentType = "text/html", Content = _view, Headers = _headers };
		}
	}

	public class FileResult : IViewResult
	{
		private readonly byte[] _file;
		private readonly string _contentType;
		private readonly Dictionary<string, string> _headers = new Dictionary<string, string>();

		public FileResult(string name, string data)
			: this(name, Encoding.UTF8.GetBytes(data), null) { }

		public FileResult(string name, byte[] data, string contentType)
		{
			var fileExtension = Path.GetExtension(name);

			if (fileExtension != null && (!string.IsNullOrEmpty(contentType)
																		|| HttpAdapterConstants.MimeTypes.ContainsKey(fileExtension)))
			{
				contentType = HttpAdapterConstants.MimeTypes[fileExtension];
			}

			_contentType = contentType;
			_file = data;

			_headers["content-disposition"] = string.Format("attachment;filename=\"{0}\"", name);
		}

		public FileResult(Regex allowedFilePattern, string path)
		{
			var fileExtension = Path.GetExtension(path);

			if (File.Exists(path) || allowedFilePattern.IsMatch(path))
			{
				if (HttpAdapterConstants.MimeTypes.ContainsKey(fileExtension))
					_contentType = HttpAdapterConstants.MimeTypes[fileExtension];

				_file = File.ReadAllBytes(path);
			}
		}

		public ViewResponse Render()
		{
			if (_file == null)
				return null;

			_headers["Cache-Control"] = string.Format("public, max-age={0}", 600);
			_headers["Expires"] = DateTime.Now.Add(new TimeSpan(0, 0, 10, 0, 0)).ToUniversalTime().ToString("r");

			return new ViewResponse()
			{
				Content = _file,
				ContentType = _contentType,
				Headers = _headers
			};
		}
	}

	// This is seriously naive and only does the most basic serialization. If there are
	// other needs that this does not meet then the StringResult is probably a better fit.
	public class JsonResult : IViewResult
	{
		private readonly string _json;

		public JsonResult(object data)
		{
			_json = JsonConvert.SerializeObject(data);
		}

		public ViewResponse Render()
		{
			return new ViewResponse() { Content = _json, ContentType = "application/json" };
		}
	}

	// The intent here is that the JsonResult is naive and only does the most basic conversion
	// if that doesn't meet your needs you can use this to return any string as a view result.
	// Any formatting needed can be done by the user and the string can be returned as a result.
	public class StringResult : IViewResult
	{
		private readonly string _value;
		private readonly string _contentType;
		private readonly Dictionary<string, string> _headers;

		public StringResult(string value) : this(value, "text/plain", null) { }

		public StringResult(string value, string contentType, Dictionary<string, string> headers)
		{
			_value = value;
			_contentType = contentType;

			if (headers == null)
				_headers = new Dictionary<string, string>();
		}

		public ViewResponse Render()
		{
			return new ViewResponse() { Headers = _headers, Content = _value, ContentType = _contentType };
		}
	}

	public class HtmlStringResult : IViewResult
	{
		private readonly Dictionary<string, string> _headers;
		private readonly string _result;

		public HtmlStringResult(string result)
		{
			_headers = new Dictionary<string, string>();
			_headers["Cache-Control"] = "no-cache";
			_headers["Pragma"] = "no-cache";
			_headers["Expires"] = "-1";

			_result = result;
		}

		public ViewResponse Render()
		{
			return (string.IsNullOrEmpty(_result)) ? null :
				new ViewResponse() { ContentType = "text/html", Content = _result, Headers = _headers };
		}
	}
	#endregion

	#region VIEW ENGINE
	#region INTERFACES AND ENUMS
	// This determines at what point the view compiler runs the particular 
	// transformation on the template.
	internal enum DirectiveProcessType { Compile, AfterCompile, Render }

	internal interface IViewCompiler
	{
		List<TemplateInfo> CompileAll();
		TemplateInfo Compile(string fullName);
		TemplateInfo Render(string fullName, Dictionary<string, string> tags);
	}

	public interface IViewEngine
	{
		string LoadView(string fullName, Dictionary<string, string> tags);
		string GetCache();
		bool CacheUpdated { get; }
	}
	#endregion

	#region DIRECTIVES AND SUBSTITUTIONS
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

	internal class HeadSubstitution : IViewCompilerSubstitutionHandler
	{
		private static readonly Regex HeadBlockRe = new Regex(@"\[\[(?<block>[\s\S]+?)\]\]", RegexOptions.Compiled);
		private const string HeadDirective = "%%Head%%";

		public DirectiveProcessType Type { get; private set; }

		public HeadSubstitution()
		{
			Type = DirectiveProcessType.Compile;
		}

		public StringBuilder Process(StringBuilder content)
		{
			MatchCollection heads = HeadBlockRe.Matches(content.ToString());

			if (heads.Count > 0)
			{
				var headSubstitutions = new StringBuilder();

				foreach (Match head in heads)
				{
					headSubstitutions.Append(Regex.Replace(head.Groups["block"].Value, @"^(\s+)", string.Empty, RegexOptions.Multiline));
					content.Replace(head.Value, string.Empty);
				}

				content.Replace(HeadDirective, headSubstitutions.ToString());
			}

			content.Replace(HeadDirective, string.Empty);

			return content;
		}
	}

	internal class AntiForgeryTokenSubstitution : IViewCompilerSubstitutionHandler
	{
		private const string TokenName = "%%AntiForgeryToken%%";
		private readonly Func<string> _createAntiForgeryToken;

		public DirectiveProcessType Type { get; private set; }

		public AntiForgeryTokenSubstitution(Func<string> createAntiForgeryToken)
		{
			this._createAntiForgeryToken = createAntiForgeryToken;

			Type = DirectiveProcessType.Render;
		}

		public StringBuilder Process(StringBuilder content)
		{
			var tokens = Regex.Matches(content.ToString(), TokenName)
												.Cast<Match>()
												.Select(m => new { Start = m.Index, End = m.Length })
												.Reverse();

			foreach (var t in tokens)
				content.Replace(TokenName, _createAntiForgeryToken(), t.Start, t.End);

			return content;
		}
	}

	internal class CommentSubstitution : IViewCompilerSubstitutionHandler
	{
		private static readonly Regex CommentBlockRe = new Regex(@"\@\@(?<block>[\s\S]+?)\@\@", RegexOptions.Compiled);

		public DirectiveProcessType Type { get; private set; }

		public CommentSubstitution()
		{
			Type = DirectiveProcessType.Compile;
		}

		public StringBuilder Process(StringBuilder content)
		{
			return new StringBuilder(CommentBlockRe.Replace(content.ToString(), string.Empty));
		}
	}

	internal class MasterPageDirective : IViewCompilerDirectiveHandler
	{
		private const string TokenName = "%%View%%";
		public DirectiveProcessType Type { get; private set; }

		public MasterPageDirective()
		{
			Type = DirectiveProcessType.Compile;
		}

		public StringBuilder Process(ViewCompilerDirectiveInfo directiveInfo)
		{
			if (directiveInfo.Directive != "Master") return directiveInfo.Content;

			var finalPage = new StringBuilder();

			var masterPageName = directiveInfo.DetermineKeyName(directiveInfo.Value);
			var masterPageTemplate = directiveInfo.ViewTemplates
				.FirstOrDefault(x => x.FullName == masterPageName)
				.Template;

			directiveInfo.AddPageDependency(masterPageName);

			finalPage.Append(masterPageTemplate);
			finalPage.Replace(TokenName, directiveInfo.Content.ToString());
			finalPage.Replace(directiveInfo.Match.Groups[0].Value, string.Empty);

			return finalPage;
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
			if (directiveInfo.Directive != "Partial") return directiveInfo.Content;

			var partialPageName = directiveInfo.DetermineKeyName(directiveInfo.Value);
			var partialPageTemplate = directiveInfo.ViewTemplates
				.FirstOrDefault(x => x.FullName == partialPageName)
				.Template;

			directiveInfo.Content.Replace(directiveInfo.Match.Groups[0].Value, partialPageTemplate);

			return directiveInfo.Content;
		}
	}

	internal class HelperBundleDirective : IViewCompilerSubstitutionHandler
	{
		public DirectiveProcessType Type { get; private set; }
		private const string HelperBundlesDirective = "%%HelperBundles%%";
		private readonly Func<Dictionary<string, StringBuilder>> _getHelperBundles;
		private readonly string _sharedResourceFolderPath;
		private const string CssIncludeTag = "<link href=\"{0}\" rel=\"stylesheet\" type=\"text/css\" />";
		private const string JsIncludeTag = "<script src=\"{0}\" type=\"text/javascript\"></script>";

		public HelperBundleDirective(string sharedResourceFolderPath, Func<Dictionary<string, StringBuilder>> getHelperBundles)
		{
			Type = DirectiveProcessType.Render;
			this._getHelperBundles = getHelperBundles;
			this._sharedResourceFolderPath = sharedResourceFolderPath;
		}

		public string ProcessBundleLink(string bundlePath)
		{
			var extension = Path.GetExtension(bundlePath).Substring(1).ToLower();

			if (string.IsNullOrEmpty(extension)) return null;

			var tag = string.Empty;
			var isAPath = bundlePath.Contains('/') ? true : false;
			var modifiedBundlePath = bundlePath;

			if (!isAPath)
				modifiedBundlePath = string.Join("/", _sharedResourceFolderPath, extension, bundlePath);

			switch (extension)
			{
				case "css":
					tag = string.Format(CssIncludeTag, modifiedBundlePath);
					break;
				case "js":
					tag = string.Format(JsIncludeTag, modifiedBundlePath);
					break;
			}

			return tag;
		}

		public StringBuilder Process(StringBuilder content)
		{
			if (!content.ToString().Contains(HelperBundlesDirective)) return content;

			var fileLinkBuilder = new StringBuilder();

			foreach (var bundlePath in _getHelperBundles().Keys)
				fileLinkBuilder.AppendLine(ProcessBundleLink(bundlePath));

			content.Replace(HelperBundlesDirective, fileLinkBuilder.ToString());

			return content;
		}
	}

	internal class BundleDirective : IViewCompilerDirectiveHandler
	{
		private readonly bool _debugMode;
		private readonly string _sharedResourceFolderPath;
		private readonly Func<string, string[]> _getBundleFiles;
		private readonly Dictionary<string, string> _bundleLinkResults;
		private const string CssIncludeTag = "<link href=\"{0}\" rel=\"stylesheet\" type=\"text/css\" />";
		private const string JsIncludeTag = "<script src=\"{0}\" type=\"text/javascript\"></script>";

		public DirectiveProcessType Type { get; private set; }

		public BundleDirective(bool debugMode, string sharedResourceFolderPath,
			Func<string, string[]> getBundleFiles)
		{
			this._debugMode = debugMode;
			this._sharedResourceFolderPath = sharedResourceFolderPath;
			this._getBundleFiles = getBundleFiles;

			_bundleLinkResults = new Dictionary<string, string>();

			Type = DirectiveProcessType.Render;
		}

		public string ProcessBundleLink(string bundlePath)
		{
			// ReSharper disable once PossibleNullReferenceException
			var extension = Path.GetExtension(bundlePath).Substring(1).ToLower();

			if (string.IsNullOrEmpty(extension)) return null;

			var tag = string.Empty;
			var isAPath = bundlePath.Contains('/') ? true : false;
			var modifiedBundlePath = bundlePath;

			if (!isAPath)
				modifiedBundlePath = string.Join("/", _sharedResourceFolderPath, extension, bundlePath);

			switch (extension)
			{
				case "css":
					tag = string.Format(CssIncludeTag, modifiedBundlePath);
					break;
				case "js":
					tag = string.Format(JsIncludeTag, modifiedBundlePath);
					break;
			}

			return tag;
		}

		public StringBuilder Process(ViewCompilerDirectiveInfo directiveInfo)
		{
			var bundleName = directiveInfo.Value;

			switch (directiveInfo.Directive)
			{
				case "Include":
					directiveInfo.Content.Replace(directiveInfo.Match.Groups[0].Value, ProcessBundleLink(bundleName));
					break;
				case "Bundle":
					{
						var fileLinkBuilder = new StringBuilder();

						if (_bundleLinkResults.ContainsKey(bundleName))
						{
							fileLinkBuilder.AppendLine(_bundleLinkResults[bundleName]);
						}
						else
						{
							if (!string.IsNullOrEmpty(bundleName))
							{
								if (_debugMode)
								{
									var bundles = _getBundleFiles(bundleName);

									if (bundles != null)
									{
										foreach (string bundlePath in _getBundleFiles(bundleName))
											fileLinkBuilder.AppendLine(ProcessBundleLink(bundlePath));
									}
								}
								else
									fileLinkBuilder.AppendLine(ProcessBundleLink(bundleName));
							}

							_bundleLinkResults[bundleName] = fileLinkBuilder.ToString();
						}

						directiveInfo.Content.Replace(directiveInfo.Match.Groups[0].Value, fileLinkBuilder.ToString());
					}
					break;
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
			if (directiveInfo.Directive != "Placeholder") return directiveInfo.Content;

			var placeholderMatch = (new Regex(string.Format(@"\[{0}\](?<block>[\s\S]+?)\[/{0}\]", directiveInfo.Value)))
				.Match(directiveInfo.Content.ToString());

			if (!placeholderMatch.Success) return directiveInfo.Content;

			directiveInfo.Content.Replace(directiveInfo.Match.Groups[0].Value, placeholderMatch.Groups["block"].Value);
			directiveInfo.Content.Replace(placeholderMatch.Groups[0].Value, string.Empty);

			return directiveInfo.Content;
		}
	}
	#endregion

	#region VIEW ENGINE INTERNALS
	internal class ViewCache
	{
		public List<TemplateInfo> ViewTemplates;
		public List<TemplateInfo> CompiledViews;
		public Dictionary<string, List<string>> ViewDependencies;
	}

	internal class TemplateInfo
	{
		public string Name { get; set; }
		public string FullName { get; set; }
		public string Path { get; set; }
		public string Template { get; set; }
		public string TemplateMd5Sum { get; set; }
		public string Result { get; set; }
	}

	internal class TemplateLoader
	{
		private readonly string _appRoot;
		private readonly string[] _viewRoots;

		public TemplateLoader(string appRoot,
													string[] viewRoots)
		{
			appRoot.ThrowIfArgumentNull();

			this._appRoot = appRoot;
			this._viewRoots = viewRoots;
		}

		public List<TemplateInfo> Load()
		{
			var templates = new List<TemplateInfo>();

			foreach (var path in _viewRoots.Select(viewRoot => Path.Combine(_appRoot, viewRoot)).Where(Directory.Exists))
			{
				templates.AddRange(new DirectoryInfo(path).GetFiles("*.html", SearchOption.AllDirectories).Select(fi => Load(fi.FullName)));
			}

			return templates;
		}

		public TemplateInfo Load(string path)
		{
			var viewRoot = _viewRoots.FirstOrDefault(x => path.StartsWith(Path.Combine(_appRoot, x)));

			if (string.IsNullOrEmpty(viewRoot)) return null;

			var rootDir = new DirectoryInfo(viewRoot);

			var extension = Path.GetExtension(path);
			var templateName = Path.GetFileNameWithoutExtension(path);
			var templateKeyName = path.Replace(rootDir.Parent.FullName, string.Empty)
																	 .Replace(_appRoot, string.Empty)
																	 .Replace(extension, string.Empty)
																	 .Replace("\\", "/").TrimStart('/');
			var template = File.ReadAllText(path);

			return new TemplateInfo()
			{
				TemplateMd5Sum = template.CalculateMd5Sum(),
				FullName = templateKeyName,
				Name = templateName,
				Path = path,
				Template = template
			};
		}
	}

	internal class ViewCompilerDirectiveInfo
	{
		public Match Match { get; set; }
		public string Directive { get; set; }
		public string Value { get; set; }
		public StringBuilder Content { get; set; }
		public List<TemplateInfo> ViewTemplates { get; set; }
		public Func<string, string> DetermineKeyName { get; set; }
		public Action<string> AddPageDependency { get; set; }
	}

	// This view engine is a simple tag based engine with master pages, partial views and Html fragments.
	// The compiler works by executing a number of directive and substitution handlers to transform
	// the Html templates. All templates are compiled and cached.
	internal class ViewCompiler : IViewCompiler
	{
		private readonly List<IViewCompilerDirectiveHandler> _directiveHandlers;
		private readonly List<IViewCompilerSubstitutionHandler> _substitutionHandlers;

		private readonly List<TemplateInfo> _viewTemplates;
		private readonly List<TemplateInfo> _compiledViews;
		private readonly Dictionary<string, List<string>> _viewDependencies;

		private static readonly Regex DirectiveTokenRe = new Regex(@"(\%\%(?<directive>[a-zA-Z0-9]+)=(?<value>(\S|\.)+)\%\%)", RegexOptions.Compiled);
		private static readonly Regex TagRe = new Regex(@"{({|\||\!)([\w]+)(}|\!|\|)}", RegexOptions.Compiled);
		private const string TagFormatPattern = @"({{({{|\||\!){0}(\||\!|}})}})";
		private const string TagEncodingHint = "{|";
		private const string MarkdownEncodingHint = "{!";
		private const string UnencodedTagHint = "{{";

		private readonly StringBuilder _directive = new StringBuilder();
		private readonly StringBuilder _value = new StringBuilder();

		public ViewCompiler(List<TemplateInfo> viewTemplates,
												List<TemplateInfo> compiledViews,
												Dictionary<string, List<string>> viewDependencies,
												List<IViewCompilerDirectiveHandler> directiveHandlers,
												List<IViewCompilerSubstitutionHandler> substitutionHandlers)
		{
			viewTemplates.ThrowIfArgumentNull();
			compiledViews.ThrowIfArgumentNull();
			viewDependencies.ThrowIfArgumentNull();
			directiveHandlers.ThrowIfArgumentNull();
			substitutionHandlers.ThrowIfArgumentNull();

			_viewTemplates = viewTemplates;
			_compiledViews = compiledViews;
			_viewDependencies = viewDependencies;
			_directiveHandlers = directiveHandlers;
			_substitutionHandlers = substitutionHandlers;
		}

		public List<TemplateInfo> CompileAll()
		{
			foreach (var vt in _viewTemplates)
			{
				if (!vt.FullName.Contains("Fragment"))
					Compile(vt.FullName);
				else
				{
					_compiledViews.Add(new TemplateInfo()
					{
						FullName = vt.FullName,
						Name = vt.Name,
						Template = vt.Template,
						Result = string.Empty,
						TemplateMd5Sum = vt.TemplateMd5Sum,
						Path = vt.Path
					});
				}
			}

			return _compiledViews;
		}

		public TemplateInfo Compile(string fullName)
		{
			TemplateInfo viewTemplate = _viewTemplates.FirstOrDefault(x => x.FullName == fullName);

			if (viewTemplate == null) throw new FileNotFoundException(string.Format("Cannot find view : {0}", fullName));

			var rawView = new StringBuilder(viewTemplate.Template);
			var compiledView = new StringBuilder();

			if (!viewTemplate.FullName.Contains("Fragment"))
				compiledView = ProcessDirectives(fullName, rawView);

			if (string.IsNullOrEmpty(compiledView.ToString()))
				compiledView = rawView;

			compiledView.Replace(compiledView.ToString(), Regex.Replace(compiledView.ToString(), @"^\s*$\n", string.Empty, RegexOptions.Multiline));

			var view = new TemplateInfo()
			{
				FullName = fullName,
				Name = viewTemplate.Name,
				Template = compiledView.ToString(),
				Result = string.Empty,
				TemplateMd5Sum = viewTemplate.TemplateMd5Sum
			};

			var previouslyCompiled = _compiledViews.FirstOrDefault(x => x.FullName == viewTemplate.FullName);

			if (previouslyCompiled != null)
				_compiledViews.Remove(previouslyCompiled);

			_compiledViews.Add(view);

			return view;
		}

		public TemplateInfo Render(string fullName, Dictionary<string, string> tags)
		{
			var compiledView = _compiledViews.FirstOrDefault(x => x.FullName == fullName);

			if (compiledView == null) return null;

			var compiledViewSb = new StringBuilder(compiledView.Template);

			compiledViewSb = _substitutionHandlers.Where(x => x.Type == DirectiveProcessType.Render)
				.Aggregate(compiledViewSb, (current, sub) => sub.Process(current));

			foreach (var dir in _directiveHandlers.Where(x => x.Type == DirectiveProcessType.Render))
			{
				var dirMatches = DirectiveTokenRe.Matches(compiledViewSb.ToString());

				foreach (Match match in dirMatches)
				{
					_directive.Clear();
					_directive.Insert(0, match.Groups["directive"].Value);

					_value.Clear();
					_value.Insert(0, match.Groups["value"].Value);

					compiledViewSb = dir.Process(new ViewCompilerDirectiveInfo()
					{
						Match = match,
						Directive = _directive.ToString(),
						Value = _value.ToString(),
						Content = compiledViewSb,
						ViewTemplates = _viewTemplates,
						AddPageDependency = null, // This is in the pipeline to be fixed
						DetermineKeyName = null // This is in the pipeline to be fixed
					});
				}
			}

			if (tags != null)
			{
				var tagSb = new StringBuilder();

				foreach (var tag in tags)
				{
					tagSb.Clear();
					tagSb.Insert(0, string.Format(TagFormatPattern, tag.Key));

					var tempTagRe = new Regex(tagSb.ToString());
					var tagMatches = tempTagRe.Matches(compiledViewSb.ToString());

					foreach (Match m in tagMatches)
					{
						if (string.IsNullOrEmpty(tag.Value)) continue;

						if (m.Value.StartsWith(UnencodedTagHint))
							compiledViewSb.Replace(m.Value, tag.Value.Trim());
						else if (m.Value.StartsWith(TagEncodingHint))
							compiledViewSb.Replace(m.Value, HttpUtility.HtmlEncode(tag.Value.Trim()));
						else if (m.Value.StartsWith(MarkdownEncodingHint))
							compiledViewSb.Replace(m.Value, new Markdown().Transform((tag.Value.Trim())));
					}
				}

				var leftoverMatches = TagRe.Matches(compiledViewSb.ToString());

				foreach (Match match in leftoverMatches)
					compiledViewSb.Replace(match.Value, string.Empty);
			}

			compiledView.Result = compiledViewSb.ToString();

			return compiledView;
		}

		public StringBuilder ProcessDirectives(string fullViewName, StringBuilder rawView)
		{
			var pageContent = new StringBuilder(rawView.ToString());

			if (!_viewDependencies.ContainsKey(fullViewName))
				_viewDependencies[fullViewName] = new List<string>();

			#region CLOSURES
			Action<string> addPageDependency = x =>
			{
				if (!_viewDependencies[fullViewName].Contains(x))
					_viewDependencies[fullViewName].Add(x);
			};

			Func<string, string> determineKeyName = name => _viewTemplates.Select(y => y.FullName).FirstOrDefault(z => z.Contains("Shared/" + name));

			Func<StringBuilder, IEnumerable<IViewCompilerDirectiveHandler>, StringBuilder> performCompilerPass = (pc, x) =>
			{
				var dirMatches = DirectiveTokenRe.Matches(pc.ToString());

				foreach (Match match in dirMatches)
				{
					_directive.Clear();
					_directive.Insert(0, match.Groups["directive"].Value);

					_value.Clear();
					_value.Insert(0, match.Groups["value"].Value);

					foreach (IViewCompilerDirectiveHandler handler in x)
					{
						pc.Replace(pc.ToString(),
								handler.Process(new ViewCompilerDirectiveInfo()
								{
									Match = match,
									Directive = _directive.ToString(),
									Value = _value.ToString(),
									Content = pc,
									ViewTemplates = _viewTemplates,
									DetermineKeyName = determineKeyName,
									AddPageDependency = addPageDependency
								}).ToString());
					}
				}
				return pc;
			};
			#endregion

			pageContent = performCompilerPass(pageContent, _directiveHandlers.Where(x => x.Type == DirectiveProcessType.Compile));
			pageContent = _substitutionHandlers.Where(x => x.Type == DirectiveProcessType.Compile).Aggregate(pageContent, (current, sub) => sub.Process(current));
			pageContent = performCompilerPass(pageContent, _directiveHandlers.Where(x => x.Type == DirectiveProcessType.AfterCompile));

			return pageContent;
		}

		public void RecompileDependencies(string fullViewName)
		{
			Action<string> compile = name =>
			{
				var template = _viewTemplates.FirstOrDefault(x => x.FullName == name);

				if (template != null)
					Compile(template.FullName);
			};

			var deps = _viewDependencies.Where(x => x.Value.FirstOrDefault(y => y == fullViewName) != null).ToList();

			if (deps.Any())
			{
				foreach (var view in deps)
					compile(view.Key);
			}
			else
				compile(fullViewName);
		}
	}

	internal class ViewEngine : IViewEngine
	{
		private string _appRoot;
		private readonly List<IViewCompilerDirectiveHandler> _dirHandlers;
		private readonly List<IViewCompilerSubstitutionHandler> _substitutionHandlers;
		private readonly List<TemplateInfo> _viewTemplates;
		private readonly List<TemplateInfo> _compiledViews;
		private readonly Dictionary<string, List<string>> _viewDependencies;
		private readonly TemplateLoader _viewTemplateLoader;
		private ViewCompiler _viewCompiler;

		public bool CacheUpdated { get; private set; }

		public ViewEngine(string appRoot,
											string[] viewRoots,
											List<IViewCompilerDirectiveHandler> dirHandlers,
											List<IViewCompilerSubstitutionHandler> substitutionHandlers,
											string cache)
		{
			this._appRoot = appRoot;
			this._dirHandlers = dirHandlers;
			this._substitutionHandlers = substitutionHandlers;

			_viewTemplateLoader = new TemplateLoader(appRoot, viewRoots);

			var watcher = new FileSystemWatcher(appRoot, "*.html") { NotifyFilter = NotifyFilters.LastWrite };

			watcher.Changed += new FileSystemEventHandler(OnChanged);
			watcher.IncludeSubdirectories = true;
			watcher.EnableRaisingEvents = true;

			if (!viewRoots.Any())
				throw new ArgumentException("At least one view root is required to load view templates from.");

			if (!string.IsNullOrEmpty(cache))
			{
				var viewCache = JsonConvert.DeserializeObject<ViewCache>(cache);

				if (viewCache != null)
				{
					_viewTemplates = viewCache.ViewTemplates;
					_compiledViews = viewCache.CompiledViews;
					_viewDependencies = viewCache.ViewDependencies;
				}
			}
			else
			{
				_compiledViews = new List<TemplateInfo>();
				_viewDependencies = new Dictionary<string, List<string>>();
				_viewTemplates = _viewTemplateLoader.Load();
			}

			_viewCompiler = new ViewCompiler(_viewTemplates, _compiledViews, _viewDependencies, dirHandlers, substitutionHandlers);

			if (!_compiledViews.Any())
				_compiledViews = _viewCompiler.CompileAll();
		}

		private void OnChanged(object sender, FileSystemEventArgs e)
		{
			var fsw = sender as FileSystemWatcher;

			try
			{
				if (fsw != null) fsw.EnableRaisingEvents = false;

				while (CanOpenForRead(e.FullPath) == false)
					Thread.Sleep(1000);

				var changedTemplate = _viewTemplateLoader.Load(e.FullPath);
				_viewTemplates.Remove(_viewTemplates.Find(x => x.FullName == changedTemplate.FullName));
				_viewTemplates.Add(changedTemplate);

				var cv = _compiledViews.FirstOrDefault(x => x.FullName == changedTemplate.FullName && x.TemplateMd5Sum != changedTemplate.TemplateMd5Sum);

				if (cv != null && !changedTemplate.FullName.Contains("Fragment"))
				{
					cv.TemplateMd5Sum = changedTemplate.TemplateMd5Sum;
					cv.Template = changedTemplate.Template;
					cv.Result = string.Empty;
				}

				_viewCompiler = new ViewCompiler(_viewTemplates, _compiledViews, _viewDependencies, _dirHandlers, _substitutionHandlers);

				if (cv != null)
					_viewCompiler.RecompileDependencies(changedTemplate.FullName);
				else
					_viewCompiler.Compile(changedTemplate.FullName);

				CacheUpdated = true;
			}
			finally
			{
				if (fsw != null) fsw.EnableRaisingEvents = true;
			}
		}

		public string GetCache()
		{
			if (CacheUpdated) CacheUpdated = false;

			return JsonConvert.SerializeObject(new ViewCache()
			{
				CompiledViews = _compiledViews,
				ViewTemplates = _viewTemplates,
				ViewDependencies = _viewDependencies
			}, Formatting.Indented);
		}

		// adapted from: http://stackoverflow.com/a/8218033/170217
		private static bool CanOpenForRead(string filePath)
		{
			try
			{
				using (var file = new FileStream(filePath, FileMode.Open, FileAccess.Read))
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

		public string LoadView(string fullName, Dictionary<string, string> tags)
		{
			string result = null;

			var renderedView = _viewCompiler.Render(fullName, tags);

			if (renderedView == null) return null;

			try
			{
				result = XDocument.Parse(renderedView.Result).ToString();
			}
			catch
			{
				// Oops, Html is not well formed, probably tried to parse a fragment
				// that had embedded string.Format placeholders or something weird.
				result = renderedView.Result;
			}

			return result;
		}
	}
	#endregion
	#endregion

	#endregion
}
