//
// Aurora - An MVC web framework for .NET
//
// Updated On: 15 June 2012
//
// Contact Info:
//
//  Frank Hale - <frankhale@gmail.com> 
//               <http://about.me/frank.hale>
//
// LICENSE - GPL version 3 <http://www.gnu.org/licenses/gpl-3.0.html>
//
// NOTE: Aurora contains some code that is not licensed under the GPLv3. 
//       that code has been labeled with it's respective license below. 
//
// NON-GPL code = my JSMin C# port which retains original JSMin license
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
// - Actions can be invoked on a special basis, they are designated with the
//   [FromRedirectOnly] attribute.
// - Built in OpenID authentication which is as easy as calling two methods. One 
//   to initiate the login with the provider and then one to finalize 
//   authentication.
// - Built in Active Directory querying so you can authenticate your user 
//   against an Active Directory user. Typically for use in client certificate
//   authentication.
// - Bundling/Minifying of Javascript and CSS.
//
// Aurora.Extra 
//
// - My fork of Massive ORM
// - My fork of the Gravatar URL generator
// - HTML helpers
// - Plugin support (can be used by apps but is not integrated at all into the
//   framework pipeline.)
//
// ---------------
// -- RATIONALE --
// --------------- 
//
// -- What is this?
//
// Aurora is a single file light MVC web framework built on top of ASP.NET. 
//
// -- Why develop yet another web framework on top of ASP.NET?
//
// The short answer is easy and that is to learn! However, there is more to it 
// than that and that is that I wanted a light modern framework that didn't hide 
// behind mountains of complexity and that was simple enough to adapt and change
// over time or with a particular project. I wanted this to be a single file
// web framework that could be dropped into a project easily. I also wanted to 
// create a sandbox so that I could play with concepts, framework design and 
// have fun doing it. I am making the code available to assist others that are
// wondering how to create their own framework or just to provide some examples
// of how to get started.
//
// NOTE: As with all things there is more than one way to skin a cat and my view 
// of how a framework should work might be quite a bit different from yours. If 
// you find something amazingly stupid in here please let me know so I can fix 
// it. Patches are welcome!
//
// -- Why is all the code in a single file?
//
// The great thing about this framework in my opinion is that it's self 
// contained in just a single file. You can drop it straight into a project
// and modify it to your needs. Everything is in here, there is no hidden 
// magic and if you want to know what's going on just look below. No need to 
// wade through file after file to figure out how the thing works. A big turn
// off when looking at somebody elses code (in my opinion) is shuffling through 
// the files to figure how all of it is put together. 
//
// What I hope to do here is provide a single file with the appropriate 
// commentary and rationale for those that are interested in building their own 
// web framework on top of ASP.NET. As this framework matures I will be putting 
// descriptive commentary throughout to describe the classes, how they work and 
// why they are there.
//
// -- Conventions used in this file:
//
// Regions are heavily (ab)used and are the logical separator between sections
// of code. Also comments that are intended to be documentation are constrained
// to 80 columns. Transitional comments (short lived that is) are not. 
//

#region LICENSE - GPL version 3 <http://www.gnu.org/licenses/gpl-3.0.html>
//
// NOTE: Aurora contains some code that is not licensed under the GPLv3. 
//       that code has been labeled with it's respective license below. 
//
// NON-GPL code = my JSMin C# port which retains original JSMin license
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

#region DOCUMENTATION
// ----------------
// --- Building ---
// ----------------
//
// Aurora requires at least .NET 4.0 to compile.
//
// You can use the included CSPROJ file that is part of the github repository to 
// compile the assembly using MSBUILD. All external dependencies are included
// in the repository.
//
// References:
//
//	System
//	System.Configuration
//	System.Core
//	System.Data
//	System.Data.DataSetExtensions
//	System.Web
//	System.Web.Abstractions
//	System.Xml.Linq
//	System.Xml.dll
//  Microsoft.CSharp
//  Microsoft.Web.Infrastructure
//	Newtonsoft.Json.NET - http://json.codeplex.com/
//	HtmlAgilityPack - http://htmlagilitypack.codeplex.com/
//  MarkdownSharp - http://code.google.com/p/markdownsharp/
//
// There are some compile flags that you can annotate if you'd like support for
// Active Directory or OpenID. 
// 
// The flags are:
//
//  COMPILE FLAG        | Reference
//  -------------------------------------------------------------------------
//  ACTIVE_DIRECTORY    | System.DirectoryServices
//  CAC_AUTHENTICATION  | System.DirectoryServices
//  OPENID              | DotNetOpenAuth.dll - http://www.dotnetopenauth.net/
//
// Other compile flags:
//
// LIBRARY - if you want to build this as a library use this flag so that 
//           assembly information is included in the build.
//
// -------------
// --- Usage ---
// -------------
//
// Compiling the code is simple but how do you use it?
//
// The best way to get started with this framework is by using the Visual Studio
// 2010 project template. The template contains the framework code and the 
// requisite project layout. You can find the template here:
//
// https://github.com/frankhale/aurora/blob/master/AuroraMVCWebApplication40.zip
//
// This is a basic Hello, World application and shows the project structure.
//
// The project structure is quite simple and looks like this:
//
//  App/
//    Controllers/    <- This is a convention (optional)
//    Models/         <- This is a convention (optional)
//    Resources/      <- Javascript, CSS, static files.
//    [PartitionName] <- Optional, controllers which declare partitions
//      Controllers/  <- This is a convention (optional)
//      ControllerName/
//        Fragments/
//        Shared/
//    Views/
//      Fragments/ (similar to partials but rendered by themselves)
//      Shared/ (for master pages and partial views)
//      ControllerName/ (If not using partitions)
//        Fragments/
//        Shared/
//
// The folder structure is quite simple. There are a couple of convention based
// directories for controllers and models. These directories are optional but 
// are encouraged for readability. The Views directory should contain Fragments 
// and Shared folders. The Fragments folder is used for small HTML fragments 
// that can be used by any actions. The Shared folder is for master pages and
// partial views. In addition to those two folders a folder for each controller 
// will also need to be created to store the controllers action views.
//
//	Master Pages - A page that will be applied as a base to your views. Master
//                 pages an contain partial views, fragments and views.
//  Views     - Either a complete HTML page or a page that depends on a master
//              page. Views can contain partial views and fragments.
//  Fragments - A succinct piece of HTML that can be used by multiple actions,
//              fragments can use tags for data substituion just like views and
//              master pages. Fragments cannot include partial views or master
//              pages.
//              
// The project structure can be organized in another way by using Partitions. 
// This allows you to separate controllers into logical partitions to keep their
// code and views grouped. I'll talk more about this in the Partitions section.
//
// -------------------
// --- Controllers ---
// -------------------
//
// Controllers are nothing more than specialized classes that subclass the 
// abstract Controller class. Controllers can have actions and they are nothing 
// more than public methods annotated with special attributes.
//
// Actions can be annotated with the folling attribute verbs.
//
// [HttpGet], [HttpPost], [HttpPut], [HttpDelete] and [FromRedirectOnly]
// 
// These verbs are used to tell the framework how to handle the action.
//
// A simple controller with an index action would look something like this:
//
//  public class Home : Controller
//  {
//    [HttpGet] 
//    public ViewResult Index()
//    {
//      return View();
//    }
//  }
//
// Notice that we proceed the Index action with [HttpGet]. The framework will
// not assume you want the method to react to a HTTP Get unless you annotate it.
//
// A more indepth description of HTTP attribute verbs and actions will be done 
// in the Actions section.
//
// In addition to having actions a controller has a few special plumbing 
// features that you can use to perform initilization code or code before or 
// after an action. 
//
// You can specify code to execute on controller initilization. This is a step
// that occurs right after the controller has been instantiated.
//
// The controller initialization method: 
//
//   OnInit
// 
// You can override this method to perform anything you like at the init phase
// of the controller.
//
//  protected override void OnInit()
//  {
//		// We want to perform some code before our actions are invoked
//    PreActionEvent += 
//			new EventHandler<RouteHandlerEventArgs>(Home_ActionHandler);
//  }  
//
// Actions have two events that occur right before and right after the action
// invocation. You can add handlers for either/both of these events as you 
// desire:
//
//	PreActionEvent
//  PostActionEvent
//
// The really cool thing about these events is that they are aware of the 
// parameters that are being passed to the action. You can do any specialized
// logic in here that you need to. Keep in mind that these events are for every
// action. If you need special logic to be performed for one or a set of actions 
// you can use an ActionFilter which will be described in the ActionFilter's 
// section.
//
// Sometimes it's not enough to have your code inside a controller init method
// or pre and post action event handlers or even action filters. Sometimes you 
// need to go one step higher so that your code can be relevant for all your 
// controllers. This is where the front controller comes in. The front 
// controller can do a lot more and has access to a set of events that the 
// controller doesn't have access to.
//
// Front Controllers have access to the folling events:
//
// NOTE: This list is going to grow in the future. Events that deal with routing
//       with exception of the PreRouteDeterminationEvent and StaticRouteEvent 
//       have access to the route specific information such as parameters to the
//       action.
//
//  PreActionEvent
//  PostActionEvent
//  StaticRouteEvent
//  CachedViewResultEvent
//  PreRouteDeterminationEvent
//  PostRouteDeterminationEvent
//  PassedSecurityEvent
//  FailedSecurityEvent
//
// The front controller also has an OnInit method.
//
// Here is a sample front controller definition:
//
//  public class MyFrontController : FrontController
//  {
//		protected override void FrontController_OnInit()
//    {
//			ActionBinder actionBinder = new ActionBinder(Context);
//			actionBinder.AddForAllControllers(new Foo());
//    }
//  }
//
// ---------------
// --- Actions ---
// --------------- 
//
// Actions are public methods in your controller that are annotated with one of 
// the following attributes:
//
// [HttpGet], [HttpPost], [HttpPut], [HttpDelete] and [FromRedirectOnly]
//
// Actions are mapped dynamically to URLs based on the parameters passed in. 
// Parameters are grouped into a number of categories (in order of their 
// precedence):
// 
//  filter results 
//  bound parameters
//  front parameters
//  url parameters
//  form parameters / post model / (HTTP Put/Delete) payload 
//  files
//
// Filter Results - If an action has been annotated with a filter then any 
// results from the invocation of the filter will be applied to the parameter 
// list of the action. Filters are invoked right after the action pre event and
// right before the action is invoked. Keep in mind filters are not required to 
// return a result.
//
// Bound Parameters - An action can have bound parameters that are applied to 
// the action when it is invoked. These bound parameters can be any valid object
// and give you a mechanism to have objects automatically passed to your actions
// without needing to worry about fetching them on your own. For instance, you 
// could use bound parameters to have a database access object passed to your
// actions.
// 
// Front Parameters - Routes can be added dynamically through code and you can
// specify default parameters that will be applied to them. These parameters are
// called front parameters.
//
// URL Parameters - Anything in the URL that isn't the action alias and is not a 
// query string is a URL parameter.
//
// Form Parameters - These are either your form elements or a post model.
//
// HTTP Put/Delete payload - The payload of a PUT or DELETE request.
//
// Files - Files that are posted to the action will be listed here. These are of
// the type HttpPostedFileBase.
//
// So with that said action parameters map like this:
//
//  public ViewResult ActionName(filter_results, 
//                               bound_parameters, 
//                               front_params, 
//                               url_parameters, 
//                               form (post model) / (HTTP Put/Delete) payload, 
//                               files)
//  {
//    ...
//  }
// 
// An index action that mapped to the following URL would look like this:
//
// URL: http://mysite.com/ControllerName/Index 
//
//  [HttpGet] 
//  public ViewResult Index()
//  {
//    ViewTags["msg"] = "Hello,World!";
//
//    return View();
//  }
//
// NOTE: The default route can be configured in the Aurora web.config section.
//       If you wanted to your Index action to be your default route then you'd
//       specify it like this in the web.config Aurora section.
//
//         <Aurora DefaultRoute="/Index" ... />
//
// An action that mapped to the following URL would look like this:
//
// URL: http://mysite.com/Users/Frank
//
//  [HttpGet("/Users")] 
//  public ViewResult Users(string name)
//  {
//    return View();
//  }
//
//
// NOTE: Actions have access to a special dictionary called ViewTags which is 
// used to describe a key=value pair where the key is the tag name in your view
// and the value is the data that will be substituted when the view is compiled.
// More on this in the Views section.
//
// NOTE: Bound parameters, Front parameters and Filter parameters will be 
// discussed in more detail their respective sections.
//
// --------------
// --- ROUTES ---
// --------------
//
// Aurora tries to automatically map routes to actions by looking at the URL
// and splitting it up in the following format:
//
//  http://yoursite.com/alias/param1/param2/param3
//
// Keep in mind that the query string is not handled in any special way. Actions
// can grab query string values by looking at the Context.QueryString 
// dictionary.
//
// The alias is what gets mapped back to an action and it can be configured in 
// a number of ways. You can declare an alias by specifying it in one of the 
// HTTP request attributes such as:
//
//  [HttpGet("/Index")]
//  public ViewResult Index() 
//  {
//    ...
//  }
//
// This would map http://yoursite.com/Index to your Index() action.
//
// You can also declare an alias in the following manner:
//
//  [Alias("/Index")]
//  [HttpGet]
//  public ViewResult Index() 
//  {
//    ...
//  }
//
// This would map http://yoursite.com/ControllerName/Index and
// http://yoursite.com/Index to your Index() action.
//
// You can also stack the Alias attribute if you want more than one alias to 
// point to the same action.
//
// Actions that do not specify a specific alias will have the following default
// alias. 
//
//  /controller_name/action_name
//
// Routes can be added dynamically through code. This is very useful if you 
// wanted to create a dynamic alias for usernames for example so that it would
// make it easier to navigate to a user details page. 
//
// Both normal controllers and the front controller have the following methods
// for adding and removing routes at runtime:
//
//  AddRoute
//  RemoveRoute
//
// -------------------------
// --- ACTION ATTRIBUTES ---
// -------------------------
//
// The various attributes which can be annotated on actions can have many 
// options that denote various characteristics of the particular attribute. For 
// instance actions can have aliases. Aliases are great if you don't want to 
// refer to your action with the following signature /ControllerName/ActionName. 
//
// Here is a list of options you can use with the various types of action 
// attributes:
//
//  Alias - used to specify an alias which will refer to an action. This 
//          attribute can be stacked if you want to specify more than one
//          alias per action.
//
//  HttpPost, HttpPut, HttpDelete and HttpGet:
//
//    ActionSecurity SecurityType - Secure
//    string RouteAlias
//    string Roles - separated by |
//    bool HttpsOnly
//    string RedirectWithoutAuthorizationTo
//    bool RequireAntiForgeryToken
//
//  HttpGet (specific):
//
//    HttpCacheability CacheabilityOption - default public
//    bool Cache
//    int Duration - default 15 minutes
//    string Refresh - in minutes
//
// A action with an alias that is secure might look like this:
// 
//  [HttpGet("/Admin", 
//           ActionSecurity.Secure, 
//           RedirectWithoutAuthorizationTo = "/Index", 
//           Roles = "Admin")] 
//  public ViewResult Admin()
//  {
//    return View();
//  }
//
// NOTE: Methods with no http request attribute will not be invoked from a URL 
//       regardless if they are public or not.
//
// There is an attribute that is used for special cases where you want to 
// redirect to an action but you don't want that action publically available. 
// The attribute to annotate this situation is called [FromRedirectOnly] and
// it's invoked by calling RedirectOnlyToAlias or RedirectOnlyToAction.
//
// --------------------
// --- VIEW RESULTS ---
// --------------------
//
// Views can have various types of results. The most common result is one that
// renders an HTML page. Actions can also return physical file results,
// virtual file results, JSON results and Fragment results.
//
// The following methods in the controller class are used to return your view 
// result at the end of your action.
//
//  public ViewResult View()
//
//    - Returns an HTML result
//
//  public ViewResult View(bool clearViewTags)
//
//    - Returns an HTML and allows you to specify that you want the view tags
//      cleared. The default behavior is to keep the view tags as they are 
//      between requests.
// 
//  public ViewResult View(string name)
//
//    - Specify a view to return for this action's result.
//
//  public ViewResult View(string name, bool clearViewTags)
//
//    - Specify a view to return and optionally clear the view tags.
//
//  public ViewResult View(string controllerName, string actionName, 
//                                                           bool clearViewTags)
//
//    - Specify a view based on it's controller and action name as well as 
//      optionally clear the view tags.
//
//  public JsonResult View(object jsonData)
//
//    - Returns a JSON result. The object is passed in as is and is converted
//      to JSON before it's sent to the client.
//
//  public VirtualFileResult View(string fileName, byte[] bytes, 
//                                                           string contentType)
//
//    - Returns a file result for a file that is not physically stored on a disk
//      this file could reside in a DB or have been created during the execution
//      of the action.
//
//  public PartialResult Fragment(string partialName)
//
//    - Returns a fragment or partial as a result. This could be used in an AJAX
//      request for instance.
//
//  public PartialResult Fragment(string partialName, bool clearViewTags)
//
//    - Returns a fragment or partial and optionally clears the view tags.
//
// ------------------------
// --- Bound Parameters ---
// ------------------------ 
//
// Actions can have parameters bound to them when they are invoked. This type of 
// parameter is special and implements the IBindToAction interface. This is 
// a very simple interface and mandates only one method be implemented. This 
// method is named ExecuteBeforeAction and as it's name suggests executes before
// the action to perform any initialization steps necessary to new up the 
// object. Once this step is complete the object is passed off to the action.
//
// A class implementing the IBindToAction could look like this:
//
//  public class DataConnector : IBindToAction
//  {
//    public WikiDataClassesDataContext DB { get; internal set; }
//
//    public void ExecuteBeforeAction()
//    {
//      DB = new WikiDataClassesDataContext();
//    }
//  }
//
// This adds a simple wrapper around a LINQ to SQL data context. This is just
// a trivial example and perhaps a better usage would be for your data access
// layer to directly implement the IBindToAction.
//
// Another usage could be to pass an instance of the currently logged in user to 
// a set of actions.
//
// Bound parameters are added to an action by the ActionBinder class. A typical
// usage of this class would be like this:
//
//  public class Home : Controller
//  {
//		protected override void OnInit()
//    {
//			Binder.Bind("Home", "Index", new Foo());
//    }
//
//    [HttpGet] 
//    public ViewResult Index(Foo f)
//    {
//      return View();
//    }
//  }
//
// At the time OnInit is executed the action binder is instructed to add a 
// bound parameter for the Index action in the Home controller. Each time 
// the action is invoked the bound parameters ExecuteBeforeAction() method will
// be triggered initializing the object and then it'll be passed to the action.
//
// In addition to the Bind methods there are a few others that make it 
// convenient to add bound parameters to all actions in a controller or even to 
// all actions in all controllers.
//
// -----------------------
// --- ACTION FILTERS ----
// -----------------------
//
// Actions can have filters that perform some arbitrary logic before the action 
// is invoked. The filter is denoted by an attribute, this attribute is created
// by subclassing ActionFilter. Action filters can optionally have results which
// are bound at the beginning of the action parameter list.
//
// Action filter results are any class that implements the IActionFilterResult
// interface. This interface defines no methods or properties so you are open
// to define your implementation however you like. This interface is necessary
// so that the framework knows how to handle your result.
//
// You can define a filter result like this: 
//
// NOTE: filter results are optional
//
//  public class FooFilterResult : IActionFilterResult
//  {
//    public string Foo { get; set; }
//
//    public FooFilterResult(string foo)
//    {
//      Foo = foo;
//    }
//  }
//
// Here is a simple action filter. 
//
//  public class Foo : ActionFilter
//  {
//    public override void OnFilter(RouteInfo routeInfo)
//    {
//      FilterResult = new FooFilterResult("Hello,World!");
//    }
//  }
//
// The action that uses this filter would be defined similar to this:
//
//  [Foo]
//  [HttpGet]
//  public ViewResult Index(FooFilterResult foo)
//  {
//    ...
//  }
//
// Your action filter is derived from the ActionFilter attribute class and 
// should override the OnFilter() method like in the above example.
//
// -----------------------------------
// --- Action Parameter Transforms ---
// -----------------------------------
// 
// To make the URL to action invocation a little bit more sane you can use a 
// feature that will transform an incoming parameter into another type. This
// is done through action parameter transforms. Action Parameter Transforms use
// an attribute annotation on your parameter in your action parameter list and a 
// class to transform the parameter to the desired one.
//
// Let's assume you have an action that looks like this that takes a string
// which represents a user id and has a bound parameter of our data access 
// layer:
//
//  [HttpGet]
//  public ViewResult Index(IDataAccess db, string uid)
//  {
//    ...
//  }
// 
// This can be transformed into a user object like this:
//
//  [HttpGet]
//  public ViewResult Index(IDataAccess db,
//													[ActionParamTransform("UserTransform")] User user)
//  {
//    ...
//  }
//
// Notice that we've changed the parameter to declare the type we want and we
// annotated the ActionParamTransform attribute to it with the class name that
// will transform the incoming string into the type we want.
// 
// The transform class could look like this: 
//
//  public class UserTransform : IActionParamTransform<User, string>
//  {
//    private IDataAccess db;
//
//    public UserTransform(IDataAccess db)
//    {
//      this.db = db;
//    }
//
//    public User Transform(string uid)
//    {
//      // Perform steps to lookup user based on the id along with any error 
//      // handling.
//    }
//  }
//
// When the action is invoked now the incoming string is transformed into the 
// type we want. 
//
// --------------------------
// --- Custom Error Class ---
// --------------------------
//
// An application can define a special class that will be used to filter errors 
// through. 
//
// NOTE: An application can only have one of these classes.
// NOTE: Turn on custom errors in your web.config
//
//	<customErrors mode="On"></customErrors>
//
// A example of a custom error class would look similar to the following:
// 
//  public class MyCustomError : CustomError
//  {
//    public override ViewResult OnError(string message, Exception e)
//    {
//      ViewTags["message"] = message;
//
//      return View();
//    }
//  }
//
// The view for the error action should be placed in the /Views/Shared folder at
// the root of your application.
//
// -------------
// --- Views ---
// -------------
//
// Views are the primary mechanism to take dynamic data and combine them with 
// HTML. Views in Aurora are simple text files with HTML and a few directives
// that tell the view engine what to do with the files. The Aurora view engine
// does not support code inside the view. Instead, code needed to produce layout
// is performed inside an action and combined with HTML from a fragment. You
// can also use the HTML helpers or create your own. 
// 
// Given a simple application scenario that doesn't use partitioning (more on 
// that later) your views would live inside a folder at the root of your 
// application called Views.
//
// Views are just HTML templates with specially formatted tags and directives to 
// give the view engine direction on what to do with the view at time they are 
// compiled.
//
// Tags (otherwise known as ViewTags) are formatted like:
//
//  {{myTag}} - Not HTML Encoded
//  {|myTag|} - HTML Encoded
//  {!myTag!} - Markdown
//
// A simple view with a tag for the page title and a tag for page content would 
// look like this:
//
//  <html>
//   <head>
//    <title>{|title|}</title>
//   </head>
//   <body>
//    {|content|}
//   </body>
//  </html>
//
// An action that would map content for the tags would look like this:
//
//  [HttpGet]
//  public ViewResult Index()
//  {
//    ViewTags["title"] = "My Website";
//    ViewTags["content"] = "Hello, World!";
//
//    return View();
//  }
//
// More often than not you'd want to create a master page that can be used by
// multiple actions. Master pages are simlar to regular views but they declare
// special directives that tell the view engine how to handle them and they live
// inside the /Views/Shared folder.
//
//  <html>
//   <head>
//    <title>My Site</title>
//    %%Head%%
//   </head>
//   <body>
//    %%View%%
//   </body>
//  </html>
//
// The %%HEAD%% directive is used as a place holder to place content into the 
// HEAD section of your page when the view that uses this master page is 
// compiled.
//
// The %%VIEW%% directive is the place holder where the view that uses this
// master page will be inserted.
//
// A view that uses this master page might look like this:
//
//  %%Master=MyMasterPage%%
//
//  [[ 
//     <script src="/Resources/jquery.js" type="text/javascript"></script>
//  ]]
//
//  ... additional html formatting ...
//
//  {{content}}
//
//  ... additional html formatting ...
//
// Notice that when we use the Master directive we specify the name of the 
// master page but we do not tell it what extension it is. All views are 
// expected to have an extension of .html
//
// Partials are incomplete pages that can contain tags just like regular views.
// Partials are used to combine with other views at compile time. They differ
// from Fragments in that they cannot be rendered inside a view tag as part of
// an action result.
//
// To include partial views you use the following directive anywhere in your 
// view (where MyView is the name of the partial you wish to include in the 
// final rendered page). Partial views live in the /Views/Shared folder:
//
//  %%Partial=MyView%%
//
// Not everything can be classified as a partial or a master page so we have
// a concept called Fragments. Fragments are snippets of HTML that you want
// to combine with dynamic data and render anywhere in your page from a view 
// tag.
// 
// To render a fragment you can do something like this:
//
//  [HttpGet]
//  public ViewResult Index()
//  {
//    ViewTags["title"] = "My Website";
//    ViewTags["content"] = RenderFragment("Foo");
//
//    return View();
//  }
//
// This would render the fragment called Foo to the ViewTags dictionary for
// key "content". 
//
// NOTE: Fragments can contain tags just like views.
//
// Actions have access to a special dictionary called ViewTags and FragTags. 
// These dictionaries are used to perform substitution on views and fragments
// with dynamic data based on the tags you place in your views and fragments. 
//
// There are two additional "dictionaries" available for use that are based on
// the new C# dynamic feature. Instead of using indexing to specify keys you 
// simply use the key as if it were a property on the dictionary. These 
// dictionaries are named DViewTags and DFragTags.
//
// Bundle directive:
// 
// Bundle directives are used to annotate a CSS or Javascript bundle that will
// be replaced at compile time with a bundle. The bundle will either be 
// compressed if your app is is in release mode or uncompressed if it isn't. 
// More on bundling later.
//
// The bundle directive looks like:
//
//  %%Bundle=All.js%%
//
// Moving on The following example shows how to post a form to an action. All 
// forms require the tag %%AntiForgeryToken%% to be designated unless the 
// [HttpPost] attribute is designated with the RequireAntiForgeryToken set to 
// false.
//
//  <form action="/Home/Index" method="post">
//    %%AntiForgeryToken%%
//    <input type="text" name="tbMessage1" /><br />
//    <input type="text" name="tbMessage2" /><br />
//    <input type="submit" value="Submit" />
//  </form>
//
// The way this form gets translated on the back end is either of two forms. 
// You can specify a posted form model that is a simple class with properties
// that have the same name as the input elements in your form or you can specify
// that the action they map to takes each of the elements as a parameter. The 
// order of the elements in the form is the order that they should appear in the 
// action parameter list.
// 
// If you take the posted form model approach the class would look like this, 
// note that it must subclass Model and it must expose properties for it's data 
// types.
//
//  public class Data : Model
//  {
//    public string tbMessage1 { get; set; }
//    public DateTime tbMessage2 { get; set; }
//  }
//
// And the action would look like this:
//
//  [HttpPost]
//  public ViewResult Index(Data d)
//  {
//    ...
//  }
//
// To specify an action that takes the posted data and applies it directly to
// the action as parameters it would look like this:
//
//  [HttpPost]
//  public ViewResult Index(string tbMessage1, DateTime tbMessage2)
//  {
//    ...
//  }
//
// --------------------
// --- PARTITIONING ---
// --------------------
//
// Partitioning is a project management feature that allows you to group similar
// controllers together so that their views are logically separate from the 
// standard location for views. 
//
// Let's say we have a controller that declares a partition called Main:
//
//  [Partition("Main")]
//  public class Home : Controller
//  {
//  } 
// 
// The folder structure of your application would need to conform to this:
//
//  /
//  /Main
//		/Controllers (this is a convention and is optional)
//			Home.cs
//		/Home
//			/Views
//        /Fragments
//			  /Shared
//  /Views
//		/Fragments
//		/Shared
//
// So really what the partition does is rearrange how your views are looked up
// by the view engine. This feature does not dictate where your controllers are
// located.
//
// ----------------------
// --- Bundle Manager ---
// ----------------------
//
// The bundle manager is responsible for taking Javascript or CSS and combining
// and minifying it into a single bundle for transport to the client. You can 
// create as many bundles as you need for whatever scenarios you have. 
//
// You can create your bundles in the Global.asax Application_Start method.
//
// Example:
//
//  HttpContextBase ctx = new HttpContextWrapper(HttpContext.Current);
//
//  BundleManager bm = new BundleManager(ctx);
//
//  string[] cssPaths = 
//  {
//    "/Resources/Styles/reset.css",
//    "/Resources/Styles/style.css",
//    "/Resources/Scripts/SimplejQueryDropdowns/css/style.css",
//    "/Resources/Scripts/tablesorter/style.css"
//  };
// 
//  bm.AddFiles(cssPaths, "all.css");
//
// Now you can add a link to your CSS in your view like this:
// 
//  <link href="/Resources/Styles/all.css" rel="stylesheet" type="text/css" />
//
// Or you can use the bundle directive in your view. This will instruct the 
// view engine to include your bundle if your app is in release mode or the 
// actual files if in debug mode. 
//
// --------------
// --- Models ---
// --------------
//
// Models are simple plain objects that expose public properties that correspond
// to the data they expose. If the model is a posted form model then it's 
// property names are the same name as the form element names. 
//
// If your model is a view model and is used by let's say the HTMLTable helper 
// (discussed below) then you can add an optional [DescriptiveName("Foo")] 
// attribute to your property names so that it will be used in the table header
// instead of the actual property name. 
//
// If you simply want to insert spaces in a camel cased name then you can use:
//
//  [DescriptiveName(DescriptiveNameOperation.SplitCamelCase)]
//
// Models also have a method called ToJSON() to return a JSON string of the data 
// that it contains.
//
// Model validation can be done using the following attributes on your 
// properties:
//
//  [Required("Error Message")]
//  [RequiredLength(10, "Error Message")]
//  [RegularExpression("Pattern", "Error Message")]
//  [Range(min, max, "Error Message")]
//
// --------------------
// --- HTML Helpers ---
// --------------------
//
// A small group of HTML helper classes have been created to make things easier.
// 
// The classes are:
//
//  HtmlTable - Takes a class derived from Model and creates a table from it's
//              properties. You can optionally ignore columns or bind your
//              own columns to it (eg. to create columns for delete/edit, etc)
//
//  HtmlAnchor - Creates an achor tag
//
//  HtmlInput - Creates any type of input tag or textarea
//
//  HtmlSpan - Creates a span tag
//
//  HtmlForm - Creates a form tag
//
//  HtmlSelect - Creates a select/option set
//
// Examples:
//
// The following snippet describes how to use the HTMLTable to generate a table
// from a data model.
//
// Given the following data model:
//
//  public class Employee : Model
//  {
//    public string Name { get; set; }
//    public string ID { get; set; }
//  }
//
// And the following action which adds an HTML table representation of our model
// to a view:
//
//  [HttpGet("/Employees")]
//  public ViewResult EmployeeList()
//  {
//    List<Employee> employees = new List<Employee>() 
//    {
//      new Employee() { ID="U0001", Name="Frank Hale" },
//      new Employee() { ID="U0002", Name="John Doe" },
//      new Employee() { ID="U0003", Name="Steve Smith" }
//    };
//
//    List<ColumnTransform<Employee>> employeeTransforms = 
//                                        new List<ColumnTransform<Employee>>();
//
//    employeeTransforms.Add(new ColumnTransform<Employee>(employees, "View", 
//                 x => string.Format("<a href=\"/View/{0}\">View</a>", x.ID)));
//    employeeTransforms.Add(new ColumnTransform<Employee>(employees, "Delete", 
//             x => string.Format("<a href=\"/Delete/{0}\">Delete</a>", x.ID)));
//
//    HtmlTable<Employee> empTable = 
//        new HtmlTable<Employee>(employees, null, employeeTransforms, 
//                                                              border => "1");
//
//    ViewTags["table"] = empTable.ToString();
//
//    return View();
//  }
//
// ------------------------
// --- Security Manager ---
// ------------------------
//
// The securing mechanism for actions is accomplished by telling the framework 
// who your logged in user is. This can be done like so:
//
//  SecurityManager.Logon(Context, ld.Username);
//
// You can also filter secure actions based on role like this:
//
//  SecurityManager.Logon(Context, ld.Username, "Admin");
//
// If the action should be filtered for multiple roles separate you can use the
// overloaded Logon function and specify a list of roles. The underlying code 
// uses the IPrincipal interface and sets the HttpContext.User with a instance
// of a class that implemented IPrincipal. 
//
//  SecurityManager.Logon(Context, ld.Username, 
//                            new List<string>() { "Role1", "Role2", "Role3" });
//
// The way you determine your logged on user is up to you. The framework just 
// needs to know if you have a user logged on to your site. It then populates
// the underlying User object that is contained in the HttpContext.User. If you
// don't want to tell the SecurityManager what roles a user has that is fine. 
// You can call the Logon method that doesn't specify the roles, however, You 
// won't be able to use the lock down actions based on roles.
//
// The SecurityManager has an overload for Logon that allows you to specify a 
// callback that will run during the security check on an action that is locked
// down to certain roles. 
//
// The signature of your callback will look like this:
// 
//  public bool CheckRoles(User u, string roles) 
//  {
//    The roles in this context are the roles that were initially passed in
//    when you logged the user in and told the SecurityManager about it. You 
//    can use this to test against your user just in case roles were changed 
//    while the user was logged in. 
//  }
//
// To tell the framework that the user has logged off do:
//
//  SecurityManager.Logoff(Context)
//
// There is also built in support for OpenID based logon. There are only
// two API's and they are:
//
//  SecurityManager.LogonViaOpenAuth(HttpContextBase ctx, string identifier, 
//                                                         Action<string> error)
//
// LogonViaOpenAuth initiates the request to the OpenID provider identified by 
// the parameter identifier. An exception may be thrown trying to initiate the 
// logon with the provider if there is an error or an invalid provider given. 
// The error action is a function that takes a string which is the error.
// 
//  SecurityManager.FinalizeLogonViaOpenAuth(HttpContextBase ctx, 
//                              Action<OpenAuthClaimsResponse> authenticated, 
//                              Action<string> cancelled, Action<string> failed)
//
// This method takes three delegates which are self explanitory. The 
// authenticated delegate passes one parameter of type OpenAuthClaimsResponse 
// and it contains details about information on the login that is authenticated 
// via the OpenID provider. 
//
// The other two delegates pass a string which is the error that was encountered 
// during logon finalization.
//
// ---------------------------
// --- Static File Manager ---
// ---------------------------
//
// If you desire the ability to protect certain static files from being 
// downloaded you can use the StaticFileManager class to protect files based on
// roles. The file can only be downloaded if the roles match the currently 
// logged in users roles that were set when the user was logged in via the 
// SecurityManager.
//
// Usage:
// 
//   StaticFileManager.ProtectFile(context, "/path/foo.bar", "role")
// 
// ------------------
// --- Web.Config ---
// ------------------
//
// Below is a sample web.config:
//
//  <?xml version="1.0"?>
//  <configuration>
//    <configSections>
//      <section name="Aurora" type="Aurora.WebConfig" 
//        requirePermission="false"/>
//      <section name="dotNetOpenAuth" 
//        type="DotNetOpenAuth.Configuration.DotNetOpenAuthSection" 
//        requirePermission="false" allowLocation="true"/>
//    </configSections>
//
//    <Aurora 
//       DefaultRoute="/Index" 
//       Debug="true" 
//       StaticFileExtWhiteList="\.(js|png|jpg|gif|ico|css|txt|swf)$" 
//       EncryptionKey="YourEncryptionKeyHere">
//		 <AllowedStaticFileContentTypes>
//			 <add FileExtension=".foo" ContentType="text/foobar" />
//		 </AllowedStaticFileContentTypes>
//    </Aurora>
//
//    <dotNetOpenAuth>
//      <openid>
//        <relyingParty>
//          <behaviors>
//           <!-- The following OPTIONAL behavior allows RPs to use SREG only,
//                but be compatible with OPs that use Attribute Exchange (in 
//                various formats). -->
//          <add type=
//   "DotNetOpenAuth.OpenId.Behaviors.AXFetchAsSregTransform, DotNetOpenAuth" />
//          </behaviors>
//        </relyingParty>
//      </openid>
//    </dotNetOpenAuth>
//
//    <system.web>
//      <compilation debug="true"/>
//      <httpHandlers>
//        <add verb="*" path="*" validate="false" type="Aurora.AuroraHandler"/>
//      </httpHandlers>
//      <httpModules>
//        <add type="Aurora.AuroraModule" name="AuroraModule" />
//      </httpModules>
//    </system.web>
//  </configuration>
//
// The web.config Aurora section can accept the following parameters:
// 
//  DefaultRoute="/Home/Index" 
//  Debug="true"
//  StaticFileExtWhiteList="\.(js|png|jpg|gif|ico|css|txt|swf)$"
//  EncryptionKey="Encryption Key"
//  ValidateRequest="true"
//  StaticContentCacheExpiry="15" <!-- 15 minutes -->
//  AuthCookieExpiration="8" <!-- 8 Hours -->
//  DisableStaticFileCaching="false"
//
// If you would like to perform basic Active Directory searching for user
// authentication purposes you can use the built in Active Directory class which
// provides some of the basic forms of searching for users within an AD 
// environment. 
//
// NOTE: You don't need to specify any of these in the web.config if you don't
//       want to. The Active Directory methods have overloads that will allow
//       you to pass in these at the time you call the method.
//  
//  ADSearchUser = "Encrypted Username"
//  ADSearchPW = "Encrypted Password"
//  ADSearchRoot = "LDAP://URL_GOES_HERE"
//  ADSearchDomain = "Active Directory Domain Name" 
//
// If you need to specify more than one domain and search path you can do this:
// 
// NOTE: It'd be nice to have the ability to specify more than one search path
//       per domain but that isn't possible at the moment. If you need more than
//       one search path per domain you'll have to have more than one line 
//       unfortunately specifying the domain, username and password again.
//
// 	<ActiveDirectorySearchInfo>
//		<add Domain="domain_here" 
//			 SearchRoot="LDAP://path_here" 
//			 UserName="Encrypted_User_Name" 
//			 Password="Encrypted_Password" />
//	</ActiveDirectorySearchInfo>
// 
// ------------------------
// --- Active Directory ---
// ------------------------
//
// The ActiveDirectory class provides a few static methods to search for a user
// account within an Active Directory based network. You specify the 
// ActiveDirectory domain, searchroot, username and password in the web.config
// as noted above or you can use the overloaded methods which allow you to 
// specify that information when you call the method.
//
// To search for a user account by samAccountName simply call the following
// method:
//
// ActiveDirectory.LookupUserByUserName("userName");
// 
// To look up a user account by the primary SMTP address call the following
// method:
//
// ActiveDirectory.LookupUserByEmailAddress("user@somewhere.com");
//
// You can also look an account up by it's universal principal name by calling
// the following method:
//
// ActiveDirectory.LookupUserByUpn("usersUpn");
//
// Assuming the search produced a result these methods will return an instance
// of the ActiveDirectoryUser which provides a basic set of fields that 
// represent an account as it is in ActiveDirectory. For instance, First and 
// Last name, Display name and a digital certificate are some of the fields 
// contained in this class.
//
// -------------------------------------------
// --- ACTIVE DIRECTORY CAC AUTHENTICATION ---
// -------------------------------------------
//
// A few assumptions are made pertaining to CAC authentication. Firstly, the 
// universalPrincipleName field in Active Directory is used to look up CAC ID's.
// Secondly the user in Active Directory will have a certificate in their 
// Active Directory account.
//
// The ActiveDirectoryAuthentication class is a bound action object that 
// performs the authentication. It will grab the CAC ID from the client 
// certificate submitted in the request. Then it will fire off the 
// ActiveDirectoryLookupEvent. This event is used to lookup the account in
// Active Directory. 
//
// Here is a sample handler:
//
//  public void ActiveDirectoryLookupHandler(object sender, 
//                                  ActiveDirectoryAuthenticationEventArgs args)
//  {
//    #if DEBUG
//     // Fake the logon
//     args.CACID = "1234567890;
//     args.User = ActiveDirectory.LookupUserByUpn(args.CACID);
//     args.Authenticated = true;
//    #else
//     args.User = ActiveDirectory.LookupUserByUpn(args.CACID, true);
//    #endif
//  }
//
// -------------
// --- Notes ---
// -------------
//
// Posting forms with checkboxes: 
// 
// when posting forms with checkboxes if the checkboxes aren't checked the 
// checkbox will not post it's value. I recommend using jQuery to add a dynamic 
// hidden input with the same name as your checkbox and a value of 'off' to make 
// sure that the a posted value gets sent to the server. This is necessary if 
// you have your posted form values bound to action parameters as the routing 
// depends on the proper number of parameters being posted to map to an action.
//
#endregion

