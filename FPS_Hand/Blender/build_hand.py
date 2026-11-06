import bpy, bmesh, math
from pathlib import Path
from mathutils import Vector
out=Path(__file__).resolve().parent.parent
bpy.ops.wm.read_factory_settings(use_empty=True)
sc=bpy.context.scene
sc.unit_settings.system='METRIC'
V=[]; F=[]; W=[]
def v(p,w):
    V.append(tuple(c*.01 for c in p)); W.append(w); return len(V)-1
def bridge(a,b):
    F.extend((a[i],a[(i+1)%len(a)],b[(i+1)%len(a)],b[i]) for i in range(len(a)))
def mesh(name,color,rough):
    me=bpy.data.meshes.new(name); me.from_pydata(V,[],F)
    bm=bmesh.new(); bm.from_mesh(me); bmesh.ops.recalc_face_normals(bm,faces=list(bm.faces)); bm.to_mesh(me); bm.free()
    ob=bpy.data.objects.new(name,me); sc.collection.objects.link(ob)
    mat=bpy.data.materials.new(name_to_mat[name]); mat.diffuse_color=(*color,1); mat.use_nodes=True
    bs=mat.node_tree.nodes.get('Principled BSDF'); bs.inputs['Base Color'].default_value=(*color,1); bs.inputs['Roughness'].default_value=rough
    me.materials.append(mat)
    for p in me.polygons: p.use_smooth=True
    for i,weights in enumerate(W):
        for bone,weight in weights.items():
            if weight: (ob.vertex_groups.get(bone) or ob.vertex_groups.new(name=bone)).add([i],weight,'REPLACE')
    bpy.context.view_layer.objects.active=ob; ob.select_set(True)
    mod=ob.modifiers.new('Rounded quad surface','SUBSURF'); mod.levels=2
    bpy.ops.object.modifier_apply(modifier=mod.name)
    bpy.ops.object.mode_set(mode='EDIT'); bpy.ops.mesh.select_all(action='SELECT'); bpy.ops.uv.smart_project(island_margin=.012); bpy.ops.object.mode_set(mode='OBJECT'); ob.select_set(False)
    return ob
name_to_mat={'Hand_Glove':'M_Glove_Black','Shirt_Cuff':'M_Cuff_Cream','Suit_Sleeve':'M_Sleeve_Gray'}
B={'Root':((0,-22,0),(0,-20,0),None),'Forearm':((0,-22,0),(0,0,0),'Root'),'Wrist':((0,0,0),(0,8,0),'Forearm')}
G={'Wrist':1}
rows=[]
for y,width,height,offset in [(-3.4,3,1.3,0),(-1.5,3.05,1.36,0),(1.2,3.55,1.6,-.1),(4.8,4.5,1.85,-.15),(7.2,4.55,1.63,-.06),(8.8,4.35,1.38,0)]:
    row=[]
    for s in (1,-1):
        row.append([v((offset+width*(i/2-1),y-(max(0,y-6)*(.10*abs(i-1)+.08*max(0,i-2))),s*height*(.78 if i in (0,4) else 1)),{'Forearm':1} if y<-1 else G) for i in range(5)])
    rows.append(row)
for j,((a,b),(c,d)) in enumerate(zip(rows,rows[1:])):
    for i in range(4): F.extend([(a[i],a[i+1],c[i+1],c[i]),(b[i],d[i],d[i+1],b[i+1])])
    F.append((a[4],b[4],d[4],c[4]))
    if j!=2: F.append((a[0],c[0],d[0],b[0]))
a,b=rows[0]
for i in range(4): F.append((a[i],b[i],b[i+1],a[i+1]))
def digit(name,loop,points,radii):
    pts=[Vector(p) for p in points]
    names=[f'{name}_{j+1:02}' for j in range(3)]
    for j,bone in enumerate(names): B[bone]=(pts[j],pts[j+1],names[j-1] if j else 'Wrist')
    for j in range(3):
        direction=(pts[j+1]-pts[j]).normalized()
        axis=Vector((0,0,1)); side=direction.cross(axis).normalized()
        for t in ((.10,.36,.72,.91) if j<2 else (.1,.42,.75,.94)):
            r=radii[j]*(1-.14*t)*(1 if j<2 else math.sqrt(max(.06,1-t**4)))
            center=pts[j].lerp(pts[j+1],t)
            weights={names[j]:1}
            if t<.2: weights={names[j]:.70,(names[j-1] if j else 'Wrist'):.30}
            if t>.8 and j<2: weights={names[j]:.70,names[j+1]:.30}
            ring=[v(center+side*r*s+axis*r*.82*z,weights) for s,z in ([(-1,1),(1,1),(1,-1),(-1,-1)] )]
            bridge(loop,ring); loop=ring
    F.append(tuple(loop))
