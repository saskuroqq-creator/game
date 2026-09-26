extends Node

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
var camera_shake=0.0
var enemy_shots:Array[Dictionary]=[]
var wave=1
var wave_timer=4.0
var defeated=0
var boss_phase_announced:Dictionary={}
var map_zone="MOONLIT VILLAGE"
var zone_hint=""
var discovered_zones:Dictionary={"MOONLIT VILLAGE":true}
var soul_caches:Array[Dictionary]=[]
var caches_collected=0
var mini_defeated=0
var quest_stage=0
var quest_label:Label
var shrine_nodes:Array[Node3D]=[]
var shrine_cooldown=0.0
var autosave_t=30.0

func _ready():
    build_main_menu()

func build_main_menu():
    var layer=CanvasLayer.new()
    add_child(layer)
    var bg=ColorRect.new()
    bg.set_anchors_and_offsets_preset(Control.PRESET_FULL_RECT)
    bg.color=Color("#05030a")
    layer.add_child(bg)

    var title=Label.new()
    title.text="YOKAI"
    title.horizontal_alignment=HORIZONTAL_ALIGNMENT_CENTER
    title.set_anchors_preset(Control.PRESET_CENTER_TOP)
    title.position=Vector2(-360,90)
    title.size=Vector2(720,90)
    title.add_theme_font_size_override("font_size",64)
    title.add_theme_color_override("font_color",Color("#e8c77d"))
    layer.add_child(title)

    var subtitle=Label.new()
    subtitle.text="SHADOW OF IZANAMI"
    subtitle.horizontal_alignment=HORIZONTAL_ALIGNMENT_CENTER
    subtitle.set_anchors_preset(Control.PRESET_CENTER_TOP)
    subtitle.position=Vector2(-360,175)
    subtitle.size=Vector2(720,50)
    subtitle.add_theme_font_size_override("font_size",24)
    subtitle.add_theme_color_override("font_color",Color("#b9a8d8"))
    layer.add_child(subtitle)

    var start=Button.new()
    start.text="START GAME"
    start.set_anchors_preset(Control.PRESET_CENTER)
    start.position=Vector2(-170,15)
    start.size=Vector2(340,72)
    start.add_theme_font_size_override("font_size",26)
    layer.add_child(start)
    start.pressed.connect(func():
        layer.queue_free()
        _start_alpha()
    )

    var note=Label.new()
    note.text="ANDROID ALPHA • SAFE START"
    note.horizontal_alignment=HORIZONTAL_ALIGNMENT_CENTER
    note.set_anchors_preset(Control.PRESET_CENTER_BOTTOM)
    note.position=Vector2(-300,-85)
    note.size=Vector2(600,40)
    note.add_theme_font_size_override("font_size",14)
    note.add_theme_color_override("font_color",Color("#756b82"))
    layer.add_child(note)

func _start_alpha():
    # Safe boot: keep the first frame lightweight and postpone all heavy world work.
    build_ui()
    say("YOKAI • SAFE BOOT",2.0)
    await get_tree().process_frame
    build_world()
    await get_tree().process_frame
    build_player()
    await get_tree().process_frame
    call_deferred("_finish_alpha_boot")

func _finish_alpha_boot():
    await get_tree().process_frame
    build_world_decorations()
    await get_tree().process_frame
    build_open_world_zones()
    await get_tree().process_frame
    build_soul_caches()
    await get_tree().process_frame
    build_shrines()
    await get_tree().process_frame
    # Keep the first combat frame small; waves add enemies later.
    for i in range(5):
        spawn_enemy(Vector3(-18+(i%3)*12,0,-10+(i/3)*14),i==4)
        await get_tree().process_frame
    spawn_enemy(Vector3(-62,0,-58),false,"ONI GUARDIAN")
    await get_tree().process_frame
    spawn_enemy(Vector3(64,0,-48),false,"KITSUNE WARDEN")
    await get_tree().process_frame
    spawn_enemy(Vector3(58,0,58),false,"MOURNING SAMURAI")
    say("YOKAI ALPHA 0.2 • READY",2.0)

func _process(d):
    world_time+=d
    if hitstop>0: hitstop-=d; return
    if not player:return
    attack_t=maxf(0,attack_t-d); dash_t=maxf(0,dash_t-d); combo_t=maxf(0,combo_t-d)
    parry_t=maxf(0,parry_t-d); invuln_t=maxf(0,invuln_t-d)
    stamina=minf(100,stamina+d*(20+mobility*3)); mana=minf(100,mana+d*(5+magic_power*1.5))
    wave_timer-=d
    autosave_t-=d
    if autosave_t<=0:
        autosave_t=30.0
        save_game(true)
    tick_soul_caches()
    tick_shrines(d)
    update_quest()
    if wave_timer<=0 and not paused:
        wave_timer=5.5
        var living=0
        var boss_alive=false
        for e in enemies:
            if is_instance_valid(e.n):
                living+=1
                boss_alive = boss_alive or e.boss
        if living<6 and defeated<80:
            wave+=1
            var spawn_count=min(4,2+int(wave/5))
            for i in range(spawn_count):
                var ang=randf_range(0.0,TAU)
                var dist=randf_range(12.0,22.0)
                spawn_enemy(player.position+Vector3(cos(ang)*dist,0,sin(ang)*dist),false)
            if wave%5==0 and not boss_alive:
                spawn_enemy(player.position+Vector3(0,0,-18),true)
                say("YOMI WAVE %d • BOSS RISING"%wave,2.2)
            elif wave%5==0:
                say("YOMI WAVE %d"%wave,1.5)
    if combo_t<=0:combo=0
    move_player(d); tick_enemies(d); tick_shots(d); tick_enemy_shots(d); tick_fx(d); tick_camera(d); update_zone(); update_ui()

func make_mat(c:Color,r=.5,e=0.0):
    var m=StandardMaterial3D.new();m.albedo_color=c;m.roughness=r
    if e>0:m.emission_enabled=true;m.emission=c;m.emission_energy_multiplier=e
    return m

