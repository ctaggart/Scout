using System.Reflection;
using System.Resources;
using System.Runtime.InteropServices;
using JetBrains.ActionManagement;
#if !RS45
using JetBrains.UI.Shell.PluginSupport;
#else
using JetBrains.UI.Application.PluginSupport;
#endif

[assembly: AssemblyTitle(AssemblyInfo.Product)]
[assembly: AssemblyProduct(AssemblyInfo.Product)]
[assembly: AssemblyCompany(AssemblyInfo.Company)]
[assembly: AssemblyVersion(AssemblyInfo.Version)]
[assembly: AssemblyFileVersion(AssemblyInfo.Version)]

[assembly: AssemblyCulture("")]
[assembly: NeutralResourcesLanguage("en")]
[assembly: Guid("e5a4cb55-1510-45e1-8961-7e614de1ffb4")]
[assembly: ComVisible(false)]

[assembly: ActionsXml("ReSharper.Scout.Properties.Actions.xml", Precompile=false)]
[assembly: PluginTitle(AssemblyInfo.Product)]
[assembly: PluginDescription(AssemblyInfo.Description)]
[assembly: PluginVendor(AssemblyInfo.Company)]

internal static class AssemblyInfo
{
	public const string Product      = "Scout";
	public const string Company      = "RSDN";
	public const string Description  = "Navigation plugin for ReSharper";
	public const string MajorVersion = "1.1";
	public const string Version      = MajorVersion + ".17.0";
}
