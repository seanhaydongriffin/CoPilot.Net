using System;
using System.Linq;
using System.Management.Automation;

namespace SharedProject
{
    class Azure
    {
        private static bool Authenticated = false;

        public static void Authenticate()
        {
            if (!Authenticated)
            {
                PowerShell ps = PowerShell.Create();
                ps.AddScript("Get-AzureRmSubscription");
                var output = ps.Invoke();

                if (output.Count == 0)
                {
                    ps = PowerShell.Create();
                    ps.AddScript("Login-AzureRmAccount");
                    ps.Invoke();
                }
            }

            Authenticated = true;
        }



        public static System.Collections.ObjectModel.Collection<PSObject> QueryVMs(String Subscription, String ResourceGroupName, String ScaleSetName, String InstanceArray)
        {
            Authenticate();
            PowerShell ps = PowerShell.Create();
            ps.AddScript("Set-ExecutionPolicy -ExecutionPolicy Unrestricted");
            ps.AddScript("Select-AzureRmSubscription -Subscription '" + Subscription + "'");
            ps.AddScript("$d = " + InstanceArray + " ; Foreach ($i in $d) { (Get-AzureRmVmssVM -ResourceGroupName '" + ResourceGroupName + "' -VMScaleSetName '" + ScaleSetName + "' -InstanceView -InstanceId $i).Statuses.DisplayStatus[1] }");
            var output = ps.Invoke();
            return output;
        }

        public static void StartVMs(String Subscription, String ResourceGroupName, String ScaleSetName, String InstanceArray)
        {
            Authenticate();
            PowerShell ps = PowerShell.Create();
            ps.AddScript("Set-ExecutionPolicy -ExecutionPolicy Unrestricted");
            ps.AddScript("Select-AzureRmSubscription -Subscription '" + Subscription + "'");
            ps.AddScript("Start-AzureRmVmss -ResourceGroupName '" + ResourceGroupName + "' -VMScaleSetName '" + ScaleSetName + "' -InstanceId " + InstanceArray + " -AsJob");
            ps.BeginInvoke();
        }

        public static void StopVMs(String Subscription, String ResourceGroupName, String ScaleSetName, String InstanceArray)
        {
            Authenticate();
            PowerShell ps = PowerShell.Create();
            ps.AddScript("Set-ExecutionPolicy -ExecutionPolicy Unrestricted");
            ps.AddScript("Select-AzureRmSubscription -Subscription '" + Subscription + "'");
            ps.AddScript("Stop-AzureRmVmss -ResourceGroupName '" + ResourceGroupName + "' -VMScaleSetName '" + ScaleSetName + "' -InstanceId " + InstanceArray + " -Force -AsJob");
            ps.BeginInvoke();
        }



    }
}
