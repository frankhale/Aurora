// NOTE: This has been removed from the vsproj but is still here to keep me on
//       track and so I don't lose sight of all the features I need to implement
//       in the new code.
//
// Aurora - An MVC web framework for .NET
//
// Updated On: 30 November 2014
//
// Source Code Location:
//
//	https://github.com/frankhale/aurora
//
// Requirements: .NET 4.5
//
// Contact Info:
//
//  Frank Hale - <frankhale@gmail.com> 
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
using Yahoo.Yui.Compressor;

namespace Aurora
{
	using Aurora.Common;

	#region FRAMEWORK ENGINE
	internal class Engine : IAspNetAdapterApplication
	{
		#region ASP.NET ADAPTER STUFF
		private Dictionary<string, object> app;
		internal Dictionary<string, object> Request;
		private Dictionary<string, string> queryString, cookies, form, payload;
		private Action<Dictionary<string, object>> response;
		private List<PostedFile> files;
		private string[] pathSegments;
		private Exception serverError;
		internal X509Certificate2 ClientCertificate { get; private set; }
		internal string IpAddress, Path, RequestType, AppRoot, ViewRoot, SessionId, Identity;
		internal Uri Url;
		#endregion

		#region MISCELLANEOUS VARIABLES
		internal EngineAppState EngineAppState;
		internal EngineSessionState EngineSessionState;
		private bool debugMode;
		internal User CurrentUser;
		private RouteInfo _currentRoute;
		#endregion

		#region FRAMEWORK METHODS

		public void Init(Dictionary<string, object> app, Dictionary<string, object> request,
			Action<Dictionary<string, object>> response)
		{
			this.app = app;
			Request = request;
			this.response = response;

			#region INITIALIZE LOCALS FROM APP/REQUEST AND MISC

			EngineAppState = GetApplication(EngineAppState.EngineAppStateSessionName) as EngineAppState ?? new EngineAppState();
			EngineSessionState = GetSession(EngineAppState.EngineSessionStateSessionName) as EngineSessionState ??
													 new EngineSessionState();

			RequestType = request[HttpAdapterConstants.RequestMethod].ToString().ToLower();
			AppRoot = request[HttpAdapterConstants.RequestPathBase].ToString();
			ViewRoot = string.Format(@"{0}\Views", AppRoot);
			IpAddress = request[HttpAdapterConstants.RequestIpAddress].ToString();
			SessionId = (request[HttpAdapterConstants.SessionId] != null)
				? request[HttpAdapterConstants.SessionId].ToString()
				: null;
			Path = request[HttpAdapterConstants.RequestPath].ToString();
			pathSegments = request[HttpAdapterConstants.RequestPathSegments] as string[];
			cookies = request[HttpAdapterConstants.RequestCookie] as Dictionary<string, string>;
			form = request[HttpAdapterConstants.RequestForm] as Dictionary<string, string>;
			payload = request[HttpAdapterConstants.RequestBody] as Dictionary<string, string>;
			files = request[HttpAdapterConstants.RequestFiles] as List<PostedFile>;
			queryString = request[HttpAdapterConstants.RequestQueryString] as Dictionary<string, string>;
			debugMode = Convert.ToBoolean(app[HttpAdapterConstants.DebugModeASPNET]);
			serverError = app[HttpAdapterConstants.ServerError] as Exception;
			ClientCertificate = request[HttpAdapterConstants.RequestClientCertificate] as X509Certificate2;
			Url = request[HttpAdapterConstants.RequestUrl] as Uri;
			Identity = request[HttpAdapterConstants.RequestIdentity] as string;

			#endregion

			#region INITIALIZE MISCELLANEOUS

			if (EngineAppState.RouteInfos == null)
				EngineAppState.RouteInfos = new List<RouteInfo>();
			else
				EngineAppState.RouteInfos.ForEach(x => x.Controller.Engine = this);

			#endregion

			#region INITIALIZE USERS

			if (EngineAppState.Users == null)
				EngineAppState.Users = new List<User>();

			#endregion

			#region INITIALIZE ANTIFORGERYTOKENS

			if (EngineAppState.AntiForgeryTokens == null)
				EngineAppState.AntiForgeryTokens = new List<string>();

			#endregion

			#region INITIALIZE MODELS

			if (EngineAppState.Models == null)
				EngineAppState.Models = GetTypeList(typeof(Model));

			#endregion

			#region INITIALIZE CONTROLLERS SESSION

			if (EngineSessionState.ControllersSession == null)
				EngineSessionState.ControllersSession = new Dictionary<string, object>();

			#endregion

			#region INITIALIZE PROTECTED FILES

			if (EngineAppState.ProtectedFiles == null)
				EngineAppState.ProtectedFiles = new Dictionary<string, string>();

			#endregion

			#region INITIALIZE ACTION BINDINGS

			if (EngineSessionState.ActionBindings == null)
				EngineSessionState.ActionBindings = new Dictionary<string, Dictionary<string, List<object>>>();

			#endregion

			#region INITIALIZE BUNDLES

			if (EngineAppState.Bundles == null)
				EngineAppState.Bundles = new Dictionary<string, Tuple<List<string>, string>>();

			#endregion

			#region INITIALIZE HELPER BUNDLES

			if (EngineSessionState.HelperBundles == null)
				EngineSessionState.HelperBundles = new Dictionary<string, StringBuilder>();

			#endregion

			#region INTIALIZE FRONT CONTROLLER

			if (EngineSessionState.FrontController == null)
				EngineSessionState.FrontController = GetFrontControllerInstance();
			else
				EngineSessionState.FrontController.Engine = this;

			#endregion

			#region INITIALIZE CONTROLLER INSTANCES

			if (EngineSessionState.Controllers == null)
				EngineSessionState.Controllers = new List<Controller>();

			#endregion

			#region RUN ALL CONTROLLER ONINIT METHODS

			if (EngineSessionState.FrontController != null)
				EngineSessionState.FrontController.RaiseEvent(EventType.OnInit);

			#endregion

			#region INITIALIZE VIEW ENGINE

			if (!EngineAppState.AllowedFilePattern.IsMatch(Path) && (EngineAppState.ViewEngine == null || debugMode))
			{
				string viewCache = null;

				if (EngineAppState.ViewEngineDirectiveHandlers == null && EngineAppState.ViewEngineSubstitutionHandlers == null)
				{
					EngineAppState.ViewEngineDirectiveHandlers = new List<IViewCompilerDirectiveHandler>();
					EngineAppState.ViewEngineSubstitutionHandlers = new List<IViewCompilerSubstitutionHandler>();

					EngineAppState.ViewEngineDirectiveHandlers.Add(new MasterPageDirective());
					EngineAppState.ViewEngineDirectiveHandlers.Add(new PlaceHolderDirective());
					EngineAppState.ViewEngineDirectiveHandlers.Add(new PartialPageDirective());
					EngineAppState.ViewEngineDirectiveHandlers.Add(new BundleDirective(debugMode,
						EngineAppState.SharedResourceFolderPath, GetBundleFiles));
					EngineAppState.ViewEngineSubstitutionHandlers.Add(new HelperBundleDirective(
						EngineAppState.SharedResourceFolderPath, GetHelperBundle));
					EngineAppState.ViewEngineSubstitutionHandlers.Add(new CommentSubstitution());
					EngineAppState.ViewEngineSubstitutionHandlers.Add(new AntiForgeryTokenSubstitution(CreateAntiForgeryToken));
					EngineAppState.ViewEngineSubstitutionHandlers.Add(new HeadSubstitution());
				}

				if (string.IsNullOrEmpty(EngineAppState.CacheFilePath))
					EngineAppState.CacheFilePath = System.IO.Path.Combine(MapPath(EngineAppState.CompiledViewsCacheFolderPath),
						EngineAppState.CompiledViewsCacheFileName);

				if (File.Exists(EngineAppState.CacheFilePath) && !debugMode)
					viewCache = File.ReadAllText(EngineAppState.CacheFilePath);

				if (EngineAppState.ViewRoots == null)
					EngineAppState.ViewRoots = GetViewRoots();

				EngineAppState.ViewEngine = new ViewEngine(AppRoot, EngineAppState.ViewRoots,
					EngineAppState.ViewEngineDirectiveHandlers, EngineAppState.ViewEngineSubstitutionHandlers, viewCache);

				if (string.IsNullOrEmpty(viewCache) || !debugMode)
					UpdateCache(EngineAppState.CacheFilePath);
			}
			else if (EngineAppState.ViewEngine.CacheUpdated ||
							 !Directory.Exists(System.IO.Path.GetDirectoryName(EngineAppState.CacheFilePath)) ||
							 !File.Exists(EngineAppState.CacheFilePath))
				UpdateCache(EngineAppState.CacheFilePath);

			#endregion

			AddApplication(EngineAppState.EngineAppStateSessionName, EngineAppState);
			AddSession(EngineAppState.EngineSessionStateSessionName, EngineSessionState);

			CurrentUser = EngineAppState.Users.FirstOrDefault(x => x.SessionId == SessionId);

			#region PROCESS REQUEST / RENDER RESPONSE

			ViewResponse viewResponse = null;
			Exception exception = serverError;
			int httpStatus = 200;

			if (exception == null)
			{
				try
				{
					viewResponse = ProcessRequest();
				}
				catch (Exception ex)
				{
					exception = ex;
				}
			}

			if (viewResponse == null && _currentRoute != null && _currentRoute.Action.ReturnType != typeof(void)) return;

			if (viewResponse == null && exception == null)
			{
				httpStatus = 404;
				viewResponse = GetErrorViewResponse("Http 404 - Page Not Found", null);
			}
			else if (exception != null)
			{
				httpStatus = 503;
				if (debugMode)
				{
					viewResponse = GetErrorViewResponse(
						(exception.InnerException != null) ? exception.InnerException.Message : exception.Message,
						(exception.InnerException != null) ? exception.InnerException.StackTrace : exception.StackTrace
						);
				}
				else
					viewResponse = GetErrorViewResponse("A problem occurred trying to process this request.", null);
			}

			viewResponse.HttpStatus = httpStatus;

			RenderResponse(viewResponse);

			#endregion
		}