func build_world():
    # Compatibility-safe base scene: no shadowed/dynamic lights during startup.
    var env=WorldEnvironment.new()
    var e=Environment.new()
    e.background_mode=Environment.BG_COLOR
    e.background_color=Color("#020207")
    e.ambient_light_source=Environment.AMBIENT_SOURCE_COLOR
    e.ambient_light_color=Color("#51416f")
    e.ambient_light_energy=.62
    e.tonemap_mode=Environment.TONE_MAPPER_LINEAR
    e.glow_enabled=false
    e.volumetric_fog_enabled=false
    e.fog_enabled=false
    env.environment=e
    add_child(env)

    var moon_mesh=MeshInstance3D.new()
    var moon_sphere=SphereMesh.new()
    moon_sphere.radius=4.6
    moon_sphere.height=9.2
    moon_mesh.mesh=moon_sphere
    moon_mesh.position=Vector3(-10,16,-48)
    moon_mesh.material_override=make_mat(Color("#b34b6a"),.22,2.0)
    add_child(moon_mesh)

    var sun=DirectionalLight3D.new()
    sun.rotation_degrees=Vector3(-52,-25,0)
    sun.light_energy=.75
    sun.shadow_enabled=false
    sun.light_color=Color("#d8cfe0")
    add_child(sun)

    var ground=StaticBody3D.new()
    var mi=MeshInstance3D.new()
    var bm=BoxMesh.new()
    bm.size=Vector3(240,1,240)
    mi.mesh=bm
    mi.material_override=make_mat(Color("#111116"),.95)
    ground.add_child(mi)
    var cs=CollisionShape3D.new()
    var bs=BoxShape3D.new()
    bs.size=Vector3(240,1,240)
    cs.shape=bs
    ground.add_child(cs)
    add_child(ground)

func build_world_decorations():
    # Bounded geometry keeps Android startup and frame time predictable.
    for i in range(24):
        var a=TAU*i/24.0
        var r=48+sin(i*2.1)*6
        pillar(Vector3(cos(a)*r,0,sin(a)*r),3+float(i%4)*.6)
        if i%2==0:
            await get_tree().process_frame
    for i in range(8):
        var a=TAU*i/8.0
        lantern(Vector3(cos(a)*13,0,sin(a)*13))
    for z in [-12.0,-30.0]:
        gate(z)
    for i in range(10):
        tree(Vector3(-28+(i%5)*7,0,-34+(i/5)*8))
        if i%3==0:
            await get_tree().process_frame
    for i in range(18):
        rock(Vector3(-38+fmod(i*17.3,76),0,-38+fmod(i*31.7,76)),0.5+fmod(i*1.7,1.4))
        if i%4==0:
            await get_tree().process_frame
    for i in range(8):
        shrine_prop(Vector3(-34+fmod(i*23.1,68),0,-34+fmod(i*13.7,68)))
    cathedral_ruin(Vector3(0,0,-40))
    stone_arch(Vector3(-27,0,-20),1.0)
    for i in range(10):
        grave_cluster(Vector3(-30+fmod(i*11.7,60),0,-34+fmod(i*17.1,64)),i%3==0)
        if i%4==0:
            await get_tree().process_frame

func build_region_geometry():
    for p in [Vector3(-62,0,-58),Vector3(64,0,-48),Vector3(58,0,58),Vector3(0,0,-92)]:
        for j in range(4):
            var a=TAU*j/8.0
            rock(p+Vector3(cos(a)*10,0,sin(a)*10),1.1+float(j%3)*.45)
    for x in [-78.0,-46.0]:
        for z in [-78.0,-38.0]: tree(Vector3(x,0,z))
    for x in [42.0,76.0]:
        for z in [-66.0,-30.0]: lantern(Vector3(x,0,z))
    for i in range(5): grave_cluster(Vector3(42+fmod(i*9.3,34),0,42+fmod(i*13.7,34)),i%2==0)
    stone_arch(Vector3(0,0,-78),1.35)
    stone_arch(Vector3(36,0,-48),1.25)
    stone_arch(Vector3(42,0,58),1.3)

func build_soul_caches():
    var points=[Vector3(-38,0,-48),Vector3(-78,0,-70),Vector3(-48,0,-42),Vector3(48,0,-62),Vector3(78,0,-34),Vector3(46,0,46),Vector3(72,0,72),Vector3(-18,0,-86)]
    for i in range(points.size()):
        var p=points[i]
        var root=Node3D.new();root.position=p;root.name="SoulCache_%d"%i;add_child(root)
        var base=MeshInstance3D.new();var bm=BoxMesh.new();bm.size=Vector3(1.15,.42,.9);base.mesh=bm;base.position.y=.22;base.material_override=make_mat(Color("#211b2c"),.72);root.add_child(base)
        var orb=MeshInstance3D.new();var sm=SphereMesh.new();sm.radius=.25;sm.height=.5;orb.mesh=sm;orb.position.y=.82;orb.material_override=make_mat(Color("#a98cff"),.08,4.0);root.add_child(orb)
        soul_caches.append({"n":root,"value":180+i*40,"taken":false})

func tick_soul_caches():
    if not player:return
    for cache in soul_caches:
        if bool(cache.taken) or not is_instance_valid(cache.n):continue
        if player.position.distance_to(cache.n.position)<2.2:
            cache.taken=true;souls+=int(cache.value);caches_collected+=1
            burst(cache.n.position+Vector3.UP,Color("#c9b7ff"),16)
            impact_ring(cache.n.position+Vector3.UP*.2,1.0,Color("#a98cff"))
            say("SOUL CACHE +%d"%int(cache.value),1.2)
            cache.n.visible=false

func build_shrines():
    var points=[Vector3(0,22,0),Vector3(-62,0,-58),Vector3(64,0,-48),Vector3(58,0,58),Vector3(0,0,-92)]
    for i in range(points.size()):
        var root=Node3D.new();root.name="Shrine_%d"%i;root.position=points[i];add_child(root)
        var base=MeshInstance3D.new();var bm=CylinderMesh.new();bm.top_radius=.75;bm.bottom_radius=1.0;bm.height=.35;base.mesh=bm;base.position.y=.18;base.material_override=make_mat(Color("#30283d"),.78);root.add_child(base)
        var orb=MeshInstance3D.new();var sm=SphereMesh.new();sm.radius=.22;sm.height=.44;orb.mesh=sm;orb.position.y=1.15;orb.material_override=make_mat(Color("#d7b7ff"),.08,4.5);root.add_child(orb)
        var tag=Label3D.new();tag.text="SHRINE";tag.position=Vector3(0,1.9,0);tag.font_size=22;tag.modulate=Color("#c9b7ff");tag.outline_size=6;root.add_child(tag)
        shrine_nodes.append(root)

