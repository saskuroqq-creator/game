extends Node3D

const BODY="res://third_party/vitruvian/godot_project/vitruvian_body.glb"
const HEAD="res://third_party/vitruvian/godot_project/vitruvian_head.glb"
const HAIR="res://third_party/vitruvian/godot_project/vitruvian_hair_rigged.glb"
var player:CharacterBody3D
var camera:Camera3D
var enemies:Array[Dictionary]=[]
var fx:Array[Dictionary]=[]
var shots:Array[Dictionary]=[]
var hp=100.0
var stamina=100.0
var mana=100.0
var combo=0
var combo_t=0.0
var attack_t=0.0
var dash_t=0.0
var parry_t=0.0
var invuln_t=0.0
var hitstop=0.0
var souls=0
var xp=0
var level=1
var skill_points=0
var blade=1
var magic_power=1
var mobility=1
var virtual_dir=Vector2.ZERO
var paused=false
var hpbar:ProgressBar
var stbar:ProgressBar
var mpbar:ProgressBar
var info:Label
var banner:Label
var skills:Label
var weapon:Node3D
var world_time=0.0
var look_input=Vector2.ZERO
var camera_yaw=0.0
var camera_pitch=12.0

func _ready():
    build_world()
    build_player()
    build_ui()
    for i in range(10): spawn_enemy(Vector3(-24+(i%5)*12,0,-14+(i/5)*18),i%4==0)
    spawn_enemy(Vector3(0,0,-34),true)
    say("THE GATE OF TSUKUYOMI",3)

func _process(d):
    world_time+=d
    if hitstop>0: hitstop-=d; return
    if not player:return
    attack_t=maxf(0,attack_t-d); dash_t=maxf(0,dash_t-d); combo_t=maxf(0,combo_t-d)
    parry_t=maxf(0,parry_t-d); invuln_t=maxf(0,invuln_t-d)
    stamina=minf(100,stamina+d*(20+mobility*3)); mana=minf(100,mana+d*(5+magic_power*1.5))
    if combo_t<=0:combo=0
    move_player(d); tick_enemies(d); tick_shots(d); tick_fx(d); tick_camera(d); update_ui()

func make_mat(c:Color,r=.5,e=0.0):
    var m=StandardMaterial3D.new();m.albedo_color=c;m.roughness=r
    if e>0:m.emission_enabled=true;m.emission=c;m.emission_energy_multiplier=e
    return m

func build_world():
    var env=WorldEnvironment.new();var e=Environment.new()
    e.background_mode=Environment.BG_COLOR;e.background_color=Color("#05050a")
    e.ambient_light_source=Environment.AMBIENT_SOURCE_COLOR;e.ambient_light_color=Color("#665174");e.ambient_light_energy=.7
    e.tonemap_mode=Environment.TONE_MAPPER_AGX;e.glow_enabled=true;e.glow_intensity=1.2
    e.volumetric_fog_enabled=true;e.volumetric_fog_density=.012;e.volumetric_fog_albedo=Color("#786d82")
    env.environment=e;add_child(env)
    var sun=DirectionalLight3D.new();sun.rotation_degrees=Vector3(-52,-25,0);sun.light_energy=.9;sun.shadow_enabled=true;sun.light_color=Color("#d8cfe0");add_child(sun)
    var moon=OmniLight3D.new();moon.position=Vector3(0,12,0);moon.omni_range=65;moon.light_color=Color("#725de2");moon.light_energy=9;add_child(moon)
    var ground=StaticBody3D.new();var mi=MeshInstance3D.new();var bm=BoxMesh.new();bm.size=Vector3(100,1,100);mi.mesh=bm;mi.material_override=make_mat(Color("#111116"),.95);ground.add_child(mi)
    var cs=CollisionShape3D.new();var bs=BoxShape3D.new();bs.size=Vector3(100,1,100);cs.shape=bs;ground.add_child(cs);add_child(ground)
    for i in range(36):
        var a=TAU*i/36.0;var r=26+sin(i*2.1)*5;pillar(Vector3(cos(a)*r,0,sin(a)*r),3+float(i%5)*.7)
    for i in range(20):
        var a=TAU*i/20.0;lantern(Vector3(cos(a)*13,0,sin(a)*13))
    for z in [-5.0,-18.0,-31.0]: gate(z)
    for i in range(20): tree(Vector3(-35+(i%10)*7,0,-39+(i/10)*8))
    for i in range(42): rock(Vector3(-42+fmod(i*17.3,84),0,-42+fmod(i*31.7,84)),0.5+fmod(i*1.7,1.8))
    for i in range(28): shrine_prop(Vector3(-40+fmod(i*23.1,80),0,-38+fmod(i*13.7,76)))

