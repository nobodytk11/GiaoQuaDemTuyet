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
            (10, "Player"), (11, "Projectile"), (12, "Gift"),
        };
        static readonly string[] Tags = { "Gift", "Enemy", "Hazard", "Collectible", "Checkpoint" };

        // Pairs of layers that must NOT collide (everything else keeps the default).
        static readonly string[][] IgnoredPairs =
        {
            new[] { "Enemy", "Enemy" }, new[] { "Enemy", "Item" }, new[] { "Enemy", "Gift" }, new[] { "Enemy", "Hazard" },
            new[] { "Player", "Gift" }, new[] { "Player", "Projectile" },
            new[] { "Projectile", "Projectile" }, new[] { "Projectile", "Item" }, new[] { "Projectile", "Gift" },
            new[] { "Projectile", "Hazard" }, new[] { "Gift", "Hazard" },
        };

        static void ApplyCollisionMatrix()
        {
            foreach (var pair in IgnoredPairs)
                Physics2D.IgnoreLayerCollision(LayerMask.NameToLayer(pair[0]), LayerMask.NameToLayer(pair[1]), true);
            // the chimney trigger sits on the Item layer and must see the gift
            Physics2D.IgnoreLayerCollision(LayerMask.NameToLayer("Gift"), LayerMask.NameToLayer("Item"), false);
            AssetDatabase.SaveAssets();
        }

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
            ApplyCollisionMatrix();

            foreach (string dir in new[] { "Assets/Art", "Assets/Kit", "Assets/Levels", "Assets/Scenes", "Assets/Scripts" })
                Directory.CreateDirectory(dir);
            AssetDatabase.Refresh();
            Debug.Log("[GiaoQua] ProjectSetup done. Ground layer = " + LayerMask.NameToLayer("Ground"));
        }
    }
}
