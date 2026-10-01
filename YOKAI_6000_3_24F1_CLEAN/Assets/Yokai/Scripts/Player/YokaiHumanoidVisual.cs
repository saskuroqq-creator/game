using UnityEngine;

namespace Yokai
{
    public sealed class YokaiHumanoidVisual : MonoBehaviour
    {
        public Transform chest;
        public Transform head;
        public Transform hips;
        public Transform leftUpperArm;
        public Transform leftLowerArm;
        public Transform rightUpperArm;
        public Transform rightLowerArm;
        public Transform leftUpperLeg;
        public Transform leftLowerLeg;
        public Transform rightUpperLeg;
        public Transform rightLowerLeg;
        public Transform leftHand;
        public Transform rightHand;
        public Transform leftFoot;
        public Transform rightFoot;
        public Transform weapon;

        Transform rig;
        YokaiMotor motor;
        YokaiCombat combat;
        YokaiEnemy enemy;
        float stepPhase;
        float hitReaction;
        float modelScale = 1f;
        Color basePrimary;
        Color baseAccent;

        public static YokaiHumanoidVisual Build(GameObject root, Color primary, Color accent, float scale)
        {
            YokaiHumanoidVisual visual = root.AddComponent<YokaiHumanoidVisual>();
            visual.BuildRig(primary, accent, scale);
            return visual;
        }

        void Awake()
        {
            motor = GetComponent<YokaiMotor>();
            combat = GetComponent<YokaiCombat>();
        }

        void Start()
        {
            if (motor == null) motor = GetComponent<YokaiMotor>();
            if (combat == null) combat = GetComponent<YokaiCombat>();
            if (enemy == null) enemy = GetComponent<YokaiEnemy>();
        }

