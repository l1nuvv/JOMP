"""Project-owned, metre-scale tool meshes; Blender background authoring, no paid services."""
import bpy, bmesh, math
from pathlib import Path
from mathutils import Vector
ROOT=Path(__file__).resolve().parents[2]
OUT=ROOT/'Assets/JustOneMorePeel/Art/Tools'
OUT.mkdir(parents=True,exist_ok=True)
bpy.ops.object.select_all(action='SELECT'); bpy.ops.object.delete(use_global=False)
bpy.context.scene.unit_settings.system='METRIC'
M={}
for name,color,metal,rough in [('Grip',(.055,.16,.15,1),0,.8),('Steel',(.48,.5,.46,1),.7,.4),('Edge',(.67,.68,.62,1),.8,.28),('Enamel',(.64,.56,.36,1),.15,.65),('Rubber',(.033,.041,.037,1),0,.9)]:
    m=bpy.data.materials.new(name);m.diffuse_color=color;m.use_nodes=True
    bs=m.node_tree.nodes.get('Principled BSDF');bs.inputs['Base Color'].default_value=color;bs.inputs['Metallic'].default_value=metal;bs.inputs['Roughness'].default_value=rough;M[name]=m
def mesh(name,verts,faces,mat,bevel=0):
    data=bpy.data.meshes.new(name);data.from_pydata(verts,[],faces);data.update()
    bm=bmesh.new();bm.from_mesh(data);bmesh.ops.recalc_face_normals(bm,faces=bm.faces);bm.to_mesh(data);bm.free()
    ob=bpy.data.objects.new(name,data);bpy.context.collection.objects.link(ob);ob.data.materials.append(M[mat])
    if bevel:
        mod=ob.modifiers.new('Soft manufactured edges','BEVEL');mod.width=bevel;mod.segments=3
        bpy.context.view_layer.objects.active=ob;bpy.ops.object.modifier_apply(modifier=mod.name)
        mod=ob.modifiers.new('Weighted normals','WEIGHTED_NORMAL');mod.keep_sharp=True
        bpy.ops.object.modifier_apply(modifier=mod.name)
    return ob
def plate(name,outline,depth,mat,z=0,bevel=.001):
    n=len(outline);v=[(x,y,z+d) for d in [-depth/2,depth/2] for x,y in outline]
    f=[tuple(reversed(range(n))),tuple(range(n,2*n))]+[(i,(i+1)%n,(i+1)%n+n,i+n) for i in range(n)]
    return mesh(name,v,f,mat,bevel)
def lathe(name,profiles,mat,center=(0,0,0),segments=32):
    v=[]
    for y,rx,rz in profiles:
        for i in range(segments):
            a=i*math.tau/segments;v.append((center[0]+math.cos(a)*rx,center[1]+y,center[2]+math.sin(a)*rz))
    f=[tuple(reversed(range(segments))),tuple((len(profiles)-1)*segments+i for i in range(segments))]
    for j in range(len(profiles)-1):
        for i in range(segments):
            a=j*segments+i;b=j*segments+(i+1)%segments;f.append((a,b,b+segments,a+segments))
    ob=mesh(name,v,f,mat)
    for p in ob.data.polygons:p.use_smooth=len(p.vertices)==4
    return ob
def export(name):
    objects=list(bpy.context.scene.objects)
    for ob in objects:ob.select_set(True)
    bpy.ops.wm.save_as_mainfile(filepath=str(Path(__file__).resolve().parent/(name+'.blend')),copy=True)
    bpy.ops.export_scene.fbx(filepath=str(OUT/(name+'.fbx')),use_selection=True,object_types={'MESH'},axis_forward='-Z',axis_up='Y',add_leaf_bones=False,bake_anim=False)
    bpy.ops.object.delete(use_global=False)
# Expired plastic membership card: rounded physical outline and mapped printed face.
m=bpy.data.materials.new('CardPlastic');m.diffuse_color=(.75,.73,.63,1);M['CardPlastic']=m
outline=[]
for cx,cy,start in [(.039,.053,0),(-.039,.053,90),(-.039,.007,180),(.039,.007,270)]:
    for step in range(9):
        a=math.radians(start+step*90/8);outline.append((cx+.004*math.cos(a),cy+.004*math.sin(a)))
ob=plate('Expired plastic card',outline,.0008,'CardPlastic',bevel=.0002)
uv=ob.data.uv_layers.new(name='Printed card')
for polygon in ob.data.polygons:
    for loop in polygon.loop_indices:
        v=ob.data.vertices[ob.data.loops[loop].vertex_index].co
        uv.data[loop].uv=((v.x+.043)/.086,(v.y-.003)/.054)
export('Card')
# Forged filling knife: flared blade, ground edge, neck, moulded contoured grip, metal rivets.
plate('Forged blade',[(-.018,.008),(.018,.008),(.038,.07),(.088,.173),(.087,.198),(-.087,.198),(-.088,.173),(-.038,.07)],.003,'Steel',bevel=.0007)
plate('Ground cutting bevel',[(-.087,.187),(.087,.187),(.087,.199),(-.087,.199)],.001,'Edge',z=-.0016,bevel=.0002)
lathe('Contoured rubber handle',[(-.205,.018,.012),(-.2,.024,.017),(-.17,.027,.018),(-.105,.022,.016),(-.045,.027,.018),(-.025,.021,.016),(-.019,.018,.012)],'Grip')
lathe('Steel ferrule',[(-.028,.022,.016),(-.008,.022,.016),(.002,.016,.011)],'Steel')
for y in [-.17,-.068]:
    ob=lathe('Flush handle rivet',[(-.001,.004,.004),(.001,.004,.004)],'Steel')
    ob.rotation_euler[0]=math.pi/2;ob.location=(0,y,-.018)
for y in [-.13,-.12,-.11,-.10]:
    lathe('Moulded grip rib',[(y,.023,.0165),(y+.002,.023,.0165)],'Rubber')
export('Scraper')
# Compact wallpaper steamer: oval reservoir, seam, cap, swept handle and perforated shoe.
lathe('Oval enamel reservoir',[(-.07,.034,.026),(-.06,.054,.041),(-.03,.061,.046),(.085,.064,.045),(.115,.055,.038),(.125,.04,.03)],'Enamel')
lathe('Reservoir seam',[(.017,.064,.047),(.021,.064,.047)],'Rubber')
plate('Swept rear handle',[(-.026,-.165),(.026,-.165),(.03,-.07),(.018,.018),(-.018,.018),(-.027,-.06)],.042,'Grip',z=.065,bevel=.005)
lathe('Filler cap',[(.123,.022,.022),(.133,.023,.023),(.139,.02,.02)],'Rubber')
lathe('Nozzle neck',[(.13,.013,.013),(.19,.013,.013)],'Steel',center=(0,0,-.015))
lathe('Broad steam shoe',[(.18,.028,.016),(.193,.079,.025),(.205,.079,.025),(.211,.073,.023)],'Steel',center=(0,0,-.015))
for x in [-.05,-.025,0,.025,.05]:
    lathe('Steam port',[(.211,.003,.003),(.212,.003,.003)],'Rubber',center=(x,0,-.015),segments=12)
plate('Brass trigger',[(-.009,-.069),(.009,-.069),(.009,-.02),(-.009,-.02)],.008,'Edge',z=.027,bevel=.002)
export('Steamer')
print('TOOLS_EXPORTED: Card.fbx, Scraper.fbx, Steamer.fbx; metres, separated material slots')
