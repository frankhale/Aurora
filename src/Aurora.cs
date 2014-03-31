//
// Aurora - An MVC web framework for .NET
//
// Updated On: 28 March 2014
//
// Source Code Location:
//
//	https://github.com/frankhale
//
// Requirements: .NET 4.0 or higher
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
//   action 
//   type and you can secure them with the ActionSecurity named parameter.
// - Actions can have filters with optional filter results that bind to action
//   parameters.  
// - Actions can have aliases. Aliases can also be added dynamically at runtime
//   along with default parameters.
// - Bundling/Minifying of Javascript and CSS.
// - Html Helpers
//
// ---------------------
// --- Documentation ---
// ---------------------
// 
// Aurora is an MVC web framework for .NET and aims to provide a lot of 
// functionality in a slim codebase. 
// 
// Aurora maintains a simplistic vision of MVC in that the primary focus is on 
// controllers, actions and views and models are this loose thing that can be 
// any number of things that allow you to manipulate your data and validate it. 
// In Aurora there is a Model base class which can be used for validation of 
// posted models or be used in HTML helpers. Aurora kind of expects that the 
// programmer will have his own notion of models and construct them in the way 
// that makes sense for them rather than providing a bunch of hurdles and 
// obstacles to jump through. Aurora considers Models as something that is 
// intermediate or a view model where it's this entity that exists in a 
// transitional state either getting ready to be converted into an HTML fragment 
// or be validated from a post request and then put into a database. 
//
// Controllers are simply classes that inherit from the Controller base class. 
// Controllers have a series of events that can be listened to in order to 
// perform logic at specific points in time. Aurora provides a FrontController 
// base class that can be inherited to allow for interception of requests before 
// they are dispatched to the proper controller.  Regular controllers can have 
// actions (C# methods), these actions are fired per the request path and once 
// these actions are complete will return a view. Views can be HTML views, 
// partial HTMl views, file views, JSON views or string views. String views are 
// a way to custom format a string response in any way you need.
//
// A quick note on how Aurora interfaces with ASP.NET. Aurora uses a small class 
// that bootstraps the framework and provides it with simple dictionaries with 
// the request data, response and callbacks to interact with a few aspects of 
// ASP.NET such as Cache, Session, Application, Cookies and sending the final 
// response.
//
// As of now there are no Visual Studio templates or similiar helpers to assist 
// in project management. 
//
// The best way to see Aurora in action is to look at a small wiki application 
// called Miranda which can be found on my github site (link above).
//  
// Aurora's project structure is flexible but relies on the folder that contains 
// views having the name Views and a structure of subdirectories named 
// Fragments, Shared and one directory for each controller with a structure 
// underneath it with Fragments and Shared directories.
// 
// Here is a typical project structure (almost all of which is by convention):
// 
// AppName/
// AppName/Web.Config
// AppName/favicon.ico
// AppName/Controllers
// AppName/Models
// AppName/Resources/
// AppName/Resources/Scripts
// AppName/Resources/Styles
// AppName/Resources/Images
// AppName/Views
// AppName/Views/Fragments
// AppName/Views/Shared
// AppName/Views/ControllerName
// AppName/Views/ControllerName/Fragments
// AppName/Views/ControllerName/Shared
//
// The Views folder is really the only hard assumption that is made by the 
// framework on where files it needs should exist. This folder will contain all 
// of the views used for master pages or partials that will be shared between 
// controllers. Additionally, this will include any globally reachable HTML 
// fragments. If not using Controller partitions then all controllers should 
// have a folder underneath Views that is named with the controller name to 
// place their specific views.
//
// NOTE: The sample Aurora projects contained on my github account all include
// Aurora in the project solution instead of just referencing a precompiled 
// class library. This is being done on purpose to make it easier to debug web 
// apps using the framework. Additionally, the framework proper is just a single 
// source file so in theory can and should be tailored to the needs of the 
// application using it.
//
// Here is a minimal web.config
//
// <?xml version="1.0"?>
// <configuration>
//   <system.web>
//     <compilation debug="true" targetFramework="4.0" />
//     <customErrors mode="On"/>
//     <httpHandlers>
//       <add verb="*" path="*" validate="false" 
//					type="AspNetAdapter.AspNetAdapterHandler"/>
//     </httpHandlers>
//     <httpModules>
//       <add type="AspNetAdapter.AspNetAdapterModule" 
//					name="AspNetAdapterModule"/>
//     </httpModules>    
//   </system.web>
// </configuration>
//
// ------------------
// --- Attributes ---
// ------------------
// 
// All actions use the Http attribute to denote how the framework should 
// interpret them.
// 
// A typical usage would be:
//
//  [Http(ActionType.Get, "/Index")]
//  public ViewResult Index() 
//  {
//     // Todo...
//
//		 return View();
//  }
//
// The following action types can be used for actions to denote what HTTP method 
// they respond to:
//
//  Get, Post, GetOrPost, Put, Delete, FromRedirectOnly
//
// The Http attribute has the following public properties:
//
// NOTE: Aurora provides only one attribute to denote metadata for a route even 
//			 though at least one property RequireAntiForgeryToken makes no sense in 
//			 terms of an HTTP Get, FromRedirectOnly verb. This property is only 
//			 checked in instances of Post, Put or Delete.
//
// 		bool RequireAntiForgeryToken 
//		bool HttpsOnly 
//		string RedirectWithoutAuthorizationTo 
//		string RouteAlias 
//		string Roles 
//		string View 
//		ActionSecurity SecurityType
//
// RequireAntiForgeryToken is fulfilled by placing a view compiler directive in 
// your view inside your form so that it will place the antiforgery token 
// element into your form.
//
// To add an antiforgery token to a form use the following: (more on views 
// later)
//
// <input type="hidden" name="AntiForgeryToken" value="%%AntiForgeryToken%%" />
//
// ------------------------
// --- Front Controller ---
// ------------------------
//
// The front controller is optional. When inherited it allows notification of 
// various events that happen ramping up to the invocation of an action. You can 
// theoretically divert (different from an ordinary redirect) a route but the 
// usefulness of this has yet to be determined. FrontController's have the 
// ability to redirect to another route in the traditional sense of redirecting.
//
// FrontController's can be used to set up bundles, add dynamic routes, logging.
//
// -------------------
// --- Controllers ---
// -------------------
// 
// Controllers are classes that inherit from the Controller base class. 
// Controllers can optionally listen for events. These events are triggered at
// various points in the lifecycle of the request. The following events are able
// to be listened to by Controllers:
//
// OnInit: 
//
//	Only fired when the controller instance is created.
//
// OnCheckRoles: 
//	
//	If an action is secure this gives you the opportunity to check the users 
//	roles before the action is invoked and either grant access or not.
//
// OnPreAction: 
//
//	This event executes right before the action is invoked.
//
// OnPostAction:
//
//	This event executes right after the action is invoked.
//
// ---------------
// --- Actions ---
// ---------------
//
// Actions are nothing more than C# methods decorated with the Http attribute.
// Actions will normally return a IViewResult or optionally redirect to other 
// views, for instance an action that responds to an Http Post and has a void
// return type would need to redirect to an action that returns a IViewResult.
//
// --------------------
// --- View Results ---
// --------------------
//
// The following types of view results are built in:
//
// ViewResult:
//
//	This is an ordinary HTML view result
//
// FileResult:
//
//	This result is for a physical file or a file built on the fly
//
// JsonResult:
//
//	This is a very basic result that processes a type through Newtonsoft.JSON
//  serialize object. If you need more control over the JSON result you can hack
//  the code or use the StringResult instead.
//
// StringResult:
//
//	The string result was added to allow for times when the simple JsonResult 
//	was not good enough. You can format your string in any way you like and it
//  will be sent to the client as is.
//
// -------------
// --- Views ---
// -------------
// 
// All views (except fragments) are compiled a head of time and are written to 
// a JSON formatted file in the Cache directory under Views.
//
// -----------------------
// --- Action Bindings ---
// -----------------------
//
// Action bindings are somewhat of a poor man's IoC. They denote that actions
// will have additional parameters passed to them at the time they are invoked.
// This is great for instances of database access objects, user objects, etc...
// 
// Action bindings can be custom types that implement the IBoundToAction 
// interface. This interface allows you to perform some custom initialization
// logic that will be executed right after an instance of this object is 
// created.
//
// -----------------------------------
// --- Action Parameter Transforms ---
// -----------------------------------
//
// Action parameter transforms are a mechanism to transpose incoming request
// variables into another type. For instance, say an incoming request variable
// was a string representing a username, this could then be transformed into
// a user object so that the action would not need to perform the transformation
// itself and instead receive the user object as a parameter to the action.
//
// ----------------------
// --- Action Filters ---
// ----------------------
//
// TODO
//
// --------------
// --- Models ---
// --------------
//
// TODO
//
// ---------------
// --- Bundles ---
// ---------------
//
// TODO
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