#region CORE USING STATEMENTS
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Configuration;
using System.Diagnostics;
using System.Dynamic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Security.Principal;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Web;
using System.Web.Caching;
using System.Web.Configuration;
using System.Web.SessionState;
using Microsoft.Web.Infrastructure.DynamicValidationHelper;
using HtmlAgilityPack;
using Newtonsoft.Json;
using MarkdownSharp;

#if TEST
//
// Aurora will be getting built in support for TDD using Nunit. What that means
// will be defined later but suffice it to say that writing tests will be as 
// seamless and simple as possible to encourage TDD design principles within
// Aurora applications.
// 
using NUnit.Framework;
#endif

#if ACTIVE_DIRECTORY
using System.DirectoryServices;
using System.DirectoryServices.ActiveDirectory;
#endif

#if OPENID
using DotNetOpenAuth.OpenId;
using DotNetOpenAuth.OpenId.Extensions.SimpleRegistration;
using DotNetOpenAuth.OpenId.RelyingParty;
#endif

#endregion

#if LIBRARY
#region ASSEMBLY INFORMATION
[assembly: AssemblyTitle("Aurora")]
[assembly: AssemblyDescription("An MVC web framework for .NET")]
[assembly: AssemblyCompany("Frank Hale")]
[assembly: AssemblyProduct("Aurora")]
[assembly: AssemblyCopyright("(GNU GPLv3) Copyleft © 2011-2012")]
[assembly: ComVisible(false)]
[assembly: CLSCompliant(true)]
[assembly: AssemblyVersion("0.99.84.0")]
#endregion
#endif

#region TODO
//TODO: All classes looking at Session or Application for values should have that code put in a method in the ApplicationInternals class
//TODO: Add HTML helpers for checkbox list and radio button list (partially done)
//TODO: Add support to handle posted forms with checkbox and radio button lists
//TODO: Need a way to expire antiforgery tokens that were not used but sit dormant in the token cache
//TODO: Bundles ought to be a collection of CSS and Javascript instead of one or the other which is how the current bundle manager handles bundling.
//TODO: Objects that need to be in Session or Application stores ought to be wrapped into an object that can store all of them for easy retrieval respectively.
//TODO: Figure out how the Plugin infrastructure will be integrated into the framework pipeline (if at all). 
//TODO: Upload NuGet package to NuGet.org Package Gallery
//TODO: Add more events to the FrontController class, decide how much power it'll ultimately have
//TODO: Routing: Add model validation checking to the form parameters if they are being placed directly in the action parameter list rather than in a model
//TODO: HTMLHelpers: All of the areas where I'm using these Func<> lambda (craziness!) params to add name=value pairs to HTML tags need to have complimentary methods that also use a dictionary. The infrastructure has been put in place in the base HTML helper but not used yet.
//TODO: Add HTTP Patch verb support (need to research this more!)
#endregion

namespace Aurora
{
  #region VERSION
  /// <summary>
  /// Provides version information about the framework.
  /// </summary>
  public sealed class Version
  {
    public readonly string Name = "Aurora";
    public readonly string Description = "An MVC web framework for .NET";
    public readonly string Copyright = "(GNU GPLv3) Copyleft © 2011-2012";
    public readonly string[] Authors = { "Frank Hale" };
    public readonly string Website = "http://github.com/frankhale/aurora";

    public int Major
    {
      get
      {
        return version.Major;
      }
    }

    public int Minor
    {
      get
      {
        return version.Minor;
      }
    }

    public int Build
    {
      get
      {
        return version.Build;
      }
    }

    public int MinorRevision
    {
      get
      {
        return version.MinorRevision;
      }
    }

    public int Revision
    {
      get
      {
        return version.Revision;
      }
    }

    private System.Version version;

    public Version(string versionString)
    {
      version = new System.Version(versionString);
    }
  }
  #endregion

  #region CONFIGURATION

  #region AURORA PUBLIC CONFIG
  /// <summary>
  /// Any miscellaneous properties and methods that can be used by applications will be in here.
  /// </summary>
  public static class AuroraConfig
  {
    public static Aurora.Version Version = new Version("0.99.84.0");

    /// <summary>
    /// Returns true if the web application is in debug mode, false otherwise.
    /// </summary>
    public static bool InDebugMode
    {
      get
      {
        return (MainConfig.ASPNETDebug) ? true : false;
      }
    }
  }
  #endregion

  #region WEB.CONFIG CONFIGURATION
  //
  // All web.config specific stuff is here. We define the Aurora configuration 
  // element and it's sub elements.
  //
  #region CONTENT TYPE ELEMENT AND COLLECTION
  public class ContentTypeConfigurationElement : ConfigurationElement
  {
    [ConfigurationProperty("FileExtension", IsRequired = true)]
    public string FileExtension
    {
      get
      {
        return this["FileExtension"] as string;
      }
    }

    [ConfigurationProperty("ContentType", IsRequired = true)]
    public string ContentType
    {
      get
      {
        return this["ContentType"] as string;
      }
    }
  }

  public class ContentTypeConfigurationCollection : ConfigurationElementCollection
  {
    public ContentTypeConfigurationElement this[int index]
    {
      get
      {
        return base.BaseGet(index) as ContentTypeConfigurationElement;
      }

      set
      {
        if (base.BaseGet(index) != null)
        {
          base.BaseRemoveAt(index);
        }

        this.BaseAdd(index, value);
      }
    }

    protected override ConfigurationElement CreateNewElement()
    {
      return new ContentTypeConfigurationElement();
    }

    protected override object GetElementKey(ConfigurationElement element)
    {
      return ((ContentTypeConfigurationElement)element).FileExtension;
    }
  }
  #endregion

  #region ACTIVE DIRECTORY DOMAIN AND SEARCH PATH ELEMENT AND COLLECTION
#if ACTIVE_DIRECTORY
  public class ActiveDirectorySearchConfigurationElement : ConfigurationElement
  {
    [ConfigurationProperty("Domain", IsRequired = true)]
    public string Domain
    {
      get
      {
        return this["Domain"] as string;
      }
    }

    [ConfigurationProperty("SearchRoot", IsRequired = true)]
    public string SearchRoot
    {
      get
      {
        return this["SearchRoot"] as string;
      }
    }

    [ConfigurationProperty("UserName", IsRequired = true)]
    public string UserName
    {
      get
      {
        return this["UserName"] as string;
      }
    }

    [ConfigurationProperty("Password", IsRequired = true)]
    public string Password
    {
      get
      {
        return this["Password"] as string;
      }
    }
  }

  public class ActiveDirectorySearchConfigurationCollection : ConfigurationElementCollection
  {
    public ActiveDirectorySearchConfigurationElement this[int index]
    {
      get
      {
        return base.BaseGet(index) as ActiveDirectorySearchConfigurationElement;
      }

      set
      {
        if (base.BaseGet(index) != null)
        {
          base.BaseRemoveAt(index);
        }

        this.BaseAdd(index, value);
      }
    }

    protected override ConfigurationElement CreateNewElement()
    {
      return new ActiveDirectorySearchConfigurationElement();
    }

    protected override object GetElementKey(ConfigurationElement element)
    {
      return ((ActiveDirectorySearchConfigurationElement)element).Domain;
    }
  }
#endif
  #endregion

  public class WebConfig : ConfigurationSection
  {
    [ConfigurationProperty("AllowedStaticFileContentTypes")]
    public ContentTypeConfigurationCollection AllowedStaticFileContentTypes
    {
      get
      {
        return this["AllowedStaticFileContentTypes"] as ContentTypeConfigurationCollection;
      }
    }

    [ConfigurationProperty("EncryptionKey", DefaultValue = "", IsRequired = false)]
    public string EncryptionKey
    {
      get { return this["EncryptionKey"] as string; }
    }

    [ConfigurationProperty("StaticFileExtWhiteList", DefaultValue = @"\.(js|css|png|jpg|gif|ico|txt)$", IsRequired = false)]
    public string StaticFileExtWhiteList
    {
      get { return this["StaticFileExtWhiteList"] as string; }
    }

    //[ConfigurationProperty("ValidateRequest", DefaultValue = true, IsRequired = false)]
    //public bool ValidateRequest
    //{
    //  get { return Convert.ToBoolean(this["ValidateRequest"], CultureInfo.CurrentCulture); }
    //}

    [ConfigurationProperty("DefaultRoute", DefaultValue = "/Home/Index", IsRequired = false)]
    public string DefaultRoute
    {
      get { return this["DefaultRoute"] as string; }
    }

    [ConfigurationProperty("StaticContentCacheExpiry", DefaultValue = "15", IsRequired = false)]
    public int StaticContentCacheExpiry
    {
      get { return Convert.ToInt32(this["StaticContentCacheExpiry"], CultureInfo.CurrentCulture); }
    }

    [ConfigurationProperty("AuthCookieExpiration", DefaultValue = "8", IsRequired = false)]
    public int AuthCookieExpiry
    {
      get { return Convert.ToInt32(this["AuthCookieExpiration"], CultureInfo.CurrentCulture); }
    }

    [ConfigurationProperty("DisableStaticFileCaching", DefaultValue = false, IsRequired = false)]
    public bool DisableStaticFileCaching
    {
      get { return Convert.ToBoolean(this["DisableStaticFileCaching"], CultureInfo.CurrentCulture); }
    }

    #region ACTIVE DIRECTORY CONFIGURATION
#if ACTIVE_DIRECTORY
    [ConfigurationProperty("ActiveDirectorySearchInfo")]
    public ActiveDirectorySearchConfigurationCollection ActiveDirectorySearchInfo
    {
      get
      {
        return this["ActiveDirectorySearchInfo"] as ActiveDirectorySearchConfigurationCollection;
      }
    }

    [ConfigurationProperty("ADSearchUser", DefaultValue = null, IsRequired = false)]
    public string ADSearchUser
    {
      get { return this["ADSearchUser"].ToString(); }
      set { this["ADSearchUser"] = value; }
    }

    [ConfigurationProperty("ADSearchPW", DefaultValue = null, IsRequired = false)]
    public string ADSearchPW
    {
      get { return this["ADSearchPW"].ToString(); }
      set { this["ADSearchPW"] = value; }
    }

    [ConfigurationProperty("ADSearchDomain", DefaultValue = null, IsRequired = false)]
    public string ADSearchDomain
    {
      get { return this["ADSearchDomain"].ToString(); }
      set { this["ADSearchDomain"] = value; }
    }

    [ConfigurationProperty("ADSearchRoot", DefaultValue = null, IsRequired = false)]
    public string ADSearchRoot
    {
      get { return this["ADSearchRoot"].ToString(); }
      set { this["ADSearchRoot"] = value; }
    }
#endif
    #endregion
  }
  #endregion

  #region MAIN CONFIG
  /// <summary>
  /// The main config class is an area to manage all framework configuration options and variables. 
  /// </summary>
  internal static class MainConfig
  {
    static MainConfig()
    {
      MimeTypes = new Dictionary<string, string>()
      {
        { ".js",  "application/x-javascript" },  
        { ".css", "text/css" },
        { ".png", "image/png" },
        { ".jpg", "image/jpg" },
        { ".gif", "image/gif" },
        { ".svg", "image/svg+xml" },
        { ".ico", "image/x-icon" },
        { ".txt", "text/plain" }
      };

      if (WebConfig != null)
      {
        foreach (ContentTypeConfigurationElement ct in WebConfig.AllowedStaticFileContentTypes)
        {
          MimeTypes.Add(ct.FileExtension, ct.ContentType);
        }

#if ACTIVE_DIRECTORY
        if (WebConfig.ActiveDirectorySearchInfo.Count > 0)
        {
          ActiveDirectorySearchInfos = new string[WebConfig.ActiveDirectorySearchInfo.Count][];

          for (int i = 0; i < ActiveDirectorySearchInfos.Length; i++)
          {
            ActiveDirectorySearchInfos[i] = new string[] 
              { 
                WebConfig.ActiveDirectorySearchInfo[i].Domain,
                WebConfig.ActiveDirectorySearchInfo[i].SearchRoot,
                Encryption.Decrypt(WebConfig.ActiveDirectorySearchInfo[i].UserName, WebConfig.EncryptionKey),
                Encryption.Decrypt(WebConfig.ActiveDirectorySearchInfo[i].Password, WebConfig.EncryptionKey)
              };
          }
        }
#endif
      }
    }

    public static WebConfig WebConfig = ConfigurationManager.GetSection("Aurora") as WebConfig;
    public static CustomErrorsSection CustomErrorsSection = ConfigurationManager.GetSection("system.web/customErrors") as CustomErrorsSection;
    public static string[] SupportedHttpVerbs = { "GET", "POST", "PUT", "DELETE" };
    public static string EncryptionKey = (MainConfig.WebConfig == null) ? null : WebConfig.EncryptionKey;
    public static int AuthCookieExpiry = (MainConfig.WebConfig == null) ? 8 : WebConfig.AuthCookieExpiry;
    public static int StaticContentCacheExpiry = (MainConfig.WebConfig == null) ? 15 : WebConfig.StaticContentCacheExpiry;
    public static bool DisableStaticFileCaching = (MainConfig.WebConfig == null) ? false : WebConfig.DisableStaticFileCaching;
    public static bool ASPNETDebug = (ConfigurationManager.GetSection("system.web/compilation") as CompilationSection).Debug;

#if ACTIVE_DIRECTORY
    public static string ADSearchUser = (MainConfig.WebConfig == null) ? null : (!string.IsNullOrEmpty(WebConfig.ADSearchUser) && !string.IsNullOrEmpty(WebConfig.EncryptionKey)) ? Encryption.Decrypt(WebConfig.ADSearchUser, WebConfig.EncryptionKey) : null;
    public static string ADSearchPW = (MainConfig.WebConfig == null) ? null : (!string.IsNullOrEmpty(WebConfig.ADSearchPW) && !string.IsNullOrEmpty(WebConfig.EncryptionKey)) ? Encryption.Decrypt(WebConfig.ADSearchPW, WebConfig.EncryptionKey) : null;
    public static string ADSearchDomain = (MainConfig.WebConfig == null) ? null : WebConfig.ADSearchDomain;
    public static string ADSearchRoot = (MainConfig.WebConfig == null) ? null : WebConfig.ADSearchRoot;
    public static string[][] ActiveDirectorySearchInfos;
    public static string ActiveDirectorySearchCriteriaNullOrEmpty = "The search criteria specified in the Active Directory lookup cannot be null or empty";
    public static string ADUserOrPWError = "The username or password used to read from Active Directory is null or empty, please check your web.config";
    public static string ADSearchRootIsNullOrEmpty = "The Active Directory search root is null or empty";
    public static string ADSearchDomainIsNullorEmpty = "The Active Directory search domain is null or empty";
    public static string ADSearchCriteriaIsNullOrEmptyError = "The LDAP query associated with this search type is null or empty, a valid query must be annotated to this search type via the MetaData attribute";
#endif

#if OPENID
    public static string OpenIDInvalidIdentifierError = "The specified login identifier is invalid";
    public static string OpenIDLoginCancelledByProviderError = "Login was cancelled at the provider";
    public static string OpenIDLoginFailedError = "Login failed using the provided OpenID identifier";
    public static string OpenIDProviderClaimsResponseError = "The open auth provider did not return a claims response with a valid email address";
    public static string OpenIdProviderUriMismatchError = "The request OpenID provider Uri does not match the response Uri";
    public static string OpenIdClaimsResponseSessionName = "__OpenAuthClaimsResponse";
    public static string OpenIdProviderUriSessionName = "__OpenIdProviderUri";
#endif

    public static Regex PathTokenRE = new Regex(@"/(?<token>[a-zA-Z0-9]+)");
    public static Regex PathStaticFileRE = (WebConfig == null) ? new Regex(@"\.(js|css|png|jpg|gif|ico|txt)$") : new Regex(WebConfig.StaticFileExtWhiteList);
    public static Dictionary<string, string> MimeTypes;
    public static string FromRedirectOnlySessionFlag = "__FROFlag";
    public static string StaticFileManagerSessionName = "__StaticFileManager";
    public static string RouteEngineSessionName = "__RouteManager";
    public static string RoutesSessionName = "__Routes";
    public static string FrontControllerSessionName = "__FrontController";
    public static string FrontControllerInstanceSessionName = "__FrontControllerInstance";
    public static string ControllersSessionName = "__Controllers";
    public static string ControllerInstancesSessionName = "__ControllerInstances";
    public static string ModelsSessionName = "__Models";
    public static string ActionBinderSessionName = "__ActionBinder";
    public static string ActionInfosSessionName = "__ActionInfos";
    public static string AntiForgeryTokenSessionName = "__AntiForgeryTokens";
    public static string SecurityManagerSessionName = "__Securitymanager";
    public static string TemplatesSessionName = "__Templates";
    public static string CompiledViewsSessionName = "__CompiledViews";
    public static string CustomErrorSessionName = "__CustomError";
    public static string CurrentUserSessionName = "__CurrentUser";
    public static string BundleManagerSessionName = "__BundleManager";
    public static string BundleManagerInfoSessionName = "__BundleManagerInfo";
    public static string ViewEngineSessionName = "__ViewEngine";
    public static string BundleNameError = "Bundle names must include a file extension and be either CSS or Javascript";
    public static string UniquedIDSessionName = "__UniquedIDs";
    public static string AntiForgeryTokenName = "AntiForgeryToken";
    public static string JsonAntiForgeryTokenName = "JsonAntiForgeryToken";
    public static string JsonAntiForgeryTokenMissing = "An AntiForgery token is required on all Json requests";
    public static string AntiForgeryTokenMissingError = "An AntiForgery token is required on all forms unless RequireAntiForgeryToken is set to false in the [HttpPost] attribute";
    public static string AntiForgeryTokenVerificationFailedError = "AntiForgery token verification failed";
    public static string AntiForgeryTokenNullOrEmptyError = "AntiForgery token cannot be null or empty.";
    public static string AuroraAuthCookieName = "AuroraAuthCookie";
    public static string AuroraAuthTypeName = "AuroraAuth";
    public static string ViewsFolderName = "Views";
    public static string SharedFolderName = "Shared";
    public static string PublicResourcesFolderName = "Resources";
    public static string FragmentsFolderName = "Fragments";
    public static string ViewRoot = Path.DirectorySeparatorChar + "Views";
    public static string ViewRootDoesNotExistError = "The view root '{0}' does not exist.";
    public static string CannotFindViewError = "Cannot find view {0}";
    public static string EncryptionKeyNotSpecifiedError = "The encryption key has not been specified in the web.config";
    public static string PostedFormActionIncorrectNumberOfParametersError = "A post action must have at least one parameter that is the model type of the form that is being posted";
    public static string HttpRequestTypeNotSupportedError = "The HTTP Request type [{0}] is not supported";
    public static string ActionParameterTransformClassUnknownError = "The action parameter transform class cannot be determined";
    public static string Http404Error = "Http 404 - Page Not Found";
    public static string Http500Error = "Http 500 - Internal Server Error";
    public static string OnlyOneCustomErrorClassPerApplicationError = "Cannot have more than one custom error class per application";
    public static string OnlyOneFrontControllerClassPerApplicationError = "Cannot have more than one front controller class per application";
    public static string RedirectWithoutAuthorizationToError = "RedirectWithoutAuthorizationTo is either null or empty";
    public static string DefaultRoute = (WebConfig == null) ? "/Home/Index" : WebConfig.DefaultRoute;
    public static string HtmlTableStartOrLengthOutOfBoundsWithModelError = "The start or length is out of bounds with the model";

    #region ACTION BINDER
    public static string ActionBinderNullOrEmptyControllerNameError = "The controller name cannot be null or empty";
    public static string ActionBinderBindingInstanceNullOrEmptyError = "The binding instance(s) cannot be null or empty";
    public static string ActionBinderActionNameNullorEmptyError = "The action name(s) cannot be null or empty";
    #endregion

    #region MODEL VALIDATION
    public static string ModelValidationErrorRequiredField = "Model Validation Error: {0} is a required field";
    public static string ModelValidationErrorRequiredLength = "Model Validation Error: {0} has a required length that was not met";
    public static string ModelValidationErrorRegularExpression = "Model Validation Error: {0} did not pass regular expression validation";
    public static string ModelValidationErrorRange = "Model Validation Error: {0} was not within the range specified";
    #endregion

    #region PLUGIN MANAGEMENT
    public static string TypeDoesNotImplementTheIPluginInterfaceError = "{0} does not implement the IPlugin interface";
    public static string AssemblyContainsNoPluginsError = "There are no plugins in {0}";
    public static string UnableToLoadPluginAssembly = "Unable to load {0}";
    #endregion

#if !DEBUG
    public static string ClientCertificateError = "The HttpContext.Request.ClientCertificate did not contain a valid certificate";
    public static string ClientCertificateSimpleNameError = "Cannot determine the simple name from the client certificate";
    public static string ClientCertificateUnexpectedFormatForCACID = "The CAC ID was not in the expected format within the common name (last.first.middle.cacid), actual CN = {0}";
#endif
  }
  #endregion

  #endregion

  #region APPLICATION INTERNALS
  /// <summary>
  /// The purpose of this class is to do all the reflection and plumbing needed to obtain information
  /// about the application that uses this framework.
  /// </summary>
  internal static class ApplicationInternals
  {
    /// <summary>
    /// This is a representation of action related information. This is used as a helper for
    /// various methods below to assist in building up the RouteInfo list.
    /// </summary>
    internal struct ActionInfo
    {
      public string ControllerName;
      public Type ControllerType;
      public string ActionName;
      public MethodInfo ActionMethod;
      public Attribute Attribute;
      public List<string> Aliases;
    }

