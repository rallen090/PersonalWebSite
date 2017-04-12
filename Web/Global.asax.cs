using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using System.Web.Optimization;
using System.Web.Routing;
using Autofac;
using Autofac.Core;
using Autofac.Integration.Mvc;
using Web.Controllers;
using WebGrease.Css.Extensions;

namespace Web
{
    public class MvcApplication : System.Web.HttpApplication
    {
        protected void Application_Start()
        {
			var builder = new ContainerBuilder();
			AutofacConfig.Register(builder);
			builder.RegisterControllers(typeof(MvcApplication).Assembly);
			var container = builder.Build();
			DependencyResolver.SetResolver(new AutofacDependencyResolver(container));

			AreaRegistration.RegisterAllAreas();
            FilterConfig.RegisterGlobalFilters(GlobalFilters.Filters);
			RouteConfig.RegisterRoutes(RouteTable.Routes);
            BundleConfig.RegisterBundles(BundleTable.Bundles);

			// for clearing old files that Azure publish misses
			//Directory
			//	.EnumerateFiles(AppDomain.CurrentDomain.BaseDirectory, "about.txt", SearchOption.AllDirectories)
			//	.ForEach(File.Delete);

			// INFO FOR DATABASE:
			//using (var connection = new SqlConnection(
			//        "Server=tcp:YOUR_SERVER_NAME_HERE.database.windows.net,1433;Database=AdventureWorksLT;User ID=YOUR_LOGIN_NAME_HERE;Password=YOUR_PASSWORD_HERE;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;"
			//        ))
			//{
			//    connection.Open();
			//    Console.WriteLine("Connected successfully.");

			//    Console.WriteLine("Press any key to finish...");
			//    Console.ReadKey(true);
			//}
		}
    }
}
