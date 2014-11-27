using Aurora.Common;
using Aurora.Rewrite;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace RewriteTest
{
	public class TestController : Controller
	{
		[Http(ActionType.Get, "/Index")]
		public void Index()
		{
			// This won't return void for long, LOL! Just doing this to code up the
			// route discovery code...
		}

		[Http(ActionType.Get, "/Foo")]
		public void Foo()
		{
			// This won't return void for long, LOL! Just doing this to code up the
			// route discovery code...
		}

		[Http(ActionType.Get, "/Bar")]
		public void Bar()
		{
			// This won't return void for long, LOL! Just doing this to code up the
			// route discovery code...
		}
	}
}