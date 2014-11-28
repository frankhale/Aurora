//
// Miranda is a tiny wiki
//
// Frank Hale <frankhale@gmail.com>
// 26 November 2014
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

using Aurora;
using Aurora.Common;
using Aurora.Extra;
using Aurora.Models;
using Aurora.Models.Massive;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Wiki.Infrastructure.Core
{
	public static class ExtensionMethods
	{
		public static string NewLinesToBR(this string value)
		{
			if (!string.IsNullOrEmpty(value))
				return value.Trim().Replace("\n", "<br />");
			else
				return value;
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
	}

	public interface IAuthentication
	{
		bool Authenticated { get; }
		string Identifier { get; }
		string Name { get; }
	}

	public class Authentication : IAuthentication, IBoundToAction
	{
		public bool Authenticated { get; private set; }
		public string Identifier { get; private set; }
		public string Name { get; private set; }

		private RouteInfo routeInfo;

		public void Initialize(RouteInfo routeInfo)
		{
			this.routeInfo = routeInfo;

#if OPENAUTH
			OpenIDAuthentication openIDAuth = new OpenIDAuthentication();

			openIDAuth.Initialize(routeInfo);

			if (openIDAuth.Authenticated)
			{
				Authenticated = openIDAuth.Authenticated;
				Identifier = openIDAuth.Identifier;
			}
#elif USERPASS
			UserNameAndPasswordAuthentication upAuth = new UserNameAndPasswordAuthentication();
			upAuth.Initialize(routeInfo);

			Authenticated = upAuth.Authenticated;
			Name = upAuth.Name;
			Identifier = upAuth.Identifier;
#endif
		}

		public void Abandon()
		{
#if OPENAUTH
			Authenticated = false;
			Identifier = null;

			(new OpenIDAuthentication()).Abandon(routeInfo);
#elif USERPASS
			(new UserNameAndPasswordAuthentication()).Abandon(routeInfo);
#endif
		}
	}

#if USERPASS
	public class UserNameAndPasswordAuthentication : IAuthentication, IBoundToAction
	{
		public bool Authenticated { get; private set; }
		public string Identifier { get; private set; }
		public string Name { get; private set; }

		public void Abandon(RouteInfo routeInfo)
		{
			routeInfo.Controller.AddSession("UPAuth", null);
		}

		public void Initialize(RouteInfo routeInfo)
		{
			var alreadyLoggedIn = routeInfo.Controller.GetSession("UPAuth") as UserNameAndPasswordAuthentication;

			if (alreadyLoggedIn != null)
			{
				Authenticated = alreadyLoggedIn.Authenticated;
				Identifier = alreadyLoggedIn.Identifier;
				Name = alreadyLoggedIn.Name;
			}

			if (routeInfo.ActionUrlParams.Length > 0)
			{
				var dc = routeInfo.ActionUrlParams.FirstOrDefault(x => x.GetType().GetInterface("IData", true) != null) as IData;
				var upw = routeInfo.ActionUrlParams.FirstOrDefault(x => x != null && x.GetType() == typeof(UnameAndPassword)) as UnameAndPassword;

				if (upw != null && dc != null)
				{
					WikiUser u = dc.GetUserByUserName(upw.UserName);

					// The password checking is better than it was. We'll use SHA1 hashes of the 
					// password for now.
					if (u != null && u.Identifier == upw.Password.CalculateSHA1Sum() &&
							u.UserName == upw.UserName)
					{
						Authenticated = true;
						Name = u.UserName;
						Identifier = "hidden";

						routeInfo.Controller.AddSession("UPAuth", this);
					}
				}
			}
		}
	}
#elif OPENAUTH
	public class OpenAuthLogonType : Model
	{
		public string Identifier { get; set; }
	}

	public class OpenIDAuthentication : IBoundToAction
	{
		public bool Authenticated { get; private set; }
		public string Identifier { get; private set; }
		public string ErrorMessage { get; private set; }
		public OpenAuthClaimsResponse ClaimsResponse { get; private set; }

		public void Abandon(RouteInfo routeInfo)
		{
			routeInfo.Controller.AddSession("OpenAuthStep1", null);
			routeInfo.Controller.AddSession("OpenAuthStep2", null);
			routeInfo.Controller.AddSession("OpenAuthAbandon", true);
		}

		public void Initialize(RouteInfo routeInfo)
		{
			OpenIDAuthentication openIDAuth = routeInfo.Controller.GetSession("OpenAuthStep2") as OpenIDAuthentication;

			if (routeInfo.Controller.GetSession("OpenAuthAbandon") != null)
			{
				routeInfo.Controller.AddSession("OpenAuthAbandon", null);
				return;
			}

			if (openIDAuth != null)
			{
				Authenticated = openIDAuth.Authenticated;
				Identifier = openIDAuth.Identifier;
				ClaimsResponse = openIDAuth.ClaimsResponse;
			}
			else
			{
				var step1 = routeInfo.Controller.GetSession("OpenAuthStep1");

				if (step1 == null)
				{
					OpenAuthLogonType logonType = routeInfo.ActionParams.FirstOrDefault(x => x != null && x.GetType() == typeof(OpenAuthLogonType)) as OpenAuthLogonType;

					if (logonType != null && !string.IsNullOrEmpty(logonType.Identifier))
					{
						OpenAuth.LogonViaOpenAuth(logonType.Identifier,
							routeInfo.Controller.Url,
							providerURI =>
							{
								Uri uri = providerURI;
								routeInfo.Controller.AddSession("OpenAuthStep1", uri);
							},
							(x => ErrorMessage = x));
					}
				}
				else if (step1 != null)
				{
					Uri uri = routeInfo.Controller.GetSession("OpenAuthStep1") as Uri;

					if (uri != null)
					{
						OpenAuth.FinalizeLogonViaOpenAuth(uri,
								claimsResponse =>
								{
									Authenticated = true;
									Identifier = claimsResponse.ClaimedIdentifier;
									ClaimsResponse = claimsResponse;
								},
								cancelled => ErrorMessage = cancelled,
								failed => ErrorMessage = failed);
					}

					routeInfo.Controller.AddSession("OpenAuthStep2", this);
				}
			}
		}
	}
#endif

	public class WikiPageTransform : IActionParamTransform<WikiPage, int>
	{
		private IData db;

		public WikiPageTransform() { }

		public WikiPageTransform(Authentication auth, IData db)
		{
			this.db = db;
		}

		public WikiPage Transform(int id)
		{
			return db.GetPage(id);
		}
	}

	public class WikiFrontController : FrontController
	{
		public WikiFrontController()
		{
			OnInit += new EventHandler(WikiFrontController_OnInit);
			OnMissingRouteEvent += new EventHandler<RouteHandlerEventArgs>(WikiFrontController_MissingRouteEventHandler);
		}

		protected void WikiFrontController_OnInit(object sender, EventArgs args)
		{
			#region SET UP BUNDLES
			string[] wikiCSS = 
			{
				"/Resources/Styles/reset.css",
				"/Resources/Styles/style.css"
			};

			string[] wikiJS = 
			{ 
				"/Resources/Scripts/jquery-1.9.1.js"
			};

			string[] syntaxHighlighterCSS = 
			{
				"/Resources/Scripts/syntaxhighlighter_3.0.83/styles/shCore.css",
				"/Resources/Scripts/syntaxhighlighter_3.0.83/styles/shCoreEmacs.css"
			};

			string[] syntaxHighterJS =
			{
				"/Resources/Scripts/syntaxhighlighter_3.0.83/scripts/shCore.js",
				"/Resources/Scripts/syntaxhighlighter_3.0.83/scripts/shBrushCSharp.js"
			};

			string[] jqueryUIAndTagItCSS = 
			{
				//"/Resources/Scripts/jquery-ui-1.9.0.custom/css/smoothness/jquery-ui-1.9.0.custom.css",
				"/Resources/Styles/tagit.css"
			};

			string[] jqueryUIAndTagItJS =
			{
				//"/Resources/Scripts/jquery-ui-1.9.0.custom/js/jquery-ui-1.9.0.custom.js",
				"/Resources/Scripts/tagit.js"
			};

			AddBundle("wiki.css", wikiCSS);
			AddBundle("wiki.js", wikiJS);
			AddBundle("sh.css", syntaxHighlighterCSS);
			AddBundle("sh.js", syntaxHighterJS);
			AddBundle("tagit.css", jqueryUIAndTagItCSS);
			AddBundle("tagit.js", jqueryUIAndTagItJS);
			#endregion

			AddBindingsForAllActions("Wiki", new object[] { 
					new Authentication(),
					new MassiveDataConnector()
			});

			#region SET UP DYNAMIC ROUTES
			List<object> bindings = GetBindings("Wiki", "Index", "/Index", new[] { typeof(IData) });

			if (bindings != null)
			{
				var dc = bindings[1] as IData;

				if (dc != null)
				{
					List<WikiTitle> titles = dc.GetAllPageTitles();

					List<string> routeAliases = GetAllRouteAliases();

					foreach (WikiTitle p in titles)
					{
						string alias = "/" + p.Alias;

						// Create dynamic routes based on wiki aliases
						if (!routeAliases.Contains(alias))
							AddRoute(alias, "Wiki", "Show", p.ID.ToString());
					}
				}
			}
			#endregion
		}

		private void WikiFrontController_MissingRouteEventHandler(object sender, RouteHandlerEventArgs e)
		{
			string p = e.Path.TrimStart('/');

			if (p.StartsWith("wiki-"))
				e.RouteInfo = FindRoute(string.Format("/Add/{0}", p));
		}
	}
}