func tick_shrines(d):
    shrine_cooldown=maxf(0,shrine_cooldown-d)

func rest_at_shrine():
    if shrine_cooldown>0 or not player:return
    for shrine in shrine_nodes:
        if is_instance_valid(shrine) and player.position.distance_to(shrine.position)<3.0:
            hp=100;stamina=100;mana=100;invuln_t=.8;shrine_cooldown=2.0
            burst(shrine.position+Vector3.UP,Color("#d7b7ff"),18)
            impact_ring(shrine.position+Vector3.UP*.2,1.5,Color("#9c79ff"))
            say("SHRINE RESTORED • HP / STAMINA / MANA",1.6)
            return
    say("APPROACH A SHRINE",.8)

func update_quest():
    if quest_stage==0 and caches_collected>=3:
        quest_stage=1;say("QUEST UPDATED • HUNT THE YOMI",1.6)
    elif quest_stage==1 and defeated>=10:
        quest_stage=2;say("QUEST UPDATED • DEFEAT A GUARDIAN",1.6)
    elif quest_stage==2 and mini_defeated>=1:
        quest_stage=3;say("QUEST UPDATED • ENTER YOMI GATE",1.6)
    elif quest_stage==3 and map_zone=="YOMI GATE":
        quest_stage=4;souls+=1000;skill_points+=2;say("QUEST COMPLETE • GATE OF YOMI OPENED",3.0)

func quest_text():
    match quest_stage:
        0:return "QUEST 01 • COLLECT SOUL CACHES  %d/3"%min(caches_collected,3)
        1:return "QUEST 02 • PURGE YOMI  %d/10"%min(defeated,10)
        2:return "QUEST 03 • DEFEAT A GUARDIAN  %d/1"%min(mini_defeated,1)
        3:return "QUEST 04 • REACH YOMI GATE"
        _:return "QUEST COMPLETE • YOMI GATE AWAKENED"

func build_open_world_zones():
    world_zone("MOONLIT VILLAGE",Vector3(0,0,22),Color("#8e7ad6"),14)
    world_zone("WISTERIA FOREST",Vector3(-62,0,-58),Color("#6fa889"),18)
    world_zone("KITSUNE VALLEY",Vector3(64,0,-48),Color("#d48a5b"),17)
    world_zone("ASHEN BATTLEFIELD",Vector3(58,0,58),Color("#b85a62"),19)
    world_zone("YOMI GATE",Vector3(0,0,-92),Color("#9c79ff"),22)

func world_zone(label:String,p:Vector3,c:Color,radius:float):
    var root=Node3D.new();root.position=p;add_child(root)
    var shrine=MeshInstance3D.new();var sm=CylinderMesh.new();sm.top_radius=1.2;sm.bottom_radius=1.6;sm.height=2.2;shrine.mesh=sm;shrine.position.y=1.1;shrine.material_override=make_mat(c,.7,.8);root.add_child(shrine)
    var ring=MeshInstance3D.new();var tm=TorusMesh.new();tm.inner_radius=radius*.72;tm.outer_radius=radius*.75;ring.mesh=tm;ring.position.y=.05;ring.material_override=make_mat(c,.9,.18);root.add_child(ring)
    var tag=Label3D.new();tag.text=label;tag.position=Vector3(0,4.2,0);tag.font_size=30;tag.modulate=c;tag.outline_size=8;root.add_child(tag)

func cathedral_ruin(p:Vector3):
    var root=Node3D.new();root.position=p;add_child(root)
    for x in [-7.0,-3.5,3.5,7.0]:
        var col=MeshInstance3D.new();var cm=CylinderMesh.new();cm.top_radius=.62;cm.bottom_radius=.9;cm.height=7.0+abs(x)*.18;col.mesh=cm;col.position=Vector3(x,3.5,0);col.material_override=make_mat(Color("#29262d"),.92);root.add_child(col)
    var wall=MeshInstance3D.new();var wm=BoxMesh.new();wm.size=Vector3(16,6.5,1.1);wall.mesh=wm;wall.position=Vector3(0,3.2,1.8);wall.material_override=make_mat(Color("#211f25"),.98);root.add_child(wall)
    for x in [-5.0,0.0,5.0]:
        var arch=MeshInstance3D.new();var am=TorusMesh.new();am.inner_radius=1.25;am.outer_radius=1.55;arch.mesh=am;arch.rotation_degrees.x=90;arch.position=Vector3(x,3.2,1.22);arch.scale=Vector3(1,1.25,1);arch.material_override=make_mat(Color("#403a44"),.86);root.add_child(arch)
    var spire=MeshInstance3D.new();var sm=CylinderMesh.new();sm.top_radius=0;sm.bottom_radius=2.1;sm.height=9;spire.mesh=sm;spire.position=Vector3(0,7.7,0);spire.material_override=make_mat(Color("#19171c"),.94);root.add_child(spire)

func stone_arch(p:Vector3,scale_factor:float):
    var root=Node3D.new();root.position=p;root.scale*=scale_factor;add_child(root)
    for x in [-2.8,2.8]:
        var post=MeshInstance3D.new();var bm=BoxMesh.new();bm.size=Vector3(1.2,5.8,1.2);post.mesh=bm;post.position=Vector3(x,2.9,0);post.material_override=make_mat(Color("#35313a"),.96);root.add_child(post)
    var beam=MeshInstance3D.new();var bm2=BoxMesh.new();bm2.size=Vector3(7.4,1.25,1.25);beam.mesh=bm2;beam.position=Vector3(0,5.8,0);beam.material_override=make_mat(Color("#3c3740"),.92);root.add_child(beam)

