// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.IO;
using System.Xml;
using UnityEngine;
using Object = UnityEngine.Object;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.Utils;

namespace UnityEditor
{
    [Serializable]
    internal struct SceneViewInfo
    {
        public int total_scene_views;
        public int num_of_2d_views;
        public bool is_default_2d_mode;
    }

    [Serializable]
    struct AndroidBuildPermissions
    {
        public string[] features;
        public string[] permissions;
    }

    internal class BuildEventsHandlerPostProcess : IPostprocessBuildWithReport
    {
        private static bool s_EventSent = false;
        private static int s_NumOfSceneViews = 0;
        private static int s_NumOf2dSceneViews = 0;
        private const string s_GradlePath = "Temp/gradleOut/build/intermediates/manifests/full";
        private const string s_StagingArea = "Temp/StagingArea";
        private const string s_AndroidManifest = "AndroidManifest.xml";

        public int callbackOrder {get { return 0; }}
        public void OnPostprocessBuild(BuildReport report)
        {
            ReportSceneViewInfo();
            if (EditorUserBuildSettings.activeBuildTarget == BuildTarget.Android)
            {
                ReportBuildTargetPermissions();
            }
        }

        private void ReportSceneViewInfo()
        {
            Object[] views = Resources.FindObjectsOfTypeAll(typeof(SceneView));
            int numOf2dSceneViews = 0;
            foreach (SceneView view in views)
            {
                if (view.in2DMode)
                    numOf2dSceneViews++;
            }
            if ((s_NumOfSceneViews != views.Length) || (s_NumOf2dSceneViews != numOf2dSceneViews) || !s_EventSent)
            {
                s_EventSent = true;
                s_NumOfSceneViews = views.Length;
                s_NumOf2dSceneViews = numOf2dSceneViews;
                EditorAnalytics.SendEventSceneViewInfo(new SceneViewInfo()
                {
                    total_scene_views = s_NumOfSceneViews, num_of_2d_views = s_NumOf2dSceneViews,
                    is_default_2d_mode = EditorSettings.defaultBehaviorMode == EditorBehaviorMode.Mode2D
                });
            }
        }

        private void ReportBuildTargetPermissions()
        {
            List<string> permissionsList = new List<string>();
            List<string> featuresList = new List<string>();
            string manifestFilePath = Path.Combine(s_StagingArea, s_AndroidManifest);
            if (EditorUserBuildSettings.androidBuildSystem == AndroidBuildSystem.Gradle)
            {
                manifestFilePath = (EditorUserBuildSettings.androidBuildType == AndroidBuildType.Release)
                    ? Paths.Combine(s_GradlePath, "release", s_AndroidManifest)
                    : Paths.Combine(s_GradlePath, "debug", s_AndroidManifest);
            }

            XmlDocument manifestFile = new XmlDocument();
            if (File.Exists(manifestFilePath))
            {
                manifestFile.Load(manifestFilePath);
                XmlNodeList permissions = manifestFile.GetElementsByTagName("uses-permission");
                XmlNodeList features = manifestFile.GetElementsByTagName("uses-feature");
                foreach (XmlNode permission in permissions)
                {
                    XmlNode attribute = permission.Attributes ? ["android:name"];
                    if (attribute != null)
                        permissionsList.Add(attribute.Value);
                }

                foreach (XmlNode feature in features)
                {
                    XmlNode attribute = feature.Attributes ? ["android:name"];
                    if (attribute != null)
                        featuresList.Add(attribute.Value);
                }

                EditorAnalytics.SendEventBuildTargetPermissions(new AndroidBuildPermissions()
                {
                    features = featuresList.ToArray(),
                    permissions = permissionsList.ToArray()
                });
            }
        }
    }
} // namespace