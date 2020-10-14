﻿/*
 * Send .csd files to the StreamAssets folder where it will be read from
 * Files can be saved directly to streaming Assets folder, but it's better to follow
 * the usual project hierarchy and place them in the scripts folder
 */
#if UNITY_EDITOR

using UnityEditor;
using UnityEngine;
using System.IO;

public class SendToStreamingAssets : AssetPostprocessor
{
    public static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths)
    {
        if (!Directory.Exists(Application.streamingAssetsPath))
        {
            Directory.CreateDirectory(Application.streamingAssetsPath);
        }

        if (!Directory.Exists(Application.streamingAssetsPath + "/CsoundFiles"))
        {
            Directory.CreateDirectory(Application.streamingAssetsPath + "/CsoundFiles");
        }

        foreach (string asset in importedAssets)
        {
            if (asset.EndsWith(".csd"))
            {
                string newFileName = Application.streamingAssetsPath + "/CsoundFiles/" + Path.GetFileName(asset);

                if (!File.Exists(newFileName))
                {

                    using (StreamWriter sw = new StreamWriter(newFileName))
                    {
                        using (StreamReader reader = new StreamReader(asset))
                        {
                            string line = reader.ReadLine();
                            while (line != null)
                            {
                                sw.WriteLine(line);
                                line = reader.ReadLine();
                            }
                        }
                    }

                    AssetDatabase.Refresh(ImportAssetOptions.Default);
                }
            }
            else if (asset.EndsWith(".orc"))
            {
                //string filePath = asset.Substring(0, asset.Length - Path.GetFileName(asset).Length) + "Generated Assets/";
                string newFileName = Application.streamingAssetsPath + "/CsoundFiles/" + Path.GetFileName(asset);

                if (!File.Exists(newFileName))
                {
                    using (StreamWriter sw = new StreamWriter(newFileName))
                    {
                        using (StreamReader reader = new StreamReader(asset))
                        {
                            string line = reader.ReadLine();
                            while (line != null)
                            {
                                sw.WriteLine(line);
                                line = reader.ReadLine();
                            }
                        }
                    }

                    AssetDatabase.Refresh(ImportAssetOptions.Default);
                }
            }
        }
    }
}

#endif