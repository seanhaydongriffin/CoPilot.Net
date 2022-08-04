using Microsoft.Win32;
using System;
using System.Security.AccessControl;

namespace SharedProject
{
    class Registry
    {

        public static bool CreateLocalMachineSubKeyAndValue(string BaseKey, string NewKey, string ValueName, Int32 ValueValue, RegistryValueKind ValueKind)
        {
            var myKey = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64);
            var FeatureControlKey = myKey.OpenSubKey(BaseKey, RegistryKeyPermissionCheck.ReadWriteSubTree, RegistryRights.FullControl);
            FeatureControlKey.CreateSubKey(NewKey, RegistryKeyPermissionCheck.ReadWriteSubTree);
            var fbfkey = FeatureControlKey.OpenSubKey(NewKey, RegistryKeyPermissionCheck.ReadWriteSubTree);
            fbfkey.SetValue(ValueName, ValueValue, ValueKind);
            return true;
        }

        public static bool CreateCurrentUserSubKeyAndValue(string BaseKey, string NewKey, string ValueName, Int32 ValueValue, RegistryValueKind ValueKind)
        {
            var myKey = RegistryKey.OpenBaseKey(RegistryHive.CurrentUser, RegistryView.Registry64);
            var FeatureControlKey = myKey.OpenSubKey(BaseKey, RegistryKeyPermissionCheck.ReadWriteSubTree, RegistryRights.FullControl);
            FeatureControlKey.CreateSubKey(NewKey, RegistryKeyPermissionCheck.ReadWriteSubTree);
            var fbfkey = FeatureControlKey.OpenSubKey(NewKey, RegistryKeyPermissionCheck.ReadWriteSubTree);
            fbfkey.SetValue(ValueName, ValueValue, ValueKind);
            return true;
        }

        public static bool DeleteCurrentUserValue(string BaseKey, string ValueName)
        {
            var myKey = RegistryKey.OpenBaseKey(RegistryHive.CurrentUser, RegistryView.Registry64);
            var FeatureControlKey = myKey.OpenSubKey(BaseKey, RegistryKeyPermissionCheck.ReadWriteSubTree, RegistryRights.FullControl);
            FeatureControlKey.DeleteValue(ValueName);
            return true;
        }



    }
}
