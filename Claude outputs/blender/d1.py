import bpy,json
o={}
for n in ['HullBlue','LH_Red','Sand','SandWet','Foam','DockWood','Shell']:
    m=bpy.data.materials.get(n)
    if not m: o[n]=None; continue
    L=[]
    for nd in m.node_tree.nodes:
        ins=[]
        for i in nd.inputs:
            if hasattr(i,'default_value') and not i.is_linked:
                try: v=list(i.default_value)
                except: v=i.default_value
                ins.append([i.identifier,i.name,v if not isinstance(v,float) else round(v,3)])
        L.append([nd.name,nd.type,getattr(nd,'blend_type',None),getattr(nd,'data_type',None),ins])
    o[n]=L
json.dump(o,open(r"E:\Game2\Cooking-Game-2D\Claude outputs\blender\d1.json","w"),default=str)