func pillar(p:Vector3,h):
    var n=MeshInstance3D.new();var m=CylinderMesh.new();m.top_radius=.65;m.bottom_radius=1;m.height=h;n.mesh=m;n.position=p+Vector3.UP*h/2;n.material_override=make_mat(Color("#28232b"),.9);add_child(n)

func lantern(p):
    var n=MeshInstance3D.new();var m=CylinderMesh.new();m.top_radius=.3;m.bottom_radius=.42;m.height=1.5;n.mesh=m;n.position=p+Vector3.UP*.75;n.material_override=make_mat(Color("#3c252e"),.75);add_child(n)
    var l=OmniLight3D.new();l.position=p+Vector3.UP*1.5;l.light_color=Color("#ff9d5c");l.light_energy=3.5;l.omni_range=7;add_child(l)

func tree(p):
    var n=Node3D.new();n.position=p;add_child(n)
    var t=MeshInstance3D.new();var cm=CylinderMesh.new();cm.top_radius=.18;cm.bottom_radius=.32;cm.height=3.2;t.mesh=cm;t.position.y=1.6;t.material_override=make_mat(Color("#2a211c"),1);n.add_child(t)
    for j in range(3):
        var c=MeshInstance3D.new();var s=SphereMesh.new();s.radius=1.5;s.height=3;c.mesh=s;c.position=Vector3(sin(j*2.1)*.9,3.3,cos(j*2.1)*.9);c.material_override=make_mat(Color("#14251d"),.95);n.add_child(c)

func rock(p:Vector3,s:float):
    var n=MeshInstance3D.new();var m=SphereMesh.new();m.radius=s;m.height=s*1.35;n.mesh=m;n.position=p+Vector3.UP*s*.35;n.scale=Vector3(1.3,.65,.9);n.rotation_degrees=Vector3(fmod(p.x*17,25),fmod(p.z*23,360),fmod(p.x*9,18));n.material_override=make_mat(Color("#29272a"),1);add_child(n)

func shrine_prop(p:Vector3):
    var n=MeshInstance3D.new();var b=BoxMesh.new();b.size=Vector3(1.4,.35,1.4);n.mesh=b;n.position=p+Vector3.UP*.18;n.material_override=make_mat(Color("#3b3027"),.9);add_child(n)
    var l=OmniLight3D.new();l.position=p+Vector3.UP*1.1;l.light_color=Color("#d98755");l.light_energy=.7;l.omni_range=3;add_child(l)

func gate(z):
    for x in [-6.0,6.0]:
        var n=MeshInstance3D.new();var b=BoxMesh.new();b.size=Vector3(1.8,10.5,1.8);n.mesh=b;n.position=Vector3(x,5.25,z);n.material_override=make_mat(Color("#43152f"),.55);add_child(n)
    var t=MeshInstance3D.new();var b=BoxMesh.new();b.size=Vector3(15,2,2.5);t.mesh=b;t.position=Vector3(0,10,z);t.material_override=make_mat(Color("#5a1837"),.4);add_child(t)