using System.Web;
using AspNetAdapter;
using MarkdownSharp;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Dynamic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Xml.Linq;
using Yahoo.Yui.Compressor;

#region ASSEMBLY INFO
[assembly: AssemblyTitle("Aurora")]
[assembly: AssemblyDescription("A Tiny MVC web framework for .NET")]
[assembly: AssemblyCompany("Frank Hale")]
[assembly: AssemblyProduct("Aurora")]
[assembly: AssemblyCopyright("Copyright © 2014 | LICENSE GNU GPLv3")]
[assembly: ComVisible(false)]
[assembly: CLSCompliant(true)]
[assembly: AssemblyVersion("2.0.53.0")]
#endregion

namespace Aurora
{
	#region ATTRIBUTES
	// None isn't really used other than to provide a default when used within the HttpAttribute
	public enum ActionSecurity { Secure, None }

	#region HTTP REQUEST
	// FromRedirectOnly is a special action type that denotes that the action cannot
	// normally be navigated to. Instead, another action has to redirect to it.	
	public enum ActionType { Get, Post, Put, Delete, FromRedirectOnly, GetOrPost }

	// This is the obligatory attribute that is used to provide meta information for controller actions. 	
	public class HttpAttribute : Attribute
	{
		// RequireAntiForgeryToken is only checked for HTTP Post and Put and Delete and doesn't make any sense to a Get.
		public bool RequireAntiForgeryToken { get; set; }
		public bool HttpsOnly { get; set; }
		public string RedirectWithoutAuthorizationTo { get; set; }
		public string RouteAlias { get; set; }
		public string Roles { get; set; }
		public string View { get; set; }
		public ActionSecurity SecurityType { get; set; }
		public ActionType ActionType { get; private set; }

