using System;
using System.Runtime.InteropServices;
namespace Shared
{
    public class Paths
    {
        public static string DATABASE
        {
            get
            {
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    return @"C:\Users\Gusta\OneDrive\Dokumenter\GitHub\SearchEngine-main\Data\searchDB.db";
                }
                else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                {
                    return @"/Users/rosell/ITA_SEM6_SearchEngine/Data/searchDB.db";
                }
                else
                {
                    // Default to Linux/Unix style path
                    return @"/Users/rosell/ITA_SEM6_SearchEngine/Data/searchDB.db";
                }
            }
        }
    }
}