func build_player():
    player=CharacterBody3D.new();player.position=Vector3(0,0,18);add_child(player)
    var cs=CollisionShape3D.new();var s=CapsuleShape3D.new();s.radius=.42;s.height=1.8;cs.shape=s;cs.position.y=.9;player.add_child(cs)
    var v=human_visual(false)
    if v:player.add_child(v)
    weapon=katana();player.add_child(weapon)
    camera=Camera3D.new();camera.fov=55;camera.current=true;camera.position=Vector3(0,5.6,9.2);add_child(camera)

func human_visual(enemy):
    # Always build a visible fallback body first. External GLB assets are optional enhancements,
    # so a missing/broken submodule can never make the player or enemies invisible.
    var root=Node3D.new()
    root.name="HumanoidVisual"
    root.position=Vector3(0,0,0)

    var skin=make_mat(Color("#b98268"),.72)
    var skin_dark=make_mat(Color("#744536"),.78)
    var cloth=make_mat(Color("#17151b"),.92)
    var cloth2=make_mat(Color("#29222e"),.82)
    var armor=make_mat(Color("#34313a"),.48)
    var armor2=make_mat(Color("#5a3c35"),.4)
    var metal=make_mat(Color("#9b9aa4"),.2,.08)
    var eye=make_mat(Color("#c59aff"),.12,4.0)

    var scale_factor=1.08 if enemy else 1.0

    var torso=MeshInstance3D.new()
    var torso_mesh=CapsuleMesh.new()
    torso_mesh.radius=.34*scale_factor
    torso_mesh.height=.82*scale_factor
    torso.mesh=torso_mesh
    torso.position=Vector3(0,1.17*scale_factor,0)
    torso.material_override=cloth
    root.add_child(torso)

    var chest=MeshInstance3D.new()
    var chest_mesh=BoxMesh.new()
    chest_mesh.size=Vector3(.66,.55,.30)*scale_factor
    chest.mesh=chest_mesh
    chest.position=Vector3(0,1.30*scale_factor,-.04)
    chest.material_override=armor
    root.add_child(chest)

    var pelvis=MeshInstance3D.new()
    var pelvis_mesh=BoxMesh.new()
    pelvis_mesh.size=Vector3(.55,.34,.28)*scale_factor
    pelvis.mesh=pelvis_mesh
    pelvis.position=Vector3(0,.78*scale_factor,0)
    pelvis.material_override=cloth2
    root.add_child(pelvis)

    var head=MeshInstance3D.new()
    var head_mesh=SphereMesh.new()
    head_mesh.radius=.245*scale_factor
    head_mesh.height=.49*scale_factor
    head.mesh=head_mesh
    head.position=Vector3(0,1.83*scale_factor,0)
    head.material_override=skin
    root.add_child(head)

    var hair=MeshInstance3D.new()
    var hair_mesh=SphereMesh.new()
    hair_mesh.radius=.265*scale_factor
    hair_mesh.height=.40*scale_factor
    hair.mesh=hair_mesh
    hair.position=Vector3(0,1.98*scale_factor,.01)
    hair.scale=Vector3(1.02,.72,1.02)
    hair.material_override=make_mat(Color("#101015"),.72)
    root.add_child(hair)

    for side in [-1.0,1.0]:
        var arm=MeshInstance3D.new()
        var arm_mesh=CylinderMesh.new()
        arm_mesh.top_radius=.105*scale_factor
        arm_mesh.bottom_radius=.125*scale_factor
        arm_mesh.height=.68*scale_factor
        arm.mesh=arm_mesh
        arm.position=Vector3(side*.48*scale_factor,1.17*scale_factor,0)
        arm.rotation_degrees.z=side*8.0
        arm.material_override=cloth2
        root.add_child(arm)

        var hand=MeshInstance3D.new()
        var hand_mesh=SphereMesh.new()
        hand_mesh.radius=.12*scale_factor
        hand_mesh.height=.20*scale_factor
        hand.mesh=hand_mesh
        hand.position=Vector3(side*.53*scale_factor,.78*scale_factor,0)
        hand.material_override=skin_dark
        root.add_child(hand)

        var leg=MeshInstance3D.new()
        var leg_mesh=CylinderMesh.new()
        leg_mesh.top_radius=.13*scale_factor
        leg_mesh.bottom_radius=.105*scale_factor
        leg_mesh.height=.72*scale_factor
        leg.mesh=leg_mesh
        leg.position=Vector3(side*.18*scale_factor,.38*scale_factor,0)
        leg.material_override=cloth
        root.add_child(leg)

        var boot=MeshInstance3D.new()
        var boot_mesh=BoxMesh.new()
        boot_mesh.size=Vector3(.23,.18,.42)*scale_factor
        boot.mesh=boot_mesh
        boot.position=Vector3(side*.18*scale_factor,.10*scale_factor,-.07)
        boot.material_override=armor
        root.add_child(boot)

    # A simple layered samurai cuirass and shoulder guards keeps the fallback readable at gameplay distance.
    for side in [-1.0,1.0]:
        var shoulder=MeshInstance3D.new()
        var sm=SphereMesh.new()
        sm.radius=.17*scale_factor
        sm.height=.22*scale_factor
        shoulder.mesh=sm
        shoulder.position=Vector3(side*.43*scale_factor,1.50*scale_factor,0)
        shoulder.scale=Vector3(1.15,.65,1.0)
        shoulder.material_override=armor2
        root.add_child(shoulder)

    if enemy:
        for x in root.find_children("*","MeshInstance3D",true,false):
            var mi=x as MeshInstance3D
            if mi and mi.material_override is StandardMaterial3D:
                var q=(mi.material_override as StandardMaterial3D).duplicate()
                q.albedo_color=q.albedo_color.lerp(Color("#541b2b"),.24)
                mi.material_override=q
        # Eyes/visor glow makes enemy silhouettes readable in fog and darkness.
        for side in [-1.0,1.0]:
            var eye_mesh=MeshInstance3D.new()
            var em=SphereMesh.new()
            em.radius=.028*scale_factor
            em.height=.056*scale_factor
            eye_mesh.mesh=em
            eye_mesh.position=Vector3(side*.085*scale_factor,1.85*scale_factor,-.225*scale_factor)
            eye_mesh.material_override=eye
            root.add_child(eye_mesh)

    # Try the photoreal Vitruvian assets as an optional layer. If they are absent, malformed,
    # or not imported in an Android build, the procedural humanoid above remains visible.
    if ResourceLoader.exists(BODY):
        var body_scene=load(BODY) as PackedScene
        if body_scene:
            var body=body_scene.instantiate()
            if body:
                body.name="VitruvianBody"
                root.add_child(body)
    if ResourceLoader.exists(HEAD):
        var head_scene=load(HEAD) as PackedScene
        if head_scene:
            var h=head_scene.instantiate()
            if h:
                h.name="VitruvianHead"
                h.position=Vector3(0,1.68,0)
                root.add_child(h)
    if ResourceLoader.exists(HAIR):
        var hair_scene=load(HAIR) as PackedScene
        if hair_scene:
            var h=hair_scene.instantiate()
            if h:
                h.name="VitruvianHair"
                h.position=Vector3(0,1.70,0)
                root.add_child(h)

    return root

