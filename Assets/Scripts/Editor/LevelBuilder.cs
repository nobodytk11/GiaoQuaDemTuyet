using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using GiaoQua.Gameplay;

namespace GiaoQua.EditorTools
{
    /// <summary>
    /// Turns Assets/Levels/LevelN.txt (a level drawn with characters, see LevelMap) into the scene Assets/Scenes/LevelN.unity.
    /// Legend (one character = one tile):
    ///  # ground  I ice  H roof  W wall  S start  o coin  h heart  b boots  s shield  ? coin box  ! heart box
    ///  ^ spikes  c checkpoint  P spring  e slime  n snail  f flyer  k chaser  m/v moving platform  t melting platform
    ///  i icicle  L seesaw  C chimney  T pine tree
    /// </summary>
    public static class LevelBuilder
    {
        const string Deluxe = "Assets/Art/Kenney/PlatformerArtDeluxe/";
        const string Winter = "Assets/Art/Kenney/PlatformerArtWinter/Tiles/";
        const string Buildings = "Assets/Art/Kenney/PlatformerArtBuildings/Tiles/";
        const string Own = "Assets/Art/Own/";
        const string LevelsFolder = "Assets/Levels/";
        const string ScenesFolder = "Assets/Scenes/";

        static Material unlit, sparkle;
        static PhysicsMaterial2D heroPhysics;
        static LevelMap map;
        static Transform terrain, decor, items, enemies;
        static readonly Dictionary<string, Sprite> sprites = new Dictionary<string, Sprite>();

        // ---------------------------------------------------------------- entry points
        [MenuItem("Tools/Giao Qua/Dựng cả 3 màn từ bản đồ chữ")]
        public static void BuildAll()
        {
            Build();
            if (Application.isBatchMode) EditorApplication.Exit(0);
        }

        public static void Build()
        {
            for (int i = 0; i < LevelCatalog.Count; i++)
            {
                string name = LevelCatalog.Levels[i].Scene;
                if (File.Exists(LevelsFolder + name + ".txt")) BuildLevel(name, i);
                else Debug.Log("[GiaoQua] No map for " + name + ", skipped.");
            }
            BuildScript.SyncBuildSettings();
        }

        [MenuItem("Tools/Giao Qua/Kiểm tra bản đồ màn (Validate)")]
        public static void ValidateAll()
        {
            bool bad = false;
            for (int i = 0; i < LevelCatalog.Count; i++)
            {
                string path = LevelsFolder + LevelCatalog.Levels[i].Scene + ".txt";
                if (!File.Exists(path)) continue;
                var issues = LevelValidator.Validate(LevelMap.Parse(File.ReadAllText(path)));
                foreach (var issue in issues) { Debug.Log($"[{LevelCatalog.Levels[i].Scene}] {issue}"); if (issue.IsError) bad = true; }
                if (issues.Count == 0) Debug.Log($"[{LevelCatalog.Levels[i].Scene}] OK, không có vấn đề.");
            }
            if (Application.isBatchMode) EditorApplication.Exit(bad ? 1 : 0);
        }

        public static void BuildLevel(string sceneName, int levelIndex)
        {
            map = LevelMap.Parse(File.ReadAllText(LevelsFolder + sceneName + ".txt"));
            foreach (var issue in LevelValidator.Validate(map))
            {
                if (issue.IsError) throw new System.InvalidOperationException($"{sceneName}: {issue}");
                Debug.LogWarning($"[{sceneName}] {issue}");
            }

            Prepare();
            Directory.CreateDirectory(ScenesFolder);
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            terrain = new GameObject("Terrain").transform;
            decor = new GameObject("Decor").transform;
            items = new GameObject("Items").transform;
            enemies = new GameObject("Enemies").transform;

            BuildCamera(out var cam);
            BuildBackground();
            BuildTerrain();
            BuildCells(levelIndex);
            BuildRegions();
            BuildKillZone();
            BuildSnow(cam);

            EditorSceneManager.SaveScene(scene, ScenesFolder + sceneName + ".unity");
            Debug.Log($"[GiaoQua] Built {sceneName}: {map.Width}x{map.Height} tiles.");
        }

