import math, sys; sys.path.insert(0,'.')
from crops import *
from pg2 import *

def R(i,a,b):
    x=math.sin(i*12.9898)*43758.5453; return a+(x-math.floor(x))*(b-a)
def T(ang,x,y,d): return f'<T>{ang}|{x}|{y}|{d}'
SOILW=[214,182,168,160,156]; SOILH=[52,40,34,32,30]

def base_soil(stage): return (d_soil(CX,BY,SOILW[stage],SOILH[stage]),'gSoil','#3A2312')
def seed_stage(extra=None):
    sh=[base_soil(0),(d_orb(CX+16,BY-58,15,1,1),'gSeed','#5C4220')]
    dt=[f'<path d="M {CX-62},{BY-32} q 22,-16 46,-9" fill="none" stroke="#D69D62" stroke-width="7" stroke-linecap="round" opacity="0.5"/>',
        f'<g fill="#3A2312" opacity="0.42"><ellipse cx="{CX-34}" cy="{BY-20}" rx="7" ry="5"/><ellipse cx="{CX+40}" cy="{BY-28}" rx="6" ry="4"/></g>']
    return sh,dt
def sprout_stage(top, lg='gLeaf', L=64):
    sh=[base_soil(1),(d_stem(CX,BY-50,top+10,15,8,4),'gStem','#3E5226')]
    dt=[]
    for a in (-20,200):
        d=d_leaf(L,L*0.40,3,0.05); sh.append((T(a,CX+2,top+14,d),lg,'#25381A'))
        dt.append(f'<g transform="translate({CX+2},{top+14}) rotate({a})"><path d="{veins(L,L*0.40,3)}" fill="none" stroke="#26391A" stroke-width="3.2" stroke-linecap="round" opacity="0.5"/></g>')
    dt.append(f'<ellipse cx="{CX+2}" cy="{top+4}" rx="11" ry="7" fill="#C6DC8E" opacity="0.5" filter="url(#bl5)"/>')
    return sh,dt

# ---------- GRASS / CEREAL ----------
def grass(stage,C):
    if stage==0: return seed_stage()
    top=BY-HGT[stage]
    if stage==1: return sprout_stage(top, C.get('lg','gLeaf'), 58)
    sh=[base_soil(stage)]; dt=[]
    n=C.get('nb',[0,0,7,10,13])[stage]; H=BY-top
    for i in range(n):
        t=i/(n-1); side=1 if i%2==0 else -1
        L=H*C.get('bl',0.98)*R(i+stage*5,.66,0.96)
        W=C.get('bw',26)*R(i+stage*5+2,.88,1.12)
        ang=-90+side*R(i+stage*5+4,4,C.get('fan',18))
        bend=C.get('bend2',0.30)*R(i+stage*5+6,.6,1.5)
        d=d_blade(L,W,bend)
        g='gLeaf' if i%2==0 else 'gLeafD'
        sh.append((T(ang,CX+side*R(i+stage*5+8,2,11),BY-SOILH[stage]*0.9,d),g,'#25381A'))
    for f in C.get('head',lambda s:([],[]))(stage)[0]: sh.append(f)
    dt+=C.get('head',lambda s:([],[]))(stage)[1]
    return sh,dt

def rice_head(stage):
    if stage<3: return [],[]
    sh=[];dt=[]
    ns=3 if stage==3 else 5
    for k in range(ns):
        bx=CX+(k-(ns-1)/2)*R(k+90,44,60); by=BY-HGT[stage]+R(k+91,26,70)
        for j in range(9):
            t=j/8; gx=bx+t*R(k+92,20,34); gy=by+t*t*70+j*3
            r=8.5-2.4*t
            sh.append((d_orb(gx,gy,r,1.5,1.5),'gGold' if stage==4 else 'gGrn','#7A6418' if stage==4 else '#3B5220'))
        dt.append(f'<path d="M {bx},{by} Q {bx+22},{by+38} {bx+34},{by+92}" fill="none" stroke="#8A7A25" stroke-width="5" opacity="0.55"/>')
    return sh,dt

