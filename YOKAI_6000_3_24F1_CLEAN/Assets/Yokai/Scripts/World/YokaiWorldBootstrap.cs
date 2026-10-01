using UnityEngine;

namespace Yokai
{
    public sealed class YokaiWorldBootstrap : MonoBehaviour
    {
        public static YokaiWorldBootstrap Instance { get; private set; }

        Transform player;
        YokaiEncounterDirector director;

        void Awake()
        {
            Instance = this;
            YokaiSaveSystem.Load();

            BuildLighting();
            BuildWorld();
            BuildSystems();
            BuildPlayer();
            BuildEncounters();
        }

        void BuildSystems()
        {
            GameObject sessionGo = new GameObject("YOKAI_GameSession");
            YokaiGameSession session = sessionGo.AddComponent<YokaiGameSession>();

            GameObject perfGo = new GameObject("YOKAI_Performance");
            perfGo.AddComponent<YokaiPerformanceManager>();

            GameObject audioGo = new GameObject("YOKAI_Audio");
            audioGo.AddComponent<YokaiAudioManager>();

            GameObject tutorialGo = new GameObject("YOKAI_Tutorial");
            tutorialGo.AddComponent<YokaiTutorialDirector>();

            GameObject coordinatorGo = new GameObject("YOKAI_AttackCoordinator");
            coordinatorGo.AddComponent<YokaiAttackCoordinator>();

            GameObject feedbackGo = new GameObject("YOKAI_Feedback");
            feedbackGo.AddComponent<YokaiFeedbackManager>();

            GameObject projectilePoolGo = new GameObject("YOKAI_ProjectilePool");
            projectilePoolGo.AddComponent<YokaiProjectilePool>();

            GameObject qaGo = new GameObject("YOKAI_RuntimeQA");
            qaGo.AddComponent<YokaiRuntimeQA>();

            GameObject dirGo = new GameObject("YOKAI_EncounterDirector");
            director = dirGo.AddComponent<YokaiEncounterDirector>();
            session.director = director;
        }

        void BuildPlayer()
        {
            GameObject hero = new GameObject("Hunter_Akira");
            hero.transform.position = YokaiSaveSystem.GetCheckpoint();

            CharacterController cc = hero.AddComponent<CharacterController>();
            cc.height = 1.92f;
            cc.radius = .38f;
            cc.center = new Vector3(0f,.96f,0f);

            YokaiAttributes a = hero.AddComponent<YokaiAttributes>();
            a.Configure(150f,110f,100f,110f,24f,6f);
            a.spirit = 45f;

            YokaiMotor motor = hero.AddComponent<YokaiMotor>();
            YokaiLockOn lockOn = hero.AddComponent<YokaiLockOn>();
            hero.AddComponent<YokaiElementalStatus>();
            hero.AddComponent<YokaiHunterArts>();
            YokaiHumanoidVisual visual = YokaiHumanoidVisual.Build(hero, new Color(.11f,.14f,.18f), new Color(.55f,.06f,.06f), 1f);
            YokaiCombat combat = hero.AddComponent<YokaiCombat>();
            hero.AddComponent<YokaiSkills>();
            hero.AddComponent<YokaiCombatMotion>();
            hero.AddComponent<YokaiAnimatorBridge>();
            hero.AddComponent<YokaiPoseAnimator>();
            hero.AddComponent<YokaiFootGrounding>();
            hero.AddComponent<YokaiWeaponTrail>();
            hero.AddComponent<YokaiAuthoredActor>().resourcePath = "YokaiArt/Player";
            YokaiPlayerController controller = hero.AddComponent<YokaiPlayerController>();

            GameObject attackOrigin = new GameObject("AttackOrigin");
            attackOrigin.transform.SetParent(hero.transform,false);
            attackOrigin.transform.localPosition = new Vector3(0f,1.0f,.25f);
            combat.attackOrigin = attackOrigin.transform;

            GameObject cameraGo = new GameObject("Main Camera");
            cameraGo.tag = "MainCamera";
            cameraGo.AddComponent<Camera>();
            cameraGo.AddComponent<AudioListener>();
            YokaiCameraRig cam = cameraGo.AddComponent<YokaiCameraRig>();
            cam.transform.position = hero.transform.position + new Vector3(0f,2.6f,-4f);
            cam.Attach(hero.transform, lockOn);
            motor.cameraTransform = cameraGo.transform;
            controller.cameraRig = cam;

            YokaiMobileHUD hud = hero.AddComponent<YokaiMobileHUD>();
            hud.Bind(hero, controller, combat, a, lockOn);
            controller.mobileHud = hud;

            player = hero.transform;
            director.player = player;
            YokaiGameSession.Instance.SetPlayer(player);
            if (YokaiTutorialDirector.Instance != null)
                YokaiTutorialDirector.Instance.Bind(player);
        }

