"""Save every actual stroke as reproducible C# eval scripts, in bounded chunks."""
from pathlib import Path
import json, math
HERE=Path(__file__).resolve().parent
HEADER='UnityEditor.EditorApplication.isPaused=true;\nvar ad=System.AppDomain.CurrentDomain;\nvar path=(System.Action<int,UnityEngine.Vector2[],float>)ad.GetData("screens2510Path");\n'
def fl(x): return f'{x:.5f}f'
def call(tool,pts,sec):
    return f'path({tool},new UnityEngine.Vector2[]{{'+','.join(f'new UnityEngine.Vector2({fl(x)},{fl(y)})' for x,y in pts)+'},'+fl(sec)+');\n'
def write(name,code):
    p=HERE/(name+'.cs');p.write_text(code,encoding='utf-8');return p.name
def batches(tag,actions):
    result=[];code=HEADER;secs=0;i=1
    for a in actions:
        tool,pts,duration=a
        if secs+duration>5 and secs>0:
            result.append(write(f'{tag}_{i:02}',code+'return CarpetCleaning.CarpetDebugApi.StatsJson()+" floor="+CarpetCleaning.CarpetDebugApi.FloorStatsJson();\n'));i+=1;code=HEADER;secs=0
        code+=call(tool,pts,duration);secs+=duration
    if secs:result.append(write(f'{tag}_{i:02}',code+'return CarpetCleaning.CarpetDebugApi.StatsJson()+" floor="+CarpetCleaning.CarpetDebugApi.FloorStatsJson();\n'))
    return result
def init(tag,rug,seed,level,crust,mud,dust,spill):
    folder='Resources/CarpetPictures/' if 'cartoon' in rug else ''
    ext='png' if 'cartoon' in rug else 'jpg'
    code=f'''UnityEditor.EditorApplication.isPaused=true;
CarpetCleaning.EconomyDebugApi.ShopOpen(false);
CarpetCleaning.CarpetDebugApi.SetHighlight(0,false);
CarpetCleaning.CarpetDebugApi.SetHighlight(1,false);
CarpetCleaning.CarpetDebugApi.UnpinToolHead();
CarpetCleaning.CarpetDebugApi.ResetWithProfile({seed},{fl(level)},{fl(crust)},{fl(mud)},{fl(dust)},{fl(spill)});
var cr=UnityEngine.Object.FindFirstObjectByType<CarpetCleaning.CarpetRenderer>();
var mat=cr.ShellsRenderer.sharedMaterial;
var tex=UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.Texture2D>("Assets/CarpetCleaning/Textures/{folder}{rug}.{ext}");
UnityEngine.Vector4 off,scale;CarpetCleaning.CarpetHandover.PictureFit(tex,out off,out scale);
mat.SetTexture("_PictureTex",tex);mat.SetVector("_PicOffset",off);mat.SetVector("_PicScale",scale);
var t=CarpetCleaning.CarpetDebugApi.Tools;t.ScriptedPointer=true;t.ScriptedUv=new UnityEngine.Vector2(-5,-5);t.ScriptedDown=false;
return CarpetCleaning.CarpetDebugApi.DirtJson();
'''
    return write(tag+'_init',code)
def finish(tag,tool,u,v,working=True,extra=''):
    # Let HUD settle with sims paused (no unexpected OS pointer). Final active stroke is appended after the settle.
    code=f'''UnityEditor.EditorApplication.isPaused=true;
var t=CarpetCleaning.CarpetDebugApi.Tools;
t.ScriptedPointer=true;t.ScriptedUv=new UnityEngine.Vector2(-5,-5);t.ScriptedDown=false;t.ScriptedRight=false;
CarpetCleaning.CarpetDebugApi.SetTool({tool});
CarpetCleaning.CarpetDebugApi.PinToolHead({tool},{fl(u)},{fl(v)},{str(working).lower()});
CarpetCleaning.CarpetDebugApi.Sim.RefreshStatsSync();
CarpetCleaning.CarpetDebugApi.Sim.Paused=true;
for(int i=0;i<35;i++)UnityEditor.EditorApplication.Step();
CarpetCleaning.CarpetDebugApi.Sim.Paused=false;
{extra}
return CarpetCleaning.StoreShotApi.Shot("{tag}")+" stats="+CarpetCleaning.CarpetDebugApi.StatsJson()+" floor="+CarpetCleaning.CarpetDebugApi.FloorStatsJson();
'''
    return write(tag+'_shot',code)
def lanes(tool,xs,y0=.09,y1=.91,seconds=1.6,turn=.018):
    result=[]
    for i,x in enumerate(xs):
        lo=y0+.015*math.sin(i*2.1);hi=y1-.024*math.sin(i*1.7)**2
        pts=[(x,lo),(x+turn,lo+(hi-lo)*.43),(x-turn*.4,hi)]
        if i%2:pts.reverse()
        result.append((tool,pts,seconds+.14*math.sin(i*1.9)))
    return result
def floor_runoff():
    return [(0,[(.11,.13),(.12,.035),(.10,-.055)],.8),(0,[(.32,.14),(.34,.02),(.36,-.065)],.9),(0,[(.84,.34),(.98,.31),(1.10,.29)],.9),
      (2,[(.08,.025),(.25,.045),(.42,.015)],1.5),(2,[(.86,.33),(.975,.29),(.99,.10)],1.8),
      (5,[(.26,.23),(.24,.10),(.21,-.10)],1.0),(5,[(.81,.28),(.95,.27),(1.13,.25)],1.1)]
def save_plan(tag,rug,seed,profile,actions,tool,end,extra=''):
    files=[init(tag,rug,seed,*profile)]+batches(tag,actions)+[finish(tag,tool,*end,extra=extra)]
    manifest={'tag':tag,'rug':rug,'seed':seed,'profile':profile,'tool':tool,'end':end,'seconds':sum(a[2] for a in actions),'recipes':files,'strokes':[{'tool':a[0],'points':a[1],'seconds':a[2]} for a in actions]}
    (HERE/(tag+'.json')).write_text(json.dumps(manifest,indent=2),encoding='utf-8')
    return files
if __name__=='__main__':
    tag='s02_anatolian_extract'
    a=floor_runoff()
    for k in range(3):a+=lanes(0,[.17,.255,.34,.425,.51],.10,.84,1.65)
    a+=lanes(1,[.15,.23,.32,.42,.52],.06,.87,1.25)
    a+=lanes(2,[.16,.31,.46],.10,.83,1.6)
    a+=lanes(3,[.20,.40],.15,.80,1.0)
    a+=lanes(4,[.18,.26,.34,.42,.50],.12,.82,1.7)
    for k in range(3):a+=lanes(6,[.18,.30,.42,.51],.08,.86,2.1)
    a+=lanes(7,[.21,.42],.10,.83,2.1)
    a+=[(6,[(.40,.80),(.42,.54),(.39,.24)],2.4)]
    print('\n'.join(save_plan(tag,'rug_anatolian_star',394,(.63,.45,.72,.55,.20),a,6,(.39,.24))))
