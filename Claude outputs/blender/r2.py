exec(open(r"E:\Game2\Cooking-Game-2D\Claude outputs\blender\h.py").read())
# qua roi: 1 qua / loai, cam chup gan
import bpy
info={}
for k,mat in [('apple','Apple'),('orange','Orange'),('lemon','Lemon'),('palm','Coconut')]:
    bpy.ops.mesh.primitive_ico_sphere_add(subdivisions=3,radius=0.2,location=(50,50,0.2))
    o=bpy.context.active_object; o.data.materials.append(bpy.data.materials[mat]); bpy.ops.object.shade_smooth()
    h=None
    if k!='palm':
        bpy.ops.mesh.primitive_ico_sphere_add(subdivisions=1,radius=0.05,location=(50-0.06,50-0.14,0.3)); h=bpy.context.active_object; h.data.materials.append(bpy.data.materials['Hilite'])
    aim((50,50,0.2),0.7,96)
    keep={o.name}|({h.name} if h else set())
    only(lambda x: x.name in keep); rend(f'fruit_{k}')
    bpy.data.objects.remove(o); 
    if h: bpy.data.objects.remove(h)
