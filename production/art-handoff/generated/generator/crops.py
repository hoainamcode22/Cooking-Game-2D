import math, sys
sys.path.insert(0,'.')
from pg2 import *

CW=CH=512; BY=460; CX=256
HGT=[80,132,229,334,440]

def rnd(i,a,b):
    x=math.sin(i*12.9898)*43758.5453
    return a+(x-math.floor(x))*(b-a)

# ---------- generic bush/vine plant ----------
def build(stage, C):
    sh, dt = [], []
    top = BY-HGT[stage]
    lg = C.get('leafgrad','gLeaf')

    if stage==0:
        sh.append((d_soil(CX,BY,214,52),'gSoil','#3A2312'))
        sh.append((d_orb(CX+16,BY-58,15,1.0,1.0),'gSeed','#5C4220'))
        dt.append(f'<path d="M {CX-62},{BY-32} q 22,-16 46,-9" fill="none" stroke="#D69D62" stroke-width="7" stroke-linecap="round" opacity="0.5"/>')
        dt.append(f'<g fill="#3A2312" opacity="0.42"><ellipse cx="{CX-34}" cy="{BY-20}" rx="7" ry="5"/><ellipse cx="{CX+40}" cy="{BY-28}" rx="6" ry="4"/></g>')
        return sh,dt

    sw = C.get('stemw',[0,15,20,24,27])[stage]
    sh.append((d_soil(CX,BY,[214,182,168,160,156][stage],[52,40,34,32,30][stage]),'gSoil','#3A2312'))
    bend = C.get('bend',[0,4,8,11,13])[stage]
    sh.append((d_stem(CX,BY-[0,50,42,40,38][stage],top+10,sw,sw*0.5,bend),'gStem','#3E5226'))

    if stage==1:
        for a,L,W in ((-20,64,26),(200,60,25)):
            d=d_leaf(L,W,3,0.05,1.0,0.0)
            sh.append((f'<T>{a}|{CX+2}|{top+14}|{d}',lg,'#25381A'))
            dt.append(f'<g transform="translate({CX+2},{top+14}) rotate({a})">'
                      f'<path d="{veins(L,W,3)}" fill="none" stroke="#26391A" stroke-width="3.2" stroke-linecap="round" opacity="0.5"/></g>')
        dt.append(f'<ellipse cx="{CX+2}" cy="{top+6}" rx="11" ry="7" fill="#C6DC8E" opacity="0.5" filter="url(#bl5)"/>')
    else:
        n = C.get('nleaf',[0,0,7,9,11])[stage]
        L0,L1 = C.get('leaflen',[(0,0),(0,0),(122,66),(142,68),(156,70)])[stage]
        serr = C.get('serr',6); lobe=C.get('lobe',0.0)
        for i in range(n):
            t=i/(n-1) if n>1 else 0
            y=(BY-46)+(top+16-(BY-46))*t
            side=1 if i%2==0 else -1
            L=(L0+(L1-L0)*t)*rnd(i+stage*7,.88,1.06); W=L*C.get('lw',0.42)
            ang=(-26 if side>0 else 206)+side*rnd(i+stage*7+3,-16,20)
            cu=rnd(i+stage*7+5,.03,.11)
            d=d_leaf(L,W,serr,cu,1.0,lobe)
            lx=CX+side*rnd(i+stage*7+9,1,7)
            g2 = lg if i%2==0 else ('gLeafD' if lg=='gLeaf' else lg)
            sh.append((f'<T>{ang}|{lx}|{y}|{d}',g2,'#25381A'))
            dt.append(f'<g transform="translate({lx:.1f},{y:.1f}) rotate({ang:.1f})">'
                      f'<path d="{veins(L,W)}" fill="none" stroke="#26391A" stroke-width="3.6" stroke-linecap="round" opacity="0.55"/>'
                      f'<path d="{veins(L,W)}" fill="none" stroke="#D3E39B" stroke-width="1.7" stroke-linecap="round" opacity="0.42" transform="translate(0,-2.5)"/>'
                      f'</g>')
        for f in C.get('fruit',lambda s:[])(stage):
            sh.append(f)
        for fl in C.get('flower',lambda s:[])(stage):
            dt.append(fl)
    return sh,dt

