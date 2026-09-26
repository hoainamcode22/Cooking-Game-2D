import bpy, mathutils, os, json
from bpy_extras.object_utils import world_to_camera_view
sc=bpy.context.scene; cam=sc.camera
dirv=mathutils.Vector((-0.612,0.612,-0.5)).normalized()
OUT=r"E:\Game2\Cooking-Game-2D\Claude outputs\blender\game"
os.makedirs(OUT,exist_ok=True)
def aim(t,s,res):
    sc.render.resolution_x=res; sc.render.resolution_y=res
    sc.render.line_thickness=max(1.2,3.0*res/1024); bpy.data.linestyles[0].thickness=sc.render.line_thickness
    cam.data.ortho_scale=s; cam.location=mathutils.Vector(t)-dirv*40
    bpy.context.view_layer.update()
def px(p,res):
    v=world_to_camera_view(sc,cam,mathutils.Vector(p)); return [round(v.x*res,2),round((1-v.y)*res,2)]
def only(pred):
    for o in bpy.data.objects:
        if o.type=='MESH': o.hide_render=not pred(o)
    for c in sc.collection.children: c.hide_render=False
def rend(name):
    sc.render.filepath=OUT+"\\"+name+".png"; bpy.ops.render.render(write_still=True)
