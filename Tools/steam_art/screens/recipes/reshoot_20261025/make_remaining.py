from make_recipes import *
def runoff(variant):
    a=floor_runoff()
    def transform(x,y):
        if variant%2:x=1-x
        if variant%3==1:y=1-y
        if variant%3==2:x+=.035
        return x,y
    return [(t,[transform(*p) for p in pts],s*(.85+.06*variant)) for t,pts,s in a]
plans=[]
# Loosely circular brush work across the dragon's face, older foam already thin on tile.
a=runoff(1)+lanes(1,[.21,.35,.49,.64,.78],.08,.89,1.25)
a+=lanes(2,[.33,.52,.70],.18,.84,1.6)
a += [(3,[(.24,.32),(.43,.37),(.66,.32),(.77,.51)],1.8),
      (4,[(.33,.30),(.41,.52),(.67,.57),(.75,.39),(.55,.31)],3.1),
      (4,[(.55,.31),(.34,.45),(.49,.64),(.72,.59)],2.9),
      (3,[(.28,.76),(.47,.79),(.70,.75)],1.3),
      (4,[(.73,.55),(.64,.43),(.49,.47),(.48,.60)],1.9)]
plans+=save_plan('s03_dragon_brush','rug_cartoon_dragon',537,(.39,.09,.64,.45,.09),a,4,(.48,.60))
# Dry scraper, a few adjacent imperfect pulls, dirt pushed to both sides rather than pristine tiles.
a=runoff(2)
for repeat in range(3):a+=lanes(0,[.26,.355,.45],.06,.88,1.65,turn=.027)
a += [(0,[(.53,.75),(.56,.56),(.54,.30)],1.6)]
plans+=save_plan('s04_green_scraper','rug_european_green',612,(.58,.60,.50,.42,.11),a,0,(.54,.30))
# Fox face cleaned with staggered overlaps; foam left mainly over the still-dirty right flank.
a=runoff(3)+lanes(0,[.29,.40,.51,.62,.73],.13,.82,1.6)
a+=lanes(1,[.28,.40,.52,.64,.76],.08,.88,1.25)
a+=lanes(2,[.30,.48,.66],.18,.82,1.7)
a+=lanes(6,[.33,.49,.65],.13,.85,1.9)
a += [(3,[(.63,.25),(.72,.42),(.68,.64),(.79,.82)],2.2),
      (4,[(.65,.29),(.57,.39),(.69,.47),(.74,.61)],2.6),
      (3,[(.73,.64),(.71,.49),(.78,.37)],1.2)]
plans+=save_plan('s05_fox_foam','rug_cartoon_fox',739,(.34,.13,.57,.36,.08),a,3,(.78,.37))
# A partly rinsed traditional carpet with unsystematic diagonal hose sweeps and a dark edge puddle.
a=runoff(4)+lanes(1,[.13,.28,.43,.58,.73,.86],.06,.92,1.15)
a += [(2,[(.18,.20),(.41,.37),(.29,.57),(.56,.73)],3.2),
      (2,[(.57,.71),(.75,.57),(.70,.30),(.86,.11)],2.7),
      (5,[(.25,.19),(.20,.07),(.12,-.09)],1.2),
      (2,[(.35,.43),(.53,.47),(.62,.39)],1.8)]
plans+=save_plan('s06_salor_hose','rug_salor_turkmen',813,(.42,.10,.78,.48,.16),a,2,(.62,.39))
# Cat, dirty wash water pushed diagonally through the near edge, visibly incomplete job.
a=runoff(5)+lanes(1,[.15,.30,.45,.60,.75,.87],.05,.94,1.1)
a+=lanes(2,[.15,.34,.53,.72,.86],.09,.87,1.7)
a += [(3,[(.19,.28),(.40,.38),(.66,.35),(.78,.55)],1.5),
      (4,[(.20,.28),(.43,.33),(.69,.29),(.75,.51),(.46,.56)],3.0),
      (5,[(.25,.77),(.23,.50),(.29,.18),(.24,-.08)],2.5),
      (5,[(.49,.82),(.52,.55),(.48,.28),(.56,-.06)],2.5),
      (5,[(.77,.84),(.73,.55),(.79,.32),(.80,.13)],2.3)]
plans+=save_plan('s07_cat_squeegee','rug_cartoon_cat',924,(.33,.055,.69,.35,.13),a,5,(.80,.13))
# Later job, a clean rya with an uneven damp corner; moisture lamp is an actual shop mechanic.
a=lanes(2,[.18,.44,.69,.86],.12,.90,1.6)
a+=lanes(6,[.14,.29,.44,.59,.74,.89],.05,.94,1.65)
a+=lanes(7,[.19,.42,.65],.04,.94,2.8)
a += [(7,[(.76,.25),(.83,.45),(.77,.67)],2.0)]
extra='CarpetCleaning.EconomyDebugApi.SetOwned(0,true); CarpetCleaning.CarpetDebugApi.SetHighlight(0,true);'
plans+=save_plan('s08_rya_moisture','rug_finnish_rya',1046,(0,0,0,0,0),a,7,(.77,.67),extra)
# Clean bright cartoon carpet with a few damp sweeping marks after final drying.
a=lanes(2,[.17,.40,.63,.84],.09,.91,1.0)
a+=lanes(6,[.13,.28,.43,.58,.73,.88],.04,.94,1.4)
a+=lanes(7,[.14,.36,.58,.80,.91],.03,.96,2.4)
a+=lanes(7,[.23,.48,.73],.06,.92,2.1)
plans+=save_plan('s09_axolotl_finish','rug_cartoon_axolotl',1171,(0,0,0,0,0),a,7,(.73,.92))
# Actual upgrade UI over a half-worked museum rug. Existing wallet is retained.
a=runoff(7)+lanes(0,[.22,.32,.42],.09,.87,1.5)+lanes(1,[.21,.34,.47],.08,.90,1.4)
extra='CarpetCleaning.EconomyDebugApi.ShopOpen(true); for(int i=0;i<30;i++)UnityEditor.EditorApplication.Step();'
plans+=save_plan('s10_transylvanian_shop','rug_transylvanian_medallion',1294,(.43,.28,.55,.50,.12),a,1,(.47,.90),extra)
(HERE/'remaining_order.json').write_text(json.dumps(plans,indent=2),encoding='utf-8')
print(f'{len(plans)} bounded eval recipes generated')