func grave_cluster(p:Vector3,large:bool):
    var n=MeshInstance3D.new();var bm=BoxMesh.new();bm.size=Vector3(.55,.9,.18) if not large else Vector3(.8,1.4,.22);n.mesh=bm;n.position=p+Vector3.UP*(bm.size.y*.5);n.rotation_degrees=Vector3(0,fmod(p.x*17+p.z*9,360),fmod(p.z*4,9)-4);n.material_override=make_mat(Color("#343039"),.95);add_child(n)
\nfunc pillar(p:Vector3,h):
    var n=MeshInstance3D.new();var m=CylinderMesh.new();m.top_radius=.65;m.bottom_radius=1;m.height=h;n.mesh=m;n.position=p+Vector3.UP*h/2;n.material_override=make_mat(Color("#28232b"),.9);add_child(n)

func lantern(p):
    var n=MeshInstance3D.new();var m=CylinderMesh.new();m.top_radius=.3;m.bottom_radius=.42;m.height=1.5;n.mesh=m;n.position=p+Vector3.UP*.75;n.material_override=make_mat(Color("#3c252e"),.75);add_child(n)

func tree(p):
    var n=Node3D.new();n.position=p;add_child(n)
    var t=MeshInstance3D.new();var cm=CylinderMesh.new();cm.top_radius=.18;cm.bottom_radius=.32;cm.height=3.2;t.mesh=cm;t.position.y=1.6;t.material_override=make_mat(Color("#2a211c"),1);n.add_child(t)
    for j in range(3):
        var c=MeshInstance3D.new();var s=SphereMesh.new();s.radius=1.5;s.height=3;c.mesh=s;c.position=Vector3(sin(j*2.1)*.9,3.3,cos(j*2.1)*.9);c.material_override=make_mat(Color("#14251d"),.95);n.add_child(c)

func rock(p:Vector3,s:float):
    var n=MeshInstance3D.new();var m=SphereMesh.new();m.radius=s;m.height=s*1.35;n.mesh=m;n.position=p+Vector3.UP*s*.35;n.scale=Vector3(1.3,.65,.9);n.rotation_degrees=Vector3(fmod(p.x*17,25),fmod(p.z*23,360),fmod(p.x*9,18));n.material_override=make_mat(Color("#29272a"),1);add_child(n)

func shrine_prop(p:Vector3):
    var n=MeshInstance3D.new();var b=BoxMesh.new();b.size=Vector3(1.4,.35,1.4);n.mesh=b;n.position=p+Vector3.UP*.18;n.material_override=make_mat(Color("#3b3027"),.9);add_child(n)

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
    camera=Camera3D.new();camera.fov=52;camera.current=true;camera.position=Vector3(0,4.9,7.4);add_child(camera)

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


    # Layered dark-fantasy samurai armor: worn plates, sash, cloak and a restrained helm silhouette.
    var belt=MeshInstance3D.new();var belt_mesh=BoxMesh.new();belt_mesh.size=Vector3(.62,.10,.34)*scale_factor;belt.mesh=belt_mesh;belt.position=Vector3(0,.91*scale_factor,-.02);belt.material_override=armor2;root.add_child(belt)
    var cloak=MeshInstance3D.new();var cloak_mesh=BoxMesh.new();cloak_mesh.size=Vector3(.72,.95,.10)*scale_factor;cloak.mesh=cloak_mesh;cloak.position=Vector3(0,1.08*scale_factor,.18);cloak.rotation_degrees.x=4;cloak.material_override=make_mat(Color("#0d0c11"),1);root.add_child(cloak)
    var helm=MeshInstance3D.new();var hm=SphereMesh.new();hm.radius=.28*scale_factor;hm.height=.34*scale_factor;helm.mesh=hm;helm.position=Vector3(0,2.02*scale_factor,.01);helm.scale=Vector3(1.02,.72,1.02);helm.material_override=armor;root.add_child(helm)
    var crest=MeshInstance3D.new();var cm=CylinderMesh.new();cm.top_radius=.025;cm.bottom_radius=.09;cm.height=.38*scale_factor;crest.mesh=cm;crest.position=Vector3(0,2.29*scale_factor,.01);crest.material_override=armor2;root.add_child(crest)
