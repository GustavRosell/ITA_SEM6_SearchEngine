using System;
using System.Runtime.InteropServices;

namespace Indexer;

public class Config
{
    // Returns the folder to be indexed based on dataset size
    // All .txt files in that folder (and subfolders) will be indexed
    public static string GetFolder(string dataset)
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) 
        {
            return dataset.ToLower() switch
            {
                "small" => @"C:\Users\Gusta\OneDrive\Dokumenter\GitHub\SearchEngine-main\Data\seData copy\small",
                "medium" => @"C:\Users\Gusta\OneDrive\Dokumenter\GitHub\SearchEngine-main\Data\seData copy\medium",
                "large" => @"C:\Users\Gusta\OneDrive\Dokumenter\GitHub\SearchEngine-main\Data\seData copy\large",
                _ => throw new ArgumentException($"Invalid dataset '{dataset}'. Valid options: small, medium, large")
            };
        }
        else
        {
            // macOS/Linux
            return dataset.ToLower() switch
            {
                "small" => @"/Users/rosell/Projects/ITA_SEM6_SearchEngine/Data/seData copy/small",
                "medium" => @"/Users/rosell/Projects/ITA_SEM6_SearchEngine/Data/seData copy/medium",
                "large" => @"/Users/rosell/Projects/ITA_SEM6_SearchEngine/Data/seData copy/large",
                _ => throw new ArgumentException($"Invalid dataset '{dataset}'. Valid options: small, medium, large")
            };
        }
    }
}