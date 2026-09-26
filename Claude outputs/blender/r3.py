exec(open(r"E:\Game2\Cooking-Game-2D\Claude outputs\blender\h.py").read())
import bpy, math, json, os, mathutils
N=32; RES=480; ORTHO=6.0
PIV=mathutils.Vector((34.0,12.0,0.35))
col=bpy.data.collections['Ship']
ship=[o for o in col.all_objects]
emp=bpy.data.objects.new("SHIP_PIVOT",None); sc.collection.objects.link(emp); emp.location=PIV
bpy.context.view_layer.update()
for o in ship:
    if o.parent is None or o.parent not in ship:
        mw=o.matrix_world.copy(); o.parent=emp; o.matrix_world=mw
names={o.name for o in ship if o.type=='MESH'}
only(lambda x: x.name in names)
aim((34,12,1.1),ORTHO,RES)
def setc(mat,c):
    for nd in bpy.data.materials[mat].node_tree.nodes:
        if nd.type=='MIX':
            for i in nd.inputs:
                if i.identifier=='A_Color': i.default_value=c
orig_hull=list([i for nd in bpy.data.materials['HullBlue'].node_tree.nodes if nd.type=='MIX' for i in nd.inputs if i.identifier=='A_Color'][0].default_value)
orig_roof=list([i for nd in bpy.data.materials['LH_Red'].node_tree.nodes if nd.type=='MIX' for i in nd.inputs if i.identifier=='A_Color'][0].default_value)
info={"N":N,"res":RES,"pivot":px(PIV,RES),"dirs":[]}
for colr in ['blue','red']:
    if colr=='red':
        setc('HullBlue',(0.72,0.05,0.03,1.0)); setc('LH_Red',(0.03,0.16,0.55,1.0))
    d=OUT+"\\ship_"+colr; os.makedirs(d,exist_ok=True)
    for i in range(N):
        th=math.radians(i*360.0/N)
        X=(math.cos(th)/0.7071-math.sin(th)/0.35355)/2; Y=(math.cos(th)/0.7071+math.sin(th)/0.35355)/2
        phi=math.atan2(Y,X)
        emp.rotation_euler=(0,0,phi); bpy.context.view_layer.update()
        if colr=='blue':
            a=px(PIV,RES); b=px(PIV+mathutils.Vector((math.cos(phi),math.sin(phi),0))*2,RES)
            info["dirs"].append([i,round(math.degrees(math.atan2(-(b[1]-a[1]),b[0]-a[0]))%360,2)])
        sc.render.filepath=d+"\\dir_%02d.png"%i; bpy.ops.render.render(write_still=True)
json.dump(info,open(OUT+"\\ship_info.json","w"))
print("DONE")