\n    # A simple layered samurai cuirass and shoulder guards keeps the fallback readable at gameplay distance.
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

    var coat_tail=MeshInstance3D.new();var ctm=BoxMesh.new();ctm.size=Vector3(.78,1.05,.16)*scale_factor;coat_tail.mesh=ctm;coat_tail.position=Vector3(0,.83*scale_factor,.30*scale_factor);coat_tail.rotation_degrees.x=-8;coat_tail.material_override=make_mat(Color("#09080e"),1);root.add_child(coat_tail)
    var sash=MeshInstance3D.new();var sm2=BoxMesh.new();sm2.size=Vector3(.76,.12,.40)*scale_factor;sash.mesh=sm2;sash.position=Vector3(0,1.00*scale_factor,-.06);sash.material_override=make_mat(Color("#5a193c"),.58,1.0);root.add_child(sash)
    var mask=MeshInstance3D.new();var mm=SphereMesh.new();mm.radius=.18*scale_factor;mm.height=.24*scale_factor;mask.mesh=mm;mask.position=Vector3(0,1.84*scale_factor,-.205*scale_factor);mask.scale=Vector3(1.35,.72,.38);mask.material_override=make_mat(Color("#221822"),.38,.7);root.add_child(mask)
    if not enemy:
        for side in [-1.0,1.0]:
            var horn=MeshInstance3D.new();var hm2=CylinderMesh.new();hm2.top_radius=.015;hm2.bottom_radius=.055;hm2.height=.34;horn.mesh=hm2;horn.position=Vector3(side*.12,2.22*scale_factor,.01);horn.rotation_degrees.z=side*22;horn.material_override=make_mat(Color("#b28a5b"),.3,.35);root.add_child(horn)
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

    # Android uses the procedural fallback for the alpha build. This avoids large external
    # GLB imports during startup on Vulkan/mobile devices; desktop can still layer Vitruvian assets.
    if OS.get_name() != "Android" and ResourceLoader.exists(BODY):
        var body_scene=load(BODY) as PackedScene
        if body_scene:
            var body=body_scene.instantiate()
            if body:
                body.name="VitruvianBody"
                root.add_child(body)
    if OS.get_name() != "Android" and ResourceLoader.exists(HEAD):
        var head_scene=load(HEAD) as PackedScene
        if head_scene:
            var h=head_scene.instantiate()
            if h:
                h.name="VitruvianHead"
                h.position=Vector3(0,1.68,0)
                root.add_child(h)
    if OS.get_name() != "Android" and ResourceLoader.exists(HAIR):
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
    var b=MeshInstance3D.new();var bm=BoxMesh.new();bm.size=Vector3(.055,1.62,.11);b.mesh=bm;b.position.y=.81;b.material_override=make_mat(Color("#f2f3ff"),.08,1.15);r.add_child(b)
    var edge=MeshInstance3D.new();var em=BoxMesh.new();em.size=Vector3(.018,1.48,.025);edge.mesh=em;edge.position=Vector3(.03,.82,-.065);edge.material_override=make_mat(Color("#bca5ff"),.05,3.0);r.add_child(edge)
    var g=MeshInstance3D.new();var gm=TorusMesh.new();gm.inner_radius=.17;gm.outer_radius=.235;g.mesh=gm;g.rotation_degrees.x=90;g.material_override=make_mat(Color("#d7a84d"),.22,.45);r.add_child(g)
    var h=MeshInstance3D.new();var cm=CylinderMesh.new();cm.height=.66;cm.top_radius=.105;cm.bottom_radius=.105;h.mesh=cm;h.position.y=-.30;h.material_override=make_mat(Color("#101016"),.62);r.add_child(h)
    var pom=MeshInstance3D.new();var pm=SphereMesh.new();pm.radius=.10;pm.height=.20;pom.mesh=pm;pom.position.y=-.64;pom.material_override=make_mat(Color("#7c4a6c"),.25,.7);r.add_child(pom);return r

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
    player.move_and_slide();player.position.x=clampf(player.position.x,-112,112);player.position.z=clampf(player.position.z,-112,112)

func dash():
    stamina-=20;dash_t=.5;invuln_t=.38;player.velocity+=-player.global_transform.basis.z*(18+mobility*1.5);camera_shake=.18;burst(player.position+Vector3.UP,Color("#9c79ff"),18);dash_trail()

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
    e.n.velocity+=dir*(7 if launch else 3);e.n.velocity.y=3.5 if launch else 1.2;hitstop=.055 if launch else .035;camera_shake=.16 if launch else .08;burst(e.n.position+Vector3.UP,Color("#ff4f86") if not launch else Color("#ffe8f4"),14 if launch else 9);impact_ring(e.n.position+Vector3.UP*.8,1.0 if launch else .65,Color("#ff5d91") if not launch else Color("#fff0fa"))
    if float(e.n.get_meta("hp"))<=0:
        souls+=100 if not e.boss else 1500;defeated+=1;gain_xp(80 if not e.boss else 600)
        if bool(e.n.get_meta("mini",false)): mini_defeated+=1
        if e.boss:say("TSUKUYOMI DEFEATED",5)
        e.n.queue_free();enemies.erase(e)

func slash(heavy,finisher=false):
    var c=Color("#ffe0ef") if finisher else (Color("#ffb7d5") if heavy else Color("#8f72ff"))
    var n=MeshInstance3D.new();var t=TorusMesh.new();t.inner_radius=2.5 if heavy or finisher else 1.5;t.outer_radius=2.62 if heavy or finisher else 1.63;n.mesh=t;n.position=player.position+Vector3.UP*.95;n.rotation_degrees=Vector3(90,-18 if combo%2==0 else 18,0);n.material_override=make_mat(c,.08,2.2);add_child(n);fx.append({"n":n,"t":.22 if heavy else .16,"v":Vector3.ZERO})
    var n2=MeshInstance3D.new();var t2=TorusMesh.new();t2.inner_radius=1.0 if finisher else .72;t2.outer_radius=1.06 if finisher else .78;n2.mesh=t2;n2.position=n.position+Vector3.UP*.08;n2.rotation_degrees=Vector3(90,35,0);n2.material_override=make_mat(Color.WHITE,.06,2.8);add_child(n2);fx.append({"n":n2,"t":.14,"v":Vector3.ZERO})
    burst(player.position+Vector3.UP*1.0,c,6 if not finisher else 14)
    camera_shake=.12 if heavy else .07

func impact_ring(p:Vector3,radius:float,c:Color):
    var n=MeshInstance3D.new();var t=TorusMesh.new();t.inner_radius=radius;t.outer_radius=radius+.07;n.mesh=t;n.position=p;n.rotation_degrees.x=90;n.material_override=make_mat(c,.08,2.8);add_child(n);fx.append({"n":n,"t":.24,"v":Vector3.ZERO})

func dash_trail():
    var n=MeshInstance3D.new();var t=TorusMesh.new();t.inner_radius=.38;t.outer_radius=.46;n.mesh=t;n.position=player.position+Vector3.UP*.85;n.rotation_degrees.x=90;n.material_override=make_mat(Color("#9c79ff"),.08,2.4);add_child(n);fx.append({"n":n,"t":.32,"v":Vector3(0,.15,0)})

func magic(school):
    if attack_t>0 or mana<18:return
    mana-=18;attack_t=.45;combo=max(combo,1);combo_t=1.4
    var c=Color("#a98cff") if school==0 else (Color("#ff6a3d") if school==1 else Color("#2c183c"))
    var n=MeshInstance3D.new();var s=SphereMesh.new();s.radius=.24;s.height=.48;n.mesh=s;n.position=player.position+Vector3.UP*1.2-player.global_transform.basis.z*1.3;n.material_override=make_mat(c,.08,2.5);add_child(n)
    shots.append({"n":n,"v":-player.global_transform.basis.z*20.0,"t":2.2,"d":70+magic_power*18,"school":school});burst(n.position,c,12);impact_ring(n.position,.42,c);camera_shake=.05
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

