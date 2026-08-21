import math, sys; sys.path.insert(0,'.')
from crops import CW,CH,BY,CX,HGT,render_cell,flower5,spec
from pg2 import *
from allcrops import R,T,SOILW,SOILH,base_soil,seed_stage,sprout_stage

# ---------------- bloom shapes (return shapes list + details list) ----------------
def ray(cx,cy,r,n,g,oc,cg='gDisc',co='#5A4210',rl=1.0,rw=.30,open01=1.0):
    sh=[];dt=[]
    for i in range(n):
        a=-90+i*360/n
        pl=r*rl*open01
        d=d_leaf(pl,pl*rw,2,0.0)
        sh.append((T(a,cx,cy,d),g,oc))
        dt.append(f'<g transform="translate({cx:.1f},{cy:.1f}) rotate({a:.1f})">'
                  f'<path d="M {pl*0.16:.1f},0 L {pl*0.92:.1f},0" stroke="{oc}" stroke-width="3.0" opacity="0.40" fill="none"/></g>')
    sh.append((d_orb(cx,cy,r*0.42,1.0,1.0),cg,co))
    dt.append(f'<circle cx="{cx:.1f}" cy="{cy:.1f}" r="{r*0.30:.1f}" fill="none" stroke="#6B4A12" stroke-width="3" opacity="0.45"/>')
    for k in range(9):
        aa=k*40*math.pi/180
        dt.append(f'<circle cx="{cx+math.cos(aa)*r*0.22:.1f}" cy="{cy+math.sin(aa)*r*0.22:.1f}" r="{r*0.052:.1f}" fill="#3A2A08" opacity="0.5"/>')
    return sh,dt

def layered(cx,cy,r,g,oc,rings=3):
    sh=[];dt=[]
    for k in range(rings,0,-1):
        rr=r*(0.42+0.58*k/rings); n=5+k*2
        for i in range(n):
            a=-90+i*360/n+k*23
            d=d_leaf(rr*1.05,rr*0.44,2,0.20)
            sh.append((T(a,cx,cy,d),g,oc))
            dt.append(f'<g transform="translate({cx:.1f},{cy:.1f}) rotate({a:.1f})">'
                      f'<path d="M 0,0 L {rr*0.95:.1f},0" stroke="{oc}" stroke-width="3.2" opacity="0.42" fill="none"/></g>')
    dt.append(f'<circle cx="{cx:.1f}" cy="{cy:.1f}" r="{r*0.13:.1f}" fill="{oc}" opacity="0.55"/>')
    dt.append(spec(cx,cy,r,.40))
    return sh,dt

def cup(cx,cy,r,g,oc,open01=1.0):
    sh=[];dt=[]
    w=r*(0.40+0.22*open01)
    for k,(dx,a) in enumerate(((-r*0.34,-104),(r*0.34,-76),(0,-90))):
        sh.append((T(a,cx+dx,cy+r*0.62,d_leaf(r*1.35,w,2,-0.16)),g,oc))
        dt.append(f'<g transform="translate({cx+dx:.1f},{cy+r*0.62:.1f}) rotate({a})">'
                  f'<path d="M {r*0.20:.1f},0 L {r*1.18:.1f},0" stroke="{oc}" stroke-width="3.4" opacity="0.45" fill="none"/></g>')
    dt.append(spec(cx-r*0.16,cy-r*0.16,r,.42))
    return sh,dt

def globe(cx,cy,r,g,oc):
    sh=[];dt=[]
    for k in range(16):
        a=k*137.5*math.pi/180; rad=r*0.72*math.sqrt((k+1)/16)
        px,py=cx+math.cos(a)*rad, cy+math.sin(a)*rad*0.92
        for j in range(4):
            aa=j*90+k*11
            sh.append((T(aa,px,py,d_leaf(r*0.27,r*0.16,2,0.0)),g,oc))
    dt.append(spec(cx,cy,r,.30))
    return sh,dt

