using System.Collections.Generic;
using UnityEngine;

namespace Yokai
{
    public sealed class YokaiMobileHUD : MonoBehaviour
    {
        public bool previewMobileInEditor;
        public bool showPerformance;
        Transform player;
        YokaiPlayerController controller;
        YokaiCombat combat;
        YokaiMotor motor;
        YokaiAttributes attributes;
        YokaiLockOn lockOn;
        YokaiHunterArts arts;
        YokaiSkills skills;
        readonly YokaiMobileLayout layout = new YokaiMobileLayout();
        readonly Dictionary<int,YokaiHudControl> buttonFingers = new Dictionary<int,YokaiHudControl>();
        struct Gesture { public Vector2 start; public float began; public bool fired; }
        readonly Dictionary<int,Gesture> gestures=new Dictionary<int,Gesture>();
        bool expandedControls;
        float guardBegan;
        readonly List<int> lostFingers = new List<int>(16);
        readonly Collider[] interactionHits = new Collider[16];
        int joystickFinger = -1, cameraFinger = -1, guardFinger = -1;
        Vector2 joystickStart, joystickNow;
        bool guardInside, canInteract;
        float sprintSince = -1f, nextInteractionScan;
        float controlSize = 1f, opacity = .55f, sensitivity = 1f;
        bool floatingJoystick = true, autoSprint = true;
        Texture2D circle;
        GUIStyle text10, text13, text16, text20, text28, buttonText;
        const string Pref = "Yokai.MobileHUD.";
        static readonly Color Red = new Color(.9f,.2f,.24f);
        static readonly Color Green = new Color(.2f,.85f,.55f);
        static readonly Color Purple = new Color(.67f,.48f,1f);
        static readonly Color Blue = new Color(.35f,.75f,1f);
        static readonly Color Gold = new Color(1f,.72f,.3f);