        void BuildRig(Color primary, Color accent, float s)
        {
            modelScale = s;
            basePrimary = primary;
            baseAccent = accent;
            rig = new GameObject("HumanoidRig_3D").transform;
            rig.SetParent(transform, false);

            Material cloth = YokaiMaterialLibrary.Get("char_primary_"+primary.ToString(),primary,.02f,.24f);
            Material accentMat = YokaiMaterialLibrary.Get("char_accent_"+accent.ToString(),accent,.05f,.28f);
            Material skin = YokaiMaterialLibrary.Get("char_skin",new Color(.55f,.42f,.33f),0f,.32f);
            Material leather = YokaiMaterialLibrary.Get("char_leather",new Color(.07f,.045f,.035f),.02f,.20f);
            Material steel = YokaiMaterialLibrary.Get("char_blade",new Color(.64f,.69f,.76f),.82f,.80f);

            hips = MeshPart("Hips", YokaiMeshLibrary.TaperedBox("char_hips",.44f,.30f,.38f,.27f,.28f), rig,
                new Vector3(0f,.88f,0f), Vector3.one*s, Quaternion.identity, cloth).transform;
            chest = MeshPart("Chest", YokaiMeshLibrary.TaperedBox("char_torso",.46f,.31f,.62f,.34f,.62f), rig,
                new Vector3(0f,1.28f,0f), Vector3.one*s, Quaternion.identity, cloth).transform;
            head = MeshPart("Head", YokaiMeshLibrary.LowSphere("char_head",10,5), rig,
                new Vector3(0f,1.79f,0f), new Vector3(.55f,.62f,.54f)*s, Quaternion.identity, skin).transform;

            MeshPart("Scarf", YokaiMeshLibrary.TaperedBox("char_scarf",.62f,.40f,.56f,.36f,.12f), rig,
                new Vector3(0f,1.55f,-.01f), Vector3.one*s, Quaternion.identity, accentMat);
            MeshPart("ScarfTail_L", YokaiMeshLibrary.TaperedBox("cloth_strip",.13f,.05f,.08f,.04f,.70f), chest,
                new Vector3(-.17f,-.18f,-.20f), Vector3.one*s, Quaternion.Euler(18f,0f,-8f), accentMat);
            MeshPart("ScarfTail_R", YokaiMeshLibrary.TaperedBox("cloth_strip",.13f,.05f,.08f,.04f,.70f), chest,
                new Vector3(.16f,-.20f,-.19f), Vector3.one*s, Quaternion.Euler(22f,0f,9f), accentMat);

            leftUpperArm = Limb("L_UpperArm",rig,new Vector3(-.39f,1.39f,0f),new Vector3(.22f,.50f,.22f)*s,cloth).transform;
            leftLowerArm = Limb("L_LowerArm",leftUpperArm,new Vector3(0f,-.39f,0f),new Vector3(.19f,.46f,.19f)*s,cloth).transform;
            rightUpperArm = Limb("R_UpperArm",rig,new Vector3(.39f,1.39f,0f),new Vector3(.22f,.50f,.22f)*s,cloth).transform;
            rightLowerArm = Limb("R_LowerArm",rightUpperArm,new Vector3(0f,-.39f,0f),new Vector3(.19f,.46f,.19f)*s,cloth).transform;

            leftHand = MeshPart("L_Hand",YokaiMeshLibrary.LowSphere("char_hand",8,4),leftLowerArm,
                new Vector3(0f,-.31f,0f),Vector3.one*.24f*s,Quaternion.identity,skin).transform;
            rightHand = new GameObject("R_Hand").transform;
            rightHand.SetParent(rightLowerArm,false); rightHand.localPosition=new Vector3(0f,-.30f,0f);
            MeshPart("R_HandMesh",YokaiMeshLibrary.LowSphere("char_hand",8,4),rightHand,Vector3.zero,Vector3.one*.24f*s,Quaternion.identity,skin);

            leftUpperLeg = Limb("L_UpperLeg",rig,new Vector3(-.16f,.66f,0f),new Vector3(.26f,.61f,.28f)*s,cloth).transform;
            leftLowerLeg = Limb("L_LowerLeg",leftUpperLeg,new Vector3(0f,-.45f,0f),new Vector3(.22f,.55f,.23f)*s,cloth).transform;
            rightUpperLeg = Limb("R_UpperLeg",rig,new Vector3(.16f,.66f,0f),new Vector3(.26f,.61f,.28f)*s,cloth).transform;
            rightLowerLeg = Limb("R_LowerLeg",rightUpperLeg,new Vector3(0f,-.45f,0f),new Vector3(.22f,.55f,.23f)*s,cloth).transform;

            leftFoot = MeshPart("L_Foot",YokaiMeshLibrary.TaperedBox("char_foot",.22f,.34f,.18f,.39f,.15f),leftLowerLeg,
                new Vector3(0f,-.35f,.08f),Vector3.one*s,Quaternion.identity,leather).transform;
            rightFoot = MeshPart("R_Foot",YokaiMeshLibrary.TaperedBox("char_foot",.22f,.34f,.18f,.39f,.15f),rightLowerLeg,
                new Vector3(0f,-.35f,.08f),Vector3.one*s,Quaternion.identity,leather).transform;

            MeshPart("Belt",YokaiMeshLibrary.TaperedBox("char_belt",.50f,.32f,.47f,.31f,.11f),rig,new Vector3(0f,.98f,0f),Vector3.one*s,Quaternion.identity,accentMat);
            MeshPart("L_ShoulderGuard",YokaiMeshLibrary.TaperedBox("shoulder",.31f,.38f,.22f,.31f,.12f),rig,new Vector3(-.40f,1.52f,0f),Vector3.one*s,Quaternion.Euler(0f,0f,8f),accentMat);
            MeshPart("R_ShoulderGuard",YokaiMeshLibrary.TaperedBox("shoulder",.31f,.38f,.22f,.31f,.12f),rig,new Vector3(.40f,1.52f,0f),Vector3.one*s,Quaternion.Euler(0f,0f,-8f),accentMat);

            MeshPart("CoatBack",YokaiMeshLibrary.TaperedBox("coat_panel",.48f,.05f,.36f,.04f,.76f),hips,new Vector3(0f,-.33f,-.16f),Vector3.one*s,Quaternion.Euler(-5f,0f,0f),cloth);
            MeshPart("CoatLeft",YokaiMeshLibrary.TaperedBox("coat_side",.22f,.05f,.15f,.04f,.67f),hips,new Vector3(-.22f,-.31f,.03f),Vector3.one*s,Quaternion.Euler(0f,0f,8f),cloth);
            MeshPart("CoatRight",YokaiMeshLibrary.TaperedBox("coat_side",.22f,.05f,.15f,.04f,.67f),hips,new Vector3(.22f,-.31f,.03f),Vector3.one*s,Quaternion.Euler(0f,0f,-8f),cloth);

            // Default hunter silhouette: tied hair and half face guard. Enemy-specific passes can cover/replace these.
            MeshPart("HairCap",YokaiMeshLibrary.LowSphere("hair_cap",8,4),head,new Vector3(0f,.17f,-.02f),new Vector3(.56f,.30f,.54f),Quaternion.identity,leather);
            MeshPart("TopKnot",YokaiMeshLibrary.LowSphere("topknot",7,4),head,new Vector3(0f,.43f,-.05f),new Vector3(.23f,.25f,.23f),Quaternion.identity,leather);
            MeshPart("HalfMask",YokaiMeshLibrary.TaperedBox("half_mask",.36f,.10f,.27f,.08f,.23f),head,new Vector3(0f,-.05f,.25f),Vector3.one,Quaternion.identity,accentMat);

            MeshPart("Sheath",YokaiMeshLibrary.Frustum("sheath",6,.5f,.42f,1f),rig,new Vector3(-.31f,.92f,-.13f),new Vector3(.10f,.88f,.10f)*s,Quaternion.Euler(72f,-8f,10f),leather);
            GameObject blade = MeshPart("Katana",YokaiMeshLibrary.Blade("katana_blade"),rightHand,new Vector3(0f,-.16f,.42f),new Vector3(.55f,1.55f,.55f)*s,Quaternion.Euler(75f,0f,0f),steel);
            weapon = blade.transform;
            MeshPart("SwordGuard",YokaiMeshLibrary.Frustum("sword_guard",10,.5f,.5f,1f),rightHand,new Vector3(0f,-.17f,-.01f),new Vector3(.26f,.055f,.26f)*s,Quaternion.identity,accentMat);
            MeshPart("SwordGrip",YokaiMeshLibrary.Frustum("sword_grip",8,.5f,.44f,1f),rightHand,new Vector3(0f,-.18f,-.20f),new Vector3(.10f,.40f,.10f)*s,Quaternion.identity,leather);
        }