    internal static List<Type> GetTypeList(IAuroraContext context, string sessionName, Type t)
    {
      context.ThrowIfArgumentNull();

      List<Type> types = null;

      if (context.Application[sessionName] != null)
      {
        types = context.Application[sessionName] as List<Type>;
      }
      else
      {
        try
        {
          types = (from assembly in AppDomain.CurrentDomain.GetAssemblies().Where(x => x.GetName().Name != "DotNetOpenAuth")
                   from type in assembly.GetTypes().Where(x => x.BaseType == t)
                   select type).ToList();
        }
        catch (ReflectionTypeLoadException rtle)
        {
          string errorMessage = string.Empty;

          if (rtle is ReflectionTypeLoadException)
          {
            StringBuilder errorBuilder = new StringBuilder();

            foreach (var ex in (rtle as ReflectionTypeLoadException).LoaderExceptions)
            {
              errorBuilder.AppendFormat("{0}<br/>", ex.Message);
            }

            errorMessage = errorBuilder.ToString();
          }

          CustomError customError = GetCustomError(context, false);

          if (customError != null)
          {
            customError.OnError(errorMessage, rtle);
          }
        }

        context.Application[sessionName] = types;
      }

      return types;
    }

    public static List<Type> AllControllers(IAuroraContext context)
    {
      context.ThrowIfArgumentNull();

      if (context.Application[MainConfig.ControllersSessionName] != null)
      {
        return context.Application[MainConfig.ControllersSessionName] as List<Type>;
      }
      else
      {
        List<Type> controllers = GetTypeList(context, MainConfig.ControllersSessionName, typeof(Controller));

        context.Application[MainConfig.ControllersSessionName] = controllers;

        return controllers;
      }
    }

    public static List<Controller> AllControllerInstances(IAuroraContext context)
    {
      context.ThrowIfArgumentNull();

      List<Controller> controllerInstances = null;

      if (context.Application[MainConfig.ControllerInstancesSessionName] == null)
      {
        controllerInstances = new List<Controller>();

        context.Application[MainConfig.ControllerInstancesSessionName] = controllerInstances;
      }
      else
      {
        controllerInstances = context.Application[MainConfig.ControllerInstancesSessionName] as List<Controller>;
      }

      return controllerInstances;
    }

    public static List<Type> AllModels(IAuroraContext context)
    {
      context.ThrowIfArgumentNull();

      return GetTypeList(context, MainConfig.ModelsSessionName, typeof(Model));
    }

    internal static IEnumerable<ActionInfo> _GetAllActionInfos(IAuroraContext context)
    {
      context.ThrowIfArgumentNull();

      foreach (Type c in AllControllers(context))
      {
        foreach (MethodInfo mi in c.GetMethods())
        {
          List<string> aliases = mi.GetCustomAttributes(false)
                                   .Where(x => x.GetType() == typeof(AliasAttribute))
                                   .Cast<AliasAttribute>()
                                   .Select(x => x.Alias)
                                   .ToList();

          foreach (Attribute a in mi.GetCustomAttributes(false).Where(x => x.GetType() != typeof(AliasAttribute)))
          {
            if ((a is HttpGetAttribute) ||
                (a is HttpPostAttribute) ||
                (a is HttpPutAttribute) ||
                (a is HttpDeleteAttribute) ||
                (a is FromRedirectOnlyAttribute))
            {
              HttpGetAttribute get = (a as HttpGetAttribute);
              HttpPostAttribute post = (a as HttpPostAttribute);
              HttpPutAttribute put = (a as HttpPutAttribute);
              HttpDeleteAttribute delete = (a as HttpDeleteAttribute);
              FromRedirectOnlyAttribute fro = (a as FromRedirectOnlyAttribute);

              if ((get != null) || (fro != null) || (post != null) || (put != null) || (delete != null))
              {
                yield return new ActionInfo()
                {
                  ControllerName = c.Name,
                  ControllerType = c,
                  ActionName = mi.Name,
                  ActionMethod = mi,
                  Attribute = a,
                  Aliases = aliases
                };
              }
            }
          }
        }
      }
    }

    public static List<ActionInfo> AllActionInfos(IAuroraContext context)
    {
      context.ThrowIfArgumentNull();

      List<ActionInfo> actionInfos = new List<ActionInfo>();

      if (context.Application[MainConfig.ActionInfosSessionName] != null)
      {
        actionInfos = context.Application[MainConfig.ActionInfosSessionName] as List<ActionInfo>;
      }
      else
      {
        foreach (ActionInfo ai in _GetAllActionInfos(context))
        {
          actionInfos.Add(ai);
        }
      }

      return actionInfos;
    }

    public static Dictionary<string, string> AllPartitionNames(IAuroraContext context)
    {
      context.ThrowIfArgumentNull();

      Dictionary<string, string> partitionNames = new Dictionary<string, string>();

      foreach (Type c in AllControllers(context))
      {
        PartitionAttribute partitionAttrib = (PartitionAttribute)c.GetCustomAttributes(false).FirstOrDefault(x => x.GetType() == typeof(PartitionAttribute));

        if (partitionAttrib != null)
        {
          partitionNames[c.Name] = partitionAttrib.Name;
        }
      }

      if (partitionNames.Count() > 0)
      {
        return partitionNames;
      }

      return null;
    }

    public static string[] AllViewRoots(IAuroraContext context)
    {
      context.ThrowIfArgumentNull();

      Dictionary<string, string> partitionNames = AllPartitionNames(context);

      List<string> partitionViewRoots = new List<string>();

      if (partitionNames != null)
      {
        foreach (KeyValuePair<string, string> kvp in partitionNames)
        {
          partitionViewRoots.Add(context.MapPath("/" + kvp.Value));
        }
      }

      partitionViewRoots.Add(context.MapPath(MainConfig.ViewRoot));

      return partitionViewRoots.ToArray();
    }

    public static List<string> AllRoutableActionNames(IAuroraContext context, string controllerName)
    {
      context.ThrowIfArgumentNull();

      List<string> routableActionNames = new List<string>();

      foreach (ActionInfo ai in _GetAllActionInfos(context).Where(x => x.ControllerType.Name == controllerName))
      {
        routableActionNames.Add(ai.ActionMethod.Name);
      }

      if (routableActionNames.Count() > 0)
      {
        return routableActionNames;
      }

      return null;
    }

    public static List<string> AllRouteAliases(IAuroraContext context)
    {
      context.ThrowIfArgumentNull();

      if (context.Application[MainConfig.RoutesSessionName] != null)
      {
        List<RouteInfo> routes = context.Application[MainConfig.RoutesSessionName] as List<RouteInfo>;

        if (routes.Count() > 0)
          return routes.Select(x => x.Alias).ToList();
      }

      return null;
    }

    public static List<RouteInfo> AllRouteInfos(IAuroraContext context)
    {
      context.ThrowIfArgumentNull();

      List<RouteInfo> routes = null;
      List<ActionInfo> actionInfos = null;

      if (context.Application[MainConfig.RoutesSessionName] != null)
      {
        routes = context.Application[MainConfig.RoutesSessionName] as List<RouteInfo>;
        actionInfos = context.Application[MainConfig.ActionInfosSessionName] as List<ActionInfo>;
      }
      else
      {
        routes = new List<RouteInfo>();
        actionInfos = new List<ActionInfo>();

        context.Application[MainConfig.RoutesSessionName] = routes;
        context.Application[MainConfig.ActionInfosSessionName] = actionInfos;
      }

      foreach (Type c in AllControllers(context))
      {
        Controller ctrl = AllControllerInstances(context).FirstOrDefault(x => x.GetType() == c);

        if (ctrl == null || routes.Count() == 0)
        {
          #region INSTANTIATE CONTROLLER
          MethodInfo createInstance = c.BaseType.GetMethod("CreateInstance", BindingFlags.Static | BindingFlags.NonPublic);
          ctrl = (Controller)createInstance.Invoke(createInstance, new object[] { c, context });
          AllControllerInstances(context).Add(ctrl);
          #endregion

          foreach (ActionInfo ai in _GetAllActionInfos(context).Where(x => x.ControllerType.Name == c.Name))
          {
            RequestTypeAttribute attr = (ai.Attribute as RequestTypeAttribute);

            if (routes.FirstOrDefault(
                x =>
                  x.Alias == attr.RouteAlias &&
                  x.Action.Name == ai.ActionMethod.Name &&
                  x.Action.GetParameters().Count() == ai.ActionMethod.GetParameters().Count()
              ) != null)
            {
              continue;
            }

            Action<string> addRoute = delegate(string alias)
            {
              RouteInfo routeInfo = new RouteInfo()
              {
                Alias = alias,
                Context = context,
                ControllerName = c.Name,
                ControllerType = c,
                Action = ai.ActionMethod,
                ActionName = ai.ActionMethod.Name,
                Bindings = new ActionBinder(context).GetBindings(c.Name, ai.ActionMethod.Name),
                FromRedirectOnlyInfo = (attr as FromRedirectOnlyAttribute) != null ? true : false,
                Dynamic = (attr as FromRedirectOnlyAttribute) != null ? true : false,
                IsFiltered = false,
                RequestType = attr.RequestType,
                Attribute = attr,
                SkipValidationParameterNames = GetSkipValidationParameterNames(ai.ActionMethod)
              };

              routes.Add(routeInfo);
            };

            addRoute((!string.IsNullOrEmpty(attr.RouteAlias)) ? attr.RouteAlias :
                     string.Format(CultureInfo.InvariantCulture, "/{0}/{1}", c.Name, ai.ActionMethod.Name));

            foreach (string alias in ai.Aliases)
            {
              addRoute(alias);
            }

            actionInfos.Add(ai);
          }
        }
        else
          ctrl.Refresh(context);

        foreach (RouteInfo routeInfo in routes)
          routeInfo.ControllerInstance = ctrl;
      }

      return routes;
    }

    public static Type GetActionTransformClassType(ActionParamTransformAttribute actionParameterTransform)
    {
      actionParameterTransform.ThrowIfArgumentNull();

      Type actionTransformClassType = (from assembly in AppDomain.CurrentDomain.GetAssemblies().Where(x => x.GetName().Name != "DotNetOpenAuth")
                                       from type in assembly.GetTypes().Where(x => x.GetInterface(typeof(IActionParamTransform<,>).Name) != null && x.Name == actionParameterTransform.TransformName)
                                       select type).FirstOrDefault();

      if (actionTransformClassType != null)
      {
        return actionTransformClassType;
      }

      throw new ActionParameterTransformClassUnknownException(MainConfig.ActionParameterTransformClassUnknownError);
    }

    public static void RemoveRouteInfo(IAuroraContext context, string alias)
    {
      context.ThrowIfArgumentNull();

      List<RouteInfo> routes = AllRouteInfos(context);
      List<RouteInfo> routeInfos = routes.Where(x => x.Dynamic == true && x.Alias == alias).ToList();

      foreach (RouteInfo routeInfo in routeInfos)
      {
        routes.Remove(routeInfo);
      }

      if (routeInfos.Count > 0)
      {
        context.Application[MainConfig.RoutesSessionName] = routes;
      }
    }

    public static void AddRouteInfo(IAuroraContext context, string alias, Controller controller, MethodInfo action, string requestType, string frontParams)
    {
      context.ThrowIfArgumentNull();

      if (string.IsNullOrEmpty(alias))
      {
        throw new ArgumentNullException("alias");
      }

      if (string.IsNullOrEmpty(requestType))
      {
        throw new ArgumentNullException("requestType");
      }

      List<RouteInfo> routes = context.Application[MainConfig.RoutesSessionName] as List<RouteInfo>;

      if (routes == null)
      {
        routes = new List<RouteInfo>();
      }

      if (!alias.StartsWith("/"))
        alias = string.Concat("/", alias);

      if (routes.FirstOrDefault(x => x.Alias == alias && x.Action == action) == null)
      {
        routes.Add(new RouteInfo()
        {
          Alias = alias,
          Action = action,
          ActionName = action.Name,
          Context = context,
          ControllerInstance = controller,
          ControllerName = action.DeclaringType.Name,
          ControllerType = controller.GetType(),
          FrontLoadedParams = frontParams,
          RequestType = requestType,
          Bindings = new ActionBinder(context).GetBindings(controller.GetType().Name, action.Name),
          Dynamic = true,
          SkipValidationParameterNames = GetSkipValidationParameterNames(action)
        });

        context.Application[MainConfig.RoutesSessionName] = routes;
      }
    }

    public static FrontController GetFrontController(IAuroraContext context)
    {
      context.ThrowIfArgumentNull();

      List<Type> frontController = GetTypeList(context, MainConfig.FrontControllerSessionName, typeof(FrontController));

      if (frontController != null && frontController.Count > 0)
      {
        if (frontController.Count > 1)
        {
          throw new Exception(MainConfig.OnlyOneFrontControllerClassPerApplicationError);
        }

        // Check to see if we already have an instance created otherwise create one
        if (context.Application[MainConfig.FrontControllerInstanceSessionName] != null)
        {
          FrontController fc = context.Application[MainConfig.FrontControllerInstanceSessionName] as FrontController;
          fc.Refresh(context);

          return fc;
        }
        else
        {
          FrontController fc = FrontController.CreateInstance(frontController[0], context);

          context.Application[MainConfig.FrontControllerInstanceSessionName] = fc;

          fc.Refresh(context);

          return fc;
        }
      }

      return null;
    }

    public static CustomError GetCustomError(IAuroraContext context, bool useDefaultHandler)
    {
      context.ThrowIfArgumentNull();

      Type errorType = typeof(DefaultCustomError);

      if (!useDefaultHandler)
      {
        List<Type> customErrors = GetTypeList(context, MainConfig.CustomErrorSessionName, typeof(CustomError)).Where(x => x.Name != "DefaultCustomError").ToList();

        if (customErrors.Count > 1)
        {
          throw new Exception(MainConfig.OnlyOneCustomErrorClassPerApplicationError);
        }

        if (customErrors.Count != 0)
        {
          errorType = customErrors[0];
        }
      }

      return CustomError.CreateInstance(errorType, context);
    }

    public static bool GetFromRedirectOnlyFlag(IAuroraContext context)
    {
      bool fromRedirectOnlyFlag = false;

      if (context.Session[MainConfig.FromRedirectOnlySessionFlag] != null)
      {
        fromRedirectOnlyFlag = (bool)context.Session[MainConfig.FromRedirectOnlySessionFlag];
      }

      return fromRedirectOnlyFlag;
    }

    public static IViewEngine GetViewEngine(IAuroraContext context)
    {
      context.ThrowIfArgumentNull();

      IViewEngine viewEngine;

      if (context.Application[MainConfig.ViewEngineSessionName] != null)
      {
        viewEngine = context.Application[MainConfig.ViewEngineSessionName] as IViewEngine;

        //if (AuroraConfig.InDebugMode)
        //{
        viewEngine.Refresh(context);
        //}
      }
      else
      {
        viewEngine = new AuroraViewEngine(context);

        context.Application[MainConfig.ViewEngineSessionName] = viewEngine;
      }

      return viewEngine;
    }

    public static IRouteEngine GetRouteEngine(IAuroraContext context)
    {
      context.ThrowIfArgumentNull();

      IRouteEngine routeEngine;

      if (!MainConfig.ASPNETDebug && context.Application[MainConfig.RouteEngineSessionName] != null)
      {
        routeEngine = context.Application[MainConfig.RouteEngineSessionName] as IRouteEngine;

        routeEngine.Refresh(context);
      }
      else
      {
        routeEngine = new AuroraRouteEngine(context);

        context.Application[MainConfig.RouteEngineSessionName] = routeEngine;
      }

      return routeEngine;
    }

    public static List<string> GetSkipValidationParameterNames(MethodInfo method)
    {
      method.ThrowIfArgumentNull();

      return method.GetParameters().Where(x => x.GetCustomAttributes(false)
                                                .FirstOrDefault(y => y.GetType() == typeof(SkipValidationAttribute)) != null)
                                                .Select(x => x.Name)
                                                .ToList();
    }
  }
  #endregion

  #region ATTRIBUTES
  public enum ActionSecurity
  {
    Secure,
    None	// dummy value for RequestTypeAttribute.SecurityType default
  }

  public enum DescriptiveNameOperation
  {
    SplitCamelCase,
    None
  }

  #region APP PARTITION
  [AttributeUsage(AttributeTargets.Class)]
  public sealed class PartitionAttribute : Attribute
  {
    public string Name { get; private set; }

    public PartitionAttribute(string name)
    {
      Name = name;
    }
  }
  #endregion

  #region MISCELLANEOUS
  [AttributeUsage(AttributeTargets.All)]
  public sealed class MetadataAttribute : Attribute
  {
    public string Metadata { get; private set; }

    public MetadataAttribute(string metadata)
    {
      Metadata = metadata;
    }
  }

  [AttributeUsage(AttributeTargets.All)]
  public sealed class DescriptiveNameAttribute : Attribute
  {
    public string Name { get; private set; }

    public DescriptiveNameOperation Op { get; private set; }

    public DescriptiveNameAttribute(string name)
    {
      Name = name;
      Op = DescriptiveNameOperation.None;
    }

    public DescriptiveNameAttribute(DescriptiveNameOperation op)
    {
      Name = string.Empty; // Name comes from property name

      Op = op; // We'll perform an operation on the property name like put spacing between camel case names, then title case the name.
    }

    public string PerformOperation(string name)
    {
      // This regex comes from this StackOverflow question answer:
      //
      // http://stackoverflow.com/questions/155303/net-how-can-you-split-a-caps-delimited-string-into-an-array
      if (Op == DescriptiveNameOperation.SplitCamelCase)
      {
        return Regex.Replace(name, @"([a-z](?=[A-Z])|[A-Z](?=[A-Z][a-z]))", "$1 ");
      }

      return null;
    }
  }

  [AttributeUsage(AttributeTargets.Parameter | AttributeTargets.Property)]
  public sealed class SanitizeAttribute : Attribute
  {
    public bool StripHtml { get; private set; }
    public bool HtmlEncode { get; private set; }

    public SanitizeAttribute()
    {
      StripHtml = true;
      HtmlEncode = true;
    }

    public string Sanitize(string value)
    {
      if (StripHtml)
      {
        value = value.StripHtml();
      }

      if (HtmlEncode)
      {
        value = HttpUtility.HtmlEncode(value);
      }

      return value;
    }
  }

  [AttributeUsage(AttributeTargets.Property)]
  public sealed class HiddenAttribute : Attribute
  {
  }

  [AttributeUsage(AttributeTargets.Property | AttributeTargets.Parameter)]
  public sealed class NotRequiredAttribute : Attribute
  {
  }

  [AttributeUsage(AttributeTargets.Property)]
  internal sealed class ExcludeFromBindingAttribute : Attribute
  {
  }

