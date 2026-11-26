using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace GiaoQua.EditorTools
{
    /// <summary>
    /// Builds the animation clips and one Animator controller per hero skin (p1, p2, p3) from the Kenney frames.
    /// Parameters: Speed (float), Grounded (bool), Ducking (bool), Hurt (trigger).
    /// States: Idle, Walk, Air, Duck, Hurt.
    /// </summary>
    public static class HeroAnimationBuilder
    {
        public static readonly string[] Skins = { "p1", "p2", "p3" };
        public const string OutputFolder = "Assets/Animation/Hero";
        const string PlayerFolder = "Assets/Art/Kenney/PlatformerArtDeluxe/Player";
        const float WalkFps = 12f;
        const float HurtSeconds = 0.4f;

        [MenuItem("Tools/Giao Qua/Tạo hoạt ảnh nhân vật")]
        public static void BuildAll()
        {
            Build();
            if (Application.isBatchMode) EditorApplication.Exit(0);
        }

        public static void Build()
        {
            Directory.CreateDirectory(OutputFolder);
            foreach (string skin in Skins) BuildSkin(skin);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[GiaoQua] Hero animations built.");
        }

        static Sprite Load(string path)
        {
            var s = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (s == null) throw new FileNotFoundException("sprite not found: " + path);
            return s;
        }

        static AnimationClip MakeClip(string name, IList<Sprite> frames, float fps, float minLength, bool loop)
        {
            var clip = new AnimationClip { name = name, frameRate = 60f };
            var binding = new EditorCurveBinding { type = typeof(SpriteRenderer), path = "", propertyName = "m_Sprite" };
            var keys = new List<ObjectReferenceKeyframe>();
            for (int i = 0; i < frames.Count; i++)
                keys.Add(new ObjectReferenceKeyframe { time = i / fps, value = frames[i] });
            float end = Mathf.Max(frames.Count / fps, minLength);
            keys.Add(new ObjectReferenceKeyframe { time = end, value = frames[frames.Count - 1] });
            AnimationUtility.SetObjectReferenceCurve(clip, binding, keys.ToArray());

            var settings = AnimationUtility.GetAnimationClipSettings(clip);
            settings.loopTime = loop;
            AnimationUtility.SetAnimationClipSettings(clip, settings);
            return clip;
        }

        static AnimationClip SaveClip(AnimationClip clip, string skin)
        {
            string path = $"{OutputFolder}/{skin}_{clip.name}.anim";
            clip.name = $"{skin}_{clip.name}";
            AssetDatabase.DeleteAsset(path);
            AssetDatabase.CreateAsset(clip, path);
            return clip;
        }

        static void BuildSkin(string skin)
        {
            var walkFrames = new List<Sprite>();
            for (int i = 1; i <= 11; i++) walkFrames.Add(Load($"{PlayerFolder}/{skin}_walk/{skin}_walk{i:00}.png"));
            Sprite stand = Load($"{PlayerFolder}/{skin}_stand.png");

            var idle = SaveClip(MakeClip("Idle", new[] { stand }, 1f, 1f, true), skin);
            var walk = SaveClip(MakeClip("Walk", walkFrames, WalkFps, 0f, true), skin);
            var air = SaveClip(MakeClip("Air", new[] { Load($"{PlayerFolder}/{skin}_jump.png") }, 1f, 1f, true), skin);
            var duck = SaveClip(MakeClip("Duck", new[] { Load($"{PlayerFolder}/{skin}_duck.png") }, 1f, 1f, true), skin);
            var hurt = SaveClip(MakeClip("Hurt", new[] { Load($"{PlayerFolder}/{skin}_hurt.png") }, 1f, HurtSeconds, false), skin);

            string controllerPath = $"{OutputFolder}/{skin}.controller";
            AssetDatabase.DeleteAsset(controllerPath);
            var c = AnimatorController.CreateAnimatorControllerAtPath(controllerPath);
            c.AddParameter("Speed", AnimatorControllerParameterType.Float);
            c.AddParameter("Grounded", AnimatorControllerParameterType.Bool);
            c.AddParameter("Ducking", AnimatorControllerParameterType.Bool);
            c.AddParameter("Hurt", AnimatorControllerParameterType.Trigger);

            var sm = c.layers[0].stateMachine;
            var sIdle = sm.AddState("Idle"); sIdle.motion = idle;
            var sWalk = sm.AddState("Walk"); sWalk.motion = walk;
            var sAir = sm.AddState("Air"); sAir.motion = air;
            var sDuck = sm.AddState("Duck"); sDuck.motion = duck;
            var sHurt = sm.AddState("Hurt"); sHurt.motion = hurt;
            sm.defaultState = sIdle;

            Link(sIdle, sWalk, ("Speed", AnimatorConditionMode.Greater, 0.1f), ("Grounded", AnimatorConditionMode.If, 0f));
            Link(sWalk, sIdle, ("Speed", AnimatorConditionMode.Less, 0.1f));
            foreach (var from in new[] { sIdle, sWalk, sDuck })
                Link(from, sAir, ("Grounded", AnimatorConditionMode.IfNot, 0f));
            Link(sAir, sIdle, ("Grounded", AnimatorConditionMode.If, 0f));
            foreach (var from in new[] { sIdle, sWalk })
                Link(from, sDuck, ("Ducking", AnimatorConditionMode.If, 0f), ("Grounded", AnimatorConditionMode.If, 0f));
            Link(sDuck, sIdle, ("Ducking", AnimatorConditionMode.IfNot, 0f));

            var anyHurt = sm.AddAnyStateTransition(sHurt);
            anyHurt.hasExitTime = false;
            anyHurt.duration = 0f;
            anyHurt.canTransitionToSelf = false;
            anyHurt.AddCondition(AnimatorConditionMode.If, 0f, "Hurt");
            var back = sHurt.AddTransition(sIdle);
            back.hasExitTime = true;
            back.exitTime = 1f;
            back.duration = 0f;

            EditorUtility.SetDirty(c);
        }

        static void Link(AnimatorState from, AnimatorState to, params (string param, AnimatorConditionMode mode, float value)[] conditions)
        {
            var t = from.AddTransition(to);
            t.hasExitTime = false;
            t.duration = 0f;
            foreach (var cond in conditions) t.AddCondition(cond.mode, cond.value, cond.param);
        }
    }
}