        public void Bind(Transform p,YokaiPlayerController c,YokaiCombat co,YokaiAttributes a,YokaiLockOn l)
        {
            player=p; controller=c; combat=co; attributes=a; lockOn=l;
            arts=p.GetComponent<YokaiHunterArts>(); skills=p.GetComponent<YokaiSkills>(); motor=p.GetComponent<YokaiMotor>();
            LoadSettings();
        }
        void LoadSettings()
        {
            controlSize=Mathf.Clamp(PlayerPrefs.GetFloat(Pref+"Size",1f),.85f,1.15f);
            opacity=Mathf.Clamp(PlayerPrefs.GetFloat(Pref+"Opacity",.55f),.55f,.95f);
            sensitivity=Mathf.Clamp(PlayerPrefs.GetFloat(Pref+"Sensitivity",1f),.75f,1.25f);
            floatingJoystick=PlayerPrefs.GetInt(Pref+"Floating",1)==1;
            autoSprint=PlayerPrefs.GetInt(Pref+"AutoSprint",1)==1;
        }
        void SaveSettings()
        {
            PlayerPrefs.SetFloat(Pref+"Size",controlSize); PlayerPrefs.SetFloat(Pref+"Opacity",opacity);
            PlayerPrefs.SetFloat(Pref+"Sensitivity",sensitivity); PlayerPrefs.SetInt(Pref+"Floating",floatingJoystick?1:0);
            PlayerPrefs.SetInt(Pref+"AutoSprint",autoSprint?1:0); PlayerPrefs.Save();
            ClearTouches();
        }
        void BuildLayout()
        {
            Vector2 previousOrigin=layout.Origin;
            float previousScale=layout.Scale;
            float previousWidth=layout.Width, previousHeight=layout.Height;
            layout.Build(Screen.width,Screen.height,Screen.safeArea,controlSize);
            if (previousScale>0 && (previousOrigin!=layout.Origin || !Mathf.Approximately(previousScale,layout.Scale) || previousWidth!=layout.Width || previousHeight!=layout.Height)) ClearTouches();
        }
        void Update()
        {
            if(controller==null) return;
            BuildLayout();
            if(Time.unscaledTime>=nextInteractionScan)
            {
                nextInteractionScan=Time.unscaledTime+.25f;
                canInteract=false;
                int n=Physics.OverlapSphereNonAlloc(player.position,2.4f,interactionHits,~0,QueryTriggerInteraction.Collide);
                for(int i=0;i<n;i++) if(interactionHits[i].GetComponentInParent<YokaiShrine>()!=null) { canInteract=true; break; }
            }
            if(!Application.isMobilePlatform) return;
            bool paused=YokaiGameSession.Instance!=null && YokaiGameSession.Instance.Paused;
            if(paused)
            {
                ClearTouches();
                for(int i=0;i<Input.touchCount;i++)
                {
                    Touch t=Input.GetTouch(i);
                    if(t.phase!=TouchPhase.Began) continue;
                    Vector2 p=layout.ToLogical(t.position);
                    for(int j=0;j<layout.Menu.Length;j++)
                        if(layout.Menu[j].Contains(p)) { MenuAction((YokaiHudMenu)j); return; }
                }
                return;
            }
            bool sawJoystick=false, sawCamera=false, sawGuard=false;
            for(int i=0;i<Input.touchCount;i++)
            {
                Touch t=Input.GetTouch(i);
                Vector2 p=layout.ToLogical(t.position);
                bool ended=t.phase==TouchPhase.Ended || t.phase==TouchPhase.Canceled;
                if(t.phase==TouchPhase.Began)
                {
                    buttonFingers.Remove(t.fingerId);
                    gestures.Remove(t.fingerId);
                    if(t.fingerId==joystickFinger)joystickFinger=-1;
                    if(t.fingerId==cameraFinger)cameraFinger=-1;
                    if(t.fingerId==guardFinger)guardFinger=-1;
                }
                if(t.fingerId==joystickFinger)
                {
                    sawJoystick=!ended; joystickNow=p;
                    if(ended) joystickFinger=-1;
                    continue;
                }
                if(t.fingerId==cameraFinger)
                {
                    sawCamera=!ended;
                    if(t.phase==TouchPhase.Moved)
                        controller.AddTouchLook(new Vector2(t.deltaPosition.x,-t.deltaPosition.y)/layout.Scale*.18f*sensitivity);
                    if(ended) cameraFinger=-1;
                    continue;
                }
                if(t.fingerId==guardFinger)
                {
                    sawGuard=!ended;
                    Rect r=layout.Controls[(int)YokaiHudControl.Guard];
                    r.xMin-=8f; r.xMax+=8f; r.yMin-=8f; r.yMax+=8f;
                    guardInside=!ended && r.Contains(p);
                    if(ended)
                    {
                        if(t.phase==TouchPhase.Ended && r.Contains(p) && Time.unscaledTime-guardBegan<.18f)
                        { controller.Guard(false);controller.Parry(); }
                        guardFinger=-1;buttonFingers.Remove(t.fingerId);
                    }
                    continue;
                }
                if(buttonFingers.ContainsKey(t.fingerId))
                {
                    HandleGesture(t.fingerId,t.phase,p);
                    if(ended) { buttonFingers.Remove(t.fingerId);gestures.Remove(t.fingerId); }
                    continue; // A finger that began on a button cannot become camera/guard.
                }
                if(t.phase!=TouchPhase.Began || !layout.ContainsPointer(p)) continue;
                if(expandedControls && !layout.Palette.Contains(p) && !layout.Controls[(int)YokaiHudControl.Skill].Contains(p))expandedControls=false;
                bool consumed=false;
                for(int j=0;j<layout.Controls.Length;j++)
                {
                    if(YokaiMobileLayout.IsAuxiliary((YokaiHudControl)j) && !expandedControls)continue;
                    if(j==(int)YokaiHudControl.Use && !canInteract) continue;
                    if(!layout.Controls[j].Contains(p)) continue;
                    var control=(YokaiHudControl)j;
                    buttonFingers[t.fingerId]=control;
                    if(control==YokaiHudControl.Guard)
                    { guardFinger=t.fingerId;guardInside=true;sawGuard=true;guardBegan=Time.unscaledTime; }
                    else if(control==YokaiHudControl.Attack || control==YokaiHudControl.Skill)
                        gestures[t.fingerId]=new Gesture { start=p,began=Time.unscaledTime,fired=false };
                    else { ControlAction(control);if(YokaiMobileLayout.IsAuxiliary(control) && control!=YokaiHudControl.Stance && control!=YokaiHudControl.Element && control!=YokaiHudControl.SkillCycle)expandedControls=false; }
                    consumed=true;
                    if(control==YokaiHudControl.Pause) { ClearTouches(); return; }
                    break;
                }
                if(consumed) continue;
                if(layout.JoystickZone.Contains(p) && joystickFinger<0)
                {
                    joystickFinger=t.fingerId;
                    float r=layout.JoystickRadius;
                    joystickStart=floatingJoystick ? new Vector2(
                        Mathf.Clamp(p.x,layout.JoystickZone.xMin+r,layout.JoystickZone.xMax-r),
                        Mathf.Clamp(p.y,layout.JoystickZone.yMin+r,Mathf.Min(layout.JoystickZone.yMax-r,layout.Height-r-34f))) : layout.JoystickCenter;
                    joystickNow=p; sawJoystick=true;
                }
                else if(p.x>=layout.Width*.38f && cameraFinger<0)
                { cameraFinger=t.fingerId; sawCamera=true; }
            }
            lostFingers.Clear();
            foreach(int id in buttonFingers.Keys)
            {
                bool found=false;
                for(int i=0;i<Input.touchCount;i++)if(Input.GetTouch(i).fingerId==id){found=true;break;}
                if(!found)lostFingers.Add(id);
            }
            foreach(int id in lostFingers){buttonFingers.Remove(id);gestures.Remove(id);}
            if(!sawJoystick) joystickFinger=-1;
            if(!sawCamera) cameraFinger=-1;
            if(!sawGuard)
            { if(guardFinger>=0) buttonFingers.Remove(guardFinger); guardFinger=-1; guardInside=false; }
            // Handles operating-system cancellation where no Ended event is delivered.
            if(Input.touchCount==0) { ClearTouches(false); return; }
            controller.Guard(guardFinger>=0 && guardInside);
            if(joystickFinger>=0)
            {
                Vector2 v=(joystickNow-joystickStart)/layout.JoystickRadius;
                v.y=-v.y;
                float magnitude=Mathf.Clamp01(v.magnitude);
                float response=magnitude<=.12f?0f:(magnitude-.12f)/.88f;
                controller.SetTouchMove(v.sqrMagnitude>.0001f?v.normalized*response:Vector2.zero);
                if(response>=.96f) { if(sprintSince<0f) sprintSince=Time.unscaledTime; }
                else sprintSince=-1f;
                controller.SetTouchSprint(autoSprint && sprintSince>=0f && Time.unscaledTime-sprintSince>=.2f);
            }
            else { controller.SetTouchMove(Vector2.zero); controller.SetTouchSprint(false); sprintSince=-1f; }
        }
        void HandleGesture(int finger,TouchPhase phase,Vector2 point)
        {
            Gesture g;YokaiHudControl control;
            if(!gestures.TryGetValue(finger,out g) || !buttonFingers.TryGetValue(finger,out control) || g.fired || phase==TouchPhase.Canceled)return;
            Vector2 delta=point-g.start;float held=Time.unscaledTime-g.began;
            if(control==YokaiHudControl.Attack)
            {
                if(delta.y < -28f) { controller.Launcher();g.fired=true; }
                else if(delta.y > 28f) { controller.Heavy();g.fired=true; }
                else if(held>=.28f && delta.magnitude<28f) { controller.Heavy();g.fired=true; }
                else if(phase==TouchPhase.Ended && delta.magnitude<28f) { controller.Attack();g.fired=true; }
            }
            else if(control==YokaiHudControl.Skill)
            {
                if(held>=.32f) { expandedControls=!expandedControls;g.fired=true; }
                else if(phase==TouchPhase.Ended && delta.magnitude<28f) { controller.Skill();expandedControls=false;g.fired=true; }
            }
            gestures[finger]=g;
        }