def spike(cx,ytop,ybot,w,g,oc):
    sh=[];dt=[]
    n=max(int((ybot-ytop)/26),3)
    for i in range(n):
        t=i/max(n-1,1); y=ybot-(ybot-ytop)*t
        ww=w*(1.0-0.62*t)
        jit=(1 if i%2==0 else -1)*ww*0.22
        for s in (-1,1):
            sh.append((T(-58*s if s>0 else 180+58,cx+jit,y,d_leaf(ww*1.25,ww*0.52,2,0.0)),g,oc))
        sh.append((d_orb(cx+jit,y-ww*0.30,ww*0.40,1.1,1.15),g,oc))
    dt.append(f'<path d="M {cx:.1f},{ybot:.1f} L {cx:.1f},{ytop:.1f}" stroke="#4E6A37" stroke-width="5" fill="none" opacity="0.55"/>')
    return sh,dt

def orchid(cx,cy,r,g,oc,n=4):
    sh=[];dt=[]
    for i in range(n):
        px=cx+(i-(n-1)/2)*r*1.05; py=cy+abs(i-(n-1)/2)*r*0.42
        for k in range(5):
            a=-90+k*72+18
            sh.append((T(a,px,py,d_leaf(r*0.80,r*0.44,2,0.05)),g,oc))
        sh.append((d_orb(px,py,r*0.24,1.0,1.0),'gDisc','#5A4210'))
        dt.append(spec(px,py,r*0.7,.32))
    return sh,dt

# ---------------- generic flower plant ----------------
def fplant(stage,C):
    if stage==0: return seed_stage()
    top=BY-HGT[stage]
    if stage==1: return sprout_stage(top,'gLeafF',C.get('cot',60))
    sh=[base_soil(stage)]; dt=[]
    H=BY-top; sy=BY-SOILH[stage]*0.8
    lt=C.get('leaf','broad')
    nst=C.get('nstem',[0,0,1,2,3])[stage]
    # basal leaves
    nl=C.get('nleaf',[0,0,5,7,8])[stage]
    for i in range(nl):
        side=1 if i%2==0 else -1; t=i/max(nl-1,1)
        LF={'broad':[0,0,0.72,0.40,0.34],'strap':[0,0,0.60,0.32,0.28],'narrow':[0,0,0.64,0.33,0.29]}
        lf=C.get('lfac',LF[lt])[stage]
        tilt=C.get('tilt',[0,0,44,4,0])[stage]
        if lt=='strap':
            L=H*lf*R(i+20,.72,1.10); d=d_blade(L,16,0.26); ang=-90+side*R(i+21,10,34)
        elif lt=='narrow':
            L=H*lf*R(i+20,.66,1.00); d=d_blade(L,11,0.22); ang=-90+side*R(i+21,8,30)
        else:
            L=H*lf*R(i+20,.62,0.94); W=L*0.58; d=d_leaf(L,W,4,0.10,1.0,0.14)
            ang=((-30-tilt) if side>0 else (210+tilt))+side*R(i+21,-12,16)
        sh.append((T(ang,CX+side*R(i+22,2,12),sy-R(i+23,0,18),d),'gLeafF' if i%2 else 'gLeafFD','#25381A'))
    # stems + blooms
    br=C.get('br',[0,0,0,0.26,0.40])[stage]*H
    for k in range(nst):
        off=(k-(nst-1)/2)*C.get('spread',46)
        stg=(0.34 if stage==4 else 0.16)
        ty=top+R(k+30,0,stg)*H*(1 if nst>1 else 0.10)
        sh.append((d_stem(CX+off,sy,ty+br*0.5,11,8,off*0.16),'gStem','#3E5226'))
        if stage==2: continue
        bx,by_=CX+off+off*0.10, ty+br*0.62
        if stage==3:
            bs,bd_=C['bud'](bx,by_,br*C.get('budr',0.62))
        else:
            bs,bd_=C['bloom'](bx,by_,br)
        sh+=bs; dt+=bd_
    if stage>=3 and C.get('extra'): 
        s2,d2=C['extra'](stage); sh+=s2; dt+=d2
    return sh,dt