  [AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
  internal sealed class AliasAttribute : Attribute
  {
    public string Alias { get; private set; }

    public AliasAttribute(string alias)
    {
      Alias = alias;
    }
  }

  /// <summary>
  /// Skips ASP.NET form post validation
  /// </summary>
  [AttributeUsage(AttributeTargets.Property | AttributeTargets.Parameter)]
  public sealed class SkipValidationAttribute : Attribute
  {
  }
  #endregion

  #region HTTP REQUEST
  public abstract class RequestTypeAttribute : Attribute
  {
    public ActionSecurity SecurityType { get; set; }
    public string RouteAlias { get; set; }
    public string Roles { get; set; }
    public bool HttpsOnly { get; set; }
    public string RedirectWithoutAuthorizationTo { get; set; }
    public string RequestType { get; private set; }

    protected RequestTypeAttribute(string requestType)
    {
      SecurityType = ActionSecurity.None;
      RequestType = requestType;
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
    public HttpCacheability CacheabilityOption { get; set; }
    public bool Cache { get; set; }
    public int Duration { get; set; } // in minutes
    public string Refresh { get; set; }

    internal TimeSpan Expires { get; set; }
    internal DateTime DateExpiry { get; set; }

    public HttpGetAttribute()
      : base("GET")
    {
      Init();
    }

    public HttpGetAttribute(string routeAlias)
      : base("GET")
    {
      RouteAlias = routeAlias;

      Init();
    }

    public HttpGetAttribute(ActionSecurity sec) : this(string.Empty, sec) { }

    public HttpGetAttribute(string routeAlias, ActionSecurity sec)
      : base("GET")
    {
      SecurityType = sec;
      RouteAlias = routeAlias;

      Init();
    }

    private void Init()
    {
      CacheabilityOption = HttpCacheability.Public;
      Duration = 15;
      Expires = new TimeSpan(0, 15, 0);
      DateExpiry = DateTime.Now.Add(new TimeSpan(0, 15, 0));
    }
  }

  [AttributeUsage(AttributeTargets.Method)]
  public sealed class HttpPostAttribute : RequestTypeAttribute
  {
    public bool RequireAntiForgeryToken { get; set; }

    public HttpPostAttribute() : base("POST") { }

    public HttpPostAttribute(string routeAlias)
      : base("POST")
    {
      RouteAlias = routeAlias;
      RequireAntiForgeryToken = true;
    }

    public HttpPostAttribute(ActionSecurity sec)
      : this(string.Empty, sec)
    {
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
    public bool RequireAntiForgeryToken { get; set; }

    public HttpPutAttribute() : base("PUT") { }

    public HttpPutAttribute(string routeAlias)
      : base("PUT")
    {
      RouteAlias = routeAlias;
      RequireAntiForgeryToken = true;
    }

    public HttpPutAttribute(ActionSecurity sec)
      : this(string.Empty, sec)
    {
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
    public bool RequireAntiForgeryToken { get; set; }
    public HttpDeleteAttribute() : base("DELETE") { }

    public HttpDeleteAttribute(string routeAlias)
      : base("DELETE")
    {
      RouteAlias = routeAlias;
      RequireAntiForgeryToken = true;
    }

    public HttpDeleteAttribute(ActionSecurity sec)
      : this(string.Empty, sec)
    {
    }

    public HttpDeleteAttribute(string routeAlias, ActionSecurity sec)
      : base("DELETE")
    {
      SecurityType = sec;
      RouteAlias = routeAlias;
    }
  }
  #endregion

  #region MODEL

  #region STRING FORMAT
  [AttributeUsage(AttributeTargets.Property)]
  public sealed class StringFormatAttribute : Attribute
  {
    public string Format { get; set; }

    public StringFormatAttribute(string format)
    {
      Format = format;
    }
  }
  #endregion

  #region DATE FORMAT
  [AttributeUsage(AttributeTargets.Property)]
  public sealed class DateFormatAttribute : Attribute
  {
    public string Format { get; set; }

    public DateFormatAttribute(string format)
    {
      Format = format;
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

  #region UNIQUE ID
  [AttributeUsage(AttributeTargets.Field)]
  public sealed class UniqueIdAttribute : Attribute
  {
    internal string Id { get; set; }

    public UniqueIdAttribute() { }

    internal void GenerateID(string name)
    {
      HttpContext ctx = HttpContext.Current;
      Dictionary<string, string> uids = null;

      if (ctx.Session[MainConfig.UniquedIDSessionName] != null)
      {
        uids = ctx.Session[MainConfig.UniquedIDSessionName] as Dictionary<string, string>;
      }
      else
      {
        ctx.Session[MainConfig.UniquedIDSessionName] = uids = new Dictionary<string, string>();
      }

      if (!uids.ContainsKey(name))
      {
        Id = NewUID();
        uids[name] = Id;
      }
      else
      {
        Id = uids[name];
      }
    }

    private static string NewUID()
    {
      return Guid.NewGuid().ToString().Replace("-", "").Substring(0, 16);
    }
  }
  #endregion

  #region ACTION FILTER
  /// <summary>
  /// This empty interface is used to identify a filter result that will be passed
  /// to an action after the filter is invoked.
  /// </summary>
  public interface IActionFilterResult
  {
    //FIXME: Warning	224	CA1040 : Microsoft.Design : Define a custom attribute to replace 'IActionFilterResult'.
  }

  //[AttributeUsage(AttributeTargets.Parameter)]
  //public class ActionFilterResultAttribute : Attribute 
  //{ 
  //  This will be used when I implement the fix for the warning listed in IActionFilterResult
  //}

  [AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
  public abstract class ActionFilterAttribute : Attribute
  {
    internal Controller Controller { get; set; }

    public IActionFilterResult FilterResult { get; set; }

    public abstract void OnFilter(RouteInfo routeInfo);

    public void RedirectToAlias(string alias)
    {
      Controller.RedirectToAlias(alias);
    }

    public void RedirectOnlyToAlias(string alias)
    {
      Controller.RedirectOnlyToAlias(alias);
    }

    public void RedirectToAction(string controllerName, string actionName)
    {
      Controller.RedirectToAction(controllerName, actionName);
    }

    public void RedirectOnlyToAction(string controllerName, string actionName)
    {
      Controller.RedirectOnlyToAction(controllerName, actionName);
    }
  }
  #endregion
  #endregion

  #region AURORA CONTEXT
  /// <summary>
  /// A thin wrapper which is used to set the various Response.Cache options
  /// </summary>
  public class AuroraCachePolicy
  {
    public HttpCacheability CacheabilityOption { get; set; }
    public DateTime Expires { get; set; }
    public TimeSpan MaxAge { get; set; }
    public bool ValidUntilExpires { get; set; }
    public bool IgnoreParams { get; set; }
    public bool NoStore { get; set; }
  }

  /// <summary>
  /// The interface which defines a set of methods used to wrap the ASP.NET Session
  /// </summary>
  public interface IAuroraSessionStore
  {
    string SessionID { get; }

    /// <summary>
    /// Adds a new session key and data value to the store.
    /// </summary>
    /// <param name="key">The key</param>
    /// <param name="value">The data</param>
    void Add(string key, object value);

    /// <summary>
    /// Removes a key and it's data value from the store.
    /// </summary>
    /// <param name="key">The key</param>
    void Remove(string key);

    /// <summary>
    /// Gets a value from the session store by looking for it's key name.
    /// </summary>
    /// <param name="key">The key</param>
    /// <returns>The data associated with this key</returns>
    object Get(string key);

    /// <summary>
    /// Gets a value from the session store by indexing on it's key name.
    /// </summary>
    /// <param name="key">The key</param>
    /// <returns>The data associated with this key</returns>
    object this[string key] { get; set; }
  }

  public class AuroraApplicationState : IAuroraSessionStore
  {
    public string SessionID
    {
      get { return string.Empty; }
    }

    private HttpApplicationStateBase AppState;

    public AuroraApplicationState(HttpApplicationStateBase state)
    {
      AppState = state;
    }

    public void Add(string key, object value)
    {
      AppState.Lock();
      AppState.Add(key, value);
      AppState.UnLock();
    }

    public void Remove(string key)
    {
      AppState.Remove(key);
    }

    public object Get(string key)
    {
      return AppState[key];
    }

    public object this[string key]
    {
      get
      {
        string k = AppState.AllKeys.FirstOrDefault(x => x == key);

        if (string.IsNullOrEmpty(k))
        {
          return null;
        }

        return AppState[key];
      }
      set
      {
        AppState.Lock();
        AppState[key] = value;
        AppState.UnLock();
      }
    }
  }

  public class AuroraSession : IAuroraSessionStore
  {
    public string SessionID
    {
      get { return Session.SessionID; }
    }

    private HttpSessionStateBase Session;

    public AuroraSession(HttpSessionStateBase session)
    {
      Session = session;
    }

    public void Add(string key, object value)
    {
      Session.Add(key, value);
    }

    public void Remove(string key)
    {
      Session.Remove(key);
    }

    public object Get(string key)
    {
      return Session[key];
    }

    public object this[string key]
    {
      get
      {
        return Session[key];
      }

      set
      {
        Session[key] = value;
      }
    }
  }

  /// <summary>
  /// A fake session store used for both Session and Application
  /// </summary>
  public class AuroraFakeSessionStore : IAuroraSessionStore
  {
    public string SessionID
    {
      get { return string.Empty; }
    }

    private Dictionary<string, object> Session;

    public AuroraFakeSessionStore()
    {
      Session = new Dictionary<string, object>();
    }

    public void Add(string key, object value)
    {
      Session.Add(key, value);
    }

    public void Remove(string key)
    {
      if (Session.ContainsKey(key))
      {
        Session.Remove(key);
      }
    }

    public object Get(string key)
    {
      if (Session.ContainsKey(key))
      {
        return Session[key];
      }

      return null;
    }

    public object this[string key]
    {
      get
      {
        if (Session.ContainsKey(key))
        {
          return Session[key];
        }

        return null;
      }
      set
      {
        Session[key] = value;
      }
    }
  }

  /// <summary>
  /// The interface which defines what an Aurora Context will look like
  /// </summary>
  public interface IAuroraContext
  {
    HttpContextBase RawContext { get; set; }

    RouteInfo RouteInfo { get; set; }

    X509Certificate2 RequestClientCertificate { get; set; }

    IAuroraSessionStore Application { get; set; }
    IAuroraSessionStore Session { get; set; }

    NameValueCollection RequestHeaders { get; set; }
    NameValueCollection Form { get; set; }
    NameValueCollection QueryString { get; set; }

    string IPAddress { get; }

    bool IsSecureConnection { get; set; }

    HttpCookieCollection RequestCookies { get; set; }
    HttpCookieCollection ResponseCookies { get; set; }
    NameValueCollection RequestServerVariables { get; set; }

    Uri RequestUrl { get; set; }

    IPrincipal User { get; set; }

    string ApplicationPhysicalPath { get; set; }
    string Path { get; set; }
    string RequestType { get; set; }
    NameValueCollection RequestPayload { get; set; }

    HttpPostedFileBase[] Files { get; set; }

    Cache Cache { get; set; }

    Stream ResponseFilter { get; set; }

    string ResponseCharset { get; set; }
    string ResponseContentType { get; set; }

    string MapPath(string path);
    void ValidateInput();
    void RewritePath(string path);
    void ResponseRedirect(string url);
    void ResponseWrite(string text);
    void ResponseBinaryWrite(byte[] bits);
    void ResponseTransmitFile(string fileName);
    void ResponseAddHeader(string name, string value);
    void ResponseAppendHeader(string name, string value);
    void ResponseClearContent();
    void ResponseClearHeaders();
    void ResponseSetCachePolicy(AuroraCachePolicy policy);
    void ResponseSetNoCache();
    void ResponseStatusCode(HttpStatusCode statusCode);
    void ServerClearError();

    string GetValidatedFormValue(string key);
    //string GetValidatedQueryStringValue(string key);
  }

  /// <summary>
  /// The AuroraContext wrapper around an HttpContext. Hopefully this can also
  /// be used as a fake context by just using the default constructor and setting
  /// the various bits. Perhaps we need some overloads here to make it a bit more
  /// sensable. 
  /// </summary>
  public class AuroraContext : IAuroraContext
  {
    public HttpContextBase RawContext { get; set; }

    public RouteInfo RouteInfo { get; set; }

    public X509Certificate2 RequestClientCertificate { get; set; }

    public IAuroraSessionStore Application { get; set; }
    public IAuroraSessionStore Session { get; set; }

    public NameValueCollection Form { get; set; }
    public NameValueCollection QueryString { get; set; }

    public HttpCookieCollection RequestCookies { get; set; }
    public HttpCookieCollection ResponseCookies { get; set; }

    public NameValueCollection RequestHeaders { get; set; }
    public NameValueCollection RequestServerVariables
    {
      get
      {
        if (RawContext != null)
        {
          return RawContext.Request.ServerVariables;
        }
        else
        {
          return new NameValueCollection();
        }
      }

      set
      {
        RequestServerVariables = value;
      }
    }

    public Uri RequestUrl
    {
      get
      {
        if (RawContext != null)
        {
          return RawContext.Request.Url;
        }
        else
        {
          return null;
        }
      }

      set
      {
        RequestUrl = value;
      }
    }

    private IPrincipal _User;
    public IPrincipal User
    {
      get
      {
        if (RawContext != null)
        {
          return RawContext.User;
        }
        else
        {
          return _User;
        }
      }

      set
      {
        if (RawContext != null)
        {
          RawContext.User = value;
        }
        else
        {
          _User = value;
        }
      }
    }

    public string IPAddress
    {
      get
      {
        if (RawContext != null)
        {
          // This method is based on the following example at StackOverflow:
          //
          // http://stackoverflow.com/questions/735350/how-to-get-a-users-client-ip-address-in-asp-net
          string ip = RawContext.Request.ServerVariables["HTTP_X_FORWARDED_FOR"];

          if (string.IsNullOrEmpty(ip))
          {
            return RawContext.Request.ServerVariables["REMOTE_ADDR"];
          }
          else
          {
            return ip.Split(',')[0];
          }
        }
        else
        {
          return "127.0.0.1";
        }
      }
    }

    public bool IsSecureConnection { get; set; }

    public string ApplicationPhysicalPath { get; set; }
    public string Path { get; set; }
    public string RequestType { get; set; }

    public NameValueCollection RequestPayload { get; set; }

    public HttpPostedFileBase[] Files { get; set; }

    public Cache Cache { get; set; }

    public Stream ResponseFilter
    {
      get
      {
        if (RawContext != null)
        {
          return RawContext.Response.Filter;
        }
        else
        {
          return null;
        }
      }

      set
      {
        if (RawContext != null)
        {
          RawContext.Response.Filter = value;
        }
        else
        {
          ResponseFilter = value;
        }
      }
    }

    public string ResponseCharset
    {
      get
      {
        if (RawContext != null)
        {
          return RawContext.Response.Charset;
        }
        else
        {
          return ResponseCharset;
        }
      }

      set
      {
        if (RawContext != null)
        {
          RawContext.Response.Charset = value;
        }
        else
        {
          ResponseCharset = value;

        }
      }
    }

    public string ResponseContentType
    {
      get
      {
        if (RawContext != null)
        {
          return RawContext.Response.ContentType;
        }
        else
        {
          return ResponseContentType;
        }
      }

      set
      {
        if (RawContext != null)
        {
          RawContext.Response.ContentType = value;
        }
        else
        {
          ResponseContentType = value;
        }
      }
    }

    private void Init()
    {
      RawContext = null;
      RequestClientCertificate = null;
      Cache = new Cache();
      User = null;
      IsSecureConnection = false;
      ApplicationPhysicalPath = string.Empty;
      Path = "~/";
      RequestType = "GET";
      Form = new NameValueCollection();
      QueryString = new NameValueCollection();
      RequestCookies = new HttpCookieCollection();
      ResponseCookies = new HttpCookieCollection();
      RequestHeaders = new NameValueCollection();
      RequestPayload = new NameValueCollection();
      Session = new AuroraFakeSessionStore();
      Application = new AuroraFakeSessionStore();
      Files = new HttpPostedFileBase[0];
    }

    public AuroraContext()
    {
      Init();
    }

    public AuroraContext(HttpContextBase ctx)
      : this()
    {
      if (ctx != null)
      {
        #region WRAP THE HTTP CONTEXT
        RawContext = ctx;

        Cache = ctx.Cache;

        if (ctx.Request.ClientCertificate != null)
        {
          RequestClientCertificate = new X509Certificate2(ctx.Request.ClientCertificate.Certificate);
        }

        Application = new AuroraApplicationState(ctx.Application);
        Session = new AuroraSession(ctx.Session);

        ApplicationPhysicalPath = ctx.Request.PhysicalApplicationPath;
        Path = ctx.Request.Path;

        RequestCookies = ctx.Request.Cookies;
        ResponseCookies = ctx.Response.Cookies;

        RequestType = ctx.Request.RequestType;

        RequestHeaders = new NameValueCollection(ctx.Request.Headers);

        IsSecureConnection = ctx.Request.IsSecureConnection;

        for (int i = 0; i < ctx.Application.Count; i++)
        {
          Application[ctx.Application.Keys[i]] = ctx.Application.Contents[i];
        }

        for (int i = 0; i < ctx.Session.Count; i++)
        {
          Session[ctx.Session.Keys[i]] = ctx.Session.Contents[i];
        }

        ResponseFilter = ctx.Response.Filter;

        if (ctx.Request.Files.Count > 0)
        {
          Files = new HttpPostedFileBase[ctx.Request.Files.Count];

          ctx.Request.Files.CopyTo(Files, 0);
        }

        ValidationUtility.EnableDynamicValidation(ctx.ApplicationInstance.Context);

        #region FORM / QUERYSTRING
        Func<NameValueCollection> _unvalidatedForm;
        Func<NameValueCollection> _unvalidatedQueryString;

        ValidationUtility.GetUnvalidatedCollections(ctx.ApplicationInstance.Context, out _unvalidatedForm, out _unvalidatedQueryString);

        Form = _unvalidatedForm();
        //QueryString = _unvalidatedQueryString();
        #endregion

        QueryString = new NameValueCollection(ctx.Request.QueryString);

        RequestPayload = new NameValueCollection();

        string payload = new StreamReader(RawContext.Request.InputStream).ReadToEnd();

        if (!string.IsNullOrEmpty(payload))
        {
          foreach (string[] arr in payload.Split(',').Select(x => x.Split('=')))
          {
            RequestPayload.Add(arr[0], arr[1]);
          }
        }
        #endregion
      }
    }

    public string MapPath(string path)
    {
      if (RawContext != null)
      {
        return RawContext.Server.MapPath(path);
      }

      return path;
    }

    public void ValidateInput()
    {
      if (RawContext != null)
      {
        RawContext.Request.ValidateInput();
        string x = RawContext.Request.RawUrl;
      }
    }

    public void RewritePath(string path)
    {
      if (RawContext != null)
      {
        RawContext.RewritePath(path);
      }
    }

    public void ResponseRedirect(string url)
    {
      if (RawContext != null)
      {
        RawContext.Response.Redirect(url);
      }
    }

    public void ResponseWrite(string text)
    {
      if (RawContext != null)
      {
        RawContext.Response.Write(text);
      }
    }

    public void ResponseBinaryWrite(byte[] buffer)
    {
      if (RawContext != null)
      {
        RawContext.Response.BinaryWrite(buffer);
      }
    }

    public void ResponseTransmitFile(string fileName)
    {
      if (RawContext != null)
      {
        RawContext.Response.TransmitFile(fileName);
      }
    }

    public void ResponseAddHeader(string name, string value)
    {
      if (RawContext != null)
      {
        RawContext.Response.AddHeader(name, value);
      }
    }

    public void ResponseAppendHeader(string name, string value)
    {
      if (RawContext != null)
      {
        RawContext.Response.AppendHeader(name, value);
      }
    }

    public void ResponseClearContent()
    {
      if (RawContext != null)
      {
        RawContext.Response.ClearContent();
      }
    }

    public void ResponseClearHeaders()
    {
      if (RawContext != null)
      {
        RawContext.Response.ClearHeaders();
      }
    }

    public void ResponseSetCachePolicy(AuroraCachePolicy policy)
    {
      if (RawContext != null)
      {
        RawContext.Response.Cache.SetCacheability(policy.CacheabilityOption);
        RawContext.Response.Cache.SetExpires(policy.Expires);
        RawContext.Response.Cache.SetMaxAge(policy.MaxAge);
        RawContext.Response.Cache.SetValidUntilExpires(policy.ValidUntilExpires);
        RawContext.Response.Cache.VaryByParams.IgnoreParams = policy.IgnoreParams;
      }
    }

    public void ResponseSetNoCache()
    {
      if (RawContext != null)
      {
        RawContext.Response.Cache.SetCacheability(HttpCacheability.NoCache);
        RawContext.Response.Cache.SetNoStore();
        RawContext.Response.Cache.SetExpires(DateTime.MinValue);
      }
    }

    public void ResponseStatusCode(HttpStatusCode statusCode)
    {
      if (RawContext != null)
      {
        RawContext.Response.StatusCode = (int)statusCode;
      }
    }

    public void ServerClearError()
    {
      if (RawContext != null)
      {
        RawContext.Server.ClearError();
      }
    }

    public string GetValidatedFormValue(string key)
    {
      if (RawContext != null)
      {
        return RawContext.Request.Form[key];
      }

      return null;
    }

    //public string GetValidatedQueryStringValue(string key)
    //{
    //  if (RawContext != null)
    //  {
    //    return RawContext.Request.QueryString[key];
    //  }
    //
    //  return null;
    //}
  }
  #endregion

  #region AURORA ENGINE
  public sealed class AuroraEngine
  {
    private IRouteEngine routeEngine;
    private IViewEngine viewEngine;
    private IAuroraContext context;
    private BundleManager bundleManager;
    private FrontController frontController;

    public AuroraEngine(IAuroraContext ctx)
    {
      ctx.ThrowIfArgumentNull();

      context = ctx;

      frontController = ApplicationInternals.GetFrontController(ctx);
      bundleManager = new BundleManager(ctx);

      if (!MainConfig.SupportedHttpVerbs.Contains(ctx.RequestType))
      {
        throw new Exception(string.Format(CultureInfo.CurrentCulture, MainConfig.HttpRequestTypeNotSupportedError, ctx.RequestType));
      }

      string incomingPath = context.Path;
      string path = string.Empty;

      if (string.Equals(incomingPath, "/", StringComparison.Ordinal) ||
          string.Equals(incomingPath, "/default.aspx", StringComparison.OrdinalIgnoreCase) ||
          incomingPath == "~/")
      {
        path = MainConfig.DefaultRoute;
      }
      else
      {
        path = (context.Path.EndsWith("/", StringComparison.Ordinal)) ?
          context.Path.Remove(context.Path.Length - 1) : context.Path;
      }

      context.RewritePath(path);

      ProcessRequest(path);
    }

    private void ProcessRequest(string path)
    {
      HttpCookie authCookie = context.RequestCookies[MainConfig.AuroraAuthCookieName];
      RouteInfo routeInfo = null;

      if (authCookie != null)
      {
        if (context.Session[MainConfig.CurrentUserSessionName] != null)
        {
          context.User = context.Session[MainConfig.CurrentUserSessionName] as User;
        }
      }

      routeEngine = ApplicationInternals.GetRouteEngine(context);

      try
      {
        IViewResult result = null;

        if (IsRequestStatic(path))
        {
          result = ProcessStaticRequest(path);
        }
        else
        {
          viewEngine = ApplicationInternals.GetViewEngine(context);

          routeInfo = routeEngine.FindRoute(path);

          // Front Controller Missing Route route event
          if (routeInfo == null && frontController != null)
          {
            routeInfo = frontController.RaiseEvent(RouteHandlerEventType.MissingRoute, path, null);
          }

          if (routeInfo != null)
          {
            context.RouteInfo = routeInfo;

            result = ProcessDynamicRequest(path, routeInfo);
          }
        }

        if (result == null)
        {
          throw new Exception(MainConfig.Http404Error);
        }

        result.Render();
      }
      catch (ThreadAbortException)
      {
        context.ServerClearError();
      }
      catch (Exception ex)
      {
        context.ServerClearError();

        // Front Controller Missing Route route event
        if (frontController != null)
        {
          frontController.RaiseEvent(RouteHandlerEventType.Error, path, routeInfo, ex);
        }

        if (MainConfig.CustomErrorsSection.Mode == CustomErrorsMode.On)
        {
          // Check to see if there is a derived CustomError class otherwise look to see if there is a cusom error method on a controller
          CustomError customError = ApplicationInternals.GetCustomError(context, false);

          customError.OnError(ex.Message, ex).Render();
        }
        else
        {
          CustomError customError = ApplicationInternals.GetCustomError(context, true);
          customError.OnError(ex.Message, ex).Render();
        }
      }
    }

    private bool IsRequestStatic(string path)
    {
      if (MainConfig.PathStaticFileRE.IsMatch(path))
      {
        // Front Controller Static route event
        if (frontController != null)
        {
          frontController.RaiseEvent(RouteHandlerEventType.Static, path, null);
        }

        return true;
      }

      return false;
    }

    private IViewResult ProcessStaticRequest(string path)
    {
      IViewResult iar = null;

      if (path.StartsWith("/" + MainConfig.PublicResourcesFolderName, StringComparison.Ordinal)
          || path.EndsWith(".ico", StringComparison.Ordinal))
      {
        if (MainConfig.PathStaticFileRE.IsMatch(path))
        {
          string staticFilePath = context.MapPath(path);

          if (File.Exists(staticFilePath) || bundleManager.Contains(Path.GetFileName(path)))
          {
            string protectedRoles = null;

            if (StaticFileManager.IsProtected(context, staticFilePath, out protectedRoles))
            {
              if (!SecurityManager.IsAuthenticated(context, protectedRoles))
              {
                return iar;
              }
            }

            return new PhysicalFileResult(context, staticFilePath);
          }
        }
      }

      return iar;
    }

    private IViewResult ProcessDynamicRequest(string path, RouteInfo routeInfo)
    {
      IViewResult result = null;

      if (routeInfo != null && !routeInfo.IsFiltered)
      {
        if (frontController != null)
        {
          // Front Controller PostRoute determination event
          frontController.RaiseEvent(RouteHandlerEventType.PostRoute, path, routeInfo);
          // Front Controller PreAction event
          frontController.RaiseEvent(RouteHandlerEventType.Pre, path, routeInfo);
        }

        // Execute Controller BeforeAction event
        routeInfo.ControllerInstance.RaiseEvent(RouteHandlerEventType.Pre, path, routeInfo);

        #region DO THE HEAVY LIFTING
        RequestTypeAttribute reqAttrib = Attribute.GetCustomAttribute(routeInfo.Action, typeof(RequestTypeAttribute), false) as RequestTypeAttribute;
        HttpGetAttribute get = routeInfo.Attribute as HttpGetAttribute;

        routeInfo.ControllerInstance.CurrentRoute = routeInfo;

        #region SECURITY CHECKING
        if (reqAttrib.HttpsOnly && !context.IsSecureConnection)
        {
          return result;
        }

        if (reqAttrib.SecurityType == ActionSecurity.Secure)
        {
          if (!SecurityManager.IsAuthenticated(context, reqAttrib.Roles))
          {
            if (frontController != null)
            {
              frontController.RaiseEvent(RouteHandlerEventType.FailedSecurity, path, routeInfo);
            }

            if (!string.IsNullOrEmpty(reqAttrib.RedirectWithoutAuthorizationTo))
            {
              return new RedirectResult(context, reqAttrib.RedirectWithoutAuthorizationTo);
            }
            else
            {
              throw new Exception(MainConfig.RedirectWithoutAuthorizationToError);
            }
          }

          if (frontController != null)
          {
            frontController.RaiseEvent(RouteHandlerEventType.PassedSecurity, path, routeInfo);
          }
        }
        #endregion

        if (get != null)
        {
          #region AJAX GET REQUEST WITH JSON RESULT
          if (routeInfo.Action.ReturnType == typeof(JsonResult))
          {
            if (context.QueryString[MainConfig.AntiForgeryTokenName] != null)
            {
              if (!AntiForgeryToken.VerifyToken(context))
              {
                throw new AntiForgeryTokenMissingException(MainConfig.JsonAntiForgeryTokenMissing);
              }
            }
          }
          #endregion

          #region HTTP GET CACHE BYPASS
          if (get.Cache)
          {
            // if we have a cached view result for this request we will return it and skip invocation of the action
            if (context.Cache[path] != null)
            {
              if (frontController != null)
              {
                frontController.RaiseEvent(RouteHandlerEventType.CachedViewResult, path, routeInfo);
              }

              CachedViewResult vr = (context.Cache[path] as CachedViewResult);

              vr.ViewResult.Refresh(context, get);

              return vr.ViewResult;
            }
          }
          #endregion
        }

        #region ANTI FORGERY TOKEN VERIFICATION
        if ((context.RequestType == "POST" && (routeInfo.Attribute as HttpPostAttribute).RequireAntiForgeryToken ||
            context.RequestType == "PUT" && (routeInfo.Attribute as HttpPutAttribute).RequireAntiForgeryToken ||
            context.RequestType == "DELETE" && (routeInfo.Attribute as HttpDeleteAttribute).RequireAntiForgeryToken))
        {
          string antiForgeryToken = string.Empty;

          NameValueCollection nameValueCollection = (routeInfo.Payload == null) ? context.Form : routeInfo.Payload;

          KeyValuePair<string, string>? kvp = nameValueCollection.GetKeyValuePair(MainConfig.AntiForgeryTokenName);

          if (kvp != null)
          {
            antiForgeryToken = kvp.Value.Value;
          }

          if (!string.IsNullOrEmpty(antiForgeryToken))
          {
            if (!AntiForgeryToken.VerifyToken(context, antiForgeryToken))
            {
              throw new Exception(MainConfig.AntiForgeryTokenVerificationFailedError);
            }

            AntiForgeryToken.RemoveToken(context, antiForgeryToken);
          }
          else
          {
            throw new Exception(MainConfig.AntiForgeryTokenMissingError);
          }
        }
        #endregion

        #region REFINE ACTION PARAMS
        routeInfo.ActionParameters = DetermineAndProcessSanitizeAttributes(routeInfo);
        routeInfo.ActionParameterTransforms = DetermineActionParameterTransforms(routeInfo);

        if (routeInfo.ActionParameterTransforms != null && routeInfo.ActionParameterTransforms.Count > 0)
        {
          routeInfo.ActionParameters = ProcessActionParameterTransforms(routeInfo, routeInfo.ActionParameterTransforms);
        }
        #endregion

        result = InvokeAction(routeInfo);

        #region VIEW CACHING
        if (get != null && get.Cache)
        {
          context.Cache.Add(
              path,
              new CachedViewResult() { ViewResult = result as ViewResult },
              null,
              get.DateExpiry,
              System.Web.Caching.Cache.NoSlidingExpiration,
              CacheItemPriority.Normal,
              null);
        }
        #endregion

        #endregion

        // Front Controller PostAction event
        if (frontController != null)
        {
          frontController.RaiseEvent(RouteHandlerEventType.Post, path, routeInfo);
        }

        // Execute Controller AfterAction
        routeInfo.ControllerInstance.RaiseEvent(RouteHandlerEventType.Post, path, routeInfo);
      }

      return result;
    }

    private IViewResult InvokeAction(RouteInfo routeInfo)
    {
      if (routeInfo != null)
      {
        IViewResult result = null;

        if (routeInfo.Bindings != null)
        {
          foreach (object i in routeInfo.Bindings)
          {
            if (i.GetType().GetInterface(typeof(IBindToAction).Name) != null)
            {
              MethodInfo boundActionObject = i.GetType().GetMethod("ExecuteBeforeAction");

              if (boundActionObject != null)
              {
                boundActionObject.Invoke(i, new object[] { context });
              }
            }
          }
        }

        if (!routeInfo.IsFiltered)
        {
          //if (routeInfo.Dynamic && context.Session[MainConfig.FromRedirectOnlySessionFlag] == null)
          if (routeInfo.Attribute is FromRedirectOnlyAttribute && context.Session[MainConfig.FromRedirectOnlySessionFlag] == null)
          {
            return null;
          }

          #region CHECK TO SEE IF WE HAVE ACTION FILTERS AND MODIFY THE ACTION PARAMETERS ACCORDINGLY
          List<ActionFilterAttribute> filters = routeInfo.Action.GetCustomAttributes(false).Where(x => x.GetType().BaseType == typeof(ActionFilterAttribute)).Cast<ActionFilterAttribute>().ToList();

          if (filters.Count() > 0)
          {
            List<IActionFilterResult> filterResults = new List<IActionFilterResult>();

            // Look for ActionFilter attributes then execute their OnFilter method
            foreach (ActionFilterAttribute af in filters)
            {
              af.Controller = routeInfo.ControllerInstance;
              af.OnFilter(routeInfo);

              if (af.FilterResult != null)
              {
                filterResults.Add(af.FilterResult);
              }
            }

            if (filterResults.Count() > 0)
            {
              var filteredMethodParams = routeInfo.Action.GetParameters().Where(x => x.GetType().GetInterfaces().FirstOrDefault(i => i.UnderlyingSystemType == typeof(IActionFilterResult)) != null);

              if (filteredMethodParams != null)
              {
                routeInfo.ActionParameters = filterResults.ToArray().Concat(routeInfo.ActionParameters).ToArray();
              }
            }
          }
          #endregion

          object _result = routeInfo.Action.Invoke(routeInfo.ControllerInstance, routeInfo.ActionParameters);

          if (context.Session[MainConfig.FromRedirectOnlySessionFlag] != null)
          {
            context.Session.Remove(MainConfig.FromRedirectOnlySessionFlag);
          }

          result = (routeInfo.Action.ReturnType.GetInterfaces().Contains(typeof(IViewResult))) ? (IViewResult)_result : new VoidResult();
        }

        return result;
      }

      return null;
    }

    private static object[] DetermineAndProcessSanitizeAttributes(RouteInfo routeInfo)
    {
      ParameterInfo[] actionParms = routeInfo.Action.GetParameters();

      object[] processedParams = new object[routeInfo.ActionParameters.Length];

      routeInfo.ActionParameters.CopyTo(processedParams, 0);

      for (int i = 0; i < actionParms.Length; i++)
      {
        if (actionParms[i].ParameterType == typeof(string))
        {
          SanitizeAttribute sanitize = (SanitizeAttribute)actionParms[i].GetCustomAttributes(false).FirstOrDefault(x => x is SanitizeAttribute);

          if (sanitize != null)
          {
            sanitize.Sanitize(processedParams[i].ToString());
          }
        }
      }

      return processedParams;
    }

    private static List<ActionParamTransformInfo> DetermineActionParameterTransforms(RouteInfo routeInfo)
    {
      Type[] actionParameterTypes = routeInfo.ActionParameters.Select(x => (x != null) ? x.GetType() : null).ToArray();

      List<ActionParamTransformInfo> actionParameterTransforms = new List<ActionParamTransformInfo>();

      for (int i = 0; i < routeInfo.Action.GetParameters().Count(); i++)
      {
        ParameterInfo pi = routeInfo.Action.GetParameters()[i];

        ActionParamTransformAttribute apt = (ActionParamTransformAttribute)pi.GetCustomAttributes(typeof(ActionParamTransformAttribute), false).FirstOrDefault();

        if (apt != null)
        {
          // The ActionParamTransform name corresponds to a class that implements the IActionParamTransform interface

          if (routeInfo.ActionParameterTransforms == null)
          {
            routeInfo.ActionParameterTransforms = new List<ActionParamTransformInfo>();
          }

          // Look up class
          Type actionParamTransformClassType = ApplicationInternals.GetActionTransformClassType(apt);

          if (actionParamTransformClassType != null)
          {
            // Call Transform method, take results and use that instead of the incoming param
            MethodInfo transformMethod = actionParamTransformClassType.GetMethod("Transform");

            if (transformMethod != null)
            {
              Type transformedParamType = transformMethod.ReturnType;

              actionParameterTypes[i] = transformedParamType;

              actionParameterTransforms.Add(new ActionParamTransformInfo()
              {
                TransformClassType = actionParamTransformClassType,
                TransformMethod = transformMethod,
                IndexIntoParamList = i
              });
            }
          }
        }
      }

      if (actionParameterTransforms.Count() > 0)
      {
        return actionParameterTransforms;
      }

      return null;
    }

    private static object[] ProcessActionParameterTransforms(RouteInfo routeInfo, List<ActionParamTransformInfo> actionParamTransformInfos)
    {
      if (actionParamTransformInfos != null)
      {
        object[] processedParams = new object[routeInfo.ActionParameters.Length];

        routeInfo.ActionParameters.CopyTo(processedParams, 0);

        foreach (ActionParamTransformInfo apti in actionParamTransformInfos)
        {
          // Instantiate the class, the constructor will receive any bound action objects that the params method received.
          object actionParamTransformClassInstance = Activator.CreateInstance(apti.TransformClassType, routeInfo.Bindings.ToArray());

          Type transformMethodParameterType = apti.TransformMethod.GetParameters()[0].ParameterType;
          Type incomingParameterType = processedParams[apti.IndexIntoParamList].GetType();

          if (transformMethodParameterType != incomingParameterType)
          {
            processedParams[apti.IndexIntoParamList] = processedParams[apti.IndexIntoParamList].ToString();
          }

          object transformedParam = apti.TransformMethod.Invoke(actionParamTransformClassInstance, new object[] { processedParams[apti.IndexIntoParamList] });

          if (transformedParam != null)
          {
            processedParams[apti.IndexIntoParamList] = transformedParam;
          }
        }

        return processedParams;
      }

      return null;
    }
  }
  #endregion

  #region AURORA ROUTE ENGINE
  internal interface IRouteEngine
  {
    void Refresh(IAuroraContext ctx);
    RouteInfo FindRoute(string path);
  }

  public class RouteInfo
  {
    public IAuroraContext Context { get; internal set; }
    public List<object> Bindings { get; internal set; }
    public string RequestType { get; internal set; }
    public string Alias { get; internal set; }
    public NameValueCollection Payload { get; internal set; }

    #region INTERNAL PROPS
    internal Type ControllerType { get; set; }
    internal Controller ControllerInstance { get; set; }
    internal MethodInfo Action { get; set; }
    internal string ControllerName { get; set; }
    internal string ActionName { get; set; }
    internal string FrontLoadedParams { get; set; }
    internal object[] ActionParameters { get; set; }
    internal bool IsFiltered { get; set; }
    internal bool Dynamic { get; set; }
    internal bool FromRedirectOnlyInfo { get; set; }
    internal List<ActionParamTransformInfo> ActionParameterTransforms { get; set; }
    internal RequestTypeAttribute Attribute { get; set; }
    internal List<string> SkipValidationParameterNames { get; set; }
    #endregion
  }

  internal class PostedFormInfo
  {
    private IAuroraContext context;

    public Type DataType { get; private set; }
    public object DataTypeInstance { get; private set; }

    private void FormProcessor(Type m)
    {
      if (m != null)
      {
        PropertyInfo[] props = m.GetProperties();

        if (props.Count() == 0)
        {
          throw new Exception(MainConfig.PostedFormActionIncorrectNumberOfParametersError);
        }

        DataType = m;
        DataTypeInstance = Activator.CreateInstance(DataType);

        foreach (PropertyInfo p in Model.GetPropertiesWithExclusions<Model>(m, true))
        {
          // We need to convert the form value to a datatype 
          SkipValidationAttribute skipValidationAttrib = (SkipValidationAttribute)p.GetCustomAttributes(false).FirstOrDefault(x => x.GetType() == typeof(SkipValidationAttribute));

          string propertyValue = context.Form[p.Name];

          if (skipValidationAttrib == null)
          {
            propertyValue = context.GetValidatedFormValue(p.Name);
          }

          if (p.PropertyType == typeof(int) ||
              p.PropertyType == typeof(int?))
          {
            if (propertyValue.IsInt32())
            {
              p.SetValue(DataTypeInstance, Convert.ToInt32(propertyValue, CultureInfo.InvariantCulture), null);
            }
          }
          else if (p.PropertyType == typeof(string))
          {
            SanitizeAttribute sanitize = (SanitizeAttribute)p.GetCustomAttributes(false).FirstOrDefault(x => x is SanitizeAttribute);

            if (sanitize != null)
            {
              sanitize.Sanitize(propertyValue);
            }

            p.SetValue(DataTypeInstance, propertyValue, null);
          }
          else if (p.PropertyType == typeof(bool))
          {
            if (propertyValue.IsBool())
            {
              p.SetValue(DataTypeInstance, Convert.ToBoolean(propertyValue, CultureInfo.InvariantCulture), null);
            }
          }
          else if (p.PropertyType == typeof(DateTime?))
          {
            DateTime? dt = null;

            propertyValue.IsDate(out dt);

            p.SetValue(DataTypeInstance, dt, null);
          }
          else if (p.PropertyType == typeof(HttpPostedFile))
          {
            if (context.Files.Length > 0)
            {
              p.SetValue(DataTypeInstance, context.Files[0], null);
            }
          }
          else if (p.PropertyType == typeof(List<HttpPostedFileBase>))
          {
            List<HttpPostedFileBase> fileList = (List<HttpPostedFileBase>)Activator.CreateInstance(p.PropertyType);

            foreach (HttpPostedFileBase f in context.Files)
            {
              fileList.Add(f);
            }

            p.SetValue(DataTypeInstance, fileList, null);
          }
        }
      }
    }

    public PostedFormInfo(IAuroraContext ctx, Type m)
    {
      ctx.ThrowIfArgumentNull();

      context = ctx;

      FormProcessor(m);
    }
  }

  internal class AuroraRouteEngine : IRouteEngine
  {
    private IAuroraContext context;

    public AuroraRouteEngine(IAuroraContext ctx)
    {
      ctx.ThrowIfArgumentNull();

      context = ctx;
    }

    public void Refresh(IAuroraContext ctx)
    {
      ctx.ThrowIfArgumentNull();

      context = ctx;
    }

    #region PARAM GETTERS
    private static object[] GetFrontParams(RouteInfo routeInfo)
    {
      return !string.IsNullOrEmpty(routeInfo.FrontLoadedParams) ?
        routeInfo.FrontLoadedParams.Split('/').ToObjectArray() :
        new object[] { };
    }

    private static object[] GetBoundParams(RouteInfo routeInfo)
    {
      return (routeInfo.Bindings != null) ? routeInfo.Bindings.ToArray() : new object[] { };
    }

    private object[] GetURLParams(string path, string alias)
    {
      string[] urlStringParams = path.Replace(alias, string.Empty).Split('/').Where(x => !string.IsNullOrEmpty(x)).Select(x => HttpUtility.UrlEncode(x)).ToArray();

      if (urlStringParams != null)
      {
        return urlStringParams.ToObjectArray();
      }

      return new object[] { };
    }

    private object[] GetFormParams(RouteInfo routeInfo)
    {
      if (context.RequestType == "POST")
      {
        if (context.Form.Count > 0)
        {
          PostedFormInfo postedFormInfo;
          Type postedFormModel = Model.DetermineModelFromPostedForm(context);

          if (postedFormModel != null)
          {
            #region POSTED FORM MODEL
            postedFormInfo = new PostedFormInfo(context, postedFormModel);

            if (postedFormInfo.DataType != null)
            {
              ((Model)postedFormInfo.DataTypeInstance).Validate(context, (Model)postedFormInfo.DataTypeInstance);

              return new object[] { postedFormInfo.DataTypeInstance };
            }
            #endregion
          }
          else
          {
            NameValueCollection form = new NameValueCollection();

            foreach (string key in context.Form.Keys)
            {
              if (key.StartsWith(MainConfig.AntiForgeryTokenName))
              {
                continue;
              }

              if (routeInfo.SkipValidationParameterNames.Contains(key))
              {
                form[key] = context.Form[key];
              }
              else
              {
                form[key] = context.GetValidatedFormValue(key);
              }
            }

            //FIXME: posted forms that put their variables directly in the action parameter list
            //       do not behave the same way as posted forms using post models. Post models
            //       convert the form value to the data type of the corresponding property, where as
            //       the former converts the form values to whatever they appear to be, strings are 
            //       strings, numbers are int's, etc...

            string[] formValues = new string[form.Count];

            form.CopyTo(formValues, 0);

            return formValues.ToObjectArray();
          }
        }
      }

      return new object[] { };
    }

    private object[] GetPutDeleteParams(RouteInfo routeInfo)
    {
      if (routeInfo.RequestType == "PUT" || routeInfo.RequestType == "DELETE")
      {
        routeInfo.Payload = context.RequestPayload;
      }

      if (routeInfo.Payload != null && routeInfo.Payload.Count > 0)
      {
        return new object[] { routeInfo.Payload };
      }

      return new object[] { };
    }

    private object[] GetFileParams()
    {
      if (context.Files != null && context.Files.Length > 0)
      {
        return context.Files;
      }

      return new object[] { };
    }
    #endregion

    /// <summary>
    /// Finds an 'Aurora Route' based on an incoming path
    /// <remarks>
    /// Actions parameters map in the following way to route parameters.
    /// 
    /// ActionName(action_filter_results, bound_parameters, front_params, url_parameters, form_parameters or (HTTP Put/Delete) payload, files)
    /// </remarks>
    /// </summary>
    /// <param name="path"></param>
    /// <returns></returns>
    public RouteInfo FindRoute(string path)
    {
      //NOTE: This method is still not where I want it. There is a lot going on in here that needs to be made a little more sane.

      bool fromRedirectOnlyFlag = ApplicationInternals.GetFromRedirectOnlyFlag(context);

      List<RouteInfo> routeInfos = ApplicationInternals.AllRouteInfos(context);

      if (routeInfos == null)
      {
        return null;
      }

      string alias = routeInfos.OrderByDescending(y => y.Alias)
                               .Where(x => path.StartsWith(x.Alias, StringComparison.Ordinal))
                               .Select(x => x.Alias).FirstOrDefault();

      if (!MainConfig.PathTokenRE.IsMatch(path))
      {
        return null;
      }

      List<RouteInfo> routeSlice = routeInfos.Where(x => x.Alias == alias && x.RequestType == context.RequestType).ToList();

      if (routeSlice.Count() == 0)
      {
        return null;
      }

      object[] urlParams = GetURLParams(path, alias);
      object[] fileParams = GetFileParams();

      foreach (RouteInfo routeInfo in routeSlice)
      {
        object[] formParams = GetFormParams(routeInfo);
        object[] boundParams = GetBoundParams(routeInfo);
        object[] frontParams = GetFrontParams(routeInfo);
        object[] putDeleteParams = GetPutDeleteParams(routeInfo);

        List<object> actionParametersList = new List<object>();

        actionParametersList.AddRange(boundParams);
        actionParametersList.AddRange(frontParams);
        actionParametersList.AddRange(urlParams);
        actionParametersList.AddRange(formParams);
        actionParametersList.AddRange(putDeleteParams);
        actionParametersList.AddRange(fileParams);

        object[] actionParameters = actionParametersList.ToArray();

        if ((fromRedirectOnlyFlag && !routeInfo.FromRedirectOnlyInfo) ||
            (actionParameters.Count() < routeInfo.Action.GetParameters().Count()))
        {
          continue;
        }

        ParameterInfo[] methodParameterInfos = routeInfo.Action.GetParameters();

        Type[] actionParameterTypes = actionParameters.Select(x => (x != null) ? x.GetType() : null).ToArray();
        Type[] methodParamTypes = methodParameterInfos.Select(x => x.ParameterType).ToArray();

        if (methodParamTypes.Count() != actionParameters.Count())
        {
          continue;
        }

        var filteredMethodParams = methodParamTypes.Where(x => x.GetInterfaces().FirstOrDefault(i => i.UnderlyingSystemType == typeof(IActionFilterResult)) != null);

        if (filteredMethodParams != null && filteredMethodParams.Count() > 0)
        {
          methodParamTypes = methodParamTypes.Except(filteredMethodParams).ToArray();
        }

        List<Type> matches = new List<Type>();

        for (int x = 0; x < methodParamTypes.Length; x++)
        {
          if ((methodParamTypes[x].FullName == actionParameterTypes[x].FullName) ||
              (actionParameterTypes[x].GetInterface(methodParamTypes[x].FullName) != null) ||
              (methodParameterInfos[x].GetCustomAttributes(typeof(ActionParamTransformAttribute), false).FirstOrDefault() != null))
          {
            matches.Add(actionParameterTypes[x]);
          }
        }

        if (matches.Count() == methodParamTypes.Count())
        {
          routeInfo.ActionParameters = actionParameters;

          return routeInfo;
        }
      }

      return null;
    }
  }
  #endregion

  #region ACTION BINDER
  public interface IBindToAction
  {
    void ExecuteBeforeAction(AuroraContext ctx);
  }

  internal class ActionBinding
  {
    public List<string> ActionNames { get; private set; }
    public List<object> BindInstances { get; private set; }

    public ActionBinding(string actionName, object bindInstance)
    {
      ActionNames = new List<string>();
      BindInstances = new List<object>();

      Add(actionName, bindInstance);
    }

    public void Add(string actionName, object bindInstance)
    {
      if (ActionNames.FirstOrDefault(x => x == actionName) == null &&
          BindInstances.FirstOrDefault(x => x == bindInstance) == null)
      {
        ActionNames.Add(actionName);
        BindInstances.Add(bindInstance);
      }
    }

    public void AddAction(string actionName)
    {
      if (ActionNames.FirstOrDefault(x => x == actionName) == null)
      {
        ActionNames.Add(actionName);
      }
    }

    public void AddBindInstance(object bindInstance)
    {
      if (BindInstances.FirstOrDefault(x => x == bindInstance) == null)
      {
        BindInstances.Add(bindInstance);
      }
    }

    public void RemoveBindInstance(string actionName, object bindInstance)
    {
      if (BindInstances.FirstOrDefault(x => x == bindInstance) != null)
      {
        ActionNames.Remove(actionName);
        BindInstances.Remove(bindInstance);
      }
    }
  }

  public class ActionBinder
  {
    private IAuroraContext context;
    internal Dictionary<string, List<ActionBinding>> bindings { get; private set; }

    public ActionBinder(IAuroraContext ctx)
    {
      ctx.ThrowIfArgumentNull();

      context = ctx;

      bindings = new Dictionary<string, List<ActionBinding>>();

      if (ctx.Application[MainConfig.ActionBinderSessionName] != null)
      {
        bindings = ctx.Application[MainConfig.ActionBinderSessionName] as Dictionary<string, List<ActionBinding>>;
      }
      else
      {
        bindings = new Dictionary<string, List<ActionBinding>>();

        ctx.Application[MainConfig.ActionBinderSessionName] = bindings;
      }
    }

    public void BindForAllActions(string controllerName, object bindInstance)
    {
      if (string.IsNullOrEmpty(controllerName))
      {
        throw new ArgumentException(MainConfig.ActionBinderNullOrEmptyControllerNameError);
      }

      bindInstance.ThrowIfArgumentNull();

      BindForAllActions(controllerName, new object[] { bindInstance });
    }

    public void BindForAllActions(string controllerName, object[] bindInstances)
    {
      if (string.IsNullOrEmpty(controllerName))
      {
        throw new ArgumentException(MainConfig.ActionBinderNullOrEmptyControllerNameError);
      }

      bindInstances.ThrowIfArgumentNull();

      if (bindInstances.Count() == 0)
      {
        throw new ArgumentException(MainConfig.ActionBinderBindingInstanceNullOrEmptyError);
      }

      foreach (string actionName in ApplicationInternals.AllRoutableActionNames(context, controllerName))
      {
        foreach (object o in bindInstances)
        {
          Bind(controllerName, actionName, o);
        }
      }
    }

    public void BindForAllControllers(object bindInstance)
    {
      bindInstance.ThrowIfArgumentNull();

      BindForAllControllers(new object[] { bindInstance });
    }

    public void BindForAllControllers(object[] bindInstances)
    {
      bindInstances.ThrowIfArgumentNull();

      if (bindInstances.Count() == 0)
      {
        throw new ArgumentException(MainConfig.ActionBinderBindingInstanceNullOrEmptyError);
      }

      foreach (Type controller in ApplicationInternals.AllControllers(context))
      {
        foreach (string actionName in ApplicationInternals.AllRoutableActionNames(context, controller.Name))
        {
          foreach (object o in bindInstances)
          {
            Bind(controller.Name, actionName, o);
          }
        }
      }
    }

    public void Bind(string controllerName, string[] actionNames, object bindInstance)
    {
      if (string.IsNullOrEmpty(controllerName))
      {
        throw new ArgumentException(MainConfig.ActionBinderNullOrEmptyControllerNameError);
      }

      actionNames.ThrowIfArgumentNull();
      bindInstance.ThrowIfArgumentNull();

      if (actionNames.Count() == 0)
      {
        throw new ArgumentException(MainConfig.ActionBinderActionNameNullorEmptyError);
      }

      foreach (string a in actionNames)
      {
        Bind(controllerName, a, bindInstance);
      }
    }

    public void Bind(string controllerName, string[] actionNames, object[] bindInstances)
    {
      if (string.IsNullOrEmpty(controllerName))
      {
        throw new ArgumentException(MainConfig.ActionBinderNullOrEmptyControllerNameError);
      }

      actionNames.ThrowIfArgumentNull();
      bindInstances.ThrowIfArgumentNull();

      if (actionNames.Count() == 0)
      {
        throw new ArgumentException(MainConfig.ActionBinderActionNameNullorEmptyError);
      }

      foreach (string a in actionNames)
      {
        foreach (object o in bindInstances)
        {
          Bind(controllerName, a, o);
        }
      }
    }

    public void Bind(string controllerName, string actionName, object[] bindInstances)
    {
      if (string.IsNullOrEmpty(controllerName))
      {
        throw new ArgumentException(MainConfig.ActionBinderNullOrEmptyControllerNameError);
      }

      if (string.IsNullOrEmpty(actionName))
      {
        throw new ArgumentException(MainConfig.ActionBinderActionNameNullorEmptyError);
      }

      bindInstances.ThrowIfArgumentNull();

      foreach (object o in bindInstances)
      {
        Bind(controllerName, actionName, o);
      }
    }

    public void Bind(string controllerName, string actionName, object bindInstance)
    {
      if (string.IsNullOrEmpty(controllerName))
      {
        throw new ArgumentException(MainConfig.ActionBinderNullOrEmptyControllerNameError);
      }

      if (string.IsNullOrEmpty(actionName))
      {
        throw new ArgumentException(MainConfig.ActionBinderActionNameNullorEmptyError);
      }

      bindInstance.ThrowIfArgumentNull();

      List<ActionBinding> binding = (bindings.ContainsKey(controllerName)) ? bindings[controllerName] : null;

      if (binding != null)
      {
        ActionBinding foundByActionName =
          binding.FirstOrDefault(x => x.ActionNames.FirstOrDefault(z => z == actionName) != null);

        ActionBinding foundByActionBinding =
          binding.FirstOrDefault(x => x.BindInstances.FirstOrDefault(y => y == bindInstance) != null);

        if (foundByActionName != null && foundByActionBinding == null)
        {
          foundByActionName.AddBindInstance(bindInstance);
        }
        else if (foundByActionBinding != null && foundByActionName == null)
        {
          foundByActionBinding.AddAction(actionName);
        }
        else
        {
          bindings[controllerName].Add(new ActionBinding(actionName, bindInstance));
        }
      }
      else
      {
        bindings[controllerName] = new List<ActionBinding>();
        bindings[controllerName].Add(new ActionBinding(actionName, bindInstance));
      }
    }

    public void RemoveInstanceFromAction(string controllerName, string actionName, object bindInstance)
    {
      if (string.IsNullOrEmpty(controllerName))
      {
        throw new ArgumentException(MainConfig.ActionBinderNullOrEmptyControllerNameError);
      }

      if (string.IsNullOrEmpty(actionName))
      {
        throw new ArgumentException(MainConfig.ActionBinderActionNameNullorEmptyError);
      }

      bindInstance.ThrowIfArgumentNull();

      List<ActionBinding> binding = bindings[controllerName];

      if (binding != null)
      {
        ActionBinding ab = binding.FirstOrDefault(x => x.ActionNames.FirstOrDefault(y => y == actionName) != null);

        if (ab != null)
        {
          ab.RemoveBindInstance(actionName, binding);
        }
      }
    }

    internal List<object> GetBindings(string controllerName, string actionName)
    {
      if (string.IsNullOrEmpty(controllerName))
      {
        throw new ArgumentException(MainConfig.ActionBinderNullOrEmptyControllerNameError);
      }

      if (string.IsNullOrEmpty(actionName))
      {
        throw new ArgumentException(MainConfig.ActionBinderActionNameNullorEmptyError);
      }

      if (bindings.ContainsKey(controllerName))
      {
        List<ActionBinding> actionBindings = bindings[controllerName];

        if (actionBindings != null)
        {
          ActionBinding ab = actionBindings.FirstOrDefault(x => x.ActionNames.FirstOrDefault(y => y == actionName) != null);

          if (ab != null)
          {
            return ab.BindInstances;
          }
        }
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

    public RouteHandlerEventArgs() { }
  }
  #endregion

  #region FRONT CONTROLLER
  public abstract class FrontController
  {
    protected IAuroraContext Context;
    protected ActionBinder Binder;
    protected BundleManager Bundler;

    private IRouteEngine routeEngine;

    protected virtual void OnInit() { }

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

    internal static FrontController CreateInstance(Type t, IAuroraContext context)
    {
      context.ThrowIfArgumentNull();

      if (t.BaseType == typeof(FrontController))
      {
        FrontController fc = (FrontController)Activator.CreateInstance(t);

        fc.Refresh(context);
        fc.OnInit();

        return fc;
      }

      return null;
    }

    internal void Refresh(IAuroraContext context)
    {
      context.ThrowIfArgumentNull();

      Context = context;
      routeEngine = ApplicationInternals.GetRouteEngine(context);
      Binder = new ActionBinder(context);
      Bundler = new BundleManager(context);
    }

    #region ADD / REMOVE ROUTE / FIND ROUTE
    public RouteInfo FindRoute(string path)
    {
      return routeEngine.FindRoute(path);
    }

    public void AddRoute(string alias, string controllerName, string actionName, string requestType)
    {
      AddRoute(alias, actionName, controllerName, requestType, null);
    }

    public void AddRoute(string alias, string controllerName, string actionName, string requestType, string frontParams)
    {
      Controller controller = ApplicationInternals.AllControllerInstances(Context).FirstOrDefault(x => x.GetType().Name == controllerName);
      MethodInfo actionMethod = GetType().GetMethods().FirstOrDefault(x => x.Name == actionName);

      if (actionMethod != null && controller != null)
      {
        ApplicationInternals.AddRouteInfo(Context, alias, controller, actionMethod, requestType, frontParams);
      }
    }

    public void RemoveRoute(string alias)
    {
      ApplicationInternals.RemoveRouteInfo(Context, alias);
    }

    public List<string> AllRouteAliases()
    {
      List<string> aliases = ApplicationInternals.AllRouteAliases(Context);

      if (aliases != null)
      {
        return aliases;
      }

      return new List<string>();
    }

    public bool IsRouteAliasAvailable(string name)
    {
      if (!AllRouteAliases().Contains(name))
      {
        return true;
      }

      return false;
    }
    #endregion

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
          {
            route = args.RouteInfo;
          }
          break;

        case RouteHandlerEventType.Error:
          ErrorEvent(this, args);
          break;
      }

      return route;
    }
  }
  #endregion

  #region APPLICATION CONTROLLER
  public abstract class Controller
  {
    public IAuroraContext Context { get; internal set; }
    protected ActionBinder Binder;
    internal RouteInfo CurrentRoute { get; set; }
    private string PartitionName;
    private IViewEngine viewEngine;
    protected Dictionary<string, string> ViewTags { get; private set; }
    protected dynamic DViewTags { get; private set; }
    protected Dictionary<string, Dictionary<string, string>> FragTags { get; private set; }
    protected dynamic DFragTags { get; private set; }
    protected NameValueCollection Form { get; private set; }
    protected NameValueCollection QueryString { get; private set; }

    public event EventHandler<RouteHandlerEventArgs> PreActionEvent = (sender, args) => { };
    public event EventHandler<RouteHandlerEventArgs> PostActionEvent = (sender, args) => { };

    protected Controller()
    {
      ViewTags = new Dictionary<string, string>();
      FragTags = new Dictionary<string, Dictionary<string, string>>();
      DFragTags = new DynamicDictionary();
      DViewTags = new DynamicDictionary();

      PartitionName = GetPartitionName();
    }

    protected virtual void OnInit() { }

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

    internal static Controller CreateInstance(Type t, IAuroraContext context)
    {
      context.ThrowIfArgumentNull();

      if (t.BaseType == typeof(Controller))
      {
        Controller controller = (Controller)Activator.CreateInstance(t);

        controller.viewEngine = ApplicationInternals.GetViewEngine(context);

        controller.Refresh(context);
        controller.OnInit();

        return controller;
      }

      return null;
    }

    internal void Refresh(IAuroraContext context)
    {
      context.ThrowIfArgumentNull();

      Context = context;
      Binder = new ActionBinder(context);
      QueryString = (context.QueryString == null) ? new NameValueCollection() : new NameValueCollection(context.QueryString);
      Form = (context.Form == null) ? new NameValueCollection() : new NameValueCollection(context.Form);
      ClearViewTags();

      if (Form.AllKeys.Contains(MainConfig.AntiForgeryTokenName))
      {
        Form.Remove(MainConfig.AntiForgeryTokenName);
      }
    }

    #region MISC
    public void ClearViewTags()
    {
      ViewTags = new Dictionary<string, string>();
      FragTags = new Dictionary<string, Dictionary<string, string>>();
      DFragTags = new DynamicDictionary();
      DViewTags = new DynamicDictionary();
    }

    public string CreateAntiForgeryToken()
    {
      return AntiForgeryToken.Create(Context, AntiForgeryTokenType.Raw);
    }

    private string GetPartitionName()
    {
      string partitionName = null;

      PartitionAttribute partitionAttrib = (PartitionAttribute)this.GetType().GetCustomAttributes(false).FirstOrDefault(x => x.GetType() == typeof(PartitionAttribute));

      if (partitionAttrib != null)
      {
        partitionName = partitionAttrib.Name;
      }

      return partitionName;
    }
    #endregion

    #region ADD / REMOVE ROUTE
    public void AddRoute(string alias, string actionName, string requestType)
    {
      AddRoute(alias, actionName, requestType, null);
    }

    public void AddRoute(string alias, string actionName, string requestType, string frontParams)
    {
      MethodInfo actionMethod = GetType().GetMethods().FirstOrDefault(x => x.Name == actionName);

      if (actionMethod != null)
      {
        ApplicationInternals.AddRouteInfo(Context, alias, this, actionMethod, requestType, frontParams);
      }
    }

    public void RemoveRoute(string alias)
    {
      ApplicationInternals.RemoveRouteInfo(Context, alias);
    }

    public List<string> AllRouteAliases()
    {
      List<string> aliases = ApplicationInternals.AllRouteAliases(Context);

      if (aliases != null)
      {
        return aliases;
      }

      return new List<string>();
    }

    public bool IsRouteAliasAvailable(string name)
    {
      if (!AllRouteAliases().Contains(name))
      {
        return true;
      }

      return false;
    }
    #endregion

    #region REDIRECT
    public void RedirectOnlyToAlias(string alias)
    {
      Context.Session[MainConfig.FromRedirectOnlySessionFlag] = alias;

      RedirectToAlias(alias);
    }

    public void RedirectOnlyToAction(string controller, string action)
    {
      Context.Session[MainConfig.FromRedirectOnlySessionFlag] = string.Format(CultureInfo.InvariantCulture, "/{0}/{1}", controller, action);

      RedirectToAction(controller, action);
    }

    public void RedirectToAlias(string alias)
    {
      Context.ResponseRedirect(alias);
    }

    public void RedirectToAlias(string alias, params string[] parameters)
    {
      Context.ResponseRedirect(string.Format(CultureInfo.InvariantCulture, "{0}/{1}", alias, string.Join("/", parameters)));
    }

    public void RedirectToAction(string action)
    {
      RedirectToAction(GetType().Name, action);
    }

    public void RedirectToAction(string action, params string[] parameters)
    {
      RedirectToAction(GetType().Name, action, parameters);
    }

    public void RedirectToAction(string controller, string action)
    {
      Context.ResponseRedirect(string.Format(CultureInfo.InvariantCulture, "/{0}/{1}", controller, action));
    }

    public void RedirectToAction(string controller, string action, params string[] parameters)
    {
      Context.ResponseRedirect(string.Format(CultureInfo.InvariantCulture, "/{0}/{1}/{2}", controller, action, string.Join("/", parameters)));
    }
    #endregion

    #region RENDER FRAGMENT
    public string RenderFragment(string fragmentName)
    {
      Dictionary<string, string> fragTags = null;

      if (!DFragTags.IsEmpty())
      {
        Dictionary<string, object> _fragTags = DFragTags.GetDynamicDictionary(fragmentName);

        if (_fragTags != null)
        {
          fragTags = _fragTags.ToDictionary(k => k.Key, k => k.Value.ToString());
        }
      }
      else
      {
        if (FragTags.ContainsKey(fragmentName))
        {
          fragTags = FragTags[fragmentName];
        }
      }

      return RenderFragment(fragmentName, fragTags);
    }

    public string RenderFragment(string fragmentName, Dictionary<string, string> fragTags)
    {
      return viewEngine.LoadView(PartitionName, this.GetType().Name, fragmentName, ViewTemplateType.Fragment, fragTags);
    }
    #endregion

    #region VIEW
    public ViewResult View()
    {
      return View(false);
    }

    public ViewResult View(bool clearViewTags)
    {
      return View(CurrentRoute.ControllerName, CurrentRoute.ActionName, clearViewTags);
    }

    public ViewResult View(string name)
    {
      return View(name, false);
    }

    public ViewResult View(string name, bool clearViewTags)
    {
      return View(CurrentRoute.ControllerName, name, clearViewTags);
    }

    public ViewResult View(string controllerName, string actionName, bool clearViewTags)
    {
      RequestTypeAttribute reqAttrib = (RequestTypeAttribute)CurrentRoute.Action.GetCustomAttributes(false).FirstOrDefault(x => x is RequestTypeAttribute);

      Dictionary<string, string> viewTags = ViewTags;

      if (!DViewTags.IsEmpty())
      {
        Dictionary<string, object> _viewTags = DViewTags.GetDynamicDictionary();

        if (_viewTags != null)
        {
          viewTags = _viewTags.ToDictionary(k => k.Key,
            k => (k.Value != null) ? k.Value.ToString() : string.Empty);
        }
      }

      ViewResult vr = new ViewResult(Context, viewEngine, PartitionName, controllerName, actionName, reqAttrib, viewTags);

      if (clearViewTags)
      {
        ClearViewTags();
      }

      return vr;
    }

    public JsonResult View(object jsonData)
    {
      return new JsonResult(Context, jsonData);
    }

    public VirtualFileResult View(string fileName, byte[] fileBytes, string contentType)
    {
      return new VirtualFileResult(Context, fileName, fileBytes, contentType);
    }

    public PartialResult Partial(string partialName)
    {
      return Partial(partialName, false);
    }

    public PartialResult Partial(string partialName, bool clearViewTags)
    {
      if (clearViewTags)
      {
        ClearViewTags();
      }

      return new PartialResult(Context, viewEngine, PartitionName, this.GetType().Name, partialName, ViewTags);
    }
    #endregion
  }
  #endregion

  #endregion

  #region MODEL BASE
  /// <summary>
  /// Represents a posted form model or a view model used by the Html helpers or custom code.
  /// </summary>
  public class Model
  {
    [Hidden] // Used by the HtmlTable helper so that these fields aren't wrapped up in a table constructed from a model.
    public bool IsValid { get; private set; }
    [Hidden] // Used by the HtmlTable helper so that these fields aren't wrapped up in a table constructed from a model.
    public string Error { get; private set; }

    public string ToJson()
    {
      return JsonConvert.SerializeObject(this);
    }

    private bool ValidateRequiredLengthAttribute(RequiredLengthAttribute requiredLengthAttribute, PropertyInfo property, object value)
    {
      bool result = false;

      if (requiredLengthAttribute != null)
      {
        string sValue = value as string;

        if (!string.IsNullOrEmpty(sValue))
        {
          if (sValue.Length >= requiredLengthAttribute.Length)
          {
            result = true;
          }
          else
          {
            result = false;

            Error = string.Format(CultureInfo.InvariantCulture, MainConfig.ModelValidationErrorRequiredLength, property.Name);
          }
        }
      }

      return result;
    }

    private bool ValidateRequiredAttribute(RequiredAttribute requiredAttribute, PropertyInfo property, object value)
    {
      bool result = false;

      if (requiredAttribute != null)
      {
        // If we pass validation during the form checking then we just check values at that point to see if they aren't null or if
        // the field is a string make sure that it isn't empty.
        string sValue = value as string;

        if (!string.IsNullOrEmpty(sValue))
        {
          if (!string.IsNullOrEmpty(sValue))
          {
            result = true;
          }
        }
        else if (value != null)
        {
          result = true;
        }
        else
        {
          result = false;

          Error = string.Format(CultureInfo.CurrentCulture, MainConfig.ModelValidationErrorRequiredField, property.Name);
        }
      }

      return result;
    }

    private bool ValidateRegularExpressionAttribute(RegularExpressionAttribute regularExpressionAttribute, PropertyInfo property, object value)
    {
      bool result = false;

      if (regularExpressionAttribute != null)
      {
        string sValue = value as string;

        if (!string.IsNullOrEmpty(sValue))
        {
          if (regularExpressionAttribute.Pattern.IsMatch(sValue))
          {
            result = true;
          }
          else
          {
            result = false;

            Error = string.Format(CultureInfo.InvariantCulture, MainConfig.ModelValidationErrorRegularExpression, property.Name);
          }
        }
      }

      return result;
    }

    private bool ValidateRangeAttribute(RangeAttribute rangeAttribute, PropertyInfo property, object value)
    {
      bool result = false;

      if (rangeAttribute != null)
      {
        if (value.GetType().IsAssignableFrom(typeof(Int64)))
        {
          if (((Int64)value).InRange(rangeAttribute.Min, rangeAttribute.Max))
          {
            result = true;
          }
        }
        else
        {
          result = false;

          Error = string.Format(CultureInfo.InvariantCulture, MainConfig.ModelValidationErrorRange, property.Name);
        }
      }

      return result;
    }

    internal void Validate(IAuroraContext context, Model instance)
    {
      context.ThrowIfArgumentNull();

      List<bool> results = new List<bool>();

      foreach (PropertyInfo pi in GetPropertiesWithExclusions<Model>(GetType(), false))
      {
        bool requiredResult = false;
        bool requiredLengthResult = false;
        bool regularExpressionResult = false;
        bool rangeResult = false;

        RequiredAttribute requiredAttribute = (RequiredAttribute)pi.GetCustomAttributes(false).FirstOrDefault(x => x is RequiredAttribute);
        RequiredLengthAttribute requiredLengthAttribute = (RequiredLengthAttribute)pi.GetCustomAttributes(false).FirstOrDefault(x => x is RequiredLengthAttribute);
        RegularExpressionAttribute regularExpressionAttribute = (RegularExpressionAttribute)pi.GetCustomAttributes(false).FirstOrDefault(x => x is RegularExpressionAttribute);
        RangeAttribute rangeAttribute = (RangeAttribute)pi.GetCustomAttributes(false).FirstOrDefault(x => x is RangeAttribute);

        object value = pi.GetValue(instance, null);

        if (requiredAttribute != null)
        {
          if (context.Form.AllKeys.FirstOrDefault(x => x == pi.Name) != null)
          {
            // Required works great for something like a string where it's default value will be null if it's not set explicitly.
            // we have to get a little bit smarter about this and check the incoming form to see if we have a match for 
            // the property type to determine if the value was actually set in the posted form.

            requiredResult = ValidateRequiredAttribute(requiredAttribute, pi, value);

            if (requiredResult)
            {
              results.Add(true);
            }
            else
            {
              results.Add(false);
            }
          }
        }

        if (requiredLengthAttribute != null)
        {
          requiredLengthResult = ValidateRequiredLengthAttribute(requiredLengthAttribute, pi, value);

          if (requiredLengthResult)
          {
            results.Add(true);
          }
          else
          {
            results.Add(false);
          }
        }

        if (regularExpressionAttribute != null)
        {
          regularExpressionResult = ValidateRegularExpressionAttribute(regularExpressionAttribute, pi, value);

          if (regularExpressionResult)
          {
            results.Add(true);
          }
          else
          {
            results.Add(false);
          }
        }

        if (rangeAttribute != null)
        {
          rangeResult = ValidateRangeAttribute(rangeAttribute, pi, value);

          if (rangeResult)
          {
            results.Add(true);
          }
          else
          {
            results.Add(false);
          }
        }
      }

      bool? finalResult = results.FirstOrDefault(x => x == false);

      if (finalResult == null)
      {
        instance.IsValid = false;
      }
      else
      {
        instance.IsValid = true;
      }
    }

    internal static List<PropertyInfo> GetPropertiesNotRequiredToPost<T>(Type t) where T : Model
    {
      var props = GetPropertiesWithExclusions<Model>(t, true)
          .Where(x => x.GetCustomAttributes(false)
                       .FirstOrDefault(y => y is NotRequiredAttribute) == null);

      if (props != null)
        return props.ToList();

      return null;
    }

    /// <summary>
    /// This method returns the properties of a Model minus hidden or excluded properties.
    /// <remarks>
    /// If postedFormBinding is false then only properties with the [Hidden] attribute are excluded. 
    /// The [Hidden] attribute is used to hide properties from the HtmlTable helper or any other helper
    /// that works on a model for constructing a view of it.
    /// 
    /// If postedFormBinding is true then the [ExcludeFromBinding] attributes are removed so that they
    /// will not be used to bind HttpContext.Request.Form parameters to.
    /// </remarks>
    /// </summary>
    /// <typeparam name="T">The base type of Model</typeparam>
    /// <param name="t">The specific type of Model</param>
    /// <param name="postedFormBinding">True if binding from an Http Post otherwise false</param>
    /// <returns>A list of PropertyInfo's</returns>
    internal static List<PropertyInfo> GetPropertiesWithExclusions<T>(Type t, bool postedFormBinding) where T : Model
    {
      var props = t.GetProperties()
                .Where(x => x.GetCustomAttributes(false)
                             .FirstOrDefault(y => y is HiddenAttribute) == null);

      if (postedFormBinding)
      {
        props = props.Where(x => x.GetCustomAttributes(false)
                             .FirstOrDefault(y => y is ExcludeFromBindingAttribute) == null);
      }

      if (props != null)
        return props.ToList();

      return null;
    }

    /// <summary>
    /// Determines the model that goes with the posted form.
    /// </summary>
    /// <param name="context">HttpContextBase</param>
    /// <returns>The type of model</returns>
    internal static Type DetermineModelFromPostedForm(IAuroraContext context)
    {
      context.ThrowIfArgumentNull();

      Type result = null;

      string[] formKeys = context.Form.AllKeys.Where(x => !x.StartsWith(MainConfig.AntiForgeryTokenName)).ToArray();

      if (formKeys.Length > 0)
      {
        foreach (Type m in ApplicationInternals.AllModels(context))
        {
          // TL;DR - This is not sufficient for complex post models.

          // To support things like checkbox lists or other input elements that may need to have a variable number
          // of instances I'll need to add some logic here and make some assumptions outright about the layout (naming convention)
          // of those elements. For a variable number of input elements we'll enforce a naming policy where the name is consistent
          // and is prepended with a number where the number increments from 1 - x. I'll then have to do a deeper inspection
          // of the form variables to see what we are dealing with and then try to map it to a specific type.

          List<string> props = GetPropertiesWithExclusions<Model>(m, true).Select(x => x.Name).ToList();

          if (props.Intersect(formKeys).Count() == props.Union(formKeys).Count())
          {
            result = m;
          }
          else
          {
            props = GetPropertiesNotRequiredToPost<Model>(m).Select(x => x.Name).ToList();

            if (props.Intersect(formKeys).Count() == props.Union(formKeys).Count())
            {
              result = m;
            }
          }
        }
      }

      return result;
    }
  }
  #endregion

  #region VIEW ENGINE / RESULTS / HTML HELPERS

  #region VIEW RESULTS
  public interface IViewResult
  {
    void Render();
  }

  internal class CachedViewResult
  {
    public ViewResult ViewResult { get; set; }
    public string View { get; set; }
  }

  internal static class ResponseHeader
  {
    public static void AddEncodingHeaders(IAuroraContext context)
    {
      context.ThrowIfArgumentNull();

      string acceptsEncoding = context.RequestHeaders["Accept-Encoding"];

      if (!string.IsNullOrEmpty(acceptsEncoding))
      {
        if (acceptsEncoding.Contains("deflate"))
        {
          context.ResponseFilter = new DeflateStream(context.ResponseFilter, CompressionMode.Compress);
          context.ResponseAppendHeader("Content-Encoding", "deflate");
        }
        else if (acceptsEncoding.Contains("gzip"))
        {
          context.ResponseFilter = new GZipStream(context.ResponseFilter, CompressionMode.Compress);
          context.ResponseAppendHeader("Content-Encoding", "gzip");
        }
      }
    }

    public static void SetContentType(IAuroraContext context, string contentType)
    {
      context.ThrowIfArgumentNull();

      context.ResponseClearHeaders();
      context.ResponseClearContent();
      context.ResponseCharset = "utf-8";
      context.ResponseContentType = contentType;
    }
  }

  /// <summary>
  /// The VirtualFileResult is intended to be used anywhere you want to return the file as a byte array.
  /// This could be from a database, built on the fly using a memory stream or some other way. Virtual 
  /// files are files that most likely don't live on your file system inside your application directory.
  /// </summary>
  public class VirtualFileResult : IViewResult
  {
    private IAuroraContext context;
    private string fileName;
    private string fileContentType;
    private byte[] fileBytes;

    public VirtualFileResult(IAuroraContext ctx, string name, byte[] bytes, string contentType)
    {
      ctx.ThrowIfArgumentNull();

      context = ctx;
      fileName = name;
      fileBytes = bytes;
      fileContentType = contentType;
    }

    public void Render()
    {
      ResponseHeader.SetContentType(context, fileContentType);
      ResponseHeader.AddEncodingHeaders(context);

      context.ResponseAddHeader("content-disposition", "attachment;filename=\"" + fileName + "\"");
      context.ResponseBinaryWrite(fileBytes);
    }
  }

  /// <summary>
  /// The PhysicalFileResult is intended to be used anywhere you'd like to return a real file that lives
  /// inside your application directory.
  /// </summary>
  public class PhysicalFileResult : IViewResult
  {
    private IAuroraContext context;
    private string filePath;
    private string contentType;
    private TimeSpan expiry;

    public PhysicalFileResult(IAuroraContext ctx, string file)
    {
      ctx.ThrowIfArgumentNull();

      context = ctx;
      filePath = file;
      contentType = string.Empty;

      string extension = Path.GetExtension(file);

      if (!string.IsNullOrEmpty(extension) && MainConfig.MimeTypes.ContainsKey(extension))
      {
        contentType = MainConfig.MimeTypes[extension];
      }

      int minutesBeforeExpiration = MainConfig.StaticContentCacheExpiry;

      if (!(minutesBeforeExpiration > 0))
      {
        minutesBeforeExpiration = 15;
      }

      expiry = new TimeSpan(0, minutesBeforeExpiration, 0);
    }

    public void Render()
    {
      ResponseHeader.SetContentType(context, contentType);
      ResponseHeader.AddEncodingHeaders(context);

      if (!MainConfig.DisableStaticFileCaching)
      {
        context.ResponseSetCachePolicy(new AuroraCachePolicy()
        {
          CacheabilityOption = HttpCacheability.Public,
          Expires = DateTime.Now.Add(expiry),
          MaxAge = expiry,
          ValidUntilExpires = true,
          IgnoreParams = true
        });
      }

      if (filePath.EndsWith(".css", StringComparison.Ordinal) ||
          filePath.EndsWith(".js", StringComparison.Ordinal) && !AuroraConfig.InDebugMode)
      {
        BundleManager bm = new BundleManager(context);

        string fileName = Path.GetFileName(filePath);

        if (bm.Contains(fileName))
        {
          if (MainConfig.DisableStaticFileCaching)
          {
            bm.RegenerateBundle(fileName);
          }

          context.ResponseWrite(bm[fileName]);
        }
        else
        {
          bool css = filePath.EndsWith(".css", StringComparison.Ordinal);

          using (Minify mini = new Minify(File.ReadAllText(filePath), css, false))
          {
            context.ResponseWrite(mini.Result);
          }
        }
      }
      else
        context.ResponseTransmitFile(filePath);
    }
  }

  public class ViewResult : IViewResult
  {
    private IAuroraContext context;
    private IViewEngine viewEngine;
    private string partitionName;
    private string controllerName;
    private string viewName;
    private string view;
    private Dictionary<string, string> tags;

    private RequestTypeAttribute requestAttribute;

    public ViewResult(IAuroraContext ctx, IViewEngine ve, string pName, string cName, string vName, RequestTypeAttribute attrib, Dictionary<string, string> vTags)
    {
      ctx.ThrowIfArgumentNull();

      context = ctx;
      viewEngine = ve;
      requestAttribute = attrib;
      partitionName = pName;
      controllerName = cName;
      viewName = vName;
      tags = vTags;
      view = string.Empty;
    }

    internal ViewResult(IAuroraContext ctx, string view)
    {
      // This constructor is used by the DefaultCustomError class to 
      // by pass the load view code in the Render method if the web application
      // does not specify the Error view.

      ctx.ThrowIfArgumentNull();

      context = ctx;
      this.view = view;
    }

    public void Refresh(IAuroraContext ctx, RequestTypeAttribute attrib)
    {
      ctx.ThrowIfArgumentNull();

      context = ctx;
      requestAttribute = attrib;
    }

    public void Render()
    {
      ResponseHeader.SetContentType(context, "text/html");

      if (string.IsNullOrEmpty(view))
      {
        #region LOAD THE VIEW
        HttpGetAttribute get = null;
        CachedViewResult cachedViewResult = null;

        if (requestAttribute is HttpGetAttribute)
        {
          get = (requestAttribute as HttpGetAttribute);
        }

        //FIXME: If the page is secure we need to make sure we are only serving the cached copy for logged in users

        if (get != null && get.Cache)
        {
          if (context.Cache[context.Path] != null)
          {
            cachedViewResult = context.Cache[context.Path] as CachedViewResult;

            view = cachedViewResult.View;
          }
        }

        if (string.IsNullOrEmpty(view))
        {
          view = viewEngine.LoadView(partitionName, controllerName, viewName, ViewTemplateType.Action, tags);
        }

        if (get != null)
        {
          string refresh = get.Refresh;

          if (!string.IsNullOrEmpty(refresh))
          {
            context.ResponseAddHeader("Refresh", refresh);
          }

          #region ADD (OR NOT) HTTP CACHE HEADERS TO THE REQUEST
          if (get.Cache)
          {
            context.ResponseSetCachePolicy(new AuroraCachePolicy()
            {
              CacheabilityOption = get.CacheabilityOption,
              Expires = get.DateExpiry,
              MaxAge = get.Expires,
              ValidUntilExpires = true,
              IgnoreParams = true
            });

            if (cachedViewResult != null && string.IsNullOrEmpty(cachedViewResult.View))
            {
              cachedViewResult.View = view;
            }
          }
          else
          {
            context.ResponseSetNoCache();
          }
          #endregion
        }
        #endregion
      }

      ResponseHeader.AddEncodingHeaders(context);

      context.ResponseWrite(view);
    }
  }

  public class PartialResult : IViewResult
  {
    private IAuroraContext context;
    private IViewEngine viewEngine;
    private string partitionName;
    private string controllerName;
    private string fragmentName;
    private Dictionary<string, string> tags;

    public PartialResult(IAuroraContext ctx, IViewEngine ve, string pName, string cName, string fName, Dictionary<string, string> vTags)
    {
      ctx.ThrowIfArgumentNull();

      context = ctx;
      viewEngine = ve;
      partitionName = pName;
      controllerName = cName;
      fragmentName = fName;
      tags = vTags;
    }

    public void Render()
    {
      ResponseHeader.SetContentType(context, "text/html");

      context.ResponseWrite(viewEngine.LoadView(partitionName, controllerName, fragmentName, ViewTemplateType.Shared, tags));
    }
  }

  public class JsonResult : IViewResult
  {
    private IAuroraContext context;
    private object data;

    public JsonResult(IAuroraContext ctx, object d)
    {
      ctx.ThrowIfArgumentNull();

      context = ctx;
      data = d;
    }

    public void Render()
    {
      try
      {
        string json = JsonConvert.SerializeObject(data);

        ResponseHeader.SetContentType(context, "text/html");
        ResponseHeader.AddEncodingHeaders(context);

        context.ResponseWrite(json);
      }
      catch
      {
        throw;
      }
    }
  }

  /// <summary>
  /// A VoidResult is a mechanism which basically says we don't want to do any processing
  /// to this view. This means the action had a void return type and it's responsible for
  /// writing out any response to the response stream.
  /// </summary>
  internal class VoidResult : IViewResult
  {
    public void Render() { }
  }

  public class RedirectResult : IViewResult
  {
    private IAuroraContext context;
    private string location;

    public RedirectResult(IAuroraContext ctx, string loc)
    {
      ctx.ThrowIfArgumentNull();

      context = ctx;
      location = loc;
    }

    public void Render()
    {
      context.ResponseRedirect(location);
    }
  }
  #endregion

  #region VIEW ENGINE

  #region VIEW TEMPLATE
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
  #endregion

  #region VIEW
  internal class View
  {
    public string Name { get; set; }
    public string FullName { get; set; }
    public string Render { get; set; }
    public string Template { get; set; }
    public string CompiledTemplate { get; set; }
    public string TemplateMD5sum { get; set; }
  }
  #endregion

  #region VIEW TEMPLATE LOADER
  internal interface IViewTemplateLoader
  {
    List<ViewTemplate> Load(string[] viewRoots);
  }

  internal sealed class ViewTemplateLoader : IViewTemplateLoader
  {
    private string physicalApplicationRoot;
    private string sharedHint = @"shared\";
    private string fragmentsHint = @"fragments\";
    private static Regex commentBlockRE = new Regex(@"\@\@(?<block>[\s\w\p{P}\p{S}]+?)\@\@");

    public ViewTemplateLoader(string applicationRoot)
    {
      if (string.IsNullOrEmpty(applicationRoot))
      {
        throw new ArgumentNullException("applicationRoot");
      }

      physicalApplicationRoot = applicationRoot;
    }

    public List<ViewTemplate> Load(string[] viewRoots)
    {
      List<ViewTemplate> templates = new List<ViewTemplate>();

      foreach (string viewRoot in viewRoots)
      {
        if (Directory.Exists(viewRoot))
        {
          DirectoryInfo rootDir = new DirectoryInfo(viewRoot);

          var files = rootDir.GetAllFiles().Where(x => x.Extension == ".html");

          foreach (FileInfo fi in files)
          {
            StringBuilder templateBuilder;
            string templateName = fi.Name.Replace(fi.Extension, string.Empty);
            string templateKeyName = fi.FullName.Replace(rootDir.Parent.FullName, string.Empty)
                                                .Replace(physicalApplicationRoot, string.Empty)
                                                .Replace(fi.Extension, string.Empty)
                                                .Replace("\\", "/").TrimStart('/');

            using (StreamReader sr = new StreamReader(fi.OpenRead()))
            {
              templateBuilder = new StringBuilder(sr.ReadToEnd());
            }

            #region STRIP COMMENT SECTIONS
            MatchCollection comments = commentBlockRE.Matches(templateBuilder.ToString());

            if (comments.Count > 0)
            {
              foreach (Match comment in comments)
              {
                templateBuilder.Replace(comment.Value, string.Empty);
              }
            }
            #endregion

            ViewTemplateType templateType = ViewTemplateType.Action;

            if (fi.FullName.ToLower().Contains(sharedHint))
            {
              templateType = ViewTemplateType.Shared;
            }
            else if (fi.FullName.ToLower().Contains(fragmentsHint))
            {
              templateType = ViewTemplateType.Fragment;
            }

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

            templates.Add(new ViewTemplate()
            {
              MD5sum = template.CalculateMD5sum(),
              Partition = partition,
              Controller = controller,
              FullName = templateKeyName,
              Name = templateName,
              Path = fi.FullName,
              Template = template,
              TemplateType = templateType
            });
          }
        }
      }

      if (templates.Count > 0)
      {
        return templates;
      }

      return null;
    }
  }
  #endregion

  #region VIEW COMPILER
  public class BundleFileLinkHandlerEventArgs : EventArgs
  {
    public bool DebugMode { get; set; }
    public string BundleName { get; set; }
    public string BundleLinks { get; set; }
    public List<string> BundleFileList { get; set; }

    public BundleFileLinkHandlerEventArgs(string bundleName, bool debugMode)
    {
      BundleName = bundleName;
      BundleFileList = null;
      DebugMode = debugMode;
    }
  }

  public class AntiForgeryRequestHandlerEventArgs : EventArgs
  {
    public AntiForgeryTokenType TokenType { get; internal set; }
    public string AntiForgeryToken { get; set; }

    public AntiForgeryRequestHandlerEventArgs(AntiForgeryTokenType tokenType)
    {
      TokenType = tokenType;
    }
  }

  internal interface IViewCompiler
  {
    List<View> CompileAll();
    View Compile(string partitionName, string controllerName, string viewName, ViewTemplateType viewType);
    View Render(string fullName, Dictionary<string, string> tags);

    void SetTemplates(List<ViewTemplate> templates);
    void SetCompiledViews(List<View> compiledViews);

    string DetermineKeyName(string partitionName, string controllerName, string viewName, ViewTemplateType viewType);

    event EventHandler<AntiForgeryRequestHandlerEventArgs> AntiForgeryRequestEvent;
    event EventHandler<BundleFileLinkHandlerEventArgs> BundleFileLinkEvent;
  }

  internal class ViewCompiler : IViewCompiler
  {
    private List<ViewTemplate> viewTemplates;
    public List<View> CompiledViews { get; set; }
    public Dictionary<string, List<string>> viewDependencies;

    private static Markdown Markdown = new Markdown();
    private Dictionary<string, List<string>> templateKeyNames;
    private static Regex directiveTokenRE = new Regex(@"(\%\%(?<directive>[a-zA-Z0-9]+)=(?<value>(\S|\.)+)\%\%)", RegexOptions.Compiled);
    private static Regex headBlockRE = new Regex(@"\[\[(?<block>[\s\w\p{P}\p{S}]+)\]\]", RegexOptions.Compiled);
    //private static Regex placeholderRE = new Regex(@"\[([a-zA-Z0-9]+)\](?<block>[\s\S]+?)\[/([a-zA-Z0-9]+)\]", RegexOptions.Compiled);
    private static Regex tagRE = new Regex(@"{({|\||\!)([\w]+)(}|\!|\|)}", RegexOptions.Compiled);
    private static string tagFormatPattern = @"({{({{|\||\!){0}(\||\!|}})}})";
    private static string tagEncodingHint = "{|";
    private static string markdownEncodingHint = "{!";
    private static string unencodedTagHint = "{{";
    private static string viewDirective = "%%View%%";
    private static string headDirective = "%%Head%%";
    private static string antiForgeryToken = "%%AntiForgeryToken%%";
    private static string jsonAntiForgeryToken = "%%JsonAntiForgeryToken%%";

    private StringBuilder directive = new StringBuilder();
    private StringBuilder value = new StringBuilder();

    public event EventHandler<AntiForgeryRequestHandlerEventArgs> AntiForgeryRequestEvent;
    public event EventHandler<BundleFileLinkHandlerEventArgs> BundleFileLinkEvent;

    public ViewCompiler()
    {
      CompiledViews = new List<View>();

      templateKeyNames = new Dictionary<string, List<string>>();
      viewDependencies = new Dictionary<string, List<string>>();
    }

    public void SetTemplates(List<ViewTemplate> templates)
    {
      templates.ThrowIfArgumentNull();

      viewTemplates = templates;
    }

    public void SetCompiledViews(List<View> compiledViews)
    {
      compiledViews.ThrowIfArgumentNull();

      CompiledViews = compiledViews;
    }

    private StringBuilder ProcessDirectives(string fullViewName, string partitionName, string controllerName, ViewTemplateType viewType, StringBuilder rawView)
    {
      MatchCollection dirMatches = directiveTokenRE.Matches(rawView.ToString());
      StringBuilder pageContent = new StringBuilder();

      if (!viewDependencies.ContainsKey(fullViewName))
      {
        viewDependencies[fullViewName] = new List<string>();
      }

      #region PROCESS DIRECTIVES
      foreach (Match match in dirMatches)
      {
        directive.Length = 0;
        directive.Insert(0, match.Groups["directive"].Value);

        value.Length = 0;
        value.Insert(0, match.Groups["value"].Value);

        string _directive = directive.ToString();
        string _value = value.ToString();

        if (_directive == "Placeholder")
        {
          continue;
        }

        if (_directive == "Bundle" && BundleFileLinkEvent != null)
        {
          string bundleName = value.ToString();

          if (!string.IsNullOrEmpty(bundleName))
          {
            StringBuilder htmlTagBuilder = new StringBuilder();

            BundleFileLinkHandlerEventArgs args =
              new BundleFileLinkHandlerEventArgs(bundleName, AuroraConfig.InDebugMode);

            BundleFileLinkEvent(this, args);

            if (!string.IsNullOrEmpty(args.BundleLinks))
            {
              htmlTagBuilder.AppendLine(args.BundleLinks);
            }

            rawView.Replace(match.Groups[0].Value, htmlTagBuilder.ToString());
          }
        }
        else
        {
          #region PROCESS MASTER AND PARTIAL
          string pageName = string.Empty;

          if (!MainConfig.PathStaticFileRE.IsMatch(_value))
          {
            pageName = DetermineKeyName(partitionName, controllerName, value.ToString(), ViewTemplateType.Shared);
          }

          if (!string.IsNullOrEmpty(pageName))
          {
            string template = viewTemplates.FirstOrDefault(x => x.FullName == pageName).Template;

            viewDependencies[fullViewName].Add(pageName);

            switch (_directive)
            {
              case "Master":
                pageContent = new StringBuilder(template);
                rawView.Replace(match.Groups[0].Value, string.Empty);
                pageContent.Replace(viewDirective, rawView.ToString());
                break;

              case "Partial":
                StringBuilder partialContent = new StringBuilder(template);
                rawView.Replace(match.Groups[0].Value, partialContent.ToString());
                break;
            }
          }
          #endregion
        }
      }
      #endregion

      pageContent = ProcessHeadSubstitutions(pageContent);

      // If during the process of building the view we have more directives to process
      // we'll recursively call ProcessDirectives to take care of them
      if (directiveTokenRE.Matches(pageContent.ToString()).Count > 0)
      {
        ProcessDirectives(fullViewName, partitionName, controllerName, viewType, pageContent);
      }

      pageContent = ProcessPlaceHolders(pageContent);
      
      return pageContent;
    }

    private StringBuilder ProcessHeadSubstitutions(StringBuilder pageContent)
    {
      MatchCollection heads = headBlockRE.Matches(pageContent.ToString());

      if (heads.Count > 0)
      {
        StringBuilder headSubstitutions = new StringBuilder();

        foreach (Match head in heads)
        {
          headSubstitutions.Append(Regex.Replace(head.Groups["block"].Value, @"^(\s+)", string.Empty, RegexOptions.Multiline));
          pageContent.Replace(head.Value, string.Empty);
        }

        pageContent.Replace(headDirective, headSubstitutions.ToString());
      }

      pageContent.Replace(headDirective, string.Empty);

      return pageContent;
    }

    private StringBuilder ProcessPlaceHolders(StringBuilder pageContent)
    {
      MatchCollection placeholderMatches = directiveTokenRE.Matches(pageContent.ToString());

      foreach (Match match in placeholderMatches)
      {
        directive.Length = 0;
        directive.Insert(0, match.Groups["directive"].Value);

        value.Length = 0;
        value.Insert(0, match.Groups["value"].Value);

        string _directive = directive.ToString();
        string _value = value.ToString();

        if (_directive == "Placeholder")
        {
          Regex placeholderRE = new Regex(string.Format(@"\[{0}\](?<block>[\s\S]+?)\[/{0}\]", _value));

          Match placeholderMatch = placeholderRE.Match(pageContent.ToString());

          if (placeholderMatch.Success)
          {
            pageContent.Replace(match.Groups[0].Value, placeholderMatch.Groups["block"].Value);
          }

          pageContent.Replace(placeholderMatch.Groups[0].Value, string.Empty);
        }
      }

      return pageContent;
    }

    public string DetermineKeyName(string partitionName, string controllerName, string viewName, ViewTemplateType viewType)
    {
      string result = null;
      List<string> keyTypes = new List<string>();
      string lookupKeyName = string.Empty;
      string viewDesignation = string.Empty;

      if (string.IsNullOrEmpty(partitionName))
      {
        partitionName = "Views";
      }

      if (string.IsNullOrEmpty(controllerName))
      {
        controllerName = "Shared";
      }

      if (viewType == ViewTemplateType.Shared)
      {
        viewDesignation = "Shared/";
      }
      else if (viewType == ViewTemplateType.Fragment)
      {
        viewDesignation = "Fragments/";
      }

      lookupKeyName = string.Format("{0}/{1}/{2}{3}", partitionName, controllerName, viewDesignation, viewName);

      if (!templateKeyNames.ContainsKey(lookupKeyName))
      {
        switch (viewType)
        {
          case ViewTemplateType.Action:

            // partitionControllerScopeActionKeyName
            keyTypes.Add(string.Format(CultureInfo.InvariantCulture, "{0}/{1}/{2}", partitionName, controllerName, viewName));
            // controllerScopeActionKeyName
            keyTypes.Add(string.Format(CultureInfo.InvariantCulture, "{0}/{1}", controllerName, viewName));

            break;

          case ViewTemplateType.Shared:

            // partitionRootScopeSharedKeyName
            keyTypes.Add(string.Format(CultureInfo.InvariantCulture, "{0}/Views/Shared/{1}", partitionName, viewName));
            // partitionControllerScopeSharedKeyName
            keyTypes.Add(string.Format(CultureInfo.InvariantCulture, "{0}/{1}/Shared/{2}", partitionName, controllerName, viewName));
            // controllerScopeSharedKeyName
            keyTypes.Add(string.Format(CultureInfo.InvariantCulture, "{0}/Shared/{1}", controllerName, viewName));
            // globalScopeSharedKeyName
            keyTypes.Add(string.Format(CultureInfo.InvariantCulture, "Views/Shared/{0}", viewName));

            break;

          case ViewTemplateType.Fragment:

            // partitionControllerScopeFragmentKeyName
            keyTypes.Add(string.Format(CultureInfo.InvariantCulture, "{0}/{1}/Fragments/{2}", partitionName, controllerName, viewName));
            // partitionRootScopeFragmentsKeyName
            keyTypes.Add(string.Format(CultureInfo.InvariantCulture, "{0}/Views/Fragments/{1}", partitionName, viewName));
            // controllerScopeFragmentKeyName
            keyTypes.Add(string.Format(CultureInfo.InvariantCulture, "{0}/Fragments/{1}", controllerName, viewName));

            // globalScopeFragmentKeyName
            keyTypes.Add(string.Format(CultureInfo.InvariantCulture, "Views/Fragments/{0}", viewName));

            break;
        }

        templateKeyNames[lookupKeyName] = keyTypes;
      }
      else
      {
        keyTypes = templateKeyNames[lookupKeyName];
      }

      var viewTemplateNames = viewTemplates.Select(x => x.FullName);

      result = keyTypes.Intersect(viewTemplateNames).FirstOrDefault();

      return result;
    }

    public View Compile(string partitionName, string controllerName, string viewName, ViewTemplateType viewType)
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
          {
            compiledView = ProcessDirectives(keyName, partitionName, controllerName, viewType, rawView);
          }

          if (string.IsNullOrEmpty(compiledView.ToString()))
          {
            compiledView = rawView;
          }

          compiledView.Replace(compiledView.ToString(), Regex.Replace(compiledView.ToString(), @"^\s*$\n", string.Empty, RegexOptions.Multiline));

          View view = new View()
          {
            FullName = keyName,
            Name = viewName,
            Template = viewTemplate.Template,
            CompiledTemplate = compiledView.ToString(),
            Render = string.Empty,
            TemplateMD5sum = viewTemplate.MD5sum
          };

          View previouslyCompiled = CompiledViews.FirstOrDefault(x => x.FullName == viewTemplate.FullName);

          if (previouslyCompiled != null)
          {
            CompiledViews.Remove(previouslyCompiled);
          }

          CompiledViews.Add(view);

          return view;
        }
      }

      throw new FileNotFoundException(string.Format(CultureInfo.CurrentCulture, "Cannot find view : {0}", viewName));
    }

    public void RecompileDependencies(string fullViewName, string partitionName, string controllerName)
    {
      var deps = viewDependencies.Where(x => x.Value.FirstOrDefault(y => y == fullViewName) != null);

      foreach (KeyValuePair<string, List<string>> view in deps)
      {
        var template = viewTemplates.FirstOrDefault(x => x.FullName == view.Key);

        if (template != null && template.TemplateType != ViewTemplateType.Fragment)
        {
          Compile(partitionName, controllerName, template.Name, template.TemplateType);
        }
      }
    }

    public List<View> CompileAll()
    {
      foreach (ViewTemplate vt in viewTemplates)
      {
        if (vt.TemplateType != ViewTemplateType.Fragment)
        {
          Compile(vt.Partition, vt.Controller, vt.Name, vt.TemplateType);
        }
        else
        {
          CompiledViews.Add(new View()
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

      if (CompiledViews.Count > 0)
      {
        return CompiledViews;
      }

      return null;
    }

    private StringBuilder ReplaceAntiForgeryTokens(StringBuilder view, string token, AntiForgeryTokenType type)
    {
      var tokens = Regex.Matches(view.ToString(), token)
                        .Cast<Match>()
                        .Select(m => new { Start = m.Index, End = m.Length })
                        .Reverse();

      foreach (var t in tokens)
      {
        AntiForgeryRequestHandlerEventArgs args = new AntiForgeryRequestHandlerEventArgs(type);

        AntiForgeryRequestEvent(this, args);

        if (string.IsNullOrEmpty(args.AntiForgeryToken))
        {
          throw new ArgumentException(MainConfig.AntiForgeryTokenNullOrEmptyError);
        }

        view.Replace(token, args.AntiForgeryToken, t.Start, t.End);
      }

      return view;
    }

    public View Render(string fullName, Dictionary<string, string> tags)
    {
      View compiledView = CompiledViews.FirstOrDefault(x => x.FullName == fullName);

      if (compiledView != null)
      {
        StringBuilder compiledViewSB = new StringBuilder(compiledView.CompiledTemplate);

        if (AntiForgeryRequestEvent != null)
        {
          compiledViewSB = ReplaceAntiForgeryTokens(compiledViewSB, antiForgeryToken, AntiForgeryTokenType.Form);
          compiledViewSB = ReplaceAntiForgeryTokens(compiledViewSB, jsonAntiForgeryToken, AntiForgeryTokenType.Json);
        }

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
                  {
                    compiledViewSB.Replace(m.Value, tag.Value.Trim());
                  }
                  else if (m.Value.StartsWith(tagEncodingHint, StringComparison.Ordinal))
                  {
                    compiledViewSB.Replace(m.Value, HttpUtility.HtmlEncode(tag.Value.Trim()));
                  }
                  else if (m.Value.StartsWith(markdownEncodingHint, StringComparison.Ordinal))
                  {
                    compiledViewSB.Replace(m.Value, Markdown.Transform(tag.Value.Trim()));
                  }
                }
              }
            }
          }

          MatchCollection leftoverMatches = tagRE.Matches(compiledViewSB.ToString());

          if (leftoverMatches != null)
          {
            foreach (Match match in leftoverMatches)
            {
              compiledViewSB.Replace(match.Value, string.Empty);
            }
          }
        }

        compiledView.Render = compiledViewSB.ToString();

        return compiledView;
      }

      return null;
    }
  }
  #endregion

  #region VIEW ENGINE
  public interface IViewEngine
  {
    void Refresh(IAuroraContext ctx);
    string LoadView(string partitionName, string controllerName, string viewName, ViewTemplateType viewType, Dictionary<string, string> tags);
  }

  internal class AuroraViewEngine : IViewEngine
  {
    private IAuroraContext context;

    private List<ViewTemplate> viewTemplates;
    private ViewTemplateLoader viewTemplateLoader;
    private ViewCompiler viewCompiler;

    private static string cssIncludeTag = @"<link href=""{0}"" rel=""stylesheet"" type=""text/css"" />";
    private static string jsIncludeTag = @"<script src=""{0}"" type=""text/javascript""></script>";

    private event EventHandler<AntiForgeryRequestHandlerEventArgs> AntiForgeryRequestEvent
    {
      add
      {
        lock (this) { viewCompiler.AntiForgeryRequestEvent += value; }
      }

      remove
      {
        lock (this) { viewCompiler.AntiForgeryRequestEvent -= value; }
      }
    }

    private event EventHandler<BundleFileLinkHandlerEventArgs> BundleFileLinkEvent
    {
      add
      {
        lock (this) { viewCompiler.BundleFileLinkEvent += value; }
      }

      remove
      {
        lock (this) { viewCompiler.BundleFileLinkEvent -= value; }
      }
    }

    public AuroraViewEngine(IAuroraContext ctx)
    {
      ctx.ThrowIfArgumentNull();

      string appRoot = ctx.ApplicationPhysicalPath;

      viewCompiler = new ViewCompiler();

      AntiForgeryRequestEvent += new EventHandler<AntiForgeryRequestHandlerEventArgs>(AntiForgeryTokenRequestHandler);
      BundleFileLinkEvent += new EventHandler<BundleFileLinkHandlerEventArgs>(BundleFileLinkHandler);

      viewTemplateLoader = new ViewTemplateLoader(appRoot);
      viewTemplates = new List<ViewTemplate>();

      Refresh(ctx);
    }

    public void Refresh(IAuroraContext ctx)
    {
      ctx.ThrowIfArgumentNull();

      context = ctx;

      string[] viewRoots = ApplicationInternals.AllViewRoots(context);

      if (!(viewRoots.Count() >= 1))
      {
        throw new ArgumentException("At least one view root is required to load view templates from.");
      }

      //FIXME: This can be done more efficiently. This needs to load only views that have changed and then we simply merge the changes back into the original list.
      viewTemplates = viewTemplateLoader.Load(viewRoots);

      if (viewTemplates.Count() > 0)
      {
        viewCompiler.SetTemplates(viewTemplates);

        if (ctx.Application[MainConfig.CompiledViewsSessionName] != null)
        {
          viewCompiler.SetCompiledViews(ctx.Application[MainConfig.CompiledViewsSessionName] as List<View>);
        }

        if (!(viewCompiler.CompiledViews.Count() > 0))
        {
          viewCompiler.CompileAll();
        }
        else
        {
          //CompileNewFiles();
          RecompileChangedTemplates();
        }

        ctx.Application[MainConfig.CompiledViewsSessionName] = viewCompiler.CompiledViews;
      }
    }

    //public void CompileNewFiles()
    //{
    //  var newTemplates = from vt in viewTemplates
    //                     join cv in viewCompiler.CompiledViews
    //                       on vt.FullName equals cv.FullName into joinedNames
    //                     from cv in joinedNames.DefaultIfEmpty()
    //                     where cv == null
    //                     select vt;

    //  if (newTemplates.Count() > 0)
    //  {
    //    foreach (ViewTemplate vt in newTemplates)
    //    {
    //      viewCompiler.Compile(vt.Partition, vt.Controller, vt.Name);
    //    }
    //  }
    //}

    public void RecompileChangedTemplates()
    {
      foreach (ViewTemplate vt in viewTemplates)
      {
        View cv = viewCompiler.CompiledViews.FirstOrDefault(x => x.FullName == vt.FullName && x.TemplateMD5sum != vt.MD5sum);

        if (cv != null)
        {
          if (vt.TemplateType == ViewTemplateType.Fragment)
          {
            cv.TemplateMD5sum = vt.MD5sum;
            cv.Template = vt.Template;
            cv.CompiledTemplate = vt.Template;
            cv.Render = string.Empty;
          }
          else
          {
            viewCompiler.RecompileDependencies(vt.FullName, vt.Partition, vt.Controller);
            viewCompiler.Compile(vt.Partition, vt.Controller, vt.Name, vt.TemplateType);
          }
        }
      }
    }

    private void AntiForgeryTokenRequestHandler(object sender, AntiForgeryRequestHandlerEventArgs args)
    {
      args.AntiForgeryToken = AntiForgeryToken.Create(context, args.TokenType);
    }

    private string ProcessBundleLink(string bundlePath)
    {
      string tag = string.Empty;
      string extension = Path.GetExtension(bundlePath);
      bool isAPath = bundlePath.Contains('/') ? true : false;

      string modifiedBundlePath = bundlePath;

      if (!isAPath)
      {
        modifiedBundlePath = string.Format(CultureInfo.InvariantCulture, "/{0}/{1}/{2}", MainConfig.PublicResourcesFolderName, extension.TrimStart('.'), bundlePath);
      }

      switch (extension)
      {
        case ".css":
          tag = string.Format(CultureInfo.InvariantCulture, cssIncludeTag, modifiedBundlePath);
          break;

        case ".js":
          tag = string.Format(CultureInfo.InvariantCulture, jsIncludeTag, modifiedBundlePath);
          break;
      }

      return tag;
    }

    private void BundleFileLinkHandler(object sender, BundleFileLinkHandlerEventArgs args)
    {
      BundleManager bundleManager = new BundleManager(context);

      StringBuilder fileLinkBuilder = new StringBuilder();

      if (AuroraConfig.InDebugMode)
      {
        List<string> bundleFileList = bundleManager.GetBundleFileList(args.BundleName);

        foreach (string bundlePath in bundleFileList)
        {
          fileLinkBuilder.Append(ProcessBundleLink(bundlePath));
        }
      }
      else
      {
        fileLinkBuilder.Append(ProcessBundleLink(args.BundleName));
      }

      args.BundleLinks = fileLinkBuilder.ToString();
    }

    public string LoadView(string partitionName, string controllerName, string viewName, ViewTemplateType viewType, Dictionary<string, string> tags)
    {
      string keyName = viewCompiler.DetermineKeyName(partitionName, controllerName, viewName, viewType);

      if (!string.IsNullOrEmpty(keyName))
      {
        View renderedView = viewCompiler.Render(keyName, tags);

        if (renderedView != null)
        {
          return renderedView.Render;
        }
      }

      return null;
    }
  }
  #endregion

  #endregion

  #endregion

  #region ACTION PARAM TRANSFORM
  public interface IActionParamTransform<T, V>
  {
    T Transform(V value);
  }

  [AttributeUsage(AttributeTargets.Parameter)]
  public sealed class ActionParamTransformAttribute : Attribute
  {
    public string TransformName { get; private set; }

    public ActionParamTransformAttribute(string transformName)
    {
      TransformName = transformName;
    }
  }

  internal class ActionParamTransformInfo
  {
    public Type TransformClassType { get; set; }
    public MethodInfo TransformMethod { get; set; }
    public int IndexIntoParamList { get; set; }
  }
  #endregion

  #region EXCEPTIONS
  [Serializable()]
  public class PluginException : Exception, ISerializable
  {
    public PluginException(string message)
      : base(message)
    {
    }

    public PluginException(string message, Exception innerException)
      : base(message, innerException)
    {
    }

    public PluginException()
      : base(MainConfig.AntiForgeryTokenMissingError)
    {
    }

    protected PluginException(SerializationInfo info, StreamingContext context)
      : base(info, context)
    {
    }
  }

  [Serializable()]
  public class AntiForgeryTokenMissingException : Exception, ISerializable
  {
    public AntiForgeryTokenMissingException(string message)
      : base(message)
    {
    }

    public AntiForgeryTokenMissingException(string message, Exception innerException)
      : base(message, innerException)
    {
    }

    public AntiForgeryTokenMissingException()
      : base(MainConfig.AntiForgeryTokenMissingError)
    {
    }

    protected AntiForgeryTokenMissingException(SerializationInfo info, StreamingContext context)
      : base(info, context)
    {
    }
  }

  [Serializable()]
  public class ActionParameterTransformClassUnknownException : Exception, ISerializable
  {
    public ActionParameterTransformClassUnknownException(string message)
      : base(message)
    {
    }

    public ActionParameterTransformClassUnknownException(string message, Exception innerException)
      : base(message, innerException)
    {
    }

    public ActionParameterTransformClassUnknownException()
      : base(MainConfig.ActionParameterTransformClassUnknownError)
    {
    }

    protected ActionParameterTransformClassUnknownException(SerializationInfo info, StreamingContext context)
      : base(info, context)
    {
    }
  }
  #endregion

  #region STATIC FILE MANAGER
  public static class StaticFileManager
  {
    private static Dictionary<string, string> GetProtectedFilesList(IAuroraContext context)
    {
      context.ThrowIfArgumentNull();

      Dictionary<string, string> protectedFiles = null;

      if (context.Application[MainConfig.StaticFileManagerSessionName] != null)
      {
        protectedFiles = context.Application[MainConfig.StaticFileManagerSessionName] as Dictionary<string, string>;
      }
      else
      {
        protectedFiles = new Dictionary<string, string>();

        context.Application[MainConfig.StaticFileManagerSessionName] = protectedFiles;
      }

      return protectedFiles;
    }

    public static void ProtectFile(IAuroraContext context, string path, string roles)
    {
      context.ThrowIfArgumentNull();

      Dictionary<string, string> protectedFiles = GetProtectedFilesList(context);

      if (!protectedFiles.ContainsKey(path))
      {
        protectedFiles[path] = roles;
      }
    }

    public static void UnprotectFile(IAuroraContext context, string path)
    {
      context.ThrowIfArgumentNull();

      Dictionary<string, string> protectedFiles = GetProtectedFilesList(context);

      if (!protectedFiles.ContainsKey(path))
      {
        protectedFiles.Remove(path);
      }
    }

    internal static bool IsProtected(IAuroraContext context, string path, out string roles)
    {
      context.ThrowIfArgumentNull();

      Dictionary<string, string> protectedFiles = GetProtectedFilesList(context);

      if (protectedFiles.ContainsKey(path))
      {
        roles = protectedFiles[path];

        return true;
      }

      roles = null;

      return false;
    }
  }
  #endregion

  #region CUSTOM ERROR HANDLING
  public abstract class CustomError
  {
    public Exception Exception { get; private set; }
    public IAuroraContext Context { get; private set; }
    public IViewEngine ViewEngine { get; private set; }

    public Dictionary<string, string> ViewTags { get; private set; }

    protected CustomError()
    {
      ViewTags = new Dictionary<string, string>();
    }

    internal static CustomError CreateInstance(Type t, IAuroraContext context)
    {
      context.ThrowIfArgumentNull();

      if (t.BaseType == typeof(CustomError))
      {
        CustomError ce = (CustomError)Activator.CreateInstance(t);

        ce.Context = context;
        ce.ViewEngine = ApplicationInternals.GetViewEngine(context);

        return ce;
      }

      return null;
    }

    public abstract ViewResult OnError(string message, Exception exception);

    public string RenderFragment(string fragmentName, Dictionary<string, string> tags)
    {
      //MainConfig.FragmentsFolderName
      return ViewEngine.LoadView(null, this.GetType().Name, fragmentName, ViewTemplateType.Fragment, tags);
    }

    public string RenderFragment(string fragmentName)
    {
      return RenderFragment(fragmentName, null);
    }

    public string RenderGlobalFragment(string fragmentName, Dictionary<string, string> tags)
    {
      return ViewEngine.LoadView(MainConfig.ViewsFolderName, MainConfig.FragmentsFolderName, fragmentName, ViewTemplateType.Fragment, tags);
    }

    public string RenderGlobalFragment(string fragmentName)
    {
      return RenderGlobalFragment(fragmentName, null);
    }

    public ViewResult View()
    {
      return View(null, "Error");
    }

    internal ViewResult View(string view)
    {
      return new ViewResult(Context, view);
    }

    internal ViewResult View(string controller, string name)
    {
      return new ViewResult(Context, ViewEngine, null, controller, name, null, ViewTags);
    }
  }

  #region DEFAULT CUSTOM ERROR IMPLEMENTATION
  public class DefaultCustomError : CustomError
  {
    // More work needs to be done in here to differentiate what types of errors are happening

    public override ViewResult OnError(string message, Exception exception)
    {
      string error = string.Empty;
      string stackTrace = string.Empty;

      if (exception != null)
      {
        string msg = string.Empty;

        if ((exception.InnerException != null && exception.InnerException is TargetParameterCountException) ||
            (exception != null && exception is TargetParameterCountException))
        {
          Context.ResponseStatusCode(HttpStatusCode.NotFound);

          msg = MainConfig.Http404Error;
        }
        else
        {
          Context.ResponseStatusCode(HttpStatusCode.InternalServerError);

          if (exception.InnerException != null)
          {
            msg = exception.InnerException.Message;
          }
          else
          {
            msg = exception.Message;
          }
        }

        error = String.Format(CultureInfo.CurrentCulture, "<i>{0}</i><br/><br/>Path: {1}", msg, Context.Path);

#if DEBUG
        StringBuilder stacktraceBuilder = new StringBuilder();

        var trace = new System.Diagnostics.StackTrace((exception.InnerException != null) ? exception.InnerException : exception, true);

        if (trace.FrameCount > 0)
        {
          foreach (StackFrame sf in trace.GetFrames())
          {
            if (!string.IsNullOrEmpty(sf.GetFileName()))
            {
              stacktraceBuilder.AppendFormat("method: {0} file: {1}<br />", sf.GetMethod().Name, Path.GetFileName(sf.GetFileName()));
            }
          }

          if (stacktraceBuilder.ToString().Length > 0)
          {
            stackTrace = stacktraceBuilder.ToString();
          }
        }
#endif
      }
      else if (!string.IsNullOrEmpty(message))
      {
        error = message;
      }
      else
      {
        error = "An error occurred.";
      }

      string sharedViewRoot = string.Format(CultureInfo.CurrentCulture, @"{0}\{1}", Context.MapPath(MainConfig.ViewRoot), MainConfig.SharedFolderName);
      string errorViewPath = string.Format(CultureInfo.CurrentCulture, @"{0}\Error.html", sharedViewRoot);
#if DEBUG
      string stackTraceView = @"<p>The problem occurred at:<p><pre><span>{0}</span></pre></p></p>";
#endif

      if (!File.Exists(errorViewPath))
      {
        string view = @"<!DOCTYPE html PUBLIC ""-//W3C//DTD XHTML 1.0 Transitional//EN"" ""http://www.w3.org/TR/xhtml1/DTD/xhtml1-transitional.dtd""><html xmlns=""http://www.w3.org/1999/xhtml""><head><title>Error</title></head><body>{0}{1}</body></html>";

#if DEBUG
        stackTraceView = string.Format(CultureInfo.CurrentCulture, stackTraceView, stackTrace);
        view = string.Format(CultureInfo.CurrentCulture, view, error, stackTraceView);
#else
        view = string.Format(CultureInfo.CurrentCulture, view, error, string.Empty);
#endif

        return View(view);
      }
      else
      {
        ViewTags["error"] = error;
#if DEBUG
        ViewTags["stacktrace"] = string.Format(CultureInfo.CurrentCulture, stackTraceView, stackTrace);
#endif

        return View();
      }
    }
  }
  #endregion
  #endregion

  #region SECURITY & AUTHENTICATION

  #region ACTIVE DIRECTORY
  /// <summary>
  /// This provides some basic Active Directory lookup methods for user accounts.  
  /// </summary>
#if ACTIVE_DIRECTORY
  public class ActiveDirectoryUser
  {
    public string FirstName { get; internal set; }
    public string LastName { get; internal set; }
    public string DisplayName { get; internal set; }
    public string UserName { get; internal set; }
    public string UserPrincipalName { get; internal set; }
    public string PrimaryEmailAddress { get; internal set; }
    public string PhoneNumber { get; internal set; }
    public string Path { get; internal set; }
    public X509Certificate2 ClientCertificate { get; internal set; }
  }

  internal enum ActiveDirectorySearchType
  {
    [Metadata("(&(objectClass=user)(userPrincipalName={0}))")]
    UPN,

    [Metadata("(&(objectClass=user)(proxyAddresses=smtp:{0}))")]
    EMAIL,

    [Metadata("(&(objectClass=user)(samAccountName={0}))")]
    USERNAME
  }

  public static class ActiveDirectory
  {
    internal static ActiveDirectoryUser LookupUser(ActiveDirectorySearchType searchType, string data, bool global)
    {
      if (string.IsNullOrEmpty(searchType.GetMetadata()))
      {
        throw new ArgumentNullException("searchType", MainConfig.ADSearchCriteriaIsNullOrEmptyError);
      }

      if (MainConfig.ActiveDirectorySearchInfos.Length > 0)
      {
        for (int i = 0; i < MainConfig.ActiveDirectorySearchInfos.Length; i++)
        {
          try
          {
            return LookupUser(searchType, data, global,
              MainConfig.ActiveDirectorySearchInfos[i][0],  //Domain
              MainConfig.ActiveDirectorySearchInfos[i][1],  //SearchRoot
              MainConfig.ActiveDirectorySearchInfos[i][2],  //UserName
              MainConfig.ActiveDirectorySearchInfos[i][3]); //Password
          }
          catch
          {
            if (i < MainConfig.ActiveDirectorySearchInfos.Length - 1)
            {
              continue;
            }
            else
            {
              throw;
            }
          }
        }
      }
      else
      {
        if (string.IsNullOrEmpty(MainConfig.ADSearchUser) || string.IsNullOrEmpty(MainConfig.ADSearchPW))
        {
          throw new Exception(MainConfig.ADUserOrPWError);
        }

        if (string.IsNullOrEmpty(MainConfig.ADSearchRoot))
        {
          throw new Exception(MainConfig.ADSearchRootIsNullOrEmpty);
        }

        if (string.IsNullOrEmpty(MainConfig.ADSearchDomain))
        {
          throw new Exception(MainConfig.ADSearchDomainIsNullorEmpty);
        }

        return LookupUser(searchType, data, global,
            MainConfig.ADSearchDomain,
            MainConfig.ADSearchRoot,
            MainConfig.ADSearchUser,
            MainConfig.ADSearchPW);
      }

      return null;
    }

    internal static ActiveDirectoryUser LookupUser(ActiveDirectorySearchType searchType, string data, bool global, string adSearchDomain, string adSearchRoot, string adSearchUser, string adSearchPW)
    {
      ActiveDirectoryUser user = null;

      if (string.IsNullOrEmpty(data))
      {
        throw new ArgumentNullException("data", MainConfig.ActiveDirectorySearchCriteriaNullOrEmpty);
      }

      try
      {
        using (DirectoryEntry searchRootDE = new DirectoryEntry())
        {
          searchRootDE.AuthenticationType = AuthenticationTypes.Secure | AuthenticationTypes.Sealing | AuthenticationTypes.Signing;
          searchRootDE.Username = adSearchUser;
          searchRootDE.Password = adSearchPW;
          searchRootDE.Path = (global) ? string.Format(CultureInfo.InvariantCulture, "GC://{0}", adSearchDomain) : adSearchRoot;

          SearchResult result = null;

          try
          {
            using (DirectorySearcher searcher = new DirectorySearcher())
            {
              searcher.SearchRoot = searchRootDE;
              searcher.Filter = string.Format(CultureInfo.InvariantCulture, searchType.GetMetadata(), data);

              result = searcher.FindOne();
            }
          }
          catch (Exception ex)
          {
            if (ex is DirectoryServicesCOMException ||
                ex is COMException)
            {
              // If there was a problem contacting the domain controller then lets try another one.

              DomainControllerCollection dcc = DomainController.FindAll(new DirectoryContext(DirectoryContextType.Domain, adSearchDomain));
              List<DomainController> domainControllers = (from DomainController dc in dcc select dc).ToList();

              for (int i = 0; i < domainControllers.Count(); i++)
              {
                DomainController dc = domainControllers[i];

                try
                {
                  using (DirectorySearcher searcher = dc.GetDirectorySearcher())
                  {
                    searcher.SearchRoot = searchRootDE;
                    searcher.Filter = string.Format(CultureInfo.InvariantCulture, searchType.GetMetadata(), data);

                    result = searcher.FindOne();
                  }

                  if (result != null)
                  {
                    break;
                  }
                }
                catch (Exception)
                {
                  // Forget about this exception and move to the next domain controller
                }
              }
            }
            else
            {
              throw;
            }
          }

          if (result != null)
          {
            user = GetUser(result.GetDirectoryEntry());
          }
        }
      }
      catch (DirectoryServicesCOMException)
      {
        throw;
      }

      return user;
    }

    #region LOOKUP BY USERNAME
    public static ActiveDirectoryUser LookupUserByUserName(string userName)
    {
      return LookupUser(ActiveDirectorySearchType.USERNAME, userName, false);
    }

    public static ActiveDirectoryUser LookupUserByUserName(string userName, string adSearchUser, string adSearchPW)
    {
      return LookupUser(ActiveDirectorySearchType.USERNAME, userName, false, adSearchUser, adSearchPW, null, null);
    }

    public static ActiveDirectoryUser LookupUserByUserName(string userName, bool global)
    {
      return LookupUser(ActiveDirectorySearchType.USERNAME, userName, global);
    }

    public static ActiveDirectoryUser LookupUserByUserName(string userName, bool global, string adSearchUser, string adSearchPW)
    {
      return LookupUser(ActiveDirectorySearchType.USERNAME, userName, global, adSearchUser, adSearchPW, null, null);
    }

    public static ActiveDirectoryUser LookupUserByUserName(string userName, bool global, string adSearchUser, string adSearchPW, string adSearchRoot, string adSearchDomain)
    {
      return LookupUser(ActiveDirectorySearchType.USERNAME, userName, global, adSearchUser, adSearchPW, adSearchRoot, adSearchDomain);
    }
    #endregion

    #region LOOKUP USER BY UPN
    public static ActiveDirectoryUser LookupUserByUpn(string upn)
    {
      return LookupUser(ActiveDirectorySearchType.UPN, upn, false);
    }

    public static ActiveDirectoryUser LookupUserByUpn(string upn, string adSearchUser, string adSearchPW)
    {
      return LookupUser(ActiveDirectorySearchType.UPN, upn, false, adSearchUser, adSearchPW, null, null);
    }

    public static ActiveDirectoryUser LookupUserByUpn(string upn, bool global)
    {
      return LookupUser(ActiveDirectorySearchType.UPN, upn, global);
    }

    public static ActiveDirectoryUser LookupUserByUpn(string upn, bool global, string adSearchUser, string adSearchPW)
    {
      return LookupUser(ActiveDirectorySearchType.UPN, upn, global, adSearchUser, adSearchPW, null, null);
    }

    public static ActiveDirectoryUser LookupUserByUpn(string upn, bool global, string adSearchUser, string adSearchPW, string adSearchRoot, string adSearchDomain)
    {
      return LookupUser(ActiveDirectorySearchType.UPN, upn, global, adSearchUser, adSearchPW, adSearchRoot, adSearchDomain);
    }
    #endregion

    #region LOOKUP USER BY EMAIL ADDRESS
    public static ActiveDirectoryUser LookupUserByEmailAddress(string email)
    {
      return LookupUser(ActiveDirectorySearchType.EMAIL, email, false);
    }

    public static ActiveDirectoryUser LookupUserByEmailAddress(string email, string adSearchUser, string adSearchPW)
    {
      return LookupUser(ActiveDirectorySearchType.EMAIL, email, false, adSearchUser, adSearchPW, null, null);
    }

    public static ActiveDirectoryUser LookupUserByEmailAddress(string email, bool global)
    {
      return LookupUser(ActiveDirectorySearchType.EMAIL, email, global);
    }

    public static ActiveDirectoryUser LookupUserByEmailAddress(string email, bool global, string adSearchUser, string adSearchPW)
    {
      return LookupUser(ActiveDirectorySearchType.EMAIL, email, global, adSearchUser, adSearchPW, null, null);
    }

    public static ActiveDirectoryUser LookupUserByEmailAddress(string email, bool global, string adSearchUser, string adSearchPW, string adSearchRoot, string adSearchDomain)
    {
      return LookupUser(ActiveDirectorySearchType.EMAIL, email, global, adSearchUser, adSearchPW, adSearchRoot, adSearchDomain);
    }
    #endregion

    private static ActiveDirectoryUser GetUser(DirectoryEntry de)
    {
      return new ActiveDirectoryUser()
      {
        FirstName = de.Properties["givenName"].Value.ToString(),
        LastName = de.Properties["sn"].Value.ToString(),
        UserPrincipalName = (de.Properties["userPrincipalName"].Value != null) ?
              de.Properties["userPrincipalName"].Value.ToString() : null,
        DisplayName = de.Properties["displayName"].Value.ToString(),
        UserName = (de.Properties["samAccountName"].Value != null) ? de.Properties["samAccountName"].Value.ToString() : null,
        PrimaryEmailAddress = GetPrimarySMTP(de) ?? string.Empty,
        PhoneNumber = de.Properties["telephoneNumber"].Value.ToString(),
        Path = de.Path,
        ClientCertificate = de.Properties.Contains("userSMIMECertificate") ?
                new X509Certificate2(de.Properties["userSMIMECertificate"].Value as byte[]) ?? null :
                new X509Certificate2(de.Properties["userCertificate"].Value as byte[]) ?? null
      };
    }

    private static List<string> GetProxyAddresses(DirectoryEntry user)
    {
      List<string> addresses = new List<string>();

      if (user.Properties.Contains("proxyAddresses"))
      {
        foreach (string addr in user.Properties["proxyAddresses"])
        {
          addresses.Add(Regex.Replace(addr, @"\s+", string.Empty, RegexOptions.IgnoreCase).Trim());
        }
      }

      return addresses;
    }

    private static string GetPrimarySMTP(DirectoryEntry user)
    {
      foreach (string p in GetProxyAddresses(user))
      {
        if (p.StartsWith("SMTP:", StringComparison.Ordinal))
        {
          return p.Replace("SMTP:", string.Empty).ToLowerInvariant();
        }
      }

      return null;
    }
  }

  #region CAC AUTHENTICATION
#if CAC_AUTHENTICATION
  public class ActiveDirectoryAuthenticationEventArgs : EventArgs
  {
    public ActiveDirectoryUser User { get; set; }
    public bool Authenticated { get; set; }
    public string CACID { get; set; }
  }

  public class ActiveDirectoryAuthentication : IBindToAction
  {
    public ActiveDirectoryUser User { get; private set; }
    public bool Authenticated { get; private set; }
    public string CACID { get; private set; }

    private event EventHandler<ActiveDirectoryAuthenticationEventArgs> ActiveDirectoryLookupEvent = (sender, args) => { };

    public ActiveDirectoryAuthentication(EventHandler<ActiveDirectoryAuthenticationEventArgs> activeDirectoryLookupHandler)
    {
      ActiveDirectoryLookupEvent += activeDirectoryLookupHandler;
    }

    public void ExecuteBeforeAction(AuroraContext context)
    {
      context.ThrowIfArgumentNull();

      ValidateClientCertificate(context);
    }

#if !DEBUG
    private static string GetCACIDFromCN(AuroraContext context, out X509Certificate2 x509certificate)
    {
      context.ThrowIfArgumentNull();

      x509certificate = context.RequestClientCertificate;

      if (x509certificate == null)
      {
        throw new Exception(MainConfig.ClientCertificateError);
      }

      string cn = x509certificate.GetNameInfo(X509NameType.SimpleName, false);
      string cacid = string.Empty;
      bool valid = true;

      if (string.IsNullOrEmpty(cn))
      {
        throw new Exception(MainConfig.ClientCertificateSimpleNameError);
      }

      if (cn.Contains("."))
      {
        string[] fields = cn.Split('.');

        if (fields.Length > 0)
        {
          cacid = fields[fields.Length - 1];

          foreach (char c in cacid.ToCharArray())
          {
            if (!Char.IsDigit(c))
            {
              valid = false;
              break;
            }
          }
        }
      }

      if (valid)
      {
        return cacid;
      }
      else
      {
        throw new Exception(string.Format(CultureInfo.CurrentCulture, MainConfig.ClientCertificateUnexpectedFormatForCACID, cn));
      }
    }
#endif

    private void ValidateClientCertificate(AuroraContext context)
    {
      context.ThrowIfArgumentNull();

      ActiveDirectoryAuthenticationEventArgs args = new ActiveDirectoryAuthenticationEventArgs();

#if DEBUG
      ActiveDirectoryLookupEvent(this, args);

      User = args.User;
      Authenticated = args.Authenticated;
      CACID = args.CACID;
#else
      X509Certificate2 x509fromASPNET;

      CACID = GetCACIDFromCN(context, out x509fromASPNET);

      User = null;
      Authenticated = false;

      if (!String.IsNullOrEmpty(CACID))
      {
        X509Chain chain = new X509Chain();
        chain.ChainPolicy.RevocationFlag = X509RevocationFlag.EntireChain;
        chain.ChainPolicy.RevocationMode = X509RevocationMode.Online;
        chain.ChainPolicy.UrlRetrievalTimeout = new TimeSpan(0, 0, 30);

        if (chain.Build(x509fromASPNET))
        {
          ActiveDirectoryUser user = null;

          try
          {
            args.CACID = CACID;

            ActiveDirectoryLookupEvent(this, args);

            if (args.User != null)
            {
              user = args.User;
            }
          }
          catch (DirectoryServicesCOMException)
          {
            throw new Exception("A problem occurred trying to communicate with Active Directory");
          }

          if (user != null)
          {
            X509Certificate2 x509fromAD;

            string cacidFromAD = GetCACIDFromCN(context, out x509fromAD);

            if (CACID == cacidFromAD)
            {
              Authenticated = true;
              User = user;
            }
          }
        }
      }
#endif
    }
  }
#endif
  #endregion
#endif
  #endregion

  #region SECURITY MANAGER
  internal class AuthCookie
  {
    public string ID { get; set; }
    public string AuthToken { get; set; }
    public DateTime Expiration { get; set; }
  }

  public class User : IPrincipal
  {
    public string AuthenticationToken { get; internal set; }
    public HttpCookie AuthenticationCookie { get; internal set; }
    public string SessionId { get; internal set; }
    public string IPAddress { get; internal set; }
    public DateTime LogOnDate { get; internal set; }
    public IIdentity Identity { get; internal set; }
    public List<string> Roles { get; internal set; }

    // A method to perform role checking against roles that were initially 
    // tracked when the user was logged in.
    internal Func<User, string, bool> CheckRoles { get; set; }
    // During the security check if the action has designated roles then we'll 
    // populate the action bindings of the method that we are operating on 
    // behalf of. The CheckRoles method will then be able to use this if need 
    // be.
    //
    // Later on down the road it may be useful to switch this to be the full 
    // action parameter list rather than just the bindings.
    public List<object> ActionBindings { get; internal set; }

    public bool IsInRole(string role)
    {
      if (Roles != null)
      {
        return Roles.Contains(role);
      }

      return false;
    }
  }

  public class Identity : IIdentity
  {
    public string AuthenticationType { get; internal set; }
    public bool IsAuthenticated { get; internal set; }
    public string Name { get; internal set; }
  }

#if OPENID
  public class OpenAuthClaimsResponse
  {
    public string ClaimedIdentifier { get; internal set; }
    public string FullName { get; internal set; }
    public string Email { get; internal set; }
  }
#endif

  public static class SecurityManager
  {
    private static List<User> GetUsers(IAuroraContext context)
    {
      context.ThrowIfArgumentNull();

      List<User> users = null;

      if (context.Application[MainConfig.SecurityManagerSessionName] != null)
      {
        users = context.Application[MainConfig.SecurityManagerSessionName] as List<User>;
      }
      else
      {
        context.Application[MainConfig.SecurityManagerSessionName] = users = new List<User>();
      }

      return users;
    }

    private static string CreateAuthenticationToken()
    {
      return (Guid.NewGuid().ToString() + Guid.NewGuid().ToString()).Replace("-", string.Empty);
    }

#if OPENID
    public static void LogonViaOpenAuth(IAuroraContext context, string identifier, Action<string> invalidLogon)
    {
      context.ThrowIfArgumentNull();

      if (!Identifier.IsValid(identifier))
      {
        if (invalidLogon != null)
        {
          invalidLogon(MainConfig.OpenIDInvalidIdentifierError);
        }
      }
      else
      {
        using (var openid = new OpenIdRelyingParty())
        {
          try
          {
            IAuthenticationRequest request = openid.CreateRequest(Identifier.Parse(identifier));

            context.Session[MainConfig.OpenIdProviderUriSessionName] = request.Provider.Uri;

            request.AddExtension(new ClaimsRequest
            {
              Email = DemandLevel.Require,
              FullName = DemandLevel.Require,
              Nickname = DemandLevel.Request
            });

            request.RedirectToProvider();
          }
          catch
          {
            throw;
          }
        }
      }
    }

    public static void FinalizeLogonViaOpenAuth(IAuroraContext context, Action<OpenAuthClaimsResponse> authenticated, Action<string> cancelled, Action<string> failed)
    {
      context.ThrowIfArgumentNull();

      using (var openid = new OpenIdRelyingParty())
      {
        IAuthenticationResponse response = openid.GetResponse();

        if (response != null)
        {
          if (context.Session[MainConfig.OpenIdProviderUriSessionName] != null)
          {
            Uri providerUri = context.Session[MainConfig.OpenIdProviderUriSessionName] as Uri;

            if (providerUri != response.Provider.Uri)
            {
              throw new Exception(MainConfig.OpenIdProviderUriMismatchError);
            }
          }
          else
          {
            throw new Exception(MainConfig.OpenIdProviderUriMismatchError);
          }

          switch (response.Status)
          {
            case AuthenticationStatus.Authenticated:

              ClaimsResponse claimsResponse = response.GetExtension<ClaimsResponse>();

              if (claimsResponse != null)
              {
                if (string.IsNullOrEmpty(claimsResponse.Email))
                {
                  throw new Exception(MainConfig.OpenIDProviderClaimsResponseError);
                }

                OpenAuthClaimsResponse openAuthClaimsResponse = new OpenAuthClaimsResponse()
                {
                  Email = claimsResponse.Email,
                  FullName = claimsResponse.FullName,
                  ClaimedIdentifier = response.ClaimedIdentifier,
                };

                context.Session[MainConfig.OpenIdClaimsResponseSessionName] = openAuthClaimsResponse;

                if (authenticated != null)
                {
                  authenticated(openAuthClaimsResponse);
                }
              }
              break;

            case AuthenticationStatus.Canceled:
              if (cancelled != null)
              {
                cancelled(MainConfig.OpenIDLoginCancelledByProviderError);
              }
              break;

            case AuthenticationStatus.Failed:
              if (failed != null)
              {
                failed(MainConfig.OpenIDLoginFailedError);
              }
              break;
          }
        }
      }
    }

    public static OpenAuthClaimsResponse GetOpenAuthClaimsResponse(IAuroraContext context)
    {
      context.ThrowIfArgumentNull();

      if (context.Session[MainConfig.OpenIdClaimsResponseSessionName] != null)
      {
        return context.Session[MainConfig.OpenIdClaimsResponseSessionName] as OpenAuthClaimsResponse;
      }

      return null;
    }
#endif

    public static string LogOn(IAuroraContext context, string id)
    {
      context.ThrowIfArgumentNull();

      return LogOn(context, id, null, null);
    }

    public static string LogOn(IAuroraContext context, string id, string[] roles, Func<User, string, bool> checkRoles)
    {
      context.ThrowIfArgumentNull();
      roles.ThrowIfArgumentNull();
      MainConfig.EncryptionKey.ThrowIfArgumentNull(MainConfig.EncryptionKeyNotSpecifiedError);

      List<User> users = GetUsers(context);

      //TODO: Probably need to disallow multiple logins. 
      User u = users.FirstOrDefault(x => x.SessionId == context.Session.SessionID
                                                     && x.Identity.Name == id);

      if (u != null)
      {
        return u.AuthenticationToken;
      }

      string authToken = CreateAuthenticationToken();
      DateTime expiration = DateTime.Now.Add(TimeSpan.FromHours(MainConfig.AuthCookieExpiry));

      // Get the frame before this one so we can obtain the method who called us so we can get the attribute
      // for the action
      StackFrame sf = new StackFrame(1);
      MethodInfo callingMethod = (MethodInfo)sf.GetMethod();

      HttpGetAttribute get = (HttpGetAttribute)callingMethod.GetCustomAttributes(false).FirstOrDefault(x => x is HttpGetAttribute);

      HttpCookie auroraAuthCookie = new HttpCookie(MainConfig.AuroraAuthCookieName)
      {
        Expires = expiration,
        HttpOnly = (get != null) ? get.HttpsOnly : true,
        //Domain = string.Format(".{0}", (ctx.Request.Url.Host == "localhost" ? "local" : ctx.Request.Url.Host)),
        Value = Encryption.Encrypt(JsonConvert.SerializeObject(new AuthCookie() { AuthToken = authToken, ID = id, Expiration = expiration }), MainConfig.EncryptionKey)
      };

      context.ResponseCookies.Add(auroraAuthCookie);

      u = new User()
      {
        AuthenticationToken = authToken,
        AuthenticationCookie = auroraAuthCookie,
        SessionId = context.Session.SessionID,
        IPAddress = context.IPAddress,
        LogOnDate = DateTime.Now,
        Identity = new Identity() { AuthenticationType = MainConfig.AuroraAuthTypeName, IsAuthenticated = true, Name = id },
        Roles = roles.ToList(),
        CheckRoles = checkRoles,
        ActionBindings = null
      };

      users.Add(u);

      context.Session[MainConfig.CurrentUserSessionName] = u;
      context.User = u;

      return u.AuthenticationToken;
    }

    public static User GetLoggedOnUser(IAuroraContext context)
    {
      context.ThrowIfArgumentNull();

      if (context.Session[MainConfig.CurrentUserSessionName] != null)
      {
        return context.Session[MainConfig.CurrentUserSessionName] as User;
      }
      else
      {
        AuthCookie cookie = GetAuthCookie(context);

        if (cookie != null)
        {
          User u = GetUsers(context).FirstOrDefault(x => x.SessionId == context.Session.SessionID && x.Identity.Name == cookie.ID);

          if (u != null)
          {
            context.Session[MainConfig.CurrentUserSessionName] = u;

            return u;
          }
        }
      }

      return null;
    }

    private static AuthCookie GetAuthCookie(IAuroraContext context)
    {
      context.ThrowIfArgumentNull();

      HttpCookie cookie = context.RequestCookies[MainConfig.AuroraAuthCookieName];

      if (cookie != null)
      {
        return JsonConvert.DeserializeObject<AuthCookie>(Encryption.Decrypt(cookie.Value, MainConfig.EncryptionKey));
      }

      return null;
    }

    public static bool LogOff(IAuroraContext context)
    {
      context.ThrowIfArgumentNull();

      HttpCookie cookie = context.RequestCookies[MainConfig.AuroraAuthCookieName];

      if (cookie != null)
      {
        AuthCookie authCookie = GetAuthCookie(context);

        List<User> users = GetUsers(context);

        User u = users.FirstOrDefault(x => x.AuthenticationToken == authCookie.AuthToken);

        if (u != null)
        {
          bool result = users.Remove(u);

          if (result)
          {
            if (context.Session[MainConfig.CurrentUserSessionName] != null)
            {
              context.Session.Remove(MainConfig.CurrentUserSessionName);
            }

            context.User = null;
          }

          context.ResponseCookies.Remove(MainConfig.AuroraAuthCookieName);

          return result;
        }
      }

      return false;
    }

    internal static bool CheckAuthentication(IAuroraContext context, User u, string authRoles)
    {
      context.ThrowIfArgumentNull();
      u.ThrowIfArgumentNull();

      if (u.AuthenticationCookie.Expires < DateTime.Now ||
          u.IPAddress != context.IPAddress)
      {
        return false;
      }

      if (!string.IsNullOrEmpty(authRoles))
      {
        if (u.CheckRoles != null)
        {
          if (context.RouteInfo != null)
          {
            u.ActionBindings = context.RouteInfo.Bindings;
          }

          return u.CheckRoles(u, authRoles);
        }
        else
        {
          List<string> minimumRoles = authRoles.Split('|').ToList();

          if (minimumRoles.Intersect(u.Roles).Count() > 0)
          {
            return true;
          }
        }
      }

      return false;
    }

    internal static bool IsAuthenticated(IAuroraContext context, string authRoles)
    {
      context.ThrowIfArgumentNull();

      AuthCookie authCookie = GetAuthCookie(context);

      if (authCookie != null)
      {
        User u = GetUsers(context).FirstOrDefault(x => x.SessionId == context.Session.SessionID && x.Identity.Name == authCookie.ID);

        if (u != null)
        {
          return CheckAuthentication(context, u, authRoles);
        }
      }

      return false;
    }
  }
  #endregion

  #region ANTIFORGERY TOKEN
  public enum AntiForgeryTokenType
  {
    Form,
    Json,
    Raw
  }

  internal static class AntiForgeryToken
  {
    private static List<string> GetTokens(IAuroraContext context)
    {
      context.ThrowIfArgumentNull();

      List<string> tokens = null;

      if (context.Application[MainConfig.AntiForgeryTokenSessionName] != null)
      {
        tokens = context.Application[MainConfig.AntiForgeryTokenSessionName] as List<string>;
      }
      else
      {
        tokens = new List<string>();

        context.Application[MainConfig.AntiForgeryTokenSessionName] = tokens;
      }

      return tokens;
    }

    private static string CreateUniqueToken(IAuroraContext context)
    {
      context.ThrowIfArgumentNull();

      List<string> tokens = GetTokens(context);

      string token = string.Format(CultureInfo.InvariantCulture, "{0}{1}", Guid.NewGuid(), Guid.NewGuid()).Replace("-", string.Empty);

      if (tokens.Contains(token))
      {
        CreateUniqueToken(context);
      }

      return token;
    }

    public static string Create(IAuroraContext context, AntiForgeryTokenType type)
    {
      context.ThrowIfArgumentNull();

      List<string> tokens = GetTokens(context);
      string token = CreateUniqueToken(context);
      tokens.Add(token);

      string renderToken = string.Empty;

      switch (type)
      {
        case AntiForgeryTokenType.Form:
          string uid = Guid.NewGuid().ToString().Replace("-", "").Substring(0, 16);
          renderToken = string.Format(CultureInfo.CurrentCulture, "<input type=\"hidden\" name=\"AntiForgeryToken-{0}\" value=\"{1}\" />", uid, token);
          break;

        case AntiForgeryTokenType.Json:
          renderToken = string.Format(CultureInfo.CurrentCulture, "AntiForgeryToken={0}", token);
          break;

        case AntiForgeryTokenType.Raw:
          renderToken = token;
          break;
      }

      return renderToken;
    }

    public static void RemoveToken(IAuroraContext context, string token)
    {
      context.ThrowIfArgumentNull();

      List<string> tokens = GetTokens(context);

      if (tokens.Contains(token))
      {
        tokens.Remove(token);

        context.Application[MainConfig.AntiForgeryTokenSessionName] = tokens;
      }
    }

    public static bool VerifyToken(IAuroraContext context, string token)
    {
      context.ThrowIfArgumentNull();

      return GetTokens(context).Contains(token);
    }

    public static bool VerifyToken(IAuroraContext context)
    {
      context.ThrowIfArgumentNull();

      if (context.Form.AllKeys.Contains(MainConfig.AntiForgeryTokenName))
      {
        string token = context.Form[MainConfig.AntiForgeryTokenName];

        return GetTokens(context).Contains(token);
      }

      return false;
    }
  }
  #endregion

  #endregion

  #region BUNDLING AND MINIFYING

  #region BUNDLE MANAGER
  /// <summary>
  /// The Bundle class takes Javascript or CSS files and combines and minifies them.
  /// The minification uses a ported copy of JSMin to minify the javascript and css
  /// files. No variable/function shortening is performed. Comments are removed and
  /// new lines are removed. 
  /// </summary>
  public class BundleManager
  {
    internal class BundleInfo
    {
      public FileInfo FileInfo { get; set; }
      public string RelativePath { get; set; }
      public string BundleName { get; set; }
    }

    private static string[] allowedExts = { ".js", ".css" };

    private Dictionary<string, string> bundles;
    private List<BundleInfo> bundleInfos;
    private IAuroraContext context;

    public BundleManager(IAuroraContext ctx)
    {
      ctx.ThrowIfArgumentNull();

      context = ctx;

      if (context.Application[MainConfig.BundleManagerSessionName] != null &&
          context.Application[MainConfig.BundleManagerInfoSessionName] != null)
      {
        bundles = context.Application[MainConfig.BundleManagerSessionName] as Dictionary<string, string>;
        bundleInfos = context.Application[MainConfig.BundleManagerInfoSessionName] as List<BundleInfo>;
      }
      else
      {
        context.Application[MainConfig.BundleManagerSessionName] = bundles = new Dictionary<string, string>();
        context.Application[MainConfig.BundleManagerInfoSessionName] = bundleInfos = new List<BundleInfo>();
      }
    }

    public string this[string bundle]
    {
      get
      {
        if (bundles.ContainsKey(bundle))
        {
          return bundles[bundle];
        }

        return null;
      }
    }

    public bool Contains(string key)
    {
      return bundles.ContainsKey(key);
    }

    public void AddDirectory(string dirPath, string fileExtension, string bundleName)
    {
      DirectoryInfo di = new DirectoryInfo(context.MapPath(dirPath));

      var files = di.GetAllFiles().Where(x => x.Extension == fileExtension);

      foreach (FileInfo file in files)
      {
        bundleInfos.Add(new BundleInfo() { BundleName = bundleName, FileInfo = file });
      }

      ProcessFiles(files, bundleName);
    }

    public void AddFiles(string[] paths, string bundleName)
    {
      paths.ThrowIfArgumentNull();

      if (bundleInfos.FirstOrDefault(x => x.BundleName == bundleName) == null)
      {
        List<FileInfo> files = new List<FileInfo>();

        foreach (string path in paths)
        {
          if (allowedExts.Where(x => Path.GetExtension(path) == x) == null)
          {
            throw new Exception(MainConfig.BundleNameError);
          }

          string fullPath = context.MapPath(path);

          if (File.Exists(fullPath))
          {
            FileInfo fi = new FileInfo(context.MapPath(path));

            files.Add(fi);

            bundleInfos.Add(new BundleInfo()
            {
              BundleName = bundleName,
              FileInfo = fi,
              RelativePath = path
            });
          }
        }

        ProcessFiles(files, bundleName);
      }
    }

    public List<string> GetBundleFileList(string bundle)
    {
      var binfos = bundleInfos
                      .Where(x => x.BundleName == bundle)
                      .Select(x => x.RelativePath);

      if (binfos != null)
      {
        return binfos.ToList();
      }

      return null;
    }

    private void ProcessFiles(IEnumerable<FileInfo> files, string bundleName)
    {
      StringBuilder bundleResult = new StringBuilder();

      foreach (FileInfo f in files)
      {
        using (StreamReader sr = new StreamReader(f.OpenRead()))
        {
          bundleResult.AppendLine();
          bundleResult.AppendLine(sr.ReadToEnd());
          bundleResult.AppendLine();
        }
      }

      if (!string.IsNullOrEmpty(bundleResult.ToString()))
      {
        bool css = bundleName.EndsWith(".css", StringComparison.Ordinal);

        using (Minify mini = new Minify(bundleResult.ToString(), css, false))
        {
          bundles[bundleName] = mini.Result;
        }
      }
    }

    internal void RegenerateBundle(string bundleName)
    {
      var files = bundleInfos.Where(x => x.BundleName == bundleName).Select(x => x.FileInfo);

      if (files != null)
      {
        ProcessFiles(files, bundleName);
      }
    }
  }
  #endregion

  #region JAVASCRIPT / CSS MINIFY
  // Minify is based on my quick port of jsmin.c to C#. I made a few changes so 
  // that it would work with CSS files as well.
  //
  // The original jsmin.c was written by Douglas Crockford, original license 
  // and information below.
  //
  // Find the C source code for jsmin.c here:
  //  https://github.com/douglascrockford/JSMin/blob/master/jsmin.c
  //
  // jsmin.c
  //   2011-09-30
  //
  // Copyright (c) 2002 Douglas Crockford  (www.crockford.com)
  //
  // Permission is hereby granted, free of charge, to any person obtaining a copy of
  // this software and associated documentation files (the "Software"), to deal in
  // the Software without restriction, including without limitation the rights to
  // use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies
  // of the Software, and to permit persons to whom the Software is furnished to do
  // so, subject to the following conditions:
  //
  // The above copyright notice and this permission notice shall be included in all
  // copies or substantial portions of the Software.
  //
  // The Software shall be used for Good, not Evil.
  //
  // THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
  // IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
  // FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
  // AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
  // LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
  // OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
  // SOFTWARE.
  public class Minify : IDisposable
  {
    private const int EOF = -1;

    private bool pack;
    private bool css;

    private int theA;
    private int theB;
    private int theLookahead = EOF;

    private StringReader reader;
    private StringBuilder result { get; set; }

    public string Result
    {
      get
      {
        if (pack)
        {
          return Regex.Replace(result.ToString().Trim(), "(\n|\r)+", " ");
        }

        return result.ToString().Trim();
      }
    }

    public Minify(string text, bool css, bool pack)
    {
      result = new StringBuilder();

      this.css = css;
      this.pack = pack;

      reader = new StringReader(text);

      Go();
    }

    public void Dispose()
    {
      Dispose(true);

      GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
      if (disposing)
      {
        if (reader != null)
        {
          reader.Dispose();
          reader = null;
        }
      }
    }

    private bool IsAlphanum(char c)
    {
      int charCode = (int)c;

      if (css && charCode == '.')
      {
        return true;
      }

      return (charCode >= 'a' && charCode <= 'z' ||
              charCode >= '0' && charCode <= '9' ||
              charCode >= 'A' && charCode <= 'Z' ||
              charCode == '_' ||
              charCode == '$' ||
              charCode == '\\' ||
              charCode == '#' ||
              charCode > 126);
    }

    private int Get()
    {
      int c = theLookahead;
      theLookahead = EOF;
      if (c == EOF)
      {
        c = reader.Read();
      }
      if (c >= ' ' || c == '\n' || c == EOF)
      {
        return c;
      }
      if (c == '\r')
      {
        return '\n';
      }
      return ' ';
    }

    private int Peek()
    {
      theLookahead = Get();
      return theLookahead;
    }

    private int Next()
    {
      int c = Get();
      if (c == '/')
      {
        switch (Peek())
        {
          case '/':
            for (; ; )
            {
              c = Get();
              if (c <= '\n')
              {
                return c;
              }
            }
          case '*':
            Get();
            for (; ; )
            {
              switch (Get())
              {
                case '*':
                  if (Peek() == '/')
                  {
                    Get();
                    return ' ';
                  }
                  break;
                case EOF:
                  throw new Exception("Error: Minify Unterminated comment.");
              }
            }
          default:
            return c;
        }
      }
      return c;
    }

    private void Action(int d)
    {
      switch (d)
      {
        case 1:
          result.Append((char)theA);
          goto case 2;
        case 2:
          theA = theB;
          if (theA == '\'' || theA == '"' || theA == '`')
          {
            for (; ; )
            {
              result.Append((char)theA);
              theA = Get();
              if (theA == theB)
              {
                break;
              }
              if (theA == '\\')
              {
                result.Append((char)theA);
                theA = Get();
              }
              if (theA == EOF)
              {
                throw new Exception("Error: Minify unterminated string literal.");
              }
            }
          }
          goto case 3;
        case 3:
          theB = Next();
          if (theB == '/' && (theA == '(' || theA == ',' || theA == '=' ||
                              theA == ':' || theA == '[' || theA == '!' ||
                              theA == '&' || theA == '|' || theA == '?' ||
                              theA == '{' || theA == '}' || theA == ';' ||
                              theA == '\n'))
          {
            result.Append((char)theA);
            result.Append((char)theB);
            for (; ; )
            {
              theA = Get();
              if (theA == '[')
              {
                for (; ; )
                {
                  result.Append((char)theA);
                  theA = Get();
                  if (theA == ']')
                  {
                    break;
                  }
                  if (theA == '\\')
                  {
                    result.Append((char)theA);
                    theA = Get();
                  }
                  if (theA == EOF)
                  {
                    throw new Exception("Error: Minify unterminated set in Regular Expression literal.");
                  }
                }
              }
              else if (theA == '/')
              {
                break;
              }
              else if (theA == '\\')
              {
                result.Append((char)theA);
                theA = Get();
              }
              if (theA == EOF)
              {
                throw new Exception("Error: Minify unterminated Regular Expression literal.");
              }
              result.Append((char)theA);
            }
            theB = Next();
          }
          break;
      }
    }

    private void Go()
    {
      if (Peek() == 0xEF)
      {
        Get();
        Get();
        Get();
      }
      theA = '\n';
      Action(3);
      while (theA != EOF)
      {
        switch (theA)
        {
          case ' ':
            if (IsAlphanum((char)theB))
            {
              Action(1);
            }
            else
            {
              Action(2);
            }
            break;
          case '\n':
            switch (theB)
            {
              case '{':
              case '[':
              case '(':
              case '+':
              case '-':
              case '!':
              case '~':
                Action(1);
                break;
              case ' ':
                Action(3);
                break;
              default:
                if (IsAlphanum((char)theB))
                {
                  Action(1);
                }
                else
                {
                  Action(2);
                }
                break;
            }
            break;
          default:
            switch (theB)
            {
              case ' ':
                if (IsAlphanum((char)theA))
                {
                  Action(1);
                  break;
                }
                Action(3);
                break;
              case '\n':
                switch (theA)
                {
                  case '}':
                  case ']':
                  case ')':
                  case '+':
                  case '-':
                  case '"':
                  case '\'':
                  case '`':
                    Action(1);
                    break;
                  default:
                    if (IsAlphanum((char)theA))
                    {
                      Action(1);
                    }
                    else
                    {
                      Action(3);
                    }
                    break;
                }
                break;
              default:
                Action(1);
                break;
            }
            break;
        }
      }
    }
  }
  #endregion

  #endregion

  #region EXTENSION METHODS / ENCRYPTION / DYNAMIC DICTIONARY

  #region EXTENSION METHODS
  /// <summary>
  /// Various extension methods are contained in here. They are used to perform data
  /// type casting, retrieve attributes and other simple stuff.
  /// </summary>
  public static class ExtensionMethods
  {
    public static KeyValuePair<string, string>? GetKeyValuePair(this NameValueCollection nvc, string startsWithName)
    {
      foreach (string key in nvc.AllKeys)
      {
        if (key.StartsWith(startsWithName))
        {
          return new KeyValuePair<string, string>(key, nvc[key]);
        }
      }

      return null;
    }

    public static List<KeyValuePair<string, string>> GetKeyValuePairs(this NameValueCollection nvc, string startsWithName)
    {
      List<KeyValuePair<string, string>> keyPairs = new List<KeyValuePair<string, string>>();

      foreach (string key in nvc.AllKeys)
      {
        if (key.StartsWith(startsWithName))
        {
          keyPairs.Add(new KeyValuePair<string, string>(key, nvc[key]));
        }
      }

      return keyPairs;
    }

    public static void ThrowIfArgumentNull<T>(this T t, string message = null)
    {
      string argName = t.GetType().Name;
      bool isNull = false;
      bool isEmptyString = false;

      if (t == null)
      {
        isNull = true;
      }
      else if (t is string)
      {
        if ((t as string) == string.Empty)
        {
          isEmptyString = true;
        }
      }

      if (isNull)
      {
        throw new ArgumentNullException(argName, message);
      }
      else if (isEmptyString)
      {
        throw new ArgumentException(argName, message);
      }
    }

    /// <summary>
    /// Converts a lowercase string to title case
    /// <remarks>
    /// Adapted from: http://stackoverflow.com/questions/271398/what-are-your-favorite-extension-methods-for-c-codeplex-com-extensionoverflow
    /// </remarks>
    /// </summary>
    /// <param name="value">String to convert</param>
    /// <returns>A title cased string</returns>
    public static string ToTitleCase(this string value)
    {
      if (string.IsNullOrEmpty(value))
      {
        return value;
      }

      System.Globalization.CultureInfo cultureInfo = System.Threading.Thread.CurrentThread.CurrentCulture;
      System.Globalization.TextInfo textInfo = cultureInfo.TextInfo;

      // TextInfo.ToTitleCase only operates on the string if is all lower case, otherwise it returns the string unchanged.
      return textInfo.ToTitleCase(value.ToLower());
    }

    /// <summary>
    /// Takes a camel cased string and returns the string with spaces between the words
    /// <remarks>
    /// from http://stackoverflow.com/questions/271398/what-are-your-favorite-extension-methods-for-c-codeplex-com-extensionoverflow
    /// </remarks>
    /// </summary>
    /// <param name="camelCaseWord">The input string</param>
    /// <returns>A string with spaces between words</returns>
    public static string Wordify(this string camelCaseWord)
    {
      // if the word is all upper, just return it
      if (!Regex.IsMatch(camelCaseWord, "[a-z]"))
        return camelCaseWord;

      return string.Join(" ", Regex.Split(camelCaseWord, @"(?<!^)(?=[A-Z])"));
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
      {
        sb.Append(hash[i].ToString("X2"));
      }

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
          #region INT32 OR 64
          if (parms[i].IsInt32())
          {
            _parms[i] = Convert.ToInt32(parms[i], CultureInfo.InvariantCulture);
          }
          else if (parms[i].IsLong())
          {
            _parms[i] = Convert.ToInt64(parms[i], CultureInfo.InvariantCulture);
          }
          #endregion
          #region DOUBLE
          else if (parms[i].IsDouble())
          {
            _parms[i] = Convert.ToDouble(parms[i], CultureInfo.InvariantCulture);
          }
          #endregion
          #region BOOLEAN
          else if (parms[i].ToLowerInvariant() == "true" ||
                  parms[i].ToLowerInvariant() == "false" ||
                  parms[i].ToLowerInvariant() == "on" || // HTML checkbox value
                  parms[i].ToLowerInvariant() == "off" || // HTML checkbox value
                  parms[i].ToLowerInvariant() == "checked") // HTML checkbox value
          {
            if (parms[i].ToLowerInvariant() == "on" || parms[i].ToLowerInvariant() == "checked")
            {
              parms[i] = "true";
            }
            else if (parms[i].ToLowerInvariant() == "off")
            {
              parms[i] = "false";
            }

            _parms[i] = Convert.ToBoolean(parms[i], CultureInfo.InvariantCulture);
          }
          #endregion
          #region DATETIME
          else if (parms[i].IsDate(out dt))
          {
            _parms[i] = dt.Value;
          }
          #endregion
          #region DEFAULT
          else
            _parms[i] = parms[i];
          #endregion
        }

        return _parms;
      }

      return null;
    }

    public static string NewLinesToBR(this string value)
    {
      if (string.IsNullOrEmpty(value))
      {
        throw new ArgumentNullException("value");
      }

      return value.Trim().Replace("\n", "<br />");
    }

    public static string StripHtml(this string value)
    {
      if (string.IsNullOrEmpty(value))
      {
        throw new ArgumentNullException("value");
      }

      HtmlDocument htmlDoc = new HtmlDocument();
      htmlDoc.LoadHtml(value);

      if (htmlDoc == null)
      {
        return value;
      }

      StringBuilder sanitizedString = new StringBuilder();

      foreach (var node in htmlDoc.DocumentNode.ChildNodes)
      {
        sanitizedString.Append(node.InnerText);
      }

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

    public static string ToJson<T>(this IEnumerable<T> type) where T : Model
    {
      return JsonConvert.SerializeObject(type);
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
      {
        return true;
      }

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

    public static List<T> ModifyForEach<T>(this List<T> l, Func<T, T> a)
    {
      List<T> newList = new List<T>();

      foreach (T t in l)
      {
        newList.Add(a(t));
      }

      return newList;
    }

    public static string GetMetadata(this Enum obj)
    {
      if (obj != null)
      {
        MetadataAttribute mda = (MetadataAttribute)obj.GetType().GetField(obj.ToString()).GetCustomAttributes(false).FirstOrDefault(x => x is MetadataAttribute);

        if (mda != null)
        {
          return mda.Metadata;
        }
      }

      return null;
    }

    public static string GetDescriptiveName(this Enum obj)
    {
      if (obj != null)
      {
        DescriptiveNameAttribute dna = (DescriptiveNameAttribute)obj.GetType().GetField(obj.ToString()).GetCustomAttributes(false).FirstOrDefault(x => x is DescriptiveNameAttribute);

        if (dna != null)
        {
          return dna.Name;
        }
      }

      return null;
    }

    public static string GetUniqueId(this Enum obj)
    {
      obj.ThrowIfArgumentNull();

      UniqueIdAttribute uid = (UniqueIdAttribute)obj.GetType().GetField(obj.ToString()).GetCustomAttributes(false).FirstOrDefault(x => x is UniqueIdAttribute);

      if (uid != null)
      {
        uid.GenerateID(string.Format(CultureInfo.InvariantCulture, "{0}.{1}", obj.GetType().Name, obj.ToString()));

        return uid.Id;
      }

      return null;
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
          {
            queue.Enqueue(subDir);
          }
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
          {
            yield return fileInfos[i];
          }
        }
      }
    }
  }
  #endregion