        void ControlAction(YokaiHudControl c)
        {
            switch(c)
            {
                case YokaiHudControl.Attack:controller.Attack();break;
                case YokaiHudControl.Heavy:controller.Heavy();break;
                case YokaiHudControl.Dodge:controller.Dodge();break;
                case YokaiHudControl.Parry:controller.Parry();break;
                case YokaiHudControl.Jump:controller.Jump();break;
                case YokaiHudControl.Launcher:controller.Launcher();break;
                case YokaiHudControl.Skill:controller.Skill();break;
                case YokaiHudControl.Art:controller.Ability();break;
                case YokaiHudControl.Lock:controller.ToggleLock();break;
                case YokaiHudControl.Heal:controller.Heal();break;
                case YokaiHudControl.Pause:controller.Pause();break;
                case YokaiHudControl.Use:controller.Interact();break;
                case YokaiHudControl.Stance:controller.Stance();break;
                case YokaiHudControl.Element:controller.CycleAbility();break;
                case YokaiHudControl.SkillCycle:controller.CycleSkill();break;
            }
        }
        void MenuAction(YokaiHudMenu action)
        {
            var session=YokaiGameSession.Instance;
            switch(action)
            {
                case YokaiHudMenu.Resume: if(session!=null)session.SetPaused(false);break;
                case YokaiHudMenu.Checkpoint: if(session!=null)session.RestartFromCheckpoint();break;
                case YokaiHudMenu.NewGame: if(session!=null)session.NewGame();break;
                case YokaiHudMenu.Quality: if(YokaiPerformanceManager.Instance!=null)YokaiPerformanceManager.Instance.CycleProfile();break;
                case YokaiHudMenu.Size:controlSize=controlSize<.93f?1f:controlSize<1.08f?1.15f:.85f;SaveSettings();break;
                case YokaiHudMenu.Opacity:opacity=opacity<.65f?.75f:opacity<.85f?.95f:.55f;SaveSettings();break;
                case YokaiHudMenu.Camera:sensitivity=sensitivity<.9f?1f:sensitivity<1.1f?1.25f:.75f;SaveSettings();break;
                case YokaiHudMenu.Joystick:floatingJoystick=!floatingJoystick;SaveSettings();break;
                case YokaiHudMenu.AutoSprint:autoSprint=!autoSprint;SaveSettings();break;
                case YokaiHudMenu.ResetControls:controlSize=1f;opacity=.55f;sensitivity=1f;floatingJoystick=autoSprint=true;SaveSettings();break;
            }
            ClearTouches();
        }
        void ClearTouches(bool closePalette=true)
        {
            joystickFinger=cameraFinger=guardFinger=-1; guardInside=false; sprintSince=-1f; buttonFingers.Clear();gestures.Clear();
            if(closePalette)expandedControls=false;
            if(controller==null)return;
            controller.SetTouchMove(Vector2.zero); controller.SetTouchSprint(false); controller.Guard(false);
        }
        void OnDisable() { ClearTouches(); }
        void OnApplicationFocus(bool focused) { if(!focused)ClearTouches(); }
        void OnApplicationPause(bool paused) { if(paused)ClearTouches(); }
        void OnDestroy() { if(circle!=null)Destroy(circle); }

