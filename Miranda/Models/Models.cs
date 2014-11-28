//
// Miranda is a tiny wiki
//
// Frank Hale <frankhale@gmail.com>
// 17 February 2013
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

using Aurora.Common;
using System;
using System.Collections.Generic;

namespace Aurora.Models
{
	public class UnameAndPassword : Model
	{
		[Required("The username is required.")]
		public string UserName { get; set; }
		[Required("The password is required.")]
		public string Password { get; set; }
	}

	public class PageData : Model
	{
		[NotRequired]
		public int? ID { get; set; }
		[Required("The wiki title is a required field.")]
		public string Title { get; set; }

		[Unsafe]
		[Required("The wiki data is a required field.")]
		public string Data { get; set; }

		[NotRequired]
		public string Tags { get; set; }
	}

	public class WikiTitle
	{
		public int ID { get; set; }
		public string Alias { get; set; }
		public string Title { get; set; }
	}

	public class WikiPage
	{
		public int ID { get; set; }
		public DateTime CreatedOn { get; set; }
		public DateTime ModifiedOn { get; set; }
		public string Alias { get; set; }
		public int AuthorID { get; set; }
		public string Title { get; set; }
		public string Body { get; set; }
		public bool Published { get; set; }
	}

	public class WikiPageTag
	{
		public int ID { get; set; }
		public int PageID { get; set; }
		public int TagID { get; set; }
	}

	public class WikiComment
	{
		public int ID { get; set; }
		public int PageID { get; set; }
		public DateTime CreatedOn { get; set; }
		public DateTime ModifiedOn { get; set; }
		public int AuthorID { get; set; }
		public string Body { get; set; }
	}

	public class WikiTag
	{
		public int ID { get; set; }
		public DateTime CreatedOn { get; set; }
		public string Name { get; set; }
	}

	public class WikiUser
	{
		public int ID { get; set; }
		public string UserName { get; set; }
		public string FirstName { get; set; }
		public string LastName { get; set; }
		public byte[] Avatar { get; set; }
		public string Identifier { get; set; }
	}

	public interface IData
	{
		#region PAGE METHODS
		List<WikiTitle> GetAllPageTitles();
		List<WikiPage> GetAllPages();
		WikiPage GetPage(int id);
		List<WikiPage> GetPages(int count);
		void AddPage(WikiPage p);
		void DeletePage(int id);
		void DeletePage(WikiPage p);
		void UpdatePage(WikiPage p);
		#endregion

		#region PAGE TAG METHODS
		List<WikiTag> GetPageTags(WikiPage p);
		List<Tuple<string, string>> GetTagsWithPageList();
		void AddPageTag(WikiPage p, string name);
		void DeletePageTags(WikiPage p);
		#endregion

		#region COMMENT METHODS
		List<WikiComment> GetAllComments();
		List<WikiComment> GetComments(int pageID);
		void AddComment(WikiComment c);
		void DeleteComment(WikiComment c);
		void UpdateComment(WikiComment c);
		#endregion

		#region TAG METHODS
		List<WikiTag> GetAllTags();
		void AddTag(string name);
		void DeleteTag(WikiTag t);
		void UpdateTag(WikiTag t);
		#endregion

		#region USER METHODS
		List<WikiUser> GetAllUsers();
		WikiUser GetUserByOpenIDIdentifier(string openIDIdentifier);
		void AddUser(WikiUser u);
		void DeleteUser(WikiUser u);
		void UpdateUser(WikiUser u);
		WikiUser GetUserByIdentifier(string identifier);
		WikiUser GetUserByUserName(string userName);
		#endregion
	}

	#region MASSIVE
	namespace Massive
	{
		using Aurora.Extra.Massive;
		
		internal class Comments : DynamicModel { }
		internal class Pages : DynamicModel { }
		internal class PageTags : DynamicModel { }
		internal class Roles : DynamicModel { }
		internal class Tags : DynamicModel { }
		internal class UserRoles : DynamicModel { }
		internal class Users : DynamicModel { }
		
		public class MassiveDataConnector : IData
		{
			private dynamic pages = new Pages();
			private dynamic comments = new Comments();
			private dynamic tags = new Tags();
			private dynamic roles = new Roles();
			private dynamic pageTags = new PageTags();
			private dynamic userRoles = new UserRoles();
			private dynamic users = new Users();

