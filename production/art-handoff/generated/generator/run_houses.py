import math, sys, os; sys.path.insert(0,'.')
from houses import *
from crops import CW,CH,render_cell
from pg2 import DEFS_CORE
from allcrops import EXTRA_DEFS

H_SPECS=[
 dict(id="house_01",name="House 01 - Teal Cottage", W=252, HH=366, roof='gambrel', rf=0.46, chim=False,
      roof_g='gRoof01', wall_g='gWall01', trim='#F5E7C8', door_g='gDoor01',
      porch=True, stone=True, wrap='gWrap01', rib='gRib01', wins=[(0,-0.66,54,46),(-0.30,-0.28,46,42),(0.30,-0.28,46,42)], deck=True),
 dict(id="house_02",name="House 02 - Blue Victorian", W=233, HH=405, roof='gable', rf=0.42, chim=False,
      roof_g='gRoof02', wall_g='gWall02', trim='#FFFFFF', door_g='gDoor02',
      porch=True, stone=False, wrap='gWrap02', rib='gRib02',
      wins=[(-0.28,-0.70,42,40),(0.28,-0.70,42,40),(-0.28,-0.40,42,44),(0.28,-0.40,42,44),(0,-0.70,38,36)], steps=True, belt=True),
 dict(id="house_03",name="House 03 - Amber Rustic", W=319, HH=397, roof='broad', rf=0.54, chim=False,
      roof_g='gRoof03', wall_g='gWall03', trim='#F2E2BC', door_g='gDoor03',
      porch=True, stone=False, wrap='gWrap03', rib='gRib03',
      wins=[(0,-0.70,50,42),(-0.32,-0.30,46,40),(0.32,-0.30,46,40)], deck=True, rail=True),
 dict(id="house_04",name="House 04 - Coral Townhouse", W=225, HH=388, roof='hip', rf=0.30, chim=False,
      roof_g='gRoof04', wall_g='gWall04', trim='#F6E7C8', door_g='gDoor04',
      porch=False, stone=False, wrap='gWrap04', rib='gRib04',
      wins=[(-0.29,-0.66,40,40),(0.29,-0.66,40,40),(0,-0.66,40,40),(-0.29,-0.36,40,40),(0.29,-0.36,40,40)], awning=True, belt=True),
 dict(id="house_05",name="House 05 - Timber Chalet", W=276, HH=370, roof='gable', rf=0.46, chim=False,
      roof_g='gRoof05', wall_g='gWall05', trim='#C98533', door_g='gDoor05',
      porch=True, stone=True, wrap='gWrap05', rib='gRib05',
      wins=[(0,-0.70,46,40),(-0.32,-0.30,40,36),(0.32,-0.30,40,36)], timber=True),
]