        /// <summary>Loads the shared material before any scene part is built.</summary>
        internal static void Prepare()
        {
            sprites.Clear();
            unlit = EnsureAssets();
        }

        // ---------------------------------------------------------------- assets
        internal static Material EnsureAssets()
        {
            Directory.CreateDirectory("Assets/Resources");
            Directory.CreateDirectory("Assets/Materials");
            const string matPath = "Assets/Resources/SpriteUnlit.mat";
            var mat = AssetDatabase.LoadAssetAtPath<Material>(matPath);
            if (mat == null)
            {
                var shader = Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default");
                if (shader == null) throw new System.InvalidOperationException("URP 2D unlit sprite shader not found");
                mat = new Material(shader) { name = "SpriteUnlit" };
                AssetDatabase.CreateAsset(mat, matPath);
            }
            const string physPath = "Assets/Materials/HeroPhysics.asset";
            heroPhysics = AssetDatabase.LoadAssetAtPath<PhysicsMaterial2D>(physPath);
            if (heroPhysics == null)
            {
                heroPhysics = new PhysicsMaterial2D("HeroPhysics") { friction = 0f, bounciness = 0f };
                AssetDatabase.CreateAsset(heroPhysics, physPath);
            }
            const string sparklePath = "Assets/Materials/Sparkle.mat";
            sparkle = AssetDatabase.LoadAssetAtPath<Material>(sparklePath);
            if (sparkle == null)
            {
                var shader = Shader.Find("GiaoQua/SparkleSprite");
                if (shader == null) throw new System.InvalidOperationException("GiaoQua/SparkleSprite shader not found");
                sparkle = new Material(shader) { name = "Sparkle" };
                AssetDatabase.CreateAsset(sparkle, sparklePath);
            }
            const string snowPath = "Assets/Materials/Snow.mat";
            if (AssetDatabase.LoadAssetAtPath<Material>(snowPath) == null)
            {
                var m = new Material(Shader.Find("Sprites/Default")) { name = "Snow" };
                m.mainTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(Own + "snowflake.png");
                AssetDatabase.CreateAsset(m, snowPath);
            }
            AssetDatabase.SaveAssets();
            return mat;
        }

        internal static Sprite Spr(string path)
        {
            if (sprites.TryGetValue(path, out var s)) return s;
            s = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (s == null) throw new FileNotFoundException("sprite not found: " + path);
            return sprites[path] = s;
        }

        // ---------------------------------------------------------------- helpers
        static GameObject Make(string name, Transform parent, Vector2 pos, string layer = null)
        {
            var go = new GameObject(name);
            if (parent != null) go.transform.SetParent(parent, false);
            go.transform.position = pos;
            if (layer != null) go.layer = LayerMask.NameToLayer(layer);
            return go;
        }

        static SpriteRenderer AddSprite(GameObject go, Sprite sprite, int order, float scale = 1f)
        {
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.sharedMaterial = unlit;
            sr.sortingOrder = order;
            if (!Mathf.Approximately(scale, 1f)) go.transform.localScale = Vector3.one * scale;
            return sr;
        }

        static BoxCollider2D AddBox(GameObject go, Vector2 size, Vector2 offset = default, bool trigger = false)
        {
            var b = go.AddComponent<BoxCollider2D>();
            b.size = size;
            b.offset = offset;
            b.isTrigger = trigger;
            return b;
        }

        // ---------------------------------------------------------------- scene parts
        static void BuildCamera(out Camera cam)
        {
            var go = new GameObject("Main Camera") { tag = "MainCamera" };
            cam = go.AddComponent<Camera>();
            cam.orthographic = true;
            cam.orthographicSize = 5.2f;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.043f, 0.114f, 0.227f);
            go.AddComponent<AudioListener>();
            var follow = go.AddComponent<CameraFollow2D>();
            follow.bounds = new Rect(0f, 0f, map.Width, map.Height);
            map.Find('S', out int sc, out int sr);
            go.transform.position = new Vector3(map.Center(sc, sr).x, map.Center(sc, sr).y + 1.6f, -10f);
        }

