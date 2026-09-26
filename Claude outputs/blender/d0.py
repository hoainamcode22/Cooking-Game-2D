import bpy, json, mathutils
out={}
for col in bpy.data.collections:
    if col.name not in ('Beach','Dock','Ship'): continue
    L=[]
    for o in col.all_objects:
        if o.type!='MESH': continue
        bb=[o.matrix_world@mathutils.Vector(c) for c in o.bound_box]
        mn=[min(v[i] for v in bb) for i in range(3)]; mx=[max(v[i] for v in bb) for i in range(3)]
        L.append([o.name,[round(x,2) for x in mn],[round(x,2) for x in mx],[m.name for m in o.data.materials if m]])
    out[col.name]=L
json.dump(out,open(r"E:\Game2\Cooking-Game-2D\Claude outputs\blender\d0.json","w"),indent=0)