			#region PAGE METHODS
			public List<WikiTitle> GetAllPageTitles()
			{
				List<WikiTitle> allTitles = new List<WikiTitle>();

				foreach (var x in pages.All(columns: "ID, Alias, Title"))
				{
					allTitles.Add(new WikiTitle()
					{
						ID = x.ID,
						Alias = x.Alias,
						Title = x.Title
					});
				}

				return allTitles;
			}

			public List<WikiPage> GetAllPages()
			{
				List<WikiPage> allPages = new List<WikiPage>();

				foreach (var x in pages.All())
				{
					allPages.Add(new WikiPage()
					{
						ID = x.ID,
						CreatedOn = x.CreatedOn,
						ModifiedOn = x.ModifiedOn,
						Alias = x.Alias,
						AuthorID = x.AuthorID,
						Title = x.Title,
						Body = x.Body,
						Published = x.Published
					});
				}

				return allPages;
			}

			public WikiPage GetPage(int id)
			{
				WikiPage result = null;

				var _p = pages.First(ID: id);

				if (_p != null)
				{
					result = new WikiPage()
					{
						ID = _p.ID,
						CreatedOn = _p.CreatedOn,
						ModifiedOn = _p.ModifiedOn,
						Alias = _p.Alias,
						AuthorID = _p.AuthorID,
						Title = _p.Title,
						Body = _p.Body,
						Published = _p.Published
					};
				}

				return result;
			}

			public List<WikiPage> GetPages(int count)
			{
				throw new NotImplementedException();
			}

			public void AddPage(WikiPage p)
			{
				if (p == null)
				{
					throw new ArgumentNullException("p");
				}

				var x = pages.Insert(new
				{
					CreatedOn = p.CreatedOn,
					ModifiedOn = p.ModifiedOn,
					Alias = p.Alias,
					AuthorID = p.AuthorID,
					Title = p.Title,
					Body = p.Body,
					Published = p.Published
				});
				
				p.ID = (int)x.ID;
			}

			public void DeletePage(int id)
			{
				WikiPage p = GetPage(id);

				if (p != null)
				{
					DeletePage(p);
				}
			}

			public void DeletePage(WikiPage p)
			{
				if (p == null)
				{
					throw new ArgumentNullException("p");
				}

				pages.Delete(p.ID);
			}

			public void UpdatePage(WikiPage p)
			{
				if (p == null)
				{
					throw new ArgumentNullException("p");
				}

				var _p = new
				{
					CreatedOn = p.CreatedOn,
					ModifiedOn = p.ModifiedOn,
					Alias = p.Alias,
					AuthorID = p.AuthorID,
					Title = p.Title,
					Body = p.Body,
					Published = p.Published
				};

				pages.Update(_p, p.ID);
			}
			#endregion

			#region COMMENT METHODS
			public List<WikiComment> GetAllComments()
			{
				throw new NotImplementedException();
			}

			public List<WikiComment> GetComments(int pageID)
			{
				throw new NotImplementedException();
			}

			public void AddComment(WikiComment c)
			{
				throw new NotImplementedException();
			}

			public void DeleteComment(WikiComment c)
			{
				throw new NotImplementedException();
			}

			public void UpdateComment(WikiComment c)
			{
				throw new NotImplementedException();
			}
			#endregion

			#region PAGE TAG METHODS
			public List<WikiTag> GetPageTags(WikiPage p) 
			{
				List<WikiTag> result = new List<WikiTag>();

				foreach (var pt in pageTags.Find(PageID: p.ID))
				{
					var t = tags.First(id: pt.TagID);

					if (t != null)
					{
						result.Add(new WikiTag()
						{
						  ID = t.ID,
						  CreatedOn = t.CreatedOn,
						  Name = t.Name
						});
					}
				}

				return result;
			}
						
			public void AddPageTag(WikiPage p, string name)
			{
				var _p = pages.First(id: p.ID);

				if (_p != null)
				{
					var _t = tags.First(Name: name);

					if (_t != null)
					{
						pageTags.Insert(new 
						{  
							PageID = _p.ID,
							TagID = _t.ID
						});
					}
				}
			}

			public void DeletePageTags(WikiPage p)
			{
				pageTags.Delete(where: "PageID = @0", args: p.ID.ToString());
			}
			#endregion