def corn_head(stage):
    if stage<3: return [],[]
    sh=[];dt=[]
    cobs=[(CX-52,300,26,86)] if stage==3 else [(CX-62,296,32,104),(CX+66,238,28,92)]
    for cx,cy,w,h in cobs:
        sh.append((d_pod(cx,cy+h/2,h,w,-90),'gGold' if stage==4 else 'gGrn','#7A6418' if stage==4 else '#3B5220'))
        if stage==4:
            for r_ in range(6):
                for c_ in range(4):
                    gx=cx-w*0.5+c_*w/3.2+ (r_%2)*w/7; gy=cy-h*0.42+r_*h/6.4
                    dt.append(f'<ellipse cx="{gx:.1f}" cy="{gy:.1f}" rx="{w*0.13:.1f}" ry="{h*0.055:.1f}" fill="#F7EB89" stroke="#A2993D" stroke-width="2" opacity="0.85"/>')
        d=d_leaf(h*0.9,w*0.42,3,0.10)
        sh.append((T(-88,cx-w*0.42,cy+h*0.52,d),'gLeafD','#25381A'))
        sh.append((T(-96,cx+w*0.42,cy+h*0.52,d),'gLeaf','#25381A'))
        dt.append(f'<path d="M {cx},{cy-h*0.5} q 6,-26 20,-40" fill="none" stroke="#C9A166" stroke-width="6" stroke-linecap="round" opacity="0.8"/>')
    return sh,dt

def cane(stage,C):
    """sugarcane: thick jointed canes + strap leaves"""
    if stage==0: return seed_stage()
    top=BY-HGT[stage]
    if stage==1: return sprout_stage(top,'gLeaf',56)
    sh=[base_soil(stage)]; dt=[]
    nc=[0,0,3,4,5][stage]
    for k in range(nc):
        off=(k-(nc-1)/2)*R(k+60,26,40); w=[0,0,17,20,23][stage]*R(k+61,.85,1.1)
        ty=top+R(k+62,10,60)
        sh.append((d_stem(CX+off,BY-SOILH[stage]*0.8,ty,w,w*0.82,off*0.10),'gCane','#5C4A18'))
        for j in range(6):
            jy=BY-SOILH[stage]-j*(BY-ty)/6.4
            if jy>ty: dt.append(f'<line x1="{CX+off-w*0.5:.1f}" y1="{jy:.1f}" x2="{CX+off+w*0.5:.1f}" y2="{jy:.1f}" stroke="#7A6428" stroke-width="4.5" opacity="0.7"/>')
        for m in range(3):
            side=1 if m%2==0 else -1
            L=[0,0,88,104,118][stage]*R(k*3+m+63,.8,1.05)
            d=d_blade(L,22,0.42)
            sh.append((T(-70+side*46,CX+off,ty+18+m*30,d),'gLeaf' if m%2 else 'gLeafD','#25381A'))
    return sh,dt

# ---------- LEAFY HEAD (bap cai) ----------
def head(stage,C):
    if stage==0: return seed_stage()
    top=BY-HGT[stage]
    if stage==1: return sprout_stage(top,'gLeaf',48)
    sh=[base_soil(stage)]; dt=[]
    H=BY-top; rr=H*0.34; cy=BY-SOILH[stage]*0.7-rr*1.02
    for k in range(7):
        a=-90+(k-3)*30; L=rr*1.30*R(k+70,.9,1.08); W=L*0.60
        d=d_leaf(L,W,4,0.12,1.0,0.16)
        sh.append((T(a,CX,cy+rr*0.34,d),'gLeafD' if k%2 else 'gLeaf','#25381A'))
    sh.append((d_orb(CX,cy,rr*0.90,1.06,1.10),'gHead','#4A6129'))
    for k in range(4):
        a=-90+(k-1.5)*44; L=rr*0.86; W=L*0.66
        d=d_leaf(L,W,3,0.10,1.0,0.12)
        sh.append((T(a,CX,cy+rr*0.24,d),'gLeaf','#25381A'))
    dt.append(f'<ellipse cx="{CX-rr*0.30:.1f}" cy="{cy-rr*0.34:.1f}" rx="{rr*0.36:.1f}" ry="{rr*0.24:.1f}" transform="rotate(-32 {CX-rr*0.30:.1f} {cy-rr*0.34:.1f})" fill="#F0F7CE" opacity="0.42" filter="url(#bl7)"/>')
    for k in range(5):
        a=math.radians(-90+(k-2)*34)
        dt.append(f'<path d="M {CX:.1f},{cy+rr*0.16:.1f} Q {CX+math.cos(a)*rr*0.4:.1f},{cy+math.sin(a)*rr*0.5:.1f} {CX+math.cos(a)*rr*0.82:.1f},{cy+math.sin(a)*rr*0.80:.1f}" fill="none" stroke="#E4EFB4" stroke-width="5" opacity="0.5" stroke-linecap="round"/>')
    return sh,dt