func tick_enemy_shots(d):
    for q in enemy_shots.duplicate():
        if not is_instance_valid(q.n):enemy_shots.erase(q);continue
        q.t-=d;q.n.position+=q.v*d
        if q.n.position.distance_to(player.position+Vector3.UP*.8)<.9:
            take_damage(q.d);burst(q.n.position,Color("#ff6a55"),8);q.n.queue_free();enemy_shots.erase(q)
        elif q.t<=0:
            q.n.queue_free();enemy_shots.erase(q)

func spawn_enemy(p,boss=false,mini_kind=""):
    var n=CharacterBody3D.new();n.position=p;add_child(n)
    var cs=CollisionShape3D.new();var s=CapsuleShape3D.new();s.radius=.5 if not boss else .72;s.height=1.8 if not boss else 2.5;cs.shape=s;cs.position.y=s.height*.5;n.add_child(cs)
    var v=human_visual(true)
    if v:v.scale*=1.08 if boss else .98;n.add_child(v)
    var mini=mini_kind!="";var elite=(not boss and randf()<.25) or mini
    var base_hp=140.0+float(wave-1)*18.0
    if elite:base_hp*=1.65
    if mini:base_hp=620.0+float(wave)*35.0
    var max_hp=(850.0+float(maxi(0,wave-1))*120.0) if boss else base_hp
    n.set_meta("hp",max_hp);n.set_meta("max_hp",max_hp);n.set_meta("stagger",0.0);n.set_meta("elite",elite);n.set_meta("phase",1);n.set_meta("mini",mini);n.set_meta("mini_name",mini_kind)
    n.set_meta("type","boss" if boss else (mini_kind if mini else (["duelist","hunter","brute"][randi()%3] if wave>=3 else "duelist")))
    if mini:n.scale=Vector3(1.22,1.22,1.22);say("%s HAS AWAKENED"%mini_kind,2.0)
    enemies.append({"n":n,"boss":boss,"a":randf_range(.4,1.5)})

func tick_enemies(d):
    for e in enemies.duplicate():
        if not is_instance_valid(e.n):enemies.erase(e);continue
        var to=player.position-e.n.position;to.y=0;var dist=to.length();var st=maxf(0,float(e.n.get_meta("stagger"))-d*45);e.n.set_meta("stagger",st)
        if e.boss or bool(e.n.get_meta("mini",false)):
            var max_hp=float(e.n.get_meta("max_hp"))
            var ratio=float(e.n.get_meta("hp"))/max_hp
            var phase=1 if ratio>.66 else (2 if ratio>.33 else 3)
            var old_phase=int(e.n.get_meta("phase",1))
            if phase!=old_phase:
                e.n.set_meta("phase",phase)
                var phase_color=Color("#9c79ff") if phase==2 else Color("#ff4f86")
                burst(e.n.position+Vector3.UP*1.4,phase_color,24)
                impact_ring(e.n.position+Vector3.UP*.15,2.2,phase_color)
                camera_shake=.16
                var phase_name=str(e.n.get_meta("mini_name","TSUKUYOMI")) if bool(e.n.get_meta("mini",false)) else "TSUKUYOMI"
                say("%s • PHASE %d"%(phase_name,phase),1.5)
        if st>90:e.n.velocity=Vector3.ZERO
        elif dist>2.6:
            var q=to.normalized();var phase_now=int(e.n.get_meta("phase",1));var special=e.boss or bool(e.n.get_meta("mini",false));var enr=special and phase_now>=2;var elite=bool(e.n.get_meta("elite"));var enemy_type=str(e.n.get_meta("type","duelist"));var speed=3.8 if enr else (4.4 if enemy_type=="duelist" else (2.2 if enemy_type=="brute" else 2.6))
            e.n.velocity.x=q.x*speed;e.n.velocity.z=q.z*speed;e.n.look_at(e.n.position+q,Vector3.UP)
        else:
            e.n.velocity.x=move_toward(e.n.velocity.x,0,d*12);e.n.velocity.z=move_toward(e.n.velocity.z,0,d*12);e.a-=d
            if e.a<=0 and parry_t<=0:
                var phase_now=int(e.n.get_meta("phase",1))
                var enemy_type=str(e.n.get_meta("type","duelist"))
                if not e.boss and enemy_type=="hunter" and dist>4.0:
                    e.a=1.35
                    var bolt=MeshInstance3D.new();var sm=SphereMesh.new();sm.radius=.12;sm.height=.24;bolt.mesh=sm;bolt.position=e.n.position+Vector3.UP*1.35;bolt.material_override=make_mat(Color("#ff8b6b"),.06,4.0);add_child(bolt)
                    enemy_shots.append({"n":bolt,"v":to.normalized()*12.0,"t":2.0,"d":13.0})
                    impact_ring(e.n.position+Vector3.UP*.15,.72,Color("#ff8b6b"));burst(bolt.position,Color("#ffb18d"),5)
                else:
                    e.a=(.45 if phase_now==3 else (.62 if phase_now==2 else .9)) if e.boss else (1.1 if enemy_type=="brute" else 1.55)
                    var mini_name=str(e.n.get_meta("mini_name",""))
                    if bool(e.n.get_meta("mini",false)) and mini_name=="ONI GUARDIAN":
                        impact_ring(e.n.position+Vector3.UP*.1,2.0 if phase_now>=2 else 1.35,Color("#ff704d"))
                        e.n.velocity+=-e.n.global_transform.basis.z*4.5
                    elif bool(e.n.get_meta("mini",false)) and mini_name=="KITSUNE WARDEN":
                        var fox=MeshInstance3D.new();var fm=SphereMesh.new();fm.radius=.16;fm.height=.32;fox.mesh=fm;fox.position=e.n.position+Vector3.UP*1.2;fox.material_override=make_mat(Color("#ff9a4d"),.08,4.5);add_child(fox)
                        enemy_shots.append({"n":fox,"v":to.normalized()*15.0,"t":1.8,"d":18.0+phase_now*4.0})
                        if phase_now>=2: e.n.position+=to.normalized()*2.5
                    elif bool(e.n.get_meta("mini",false)) and mini_name=="MOURNING SAMURAI":
                        e.n.velocity+=to.normalized()*(8.0+phase_now*2.0)
                        impact_ring(e.n.position+Vector3.UP*.1,.9,Color("#c9b8ff"))
                    var attack_damage=(34 if phase_now==3 else (28 if phase_now==2 else 22)) if e.boss else ((25 if phase_now==3 else (20 if phase_now==2 else 16)) if bool(e.n.get_meta("mini",false)) else (14 if enemy_type=="brute" else 9))
                    impact_ring(e.n.position+Vector3.UP*.1,1.0 if e.boss else (.72 if enemy_type=="brute" else .55),Color("#ff4f86") if e.boss else (Color("#ff704d") if enemy_type=="brute" else Color("#9c79ff")))
                    take_damage(attack_damage)
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
    var target=player.position+Vector3.UP*1.15
    var lx=Input.get_axis("ui_home","ui_end")
    var ly=Input.get_axis("ui_page_up","ui_page_down")
    if abs(lx)+abs(ly)>.05:
        camera_yaw-=lx*d*2.6
        camera_pitch=clampf(camera_pitch+ly*d*70.0,-8.0,42.0)
    var rot=Basis(Vector3.UP,camera_yaw)*Basis(Vector3.RIGHT,deg_to_rad(camera_pitch))
    var offset=rot*Vector3(0,0,7.4)
    if camera_shake>0:
        camera_shake=maxf(0,camera_shake-d)
        offset+=Vector3(randf_range(-1,1),randf_range(-.7,.7),randf_range(-1,1))*camera_shake*2.5
    camera.position=camera.position.lerp(target+offset,1-exp(-d*8))
    camera.look_at(target,Vector3.UP)

