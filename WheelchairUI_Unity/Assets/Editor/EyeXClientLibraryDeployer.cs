//-----------------------------------------------------------------------
// Copyright 2014 Tobii Technology AB. All rights reserved.
//-----------------------------------------------------------------------
 
using UnityEngine;
using UnityEditor;
using System;
using System.IO;
using UnityEditor.Callbacks;
 
/// <summary>
/// Editor class for deployment of the Tobii EyeX Client library and .NET binding
/// to where they need to be.
/// </summary>
[InitializeOnLoad]
public class EyeXClientLibraryDeployer
{
    private const string ClientLibraryFileName = "Tobii.EyeX.Client.dll";
 
    private const string Source32BitDirectory = "Assets/Plugins/x86";
    private const string Source64BitDirectory = "Assets/Plugins/x86_64";
 
    /// <summary>
    /// When loading the editor, copy the correct version of the EyeX client
    /// library to the project root folder to be able to run in the editor.
    /// </summary>
    static EyeXClientLibraryDeployer()
    {
        var targetClientLibraryPath = Path.Combine(Directory.GetCurrentDirectory(), ClientLibraryFileName);
        if(!File.Exists(targetClientLibraryPath))
        {
            if(System.IntPtr.Size == 8)
            {
            
                Copy64BitClientLibrary(targetClientLibraryPath);
            }
            else
            {
                Copy32BitClientLibrary(targetClientLibraryPath);
            }
        }              
    }
 
    /// <summary>
    /// After a build, copy the correct EyeX client library to the output directory.
    /// </summary>
    [PostProcessBuild]
    public static void OnPostProcessBuild(BuildTarget target, string pathToBuiltProject)
    {
        var targetClientLibraryPath = Path.Combine(Path.GetDirectoryName(pathToBuiltProject), ClientLibraryFileName);
        if(!File.Exists(targetClientLibraryPath))
        {
            if (target == BuildTarget.StandaloneWindows)
            {
                if(System.IntPtr.Size == 8)
                {
                
                    Copy64BitClientLibrary(targetClientLibraryPath);
                }
                else
                {
                    Copy32BitClientLibrary(targetClientLibraryPath);
                }            
            }
            else if (target == BuildTarget.StandaloneWindows64)
            {
                Copy64BitClientLibrary(targetClientLibraryPath);
            }
            else
            {
                Debug.LogWarning("The Tobii EyeX Framework for Unity is only compatible with Windows Standalone builds.");
            }
        }
    }
 
    private static void Copy32BitClientLibrary(string targetClientDllPath)
    {
        File.Copy(Path.Combine(Source32BitDirectory, ClientLibraryFileName), targetClientDllPath, true);
    }
 
    private static void Copy64BitClientLibrary(string targetClientDllPath)
    {
        File.Copy(Path.Combine(Source64BitDirectory, ClientLibraryFileName), targetClientDllPath, true);
    }
}