using System.Linq;
using System.Reflection;

namespace GameDevWare.Charon.Unity.Utils
{
	internal static class AssemblyExtensions
	{
		public static string GetInformationalVersion(this Assembly assembly)
		{
			var informationVersion = (AssemblyInformationalVersionAttribute)assembly.GetCustomAttributes(typeof(AssemblyInformationalVersionAttribute), false).FirstOrDefault();
			if (informationVersion != null && string.IsNullOrEmpty(informationVersion.InformationalVersion) == false)
			{
				return informationVersion.InformationalVersion;
			}
			return assembly.GetName().Version.ToString();
		}
	}
}
