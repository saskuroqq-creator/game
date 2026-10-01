#if UNITY_EDITOR
using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;
namespace Yokai.EditorTools
{
    // Executes the actual CharacterController reaction solver on isolated temporary actors.
    public static class YokaiAirPhysicsRegression
    {
        static readonly MethodInfo tick = typeof(YokaiEnemy).GetMethod("TickReaction", BindingFlags.Instance | BindingFlags.NonPublic);
        static readonly MethodInfo awake = typeof(YokaiEnemy).GetMethod("Awake", BindingFlags.Instance | BindingFlags.NonPublic);
        [MenuItem("YOKAI/Validate Air Physics")]
        public static void Validate()
        {
            if (Application.isPlaying) throw new InvalidOperationException("Run air regression in Edit Mode.");
            foreach (int fps in new[] { 30, 60, 120 }) Flight(fps, false, false);
            Flight(30, true, false);
            Flight(60, false, true);
            Debug.Log("[YOKAI_AIR_PHYSICS_PASS] Actual controller launch, gravity, landing, ceiling, dive, armor and reset checks passed.");
        }
        static void Flight(int fps, bool dive, bool ceiling)
        {
            GameObject actor = null, floor = null, roof = null;
            Vector3 origin = new Vector3(18000f, 0f, 18000f);
            try
            {
                floor = new GameObject("AirQA_Floor"); floor.hideFlags = HideFlags.HideAndDontSave;
                floor.transform.position = origin + Vector3.down * .5f;
                floor.AddComponent<BoxCollider>().size = new Vector3(30f, 1f, 30f);
                if (ceiling)
                {
                    roof = new GameObject("AirQA_Roof"); roof.hideFlags = HideFlags.HideAndDontSave;
                    roof.transform.position = origin + Vector3.up * 2.8f;
                    roof.AddComponent<BoxCollider>().size = new Vector3(30f, .3f, 30f);
                }
                actor = new GameObject("AirQA_Enemy"); actor.hideFlags = HideFlags.HideAndDontSave;
                actor.transform.position = origin + Vector3.up * .03f;
                var controller = actor.AddComponent<CharacterController>();
                controller.height = 1.8f; controller.center = Vector3.up * .9f;
                controller.radius = .3f; controller.skinWidth = .02f; controller.minMoveDistance = 0f;
                var enemy = actor.AddComponent<YokaiEnemy>();
                if (actor.transform.Find("AimPoint") == null) awake.Invoke(enemy, null);
                Physics.SyncTransforms();
                enemy.archetype = YokaiEnemyArchetype.Heavy;
                Check(!enemy.TryLaunch(9.5f), "heavy armor");
                enemy.archetype = YokaiEnemyArchetype.Boss;
                Check(!enemy.TryLaunch(9.5f), "boss armor");
                enemy.archetype = YokaiEnemyArchetype.Grunt;
                Check(enemy.TryLaunch(9.5f, Vector3.forward), "launch accepted");
                Check(!enemy.TryLaunch(9.5f), "cannot relaunch midair");
                float highest = 0f;
                for (int frame = 0; frame < fps * 3; frame++)
                {
                    if (dive && frame == fps / 4)
                    { Check(enemy.SlamDown(Vector3.forward), "dive accepted"); enemy.HoldAir(5f); Check(enemy.VerticalSpeed < -20f, "hold cannot cancel dive"); }
                    tick.Invoke(enemy, new object[] { 1f / fps });
                    highest = Mathf.Max(highest, actor.transform.position.y - origin.y);
                    if (!enemy.IsAirborne) break;
                }
                Check(!enemy.IsAirborne, "landed within three seconds");
                Check(enemy.IsLanding, "landing recovery");
                Check(actor.transform.position.y >= origin.y - .08f, "floor stops descent");
                if (ceiling) Check(highest < 1.1f, "roof stops ascent");
                else Check(highest > .6f, "visible ascent");
                Check(!enemy.SlamDown(Vector3.forward), "grounded dive rejected");
                enemy.ResetEnemy();
                Check(!enemy.IsAirborne && !enemy.IsLanding, "reset clears reactions");
                Check(Vector3.Distance(actor.transform.position, origin + Vector3.up * .03f) < .05f, "reset position");
            }
            finally
            {
                if (actor != null) UnityEngine.Object.DestroyImmediate(actor);
                if (floor != null) UnityEngine.Object.DestroyImmediate(floor);
                if (roof != null) UnityEngine.Object.DestroyImmediate(roof);
            }
        }
        static void Check(bool pass, string name)
        { if (!pass) throw new InvalidOperationException("Air physics regression: " + name); }
    }
}
#endif
