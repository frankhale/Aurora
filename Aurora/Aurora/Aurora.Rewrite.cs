// 
// Frank Hale <frankhale@gmail.com>
// 27 November 2014
//

using AspNetAdapter;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Web;

namespace Aurora.Rewrite
{
	using Aurora.Common;

	internal class EngineAppState
	{
		public EngineAppState(Dictionary<string, object> app,
			Dictionary<string, object> request)
		{

		}
	}

	internal class EngineSessionState
	{
		public EngineSessionState(Dictionary<string, object> app, Dictionary<string,
			object> request)
		{

		}
	}

	internal class AspNetAdapterCallbacks
	{
		private Dictionary<string, object> app;
		private Dictionary<string, object> request;

		public AspNetAdapterCallbacks(Dictionary<string, object> app,
			Dictionary<string, object> request)
		{
			this.app = app;
			this.request = request;
		}

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
				//FIXME: This needs to happen at the engine level, not here!
				//if (fromRedirectOnly)
				//	AddSession(EngineAppState.FromRedirectOnlySessionName, true);

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
	}

	#region CONTROLLER
	public class BaseController
	{ }

	public class FrontController : BaseController
	{ }

	public class Controller : BaseController
	{ }
	#endregion

	#region ROUTE INFO
	// This contains all the information necessary to associate a route with a 
	// controller method this also contains extra information pertaining to 
	// parameters we may want to pass the method at the time of invocation.
	public class RouteInfo
	{
		// A list of string aliases eg. /Index, /Foo, /Bar that we want to use in 
		// order to navigate from a URL to the controller action that it represents.
		public List<string> Aliases { get; internal set; }
		public MethodInfo Action { get; internal set; }
		public Type Controller { get; internal set; }
		public HttpAttribute RequestTypeAttribute { get; internal set; }
		// The parameters passed in the URL eg. anything that is not the alias or 
		// querystring action parameters are delimited like:
		// /alias/param1/param2/param3
		public object[] UrlParams { get; internal set; }
		// The parameters that are bound to this action that are declared in an 
		// OnInit handler.
		public object[] BoundParams { get; internal set; }
		// Default parameters are used if you want to mask a more complex URL with 
		// just an alias.
		public object[] DefaultParams { get; internal set; }
		public IBoundToAction[] BoundToActionParams { get; internal set; }
		//public List<Tuple<ActionParameterTransformAttribute, int>> 
		//	ActionParamTransforms { get; internal set; }
		//public Dictionary<string, object> CachedActionParamTransformInstances 
		//	{ get; internal set; }
		// Routes that are created by the framework are not dynamic. Dynamic routes 
		// are created in the controller by the end user.
		public bool Dynamic { get; internal set; }
	}
	#endregion

	// The router will be responsible for determining all routes, finding routes
	// and adding and removing of dynamic routes. The router will not maintain
	// any internal state. It will simply provide the mechanism to do the work.
	// It's the responsbility of the consumer to manage any residual state.
	internal static class Routing
	{
		public static RouteInfo FindRoute(List<RouteInfo> routeInfos, string path)
		{
			throw new NotImplementedException();
		}

		public static RouteInfo FindRoute(List<RouteInfo> routeInfos, string path, string[] urlParameters)
		{
			throw new NotImplementedException();
		}

		public static List<RouteInfo> GetRoutesForController(string controllerName)
		{
			var controller =
				ReflectionHelpers.GetTypeByTypeAndName(typeof(Controller),
				controllerName);

			var actions = controller.GetMethods()
				.Where(x => x.GetCustomAttributes(typeof(HttpAttribute), false).Any())
				.ToList();

			var results = new List<RouteInfo>();

			if (actions != null)
			{
				foreach (var action in actions)
				{
					var httpAttribute = action.GetCustomAttributes(typeof(HttpAttribute), false).FirstOrDefault() as HttpAttribute;
					var aliasAttributes = action.GetCustomAttributes(typeof(AliasAttribute)).Cast<AliasAttribute>();

					var aliases = new List<string>();

					foreach(var alias in aliasAttributes)
					{
						aliases.Add(alias.Alias);
					}

					aliases.Add(httpAttribute.RouteAlias);
					aliases.Add(string.Format("/{0}/{1}", controller.Name, httpAttribute.RouteAlias));

					var route = CreateRoute(controller, action, aliases, null, false);

					results.Add(route);
				}
			}

			return results;
		}

		public static List<RouteInfo> GetAllRoutesForAllControllers()
		{
			var results = new List<RouteInfo>();
			var controllers = ReflectionHelpers.GetTypeList(typeof(Controller));

			foreach(var c in controllers)
			{
				var routes = GetRoutesForController(c.Name);
				
				if(routes.Count()>0)
					results.AddRange(routes);
			}

			return results;
		}

		private static RouteInfo CreateRoute(Type controller, MethodInfo action, List<string> aliases, string defaultParams, bool dynamic)
		{
			controller.ThrowIfArgumentNull();
			action.ThrowIfArgumentNull();

			var result = new RouteInfo()
			{
				Aliases = aliases,
				Action = action,
				Controller = controller,
				RequestTypeAttribute = (HttpAttribute)action.GetCustomAttributes(typeof(HttpAttribute), false).FirstOrDefault(),
				DefaultParams = (!string.IsNullOrEmpty(defaultParams)) ? defaultParams.Split('/').ConvertToObjectTypeArray() : new object[] { },
				Dynamic = dynamic
			};

			return result;
		}

		public static RouteInfo CreateRoute(string alias, string controllerName, string actionName, string defaultParams, bool dynamic)
		{
			throw new NotImplementedException();
		}

		public static void RemoveRoute(List<RouteInfo> routeInfos, string alias)
		{
			throw new NotImplementedException();
		}

		public static void RemoveRoute(List<RouteInfo> routeInfos, string alias, string controllerName, string actionName)
		{
			throw new NotImplementedException();
		}
	}

	internal class Engine : IAspNetAdapterApplication
	{
		private EngineAppState engineAppState;
		private EngineSessionState engineSessionState;
		private AspNetAdapterCallbacks aspNetAdapterCallbacks;

		public void Init(Dictionary<string, object> app,
										 Dictionary<string, object> request,
										 Action<Dictionary<string, object>> response)
		{
			engineAppState = new EngineAppState(app, request);
			engineSessionState = new EngineSessionState(app, request);
			aspNetAdapterCallbacks = new AspNetAdapterCallbacks(app, request);

			var routeInfos = Routing.GetAllRoutesForAllControllers();
		}
	}
}