		#region PRIVATE METHODS
		private ViewResponse ProcessRequest()
		{
			ViewResponse viewResponse = null;

			if (EngineAppState.AllowedFilePattern.IsMatch(Path))
			{
				#region FILE RESPONSE
				RaiseEventOnFrontController(RouteHandlerEventType.Static, Path, null, null);

				if (Path.StartsWith(EngineAppState.SharedResourceFolderPath) || Path.EndsWith(".ico"))
				{
					var filePath = MapPath(Path);

					if (CanAccessFile(filePath))
					{
						if (File.Exists(filePath))
							viewResponse = new FileResult(EngineAppState.AllowedFilePattern, filePath).Render();
						else
						{
							var fileName = System.IO.Path.GetFileName(filePath);

							if (EngineAppState.Bundles.ContainsKey(fileName))
								viewResponse = new FileResult(fileName, EngineAppState.Bundles[fileName].Item2).Render();
							else if (EngineSessionState.HelperBundles.ContainsKey(fileName))
								viewResponse = new FileResult(fileName, EngineSessionState.HelperBundles[fileName].ToString()).Render();
						}
					}
				}
				#endregion
			}
			else
			{
				#region ACTION RESPONSE
				RouteInfo routeInfo = null;

				if (Path == "/" || Path == "~/" || Path.ToLower() == "/default.aspx" || Path == "/Index")
				{
					Path = "/Index";
					pathSegments[0] = "Index";
				}

				RaiseEventOnFrontController(RouteHandlerEventType.PreRoute, Path, null, null);

				routeInfo = FindRoute(string.Concat("/", pathSegments[0]), pathSegments);

				RaiseEventOnFrontController(RouteHandlerEventType.PostRoute, Path, routeInfo, null);

				if (routeInfo == null)
					routeInfo = RaiseEventOnFrontController(RouteHandlerEventType.MissingRoute, Path, null, null);

				if (routeInfo != null)
				{
					_currentRoute = routeInfo;

					if (routeInfo.RequestTypeAttribute.ActionType == ActionType.FromRedirectOnly && !EngineSessionState.FromRedirectOnly)
						return null;

					if (routeInfo.RequestTypeAttribute.RequireAntiForgeryToken &&
							RequestType == "post" || RequestType == "put" || RequestType == "delete")
					{
						if (!(form.ContainsKey(EngineAppState.AntiForgeryTokenName) ||
									payload.ContainsKey(EngineAppState.AntiForgeryTokenName)))
						{
							// i
							return GetErrorViewResponse("AntiForgeryToken Required", "All forms require an AntiForgeryToken by default.");
						}
						else
						{
							if (EngineAppState.AntiForgeryTokens.Contains(form[EngineAppState.AntiForgeryTokenName]) ||
								EngineAppState.AntiForgeryTokens.Contains(payload[EngineAppState.AntiForgeryTokenName]))
							{
								EngineAppState.AntiForgeryTokens.Remove(form[EngineAppState.AntiForgeryTokenName]);
								EngineAppState.AntiForgeryTokens.Remove(payload[EngineAppState.AntiForgeryTokenName]);
							}
							else
							{
								return GetErrorViewResponse("AntiForgeryToken Required", "All forms require a valid AntiForgeryToken.");
							}
						}
					}

					if (routeInfo.RequestTypeAttribute.SecurityType == ActionSecurity.Secure &&
							routeInfo.RequestTypeAttribute.SecurityType == ActionSecurity.Secure && CurrentUser == null ||
							routeInfo.RequestTypeAttribute.SecurityType == ActionSecurity.Secure && routeInfo.RequestTypeAttribute.Roles == null ||
							routeInfo.RequestTypeAttribute.SecurityType == ActionSecurity.Secure && !(CurrentUser.Roles.Intersect(routeInfo.RequestTypeAttribute.Roles.Split('|')).Any()) ||
							routeInfo.RequestTypeAttribute.SecurityType == ActionSecurity.Secure && routeInfo.Controller.RaiseCheckRoles(new CheckRolesHandlerEventArgs() { RouteInfo = routeInfo }))
					{
						RaiseEventOnFrontController(RouteHandlerEventType.FailedSecurity, Path, routeInfo, null);

						if (!string.IsNullOrEmpty(routeInfo.RequestTypeAttribute.RedirectWithoutAuthorizationTo))
						{
							viewResponse = new ViewResponse() { RedirectTo = routeInfo.RequestTypeAttribute.RedirectWithoutAuthorizationTo };
						}
					}
					else
					{
						RaiseEventOnFrontController(RouteHandlerEventType.PassedSecurity, Path, routeInfo, null);
						RaiseEventOnFrontController(RouteHandlerEventType.PreAction, Path, routeInfo, null);
						routeInfo.Controller.RaiseEvent(RouteHandlerEventType.PreAction, Path, routeInfo);

						if (routeInfo.RequestTypeAttribute.ActionType == ActionType.FromRedirectOnly && EngineSessionState.FromRedirectOnly)
							RemoveSession(EngineAppState.FromRedirectOnlySessionName);

						if (routeInfo.BoundToActionParams != null)
						{
							foreach (var bta in routeInfo.BoundToActionParams)
								bta.Initialize(routeInfo);
						}

						if (routeInfo.BoundParams != null)
						{
							for (var i = 0; i < routeInfo.BoundParams.Count(); i++)
							{
								if (routeInfo.BoundParams[i].GetType().GetInterface(typeof(IBoundToAction).Name) == null)
									routeInfo.BoundParams[i] = Activator.CreateInstance(routeInfo.BoundParams[i].GetType(), null);
							}
						}

						if (routeInfo.ActionParamTransforms != null)
						{
							foreach (var apt in routeInfo.ActionParamTransforms)
							{
								var transformMethod = routeInfo.CachedActionParamTransformInstances[apt.Item1.TransformName] as Tuple<MethodInfo, object>;

								if (transformMethod != null)
								{
									var t = transformMethod.Item1.GetParameters()[0].ParameterType;
									var param = routeInfo.ActionUrlParams[apt.Item2];

									if (routeInfo.ActionUrlParams[apt.Item2] != null &&
											routeInfo.ActionUrlParams[apt.Item2].GetType() != t)
									{
										try
										{
											param = Convert.ChangeType(routeInfo.ActionUrlParams[apt.Item2], t);
										}
										catch
										{
											// Oops! We probably tried to convert a type to another type and it failed! 
											// In which case we'll pretend like nothing happened.
										}
									}

									try
									{
										routeInfo.ActionUrlParams[apt.Item2] =
											transformMethod.Item1.Invoke(transformMethod.Item2, new object[] { param });
									}
									catch
									{
										// Oops! We probably tried to invoke an action with incorrect types! 
										// In which case we'll pretend like nothing happened.
									}
								}
							}
						}

						var filterResults = ProcessAnyActionFilters(routeInfo);

						if (filterResults.Any())
						{
							var actionParams = routeInfo.ActionUrlParams;
							Array.Resize(ref actionParams, actionParams.Count() + filterResults.Count());
							filterResults.CopyTo(actionParams, EngineSessionState.ActionBindings.Count());
							routeInfo.ActionUrlParams = actionParams;
						}

						routeInfo.Controller.HttpAttribute = routeInfo.RequestTypeAttribute;

						IViewResult viewResult;
						try
						{
							viewResult = (IViewResult)routeInfo.Action.Invoke(routeInfo.Controller, routeInfo.ActionUrlParams);
						}
						catch (Exception ex)
						{
							// I'm not completely sure how I want to ultimately handle this condtion yet,
							// for now let's fire off the front controllers error event in case anyone is
							// looking for it. The usefulness of this is questionable, what are they going to
							// do with the information? Should we allow another action to be potentially 
							// invoked so that we can get a good viewResult? Thereby dropping the throw below
							// this call?!? 
							RaiseEventOnFrontController(RouteHandlerEventType.Error, Path, routeInfo, ex);

							throw;
						}

						if (routeInfo.Action.ReturnType != typeof(void))
						{
							if (viewResult != null)
								viewResponse = viewResult.Render();

							if (viewResponse == null)
								RaiseEventOnFrontController(RouteHandlerEventType.Error, Path, routeInfo,
									"A problem occurred trying to render the result causing it to be null. Please check to make sure you have a view for this action.");
						}

						RaiseEventOnFrontController(RouteHandlerEventType.PostAction, Path, routeInfo, viewResponse);
						routeInfo.Controller.RaiseEvent(RouteHandlerEventType.PostAction, Path, routeInfo);
					}
				}
				#endregion
			}

			return viewResponse;
		}