		public HttpAttribute(ActionType actionType) : this(actionType, string.Empty, ActionSecurity.None) { }

		public HttpAttribute(ActionType actionType, string alias) : this(actionType, alias, ActionSecurity.None) { }

		public HttpAttribute(ActionType actionType, ActionSecurity actionSecurity) : this(actionType, null, actionSecurity) { }

		public HttpAttribute(ActionType actionType, string alias, ActionSecurity actionSecurity)
		{
			RequireAntiForgeryToken = true; // require by default : This is only used for post/put/delete
			SecurityType = actionSecurity;
			RouteAlias = alias;
			ActionType = actionType;
		}
	}
	#endregion

	#region MISCELLANEOUS
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

	// Partitions allow you declare that a controller's views will be segrated outside
	// of the global views directory into it's own directory. The name of this directory
	// is the same as the controller.
	[AttributeUsage(AttributeTargets.Class)]
	public sealed class PartitionAttribute : Attribute
	{
		public string Name { get; private set; }

		public PartitionAttribute(string name)
		{
			Name = name;
		}
	}

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

	#region FRAMEWORK ENGINE STATE
	// EngineAppState maps to state that is stored in the ASP.NET Application store.
	internal class EngineAppState
	{
		public static readonly Regex AllowedFilePattern = new Regex(@"^.*\.(js|css|png|jpg|gif|ico|pptx|xlsx|csv|txt)$", RegexOptions.Compiled | RegexOptions.IgnoreCase);
		public static readonly Regex CssorJsExtPattern = new Regex(@"^.*\.(js|css)$", RegexOptions.Compiled | RegexOptions.IgnoreCase);
		public static readonly string EngineAppStateSessionName = "__ENGINE_APP_STATE__";
		public static readonly string EngineSessionStateSessionName = "__ENGINE_SESSION_STATE__";
		public static readonly string FromRedirectOnlySessionName = "__FromRedirectOnly";
		public static readonly string SharedResourceFolderPath = "/Resources";
		public static readonly string CompiledViewsCacheFolderPath = "/Views/Cache";
		public static readonly string CompiledViewsCacheFileName = "viewsCache.json";
		public static readonly string AntiForgeryTokenName = "AntiForgeryToken";

		public ViewEngine ViewEngine { get; set; }
		public List<User> Users { get; set; }
		public List<string> AntiForgeryTokens { get; set; }
		public Dictionary<string, string> ProtectedFiles { get; set; }
		public Dictionary<string, object> ControllersSession { get; set; }
		public List<Type> Models { get; set; }
		public string CacheFilePath { get; set; }
		public List<RouteInfo> RouteInfos { get; set; }
		public string[] ViewRoots { get; set; }
		public List<IViewCompilerDirectiveHandler> ViewEngineDirectiveHandlers { get; set; }
		public List<IViewCompilerSubstitutionHandler> ViewEngineSubstitutionHandlers { get; set; }
		public Dictionary<string, Tuple<List<string>, string>> Bundles { get; set; }
	}

	// EngineSessionState maps to state that is stored in the ASP.NET Session store.
	internal class EngineSessionState
	{
		public FrontController FrontController { get; set; }
		public List<Controller> Controllers { get; set; }
		// I've wanted to put this in app state but it's problematic because the OnInit method
		// for a controller is ran for each new session and that is kind of where I want to add
		// new bindings which means they will run each time an instance of a controller is created.
		// I haven't figured out a good method to ignore this. So since adding bindings is something
		// that happens per controller instance it is stuck here for now even though bindings won't
		// change between instances. 
		public Dictionary<string, Dictionary<string, List<object>>> ActionBindings { get; set; }
		// Helper bundles are impromptu bundles that are added by HTML Helpers
		public Dictionary<string, StringBuilder> HelperBundles { get; set; }
		public List<MethodInfo> ControllerActions { get; set; }
		public bool FromRedirectOnly { get; set; }
		public User CurrentUser { get; set; }
	}
	#endregion

	#region FRAMEWORK ENGINE
	internal class Engine : IAspNetAdapterApplication
	{
		#region ASP.NET ADAPTER STUFF
		private Dictionary<string, object> _app;
		internal Dictionary<string, object> Request;
		private Dictionary<string, string> _queryString, _cookies, _form, _payload;
		private Action<Dictionary<string, object>> _response;
		private List<PostedFile> _files;
		private string[] _pathSegments;
		private Exception _serverError;
		internal X509Certificate2 ClientCertificate { get; private set; }
		internal string IpAddress, Path, RequestType, AppRoot, ViewRoot, SessionId, Identity;
		internal Uri Url;
		#endregion

		#region MISCELLANEOUS VARIABLES
		internal EngineAppState EngineAppState;
		internal EngineSessionState EngineSessionState;
		private bool _debugMode;
		internal User CurrentUser;
		private RouteInfo _currentRoute;
		#endregion

		#region FRAMEWORK METHODS

