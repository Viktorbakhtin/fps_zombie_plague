using FishNet.Component.Spawning;
using FishNet.Managing.Object;
using FishNet.Object;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using ZombiePlague.Infrastructure.AI;
using ZombiePlague.Infrastructure.Actors;
using ZombiePlague.Infrastructure.Bootstrap;
using ZombiePlague.Infrastructure.Combat;
using ZombiePlague.Infrastructure.Player;

namespace ZombiePlague.Editor
{
    public static class DebugSceneBuilder
    {
        private const string Root = "Assets/_Game/";
        public static string Build()
        {
            EditorSceneManager.OpenScene(Root + "Scenes/TestArena.unity");
            var session = Object.FindFirstObjectByType<PrototypeSession>();
            session.AutoHost = true; session.SpawnInitialZombie = false; session.Port = 7778;
            var floor = GameObject.Find("Floor"); if (floor != null) Object.DestroyImmediate(floor);
            for (int i = 0; i < 4; i++) { var wall = GameObject.Find("Boundary" + i); if(wall != null) Object.DestroyImmediate(wall); }
            var environment = new GameObject("Template environment assets");
            for(int x = -8; x < 8; x += 2) for(int z = -8; z < 8; z += 2)
                Place("Assets/Art/Environment/Tileables/Pfb_simpleFloor_2x2x0_4.prefab", environment.transform, new Vector3(x+2,0,z+2), Quaternion.identity);
            for(int i = -8; i < 8; i += 2)
            {
                Place("Assets/Art/Environment/Tileables/Pfb_simpleWall_2x2x0_4.prefab", environment.transform,new Vector3(i+2,0,-8),Quaternion.identity);
                Place("Assets/Art/Environment/Tileables/Pfb_simpleWall_2x2x0_4.prefab", environment.transform,new Vector3(i,0,8),Quaternion.Euler(0,180,0));
                Place("Assets/Art/Environment/Tileables/Pfb_simpleWall_2x2x0_4.prefab", environment.transform,new Vector3(-8,0,i),Quaternion.Euler(0,90,0));
                Place("Assets/Art/Environment/Tileables/Pfb_simpleWall_2x2x0_4.prefab", environment.transform,new Vector3(8,0,i+2),Quaternion.Euler(0,-90,0));
            }
            var animator = MakeAnimator();
            var playerGo = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(Root+"Prefabs/Player.prefab"));
            playerGo.name = "DebugPlayer";
            AddVisual(playerGo, animator);
            var motor = playerGo.GetComponent<ServerFpsMotor>();
            var gun = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Art/Guns/AssaultRifle/Pfb_assaultRifle.prefab"));
            gun.transform.SetParent(motor.ViewPivot,false); gun.transform.localPosition = new Vector3(0.2f,-0.22f,0.35f);
            gun.transform.localRotation = Quaternion.Euler(0,-90,0); gun.transform.localScale = Vector3.one * 0.65f;
            playerGo.GetComponent<ActorPresentation>().ViewWeapon = gun.transform;
            var player = PrefabUtility.SaveAsPrefabAsset(playerGo,Root+"Prefabs/DebugPlayer.prefab").GetComponent<NetworkObject>(); Object.DestroyImmediate(playerGo);
            var botGo = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(Root+"Prefabs/Zombie.prefab"));
            botGo.name = "DebugBot"; Object.DestroyImmediate(botGo.GetComponent<ServerZombie>());
            botGo.GetComponent<NetworkActor>().StartsAsZombie = false;
            botGo.AddComponent<NetworkWeapon>(); botGo.AddComponent<DebugBot>(); AddVisual(botGo,animator);
            var bot = PrefabUtility.SaveAsPrefabAsset(botGo,Root+"Prefabs/DebugBot.prefab").GetComponent<NetworkObject>(); Object.DestroyImmediate(botGo);
            var collection = AssetDatabase.LoadAssetAtPath<SinglePrefabObjects>(Root+"Configs/DebugPrefabs.asset");
            if(collection == null) { collection = ScriptableObject.CreateInstance<SinglePrefabObjects>(); AssetDatabase.CreateAsset(collection,Root+"Configs/DebugPrefabs.asset"); }
            collection.Clear(); collection.AddObject(player); collection.AddObject(bot); EditorUtility.SetDirty(collection);
            session.Manager.SpawnablePrefabs = collection;
            session.GetComponent<PlayerSpawner>().SetPlayerPrefab(player);
            var director = session.gameObject.AddComponent<DebugRound>(); director.Session = session; director.BotPrefab = bot;
            EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene(),Root+"Scenes/DebugArena.unity");
            AssetDatabase.SaveAssets();
            DebugSimulationBuilder.Upgrade();
            CombatEffectsBuilder.Upgrade();
            return "DebugArena: auto host, 2 bots, random first infection, template environment/model/animations/rifle.";
        }
        private static void Place(string path, Transform parent, Vector3 position, Quaternion rotation)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if(prefab == null) throw new System.Exception("Missing template asset: "+path);
            var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab); go.transform.SetParent(parent);
            go.transform.SetPositionAndRotation(position,rotation * prefab.transform.localRotation);
        }
        private static void AddVisual(GameObject actorGo, RuntimeAnimatorController controller)
        {
            var oldBody = actorGo.transform.Find("Body"); if(oldBody != null) Object.DestroyImmediate(oldBody.gameObject);
            var source = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/PlayerGhosts/ArmaturePlayer_Rifle.prefab");
            var visual = Object.Instantiate(source.transform.Find("Armature_3P").gameObject);
            visual.name = "Template character visual"; visual.transform.SetParent(actorGo.transform,false);
            visual.transform.localPosition = Vector3.zero; visual.transform.localRotation = Quaternion.identity; visual.transform.localScale = Vector3.one;
            foreach(var script in visual.GetComponentsInChildren<MonoBehaviour>(true)) Object.DestroyImmediate(script);
            foreach(var collider in visual.GetComponentsInChildren<Collider>(true)) Object.DestroyImmediate(collider);
            var animator = visual.GetComponent<Animator>(); animator.runtimeAnimatorController = controller; animator.applyRootMotion = false;
            var actor = actorGo.GetComponent<NetworkActor>();
            foreach(var renderer in visual.GetComponentsInChildren<Renderer>(true))
            {
                if(renderer.name == "Armature_Mesh") actor.Body = renderer;
                if(renderer.name.Contains("ArmsOnly")) renderer.gameObject.SetActive(false);
            }
            var motor = actorGo.GetComponent<ServerFpsMotor>(); if(motor != null) motor.Body = actor.Body;
            var presentation = actorGo.AddComponent<ActorPresentation>(); presentation.Actor = actor;
            presentation.ThirdPersonVisual = visual; presentation.Animator = animator;
            AddSound(actorGo,presentation);
        }
        private static void AddSound(GameObject go, ActorPresentation presentation)
        {
            var audio = go.GetComponent<AudioSource>(); if(audio==null) audio=go.AddComponent<AudioSource>();
            audio.playOnAwake=false; audio.spatialBlend=0.75f; audio.minDistance=2; audio.maxDistance=25;
            presentation.ShotAudio=AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/Clips/Weapons/Rifle/Shoot/Rifle_Shoot-001.wav");
            presentation.ReloadAudio=AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/Clips/Weapons/Rifle/Reload/rifle_reload.wav");
        }
        public static string UpgradeVisuals()
        {
            var controller=MakeAnimator();
            foreach(var path in new string[]{Root+"Prefabs/DebugPlayer.prefab",Root+"Prefabs/DebugBot.prefab"})
            {
                var go=PrefabUtility.LoadPrefabContents(path); var presentation=go.GetComponent<ActorPresentation>();
                presentation.Animator.runtimeAnimatorController=controller;
                AddSound(go,presentation);
                var motor=go.GetComponent<ServerFpsMotor>();
                if(motor!=null && motor.ViewPivot.childCount>0) presentation.ViewWeapon=motor.ViewPivot.GetChild(0);
                PrefabUtility.SaveAsPrefabAsset(go,path); PrefabUtility.UnloadPrefabContents(go);
            }
            AssetDatabase.SaveAssets(); return "Directional movement, upper-body shoot/reload animation and template audio connected";
        }
        private static AnimatorController MakeAnimator()
        {
            System.IO.Directory.CreateDirectory(Root+"Content/Animations"); AssetDatabase.Refresh();
            var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(Root+"Content/Animations/DebugCharacter.controller");
            if(controller != null) { UpgradeAnimator(controller); return controller; }
            controller = AnimatorController.CreateAnimatorControllerAtPath(Root+"Content/Animations/DebugCharacter.controller");
            controller.AddParameter("Speed",AnimatorControllerParameterType.Float);
            var machine = controller.layers[0].stateMachine;
            var idle = machine.AddState("Idle"); idle.motion = CleanClip("Assets/Art/Characters/Armature/Animations/Stand--Idle.anim.fbx","DebugIdle");
            var run = machine.AddState("Move"); run.motion = CleanClip("Assets/Art/Characters/Armature/Animations/Locomotion--Run_N.anim.fbx","DebugRun");
            machine.defaultState = idle;
            var start = idle.AddTransition(run); start.hasExitTime = false; start.duration = 0.1f; start.AddCondition(AnimatorConditionMode.Greater,0.1f,"Speed");
            var stop = run.AddTransition(idle); stop.hasExitTime = false; stop.duration = 0.1f; stop.AddCondition(AnimatorConditionMode.Less,0.1f,"Speed");
            UpgradeAnimator(controller); return controller;
        }
        private static void UpgradeAnimator(AnimatorController controller)
        {
            if(controller.layers.Length>1) return;
            controller.AddParameter("Forward",AnimatorControllerParameterType.Float);
            controller.AddParameter("Shoot",AnimatorControllerParameterType.Trigger);
            controller.AddParameter("Reload",AnimatorControllerParameterType.Trigger);
            var tree=new BlendTree { name="Directional run", blendType=BlendTreeType.Simple1D, blendParameter="Forward",useAutomaticThresholds=false };
            tree.AddChild(CleanClip("Assets/Art/Characters/Armature/Animations/Locomotion--Run_S.anim.fbx","DebugRunBack"),-1);
            tree.AddChild(CleanClip("Assets/Art/Characters/Armature/Animations/Locomotion--Run_N.anim.fbx","DebugRun"),1);
            AssetDatabase.AddObjectToAsset(tree,controller);
            foreach(var child in controller.layers[0].stateMachine.states) if(child.state.name=="Move") child.state.motion=tree;
            controller.AddLayer("Upper body"); var layers=controller.layers;
            layers[1].defaultWeight=1; layers[1].avatarMask=AssetDatabase.LoadAssetAtPath<AvatarMask>("Assets/Art/Characters/Armature/Animations/ArmatureUpperBody.mask");
            controller.layers=layers;
            var machine=layers[1].stateMachine;
            var empty=machine.AddState("Ready"); empty.writeDefaultValues=false; machine.defaultState=empty;
            var shoot=machine.AddState("Shoot"); shoot.writeDefaultValues=false;
            shoot.motion=CleanClip("Assets/Art/Characters/Armature/Animations/Armature_Shoot_AssaultRifle F Recoil.fbx","DebugShoot",false);
            var reload=machine.AddState("Reload"); reload.writeDefaultValues=false;
            reload.motion=CleanClip("Assets/Art/Characters/Armature/Animations/ShootingPoses/Armature_Shoot_Reload.fbx","DebugReload",false);
            reload.speed=reload.motion is AnimationClip clip ? clip.length/2.45f : 1;
            var shooting=machine.AddAnyStateTransition(shoot); shooting.hasExitTime=false; shooting.duration=0.02f; shooting.AddCondition(AnimatorConditionMode.If,0,"Shoot");
            var reloading=machine.AddAnyStateTransition(reload); reloading.hasExitTime=false; reloading.canTransitionToSelf=false; reloading.duration=0.05f; reloading.AddCondition(AnimatorConditionMode.If,0,"Reload");
            foreach(var state in new[]{shoot,reload}) { var finish=state.AddTransition(empty); finish.hasExitTime=true; finish.exitTime=1; finish.duration=0.05f; }
            EditorUtility.SetDirty(controller);
        }
        private static AnimationClip CleanClip(string source, string name, bool loop = true)
        {
            var existing=AssetDatabase.LoadAssetAtPath<AnimationClip>(Root+"Content/Animations/"+name+".anim"); if(existing!=null) return existing;
            foreach(var asset in AssetDatabase.LoadAllAssetsAtPath(source)) if(asset is AnimationClip clip && !clip.name.StartsWith("__"))
            {
                var copy = Object.Instantiate(clip); copy.name = name; AnimationUtility.SetAnimationEvents(copy,new AnimationEvent[0]);
                var settings = AnimationUtility.GetAnimationClipSettings(copy); settings.loopTime = loop; AnimationUtility.SetAnimationClipSettings(copy,settings);
                AssetDatabase.CreateAsset(copy,Root+"Content/Animations/"+name+".anim"); return copy;
            }
            throw new System.Exception("Missing animation clip: "+source);
        }
    }
}
