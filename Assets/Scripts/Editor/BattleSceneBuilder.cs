using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Rendering;
using UnityEngine.UI;

namespace TPRPG.EditorTools
{
    public static class BattleSceneBuilder
    {
        const string DataDir = "Assets/Data";
        const string CharactersDir = DataDir + "/Characters";
        const string SkillsDir = DataDir + "/Skills";
        const string ItemsDir = DataDir + "/Items";
        const string VoicesDir = DataDir + "/Voices";
        const string MaterialsDir = "Assets/Materials";
        const string ScenesDir = "Assets/Scenes";
        const string ScenePath = ScenesDir + "/BattleScene.unity";

        const string DanielaGlb = "Assets/Models/GLB format/character-female-f.glb";
        const string DanielaObj = "Assets/Models/OBJ format/character-female-f.obj";
        const string LaraGlb = "Assets/Models/GLB format/character-female-d.glb";
        const string LaraObj = "Assets/Models/OBJ format/character-female-d.obj";
        const float ModelScale = 2f;

        [MenuItem("TP RPG/Build Battle Scene (data + scene)", priority = 0)]
        public static void BuildAll()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            var d = CreateData();
            BuildScene(d);
        }

        [MenuItem("TP RPG/Create Data Only (ScriptableObjects)", priority = 20)]
        public static void DataOnly()
        {
            CreateData();
            EditorUtility.DisplayDialog("TP RPG", "Data created in " + DataDir, "OK");
        }

        public class CreatedData
        {
            public BattleConfig config;
            public InventoryData inventory;
            public CharacterData daniela, lara;
        }