        void OnGUI()
        {
            if(attributes==null || combat==null)return;
            BuildLayout(); EnsureStyles();
            Matrix4x4 previousMatrix=GUI.matrix; Color previousColor=GUI.color;
            GUI.matrix=Matrix4x4.TRS(new Vector3(layout.Origin.x,layout.Origin.y,0),Quaternion.identity,new Vector3(layout.Scale,layout.Scale,1));
            try
            {
                DrawStatus(); DrawWorldMessages();
                if(Application.isMobilePlatform || previewMobileInEditor) DrawControls();
                else Text(new Rect(276f,layout.Height-92f,layout.Width-300f,72f),
                    "WASD move • Space dodge • Q parry • Alt guard • LMB attack • F heavy\nV jump • T launcher • B skill • N skill select • E art • R element\nC lock • Z/X target • H heal • G shrine • Esc pause",text13,Color.white);
                if(YokaiGameSession.Instance!=null && YokaiGameSession.Instance.Paused) DrawPause();
            }
            finally { GUI.matrix=previousMatrix; GUI.color=previousColor; }
        }
        void DrawStatus()
        {
            Rect r=layout.Status;Fill(r,new Color(.025f,.035f,.055f,.70f));
            Rect hp=new Rect(r.x+10,r.y+6,r.width-20,20);
            Bar(hp,attributes.health/Mathf.Max(1,attributes.maxHealth),new Color(.68f,.1f,.16f));
            Text(hp,"HP "+attributes.health.ToString("0")+" / "+attributes.maxHealth.ToString("0"),text13,Color.white);
            Rect stamina=new Rect(r.x+10,r.y+32,r.width-20,20);
            float fraction=attributes.stamina/Mathf.Max(1,attributes.maxStamina);
            Bar(stamina,fraction,fraction<.2f?new Color(.57f,.36f,.07f):new Color(.1f,.43f,.25f));
            Text(stamina,"СТАМИНА "+attributes.stamina.ToString("0"),text13,Color.white);
            Rect spirit=new Rect(r.x+10,r.y+58,r.width-20,16);
            Bar(spirit,attributes.spirit/Mathf.Max(1,attributes.maxSpirit),new Color(.35f,.22f,.58f));
            Text(spirit,"ДУХ "+attributes.spirit.ToString("0"),text10,Color.white);
            Bar(new Rect(r.x+10,r.y+82,82,4),attributes.posture/Mathf.Max(1,attributes.maxPosture),Gold);
            Bar(new Rect(r.x+108,r.y+82,82,4),attributes.bladeFlow/100f,Blue);
        }
        void DrawWorldMessages()
        {
            var session=YokaiGameSession.Instance;
            if(session!=null)
            {
                Text(layout.Objective,session.ObjectiveText,text13,Color.white);
                var boss=session.director!=null?session.director.boss:null;
                if(boss!=null && boss.CanTarget && Vector3.Distance(player.position,boss.transform.position)<27f)
                {
                    var a=boss.GetComponent<YokaiAttributes>(); Rect r=layout.Boss;
                    Fill(r,new Color(.03f,.025f,.04f,.85f));
                    Text(new Rect(r.x,r.y,r.width,22),"KAGANE • PHASE "+boss.Phase,text13,Gold);
                    Bar(new Rect(r.x+12,r.y+27,r.width-24,12),a.health/Mathf.Max(1,a.maxHealth),Red);
                    Bar(new Rect(r.x+12,r.y+44,r.width-24,4),a.posture/Mathf.Max(1,a.maxPosture),Gold);
                }
                if(session.Victory) Text(new Rect(layout.Width*.3f,layout.Height*.3f,layout.Width*.4f,70f),"YOKAI VANQUISHED",text28,Gold);
                if(session.Stage==YokaiGameStage.Boss && Time.time-session.StageChangedAt<4f)
                    Text(new Rect(layout.Width*.28f,layout.Height*.3f,layout.Width*.44f,60f),"ONI WARDEN KAGANE",text28,Gold);
            }
            if(combat.FinisherReady) Text(layout.Finisher,"ДОБИВАНИЕ → HEAVY",text16,Gold);
            var tutorial=YokaiTutorialDirector.Instance;
            if(tutorial!=null && tutorial.HintAlpha>.01f)
            {
                Fill(layout.Hint,new Color(.025f,.04f,.06f,.75f*tutorial.HintAlpha));
                Text(layout.Hint,tutorial.CurrentHint,text13,new Color(1,1,1,tutorial.HintAlpha));
            }
            if(showPerformance && YokaiPerformanceManager.Instance!=null)
                Text(new Rect(layout.Width*.4f,layout.Height-24f,layout.Width*.2f,20),YokaiPerformanceManager.Instance.SmoothedFps.ToString("0")+" FPS",text13,Color.white);
        }
        void DrawControls()
        {
            bool air=motor!=null && motor.IsAirborne;
            Button(YokaiHudControl.Attack,air?"AIR ATK":"ATK","",Red);
            Button(YokaiHudControl.Dodge,"DODGE","",Blue);
            Button(YokaiHudControl.Guard,"GUARD","",Gold);
            Button(YokaiHudControl.Jump,"JUMP","",Blue);
            Button(YokaiHudControl.Skill,"SKILL",skills!=null && skills.CooldownRemaining>.05f?skills.CooldownRemaining.ToString("0.0"):"",Purple);
            Button(YokaiHudControl.Lock,lockOn!=null && lockOn.IsLocked?"LOCK+":"LOCK","",Blue);
            Button(YokaiHudControl.Heal,"HEAL",combat.HealCharges.ToString(),Green);
            Button(YokaiHudControl.Pause,"II","",Color.white);
            if(canInteract)Chip(YokaiHudControl.Use,"USE");
            if(expandedControls)
            {
                Fill(layout.Palette,new Color(.035f,.055f,.085f,.9f));
                Chip(YokaiHudControl.Heavy,air?"SLAM":"HEAVY");Chip(YokaiHudControl.Launcher,"LAUNCH");
                Chip(YokaiHudControl.Parry,"PARRY");Chip(YokaiHudControl.Art,"ART");
                Chip(YokaiHudControl.Stance,combat.Stance.ToString());Chip(YokaiHudControl.Element,arts!=null?arts.ActiveArt.ToString():"ELEMENT");
                Chip(YokaiHudControl.SkillCycle,skills!=null?(skills.ActiveSkill==YokaiSkill.CrescentDash?"DASH >":skills.ActiveSkill==YokaiSkill.BladeStorm?"STORM >":"WARD >"):"SKILLS");
            }
            Vector2 center=joystickFinger>=0?joystickStart:layout.JoystickCenter;
            float radius=layout.JoystickRadius;
            Disc(new Rect(center.x-radius,center.y-radius,radius*2,radius*2),new Color(.13f,.2f,.29f,opacity*.7f));
            Disc(new Rect(center.x-radius+3,center.y-radius+3,radius*2-6,radius*2-6),new Color(.025f,.045f,.07f,opacity));
            Vector2 offset=joystickFinger>=0?Vector2.ClampMagnitude(joystickNow-joystickStart,radius):Vector2.zero;
            float knob=radius*.45f;
            Disc(new Rect(center.x+offset.x-knob,center.y+offset.y-knob,knob*2,knob*2),new Color(.35f,.55f,.7f,opacity));
            Text(new Rect(center.x-radius,center.y+radius+2,radius*2,18),"",text10,new Color(.75f,.83f,.9f));
        }
        void DrawPause()
        {
            Fill(new Rect(0,0,layout.Width,layout.Height),new Color(.015f,.025f,.04f,.92f));
            Fill(layout.PausePanel,new Color(.045f,.065f,.09f,1f));
            Text(new Rect(layout.PausePanel.x,layout.PausePanel.y+10,layout.PausePanel.width,34),"ПАУЗА • УПРАВЛЕНИЕ",text20,Color.white);
            string[] labels={"ПРОДОЛЖИТЬ","К КОНТРОЛЬНОЙ ТОЧКЕ","НОВАЯ ИГРА",
                "ГРАФИКА: "+(YokaiPerformanceManager.Instance!=null?YokaiPerformanceManager.Instance.ActiveProfile.ToString():"AUTO"),
                "СБРОСИТЬ НАСТРОЙКИ УПРАВЛЕНИЯ","РАЗМЕР КНОПОК: "+(controlSize*100f).ToString("0")+"%",
                "НЕПРОЗРАЧНОСТЬ: "+(opacity*100f).ToString("0")+"%","КАМЕРА: "+(sensitivity*100f).ToString("0")+"%",
                "ДЖОЙСТИК: "+(floatingJoystick?"ПЛАВАЮЩИЙ":"ФИКСИРОВАННЫЙ"),"АВТОБЕГ: "+(autoSprint?"ВКЛ":"ВЫКЛ")};
            for(int i=0;i<labels.Length;i++)
            {
                Fill(layout.Menu[i],new Color(.12f,.18f,.25f,1));
                if(!Application.isMobilePlatform)
                { if(GUI.Button(layout.Menu[i],GUIContent.none))MenuAction((YokaiHudMenu)i); }
                Text(layout.Menu[i],labels[i],text13,Color.white);
            }
        }
        void Button(YokaiHudControl id,string label,string secondary,Color accent)
        {
            Rect r=layout.Controls[(int)id]; bool pressed=id==YokaiHudControl.Guard?guardFinger>=0 && guardInside:buttonFingers.ContainsValue(id);
            Disc(r,new Color(accent.r,accent.g,accent.b,opacity));
            Rect inside=new Rect(r.x+2,r.y+2,r.width-4,r.height-4);
            Disc(inside,pressed?new Color(accent.r*.5f,accent.g*.5f,accent.b*.5f,opacity):new Color(.035f,.05f,.075f,opacity));
            TextFit(new Rect(r.x+r.width*.075f,string.IsNullOrEmpty(secondary)?r.y:r.y+r.height*.23f,r.width*.85f,string.IsNullOrEmpty(secondary)?r.height:r.height*.35f),label,r.width>70?16:r.width<50?10:13,Color.white);
            if(!string.IsNullOrEmpty(secondary))TextFit(new Rect(r.x+r.width*.075f,r.y+r.height*.59f,r.width*.85f,r.height*.22f),secondary,10,accent);
        }
        void Chip(YokaiHudControl id,string label)
        {
            Rect r=layout.Controls[(int)id]; Fill(r,new Color(.045f,.075f,.11f,opacity));
            Text(r,label,text13,buttonFingers.ContainsValue(id)?Gold:Color.white);
        }
        void EnsureStyles()
        {
            if(text13==null)
            {
                text10=MakeStyle(10);text13=MakeStyle(13);text16=MakeStyle(16);text20=MakeStyle(20);text28=MakeStyle(28);buttonText=MakeStyle(13);buttonText.wordWrap=false;
            }
            if(circle!=null)return;
            circle=new Texture2D(96,96,TextureFormat.RGBA32,false);
            circle.name="YokaiHUDCircle"; circle.wrapMode=TextureWrapMode.Clamp; circle.filterMode=FilterMode.Bilinear;
            Color[] pixels=new Color[96*96];
            for(int y=0;y<96;y++)for(int x=0;x<96;x++)
            { float d=Vector2.Distance(new Vector2(x+.5f,y+.5f),new Vector2(48,48));pixels[y*96+x]=new Color(1,1,1,Mathf.Clamp01(48f-d)); }
            circle.SetPixels(pixels);circle.Apply(false,true);
        }
        GUIStyle MakeStyle(int size)
        {
            var s=new GUIStyle(GUI.skin.label);s.fontSize=size;s.fontStyle=FontStyle.Bold;
            s.alignment=TextAnchor.MiddleCenter;s.wordWrap=true;s.padding=new RectOffset(1,1,0,0);s.normal.textColor=Color.white;return s;
        }
        void TextFit(Rect r,string value,int fontSize,Color color)
        {
            buttonText.fontSize=Mathf.Max(8,Mathf.RoundToInt(fontSize*controlSize));
            while(buttonText.fontSize>8 && buttonText.CalcSize(new GUIContent(value)).x>r.width)buttonText.fontSize--;
            Text(r,value,buttonText,color);
        }
        void Text(Rect r,string value,GUIStyle style,Color color)
        { GUI.color=color;GUI.Label(r,value,style);GUI.color=Color.white; }
        void Fill(Rect r,Color color) { GUI.color=color;GUI.DrawTexture(r,Texture2D.whiteTexture);GUI.color=Color.white; }
        void Disc(Rect r,Color color) { GUI.color=color;GUI.DrawTexture(r,circle);GUI.color=Color.white; }
        void Bar(Rect r,float fraction,Color color)
        { Fill(r,new Color(.07f,.09f,.12f,1));Fill(new Rect(r.x,r.y,r.width*Mathf.Clamp01(fraction),r.height),color); }
    }
}