		private void UpdateCache(string cacheFilePath)
		{
			try
			{
				var path = System.IO.Path.GetDirectoryName(cacheFilePath);

				if (!Directory.Exists(path))
				{
					try { Directory.CreateDirectory(path); }
					catch { /* Silently ignore failure */ }
				}

				if (!Directory.Exists(path)) return;

				using (var cacheWriter = new StreamWriter(cacheFilePath))
					cacheWriter.Write(EngineAppState.ViewEngine.GetCache());
			}
			catch { /* Silently ignore any write failures */ }
		}

		private ViewResponse GetErrorViewResponse(string error, string stackTrace)
		{
			if (!string.IsNullOrEmpty(stackTrace))
				stackTrace = string.Format("<p><pre>{0}</pre></p>", stackTrace);

			Dictionary<string, string> tags;

			if (_currentRoute != null)
			{
				Dictionary<string, object> ttags = _currentRoute.Controller.ViewBag.AsDictionary();

				tags = ttags.ToDictionary(k => k.Key, k => k.Value.ToString());
			}
			else
				tags = new Dictionary<string, string>();

			tags["error"] = error;
			tags["stacktrace"] = stackTrace;

			var errorView = EngineAppState.ViewEngine.LoadView("Views/Shared/Error", tags);

			var viewResponse = new ViewResponse()
			{
				ContentType = "text/html",
				Content = !string.IsNullOrEmpty(errorView) ? errorView : string.Format("<!DOCTYPE html><body>{0} : {1} {2}</body></html>", Path, error, stackTrace)
			};

			return viewResponse;
		}

