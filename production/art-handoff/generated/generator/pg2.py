import math
OC, OW = "#442510", 17          # outline color / width (silhouette pass)

# ---------------- primitives: return path 'd' only ----------------
def d_leaf(L, W, serr=6, curl=0.06, asym=1.0, lobe=0.0):
    top, bot, N = [], [], 52
    for i in range(N+1):
        t=i/N; x=L*t
        env=math.sin(math.pi*t)**0.72
        jag=1+0.14*math.sin(t*serr*2*math.pi)
        lb =1+lobe*math.sin(t*3*math.pi)
        top.append((x, -W*env*jag*lb + curl*L*t*t))
    for i in range(N+1):
        t=1-i/N; x=L*t
        env=math.sin(math.pi*t)**0.72
        jag=1+0.14*math.sin(t*serr*2*math.pi+math.pi)
        lb =1+lobe*math.sin(t*3*math.pi+1.1)
        bot.append((x, W*asym*env*jag*lb + curl*L*t*t))
    p=top+bot
    return "M "+" L ".join(f"{x:.1f},{y:.1f}" for x,y in p)+" Z"

def d_blade(L, W, bend=0.35):
    """grass / cereal blade — long tapered, curving"""
    pts=[]; N=40
    for i in range(N+1):
        t=i/N; x=L*t; y=-bend*L*t*t
        w=W*(0.36+0.64*(1-t)**0.9)*math.sin(math.pi*min(t*1.9,1))**0.22
        pts.append((x,y-w))
    for i in range(N+1):
        t=1-i/N; x=L*t; y=-bend*L*t*t
        w=W*(0.36+0.64*(1-t)**0.9)*math.sin(math.pi*min(t*1.9,1))**0.22
        pts.append((x,y+w))
    return "M "+" L ".join(f"{x:.1f},{y:.1f}" for x,y in pts)+" Z"

def d_stem(x,y0,y1,w0,w1,bend=0.0):
    xt=x+bend; h=y1-y0
    return (f'M {x-w0/2:.1f},{y0:.1f} C {x-w0/2+bend*.3:.1f},{y0+h*.45:.1f} {xt-w1/2:.1f},{y0+h*.7:.1f} '
            f'{xt-w1/2:.1f},{y1:.1f} L {xt+w1/2:.1f},{y1:.1f} C {xt+w1/2:.1f},{y0+h*.7:.1f} '
            f'{x+w0/2+bend*.3:.1f},{y0+h*.45:.1f} {x+w0/2:.1f},{y0:.1f} Z')

def d_soil(cx,by,w,h):
    return (f'M {cx-w/2:.1f},{by:.1f} C {cx-w/2:.1f},{by-h*1.25:.1f} {cx-w*.22:.1f},{by-h*1.5:.1f} '
            f'{cx:.1f},{by-h*1.45:.1f} C {cx+w*.24:.1f},{by-h*1.55:.1f} {cx+w/2:.1f},{by-h*1.2:.1f} '
            f'{cx+w/2:.1f},{by:.1f} Z')

def d_orb(cx,cy,r,sq=1.12,fl=1.18):
    return (f'M {cx-r:.1f},{cy:.1f} C {cx-r:.1f},{cy-r*fl:.1f} {cx+r:.1f},{cy-r*fl:.1f} {cx+r:.1f},{cy:.1f} '
            f'C {cx+r:.1f},{cy+r*sq:.1f} {cx-r:.1f},{cy+r*sq:.1f} {cx-r:.1f},{cy:.1f} Z')

def d_pod(cx,cy,L,W,ang=90):
    """elongated fruit (chili) pointing at ang degrees"""
    a=math.radians(ang); ca,sa=math.cos(a),math.sin(a)
    pts=[]; N=34
    for i in range(N+1):
        t=i/N; w=W*math.sin(math.pi*t)**0.62*(1-0.45*t)
        pts.append((L*t, -w))
    for i in range(N+1):
        t=1-i/N; w=W*math.sin(math.pi*t)**0.62*(1-0.45*t)
        pts.append((L*t, w))
    out=[]
    for x,y in pts:
        out.append((cx+x*ca-y*sa, cy+x*sa+y*ca))
    return "M "+" L ".join(f"{x:.1f},{y:.1f}" for x,y in out)+" Z"

def d_cap(cx,cy,r,h):
    """mushroom cap"""
    return (f'M {cx-r:.1f},{cy:.1f} C {cx-r*.98:.1f},{cy-h*1.35:.1f} {cx+r*.98:.1f},{cy-h*1.35:.1f} '
            f'{cx+r:.1f},{cy:.1f} C {cx+r*.55:.1f},{cy+h*.30:.1f} {cx-r*.55:.1f},{cy+h*.30:.1f} {cx-r:.1f},{cy:.1f} Z')

def rot(d, cx, cy, ang, sc=1.0):
    return f'<g transform="translate({cx:.1f},{cy:.1f}) rotate({ang:.1f}) scale({sc:.3f})">§{d}§</g>'

