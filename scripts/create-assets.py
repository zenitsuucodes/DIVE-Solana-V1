import bpy, math, os
from mathutils import Vector

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
OUT = os.path.join(ROOT, 'public', 'assets')
os.makedirs(OUT, exist_ok=True)
bpy.ops.object.select_all(action='SELECT')
bpy.ops.object.delete(use_global=False)

def mat(name, color, metallic=0, rough=.4):
    m=bpy.data.materials.new(name); m.diffuse_color=(*color,1); m.use_nodes=True
    bs=m.node_tree.nodes.get('Principled BSDF'); bs.inputs['Base Color'].default_value=(*color,1)
    bs.inputs['Metallic'].default_value=metallic; bs.inputs['Roughness'].default_value=rough
    return m
team=mat('Team',(.015,.34,.85),.15,.29)
rubber=mat('Wetsuit',(.018,.045,.065),.12,.38)
glass=mat('Mask',(.25,.87,.91),.7,.16)
metal=mat('Tank',(.34,.41,.44),.7,.35)
skin=mat('Skin',(.73,.43,.25),0,.65)
white=mat('Details',(.85,.93,.93),.15,.4)

def ell(name, loc, scale, material):
    bpy.ops.mesh.primitive_uv_sphere_add(segments=16, ring_count=8, location=loc)
    ob=bpy.context.object; ob.name=name; ob.scale=scale; ob.data.materials.append(material)
    for f in ob.data.polygons:f.use_smooth=True
    return ob
def limb(name,a,b,r,material):
    mid=(Vector(a)+Vector(b))/2; ob=ell(name,mid,(r,r,(Vector(b)-Vector(a)).length/2+r*.3),material)
    ob.rotation_euler=(Vector(b)-Vector(a)).to_track_quat('Z','Y').to_euler(); return ob
def cube(name,loc,scale,material,bevel=.05):
    bpy.ops.mesh.primitive_cube_add(size=1,location=loc); ob=bpy.context.object;ob.name=name;ob.dimensions=scale
    bpy.ops.object.transform_apply(location=False,rotation=False,scale=True); ob.data.materials.append(material)
    mod=ob.modifiers.new('Soft edges','BEVEL'); mod.width=bevel;mod.segments=3
    ob.modifiers.new('Normals','WEIGHTED_NORMAL');return ob
# Blender Z-up, head pointing +Y. glTF converts to Y-up, facing -Z.
ell('Torso',(0,0,.28),(.22,.37,.15),team)
ell('Hips',(0,-.31,.26),(.18,.17,.13),rubber)
ell('Head',(0,.48,.29),(.155,.17,.14),skin)
ell('Hood',(0,.43,.34),(.165,.16,.10),team)
cube('Mask',(0,.58,.29),(.29,.075,.15),rubber,.035)
cube('Lens',(0,.626,.30),(.235,.026,.09),glass,.02)
cube('Tank',(0,-.04,.47),(.16,.43,.14),metal,.065)
cube('TankBand',(0,-.04,.55),(.18,.07,.04),rubber,.01)
for side in [-1,1]:
    limb('Thigh',(.12*side,-.33,.25),(.16*side,-.61,.2),.095,rubber)
    limb('Shin',(.16*side,-.61,.2),(.24*side,-.86,.24),.066,team)
    fin=cube('Fin'+str(side),(.25*side,-1.02,.21),(.21,.39,.045),team,.025)
    fin.rotation_euler.z=side*-.12
    limb('Arm',(.2*side,.2,.28),(.38*side,.32,.19),.068,team)
    limb('Forearm',(.38*side,.32,.19),(.38*side,.57,.14),.055,skin)
    ell('Glove',(.38*side,.59,.14),(.068,.078,.052),rubber)
limb('Stick',(.38,.62,.13),(.48,.91,.10),.024,white)
limb('Blade',(.48,.91,.10),(.7,.96,.10),.035,rubber)
cube('Stripe',(0,.21,.423),(.30,.065,.016),white,.006)
os.makedirs(os.path.join(ROOT,'art'),exist_ok=True)
bpy.ops.wm.save_as_mainfile(filepath=os.path.join(ROOT,'art','diver.blend'))
bpy.ops.export_scene.gltf(filepath=os.path.join(OUT,'diver.glb'),export_format='GLB',export_apply=True)
print('DIVE_ASSET_READY',os.path.getsize(os.path.join(OUT,'diver.glb')))
