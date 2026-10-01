#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEngine;
namespace Yokai.EditorTools
{
    public static class YokaiCombatRegression
    {
        [MenuItem("YOKAI/Validate Combat Geometry")]
        public static void Validate()
        {
            Vector3 o = Vector3.zero, f = Vector3.forward;
            Check(YokaiCombatGeometry.InArc(o,f,new Vector3(0,1,2),2.1f,120f), "front target");
            Check(!YokaiCombatGeometry.InArc(o,f,new Vector3(0,1,-1),2.1f,120f), "reject rear target");
            Check(!YokaiCombatGeometry.InArc(o,f,new Vector3(2,0,0),2.1f,120f), "reject flank outside arc");
            Check(!YokaiCombatGeometry.InArc(o,f,new Vector3(0,0,3),2.1f,120f), "reject out of range");
            Check(!YokaiCombatGeometry.InArc(o,f,new Vector3(0,3,1),2.1f,120f), "reject another floor");
            Check(YokaiCombatGeometry.InArc(o,f,o,2.1f,120f), "overlapping target");
            var moves = YokaiCombatTuning.Defaults();
            Check(moves.Length == 5, "five hit chain");
            foreach (var move in moves)
                Check(move.contact < .48f && move.contact > 0f && move.stamina > 0f && move.duration > .1f,
                    "contact precedes default cancel window");
            Check((int)YokaiActionState.Dead == 10 && (int)YokaiActionState.Jump == 11 && (int)YokaiActionState.Landing == 16,
                "stable Animator enum values");
            Check(YokaiAirCombatRules.CanContinueCombo(0) && YokaiAirCombatRules.CanContinueCombo(2), "air hits available");
            Check(!YokaiAirCombatRules.CanContinueCombo(3), "air combo cap");
            Check(YokaiAirCombatRules.CanLaunch(YokaiEnemyArchetype.Grunt, false), "grunt can launch");
            Check(!YokaiAirCombatRules.CanLaunch(YokaiEnemyArchetype.Heavy, true), "heavy immune to launch");
            Check(!YokaiAirCombatRules.CanLaunch(YokaiEnemyArchetype.Boss, true), "boss immune to launch");
            Check(!YokaiAirCombatRules.CanLaunch(YokaiEnemyArchetype.Elite, false), "elite armor blocks launch");
            Check(YokaiAirCombatRules.CanLaunch(YokaiEnemyArchetype.Elite, true), "broken elite can launch");
            Debug.Log("[YOKAI_COMBAT_GEOMETRY_PASS] Direction/range/height and default combo checks passed.");
        }
        static void Check(bool condition, string name)
        {
            if (!condition) throw new InvalidOperationException("Combat regression failed: " + name);
        }
    }
}
#endif