			#region TAG METHODS
			public List<WikiTag> GetAllTags()
			{
				List<WikiTag> allTags = new List<WikiTag>();

				foreach (var t in tags.All())
				{
					allTags.Add(new WikiTag()
					{
						ID = t.ID,
						CreatedOn = t.CreatedOn,
						Name = t.Name
					});
				}

				return allTags;
			}

			public void AddTag(string name)
			{
				var _t = tags.First(Name: name);

				if (_t == null)
				{
					var tag = new
					{
						CreatedOn = DateTime.Now,
						Name = name
					};

					tags.Insert(tag);
				}
			}

			public void DeleteTag(WikiTag t)
			{
				throw new NotImplementedException();
			}

			public void UpdateTag(WikiTag t)
			{
				throw new NotImplementedException();
			}

			public List<Tuple<string, string>> GetTagsWithPageList()
			{
				List<Tuple<string, string>> results = null;

				dynamic p = tags.Query("SELECT Tag, IDList FROM v_AllTagsWithPageList");

				if (p != null)
				{
					results = new List<Tuple<string, string>>();

					foreach (var r in p)
					{
						results.Add(new Tuple<string, string>(r.Tag, r.IDList));
					}
				}

				return results;
			}
			#endregion

			#region USER METHODS
			public List<WikiUser> GetAllUsers()
			{
				throw new NotImplementedException();

				//return db.__Users.Select(x => new WikiUser()
				//{
				//  ID = x.ID,
				//  Avatar = x.Avatar.ToArray(),
				//  FirstName = x.FirstName,
				//  LastName = x.LastName,
				//  UserName = x.UserName,
				//  Identifier = x.Identifier
				//}).ToList();
			}

			public WikiUser GetUserByOpenIDIdentifier(string openIDIdentifier)
			{
				throw new NotImplementedException();

				//return db.__Users.Select(x => new WikiUser()
				//{
				//  ID = x.ID,
				//  Avatar = (x.Avatar != null) ? x.Avatar.ToArray() : null,
				//  FirstName = x.FirstName,
				//  LastName = x.LastName,
				//  UserName = x.UserName,
				//  Identifier = x.Identifier
				//}).FirstOrDefault(u => u.Identifier == openIDIdentifier);
			}

			public void AddUser(WikiUser u)
			{
				//__User _u = db.__Users.FirstOrDefault(x => x.Identifier == u.Identifier);

				//if (_u == null)
				//{
				//  _u = new __User()
				//  {
				//    Avatar = u.Avatar,
				//    FirstName = u.FirstName,
				//    LastName = u.LastName,
				//    UserName = u.UserName,
				//    Identifier = u.Identifier
				//  };

				//  db.__Users.InsertOnSubmit(_u);
				//  db.SubmitChanges();
				//}
			}

			public void DeleteUser(WikiUser u)
			{
				//__User deleteUser = db.__Users.FirstOrDefault(x => x.ID == u.ID);

				//if (deleteUser != null)
				//{
				//  db.__Users.DeleteOnSubmit(deleteUser);
				//  db.SubmitChanges();
				//}
			}

			public void UpdateUser(WikiUser u)
			{
				//__User updateUser = db.__Users.FirstOrDefault(x => x.ID == u.ID);

				//if (updateUser != null)
				//{
				//  updateUser.FirstName = u.FirstName;
				//  updateUser.LastName = u.LastName;
				//  updateUser.Avatar = u.Avatar;
				//  updateUser.UserName = u.UserName;

				//  db.SubmitChanges();
				//}
			}

			public WikiUser GetUserByIdentifier(string identifier)
			{
				WikiUser u = null;

				var _u = users.First(Identifier: identifier);

				if (_u != null)
				{
					u = new WikiUser()
					{
						ID = _u.ID,
						Identifier = _u.Identifier,
						FirstName = _u.FirstName,
						LastName = _u.LastName,
						UserName = _u.UserName
					};
				}

				return u;
			}

			public WikiUser GetUserByUserName(string userName)
			{
				WikiUser u = null;

				var _u = users.First(userName: userName);

				if (_u != null)
				{
					u = new WikiUser()
					{
						ID = _u.ID,
						Identifier = _u.Identifier,
						FirstName = _u.FirstName,
						LastName = _u.LastName,
						UserName = _u.UserName
					};
				}

				return u;
			}
			#endregion
		}
	}
	#endregion
}