  #region ENCRYPTION
  public static class Encryption
  {
    private static byte[] GetPassphraseHash(string passphrase, int size)
    {
      byte[] phash;

      using (SHA1CryptoServiceProvider hashsha1 = new SHA1CryptoServiceProvider())
      {
        phash = hashsha1.ComputeHash(ASCIIEncoding.ASCII.GetBytes(passphrase));
        Array.Resize(ref phash, size);
      }

      return phash;
    }

    public static string Encrypt(string original)
    {
      return Encrypt(original, MainConfig.EncryptionKey);
    }

    public static string Decrypt(string encrypted)
    {
      return Decrypt(encrypted, MainConfig.EncryptionKey);
    }

    public static string Encrypt(string original, string key)
    {
      string encrypted = string.Empty;

      using (TripleDESCryptoServiceProvider des = new TripleDESCryptoServiceProvider())
      {
        des.Key = GetPassphraseHash(key, des.KeySize / 8);
        des.IV = GetPassphraseHash(key, des.BlockSize / 8);
        des.Padding = PaddingMode.PKCS7;
        des.Mode = CipherMode.ECB;

        byte[] buff = ASCIIEncoding.ASCII.GetBytes(original);
        encrypted = Convert.ToBase64String(des.CreateEncryptor().TransformFinalBlock(buff, 0, buff.Length));
      }

      return encrypted;
    }