def flower5(cx,cy,r,grad='gGold',oc='#6B4A12'):
    p=[]
    for i in range(5):
        a=math.radians(-90+i*72)
        px,py=cx+math.cos(a)*r*.78, cy+math.sin(a)*r*.78
        p.append(f'<ellipse cx="{px:.1f}" cy="{py:.1f}" rx="{r*.52:.1f}" ry="{r*.36:.1f}" '
                 f'transform="rotate({math.degrees(a):.0f} {px:.1f} {py:.1f})" fill="url(#{grad})" stroke="{oc}" stroke-width="4.5"/>')
    p.append(f'<circle cx="{cx:.1f}" cy="{cy:.1f}" r="{r*.34:.1f}" fill="#8A6A1E" stroke="#5A4210" stroke-width="4"/>')
    return "".join(p)

def calyx(cx,cy,r):
    pts=[]
    for i in range(6):
        a=math.radians(-90+i*60)
        pts.append(f'{cx+math.cos(a)*r*1.05:.1f},{cy+math.sin(a)*r*1.05:.1f}')
        a2=math.radians(-90+i*60+30)
        pts.append(f'{cx+math.cos(a2)*r*.34:.1f},{cy+math.sin(a2)*r*.34:.1f}')
    return f'<path d="M {" L ".join(pts)} Z" fill="url(#gLeafD)" stroke="#2F4A1E" stroke-width="5.5" stroke-linejoin="round"/>'

def spec(cx,cy,r,op=.72):
    return (f'<ellipse cx="{cx-r*.40:.1f}" cy="{cy-r*.46:.1f}" rx="{r*.30:.1f}" ry="{r*.20:.1f}" '
            f'transform="rotate(-34 {cx-r*.40:.1f} {cy-r*.46:.1f})" fill="#FFF3EA" opacity="{op}" filter="url(#bl5)"/>'
            f'<ellipse cx="{cx-r*.15:.1f}" cy="{cy-r*.66:.1f}" rx="{r*.12:.1f}" ry="{r*.07:.1f}" fill="#FFFFFF" opacity="0.5"/>')

# ---------- render ----------
def expand(tok):
    """resolve '<T>ang|x|y|d' rotated-path tokens into absolute path d via transform group"""
    return tok

def render_cell(shapes, details, uid, ow=None):
    """shapes may contain '<T>ang|x|y|d' — emit as transformed <g> inside silhouette group"""
    def paths(fillmode):
        out=[]
        for d,f,s in shapes:
            if d.startswith('<T>'):
                _,rest=d.split('<T>',1); ang,x,y,dd=rest.split('|',3)
                inner = (f'<path d="{dd}"/>' if fillmode is None
                         else f'<path d="{dd}" fill="url(#{f})" stroke="{s}" stroke-width="4.4" stroke-opacity="0.85" stroke-linejoin="round"/>')
                out.append(f'<g transform="translate({float(x):.1f},{float(y):.1f}) rotate({float(ang):.1f})">{inner}</g>')
            else:
                out.append(f'<path d="{d}"/>' if fillmode is None
                           else f'<path d="{d}" fill="url(#{f})" stroke="{s}" stroke-width="4.4" stroke-opacity="0.85" stroke-linejoin="round"/>')
        return "".join(out)
    dfs=(f'<g id="s{uid}">{paths(None)}</g><clipPath id="c{uid}"><use href="#s{uid}"/></clipPath>')
    ow = OW if ow is None else ow
    b=[f'<use href="#s{uid}" fill="{OC}" stroke="{OC}" stroke-width="{ow}" stroke-linejoin="round"/>',
       paths(True),
       f'<g clip-path="url(#c{uid})"><use href="#s{uid}" fill="none" stroke="#2B0F09" stroke-width="16" filter="url(#bl7)" opacity="0.24"/></g>',
       f'<g clip-path="url(#c{uid})"><use href="#s{uid}" fill="none" stroke="#F2F7D8" stroke-width="12" filter="url(#bl7)" opacity="0.34" transform="translate(-6,-7)"/></g>']
    b+=details
    return dfs,"".join(b)