        static void Folder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            int i = path.LastIndexOf('/');
            string parent = path.Substring(0, i);
            Folder(parent);
            AssetDatabase.CreateFolder(parent, path.Substring(i + 1));
        }

        static T Asset<T>(string path, System.Action<T> setup) where T : ScriptableObject
        {
            var a = AssetDatabase.LoadAssetAtPath<T>(path);
            if (a != null) return a;
            a = ScriptableObject.CreateInstance<T>();
            setup(a);
            AssetDatabase.CreateAsset(a, path);
            return a;
        }

        static EffectEntry Damage(float p, ScalingStat sc) => new EffectEntry { type = EffectType.Damage, power = p, scaling = sc };
        static EffectEntry Heal(float p, ScalingStat sc) => new EffectEntry { type = EffectType.Heal, power = p, scaling = sc };
        static EffectEntry Mana(float p) => new EffectEntry { type = EffectType.RestoreMana, power = p, scaling = ScalingStat.None };
        static EffectEntry Stat(StatType s, float mult, int turns) => new EffectEntry { type = EffectType.ModifyStat, stat = s, multiplier = mult, durationTurns = turns, scaling = ScalingStat.None };
        static EffectEntry ATB(float amount) => new EffectEntry { type = EffectType.AlterATB, atbAmount = amount, scaling = ScalingStat.None };
        static EffectEntry Guard(float m) => new EffectEntry { type = EffectType.Guard, multiplier = m, scaling = ScalingStat.None };

        class Look
        {
            public ActionAnimation anim;
            public HitReaction reaction;
            public float knockback;
            public Color color;
            public FeedbackSound sound;
            public bool shake;

            public Look(ActionAnimation anim, HitReaction reaction, float knockback, Color color, FeedbackSound sound, bool shake = false)
            {
                this.anim = anim;
                this.reaction = reaction;
                this.knockback = knockback;
                this.color = color;
                this.sound = sound;
                this.shake = shake;
            }
        }

        static void Fill(ActionData a, string displayName, string desc, TargetType target, float recovery, Look look, EffectEntry[] effects)
        {
            a.displayName = displayName;
            a.description = desc;
            a.target = target;
            a.recoveryCost = recovery;
            a.actionAnimation = look.anim;
            a.reaction = look.reaction;
            a.knockback = look.knockback;
            a.effectColor = look.color;
            a.sound = look.sound;
            a.shakeCamera = look.shake;
            a.effects = new List<EffectEntry>(effects);
        }

        static SkillData Skill(string file, string displayName, string desc, int mp, TargetType target, float recovery, Look look, params EffectEntry[] effects)
        {
            return Asset<SkillData>($"{SkillsDir}/{file}.asset", s =>
            {
                Fill(s, displayName, desc, target, recovery, look, effects);
                s.manaCost = mp;
            });
        }

        static ItemData Item(string file, string displayName, string desc, TargetType target, float recovery, Look look, params EffectEntry[] effects)
        {
            return Asset<ItemData>($"{ItemsDir}/{file}.asset", s => Fill(s, displayName, desc, target, recovery, look, effects));
        }

        static CharacterVoice Voice(string file, params ActionData[] actions)
        {
            return Asset<CharacterVoice>($"{VoicesDir}/{file}.asset", v =>
            {
                v.attackGrunts = new List<ActionVoiceLine>();
                foreach (var a in actions)
                    if (a != null) v.attackGrunts.Add(new ActionVoiceLine { label = a.displayName, action = a });
            });
        }

        static CharacterData Character(string file, string displayName, Color color, int hp, int mp, int atk, int def, int wit, float spd,
                                       SkillData basic, SkillData guard, SkillData[] skills, CharacterVoice voice,
                                       AIProfile profile = AIProfile.Random, float skillChance = 0.4f)
        {
            return Asset<CharacterData>($"{CharactersDir}/{file}.asset", c =>
            {
                c.displayName = displayName; c.mainColor = color;
                c.maxHealth = hp; c.maxMana = mp;
                c.attack = atk; c.defense = def; c.wit = wit; c.speed = spd;
                c.startATBMin = 0f; c.startATBMax = 45f;
                c.basicAttack = basic; c.guardAction = guard;
                c.skills = new List<SkillData>(skills);
                c.aiProfile = profile; c.skillChance = skillChance;
                c.voice = voice;
            });
        }

        public static CreatedData CreateData()
        {
            Folder(CharactersDir); Folder(SkillsDir); Folder(ItemsDir); Folder(VoicesDir);
            Folder(MaterialsDir); Folder(ScenesDir);

            var d = new CreatedData();
            const TargetType RIVAL = TargetType.SingleEnemy, SELF = TargetType.Self;
            const ScalingStat ATK = ScalingStat.Attack, WIT = ScalingStat.Wit, NONE = ScalingStat.None;

            d.config = Asset<BattleConfig>($"{DataDir}/BattleConfig.asset", c => { });

            var guard = Skill("Common_Guard", "Guard", "Covers up: halves the damage taken until her next turn. Fast action.", 0, SELF, -30f,
                new Look(ActionAnimation.Guard, HitReaction.None, 0f, new Color(0.5f, 0.85f, 1f), FeedbackSound.Buff),
                Guard(0.5f));

            var dPunch = Skill("Daniela_Punch", "Punch", "A straight punch. No cost.", 0, RIVAL, 0f,
                new Look(ActionAnimation.Punch, HitReaction.Hurt, 0.3f, Color.white, FeedbackSound.Hit),
                Damage(12, ATK));
            var dInsult = Skill("Daniela_Insult", "Insult", "A verbal jab at her pride: damage and -25% DEF for 2 turns. Fast recovery.", 5, RIVAL, -15f,
                new Look(ActionAnimation.Insult, HitReaction.Outrage, 0f, new Color(1f, 0.55f, 0.85f), FeedbackSound.Verbal, false),
                Damage(8, WIT), Stat(StatType.Defense, 0.75f, 2));
            var dHairPull = Skill("Daniela_HairPull", "Hair Pull", "Grabs her by the hair and yanks hard. Huge damage, but leaves Daniela worn out (very slow recovery).", 9, RIVAL, 50f,
                new Look(ActionAnimation.HairPull, HitReaction.Pain, 0.5f, new Color(1f, 0.6f, 0.2f), FeedbackSound.Critical, true),
                Damage(26, ATK));
            var dShove = Skill("Daniela_Shove", "Shove", "Two-handed push that knocks her back and delays her ATB bar. Fast recovery.", 6, RIVAL, -25f,
                new Look(ActionAnimation.Shove, HitReaction.Surprise, 1.1f, new Color(0.8f, 0.9f, 1f), FeedbackSound.Hit),
                Damage(7, ATK), ATB(-45f));

            var lPunch = Skill("Lara_Punch", "Punch", "A straight punch. No cost.", 0, RIVAL, 0f,
                new Look(ActionAnimation.Punch, HitReaction.Hurt, 0.3f, Color.white, FeedbackSound.Hit),
                Damage(12, ATK));
            var lCallout = Skill("Lara_Callout", "Callout", "Snaps a photo and exposes her in front of everyone: damage and -20% ATK for 3 turns.", 7, RIVAL, 20f,
                new Look(ActionAnimation.Callout, HitReaction.Outrage, 0f, new Color(1f, 1f, 0.85f), FeedbackSound.Verbal, false),
                Damage(14, WIT), Stat(StatType.Attack, 0.8f, 3));
            var lDefamation = Skill("Lara_Defamation", "Defamation", "Spreads a rumor around the class: heavy damage, -30% SPD for 3 turns and delays her ATB. Very slow recovery.", 11, RIVAL, 50f,
                new Look(ActionAnimation.Defamation, HitReaction.Outrage, 0f, new Color(0.75f, 0.45f, 1f), FeedbackSound.Verbal, true),
                Damage(20, WIT), Stat(StatType.Speed, 0.7f, 3), ATB(-30f));
            var lSpit = Skill("Lara_Spit", "Spit", "Gross and quick: small damage and -15% DEF for 2 turns. Fast recovery.", 4, RIVAL, -20f,
                new Look(ActionAnimation.Spit, HitReaction.Disgust, 0f, new Color(0.75f, 0.92f, 1f), FeedbackSound.Splat),
                Damage(8, ATK), Stat(StatType.Defense, 0.85f, 2));

            var chocolate = Item("Item_ChocolateBar", "Chocolate Bar", "Restores 50 HP.", SELF, 10f,
                new Look(ActionAnimation.Consume, HitReaction.None, 0f, new Color(0.4f, 1f, 0.5f), FeedbackSound.Heal), Heal(50, NONE));
            var energy = Item("Item_EnergyDrink", "Energy Drink", "Restores 25 MP and lets her act again quickly.", SELF, -40f,
                new Look(ActionAnimation.Consume, HitReaction.None, 0f, new Color(0.4f, 0.6f, 1f), FeedbackSound.Heal), Mana(25));
            var scrunchie = Item("Item_LuckyScrunchie", "Lucky Scrunchie", "Buff: +50% ATK for 3 turns.", SELF, 10f,
                new Look(ActionAnimation.Consume, HitReaction.None, 0f, new Color(1f, 0.5f, 0.2f), FeedbackSound.Buff), Stat(StatType.Attack, 1.5f, 3));
            var gossip = Item("Item_GossipNote", "Gossip Note", "Debuff: -40% DEF and -20% ATK on the rival for 3 turns.", RIVAL, 10f,
                new Look(ActionAnimation.Throw, HitReaction.Outrage, 0f, new Color(0.7f, 0.4f, 1f), FeedbackSound.Debuff),
                Stat(StatType.Defense, 0.6f, 3), Stat(StatType.Attack, 0.8f, 3));
            var gum = Item("Item_StickyGum", "Sticky Gum", "Debuff: -40% SPD on the rival and delays her ATB bar.", RIVAL, 5f,
                new Look(ActionAnimation.Throw, HitReaction.Disgust, 0f, new Color(1f, 0.5f, 0.75f), FeedbackSound.Debuff),
                ATB(-40f), Stat(StatType.Speed, 0.6f, 3));
            var balloon = Item("Item_WaterBalloon", "Water Balloon", "25 fixed damage to the rival.", RIVAL, 15f,
                new Look(ActionAnimation.Throw, HitReaction.Surprise, 0.3f, new Color(0.3f, 0.6f, 1f), FeedbackSound.Splat), Damage(25, NONE));

            d.inventory = Asset<InventoryData>($"{DataDir}/StartingInventory.asset", inv =>
            {
                inv.items = new List<ItemStack>
                {
                    new ItemStack { item = chocolate, amount = 3 },
                    new ItemStack { item = energy, amount = 2 },
                    new ItemStack { item = scrunchie, amount = 1 },
                    new ItemStack { item = gossip, amount = 2 },
                    new ItemStack { item = gum, amount = 2 },
                    new ItemStack { item = balloon, amount = 2 },
                };
            });

            var danielaVoice = Voice("Daniela_Voice", dPunch, dInsult, dHairPull, dShove);
            var laraVoice = Voice("Lara_Voice", lPunch, lCallout, lDefamation, lSpit);

            d.daniela = Character("Player_Daniela", "Daniela", new Color(0.55f, 0.75f, 1f), 160, 40, 15, 12, 12, 10f,
                dPunch, guard, new[] { dInsult, dHairPull, dShove }, danielaVoice);
            d.lara = Character("Rival_Lara", "Lara", new Color(1f, 0.55f, 0.75f), 200, 45, 13, 12, 16, 9f,
                lPunch, null, new[] { lCallout, lDefamation, lSpit }, laraVoice, AIProfile.Random, 0.55f);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            return d;
        }

        static Shader litShader;

        static Material Mat(string matName, Color c)
        {
            string path = $"{MaterialsDir}/{matName}.mat";
            var m = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (m != null) return m;
            if (litShader == null)
            {
                var tmp = GameObject.CreatePrimitive(PrimitiveType.Cube);
                var sm = tmp.GetComponent<Renderer>().sharedMaterial;
                litShader = sm != null ? sm.shader : Shader.Find("Standard");
                Object.DestroyImmediate(tmp);
            }
            m = new Material(litShader);
            if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", c);
            if (m.HasProperty("_Color")) m.SetColor("_Color", c);
            AssetDatabase.CreateAsset(m, path);
            return m;
        }

        static GameObject Prim(PrimitiveType t, string objName, Transform parent, Vector3 pos, Vector3 scale, Material m, Vector3? rot = null)
        {
            var g = GameObject.CreatePrimitive(t);
            g.name = objName;
            g.transform.SetParent(parent, false);
            g.transform.localPosition = pos;
            g.transform.localScale = scale;
            if (rot.HasValue) g.transform.localRotation = Quaternion.Euler(rot.Value);
            var c = g.GetComponent<Collider>();
            if (c != null) Object.DestroyImmediate(c);
            if (m != null) g.GetComponent<Renderer>().sharedMaterial = m;
            return g;
        }

        static GameObject Group(string objName, Transform parent = null)
        {
            var g = new GameObject(objName);
            if (parent != null) g.transform.SetParent(parent, false);
            return g;
        }

        static void Face(Transform t, Vector3 point)
        {
            Vector3 d = point - t.position;
            d.y = 0f;
            t.rotation = Quaternion.LookRotation(d.normalized, Vector3.up);
        }

        public static void BuildScene(CreatedData d)
        {
            litShader = null;
            Folder(MaterialsDir);
            Folder(ScenesDir);
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.55f, 0.58f, 0.62f);

            var lightGO = new GameObject("Directional Light");
            var l = lightGO.AddComponent<Light>();
            l.type = LightType.Directional;
            l.intensity = 1.05f;
            l.shadows = LightShadows.Soft;
            lightGO.transform.rotation = Quaternion.Euler(45f, -35f, 0f);

            var camGO = new GameObject("Main Camera");
            camGO.tag = "MainCamera";
            var cam = camGO.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.55f, 0.8f, 0.98f);
            cam.fieldOfView = 40f;
            camGO.AddComponent<AudioListener>();
            camGO.AddComponent<PhysicsRaycaster>();
            var shake = camGO.AddComponent<CameraShake>();
            camGO.transform.position = new Vector3(0f, 1.9f, -6.2f);
            camGO.transform.rotation = Quaternion.Euler(14f, 0f, 0f);

            var systems = Group("Systems");
            var bmGO = Group("BattleManager", systems.transform);
            var manager = bmGO.AddComponent<BattleManager>();
            var ai = bmGO.AddComponent<EnemyAI>();
            var choreography = bmGO.AddComponent<ActionChoreography>();
            var fbGO = Group("Feedback", systems.transform);
            var feedback = fbGO.AddComponent<FeedbackManager>();
            var sfx = fbGO.AddComponent<ProceduralAudio>();

            BuildSchoolyard();

            var turnMat = Mat("TurnMarker", new Color(1f, 0.85f, 0.2f));
            var targetMat = Mat("TargetMarker", new Color(1f, 0.2f, 0.2f));
            var fighters = Group("Fighters");
            var playerSide = Group("PlayerSide", fighters.transform);
            var enemySide = Group("EnemySide", fighters.transform);

            Vector3 playerPos = new Vector3(-1.3f, 0.05f, -3.2f);
            Vector3 enemyPos = new Vector3(1.9f, 0.05f, 2.5f);
            var daniela = CreateUnit("Daniela", d.daniela, playerSide.transform, playerPos, enemyPos, true, DanielaGlb, DanielaObj, false, turnMat, targetMat);
            var lara = CreateUnit("Lara", d.lara, enemySide.transform, enemyPos, playerPos, false, LaraGlb, LaraObj, true, turnMat, targetMat);

            var ui = BuildUI(out Image screenFlash);

            var es = new GameObject("EventSystem", typeof(EventSystem));
            AddInputModule(es);

            manager.config = d.config;
            manager.startingInventory = d.inventory;
            manager.players = new List<BattleUnit> { daniela };
            manager.enemies = new List<BattleUnit> { lara };
            manager.ui = ui;
            manager.feedback = feedback;
            manager.ai = ai;
            manager.choreography = choreography;
            choreography.feedback = feedback;

            feedback.cam = cam;
            feedback.cameraShake = shake;
            feedback.sfx = sfx;
            feedback.screenFlash = screenFlash;

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, ScenePath);
            var list = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
            list.RemoveAll(s => s.path == ScenePath);
            list.Insert(0, new EditorBuildSettingsScene(ScenePath, true));
            EditorBuildSettings.scenes = list.ToArray();
            AssetDatabase.SaveAssets();

            Selection.activeGameObject = bmGO;
            EditorGUIUtility.PingObject(bmGO);
            bool glb = AssetDatabase.LoadAssetAtPath<GameObject>(DanielaGlb) != null;
            Debug.Log("[TP RPG] Battle scene created at " + ScenePath + (glb
                ? ". Using the animated GLB models. Press Play."
                : ". GLB models are not imported (install glTFast), using OBJ models without skeletal animations."));
        }

        static void AddInputModule(GameObject es)
        {
            bool newInput = false;
#if ENABLE_INPUT_SYSTEM
            newInput = true;
#endif
            var type = System.Type.GetType("UnityEngine.InputSystem.UI.InputSystemUIInputModule, Unity.InputSystem");
            if (newInput && type != null) es.AddComponent(type);
            else es.AddComponent<StandaloneInputModule>();
        }

        static void BuildSchoolyard()
        {
            var grass = Mat("Grass", new Color(0.45f, 0.75f, 0.35f));
            var darkGrass = Mat("DarkGrass", new Color(0.3f, 0.6f, 0.3f));
            var platform = Mat("Platform", new Color(0.8f, 0.85f, 0.55f));
            var trunk = Mat("Trunk", new Color(0.45f, 0.3f, 0.18f));
            var leaves = Mat("Leaves", new Color(0.2f, 0.5f, 0.25f));
            var wall = Mat("SchoolWall", new Color(0.95f, 0.88f, 0.72f));
            var roof = Mat("SchoolRoof", new Color(0.75f, 0.3f, 0.25f));
            var glass = Mat("SchoolWindow", new Color(0.6f, 0.82f, 0.98f));
            var door = Mat("SchoolDoor", new Color(0.45f, 0.3f, 0.2f));

            var yard = Group("Schoolyard").transform;
            Prim(PrimitiveType.Plane, "Ground", yard, new Vector3(0f, 0f, 4f), new Vector3(4f, 1f, 3f), grass);
            Prim(PrimitiveType.Cylinder, "PlayerPlatform", yard, new Vector3(-1.3f, 0.02f, -3.2f), new Vector3(2.6f, 0.03f, 1.4f), platform);
            Prim(PrimitiveType.Cylinder, "RivalPlatform", yard, new Vector3(1.9f, 0.02f, 2.5f), new Vector3(3.2f, 0.03f, 1.8f), platform);

            var background = Group("Background", yard).transform;
            Prim(PrimitiveType.Sphere, "Hill1", background, new Vector3(-6f, -1f, 14f), new Vector3(14f, 5f, 4f), darkGrass);
            Prim(PrimitiveType.Sphere, "Hill2", background, new Vector3(7f, -1.5f, 15f), new Vector3(16f, 6f, 4f), darkGrass);

            var school = Group("School", background).transform;
            school.localPosition = new Vector3(0.5f, 0f, 12f);
            Prim(PrimitiveType.Cube, "Building", school, new Vector3(0f, 2.2f, 0f), new Vector3(9f, 4.4f, 2f), wall);
            Prim(PrimitiveType.Cube, "Roof", school, new Vector3(0f, 4.6f, 0f), new Vector3(9.6f, 0.4f, 2.4f), roof);
            Prim(PrimitiveType.Cube, "Door", school, new Vector3(0f, 0.8f, -1.01f), new Vector3(1.2f, 1.6f, 0.05f), door);
            for (int row = 0; row < 2; row++)
                for (int col = 0; col < 6; col++)
                {
                    float x = -3.75f + col * 1.5f;
                    if (row == 0 && Mathf.Abs(x) < 1f) continue;
                    Prim(PrimitiveType.Cube, $"Window_{row}_{col}", school, new Vector3(x, 1.4f + row * 1.6f, -1.01f), new Vector3(0.9f, 0.8f, 0.05f), glass);
                }
            var sign = Group("Sign", school);
            sign.transform.localPosition = new Vector3(0f, 3.9f, -1.06f);
            var tm = sign.AddComponent<TextMesh>();
            var signFont = LoadFont();
            tm.font = signFont;
            tm.text = "SCHOOL";
            tm.fontSize = 64;
            tm.characterSize = 0.06f;
            tm.anchor = TextAnchor.MiddleCenter;
            tm.alignment = TextAlignment.Center;
            tm.color = new Color(0.35f, 0.2f, 0.15f);
            var signRenderer = sign.GetComponent<MeshRenderer>();
            if (signRenderer == null) signRenderer = sign.AddComponent<MeshRenderer>();
            if (signFont != null) signRenderer.sharedMaterial = signFont.material;

            float[] xs = { -7f, -4.5f, 4.5f, 7.5f, 10f };
            for (int i = 0; i < xs.Length; i++)
            {
                var tree = Group("Tree_" + (i + 1), background).transform;
                tree.localPosition = new Vector3(xs[i], 0f, 9f + (i % 2));
                Prim(PrimitiveType.Cylinder, "Trunk", tree, new Vector3(0f, 0.8f, 0f), new Vector3(0.35f, 0.8f, 0.35f), trunk);
                Prim(PrimitiveType.Sphere, "Crown", tree, new Vector3(0f, 2.2f, 0f), new Vector3(1.8f, 1.8f, 1.8f), leaves);
            }
        }

        static Transform FindDeep(Transform root, string objName)
        {
            if (root.name == objName) return root;
            foreach (Transform c in root)
            {
                var r = FindDeep(c, objName);
                if (r != null) return r;
            }
            return null;
        }

        static AnimationClip[] LoadClips(string path)
        {
            var result = new List<AnimationClip>();
            foreach (var o in AssetDatabase.LoadAllAssetsAtPath(path))
                if (o is AnimationClip c && !c.name.StartsWith("__preview__")) result.Add(c);
            return result.ToArray();
        }

        static BattleUnit CreateUnit(string objName, CharacterData data, Transform parent, Vector3 pos, Vector3 facing, bool playerSide,
                                     string glbPath, string objPath, bool phone, Material turnMat, Material targetMat)
        {
            var root = new GameObject(objName);
            root.transform.SetParent(parent, false);
            root.transform.position = pos;
            Face(root.transform, facing);

            string modelPath = glbPath;
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(glbPath);
            if (prefab == null) { modelPath = objPath; prefab = AssetDatabase.LoadAssetAtPath<GameObject>(objPath); }

            Transform model;
            if (prefab != null)
            {
                var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, root.transform);
                instance.name = "Model";
                model = instance.transform;
            }
            else
            {
                model = Group("Model", root.transform).transform;
                Prim(PrimitiveType.Capsule, "Placeholder", model, new Vector3(0f, 0.32f, 0f), new Vector3(0.3f, 0.32f, 0.3f), Mat(objName + "_Placeholder", data.mainColor));
            }
            model.localPosition = Vector3.zero;
            model.localRotation = Quaternion.identity;
            model.localScale = Vector3.one * ModelScale;

            var torso = FindDeep(model, "torso");
            var head = FindDeep(model, "head");
            var armLeft = FindDeep(model, "arm-left");
            var armRight = FindDeep(model, "arm-right");
            if (armLeft != null && armRight != null &&
                model.InverseTransformPoint(armLeft.position).x > model.InverseTransformPoint(armRight.position).x)
                model.localRotation = Quaternion.Euler(0f, 180f, 0f);

            var anim = root.AddComponent<CharacterAnimator>();
            anim.modelRoot = model;
            anim.clips = prefab != null ? LoadClips(modelPath) : new AnimationClip[0];

            GameObject prop = null;
            if (phone)
            {
                var caseMat = Mat("Phone_Case", new Color(0.12f, 0.12f, 0.15f));
                var screenMat = Mat("Phone_Screen", new Color(0.55f, 0.85f, 1f));
                prop = Group("Phone", armRight != null ? armRight : root.transform);
                float inv = armRight != null ? 1f / ModelScale : 1f;
                prop.transform.localPosition = armRight != null ? new Vector3(0.14f, 0f, 0.02f) : new Vector3(0.25f, 0.8f, 0.3f);
                prop.transform.localRotation = Quaternion.identity;
                Prim(PrimitiveType.Cube, "Case", prop.transform, Vector3.zero, new Vector3(0.08f, 0.15f, 0.015f) * inv, caseMat);
                Prim(PrimitiveType.Cube, "Screen", prop.transform, new Vector3(0f, 0f, -0.009f * inv), new Vector3(0.07f, 0.13f, 0.005f) * inv, screenMat);
                prop.SetActive(false);
            }

            var ring = Prim(PrimitiveType.Cylinder, "TurnMarker", root.transform, new Vector3(0f, 0.03f, 0f), new Vector3(1.1f, 0.02f, 1.1f), turnMat);
            var arrow = Prim(PrimitiveType.Cube, "TargetMarker", root.transform, new Vector3(0f, 1.75f, 0f), Vector3.one * 0.2f, targetMat, new Vector3(45f, 0f, 45f));

            var col = root.AddComponent<BoxCollider>();
            col.center = new Vector3(0f, 0.65f, 0f);
            col.size = new Vector3(0.8f, 1.3f, 0.8f);

            var vis = root.AddComponent<UnitVisual>();
            vis.model = model;
            vis.animator = anim;
            vis.torso = torso;
            vis.head = head;
            vis.armLeft = armLeft;
            vis.armRight = armRight;
            vis.prop = prop;
            vis.turnMarker = ring;
            vis.targetMarker = arrow;
            vis.impactHeight = 0.6f;
            vis.approachDistance = 0.85f;

            root.AddComponent<UnitVoice>().voice = data.voice;

            var bu = root.AddComponent<BattleUnit>();
            bu.data = data;
            bu.isPlayerSide = playerSide;
            return bu;
        }

        static Font font;
        static Sprite rounded;
        static readonly Color Border = new Color(0.22f, 0.24f, 0.3f);
        static readonly Color Cream = new Color(0.98f, 0.97f, 0.9f);
        static readonly Color DarkText = new Color(0.2f, 0.2f, 0.25f);

        static Font LoadFont()
        {
            Font f = null;
#if UNITY_2022_2_OR_NEWER
            f = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
#endif
            if (f == null) f = Resources.GetBuiltinResource<Font>("Arial.ttf");
            return f;
        }

        static RectTransform Rect(string objName, Transform parent, Vector2 aMin, Vector2 aMax, Vector2 oMin, Vector2 oMax)
        {
            var g = new GameObject(objName, typeof(RectTransform));
            g.transform.SetParent(parent, false);
            var rt = (RectTransform)g.transform;
            rt.anchorMin = aMin; rt.anchorMax = aMax;
            rt.offsetMin = oMin; rt.offsetMax = oMax;
            return rt;
        }

        static RectTransform Stretch(string objName, Transform parent, float margin = 0f) =>
            Rect(objName, parent, Vector2.zero, Vector2.one, new Vector2(margin, margin), new Vector2(-margin, -margin));

        static Image Img(RectTransform rt, Color c, bool round = true, bool raycast = false)
        {
            var i = rt.gameObject.AddComponent<Image>();
            if (round && rounded != null)
            {
                i.sprite = rounded;
                i.type = Image.Type.Sliced;
                i.pixelsPerUnitMultiplier = 0.35f;
            }
            i.color = c;
            i.raycastTarget = raycast;
            return i;
        }

        static RectTransform Box(string objName, Transform parent, Vector2 aMin, Vector2 aMax, Vector2 oMin, Vector2 oMax, Color inner, float thickness, out Image innerImage)
        {
            var outer = Rect(objName, parent, aMin, aMax, oMin, oMax);
            Img(outer, Border, true, true);
            var inside = Stretch("Inner", outer, thickness);
            innerImage = Img(inside, inner);
            return inside;
        }

        static Text Txt(string objName, Transform parent, string text, int size, TextAnchor align, Color c,
                        Vector2 aMin, Vector2 aMax, Vector2 oMin, Vector2 oMax, bool shadow = false)
        {
            var rt = Rect(objName, parent, aMin, aMax, oMin, oMax);
            var x = rt.gameObject.AddComponent<Text>();
            x.font = font;
            x.text = text;
            x.fontSize = size;
            x.alignment = align;
            x.color = c;
            x.fontStyle = FontStyle.Bold;
            x.supportRichText = true;
            x.horizontalOverflow = HorizontalWrapMode.Wrap;
            x.verticalOverflow = VerticalWrapMode.Truncate;
            x.resizeTextForBestFit = true;
            x.resizeTextMinSize = 10;
            x.resizeTextMaxSize = size;
            x.raycastTarget = false;
            if (shadow)
            {
                var s = rt.gameObject.AddComponent<Shadow>();
                s.effectColor = new Color(0f, 0f, 0f, 0.45f);
                s.effectDistance = new Vector2(3f, -3f);
            }
            return x;
        }

        static Button Btn(string objName, Transform parent, string text, Color color, int size, Color textColor, out Text label)
        {
            var rt = Rect(objName, parent, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            var border = Img(rt, Border, true, true);
            var face = Stretch("Face", rt, 5f);
            var img = Img(face, color, true, false);
            var b = rt.gameObject.AddComponent<Button>();
            b.targetGraphic = img;
            var cb = b.colors;
            cb.normalColor = new Color(0.85f, 0.85f, 0.85f);
            cb.highlightedColor = Color.white;
            cb.selectedColor = Color.white;
            cb.pressedColor = new Color(0.6f, 0.6f, 0.6f);
            cb.disabledColor = new Color(0.45f, 0.45f, 0.45f, 0.8f);
            cb.colorMultiplier = 1f;
            cb.fadeDuration = 0.05f;
            b.colors = cb;
            var o = border.gameObject.AddComponent<Outline>();
            o.effectColor = new Color(0f, 0f, 0f, 0.25f);
            o.effectDistance = new Vector2(2f, -2f);
            label = Txt("Label", face, text, size, TextAnchor.MiddleCenter, textColor, Vector2.zero, Vector2.one, new Vector2(8f, 0f), new Vector2(-8f, 0f), true);
            return b;
        }

        static UIBar Bar(string objName, Transform parent, Vector2 aMin, Vector2 aMax, string tag, Color tagColor, out Text text)
        {
            var container = Rect(objName, parent, aMin, aMax, new Vector2(24f, 2f), new Vector2(-24f, -2f));
            var tagBox = Rect("Tag", container, new Vector2(0f, 0f), new Vector2(0f, 1f), Vector2.zero, new Vector2(84f, 0f));
            Img(tagBox, new Color(0.3f, 0.3f, 0.32f));
            Txt("Text", tagBox, tag, 22, TextAnchor.MiddleCenter, tagColor, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            var back = Rect("Background", container, Vector2.zero, Vector2.one, new Vector2(80f, 0f), Vector2.zero);
            Img(back, new Color(0.3f, 0.3f, 0.32f));
            var hole = Stretch("Hole", back, 4f);
            Img(hole, new Color(0.15f, 0.15f, 0.17f), false);
            var ghost = Rect("Ghost", hole, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            Img(ghost, new Color(1f, 1f, 1f, 0.6f), false);
            var fill = Rect("Fill", hole, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            var fillImg = Img(fill, Color.green, false);
            text = Txt("Value", hole, "", 20, TextAnchor.MiddleCenter, Color.white, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, true);
            var bar = container.gameObject.AddComponent<UIBar>();
            bar.fill = fill;
            bar.fillImage = fillImg;
            bar.ghost = ghost;
            return bar;
        }

        static RectTransform Cell(Transform parent, int col, int row, float spacing)
        {
            float x0 = col * 0.5f, y1 = 1f - row * 0.5f;
            float h = spacing * 0.5f;
            return Rect($"Cell_{row}_{col}", parent, new Vector2(x0, y1 - 0.5f), new Vector2(x0 + 0.5f, y1), new Vector2(h, h), new Vector2(-h, -h));
        }

        static void WindowTitle(RectTransform inner, string title)
        {
            var strip = Rect("Title", inner, new Vector2(0f, 1f), Vector2.one, new Vector2(0f, -70f), Vector2.zero);
            Img(strip, new Color(0.85f, 0.3f, 0.25f));
            Txt("Text", strip, title, 34, TextAnchor.MiddleCenter, Color.white, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, true);
        }

        static UnitStatusRow StatusBox(string objName, Transform parent, Vector2 anchor, Vector2 oMin, Vector2 oMax)
        {
            var inner = Box(objName, parent, anchor, anchor, oMin, oMax, Cream, 6f, out Image background);
            var row = inner.parent.gameObject.AddComponent<UnitStatusRow>();
            row.background = background;
            row.tintName = false;
            row.nameText = Txt("Name", inner, "NAME", 38, TextAnchor.MiddleLeft, DarkText, new Vector2(0f, 0.76f), new Vector2(0.55f, 1f), new Vector2(26f, 0f), new Vector2(0f, -6f), true);
            row.statusText = Txt("Status", inner, "", 24, TextAnchor.MiddleRight, DarkText, new Vector2(0.55f, 0.76f), Vector2.one, Vector2.zero, new Vector2(-26f, -6f));
            row.healthBar = Bar("HealthBar", inner, new Vector2(0f, 0.53f), new Vector2(1f, 0.74f), "HP", new Color(1f, 0.75f, 0.2f), out row.healthText);
            row.manaBar = Bar("ManaBar", inner, new Vector2(0f, 0.3f), new Vector2(1f, 0.51f), "MP", new Color(0.5f, 0.75f, 1f), out row.manaText);
            row.atbBar = Bar("ATBBar", inner, new Vector2(0f, 0.07f), new Vector2(1f, 0.28f), "ATB", new Color(0.6f, 1f, 0.95f), out row.atbText);
            row.atbBar.ghost.gameObject.SetActive(false);
            row.atbBar.ghost = null;
            return row;
        }

        static ListEntryUI ListEntry(RectTransform container)
        {
            var b = Btn("EntryTemplate", container, "Item", new Color(0.95f, 0.95f, 0.98f), 32, DarkText, out Text label);
            b.gameObject.AddComponent<LayoutElement>().preferredHeight = 70f;
            label.alignment = TextAnchor.MiddleLeft;
            label.rectTransform.offsetMin = new Vector2(30f, 0f);
            label.GetComponent<Shadow>().effectColor = new Color(0f, 0f, 0f, 0.12f);
            var entry = b.gameObject.AddComponent<ListEntryUI>();
            entry.button = b;
            entry.label = label;
            entry.detail = Txt("Detail", label.transform.parent, "", 30, TextAnchor.MiddleRight, new Color(0.3f, 0.45f, 0.8f), Vector2.zero, Vector2.one, Vector2.zero, new Vector2(-30f, 0f));
            return entry;
        }

        static void Vertical(RectTransform rt)
        {
            var v = rt.gameObject.AddComponent<VerticalLayoutGroup>();
            v.spacing = 10f;
            v.childControlWidth = true;
            v.childControlHeight = true;
            v.childForceExpandWidth = true;
            v.childForceExpandHeight = false;
        }

        static BattleUI BuildUI(out Image screenFlash)
        {
            font = LoadFont();
            rounded = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");

            var canvasGO = new GameObject("Canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasGO.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasGO.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 1f;
            var c = canvasGO.transform;
            var ui = canvasGO.AddComponent<BattleUI>();
            Text t;

            ui.enemyBoxes = new[] { StatusBox("RivalBox", c, new Vector2(0f, 1f), new Vector2(60f, -320f), new Vector2(760f, -50f)) };
            ui.playerBoxes = new[] { StatusBox("PlayerBox", c, new Vector2(1f, 0f), new Vector2(-760f, 315f), new Vector2(-60f, 585f)) };

            ui.atbModeText = Txt("ATBModeText", c, "", 24, TextAnchor.UpperRight, new Color(0.15f, 0.2f, 0.3f),
                Vector2.one, Vector2.one, new Vector2(-760f, -110f), new Vector2(-40f, -40f));

            var dialog = Box("DialogBox", c, Vector2.zero, new Vector2(1f, 0f), new Vector2(20f, 20f), new Vector2(-20f, 295f), new Color(0.17f, 0.27f, 0.36f), 9f, out Image tmpImg);
            var frame = Stretch("Frame", dialog, 8f);
            Img(frame, new Color(0.24f, 0.37f, 0.48f));
            ui.dialogText = Txt("Text", frame, "", 42, TextAnchor.MiddleLeft, Color.white,
                Vector2.zero, Vector2.one, new Vector2(40f, 20f), new Vector2(-830f, -20f), true);
            ui.dialogText.resizeTextMinSize = 18;
            var arrow = Rect("ContinueIndicator", frame, new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-70f, 30f), new Vector2(-46f, 54f));
            Img(arrow, new Color(0.95f, 0.3f, 0.25f), false);
            arrow.localRotation = Quaternion.Euler(0f, 0f, 45f);
            ui.continueIndicator = arrow.gameObject;

            var commands = Box("CommandPanel", c, new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-800f, 20f), new Vector2(-20f, 295f), Cream, 8f, out tmpImg);
            ui.commandPanel = commands.parent.gameObject;
            var grid = Stretch("Buttons", commands, 12f);
            ui.attackButton = Btn("AttackButton", Cell(grid, 0, 0, 16f), "PUNCH", new Color(0.92f, 0.33f, 0.3f), 42, Color.white, out ui.attackLabel);
            ui.skillsButton = Btn("SkillsButton", Cell(grid, 1, 0, 16f), "SKILLS", new Color(0.95f, 0.72f, 0.2f), 42, Color.white, out t);
            ui.itemsButton = Btn("BagButton", Cell(grid, 0, 1, 16f), "BAG", new Color(0.35f, 0.75f, 0.35f), 42, Color.white, out t);
            ui.guardButton = Btn("GuardButton", Cell(grid, 1, 1, 16f), "GUARD", new Color(0.3f, 0.55f, 0.95f), 42, Color.white, out t);

            var skills = Box("SkillsPanel", c, Vector2.zero, new Vector2(1f, 0f), new Vector2(20f, 20f), new Vector2(-20f, 295f), Cream, 8f, out tmpImg);
            ui.skillsPanel = skills.parent.gameObject;
            var skillGrid = Rect("Skills", skills, Vector2.zero, new Vector2(0.62f, 1f), new Vector2(12f, 12f), new Vector2(-4f, -12f));
            ui.skillButtons = new ListEntryUI[4];
            for (int i = 0; i < 4; i++)
            {
                var b = Btn("Skill" + (i + 1), Cell(skillGrid, i % 2, i / 2, 16f), "-", Color.white, 36, Color.white, out Text label);
                var entry = b.gameObject.AddComponent<ListEntryUI>();
                entry.button = b;
                entry.label = label;
                ui.skillButtons[i] = entry;
            }
            var info = Rect("Info", skills, new Vector2(0.62f, 0f), Vector2.one, new Vector2(10f, 14f), new Vector2(-20f, -14f));
            var separator = Rect("Separator", info, Vector2.zero, new Vector2(0f, 1f), Vector2.zero, new Vector2(6f, 0f));
            Img(separator, Border, false);
            ui.skillCostText = Txt("CostText", info, "MP 0", 44, TextAnchor.MiddleLeft, DarkText, new Vector2(0f, 0.4f), Vector2.one, new Vector2(30f, 0f), new Vector2(-10f, 0f), true);
            var back = Rect("Back", info, new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-230f, 4f), new Vector2(0f, 76f));
            ui.skillsBackButton = Btn("Button", back, "BACK", new Color(0.55f, 0.55f, 0.6f), 32, Color.white, out t);

            var bag = Box("BagPanel", c, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-460f, -225f), new Vector2(460f, 490f), Cream, 8f, out tmpImg);
            ui.bagPanel = bag.parent.gameObject;
            WindowTitle(bag, "BAG");
            var bagList = Rect("Container", bag, Vector2.zero, Vector2.one, new Vector2(30f, 110f), new Vector2(-30f, -90f));
            Vertical(bagList);
            ui.bagContainer = bagList;
            ui.entryTemplate = ListEntry(bagList);
            ui.entryTemplate.gameObject.SetActive(false);
            var closeBag = Rect("Close", bag, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(-150f, 18f), new Vector2(150f, 88f));
            ui.bagCloseButton = Btn("Button", closeBag, "CLOSE", new Color(0.55f, 0.55f, 0.6f), 30, Color.white, out t);

            var targets = Box("TargetPanel", c, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-460f, -125f), new Vector2(460f, 390f), Cream, 8f, out tmpImg);
            ui.targetPanel = targets.parent.gameObject;
            WindowTitle(targets, "TARGET");
            var targetList = Rect("Container", targets, Vector2.zero, Vector2.one, new Vector2(30f, 110f), new Vector2(-30f, -90f));
            Vertical(targetList);
            ui.targetContainer = targetList;
            var closeTargets = Rect("Close", targets, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(-150f, 18f), new Vector2(150f, 88f));
            ui.targetCloseButton = Btn("Button", closeTargets, "BACK", new Color(0.55f, 0.55f, 0.6f), 30, Color.white, out t);

            var flash = Stretch("ScreenFlash", c);
            screenFlash = flash.gameObject.AddComponent<Image>();
            screenFlash.color = new Color(1f, 1f, 1f, 0f);
            screenFlash.raycastTarget = false;

            var result = Stretch("ResultPanel", c);
            var dim = result.gameObject.AddComponent<Image>();
            dim.color = new Color(0f, 0f, 0f, 0.6f);
            ui.resultPanel = result.gameObject;
            ui.resultText = Txt("Title", result, "VICTORY!", 120, TextAnchor.MiddleCenter, Color.yellow,
                new Vector2(0f, 0.5f), new Vector2(1f, 0.5f), new Vector2(0f, 60f), new Vector2(0f, 240f), true);
            ui.resultSubtitle = Txt("Subtitle", result, "", 44, TextAnchor.MiddleCenter, Color.white,
                new Vector2(0f, 0.5f), new Vector2(1f, 0.5f), new Vector2(0f, -10f), new Vector2(0f, 60f), true);
            var restart = Rect("RestartButton", result, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-280f, -150f), new Vector2(280f, -40f));
            ui.restartButton = Btn("Button", restart, "PLAY AGAIN", new Color(0.92f, 0.33f, 0.3f), 40, Color.white, out t);
            result.gameObject.SetActive(false);

            ui.skillsPanel.SetActive(false);
            ui.bagPanel.SetActive(false);
            ui.targetPanel.SetActive(false);
            return ui;
        }
    }
}
