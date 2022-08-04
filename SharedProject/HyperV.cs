using System;
using System.Linq;
using System.Management.Automation;

namespace SharedProject
{
    class HyperV
    {

        public static System.Collections.ObjectModel.Collection<PSObject> QueryVM(String HostName)
        {
            PowerShell ps = PowerShell.Create();
            ps.AddScript(@"Import-Module Hyper-V");
            ps.AddScript("Get-VM " + HostName + " | foreach { $_.State }");
            var output = ps.Invoke();
            return output;
        }

        public static void StartVMs(String VMNames)
        {
            PowerShell ps = PowerShell.Create();
            ps.AddScript(@"Import-Module Hyper-V");
            ps.AddScript("Start-VM -Name " + VMNames);
            ps.BeginInvoke();
        }

        public static void StopVMs(String VMNames)
        {
            PowerShell ps = PowerShell.Create();
            ps.AddScript(@"Import-Module Hyper-V");
            ps.AddScript("Stop-VM -Name " + VMNames + " -Force");
            ps.BeginInvoke();
        }

    }
}
