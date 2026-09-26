import bpy, mathutils
top=max((o for o in bpy.data.objects if o.name.startswith('PT_seg')), key=lambda o:o.location.z)
tp=top.matrix_world @ mathutils.Vector((0,0,0.55))
offs=[(0.28,-0.2,-0.38),(0.02,-0.34,-0.42),(0.3,0.08,-0.45)]
cs=sorted([o for o in bpy.data.objects if o.name.startswith('PT_coco')],key=lambda o:o.name)
for o,d in zip(cs,offs):
    o.location=tp+mathutils.Vector(d); o.scale=(1.3,1.3,1.3)
bpy.ops.wm.save_mainfile()