        internal static void BuildBackground()
        {
            var root = new GameObject("Background");
            AddParallax(root.transform, "Stars", Own + "bg_stars.png", 0.04f, 0f, -30, 1.4f);
            AddParallax(root.transform, "HillsFar", Own + "hills_far.png", 0.18f, -2.35f, -20, 1.4f);
            AddParallax(root.transform, "HillsNear", Own + "hills_near.png", 0.38f, -3.1f, -10, 1.4f);
        }

        internal static void AddParallax(Transform parent, string name, string spritePath, float factor, float offsetY, int order, float scale)
        {
            var layer = new GameObject(name);
            layer.transform.SetParent(parent, false);
            var sprite = Spr(spritePath);
            float width = sprite.bounds.size.x * scale;
            for (int i = 0; i < 3; i++)
            {
                var copy = new GameObject(name + i);
                copy.transform.SetParent(layer.transform, false);
                AddSprite(copy, sprite, order, scale);
            }
            var p = layer.AddComponent<ParallaxLayer>();
            p.factor = factor;
            p.tileWidth = width;
            p.offset = new Vector2(0f, offsetY);
        }

        static void BuildTerrain()
        {
            // colliders: few big rectangles
            AddColliderRects("Ground", new[] { LevelMap.Solid }, false);
            AddColliderRects("Ice", new[] { LevelMap.Ice }, true);
            AddColliderRects("House", new[] { LevelMap.Roof, LevelMap.Wall }, false);

            // pictures: one sprite per tile
            foreach (var (c, r, ch) in map.Cells())
            {
                if (ch != LevelMap.Solid && ch != LevelMap.Ice && ch != LevelMap.Roof && ch != LevelMap.Wall) continue;
                Sprite s;
                bool same(int cc, int rr) => map.At(cc, rr) == ch;
                bool topExposed = !map.IsSolid(c, r - 1);
                switch (ch)
                {
                    case LevelMap.Ice: s = Spr(Winter + "iceBlock.png"); break;
                    case LevelMap.Wall: s = Spr(Deluxe + "Tiles/brickWall.png"); break;
                    case LevelMap.Roof:
                        s = !same(c - 1, r) ? Spr(Buildings + "roofRedLeft.png") : !same(c + 1, r) ? Spr(Buildings + "roofRedRight.png") : Spr(Buildings + "roofRedMid.png");
                        break;
                    default:
                        if (!topExposed) s = Spr(Deluxe + "Tiles/snowCenter.png");
                        else if (!map.IsSolid(c - 1, r) && map.IsSolid(c + 1, r)) s = Spr(Deluxe + "Tiles/snowLeft.png");
                        else if (map.IsSolid(c - 1, r) && !map.IsSolid(c + 1, r)) s = Spr(Deluxe + "Tiles/snowRight.png");
                        else s = Spr(Deluxe + "Tiles/snowMid.png");
                        break;
                }
                var go = Make("tile", terrain, map.Center(c, r));
                var tileRenderer = AddSprite(go, s, 0);
                if (ch == LevelMap.Ice) tileRenderer.sharedMaterial = sparkle;
            }
        }

        static void AddColliderRects(string name, char[] kinds, bool ice)
        {
            foreach (var rect in map.MergedRects(kinds))
            {
                var center = new Vector2(rect.x + rect.width / 2f, map.Height - rect.y - rect.height / 2f);
                var go = Make(name, terrain, center, "Ground");
                AddBox(go, new Vector2(rect.width, rect.height));
                if (ice) go.AddComponent<IceSurface>();
            }
        }

        static void BuildKillZone()
        {
            var go = Make("KillZone", null, new Vector2(map.Width / 2f, -7f), "Hazard");
            AddBox(go, new Vector2(map.Width + 80f, 4f), default, true);
            go.AddComponent<KillZone>();
        }