func build_ui():
    var layer=CanvasLayer.new();add_child(layer)
    var panel=ColorRect.new();panel.position=Vector2(18,18);panel.size=Vector2(500,155);panel.color=Color(.02,.015,.04,.86);layer.add_child(panel)
    var title=Label.new();title.text="YOKAI // SHADOW OF IZANAMI";title.position=Vector2(34,25);title.add_theme_font_size_override("font_size",22);title.add_theme_color_override("font_color",Color("#e8c77d"));layer.add_child(title)
    hpbar=bar(layer,Vector2(34,60),Color("#d83f63"));stbar=bar(layer,Vector2(34,82),Color("#59cfa3"));mpbar=bar(layer,Vector2(34,104),Color("#7668e8"))
    info=Label.new();info.position=Vector2(530,25);info.add_theme_font_size_override("font_size",18);layer.add_child(info)
    skills=Label.new();skills.position=Vector2(34,125);skills.add_theme_font_size_override("font_size",14);layer.add_child(skills)
    quest_label=Label.new();quest_label.position=Vector2(530,135);quest_label.add_theme_font_size_override("font_size",16);quest_label.add_theme_color_override("font_color",Color("#d7b7ff"));layer.add_child(quest_label)
    var specs=[["ATK",Vector2(940,580),"attack"],["HEAVY",Vector2(1080,620),"heavy"],["DASH",Vector2(1110,520),"dash"],["MOON",Vector2(930,500),"m0"],["FIRE",Vector2(1030,455),"m1"],["VOID",Vector2(1130,455),"m2"],["PARRY",Vector2(790,610),"parry"],["SHRINE",Vector2(670,610),"shrine"]]
    for a in specs:
        var b=Button.new();b.text=a[0];b.position=a[1];b.size=Vector2(120,55);layer.add_child(b);b.pressed.connect(func():mobile(a[2]))
    # Native Godot buttons only: no third-party joystick dependency is required.
    hold_button(layer,"▲",Vector2(150,470),Vector2(90,70),Vector2(0,-1))
    hold_button(layer,"▼",Vector2(150,630),Vector2(90,70),Vector2(0,1))
    hold_button(layer,"◀",Vector2(55,550),Vector2(90,70),Vector2(-1,0))
    hold_button(layer,"▶",Vector2(245,550),Vector2(90,70),Vector2(1,0))
    hold_button(layer,"CAM ◀",Vector2(880,650),Vector2(110,55),Vector2(-1,0),true)
    hold_button(layer,"CAM ▶",Vector2(1130,650),Vector2(110,55),Vector2(1,0),true)
    var save=Button.new();save.text="SAVE";save.position=Vector2(20,440);save.size=Vector2(100,48);layer.add_child(save);save.pressed.connect(save_game)
    var load=Button.new();load.text="LOAD";load.position=Vector2(130,440);load.size=Vector2(100,48);layer.add_child(load);load.pressed.connect(load_game)
    var up1=Button.new();up1.text="BLADE +";up1.position=Vector2(250,440);up1.size=Vector2(105,48);layer.add_child(up1);up1.pressed.connect(func():upgrade_skill(0))
    var up2=Button.new();up2.text="MAGIC +";up2.position=Vector2(365,440);up2.size=Vector2(105,48);layer.add_child(up2);up2.pressed.connect(func():upgrade_skill(1))
    var up3=Button.new();up3.text="MOBILITY +";up3.position=Vector2(480,440);up3.size=Vector2(115,48);layer.add_child(up3);up3.pressed.connect(func():upgrade_skill(2))
    var pause=Button.new();pause.text="Ⅱ";pause.position=Vector2(1190,25);pause.size=Vector2(65,55);layer.add_child(pause);pause.pressed.connect(toggle_pause)
    banner=Label.new();banner.position=Vector2(0,235);banner.size=Vector2(1280,80);banner.horizontal_alignment=HORIZONTAL_ALIGNMENT_CENTER;banner.add_theme_font_size_override("font_size",32);banner.add_theme_color_override("font_color",Color("#f0c878"));layer.add_child(banner)

func hold_button(layer,text_value,pos,size,dir,camera_button=false):
    var b=Button.new()
    b.text=text_value
    b.position=pos
    b.size=size
    b.modulate=Color(1,1,1,.82)
    layer.add_child(b)
    if camera_button:
        b.button_down.connect(func(): Input.action_press("ui_home" if dir.x<0 else "ui_end"))
        b.button_up.connect(func(): Input.action_release("ui_home" if dir.x<0 else "ui_end"))
    else:
        b.button_down.connect(func(): virtual_dir=dir)
        b.button_up.connect(func(): if virtual_dir==dir: virtual_dir=Vector2.ZERO)

