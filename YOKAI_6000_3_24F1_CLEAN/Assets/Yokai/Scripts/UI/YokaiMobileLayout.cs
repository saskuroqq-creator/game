using UnityEngine;
namespace Yokai
{
    public enum YokaiHudControl { Attack, Heavy, Dodge, Parry, Guard, Jump, Launcher, Skill, Art, Lock, Heal, Pause, Use, Stance, Element, SkillCycle }
    public enum YokaiHudMenu { Resume, Checkpoint, NewGame, Quality, ResetControls, Size, Opacity, Camera, Joystick, AutoSprint }

    // All rectangles use logical units inside the device safe area. Rendering and input
    // share this exact transform, so changing resolution never changes hit alignment.
    public sealed class YokaiMobileLayout
    {
        static readonly YokaiHudControl[] auxiliary={YokaiHudControl.Heavy,YokaiHudControl.Launcher,YokaiHudControl.Parry,
                YokaiHudControl.Art,YokaiHudControl.Stance,YokaiHudControl.Element,YokaiHudControl.SkillCycle};
        public float Scale { get; private set; }
        public float Width { get; private set; }
        public float Height { get; private set; }
        public Vector2 Origin { get; private set; }
        public readonly Rect[] Controls = new Rect[16];
        public readonly Rect[] Menu = new Rect[10];
        public Rect Palette;
        public Rect Status, Objective, Boss, Hint, Finisher, PausePanel, JoystickZone;
        public Vector2 JoystickCenter;
        public float JoystickRadius;
        public Rect SafeGui { get; private set; }
        public void Build(float screenWidth, float screenHeight, Rect safeArea, float controlsScale)
        {
            screenHeightCached = screenHeight;
            if (safeArea.width < 1f || safeArea.height < 1f) safeArea = new Rect(0,0,screenWidth,screenHeight);
            SafeGui = new Rect(safeArea.x, screenHeight - safeArea.yMax, safeArea.width, safeArea.height);
            Scale = Mathf.Max(.01f, Mathf.Min(safeArea.width / 960f, safeArea.height / 540f));
            Width = safeArea.width / Scale;
            Height = safeArea.height / Scale;
            Origin = SafeGui.position;
            float size = Mathf.Clamp(controlsScale,.85f,1.15f);
            float right=Width-20f, bottom=Height-20f;
            Controls[(int)YokaiHudControl.Attack]=new Rect(right-72f*size,bottom-72f*size,72f*size,72f*size);
            Controls[(int)YokaiHudControl.Dodge]=new Rect(right-154f*size,bottom-70f*size,70f*size,70f*size);
            Controls[(int)YokaiHudControl.Guard]=new Rect(right-218f*size,bottom-52f*size,52f*size,52f*size);
            Controls[(int)YokaiHudControl.Jump]=new Rect(right-66f*size,bottom-160f*size,60f*size,60f*size);
            Controls[(int)YokaiHudControl.Skill]=new Rect(right-148f*size,bottom-160f*size,60f*size,60f*size);
            float left=right-218f*size;
            Palette=new Rect(right-270f*size,bottom-334f*size,270f*size,162f*size);
            for(int i=0;i<auxiliary.Length;i++)
                Controls[(int)auxiliary[i]]=new Rect(Palette.x+(8f+(i%3)*84f)*size,
                    Palette.y+(8f+(i/3)*50f)*size,76f*size,42f*size);
            float utility=52f*size;
            Controls[(int)YokaiHudControl.Pause]=new Rect(Width-20f-utility,16f,utility,utility);
            Controls[(int)YokaiHudControl.Heal]=new Rect(Width-28f-utility*2f,16f,utility,utility);
            Controls[(int)YokaiHudControl.Lock]=new Rect(Width-36f-utility*3f,16f,utility,utility);
            Controls[(int)YokaiHudControl.Use] = new Rect(240f,Height-78f,64f,50f);
            Status = new Rect(16f,16f,200f,94f);
            float centerLeft = Status.xMax+18f;
            float centerRight = Controls[(int)YokaiHudControl.Lock].x-18f;
            Objective = new Rect(centerLeft,16f,centerRight-centerLeft,42f);
            Boss = new Rect(centerLeft,66f,centerRight-centerLeft,54f);
            float worldWidth = left-centerLeft-18f;
            Hint = new Rect(centerLeft,Height*.60f,Mathf.Max(100f,worldWidth),54f);
            Finisher = new Rect(centerLeft,Height*.49f,Mathf.Max(100f,worldWidth),30f);
            JoystickRadius = 52f * size;
            JoystickCenter = new Vector2(24f+JoystickRadius+24f,Height-34f-JoystickRadius);
            JoystickZone = new Rect(16f,Height*.46f,Width*.36f-16f,Height*.54f-16f);
            PausePanel = new Rect(Width*.5f-370f,Height*.5f-175f,740f,350f);
            for (int row=0;row<5;row++)
            {
                Menu[row] = new Rect(PausePanel.x+20f,PausePanel.y+58f+row*54f,340f,44f);
                Menu[row+5] = new Rect(PausePanel.x+380f,PausePanel.y+58f+row*54f,340f,44f);
            }
        }
        public static bool IsAuxiliary(YokaiHudControl id)
        {
            return id==YokaiHudControl.Heavy || id==YokaiHudControl.Launcher || id==YokaiHudControl.Parry ||
                id==YokaiHudControl.Art || id==YokaiHudControl.Stance || id==YokaiHudControl.Element || id==YokaiHudControl.SkillCycle;
        }
        void SetCell(YokaiHudControl id,int col,int row,float left,float top,float cell,float diameter)
        {
            Controls[(int)id] = new Rect(left+col*cell+(cell-diameter)*.5f,
                top+row*cell+(cell-diameter)*.5f,diameter,diameter);
        }
        public Vector2 ToLogical(Vector2 screenPosition)
        {
            // ScreenPosition uses Unity bottom-left coordinates.
            return new Vector2((screenPosition.x-Origin.x)/Scale,(screenHeightCached-screenPosition.y-Origin.y)/Scale);
        }
        float screenHeightCached;
        public bool ContainsPointer(Vector2 logical) { return logical.x>=0 && logical.y>=0 && logical.x<=Width && logical.y<=Height; }
    }
}
