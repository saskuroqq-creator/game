extends Node3D

var player: CharacterBody3D
var camera: Camera3D
var enemies: Array[Dictionary] = []
var fx: Array[Dictionary] = []
var hp := 100.0
var stamina := 100.0
var mana := 100.0
var combo := 0
var combo_t := 0.0
var attack_t := 0.0
var dash_t := 0.0
var souls := 0
var hpbar: ProgressBar
var stbar: ProgressBar
var mpbar: ProgressBar
var info: Label
var banner: Label

func _ready():
    _world()
    _player()
    _ui()
    for i in range(8):
        spawn_enemy(Vector3(-18.0 + float(i % 4) * 12.0, 1.0, -18.0 + float(i / 4) * 18.0), false)
    spawn_enemy(Vector3(0, 1.6, -30), true)
    say("THE GATE OF TSUKUYOMI", 3.0)

func _process(d):
    if not player: return
    attack_t=maxf(0.0,attack_t-d); dash_t=maxf(0.0,dash_t-d); combo_t=maxf(0.0,combo_t-d)
    if combo_t<=0: combo=0
    stamina=minf(100.0,stamina+d*20.0); mana=minf(100.0,mana+d*5.0)
    move_player(d); enemies_tick(d); fx_tick(d); camera_tick(d); ui_tick()

func mat(c:Color,emit:=0.0)->StandardMaterial3D:
    var m=StandardMaterial3D.new(); m.albedo_color=c; m.roughness=.45
    if emit>0: m.emission_enabled=true; m.emission=c; m.emission_energy_multiplier=emit
    return m

func _world():
    var env=WorldEnvironment.new(); var e=Environment.new()
    e.background_mode=Environment.BG_COLOR; e.background_color=Color("#080711")
    e.ambient_light_source=Environment.AMBIENT_SOURCE_COLOR; e.ambient_light_color=Color("#56446f"); e.ambient_light_energy=.6
    e.tonemap_mode=Environment.TONE_MAPPER_FILMIC; e.glow_enabled=true; e.glow_intensity=1.1
    env.environment=e; add_child(env)
    var sun=DirectionalLight3D.new(); sun.rotation_degrees=Vector3(-55,-25,0); sun.light_energy=.7; sun.shadow_enabled=true; sun.light_color=Color("#c2b3d4"); add_child(sun)
    var moon=OmniLight3D.new(); moon.position=Vector3(0,10,0); moon.omni_range=50; moon.light_color=Color("#735de0"); moon.light_energy=8; add_child(moon)
    var ground=StaticBody3D.new(); var gm=MeshInstance3D.new(); var gb=BoxMesh.new(); gb.size=Vector3(90,1,90); gm.mesh=gb; gm.material_override=mat(Color("#14121b")); ground.add_child(gm)
    var gc=CollisionShape3D.new(); var gs=BoxShape3D.new(); gs.size=Vector3(90,1,90); gc.shape=gs; ground.add_child(gc); add_child(ground)
    for i in range(28):
        var a=TAU*i/28.0; var r=23.0+sin(i*3.7)*4.0; pillar(Vector3(cos(a)*r,0,sin(a)*r),3.0+float(i%4))
    for i in range(16):
        var a=TAU*i/16.0; lantern(Vector3(cos(a)*11,0,sin(a)*11))
    gate()

func pillar(p:Vector3,h:float):
    var n=MeshInstance3D.new(); var c=CylinderMesh.new(); c.top_radius=.65; c.bottom_radius=1.0; c.height=h; n.mesh=c; n.position=p+Vector3.UP*h/2; n.material_override=mat(Color("#251d2d")); add_child(n)

func lantern(p:Vector3):
    var n=MeshInstance3D.new(); var c=CylinderMesh.new(); c.top_radius=.3; c.bottom_radius=.42; c.height=1.5; n.mesh=c; n.position=p+Vector3.UP*.75; n.material_override=mat(Color("#3c252e")); add_child(n)
    var l=OmniLight3D.new(); l.position=p+Vector3.UP*1.5; l.light_color=Color("#ff9d5c"); l.light_energy=3.5; l.omni_range=7; add_child(l)

func gate():
    for x in [-6.0,6.0]:
        var n=MeshInstance3D.new(); var b=BoxMesh.new(); b.size=Vector3(2.4,11,2.4); n.mesh=b; n.position=Vector3(x,5.5,-34); n.material_override=mat(Color("#35162d"),.0); add_child(n)
    var t=MeshInstance3D.new(); var b=BoxMesh.new(); b.size=Vector3(15,2.2,2.8); t.mesh=b; t.position=Vector3(0,10.2,-34); t.material_override=mat(Color("#4a1833"),.3); add_child(t)
    var l=OmniLight3D.new(); l.position=Vector3(0,6,-34); l.light_color=Color("#e04a78"); l.light_energy=7; l.omni_range=10; add_child(l)

