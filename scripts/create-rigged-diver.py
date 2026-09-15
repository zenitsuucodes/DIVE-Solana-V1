import bpy, math, os
from mathutils import Vector
ROOT=r'C:\Users\sreer\OneDrive\Documents\Codex\dive'
out=os.path.join(ROOT,'unity','Assets','Dive','Resources')
os.makedirs(out,exist_ok=True)
scene=bpy.data.scenes.new('DIVE Rigged Athlete')
bpy.context.window.scene=scene
scene.unit_settings.system='METRIC'
parts=[]
def mat(name,c,metal=0,rough=.4):
 m=bpy.data.materials.new(name);m.diffuse_color=(*c,1);m.use_nodes=True
 p=m.node_tree.nodes.get('Principled BSDF');p.inputs['Base Color'].default_value=(*c,1);p.inputs['Metallic'].default_value=metal;p.inputs['Roughness'].default_value=rough
 return m
suit=mat('Athlete Neoprene',(.016,.034,.048),.05,.52)
team=mat('Athlete Team',(.015,.3,.65),.12,.32)
skin=mat('Athlete Skin',(.5,.27,.15),0,.55)
glass=mat('Athlete Glass',(.09,.54,.62),.65,.13)
metal=mat('Athlete Metal',(.48,.55,.58),.8,.25)
ivory=mat('Athlete Ivory',(.85,.89,.81),.1,.3)
def finish(o,name,m,bone):
 o.name=name;o.data.materials.append(m)
 bpy.ops.object.transform_apply(location=False,rotation=False,scale=True)
 for f in o.data.polygons:f.use_smooth=True
 g=o.vertex_groups.new(name=bone);g.add(list(range(len(o.data.vertices))),1,'REPLACE');parts.append(o);return o
def ell(name,loc,scale,m,bone):
 bpy.ops.mesh.primitive_uv_sphere_add(segments=24,ring_count=12,location=loc)
 o=bpy.context.object;o.scale=scale;return finish(o,name,m,bone)
def limb(name,a,b,r1,r2,m,bone):
 d=Vector(b)-Vector(a)
 bpy.ops.mesh.primitive_cone_add(vertices=20,radius1=r1,radius2=r2,depth=d.length,location=(Vector(a)+Vector(b))/2)
 o=bpy.context.object;o.rotation_euler=d.to_track_quat('Z','Y').to_euler()
 mod=o.modifiers.new('Rounded seams','BEVEL');mod.width=.025;mod.segments=3
 bpy.ops.object.modifier_apply(modifier=mod.name)
 return finish(o,name,m,bone)
def box(name,loc,scale,m,bone,bevel=.02):
 bpy.ops.mesh.primitive_cube_add(size=1,location=loc);o=bpy.context.object;o.scale=scale;bpy.ops.object.transform_apply(location=False,rotation=False,scale=True)
 mod=o.modifiers.new('Rounded edges','BEVEL');mod.width=bevel;mod.segments=3;bpy.ops.object.modifier_apply(modifier=mod.name)
 return finish(o,name,m,bone)
# Horizontal swimmer, head toward Blender +Y; Unity import faces -Z.
ell('Ribcage',(0,.13,0),(.245,.31,.14),team,'Spine')
ell('Waist',(0,-.16,-.01),(.175,.2,.12),suit,'Spine')
ell('Pelvis',(0,-.33,-.02),(.205,.16,.125),suit,'Hips')
limb('Neck',(0,.38,0),(0,.49,.035),.078,.082,skin,'Head')
ell('Head',(0,.58,.025),(.13,.17,.13),skin,'Head')
ell('Hood',(0,.54,.085),(.135,.145,.092),suit,'Head')
ell('Nose',(0,.71,.008),(.037,.049,.04),skin,'Head')
box('Mask frame',(0,.689,.078),(.258,.065,.115),suit,'Head')
for s in [-1,1]:
 box('Mask lens',(s*.065,.724,.083),(.112,.015,.074),glass,'Head',.014)