        internal static void BuildSnow(Camera cam)
        {
            var go = new GameObject("Snow");
            go.transform.SetParent(cam.transform, false);
            go.transform.localPosition = new Vector3(0f, 7f, 5f);
            var ps = go.AddComponent<ParticleSystem>();
            var main = ps.main;
            main.loop = true;
            main.startLifetime = 7f;
            main.startSpeed = 0f;
            main.startSize = new ParticleSystem.MinMaxCurve(0.1f, 0.26f);
            main.startColor = new Color(1f, 1f, 1f, 0.85f);
            main.maxParticles = 350;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            var em = ps.emission;
            em.rateOverTime = 45f;
            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = new Vector3(24f, 1f, 1f);
            var vel = ps.velocityOverLifetime;
            vel.enabled = true;
            vel.space = ParticleSystemSimulationSpace.World;
            vel.y = new ParticleSystem.MinMaxCurve(-1.6f, -0.8f);
            vel.x = new ParticleSystem.MinMaxCurve(-0.5f, 0.5f);
            vel.z = new ParticleSystem.MinMaxCurve(0f, 0f);      // all three curves must use the same mode
            var rend = go.GetComponent<ParticleSystemRenderer>();
            rend.sharedMaterial = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Snow.mat");
            rend.sortingOrder = 20;
        }

        // ---------------------------------------------------------------- cells
        static void BuildCells(int levelIndex)
        {
            Transform player = null;
            Vector2 spawn = Vector2.zero;
            foreach (var (c, r, ch) in map.Cells())
            {
                Vector2 center = map.Center(c, r);
                Vector2 bottom = map.Bottom(c, r);
                switch (ch)
                {
                    case 'S': spawn = bottom; player = BuildPlayer(bottom); break;
                    case 'o': BuildPickup("Coin", CollectibleKind.Coin, Deluxe + "Items/coinGold.png", center, 0.35f, true); break;
                    case 'h': BuildPickup("Heart", CollectibleKind.Heart, Deluxe + "HUD/hud_heartFull.png", center, 0.4f, true); break;
                    case 'b': BuildPickup("Boots", CollectibleKind.Boots, Own + "boots.png", center, 0.4f, true); break;
                    case 's': BuildPickup("Shield", CollectibleKind.Shield, Own + "shield.png", center, 0.4f, true); break;
                    case '?': BuildBox(center, false); break;
                    case '!': BuildBox(center, true); break;
                    case '^': BuildSpikes(center); break;
                    case 'c': BuildCheckpoint(center); break;
                    case 'P': BuildSpring(center); break;
                    case 'e': BuildEnemy<EnemyPatrol>("Slime", bottom, Deluxe + "Enemies/slimeWalk1.png", new[] { "slimeWalk1", "slimeWalk2" }, 4f); break;
                    case 'n': BuildEnemy<EnemyPatrol>("Snail", bottom, Deluxe + "Enemies/snailWalk1.png", new[] { "snailWalk1", "snailWalk2" }, 3f).speed = 1.0f; break;
                    case 'f': BuildFlyer(bottom + Vector2.up * 0.3f); break;
                    case 'k': BuildChaser(bottom); break;
                    case 'm': BuildMover(center, new Vector2(4f, 0f)); break;
                    case 'v': BuildMover(center, new Vector2(0f, 3.5f)); break;
                    case 't': BuildMelting(center); break;
                    case 'i': BuildIcicle(center); break;
                    case 'T': AddSprite(Make("Pine", decor, center), Spr(Winter + "pineSapling.png"), -2); break;
                }
            }
            BuildSeesawAndChimney();

            new GameObject("HUD").AddComponent<GiaoQua.UI.GameHud>();
            var lc = new GameObject("LevelController").AddComponent<LevelController>();
            lc.levelIndex = levelIndex;
            lc.spawnPoint = spawn;
            var follow = Camera.main != null ? Camera.main.GetComponent<CameraFollow2D>() : Object.FindFirstObjectByType<CameraFollow2D>();
            if (follow != null && player != null) follow.target = player;
        }