		private void RenderResponse(ViewResponse viewResponse)
		{
			if (string.IsNullOrEmpty(viewResponse.RedirectTo))
			{
				response(new Dictionary<string, object>
				{
					{HttpAdapterConstants.ResponseBody, viewResponse.Content},
					{HttpAdapterConstants.ResponseContentType, viewResponse.ContentType},
					{HttpAdapterConstants.ResponseHeaders, viewResponse.Headers},
					{HttpAdapterConstants.ResponseStatus, viewResponse.HttpStatus}
				});
			}
			else
			{
				var redirectRoute = FindRoute(viewResponse.RedirectTo, new string[] { });

				if (redirectRoute != null)
					ResponseRedirect(viewResponse.RedirectTo, (redirectRoute.RequestTypeAttribute.ActionType == ActionType.FromRedirectOnly) ? true : false);
				else
					RenderResponse(GetErrorViewResponse("Unable to determine the route to redirect to.", null));
			}
		}

		private string[] GetViewRoots()
		{
			var viewRoots = new List<string>() { ViewRoot };
			var partitionAttributes = GetTypeList(typeof(Controller))
				.SelectMany(x => x.GetCustomAttributes(typeof(PartitionAttribute), false)
				.Cast<PartitionAttribute>());

			viewRoots.AddRange(partitionAttributes.Select(x => string.Format(@"{0}\{1}", AppRoot, x.Name)));

			return viewRoots.ToArray();
		}

		private object PayloadToModel(Dictionary<string, string> payload)
		{
			object result = null;
			Type model = null;
			var payloadNames = new HashSet<string>(payload.Keys.Where(x => x != "AntiForgeryToken"));

			foreach (Type m in EngineAppState.Models)
			{
				var props = new HashSet<string>(Model.GetPropertiesWithExclusions(m, true).Select(x => x.Name));

				if (props.Intersect(payloadNames).Count() == props.Union(payloadNames).Count())
					model = m;
				else
				{
					props = new HashSet<string>(Model.GetPropertiesNotRequiredToPost(m).Select(x => x.Name));

					if (props.IsSubsetOf(payloadNames))
						model = m;
				}
			}

			if (model != null)
			{
				result = Activator.CreateInstance(model);

				foreach (PropertyInfo p in Model.GetPropertiesWithExclusions(model, true))
				{
					var skipValidationAttrib = (UnsafeAttribute)p.GetCustomAttributes(typeof(UnsafeAttribute), false).FirstOrDefault();
					var notRequiredAttrib = (NotRequiredAttribute)p.GetCustomAttributes(typeof(NotRequiredAttribute), false).FirstOrDefault();

					if (notRequiredAttrib != null && !payload.ContainsKey(p.Name)) continue;

					var propertyValue = payload[p.Name];

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
						if (files.Any())
							p.SetValue(result, files[0], null);
					}
					else if (p.PropertyType == typeof(List<PostedFile>))
						p.SetValue(result, files, null);
				}

				var model1 = result as Model;
				if (model1 != null) model1.Validate(payload);
			}

			return result;
		}

		private static List<Type> GetTypeList(Type t)
		{
			t.ThrowIfArgumentNull();

			return Utility.GetAssemblies()
							.SelectMany(x => x.GetLoadableTypes()
							.AsParallel()
							.Where(y => y.IsClass && y.BaseType == t)).ToList();
		}