func katana():
    var r=Node3D.new();r.position=Vector3(.62,1,-.05);r.rotation_degrees=Vector3(5,-8,-28)
    var b=MeshInstance3D.new();var bm=BoxMesh.new();bm.size=Vector3(.075,1.45,.14);b.mesh=bm;b.position.y=.7;b.material_override=make_mat(Color("#e8eef7"),.12,.2);r.add_child(b)
    var g=MeshInstance3D.new();var gm=TorusMesh.new();gm.inner_radius=.17;gm.outer_radius=.23;g.mesh=gm;g.rotation_degrees.x=90;g.material_override=make_mat(Color("#d7a84d"),.28);r.add_child(g)
    var h=MeshInstance3D.new();var cm=CylinderMesh.new();cm.height=.62;cm.top_radius=.11;cm.bottom_radius=.11;h.mesh=cm;h.position.y=-.28;h.material_override=make_mat(Color("#15151a"),.7);r.add_child(h);return r

func move_player(d):
    if paused:return
    var v=Vector2(Input.get_axis("ui_left","ui_right"),Input.get_axis("ui_up","ui_down"));if virtual_dir.length()>.05:v=virtual_dir
    var dir=Vector3(v.x,0,v.y)
    if dir.length()>.05:
        dir=dir.normalized();player.velocity.x=move_toward(player.velocity.x,dir.x*(8+mobility*.35),d*32);player.velocity.z=move_toward(player.velocity.z,dir.z*(8+mobility*.35),d*32);player.look_at(player.position+dir,Vector3.UP)
    else:
        player.velocity.x=move_toward(player.velocity.x,0,d*26);player.velocity.z=move_toward(player.velocity.z,0,d*26)
    if Input.is_action_just_pressed("dash") and dash_t<=0 and stamina>=20:dash()
    if Input.is_action_just_pressed("attack") and attack_t<=0:melee(false)
    if Input.is_action_just_pressed("heavy") and attack_t<=0:melee(true)
    player.move_and_slide();player.position.x=clampf(player.position.x,-44,44);player.position.z=clampf(player.position.z,-44,44)