        public void ApplyArchetype(YokaiEnemyArchetype type)
        {
            if (rig == null) return;
            Transform identity = new GameObject("Archetype_"+type).transform;
            identity.SetParent(rig,false);
            Material bone = YokaiMaterialLibrary.Get("enemy_bone",new Color(.55f,.50f,.42f),.02f,.22f);
            Material black = YokaiMaterialLibrary.Get("enemy_black",new Color(.045f,.045f,.055f),.06f,.22f);
            Material red = YokaiMaterialLibrary.Get("enemy_red",new Color(.42f,.035f,.025f),.08f,.30f);
            Material steel = YokaiMaterialLibrary.Get("enemy_steel",new Color(.36f,.39f,.43f),.72f,.52f);

            if (type == YokaiEnemyArchetype.Grunt)
            {
                MeshPart("Kasa",YokaiMeshLibrary.Frustum("kasa",12,.62f,0f,.14f),identity,new Vector3(0f,2.08f,0f),new Vector3(.92f,1f,.92f)*modelScale,Quaternion.identity,baseAccentMaterial());
                MeshPart("FaceWrap",YokaiMeshLibrary.TaperedBox("face_wrap",.38f,.10f,.30f,.08f,.20f),identity,new Vector3(0f,1.75f,.27f),Vector3.one*modelScale,Quaternion.identity,black);
            }
            else if (type == YokaiEnemyArchetype.Ronin)
            {
                MeshPart("RoninKasa",YokaiMeshLibrary.Frustum("ronin_kasa",14,.68f,.08f,.17f),identity,new Vector3(0f,2.10f,0f),new Vector3(1.02f,1f,1.02f)*modelScale,Quaternion.identity,black);
                MeshPart("NeckGuard",YokaiMeshLibrary.TaperedBox("neck_guard",.62f,.34f,.52f,.30f,.17f),identity,new Vector3(0f,1.58f,0f),Vector3.one*modelScale,Quaternion.identity,steel);
                MeshPart("BackBanner",YokaiMeshLibrary.TaperedBox("back_flag",.26f,.05f,.20f,.04f,.82f),identity,new Vector3(-.36f,1.48f,-.20f),Vector3.one*modelScale,Quaternion.Euler(8f,0f,4f),baseAccentMaterial());
            }
            else if (type == YokaiEnemyArchetype.Stalker)
            {
                HideSword();
                MeshPart("FoxMask",YokaiMeshLibrary.TaperedBox("fox_mask",.42f,.12f,.29f,.08f,.43f),identity,new Vector3(0f,1.82f,.30f),Vector3.one*modelScale,Quaternion.Euler(-4f,0f,0f),bone);
                MeshPart("EarL",YokaiMeshLibrary.Frustum("fox_ear",5,.22f,0f,.45f),identity,new Vector3(-.18f,2.11f,.02f),Vector3.one*.66f*modelScale,Quaternion.Euler(0f,0f,-8f),bone);
                MeshPart("EarR",YokaiMeshLibrary.Frustum("fox_ear",5,.22f,0f,.45f),identity,new Vector3(.18f,2.11f,.02f),Vector3.one*.66f*modelScale,Quaternion.Euler(0f,0f,8f),bone);
                AddClaws(leftHand,black,modelScale); AddClaws(rightHand,black,modelScale);
            }
            else if (type == YokaiEnemyArchetype.Heavy)
            {
                HideSword();
                MeshPart("HeavyMask",YokaiMeshLibrary.TaperedBox("heavy_mask",.50f,.14f,.39f,.11f,.42f),identity,new Vector3(0f,1.80f,.29f),Vector3.one*modelScale,Quaternion.identity,steel);
                MeshPart("HeavyShoulderL",YokaiMeshLibrary.TaperedBox("heavy_shoulder",.46f,.52f,.31f,.42f,.24f),identity,new Vector3(-.49f,1.52f,0f),Vector3.one*modelScale,Quaternion.Euler(0f,0f,12f),steel);
                MeshPart("HeavyShoulderR",YokaiMeshLibrary.TaperedBox("heavy_shoulder",.46f,.52f,.31f,.42f,.24f),identity,new Vector3(.49f,1.52f,0f),Vector3.one*modelScale,Quaternion.Euler(0f,0f,-12f),steel);
                BuildKanabo(rightHand,black,steel,modelScale,1.10f);
            }
            else if (type == YokaiEnemyArchetype.Elite)
            {
                MeshPart("EliteMask",YokaiMeshLibrary.TaperedBox("elite_mask",.44f,.13f,.32f,.09f,.38f),identity,new Vector3(0f,1.82f,.29f),Vector3.one*modelScale,Quaternion.identity,red);
                BuildHorns(identity,new Vector3(0f,1.98f,0f),bone,modelScale*.72f);
                MeshPart("EliteChestPlate",YokaiMeshLibrary.TaperedBox("elite_plate",.57f,.36f,.64f,.34f,.38f),identity,new Vector3(0f,1.34f,.03f),Vector3.one*modelScale,Quaternion.identity,steel);
            }
            else if (type == YokaiEnemyArchetype.Boss)
            {
                HideSword();
                MeshPart("OniMask",YokaiMeshLibrary.TaperedBox("oni_mask",.58f,.16f,.42f,.10f,.52f),identity,new Vector3(0f,1.84f,.33f),Vector3.one*modelScale,Quaternion.identity,red);
                BuildHorns(identity,new Vector3(0f,2.08f,0f),bone,modelScale*1.05f);
                MeshPart("OniJaw",YokaiMeshLibrary.TaperedBox("oni_jaw",.43f,.13f,.50f,.16f,.20f),identity,new Vector3(0f,1.62f,.34f),Vector3.one*modelScale,Quaternion.identity,bone);
                MeshPart("BossChest",YokaiMeshLibrary.TaperedBox("boss_chest",.69f,.42f,.79f,.43f,.46f),identity,new Vector3(0f,1.37f,.02f),Vector3.one*modelScale,Quaternion.identity,steel);
                MeshPart("BossShoulderL",YokaiMeshLibrary.TaperedBox("boss_shoulder",.52f,.58f,.34f,.46f,.29f),identity,new Vector3(-.54f,1.54f,0f),Vector3.one*modelScale,Quaternion.Euler(0f,0f,14f),red);
                MeshPart("BossShoulderR",YokaiMeshLibrary.TaperedBox("boss_shoulder",.52f,.58f,.34f,.46f,.29f),identity,new Vector3(.54f,1.54f,0f),Vector3.one*modelScale,Quaternion.Euler(0f,0f,-14f),red);
                BuildKanabo(rightHand,black,steel,modelScale,1.45f);
                MeshPart("BackBanner",YokaiMeshLibrary.TaperedBox("boss_flag",.58f,.05f,.42f,.04f,1.35f),identity,new Vector3(0f,1.65f,-.30f),Vector3.one*modelScale,Quaternion.Euler(8f,0f,0f),red);
            }
        }