FDEFS='''
<linearGradient id="gLeafF" x1="0" y1="0" x2="0.22" y2="1">
 <stop offset="0" stop-color="#AFD07C"/><stop offset="0.45" stop-color="#6E9448"/><stop offset="1" stop-color="#2F5225"/></linearGradient>
<linearGradient id="gLeafFD" x1="0" y1="0" x2="0.22" y2="1">
 <stop offset="0" stop-color="#95B968"/><stop offset="0.5" stop-color="#5C8440"/><stop offset="1" stop-color="#27431E"/></linearGradient>
<radialGradient id="gDisc" cx="0.36" cy="0.3" r="0.85">
 <stop offset="0" stop-color="#8A6A2E"/><stop offset="0.6" stop-color="#4E3512"/><stop offset="1" stop-color="#2B1A08"/></radialGradient>
<radialGradient id="gSun" cx="0.34" cy="0.28" r="0.88">
 <stop offset="0" stop-color="#FDF3A8"/><stop offset="0.45" stop-color="#EBC33E"/><stop offset="1" stop-color="#9A7A18"/></radialGradient>
<radialGradient id="gRose" cx="0.33" cy="0.26" r="0.86">
 <stop offset="0" stop-color="#F0837C"/><stop offset="0.42" stop-color="#C4344A"/><stop offset="1" stop-color="#6B1226"/></radialGradient>
<radialGradient id="gLav" cx="0.34" cy="0.28" r="0.88">
 <stop offset="0" stop-color="#C9B6E8"/><stop offset="0.45" stop-color="#8A72C0"/><stop offset="1" stop-color="#453F7A"/></radialGradient>
<radialGradient id="gOrc" cx="0.34" cy="0.28" r="0.88">
 <stop offset="0" stop-color="#EDC4EE"/><stop offset="0.45" stop-color="#B972B8"/><stop offset="1" stop-color="#713464"/></radialGradient>
<radialGradient id="gWhite" cx="0.34" cy="0.26" r="0.88">
 <stop offset="0" stop-color="#FFFFFA"/><stop offset="0.55" stop-color="#EDE8D2"/><stop offset="1" stop-color="#B0A88C"/></radialGradient>
<radialGradient id="gTulip" cx="0.33" cy="0.26" r="0.86">
 <stop offset="0" stop-color="#F58D8D"/><stop offset="0.42" stop-color="#D6404F"/><stop offset="1" stop-color="#7A1428"/></radialGradient>
<radialGradient id="gMari" cx="0.33" cy="0.26" r="0.86">
 <stop offset="0" stop-color="#FFCE7A"/><stop offset="0.42" stop-color="#E8862A"/><stop offset="1" stop-color="#8A4410"/></radialGradient>
<radialGradient id="gPeony" cx="0.33" cy="0.26" r="0.86">
 <stop offset="0" stop-color="#FDD3DE"/><stop offset="0.45" stop-color="#E288A4"/><stop offset="1" stop-color="#913E4C"/></radialGradient>
<radialGradient id="gHyd" cx="0.34" cy="0.28" r="0.88">
 <stop offset="0" stop-color="#BFD0F2"/><stop offset="0.45" stop-color="#7C8FD0"/><stop offset="1" stop-color="#3F4A85"/></radialGradient>
<radialGradient id="gPrim" cx="0.33" cy="0.26" r="0.86">
 <stop offset="0" stop-color="#FFF0B8"/><stop offset="0.45" stop-color="#F2C777"/><stop offset="1" stop-color="#B07A3A"/></radialGradient>
<radialGradient id="gBud" cx="0.34" cy="0.28" r="0.86">
 <stop offset="0" stop-color="#A9C47A"/><stop offset="0.5" stop-color="#6E9448"/><stop offset="1" stop-color="#2F5225"/></radialGradient>
'''
