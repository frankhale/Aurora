//
// Aurora - A tiny MVC web framework for .NET
//
// Updated On: 22 December 2011
//
// Contact Info:
//
//  Frank Hale - <frankhale@gmail.com> 
//               <http://about.me/frank.hale>
//
// ---------------
// --- CAUTION ---
// ---------------
//
// As with anything new the features listed may or may not work. Things are in
// a constant state of flux and I frequently break stuff along the way. As time
// marches on Aurora has gotten more stable and I am fully engaged in making
// this as stable as possible. Bare with me, probably best to put a hard hat on.
//
// ------------
// --- Why? ---
// ------------
//
// This was born out of curiosity and the desire to dive into web framework 
// construction to gain a better understanding of the internals and to play
// with the ideas of creating applications in new ways.
//
// --------------------
// --- Feature List ---
// --------------------
//
//  - MVC based
//  - Simple tag based view engine with master pages and partial views as well
//    as fragments
//  - URL parameters bind to action method parameters automatically
//  - Posted forms binds to post models or action parameters automatically
//  - Actions can have bound parameters that are bound at runtime
//  - Actions can be segregated based on HttpGet, HttpPost attributes and you 
//    can secure them with the Secure named parameter. Actions without a 
//    designation will not be invoked from a URL.
//  - Actions can have aliases. Aliases can also be added dynamically at 
//    runtime.
//  - Built in OpenID authentication which is as easy as calling two methods. 
//    One to initiate the login with the provider and then one to finalize 
//    the authentication.
//  - Built in Active Directory querying so you can authenticate your user 
//    against an Active Directory user. Typically for use in client certificate
//    authentication.
//
// ----------------
// --- Building ---
// ----------------
//
// This code depends on .NET 3.5 at a minimum
//
// The source code contained here simply needs to be built as a class library.
// I didn't include a Visual Studio or SharpDevelop project because I wanted 
// this to remain as cruft free as possible. For now you can just create a new 
// project in the environment you desire (VS 2008 or higher, MonoDevelop,
// SharpDevelop)
// 
// Add the following references:
//
// System
// System.Configuration
// System.Core
// System.Data
// System.Data.DataSetExtensions
// System.Web
// System.Web.Abstractions
// System.Xml.Linq
// System.Xml.dll
// Newtonsoft.Json.NET35 - http://json.codeplex.com/
// HtmlAgilityPack - http://htmlagilitypack.codeplex.com/
// 
// If you need Active Directory support add the following and add the build flag 
// ACTIVEDIRECTORY:
//
//  System.DirectoryServices
//
// If you want OpenID support add and then add a build flag OPENID:
//
//  DotNetOpenAuth.dll - http://www.dotnetopenauth.net/
//
// -------------
// --- Usage ---
// -------------
//
// There are no project templates yet to make it simple to create a new 
// project. With that said, it's not difficult to create a new project. You can 
// start with a blank ASP.NET project and just delete everything except for the 
// web.config and the default.aspx. I delete all the code inside the 
// default.aspx so that it's just a blank file. This is useful if IIS is set up 
// to hit the default.aspx page if no action is provided. The framework ignores 
// requests for default.aspx and replaces a request for it with the default 
// action instead.
//
// An Aurora web application takes on the form below. There are some convention
// based directories like Controllers and Models. Those aren't needed by the 
// framework, however, they make the project easier to read.
//
// The only folder that the framework mandates is the Views folder along with
// it's child folders (eg. Fragments, Shared)
//
// Controllers will have a subfolder under Views folder that is the same name of 
// the controller. 
//
// The Shared folder is for partial and master views.
//
// The Fragments folder is for HTML fragments that are used by actions to be
// combined with dynamic data.
//
// Project layout:
//
//  App/
//    Controllers/    <- This is a convention
//    Models/         <- This is a convention
//    Views/
//      Fragments/ (similar to partials but rendered by themselves)
//      Shared/ (for master pages and partial views)
//      Home/ (one folder for each controller, folder is the name as controller)
//
// NOTE: You may need to add an empty file called Default.aspx so that server 
//       will find it on an empty request and then redirect to the default 
//       route. 
//
// -------------------
// --- Controllers ---
// -------------------
//
// Controllers in Aurora are just a class that subclass the Controller class 
// defined in the framework. Controllers have actions (methods) that get invoked
// based on the URL being requested. 
//
// Controllers can have event handlers to perform some logic before or after an
// action.
//
// Simply add an event handler for any or both of the following:
//
//  Controller_BeforeAction 
//  Controller_AfterAction 
//
// In addition to event handlers a controller can override the Controller_OnInit
// method to perform some logic right after the controller has bee instantiated.
//
// Controller instances are cached for each session.
//
// ---------------
// --- Actions ---
// --------------- 
//
// Actions are nothing more than public methods in your controller that are 
// labeled with special attributes to tell the framework how to invoke them
// based on the request.
//
// Actions are segregated based of two main types of requests.HttpGet and 
// HttpPost. Actions can also be attributed with FromRedirectOnly to designate 
// that the action can only be invoked from a redirect.
//
// HttpGet has the following named parameters:
//
//  RouteAlias (string)
//  Secure (bool)
//  Roles (string containing roles deliminted by |)
//  CacheabilityOption (HttpCacheability)
//  Cache (bool)
//  Duration (int)
//  HttpsOnly (bool)
//
// HttpPost has the following named parameters: 
//
//  RouteAlias (string)
//  RequireAntiForgeryToken (bool)
//  Secure (bool)
//  Roles (string containing roles deliminted by |)
//  HttpsOnly (bool)
//
// Below shows the typical HttpGet request:
//
// public class Home : Controller
// {
//   [HttpGet]
//   public ViewResult Index()
//   {
//     ViewTags["msg"] = "Hello,World!";
//
//     return View();
//   }
// }
//
// The URL that would call this controller and action would be: 
//
// http://yoursite.com/Home/Index
//
// Actions can have a route alias
//
//  [HttpGet("/Index")]
//  public ViewResult Index()
//  {
//    ...
//  }
//
// The action can then be called like:
//
//  http://yoursite.com/Index
//
// In addition to [HttpGet] and [HttpPost] you can designate an action so that
// it can only be invoked from a redirect. The attribute is named 
// [FromRedirectOnly] and you invoke it by calling RedirectOnlyToAlias or 
// RedirectOnlyToAction.
//
// ------------------------
// --- Bound Parameters ---
// ------------------------ 
// 
// Actions can have bound parameters (think dependency injection) that happens
// automatically when the action is invoked from a URL. Bound parameters are 
// useful so that you can break out some logic that needs to be shared by 
// many actions and have an instance of it passed as a parameter to an action
// at the time it's invoked. This makes actions less complex and they can just
// use an instance of a bound parameter without really worrying where it came 
// from.
//
// Instances are bound to actions using the ActionBinder class. This keeps track 
// of all instances of objects you want to bind to a particular action. These 
// instances are then propagated to the action when they are called from a URL. 
// The example below uses one of the overloaded Add methods to bind a database 
// access layer instance to a bunch of actions in a small Wiki application I 
// wrote.
// 
// protected override void Controller_OnInit()
// {
//   new ActionBinder(Context).Add("Wiki", new string[] { "Index", "Show", 
//                    "Edit", "Delete", "Save", "List" }, new DataConnector());
// }
//
// Now an action definition would look like this:
//
// [HttpGet]
// public ViewResult Index(DataConnector dc)
// {
//   ...
// }
//
// When a request is made for that action a DataConnector instance is passed
// along as the first parameter in the action.
//
// Another interesting thing you can do is have some arbitary code execute 
// before each action that does something to the bound parameters. In the case 
// below I have the database context get newed up before each action tries to 
// use it.
//
// public class DataConnector : IBoundActionObject
// {
//   public WikiDataClassesDataContext DB { get; internal set; }
//
//   public void ExecuteBeforeAction()
//   {
//     DB = new WikiDataClassesDataContext();
//   }
// }
//
// You can do other things in there for instance if your instance needs to be 
// cached then you can reuse instances between action invocations.
//
// --------------------
// --- Error action --- /// NOTE: This is currently not implemented in Aurora
// -------------------- ///       but I'll retain the information here in the
//                      ///       event it's added back.
//
// Controllers can have a custom error action so that any server errors will be
// sent through your action to be displayed. A controller can only have one
// of these.
//
// A example of this would look similar to the following:
//
// [Error]
// public ViewResult Error(string message)
// {
//   ViewTags["message"] = message;
//
//   return View();
// }
//
// NOTE: An error view using this approach would live in the actions view 
//       folder. So if the controller is named Home then this error view path 
//       would be /Views/Home/Error.html
//
// -------------------
// --- Error class ---
// -------------------
// 
// An application can define a special class that will be used to filter server
// errors through. An application can only have one of these classes. 
//
// A example of this would look similar to the following:
// 
// public class MyCustomError : CustomError
// {
//   public override ViewResult Error(string message, Exception e)
//   {
//     ViewTags["message"] = message;
//
//     return View();
//   }
// }
//
// NOTE: Put your custom error view in the /Views/Shared folder.
//
// -------------
// --- Views ---
// -------------
//
// Views are put in a directory called Views with subdirectories named for the 
// controllers they go with. The view name is the same name as the action. A 
// master page can be placed in a folder called Shared which lives the root of 
// the View directory.
//
// Views are simply just HTML templates with specially formatted tags and 
// directives to give the view engine direction on what to do with the view at 
// time of they are compiled.
//
// A simple view with a tag for the page title and a tag for page content would 
// look like this:
//
// <html>
// <head>
// <title>{{title}}</title>
// </head>
// <body>
// {{content}}
// </body>
// </html>
//
// You can HTML encode your dynamic data by enclosing your tag like this 
// {|content|}
//
// An action that would map content for the tags would look like this:
//
// [HttpGet]
// public ViewResult Index()
// {
//   ViewTags["title"] = "My Website";
//   ViewTags["content"] = "Hello, World!";
//
//   return View();
// }
//
// If you wanted to create a master page you would do something like this:
//
// <html>
// <head>
// <title>My Site</title>
// %%Head%%
// </head>
// <body>
// %%View%%
// </body>
// </html>
//
// NOTE: A master page is just an HTML definition that lives in the Shared 
// folder. You can add %%Head%% to the head tag and then add content to the head
// from your view pages by enclosing the content in [[ stuff here ]]. This is
// primarily for including javascript or css that is specific to a view. 
//
// To use this master page in a view use the following directive at the top of 
// your view (where SiteMaster is the name of the master page minus the file 
// extension, master pages live in the /Views/Shared folder):
//
// %%Master=SiteMaster%%
//
// To include partial views in a view you use the following directive anywhere 
// in your view (where MyView is the name of the partial you wish to include in 
// the final rendered page). Partial views live in the /Views/Shared folder:
//
// %%Partial=MyView%%
//
// Not everything can be classified as a partial or a master page so we have
// a concept called Fragments. Fragments are snippets of HTML that you want
// to combine with dynamic data and render at any arbitrary time in your page.
// To render a fragment you can do something like this:
//
// // [HttpGet]
// public ViewResult Index()
// {
//   ViewTags["title"] = "My Website";
//   ViewTags["content"] = RenderFragment("Foo");
//
//   return View();
// }
//
// This would render the fragment called Foo to the ViewTags dictionary for
// key "content". You can use the rendered fragment in any way you like and
// you are free to do string substitution on it or use it in a combined way
// to build up bigger fragments. 
//
// Moving on The following example shows how to post a form to an action. All 
// forms require the tag %%AntiForgeryToken%% to be designated unless the 
// [HttpPost] attribute is designated with the RequireAntiForgeryToken set to 
// false.
//
// <form action="/Home/Index" method="post">
//   %%AntiForgeryToken%%
//   <input type="text" name="tbMessage1" /><br />
//   <input type="text" name="tbMessage2" /><br />
//   <input type="submit" value="Submit" />
// </form>
//
// The way this form gets translated on the back end is either of two forms. 
// You can specify a posted form model that is a simple class with properties
// that have the same name as the input elements in your form or you can specify
// that the action they map to takes each of the elements as a parameter.
// 
// If you take the posted form model approach the class would look like this, 
// note that it must subclass Model and it must expose properties for it's data 
// types.
//
// public class Data : Model
// {
//   public string tbMessage1 { get; set; }
//   public DateTime tbMessage2 { get; set; }
// }
//
// And the action would look like this:
//
// [HttpPost]
// public ViewResult Index(Data d)
// {
//   // do something with the posted form data here
//
//   return View();
// }
//
// --------------
// --- Models ---
// --------------
// 
// At this point in time models are largely notional in that there is no magic
// behind the scenes or validation of them. Your post or view model subclasses 
// the abstract Model class and this is used internally in the framework for the 
// HTML helpers and the posted form determination logic. 
//
// Models are simple plain objects that expose public properties that correspond
// to the data they expose. If the model is a posted form model then it's 
// property names are the same name as the form element names. 
//
// If your model is a view model and is used by let's say the HTMLTable helper 
// (discussed below) then you can add an optional [DescriptiveName("Foo")] 
// attribute to your property names so that it will be used in the table header
// instead of the actual property name. This is great for camel case property 
// names that you want to present with a friendly name.
//
// Models can optionally call the ToJSON() method to return a JSON string of the 
// data that it contains.
//
// --------------------
// --- HTML Helpers ---
// --------------------
//
// A small group of HTML helper classes have been created to assist in creating 
// formatted dynamic data for easy insertion into the ViewTags dictionary. 
// 
// The classes are:
//
//  HTMLTable - Takes a class derived from Model and creates a table from it's
//              properties. You can optionally ignore columns or bind your
//              own columns to it (eg. to create columns for delete/edit)
//
//  HTMLAnchor - Creates an achor tag
//
//  HTMLInput - Creates any type of input tag or textarea
//
//  HTMLSpan - Creates a span tag
//
//  HTMLForm - Creates a form tag
//
//  HTMLSelect - Creates a select/option set
//
// Examples:
//
// The following snippet describes how to use the HTMLTable to generate a table
// from a data model.
//
// Given the following data model:
//
// public class Employee : Model
// {
//   public string Name { get; set; }
//   public string ID { get; set; }
// }
//
// And the following action which adds an HTML table representation of our model
// to a view:
//
// [HttpGet("/Employees")]
// public ViewResult EmployeeList()
// {
//   List<Employee> employees = new List<Employee>() 
//   {
//     new Employee() { ID="U0001", Name="Frank Hale" },
//     new Employee() { ID="U0002", Name="John Doe" },
//     new Employee() { ID="U0003", Name="Steve Smith" }
//   };
//
//   List<ColumnTransform<Employee>> employeeTransforms = 
//                                        new List<ColumnTransform<Employee>>();
//
//   employeeTransforms.Add(new ColumnTransform<Employee>(employees, "View", 
//                 x => string.Format("<a href=\"/View/{0}\">View</a>", x.ID)));
//   employeeTransforms.Add(new ColumnTransform<Employee>(employees, "Delete", 
//             x => string.Format("<a href=\"/Delete/{0}\">Delete</a>", x.ID)));
//
//   HTMLTable<Employee> empTable = 
//        new HTMLTable<Employee>(employees, null, employeeTransforms, 
//                                                              border => "1");
//
//   ViewTags["table"] = empTable.ToString();
//
//   return View();
// }
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
// You can call the Logon method that doesn't specify the roles. 
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
// ------------------
// --- Web.Config ---
// ------------------
//
// Below is a sample web.config:
//
// <?xml version="1.0"?>
// <configuration>
//   <configSections>
//     <section name="Aurora" type="Aurora.WebConfig" 
//       requirePermission="false"/>
//     <section name="dotNetOpenAuth" 
//       type="DotNetOpenAuth.Configuration.DotNetOpenAuthSection" 
//       requirePermission="false" allowLocation="true"/>
//   </configSections>
//
//   <Aurora DefaultRoute="/Index" Debug="true" 
//       StaticFileExtWhiteList="\.(js|png|jpg|gif|ico|css|txt|swf)$" 
//       EncryptionKey="YourEncryptionKeyHere" />
//
//   <dotNetOpenAuth>
//     <openid>
//       <relyingParty>
//         <behaviors>
//           <!-- The following OPTIONAL behavior allows RPs to use SREG only,
//                but be compatible with OPs that use Attribute Exchange (in 
//                various formats). -->
//         <add type=
//  "DotNetOpenAuth.OpenId.Behaviors.AXFetchAsSregTransform, DotNetOpenAuth" />
//         </behaviors>
//       </relyingParty>
//     </openid>
//   </dotNetOpenAuth>
//
//   <system.web>
//     <compilation debug="true"/>
//     <httpHandlers>
//       <add verb="*" path="*" validate="false" type="Aurora.AuroraHandler"/>
//     </httpHandlers>
//     <httpModules>
//       <add type="Aurora.AuroraModule" name="AuroraModule" />
//     </httpModules>
//   </system.web>
// </configuration>
//
// The web.config Aurora section can accept the following parameters:
// 
// DefaultRoute="/Home/Index" 
// Debug="true"
// StaticFileExtWhiteList="\.(js|png|jpg|gif|ico|css|txt|swf)$"
// ApplicationMountPoint="/Some/Path/Here"
// EncryptionKey="Encryption Key"
//
// If you would like to perform basic Active Directory searching for user
// authentication purposes you can use the built in Active Directory class which
// provides some of the basic forms of searching for users within an AD 
// environment. 
// 
// ADSearchUser = "Encrypted Username"
// ADSearchPW = "Encrypted Password"
// ADSearchRoot = "LDAP://URL_GOES_HERE"
// ADSearchDomain = "Active Directory Domain Name" 
//
// I'd recommend encrypting the Active Directory username and password in the 
// web.config. You can use the built in Encryption class to encrypt or decrypt 
// them.
//
// ------------------------
// --- Active Directory ---
// ------------------------
//
// The ActiveDirectory class provides a few static methods to search for a user
// account within an Active Directory based network. You specify the 
// ActiveDirectory domain, searchroot, username and password in the web.config
// as noted above.
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
// ActiveDirectory.LookupUserByUPN("usersUPN");
//
// Assuming the search produced a result these methods will return an instance
// of the ActiveDirectoryUser which provides a basic set of fields that 
// represent an account as it is in ActiveDirectory. For instance, First and 
// Last name, Display name and a digital certificate are some of the fields 
// contained in this class.
// 
// ----------------------
// --- Final Thoughts ---
// ----------------------
//
// I don't claim to be an expert at web development or framework design. It's 
// quite possible that I've totally missed the boat on a particular aspect of 
// the framework and implemented it all wrong. if you find a bug or have a 
// question or suggestion for implementing parts (or all) of this then please 
// let me know.
//
// ---------------
// --- LICENSE ---
// ---------------
//
// GPL version 3 <http://www.gnu.org/licenses/gpl-3.0.html>
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

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
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
using Newtonsoft.Json;
using HtmlAgilityPack;