        Material baseAccentMaterial()
        {
            return YokaiMaterialLibrary.Get("char_accent_"+baseAccent.ToString(),baseAccent,.05f,.28f);
        }


        void HideSword()
        {
            if (weapon != null) weapon.gameObject.SetActive(false);
            if (rightHand == null) return;
            Transform guard = rightHand.Find("SwordGuard");
            Transform grip = rightHand.Find("SwordGrip");
            if (guard != null) guard.gameObject.SetActive(false);
            if (grip != null) grip.gameObject.SetActive(false);
        }
        void AddClaws(Transform hand, Material material, float s)
        {
            if (hand == null) return;
            for (int i=-1;i<=1;i++)
                MeshPart("Claw",YokaiMeshLibrary.Blade("claw_blade"),hand,new Vector3(i*.07f,-.08f,.20f),new Vector3(.18f,.50f,.18f)*s,Quaternion.Euler(78f,0f,i*5f),material);
        }

        void BuildHorns(Transform parent, Vector3 p, Material material, float s)
        {
            MeshPart("HornL",YokaiMeshLibrary.Frustum("oni_horn",7,.18f,0f,.60f),parent,p+new Vector3(-.20f,.11f,0f),Vector3.one*s,Quaternion.Euler(0f,0f,-20f),material);
            MeshPart("HornR",YokaiMeshLibrary.Frustum("oni_horn",7,.18f,0f,.60f),parent,p+new Vector3(.20f,.11f,0f),Vector3.one*s,Quaternion.Euler(0f,0f,20f),material);
        }