# ---------- ROOT (ca rot / khoai tay) ----------
def root(stage,C):
    if stage==0: return seed_stage()
    top=BY-HGT[stage]
    if stage==1: return sprout_stage(top,'gLeaf',56)
    sh=[base_soil(stage)]; dt=[]
    H=BY-top
    if C['kind']=='carrot':
        rw=[0,0,70,104,138][stage]; rh=[0,0,40,60,80][stage]
        sh.append((d_orb(CX,BY-SOILH[stage]*0.8-rh*0.18,rw*0.5,1.4,0.9),'gOrange','#8A3F10'))
        for k in range(11):
            a=-90+(k-5)*6.2; L=H*R(k+80,.42,.60)
            d=d_blade(L,19,0.20)
            sh.append((T(a,CX+(k-5)*4,BY-SOILH[stage]*0.8-rh*0.62,d),'gLeaf' if k%2 else 'gLeafD','#25381A'))
        dt.append(f'<g opacity="0.55" stroke="#B4551F" stroke-width="4" fill="none" stroke-linecap="round">'
                  f'<path d="M {CX-rw*0.24},{BY-SOILH[stage]*0.8-rh*0.1} q {rw*0.24},6 {rw*0.46},-2"/></g>')
    else:
        for cx,cy,r in [(0,0,0)] if stage<3 else ([(CX-56,BY-52,34),(CX+58,BY-44,29)] if stage==3 else [(CX-74,BY-54,44),(CX+70,BY-46,38),(CX+2,BY-34,32)]):
            if r: sh.append((d_orb(cx,cy,r,1.05,1.0),'gTan','#6E4A22'))
        for k in range(C.get('nleaf2',[0,0,7,9,11])[stage]):
            t=k/max(C.get('nleaf2',[0,0,7,9,11])[stage]-1,1); side=1 if k%2==0 else -1
            y=(BY-56)+(top+20-(BY-56))*t
            L=H*R(k+84,.20,.30); W=L*0.46
            d=d_leaf(L,W,5,0.10,1.0,0.18)
            sh.append((T((-24 if side>0 else 204)+side*R(k+85,-14,18),CX+side*R(k+86,2,10),y,d),'gLeaf' if k%2 else 'gLeafD','#25381A'))
    return sh,dt

# ---------- FUNGUS ----------
def fungus(stage,C):
    if stage==0: return seed_stage()
    sh=[base_soil(stage)]; dt=[]
    top=BY-HGT[stage]; H=BY-top
    specs={1:[(CX,1.0)],2:[(CX-40,0.60),(CX+30,1.0)],
           3:[(CX-56,0.52),(CX-4,0.86),(CX+50,1.0),(CX+16,0.44)],
           4:[(CX-70,0.44),(CX-22,0.74),(CX+30,1.0),(CX+76,0.56),(CX-42,0.32),(CX+4,0.28)]}[stage]
    for k,(cx,f) in enumerate(sorted(specs,key=lambda z:-z[1])):
        h=H*f; cr=h*0.44; sy=BY-SOILH[stage]*0.6
        sh.append((d_stem(cx,sy,sy-h*0.62,cr*0.44,cr*0.36,R(k+50,-5,5)),'gCream','#8A6B3C'))
        sh.append((d_cap(cx,sy-h*0.60,cr,cr*0.66),'gCapM','#6B3A18'))
        for j in range(3):
            dt.append(f'<ellipse cx="{cx+R(k*3+j+51,-cr*0.55,cr*0.55):.1f}" cy="{sy-h*0.60-cr*R(k*3+j+52,0.18,0.46):.1f}" rx="{cr*R(k*3+j+53,.10,.17):.1f}" ry="{cr*R(k*3+j+54,.07,.12):.1f}" fill="#FFF4DE" opacity="0.75" stroke="#C9A272" stroke-width="2.5"/>')
        dt.append(f'<path d="M {cx-cr*0.72:.1f},{sy-h*0.60-cr*0.18:.1f} q {cr*0.34:.1f},{-cr*0.46:.1f} {cr*0.74:.1f},{-cr*0.52:.1f}" fill="none" stroke="#F0C49E" stroke-width="{cr*0.16:.1f}" stroke-linecap="round" opacity="0.45"/>')
    return sh,dt