#if ACTIVEDIRECTORY
using System.DirectoryServices;
#endif

#if OPENID
using DotNetOpenAuth.OpenId;
using DotNetOpenAuth.OpenId.Extensions.SimpleRegistration;
using DotNetOpenAuth.OpenId.RelyingParty;
#endif

#region ASSEMBLY INFORMATION
[assembly: AssemblyTitle("Aurora")]
[assembly: AssemblyDescription("A tiny MVC framework for .NET")]
[assembly: AssemblyCompany("Frank Hale")]
[assembly: AssemblyProduct("Aurora")]
[assembly: AssemblyCopyright("Copyright © 2011")]
[assembly: ComVisible(false)]
[assembly: AssemblyVersion("1.99.10.*")]
#endregion

//TODO: Look into using HttpRuntime.Cache instead of using HttpContext.Session and HttpContext.Application

namespace Aurora
{
  #region WEB.CONFIG CONFIGURATION
  internal class WebConfig : ConfigurationSection
  {
    [ConfigurationProperty("EncryptionKey", DefaultValue = "", IsRequired = true)]
    public string EncryptionKey
    {
      get { return this["EncryptionKey"] as string; }
    }

    [ConfigurationProperty("StaticFileExtWhiteList", DefaultValue = @"\.(js|png|jpg|gif|ico|css|txt|swf)$", IsRequired = false)]
    public string StaticFileExtWhiteList
    {
      get { return this["StaticFileExtWhiteList"] as string; }
    }

    [ConfigurationProperty("ApplicationMountPoint", DefaultValue = "", IsRequired = false)]
    public string ApplicationMountPoint
    {
      get { return this["ApplicationMountPoint"] as string; }
    }

    [ConfigurationProperty("DefaultRoute", DefaultValue = "/Home/Index", IsRequired = false)]
    public string DefaultRoute
    {
      get { return this["DefaultRoute"] as string; }
    }

    [ConfigurationProperty("Debug", DefaultValue = false, IsRequired = false)]
    public bool Debug
    {
      get { return Convert.ToBoolean(this["Debug"]); }
    }

    [ConfigurationProperty("StaticContentCacheExpiry", DefaultValue = "15", IsRequired = false)]
    public int StaticContentCacheExpiry
    {
      get { return Convert.ToInt32(this["StaticContentCacheExpiry"]); }
    }

    [ConfigurationProperty("AuthCookieExpiration", DefaultValue = "8", IsRequired = false)]
    public int AuthCookieExpiry
    {
      get { return Convert.ToInt32(this["AuthCookieExpiration"]); }
    }


    #region ACTIVE DIRECTORY CONFIGURATION
#if ACTIVEDIRECTORY
    [ConfigurationProperty("ADSearchUser", DefaultValue = null, IsRequired = false)]
    public string ADSearchUser
    {
      get { return this["ADSearchUser"].ToString(); }
    }

    [ConfigurationProperty("ADSearchPW", DefaultValue = null, IsRequired = false)]
    public string ADSearchPW
    {
      get { return this["ADSearchPW"].ToString(); }
    }

    [ConfigurationProperty("ADSearchDomain", DefaultValue = null, IsRequired = false)]
    public string ADSearchDomain
    {
      get { return this["ADSearchDomain"].ToString(); }
    }

    [ConfigurationProperty("ADSearchRoot", DefaultValue = null, IsRequired = false)]
    public string ADSearchRoot
    {
      get { return this["ADSearchRoot"].ToString(); }
    }
#endif
    #endregion
  }
  #endregion

  #region MAIN CONFIG
  internal static class MainConfig
  {
    public static WebConfig WebConfig = ConfigurationManager.GetSection("Aurora") as WebConfig;
    public static CustomErrorsSection CustomErrorsSection = ConfigurationManager.GetSection("system.web/customErrors") as CustomErrorsSection;
    public static string EncryptionKey = (MainConfig.WebConfig == null) ? null : WebConfig.EncryptionKey;
    public static int AuthCookieExpiry = (MainConfig.WebConfig == null) ? 8 : WebConfig.AuthCookieExpiry;
    public static int StaticContentCacheExpiry = (MainConfig.WebConfig == null) ? 15 : WebConfig.StaticContentCacheExpiry;
    // The UserName and Password used to search Active Directory should be encrypted in the Web.Config
#if ACTIVEDIRECTORY
    public static string ADSearchUser = (MainConfig.WebConfig == null) ? null : (!string.IsNullOrEmpty(WebConfig.ADSearchUser)) ? Encryption.Decrypt(WebConfig.ADSearchUser, WebConfig.EncryptionKey) : null;
    public static string ADSearchPW = (MainConfig.WebConfig == null) ? null : (!string.IsNullOrEmpty(WebConfig.ADSearchPW)) ? Encryption.Decrypt(WebConfig.ADSearchPW, WebConfig.EncryptionKey) : null;
    public static string ADSearchDomain = (MainConfig.WebConfig == null) ? null : WebConfig.ADSearchDomain;
    public static string ADSearchRoot = (MainConfig.WebConfig == null) ? null : WebConfig.ADSearchRoot;
#endif
    public static Regex PathTokenRE = new Regex(@"/(?<token>[a-zA-Z0-9]+)");
    public static Regex PathStaticFileRE = (WebConfig == null) ? new Regex(@"\.(js|png|jpg|gif|ico|css|txt|swf)$") : new Regex(WebConfig.StaticFileExtWhiteList);
    public static bool Debug = (WebConfig == null) ? true : WebConfig.Debug;
    public static string ApplicationMountPoint = (WebConfig == null) ? string.Empty : WebConfig.ApplicationMountPoint;
    public static string FromRedirectOnlySessionFlag = "__FROFlag";
    public static string RouteManagerSessionName = "__RouteManager";
    public static string RoutesSessionName = "__Routes";
    public static string ControllersSessionName = "__Controllers";
    public static string ControllerInstancesSessionName = "__ControllerInstances";
    public static string ModelsSessionName = "__Models";
    public static string ActionBinderSessionName = "__ActionBinder";
    public static string AntiForgeryTokenSessionName = "__AntiForgeryTokens";
    public static string SecurityManagerSessionName = "__Securitymanager";
    public static string TemplatesSessionName = "__Templates";
    public static string CustomErrorSessionName = "__CustomError";
    public static string CurrentUserSessionName = "__CurrentUser";
    public static string AntiForgeryTokenName = "AntiForgeryToken";
    public static string JsonAntiForgeryTokenName = "JsonAntiForgeryToken";
    public static string AntiForgeryTokenMissing = "An AntiForgery token is required on all forms";
    public static string AntiForgeryTokenVerificationFailed = "AntiForgery token verification failed";
    public static string JsonAntiForgeryTokenMissing = "An AntiForgry token is required on all Json requests";
    public static string AuroraAuthCookieName = "AuroraAuthCookie";
    public static string AuroraAuthTypeName = "AuroraAuth";
    public static string OpenIdClaimsResponseSessionName = "__OpenAuthClaimsResponse";
    public static string OpenIdProviderUriSessionName = "__OpenIdProviderUri";
    public static string SharedFolderName = "Shared";
    public static string PublicResourcesFolderName = "Resources";
    public static string FragmentsFolderName = "Fragments";
    public static string OpenIDInvalidIdentifierError = "The specified login identifier is invalid";
    public static string OpenIDLoginCancelledByProviderError = "Login was cancelled at the provider";
    public static string OpenIDLoginFailedError = "Login failed using the provided OpenID identifier";
    public static string OpenIDProviderClaimsResponseError = "The open auth provider did not return a claims response with a valid email address";
    public static string OpenIdProviderUriMismatchError = "The request OpenID provider Uri does not match the response Uri";
    public static string EncryptionKeyNotSpecifiedError = "The encryption key has not been specified in the web.config";
    public static string PostedFormActionIncorrectNumberOfParametersError = "A post action must have at least one parameter that is the model type of the form that is being posted";
    public static string ADUserOrPWError = "The username or password used to read from Active Directory is null or empty, please check your web.config";
    public static string ADSearchRootIsNullOrEmpty = "The Active Directory search root is null or empty, please check your web.config";
    public static string ADSearchCriteriaIsNullOrEmptyError = "The LDAP query associated with this search type is null or empty, a valid query must be annotated to this search type via the MetaData attribute";
    public static string Http404Error = "Http 404 - Page Not Found";
    public static string Http401Error = "Http 401 - Unauthorized";
    public static string PostRequestError = "Cannot handle this POST request";
    public static string GetRequestError = "Cannot handle this GET request";
    public static string CustomErrorNullExceptionError = "The customer error instance was null";
    public static string OnlyOneCustomErrorClassPerApplicationError = "Cannot have more than one custom error class per application";
    public static string OnlyOneErrorActionPerControllerError = "Cannot have more than one error action per controller";
    public static string RedirectWithoutAuthorizationToError = "RedirectWithoutAuthorizationTo is either null or empty";
    public static string GenericErrorMessage = "An error occurred trying to process this request.";
    public static string DefaultRoute = (WebConfig == null) ? "/Home/Index" : WebConfig.DefaultRoute;
    public static string ViewRoot = Path.DirectorySeparatorChar + "Views";
    public static string CannotFindViewError = "Cannot find view {0}";
    public static string ModelValidationErrorRequiredField = "Model Validation Error: {0} is a required field.";
    public static string ModelValidationErrorRequiredLength = "Model Validation Error: {0} has a required length that was not met.";
    public static string ModelValidationErrorRegularExpression = "Model Validation Error: {0} did not pass regular expression validation.";
    public static string ModelValidationErrorRange = "Model Validation Error: {0} was not within the range specified.";
  }
  #endregion