		private static List<string> GetControllerActionNames(string controllerName)
		{
			controllerName.ThrowIfArgumentNull();

			var result = new List<string>();

			var controller = GetTypeList(typeof(Controller)).FirstOrDefault(x =>
				x.Name == controllerName);

			if (controller != null)
			{
				result = controller.GetMethods()
													 .Where(x =>
														 x.GetCustomAttributes(typeof(HttpAttribute), false)
														 .Any() && x.IsPublic)
													 .Select(x => x.Name)
													 .ToList();
			}

			return result;
		}

		private Controller GetControllerInstances(string controllerName)
		{
			var ctrlInstance = EngineSessionState.Controllers.FirstOrDefault(x => x.GetType().Name == controllerName);

			if (ctrlInstance == null)
			{
				ctrlInstance = Controller.CreateInstance(GetTypeList(typeof(Controller))
																 .FirstOrDefault(x => x.Name == controllerName), this);
				EngineSessionState.Controllers.Add(ctrlInstance);
				ctrlInstance.RaiseEvent(EventType.OnInit);
			}
			else
				ctrlInstance.Engine = this;

			return ctrlInstance;
		}

		private FrontController GetFrontControllerInstance()
		{
			FrontController result = null;
			var fcType = GetTypeList(typeof(FrontController)).FirstOrDefault();

			if (fcType != null)
				result = FrontController.CreateInstance(fcType, this);

			return result;
		}

		private List<RouteInfo> GetRouteInfos(string path)
		{
			foreach (var c in GetTypeList(typeof(Controller)))
			{
				List<MethodInfo> actions = null;

				if (EngineSessionState.ControllerActions == null)
				{
					actions = c.GetMethods()
										 .Where(x => x.GetCustomAttributes(typeof(HttpAttribute), false).Any())
										 .ToList();

					EngineSessionState.ControllerActions = actions;
				}
				else
					actions = EngineSessionState.ControllerActions;

				var refinedActions = actions.Select(x => new
				{
					Method = x,
					Attribute = (HttpAttribute)x.GetCustomAttributes(typeof(HttpAttribute), false).FirstOrDefault(),
					Aliases = x.GetCustomAttributes(typeof(AliasAttribute), false).Select(a => (a as AliasAttribute).Alias).ToList()
				})
				.Where(x => (x.Aliases.Any()) ? x.Aliases.FirstOrDefault(y => y == path).Any() : x.Attribute.RouteAlias == path);

				var controller = GetControllerInstances(c.Name);

				foreach (var action in refinedActions)
				{
					if (string.IsNullOrEmpty(action.Attribute.RouteAlias))
						action.Aliases.Add(string.Format("/{0}/{1}", c.Name, action.Method.Name));
					else
						action.Aliases.Add(action.Attribute.RouteAlias);

					AddRoute(EngineAppState.RouteInfos, controller, action.Method, action.Aliases, null, false);
				}
			}

			return EngineAppState.RouteInfos;
		}

		private ActionParameterInfo GetActionParameterTransforms(ParameterInfo[] actionParams, List<object> bindings)
		{
			var cachedActionParamTransformInstances = new Dictionary<string, object>();

			// Eventually I want to change this algorithm to not require the attribute and instead figure
			// it out based on the type parameter and if there are any action parameter transform classes
			// that implement the IActionParamTransform<T, V> with the type parameter. This may make it slower
			// since it would have to check more action parameters in it's parameter list.

			// The int in this is for the index of the parameter in the action parameter list.
			var actionParameterTransforms = actionParams
					.Select((x, i) => new Tuple<ActionParameterTransformAttribute, int>
						((ActionParameterTransformAttribute)x.GetCustomAttributes(typeof(ActionParameterTransformAttribute), false).FirstOrDefault(), i))
					.Where(x => x.Item1 != null)
					.ToList();

			if (actionParameterTransforms.Count > 0)
			{
				foreach (var apt in actionParameterTransforms)
				{
					var actionTransformClassType = Utility.GetAssemblies()
															.SelectMany(x => x.GetLoadableTypes().Where(y => y.GetInterface(typeof(IActionParamTransform<,>).Name) != null && y.Name == apt.Item1.TransformName))
															.FirstOrDefault();

					if (actionTransformClassType != null)
					{
						try
						{
							var instance = Activator.CreateInstance(actionTransformClassType, (bindings != null) ? bindings.ToArray() : null);
							var transformMethod = actionTransformClassType.GetMethod("Transform");

							cachedActionParamTransformInstances[apt.Item1.TransformName] = new Tuple<MethodInfo, object>(transformMethod, instance);
						}
						catch
						{
							cachedActionParamTransformInstances[apt.Item1.TransformName] = null;
						}
					}
				}
			}

			return new ActionParameterInfo()
			{
				ActionParamTransforms = actionParameterTransforms.Any() ? actionParameterTransforms : null,
				ActionParamTransformInstances = cachedActionParamTransformInstances.Any() ? cachedActionParamTransformInstances : null
			};
		}

		private IActionFilterResult[] ProcessAnyActionFilters(RouteInfo routeInfo)
		{
			var results = new List<IActionFilterResult>();
			var actionFilterAttributes =
					routeInfo.Action.GetCustomAttributes(typeof(ActionFilterAttribute), false).Cast<ActionFilterAttribute>().ToList();

			foreach (var afa in actionFilterAttributes)
			{
				afa.Init(this);
				afa.Controller = routeInfo.Controller;
				afa.OnFilter(routeInfo);

				if (afa.DivertRoute != null)
					routeInfo = afa.DivertRoute;

				if (afa.FilterResult != null)
					results.Add(afa.FilterResult);
			}

			return results.ToArray();
		}

		private RouteInfo RaiseEventOnFrontController(RouteHandlerEventType eventType, string path, RouteInfo routeInfo, object data)
		{
			if (EngineSessionState.FrontController != null)
				return EngineSessionState.FrontController.RaiseEvent(eventType, path, routeInfo, data);

			return routeInfo;
		}

		private static string CreateToken()
		{
			return Guid.NewGuid().ToString().Replace("-", string.Empty);
		}
		#endregion