        void BuildKanabo(Transform hand, Material gripMat, Material metalMat, float s, float lengthScale)
        {
            if (hand == null) return;
            Transform root = new GameObject("Kanabo").transform; root.SetParent(hand,false); root.localPosition=new Vector3(0f,-.15f,.38f); root.localRotation=Quaternion.Euler(74f,0f,0f);
            MeshPart("Grip",YokaiMeshLibrary.Frustum("kanabo_grip",8,.5f,.45f,1f),root,new Vector3(0f,-.22f,0f),new Vector3(.11f,.48f,.11f)*s,Quaternion.identity,gripMat);
            MeshPart("Club",YokaiMeshLibrary.Frustum("kanabo_club",10,.26f,.18f,1f),root,new Vector3(0f,.46f,0f),new Vector3(.72f,1.22f*lengthScale,.72f)*s,Quaternion.identity,metalMat);
            for(int i=0;i<4;i++)
            {
                float y=.05f+i*.26f;
                MeshPart("Stud_"+i,YokaiMeshLibrary.Frustum("kanabo_stud",6,.16f,0f,.25f),root,new Vector3(.19f,y,0f),Vector3.one*.52f*s,Quaternion.Euler(0f,0f,-90f),metalMat);
                MeshPart("StudB_"+i,YokaiMeshLibrary.Frustum("kanabo_stud",6,.16f,0f,.25f),root,new Vector3(-.19f,y,0f),Vector3.one*.52f*s,Quaternion.Euler(0f,0f,90f),metalMat);
            }
        }

        GameObject Limb(string name, Transform parent, Vector3 localPos, Vector3 scale, Material material)
        {
            return MeshPart(name,YokaiMeshLibrary.Frustum("char_limb",8,.48f,.40f,1f),parent,localPos,scale,Quaternion.identity,material);
        }

        GameObject MeshPart(string name, Mesh mesh, Transform parent, Vector3 localPos, Vector3 localScale, Quaternion rotation, Material material)
        {
            return YokaiArtUtility.MeshPart(name,parent,mesh,localPos,localScale,rotation,material,false);
        }

        public void PlayHitReaction()
        {
            hitReaction = 1f;
        }

