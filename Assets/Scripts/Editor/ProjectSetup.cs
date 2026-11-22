using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEngine;

namespace GiaoQua.EditorTools
{
    /// <summary>Player settings, tags and physics layers that every scene relies on.</summary>
    public static class ProjectSetup
    {
        public const string ProductName = "GiaoQuaDemTuyet";
        public const string CompanyName = "Nhom06";

        // Layer slots (names are what code uses; the numbers only matter inside the collision matrix).
        static readonly (int slot, string name)[] Layers =
        {
            (6, "Ground"), (7, "Enemy"), (8, "Hazard"), (9, "Item"),
        };
        static readonly string[] Tags = { "Gift", "Enemy", "Hazard", "Collectible", "Checkpoint" };

        [MenuItem("Tools/Giao Qua/Cài đặt project")]
        public static void Apply()
        {
            PlayerSettings.companyName = CompanyName;
            PlayerSettings.productName = ProductName;
            PlayerSettings.bundleVersion = "1.0.0";
            PlayerSettings.defaultScreenWidth = 1280;
            PlayerSettings.defaultScreenHeight = 720;
            PlayerSettings.fullScreenMode = FullScreenMode.Windowed;
            PlayerSettings.resizableWindow = true;
            PlayerSettings.runInBackground = true;
            PlayerSettings.SetScriptingBackend(NamedBuildTarget.Standalone, ScriptingImplementation.Mono2x);
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Android, "com.nhom06.giaoquademtuyet");
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.iOS, "com.nhom06.giaoquademtuyet");

            var tagManager = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);

            SerializedProperty tags = tagManager.FindProperty("tags");
            foreach (string tag in Tags)
            {
                bool found = false;
                for (int i = 0; i < tags.arraySize; i++)
                    if (tags.GetArrayElementAtIndex(i).stringValue == tag) { found = true; break; }
                if (found) continue;
                tags.InsertArrayElementAtIndex(tags.arraySize);
                tags.GetArrayElementAtIndex(tags.arraySize - 1).stringValue = tag;
            }

            SerializedProperty layers = tagManager.FindProperty("layers");
            foreach (var (slot, name) in Layers)
            {
                SerializedProperty p = layers.GetArrayElementAtIndex(slot);
                if (string.IsNullOrEmpty(p.stringValue)) p.stringValue = name;
            }

            tagManager.ApplyModifiedProperties();
            AssetDatabase.SaveAssets();

            foreach (string dir in new[] { "Assets/Art", "Assets/Kit", "Assets/Levels", "Assets/Scenes", "Assets/Scripts" })
                Directory.CreateDirectory(dir);
            AssetDatabase.Refresh();
            Debug.Log("[GiaoQua] ProjectSetup done. Ground layer = " + LayerMask.NameToLayer("Ground"));
        }
    }
}