		#region INTERNAL METHODS
		// Allows a file to be protected from download by users that are not logged in
		internal void ProtectFile(string path, string roles)
		{
			path.ThrowIfArgumentNull();
			roles.ThrowIfArgumentNull();

			EngineAppState.ProtectedFiles[string.Format(@"{0}\{1}", AppRoot, path)] = roles;
		}

		internal bool CanAccessFile(string path)
		{
			path.ThrowIfArgumentNull();

			if (EngineAppState.ProtectedFiles.ContainsKey(path))
				return (CurrentUser != null && CurrentUser.Roles.Intersect(EngineAppState.ProtectedFiles[path].Split('|')).Any()) ? true : false;

			return true;
		}

		internal void AddBundles(Dictionary<string, string[]> bundles)
		{
			foreach (var bundle in bundles)
				AddBundle(bundle.Key, bundle.Value);
		}

		internal void AddBundle(string name, string[] paths)
		{
			name.ThrowIfArgumentNull();
			paths.ThrowIfArgumentNull();

			if (paths.Length == 0) return;

			var extension = System.IO.Path.GetExtension(name);
			string fileContentResult = null;
			var combinedFiles = new StringBuilder();

			foreach (var p in paths)
			{
				var resourcePath = AppRoot + p.Replace('/', '\\');

				if (File.Exists(resourcePath) &&
						(System.IO.Path.GetExtension(p) == ".css" ||
						 System.IO.Path.GetExtension(p) == ".js"))
				{
					combinedFiles.AppendLine(File.ReadAllText(resourcePath));
				}
			}

			if (!debugMode)
			{
				switch (extension)
				{
					case ".js":
						fileContentResult = new JavaScriptCompressor().Compress(combinedFiles.ToString());
						break;
					case ".css":
						fileContentResult = new CssCompressor().Compress(combinedFiles.ToString());
						break;
				}
			}
			else
				fileContentResult = combinedFiles.ToString();

			EngineAppState.Bundles[name] = new Tuple<List<string>, string>(paths.ToList(), fileContentResult);
		}

		// Helper bundles are a special mechanism that will allow HtmlHelpers to inject CSS or JS when
		// they are used. This also provides a nice way to package up controls that can be used over
		// and over.
		internal void AddHelperBundle(string name, string code)
		{
			EngineSessionState.HelperBundles[name] = new StringBuilder(code);

			if (!debugMode)
			{
				var match = EngineAppState.CssorJsExtPattern.Match(name);

				if (match.Value.EndsWith(".js"))
				{
					if (!EngineSessionState.HelperBundles[name].ToString().Contains(code))
						EngineSessionState.HelperBundles[name].AppendLine(new JavaScriptCompressor().Compress(code));
				}
				else if (match.Value.EndsWith(".css"))
				{
					if (!EngineSessionState.HelperBundles[name].ToString().Contains(code))
						EngineSessionState.HelperBundles[name].AppendLine(new CssCompressor().Compress(code));
				}
			}
			else
			{
				if (!EngineSessionState.HelperBundles[name].ToString().Contains(code))
					EngineSessionState.HelperBundles[name].AppendLine(code);
			}
		}

		// This is used to provide a way to get the file path so if we are in debug mode
		// we can ignore the bundles and instead place all of the real file includes in the
		// HTML head.
		internal string[] GetBundleFiles(string name)
		{
			return (EngineAppState.Bundles.ContainsKey(name)) ? EngineAppState.Bundles[name].Item1.ToArray() : null;
		}

		internal Dictionary<string, StringBuilder> GetHelperBundle()
		{
			return EngineSessionState.HelperBundles;
		}

		// Bindings are a poor mans IoC and even then not really. They just provide a mechanism
		// to predefine what parameters get used to invoke an action.
		internal void AddBinding(string controllerName, string actionName, object bindInstance)
		{
			controllerName.ThrowIfArgumentNull();
			actionName.ThrowIfArgumentNull();
			bindInstance.ThrowIfArgumentNull();

			if (!EngineSessionState.ActionBindings.ContainsKey(controllerName))
				EngineSessionState.ActionBindings[controllerName] = new Dictionary<string, List<object>>();

			if (!EngineSessionState.ActionBindings[controllerName].ContainsKey(actionName))
				EngineSessionState.ActionBindings[controllerName][actionName] = new List<object>();

			if (!EngineSessionState.ActionBindings[controllerName][actionName].Contains(bindInstance))
				EngineSessionState.ActionBindings[controllerName][actionName].Add(bindInstance);
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
			foreach (var actionName in actionNames)
				AddBinding(controllerName, actionName, bindInstance);
		}

		internal void AddBinding(string controllerName, string[] actionNames, object[] bindInstances)
		{
			foreach (var actionName in actionNames)
				foreach (var bindInstance in bindInstances)
					AddBinding(controllerName, actionName, bindInstance);
		}

		internal void AddBindingForAllActions(string controllerName, object bindInstance)
		{
			foreach (var actionName in GetControllerActionNames(controllerName))
				AddBinding(controllerName, actionName, bindInstance);
		}

		internal void AddBindingsForAllActions(string controllerName, object[] bindInstances)
		{
			foreach (var actionName in GetControllerActionNames(controllerName))
				foreach (var bindInstance in bindInstances)
					AddBinding(controllerName, actionName, bindInstance);
		}

		internal List<object> GetBindings(string controllerName, string actionName, string alias, Type[] initializeTypes)
		{
			var bindings = (EngineSessionState.ActionBindings.ContainsKey(controllerName) && EngineSessionState.ActionBindings[controllerName].ContainsKey(actionName)) ?
				EngineSessionState.ActionBindings[controllerName][actionName] : null;

			if (bindings != null)
			{
				var routeInfo = FindRoute(string.Format("/{0}", actionName), pathSegments);

				if (routeInfo != null && routeInfo.BoundToActionParams != null)
				{
					var boundActionParams = routeInfo.BoundToActionParams.Where(x => initializeTypes.Any(y => x.GetType() == y));

					foreach (var b in boundActionParams)
						b.Initialize(routeInfo);
				}
			}

			return bindings;
		}
		#endregion

