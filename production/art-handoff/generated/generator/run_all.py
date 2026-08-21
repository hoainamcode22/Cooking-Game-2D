import math, sys, os; sys.path.insert(0,'.')
from crops import *
from pg2 import *
from allcrops import *

def orbs(pts,g,oc): return [(d_orb(x,y,r),g,oc) for x,y,r in pts]
def specs(pts,op=.75): return "".join(spec(x,y,r,op) for x,y,r in pts)
def calys(pts,f=.82): return "".join(calyx(x,y-r*f,r*.64) for x,y,r in pts)

# ---- bush fruit configs ----
def mk_bush(f3,f4,g3,g4,oc3,oc4,fl3=(),fl4=(),flg='gGold',pod=False):
    def fruit(s):
        if s==3: return ([(d_pod(x,y,r*2.4,r*0.9,72),g3,oc3) for x,y,r in f3] if pod else orbs(f3,g3,oc3))
        if s==4: return ([(d_pod(x,y,r*2.4,r*0.9,72),g4,oc4) for x,y,r in f4] if pod else orbs(f4,g4,oc4))
        return []
    def flower(s):
        if s==3: return [flower5(x,y,r,flg) for x,y,r in fl3]
        if s==4: return [flower5(x,y,r,flg) for x,y,r in fl4]
        return []
    def det(s):
        if pod: return []
        if s==3: return [calys(f3)+specs(f3,.42)]
        if s==4: return [calys(f4)+specs(f4,.78)]
        return []
    return fruit,flower,det

TOM_F3=[(CX-70,330,32),(CX+74,276,28)]
TOM_F4=[(CX-84,336,50),(CX+86,262,45),(CX-34,176,39),(CX+52,128,31)]
LEM_F3=[(CX-66,326,26),(CX+72,282,23)]
LEM_F4=[(CX-80,330,38),(CX+84,268,35),(CX-28,182,31),(CX+46,140,27)]
OT_F3=[(CX-58,318,17),(CX+66,286,15)]
OT_F4=[(CX-74,322,24),(CX+80,266,22),(CX-26,196,20),(CX+44,150,18)]

tf,tfl,td = mk_bush(TOM_F3,TOM_F4,'gGrn','gRed','#3B5220','#5C1410',
                    [(CX+60,356,23),(CX-54,232,21),(CX+30,196,19)],[(CX-66,112,19)])
lf,lfl,ld = mk_bush(LEM_F3,LEM_F4,'gGrn','gLem','#3B5220','#7A6418',
                    [(CX+58,352,20),(CX-50,228,19)],[(CX-62,118,18)],'gCream')
of,ofl,od = mk_bush(OT_F3,OT_F4,'gGrn','gRed','#3B5220','#5C1410',
                    [(CX+56,350,17),(CX-48,224,16)],[(CX-58,116,15)],'gCream',pod=True)

CROPS=[
 ("rice","Lua",       grass, dict(nb=[0,0,9,12,15], bl=0.86, bw=30, bend2=0.22, fan=16, head=rice_head, ow=12)),
 ("bapcai","Bap Cai", head,  dict()),
 ("ngo","Ngo",        grass, dict(nb=[0,0,7,9,11], bl=0.90, bw=42, bend2=0.20, fan=17, head=corn_head, ow=13)),
 ("cachua","Ca Chua", build, dict(serr=6,lobe=0.10,fruit=tf,flower=tfl,_det=td)),
 ("carot","Ca Rot",   root,  dict(kind='carrot', ow=12)),
 ("khoaitay","Khoai Tay", root, dict(kind='potato')),
 ("nam","Nam",        fungus,dict()),
 ("mia","Mia",        cane,  dict(ow=14)),
 ("chanh","Chanh",    build, dict(serr=4,lobe=0.04,lw=0.46,fruit=lf,flower=lfl,_det=ld)),
 ("ot","Ot",          build, dict(serr=3,lobe=0.02,lw=0.34,leaflen=[(0,0),(0,0),(112,60),(128,62),(140,64)],fruit=of,flower=ofl,_det=od)),
 ("tieu","Tieu",      vine,  dict()),
]
LBL=["1 vua gieo","2 mam","3 cay non","4 truong thanh","5 CHIN"]

def cell_svg(slug, fn, cfg, s, uid):
    sh,dt = fn(s,cfg)
    if '_det' in cfg: dt = dt + cfg['_det'](s)
    return render_cell(sh,dt,uid,cfg.get('ow'))

def strip(slug,name,fn,cfg,out):
    W=CW*5; H=CH+78; defs=[];body=[]
    for s in range(5):
        df,bd = cell_svg(slug,fn,cfg,s,f"{slug}{s}")
        defs.append(df); body.append(f'<g transform="translate({s*CW},0)">{bd}</g>')
    o=[f'<svg xmlns="http://www.w3.org/2000/svg" width="{W}" height="{H}" viewBox="0 0 {W} {H}">',
       f'<defs>{DEFS_CORE}{EXTRA_DEFS}{"".join(defs)}</defs>',
       f'<rect width="{W}" height="{H}" fill="#FBF3E2"/>']
    for i in range(1,5): o.append(f'<line x1="{i*CW}" y1="8" x2="{i*CW}" y2="{CH-4}" stroke="#E2D0AC" stroke-width="2" stroke-dasharray="8 8"/>')
    o.append(f'<line x1="0" y1="{BY}" x2="{W}" y2="{BY}" stroke="#C9A96E" stroke-width="3" stroke-dasharray="14 9"/>')
    o+=body
    for i in range(5):
        o.append(f'<text x="{i*CW+CW/2}" y="{CH+32}" text-anchor="middle" font-family="DejaVu Sans" font-size="21" font-weight="700" fill="#8A6A44">{LBL[i]}</text>')
        o.append(f'<text x="{i*CW+CW/2}" y="{CH+58}" text-anchor="middle" font-family="DejaVu Sans" font-size="16" fill="#B39A72">{HGT[i]}px &#183; {int(HGT[i]/440*100)}%</text>')
    o.append(f'<text x="16" y="32" font-family="DejaVu Sans" font-size="24" font-weight="700" fill="#8A6A44">{name.upper()} ({slug})</text>')
    o.append('</svg>')
    open(out,'w').write("\n".join(o)); return W,H

os.makedirs('out',exist_ok=True)
for slug,name,fn,cfg in CROPS:
    w,h=strip(slug,name,fn,cfg,f'out/{slug}.svg')
    open(f'out/{slug}.html','w').write('<html><head><style>html,body{margin:0;padding:0;background:#FBF3E2}svg{display:block}</style></head><body>'+open(f'out/{slug}.svg').read()+'</body></html>')
print("svg built:", len(CROPS))
