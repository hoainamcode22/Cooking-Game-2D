exec(open(r"E:\Game2\Cooking-Game-2D\Claude outputs\blender\h.py").read())
info={}
T={'apple':('Tree_Apple',0),'orange':('Tree_Orange',6),'lemon':('Tree_Lemon',12)}
isT=lambda o: o.name.startswith('AT_trunk') or o.name.startswith('AT_branch')
for k,(cn,x) in T.items():
    aim((x,0,2.5),7.5,512)
    names=set(o.name for o in bpy.data.collections[cn].objects)
    only(lambda o: o.name in names and isT(o)); rend(f'tree_{k}_trunk')
    only(lambda o: o.name in names and not isT(o)); rend(f'tree_{k}_canopy')
    info[k]={'base':px((x,0,0),512),'top':px((x,0,2.0),512)}
# dua
aim((18.5,0,2.6),7.5,512)
names=set(o.name for o in bpy.data.collections['Tree_Palm'].objects)
only(lambda o: o.name in names and o.name.startswith('PT_seg')); rend('tree_palm_trunk')
only(lambda o: o.name in names and not o.name.startswith('PT_seg')); rend('tree_palm_canopy')
top=max((o for o in bpy.data.objects if o.name.startswith('PT_seg')), key=lambda o:o.location.z)
info['palm']={'base':px((18,0,0),512),'top':px(tuple(top.matrix_world @ mathutils.Vector((0,0,0.55))),512)}
# hai dang
aim((0,12,3.8),9,768)
names=set(o.name for o in bpy.data.collections['Lighthouse'].objects)
only(lambda o: o.name in names); rend('lighthouse')
lamp=bpy.data.objects['LH_lamp']
info['lighthouse']={'base':px((0,12,0),768),'lamp':px(tuple(lamp.matrix_world @ mathutils.Vector((0,0,0.4))),768),'ortho':9,'res':768}
json.dump(info,open(OUT+"\\info1.json","w"),indent=1)
