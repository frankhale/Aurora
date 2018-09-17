//
// Miranda is a tiny wiki
//
// Frank Hale <frankhale@gmail.com>
// 26 November 2014
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

using Aurora;
using Aurora.Extra;
using Aurora.Models;
using MarkdownSharp;
using System;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Wiki.Infrastructure.Core;

namespace Wiki.Controllers
{
  public class Wiki : Controller
  {
    private static readonly Regex MatchSpecialCharacters = new Regex("(?:[^a-z0-9 ]|(?<=['\"])s)", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled);
    private static readonly Regex MatchWhitespaces = new Regex(@"\s+", RegexOptions.Compiled);
    private static readonly Regex Csharp = new Regex(@"\[cs\](?<block>[\s\S]+?)\[/cs\]", RegexOptions.Compiled);
    private static readonly Regex Markdown = new Regex(@"\[md\](?<block>[\s\S]+?)\[/md\]", RegexOptions.Compiled);

    public Wiki()
    {
      OnPreAction += Wiki_PreActionEvent;
    }

    void Wiki_PreActionEvent(object sender, RouteHandlerEventArgs e)
    {
      ViewBag.appTitle = WebJsonInfo.AppSettings["title"];

      if (CurrentUser == null)
      {
        return;
      }

      ViewBag.currentUser = CurrentUser.Name;
      ViewBag.logOff = new HtmlAnchor("/Logoff", "[Logoff]").ToString();
    }

    #region LOGON / LOGOFF
#if USERPASS
    [Http(ActionType.GetOrPost, "/Logon")]
    public ViewResult Logon(Authentication auth, IData dc, UnameAndPassword upw)
    {
      if (auth.Authenticated)
      {
        var user = dc.GetUserByUserName(auth.Name);

        if (user != null)
        {
          LogOn(user.UserName, new string[] { "Admin" }, user);
          Redirect("/Index");
        }
        else
        {
          ViewBag.message = "You do not have access to this system";
        }
      }
      else
      {
        if (RequestType == "post")
        {
          ViewBag.message = "Username and Password are required fields.";
        }

        //ViewBag.logonForm = new HtmlUserNameAndPasswordForm(this, "/Logon", ConfigurationManager.AppSettings["appName"], CreateAntiForgeryToken()).ToString(); 
        ViewBag.logonForm = RenderFragment("UserNameAndPasswordForm");
      }

      return View();
    }
#elif OPENAUTH
		[Http(ActionType.GetOrPost, "/Logon")]
		public ViewResult Logon(Authentication auth, IData dc, OpenAuthLogonType logonType)
		{
			string message = string.Empty;

			if (auth.Authenticated)
			{
				WikiUser user = dc.GetUserByIdentifier(auth.Identifier);

				if (user != null)
				{
					LogOn(user.UserName, new string[] { "Admin" }, user);
					Redirect("/Index");
				}
				else
				{
					message = "You do not have access to this system";
				}
			}

			if (!string.IsNullOrEmpty(message))
				ViewBag.message = message;

			ViewBag.logonForm = RenderFragment("OpenAuthForm");

			return View();
		}
#elif ACTIVEDIRECTORY
		[Http(ActionType.Get, "/Logon")]
		public ViewResult Logon(Authentication auth, IData dc)
		{
			if (auth.Authenticated && CurrentUser != null)
				Redirect("/Index");

			string message = GetQueryString("message", true);

			if (!string.IsNullOrEmpty(message))
				ViewBag.message = message;

			ViewBag.logonForm = RenderFragment("CACForm");

			return View();
		}

		[Http(ActionType.Post, "/Go")]
		public void Go(Authentication auth, IData dc)
		{
			if (CurrentUser != null)
				Redirect("/Index");

			if (auth.Authenticated)
			{
				WikiUser user = dc.GetUserByIdentifier(auth.Identifier);

				if (auth.Authenticated && user != null)
				{
					LogOn(user.UserName, new string[] { "Admin" }, user);
					Redirect("/Index");
				}
			}

			Redirect(string.Format("/Logon?message={0}", "You do not have access to this system".ToURLEncodedString()));
		}
#endif

    [Http(ActionType.Get, "/Logoff")]
    public void LogOff(Authentication auth, IData dc)
    {
      LogOff();

#if OPENAUTH || USERPASS
      auth.Abandon();
#endif

      Redirect("/Logon");
    }
    #endregion

    [Http(ActionType.Get, "/Index", ActionSecurity.Secure, RedirectWithoutAuthorizationTo = "/Logon", Roles = "User|Admin")]
    public ViewResult Index(Authentication auth, IData dc)
    {
      ViewBag.currentUser = CurrentUser.Name;
      ViewBag.content = WikiList(dc); //WikiGroupList(dc);

      return View();
    }