		public void Init(Dictionary<string, object> app, Dictionary<string, object> request,
			Action<Dictionary<string, object>> response)
		{
			_app = app;
			Request = request;
			_response = response;

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
			_pathSegments = request[HttpAdapterConstants.RequestPathSegments] as string[];
			_cookies = request[HttpAdapterConstants.RequestCookie] as Dictionary<string, string>;
			_form = request[HttpAdapterConstants.RequestForm] as Dictionary<string, string>;
			_payload = request[HttpAdapterConstants.RequestBody] as Dictionary<string, string>;
			_files = request[HttpAdapterConstants.RequestFiles] as List<PostedFile>;
			_queryString = request[HttpAdapterConstants.RequestQueryString] as Dictionary<string, string>;
			_debugMode = Convert.ToBoolean(app[HttpAdapterConstants.DebugMode]);
			_serverError = app[HttpAdapterConstants.ServerError] as Exception;
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

			if (EngineAppState.ControllersSession == null)
				EngineAppState.ControllersSession = new Dictionary<string, object>();

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

			if (!EngineAppState.AllowedFilePattern.IsMatch(Path) && (EngineAppState.ViewEngine == null || _debugMode))
			{
				string viewCache = null;

				if (EngineAppState.ViewEngineDirectiveHandlers == null && EngineAppState.ViewEngineSubstitutionHandlers == null)
				{
					EngineAppState.ViewEngineDirectiveHandlers = new List<IViewCompilerDirectiveHandler>();
					EngineAppState.ViewEngineSubstitutionHandlers = new List<IViewCompilerSubstitutionHandler>();

					EngineAppState.ViewEngineDirectiveHandlers.Add(new MasterPageDirective());
					EngineAppState.ViewEngineDirectiveHandlers.Add(new PlaceHolderDirective());
					EngineAppState.ViewEngineDirectiveHandlers.Add(new PartialPageDirective());
					EngineAppState.ViewEngineDirectiveHandlers.Add(new BundleDirective(_debugMode,
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

				if (File.Exists(EngineAppState.CacheFilePath) && !_debugMode)
					viewCache = File.ReadAllText(EngineAppState.CacheFilePath);

				if (EngineAppState.ViewRoots == null)
					EngineAppState.ViewRoots = GetViewRoots();

				EngineAppState.ViewEngine = new ViewEngine(AppRoot, EngineAppState.ViewRoots,
					EngineAppState.ViewEngineDirectiveHandlers, EngineAppState.ViewEngineSubstitutionHandlers, viewCache);

				if (string.IsNullOrEmpty(viewCache) || !_debugMode)
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
			Exception exception = _serverError;
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
				if (_debugMode)
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
					_pathSegments[0] = "Index";
				}

				RaiseEventOnFrontController(RouteHandlerEventType.PreRoute, Path, null, null);

				routeInfo = FindRoute(string.Concat("/", _pathSegments[0]), _pathSegments);

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
						if (!(_form.ContainsKey(EngineAppState.AntiForgeryTokenName) || _payload.ContainsKey(EngineAppState.AntiForgeryTokenName)))
							throw new Exception("An AntiForgeryToken is required on all forms by default.");
						else
						{
							EngineAppState.AntiForgeryTokens.Remove(_form[EngineAppState.AntiForgeryTokenName]);
							EngineAppState.AntiForgeryTokens.Remove(_payload[EngineAppState.AntiForgeryTokenName]);
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
				_response(new Dictionary<string, object>
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
			var controllers = GetTypeList(typeof(Controller));
			var partitionAttributes = controllers.SelectMany(x =>
				x.GetCustomAttributes(typeof(PartitionAttribute), false).Cast<PartitionAttribute>());

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
						if (_files.Any())
							p.SetValue(result, _files[0], null);
					}
					else if (p.PropertyType == typeof(List<PostedFile>))
						p.SetValue(result, _files, null);
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
							.SelectMany(x => x.GetTypes()
											.AsParallel()
											.Where(y => y.IsClass && y.BaseType == t)).ToList();
		}

		private static List<string> GetControllerActionNames(string controllerName)
		{
			controllerName.ThrowIfArgumentNull();

			var result = new List<string>();

			var controller = GetTypeList(typeof(Controller)).FirstOrDefault(x => x.Name == controllerName);

			if (controller != null)
			{
				result = controller.GetMethods()
													 .Where(x => x.GetCustomAttributes(typeof(HttpAttribute), false).Any())
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
				ctrlInstance = Controller.CreateInstance(GetTypeList(typeof(Controller)).FirstOrDefault(x => x.Name == controllerName), this);
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
															.SelectMany(x => x.GetTypes().Where(y => y.GetInterface(typeof(IActionParamTransform<,>).Name) != null && y.Name == apt.Item1.TransformName))
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
			return (Guid.NewGuid().ToString() + Guid.NewGuid().ToString()).Replace("-", string.Empty);
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

			if (!_debugMode)
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

			if (!_debugMode)
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
				var routeInfo = FindRoute(string.Format("/{0}", actionName), _pathSegments);

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
			return FindRoute(path, _pathSegments);
		}

		internal RouteInfo FindRoute(string path, string[] urlParameters)
		{
			path.ThrowIfArgumentNull();

			RouteInfo result = null;

			var routeSlice = GetRouteInfos(path).SelectMany(routeInfo => routeInfo.Aliases, (routeInfo, alias) => new { routeInfo, alias }).Where(x => path == x.alias)
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
						allParams.AddRange(getModelOrParams(_form));
						break;
					case "delete":
					case "put":
						allParams.AddRange(getModelOrParams(_payload));
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
			string token = CreateToken();

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
				EngineAppState.ControllersSession[key] = value;
		}

		internal object GetControllerSession(string key)
		{
			return (EngineAppState.ControllersSession.ContainsKey(key)) ? EngineAppState.ControllersSession[key] : null;
		}

		internal void AbandonControllerSession()
		{
			EngineAppState.ControllersSession = null;
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
			if (_app.ContainsKey(HttpAdapterConstants.ApplicationSessionStoreGetCallback) &&
					_app[HttpAdapterConstants.ApplicationSessionStoreGetCallback] is Func<string, object>)
				return (_app[HttpAdapterConstants.ApplicationSessionStoreGetCallback] as Func<string, object>)(key);

			return null;
		}

		public void AddApplication(string key, object value)
		{
			if (_app.ContainsKey(HttpAdapterConstants.ApplicationSessionStoreAddCallback) &&
					_app[HttpAdapterConstants.ApplicationSessionStoreAddCallback] is Action<string, object>)
				(_app[HttpAdapterConstants.ApplicationSessionStoreAddCallback] as Action<string, object>)(key, value);
		}

		public object GetSession(string key)
		{
			if (_app.ContainsKey(HttpAdapterConstants.UserSessionStoreGetCallback) &&
					_app[HttpAdapterConstants.UserSessionStoreGetCallback] is Func<string, object>)
				return (_app[HttpAdapterConstants.UserSessionStoreGetCallback] as Func<string, object>)(key);

			return null;
		}

		public void AddSession(string key, object value)
		{
			if (_app.ContainsKey(HttpAdapterConstants.UserSessionStoreAddCallback) &&
					_app[HttpAdapterConstants.UserSessionStoreAddCallback] is Action<string, object>)
				(_app[HttpAdapterConstants.UserSessionStoreAddCallback] as Action<string, object>)(key, value);
		}

		public void RemoveSession(string key)
		{
			if (_app.ContainsKey(HttpAdapterConstants.UserSessionStoreRemoveCallback) &&
					_app[HttpAdapterConstants.UserSessionStoreRemoveCallback] is Action<string>)
				(_app[HttpAdapterConstants.UserSessionStoreRemoveCallback] as Action<string>)(key);
		}

		public void ResponseRedirect(string path, bool fromRedirectOnly)
		{
			if (_app.ContainsKey(HttpAdapterConstants.ResponseRedirectCallback) &&
					_app[HttpAdapterConstants.ResponseRedirectCallback] is Action<string, Dictionary<string, string>>)
			{
				if (fromRedirectOnly)
					AddSession(EngineAppState.FromRedirectOnlySessionName, true);

				(_app[HttpAdapterConstants.ResponseRedirectCallback] as Action<string, Dictionary<string, string>>)(path, null);
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
			if (_app.ContainsKey(HttpAdapterConstants.CacheAddCallback) &&
					_app[HttpAdapterConstants.CacheAddCallback] is Action<string, object, DateTime>)
				(_app[HttpAdapterConstants.CacheAddCallback] as Action<string, object, DateTime>)(key, value, expiresOn);
		}

		public object GetCache(string key)
		{
			if (_app.ContainsKey(HttpAdapterConstants.CacheGetCallback) &&
				_app[HttpAdapterConstants.CacheGetCallback] is Func<string, object>)
				return (_app[HttpAdapterConstants.CacheGetCallback] as Func<string, object>)(key);

			return null;
		}

		public void RemoveCache(string key)
		{
			if (_app.ContainsKey(HttpAdapterConstants.CacheRemoveCallback) &&
			_app[HttpAdapterConstants.CacheRemoveCallback] is Action<string>)
				(_app[HttpAdapterConstants.CacheRemoveCallback] as Action<string>)(key);
		}

		public void AddCookie(HttpCookie cookie)
		{
			if (_app.ContainsKey(HttpAdapterConstants.CookieAddCallback) &&
				_app[HttpAdapterConstants.CookieAddCallback] is Action<HttpCookie>)
				(_app[HttpAdapterConstants.CookieAddCallback] as Action<HttpCookie>)(cookie);
		}

		public HttpCookie GetCookie(string name)
		{
			if (_app.ContainsKey(HttpAdapterConstants.CookieGetCallback) &&
				_app[HttpAdapterConstants.CookieGetCallback] is Func<string, HttpCookie>)
				return (_app[HttpAdapterConstants.CookieGetCallback] as Func<string, HttpCookie>)(name);

			return null;
		}

		public void RemoveCookie(string name)
		{
			if (_app.ContainsKey(HttpAdapterConstants.CookieRemoveCallback) &&
				_app[HttpAdapterConstants.CookieRemoveCallback] is Action<string>)
				(_app[HttpAdapterConstants.CookieRemoveCallback] as Action<string>)(name);
		}
		#endregion
	}
	#endregion

	#region ROUTE INFO
	// This contains all the information necessary to associate a route with a controller method
	// this also contains extra information pertaining to parameters we may want to pass the method
	// at the time of invocation.
	public class RouteInfo
	{
		// A list of string aliases eg. /Index, /Foo, /Bar that we want to use in order to navigate
		// from a URL to the controller action that it represents.
		public List<string> Aliases { get; internal set; }
		public MethodInfo Action { get; internal set; }
		public Controller Controller { get; internal set; }
		public HttpAttribute RequestTypeAttribute { get; internal set; }
		// The parameters passed in the URL eg. anything that is not the alias or querystring
		// action parameters are delimited like /alias/param1/param2/param3
		public object[] ActionUrlParams { get; internal set; }
		// The parameters that are bound to this action that are declared in an OnInit method of 
		// the constructor
		public object[] BoundParams { get; internal set; }
		// Default parameters are used if you want to mask a more complex URL with just an alias
		public object[] DefaultParams { get; internal set; }
		public IBoundToAction[] BoundToActionParams { get; internal set; }
		public List<Tuple<ActionParameterTransformAttribute, int>> ActionParamTransforms { get; internal set; }
		public Dictionary<string, object> CachedActionParamTransformInstances { get; internal set; }
		// Routes that are created by the framework are not dynamic. Dynamic routes are created 
		// in the controller.
		public bool Dynamic { get; internal set; }
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
		public User CurrentUser { get { return _engine.CurrentUser; } }
		public abstract void OnFilter(RouteInfo routeInfo);

		internal void Init(Engine e)
		{
			_engine = e;
		}

		#region WRAPPERS FOR ENGINE METHODS
		protected RouteInfo FindRoute(string path)
		{
			return _engine.FindRoute(path);
		}
		protected RouteInfo FindRoute(string path, string[] urlParameters)
		{
			return _engine.FindRoute(path, urlParameters);
		}
		protected void Redirect(string alias)
		{
			_engine.ResponseRedirect(alias, false);
		}
		protected void RedirectOnly(string alias)
		{
			_engine.ResponseRedirect(alias, true);
		}
		protected void LogOn(string id, string[] roles, object archeType = null)
		{
			_engine.LogOn(id, roles, archeType);
		}
		protected void LogOff()
		{
			_engine.LogOff();
		}
		protected void AddApplication(string key, object value)
		{
			_engine.AddApplication(key, value);
		}
		protected object GetApplication(string key)
		{
			return _engine.GetApplication(key);
		}
		protected void AddSession(string key, object value)
		{
			_engine.AddControllerSession(key, value);
		}
		protected object GetSession(string key)
		{
			return _engine.GetControllerSession(key);
		}
		protected void AddCache(string key, object value, DateTime expiresOn)
		{
			_engine.AddCache(key, value, expiresOn);
		}
		protected object GetCache(string key)
		{
			return _engine.GetCache(key);
		}
		protected void RemoveCache(string key)
		{
			_engine.RemoveCache(key);
		}
		protected void AbandonSession()
		{
			_engine.AbandonControllerSession();
		}
		protected string GetQueryString(string key, bool validate)
		{
			return _engine.GetQueryString(key, validate);
		}
		protected string MapPath(string path)
		{
			return _engine.MapPath(path);
		}
		protected void AddCookie(HttpCookie cookie)
		{
			_engine.AddCookie(cookie);
		}
		protected HttpCookie GetCookie(string name)
		{
			return _engine.GetCookie(name);
		}
		protected void RemoveCookie(string name)
		{
			_engine.RemoveCookie(name);
		}
		#endregion
	}
	#endregion

	#region ACTION BINDINGS
	// objects can be bound to the parameter list of an action. These objects can optionally
	// implement this interface and the Initialize(RouteInfo) method will be called each time
	// an action using the bound object. 
	public interface IBoundToAction
	{
		void Initialize(RouteInfo routeInfo);
	}
	#endregion

	#region MODEL
	// Models in the sense of Aurora are more of an intermediate mechanism in order to transition
	// from one state to another. For instance from a form post to an action so that all parameters
	// are grouped into a model instance or from a collection to a view for instance when using
	// the HTMLTable helper. 
	//
	// Models have the obligatory set of property validators, Required, Required Length, Regular Expression
	// and Range attributes.
	public abstract class Model
	{
		[Hidden]
		public bool IsValid { get; private set; }
		[Hidden]
		public string Error { get; private set; }

		private static bool ValidateRequiredLengthAttribute(RequiredLengthAttribute requiredLengthAttribute, PropertyInfo property, object value, out string error)
		{
			var result = false;
			error = string.Empty;

			if (requiredLengthAttribute != null)
			{
				var sValue = value as string;

				if (!string.IsNullOrEmpty(sValue))
					result = (sValue.Length >= requiredLengthAttribute.Length) ? true : false;
			}

			if (!result)
				error = string.Format("{0} has a required length that was not met", property.Name);

			return result;
		}

		private static bool ValidateRequiredAttribute(RequiredAttribute requiredAttribute, PropertyInfo property, object value, out string error)
		{
			var result = false;
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

		private static bool ValidateRegularExpressionAttribute(RegularExpressionAttribute regularExpressionAttribute, PropertyInfo property, object value, out string error)
		{
			var result = false;
			error = string.Empty;

			if (regularExpressionAttribute != null)
			{
				var sValue = value as string;

				if (!string.IsNullOrEmpty(sValue))
					result = (regularExpressionAttribute.Pattern.IsMatch(sValue)) ? true : false;
				else
					result = false;
			}

			if (!result)
				error = string.Format("{0} did not pass regular expression validation", property.Name);

			return result;
		}

		private static bool ValidateRangeAttribute(RangeAttribute rangeAttribute, PropertyInfo property, object value, out string error)
		{
			var result = false;
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
			var results = new List<bool>();
			var errors = new StringBuilder();

			foreach (var pi in GetPropertiesWithExclusions(GetType(), false))
			{
				var requiredAttribute = (RequiredAttribute)pi.GetCustomAttributes(false).FirstOrDefault(x => x is RequiredAttribute);
				var requiredLengthAttribute = (RequiredLengthAttribute)pi.GetCustomAttributes(false).FirstOrDefault(x => x is RequiredLengthAttribute);
				var regularExpressionAttribute = (RegularExpressionAttribute)pi.GetCustomAttributes(false).FirstOrDefault(x => x is RegularExpressionAttribute);
				var rangeAttribute = (RangeAttribute)pi.GetCustomAttributes(false).FirstOrDefault(x => x is RangeAttribute);

				var value = pi.GetValue(this, null);

				#region REQUIRED
				if (requiredAttribute != null)
				{
					if (form.Keys.FirstOrDefault(x => x == pi.Name) != null)
					{
						string error;

						var requiredResult = ValidateRequiredAttribute(requiredAttribute, pi, value, out error);

						results.Add(requiredResult);

						if (!string.IsNullOrEmpty(error))
							errors.AppendLine(error);
					}
				}
				#endregion

				#region REQUIRED LENGTH
				if (requiredLengthAttribute != null)
				{
					string error;

					var requiredLengthResult = ValidateRequiredLengthAttribute(requiredLengthAttribute, pi, value, out error);

					results.Add(requiredLengthResult);

					if (!string.IsNullOrEmpty(error))
						errors.AppendLine(error);
				}
				#endregion

				#region REGULAR EXPRESSION
				if (regularExpressionAttribute != null)
				{
					string error;

					var regularExpressionResult = ValidateRegularExpressionAttribute(regularExpressionAttribute, pi, value, out error);

					results.Add(regularExpressionResult);

					if (!string.IsNullOrEmpty(error))
						errors.AppendLine(error);
				}
				#endregion

				#region RANGE
				if (rangeAttribute != null)
				{
					string error;

					var rangeResult = ValidateRangeAttribute(rangeAttribute, pi, value, out error);

					results.Add(rangeResult);

					if (!string.IsNullOrEmpty(error))
						errors.AppendLine(error);
				}
				#endregion
			}

			if (errors.Length > 0)
				Error = errors.ToString();

			var finalResult = results.Where(x => x == false);

			IsValid = (!finalResult.Any());
		}

		internal static List<PropertyInfo> GetPropertiesNotRequiredToPost(Type t)
		{
			if (t.BaseType != typeof(Model)) return null;

			var props = GetPropertiesWithExclusions(t, true).Where(x => x.GetCustomAttributes(false).FirstOrDefault(y => y is NotRequiredAttribute) == null);

			return props.ToList();
		}

		internal static List<PropertyInfo> GetPropertiesWithExclusions(Type t, bool postedFormBinding)
		{
			if (t.BaseType != typeof(Model)) return null;

			var props = t.GetProperties().Where(x => x.GetCustomAttributes(false).FirstOrDefault(y => y is HiddenAttribute) == null);

			if (postedFormBinding)
				props = props.Where(x => x.GetCustomAttributes(false).FirstOrDefault(y => y is ExcludeFromBindingAttribute) == null);

			return props.ToList();
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
		public Dictionary<string, object> Request { get { return Engine.Request.ToDictionary(x => x.Key, x => x.Value); } }
		public User CurrentUser { get { return Engine.CurrentUser; } }
		public X509Certificate2 ClientCertificate { get { return Engine.ClientCertificate; } }
		public Uri Url { get { return Engine.Url; } }
		public string RequestType { get { return Engine.RequestType; } }
		public string Identity { get { return Engine.Identity; } }

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
			return Engine.FindRoute(path);
		}
		protected RouteInfo FindRoute(string path, string[] urlParameters)
		{
			return Engine.FindRoute(path, urlParameters);
		}
		protected void AddRoute(string alias, string controllerName, string actionName, string defaultParams)
		{
			Engine.AddRoute(Engine.EngineAppState.RouteInfos, alias, controllerName, actionName, defaultParams, true);
		}
		protected void RemoveRoute(string alias)
		{
			Engine.RemoveRoute(alias);
		}
		protected void AddBinding(string actionName, object bindInstance)
		{
			Engine.AddBinding(this.GetType().Name, actionName, bindInstance);
		}
		protected void AddBinding(string[] actionNames, object bindInstance)
		{
			Engine.AddBinding(this.GetType().Name, actionNames, bindInstance);
		}
		protected void AddBinding(string[] actionNames, object[] bindInstances)
		{
			Engine.AddBinding(this.GetType().Name, actionNames, bindInstances);
		}
		protected void AddBindingForAllActions(string controllerName, object bindInstance)
		{
			Engine.AddBindingForAllActions(controllerName, bindInstance);
		}
		protected void AddBindingsForAllActions(string controllerName, object[] bindInstances)
		{
			Engine.AddBindingsForAllActions(controllerName, bindInstances);
		}
		protected void AddBindingForAllActions(object bindInstance)
		{
			Engine.AddBindingForAllActions(this.GetType().Name, bindInstance);
		}
		protected void AddBindingsForAllActions(object[] bindInstances)
		{
			Engine.AddBindingsForAllActions(this.GetType().Name, bindInstances);
		}
		protected void AddBundles(Dictionary<string, string[]> bundles)
		{
			Engine.AddBundles(bundles);
		}
		protected void AddBundle(string name, string[] paths)
		{
			Engine.AddBundle(name, paths);
		}
		public void AddHelperBundle(string name, string data)
		{
			Engine.AddHelperBundle(name, data);
		}
		protected void LogOn(string id, string[] roles, object archeType = null)
		{
			Engine.LogOn(id, roles, archeType);
		}
		protected void LogOff()
		{
			Engine.LogOff();
		}
		protected List<string> GetAllRouteAliases()
		{
			return Engine.GetAllRouteAliases();
		}
		protected void Redirect(string path)
		{
			Engine.ResponseRedirect(path, false);
		}
		protected void Redirect(string alias, params string[] parameters)
		{
			Engine.ResponseRedirect(string.Format("{0}/{1}", alias, string.Join("/", parameters)), false);
		}
		protected void RedirectOnly(string path)
		{
			Engine.ResponseRedirect(path, true);
		}
		protected void ProtectFile(string path, string roles)
		{
			Engine.ProtectFile(path, roles);
		}
		public void AddApplication(string key, object value)
		{
			Engine.AddApplication(key, value);
		}
		public object GetApplication(string key)
		{
			return Engine.GetApplication(key);
		}
		public void AddSession(string key, object value)
		{
			Engine.AddControllerSession(key, value);
		}
		public object GetSession(string key)
		{
			return Engine.GetControllerSession(key);
		}
		public void AddCache(string key, object value, DateTime expiresOn)
		{
			Engine.AddCache(key, value, expiresOn);
		}
		public object GetCache(string key)
		{
			return Engine.GetCache(key);
		}
		public void RemoveCache(string key)
		{
			Engine.RemoveCache(key);
		}
		public void AbandonSession()
		{
			Engine.AbandonControllerSession();
		}
		protected string GetQueryString(string key, bool validate)
		{
			return Engine.GetQueryString(key, validate);
		}
		protected string MapPath(string path)
		{
			return Engine.MapPath(path);
		}
		protected void AddCookie(HttpCookie cookie)
		{
			Engine.AddCookie(cookie);
		}
		protected HttpCookie GetCookie(string name)
		{
			return Engine.GetCookie(name);
		}
		protected void RemoveCookie(string name)
		{
			Engine.RemoveCookie(name);
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
			return Engine.GetBindings(controllerName, actionName, alias, initializeTypes);
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

	public abstract class Controller : BaseController
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
			if (expiresOn != null)
			{
				var cr = Engine.GetCache(fragmentName);

				if (cr != null) return cr as string;
			}

			if (!string.IsNullOrEmpty(forRoles) && CurrentUser != null && !CurrentUser.IsInRole(forRoles))
				return string.Empty;

			if (canViewFragment != null && !canViewFragment())
				return string.Empty;

			if (fragTags == null)
				fragTags = GetTagsDictionary(FragTags.ContainsKey(fragmentName) ? FragTags[fragmentName] : null, FragBag, fragmentName);

			var result = Engine.EngineAppState.ViewEngine.LoadView(string.Format("{0}/{1}/Fragments/{2}", PartitionName, this.GetType().Name, fragmentName), fragTags);

			if (expiresOn != null)
				Engine.AddCache(fragmentName, result, expiresOn.Value);

			return result;
		}

		public string RenderPartial(string partialName)
		{
			return RenderPartial(partialName, null);
		}

		public string RenderPartial(string partialName, Dictionary<string, string> tags)
		{
			return Engine.EngineAppState.ViewEngine.LoadView(string.Format("{0}/{1}/Shared/{3}", PartitionName, this.GetType().Name, partialName), tags ?? GetTagsDictionary(ViewTags, ViewBag, null));
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
			var result = new ViewResult(Engine.EngineAppState.ViewEngine, GetTagsDictionary(ViewTags, ViewBag, null), PartitionName, controllerName, actionName);
			InitializeViewTags();
			return result;
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
			var result = new ViewResult(Engine.EngineAppState.ViewEngine, GetTagsDictionary(ViewTags, ViewBag, null), PartitionName, controllerName, actionName, "Shared/");
			InitializeViewTags();
			return result;
		}
		#endregion
	}
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

		// from: http://blogs.msdn.com/b/csharpfaq/archive/2006/10/09/how-do-i-calculate-a-md5-hash-from-a-string_3f00_.aspx
		public static string CalculateMd5Sum(this string input)
		{
			var md5 = System.Security.Cryptography.MD5.Create();
			var inputBytes = System.Text.Encoding.UTF8.GetBytes(input);
			var hash = md5.ComputeHash(inputBytes);

			var sb = new StringBuilder();

			foreach (var t in hash)
				sb.Append(t.ToString("X2"));

			return sb.ToString();
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