    public static string Decrypt(string encrypted, string key)
    {
      string decrypted = string.Empty;

      using (TripleDESCryptoServiceProvider des = new TripleDESCryptoServiceProvider())
      {
        des.Key = GetPassphraseHash(key, des.KeySize / 8);
        des.IV = GetPassphraseHash(key, des.BlockSize / 8);
        des.Padding = PaddingMode.PKCS7;
        des.Mode = CipherMode.ECB;

        byte[] buff = Convert.FromBase64String(encrypted);
        decrypted = ASCIIEncoding.ASCII.GetString(des.CreateDecryptor().TransformFinalBlock(buff, 0, buff.Length));
      }

      return decrypted;
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
      {
        return false;
      }

      return true;
    }

    public override IEnumerable<string> GetDynamicMemberNames()
    {
      return _members.Keys;
    }

    public IEnumerable<string> GetDynamicMemberNames(string key)
    {
      if (_members.ContainsKey(key))
      {
        if (_members[key] is DynamicDictionary)
          return (_members[key] as DynamicDictionary)._members.Keys;
      }

      return null;
    }

    public Dictionary<string, object> GetDynamicDictionary()
    {
      return _members;
    }

    public Dictionary<string, object> GetDynamicDictionary(string key)
    {
      if (_members.ContainsKey(key))
      {
        if (_members[key] is DynamicDictionary)
          return (_members[key] as DynamicDictionary)._members;
      }

      return null;
    }