a,b=rows[-1]
for i,(name,x,y,L,r) in enumerate([('Index',-3.25,8.35,7.6,1.15),('Middle',-1.1,8.7,8.35,1.17),('Ring',1.1,8.4,7.75,1.10),('Pinky',3.25,7.85,6.1,.98)]):
    spread=[-.73,-.18,.30,.91][i]
    digit(name,[a[i],a[i+1],b[i+1],b[i]],[(x+spread*t,y+L*t,-.2-1.3*t*t) for t in (0,.46,.76,1)],(r,r*.95,r*.89))
a,b=rows[2]; c,d=rows[3]
digit('Thumb',[a[0],c[0],d[0],b[0]],[(-3.15,2.3,-.08),(-5.5,4.65,-.65),(-6.85,6.3,-.9),(-7.6,8.18,-1.25)],(1.58,1.37,1.18))
objects=[mesh('Hand_Glove',(.010,.009,.008),.70)]
def cloth(name,profile,color):
    V.clear();F.clear();W.clear(); prev=None
    for y,x,z in profile:
        ring=[]
        for i in range(24):
            t=i*math.tau/24
            fold=1+.018*math.sin(t*3+y*.28)+.012*math.cos(t*5-y*.42)
            ring.append(v((x*math.cos(t)*fold,y,z*math.sin(t)*fold),{'Forearm':1}))
        if prev is None: F.append(tuple(reversed(ring)))
        else: bridge(prev,ring)
        prev=ring
    F.append(tuple(prev))
    objects.append(mesh(name,color,.79))
cloth('Shirt_Cuff',[(-6.8,3.96,2.49),(-6.65,4.15,2.65),(-6.3,4.2,2.68),(-2.9,3.65,2.35),(-2.62,3.61,2.30),(-2.49,3.42,2.13)],(.77,.66,.48))
cloth('Suit_Sleeve',[(-23,5.75,4.28),(-22.5,5.76,4.28),(-19,5.54,4.12),(-15,5.01,3.68),(-11,4.61,3.2),(-7,4.24,2.77),(-6.8,4.21,2.76),(-6.65,4.02,2.58)],(.045,.040,.037))
B['GripSocket']=((0,7,-3.9),(0,10,-3.9),'Wrist')
arm=bpy.data.armatures.new('Right Hand Skeleton')
rig=bpy.data.objects.new('FPS_Hand',arm); sc.collection.objects.link(rig); rig.show_in_front=True
bpy.context.view_layer.objects.active=rig; rig.select_set(True); bpy.ops.object.mode_set(mode='EDIT')
for name,(head,tail,parent) in B.items():
    bone=arm.edit_bones.new(name); bone.head=Vector(head)*.01; bone.tail=Vector(tail)*.01
    if parent: bone.parent=arm.edit_bones[parent]
bpy.ops.object.mode_set(mode='OBJECT')
for ob in objects:
    ob.parent=rig; mod=ob.modifiers.new('Skeleton','ARMATURE'); mod.object=rig; mod.use_deform_preserve_volume=False; ob.select_set(True)
sc.render.fps=30
def pose(curl,reach):
    for p in rig.pose.bones:
        p.rotation_mode='XYZ'; p.rotation_euler=(0,0,0); p.location=(0,0,0)
        if p.name.startswith(('Index','Middle','Ring','Pinky')):
            j=int(p.name[-2:])-1; p.rotation_euler.x=-curl*(.96,1.26,.69)[j]
        if p.name.startswith('Thumb'): p.rotation_euler.x=-curl*.48; p.rotation_euler.z=-curl*(.71 if p.name.endswith('01') else .08)
    rig.pose.bones['Forearm'].location.y=reach
    rig.pose.bones['Wrist'].rotation_euler.x=.10-.30*reach
rig.animation_data_create()
for name,keys in {
    'Idle':[(1,.24,0),(31,.29,.003),(61,.24,0)],
    'Reach':[(1,.24,0),(18,.05,.10)],
    'Grab':[(1,.05,.10),(15,.73,.10)],
    'Hold':[(1,.73,.10),(31,.75,.102),(61,.73,.10)],
    'Release':[(1,.73,.10),(15,.05,.10)],
    'Return':[(1,.05,.10),(20,.24,0)]
}.items():
    act=bpy.data.actions.new('Hand_'+name); act.use_fake_user=True; rig.animation_data.action=act
    for frame,curl,reach in keys:
        pose(curl,reach)
        for p in rig.pose.bones:
            p.keyframe_insert('rotation_euler',frame=frame)
            p.keyframe_insert('location',frame=frame)