  #region ATTRIBUTES
  public enum ActionSecurity
  {
    Secure,
    NonSecure
  }

  [AttributeUsage(AttributeTargets.All)]
  public class MetaDataAttribute : Attribute
  {
    public string MetaData { get; internal set; }

    public MetaDataAttribute(string metaData)
    {
      MetaData = metaData;
    }
  }

  [AttributeUsage(AttributeTargets.Property)]
  public class DescriptiveNameAttribute : Attribute
  {
    public string Name { get; private set; }

    public DescriptiveNameAttribute(string name)
    {
      Name = name;
    }
  }

  #region HTTP REQUEST
  public abstract class RequestTypeAttribute : Attribute
  {
    public ActionSecurity SecurityType = ActionSecurity.NonSecure;
    public string RouteAlias = string.Empty;
    public string Roles = string.Empty;
    public bool HttpsOnly = false;
    public string RedirectWithoutAuthorizationTo = string.Empty;
  }

  [AttributeUsage(AttributeTargets.Method)]
  public class FromRedirectOnlyAttribute : RequestTypeAttribute
  {
    public FromRedirectOnlyAttribute(string routeAlias)
    {
      RouteAlias = routeAlias;
    }
  }

  [AttributeUsage(AttributeTargets.Method)]
  public class HttpGetAttribute : RequestTypeAttribute
  {
    public HttpCacheability CacheabilityOption = HttpCacheability.Public;

    public bool Cache = false;

    public int Duration = 0;

    public HttpGetAttribute()
    {
    }

    public HttpGetAttribute(string routeAlias)
    {
      RouteAlias = routeAlias;
    }

    public HttpGetAttribute(ActionSecurity sec) : this(string.Empty, sec) { }

    public HttpGetAttribute(string routeAlias, ActionSecurity sec)
    {
      SecurityType = sec;
      RouteAlias = routeAlias;
    }
  }

  [AttributeUsage(AttributeTargets.Method)]
  public class HttpPostAttribute : RequestTypeAttribute
  {
    public bool RequireAntiForgeryToken = true;

    public HttpPostAttribute() { }

    public HttpPostAttribute(string routeAlias)
    {
      RouteAlias = routeAlias;
    }

    public HttpPostAttribute(ActionSecurity sec)
      : this(string.Empty, sec)
    {
    }

    public HttpPostAttribute(string routeAlias, ActionSecurity sec)
    {
      SecurityType = sec;
      RouteAlias = routeAlias;
    }
  }

  [AttributeUsage(/*AttributeTargets.Method |*/ AttributeTargets.Class)]
  public class ErrorAttribute : Attribute
  {
    public ErrorAttribute()
    {
    }
  }
  #endregion

  #region MODEL VALIDATION
  public abstract class ModelValidationAttributeBase : Attribute
  {
    internal string Error { get; set; }

    public ModelValidationAttributeBase(string errorMessage)
    {
    }
  }