func bar(layer,pos,c):
    var b=ProgressBar.new();b.position=pos;b.size=Vector2(400,14);b.max_value=100;b.show_percentage=false
    var bg=StyleBoxFlat.new();bg.bg_color=Color(.08,.07,.12,.9);var fill=StyleBoxFlat.new();fill.bg_color=c;b.add_theme_stylebox_override("background",bg);b.add_theme_stylebox_override("fill",fill);layer.add_child(b);return b

func mobile(a):
    if a=="attack" and attack_t<=0:melee(false)
    elif a=="heavy" and attack_t<=0:melee(true)
    elif a=="dash" and dash_t<=0 and stamina>=20:dash()
    elif a=="parry":parry()
    elif a=="shrine":rest_at_shrine()
    elif a.begins_with("m") and attack_t<=0:magic(int(a.substr(1)))

func upgrade_skill(kind):
    if skill_points<=0:
        say("NO SKILL POINTS",1.0)
        return
    skill_points-=1
    if kind==0:
        blade+=1
        say("BLADE MASTERY %d"%blade,1.0)
    elif kind==1:
        magic_power+=1
        say("ONMYO MASTERY %d"%magic_power,1.0)
    else:
        mobility+=1
        stamina=minf(100,stamina+15)
        say("SHADOW STEP %d"%mobility,1.0)

func update_zone():
    if not player:return
    var p=player.position
    var best="MOONLIT VILLAGE"
    var best_dist=999999.0
    var zones={"MOONLIT VILLAGE":Vector3(0,0,22),"WISTERIA FOREST":Vector3(-62,0,-58),"KITSUNE VALLEY":Vector3(64,0,-48),"ASHEN BATTLEFIELD":Vector3(58,0,58),"YOMI GATE":Vector3(0,0,-92)}
    for name in zones:
        var dist=p.distance_to(zones[name])
        if dist<best_dist:
            best_dist=dist;best=name
    if best_dist>28.0:best="MOONLIT VILLAGE"
    if best!=map_zone:
        map_zone=best
        if not discovered_zones.has(best):
            discovered_zones[best]=true
            say("REGION DISCOVERED • %s"%best,2.4)
        else:
            say(best,1.0)
    zone_hint="%s  •  %dm TO HEART"%[map_zone,int(best_dist)]

func update_ui():
    hpbar.value=hp;stbar.value=stamina;mpbar.value=mana
    var boss_hp=0
    for e in enemies:
        if e.boss and is_instance_valid(e.n):boss_hp=int(e.n.get_meta("hp"))
    info.text="LV %d  HP %d  ST %d  MP %d  COMBO x%d  SOULS %d  XP %d/%d  WAVE %d\n%s" %[level,hp,stamina,mana,combo,souls,xp,level*250,wave,zone_hint]
    if boss_hp>0:
        var boss_max=850
        for e in enemies:
            if e.boss and is_instance_valid(e.n):boss_max=int(e.n.get_meta("max_hp"))
        var phase_text="I" if boss_hp>boss_max*.66 else ("II" if boss_hp>boss_max*.33 else "III")
        info.text+="
TSUKUYOMI  %d  • PHASE %s"%(boss_hp,phase_text)
    skills.text="BLADE %d  MAGIC %d  MOBILITY %d  |  SP %d  KILLS %d  CACHES %d/8"%[blade,magic_power,mobility,skill_points,defeated,caches_collected]
    if quest_label:quest_label.text=quest_text()

func gain_xp(a):
    xp+=a
    while xp>=level*250:xp-=level*250;level+=1;skill_points+=1;hp=100;stamina=100;mana=100;say("LEVEL UP • SOUL LEVEL %d"%level,2)

func save_game(silent=false):
    if not player:return
    var taken=[]
    for cache in soul_caches:taken.append(bool(cache.taken))
    var d={"version":2,"hp":hp,"stamina":stamina,"mana":mana,"souls":souls,"xp":xp,"level":level,"skill_points":skill_points,"blade":blade,"magic_power":magic_power,"mobility":mobility,"pos":[player.position.x,player.position.y,player.position.z],"wave":wave,"defeated":defeated,"caches_collected":caches_collected,"mini_defeated":mini_defeated,"quest_stage":quest_stage,"cache_taken":taken}
    var f=FileAccess.open("user://yokai_save.json",FileAccess.WRITE)
    if f:
        f.store_string(JSON.stringify(d))
        if not silent:say("GAME SAVED",1.5)

func load_game():
    if not FileAccess.file_exists("user://yokai_save.json"):say("NO SAVE FOUND",1.5);return
    var f=FileAccess.open("user://yokai_save.json",FileAccess.READ)
    var d=JSON.parse_string(f.get_as_text())
    if d:
        hp=d.get("hp",100);stamina=d.get("stamina",100);mana=d.get("mana",100);souls=d.get("souls",0);xp=d.get("xp",0);level=d.get("level",1);skill_points=d.get("skill_points",0);blade=d.get("blade",1);magic_power=d.get("magic_power",1);mobility=d.get("mobility",1);wave=d.get("wave",1);defeated=d.get("defeated",0);caches_collected=d.get("caches_collected",0);mini_defeated=d.get("mini_defeated",0);quest_stage=d.get("quest_stage",0)
        var p=d.get("pos",[0,0,18]);player.position=Vector3(float(p[0]),float(p[1]),float(p[2]))
        var taken=d.get("cache_taken",[])
        for i in range(min(taken.size(),soul_caches.size())):
            soul_caches[i].taken=bool(taken[i])
            if soul_caches[i].taken and is_instance_valid(soul_caches[i].n):soul_caches[i].n.visible=false
        say("GAME LOADED",1.5)

func toggle_pause():
    paused=!paused
    if banner:banner.text="PAUSED" if paused else ""

func say(t,sec):
    if banner:banner.text=t;get_tree().create_timer(sec).timeout.connect(func():if is_instance_valid(banner):banner.text="")
