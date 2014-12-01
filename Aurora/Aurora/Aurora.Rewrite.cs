// 
// Frank Hale <frankhale@gmail.com>
// 30 November 2014
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
		public EngineAppState(Dictionary<string, object> app, Dictionary<string, object> request)
		{

		}
	}

	internal class EngineSessionState
	{
		public EngineSessionState(Dictionary<string, object> app, Dictionary<string, object> request)
		{

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
		}
	}
}