box('Mask strap',(0,.56,.148),(.262,.042,.023),ivory,'Head',.007)
ell('Regulator',(0,.704,-.045),(.052,.045,.04),metal,'Head')
for s in [-1,1]:
 b='L' if s<0 else 'R'
 shoulder=(s*.23,.27,0);elbow=(s*.38,.43,-.09);wrist=(s*.34,.7,-.12)
 ell('Shoulder',shoulder,(.105,.12,.11),team,'UpperArm'+b)
 limb('Upper arm',shoulder,elbow,.092,.073,team,'UpperArm'+b)
 ell('Elbow',elbow,(.073,.073,.07),suit,'LowerArm'+b)
 limb('Forearm',elbow,wrist,.072,.047,suit,'LowerArm'+b)
 box('Wrist stripe',(s*.344,.667,-.11),(.105,.042,.095),team,'LowerArm'+b,.01)
 ell('Palm',(s*.34,.756,-.12),(.063,.077,.03),suit,'Hand'+b)
 for f in range(4):
  x=s*.34+(f-1.5)*.027
  limb('Glove finger',(x,.79,-.12),(x,.86-abs(f-1.5)*.012,-.126),.014,.011,suit,'Hand'+b)
 limb('Thumb',(s*.29,.74,-.12),(s*.255,.794,-.135),.021,.014,suit,'Hand'+b)
 hip=(s*.12,-.36,-.02);knee=(s*.15,-.77,-.075);ankle=(s*.18,-1.12,-.015)
 limb('Thigh',hip,knee,.105,.075,suit,'Thigh'+b)
 ell('Knee pad',knee,(.08,.094,.075),team,'Shin'+b)
 limb('Calf',knee,ankle,.078,.043,suit,'Shin'+b)
 ell('Boot',(s*.18,-1.18,-.02),(.068,.12,.06),suit,'Foot'+b)
 box('Fin blade',(s*.18,-1.42,-.035),(.235,.43,.033),team,'Foot'+b,.045)
 for stripe in [-1,1]:box('Fin rib',(s*.18+stripe*.082,-1.43,-.009),(.012,.36,.018),suit,'Foot'+b,.005)
 box('Tank',(s*.102,-.015,.2),(.16,.43,.14),metal,'Spine',.069)
 box('Tank restraint',(s*.102,-.05,.268),(.17,.058,.022),suit,'Spine',.008)
 limb('Shoulder harness',(s*.16,-.19,.13),(s*.18,.32,.127),.025,.025,suit,'Spine')
box('Chest crest',(0,.28,-.133),(.12,.12,.012),ivory,'Spine',.012)
box('Waist belt',(0,-.255,0),(.365,.053,.26),suit,'Hips',.01)
box('Buckle',(0,-.255,-.14),(.067,.054,.02),metal,'Hips',.007)
limb('Hockey stick',(.34,.82,-.13),(.4,1.1,-.17),.022,.019,ivory,'HandR')
box('Stick blade',(.49,1.105,-.17),(.22,.07,.036),suit,'HandR',.014)
# An explicit deform rig; every mesh vertex has a bone weight.
bpy.ops.object.select_all(action='DESELECT')
arm=bpy.data.armatures.new('Athlete skeleton');rig=bpy.data.objects.new('AthleteRig',arm);scene.collection.objects.link(rig);bpy.context.view_layer.objects.active=rig;rig.select_set(True);bpy.ops.object.mode_set(mode='EDIT')
def bone(n,h,t,parent=None):
 b=arm.edit_bones.new(n);b.head=h;b.tail=t
 if parent:b.parent=arm.edit_bones[parent]
bone('Hips',(0,-.33,0),(0,-.12,0))
bone('Spine',(0,-.12,0),(0,.37,0),'Hips');bone('Head',(0,.37,0),(0,.7,.03),'Spine')
for s in [-1,1]:
 b='L' if s<0 else 'R'
 bone('UpperArm'+b,(s*.23,.27,0),(s*.38,.43,-.09),'Spine')
 bone('LowerArm'+b,(s*.38,.43,-.09),(s*.34,.7,-.12),'UpperArm'+b)
 bone('Hand'+b,(s*.34,.7,-.12),(s*.34,.86,-.12),'LowerArm'+b)
 bone('Thigh'+b,(s*.12,-.36,-.02),(s*.15,-.77,-.075),'Hips')
 bone('Shin'+b,(s*.15,-.77,-.075),(s*.18,-1.12,-.015),'Thigh'+b)
 bone('Foot'+b,(s*.18,-1.12,-.015),(s*.18,-1.6,-.03),'Shin'+b)