func dash():
    stamina-=20;dash_t=.5;invuln_t=.38;player.velocity+=-player.global_transform.basis.z*(18+mobility*1.5);burst(player.position+Vector3.UP,Color("#9c79ff"),18)

func melee(heavy):
    var finisher=combo>=4 and combo_t>0;attack_t=.62 if heavy or finisher else .26;combo+=1;combo_t=1.05
    var r=4.9 if heavy or finisher else 3.1;var dmg=(88+blade*14) if heavy or finisher else (25+blade*5);dmg+=combo*4;if finisher:dmg+=70
    slash(heavy,finisher)
    for e in enemies.duplicate():
        if is_instance_valid(e.n) and player.position.distance_to(e.n.position)<=r:
            var dir=e.n.position-player.position;dir.y=0
            if dir.length()>0.1:damage_enemy(e,dmg,dir.normalized(),heavy or finisher)
    if finisher:say("BLADE ART • MOON-SPLITTER",1)

func damage_enemy(e,dmg,dir,launch=false):
    e.n.set_meta("hp",float(e.n.get_meta("hp"))-dmg);e.n.set_meta("stagger",float(e.n.get_meta("stagger",0))+dmg*.65)
    e.n.velocity+=dir*(7 if launch else 3);e.n.velocity.y=3.5 if launch else 1.2;hitstop=.045;burst(e.n.position+Vector3.UP,Color("#ff4f86"),10)
    if float(e.n.get_meta("hp"))<=0:
        souls+=100 if not e.boss else 1500;gain_xp(80 if not e.boss else 600)
        if e.boss:say("TSUKUYOMI DEFEATED",5)
        e.n.queue_free();enemies.erase(e)

func slash(heavy,finisher=false):
    var n=MeshInstance3D.new();var t=TorusMesh.new();t.inner_radius=2.5 if heavy else 1.5;t.outer_radius=2.62 if heavy else 1.63;n.mesh=t;n.position=player.position+Vector3.UP*.95;n.rotation_degrees.x=90;n.material_override=make_mat(Color("#ffe0ef") if finisher else (Color("#ffb7d5") if heavy else Color("#8f72ff")),.08,1.4);add_child(n);fx.append({"n":n,"t":.28,"v":Vector3.ZERO})