        void BuildEncounters()
        {
            GameObject gate1 = BuildGate("Gate_First", new Vector3(0f,1.4f,18f), new Color(.5f,.08f,.05f), true);
            GameObject gate2 = BuildGate("Gate_Elite", new Vector3(0f,1.4f,51f), new Color(.35f,.07f,.5f), true);
            director.gateOne = gate1;
            director.gateTwo = gate2;

            director.firstEncounter.Add(BuildEnemy("Mire Grunt A", YokaiEnemyArchetype.Grunt, new Vector3(-3f,1f,7f), new Color(.24f,.28f,.22f)));
            director.firstEncounter.Add(BuildEnemy("Mire Grunt B", YokaiEnemyArchetype.Grunt, new Vector3(3f,1f,9f), new Color(.24f,.28f,.22f)));
            director.firstEncounter.Add(BuildEnemy("Mirror Ronin", YokaiEnemyArchetype.Ronin, new Vector3(0f,1f,13f), new Color(.22f,.18f,.2f)));

            director.eliteEncounter.Add(BuildEnemy("Crimson Ronin", YokaiEnemyArchetype.Elite, new Vector3(0f,1f,34f), new Color(.34f,.08f,.07f)));
            director.eliteEncounter.Add(BuildEnemy("Stalker", YokaiEnemyArchetype.Stalker, new Vector3(-4f,1f,31f), new Color(.14f,.2f,.23f)));
            director.eliteEncounter.Add(BuildEnemy("Stone Brute", YokaiEnemyArchetype.Heavy, new Vector3(4.5f,1f,38f), new Color(.24f,.19f,.14f)));

            BuildShrine(new Vector3(0f,0f,55f));

            YokaiBoss boss = BuildBoss(new Vector3(0f,1f,78f));
            director.boss = boss;

            if (YokaiSaveSystem.Data.bossDefeated)
            {
                boss.gameObject.SetActive(false);
                YokaiGameSession.Instance.RestoreSavedVictory();
            }
        }

        YokaiEnemy BuildEnemy(string name, YokaiEnemyArchetype type, Vector3 position, Color color)
        {
            GameObject go = new GameObject(name);
            go.transform.position = position;

            CharacterController cc = go.AddComponent<CharacterController>();
            cc.height = 1.9f;
            cc.radius = .4f;
            cc.center = new Vector3(0f,.95f,0f);

            go.AddComponent<YokaiAttributes>();
            go.AddComponent<YokaiElementalStatus>();
            float visualScale = type == YokaiEnemyArchetype.Heavy ? 1.16f : (type == YokaiEnemyArchetype.Elite ? 1.12f : (type == YokaiEnemyArchetype.Stalker ? .94f : 1f));
            YokaiHumanoidVisual visual = YokaiHumanoidVisual.Build(go, color, new Color(.52f,.17f,.08f), visualScale);
            YokaiEnemy enemy = go.AddComponent<YokaiEnemy>();
            go.AddComponent<YokaiPoseAnimator>();
            go.AddComponent<YokaiFootGrounding>();
            go.AddComponent<YokaiEnemyIndicator>();
            enemy.Configure(type, player);
            visual.ApplyArchetype(type);
            go.AddComponent<YokaiAuthoredActor>().resourcePath = "YokaiArt/" + type;
            return enemy;
        }

