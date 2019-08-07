using UnityEngine;
using UnityEditor;
using UnityEditor.PackageManager;
using UnityEditor.PackageManager.Requests;
using System;

public class PackageChecker : ScriptableObject
{
    // The current version of the package checker
    // This will be updated each time the packages are checked so it only happens once each version
    public int version = -1;

    // The toolbar entry in the editor. Change this to better reflect your asset or project
    const string k_MenuEntry = "Tools/PackageChecker/Check Packages";

    // The path to store the asset
    const string k_DefaultPackageCheckerPath = "Assets/PackageChecker/Editor/PackageCheckerInfo.asset";

    // The target version of the package checker. Increment this each time you update the packages array below
    const int k_TargetVersion = 1;

    // The packages to add (if you change this, increment the target version above)
    // "com.unity.cinemachine@2.2.10-preview.3" for specific version
    // See https://docs.unity3d.com/ScriptReference/PackageManager.Client.Add.html for more details
    readonly string[] k_Packages = new string[]
    {
        "com.unity.cinemachine"
    };


    [MenuItem(k_MenuEntry)]
    static void CheckPackagesMenuEntry ()
    {
        CheckPackages(true);
    }

    [InitializeOnLoadMethod]
    static void InitializeOnLoad()
    {
        // Skip if playing
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            return;
        else
        {
            // If the editor is busy, wait for it to be done
            if (EditorApplication.isUpdating || EditorApplication.isCompiling)
                EditorApplication.update += WaitForEditor;
            else
                CheckPackages();
        }
    }
    
    static void WaitForEditor()
    {
        // Called each editor update and waits for editor to stop importing / compiling assets
        if (!EditorApplication.isUpdating && !EditorApplication.isCompiling)
        {
            CheckPackages();
            EditorApplication.update -= WaitForEditor;
        }
    }

    static void CheckPackages (bool ignoreVersion = false)
    {
        PackageChecker instance;

        // Load or create a PackageChecker instance
        var guids = AssetDatabase.FindAssets("t:PackageChecker");
        if (guids != null && guids.Length > 0)
        {
            instance = AssetDatabase.LoadAssetAtPath<PackageChecker>(AssetDatabase.GUIDToAssetPath(guids[0]));
        }
        else
        {
            instance = CreateInstance<PackageChecker>();
            AssetDatabase.CreateAsset(instance, k_DefaultPackageCheckerPath);
        }

        // Only actually perform the check if it hasn't been done for the target version
        if (ignoreVersion || instance.version < k_TargetVersion)
            instance.CheckPackagesInternal();
    }

    readonly char[] k_VersionSeparator = new char[] { '@' };

    private int m_PackageIndex = -1;
    private SearchRequest m_SearchRequest;
    private AddRequest m_AddRequest;

    void CheckPackagesInternal ()
    {
        if (k_Packages != null && k_Packages.Length > 0)
        {
            Debug.Log("Checking Package Dependencies");

            // Check the first package
            m_PackageIndex = -1;
            CheckNextPackage();

            // Wait for check to complete
            EditorApplication.update += WaitForSearch;
        }

        version = k_TargetVersion;
    }

    bool CheckNextPackage()
    {
        ++m_PackageIndex;
        if (m_PackageIndex < k_Packages.Length)
        {
            // Search for the next package
            m_SearchRequest = Client.Search(k_Packages[m_PackageIndex]);
            return true;
        }
        else
            return false;
    }

    void InstallPackage()
    {
        // Unsubscribe from event
        EditorApplication.update -= WaitForSearch;

        // Package not installed. Send add request
        m_AddRequest = Client.Add(k_Packages[m_PackageIndex]);
        EditorApplication.update += WaitForInstall;
    }

    void WaitForSearch()
    {
        if (!m_SearchRequest.IsCompleted)
            return;

        if (m_SearchRequest.Status == StatusCode.Success)
        {
            var package = m_SearchRequest.Result[0];

            if (package.status == PackageStatus.Available)
            {
                // Found. Check for version specification if required
                if (k_Packages[m_PackageIndex].Contains("@"))
                {
                    // Check version
                    Version vInstalled = new Version(package.version);
                    Version vRequired = new Version(k_Packages[m_PackageIndex].Split(k_VersionSeparator)[1]);
                    if (vRequired > vInstalled)
                    {
                        // Out of date. Install
                        InstallPackage();
                    }
                    else
                    {
                        // Up to date. Move to next
                        if (!CheckNextPackage())
                        {
                            EditorApplication.update -= WaitForSearch;
                            OnComplete();
                        }
                    }
                }
                else
                {
                    // Version not specified. Move to next
                    if (!CheckNextPackage())
                    {
                        EditorApplication.update -= WaitForSearch;
                        OnComplete();
                    }
                }
            }
            else
            {
                // Not found. Install it
                InstallPackage();
            }
        }
        else
        {
            Debug.Log("Checking for invalid package. Please check the name is correct: " + k_Packages[m_PackageIndex]);
            if (!CheckNextPackage())
            {
                EditorApplication.update -= WaitForSearch;
                OnComplete();
            }
        }
    }

    void WaitForInstall()
    {
        if (!m_AddRequest.IsCompleted)
            return;

        EditorApplication.update -= WaitForInstall;

        if (CheckNextPackage())
            EditorApplication.update += WaitForSearch;
        else
            OnComplete();
    }

    void OnComplete ()
    {
        // Add any code to run after completion here
    }
}