        static Transform BuildPlayer(Vector2 feet)
        {
            var controllers = new RuntimeAnimatorController[3];
            var idle = new Sprite[3];
            for (int i = 0; i < 3; i++)
            {
                controllers[i] = AssetDatabase.LoadAssetAtPath<AnimatorController>($"Assets/Animation/Hero/p{i + 1}.controller");
                idle[i] = Spr($"{Deluxe}Player/p{i + 1}_stand.png");
            }
            var go = PlayerFactory.Create(feet, idle[0], controllers[0], useSave: true);
            go.GetComponent<SpriteRenderer>().sharedMaterial = unlit;
            go.GetComponent<BoxCollider2D>().sharedMaterial = heroPhysics;
            go.GetComponent<PlayerShooter>().snowballSprite = Spr(Winter + "snowBall.png");
            var skin = go.AddComponent<PlayerSkin>();
            skin.controllers = controllers;
            skin.idleSprites = idle;
            return go.transform;
        }

        static void BuildPickup(string name, CollectibleKind kind, string spritePath, Vector2 pos, float radius, bool bob)
        {
            var go = Make(name, items, pos, "Item");
            var sprite = Spr(spritePath);
            // all pickups are drawn about 0.8 tiles wide
            float scale = 0.8f / Mathf.Max(sprite.bounds.size.x, sprite.bounds.size.y);
            AddSprite(go, sprite, 3, Mathf.Min(1f, scale)).sharedMaterial = sparkle;
            var col = go.AddComponent<CircleCollider2D>();
            col.radius = radius / go.transform.localScale.x;
            col.isTrigger = true;
            go.AddComponent<Collectible>().kind = kind;
            if (bob) go.AddComponent<Floaty>();
        }

        static void BuildBox(Vector2 center, bool heart)
        {
            var go = Make(heart ? "HeartBox" : "CoinBox", terrain, center, "Ground");
            AddSprite(go, Spr(Deluxe + (heart ? "Tiles/boxItem.png" : "Tiles/boxCoin.png")), 1);
            AddBox(go, Vector2.one);
            var box = go.AddComponent<BonusBox>();
            box.emptySprite = Spr(Deluxe + "Tiles/boxEmpty.png");
            if (heart)
            {
                var content = Make("Heart", go.transform, center + Vector2.up);
                AddSprite(content, Spr(Deluxe + "HUD/hud_heartFull.png"), 3, 0.7f);
                var col = content.AddComponent<CircleCollider2D>();
                col.radius = 0.5f;
                col.isTrigger = true;
                content.layer = LayerMask.NameToLayer("Item");
                content.AddComponent<Collectible>().kind = CollectibleKind.Heart;
                content.SetActive(false);
                box.content = content;
            }
        }

        static void BuildSpikes(Vector2 center)
        {
            var go = Make("Spikes", items, center, "Hazard");
            AddSprite(go, Spr(Deluxe + "Items/spikes.png"), 2);
            AddBox(go, new Vector2(0.86f, 0.4f), new Vector2(0f, -0.28f), true);
            go.AddComponent<Hazard>();
        }

        static void BuildCheckpoint(Vector2 center)
        {
            var go = Make("Checkpoint", items, center, "Item");
            AddSprite(go, Spr(Deluxe + "Items/flagGreenHanging.png"), 2);
            AddBox(go, new Vector2(0.9f, 1.6f), Vector2.zero, true);
            var cp = go.AddComponent<Checkpoint>();
            cp.offSprite = Spr(Deluxe + "Items/flagGreenHanging.png");
            cp.onSprite = Spr(Deluxe + "Items/flagGreen.png");
        }

        static void BuildSpring(Vector2 center)
        {
            var go = Make("Spring", items, center, "Ground");
            var up = Spr(Deluxe + "Items/springboardUp.png");
            AddSprite(go, up, 2);
            AddBox(go, new Vector2(0.95f, 0.7f), new Vector2(0f, -0.15f));
            var pad = go.AddComponent<BouncePad>();
            pad.upSprite = up;
            pad.downSprite = Spr(Deluxe + "Items/springboardDown.png");
        }

