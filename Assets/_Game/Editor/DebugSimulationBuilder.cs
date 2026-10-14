using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using Unity.AI.Navigation;
using ZombiePlague.Infrastructure.AI;
using ZombiePlague.Infrastructure.Configs;
namespace ZombiePlague.Editor
{
    public static class DebugSimulationBuilder
    {
        public static string Upgrade()
        {
            const string root = "Assets/_Game/";
            EditorSceneManager.OpenScene(root + "Scenes/DebugArena.unity");
            var config = AssetDatabase.LoadAssetAtPath<BotConfig>(root + "Configs/DebugBots.asset");
            if (config == null) { config = ScriptableObject.CreateInstance<BotConfig>(); AssetDatabase.CreateAsset(config, root + "Configs/DebugBots.asset"); }
            var bot = PrefabUtility.LoadPrefabContents(root + "Prefabs/DebugBot.prefab");
            bot.GetComponent<DebugBot>().Config = config;
            PrefabUtility.SaveAsPrefabAsset(bot, root + "Prefabs/DebugBot.prefab"); PrefabUtility.UnloadPrefabContents(bot);
            var environment = GameObject.Find("Template environment assets");
            if (environment.transform.Find("Simulation cover") == null)
            {
                var cover = new GameObject("Simulation cover"); cover.transform.SetParent(environment.transform);
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Art/Environment/Tileables/Pfb_simpleWall_2x2x0_4.prefab");
                for (int i = 0; i < 3; i++)
                {
                    var wall = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
                    wall.transform.SetParent(cover.transform);
                    wall.transform.SetPositionAndRotation(new Vector3(-2 + i * 2, 0, 0), prefab.transform.localRotation);
                }
            }
            var surface = environment.GetComponent<NavMeshSurface>();
            if (surface == null) surface = environment.AddComponent<NavMeshSurface>();
            surface.collectObjects = CollectObjects.Children; surface.useGeometry = NavMeshCollectGeometry.PhysicsColliders;
            surface.BuildNavMesh();
            string assetPath = root + "Configs/DebugNavigation.asset";
            var previous = AssetDatabase.LoadAssetAtPath<NavMeshData>(assetPath);
            if (previous == null) AssetDatabase.CreateAsset(surface.navMeshData, assetPath);
            else { EditorUtility.CopySerialized(surface.navMeshData, previous); surface.navMeshData = previous; EditorUtility.SetDirty(previous); }
            EditorUtility.SetDirty(surface); AssetDatabase.SaveAssets();
            EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
            return "Bot configuration assigned, template cover added, navigation mesh baked.";
        }
    }
}