		internal RouteInfo FindRoute(string path)
		{
			return FindRoute(path, pathSegments);
		}

		internal RouteInfo FindRoute(string path, string[] urlParameters)
		{
			path.ThrowIfArgumentNull();

			RouteInfo result = null;

			var routeSlice = GetRouteInfos(path).SelectMany(routeInfo => routeInfo.Aliases, (routeInfo, alias) => 
																	new { routeInfo, alias }).Where(x => path == x.alias)
																 .OrderBy(x => x.routeInfo.Action.GetParameters().Length)
																 .ToList();

			if (routeSlice.Any())
			{
				var allParams = new List<object>()
					.Concat(routeSlice[0].routeInfo.BoundParams)
					.Concat(urlParameters.Skip(1))
					.Concat(routeSlice[0].routeInfo.DefaultParams)
					.ToList();

				Func<Dictionary<string, string>, object[]> getModelOrParams =
					pl =>
					{
						var model = PayloadToModel(pl);
						var payloadParams = (model != null) ? new object[] { model } : pl.Values.Where(x => !EngineAppState.AntiForgeryTokens.Contains(x)).ToArray();
						return payloadParams;
					};

				switch (RequestType)
				{
					case "post":
						allParams.AddRange(getModelOrParams(form));
						break;
					case "delete":
					case "put":
						allParams.AddRange(getModelOrParams(payload));
						break;
				}

				var finalParams = allParams.ToArray();

				// This loop is pretty horrible and needs to be revised!
				foreach (var routeInfo in routeSlice
					.Where(x => x.routeInfo.Action.GetParameters().Count() >= finalParams.Count()).Select(x => x.routeInfo))
				{
					var finalParamTypes = finalParams.Select(x => x.GetType()).ToArray();
					var actionParamTypes = routeInfo.Action.GetParameters()
																										.Where(x => x.ParameterType.GetInterface("IActionFilterResult") == null)
																										.Select(x => x.ParameterType).ToArray();

					if (routeInfo.ActionParamTransforms != null && finalParamTypes.Count() == actionParamTypes.Count())
						foreach (var apt in routeInfo.ActionParamTransforms)
							finalParamTypes[apt.Item2] = actionParamTypes[apt.Item2];

					for (var i = 0; i < routeInfo.BoundParams.Count(); i++)
						if (actionParamTypes[i].IsInterface && finalParamTypes[i].GetInterface(actionParamTypes[i].Name) != null)
							finalParamTypes[i] = actionParamTypes[i];

					if (finalParamTypes.Intersect(actionParamTypes).Count() < finalParamTypes.Count())
					{
						for (var i = 0; i < finalParamTypes.Count(); i++)
						{
							if (finalParamTypes[i] == actionParamTypes[i]) continue;

							finalParams[i] = Convert.ChangeType(finalParams[i], actionParamTypes[i]);
							finalParamTypes[i] = actionParamTypes[i];
						}
					}

					var intersection = finalParamTypes.Except(actionParamTypes);

					if (actionParamTypes.Except(finalParamTypes).Any())
					{
						finalParamTypes = actionParamTypes;
						Array.Resize(ref finalParams, finalParamTypes.Length);
					}

					if (!finalParamTypes.SequenceEqual(actionParamTypes)) continue;

					routeInfo.ActionUrlParams = finalParams;
					result = routeInfo;
					_currentRoute = routeInfo;
					break;
				}
			}

			return result;
		}

		internal void RemoveRoute(string alias)
		{
			var routeInfo = EngineAppState.RouteInfos.FirstOrDefault(x => x.Aliases.FirstOrDefault(a => a == alias) != null && x.Dynamic);

			if (routeInfo != null)
				EngineAppState.RouteInfos.Remove(routeInfo);
		}

