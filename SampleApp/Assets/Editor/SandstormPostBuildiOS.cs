#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEditor.iOS.Xcode;
using UnityEngine;

class SandstormPostBuildiOS
{
    public const string LocationWhenInUseDescription = "To determine the type of place the user is situated in while interacting with the app.";
    public const string TrackingUsageDescription = "This identifier will be used to deliver personalized ads to you.";
    public const string MotionUsageDescription = "To determine physical interaction with the device while using the app.";

    [PostProcessBuild]
    public static void OnPostProcessBuild(BuildTarget buildTarget, string buildPath)
    {
        if (buildTarget != BuildTarget.iOS)
        {
            return;
        }

        var projPath = PBXProject.GetPBXProjectPath(buildPath);
        var proj = new PBXProject();
        proj.ReadFromFile(projPath);
        var targetGuid = proj.GetUnityMainTargetGuid();
        var targetGuidFramework = proj.GetUnityFrameworkTargetGuid();

        proj.SetBuildProperty(targetGuid, "GENERATE_INFOPLIST_FILE", "YES");
        proj.SetBuildProperty(targetGuidFramework, "BUILD_LIBRARY_FOR_DISTRIBUTION", "YES");

        proj.WriteToFile(projPath);


        var plistPath = buildPath + "/Info.plist";
        var plist = new PlistDocument();
        plist.ReadFromString(File.ReadAllText(plistPath));
        PlistElementDict rootDict = plist.root;
        rootDict.SetString("NSUserTrackingUsageDescription", TrackingUsageDescription);
        rootDict.SetString("NSLocationWhenInUseUsageDescription", LocationWhenInUseDescription);
        rootDict.SetString("NSMotionUsageDescription", MotionUsageDescription);
        File.WriteAllText(plistPath, plist.WriteToString());
    }

}
#endif