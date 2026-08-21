import sys, os; sys.path.insert(0,'.')
from run_all import CROPS, cell_svg
from crops import CW,CH,BY,CX,HGT
from pg2 import DEFS_CORE
from allcrops import EXTRA_DEFS
os.makedirs('out/raw',exist_ok=True)
for slug,name,fn,cfg in CROPS:
    defs=[];body=[]
    for s in range(5):
        df,bd = cell_svg(slug,fn,cfg,s,f"x{slug}{s}")
        defs.append(df); body.append(f'<g transform="translate({s*CW},0)">{bd}</g>')
    svg=(f'<svg xmlns="http://www.w3.org/2000/svg" width="{CW*5}" height="{CH}" viewBox="0 0 {CW*5} {CH}">'
         f'<defs>{DEFS_CORE}{EXTRA_DEFS}{"".join(defs)}</defs>{"".join(body)}</svg>')
    open(f'out/raw/{slug}.html','w').write(
        '<html><head><style>html,body{margin:0;padding:0;background:transparent}svg{display:block}</style></head><body>'+svg+'</body></html>')
print("raw html:",len(CROPS))