    [Http(ActionType.Get, "/Add", ActionSecurity.Secure, RedirectWithoutAuthorizationTo = "/Logon", Roles = "Admin")]
    public ViewResult Add(Authentication auth, IData dc)
    {
      FragBag.tagitJS = RenderFragment("TagitJS");

      FragBag.TagitJS.availableTags = "[]";
      FragBag.TagitJS.preselectedTags = "[]";
      ViewBag.tagitJS = RenderFragment("TagitJS");
      ViewBag.wikiPageForm = RenderFragment("WikiPageForm");

      return View();
    }

    [Http(ActionType.Get, "/Add", ActionSecurity.Secure, RedirectWithoutAuthorizationTo = "/Logon", Roles = "Admin")]
    public ViewResult Add(Authentication auth, IData dc, string alias)
    {
      if (!string.IsNullOrEmpty(alias))
      {
        FragBag.WikiPageForm.title = alias.Replace("wiki-", string.Empty)
                             .Replace('-', ' ')
                             .Wordify()
                             .ToTitleCase();
      }

      FragBag.TagitJS.availableTags = "[]";
      FragBag.TagitJS.preselectedTags = "[]";
      ViewBag.tagitJS = RenderFragment("TagitJS");
      ViewBag.wikiPageForm = RenderFragment("WikiPageForm");

      return View();
    }

    [Http(ActionType.Get, "/Show", ActionSecurity.Secure, RedirectWithoutAuthorizationTo = "/Logon", Roles = "User|Admin")]
    public ViewResult Show(Authentication auth, IData dc, [ActionParameterTransform("WikiPageTransform")] WikiPage p)
    {
      if (p != null)
      {
        FragBag.WikiTitle.title = p.Title;

        ViewBag.id = p.ID;
        ViewBag.title = RenderFragment("WikiTitle");
        ViewBag.appTitle = p.Title;

        p.Body = p.Body.ToHtmlEncodedString();

        var csBlocks = Csharp.Matches(p.Body);
        var mdBlocks = Markdown.Matches(p.Body);

        #region C# BLOCKS
        foreach (Match m in csBlocks)
        {
          FragBag.CSharpCodeBlock.code = m.Groups["block"].Value.Trim().ToHtmlDecodedString();

          p.Body = p.Body.Replace(m.Value, RenderFragment("CSharpCodeBlock"));
        }
        #endregion

        #region MARKDOWN BLOCKS
        foreach (Match m in mdBlocks)
        {
          p.Body = p.Body.Replace(m.Value, new Markdown().Transform(m.Groups["block"].Value.Trim().ToHtmlDecodedString()));
        }
        #endregion

        ViewBag.data = p.Body;

        FragBag.EditDeleteMenuItems.edit = p.ID;
        FragBag.EditDeleteMenuItems.delete = p.ID;

        ViewBag.menu = RenderFragment("EditDeleteMenuItems");

        string tags = string.Join(", ", dc.GetPageTags(p).Select(x => x.Name));

        if (!string.IsNullOrEmpty(tags))
        {
          FragBag.FiledUnder.tags = tags;
          ViewBag.filedUnder = RenderFragment("FiledUnder");
        }
      }
      else
      {
        ViewBag.title = "Error!";
        ViewBag.data = "The requested wiki does not exist";
      }

      return View();
    }

    [Http(ActionType.Get, "/Edit", ActionSecurity.Secure, RedirectWithoutAuthorizationTo = "/Logon", Roles = "Admin")]
    public ViewResult Edit(Authentication auth, IData dc, int id)
    {
      var p = dc.GetPage(id);

      if (p != null)
      {
        FragBag.WikiPageForm.id = p.ID;
        FragBag.WikiPageForm.title = p.Title;
        FragBag.WikiPageForm.data = p.Body;
        FragBag.TagitJS.preselectedTags = GetWikiPageTagsAsJSArray(dc, p);
      }

      FragBag.TagitJS.availableTags = "[]";
      ViewBag.tagitJS = RenderFragment("TagitJS");
      ViewBag.wikiPageForm = RenderFragment("WikiPageForm");

      return View();
    }

    [Http(ActionType.Get, "/Delete", ActionSecurity.Secure, RedirectWithoutAuthorizationTo = "/Logon", Roles = "Admin")]
    public void Delete(Authentication auth, IData dc, int id)
    {
      var p = dc.GetPage(id);

      if (p != null)
      {
        var alias = "/" + p.Alias;

        if (GetAllRouteAliases().Contains(alias))
        {
          RemoveRoute(alias);
        }

        dc.DeletePage(id);
      }

      Redirect("/Index");
    }