def build_house(S, walls=1.0, roof=1.0, detail=True):
    global HH
    HH=S.get('HH',300)
    """walls/roof in 0..1 = build progress"""
    sh=[];dt=[]; W=S['W']; rf=S['rf']
    wallH=HH*(1-rf); roofH=HH*rf
    wtop=BY-wallH*walls
    if S.get('stone'):
        sh.append((rect(CX-W/2-6,BY-18,W+12,18,4),'gStone','#4E4C40'))
    if walls>0.02:
        sh.append((rect(CX-W/2,wtop,W,BY-wtop,5),S['wall_g'],'#5A4A2A'))
    if detail and walls>0.55:
        if S.get('belt'):
            dt.append(f'<path d="{rect(CX-W/2,BY-wallH*0.52,W,10,2)}" fill="{S["trim"]}" opacity="0.92" stroke="#8A7A58" stroke-width="2"/>')
        if S.get('timber'):
            for k in range(3):
                x=CX-W/2+ (k+1)*W/4
                dt.append(f'<path d="{rect(x-8,wtop,16,BY-wtop,2)}" fill="url(#gFrame)" stroke="#5A4018" stroke-width="3"/>')
            dt.append(f'<path d="{rect(CX-W/2,BY-wallH*0.55,W,15,2)}" fill="url(#gFrame)" stroke="#5A4018" stroke-width="3"/>')
        for k in range(1,7):
            y=wtop+ (BY-wtop)*k/7
            dt.append(f'<line x1="{CX-W/2+3:.1f}" y1="{y:.1f}" x2="{CX+W/2-3:.1f}" y2="{y:.1f}" stroke="#00000022" stroke-width="3"/>')
    if roof>0.02:
        rtop=wtop-roofH*roof
        sh.append((d_roof(S['roof'],CX,rtop,wtop,W),S['roof_g'],'#3A2A16'))
        if roof>0.9:
            sh.append((d_ridge(CX,rtop+4,W*0.30),S['roof_g'],'#3A2A16'))
        if detail:
            for k in range(1,6):
                y=rtop+(wtop-rtop)*k/6
                hw=W*1.22/2*(k/6)*(1 if S['roof']!='gambrel' else 1)
                dt.append(f'<line x1="{CX-hw:.1f}" y1="{y:.1f}" x2="{CX+hw:.1f}" y2="{y:.1f}" stroke="#00000030" stroke-width="3.4"/>')
    if detail and walls>0.9 and roof>0.9:
        for fx,fy,fw,fh in S['wins']:
            s2,d2=win(CX+fx*W, BY+fy*HH, fw,fh, S['trim'] if S['trim'].startswith('#') else S['trim'],'gGlass')
            sh.append((s2[0][0],'gTrim','#5A4A2A')); dt+=d2
        s2,d2=door(CX, BY-4, 52, 78, S['door_g']); sh+=s2; dt+=d2
        if S.get('porch'):
            sh.append((rect(CX-56,BY-96,112,14,4),S['roof_g'],'#3A2A16'))
            for s in (-1,1):
                sh.append((rect(CX+s*48-6,BY-96,12,96,3),'gTrim','#5A4A2A'))
        if S.get('awning'):
            sh.append((rect(CX-46,BY-92,92,16,5),'gTrim','#5A4A2A'))
        if S.get('deck'):
            sh.append((rect(CX-70,BY-14,140,16,4),'gDeck','#5A3A16'))
        if S.get('steps'):
            for k in range(3):
                sh.append((rect(CX-40+k*5,BY-6-k*9,80-k*10,10,2),'gStone','#4E4C40'))
        if S.get('rail'):
            for s in (-1,1):
                sh.append((rect(CX+s*72-6,BY-58,12,58,3),'gDeck','#5A3A16'))
            sh.append((rect(CX-78,BY-58,156,10,3),'gDeck','#5A3A16'))
    return sh,dt

def stage(S,i):
    global HH
    HH=S.get('HH',300)
    W=S['W']
    if i==0:   # mong + khung
        sh=[(rect(CX-W/2-10,BY-20,W+20,20,4),'gStone','#4E4C40')]; dt=[]
        for k in range(4):
            x=CX-W/2+8+k*(W-16)/3
            sh.append((rect(x-9,BY-HH*0.60,18,HH*0.60,3),'gFrame','#5A4018'))
        sh.append((rect(CX-W/2-4,BY-HH*0.62,W+8,16,3),'gFrame','#5A4018'))
        sh.append((poly([(CX-W/2,BY-HH*0.62),(CX,BY-HH*0.92),(CX+W/2,BY-HH*0.62),(CX+W/2-16,BY-HH*0.62),(CX,BY-HH*0.84),(CX-W/2+16,BY-HH*0.62)]),'gFrame','#5A4018'))
        s2,d2=scaffold(CX,BY,W*0.92,HH*0.78); sh+=s2; dt+=d2
        return sh,dt
    if i==1:   # bat dau xay
        sh,dt=build_house(S,walls=0.34,roof=0.0,detail=True)
        s2,d2=scaffold(CX,BY,W*0.92,HH*0.80); sh+=s2; dt+=d2
        s3,d3=matpile(CX-W*0.66,BY,0.8); sh+=s3; dt+=d3
        return sh,dt
    if i==2:   # xay nua
        sh,dt=build_house(S,walls=1.0,roof=0.52,detail=True)
        s2,d2=scaffold(CX+W*0.20,BY,W*0.62,HH*0.92); sh+=s2; dt+=d2
        return sh,dt
    if i==3:   # hoan chinh
        return build_house(S,1.0,1.0,True)
    if i==4:   # goi hop qua
        return giftbox(CX,BY,S['W'],HH,S['wrap'],S['rib'],0.0)
    # i==5 mo hop
    sh,dt=build_house(S,1.0,1.0,True)
    s2,d2=giftbox(CX,BY,S['W'],HH,S['wrap'],S['rib'],1.0)
    return sh+s2, dt+d2

