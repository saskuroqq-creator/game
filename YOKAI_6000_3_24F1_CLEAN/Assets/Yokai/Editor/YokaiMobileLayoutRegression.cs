#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEngine;
namespace Yokai.EditorTools
{
    public static class YokaiMobileLayoutRegression
    {
        [MenuItem("YOKAI/Validate Mobile HUD Layout")]
        public static void Validate()
        {
            Vector2[] sizes={new Vector2(640,360),new Vector2(1280,720),new Vector2(1920,1080),new Vector2(2340,1080),new Vector2(2400,1080),new Vector2(1080,2400)};
            float[] scales={.85f,1f,1.15f};
            int cases=0;
            foreach(var size in sizes)foreach(float controls in scales)foreach(bool notch in new[]{false,true})
            {
                Rect safe=notch?new Rect(40f,24f,size.x-64f,size.y-36f):new Rect(0,0,size.x,size.y);
                var layout=new YokaiMobileLayout();layout.Build(size.x,size.y,safe,controls);
                Rect bounds=new Rect(0,0,layout.Width,layout.Height);
                Check(Inside(bounds,layout.Status),"status bounds");
                Check(Inside(bounds,layout.Objective),"objective bounds");
                Check(!layout.Status.Overlaps(layout.Objective),"status and objective");
                Check(!layout.Status.Overlaps(layout.Boss),"status and boss");
                for(int i=0;i<layout.Controls.Length;i++)
                {
                    Rect r=layout.Controls[i];Check(Inside(bounds,r),"control bounds "+i);
                    for(int j=i+1;j<layout.Controls.Length;j++)Check(!r.Overlaps(layout.Controls[j]),"controls overlap "+i+" / "+j);
                    Check(!r.Overlaps(layout.Status),"control and status "+i);
                    Check(!r.Overlaps(layout.Objective),"control and objective "+i);
                    Check(!r.Overlaps(layout.Boss),"control and boss "+i);
                    Vector2 physicalGui=layout.Origin+r.center*layout.Scale;
                    Vector2 physical=new Vector2(physicalGui.x,size.y-physicalGui.y);
                    Check(Vector2.Distance(layout.ToLogical(physical),r.center)<.01f,"touch/render transform "+i);
                }
                for(int i=0;i<layout.Menu.Length;i++)
                {
                    Check(Inside(layout.PausePanel,layout.Menu[i]),"menu bounds");
                    for(int j=i+1;j<layout.Menu.Length;j++)Check(!layout.Menu[i].Overlaps(layout.Menu[j]),"menu overlap");
                }
                Check(Inside(bounds,layout.PausePanel),"pause panel bounds");
                cases++;
            }
            Debug.Log("[YOKAI_MOBILE_LAYOUT_PASS] "+cases+" safe-area / size / touch-transform cases.");
        }
        static bool Inside(Rect outer,Rect inner)
        {
            const float epsilon=.01f;
            return inner.width>0 && inner.height>0 && inner.xMin>=outer.xMin-epsilon && inner.yMin>=outer.yMin-epsilon &&
                inner.xMax<=outer.xMax+epsilon && inner.yMax<=outer.yMax+epsilon;
        }
        static void Check(bool ok,string message) { if(!ok)throw new InvalidOperationException("Mobile HUD: "+message); }
    }
}
#endif