		internal void AddRoute(List<RouteInfo> routeInfos, Controller c, MethodInfo action, List<string> aliases, string defaultParams, bool dynamic)
		{
			if (EngineAppState.RouteInfos.FirstOrDefault(x => x.Action.GetParameters().Count() == action.GetParameters().Count() && x.Aliases.Except(aliases).Count() == 0) != null)
				return;

			if (action != null)
			{
				List<object> bindings = null;
				var rta = (HttpAttribute)action.GetCustomAttributes(typeof(HttpAttribute), false).FirstOrDefault();

				if (EngineSessionState.ActionBindings.ContainsKey(c.GetType().Name))
					if (EngineSessionState.ActionBindings[c.GetType().Name].ContainsKey(action.Name))
						bindings = EngineSessionState.ActionBindings[c.GetType().Name][action.Name];

				var actionParameterInfo = GetActionParameterTransforms(action.GetParameters(), bindings);

				routeInfos.Add(new RouteInfo()
				{
					Aliases = aliases,
					Action = action,
					Controller = c,
					RequestTypeAttribute = rta,
					BoundParams = (bindings != null) ? bindings.ToArray() : new object[] { },
					BoundToActionParams = (bindings != null) ? bindings.Where(x => x.GetType().GetInterface("IBoundToAction") != null).Cast<IBoundToAction>().ToArray() : null,
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

			var c = EngineSessionState.Controllers.FirstOrDefault(x => x.GetType().Name == controllerName);

			if (c != null)
			{
				var action = c.GetType().GetMethods().FirstOrDefault(x => x.GetCustomAttributes(typeof(HttpAttribute), false).Any() && x.Name == actionName);

				if (action != null)
					AddRoute(routeInfos, c, action, new List<string> { alias }, defaultParams, dynamic);
			}
		}

		// If you are creating dynamic routes it may be useful to obtain a list of all of the
		// routes the framework knows about, especially for debugging purposes.
		internal List<string> GetAllRouteAliases()
		{
			return EngineAppState.RouteInfos.SelectMany(x => x.Aliases).ToList();
		}

		internal string CreateAntiForgeryToken()
		{
			var token = CreateToken();
			EngineAppState.AntiForgeryTokens.Add(token);

			return token;
		}

		// The framework per se doesn't really care how you determine who is able to login
		// the framework only cares about who *you* think should be logged in for the purposes
		// of keeping track of the users between requests.
		internal void LogOn(string id, string[] roles, object archeType = null)
		{
			id.ThrowIfArgumentNull();
			roles.ThrowIfArgumentNull();

			if (CurrentUser != null && CurrentUser.SessionId == SessionId)
				return;

			var alreadyLoggedInWithDiffSession = EngineAppState.Users.FirstOrDefault(x => x.Name == id);

			if (alreadyLoggedInWithDiffSession != null)
				EngineAppState.Users.Remove(alreadyLoggedInWithDiffSession);

			var authCookie = new AuthCookie()
			{
				Id = id,
				AuthToken = CreateToken(),
				Expiration = DateTime.Now.Add(TimeSpan.FromHours(8))
			};

			var u = new User()
			{
				AuthenticationCookie = authCookie,
				SessionId = SessionId,
				ClientCertificate = ClientCertificate,
				IpAddress = IpAddress,
				LogOnDate = DateTime.Now,
				Name = id,
				ArcheType = archeType,
				Roles = roles.ToList()
			};

			EngineAppState.Users.Add(u);
			CurrentUser = u;
		}

		internal bool LogOff()
		{
			if (CurrentUser == null || !EngineAppState.Users.Remove(CurrentUser)) return false;

			CurrentUser = null;

			return true;
		}

		// Sessions within the controller are sandboxed.
		internal void AddControllerSession(string key, object value)
		{
			if (!string.IsNullOrEmpty(key))
				EngineSessionState.ControllersSession[key] = value;
		}

		internal object GetControllerSession(string key)
		{
			return (EngineSessionState.ControllersSession.ContainsKey(key)) ? EngineSessionState.ControllersSession[key] : null;
		}

		internal void AbandonControllerSession()
		{
			EngineSessionState.ControllersSession = null;
		}

		internal string MapPath(string path)
		{
			return AppRoot + path.Replace('/', '\\');
		}
		#endregion
		#endregion

		#region ASP.NET ADAPTER CALLBACKS
		// All of these methods are callbacks that are defined in the AspNetAdapter class. 
		// they call their ASP.NET counterparts to access the ASP.NET Application and Session
		// stores as well as obtain querystring, unvalidated form fields and access the Cache.

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

		public void ResponseRedirect(string path, bool fromRedirectOnly)
		{
			if (app.ContainsKey(HttpAdapterConstants.ResponseRedirectCallback) &&
					app[HttpAdapterConstants.ResponseRedirectCallback] is Action<string, Dictionary<string, string>>)
			{
				if (fromRedirectOnly)
					AddSession(EngineAppState.FromRedirectOnlySessionName, true);

				(app[HttpAdapterConstants.ResponseRedirectCallback] as Action<string, Dictionary<string, string>>)(path, null);
			}
		}

		public string GetValidatedFormValue(string key)
		{
			if (Request.ContainsKey(HttpAdapterConstants.RequestFormCallback) &&
					Request[HttpAdapterConstants.RequestFormCallback] is Func<string, bool, string>)
				return (Request[HttpAdapterConstants.RequestFormCallback] as Func<string, bool, string>)(key, true);

			return null;
		}

		public string GetQueryString(string key, bool validated)
		{
			string result = null;

			if (!validated)
			{
				if (Request.ContainsKey(HttpAdapterConstants.RequestQueryString) &&
						Request[HttpAdapterConstants.RequestQueryString] is Dictionary<string, string>)
					(Request[HttpAdapterConstants.RequestQueryString] as Dictionary<string, string>).TryGetValue(key, out result);
			}
			else
			{
				if (Request.ContainsKey(HttpAdapterConstants.RequestQueryStringCallback) &&
					Request[HttpAdapterConstants.RequestQueryStringCallback] is Func<string, bool, string>)
					result = (Request[HttpAdapterConstants.RequestQueryStringCallback] as Func<string, bool, string>)(key, true);
			}

			return result;
		}

		public void AddCache(string key, object value, DateTime expiresOn)
		{
			if (app.ContainsKey(HttpAdapterConstants.CacheAddCallback) &&
					app[HttpAdapterConstants.CacheAddCallback] is Action<string, object, DateTime>)
				(app[HttpAdapterConstants.CacheAddCallback] as Action<string, object, DateTime>)(key, value, expiresOn);
		}

		public object GetCache(string key)
		{
			if (app.ContainsKey(HttpAdapterConstants.CacheGetCallback) &&
				app[HttpAdapterConstants.CacheGetCallback] is Func<string, object>)
				return (app[HttpAdapterConstants.CacheGetCallback] as Func<string, object>)(key);

			return null;
		}

		public void RemoveCache(string key)
		{
			if (app.ContainsKey(HttpAdapterConstants.CacheRemoveCallback) &&
			app[HttpAdapterConstants.CacheRemoveCallback] is Action<string>)
				(app[HttpAdapterConstants.CacheRemoveCallback] as Action<string>)(key);
		}

		public void AddCookie(HttpCookie cookie)
		{
			if (app.ContainsKey(HttpAdapterConstants.CookieAddCallback) &&
				app[HttpAdapterConstants.CookieAddCallback] is Action<HttpCookie>)
				(app[HttpAdapterConstants.CookieAddCallback] as Action<HttpCookie>)(cookie);
		}

		public HttpCookie GetCookie(string name)
		{
			if (app.ContainsKey(HttpAdapterConstants.CookieGetCallback) &&
				app[HttpAdapterConstants.CookieGetCallback] is Func<string, HttpCookie>)
				return (app[HttpAdapterConstants.CookieGetCallback] as Func<string, HttpCookie>)(name);

			return null;
		}

		public void RemoveCookie(string name)
		{
			if (app.ContainsKey(HttpAdapterConstants.CookieRemoveCallback) &&
				app[HttpAdapterConstants.CookieRemoveCallback] is Action<string>)
				(app[HttpAdapterConstants.CookieRemoveCallback] as Action<string>)(name);
		}
		#endregion
	}
	#endregion
}