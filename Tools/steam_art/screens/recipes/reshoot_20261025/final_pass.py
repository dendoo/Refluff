"""Final alternating macro/wide capture pass, preserving real simulated floor mess between jobs."""
from make_recipes import *
import re
order=[];meta=[]
# The floor starts with real dirty rinse water from a preceding rug. It is never painted or cleared between final jobs.
write('floor_start','UnityEditor.EditorApplication.isPaused=true; CarpetCleaning.EconomyDebugApi.ShopOpen(false); CarpetCleaning.CarpetDebugApi.Reset(233,.95f); return CarpetCleaning.CarpetDebugApi.DirtJson();')
floor=[(2,[(.10,.13),(.38,.16),(.66,.12),(.90,.15)],3.0),
 (2,[(.88,.04),(.62,.07),(.35,.035),(.09,.06)],3.0),
 (4,[(.13,.12),(.38,.10),(.65,.14),(.86,.09)],2.4),
 (5,[(.19,.30),(.18,.13),(.15,-.17)],1.8),
 (5,[(.46,.32),(.49,.12),(.45,-.18)],1.8),
 (5,[(.78,.31),(.76,.11),(.82,-.16)],1.8),
 (2,[(.95,.38),(.99,.19),(.97,.03)],2.1),
 (5,[(.80,.32),(.98,.30),(1.18,.27)],1.6)]
order=['floor_start.cs']+batches('floor_mess',floor)
poses={
2: ((-.12,.27,-1.035),(-.12,.012,-.64),51),
4: ((.07,.24,-.99),(.07,.012,-.65),51),
6: ((.43,.22,-.96),(.43,.012,-.65),52),
8: ((.55,.20,-.08),(.45,.012,.23),49),
10:((-.20,.23,-.99),(-.15,.012,-.63),51),
}
for slot,old in enumerate(['s02_anatolian_extract','s03_dragon_brush','s04_green_scraper','s05_fox_foam','s06_salor_hose','s07_cat_squeegee','s08_rya_moisture','s09_axolotl_finish','s10_transylvanian_shop'],2):
    j=json.loads((HERE/(old+'.json')).read_text());tag='r'+old[1:]
    if slot==10:tag='r10_transylvanian_vacuum'
    seq=[]
    initial=(HERE/(old+'_init.cs')).read_text()
    seed=j['seed'];level,crust,mud,dust,spill=j['profile']
    # A natural new job retains the surrounding floor. Reset only the carpet, then restart its job clock.
    replacement=f'''var d=CarpetCleaning.CarpetSimulation.RollDirt({seed},{fl(level)});
d.level={fl(level)};d.crust={fl(crust)};d.mud={fl(mud)};d.dust={fl(dust)};d.spill={fl(spill)};
CarpetCleaning.ToolTuning.EnterScriptedRun();
CarpetCleaning.CarpetDebugApi.Sim.ResetCarpet({seed},false,d);
CarpetCleaning.CarpetDebugApi.HandoverSnap();
typeof(CarpetCleaning.Economy).GetMethod("StartClock",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).Invoke(CarpetCleaning.EconomyDebugApi.Eco,null);'''
    initial=re.sub(r'CarpetCleaning\.CarpetDebugApi\.ResetWithProfile\([^;]+;',replacement,initial)
    seq.append(write(tag+'_init',initial))
    actions=[(x['tool'],x['points'],x['seconds']) for x in j['strokes']]
    # Keep the original wide views. In the macro shots move the real tool to the photographed near-edge detail.
    tool=j['tool'];end=j['end'];extra=''
    if slot==4:
        actions += [(0,[(.53,.40),(.55,.28),(.54,.15)],1.7)];end=(.54,.15)
    if slot==6:
        actions += [(5,[(.79,.41),(.85,.20),(.89,-.09)],1.5),
                    (2,[(.77,.26),(.86,.19),(.84,.12)],1.6)];end=(.84,.12)
    if slot==8:
        # Plain light keeps the wool legible; the remaining damp corner is visible without the inspection overlay.
        actions += [(7,[(.77,.55),(.76,.65),(.79,.72)],1.0)];end=(.79,.72)
    if slot==10:
        actions += [(1,[(.25,.36),(.28,.25),(.26,.12)],2.3)];tool=1;end=(.26,.12)
    # Fresh final plow takes dirty wash water off the cartoon carpets too; retain the foam where the tool last worked.
    if slot==3:
        actions += [(5,[(.50,.45),(.47,.20),(.44,-.13)],1.7),
                    (4,[(.41,.46),(.49,.54),(.48,.60)],1.0)];end=(.48,.60)
    if slot==5:
        actions += [(5,[(.57,.43),(.75,.26),(1.10,.17)],2.0),
                    (3,[(.73,.64),(.71,.49),(.78,.37)],.8)];end=(.78,.37)
    seq+=batches(tag,actions)
    finish(tag,tool,*end)
    shot=(HERE/(tag+'_shot.cs')).read_text()
    if slot in poses:
        pos,target,fov=poses[slot]
        cp='new UnityEngine.Vector3('+','.join(fl(x) for x in pos)+')'
        tp='new UnityEngine.Vector3('+','.join(fl(x) for x in target)+')'
        replacement=f'''var cp={cp};var ce=UnityEngine.Quaternion.LookRotation({tp}-cp).eulerAngles;
var sr=CarpetCleaning.StoreShotApi.RenderView("shots/{tag}_scene.png",3840,2160,cp.x,cp.y,cp.z,ce.x,ce.y,ce.z,{fl(fov)});
var hr=CarpetCleaning.EconomyDebugApi.CaptureHud("C:/UnityProjects/CarpetCleaning/SteamStore/_work/shots/{tag}_hud.png",2560,1440);
return sr+hr'''
        shot=shot.replace(f'return CarpetCleaning.StoreShotApi.Shot("{tag}")',replacement)
    seq.append(write(tag+'_shot',shot))
    order+=seq
    meta.append(dict(slot=slot,tag=tag,rug=j['rug'],view='macro' if slot in poses else 'wide',pose=poses.get(slot),tool=tool,recipes=seq,category='Gameplay'))
(HERE/'final_order.json').write_text(json.dumps(meta,indent=2),encoding='utf-8')
(HERE/'final_run_order.json').write_text(json.dumps(order,indent=2),encoding='utf-8')
print(f'{len(order)} final-pass recipes, macro at 2/4/6/8/10, fixed wide at 3/5/7/9')
