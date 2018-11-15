using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using System.Web.Optimization;
using System.Web.Routing;
using Autofac;
using Autofac.Core;
using Autofac.Integration.Mvc;
using Web.Controllers;
using Web.Utilities;
using WebGrease.Css.Extensions;

namespace Web
{
    public class MvcApplication : System.Web.HttpApplication
    {
	    private static readonly IReadOnlyList<string> AppGetUrlPaths = CoreUtilities.FindInheritorsOf<Controller>()
		    .Select(c => c.GetMethods())
			.SelectMany(m => m)
			// only HttpGet, pingable, and no arguments
			.Where(m => m.GetCustomAttribute<HttpGetAttribute>() != null 
				&& (m.GetCustomAttribute<PingableAttribute>() != null || m.DeclaringType?.GetCustomAttribute<PingableAttribute>() != null) 
				&& m.GetParameters().Length == 0)
			.Select(m => m.GetCustomAttribute<RouteAttribute>().Template)
			.ToList();

	    private static RecurringAction _heartbeatAction;

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

			_heartbeatAction = new RecurringAction(() => Task.Run(async () =>
			{
				using (var client = new HttpClient())
				{
					foreach (var path in AppGetUrlPaths)
					{
						var url = $"{AppUrl.AppBasePath.Value}/{path}";
						await client.GetAsync(url).ConfigureAwait(false);
					}
				}
			}), TimeSpan.FromMinutes(5));

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