        static T BuildEnemy<T>(string name, Vector2 bottom, string spritePath, string[] frameNames, float fps) where T : Enemy
        {
            var go = Make(name, enemies, bottom, "Enemy");
            var sprite = Spr(spritePath);
            AddSprite(go, sprite, 5);
            var rb = go.AddComponent<Rigidbody2D>();
            rb.freezeRotation = true;
            rb.gravityScale = 3f;
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            var size = sprite.bounds.size;
            AddBox(go, new Vector2(size.x * 0.9f, size.y * 0.9f), new Vector2(0f, size.y / 2f));
            var e = go.AddComponent<T>();
            if (frameNames != null)
            {
                var fb = go.AddComponent<SpriteFlipbook>();
                string folder = Deluxe + "Enemies/";
                fb.frames = System.Array.ConvertAll(frameNames, n => Spr(folder + n + ".png"));
                fb.fps = fps;
            }
            return e;
        }

        static void BuildFlyer(Vector2 pos)
        {
            var e = BuildEnemy<EnemyFlyer>("Fly", pos, Deluxe + "Enemies/flyFly1.png", new[] { "flyFly1", "flyFly2" }, 8f);
            e.range = 3f;
            e.speed = 2f;
            var rb = e.GetComponent<Rigidbody2D>();
            rb.gravityScale = 0f;
        }

        static void BuildChaser(Vector2 bottom)
        {
            var e = BuildEnemy<EnemyChaser>("Chaser", bottom, Deluxe + "Enemies/blockerMad.png", null, 1f);
            e.stompable = false;
            e.hp = 2;
        }

        static void BuildMover(Vector2 cell, Vector2 offset)
        {
            // three half-ice blocks wide; the pictures are drawn in the top 57% of their tile
            var top = cell + Vector2.up * 0.5f;
            var go = Make("MovingPlatform", terrain, new Vector2(cell.x, top.y - 0.285f), "Ground");
            AddBox(go, new Vector2(3f, 0.57f));
            go.AddComponent<Rigidbody2D>().bodyType = RigidbodyType2D.Kinematic;
            var mp = go.AddComponent<MovingPlatform>();
            mp.offset = offset;
            mp.speed = offset.y != 0f ? 1.4f : 1.6f;
            for (int i = -1; i <= 1; i++)
            {
                var tile = Make("block", go.transform, Vector2.zero);
                tile.transform.position = new Vector2(cell.x + i, cell.y);
                AddSprite(tile, Spr(Winter + "iceBlockHalf.png"), 1);
            }
        }

        static void BuildMelting(Vector2 cell)
        {
            var top = cell + Vector2.up * 0.5f;
            var go = Make("MeltingPlatform", terrain, new Vector2(cell.x + 0.5f, top.y - 0.285f), "Ground");
            AddBox(go, new Vector2(2f, 0.57f));
            go.AddComponent<MeltingPlatform>();
            for (int i = 0; i < 2; i++)
            {
                var tile = Make("block", go.transform, Vector2.zero);
                tile.transform.position = new Vector2(cell.x + i, cell.y);
                AddSprite(tile, Spr(Winter + "iceBlockHalf.png"), 1).color = new Color(0.85f, 0.95f, 1f, 0.8f);
            }
        }

        static void BuildIcicle(Vector2 cell)
        {
            var go = Make("Icicle", enemies, new Vector2(cell.x, cell.y), "Hazard");
            AddSprite(go, Spr(Own + "icicle.png"), 4);
            AddBox(go, new Vector2(0.3f, 0.9f));
            go.AddComponent<Rigidbody2D>();
            go.AddComponent<FallingIcicle>();
        }