        YokaiBoss BuildBoss(Vector3 position)
        {
            GameObject go = new GameObject("ONI_WARDEN_KAGANE");
            go.transform.position = position;

            CharacterController cc = go.AddComponent<CharacterController>();
            cc.height = 2.5f;
            cc.radius = .55f;
            cc.center = new Vector3(0f,1.25f,0f);

            go.AddComponent<YokaiAttributes>();
            go.AddComponent<YokaiElementalStatus>();
            YokaiHumanoidVisual visual = YokaiHumanoidVisual.Build(go, new Color(.09f,.07f,.08f), new Color(.62f,.04f,.03f), 1.3f);
            YokaiBoss boss = go.AddComponent<YokaiBoss>();
            go.AddComponent<YokaiPoseAnimator>();
            go.AddComponent<YokaiFootGrounding>();
            visual.ApplyArchetype(YokaiEnemyArchetype.Boss);
            go.AddComponent<YokaiBossPresentation>();
            boss.ConfigureBoss(player);
            go.AddComponent<YokaiAuthoredActor>().resourcePath = "YokaiArt/Kagane";
            return boss;
        }

        void BuildShrine(Vector3 position)
        {
            GameObject shrine = new GameObject("Spirit_Shrine");
            shrine.transform.position = position;
            YokaiShrine component = shrine.AddComponent<YokaiShrine>();
            shrine.AddComponent<YokaiElementReactive>();

            Material stone = YokaiMaterialLibrary.Get("shrine_stone", new Color(.20f,.205f,.19f), 0f, .22f);
            Material red = YokaiMaterialLibrary.Get("shrine_red", new Color(.45f,.045f,.035f), .02f, .24f);
            Material roof = YokaiMaterialLibrary.Get("shrine_roof", new Color(.10f,.09f,.085f), .03f, .24f);
            YokaiArtUtility.MeshPart("StoneBase", shrine.transform, YokaiMeshLibrary.TaperedBox("shrine_base",1f,1f,.90f,.90f,1f), new Vector3(0f,.24f,0f), new Vector3(2.8f,.48f,2.0f), Quaternion.identity, stone);
            YokaiArtUtility.MeshPart("PostL", shrine.transform, YokaiMeshLibrary.Frustum("shrine_post",8,.5f,.44f,1f), new Vector3(-.92f,1.35f,0f), new Vector3(.24f,2.2f,.24f), Quaternion.identity, red);
            YokaiArtUtility.MeshPart("PostR", shrine.transform, YokaiMeshLibrary.Frustum("shrine_post",8,.5f,.44f,1f), new Vector3(.92f,1.35f,0f), new Vector3(.24f,2.2f,.24f), Quaternion.identity, red);
            YokaiArtUtility.MeshPart("Roof", shrine.transform, YokaiMeshLibrary.Roof("shrine_small_roof"), new Vector3(0f,2.42f,0f), new Vector3(2.9f,.95f,1.65f), Quaternion.identity, roof);

            GameObject core = YokaiArtUtility.MeshPart("SpiritCore", shrine.transform, YokaiMeshLibrary.LowSphere("shrine_core_sphere",10,5), new Vector3(0f,1.3f,.1f), Vector3.one*.75f, Quaternion.identity, YokaiMaterialLibrary.Emissive("shrine_core", new Color(.15f,.75f,.55f)));
            core.GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

            GameObject cp = new GameObject("CheckpointPoint");
            cp.transform.SetParent(shrine.transform,false);
            cp.transform.localPosition = new Vector3(0f,1f,-2f);
            component.checkpointPoint = cp.transform;

            SphereCollider trigger = shrine.AddComponent<SphereCollider>();
            trigger.isTrigger = true;
            trigger.radius = 2.4f;
        }