func _player():
    player=CharacterBody3D.new(); player.position=Vector3(0,1.2,15); add_child(player)
    var cs=CollisionShape3D.new(); var sh=CapsuleShape3D.new(); sh.radius=.65; sh.height=2.4; cs.shape=sh; player.add_child(cs)
    var m=MeshInstance3D.new(); var cm=CapsuleMesh.new(); cm.radius=.65; cm.height=2.4; m.mesh=cm; m.material_override=mat(Color("#d9d6e6"),.0); player.add_child(m)
    var glow=OmniLight3D.new(); glow.position.y=1; glow.light_color=Color("#8565ff"); glow.light_energy=2; glow.omni_range=5; player.add_child(glow)
    camera=Camera3D.new(); camera.fov=58; camera.current=true; add_child(camera); camera.position=Vector3(0,7,11)

func move_player(d):
    var v=Vector2(Input.get_axis("ui_left","ui_right"),Input.get_axis("ui_up","ui_down")); var dir=Vector3(v.x,0,v.y)
    if dir.length()>.05:
        dir=dir.normalized(); player.velocity.x=move_toward(player.velocity.x,dir.x*8,d*30); player.velocity.z=move_toward(player.velocity.z,dir.z*8,d*30); player.look_at(player.global_position+dir,Vector3.UP)
    else:
        player.velocity.x=move_toward(player.velocity.x,0,d*25); player.velocity.z=move_toward(player.velocity.z,0,d*25)
    if Input.is_action_just_pressed("dash") and dash_t<=0 and stamina>=25:
        stamina-=25; dash_t=.65; player.velocity+=-player.global_transform.basis.z*18; burst(player.position+Vector3.UP,Color("#9c79ff"),18)
    if Input.is_action_just_pressed("attack") and attack_t<=0: hit(false)
    if Input.is_action_just_pressed("heavy") and attack_t<=0 and mana>=18: mana-=18; hit(true)
    player.move_and_slide(); player.position.x=clampf(player.position.x,-42,42); player.position.z=clampf(player.position.z,-42,42)

func hit(heavy:bool):
    attack_t=.55 if heavy else .25; combo+=1; combo_t=1.0
    var r=4.6 if heavy else 3.0; var dmg=(65+combo*8) if heavy else (24+combo*5)
    slash(heavy)
    for e in enemies.duplicate():
        if not is_instance_valid(e.n): enemies.erase(e); continue
        if player.position.distance_to(e.n.position)<=r:
            e.n.set_meta("hp",float(e.n.get_meta("hp"))-dmg); burst(e.n.position+Vector3.UP,Color("#ff4f86"),9)
            if float(e.n.get_meta("hp"))<=0:
                souls+=100 if not e.boss else 1000
                if e.boss: say("TSUKUYOMI DEFEATED",5.0)
                e.n.queue_free(); enemies.erase(e)

func slash(heavy):
    var n=MeshInstance3D.new(); var t=TorusMesh.new(); t.inner_radius=2.2 if heavy else 1.4; t.outer_radius=2.35 if heavy else 1.55; n.mesh=t; n.position=player.position+Vector3.UP; n.rotation_degrees.x=90; n.material_override=mat(Color("#ffb7d5") if heavy else Color("#8f72ff"),.1,1.0); add_child(n); fx.append({"n":n,"t":.3})

func spawn_enemy(p:Vector3,boss:bool):
    var n=CharacterBody3D.new(); n.position=p; add_child(n)
    var cs=CollisionShape3D.new(); var s=CapsuleShape3D.new(); s.radius=1.1 if boss else .65; s.height=3.2 if boss else 2.1; cs.shape=s; n.add_child(cs)
    var m=MeshInstance3D.new(); var cm=CapsuleMesh.new(); cm.radius=s.radius; cm.height=s.height; m.mesh=cm; m.material_override=mat(Color("#9b2454") if boss else Color("#38203f"),.2,.2); n.add_child(m)
    var l=OmniLight3D.new(); l.position.y=1; l.light_color=Color("#ff426c"); l.light_energy=3 if boss else 1.3; l.omni_range=5; n.add_child(l)
    n.set_meta("hp",520.0 if boss else 90.0); enemies.append({"n":n,"boss":boss,"a":randf_range(.4,1.5)})