rig.animation_data.action=None; pose(0,0)
rig.pose.bones['Wrist'].rotation_euler.x=0
bpy.ops.export_scene.fbx(filepath=str(out/'Export/FPS_Hand.fbx'),use_selection=True,add_leaf_bones=False,bake_anim_use_all_actions=True,bake_anim_use_nla_strips=False,use_tspace=True,axis_forward='-Z',axis_up='Y')
def point(ob,target): ob.rotation_euler=(Vector(target)-ob.location).to_track_quat('-Z','Y').to_euler()
def camera(name,pos,target):
    ob=bpy.data.objects.new(name,bpy.data.cameras.new(name)); sc.collection.objects.link(ob)
    ob.location=pos; point(ob,target); ob.data.lens=42; return ob
cam=camera('Pose review',(.25,.13,.62),(0,-.028,0))
cam.rotation_euler.rotate_axis('Z',math.pi)
sc.camera=cam
sc.world=bpy.data.worlds.new('Studio'); sc.world.color=(.23,.23,.23)
for pos,energy,size in [((-.28,.22,.44),14,.32),((.27,-.28,.26),8,.27),((-.1,-.26,.46),7,.36),((.18,.25,-.34),7,.32)]:
    ld=bpy.data.lights.new('Softbox','AREA'); ld.energy=energy; ld.shape='DISK'; ld.size=size
    lo=bpy.data.objects.new('Softbox',ld); sc.collection.objects.link(lo); lo.location=pos; point(lo,(0,0,0))
sc.render.engine='CYCLES'; sc.cycles.samples=12
sc.render.resolution_x=740; sc.render.resolution_y=600; sc.render.resolution_percentage=100
rig.animation_data.action=bpy.data.actions['Hand_Idle']; sc.frame_set(1)
bpy.context.preferences.filepaths.save_version=0
bpy.ops.wm.save_as_mainfile(filepath=str(out/'Blender/FPS_Hand_Source.blend'))
rig.animation_data.action=None
for name,curl,pos in [('Relaxed',.24,(.25,.13,.62)),('Open',0,(.25,.13,.62)),('Closed',.98,(.29,.16,-.52)),('Holding',.73,(.29,.16,-.52))]:
    pose(curl,0); cam.location=pos; point(cam,(0,-.028,0)); cam.rotation_euler.rotate_axis("Z",math.pi)
    sc.render.filepath=str(out/f'Previews/{name}.png'); bpy.ops.render.render(write_still=True)


pose(.24,0)
bpy.context.view_layer.update()
cam.location=(.12,.09,.67); point(cam,(0,-.028,0)); cam.rotation_euler.rotate_axis('Z',math.pi)
direction=(cam.location-Vector((0,0,0))).normalized()
overlay=bpy.data.materials.new('Preview skeleton'); overlay.diffuse_color=(.12,.8,.75,1)
overlay.use_nodes=True
bs=overlay.node_tree.nodes.get('Principled BSDF')
bs.inputs['Emission Color'].default_value=(.12,.8,.75,1)
bs.inputs['Emission Strength'].default_value=1
for bone in rig.pose.bones:
    data=bpy.data.curves.new(bone.name,'CURVE'); data.dimensions='3D'; data.bevel_depth=.00085
    spline=data.splines.new('POLY'); spline.points.add(1)
    for pt,pos in zip(spline.points,[bone.head,bone.tail]):
        pt.co=(*(pos+direction*.06),1)
    ob=bpy.data.objects.new(bone.name,data); sc.collection.objects.link(ob); data.materials.append(overlay)
sc.render.filepath=str(out/'Previews/Skeleton.png'); bpy.ops.render.render(write_still=True)
for ob in objects:
    ob.data.calc_loop_triangles()
    bm=bmesh.new(); bm.from_mesh(ob.data)
    print('MESH_CHECK',ob.name,len(ob.data.loop_triangles), 'nonmanifold',sum(not e.is_manifold for e in bm.edges), 'unweighted',sum(not v.groups for v in ob.data.vertices), 'uv',len(ob.data.uv_layers))
    bm.free()
