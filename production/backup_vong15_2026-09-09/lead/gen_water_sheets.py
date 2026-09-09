from PIL import Image
import numpy as np, colorsys, os

SRC='/mnt/user-data/uploads/Cooking-Game-2D/Assets/maptitle/Map45Iso/Sheet_IsoWater45.png'
OUT='/mnt/user-data/outputs/v15/Assets/maptitle/Map45Iso/WaterAnim/'
os.makedirs(OUT,exist_ok=True)
sheet=np.array(Image.open(SRC).convert('RGBA')).astype(np.float64)
def cell(r,c): return sheet[r*80:(r+1)*80, c*128:(c+1)*128].copy()

H,W=80,128
yy,xx=np.mgrid[0:H,0:W]
# Toa do lattice iso: chu ky (128,0) va (0,64); moi song can m+n chan de lien mach khi lat gach.
px=xx/128.0
py=yy/64.0
F=8   # so frame

def wave(m,n,t,phase=0.0):
    return np.sin(2*np.pi*(m*px + n*py - t) + phase)

def light_field(t, style):
    if style=='calm':
        L  = 0.55*wave(1, 1, t)
        L += 0.30*wave(1,-1, t*0.62 + 0.25)
        L += 0.18*wave(2, 2, t*1.35 + 1.1)
        L += 0.10*wave(3, 1, t*0.8  + 2.2)
    elif style=='ripple':
        L  = 0.45*wave(2, 0, t*1.1)
        L += 0.38*wave(0, 2, t*0.7 + 0.9)
        L += 0.24*wave(1, 1, t*1.5 + 2.0)
        L += 0.12*wave(3,-1, t*0.9 + 0.4)
    elif style=='deep':
        L  = 0.60*wave(1, 1, t*0.55)
        L += 0.22*wave(1,-1, t*0.35 + 1.7)
        L += 0.10*wave(2, 2, t*0.9  + 0.3)
    else:  # 'shore' - song don dap, nhip nhanh hon
        L  = 0.50*wave(1,-1, t*1.25)
        L += 0.34*wave(2, 0, t*0.95 + 1.3)
        L += 0.22*wave(1, 1, t*1.6  + 0.6)
        L += 0.12*wave(2,-2, t*1.1  + 2.4)
    return L/1.13

def build(src_rc, style, amp_v, amp_s, spec, name, v_shift=0.0, s_shift=0.0):
    base=cell(*src_rc)
    rgb=base[...,:3]/255.0
    a=base[...,3]
    mask=a>0
    hsv=np.zeros_like(rgb)
    mx=rgb.max(2); mn=rgb.min(2); d=mx-mn
    v=mx
    s=np.where(mx>0, d/np.maximum(mx,1e-6), 0)
    # hue
    h=np.zeros_like(mx)
    r,g,b=rgb[...,0],rgb[...,1],rgb[...,2]
    nz=d>1e-6
    h=np.where(nz&(mx==r), ((g-b)/np.maximum(d,1e-6))%6, h)
    h=np.where(nz&(mx==g), (b-r)/np.maximum(d,1e-6)+2, h)
    h=np.where(nz&(mx==b), (r-g)/np.maximum(d,1e-6)+4, h)
    h=h/6.0

    frames=[]
    for f in range(F):
        t=f/float(F)
        L=light_field(t,style)
        v2=np.clip(v*(1.0+amp_v*L)+v_shift, 0, 1)
        s2=np.clip(s*(1.0-amp_s*L)+s_shift, 0, 1)
        # dai sang bac tren dinh song
        hi=np.clip((L-0.72)/0.28, 0, 1)**2
        v2=np.clip(v2+spec*hi, 0, 1)
        s2=np.clip(s2-0.35*spec*hi, 0, 1)
        # HSV -> RGB vector hoa
        i=np.floor(h*6.0)
        ff=h*6.0-i
        p=v2*(1-s2); q=v2*(1-s2*ff); tt=v2*(1-s2*(1-ff))
        i=i.astype(int)%6
        out=np.zeros_like(rgb)
        for k,(rr,gg,bb) in enumerate([(v2,tt,p),(q,v2,p),(p,v2,tt),(p,q,v2),(tt,p,v2),(v2,p,q)]):
            sel=i==k
            out[...,0]=np.where(sel,rr,out[...,0])
            out[...,1]=np.where(sel,gg,out[...,1])
            out[...,2]=np.where(sel,bb,out[...,2])
        frame=np.zeros((H,W,4))
        frame[...,:3]=np.where(mask[...,None], out*255.0, 0)
        frame[...,3]=a
        frames.append(frame)

    strip=np.concatenate(frames,axis=1).round().clip(0,255).astype(np.uint8)
    Image.fromarray(strip,'RGBA').save(OUT+name+'.png')
    return strip

# 4 sheet, TAT CA dung mask thoi 6504 giong het nhau -> lat gach khong ho
# TAT CA lay o r0c7 cua Sep: mat nuoc PHANG (khong co vignette hinh goi nhu hang 5),
# lat gach ra la mot mat ho lien tuc. Chi khac nhau kieu song + do dam nhat.
build((0,7),'calm',   0.105, 0.09, 0.07, 'Sheet_WaterAnim45_fill')
build((0,7),'ripple', 0.115, 0.10, 0.08, 'Sheet_WaterAnim45_fill_b')
build((0,7),'deep',   0.090, 0.07, 0.05, 'Sheet_WaterAnim45_deep',  v_shift=-0.085, s_shift=0.045)
build((0,7),'shore',  0.120, 0.10, 0.08, 'Sheet_WaterAnim45_edge',  v_shift= 0.050, s_shift=-0.030)
print('done')