func enemies_tick(d):
    for e in enemies.duplicate():
        if not is_instance_valid(e.n): enemies.erase(e); continue
        var to=player.position-e.n.position; to.y=0; var dist=to.length()
        if dist>2.7:
            var q=to.normalized(); e.n.velocity.x=q.x*(2.2 if e.boss else 1.6); e.n.velocity.z=q.z*(2.2 if e.boss else 1.6); e.n.look_at(e.n.position+q,Vector3.UP)
        else:
            e.n.velocity.x=move_toward(e.n.velocity.x,0,d*10); e.n.velocity.z=move_toward(e.n.velocity.z,0,d*10); e.a-=d
            if e.a<=0:
                e.a=1.4 if e.boss else 2.0; hp-=12 if e.boss else 7; burst(player.position+Vector3.UP,Color("#ff405e"),5)
                if hp<=0: hp=100; say("DEFEAT — THE SPIRIT RETURNS",2)
        e.n.move_and_slide(); e.n.position.y=1.6 if e.boss else 1.1

func burst(p:Vector3,c:Color,count:int):
    for i in range(count):
        var n=MeshInstance3D.new(); var s=SphereMesh.new(); s.radius=.06; s.height=.12; n.mesh=s; n.position=p; n.material_override=mat(c,.1,1); add_child(n)
        fx.append({"n":n,"t":randf_range(.25,.65),"v":Vector3(randf_range(-1,1),randf_range(.2,1.5),randf_range(-1,1)).normalized()*randf_range(2,6)})

func fx_tick(d):
    for f in fx.duplicate():
        if not is_instance_valid(f.n): fx.erase(f); continue
        f.t-=d; f.n.position+=f.get("v",Vector3.ZERO)*d; f.n.scale*=1+d*4
        if f.t<=0: f.n.queue_free(); fx.erase(f)

func camera_tick(d):
    var target=player.position+Vector3.UP; var want=target+Vector3(0,6.5,10.5); camera.position=camera.position.lerp(want,1-exp(-d*7)); camera.look_at(target,Vector3.UP)

func _ui():
    var layer=CanvasLayer.new(); add_child(layer)
    var panel=ColorRect.new(); panel.position=Vector2(18,18); panel.size=Vector2(430,125); panel.color=Color(0.02,.015,.04,.8); layer.add_child(panel)
    var title=Label.new(); title.text="YOKAI  //  SHADOW OF IZANAMI"; title.position=Vector2(34,25); title.add_theme_font_size_override("font_size",22); title.add_theme_color_override("font_color",Color("#e8c77d")); layer.add_child(title)
    hpbar=bar(layer,Vector2(34,60),Color("#d83f63")); stbar=bar(layer,Vector2(34,82),Color("#59cfa3")); mpbar=bar(layer,Vector2(34,104),Color("#7668e8"))
    info=Label.new(); info.position=Vector2(470,25); info.add_theme_font_size_override("font_size",20); layer.add_child(info)
    for spec in [["ATTACK",Vector2(950,590),"attack"],["HEAVY",Vector2(1080,620),"heavy"],["DASH",Vector2(1110,520),"dash"]]:
        var b=Button.new(); b.text=spec[0]; b.position=spec[1]; b.size=Vector2(130,65); b.add_theme_font_size_override("font_size",17); layer.add_child(b)
        b.pressed.connect(func(): mobile_action(spec[2]))
    banner=Label.new(); banner.position=Vector2(0,245); banner.size=Vector2(1280,80); banner.horizontal_alignment=HORIZONTAL_ALIGNMENT_CENTER; banner.add_theme_font_size_override("font_size",34); banner.add_theme_color_override("font_color",Color("#f0c878")); layer.add_child(banner)

func bar(layer,pos,c):
    var b=ProgressBar.new(); b.position=pos; b.size=Vector2(365,14); b.max_value=100; b.show_percentage=false
    var bg=StyleBoxFlat.new(); bg.bg_color=Color(.08,.07,.12,.9); var fill=StyleBoxFlat.new(); fill.bg_color=c
    b.add_theme_stylebox_override("background",bg); b.add_theme_stylebox_override("fill",fill); layer.add_child(b); return b

func mobile_action(a):
    if a=="attack" and attack_t<=0: hit(false)
    elif a=="heavy" and attack_t<=0 and mana>=18: mana-=18; hit(true)
    elif a=="dash" and dash_t<=0 and stamina>=25: stamina-=25; dash_t=.65; player.velocity+=-player.global_transform.basis.z*18

func ui_tick():
    hpbar.value=hp; stbar.value=stamina; mpbar.value=mana
    var boss_hp=0
    for e in enemies:
        if e.boss: boss_hp=int(e.n.get_meta("hp"))
    info.text="HP %d   ST %d   SP %d   COMBO x%d   SOULS %d" % [hp,stamina,mana,combo,souls]
    if boss_hp>0: info.text+="\nTSUKUYOMI  %d / 520"%boss_hp

func say(t:String,sec:float):
    if banner: banner.text=t; get_tree().create_timer(sec).timeout.connect(func(): if is_instance_valid(banner): banner.text="")
