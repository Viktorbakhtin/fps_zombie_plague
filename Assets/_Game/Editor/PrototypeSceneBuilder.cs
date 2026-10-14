using FishNet.Component.Spawning;
using FishNet.Component.Transforming;
using FishNet.Managing;
using FishNet.Managing.Object;
using FishNet.Object;
using FishNet.Transporting.Tugboat;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using ZombiePlague.Infrastructure.Actors;
using ZombiePlague.Infrastructure.AI;
using ZombiePlague.Infrastructure.Bootstrap;
using ZombiePlague.Infrastructure.Combat;
using ZombiePlague.Infrastructure.Configs;
using ZombiePlague.Infrastructure.Player;

namespace ZombiePlague.Editor
{
    // Invoke through Unity MCP execute_code; never hand-author scene YAML.
    public static class PrototypeSceneBuilder
    {
        public static string Build()
        {
            const string root = "Assets/_Game/";
            System.IO.Directory.CreateDirectory(root + "Scenes");
            System.IO.Directory.CreateDirectory(root + "Prefabs");
            System.IO.Directory.CreateDirectory(root + "Content/Materials");
            System.IO.Directory.CreateDirectory(root + "Configs");
            AssetDatabase.Refresh();
            var config = AssetDatabase.LoadAssetAtPath<PrototypeConfig>(root + "Configs/Prototype.asset");
            if (config == null) { config = ScriptableObject.CreateInstance<PrototypeConfig>(); AssetDatabase.CreateAsset(config, root + "Configs/Prototype.asset"); }
            var shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) throw new System.Exception("URP Lit shader missing");
            var material = AssetDatabase.LoadAssetAtPath<Material>(root + "Content/Materials/Prototype.mat");
            if (material == null) { material = new Material(shader); AssetDatabase.CreateAsset(material, root + "Content/Materials/Prototype.mat"); }
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var floor = GameObject.CreatePrimitive(PrimitiveType.Cube); floor.name = "Floor";
            floor.transform.position = new Vector3(0, -0.5f, 0); floor.transform.localScale = new Vector3(30, 1, 30);
            floor.GetComponent<Renderer>().sharedMaterial = material;
            for (int i = 0; i < 4; i++)
            {
                var wall = GameObject.CreatePrimitive(PrimitiveType.Cube); wall.name = "Boundary" + i;
                wall.transform.position = i < 2 ? new Vector3(i == 0 ? -15 : 15, 2, 0) : new Vector3(0, 2, i == 2 ? -15 : 15);
                wall.transform.localScale = i < 2 ? new Vector3(1, 4, 30) : new Vector3(30, 4, 1);
                wall.GetComponent<Renderer>().sharedMaterial = material;
            }
            var light = new GameObject("Sun").AddComponent<Light>(); light.type = LightType.Directional;
            light.intensity = 1.5f; light.transform.rotation = Quaternion.Euler(50, -30, 0);
            RenderSettings.ambientLight = new Color(0.5f, 0.5f, 0.55f);
            var overview = new GameObject("Overview Camera").AddComponent<Camera>();
            overview.transform.position = new Vector3(0, 18, -18); overview.transform.LookAt(Vector3.zero);
            overview.tag = "MainCamera";
            var movement = AssetDatabase.LoadAssetAtPath<GoldSrcMovementConfig>(root + "Configs/GoldSrcMovement.asset");
            if (movement == null) { movement = ScriptableObject.CreateInstance<GoldSrcMovementConfig>(); AssetDatabase.CreateAsset(movement, root + "Configs/GoldSrcMovement.asset"); }
            var player = MakeActor("Player", config, material, false, movement);
            var zombie = MakeActor("Zombie", config, material, true, movement);
            var collection = AssetDatabase.LoadAssetAtPath<SinglePrefabObjects>(root + "Configs/NetworkPrefabs.asset");
            if (collection == null) { collection = ScriptableObject.CreateInstance<SinglePrefabObjects>(); AssetDatabase.CreateAsset(collection, root + "Configs/NetworkPrefabs.asset"); }
            collection.Clear(); collection.AddObject(player); collection.AddObject(zombie); EditorUtility.SetDirty(collection);
            var network = new GameObject("FishNet Prototype"); network.AddComponent<Tugboat>();
            var timing = network.AddComponent<FishNet.Managing.Timing.TimeManager>();
            var manager = network.AddComponent<NetworkManager>(); manager.SpawnablePrefabs = collection;
            timing.SetTickRate(100);
            var spawner = network.AddComponent<PlayerSpawner>(); spawner.SetPlayerPrefab(player);
            spawner.Spawns = new Transform[2];
            for (int i = 0; i < 2; i++)
            { var spawn = new GameObject("Spawn " + i); spawn.transform.position = new Vector3(i == 0 ? -3 : 3, 0.1f, -6); spawner.Spawns[i] = spawn.transform; }
            var session = network.AddComponent<PrototypeSession>(); session.Manager = manager;
            session.Config = config; session.ZombiePrefab = zombie;
            EditorSceneManager.SaveScene(scene, root + "Scenes/TestArena.unity");
            AssetDatabase.SaveAssets();
            return "Created TestArena, Player/Zombie prefabs, config and FishNet prefab collection.";
        }