    [Http(ActionType.Post, "/Save", ActionSecurity.Secure, RedirectWithoutAuthorizationTo = "/Logon", Roles = "Admin")]
    public void Save(Authentication auth, IData dc, PageData data)
    {
      if (data.IsValid)
      {
        var u = CurrentUser.ArcheType as WikiUser;
        var edit = false;

        WikiPage p;

        if (data.ID != null)
        {
          p = dc.GetPage(data.ID.Value);

          if (p == null)
          {
            p = new WikiPage();
          }
          else
          {
            edit = true;
          }
        }
        else
        {
          p = new WikiPage();
        }

        if (u != null)
        {
          p.AuthorID = u.ID;
        }

        p.Body = data.Data;
        p.ModifiedOn = DateTime.Now;
        p.Title = data.Title;

        Func<string, string> aliasify = delegate (string s)
        {
          s = MatchSpecialCharacters.Replace(s, string.Empty).Trim();
          s = MatchWhitespaces.Replace(s, "-");

          return s;
        };

        p.Alias = aliasify(data.Title);

        if (!edit)
        {
          p.CreatedOn = DateTime.Now;

          dc.AddPage(p);
        }
        else
        {
          dc.UpdatePage(p);
        }

        // DO TAG related things here
        dc.DeletePageTags(p);

        if (!string.IsNullOrEmpty(data.Tags))
        {
          var allTags = dc.GetAllTags().Select(x => x.Name).ToList();
          var incomingTags = data.Tags.Split(',').ToList();

          if (incomingTags.Any())
          {
            var tagDiffs = (allTags.Any()) ? incomingTags.Except(allTags) : incomingTags;

            if (tagDiffs.Any())
            {
              foreach (var t in tagDiffs)
              {
                dc.AddTag(t);
              }
            }

            foreach (var it in incomingTags)
            {
              dc.AddPageTag(p, it);
            }
          }
        }

        var alias = "/" + p.Alias;

        if (!GetAllRouteAliases().Contains(alias))
        {
          AddRoute(alias, "Wiki", "Show", p.ID.ToString());
        }

        Redirect(alias);
        return;
      }

      if (!string.IsNullOrEmpty(data.Error))
      {
        ViewBag.error = data.Error.NewLinesToBR() + "<hr />";
      }

      Redirect("/Index");
    }

    [Http(ActionType.Get, "/About", ActionSecurity.Secure, RedirectWithoutAuthorizationTo = "/Logon", Roles = "User|Admin")]
    public ViewResult About(Authentication auth, IData dc)
    {
      FragBag.AboutInfo.appName = WebJsonInfo.AppSettings["title"];
      FragBag.AboutInfo.author = WebJsonInfo.AppSettings["author"];
      FragBag.AboutInfo.lastUpdate = WebJsonInfo.AppSettings["modifiedDate"];
      
      ViewBag.content = RenderFragment("AboutInfo");

      return View();
    }

    [Http(ActionType.Get, "/Aliases", ActionSecurity.Secure, RedirectWithoutAuthorizationTo = "/Logon", Roles = "User|Admin")]
    public ViewResult Aliases(Authentication auth, IData dc)
    {
      ViewBag.content = string.Join(", ", GetAllRouteAliases().ToArray());

      return View("Index");
    }

    #region HELPERS
    private string WikiList(IData dc)
    {
      var titles = dc.GetAllPageTitles();

      if (!titles.Any())
      {
        return "There are no pages in this wiki";
      }
      else
      {
        return string.Join(" | ", (from p in titles
                                   let alias = "/" + p.Alias
                                   select string.Format("<a href=\"/{0}\">{1}</a>", p.Alias.ToURLEncodedString(), p.Title.Replace(" ", "_")
                                   )).ToArray());
      }
    }

    private string WikiGroupList(IData dc)
    {
      var result = new StringBuilder();

      var p = dc.GetTagsWithPageList();

      if (!p.Any())
      {
        return "There are no pages in this wiki";
      }
      else
      {
        foreach (var r in p)
        {
          var pageLinks = r.Item2.Split(',').Select(id =>
            dc.GetPage(Convert.ToInt32(id))).Select(page =>
              string.Format("<a href=\"/Show/{0}\">{1}</a>", page.ID, page.Title)).ToList();

          FragBag.TagGroup.tag = r.Item1 ?? "Untagged";
          FragBag.TagGroup.pages = string.Join(",&nbsp;", pageLinks);

          result.Append(RenderFragment("TagGroup"));
        }

        if (result.Length > 0)
        {
          return result.ToString();
        }

        return null;
      }
    }

    private string GetWikiPageTagsAsJSArray(IData dc, WikiPage p)
    {
      var tagNames = dc.GetPageTags(p).Select(x => x.Name).ToList();
      var mungedNames = tagNames.Select(t => string.Format("\"{0}\"", t)).ToList();

      return string.Format("[{0}]", string.Join(",", mungedNames));
    }
    #endregion
  }
}