        void BuildWorld()
        {
            GameObject world = new GameObject("Hoshikawa_Cedar_Pass");
            world.AddComponent<YokaiAtmosphere>();
            world.AddComponent<YokaiWorldMood>();

            // Stable collision shell: intentionally simple so mobile traversal never catches on decorative art.
            GameObject ground = CreatePrimitive(PrimitiveType.Cube, world.transform, new Vector3(0f,-.42f,35f), new Vector3(36f,.70f,120f), new Color(.08f,.10f,.08f));
            ground.name = "GameplayGround";
            Renderer groundRenderer = ground.GetComponent<Renderer>();
            if (groundRenderer != null) groundRenderer.enabled = false;

            GameObject road = CreatePrimitive(PrimitiveType.Cube, world.transform, new Vector3(0f,.01f,35f), new Vector3(5.8f,.10f,115f), new Color(.18f,.13f,.08f));
            road.name = "GameplayRoad";
            Renderer roadRenderer = road.GetComponent<Renderer>();
            if (roadRenderer != null) roadRenderer.enabled = false;

            YokaiWorldArt.Build(world.transform);

            BuildSpiritWisp(world.transform, new Vector3(-3.6f,1.1f,20f));
            BuildSpiritWisp(world.transform, new Vector3(3.4f,1.15f,43f));
            BuildSpiritWisp(world.transform, new Vector3(-4.2f,1.05f,61f));
        }

        GameObject BuildGate(string name, Vector3 position, Color color, bool collisionWall)
        {
            GameObject gate = new GameObject(name);
            gate.transform.position = position;
            Material gateMat = YokaiMaterialLibrary.Get("gate_"+name, color, .03f, .22f);
            Material capMat = YokaiMaterialLibrary.Get("gate_cap_"+name, new Color(.08f,.045f,.035f), .02f, .18f);
            YokaiArtUtility.MeshPart("PostL", gate.transform, YokaiMeshLibrary.Frustum("gate_post",8,.5f,.42f,1f), new Vector3(-2f,1.4f,0f), new Vector3(.34f,2.8f,.34f), Quaternion.identity, gateMat);
            YokaiArtUtility.MeshPart("PostR", gate.transform, YokaiMeshLibrary.Frustum("gate_post",8,.5f,.42f,1f), new Vector3(2f,1.4f,0f), new Vector3(.34f,2.8f,.34f), Quaternion.identity, gateMat);
            YokaiArtUtility.MeshPart("Lintel", gate.transform, YokaiMeshLibrary.TaperedBox("gate_beam",1f,1f,.92f,.92f,1f), new Vector3(0f,2.75f,0f), new Vector3(4.8f,.28f,.38f), Quaternion.identity, gateMat);
            YokaiArtUtility.MeshPart("TopCap", gate.transform, YokaiMeshLibrary.TaperedBox("gate_top",1f,1f,.94f,.94f,1f), new Vector3(0f,3.08f,0f), new Vector3(5.35f,.18f,.43f), Quaternion.identity, capMat);

            if (collisionWall)
            {
                BoxCollider wall = gate.AddComponent<BoxCollider>();
                wall.center = new Vector3(0f,1.1f,0f);
                wall.size = new Vector3(4.5f,2.2f,.45f);
            }
            return gate;
        }

