import math, sys; sys.path.insert(0,'.')
from crops import CW,CH,render_cell
from pg2 import *
from allcrops import R,T

BY=470; CX=256; HH=300          # baseline, centre, finished house height

def rect(x,y,w,h,r=0):
    if r<=0: return f'M {x:.1f},{y:.1f} L {x+w:.1f},{y:.1f} L {x+w:.1f},{y+h:.1f} L {x:.1f},{y+h:.1f} Z'
    return (f'M {x+r:.1f},{y:.1f} L {x+w-r:.1f},{y:.1f} Q {x+w:.1f},{y:.1f} {x+w:.1f},{y+r:.1f} '
            f'L {x+w:.1f},{y+h-r:.1f} Q {x+w:.1f},{y+h:.1f} {x+w-r:.1f},{y+h:.1f} '
            f'L {x+r:.1f},{y+h:.1f} Q {x:.1f},{y+h:.1f} {x:.1f},{y+h-r:.1f} '
            f'L {x:.1f},{y+r:.1f} Q {x:.1f},{y:.1f} {x+r:.1f},{y:.1f} Z')
def poly(pts): return "M "+" L ".join(f"{x:.1f},{y:.1f}" for x,y in pts)+" Z"

# ---------- roof shapes ----------
def d_roof(kind,cx,ytop,ybot,w,ov=1.14):
    hw=w*ov/2
    if kind=='gambrel':
        b=(ybot-ytop)
        return poly([(cx-hw,ybot),(cx-hw*0.66,ybot-b*0.52),(cx-hw*0.22,ytop),(cx+hw*0.22,ytop),
                     (cx+hw*0.66,ybot-b*0.52),(cx+hw,ybot)])
    if kind=='hip':
        return poly([(cx-hw,ybot),(cx-hw*0.34,ytop),(cx+hw*0.34,ytop),(cx+hw,ybot)])
    if kind=='broad':
        b=(ybot-ytop)
        return poly([(cx-hw,ybot),(cx-hw*0.92,ybot-b*0.14),(cx,ytop),(cx+hw*0.92,ybot-b*0.14),(cx+hw,ybot)])
    return poly([(cx-hw,ybot),(cx,ytop),(cx+hw,ybot)])   # gable / steep

def d_ridge(cx,y,w,h=13): return rect(cx-w/2,y-h*0.5,w,h,h*0.45)

# ---------- parts ----------
def win(cx,cy,w,h,frame,glass):
    sh=[(rect(cx-w/2,cy-h/2,w,h,3),frame,'#4A3A22')]
    dt=[f'<path d="{rect(cx-w/2+w*0.14,cy-h/2+h*0.14,w*0.72,h*0.72,2)}" fill="url(#{glass})" stroke="#2A3450" stroke-width="3"/>',
        f'<line x1="{cx:.1f}" y1="{cy-h*0.34:.1f}" x2="{cx:.1f}" y2="{cy+h*0.34:.1f}" stroke="#E8E2CE" stroke-width="3.4"/>',
        f'<line x1="{cx-w*0.34:.1f}" y1="{cy:.1f}" x2="{cx+w*0.34:.1f}" y2="{cy:.1f}" stroke="#E8E2CE" stroke-width="3.4"/>',
        f'<path d="M {cx-w*0.30:.1f},{cy-h*0.26:.1f} l {w*0.22:.1f},{h*0.26:.1f}" stroke="#FFFFFF" stroke-width="4" opacity="0.35" fill="none"/>']
    return sh,dt

def door(cx,by_,w,h,g):
    sh=[(rect(cx-w/2,by_-h,w,h,w*0.30),g,'#3A2A14')]
    dt=[f'<circle cx="{cx+w*0.26:.1f}" cy="{by_-h*0.46:.1f}" r="4.6" fill="#E8C86A" stroke="#8A6A20" stroke-width="2.4"/>',
        f'<path d="{rect(cx-w*0.28,by_-h*0.86,w*0.56,h*0.26,4)}" fill="url(#gGlass)" stroke="#2A3450" stroke-width="3"/>']
    return sh,dt

def scaffold(cx,by_,w,h,n=3):
    sh=[];dt=[]
    hw=w*0.58
    for i in range(2):
        x=cx-hw+i*(2*hw)
        sh.append((rect(x-6,by_-h,12,h,3),'gScaf','#4A3A18'))
    for k in range(n):
        y=by_-h*(0.30+0.32*k)
        sh.append((rect(cx-hw-6,y,2*hw+12,9,3),'gScaf','#4A3A18'))
    for k in range(2):
        x0=cx-hw; x1=cx+hw; y0=by_-h*0.30; y1=by_-h*0.94
        pts=[(x0,y0),(x0+13,y0),(x1,y1),(x1-13,y1)] if k==0 else [(x1,y0),(x1-13,y0),(x0,y1),(x0+13,y1)]
        sh.append((poly(pts),'gScaf','#4A3A18'))
    return sh,dt

