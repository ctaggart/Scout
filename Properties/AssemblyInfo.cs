using System.Reflection;
using System.Resources;
using System.Runtime.InteropServices;
using JetBrains.ActionManagement;
using JetBrains.UI.Shell.PluginSupport;

[assembly: AssemblyTitle(AssemblyInfo.Product)]
[assembly: AssemblyProduct(AssemblyInfo.Product)]
[assembly: AssemblyCompany(AssemblyInfo.Company)]
[assembly: AssemblyVersion(AssemblyInfo.Version)]
[assembly: AssemblyFileVersion(AssemblyInfo.Version)]

[assembly: AssemblyCulture("")]
[assembly: NeutralResourcesLanguage("en")]
[assembly: Guid("e5a4cb55-1510-45e1-8961-7e614de1ffb4")]
[assembly: ComVisible(false)]

#if !RS45
[assembly: ActionsXml("ReSharper.Scout.Properties.Actions.xml", Precompile=false)]
#endif
[assembly: ActionsXml("ReSharper.Scout.Properties.VSWindowsActions.xml", Precompile=false)]

[assembly: PluginTitle(AssemblyInfo.Product)]
[assembly: PluginDescription(AssemblyInfo.Description)]
[assembly: PluginVendor(AssemblyInfo.Company)]

internal class AssemblyInfo
{
	public const string Product      = "Scout";
	public const string Company      = "RSDN";
	public const string Description  = "Navigation plugin for ReSharper";
	public const string MajorVersion = "1.1";
	public const string Version      = MajorVersion + ".15.0";
}

