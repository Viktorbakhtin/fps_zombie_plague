using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using ZombiePlague.Infrastructure.Actors;
using ZombiePlague.Infrastructure.Combat;

namespace ZombiePlague.Editor
{
    public static class CombatEffectsBuilder
    {
        public static string Upgrade()
        {
            const string root = "Assets/_Game/";
            if (!AssetDatabase.IsValidFolder(root + "Prefabs/Effects")) AssetDatabase.CreateFolder(root + "Prefabs", "Effects");
            var flash = CopyEffect("Assets/Prefabs/Effects/CustomMuzzleFlash.prefab", root + "Prefabs/Effects/MuzzleFlash.prefab", 0.45f);
            var impact = CopyEffect("Assets/Prefabs/Effects/BulletImpact.prefab", root + "Prefabs/Effects/BulletImpact.prefab", 0.6f);
            foreach (var name in new[] { "DebugPlayer", "DebugBot" })
            {
                var go = PrefabUtility.LoadPrefabContents(root + "Prefabs/" + name + ".prefab");
                var presentation = go.GetComponent<ActorPresentation>();
                Transform gun = presentation.ViewWeapon;
                if (gun == null) foreach (var t in presentation.ThirdPersonVisual.GetComponentsInChildren<Transform>(true))
                    if (t.name.StartsWith("geo_assaultRifle_mainBody")) { gun = t; break; }
                if (gun == null) throw new System.Exception("Rifle muzzle attachment not found: " + name);
                var muzzle = gun.Find("Shot muzzle");
                if (muzzle == null) { muzzle = new GameObject("Shot muzzle").transform; muzzle.SetParent(gun, false); }
                muzzle.localPosition = new Vector3(0.76f, 0.14f, 0); muzzle.localRotation = Quaternion.Euler(0, 90, 0);
                presentation.Muzzle = muzzle; presentation.MuzzleFlashPrefab = flash;
                PrefabUtility.SaveAsPrefabAsset(go, root + "Prefabs/" + name + ".prefab"); PrefabUtility.UnloadPrefabContents(go);
            }
            foreach (var scene in new[] { "DebugArena", "TestArena" })
            {
                EditorSceneManager.OpenScene(root + "Scenes/" + scene + ".unity");
                var session = Object.FindFirstObjectByType<Infrastructure.Bootstrap.PrototypeSession>();
                var pool = session.GetComponent<CombatEffects>(); if (pool == null) pool = session.gameObject.AddComponent<CombatEffects>();
                pool.ImpactPrefab = impact; EditorUtility.SetDirty(pool);
                EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
            }
            EditorSceneManager.OpenScene(root + "Scenes/DebugArena.unity"); AssetDatabase.SaveAssets();
            return "Template muzzle flash and surface impact copied without legacy Netcode scripts; pooled effects assigned.";
        }
        private static GameObject CopyEffect(string source, string destination, float scale)
        {
            var go = PrefabUtility.LoadPrefabContents(source);
            foreach (var script in go.GetComponentsInChildren<MonoBehaviour>(true)) Object.DestroyImmediate(script);
            foreach (var collider in go.GetComponentsInChildren<Collider>(true)) Object.DestroyImmediate(collider);
            foreach (var ps in go.GetComponentsInChildren<ParticleSystem>(true))
            {
                var main = ps.main; main.loop = false; main.playOnAwake = false;
                if (ps.transform == go.transform && source.Contains("CustomMuzzleFlash"))
                {
                    main.startLifetime = 0.06f; main.startSpeed = 0; main.startSize = 0.18f;
                    main.startColor = new Color(1, 0.7f, 0.3f, 1);
                    var emission = ps.emission; emission.rateOverTime = 0; emission.SetBursts(new[] { new ParticleSystem.Burst(0, 1) });
                    var shape = ps.shape; shape.enabled = false;
                    var color = ps.colorOverLifetime; color.enabled = false;
                    var size = ps.sizeOverLifetime; size.enabled = false;
                    var sheet = ps.textureSheetAnimation; sheet.enabled = false;
                }
                if (ps.name == "Decal_PS") main.startLifetime = 8f;
                var renderer = ps.GetComponent<ParticleSystemRenderer>();
                if (renderer.sharedMaterial != null && renderer.sharedMaterial.shader.name.StartsWith("Legacy Shaders/Particles/"))
                    renderer.sharedMaterial = ParticleMaterial(renderer.sharedMaterial);
            }
            go.transform.localRotation = Quaternion.identity; go.transform.localScale = Vector3.one * scale;
            var saved = PrefabUtility.SaveAsPrefabAsset(go, destination); PrefabUtility.UnloadPrefabContents(go); return saved;
        }
        private static Material ParticleMaterial(Material source)
        {
            string path = "Assets/_Game/Content/Materials/FX_" + source.name + ".mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null) { material = new Material(Shader.Find("Universal Render Pipeline/Particles/Unlit")); AssetDatabase.CreateAsset(material, path); }
            bool additive = source.shader.name.Contains("Additive");
            material.SetTexture("_BaseMap", source.mainTexture);
            material.SetColor("_BaseColor", additive ? new Color(4, 1.6f, 0.3f, 1) : source.HasProperty("_TintColor") ? source.GetColor("_TintColor") : Color.white);
            material.SetFloat("_Surface", 1); material.SetFloat("_Blend", additive ? 2 : 0);
            material.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
            material.SetFloat("_DstBlend", (float)(additive ? UnityEngine.Rendering.BlendMode.One : UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha));
            material.SetFloat("_SrcBlendAlpha", (float)UnityEngine.Rendering.BlendMode.One);
            material.SetFloat("_DstBlendAlpha", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            material.SetFloat("_ZWrite", 0); material.SetFloat("_Cull", 0);
            material.SetOverrideTag("RenderType", "Transparent"); material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            material.renderQueue = 3000; EditorUtility.SetDirty(material); return material;
        }
    }
}