def matpile(cx,by_,s=1.0):
    sh=[];dt=[]
    for k in range(3):
        sh.append((rect(cx-26*s+k*4*s,by_-14*s-k*11*s,52*s,11*s,2),'gBrick','#6B2A18'))
    sh.append((poly([(cx+52*s,by_),(cx+96*s,by_),(cx+74*s,by_-30*s)]),'gSand','#8A6A2A'))
    return sh,dt

def giftbox(cx,by_,w,h,wrap,rib,openv=0.0):
    """openv 0 = closed, 1 = opening"""
    sh=[];dt=[]
    bw,bh=w*1.12,h*0.94
    x0,y0=cx-bw/2, by_-bh
    if openv<0.5:
        sh.append((rect(x0,y0,bw,bh,10),wrap,'#5A2A18'))
        sh.append((rect(cx-bw*0.09,y0,bw*0.18,bh,4),rib,'#7A4A10'))
        sh.append((rect(x0,by_-bh*0.62,bw,bh*0.17,4),rib,'#7A4A10'))
        for s in (-1,1):
            sh.append((T(0,cx,y0+3,d_leaf(bw*0.26,bw*0.13,2,0.30*s)),rib,'#7A4A10'))
            sh.append((T(180,cx,y0+3,d_leaf(bw*0.26,bw*0.13,2,0.30*s)),rib,'#7A4A10'))
        sh.append((d_orb(cx,y0+1,bw*0.08,1.1,1.1),rib,'#7A4A10'))
        dt.append(f'<path d="M {x0+bw*0.10:.1f},{y0+bh*0.16:.1f} q {bw*0.16:.1f},{-bh*0.10:.1f} {bw*0.30:.1f},{-bh*0.07:.1f}" fill="none" stroke="#FFFFFF" stroke-width="9" opacity="0.22" stroke-linecap="round"/>')
    else:
        # side panels falling outward + lid tilted off
        for s,ang in ((-1,-20),(1,18)):
            sh.append((T(ang,cx+s*bw*0.30,by_-bh*0.04,rect(0,-bh*0.32,bw*0.28,bh*0.32,8)),wrap,'#5A2A18'))
        sh.append((T(-9,cx-bw*0.40,y0-bh*0.05,rect(0,0,bw*0.80,bh*0.14,7)),wrap,'#5A2A18'))
        sh.append((T(-9,cx-bw*0.40,y0-bh*0.05,rect(bw*0.32,-2,bw*0.14,bh*0.16,4)),rib,'#7A4A10'))
        sh.append((d_orb(cx+bw*0.02,y0-bh*0.13,bw*0.09,1.1,1.1),rib,'#7A4A10'))
        for k in range(7):
            a=k*51.4; rr=bw*(0.36+0.22*R(k+5,0,1))
            dt.append(f'<path d="M {cx+math.cos(math.radians(a))*rr:.1f},{by_-bh*0.62+math.sin(math.radians(a))*rr*0.7:.1f} l 0,-13" stroke="#FFE9A8" stroke-width="6" stroke-linecap="round" opacity="0.75"/>')
    return sh,dt

HDEFS='''
<linearGradient id="gScaf" x1="0" y1="0" x2="1" y2="0">
 <stop offset="0" stop-color="#C79A50"/><stop offset="0.45" stop-color="#946C2E"/><stop offset="1" stop-color="#4A3A18"/></linearGradient>
<linearGradient id="gBrick" x1="0" y1="0" x2="0" y2="1">
 <stop offset="0" stop-color="#C46A48"/><stop offset="1" stop-color="#8A3A22"/></linearGradient>
<linearGradient id="gSand" x1="0" y1="0" x2="0" y2="1">
 <stop offset="0" stop-color="#E8CE8A"/><stop offset="1" stop-color="#A8843A"/></linearGradient>
<radialGradient id="gGlass" cx="0.3" cy="0.25" r="0.9">
 <stop offset="0" stop-color="#6E86B8"/><stop offset="0.5" stop-color="#33456E"/><stop offset="1" stop-color="#1E2A46"/></radialGradient>
<linearGradient id="gStone" x1="0" y1="0" x2="0" y2="1">
 <stop offset="0" stop-color="#B4B49E"/><stop offset="1" stop-color="#6E7060"/></linearGradient>
<linearGradient id="gDeck" x1="0" y1="0" x2="0" y2="1">
 <stop offset="0" stop-color="#C98A4E"/><stop offset="1" stop-color="#7A4A22"/></linearGradient>
<linearGradient id="gFrame" x1="0" y1="0" x2="1" y2="0">
 <stop offset="0" stop-color="#D0A464"/><stop offset="0.5" stop-color="#A07434"/><stop offset="1" stop-color="#5A4018"/></linearGradient>
'''