PAL='''
<linearGradient id="gTrim" x1="0" y1="0" x2="0" y2="1"><stop offset="0" stop-color="#FFF6E0"/><stop offset="1" stop-color="#D8C69E"/></linearGradient>
<linearGradient id="gRoof01" x1="0" y1="0" x2="0.3" y2="1"><stop offset="0" stop-color="#63A99C"/><stop offset="0.5" stop-color="#3F8478"/><stop offset="1" stop-color="#255049"/></linearGradient>
<linearGradient id="gWall01" x1="0" y1="0" x2="1" y2="0"><stop offset="0" stop-color="#FBEFD4"/><stop offset="0.55" stop-color="#F0DFBA"/><stop offset="1" stop-color="#CDB68E"/></linearGradient>
<linearGradient id="gDoor01" x1="0" y1="0" x2="1" y2="0"><stop offset="0" stop-color="#4E9188"/><stop offset="1" stop-color="#2A5D55"/></linearGradient>
<linearGradient id="gWrap01" x1="0" y1="0" x2="0.3" y2="1"><stop offset="0" stop-color="#F79A78"/><stop offset="0.5" stop-color="#E2674A"/><stop offset="1" stop-color="#9A3620"/></linearGradient>
<linearGradient id="gRib01" x1="0" y1="0" x2="1" y2="0"><stop offset="0" stop-color="#FBE08A"/><stop offset="0.5" stop-color="#E0B33E"/><stop offset="1" stop-color="#96741A"/></linearGradient>
<linearGradient id="gRoof02" x1="0" y1="0" x2="0.3" y2="1"><stop offset="0" stop-color="#4A5670"/><stop offset="0.5" stop-color="#2E3648"/><stop offset="1" stop-color="#1A2030"/></linearGradient>
<linearGradient id="gWall02" x1="0" y1="0" x2="1" y2="0"><stop offset="0" stop-color="#9FB2C6"/><stop offset="0.55" stop-color="#7E93A8"/><stop offset="1" stop-color="#5A6E83"/></linearGradient>
<linearGradient id="gDoor02" x1="0" y1="0" x2="1" y2="0"><stop offset="0" stop-color="#3E4C68"/><stop offset="1" stop-color="#212A40"/></linearGradient>
<linearGradient id="gWrap02" x1="0" y1="0" x2="0.3" y2="1"><stop offset="0" stop-color="#F5C86A"/><stop offset="0.5" stop-color="#DB9B2A"/><stop offset="1" stop-color="#8E5C10"/></linearGradient>
<linearGradient id="gRib02" x1="0" y1="0" x2="1" y2="0"><stop offset="0" stop-color="#E0705E"/><stop offset="0.5" stop-color="#B33528"/><stop offset="1" stop-color="#6E1810"/></linearGradient>
<linearGradient id="gRoof03" x1="0" y1="0" x2="0.3" y2="1"><stop offset="0" stop-color="#A0703F"/><stop offset="0.5" stop-color="#6B4126"/><stop offset="1" stop-color="#3E2414"/></linearGradient>
<linearGradient id="gWall03" x1="0" y1="0" x2="1" y2="0"><stop offset="0" stop-color="#EEB964"/><stop offset="0.55" stop-color="#D9963C"/><stop offset="1" stop-color="#A26824"/></linearGradient>
<linearGradient id="gDoor03" x1="0" y1="0" x2="1" y2="0"><stop offset="0" stop-color="#639548"/><stop offset="1" stop-color="#33582A"/></linearGradient>
<linearGradient id="gWrap03" x1="0" y1="0" x2="0.3" y2="1"><stop offset="0" stop-color="#6FA858"/><stop offset="0.5" stop-color="#3F7A38"/><stop offset="1" stop-color="#1E4420"/></linearGradient>
<linearGradient id="gRib03" x1="0" y1="0" x2="1" y2="0"><stop offset="0" stop-color="#FBE08A"/><stop offset="0.5" stop-color="#E0B33E"/><stop offset="1" stop-color="#96741A"/></linearGradient>
<linearGradient id="gRoof04" x1="0" y1="0" x2="0.3" y2="1"><stop offset="0" stop-color="#FBEED6"/><stop offset="0.5" stop-color="#EEDCB6"/><stop offset="1" stop-color="#BFA87E"/></linearGradient>
<linearGradient id="gWall04" x1="0" y1="0" x2="1" y2="0"><stop offset="0" stop-color="#F08E6C"/><stop offset="0.55" stop-color="#E0714F"/><stop offset="1" stop-color="#A64A2E"/></linearGradient>
<linearGradient id="gDoor04" x1="0" y1="0" x2="1" y2="0"><stop offset="0" stop-color="#96AE88"/><stop offset="1" stop-color="#5E7854"/></linearGradient>
<linearGradient id="gWrap04" x1="0" y1="0" x2="0.3" y2="1"><stop offset="0" stop-color="#A87CB4"/><stop offset="0.5" stop-color="#75478A"/><stop offset="1" stop-color="#402350"/></linearGradient>
<linearGradient id="gRib04" x1="0" y1="0" x2="1" y2="0"><stop offset="0" stop-color="#FFF6E0"/><stop offset="0.5" stop-color="#E8D8B0"/><stop offset="1" stop-color="#A8946A"/></linearGradient>
<linearGradient id="gRoof05" x1="0" y1="0" x2="0.3" y2="1"><stop offset="0" stop-color="#9CC078"/><stop offset="0.5" stop-color="#7EA05C"/><stop offset="1" stop-color="#4A6438"/></linearGradient>
<linearGradient id="gWall05" x1="0" y1="0" x2="1" y2="0"><stop offset="0" stop-color="#8A9E5E"/><stop offset="0.55" stop-color="#6E8049"/><stop offset="1" stop-color="#4A5830"/></linearGradient>
<linearGradient id="gDoor05" x1="0" y1="0" x2="1" y2="0"><stop offset="0" stop-color="#D69A4A"/><stop offset="1" stop-color="#8A5A20"/></linearGradient>
<linearGradient id="gWrap05" x1="0" y1="0" x2="0.3" y2="1"><stop offset="0" stop-color="#E8646A"/><stop offset="0.5" stop-color="#BE2E38"/><stop offset="1" stop-color="#6E1018"/></linearGradient>
<linearGradient id="gRib05" x1="0" y1="0" x2="1" y2="0"><stop offset="0" stop-color="#FBE08A"/><stop offset="0.5" stop-color="#E0B33E"/><stop offset="1" stop-color="#96741A"/></linearGradient>
'''
LBL=["1 mong+khung","2 bat dau xay","3 xay nua","4 HOAN CHINH","5 hop qua","6 mo hop"]
os.makedirs('out/h',exist_ok=True); os.makedirs('out/hraw',exist_ok=True)
for S in H_SPECS:
    defs=[];body=[];rd=[];rb=[]
    for i in range(6):
        sh,dt=stage(S,i)
        df,bd=render_cell(sh,dt,f"h{S['id']}{i}",16,True); defs.append(df); body.append(f'<g transform="translate({i*CW},0)">{bd}</g>')
        df2,bd2=render_cell(sh,dt,f"q{S['id']}{i}",16,True); rd.append(df2); rb.append(f'<g transform="translate({i*CW},0)">{bd2}</g>')
    W=CW*6;Ht=CH+78
    o=[f'<svg xmlns="http://www.w3.org/2000/svg" width="{W}" height="{Ht}" viewBox="0 0 {W} {Ht}">',
       f'<defs>{DEFS_CORE}{EXTRA_DEFS}{HDEFS}{PAL}{"".join(defs)}</defs>',
       f'<rect width="{W}" height="{Ht}" fill="#FBF3E2"/>']
    for i in range(1,6): o.append(f'<line x1="{i*CW}" y1="8" x2="{i*CW}" y2="{CH-4}" stroke="#E2D0AC" stroke-width="2" stroke-dasharray="8 8"/>')
    o.append(f'<line x1="0" y1="{BY}" x2="{W}" y2="{BY}" stroke="#C9A96E" stroke-width="3" stroke-dasharray="14 9"/>')
    o+=body
    for i in range(6):
        o.append(f'<text x="{i*CW+CW/2}" y="{CH+32}" text-anchor="middle" font-family="DejaVu Sans" font-size="21" font-weight="700" fill="#8A6A44">{LBL[i]}</text>')
    o.append(f'<text x="16" y="32" font-family="DejaVu Sans" font-size="24" font-weight="700" fill="#8A6A44">{S["name"]}</text>')
    o.append('</svg>')
    open(f'out/h/{S["id"]}.html','w').write('<html><head><style>html,body{margin:0;padding:0;background:#FBF3E2}svg{display:block}</style></head><body>'+"\n".join(o)+'</body></html>')
    raw=(f'<svg xmlns="http://www.w3.org/2000/svg" width="{CW*6}" height="{CH}" viewBox="0 0 {CW*6} {CH}">'
         f'<defs>{DEFS_CORE}{EXTRA_DEFS}{HDEFS}{PAL}{"".join(rd)}</defs>{"".join(rb)}</svg>')
    open(f'out/hraw/{S["id"]}.html','w').write('<html><head><style>html,body{margin:0;padding:0;background:transparent}svg{display:block}</style></head><body>'+raw+'</body></html>')
print("houses built:",len(H_SPECS))