func magic(school):
    if attack_t>0 or mana<18:return
    mana-=18;attack_t=.45;combo=max(combo,1);combo_t=1.4
    var c=Color("#a98cff") if school==0 else (Color("#ff6a3d") if school==1 else Color("#2c183c"))
    var n=MeshInstance3D.new();var s=SphereMesh.new();s.radius=.24;s.height=.48;n.mesh=s;n.position=player.position+Vector3.UP*1.2-player.global_transform.basis.z*1.3;n.material_override=make_mat(c,.08,2.5);add_child(n)
    shots.append({"n":n,"v":-player.global_transform.basis.z*20.0,"t":2.2,"d":70+magic_power*18,"school":school});burst(n.position,c,12)
    say(["MOON ART • LUNAR LANCE","FIRE ART • KAGUTSUCHI","VOID ART • YOMI RIFT"][school],.9)

func tick_shots(d):
    for q in shots.duplicate():
        if not is_instance_valid(q.n):shots.erase(q);continue
        q.t-=d;q.n.position+=q.v*d
        var hit=false
        for e in enemies.duplicate():
            if is_instance_valid(e.n) and q.n.position.distance_to(e.n.position+Vector3.UP)<1.4:
                damage_enemy(e,q.d,q.v.normalized(),q.school==2);burst(q.n.position,Color.WHITE,14);hit=true;break
        if hit or q.t<=0:q.n.queue_free();shots.erase(q)

func spawn_enemy(p,boss=false):
    var n=CharacterBody3D.new();n.position=p;add_child(n)
    var cs=CollisionShape3D.new();var s=CapsuleShape3D.new();s.radius=.5 if not boss else .72;s.height=1.8 if not boss else 2.5;cs.shape=s;cs.position.y=s.height*.5;n.add_child(cs)
    var v=human_visual(true)
    if v:v.scale*=1.08 if boss else .98;n.add_child(v)
    n.set_meta("hp",850.0 if boss else 140.0);n.set_meta("stagger",0.0);n.set_meta("elite",not boss and randf()<.25)
    enemies.append({"n":n,"boss":boss,"a":randf_range(.4,1.5)})

func tick_enemies(d):
    for e in enemies.duplicate():
        if not is_instance_valid(e.n):enemies.erase(e);continue
        var to=player.position-e.n.position;to.y=0;var dist=to.length();var st=maxf(0,float(e.n.get_meta("stagger"))-d*45);e.n.set_meta("stagger",st)
        if st>90:e.n.velocity=Vector3.ZERO
        elif dist>2.6:
            var q=to.normalized();var enr=e.boss and float(e.n.get_meta("hp"))<425;var elite=bool(e.n.get_meta("elite"));var speed=3.8 if enr else (3.0 if e.boss else (2.8 if elite else 2.0))
            e.n.velocity.x=q.x*speed;e.n.velocity.z=q.z*speed;e.n.look_at(e.n.position+q,Vector3.UP)
        else:
            e.n.velocity.x=move_toward(e.n.velocity.x,0,d*12);e.n.velocity.z=move_toward(e.n.velocity.z,0,d*12);e.a-=d
            if e.a<=0 and parry_t<=0:
                e.a=.55 if e.boss and float(e.n.get_meta("hp"))<425 else (1.0 if e.boss else 1.55);take_damage(22 if e.boss else 9)
        e.n.move_and_slide();e.n.position.y=0

func take_damage(a):
    if invuln_t>0:return
    hp-=a;invuln_t=.18;burst(player.position+Vector3.UP,Color("#ff405e"),7)
    if hp<=0:hp=100;stamina=100;mana=100;say("DEFEAT — THE SPIRIT RETURNS",2)

