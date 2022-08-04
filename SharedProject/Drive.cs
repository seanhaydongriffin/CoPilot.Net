using System;
using System.IO;
using System.Linq;

namespace SharedProject
{
    class Drive
    {

        public static bool Exists(Char DriveLetter)
        {
            var drives = DriveInfo.GetDrives();

            if (drives.Where(data => data.Name == DriveLetter.ToString().ToUpper() + ":\\").Count() == 1)

                return true;

            return false;
        }

    }
}
