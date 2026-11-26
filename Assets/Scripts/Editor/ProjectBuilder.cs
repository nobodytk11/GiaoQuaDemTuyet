using UnityEditor;
using UnityEngine;

namespace GiaoQua.EditorTools
{
    /// <summary>One call that rebuilds everything generated: settings, animations, banks, levels, menu scenes.</summary>
    public static class ProjectBuilder
    {
        [MenuItem("Tools/Giao Qua/Dựng lại tất cả")]
        public static void BuildEverything()
        {
            ProjectSetup.Apply();
            HeroAnimationBuilder.Build();
            ResourceBanksBuilder.Build();
            LevelBuilder.Build();
            MenuSceneBuilder.Build();
            AssetDatabase.SaveAssets();
            Debug.Log("[GiaoQua] BuildEverything done.");
            if (Application.isBatchMode) EditorApplication.Exit(0);
        }
    }
}
