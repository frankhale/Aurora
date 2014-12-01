// 
// Frank Hale <frankhale@gmail.com>
// 1 December 2014
//

using AspNetAdapter;
using Aurora.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Aurora.Middleware
{
	public class RouteDiscovery : IAspNetAdapterMiddleware
	{
		public static string ApplicationRoutes = "ApplicationRoutes";
		public static string ApplicationRoutesSessionName = "__APPLICATION_ROUTES__";		

		public MiddlewareResult Transform(Dictionary<string, object> app, Dictionary<string, object> request)
		{
			var callbacks = new AspNetAdapterCallbacks(app, request);
			var applicationRoutes = callbacks.GetApplication(ApplicationRoutesSessionName) as List<RouteInfo>;

			if (applicationRoutes == null)
			{
				applicationRoutes = Routing.GetAllRoutesForAllControllers();
			}

			if(applicationRoutes.Count > 0)
				callbacks.AddApplication(ApplicationRoutesSessionName, applicationRoutes);
				
			app[ApplicationRoutes] = applicationRoutes;

			return new MiddlewareResult()
			{
				App = app,
				Request = request
			};
		}
	}
}
