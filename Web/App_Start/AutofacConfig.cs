using Autofac;
using Autofac.Core;

namespace Web
{
	public class AutofacConfig
	{
		public static void Register(ContainerBuilder builder)
		{
			builder.RegisterType<DirectoryManager>().AsSelf().InstancePerDependency();
			builder.RegisterType<CommandProcessor>().AsSelf().InstancePerDependency();
		}
	}
}