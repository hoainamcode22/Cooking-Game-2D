import sys,os; sys.path.insert(0,'.')
for f in ["huong_duong","hoa_hong","hoa_oai_huong","hoa_lan","hoa_cuc_trang","tulip","hoa_cuc_van_tho","hoa_mau_don","hoa_cam_tu_cau","hoa_anh_thao"]:
    os.system(f'/opt/pw-browsers/chromium-1194/chrome-linux/chrome --headless=new --no-sandbox --disable-gpu --hide-scrollbars --window-size=2620,560 --default-background-color=00000000 --screenshot=/home/claude/art/out/fraw/{f}.png /home/claude/art/out/fraw/{f}.html 2>/dev/null')