    public override bool TrySetMember(SetMemberBinder binder, object value)
    {
      if (!_members.ContainsKey(binder.Name))
      {
        _members.Add(binder.Name, value);
      }
      else
      {
        _members[binder.Name] = value;
      }

      return true;
    }

    public override bool TryGetMember(GetMemberBinder binder, out object result)
    {
      if (_members.ContainsKey(binder.Name))
      {
        result = _members[binder.Name];
      }
      else
      {
        result = _members[binder.Name] = new DynamicDictionary();
      }

      return true;
    }
  }
  #endregion

  #endregion

  #region ASP.NET HOOKS - IHTTPHANDLER / IHTTPMODULE

  #region HTTP HANDLER
  public sealed class AuroraHandler : IHttpHandler, IRequiresSessionState
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
      new AuroraEngine(new AuroraContext(new HttpContextWrapper(context)));
    }
  }
  #endregion

  #region HTTP MODULE
  public sealed class AuroraModule : IHttpModule
  {
    public void Dispose() { }

    public void Init(HttpApplication context)
    {
      context.ThrowIfArgumentNull();

      context.Error += new EventHandler(app_Error);
    }

    private void app_Error(object sender, EventArgs e)
    {
      HttpContext context = HttpContext.Current;
      Exception ex = context.Server.GetLastError();
      CustomError customError = ApplicationInternals.GetCustomError(new AuroraContext(new HttpContextWrapper(context)), false);

      customError.OnError(null, ex).Render();
      context.Server.ClearError();
    }
  }
  #endregion

  #endregion
}