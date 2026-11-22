"""Project-owned, metre-scale tool meshes; Blender background authoring, no paid services."""
import bpy, bmesh, math, sys
from pathlib import Path
from mathutils import Vector
ROOT=Path(__file__).resolve().parents[2]
OUT=ROOT/'Assets/JustOneMorePeel/Art/Tools'
OUT.mkdir(parents=True,exist_ok=True)
bpy.ops.object.select_all(action='SELECT'); bpy.ops.object.delete(use_global=False)
bpy.context.scene.unit_settings.system='METRIC'
M={}
bpy.context.preferences.filepaths.save_version = 0
requested = set(sys.argv[sys.argv.index('--') + 1:]) if '--' in sys.argv else set()
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
    if requested and name not in requested:
        bpy.ops.object.delete(use_global=False)
        return
    bpy.ops.wm.save_as_mainfile(filepath=str(Path(__file__).resolve().parent/(name+'.blend')),copy=True)
    bpy.ops.export_scene.fbx(filepath=str(OUT/(name+'.fbx')),use_selection=True,object_types={'MESH'},axis_forward='-Z',axis_up='Y',add_leaf_bones=False,bake_anim=False)
    bpy.ops.object.delete(use_global=False)
# Expired plastic membership card: rounded physical outline and mapped printed face.
m=bpy.data.materials.new('CardPlastic');m.diffuse_color=(.75,.73,.63,1);M['CardPlastic']=m
outline=[]
for cx,cy,start in [(.039,.053,0),(-.039,.053,90),(-.039,.007,180),(.039,.007,270)]:
    if start==90:
        # The yard office clips one corner when a pass expires.
        outline.extend([(-.035,.057),(-.043,.049)])
        continue
    for step in range(9):
        a=math.radians(start+step*90/8);outline.append((cx+.004*math.cos(a),cy+.004*math.sin(a)))
ob=plate('Expired plastic card',outline,.0008,'CardPlastic',bevel=.0002)
uv=ob.data.uv_layers.new(name='Printed card')
for polygon in ob.data.polygons:
    for loop in polygon.loop_indices:
        v=ob.data.vertices[ob.data.loops[loop].vertex_index].co
        u=(v.x+.043)/.086
        front=polygon.normal.z<0
        uv.data[loop].uv=((1-u)*.5 if front else .5+u*.5,(v.y-.003)/.054)
export('Card')
# Forged filling knife: flared blade, ground edge, neck, moulded contoured grip, metal rivets.
def blade_section(name, stations, material):
    verts=[]; faces=[]
    # A raised tang feathers out into spring steel; both faces taper into the working edge.
    for i,(y,width,side,spine) in enumerate(stations):
        for x,z in [(-width,-side),(0,-spine),(width,-side),(width,side),(0,spine),(-width,side)]:
            verts.append((x,y,z))
        if i:
            for j in range(6):
                a=(i-1)*6+j; b=(i-1)*6+(j+1)%6
                faces.append((a,b,b+6,a+6))
    faces.extend([tuple(range(5,-1,-1)),tuple(range(len(verts)-6,len(verts)))])
    return mesh(name,verts,faces,material)
grind=(.186,.088,.0010,.0013)
blade_section('Forged blade',[(-.015,.013,.002,.0038),(.028,.018,.0019,.0042),
    (.070,.036,.0016,.0038),(.163,.085,.0011,.0018),(.177,.088,.0011,.0015),grind],'Steel')
blade_section('Ground cutting bevel',[grind,(.198,.0875,.00018,.00022)],'Edge')
lathe('Contoured rubber handle',[(-.205,.018,.012),(-.2,.024,.017),(-.17,.027,.018),(-.105,.022,.016),(-.045,.027,.018),(-.025,.021,.016),(-.019,.018,.012)],'Grip')
lathe('Steel ferrule',[(-.028,.022,.016),(-.008,.022,.016),(.002,.016,.011)],'Steel')
for y in [-.17,-.068]:
    ob=lathe('Flush handle rivet',[(-.001,.004,.004),(.001,.004,.004)],'Steel')
    ob.rotation_euler[0]=math.pi/2;ob.location=(0,y,-.018)
for y in [-.13,-.12,-.11,-.10]:
    lathe('Moulded grip rib',[(y,.023,.0165),(y+.002,.023,.0165)],'Rubber')
export('Scraper')
# Compact wallpaper steamer: oval reservoir, seam, cap, swept handle and perforated shoe.
lathe('Oval enamel reservoir',[(-.157,.029,.023),(-.151,.044,.032),(-.135,.052,.038),
    (-.074,.056,.042),(.017,.061,.046),(.073,.057,.042),(.100,.046,.035),
    (.116,.035,.029),(.126,.027,.024)],'Enamel',center=(0,0,-.025))
lathe('Reservoir seam',[(.015,.061,.046),(.018,.062,.047),(.021,.061,.046)],'Rubber',center=(0,0,-.025))
# Continuous oval grip with two curved returns into the shell and clearance for the glove.
grip_path=[(-.138,-.021,.023,.012),(-.151,.012,.021,.013),(-.158,.044,.020,.014),
    (-.154,.067,.023,.015),(-.143,.077,.025,.016),(-.120,.080,.025,.017),
    (-.064,.078,.022,.016),(-.019,.071,.024,.017),(.005,.064,.026,.018),
    (.024,.048,.027,.019),(.034,.026,.027,.018),(.037,.000,.024,.017)]
verts=[]; faces=[]; sides=24
for i,(y,z,rx,r) in enumerate(grip_path):
    prev=grip_path[max(0,i-1)]; following=grip_path[min(i+1,len(grip_path)-1)]
    dy=following[0]-prev[0]; dz=following[1]-prev[1]; length=math.hypot(dy,dz)
    for j in range(sides):
        angle=math.tau*j/sides; offset=r*math.sin(angle)/length
        verts.append((rx*math.cos(angle),y-dz*offset,z+dy*offset))
        if i:
            a=(i-1)*sides+j; b=(i-1)*sides+(j+1)%sides
            faces.append((a,b,b+sides,a+sides))
faces.extend([tuple(reversed(range(sides))),tuple(range(len(verts)-sides,len(verts)))])
handle=mesh('Swept rear handle',verts,faces,'Grip')
for p in handle.data.polygons:p.use_smooth=len(p.vertices)==4
lathe('Filler cap',[(.105,.013,.013),(.115,.016,.016),(.122,.016,.016),(.125,.014,.014)],'Rubber',center=(.014,0,-.046))
lathe('Nozzle neck',[(.13,.013,.013),(.19,.013,.013)],'Steel',center=(0,0,-.015))
lathe('Broad steam shoe',[(.18,.028,.016),(.193,.079,.025),(.205,.079,.025),(.211,.073,.023)],'Steel',center=(0,0,-.015))
for x in [-.05,-.025,0,.025,.05]:
    lathe('Steam port',[(.211,.003,.003),(.212,.003,.003)],'Rubber',center=(x,0,-.015),segments=12)
plate('Brass trigger',[(-.012,-.059),(.012,-.059),(.014,-.034),(.010,-.024),(-.010,-.024),(-.014,-.034)],.014,'Edge',z=.060,bevel=.003)
export('Steamer')
print('TOOLS_EXPORTED:', ', '.join(sorted(requested)) if requested else 'Card, Scraper, Steamer')
