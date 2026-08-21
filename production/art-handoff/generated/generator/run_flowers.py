import math, sys, os; sys.path.insert(0,'.')
from flowers import *
from crops import CW,CH,BY,CX,HGT,render_cell
from pg2 import DEFS_CORE, d_leaf, d_orb
from allcrops import EXTRA_DEFS, T

def bud_generic(g='gBud',oc='#2F5225',petal=None):
    def f(cx,cy,r):
        sh=[];dt=[]
        for k in range(5):
            a=-90+k*72
            sh.append((T(a,cx,cy+r*0.30,d_leaf(r*1.10,r*0.42,2,-0.14)),g,oc))
        if petal:
            sh.append((d_orb(cx,cy-r*0.34,r*0.20,1.2,1.2),petal,oc))
        dt.append(f'<ellipse cx="{cx-r*0.22:.1f}" cy="{cy-r*0.30:.1f}" rx="{r*0.22:.1f}" ry="{r*0.14:.1f}" fill="#E4F0BC" opacity="0.35" filter="url(#bl5)"/>')
        return sh,dt
    return f

F=[]
F.append(("huong_duong","Huong Duong", dict(leaf='broad', nstem=[0,0,0,1,1], spread=0, br=[0,0,0,0.18,0.25],
    bloom=lambda x,y,r: ray(x,y,r,16,'gSun','#8A6A18',rl=1.25,rw=.26),
    bud=bud_generic('gBud','#2F5225','gSun'), budr=0.66, cot=64)))
F.append(("hoa_hong","Hoa Hong", dict(leaf='broad', nstem=[0,0,0,2,2], spread=69, br=[0,0,0,0.15,0.21],
    bloom=lambda x,y,r: layered(x,y,r,'gRose','#6B1226',4),
    bud=bud_generic('gBud','#2F5225','gRose'), budr=0.60)))
F.append(("hoa_oai_huong","Oai Huong", dict(leaf='narrow', nstem=[0,0,0,4,6], spread=49, br=[0,0,0,0.30,0.38],
    bloom=lambda x,y,r: spike(x,y-r*0.9,y+r*0.5,r*0.30,'gLav','#3A3468'),
    bud=lambda x,y,r: spike(x,y-r*0.7,y+r*0.3,r*0.24,'gBud','#2F5225'), budr=0.70)))
F.append(("hoa_lan","Hoa Lan", dict(leaf='strap', nstem=[0,0,0,2,2], spread=78, br=[0,0,0,0.16,0.22],
    bloom=lambda x,y,r: orchid(x,y,r*0.62,'gOrc','#5C2450',3),
    bud=bud_generic('gBud','#2F5225','gOrc'), budr=0.58)))
F.append(("hoa_cuc_trang","Cuc Trang", dict(leaf='broad', nstem=[0,0,0,3,4], spread=63, br=[0,0,0,0.15,0.19],
    bloom=lambda x,y,r: ray(x,y,r,14,'gWhite','#8A8268',rl=1.30,rw=.24),
    bud=bud_generic('gBud','#2F5225','gWhite'), budr=0.56)))
F.append(("tulip","Tulip", dict(leaf='strap', nstem=[0,0,0,3,4], spread=75, br=[0,0,0,0.16,0.22],
    bloom=lambda x,y,r: cup(x,y,r*0.86,'gTulip','#7A1428',1.0),
    bud=lambda x,y,r: cup(x,y,r*0.72,'gBud','#2F5225',0.25), budr=0.72)))
F.append(("hoa_cuc_van_tho","Cuc Van Tho", dict(leaf='narrow', nstem=[0,0,0,2,3], spread=66, br=[0,0,0,0.15,0.19],
    bloom=lambda x,y,r: layered(x,y,r*0.94,'gMari','#8A4410',5),
    bud=bud_generic('gBud','#2F5225','gMari'), budr=0.56)))