# ---------------- veins / detail overlays ----------------
def veins(L,W,n=4):
    o=[f"M 0,0 L {L*.93:.1f},0"]
    for i in range(1,n+1):
        t=i/(n+1); x=L*t; env=math.sin(math.pi*t)**.72
        for s in(-1,1):
            o.append(f"M {x:.1f},0 Q {x+L*.10:.1f},{s*W*env*.45:.1f} {x+L*.17:.1f},{s*W*env*.80:.1f}")
    return " ".join(o)

# ---------------- assembly: 2-pass silhouette ----------------
def assemble(shapes, details, uid, ow=OW, oc=OC, ao=0.40, rim=0.26):
    """shapes = [(d, fillGradId, innerStrokeColor), ...] in back-to-front order"""
    sil = f'<g id="s{uid}">' + "".join(f'<path d="{d}"/>' for d,_,_ in shapes) + '</g>'
    dfs = f'{sil}<clipPath id="c{uid}"><use href="#s{uid}"/></clipPath>'
    b  = [f'<use href="#s{uid}" fill="{oc}" stroke="{oc}" stroke-width="{ow}" stroke-linejoin="round"/>']
    for d,f,s in shapes:
        b.append(f'<path d="{d}" fill="url(#{f})" stroke="{s}" stroke-width="3.0" '
                 f'stroke-opacity="0.50" stroke-linejoin="round"/>')
    b.append(f'<g clip-path="url(#c{uid})"><use href="#s{uid}" fill="none" stroke="#2B0F09" '
             f'stroke-width="26" filter="url(#bl12)" opacity="{ao}"/></g>')
    b.append(f'<g clip-path="url(#c{uid})"><use href="#s{uid}" fill="none" stroke="#F2F7D8" '
             f'stroke-width="11" filter="url(#bl7)" opacity="{rim}" transform="translate(-5,-6)"/></g>')
    b += details
    return dfs, "".join(b)

DEFS_CORE = '''
<linearGradient id="gLeaf" x1="0" y1="0" x2="0.22" y2="1">
 <stop offset="0" stop-color="#C2D883"/><stop offset="0.45" stop-color="#7E9450"/><stop offset="1" stop-color="#3D5228"/></linearGradient>
<linearGradient id="gLeafD" x1="0" y1="0" x2="0.22" y2="1">
 <stop offset="0" stop-color="#9CB566"/><stop offset="0.5" stop-color="#647B3E"/><stop offset="1" stop-color="#32421F"/></linearGradient>
<linearGradient id="gStem" x1="0" y1="0" x2="1" y2="0">
 <stop offset="0" stop-color="#9CB466"/><stop offset="0.42" stop-color="#6B8340"/><stop offset="1" stop-color="#3E5226"/></linearGradient>
<radialGradient id="gSoil" cx="0.36" cy="0.2" r="0.92">
 <stop offset="0" stop-color="#D69D62"/><stop offset="0.42" stop-color="#7C563A"/><stop offset="1" stop-color="#513524"/></radialGradient>
<radialGradient id="gRed" cx="0.33" cy="0.26" r="0.86">
 <stop offset="0" stop-color="#EE7A64"/><stop offset="0.40" stop-color="#C93B2C"/><stop offset="0.80" stop-color="#A32F25"/><stop offset="1" stop-color="#64120C"/></radialGradient>
<radialGradient id="gGrn" cx="0.33" cy="0.26" r="0.86">
 <stop offset="0" stop-color="#C7D486"/><stop offset="0.45" stop-color="#8CA352"/><stop offset="1" stop-color="#4A6129"/></radialGradient>
<radialGradient id="gGold" cx="0.34" cy="0.28" r="0.86">
 <stop offset="0" stop-color="#FAF0A2"/><stop offset="0.45" stop-color="#D9BC48"/><stop offset="1" stop-color="#8A7A25"/></radialGradient>
<radialGradient id="gOrange" cx="0.33" cy="0.26" r="0.86">
 <stop offset="0" stop-color="#FFC078"/><stop offset="0.45" stop-color="#E88A34"/><stop offset="1" stop-color="#9A4E13"/></radialGradient>
<radialGradient id="gTan" cx="0.35" cy="0.28" r="0.88">
 <stop offset="0" stop-color="#EFD9A8"/><stop offset="0.5" stop-color="#C9A166"/><stop offset="1" stop-color="#8A6335"/></radialGradient>
<radialGradient id="gCream" cx="0.35" cy="0.28" r="0.88">
 <stop offset="0" stop-color="#FFF7E4"/><stop offset="0.55" stop-color="#EBD9B4"/><stop offset="1" stop-color="#B79B70"/></radialGradient>
<radialGradient id="gSeed" cx="0.35" cy="0.3" r="0.8">
 <stop offset="0" stop-color="#E8D2A6"/><stop offset="1" stop-color="#9A7742"/></radialGradient>
<filter id="bl12" x="-70%" y="-70%" width="240%" height="240%"><feGaussianBlur stdDeviation="11"/></filter>
<filter id="bl7"  x="-70%" y="-70%" width="240%" height="240%"><feGaussianBlur stdDeviation="7"/></filter>
<filter id="bl5"  x="-70%" y="-70%" width="240%" height="240%"><feGaussianBlur stdDeviation="5"/></filter>
'''