# ---------- VINE ON POST (tieu) ----------
def vine(stage,C):
    if stage==0: return seed_stage()
    top=BY-HGT[stage]
    if stage==1: return sprout_stage(top,'gLeaf',56)
    sh=[base_soil(stage)]; dt=[]
    sh.append((d_stem(CX,BY-SOILH[stage]*0.7,top+6,26,21,0),'gWood','#4A3016'))
    n=[0,0,8,11,14][stage]; H=BY-top
    for i in range(n):
        t=i/(n-1); side=1 if i%2==0 else -1
        y=(BY-52)+(top+22-(BY-52))*t
        L=[0,0,96,110,120][stage]*R(i+40,.82,1.04); W=L*0.52
        d=d_leaf(L,W,3,0.09,1.0,0.05)
        sh.append((T((-22 if side>0 else 202)+side*R(i+41,-14,16),CX+side*14,y,d),'gLeaf' if i%2 else 'gLeafD','#25381A'))
    if stage>=3:
        strings=[(CX-56,300,7)] if stage==3 else [(CX-66,296,10),(CX+70,240,9),(CX-24,180,8)]
        for k,(bx,by_,cnt) in enumerate(strings):
            for j in range(cnt):
                gy=by_+j*17; gx=bx+math.sin(j*0.8)*7
                sh.append((d_orb(gx,gy,9.5,1.1,1.1),'gGrn' if stage==3 else 'gPep','#3B5220' if stage==3 else '#3A2010'))
            dt.append(f'<path d="M {bx},{by_-12} L {bx},{by_+cnt*17}" stroke="#5A7038" stroke-width="4" fill="none" opacity="0.7"/>')
    return sh,dt

EXTRA_DEFS='''
<linearGradient id="gCane" x1="0" y1="0" x2="1" y2="0">
 <stop offset="0" stop-color="#D9C271"/><stop offset="0.45" stop-color="#A8913C"/><stop offset="1" stop-color="#5C4A18"/></linearGradient>
<radialGradient id="gHead" cx="0.34" cy="0.26" r="0.86">
 <stop offset="0" stop-color="#E4F0BC"/><stop offset="0.45" stop-color="#A7C273"/><stop offset="1" stop-color="#5A7A38"/></radialGradient>
<radialGradient id="gCapM" cx="0.34" cy="0.24" r="0.86">
 <stop offset="0" stop-color="#E0A277"/><stop offset="0.45" stop-color="#B46A38"/><stop offset="1" stop-color="#6B3A18"/></radialGradient>
<linearGradient id="gWood" x1="0" y1="0" x2="1" y2="0">
 <stop offset="0" stop-color="#C09256"/><stop offset="0.45" stop-color="#8A6335"/><stop offset="1" stop-color="#4A3016"/></linearGradient>
<radialGradient id="gPep" cx="0.34" cy="0.26" r="0.86">
 <stop offset="0" stop-color="#8A6A50"/><stop offset="0.5" stop-color="#4E3524"/><stop offset="1" stop-color="#2B1A0F"/></radialGradient>
<radialGradient id="gLem" cx="0.33" cy="0.26" r="0.86">
 <stop offset="0" stop-color="#FCF19A"/><stop offset="0.42" stop-color="#E0C63C"/><stop offset="1" stop-color="#8A7A1A"/></radialGradient>
'''
