using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace TPRPG.EditorTools
{

    public static class FixJuana
    {
        const string SkillsDir = "Assets/Data/Skills";
        const string CharactersDir = "Assets/Data/Characters";
        const string VoiceAsset = "Assets/Data/Voices/Juana_Voice.asset";

        [MenuItem("TP RPG/Fix Juana (skills + animations)", priority = 6)]
        public static void Run()
        {
            BattleUnit daniela = null, juana = null;
            foreach (var u in Object.FindObjectsByType<BattleUnit>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (u.gameObject.name == "Daniela") daniela = u;
                else if (u.gameObject.name == "Juana") juana = u;
            }

            if (juana == null)
            {
                EditorUtility.DisplayDialog("TP RPG", "No encontré un objeto 'Juana' con BattleUnit en la escena abierta.", "OK");
                return;
            }

            var report = new List<string>();
            FixSkills(daniela, juana, report);
            FixRig(daniela, juana, report);

            EditorSceneManager.MarkSceneDirty(juana.gameObject.scene);
            AssetDatabase.SaveAssets();

            string msg = string.Join("\n\n", report) + "\n\nGuardá la escena (Ctrl+S) y dale Play.";
            Debug.Log("[TP RPG] " + msg.Replace("\n", " | "));
            EditorUtility.DisplayDialog("TP RPG - Fix Juana", msg, "OK");
        }

        static SkillData Load(string file)
        {
            return AssetDatabase.LoadAssetAtPath<SkillData>($"{SkillsDir}/{file}.asset");
        }

        static string Names(List<SkillData> list)
        {
            var names = new List<string>();
            foreach (var s in list) names.Add(s != null ? s.displayName : "(vacío)");
            return names.Count > 0 ? string.Join(", ", names) : "(ninguna)";
        }

        static bool SameSkills(List<SkillData> a, List<SkillData> b)
        {
            if (a == null || b == null || a.Count != b.Count) return false;
            for (int i = 0; i < a.Count; i++) if (a[i] != b[i]) return false;
            return true;
        }

        static void FixSkills(BattleUnit daniela, BattleUnit juana, List<string> report)
        {
            var slap = Load("Juana_Slap");
            var pep = Load("Juana_PepTalk");
            var cheer = Load("Juana_CheerSquad");
            var comeback = Load("Juana_SassyComeback");
            var tattle = Load("Juana_Tattle");

            if (slap == null || pep == null || cheer == null || comeback == null || tattle == null)
            {
                report.Add("SKILLS: no encontré los assets Juana_* en " + SkillsDir +
                           ". Ejecutá primero el menú TP RPG > Create Data Only (ScriptableObjects) y volvé a correr esta herramienta.");
                return;
            }

            CharacterData danielaData = daniela != null ? daniela.data : null;
            var data = juana.data;

            if (data == null || data == danielaData)
            {
                string path = CharactersDir + "/Player_Juana.asset";
                var existing = AssetDatabase.LoadAssetAtPath<CharacterData>(path);
                if (existing != null && existing != danielaData)
                {
                    data = existing;
                }
                else
                {
                    data = ScriptableObject.CreateInstance<CharacterData>();
                    data.displayName = "Juana";
                    data.mainColor = new Color(0.7f, 0.9f, 0.5f);
                    data.maxHealth = 140; data.maxMana = 55;
                    data.attack = 10; data.defense = 11; data.wit = 15; data.speed = 11f;
                    data.startATBMin = 0f; data.startATBMax = 45f;
                    AssetDatabase.CreateAsset(data, path);
                }
                Undo.RecordObject(juana, "Assign Juana data");
                juana.data = data;
                EditorUtility.SetDirty(juana);
                report.Add("SKILLS: a Juana le faltaba un CharacterData propio. Le asigné " + path + ".");
            }

            bool sameAsDaniela = danielaData != null && SameSkills(data.skills, danielaData.skills);
            if (data.skills == null || data.skills.Count == 0 || sameAsDaniela)
            {
                Undo.RecordObject(data, "Juana skills");
                if (data.basicAttack == null || (danielaData != null && data.basicAttack == danielaData.basicAttack))
                    data.basicAttack = slap;
                data.skills = new List<SkillData> { pep, cheer, comeback, tattle };
                if (data.guardAction == null && danielaData != null) data.guardAction = danielaData.guardAction;
                if (data.voice == null) data.voice = AssetDatabase.LoadAssetAtPath<CharacterVoice>(VoiceAsset);
                EditorUtility.SetDirty(data);
                report.Add("SKILLS: Juana tenía las skills de Daniela (o ninguna). Ahora usa: Slap (básico) + " + Names(data.skills) + ".");
            }
            else
            {
                report.Add("SKILLS: Juana ya tiene sus propias skills (" + Names(data.skills) + "). No las toqué.");
            }
        }

        static void FixRig(BattleUnit daniela, BattleUnit juana, List<string> report)
        {
            Transform model = juana.transform.Find("ModelJ");
            if (model == null)
            {
                foreach (Transform c in juana.transform)
                {
                    if (c.GetComponentInChildren<Renderer>(true) != null) { model = c; break; }
                }
            }
            if (model == null)
            {
                report.Add("ANIMACIONES: no encontré el modelo 3D dentro de Juana (esperaba un hijo llamado 'ModelJ').");
                return;
            }

            var danielaAnim = daniela != null ? daniela.GetComponent<CharacterAnimator>() : null;
            var danielaVisual = daniela != null ? daniela.GetComponent<UnitVisual>() : null;

            var visual = juana.GetComponent<UnitVisual>();
            if (visual == null) visual = Undo.AddComponent<UnitVisual>(juana.gameObject);

            var anim = juana.GetComponent<CharacterAnimator>();
            if (anim == null) anim = Undo.AddComponent<CharacterAnimator>(juana.gameObject);

            if (juana.GetComponent<UnitVoice>() == null) Undo.AddComponent<UnitVoice>(juana.gameObject);

            if (juana.GetComponent<Collider>() == null)
            {
                var col = Undo.AddComponent<BoxCollider>(juana.gameObject);
                var dc = daniela != null ? daniela.GetComponent<BoxCollider>() : null;
                col.center = dc != null ? dc.center : new Vector3(0f, 0.65f, 0f);
                col.size = dc != null ? dc.size : new Vector3(0.8f, 1.3f, 0.8f);
            }

            var clips = ClipsOf(model);
            string clipSource = "del propio modelo de Juana";
            if (clips.Length == 0 && danielaAnim != null && danielaAnim.clips != null && danielaAnim.clips.Length > 0)
            {
                clips = danielaAnim.clips;
                clipSource = "copiados de Daniela";
            }

            Undo.RecordObject(anim, "Juana animator");
            anim.modelRoot = model;
            anim.clips = clips;
            EditorUtility.SetDirty(anim);

            var torso = FindDeep(model, "torso");
            var head = FindDeep(model, "head");
            var armLeft = FindDeep(model, "arm-left");
            var armRight = FindDeep(model, "arm-right");

            Undo.RecordObject(visual, "Juana visual");
            visual.model = model;
            visual.animator = anim;
            visual.torso = torso;
            visual.head = head;
            visual.armLeft = armLeft;
            visual.armRight = armRight;
            if (danielaVisual != null && !string.IsNullOrEmpty(danielaVisual.idleClip)) visual.idleClip = danielaVisual.idleClip;
            EditorUtility.SetDirty(visual);

            var missing = new List<string>();
            if (torso == null) missing.Add("torso");
            if (head == null) missing.Add("head");
            if (armLeft == null) missing.Add("arm-left");
            if (armRight == null) missing.Add("arm-right");

            bool hasIdle = System.Array.Exists(clips, c => c != null && c.name == visual.idleClip);

            string text = $"ANIMACIONES: modelo = {model.name}, {clips.Length} clips ({clipSource}).";
            text += missing.Count == 0
                ? " Huesos: torso, head, arm-left y arm-right encontrados."
                : " OJO: no encontré los huesos: " + string.Join(", ", missing) + " (las poses de ataque no van a funcionar).";
            if (!hasIdle)
            {
                var sample = new List<string>();
                foreach (var c in clips) { if (c != null && sample.Count < 12) sample.Add(c.name); }
                text += $" OJO: no existe el clip de idle '{visual.idleClip}'. Clips disponibles: " + string.Join(", ", sample) + ".";
            }
            report.Add(text);
        }

        static AnimationClip[] ClipsOf(Transform model)
        {
            var result = new List<AnimationClip>();
            var source = PrefabUtility.GetCorrespondingObjectFromOriginalSource(model.gameObject);
            string path = source != null ? AssetDatabase.GetAssetPath(source) : "";
            if (string.IsNullOrEmpty(path)) return result.ToArray();
            foreach (var o in AssetDatabase.LoadAllAssetsAtPath(path))
                if (o is AnimationClip c && !c.name.StartsWith("__preview__")) result.Add(c);
            return result.ToArray();
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
    }
}