bpy.ops.object.mode_set(mode='OBJECT');rig.select_set(False)
for p in parts:p.select_set(True)
bpy.context.view_layer.objects.active=parts[0];bpy.ops.object.join();body=bpy.context.object;body.name='AthleteBody';body.parent=rig
mod=body.modifiers.new('Athlete skin','ARMATURE');mod.object=rig
rig.animation_data_create()
for clip,amp in [('Swim',1),('Idle',.28),('Shoot',.5)]:
 action=bpy.data.actions.new(clip);rig.animation_data.action=action
 for frame in range(1,62,5):
  phase=(frame-1)/60*2*math.pi
  for p in rig.pose.bones:
   p.rotation_mode='XYZ';p.rotation_euler=(0,0,0)
   sign=-1 if p.name.endswith('L') else 1
   if p.name.startswith('Thigh'):p.rotation_euler.x=math.sin(phase)*.3*amp*sign
   if p.name.startswith('Shin'):p.rotation_euler.x=max(0,math.sin(phase)*sign)*.4*amp
   if p.name.startswith('Foot'):p.rotation_euler.x=math.sin(phase+.5)*.13*amp*sign
   if p.name.startswith('UpperArm'):p.rotation_euler.z=math.sin(phase)*.14*amp*sign
   if p.name.startswith('LowerArm'):p.rotation_euler.x=math.sin(phase+.6)*.13*amp
   if clip=='Shoot' and p.name=='UpperArmR':p.rotation_euler.x=-math.sin(phase)*.65
   p.keyframe_insert('rotation_euler',frame=frame,group=p.name)
 rig.animation_data.action=None
scene.frame_start=1;scene.frame_end=61;scene.render.fps=30
for p in rig.pose.bones:p.rotation_euler=(0,0,0)
bpy.ops.object.select_all(action='DESELECT');rig.select_set(True);body.select_set(True);bpy.context.view_layer.objects.active=rig
bpy.ops.export_scene.fbx(filepath=os.path.join(out,'Athlete.fbx'),use_selection=True,add_leaf_bones=False,bake_anim=True,bake_anim_use_all_actions=True,bake_anim_use_nla_strips=False,axis_forward='-Z',axis_up='Y')
bpy.ops.wm.save_as_mainfile(filepath=os.path.join(ROOT,'art','athlete-rigged.blend'))
result={'fbx':os.path.join(out,'Athlete.fbx'),'bones':len(arm.bones),'vertices':len(body.data.vertices),'clips':['Swim','Idle','Shoot']}

# Final Unity export: triangulated skin, explicit FBX unit scaling, and first-person arms.
import bmesh
hands=body.copy();hands.data=body.data.copy();scene.collection.objects.link(hands);hands.name='FirstPersonHands'
keep={g.index for g in hands.vertex_groups if g.name in ['LowerArmL','HandL','LowerArmR','HandR']}
bm=bmesh.new();bm.from_mesh(hands.data);layer=bm.verts.layers.deform.active
bmesh.ops.delete(bm,geom=[v for v in bm.verts if not any(k in keep and w>.5 for k,w in v[layer].items())],context='VERTS');bm.to_mesh(hands.data);bm.free()
for ob,filename in [(body,'Athlete.fbx'),(hands,'SwimHands.fbx')]:
 bm=bmesh.new();bm.from_mesh(ob.data);bmesh.ops.triangulate(bm,faces=list(bm.faces));bm.to_mesh(ob.data);bm.free()
 bpy.ops.object.select_all(action='DESELECT');ob.select_set(True);rig.select_set(True);bpy.context.view_layer.objects.active=rig
 bpy.ops.export_scene.fbx(filepath=os.path.join(out,filename),use_selection=True,add_leaf_bones=False,bake_anim=True,bake_anim_use_all_actions=True,bake_anim_use_nla_strips=False,apply_scale_options='FBX_SCALE_ALL',axis_forward='-Z',axis_up='Y')
hands.hide_render=True
bpy.ops.wm.save_as_mainfile(filepath=os.path.join(ROOT,'art','athlete-rigged.blend'))
