using System.Web.Mvc;
using System.Web.Routing;
using AttributeRouting;

[assembly: WebActivator.PreApplicationStartMethod(typeof(IPRequestForm.App_Start.AttributeRouting), "Start")]

namespace IPRequestForm.App_Start {
    public static class AttributeRouting {
		public static void RegisterRoutes(RouteCollection routes) {
            // See http://github.com/mccalltd/AttributeRouting/wiki/3.-Configuration for more options.
			// To debug routes locally using the built in ASP.NET development server, go to /routes.axd
            routes.MapAttributeRoutes();
		}

        public static void Start() {
            RegisterRoutes(RouteTable.Routes);
        }
    }
}