        private static NetworkObject MakeActor(string name, PrototypeConfig config, Material material, bool zombie, GoldSrcMovementConfig movement)
        {
            var go = new GameObject(name);
            var nob = go.AddComponent<NetworkObject>();
            var nt = go.AddComponent<NetworkTransform>();
            var serialized = new SerializedObject(nt);
            serialized.FindProperty("_clientAuthoritative").boolValue = false;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            var controller = go.AddComponent<CharacterController>(); controller.height = 1.8f;
            controller.radius = 0.35f; controller.center = new Vector3(0, 0.9f, 0);
            var body = GameObject.CreatePrimitive(PrimitiveType.Capsule); body.name = "Body";
            body.transform.SetParent(go.transform, false); body.transform.localPosition = Vector3.up * 0.9f;
            body.transform.localScale = new Vector3(0.7f, 0.9f, 0.7f);
            Object.DestroyImmediate(body.GetComponent<Collider>()); body.GetComponent<Renderer>().sharedMaterial = material;
            var actor = go.AddComponent<NetworkActor>(); actor.Config = config; actor.StartsAsZombie = zombie;
            actor.Body = body.GetComponent<Renderer>();
            if (zombie) go.AddComponent<ServerZombie>();
            else
            {
                var pivot = new GameObject("ViewPivot"); pivot.transform.SetParent(go.transform, false);
                var visualRoot = new GameObject("View graphics"); visualRoot.transform.SetParent(go.transform, false);
                pivot.transform.SetParent(visualRoot.transform, false);
                pivot.transform.localPosition = Vector3.up * 1.6f;
                var camera = pivot.AddComponent<Camera>(); camera.enabled = false; camera.depth = 1;
                pivot.AddComponent<AudioListener>().enabled = false;
                var motor = go.AddComponent<ServerFpsMotor>(); motor.View = camera; motor.ViewPivot = pivot.transform;
                motor.Movement = movement;
                controller.height = 72 * movement.MetresPerUnit;
                controller.center = Vector3.up * controller.height * 0.5f;
                controller.radius = 16 * movement.MetresPerUnit;
                controller.stepOffset = movement.StepSize * movement.MetresPerUnit;
                controller.skinWidth = 0.001f;
                var prediction = new SerializedObject(nob);
                prediction.FindProperty("_enablePrediction").boolValue = true;
                prediction.FindProperty("_enableStateForwarding").boolValue = false;
                prediction.FindProperty("_networkTransform").objectReferenceValue = nt;
                prediction.FindProperty("_graphicalObject").objectReferenceValue = visualRoot.transform;
                prediction.FindProperty("_detachGraphicalObject").boolValue = true;
                prediction.FindProperty("_ownerSmoothedProperties").intValue = (int)TransformPropertiesFlag.Position;
                prediction.FindProperty("_enableTeleport").boolValue = true;
                prediction.ApplyModifiedPropertiesWithoutUndo();
                motor.Body = body.GetComponent<Renderer>(); go.AddComponent<NetworkWeapon>();
            }
            var prefab = PrefabUtility.SaveAsPrefabAsset(go, "Assets/_Game/Prefabs/" + name + ".prefab");
            Object.DestroyImmediate(go);
            return prefab.GetComponent<NetworkObject>();
        }
    }
}