        // ---------------------------------------------------------------- seesaw and chimney
        static void BuildSeesawAndChimney()
        {
            map.Find('L', out int lc, out int lr);
            map.Find('C', out int cc, out int cr);
            Vector2 fulcrum = map.Bottom(lc, lr);            // standing on the ground
            float gt = fulcrum.y;

            var left = Make("SeesawLeft", terrain, new Vector2(fulcrum.x - 0.95f, gt + 0.25f), "Ground");
            AddBox(left, new Vector2(1.5f, 0.5f));
            var pad = Make("SeesawPad", terrain, new Vector2(fulcrum.x + 0.95f, gt + 0.3f), "Ground");
            AddBox(pad, new Vector2(1.5f, 0.6f));

            var plank = Make("Plank", pad.transform, new Vector2(fulcrum.x, gt + 0.55f));
            var plankSprite = Spr(Own + "seesaw_plank.png");
            AddSprite(plank, plankSprite, 2);
            plank.transform.localRotation = Quaternion.Euler(0f, 0f, -12f);
            AddSprite(Make("Fulcrum", pad.transform, new Vector2(fulcrum.x, gt + 0.26f)), Spr(Own + "seesaw_fulcrum.png"), 1, 0.9f);

            // the gift rests on the left end
            var giftHome = new Vector2(fulcrum.x - 0.95f, gt + 0.5f + 0.3f);
            var gift = Make("Gift", null, giftHome, "Gift");
            AddSprite(gift, Spr(Own + "gift.png"), 6, 0.75f).sharedMaterial = sparkle;
            var grb = gift.AddComponent<Rigidbody2D>();
            grb.gravityScale = 2f;
            grb.interpolation = RigidbodyInterpolation2D.Interpolate;
            grb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            AddBox(gift, new Vector2(0.8f, 0.8f));      // local size; the object is scaled 0.75 so it is 0.6 in the world
            var giftComponent = gift.AddComponent<Gift>();

            // the chimney and its catch zone above the open top
            Vector2 chimneyCell = map.Center(cc, cr);
            AddSprite(Make("Chimney", decor, chimneyCell), Spr(Buildings + "chimney.png"), 1);
            var goalPos = new Vector2(chimneyCell.x, chimneyCell.y + 0.9f);
            var goal = Make("Goal", items, goalPos, "Item");
            AddBox(goal, new Vector2(1.3f, 1.0f), Vector2.zero, true);
            goal.AddComponent<GoalZone>();

            // arc from the gift to the goal
            float minImpact = 8f, angle = 60f;
            var d = map.Get("seesaw");
            if (d.HasValue) { minImpact = d.Value.Float(0, minImpact); angle = d.Value.Float(1, angle); }
            var launch = Seesaw.SolveLaunch(goalPos.x - giftHome.x, goalPos.y - giftHome.y, angle, 9.81f * 2f);
            if (launch == Vector2.zero) throw new System.InvalidOperationException("Seesaw: the chimney cannot be reached at that angle.");

            var seesaw = pad.AddComponent<Seesaw>();
            seesaw.gift = giftComponent;
            seesaw.plankVisual = plank.transform;
            seesaw.launchVelocity = launch;
            seesaw.minImpact = minImpact;
        }

        // ---------------------------------------------------------------- wind and gravity regions
        static void BuildRegions()
        {
            foreach (var d in map.Directives)
            {
                if (d.Name != "wind" && d.Name != "gravity") continue;
                float x0 = d.Float(0), y0 = d.Float(1), x1 = d.Float(2) + 1f, y1 = d.Float(3) + 1f;
                var a = map.CornerToWorld(x0, y0);
                var b = map.CornerToWorld(x1, y1);
                var go = Make(d.Name == "wind" ? "Wind" : "LowGravity", null, (a + b) / 2f, "Default");
                AddBox(go, new Vector2(Mathf.Abs(b.x - a.x), Mathf.Abs(a.y - b.y)), Vector2.zero, true);
                if (d.Name == "wind")
                {
                    var w = go.AddComponent<WindField>();
                    w.wind = new Vector2(d.Float(4, 3f), d.Float(5, 0f));
                    w.period = d.Float(6, 0f);
                    w.onSeconds = d.Float(7, 2f);
                }
                else go.AddComponent<GravityZone>().gravityScale = d.Float(4, 0.35f);
            }
        }
    }
}