  [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
  public class RequiredAttribute : ModelValidationAttributeBase
  {
    public RequiredAttribute(string errorMessage)
      : base(errorMessage)
    {
    }
  }

  [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
  public class RequiredLengthAttribute : ModelValidationAttributeBase
  {
    internal int Length { get; private set; }

    public RequiredLengthAttribute(int length, string errorMessage)
      : base(errorMessage)
    {
      Length = length;
    }
  }

  [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
  public class RegularExpressionAttribute : ModelValidationAttributeBase
  {
    internal Regex Pattern { get; private set; }

    public RegularExpressionAttribute(string pattern, string errorMessage)
      : base(errorMessage)
    {
      Pattern = new Regex(pattern);
    }
  }

  [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
  public class RangeAttribute : ModelValidationAttributeBase
  {
    internal int Min { get; private set; }
    internal int Max { get; private set; }

    public RangeAttribute(int min, int max, string errorMessage)
      : base(errorMessage)
    {
      Min = min;
      Max = max;
    }
  }
  #endregion

  [AttributeUsage(AttributeTargets.Field)]
  public class UniqueIDAttribute : Attribute
  {
    public string ID { get; internal set; }

    public UniqueIDAttribute() { }

    internal void GenerateID(string name)
    {
      HttpContext ctx = HttpContext.Current;
      Dictionary<string, string> uids = null;

      if (ctx.Session["__UniquedIDs"] != null)
      {
        uids = ctx.Session["__UniquedIDs"] as Dictionary<string, string>;
      }
      else
      {
        uids = new Dictionary<string, string>();

        ctx.Session["__UniquedIDs"] = uids;
      }

      if (!uids.ContainsKey(name))
      {
        ID = NewUID();
        uids[name] = ID;
      }
      else
      {
        ID = uids[name];
      }
    }

    private string NewUID()
    {
      return Guid.NewGuid().ToString().Replace("-", "").Substring(0, 16);
    }
  }
  #endregion

  #region ACTIVE DIRECTORY
#if ACTIVEDIRECTORY
  public class ActiveDirectoryUser
  {
    public string FirstName { get; internal set; }
    public string LastName { get; internal set; }
    public string DisplayName { get; internal set; }
    public string UserName { get; internal set; }
    public string UserPrincipalName { get; internal set; }
    public string PrimaryEmailAddress { get; internal set; }
    public string PhoneNumber { get; internal set; }
    public X509Certificate2 ClientCertificate { get; internal set; }
  }

  internal enum ActiveDirectorySearchType
  {
    [MetaData("(&(objectClass=user)(userPrincipalName={0}))")]
    UPN,

    [MetaData("(&(objectClass=user)(proxyAddresses=smtp:{0}))")]
    EMAIL,

    [MetaData("(&(objectClass=user)(samAccountName={0}))")]
    USERNAME
  }

  public class ActiveDirectory
  {
    private static string GLOBAL_CATALOG = string.Format("GC://{0}", MainConfig.ADSearchDomain);

    internal static ActiveDirectoryUser LookupUser(ActiveDirectorySearchType searchType, string data, bool global)
    {
      if (string.IsNullOrEmpty(MainConfig.ADSearchUser) || string.IsNullOrEmpty(MainConfig.ADSearchPW))
        throw new Exception(MainConfig.ADUserOrPWError);

      if (string.IsNullOrEmpty(searchType.GetMetaData()))
        throw new Exception(MainConfig.ADSearchCriteriaIsNullOrEmptyError);

      if (string.IsNullOrEmpty(data)) return null;

      DirectoryEntry searchRoot = new DirectoryEntry()
      {
        AuthenticationType = AuthenticationTypes.Secure | AuthenticationTypes.Sealing | AuthenticationTypes.Signing,
        Username = MainConfig.ADSearchUser,
        Password = MainConfig.ADSearchPW,
        Path = GetOU(global)
      };

      DirectorySearcher searcher = new DirectorySearcher();
      searcher.SearchRoot = searchRoot;
      searcher.Filter = string.Format(searchType.GetMetaData(), data);

      try
      {
        SearchResult result = searcher.FindOne();

        if (result != null)
          return GetUser(result.GetDirectoryEntry());
      }
      catch
      {
        throw;
      }

      return null;
    }

    public static ActiveDirectoryUser LookupUserByUserName(string userName)
    {
      return LookupUser(ActiveDirectorySearchType.USERNAME, userName, false);
    }

    public static ActiveDirectoryUser LookupUserByUserName(string userName, bool global)
    {
      return LookupUser(ActiveDirectorySearchType.USERNAME, userName, global);
    }

    public static ActiveDirectoryUser LookupUserByUPN(string upn)
    {
      return LookupUser(ActiveDirectorySearchType.UPN, upn, false);
    }

    public static ActiveDirectoryUser LookupUserByUPN(string upn, bool global)
    {
      return LookupUser(ActiveDirectorySearchType.UPN, upn, global);
    }

    public static ActiveDirectoryUser LookupUserByEmailAddress(string email)
    {
      return LookupUser(ActiveDirectorySearchType.EMAIL, email, false);
    }

    public static ActiveDirectoryUser LookupUserByEmailAddress(string email, bool global)
    {
      return LookupUser(ActiveDirectorySearchType.EMAIL, email, global);
    }

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
        if (p.StartsWith("SMTP:")) return p.Replace("SMTP:", string.Empty).ToLower();
      }

      return null;
    }

    private static string GetOU(bool global)
    {
      if (string.IsNullOrEmpty(MainConfig.ADSearchRoot))
        throw new Exception(MainConfig.ADSearchRootIsNullOrEmpty);

      return (global) ? GLOBAL_CATALOG : MainConfig.ADSearchRoot;
    }
  }
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
    public string SessionID { get; internal set; }
    public string IPAddress { get; internal set; }
    public DateTime LoginDate { get; internal set; }
    public IIdentity Identity { get; internal set; }
    public List<string> Roles { get; internal set; }

    public bool IsInRole(string role)
    {
      if (Roles != null)
        return Roles.Contains(role);

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

  public class SecurityManager
  {
    private static List<User> GetUsers(HttpContextBase context)
    {
      List<User> users = null;

      if (context.Application[MainConfig.SecurityManagerSessionName] != null)
        users = context.Application[MainConfig.SecurityManagerSessionName] as List<User>;
      else
        context.Application[MainConfig.SecurityManagerSessionName] = users = new List<User>();

      return users;
    }

    private static string CreateAuthenticationToken()
    {
      return (Guid.NewGuid().ToString() + Guid.NewGuid().ToString()).Replace("-", string.Empty);
    }

#if OPENID
    public static void LogonViaOpenAuth(HttpContextBase ctx, string identifier, Action<string> invalidLogon)
    {
      if (!Identifier.IsValid(identifier))
      {
        if (invalidLogon != null)
          invalidLogon(MainConfig.OpenIDInvalidIdentifierError);
      }
      else
      {
        using (var openid = new OpenIdRelyingParty())
        {
          try
          {
            IAuthenticationRequest request = openid.CreateRequest(Identifier.Parse(identifier));

            ctx.Session[MainConfig.OpenIdProviderUriSessionName] = request.Provider.Uri;

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

    public static void FinalizeLogonViaOpenAuth(HttpContextBase ctx, Action<OpenAuthClaimsResponse> authenticated, Action<string> cancelled, Action<string> failed)
    {
      using (var openid = new OpenIdRelyingParty())
      {
        IAuthenticationResponse response = openid.GetResponse();

        if (response != null)
        {
          if (ctx.Session[MainConfig.OpenIdProviderUriSessionName] != null)
          {
            Uri providerUri = ctx.Session[MainConfig.OpenIdProviderUriSessionName] as Uri;

            if (providerUri != response.Provider.Uri)
              throw new Exception(MainConfig.OpenIdProviderUriMismatchError);
          }
          else
            throw new Exception(MainConfig.OpenIdProviderUriMismatchError);

          switch (response.Status)
          {
            case AuthenticationStatus.Authenticated:

              ClaimsResponse claimsResponse = response.GetExtension<ClaimsResponse>();

              if (claimsResponse != null)
              {
                if (string.IsNullOrEmpty(claimsResponse.Email))
                  throw new Exception(MainConfig.OpenIDProviderClaimsResponseError);

                OpenAuthClaimsResponse openAuthClaimsResponse = new OpenAuthClaimsResponse()
                {
                  Email = claimsResponse.Email,
                  FullName = claimsResponse.FullName,
                  ClaimedIdentifier = response.ClaimedIdentifier
                };

                ctx.Session[MainConfig.OpenIdClaimsResponseSessionName] = openAuthClaimsResponse;

                if (authenticated != null)
                  authenticated(openAuthClaimsResponse);
              }
              break;

            case AuthenticationStatus.Canceled:
              if (cancelled != null)
                cancelled(MainConfig.OpenIDLoginCancelledByProviderError);
              break;

            case AuthenticationStatus.Failed:
              if (failed != null)
                failed(MainConfig.OpenIDLoginFailedError);
              break;
          }
        }
      }
    }

    /// <summary>
    /// Gets an open auth claims response so we can identify who logged on.
    /// </summary>
    /// <param name="ctx"></param>
    /// <returns></returns>
    public static OpenAuthClaimsResponse GetOpenAuthClaimsResponse(HttpContextBase ctx)
    {
      if (ctx.Session[MainConfig.OpenIdClaimsResponseSessionName] != null)
        return ctx.Session[MainConfig.OpenIdClaimsResponseSessionName] as OpenAuthClaimsResponse;

      return null;
    }
#endif

    public static string Logon(HttpContextBase ctx, string id)
    {
      return Logon(ctx, id, null);
    }

    public static string Logon(HttpContextBase ctx, string id, string[] roles)
    {
      if (string.IsNullOrEmpty(MainConfig.EncryptionKey))
        throw new Exception(MainConfig.EncryptionKeyNotSpecifiedError);

      List<User> users = GetUsers(ctx);

      User u = users.FirstOrDefault(x => x.SessionID == ctx.Session.SessionID && x.Identity.Name == id);

      if (u != null) return u.AuthenticationToken;

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

      ctx.Response.Cookies.Add(auroraAuthCookie);

      u = new User()
      {
        AuthenticationToken = authToken,
        AuthenticationCookie = auroraAuthCookie,
        SessionID = ctx.Session.SessionID,
        IPAddress = ctx.IPAddress(),
        LoginDate = DateTime.Now,
        Identity = new Identity() { AuthenticationType = MainConfig.AuroraAuthTypeName, IsAuthenticated = true, Name = id },
        Roles = roles.ToList()
      };

      users.Add(u);

      ctx.Session[MainConfig.CurrentUserSessionName] = u;
      ctx.User = u;

      return u.AuthenticationToken;
    }

    public static User GetLoggedOnUser(HttpContextBase ctx)
    {
      if (ctx.Session[MainConfig.CurrentUserSessionName] != null)
        return ctx.Session[MainConfig.CurrentUserSessionName] as User;

      return null;
    }

    private static AuthCookie GetAuthCookie(HttpContextBase ctx)
    {
      HttpCookie cookie = ctx.Request.Cookies[MainConfig.AuroraAuthCookieName];

      if (cookie != null)
        return JsonConvert.DeserializeObject<AuthCookie>(Encryption.Decrypt(cookie.Value, MainConfig.EncryptionKey));

      return null;
    }

    public static bool Logoff(HttpContextBase ctx)
    {
      HttpCookie cookie = ctx.Request.Cookies[MainConfig.AuroraAuthCookieName];

      if (cookie != null)
      {
        AuthCookie authCookie = GetAuthCookie(ctx);

        List<User> users = GetUsers(ctx);

        User u = GetUsers(ctx).FirstOrDefault(x => x.AuthenticationToken == authCookie.AuthToken);

        if (u != null)
        {
          bool result = users.Remove(u);

          if (result)
          {
            if (ctx.Session[MainConfig.CurrentUserSessionName] != null)
              ctx.Session.Remove(MainConfig.CurrentUserSessionName);

            ctx.User = null;
          }

          ctx.Response.Cookies.Remove(MainConfig.AuroraAuthCookieName);

          return result;
        }
      }

      return false;
    }

    internal static bool IsAuthenticated(HttpContextBase ctx)
    {
      return IsAuthenticated(ctx, null);
    }

    internal static bool IsAuthenticated(HttpContextBase ctx, string authRoles)
    {
      AuthCookie authCookie = GetAuthCookie(ctx);
      
      if (authCookie != null)
      {
        User u = GetUsers(ctx).FirstOrDefault(x => x.SessionID == ctx.Session.SessionID && x.Identity.Name == authCookie.ID);

        //TODO: The User class now has the AuthenticationToken. Let's do an additional check to see if it's not expired.
        //      if it's not then we'll rely on it.
        
        if (u != null)
        {
          if (!string.IsNullOrEmpty(authRoles))
          {
            List<string> minimumRoles = authRoles.Split('|').ToList();

            if (minimumRoles.Intersect(u.Roles).Count() > 0) return true;
          }
          else
            return true;
        }
      }

      return false;
    }
  }
  #endregion

  #region APPLICATION INTERNALS
  internal static class ApplicationInternals
  {
    private static List<Type> GetTypeList(HttpContextBase context, string sessionName, Type t)
    {
      List<Type> types = null;

      if (context.Application[sessionName] != null)
        types = context.Application[sessionName] as List<Type>;
      else
      {
        types = (from assembly in AppDomain.CurrentDomain.GetAssemblies().Where(x => x.GetName().Name != "DotNetOpenAuth")
                 from type in assembly.GetTypes().Where(x => x.BaseType == t)
                 select type).ToList();

        context.Application[sessionName] = types;
      }

      return types;
    }

    public static List<Type> AllControllers(HttpContextBase context)
    {
      return GetTypeList(context, MainConfig.ControllersSessionName, typeof(Controller));
    }

    public static List<Controller> AllControllerInstances(HttpContextBase context)
    {
      List<Controller> controllerInstances = null;

      if (context.Session[MainConfig.ControllerInstancesSessionName] == null)
      {
        controllerInstances = new List<Controller>();
        context.Session[MainConfig.ControllerInstancesSessionName] = controllerInstances;
      }
      else
        controllerInstances = context.Session[MainConfig.ControllerInstancesSessionName] as List<Controller>;

      return controllerInstances;
    }

    public static List<Type> AllModels(HttpContextBase context)
    {
      return GetTypeList(context, MainConfig.ModelsSessionName, typeof(Model));
    }

    public static List<string> AllRoutableActionNames(HttpContextBase context, string controllerName)
    {
      List<string> routableActionNames = new List<string>();

      foreach (Type c in AllControllers(context).Where(x => x.Name == controllerName))
      {
        foreach (MethodInfo mi in c.GetMethods())
        {
          foreach (Attribute a in mi.GetCustomAttributes(false))
          {
            if ((a is HttpGetAttribute) || (a is HttpPostAttribute) || (a is FromRedirectOnlyAttribute))
            {
              HttpGetAttribute get = (a as HttpGetAttribute);
              HttpPostAttribute post = (a as HttpPostAttribute);
              FromRedirectOnlyAttribute fro = (a as FromRedirectOnlyAttribute);

              if ((get != null) || (fro != null) || (post != null))
              {
                routableActionNames.Add(mi.Name);
              }
            }
          }
        }
      }

      if (routableActionNames.Count() > 0) return routableActionNames;

      return null;
    }

    public static List<RouteInfo> AllRouteInfos(HttpContextBase context)
    {
      List<RouteInfo> routes = null;

      if (context.Session[MainConfig.RoutesSessionName] != null)
      {
        // If we have a route list already let's return that
        routes = context.Session[MainConfig.RoutesSessionName] as List<RouteInfo>;
      }
      else
      {
        // Otherwise we'll build an initial route list based on the controllers and actions in the application
        #region BUILD INITIAL ROUTE LIST
        routes = new List<RouteInfo>();

        StringBuilder alias = new StringBuilder();

        foreach (Type c in AllControllers(context))
        {
          #region NEW UP THE CONTROLLER
          Controller ctrl = AllControllerInstances(context).FirstOrDefault(x => x.GetType() == c);

          if (ctrl == null)
          {
            MethodInfo createInstance = c.BaseType.GetMethod("CreateInstance", BindingFlags.Static | BindingFlags.NonPublic).MakeGenericMethod(c);
            ctrl = (Controller)createInstance.Invoke(createInstance, new object[] { context });
            AllControllerInstances(context).Add(ctrl);
          }
          else
            ctrl.Refresh(context);
          #endregion

          foreach (MethodInfo mi in c.GetMethods())
          {
            foreach (Attribute a in mi.GetCustomAttributes(false))
            {
              if ((a is HttpGetAttribute) || (a is HttpPostAttribute) || (a is FromRedirectOnlyAttribute))
              {
                alias.Length = 0;

                HttpGetAttribute get = (a as HttpGetAttribute);
                HttpPostAttribute post = (a as HttpPostAttribute);
                FromRedirectOnlyAttribute fro = (a as FromRedirectOnlyAttribute);

                alias.Insert(0, (a as RequestTypeAttribute).RouteAlias);

                RouteInfo routeInfo = new RouteInfo()
                {
                  RequestType = context.Request.RequestType,
                  Alias = (!String.IsNullOrEmpty(alias.ToString())) ? alias.ToString() : string.Format("/{0}/{1}", c.Name, mi.Name),
                  ControllerName = c.Name,
                  ControllerType = c,
                  ControllerInstance = ctrl,
                  Action = mi,
                  ActionName = mi.Name,
                  Bindings = new ActionBinder(context).GetBindings(c.Name, mi.Name),
                  FromRedirectOnlyInfo = (fro != null) ? true : false,
                  Dynamic = (fro != null) ? true : false,
                  Form = (context.Request.Form == null) ? new NameValueCollection() : new NameValueCollection(context.Request.Form),
                  QueryString = (context.Request.QueryString == null) ? new NameValueCollection() : new NameValueCollection(context.Request.QueryString)
                };

                if (get != null || fro != null)
                {
                  routeInfo.RequestType = "GET";
                  routeInfo.IsFiltered = false;
                }
                else if (post != null)
                {
                  routeInfo.RequestType = "POST";
                  routeInfo.IsFiltered = false;
                }
                else
                {
                  routeInfo.IsFiltered = true;
                }

                if ((routeInfo.Form.Count > 0) && (routeInfo.Form.AllKeys.Contains(MainConfig.AntiForgeryTokenName)))
                  routeInfo.Form.Remove(MainConfig.AntiForgeryTokenName);

                if (routeInfo.RequestType == "POST")
                {
                  routeInfo.PostedFormModel = Model.DetermineModelFromPostedForm(context);

                  if (routeInfo.PostedFormModel != null)
                    routeInfo.PostedFormInfo = new PostedFormInfo(context, routeInfo.PostedFormModel);
                }

                routes.Add(routeInfo);
              }
            }
          }
        }

        context.Application[MainConfig.RoutesSessionName] = routes;
        #endregion
      }

      return routes;
    }

    public static void RemoveRouteInfo(HttpContextBase context, string alias)
    {
      List<RouteInfo> routes = AllRouteInfos(context);

      RouteInfo routeInfo = routes.FirstOrDefault(x => x.Dynamic == true && x.Alias == alias);

      if (routeInfo != null)
        routes.Remove(routeInfo);
    }

    public static void AddRouteInfo(HttpContextBase context, string alias, Controller controller, MethodInfo action, string frontParams)
    {
      List<RouteInfo> routes = AllRouteInfos(context);

      routes.Add(new RouteInfo()
      {
        Alias = alias,
        Action = action,
        ControllerInstance = controller,
        ControllerName = action.DeclaringType.Name,
        FrontLoadedParams = frontParams,
        Dynamic = true
      });
    }

    public static CustomError GetCustomError(HttpContextBase context, Exception e)
    {
      List<Type> customErrors = GetTypeList(context, MainConfig.CustomErrorSessionName, typeof(CustomError));

      if (customErrors.Count > 1)
        throw new Exception(MainConfig.OnlyOneCustomErrorClassPerApplicationError);

      if (customErrors.Count == 1)
        return CustomError.CreateInstance(customErrors[0], context);

      return null;
    }
  }
  #endregion

  #region ACTION BINDER
  public interface IBoundActionObject
  {
    void ExecuteBeforeAction(HttpContextBase ctx);
  }

  internal class BoundAction
  {
    public string ControllerName { get; private set; }
    public string ActionName { get; private set; }
    public List<object> BoundInstances { get; private set; }

    public object[] BoundObjectTypes { get; private set; }
    public object[] BoundObjects { get; private set; }

    public BoundAction(string controllerName, string actionName, object bindInstance)
    {
      ControllerName = controllerName;
      ActionName = actionName;
      BoundInstances = new List<object>();

      AddBinding(bindInstance);
    }

    public void AddBinding(object b)
    {
      object alreadyBound = BoundInstances.FirstOrDefault(x => x == b);

      if (alreadyBound == null)
      {
        BoundInstances.Add(b);

        BoundObjectTypes = BoundInstances.Select(x => x.GetType()).ToArray();
        BoundObjects = BoundInstances.ToArray();
      }
    }
  }

  public class ActionBinder
  {
    private HttpContextBase context;
    internal List<BoundAction> bindings { get; private set; }

    public ActionBinder(HttpContextBase ctx)
    {
      context = ctx;

      if (context.Session[MainConfig.ActionBinderSessionName] != null)
        bindings = context.Session[MainConfig.ActionBinderSessionName] as List<BoundAction>;
      else
      {
        bindings = new List<BoundAction>();
        context.Session[MainConfig.ActionBinderSessionName] = bindings;
      }
    }

    public void AddForAllActions(string controllerName, object[] bindInstances)
    {
      foreach (string actionName in ApplicationInternals.AllRoutableActionNames(context, controllerName))
        foreach (object o in bindInstances)
          Add(controllerName, actionName, o);
    }

    public void Add(string controllerName, string[] actions, object bindInstance)
    {
      foreach (string a in actions)
      {
        Add(controllerName, a, bindInstance);
      }
    }

    public void Add(string controllerName, string[] actions, object[] bindInstances)
    {
      foreach (string a in actions)
        foreach (object o in bindInstances)
          Add(controllerName, a, o);
    }

    public void Add(string controllerName, string action, object[] bindInstances)
    {
      foreach (object o in bindInstances)
        Add(controllerName, action, o);
    }

    public void Add(string controllerName, string actionName, object bindInstance)
    {
      BoundAction ba = bindings.FirstOrDefault(x => x.ControllerName == controllerName && x.ActionName == actionName);

      if (ba == null)
        bindings.Add(new BoundAction(controllerName, actionName, bindInstance));
      else
        ba.AddBinding(bindInstance);
    }

    public void Remove(string controllerName, string actionName, object bindInstance)
    {
      BoundAction ba = bindings.FirstOrDefault(x => x.ControllerName == controllerName && x.ActionName == actionName);

      if (ba != null)
      {
        if (ba.BoundInstances.Contains(bindInstance))
          ba.BoundInstances.Remove(bindInstance);
      }
    }

    internal BoundAction GetBindings(string controllerName, string actionName)
    {
      return bindings.FirstOrDefault(x => x.ControllerName == controllerName && x.ActionName == actionName);
    }
  }
  #endregion

  #region MODEL BASE
  public class Model
  {
    public bool IsValid { get; private set; }

    public string Error { get; private set; }

    public string ToJSON()
    {
      return JsonConvert.SerializeObject(this);
    }

    internal void Validate(HttpContextBase context, Model instance)
    {
      bool isValid = false;

      foreach (PropertyInfo pi in GetPropertiesWithExclusions<Model>(GetType()))
      {
        //FIXME: This validation logic needs to be put into it's own class so I can also validate against parameter fields because
        //       posted forms can be posted to a model or straight to parameters of an action.

        RequiredAttribute requiredAttribute = (RequiredAttribute)pi.GetCustomAttributes(false).FirstOrDefault(x => x is RequiredAttribute);
        RequiredLengthAttribute requiredLengthAttribute = (RequiredLengthAttribute)pi.GetCustomAttributes(false).FirstOrDefault(x => x is RequiredLengthAttribute);
        RegularExpressionAttribute regularExpressionAttribute = (RegularExpressionAttribute)pi.GetCustomAttributes(false).FirstOrDefault(x => x is RegularExpressionAttribute);
        RangeAttribute rangeAttribute = (RangeAttribute)pi.GetCustomAttributes(false).FirstOrDefault(x => x is RangeAttribute);

        object value = pi.GetValue(instance, null);

        if (requiredAttribute != null)
        {
          // Required works great for something like a string where it's default value will be null if it's not set explicitly.
          // we have to get a little bit more smart about this and check the incoming form to see if we have a match for 
          // the property type to determine if the value was actually set in the posted form.
          if (context.Request.Form.AllKeys.FirstOrDefault(x => x == pi.Name) != null)
          {
            // If we pass validation during the form checking then we just check values at that point to see if they aren't null or if
            // the field is a string make sure that it isn't empty.

            if (value.GetType() == typeof(string))
            {
              if (!String.IsNullOrEmpty((string)value))
                isValid = true;
            }
            else if (value != null)
              isValid = true;
            else
            {
              isValid = false;
              Error = string.Format(MainConfig.ModelValidationErrorRequiredField, pi.Name);
            }
          }
        }

        if (value.GetType() == typeof(string))
        {
          if (requiredLengthAttribute != null)
          {
            if (value.GetType() == typeof(string))
            {
              if (((string)value).Length >= requiredLengthAttribute.Length)
                isValid = true;
              else
              {
                isValid = false;
                Error = string.Format(MainConfig.ModelValidationErrorRequiredLength, pi.Name);
              }
            }
          }

          if (regularExpressionAttribute != null)
          {
            if (value.GetType() == typeof(string))
            {
              if (regularExpressionAttribute.Pattern.IsMatch(((string)value)))
                isValid = true;
              else
              {
                isValid = false;
                Error = string.Format(MainConfig.ModelValidationErrorRegularExpression, pi.Name);
              }
            }
          }
        }

        if (value.GetType() == typeof(Int32))
        {
          //FIXME: Any numeric data type should be checked against this but all I'm checking now is integer (Int32)

          if (rangeAttribute != null)
          {
            if (((int)value).InRange(rangeAttribute.Min, rangeAttribute.Max))
              isValid = true;
            else
            {
              isValid = false;
              Error = string.Format(MainConfig.ModelValidationErrorRange, pi.Name);
            }
          }
        }
      }

      instance.IsValid = isValid;
    }

    internal static List<PropertyInfo> GetPropertiesWithExclusions<T>(Type t) where T : Model
    {
      return t.GetProperties().Where(x => x.Name != "IsValid" && x.Name != "Error").ToList();
    }

    internal static Type DetermineModelFromPostedForm(HttpContextBase context)
    {
      string[] formKeys = context.Request.Form.AllKeys.Where(x => x != MainConfig.AntiForgeryTokenName).ToArray();

      if (formKeys.Length > 0)
      {
        foreach (Type m in ApplicationInternals.AllModels(context))
        {
          List<string> props = m.GetProperties().Select(x => x.Name).Where(x => x != "IsValid" && x != "Error").ToList();

          // To support things like checkbox lists or other input elements that may need to have a variable number
          // of instances I'll need to add some logic here and make some assumptions outright about the layout (naming convention)
          // of those elements. For a variable number of input elements we'll enforce a naming policy where the name is consistent
          // and is prepended with a number where the number increments from 1 - x. I'll then have to do a deeper inspection
          // of the form variables to see what we are dealing with and then try to map it to a specific type.

          if (props.Intersect(formKeys).Count() == props.Union(formKeys).Count())
            return m;
        }
      }

      return null;
    }
  }
  #endregion

  #region CONTROLLER
  public abstract class Controller
  {
    public HttpContextBase Context;

    private IViewEngine viewEngine;

    protected Dictionary<string, string> ViewTags;
    protected NameValueCollection Form { get; set; }
    protected NameValueCollection QueryString { get; set; }

    public delegate void Controller_PreOrPostActionHandler();

    // Used in RaiseEvent to determine which event to raise during route determination and execution
    internal enum PreOrPostActionType
    {
      Before,
      After
    }

    public event Controller_PreOrPostActionHandler Controller_BeforeAction;
    public event Controller_PreOrPostActionHandler Controller_AfterAction;

    public Controller()
    {
      ViewTags = new Dictionary<string, string>();

      Controller_AfterAction += new Controller_PreOrPostActionHandler(Controller_AfterActionHandler);
      Controller_BeforeAction += new Controller_PreOrPostActionHandler(Controller_BeforeActionHandler);
    }

    protected virtual void Controller_BeforeActionHandler() { }
    protected virtual void Controller_AfterActionHandler() { }
    protected virtual void Controller_OnInit() { }

    internal void RaiseEvent(PreOrPostActionType type)
    {
      switch (type)
      {
        case PreOrPostActionType.Before:
          Controller_BeforeAction();
          break;

        case PreOrPostActionType.After:
          Controller_AfterAction();
          break;
      }
    }

    internal static Controller CreateInstance<T>(HttpContextBase context) where T : Controller
    {
      Controller controller = (Controller)Activator.CreateInstance(typeof(T));

      controller.Refresh(context);
      controller.Controller_OnInit();

      return controller;
    }

    internal void Refresh(HttpContextBase context)
    {
      Context = context;

      QueryString = (context.Request.QueryString == null) ? new NameValueCollection() : new NameValueCollection(context.Request.QueryString);
      Form = (context.Request.Form == null) ? new NameValueCollection() : new NameValueCollection(context.Request.Form);

      viewEngine = new AuroraViewEngine(context, context.Server.MapPath(MainConfig.ViewRoot));

      if (Form.AllKeys.Contains(MainConfig.AntiForgeryTokenName))
        Form.Remove(MainConfig.AntiForgeryTokenName);
    }

    public void ClearViewTags()
    {
      ViewTags = new Dictionary<string, string>();
    }

    #region ADD / REMOVE ROUTE
    public void AddRoute(string alias, string action)
    {
      AddRoute(alias, action, null);
    }

    public void AddRoute(string alias, string action, string frontParams)
    {
      MethodInfo actionMethod = GetType().GetMethods().FirstOrDefault(x => x.Name == action);

      if (actionMethod != null)
        ApplicationInternals.AddRouteInfo(Context, alias, this, actionMethod, frontParams);
    }

    public void RemoveRoute(string alias)
    {
      ApplicationInternals.RemoveRouteInfo(Context, alias);
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
      Context.Session[MainConfig.FromRedirectOnlySessionFlag] = String.Format("/{0}/{1}", controller, action);

      RedirectToAction(controller, action);
    }

    public void RedirectToAlias(string alias)
    {
      Context.Response.Redirect(alias);
    }

    public void RedirectToAlias(string alias, params string[] parameters)
    {
      Context.Response.Redirect(string.Format("{0}/{1}", alias, String.Join("/", parameters)));
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
      Context.Response.Redirect(string.Format("/{0}/{1}", controller, action));
    }

    public void RedirectToAction(string controller, string action, params string[] parameters)
    {
      Context.Response.Redirect(string.Format("/{0}/{1}/{2}", controller, action, String.Join("/", parameters)));
    }
    #endregion

    #region VIEW
    public ViewResult View()
    {
      StackFrame sf = new StackFrame(1);
      string viewName = sf.GetMethod().Name;
      string className = sf.GetMethod().DeclaringType.Name;

      return View(className, viewName);
    }

    public ViewResult View(string name)
    {
      StackFrame sf = new StackFrame(1);
      string className = sf.GetMethod().DeclaringType.Name;

      return View(className, name);
    }

    public ViewResult View(string controllerName, string actionName)
    {
      ViewResult vr = new ViewResult(Context, viewEngine, controllerName, actionName, ViewTags);

      ClearViewTags();

      return vr;
    }

    public JsonResult View(object jsonData)
    {
      return new JsonResult(Context, jsonData);
    }

    public VirtualFileResult View(string fileName, byte[] bytes, string contentType)
    {
      return new VirtualFileResult(Context, fileName, bytes, contentType);
    }

    public FragmentResult Fragment(string fragmentName)
    {
      return new FragmentResult(Context, viewEngine, fragmentName, ViewTags);
    }

    public string RenderFragment(string fragmentName)
    {
      return RenderFragment(fragmentName, null);
    }

    public string RenderFragment(string fragmentName, Dictionary<string, string> tags)
    {
      viewEngine.LoadView(fragmentName, tags);

      return viewEngine[fragmentName];
    }
    #endregion
  }
  #endregion

  #region ENCRYPTION
  public class Encryption
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

  #region STRING CONVERSION
  internal static class StringConversion
  {
    public static object[] ToObjectArray(string[] parms)
    {
      if (parms != null)
      {
        object[] _parms = new object[parms.Length];

        for (int i = 0; i < parms.Length; i++)
        {
          if (parms[i].IsInt32())
          {
            _parms[i] = Convert.ToInt32(parms[i]);
          }
          else if (parms[i].IsLong())
          {
            _parms[i] = Convert.ToInt64(parms[i]);
          }
          else if (parms[i].IsDouble())
          {
            _parms[i] = Convert.ToDouble(parms[i]);
          }
          else
            _parms[i] = parms[i];
        }

        return _parms;
      }

      return null;
    }
  }
  #endregion

  #region EXTENSION & MISCELLANEOUS METHODS
  public static class ExtensionMethods
  {
    // This method is based on the following example at StackOverflow:
    //
    // http://stackoverflow.com/questions/735350/how-to-get-a-users-client-ip-address-in-asp-net
    public static string IPAddress(this HttpContextBase context)
    {
      string ip = context.Request.ServerVariables["HTTP_X_FORWARDED_FOR"];

      if (string.IsNullOrEmpty(ip))
        return context.Request.ServerVariables["REMOTE_ADDR"];
      else
        return ip.Split(',')[0];
    }

    public static string NewLinesToBr(this string s)
    {
      return s.Replace("\n", "<br />");
    }

    public static string StripHTML(this string s)
    {
      HtmlDocument htmlDoc = new HtmlDocument();
      htmlDoc.LoadHtml(s);

      if (htmlDoc == null) return s;

      StringBuilder sanitizedString = new StringBuilder();

      foreach (var node in htmlDoc.DocumentNode.ChildNodes)
      {
        sanitizedString.Append(node.InnerText);
      }

      return sanitizedString.ToString();
    }

    public static bool InRange(this int value, int min, int max)
    {
      return value <= max && value >= min;
    }

    public static string ToJSON<T>(this List<T> t) where T : Model
    {
      return JsonConvert.SerializeObject(t);
    }

    public static bool IsDate(this string s)
    {
      DateTime x;

      return DateTime.TryParse(s, out x);
    }

    public static bool IsLong(this string s)
    {
      long x = 0;

      return long.TryParse(s, out x);
    }

    public static bool IsInt32(this string s)
    {
      int x = 0;

      return int.TryParse(s, out x);
    }

    public static bool IsDouble(this string s)
    {
      double x = 0;

      return double.TryParse(s, out x);
    }

    public static bool IsBool(this string s)
    {
      bool x = false;

      return bool.TryParse(s, out x);
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

    public static string GetMetaData(this Enum e)
    {
      MetaDataAttribute mda = (MetaDataAttribute)e.GetType().GetField(e.ToString()).GetCustomAttributes(false).FirstOrDefault(x => x is MetaDataAttribute);

      if (mda != null)
        return mda.MetaData;

      return null;
    }

    public static string GetUniqueID(this Enum e)
    {
      UniqueIDAttribute uid = (UniqueIDAttribute)e.GetType().GetField(e.ToString()).GetCustomAttributes(false).FirstOrDefault(x => x is UniqueIDAttribute);

      if (uid != null)
      {
        uid.GenerateID(string.Format("{0}.{1}", e.GetType().Name, e.ToString()));

        return uid.ID;
      }

      return null;
    }
  }
  #endregion

  #region ROUTE MANAGER
  internal interface IRouteManager
  {
    IAuroraResult HandleRoute();
    void Refresh(HttpContextBase ctx);
  }

  internal class RouteInfo
  {
    public Type ControllerType { get; set; }
    public Controller ControllerInstance { get; set; }
    public MethodInfo Action { get; set; }
    public BoundAction Bindings { get; set; }

    public NameValueCollection QueryString { get; set; }
    public NameValueCollection Form { get; set; }

    public string RequestType { get; set; }
    public string Alias { get; set; }
    public string ControllerName { get; set; }
    public string ActionName { get; set; }
    public string Path { get; set; }
    public string FrontLoadedParams { get; set; }

    public object[] UrlObjectParameters { get; set; }
    public string[] UrlStringParameters { get; set; }

    public object[] ActionParameters { get; set; }

    public bool IsFiltered { get; set; }

    public PostedFormInfo PostedFormInfo { get; set; }
    public Type PostedFormModel { get; set; }

    public bool Dynamic { get; set; }

    public bool FromRedirectOnlyInfo { get; set; }
  }

  internal class PostedFormInfo
  {
    private HttpContextBase context;
    private string DataTypeName;

    public Type DataType { get; private set; }
    public object DataTypeInstance { get; private set; }

    private void FormProcessor(Type m)
    {
      if (m != null)
      {
        DataTypeName = m.Name;

        PropertyInfo[] props = m.GetProperties();

        if (props.Count() == 0)
          throw new Exception(MainConfig.PostedFormActionIncorrectNumberOfParametersError);

        DataType = m;
        DataTypeInstance = Activator.CreateInstance(DataType);

        foreach (PropertyInfo p in Model.GetPropertiesWithExclusions<Model>(m))
        {
          // We need to convert the form value to a datatype 

          if (p.PropertyType == typeof(int))
          {
            if (context.Request.Form[p.Name].IsInt32())
              p.SetValue(DataTypeInstance, Convert.ToInt32(context.Request.Form[p.Name]), null);
          }
          else if (p.PropertyType == typeof(string))
          {
            p.SetValue(DataTypeInstance, context.Request.Form[p.Name], null);
          }
          else if (p.PropertyType == typeof(bool))
          {
            if (context.Request.Form[p.Name].IsBool())
              p.SetValue(DataTypeInstance, Convert.ToBoolean(context.Request.Form[p.Name]), null);
          }
          else if (p.PropertyType == typeof(DateTime?))
          {
            DateTime? d = (context.Request.Form[p.Name].IsDate()) ? (DateTime?)DateTime.Parse(context.Request.Form[p.Name]) : null;

            p.SetValue(DataTypeInstance, d, null);
          }
          else if (p.PropertyType == typeof(List<HttpPostedFileBase>))
          {
            List<HttpPostedFileBase> fileList = (List<HttpPostedFileBase>)Activator.CreateInstance(p.PropertyType);

            foreach (string file in context.Request.Files)
            {
              fileList.Add(context.Request.Files[file] as HttpPostedFileBase);
            }

            p.SetValue(DataTypeInstance, fileList, null);
          }
          else if (p.PropertyType == typeof(HttpPostedFile))
          {
            if (context.Request.Files.Count > 0)
              p.SetValue(DataTypeInstance, context.Request.Files[0], null);
          }
        }
      }
    }

    public PostedFormInfo(HttpContextBase ctx, Type m)
    {
      context = ctx;

      FormProcessor(m);
    }
  }

  internal class RouteManager : IRouteManager
  {
    private HttpContextBase context;
    private string path;
    private string alias;

    private string[] urlStringParams;
    private object[] urlObjectParams;

    private PostedFormInfo postedFormInfo;
    private Type postedFormModel;

    private bool fromRedirectOnlyFlag = false;

    private List<RouteInfo> Routes { get; set; }

    public RouteManager(HttpContextBase ctx)
    {
      Routes = new List<RouteInfo>();

      Refresh(ctx);
    }

    public void Refresh(HttpContextBase ctx)
    {
      context = ctx;

      context.Request.ValidateInput();
      string rawURL = context.Request.RawUrl;

      string incomingPath = context.Request.Path;

      if (string.Equals(incomingPath, "/default.aspx", StringComparison.InvariantCultureIgnoreCase) || incomingPath == "~/")
        path = MainConfig.DefaultRoute;
      else
        path = (context.Request.Path.EndsWith("/")) ? context.Request.Path.Remove(context.Request.Path.Length - 1) : context.Request.Path;

      if (MainConfig.ApplicationMountPoint.Length > 0)
        path = path.Replace(MainConfig.ApplicationMountPoint, string.Empty);

      context.RewritePath(path);

      Routes = ApplicationInternals.AllRouteInfos(context);

      alias = Routes.Where(x => path.StartsWith(x.Alias)).Select(x => x.Alias).FirstOrDefault();

      if (!String.IsNullOrEmpty(alias))
      {
        urlStringParams = path.Replace(alias, string.Empty).Split('/').Where(x => !string.IsNullOrEmpty(x)).ToArray();

        if (urlStringParams != null)
          urlObjectParams = StringConversion.ToObjectArray(urlStringParams);

        postedFormModel = Model.DetermineModelFromPostedForm(context);

        if (postedFormModel != null)
          postedFormInfo = new PostedFormInfo(context, postedFormModel);
      }
    }

    private RouteInfo FindRoute(string path)
    {
      //
      // Actions map like this: ViewResult ActionName(front params, bound_parameters, url_parameters, form_parameters)
      //

      if (!MainConfig.PathTokenRE.IsMatch(path)) return null;

      object[] actionParameters = null;
      object[] formParams = null;

      int urlParamLength = (urlObjectParams != null) ? urlObjectParams.Length : 0;

      if (context.Request.Form.Count > 0)
      {
        if (postedFormModel != null)
        {
          if (postedFormInfo.DataType != null)
          {
            ((Model)postedFormInfo.DataTypeInstance).Validate(context, (Model)postedFormInfo.DataTypeInstance);
          }
        }
      }

      foreach (RouteInfo routeInfo in Routes.FindAll(x => x.Alias == alias && x.RequestType == context.Request.RequestType))
      {
        if (fromRedirectOnlyFlag && !routeInfo.FromRedirectOnlyInfo) continue;

        #region DETERMINE THE PARAM LAYOUT
        int boundParamLength = (routeInfo.Bindings != null) ? routeInfo.Bindings.BoundInstances.Count() : 0;
        int totalParamLength = boundParamLength + urlParamLength;

        if (routeInfo.Action.GetParameters().Count() < totalParamLength) continue;
        
        if ((postedFormModel == null) && (context.Request.Form.Count > 0))
        {
          string[] formValues = new string[routeInfo.Form.AllKeys.Length];

          for (int i = 0; i < routeInfo.Form.AllKeys.Length; i++)
          {
            formValues[i] = routeInfo.Form.Get(i);
          }

          formParams = StringConversion.ToObjectArray(formValues);

          totalParamLength += formParams.Length;
        }
        else if (postedFormInfo != null && postedFormInfo.DataType != null)
        {
          totalParamLength++;
        }
        #endregion

        #region SETS UP FINAL ARRAY OF PARAMS
        actionParameters = new object[totalParamLength + context.Request.Files.Count];

        if (boundParamLength > 0)
          routeInfo.Bindings.BoundInstances.CopyTo(actionParameters, 0);

        if (postedFormModel == null)
        {
          if (context.Request.Form.Count > 0)
            formParams.CopyTo(actionParameters, boundParamLength);

          urlObjectParams.CopyTo(actionParameters, boundParamLength);
        }
        else
        {
          new object[] { postedFormInfo.DataTypeInstance }.CopyTo(actionParameters, boundParamLength);
        }

        if (context.Request.Files.Count > 0)
          context.Request.Files.CopyTo(actionParameters, totalParamLength);
        #endregion

        #region FIGURE OUT WHICH ACTION GOES WITH THIS ROUTE
        Type[] types = actionParameters.Select(x => x.GetType()).ToArray();

        var methodParams = routeInfo.Action.GetParameters().Select(x => x.ParameterType);

        var matches = methodParams.Where(p => (types.FirstOrDefault(t => (t.GetInterfaces().Where(x => x.Name == p.Name).FirstOrDefault() != null) || t == p)) != null);

        if (matches.Count() == methodParams.Count())
        {
          routeInfo.ActionParameters = actionParameters;

          routeInfo.Path = path;
          routeInfo.UrlStringParameters = urlStringParams;
          routeInfo.UrlObjectParameters = urlObjectParams;

          if (!String.IsNullOrEmpty(routeInfo.FrontLoadedParams))
          {
            object[] convertedFrontParams = StringConversion.ToObjectArray(routeInfo.FrontLoadedParams.Split('/'));

            routeInfo.UrlObjectParameters = convertedFrontParams.Concat(routeInfo.UrlObjectParameters).ToArray();
          }

          Routes.Add(routeInfo);

          return routeInfo;
        }
        #endregion
      }

      return null;
    }

    public IAuroraResult HandleRoute()
    {
      IAuroraResult iar = null;

      if (MainConfig.PathStaticFileRE.IsMatch(path))
      {
        iar = ExecuteStaticRoute();
      }
      else
      {
        RouteInfo routeInfo = FindRoute(path);

        if (routeInfo != null)
        {
          // Execute Controller_BeforeAction
          routeInfo.ControllerInstance.RaiseEvent(Controller.PreOrPostActionType.Before);

          switch (context.Request.RequestType)
          {
            case "GET":
              iar = ProcessGetRoute(routeInfo);
              break;

            case "POST":
              iar = ProcessPostRoute(routeInfo);
              break;
          }

          // Execute Controller_AfterAction
          routeInfo.ControllerInstance.RaiseEvent(Controller.PreOrPostActionType.After);
        }
      }

      return iar;
    }

    private IAuroraResult ExecuteStaticRoute()
    {
      IAuroraResult iar = null;

      if (path.StartsWith("/" + MainConfig.PublicResourcesFolderName) || path.EndsWith(".ico"))
      {
        if (MainConfig.PathStaticFileRE.IsMatch(path))
        {
          string staticFilePath = context.Server.MapPath(path);

          if (File.Exists(staticFilePath))
            return new PhysicalFileResult(context, staticFilePath);
        }
      }

      return iar;
    }

    private IAuroraResult ProcessGetRoute(RouteInfo routeInfo)
    {
      IAuroraResult result = null;

      if (routeInfo != null && !routeInfo.IsFiltered)
      {
        HttpGetAttribute get = Attribute.GetCustomAttribute(routeInfo.Action, typeof(HttpGetAttribute), false) as HttpGetAttribute;

        if (get != null)
        {
          if (get.HttpsOnly && !context.Request.IsSecureConnection) return result;

          #region HTTP CACHING
          if (get.Cache)
          {
            TimeSpan expires = new TimeSpan(0, 0, get.Duration);

            context.Response.Cache.SetCacheability(get.CacheabilityOption);
            context.Response.Cache.SetExpires(DateTime.Now.Add(expires));
            context.Response.Cache.SetMaxAge(expires);
            context.Response.Cache.SetValidUntilExpires(true);
            context.Response.Cache.VaryByParams.IgnoreParams = true;
          }
          else
          {
            context.Response.Cache.SetCacheability(HttpCacheability.NoCache);
            context.Response.Cache.SetNoStore();
            context.Response.Cache.SetExpires(DateTime.MinValue);
          }
          #endregion

          if (get.SecurityType == ActionSecurity.Secure)
          {
            if (!SecurityManager.IsAuthenticated(context, get.Roles))
            {
              if (!string.IsNullOrEmpty(get.RedirectWithoutAuthorizationTo))
                return new RedirectResult(context, get.RedirectWithoutAuthorizationTo);
              else
                throw new Exception(MainConfig.Http404Error);
            }
          }
        }

        //TODO: All the places I'm returning an Http 404 can be fixed up to return a better error.
        //      Need to add code to check the web.config custom errors section (specifically the mode).

        if (routeInfo.Action.ReturnType == typeof(JsonResult))
        {
          // make sure the query string has a antiforgery token
          if (context.Request.QueryString[MainConfig.AntiForgeryTokenName] != null)
          {
            // if so, check that it's valid and if it's not return a Http 404
            if (!AntiForgeryToken.VerifyToken(context))
              throw new Exception(MainConfig.AntiForgeryTokenMissing);
          }
        }

        result = ExecuteAction(routeInfo);
      }

      return result;
    }

    private IAuroraResult ProcessPostRoute(RouteInfo routeInfo)
    {
      IAuroraResult result = null;

      if (routeInfo != null && !routeInfo.IsFiltered)
      {
        if (context.Request.Form[MainConfig.AntiForgeryTokenName] != null)
        {
          // if so, check that it's valid and if it's not return a Http 404
          if (!AntiForgeryToken.VerifyToken(context))
            throw new Exception(MainConfig.AntiForgeryTokenVerificationFailed);
        }
        else
          throw new Exception(MainConfig.AntiForgeryTokenMissing);

        HttpPostAttribute post = Attribute.GetCustomAttribute(routeInfo.Action, typeof(HttpPostAttribute), false) as HttpPostAttribute;

        if (post.HttpsOnly && !context.Request.IsSecureConnection) return result;

        if (post.SecurityType == ActionSecurity.Secure)
        {
          if (!SecurityManager.IsAuthenticated(context, post.Roles))
          {
            if (!string.IsNullOrEmpty(post.RedirectWithoutAuthorizationTo))
              return new RedirectResult(context, post.RedirectWithoutAuthorizationTo);
            else
              throw new Exception(MainConfig.RedirectWithoutAuthorizationToError);
          }
        }

        result = ExecuteAction(routeInfo);
      }

      return result;
    }

    private IAuroraResult ExecuteAction(RouteInfo routeInfo)
    {
      if (routeInfo != null)
      {
        IAuroraResult result = null;

        if (routeInfo.Bindings != null)
        {
          foreach (object i in routeInfo.Bindings.BoundInstances)
          {
            if (i.GetType().GetInterface("IBoundActionObject") != null)
            {
              MethodInfo boundActionObject = i.GetType().GetMethod("ExecuteBeforeAction");

              if (boundActionObject != null)
                boundActionObject.Invoke(i, new object[] { context });
            }
          }
        }

        if (!routeInfo.IsFiltered)
        {
          if (routeInfo.Dynamic && context.Session[MainConfig.FromRedirectOnlySessionFlag] == null)
            return null;

          object _result = routeInfo.Action.Invoke(routeInfo.ControllerInstance, routeInfo.ActionParameters);

          if (context.Session[MainConfig.FromRedirectOnlySessionFlag] != null)
            context.Session.Remove(MainConfig.FromRedirectOnlySessionFlag);

          if (routeInfo.Action.ReturnType.GetInterfaces().Contains(typeof(IAuroraResult)))
            result = (IAuroraResult)_result;
          else
            result = new VoidResult();
        }

        return result;
      }

      return null;
    }
  }
  #endregion

  #region ENGINE
  public sealed class AuroraEngine
  {
    private HttpContextBase context;
    private IRouteManager routeManager;

    public AuroraEngine(HttpContextBase ctx)
    {
      context = ctx;

      if ((!MainConfig.Debug) && (context.Application[MainConfig.RouteManagerSessionName] != null))
      {
        routeManager = (IRouteManager)context.Application[MainConfig.RouteManagerSessionName];
        routeManager.Refresh(ctx);
      }
      else
      {
        routeManager = new RouteManager(ctx);
        context.Application[MainConfig.RouteManagerSessionName] = routeManager;
      }

      HttpCookie authCookie = context.Request.Cookies[MainConfig.AuroraAuthCookieName];

      if (authCookie != null)
      {
        if (context.Session[MainConfig.CurrentUserSessionName] != null)
        {
          context.User = context.Session[MainConfig.CurrentUserSessionName] as User;
        }
      }

      try
      {
        IAuroraResult result = routeManager.HandleRoute();

        if (result == null) throw new Exception(MainConfig.Http404Error);

        result.Render();
      }
      catch (Exception e)
      {
        if (e is ThreadAbortException) throw;

        //if (MainConfig.CustomErrorsSection.Mode != CustomErrorsMode.Off &&
        //    MainConfig.CustomErrorsSection.Mode != CustomErrorsMode.RemoteOnly)
        //{
        // Check to see if there is a derived CustomError class otherwise look to see if there is a cusom error method on a controller
        CustomError customError = ApplicationInternals.GetCustomError(context, e);

        if (customError == null)
        {
          RenderError(e);
        }
        else
        {
          // The custom error class is for all controllers and all static content that may produce an error.
          customError.Error(e.Message, e).Render();
          context.Server.ClearError();
        }
        //}
      }
    }

    private void RenderError(Exception e)
    {
      Render(new ErrorResult(context, e));
    }

    private void Render(IAuroraResult result)
    {
      result.Render();
    }
  }
  #endregion

  #region CUSTOM ERROR
  public abstract class CustomError
  {
    public Exception Exception { get; private set; }

    public HttpContextBase Context { get; private set; }

    public readonly Dictionary<string, string> ViewTags;

    public CustomError()
    {
      ViewTags = new Dictionary<string, string>();
    }

    internal static CustomError CreateInstance(Type t, HttpContextBase context)
    {
      if (t.BaseType == typeof(CustomError))
      {
        CustomError ce = (CustomError)Activator.CreateInstance(t);

        ce.Context = context;

        return ce;
      }

      return null;
    }

    //public static void HandleError(HttpContextBase context, Exception e)
    //{
    //  CustomError customError = ApplicationInternals.GetCustomError(context, e);

    //  if (customError != null)
    //  {
    //    customError.Exception = context.Server.GetLastError();
    //    customError.Error(null, customError.Exception);
    //  }

    //  throw new NullReferenceException(MainConfig.CustomErrorNullExceptionError);
    //}

    public virtual ViewResult Error(string message, Exception e)
    {
      return View();
    }

    public ViewResult View()
    {
      return View(MainConfig.SharedFolderName, "Error");
    }

    internal ViewResult View(string controller, string name)
    {
      return new ViewResult(Context, new AuroraViewEngine(Context, Context.Server.MapPath(MainConfig.ViewRoot)), controller, name, ViewTags);
    }
  }
  #endregion

  #region HTML HELPERS
  internal enum ColumnTransformType
  {
    New,
    Existing
  }

  public enum HTMLInputType
  {
    [MetaData("<input type=\"button\" {0} />")]
    Button,

    [MetaData("<input type=\"checkbox\" {0} />")]
    Checkbox,

    [MetaData("<input type=\"file\" {0} />")]
    File,

    [MetaData("<input type=\"hidden\" {0} />")]
    Hidden,

    [MetaData("<input type=\"image\" {0} />")]
    Image,

    [MetaData("<input type=\"password\" {0} />")]
    Password,

    [MetaData("<input type=\"radio\" {0} />")]
    Radio,

    [MetaData("<input type=\"reset\" {0} />")]
    Reset,

    [MetaData("<input type=\"submit\" {0} />")]
    Submit,

    [MetaData("<input type=\"text\" {0} />")]
    Text,

    [MetaData("<textarea {0}>{1}</textarea>")]
    TextArea
  }

  public enum HTMLFormPostMethod
  {
    Get,
    Post
  }

  //TODO: All of the areas where I'm using these Func<> lambda params stuff to add name=value pairs to HTML tags need to have...
  //      complimentary methods that also use a dictionary.

  public abstract class HTMLBase
  {
    protected Func<string, string>[] Attribs;

    protected string CondenseAttribs()
    {
      return (Attribs != null) ? GetParams(Attribs) : string.Empty;
    }

    protected string GetParams(params Func<string, string>[] x)
    {
      StringBuilder sb = new StringBuilder();

      foreach (Func<string, string> f in x)
      {
        Type lambdaType = f.GetType();

        string identifier = (f.Method.GetParameters()[0].Name == "@class") ? "class" : f.Method.GetParameters()[0].Name;

        sb.AppendFormat("{0}=\"{1}\" ", identifier, f(null));
      }

      return sb.ToString().Trim();
    }
  }

  public class ColumnTransform<T> where T : Model
  {
    private List<T> Models;
    private Func<T, string> Func;
    private PropertyInfo ColumnInfo;
    internal ColumnTransformType TransformType { get; private set; }

    public string ColumnName { get; private set; }

    public ColumnTransform(List<T> models, string columnName, Func<T, string> func)
    {
      Models = models;
      Func = func;
      ColumnName = columnName;
      ColumnInfo = typeof(T).GetProperties().FirstOrDefault(x => x.Name == ColumnName);

      if (ColumnInfo != null)
        TransformType = ColumnTransformType.Existing;
      else
        TransformType = ColumnTransformType.New;
    }

    public string Result(int index)
    {
      return Func(Models[index]);
    }

    public IEnumerable<string> Results()
    {
      foreach (T t in Models)
      {
        yield return Func(t);
      }
    }
  }

  public class HTMLTable<T> : HTMLBase where T : Model
  {
    private List<T> Models;
    private List<string> PropertyNames;
    private List<PropertyInfo> PropertyInfos;
    private List<string> IgnoreColumns;
    private List<ColumnTransform<T>> ColumnTransforms;

    public HTMLTable(List<T> models, List<string> ignoreColumns, List<ColumnTransform<T>> columnTransforms, params Func<string, string>[] attribs)
    {
      Init(models, ignoreColumns, columnTransforms, attribs);
    }

    private void Init(List<T> models, List<string> ignoreColumns,
        List<ColumnTransform<T>> columnTransforms, params Func<string, string>[] attribs)
    {
      Models = models;

      IgnoreColumns = ignoreColumns;
      Attribs = attribs;
      ColumnTransforms = columnTransforms;

      PropertyNames = ObtainPropertyNames();
    }

    private List<string> ObtainPropertyNames()
    {
      PropertyNames = new List<string>();
      List<string> hasDescriptiveNames = new List<string>();

      if (Models.Count() > 0)
      {
        PropertyInfos = Model.GetPropertiesWithExclusions<Model>(Models[0].GetType());

        foreach (PropertyInfo p in PropertyInfos)
        {
          DescriptiveNameAttribute pn = (DescriptiveNameAttribute)p.GetCustomAttributes(typeof(DescriptiveNameAttribute), false).FirstOrDefault();

          if ((IgnoreColumns != null) && IgnoreColumns.Contains(p.Name)) continue;

          if (pn != null)
          {
            PropertyNames.Add(pn.Name);

            hasDescriptiveNames.Add(p.Name);
          }
          else
            PropertyNames.Add(p.Name);
        }

        if (ColumnTransforms != null)
        {
          foreach (ColumnTransform<T> addColumn in ColumnTransforms)
          {
            if ((!PropertyNames.Contains(addColumn.ColumnName)) && (!hasDescriptiveNames.Contains(addColumn.ColumnName)))
              PropertyNames.Add(addColumn.ColumnName);
          }
        }

        if (PropertyNames.Count > 0)
          return PropertyNames;
      }

      return null;
    }

    public string ToString(int start, int length)
    {
      if (start > Models.Count() ||
          start < 0 ||
          (length - start) > Models.Count() ||
          (length - start) < 0)
      {
        throw new ArgumentOutOfRangeException();
      }

      StringBuilder html = new StringBuilder();

      html.AppendFormat("<table {0}>", (Attribs != null) ? GetParams(Attribs) : string.Empty);

      html.Append("<thead><tr>");

      foreach (string pn in PropertyNames)
        html.AppendFormat("<th>{0}</th>", pn);

      html.Append("</tr></thead><tbody>");

      for (int i = start; i < length; i++)
      {
        html.Append("<tr>");

        foreach (PropertyInfo pn in PropertyInfos)
        {
          if ((IgnoreColumns != null) && IgnoreColumns.Contains(pn.Name)) continue;

          if (pn.CanRead)
          {
            object o = pn.GetValue(Models[i], null);

            string value = (o == null) ? "NULL" : o.ToString();

            if (ColumnTransforms != null)
            {
              ColumnTransform<T> transform = (ColumnTransform<T>)ColumnTransforms.FirstOrDefault(x => x.ColumnName == pn.Name && x.TransformType == ColumnTransformType.Existing);

              if (transform != null)
              {
                value = transform.Result(i);
              }
            }

            html.AppendFormat("<td>{0}</td>", value);
          }
        }

        if (ColumnTransforms != null)
        {
          foreach (ColumnTransform<T> ct in ColumnTransforms.Where(x => x.TransformType == ColumnTransformType.New))
          {
            html.AppendFormat("<td>{0}</td>", ct.Result(i));
          }
        }

        html.Append("</tr>");
      }

      html.Append("</tbody></table>");

      return html.ToString();
    }

    public override string ToString()
    {
      return ToString(0, Models.Count());
    }
  }

  public class HTMLAnchor : HTMLBase
  {
    private string Url;
    private string Description;

    public HTMLAnchor(string url, string description, params Func<string, string>[] attribs)
    {
      Url = url;
      Description = description;
      Attribs = attribs;
    }

    public override string ToString()
    {
      return string.Format("<a {0} href=\"{1}\">{2}</a>", (Attribs != null) ? GetParams(Attribs) : string.Empty, Url, Description);
    }
  }

  public class HTMLInput : HTMLBase
  {
    private HTMLInputType InputType;

    public HTMLInput(HTMLInputType type, params Func<string, string>[] attribs)
    {
      Attribs = attribs;
      InputType = type;
    }

    public override string ToString()
    {
      if (InputType == HTMLInputType.TextArea)
        return string.Format(InputType.GetMetaData(), CondenseAttribs(), string.Empty);

      return string.Format(InputType.GetMetaData(), CondenseAttribs());
    }

    public string ToString(string text)
    {
      if (InputType == HTMLInputType.TextArea)
        return string.Format(InputType.GetMetaData(), CondenseAttribs(), text);

      return string.Format(InputType.GetMetaData(), CondenseAttribs());
    }
  }

  public class HTMLForm : HTMLBase
  {
    private List<string> InputTags;

    public HTMLForm(string action, HTMLFormPostMethod method, List<string> inputTags, params Func<string, string>[] attribs)
    {
      Attribs = attribs;
      InputTags = inputTags;
    }

    public override string ToString()
    {
      StringBuilder sb = new StringBuilder();

      sb.AppendFormat("<form {0}>", CondenseAttribs());

      foreach (string i in InputTags)
      {
        sb.Append(i);
      }

      sb.Append("</form>");

      return sb.ToString();
    }
  }

  public class HTMLSpan : HTMLBase
  {
    private string Contents;

    public HTMLSpan(string contents, params Func<string, string>[] attribs)
    {
      Contents = contents;
      Attribs = attribs;
    }

    public override string ToString()
    {
      StringBuilder sb = new StringBuilder();

      sb.AppendFormat("<span {0}>{1}</span>", CondenseAttribs(), Contents);

      return sb.ToString();
    }
  }

  public class HTMLSelect : HTMLBase
  {
    private List<string> Options;

    public HTMLSelect(List<string> options, params Func<string, string>[] attribs)
    {
      Options = options;
      Attribs = attribs;
    }

    public override string ToString()
    {
      StringBuilder sb = new StringBuilder();

      sb.AppendFormat("<select {0}>", CondenseAttribs());

      sb.Append("<option selected=\"selected\"></option>");

      int count = 0;

      foreach (string o in Options)
      {
        sb.AppendFormat("<option name=\"opt{0}\">{1}</option>", count, o);
        count++;
      }

      sb.Append("</select>");

      return sb.ToString();
    }
  }

  public class HTMLImage : HTMLBase
  {
    public string Src { get; set; }

    public HTMLImage(string src) : this(src, null) { }

    public HTMLImage(string src, params Func<string, string>[] attribs)
    {
      Src = src;
      Attribs = attribs;
    }

    public override string ToString()
    {
      return string.Format("<img src=\"{0}\" {1}/>", Src, CondenseAttribs());
    }
  }
  #endregion

  #region VIEW RESULTS
  public interface IAuroraResult
  {
    void Render();
  }

  public class VirtualFileResult : IAuroraResult
  {
    private HttpContextBase context;
    private string fileName;
    private string fileContentType;
    private byte[] fileBytes;

    public VirtualFileResult(HttpContextBase ctx, string name, byte[] bytes, string contentType)
    {
      context = ctx;
      fileName = name;
      fileBytes = bytes;
      fileContentType = contentType;
    }

    public void Render()
    {
      context.Response.ClearContent();
      context.Response.ClearHeaders();
      context.Response.ContentType = fileContentType;
      context.Response.AddHeader("content-disposition", "attachment;filename=" + fileName);
      context.Response.AddHeader("Content-Length", fileBytes.Length.ToString());
      context.Response.BinaryWrite(fileBytes);
      context.Response.Flush();
      context.Response.End();
    }
  }

  public class PhysicalFileResult : IAuroraResult
  {
    private HttpContextBase context;
    private string filePath;
    private string contentType;

    public PhysicalFileResult(HttpContextBase ctx, string file)
    {
      context = ctx;
      filePath = file;
      contentType = string.Empty;

      // This is really lame, there has to be a better way to do this!
      if (file.EndsWith(".css"))
        contentType = "text/css";
      else if (file.EndsWith(".js"))
        contentType = "application/x-javascript";
      else if (file.EndsWith(".jpg"))
        contentType = "image/jpg";
      else if (file.EndsWith(".ico"))
        contentType = "image/x-icon";
      else if (file.EndsWith(".txt"))
        contentType = "text/plain";
    }

    public void Render()
    {
      int minutesBeforeExpiration = MainConfig.StaticContentCacheExpiry;

      if (!(minutesBeforeExpiration > 0))
        minutesBeforeExpiration = 15;

      TimeSpan expiry = new TimeSpan(0, minutesBeforeExpiration, 0);

      context.Response.Cache.SetCacheability(HttpCacheability.Public);
      context.Response.Cache.SetExpires(DateTime.Now.Add(expiry));
      context.Response.Cache.SetMaxAge(expiry);
      context.Response.Cache.SetValidUntilExpires(true);
      context.Response.Cache.VaryByParams.IgnoreParams = true;

      context.Response.ContentType = contentType;
      context.Response.TransmitFile(filePath);
    }
  }

  public class ViewResult : IAuroraResult
  {
    private HttpContextBase context;
    private IViewEngine viewEngine;
    private string viewKeyName;

    public ViewResult(HttpContextBase ctx, IViewEngine ve, string controllerName, string viewName, Dictionary<string, string> tags)
    {
      context = ctx;
      viewEngine = ve;
      viewKeyName = string.Format("{0}/{1}", controllerName, viewName);

      ve.LoadView(controllerName, viewName, tags);
    }

    public void Render()
    {
      context.Response.ContentType = "text/html";
      context.Response.Write(viewEngine[viewKeyName]);
    }
  }

  public class FragmentResult : IAuroraResult
  {
    private HttpContextBase context;
    private IViewEngine viewEngine;
    private string viewKeyName;

    public FragmentResult(HttpContextBase ctx, IViewEngine ve, string fragmentName, Dictionary<string, string> tags)
    {
      context = ctx;
      viewEngine = ve;
      viewKeyName = fragmentName;

      ve.LoadView(fragmentName, tags);
    }

    public void Render()
    {
      try
      {
        context.Response.ContentType = "text/html";
        context.Response.Write(viewEngine[viewKeyName]);
      }
      catch
      {
        throw;
      }
    }
  }

  public class JsonResult : IAuroraResult
  {
    private HttpContextBase context;
    private object data;

    public JsonResult(HttpContextBase ctx, object d)
    {
      context = ctx;
      data = d;
    }

    public void Render()
    {
      try
      {
        string json = JsonConvert.SerializeObject(data);

        context.Response.ContentType = "text/html";
        context.Response.Write(json);
      }
      catch
      {
        throw;
      }
    }
  }

  public class ErrorResult : IAuroraResult
  {
    private HttpContextBase context;
    private Exception exception;

    public ErrorResult(HttpContextBase ctx, Exception e)
    {
      context = ctx;
      exception = e;
    }

    public void Render()
    {
      string message = string.Empty;

      if (exception.InnerException == null)
        message = exception.Message;
      else
        message = exception.InnerException.Message;

      context.Response.StatusCode = (int)System.Net.HttpStatusCode.NotFound;
      context.Response.StatusDescription = message;
      context.Response.Write(string.Format("{0} - {1}", message, context.Request.Path));
      context.Response.End();
    }
  }

  internal class VoidResult : IAuroraResult
  {
    public void Render() { }
  }

  public class RedirectResult : IAuroraResult
  {
    private HttpContextBase context;
    private string location;

    public RedirectResult(HttpContextBase ctx, string loc)
    {
      context = ctx;
      location = loc;
    }

    public void Render()
    {
      context.Response.Redirect(location);
    }
  }
  #endregion

  #region ANTIFORGERYTOKEN
  internal enum AntiForgeryTokenType
  {
    Form,
    Json
  }

  internal class AntiForgeryToken
  {
    private static List<string> GetTokens(HttpContextBase context)
    {
      List<string> tokens = null;

      if (context.Session[MainConfig.AntiForgeryTokenSessionName] != null)
        tokens = context.Session[MainConfig.AntiForgeryTokenSessionName] as List<string>;
      else
        tokens = new List<string>();

      return tokens;
    }

    private static string CreateUniqueToken(List<string> tokens)
    {
      string token = string.Format("{0}{1}", Guid.NewGuid(), Guid.NewGuid()).Replace("-", string.Empty);

      if (tokens.Contains(token))
        CreateUniqueToken(tokens);

      return token;
    }

    public static string Create(HttpContextBase context, AntiForgeryTokenType type)
    {
      List<string> tokens = GetTokens(context);
      string token = CreateUniqueToken(tokens);
      tokens.Add(token);

      context.Session[MainConfig.AntiForgeryTokenSessionName] = tokens;

      string renderToken = string.Empty;

      switch (type)
      {
        case AntiForgeryTokenType.Form:
          renderToken = string.Format("<input type=\"hidden\" name=\"AntiForgeryToken\" value=\"{0}\" />", token);
          break;

        case AntiForgeryTokenType.Json:
          renderToken = string.Format("AntiForgeryToken={0}", token);
          break;
      }

      return renderToken;
    }

    public static void RemoveToken(HttpContextBase context)
    {
      if (context.Request.Form.AllKeys.Contains(MainConfig.AntiForgeryTokenName))
      {
        string token = context.Request.Form[MainConfig.AntiForgeryTokenName];

        List<string> tokens = GetTokens(context);

        if (tokens.Contains(token))
        {
          tokens.Remove(token);

          context.Session[MainConfig.AntiForgeryTokenSessionName] = tokens;
        }
      }
    }

    public static bool VerifyToken(HttpContextBase context)
    {
      if (context.Request.Form.AllKeys.Contains(MainConfig.AntiForgeryTokenName))
      {
        string token = context.Request.Form[MainConfig.AntiForgeryTokenName];

        return GetTokens(context).Contains(token);
      }

      return false;
    }
  }
  #endregion

  #region AURORA VIEW ENGINE
  public interface IViewEngine
  {
    void LoadView(string controllerName, string viewName, Dictionary<string, string> tags);

    void LoadView(string fragmentName, Dictionary<string, string> tags);

    bool ContainsView(string view);

    string this[string view] { get; }
  }

  internal class ViewTypeContainer
  {
    public Dictionary<string, StringBuilder> RawTemplates { get; set; }
    public Dictionary<string, string> CompiledViews { get; set; }

    public ViewTypeContainer()
    {
      RawTemplates = new Dictionary<string, StringBuilder>();
      CompiledViews = new Dictionary<string, string>();
    }
  }

  internal class ViewTemplateInfo
  {
    public ViewTypeContainer Views { get; set; }
    public ViewTypeContainer Fragments { get; set; }

    public ViewTemplateInfo()
    {
      Views = new ViewTypeContainer();
      Fragments = new ViewTypeContainer();
    }
  }

  internal class AuroraViewEngine : IViewEngine
  {
    private HttpContextBase context;
    private string viewRoot;
    private static Regex directiveTokenRE = new Regex(@"(\%\%(?<directive>[a-zA-Z0-9]+)=(?<value>[a-zA-Z0-9]+)\%\%)");
    private Regex headBlockRE = new Regex(@"\[\[(?<block>[\s\w\p{P}\p{S}]+)\]\]");
    private string tagFormatPattern = @"({{({{|\|){0}(\||}})}})";
    private string tagPattern = @"{({|\|)([\w]+)(}|\|)}";
    private string tagEncodingHint = "|";
    private static string antiForgeryToken = string.Format("%%{0}%%", MainConfig.AntiForgeryTokenName);
    private static string jsonAntiForgeryToken = string.Format("%%{0}%%", MainConfig.JsonAntiForgeryTokenName);
    private static string viewDirective = "%%View%%";
    private static string headDirective = "%%Head%%";
    private static string partialDirective = "%%Partial={0}%%";
    private static string sharedFolderName = MainConfig.SharedFolderName;
    private static string fragmentsFolderName = MainConfig.FragmentsFolderName;

    private ViewTemplateInfo templateInfo;

    public AuroraViewEngine(HttpContextBase ctx, string vr)
    {
      context = ctx;
      viewRoot = vr;

      // Load all templates here and then cache them in the application store
      if ((context.Application[MainConfig.TemplatesSessionName] != null) && (!MainConfig.Debug))
      {
        templateInfo = context.Application[MainConfig.TemplatesSessionName] as ViewTemplateInfo;
      }
      else
      {
        templateInfo = new ViewTemplateInfo();

        LoadTemplates(viewRoot);
        LoadFragments(viewRoot);

        context.Application[MainConfig.TemplatesSessionName] = templateInfo;
      }
    }

    private void LoadTemplates(string path)
    {
      foreach (FileInfo fi in GetFiles(path).Where(x => x.Directory.Name != MainConfig.FragmentsFolderName))
      {
        using (StreamReader sr = new StreamReader(fi.OpenRead()))
        {
          string template = sr.ReadToEnd();
          string viewKeyName = string.Format("{0}/{1}", fi.Directory.Name, fi.Name.Replace(fi.Extension, string.Empty));

          templateInfo.Views.RawTemplates.Add(viewKeyName, new StringBuilder(template));
        }
      }
    }

    private void LoadFragments(string path)
    {
      foreach (FileInfo fi in GetFiles(path).Where(x => x.Directory.Name == MainConfig.FragmentsFolderName))
      {
        using (StreamReader sr = new StreamReader(fi.OpenRead()))
        {
          string fragment = sr.ReadToEnd();
          string fragmentKeyName = fi.Name.Replace(fi.Extension, string.Empty);

          templateInfo.Fragments.RawTemplates.Add(fragmentKeyName, new StringBuilder(fragment));
        }
      }
    }

    // This code was adapted to work with FileInfo but was originally from the following question on SO:
    //
    // http://stackoverflow.com/questions/929276/how-to-recursively-list-all-the-files-in-a-directory-in-c
    private static IEnumerable<FileInfo> GetFiles(string path)
    {
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

    private StringBuilder ProcessDirectives(string viewKeyName, Dictionary<string, StringBuilder> rawTemplates, StringBuilder rawView)
    {
      MatchCollection dirMatches = directiveTokenRE.Matches(rawView.ToString());
      StringBuilder pageContent = new StringBuilder();
      StringBuilder directive = new StringBuilder();
      StringBuilder value = new StringBuilder();

      #region PROCESS KEY=VALUE DIRECTIVES (MASTER AND PARTIAL VIEWS)
      foreach (Match match in dirMatches)
      {
        directive.Length = 0;
        directive.Insert(0, match.Groups["directive"].Value);

        value.Length = 0;
        value.Insert(0, match.Groups["value"].Value);

        string pageName = String.Join("/", new string[] { MainConfig.SharedFolderName, value.ToString() });

        if (!String.IsNullOrEmpty(pageName))
        {
          string template = rawTemplates[pageName].ToString();

          switch (directive.ToString())
          {
            case "Master":
              pageContent = new StringBuilder(template);
              rawView.Replace(match.Groups[0].Value, string.Empty);
              pageContent.Replace(viewDirective, rawView.ToString());
              break;

            case "Partial":
              StringBuilder partialContent = new StringBuilder(template);
              rawView.Replace(string.Format(partialDirective, value), partialContent.ToString());
              break;
          }
        }
      }
      #endregion

      // If during the process of building the view we have more directives to process
      // we'll recursively call ProcessDirectives to take care of them
      if (directiveTokenRE.Matches(pageContent.ToString()).Count > 0)
        ProcessDirectives(viewKeyName, rawTemplates, pageContent);

      #region PROCESS HEAD SUBSTITUTIONS AFTER ALL TEMPLATES HAVE BEEN COMPILED
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
      #endregion

      return pageContent;
    }

    private StringBuilder ReplaceAntiForgeryTokens(StringBuilder view, string token, AntiForgeryTokenType type)
    {
      var tokens = Regex.Matches(view.ToString(), token).Cast<Match>().Select(m => new { Start = m.Index, End = m.Length }).Reverse();

      foreach (var t in tokens)
      {
        view.Replace(token, AntiForgeryToken.Create(context, type), t.Start, t.End);
      }

      return view;
    }

    private void Compile(string viewKeyName, Dictionary<string, StringBuilder> rawTemplates, Dictionary<string, string> compiledViews, Dictionary<string, string> tags, bool fragments)
    {
      StringBuilder rawView = new StringBuilder(rawTemplates[viewKeyName].ToString());
      StringBuilder compiledView = new StringBuilder();

      if (!fragments)
        compiledView = ProcessDirectives(viewKeyName, rawTemplates, rawView);

      if (string.IsNullOrEmpty(compiledView.ToString()))
        compiledView = rawView;

      compiledView = ReplaceAntiForgeryTokens(compiledView, antiForgeryToken, AntiForgeryTokenType.Form);
      compiledView = ReplaceAntiForgeryTokens(compiledView, jsonAntiForgeryToken, AntiForgeryTokenType.Json);

      compiledView.Replace(compiledView.ToString(), Regex.Replace(compiledView.ToString(), @"^\s*$\n", string.Empty, RegexOptions.Multiline));

      if (tags != null)
      {
        StringBuilder tagSB = new StringBuilder();

        foreach (KeyValuePair<string, string> tag in tags)
        {
          tagSB.Length = 0;
          tagSB.Insert(0, string.Format(tagFormatPattern, tag.Key));

          Regex nonHTMLEncodedTagRE = new Regex(tagSB.ToString());

          if (nonHTMLEncodedTagRE.IsMatch(compiledView.ToString()))
          {
            MatchCollection nonEncodedMatches = nonHTMLEncodedTagRE.Matches(compiledView.ToString());

            foreach (Match m in nonEncodedMatches)
            {
              if (!m.Value.Contains(tagEncodingHint))
                compiledView.Replace(m.Value, tag.Value);
              else
                compiledView.Replace(m.Value, context.Server.HtmlEncode(tag.Value));
            }
          }
        }

        Regex leftOverTags = new Regex(tagPattern);

        if (leftOverTags.IsMatch(compiledView.ToString()))
        {
          MatchCollection m = leftOverTags.Matches(compiledView.ToString());

          foreach (Match match in m)
          {
            compiledView.Replace(match.Value, string.Empty);
          }
        }
      }

      compiledViews[viewKeyName] = compiledView.ToString();
    }

    public void LoadView(string controllerName, string viewName, Dictionary<string, string> tags)
    {
      string viewKeyName = string.Format("{0}/{1}", controllerName, viewName);

      if (templateInfo.Views.RawTemplates.ContainsKey(viewKeyName))
        Compile(viewKeyName, templateInfo.Views.RawTemplates, templateInfo.Views.CompiledViews, tags, false);
    }

    public void LoadView(string fragmentName, Dictionary<string, string> tags)
    {
      if (templateInfo.Fragments.RawTemplates.ContainsKey(fragmentName))
        Compile(fragmentName, templateInfo.Fragments.RawTemplates, templateInfo.Fragments.CompiledViews, tags, true);
    }

    public bool ContainsView(string viewName)
    {
      if (templateInfo.Views.CompiledViews.ContainsKey(viewName) || templateInfo.Fragments.CompiledViews.ContainsKey(viewName))
        return true;

      return false;
    }

    public string this[string key]
    {
      get
      {
        if (templateInfo.Views.CompiledViews.ContainsKey(key))
          return templateInfo.Views.CompiledViews[key];

        if (templateInfo.Fragments.CompiledViews.ContainsKey(key))
          return templateInfo.Fragments.CompiledViews[key];

        throw new Exception(string.Format(MainConfig.CannotFindViewError, key));
      }
    }
  }
  #endregion

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
      new AuroraEngine(new HttpContextWrapper(context));
    }
  }
  #endregion

  #region HTTP MODULE
  public sealed class AuroraModule : IHttpModule
  {
    public void Dispose() { }

    public void Init(HttpApplication app)
    {
      app.Error += new EventHandler(app_Error);
    }

    private void app_Error(object sender, EventArgs e)
    {
      HttpContext context = HttpContext.Current;

      Exception ex = context.Server.GetLastError();

      CustomError customError = ApplicationInternals.GetCustomError(new HttpContextWrapper(context), ex);

      if (customError != null)
      {
        if (MainConfig.CustomErrorsSection.Mode != CustomErrorsMode.Off &&
           MainConfig.CustomErrorsSection.Mode != CustomErrorsMode.RemoteOnly)
          ex = new Exception(MainConfig.GenericErrorMessage);

        customError.Error(null, ex).Render();
        context.Server.ClearError();
      }
    }
  }
  #endregion
}
