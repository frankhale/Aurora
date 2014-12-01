﻿// 
// Frank Hale <frankhale@gmail.com>
// 30 November 2014
//

using AspNetAdapter;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web;

namespace Aurora.Common
{
	#region ATTRIBUTES
	// FromRedirectOnly is a special action type that denotes that the action 
	// cannot normally be navigated to. Instead, another action has to redirect to 
	// it.	
	public enum ActionType { Get, Post, Put, Delete, FromRedirectOnly, GetOrPost }

	// None isn't really used other than to provide a default when used within the 
	// HttpAttribute
	public enum ActionSecurity { Secure, None }

	// This can be used to provide more than one alias to a controller action
	[AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
	public sealed class AliasAttribute : Attribute
	{
		public string Alias { get; private set; }

		public AliasAttribute(string alias)
		{
			Alias = alias;
		}
	}

	// Partitions allow you declare that a controller's views will be segrated 
	// outside of the global views directory into it's own directory. The Name 
	// attribute denotes the folder name where the Controller views will live.
	[AttributeUsage(AttributeTargets.Class)]
	public sealed class PartitionAttribute : Attribute
	{
		public string Name { get; private set; }

		public PartitionAttribute(string name)
		{
			Name = name;
		}
	}

	// This is the obligatory attribute that is used to provide meta information 
	// for controller actions. 	
	public class HttpAttribute : Attribute
	{
		// RequireAntiForgeryToken is only checked for HTTP Post and Put and Delete 
		// and doesn't make any sense to a Get.
		public bool RequireAntiForgeryToken { get; set; }
		public bool HttpsOnly { get; set; }
		public string RedirectWithoutAuthorizationTo { get; set; }
		public string RouteAlias { get; set; }
		public string Roles { get; set; }
		public string View { get; set; }
		public ActionSecurity SecurityType { get; set; }
		public ActionType ActionType { get; private set; }

		public HttpAttribute(ActionType actionType)
			: this(actionType, string.Empty,
				ActionSecurity.None) { }

		public HttpAttribute(ActionType actionType, string alias)
			: this(actionType,
				alias, ActionSecurity.None) { }

		public HttpAttribute(ActionType actionType, ActionSecurity actionSecurity)
			: this(actionType, null, actionSecurity) { }

		public HttpAttribute(ActionType actionType, string alias, ActionSecurity
			actionSecurity)
		{
			// require by default : This is only used for post/put/delete
			RequireAntiForgeryToken = true;
			SecurityType = actionSecurity;
			RouteAlias = alias;
			ActionType = actionType;
		}
	}

	#region MISCELLANEOUS
	// Used inside a model to denote that will not be shown if an HTML helper such as the 
	// HTMLTable helper is used to construct a UI representation from a collection of a 
	// particular model.
	[AttributeUsage(AttributeTargets.Property)]
	public sealed class HiddenAttribute : Attribute { }

	// This denotes that a Model property is not required in a HTTP Post if you are using
	// the model as the container for the post parameters.
	[AttributeUsage(AttributeTargets.Property)]
	public sealed class NotRequiredAttribute : Attribute { }

	// This denotes that a model property cannot be bound to from an HTTP Post if you are using
	// the model as the container for the post parameters.
	[AttributeUsage(AttributeTargets.Property)]
	public sealed class ExcludeFromBindingAttribute : Attribute { }

	// This denotes that a model property can have unsafe content such as HTML or javascript if
	// you are using the model as the container for post parameters.
	[AttributeUsage(AttributeTargets.Property | AttributeTargets.Parameter)]
	public sealed class UnsafeAttribute : Attribute { }

	// This feature isn't quite what I want yet. In the future I'd like to have this be able
	// to be figured out from the parameter list of the action rather than having to explictly
	// label a parameter with this attribute.
	//
	// This denotes that a transformation is necessary to transform incoming posted data into
	// another type.
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

	// This is used to denote that a model property is required during a post if you are using
	// the model as a container for the post parameters.
	public sealed class RequiredAttribute : ModelValidationBaseAttribute
	{
		public RequiredAttribute(string errorMessage) : base(errorMessage) { }
	}

	// This is used to denote that a model property has a required length during a post if you 
	// are using the model as a container for the post parameters.
	public sealed class RequiredLengthAttribute : ModelValidationBaseAttribute
	{
		public int Length { get; private set; }

		public RequiredLengthAttribute(int length, string errorMessage)
			: base(errorMessage)
		{
			Length = length;
		}
	}

	// This is used to denote that a model property must conform to a regular expression during 
	// a post if you are using the model as a container for the post parameters.
	public sealed class RegularExpressionAttribute : ModelValidationBaseAttribute
	{
		public Regex Pattern { get; set; }

		public RegularExpressionAttribute(string pattern, string errorMessage)
			: base(errorMessage)
		{
			Pattern = new Regex(pattern);
		}
	}

	// This is used to denote that a model property must conform to the specified range during 
	// a post if you are using the model as a container for the post parameters.
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

	#region SECURITY
	public class AuthCookie
	{
		public string Id { get; set; }
		public string AuthToken { get; set; }
		public DateTime Expiration { get; set; }
	}

	public class User
	{
		public string Name { get; internal set; }
		public AuthCookie AuthenticationCookie { get; internal set; }
		public string SessionId { get; internal set; }
		public string IpAddress { get; internal set; }
		public DateTime LogOnDate { get; internal set; }
		public List<string> Roles { get; internal set; }
		public X509Certificate2 ClientCertificate { get; internal set; }
		public object ArcheType { get; internal set; }

		public bool IsInRole(string role)
		{
			return Roles != null && Roles.Contains(role);
		}
	}
	#endregion

	#region ACTION PARAMETER TRANSFORM
	// Parameters can be transformed from an incoming representation to another representation
	// by implementing this interface and denoting the parameter in the action with the ActionParameterTransform
	// Attribute. The ActionParameterTransform takes a parameter that is the string name of the class
	// that will perform the transformation.
	public interface IActionParamTransform<T, V>
	{
		// This method will perform the transformation and return the result.
		T Transform(V value);
	}

	// Used internally to store information pertaining to action parameters transforms. This is 
	// used in the logic that processes the parameter transforms.
	internal class ActionParameterInfo
	{
		public Dictionary<string, object> ActionParamTransformInstances { get; set; }
		public List<Tuple<ActionParameterTransformAttribute, int>> ActionParamTransforms { get; set; }
	}
	#endregion

	#region ACTION BINDINGS
	// objects can be bound to the parameter list of an action. These objects can 
	// optionally implement this interface and the Initialize(RouteInfo) method 
	// will be called each time an action using the bound object. 
	public interface IBoundToAction
	{
		void Initialize(RouteInfo routeInfo);
	}

	// This notion of adding a 2 onto this interface and RouteInfo will go away
	// in time. 
	public interface IBoundToAction2
	{
		void Initialize(RouteInfo2 routeInfo);
	}
	#endregion

	#region HELPERS
	public static class MiscellaneousHelpers
	{
		public static string CreateToken()
		{
			return Guid.NewGuid().ToString().Replace("-", string.Empty);
		}
	}

	public static class ReflectionHelpers
	{
		public static List<Type> GetTypeList(Type t)
		{
			t.ThrowIfArgumentNull();

			return Utility.GetAssemblies()
							.SelectMany(x => x.GetLoadableTypes()
							.AsParallel()
							.Where(y => y.IsClass && y.BaseType == t)).ToList();
		}

		public static Type GetTypeByTypeAndName(Type t, string name)
		{
			t.ThrowIfArgumentNull();
			name.ThrowIfArgumentNull();

			return Utility.GetAssemblies()
							.SelectMany(x => x.GetLoadableTypes()
							.AsParallel()
							.Where(y => y.IsClass &&
													y.BaseType == t &&
													y.Name == name))
							.FirstOrDefault();
		}

		public static List<string> GetControllerActionNames(string controllerName)
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

		public static FrontController GetFrontControllerInstance()
		{
			/*FrontController result = null;
			var fcType = GetTypeList(typeof(FrontController)).FirstOrDefault();

			if (fcType != null)
				result = FrontController.CreateInstance(fcType, this);

			return result;*/

			throw new NotImplementedException();
		}
	}
	#endregion

	#region ASPNETADAPTER CALLBACK HELPERS
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
	#endregion

	#region ROUTING
	
	// This contains all the information necessary to associate a route with a 
	// controller method this also contains extra information pertaining to 
	// parameters we may want to pass the method at the time of invocation.
	public class RouteInfo2
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

	// The router will be responsible for determining all routes, finding routes
	// and adding and removing of dynamic routes. The router will not maintain
	// any internal state. It will simply provide the mechanism to do the work.
	// It's the responsbility of the consumer to manage any residual state.
	internal static class Routing
	{
		public static RouteInfo2 FindRoute(List<RouteInfo2> routeInfos, string path)
		{
			throw new NotImplementedException();
		}

		public static RouteInfo2 FindRoute(List<RouteInfo2> routeInfos, string path, string[] urlParameters)
		{
			throw new NotImplementedException();
		}

		public static List<RouteInfo2> GetRoutesForController(string controllerName)
		{
			var controller =
				ReflectionHelpers.GetTypeByTypeAndName(typeof(Aurora.Rewrite.Controller),
				controllerName);

			var actions = controller.GetMethods()
				.Where(x => x.GetCustomAttributes(typeof(HttpAttribute), false).Any())
				.ToList();

			var results = new List<RouteInfo2>();

			if (actions != null)
			{
				foreach (var action in actions)
				{
					var httpAttribute = action.GetCustomAttributes(typeof(HttpAttribute), false).FirstOrDefault() as HttpAttribute;
					var aliasAttributes = action.GetCustomAttributes(typeof(AliasAttribute)).Cast<AliasAttribute>();

					var aliases = new List<string>();

					foreach (var alias in aliasAttributes)
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

		public static List<RouteInfo2> GetAllRoutesForAllControllers()
		{
			var results = new List<RouteInfo2>();
			var controllers = ReflectionHelpers.GetTypeList(typeof(Aurora.Rewrite.Controller));

			foreach (var c in controllers)
			{
				var routes = GetRoutesForController(c.Name);

				if (routes.Count() > 0)
					results.AddRange(routes);
			}

			return results;
		}

		private static RouteInfo2 CreateRoute(Type controller, MethodInfo action, List<string> aliases, string defaultParams, bool dynamic)
		{
			controller.ThrowIfArgumentNull();
			action.ThrowIfArgumentNull();

			var result = new RouteInfo2()
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

		public static RouteInfo2 CreateRoute(string alias, string controllerName, string actionName, string defaultParams, bool dynamic)
		{
			throw new NotImplementedException();
		}

		public static void RemoveRoute(List<RouteInfo2> routeInfos, string alias)
		{
			throw new NotImplementedException();
		}

		public static void RemoveRoute(List<RouteInfo2> routeInfos, string alias, string controllerName, string actionName)
		{
			throw new NotImplementedException();
		}
	}
	#endregion

	#region EXTENSION METHODS / DYNAMIC DICTIONARY
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

		public static string CalculateMd5Sum(this string input)
		{
			var md5 = MD5.Create();
			var hash = md5.ComputeHash(Encoding.UTF8.GetBytes(input));

			return Convert.ToBase64String(hash);
		}

		public static string CalculateSHA1Sum(this string input)
		{
			using (SHA1Managed sha1 = new SHA1Managed())
			{
				var hash = sha1.ComputeHash(Encoding.UTF8.GetBytes(input));
				return Convert.ToBase64String(hash);
			}
		}

		public static object[] ConvertToObjectTypeArray(this string[] parms)
		{
			if (parms != null)
			{
				var _parms = new object[parms.Length];

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
					else
					{
						DateTime? dt = null;
						if (parms[i].IsDate(out dt))
							_parms[i] = dt.Value;
						else
							_parms[i] = parms[i];
					}
				}

				return _parms;
			}

			return null;
		}

		public static bool InRange(this long value, int min, int max)
		{
			return value <= max && value >= min;
		}

		public static bool IsDate(this string value, out DateTime? dt)
		{
			DateTime x;
			dt = null;

			if (!DateTime.TryParse(value, out x)) return false;

			dt = x;

			return true;
		}

		public static bool IsLong(this string value)
		{
			long x = 0;

			return long.TryParse(value, out x);
		}

		public static bool IsInt32(this string value)
		{
			var x = 0;

			return int.TryParse(value, out x);
		}

		public static bool IsDouble(this string value)
		{
			double x = 0;

			return double.TryParse(value, out x);
		}

		public static bool IsBool(this string value)
		{
			var x = false;

			return bool.TryParse(value, out x);
		}
	}

	public class DynamicDictionary : DynamicObject
	{
		private readonly Dictionary<string, object> _members = new Dictionary<string, object>();

		public bool IsEmpty()
		{
			return !(_members.Keys.Count() > 0);
		}

		public override IEnumerable<string> GetDynamicMemberNames()
		{
			return _members.Keys;
		}

		public IEnumerable<string> GetDynamicMemberNames(string key)
		{
			if (_members.ContainsKey(key) && _members[key] is DynamicDictionary)
				return (_members[key] as DynamicDictionary)._members.Keys;

			return null;
		}

		public Dictionary<string, object> AsDictionary()
		{
			return _members;
		}

		public Dictionary<string, object> AsDictionary(string key)
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
			result = (_members.ContainsKey(binder.Name)) ? _members[binder.Name] : _members[binder.Name] = new DynamicDictionary();

			return true;
		}
	}
	#endregion
}