        void BuildTree(Transform parent, Vector3 position, float scale)
        {
            GameObject tree = new GameObject("Cedar");
            tree.transform.SetParent(parent,false);
            tree.transform.localPosition = position;

            CreatePrimitive(PrimitiveType.Cylinder, tree.transform, new Vector3(0f,2f,0f), new Vector3(.35f*scale,2f*scale,.35f*scale), new Color(.18f,.11f,.07f));
            GameObject crownA = CreatePrimitive(PrimitiveType.Sphere, tree.transform, new Vector3(0f,4.2f*scale,0f), new Vector3(1.7f*scale,2.5f*scale,1.7f*scale), new Color(.08f,.22f,.11f));
            GameObject crownB = CreatePrimitive(PrimitiveType.Sphere, tree.transform, new Vector3(.25f,5.4f*scale,.1f), new Vector3(1.25f*scale,1.8f*scale,1.25f*scale), new Color(.07f,.19f,.1f));
            Collider crownColA = crownA.GetComponent<Collider>();
            Collider crownColB = crownB.GetComponent<Collider>();
            if (crownColA != null) Destroy(crownColA);
            if (crownColB != null) Destroy(crownColB);
            crownA.AddComponent<YokaiTreeSway>();
            crownB.AddComponent<YokaiTreeSway>();
        }

        void BuildLantern(Transform parent, Vector3 position)
        {
            GameObject root = new GameObject("StoneLantern");
            root.transform.SetParent(parent,false);
            root.transform.localPosition = position;
            root.AddComponent<YokaiElementReactive>();
            CreatePrimitive(PrimitiveType.Cylinder, root.transform, new Vector3(0f,.65f,0f), new Vector3(.16f,.65f,.16f), new Color(.3f,.3f,.28f));
            GameObject lamp = CreatePrimitive(PrimitiveType.Cube, root.transform, new Vector3(0f,1.35f,0f), new Vector3(.42f,.42f,.42f), new Color(.82f,.62f,.25f));
            lamp.GetComponent<Renderer>().sharedMaterial = YokaiMaterialLibrary.Emissive("lantern", new Color(.7f,.48f,.15f));
        }

        void BuildRock(Transform parent, Vector3 position, float scale)
        {
            GameObject rock = CreatePrimitive(PrimitiveType.Sphere, parent, position, new Vector3(scale*1.4f,scale*.75f,scale), new Color(.2f,.21f,.2f));
            rock.transform.rotation = Quaternion.Euler(0f,position.z*13f,12f);
        }

        GameObject CreatePrimitive(PrimitiveType type, Transform parent, Vector3 localPosition, Vector3 localScale, Color color)
        {
            GameObject go = GameObject.CreatePrimitive(type);
            go.transform.SetParent(parent,false);
            go.transform.localPosition = localPosition;
            go.transform.localScale = localScale;
            go.GetComponent<Renderer>().sharedMaterial = YokaiMaterialLibrary.Get("world_" + color.ToString(), color, .02f, .28f);
            return go;
        }


        void BuildSpiritWisp(Transform parent, Vector3 position)
        {
            GameObject root = new GameObject("Spirit_Wisp");
            root.transform.SetParent(parent,false);
            root.transform.localPosition = position;

            GameObject orb = YokaiArtUtility.MeshPart("WispCore", root.transform, YokaiMeshLibrary.LowSphere("wisp_orb",8,4), Vector3.zero, Vector3.one*.52f, Quaternion.identity,
                YokaiMaterialLibrary.Emissive("spirit_wisp", new Color(.18f,.8f,1f)));
            orb.GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

            YokaiSpiritWisp wisp = root.AddComponent<YokaiSpiritWisp>();
            wisp.spiritReward = 18f;
            wisp.bladeFlowReward = 8f;
        }

        void BuildLighting()
        {
            RenderSettings.ambientLight = new Color(.22f,.25f,.3f);
            RenderSettings.fog = true;
            RenderSettings.fogColor = new Color(.12f,.17f,.2f);
            RenderSettings.fogDensity = .008f;

            GameObject sun = new GameObject("MoonSun");
            Light light = sun.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.15f;
            light.color = new Color(.8f,.87f,1f);
            sun.transform.rotation = Quaternion.Euler(48f,-35f,0f);
        }
    }
}