func parry():
    if stamina<12:return
    stamina-=12;parry_t=.42;invuln_t=.42;burst(player.position+Vector3.UP,Color("#ffe48a"),20);say("PERFECT PARRY",.7)
    for e in enemies:
        if is_instance_valid(e.n) and player.position.distance_to(e.n.position)<4: e.n.set_meta("stagger",140.0)

func burst(p,c,count):
    for i in range(count):
        var n=MeshInstance3D.new();var s=SphereMesh.new();s.radius=.05;s.height=.1;n.mesh=s;n.position=p;n.material_override=make_mat(c,.08,1.8);add_child(n);fx.append({"n":n,"t":randf_range(.25,.7),"v":Vector3(randf_range(-1,1),randf_range(.2,1.5),randf_range(-1,1)).normalized()*randf_range(2,7)})

func tick_fx(d):
    for f in fx.duplicate():
        if not is_instance_valid(f.n):fx.erase(f);continue
        f.t-=d;f.n.position+=f.v*d;f.n.scale*=1+d*3
        if f.t<=0:f.n.queue_free();fx.erase(f)

func tick_camera(d):
    var target=player.position+Vector3.UP;camera.position=camera.position.lerp(target+Vector3(0,5.6,9.2),1-exp(-d*7));camera.look_at(target,Vector3.UP)

func build_ui():
    var layer=CanvasLayer.new();add_child(layer)
    var panel=ColorRect.new();panel.position=Vector2(18,18);panel.size=Vector2(500,155);panel.color=Color(.02,.015,.04,.86);layer.add_child(panel)
    var title=Label.new();title.text="YOKAI // SHADOW OF IZANAMI";title.position=Vector2(34,25);title.add_theme_font_size_override("font_size",22);title.add_theme_color_override("font_color",Color("#e8c77d"));layer.add_child(title)
    hpbar=bar(layer,Vector2(34,60),Color("#d83f63"));stbar=bar(layer,Vector2(34,82),Color("#59cfa3"));mpbar=bar(layer,Vector2(34,104),Color("#7668e8"))
    info=Label.new();info.position=Vector2(530,25);info.add_theme_font_size_override("font_size",18);layer.add_child(info)
    skills=Label.new();skills.position=Vector2(34,125);skills.add_theme_font_size_override("font_size",14);layer.add_child(skills)
    var specs=[["ATK",Vector2(940,580),"attack"],["HEAVY",Vector2(1080,620),"heavy"],["DASH",Vector2(1110,520),"dash"],["MOON",Vector2(930,500),"m0"],["FIRE",Vector2(1030,455),"m1"],["VOID",Vector2(1130,455),"m2"],["PARRY",Vector2(790,610),"parry"]]
    for a in specs:
        var b=Button.new();b.text=a[0];b.position=a[1];b.size=Vector2(120,55);layer.add_child(b);b.pressed.connect(func():mobile(a[2]))
    var joy=VirtualJoystick.new()
    joy.name="MovementJoystick";joy.position=Vector2(34,500);joy.size=Vector2(210,210)
    joy.joystick_size=185;joy.tip_size=82;joy.deadzone_ratio=.12;joy.joystick_mode=VirtualJoystick.JOYSTICK_FIXED
    joy.action_up=&"ui_up";joy.action_down=&"ui_down";joy.action_left=&"ui_left";joy.action_right=&"ui_right"
    layer.add_child(joy)
    var look=VirtualJoystick.new()
    look.name="CameraJoystick";look.position=Vector2(1030,500);look.size=Vector2(210,210)
    look.joystick_size=185;look.tip_size=82;look.deadzone_ratio=.14;look.joystick_mode=VirtualJoystick.JOYSTICK_FIXED
    look.action_up=&"ui_page_up";look.action_down=&"ui_page_down";look.action_left=&"ui_home";look.action_right=&"ui_end"
    layer.add_child(look)
    var save=Button.new();save.text="SAVE";save.position=Vector2(20,440);save.size=Vector2(100,48);layer.add_child(save);save.pressed.connect(save_game)
    var load=Button.new();load.text="LOAD";load.position=Vector2(130,440);load.size=Vector2(100,48);layer.add_child(load);load.pressed.connect(load_game)
    var pause=Button.new();pause.text="Ⅱ";pause.position=Vector2(1190,25);pause.size=Vector2(65,55);layer.add_child(pause);pause.pressed.connect(toggle_pause)
    banner=Label.new();banner.position=Vector2(0,235);banner.size=Vector2(1280,80);banner.horizontal_alignment=HORIZONTAL_ALIGNMENT_CENTER;banner.add_theme_font_size_override("font_size",32);banner.add_theme_color_override("font_color",Color("#f0c878"));layer.add_child(banner)