F.append(("hoa_mau_don","Mau Don", dict(leaf='broad', nstem=[0,0,0,1,2], spread=84, br=[0,0,0,0.18,0.25],
    bloom=lambda x,y,r: layered(x,y,r,'gPeony','#913E4C',5),
    bud=bud_generic('gBud','#2F5225','gPeony'), budr=0.64)))
F.append(("hoa_cam_tu_cau","Cam Tu Cau", dict(leaf='broad', nstem=[0,0,0,1,2], spread=81, br=[0,0,0,0.18,0.24],
    bloom=lambda x,y,r: globe(x,y,r*0.96,'gHyd','#3F4A85'),
    bud=bud_generic('gBud','#2F5225','gHyd'), budr=0.62)))
F.append(("hoa_anh_thao","Anh Thao", dict(leaf='broad', nstem=[0,0,0,4,5], spread=58, br=[0,0,0,0.12,0.16],
    bloom=lambda x,y,r: ray(x,y,r*1.05,5,'gPrim','#A06A2A',rl=1.0,rw=.52),
    bud=bud_generic('gBud','#2F5225','gPrim'), budr=0.52)))

LBL=["1 vua gieo","2 mam","3 cay non","4 nu","5 NO RO"]
os.makedirs('out/f',exist_ok=True); os.makedirs('out/fraw',exist_ok=True)
for slug,name,cfg in F:
    defs=[];body=[];rawb=[];rawd=[]
    for s in range(5):
        sh,dt = fplant(s,cfg)
        df,bd = render_cell(sh,dt,f"f{slug}{s}",cfg.get('ow'),True)
        defs.append(df); body.append(f'<g transform="translate({s*CW},0)">{bd}</g>')
        df2,bd2 = render_cell(sh,dt,f"z{slug}{s}",cfg.get('ow'),True)
        rawd.append(df2); rawb.append(f'<g transform="translate({s*CW},0)">{bd2}</g>')
    W=CW*5;H=CH+78
    o=[f'<svg xmlns="http://www.w3.org/2000/svg" width="{W}" height="{H}" viewBox="0 0 {W} {H}">',
       f'<defs>{DEFS_CORE}{EXTRA_DEFS}{FDEFS}{"".join(defs)}</defs>',
       f'<rect width="{W}" height="{H}" fill="#FBF3E2"/>']
    for i in range(1,5): o.append(f'<line x1="{i*CW}" y1="8" x2="{i*CW}" y2="{CH-4}" stroke="#E2D0AC" stroke-width="2" stroke-dasharray="8 8"/>')
    o.append(f'<line x1="0" y1="{BY}" x2="{W}" y2="{BY}" stroke="#C9A96E" stroke-width="3" stroke-dasharray="14 9"/>')
    o+=body
    for i in range(5):
        o.append(f'<text x="{i*CW+CW/2}" y="{CH+32}" text-anchor="middle" font-family="DejaVu Sans" font-size="21" font-weight="700" fill="#8A6A44">{LBL[i]}</text>')
    o.append(f'<text x="16" y="32" font-family="DejaVu Sans" font-size="24" font-weight="700" fill="#8A6A44">{name.upper()} ({slug})</text>')
    o.append('</svg>')
    open(f'out/f/{slug}.html','w').write('<html><head><style>html,body{margin:0;padding:0;background:#FBF3E2}svg{display:block}</style></head><body>'+"\n".join(o)+'</body></html>')
    raw=(f'<svg xmlns="http://www.w3.org/2000/svg" width="{CW*5}" height="{CH}" viewBox="0 0 {CW*5} {CH}">'
         f'<defs>{DEFS_CORE}{EXTRA_DEFS}{FDEFS}{"".join(rawd)}</defs>{"".join(rawb)}</svg>')
    open(f'out/fraw/{slug}.html','w').write('<html><head><style>html,body{margin:0;padding:0;background:transparent}svg{display:block}</style></head><body>'+raw+'</body></html>')
print("flowers built:",len(F))