        void LateUpdate()
        {
            if (GetComponent<YokaiPoseAnimator>() != null) return;
            if (chest == null) return;

            if (combat == null && enemy != null)
            {
                stepPhase += Time.deltaTime * 4.5f;
                float sway = Mathf.Sin(stepPhase) * 8f;
                leftUpperLeg.localRotation = Quaternion.Euler(sway,0f,0f);
                rightUpperLeg.localRotation = Quaternion.Euler(-sway,0f,0f);
                leftUpperArm.localRotation = Quaternion.Euler(-sway*.5f,0f,8f);
                rightUpperArm.localRotation = enemy.IsAttacking
                    ? Quaternion.Euler(-85f + Mathf.Sin(stepPhase*2f)*55f,-25f,-35f)
                    : Quaternion.Euler(sway*.5f,0f,-8f);
                chest.localRotation = Quaternion.Euler(0f,Mathf.Sin(stepPhase*.5f)*3f,0f);

                if (hitReaction > 0f)
                {
                    hitReaction = Mathf.MoveTowards(hitReaction, 0f, Time.deltaTime * 5f);
                    chest.localRotation *= Quaternion.Euler(-14f*hitReaction, 0f, 7f*hitReaction);
                }
                return;
            }

            if (motor == null || combat == null) return;

            float speed = motor.PlanarSpeed;
            stepPhase += Time.deltaTime * (3.0f + speed * 1.25f);
            float locomotion = Mathf.Clamp01(speed / 5f);
            float swing = Mathf.Sin(stepPhase) * 24f * locomotion;

            if (combat.State == YokaiActionState.Free || combat.State == YokaiActionState.Guard)
            {
                leftUpperLeg.localRotation = Quaternion.Euler(swing,0f,0f);
                rightUpperLeg.localRotation = Quaternion.Euler(-swing,0f,0f);
                leftUpperArm.localRotation = Quaternion.Euler(-swing*.55f,0f,5f);
                rightUpperArm.localRotation = Quaternion.Euler(swing*.4f,0f,-5f);
                chest.localRotation = Quaternion.Euler(0f, Mathf.Sin(stepPhase*.5f)*2f, 0f);
            }

            float t = combat.ActionNormalized;
            if (combat.State == YokaiActionState.Light)
            {
                rightUpperArm.localRotation = Quaternion.Euler(Mathf.Lerp(-70f,72f,t), Mathf.Lerp(-45f,35f,t), -42f);
                rightLowerArm.localRotation = Quaternion.Euler(Mathf.Lerp(-15f,-78f,t),0f,0f);
                chest.localRotation = Quaternion.Euler(0f,Mathf.Lerp(-28f,34f,t),0f);
            }
            else if (combat.State == YokaiActionState.Heavy)
            {
                rightUpperArm.localRotation = Quaternion.Euler(Mathf.Lerp(-115f,95f,t),-20f,-30f);
                leftUpperArm.localRotation = Quaternion.Euler(Mathf.Lerp(-75f,35f,t),25f,20f);
                chest.localRotation = Quaternion.Euler(Mathf.Lerp(-9f,12f,t),Mathf.Lerp(-42f,44f,t),0f);
            }
            else if (combat.State == YokaiActionState.Parry)
            {
                rightUpperArm.localRotation = Quaternion.Euler(-58f,-55f,-20f);
                rightLowerArm.localRotation = Quaternion.Euler(-82f,0f,0f);
            }
            else if (combat.State == YokaiActionState.Guard)
            {
                rightUpperArm.localRotation = Quaternion.Euler(-52f,-35f,-18f);
                leftUpperArm.localRotation = Quaternion.Euler(-35f,28f,18f);
            }
            else if (combat.State == YokaiActionState.Art)
            {
                rightUpperArm.localRotation = Quaternion.Euler(-120f + 180f*t,0f,-24f);
                leftUpperArm.localRotation = Quaternion.Euler(-80f + 60f*t,0f,24f);
            }
            else if (combat.State == YokaiActionState.Finisher)
            {
                rightUpperArm.localRotation = Quaternion.Euler(-135f + 230f*t,-15f,-35f);
                chest.localRotation = Quaternion.Euler(0f,-50f + 100f*t,0f);
            }

            if (hitReaction > 0f)
            {
                hitReaction = Mathf.MoveTowards(hitReaction, 0f, Time.deltaTime * 5f);
                chest.localRotation *= Quaternion.Euler(-14f*hitReaction, 0f, 7f*hitReaction);
            }
        }
    }
}