func bar(layer,pos,c):
    var b=ProgressBar.new();b.position=pos;b.size=Vector2(400,14);b.max_value=100;b.show_percentage=false
    var bg=StyleBoxFlat.new();bg.bg_color=Color(.08,.07,.12,.9);var fill=StyleBoxFlat.new();fill.bg_color=c;b.add_theme_stylebox_override("background",bg);b.add_theme_stylebox_override("fill",fill);layer.add_child(b);return b

func mobile(a):
    if a=="attack" and attack_t<=0:melee(false)
    elif a=="heavy" and attack_t<=0:melee(true)
    elif a=="dash" and dash_t<=0 and stamina>=20:dash()
    elif a=="parry":parry()
    elif a.begins_with("m") and attack_t<=0:magic(int(a.substr(1)))

func update_ui():
    hpbar.value=hp;stbar.value=stamina;mpbar.value=mana
    var boss_hp=0
    for e in enemies:
        if e.boss and is_instance_valid(e.n):boss_hp=int(e.n.get_meta("hp"))
    info.text="LV %d  HP %d  ST %d  MP %d  COMBO x%d  SOULS %d  XP %d/%d" %[level,hp,stamina,mana,combo,souls,xp,level*250]
    if boss_hp>0:info.text+="
TSUKUYOMI  %d / 850"%boss_hp
    skills.text="BLADE %d  MAGIC %d  MOBILITY %d  |  SP %d"%[blade,magic_power,mobility,skill_points]

func gain_xp(a):
    xp+=a
    while xp>=level*250:xp-=level*250;level+=1;skill_points+=1;hp=100;stamina=100;mana=100;say("LEVEL UP • SOUL LEVEL %d"%level,2)

func save_game():
    var d={"hp":hp,"stamina":stamina,"mana":mana,"souls":souls,"xp":xp,"level":level,"skill_points":skill_points,"blade":blade,"magic_power":magic_power,"mobility":mobility,"pos":[player.position.x,player.position.y,player.position.z]}
    var f=FileAccess.open("user://yokai_save.json",FileAccess.WRITE);f.store_string(JSON.stringify(d));say("GAME SAVED",1.5)

func load_game():
    if not FileAccess.file_exists("user://yokai_save.json"):say("NO SAVE FOUND",1.5);return
    var f=FileAccess.open("user://yokai_save.json",FileAccess.READ);var d=JSON.parse_string(f.get_as_text())
    if d:
        hp=d.get("hp",100);stamina=d.get("stamina",100);mana=d.get("mana",100);souls=d.get("souls",0);xp=d.get("xp",0);level=d.get("level",1);skill_points=d.get("skill_points",0);blade=d.get("blade",1);magic_power=d.get("magic_power",1);mobility=d.get("mobility",1)
        var p=d.get("pos",[0,0,18]);player.position=Vector3(p[0],p[1],p[2]);say("GAME LOADED",1.5)

func toggle_pause():
    paused=!paused
    if banner:banner.text="PAUSED" if paused else ""

func say(t,sec):
    if banner:banner.text=t;get_tree().create_timer(sec).timeout.connect(func():if is_instance_valid(banner):banner.text="")
