using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

namespace CarpetCleaning
{
    /// <summary>Authored workshop dressing. All geometry is generated here, baked to reusable assets,
    /// and combined by zone/material. It never reads or changes carpet assets or simulation state.</summary>
    public static class WorkshopDetailBuilder
    {
        const string RootDir = "Assets/CarpetCleaning/Models/WorkshopGenerated";
        const string MatDir = "Assets/CarpetCleaning/Materials/Workshop/Details";
        static readonly Dictionary<string, Mesh> Meshes = new Dictionary<string, Mesh>();
        static readonly List<Mesh> TemporaryMeshes = new List<Mesh>();
        static Material oak, sage, iron, rubber, cream, brass, chrome, clay, leaf, leafLight;
        static Material blue, ochre, paper, linen, soil, glow, red;

        public static void Build(Transform workshop)
        {
            EnsureFolder(RootDir); EnsureFolder(MatDir);
            var old = workshop.Find("WorkshopDressing");
            if (old != null) Object.DestroyImmediate(old.gameObject);
            var root = Group(workshop, "WorkshopDressing", Vector3.zero);
            Meshes.Clear(); TemporaryMeshes.Clear();
            oak = Material("OiledOak", new Color(.49f,.31f,.15f), 0, .28f, 1);
            sage = Material("SageEnamel", new Color(.21f,.32f,.27f), .22f, .38f, 2);
            iron = Material("GraphiteSteel", new Color(.085f,.105f,.10f), .65f, .33f, 2);
            rubber = Material("CharcoalRubber", new Color(.035f,.042f,.038f), 0, .16f, 2);
            cream = Material("WarmPorcelain", new Color(.83f,.79f,.64f), .06f, .64f, 2);
            brass = Material("AgedBrass", new Color(.50f,.35f,.13f), .75f, .42f, 2);
            chrome = Material("BrushedSteel", new Color(.53f,.56f,.53f), .82f, .55f, 2);
            clay = Material("Terracotta", new Color(.53f,.235f,.13f), 0, .13f, 2);
            leaf = Material("LeafDeepGreen", new Color(.07f,.19f,.085f), 0, .38f, 2);
            leafLight = Material("LeafSageGreen", new Color(.20f,.31f,.10f), 0, .32f, 2);
            blue = Material("MutedBlueBottle", new Color(.13f,.30f,.36f), .05f, .48f, 2);
            ochre = Material("OchreBottle", new Color(.68f,.43f,.12f), .03f, .36f, 2);
            paper = Material("KraftPaper", new Color(.54f,.40f,.24f), 0, .08f, 2);
            linen = Material("CottonCloth", new Color(.63f,.61f,.48f), 0, .08f, 2);
            soil = Material("PottingSoil", new Color(.065f,.047f,.029f), 0, .02f, 2);
            red = Material("OxideRed", new Color(.43f,.10f,.055f), .10f, .34f, 2);
            glow = Material("TaskLampGlass", new Color(1f,.69f,.31f), 0, .4f, 0);
            glow.SetColor("_EmissionColor",new Color(1f,.66f,.26f)*2.5f);
            glow.globalIlluminationFlags=MaterialGlobalIlluminationFlags.RealtimeDirectEmission;
            glow.EnableKeyword("_EMISSION"); EditorUtility.SetDirty(glow);

            Storage(Group(root,"NorthStorage",new Vector3(-3.38f,0,5.42f)));
            WashStation(Group(root,"WashStation",new Vector3(2.45f,0,5.45f)));
            BenchDetails(Group(root,"BenchDetails",Vector3.zero));
            Cart(Group(root,"ServiceCart",new Vector3(4.75f,0,-1.8f),-14f));
            WallServices(Group(root,"WallServices",Vector3.zero));
            WindowGarden(Group(root,"WindowGarden",Vector3.zero));
            SmallStorage(Group(root,"CabinetDetails",new Vector3(4.8f,0,4.8f)));
            WorkNotes(Group(root,"WorkNotes",Vector3.zero));
            var baseOak=AssetDatabase.LoadAssetAtPath<Material>("Assets/CarpetCleaning/Materials/Workshop/Oak.mat");
            if(baseOak!=null) ApplyOak(baseOak);
            var ceilingTimber=AssetDatabase.LoadAssetAtPath<Material>("Assets/CarpetCleaning/Materials/Workshop/CeilingTimber.mat");
            if(ceilingTimber!=null) ApplyOak(ceilingTimber);

            for(int i=0;i<root.childCount;i++) CombineZone(root.GetChild(i));
            foreach(var m in TemporaryMeshes) if(m!=null) Object.DestroyImmediate(m);
            TemporaryMeshes.Clear(); Meshes.Clear();
            AssetDatabase.SaveAssets();
        }

        static void Storage(Transform p)
        {
            // A real open steel rack: slender legs, bolted braces, oak shelf boards and mixed stock.
            const float width=3.1f, depth=.69f;
            for(int x=-1;x<=1;x+=2) for(int z=-1;z<=1;z+=2)
            {
                Box(p,"RackUpright",new Vector3(x*width*.5f,1.40f,z*depth*.5f),new Vector3(.055f,2.8f,.055f),iron,.006f);
                Box(p,"RackFoot",new Vector3(x*width*.5f,.025f,z*depth*.5f),new Vector3(.13f,.05f,.13f),rubber,.012f);
            }
            for(int level=0;level<5;level++)
            {
                float y=.24f+level*.56f;
                Box(p,"ShelfOak",new Vector3(0,y,0),new Vector3(3.18f,.055f,.76f),oak,.009f);
                Box(p,"ShelfFrontRail",new Vector3(0,y-.035f,-.35f),new Vector3(3.1f,.07f,.025f),sage,.004f);
                for(int x=-1;x<=1;x+=2)
                {
                    Box(p,"ShelfSupport",new Vector3(x*1.53f,y-.06f,0),new Vector3(.025f,.10f,.65f),iron,.004f);
                    Disk(p,"ShelfBolt",new Vector3(x*1.55f,y-.035f,-.386f),.012f,.007f,brass,Quaternion.Euler(90,0,0));
                }
            }
            Tube(p,"RackDiagonal",new[]{new Vector3(-1.50f,.2f,.35f),new Vector3(1.50f,2.70f,.35f)},.012f,iron);
            Tube(p,"RackDiagonal",new[]{new Vector3(1.50f,.2f,.35f),new Vector3(-1.50f,2.70f,.35f)},.012f,iron);
            Collider(p,"RackCollision",new Vector3(0,1.4f,0),new Vector3(3.25f,2.8f,.78f));

            // Floor shelf: tactile slatted wood boxes and rolled cloths.
            Crate(p,new Vector3(-.93f,.27f,0),new Vector3(.87f,.42f,.59f),0);
            Crate(p,new Vector3(.10f,.27f,.025f),new Vector3(.88f,.38f,.60f),0);
            Bucket(p,new Vector3(1.08f,.28f,-.015f),.23f,.43f,sage);
            for(int i=0;i<5;i++) Bottle(p,new Vector3(-1.25f+i*.21f,.83f,-.09f),.075f,.34f,i%2==0?ochre:blue,i%3);
            for(int i=0;i<3;i++) Can(p,new Vector3(.10f+i*.38f,.83f,-.03f),.145f,.39f,cream,i);
            FoldedCloths(p,new Vector3(1.23f,.83f,0),.45f,.47f,4);
            for(int i=0;i<3;i++) Carton(p,new Vector3(-1.13f+i*.50f,1.39f,0),new Vector3(.44f,.38f,.52f),i*3-3);
            Bottle(p,new Vector3(.47f,1.39f,-.10f),.095f,.43f,sage,0);
            Bottle(p,new Vector3(.77f,1.39f,.03f),.10f,.40f,ochre,1);
            Bottle(p,new Vector3(1.11f,1.39f,-.07f),.10f,.43f,blue,2);
            for(int i=0;i<4;i++) Can(p,new Vector3(-1.25f+i*.34f,1.95f,-.025f),.13f,.40f,sage,i);
            FoldedCloths(p,new Vector3(.53f,1.95f,.02f),.62f,.49f,5);
            FoldedCloths(p,new Vector3(1.18f,1.95f,-.02f),.49f,.44f,4);
            Carton(p,new Vector3(-.93f,2.51f,0),new Vector3(.92f,.44f,.61f),-4);
            Carton(p,new Vector3(.1f,2.51f,.02f),new Vector3(.82f,.37f,.59f),3);
            Bucket(p,new Vector3(1.05f,2.51f,0),.23f,.38f,cream);
        }

        static void WashStation(Transform p)
        {
            // Glazed utility sink, open drain space and exposed copper plumbing.
            for(int x=-1;x<=1;x+=2) for(int z=-1;z<=1;z+=2)
                Box(p,"SinkLeg",new Vector3(x*.78f,.44f,z*.30f),new Vector3(.055f,.88f,.055f),iron,.006f);
            Box(p,"SinkShelf",new Vector3(0,.23f,0),new Vector3(1.62f,.045f,.66f),oak,.01f);
            Box(p,"SinkRightCounter",new Vector3(.60f,.91f,0),new Vector3(.71f,.07f,.86f),oak,.014f);
            Box(p,"SinkLeftCounter",new Vector3(-.91f,.91f,0),new Vector3(.10f,.07f,.86f),oak,.014f);
            Box(p,"SinkBackCounter",new Vector3(-.30f,.91f,.385f),new Vector3(1.12f,.07f,.09f),oak,.012f);
            Box(p,"SinkFrontCounter",new Vector3(-.30f,.91f,-.405f),new Vector3(1.12f,.07f,.05f),oak,.009f);
            // Basin parts overlap invisibly, with a genuinely recessed interior instead of a black painted rectangle.
            Box(p,"BasinBottom",new Vector3(-.30f,.825f,-.03f),new Vector3(.88f,.08f,.60f),cream,.045f);
            Box(p,"BasinFront",new Vector3(-.30f,.94f,-.35f),new Vector3(1.04f,.30f,.105f),cream,.035f);
            Box(p,"BasinBack",new Vector3(-.30f,.98f,.29f),new Vector3(1.04f,.38f,.105f),cream,.035f);
            Box(p,"BasinLeft",new Vector3(-.79f,.94f,-.03f),new Vector3(.105f,.30f,.65f),cream,.035f);
            Box(p,"BasinRight",new Vector3(.19f,.94f,-.03f),new Vector3(.105f,.30f,.65f),cream,.035f);
            Disk(p,"DrainSteel",new Vector3(-.30f,.873f,-.03f),.064f,.008f,chrome,Quaternion.identity);
            for(int i=0;i<6;i++) {float a=i*Mathf.PI/3; Disk(p,"DrainHole",new Vector3(-.30f+Mathf.Cos(a)*.033f,.879f,-.03f+Mathf.Sin(a)*.033f),.008f,.003f,rubber,Quaternion.identity);}
            Tube(p,"SwanNeckTap",new[]{new Vector3(-.30f,1.10f,.30f),new Vector3(-.30f,1.46f,.30f),new Vector3(-.30f,1.53f,.25f),new Vector3(-.30f,1.53f,.10f),new Vector3(-.30f,1.47f,.04f)},.023f,chrome);
            for(int i=-1;i<=1;i+=2)
            {
                Disk(p,"TapBase",new Vector3(-.30f+i*.20f,1.16f,.29f),.05f,.035f,chrome,Quaternion.identity);
                Box(p,"TapLever",new Vector3(-.30f+i*.20f,1.20f,.26f),new Vector3(.10f,.022f,.025f),brass,.009f);
            }
            Tube(p,"DrainTrap",new[]{new Vector3(-.30f,.82f,-.03f),new Vector3(-.30f,.51f,-.03f),new Vector3(-.30f,.43f,.06f),new Vector3(-.30f,.43f,.22f),new Vector3(-.30f,.60f,.31f),new Vector3(-.30f,.60f,.50f)},.032f,chrome);
            Box(p,"TileBackboard",new Vector3(0,1.35f,.47f),new Vector3(2.18f,.92f,.025f),sage,.01f);
            for(int row=0;row<4;row++) for(int col=0;col<9;col++)
                Box(p,"GlazedBacksplashTile",new Vector3(-.96f+col*.24f,1.02f+row*.22f,.444f),new Vector3(.229f,.209f,.016f),row%2==0?cream:linen,.006f);
            Bottle(p,new Vector3(.49f,.955f,.17f),.075f,.32f,ochre,2);
            Bottle(p,new Vector3(.72f,.955f,.13f),.07f,.27f,blue,1);
            Box(p,"ScrubSponge",new Vector3(.58f,.976f,-.19f),new Vector3(.23f,.045f,.14f),ochre,.02f);
            Box(p,"SpongeScrubSide",new Vector3(.58f,1.001f,-.19f),new Vector3(.23f,.009f,.14f),sage,.004f);
            Bucket(p,new Vector3(.50f,.255f,-.015f),.23f,.42f,cream);
            Tube(p,"TowelRail",new[]{new Vector3(.96f,.80f,-.24f),new Vector3(1.08f,.80f,-.24f),new Vector3(1.08f,.80f,.25f),new Vector3(.96f,.80f,.25f)},.013f,chrome);
            Box(p,"DrapedTowel",new Vector3(1.09f,.57f,-.03f),new Vector3(.022f,.46f,.32f),linen,.009f);
            for(int i=0;i<7;i++) Box(p,"TowelSeam",new Vector3(1.105f,.57f,-.16f+i*.042f),new Vector3(.006f,.43f,.008f),cream,.003f);
            Collider(p,"WashStationCollision",new Vector3(0,.55f,0),new Vector3(1.96f,1.10f,.90f));
        }

        static void BenchDetails(Transform p)
        {
            // Wall organisation is behind the bench, leaving its terminal and E interaction exposed.
            var b=Group(p,"Pegboard",new Vector3(-5.86f,1.95f,.50f),90f);
            Box(b,"OakFrame",Vector3.zero,new Vector3(2.65f,1.46f,.07f),oak,.015f);
            Box(b,"PerforatedBoard",new Vector3(0,0,.043f),new Vector3(2.54f,1.35f,.023f),paper,.006f);
            for(int row=0;row<13;row++) for(int col=0;col<25;col++)
                Disk(b,"Perforation",new Vector3(-1.20f+col*.10f,-.60f+row*.10f,.057f),.007f,.002f,rubber,Quaternion.Euler(90,0,0));
            // Hanging brush heads, squeegee, wrenches, and a coiled hose have distinct silhouettes.
            for(int i=0;i<3;i++)
            {
                float x=-.91f+i*.38f;
                Tube(b,"PegHook",new[]{new Vector3(x,.42f,.06f),new Vector3(x,.42f,.14f),new Vector3(x,.46f,.14f)},.008f,chrome);
                Box(b,"HandBrushHandle",new Vector3(x,.20f,.13f),new Vector3(.055f,.43f,.04f),oak,.017f);
                Box(b,"HandBrushBody",new Vector3(x,-.04f,.14f),new Vector3(.24f,.12f,.08f),oak,.025f);
                for(int j=0;j<8;j++) Tube(b,"BrushBristle",new[]{new Vector3(x-.10f+j*.028f,-.10f,.15f),new Vector3(x-.10f+j*.028f,-.19f,.15f)},.009f,linen);
            }
            Box(b,"HangingSqueegeeHandle",new Vector3(.43f,.02f,.15f),new Vector3(.036f,.91f,.036f),chrome,.012f);
            Box(b,"HangingSqueegeeBlade",new Vector3(.43f,-.42f,.16f),new Vector3(.65f,.055f,.06f),sage,.014f);
            Box(b,"SqueegeeRubber",new Vector3(.43f,-.46f,.17f),new Vector3(.67f,.03f,.022f),rubber,.003f);
            Coil(b,new Vector3(.98f,.10f,.19f),.23f,3,.021f,sage,Quaternion.Euler(90,0,0));
            Box(p,"BenchShelf",new Vector3(-5.66f,2.86f,.50f),new Vector3(.45f,.055f,2.9f),oak,.009f);
            for(int i=0;i<5;i++) Bottle(p,new Vector3(-5.65f,2.89f,-.58f+i*.35f),.083f,.36f,i%2==0?cream:blue,i);
            for(int i=0;i<2;i++)
            {
                float z=-.60f+i*2.2f;
                Tube(p,"ShelfBracket",new[]{new Vector3(-5.91f,2.55f,z),new Vector3(-5.46f,2.81f,z),new Vector3(-5.91f,2.81f,z)},.015f,iron);
            }
            // Worktop props occupy the far end, not the upgrade terminal.
            var tools=Group(p,"WorkbenchSmallTools",new Vector3(-4.62f,.962f,-.13f),0);
            Can(tools,new Vector3(-.15f,0,-.35f),.085f,.16f,cream,0);
            for(int i=0;i<4;i++) Tube(tools,"Pencil",new[]{new Vector3(-.17f+i*.017f,.10f,-.35f),new Vector3(-.18f+i*.023f,.33f,-.34f)},.006f,i%2==0?ochre:oak);
            Box(tools,"FoldedWorkCloth",new Vector3(.04f,.026f,.0f),new Vector3(.36f,.05f,.28f),linen,.012f);
            Tube(tools,"HandToolGrip",new[]{new Vector3(.04f,.067f,-.12f),new Vector3(.04f,.067f,.09f)},.016f,sage);
            Tube(tools,"HandToolSteel",new[]{new Vector3(.04f,.067f,.09f),new Vector3(.04f,.067f,.22f)},.005f,chrome);
            TaskLamp(p,new Vector3(-4.68f,.96f,-.68f));
            // Low stool tucked beside, with circular wood seat and tapered steel legs.
            var stool=Group(p,"WorkshopStool",new Vector3(-4.03f,0,-1.19f),14);
            Disk(stool,"WoodSeat",new Vector3(0,.55f,0),.235f,.065f,oak,Quaternion.identity);
            for(int i=0;i<4;i++)
            {
                float a=i*Mathf.PI*.5f+Mathf.PI*.25f;
                Tube(stool,"StoolLeg",new[]{new Vector3(Mathf.Cos(a)*.24f,.03f,Mathf.Sin(a)*.24f),new Vector3(Mathf.Cos(a)*.15f,.53f,Mathf.Sin(a)*.15f)},.018f,iron);
            }
            Ring(stool,"FootRing",new Vector3(0,.19f,0),.20f,.013f,iron,Quaternion.identity);
            Collider(stool,"StoolCollision",new Vector3(0,.30f,0),new Vector3(.49f,.60f,.49f));
        }

        static void Cart(Transform p)
        {
            for(int x=-1;x<=1;x+=2) for(int z=-1;z<=1;z+=2)
            {
                Box(p,"CartUpright",new Vector3(x*.35f,.53f,z*.50f),new Vector3(.038f,.82f,.038f),sage,.008f);
                Disk(p,"CasterWheel",new Vector3(x*.35f,.08f,z*.50f),.078f,.046f,rubber,Quaternion.Euler(0,0,90));
                Box(p,"CasterBracket",new Vector3(x*.35f,.15f,z*.50f),new Vector3(.08f,.07f,.045f),chrome,.009f);
            }
            for(int i=0;i<3;i++)
            {
                float y=.22f+i*.34f;
                Box(p,"CartTray",new Vector3(0,y,0),new Vector3(.79f,.04f,1.12f),sage,.015f);
                for(int x=-1;x<=1;x+=2) Box(p,"CartTrayEdge",new Vector3(x*.38f,y+.04f,0),new Vector3(.025f,.08f,1.10f),sage,.006f);
                for(int z=-1;z<=1;z+=2) Box(p,"CartTrayEdge",new Vector3(0,y+.04f,z*.54f),new Vector3(.77f,.08f,.025f),sage,.006f);
            }
            Tube(p,"CartPushHandle",new[]{new Vector3(-.35f,.91f,.50f),new Vector3(-.35f,1.12f,.64f),new Vector3(.35f,1.12f,.64f),new Vector3(.35f,.91f,.50f)},.022f,chrome);
            Tube(p,"HandleRubberGrip",new[]{new Vector3(-.22f,1.12f,.64f),new Vector3(.22f,1.12f,.64f)},.028f,rubber);
            Bucket(p,new Vector3(0,.245f,.05f),.245f,.27f,cream);
            FoldedCloths(p,new Vector3(0,.585f,-.23f),.55f,.42f,3);
            Can(p,new Vector3(-.13f,.585f,.28f),.12f,.25f,ochre,1);
            Can(p,new Vector3(.15f,.585f,.28f),.12f,.25f,blue,2);
            Bottle(p,new Vector3(-.19f,.925f,.25f),.07f,.32f,blue,2);
            Bottle(p,new Vector3(.08f,.925f,.29f),.08f,.35f,ochre,1);
            Bottle(p,new Vector3(.23f,.925f,.04f),.07f,.31f,cream,2);
            Box(p,"CartBrushBlock",new Vector3(-.06f,.96f,-.20f),new Vector3(.32f,.08f,.16f),oak,.025f);
            for(int i=0;i<10;i++) Tube(p,"CartBrushBristle",new[]{new Vector3(-.19f+i*.03f,.96f,-.27f),new Vector3(-.19f+i*.03f,1.025f,-.27f)},.009f,linen);
            Collider(p,"CartCollision",new Vector3(0,.54f,.04f),new Vector3(.88f,1.08f,1.28f));
        }

        static void WallServices(Transform p)
        {
            // Copper supply follows the north wall, with clips and an easily readable shutoff valve.
            Tube(p,"CopperWaterSupply",new[]{new Vector3(3.50f,.20f,5.84f),new Vector3(3.50f,2.17f,5.84f),new Vector3(1.52f,2.17f,5.84f),new Vector3(1.52f,1.18f,5.84f)},.022f,brass);
            for(int i=0;i<5;i++)
                Box(p,"PipeClip",new Vector3(3.50f,.39f+i*.35f,5.87f),new Vector3(.085f,.025f,.085f),iron,.008f);
            Ring(p,"RedShutoffValve",new Vector3(3.50f,1.55f,5.77f),.075f,.011f,red,Quaternion.Euler(90,0,0));
            Tube(p,"ValveCross",new[]{new Vector3(3.44f,1.55f,5.77f),new Vector3(3.56f,1.55f,5.77f)},.008f,red);
            Tube(p,"ValveCross",new[]{new Vector3(3.50f,1.49f,5.77f),new Vector3(3.50f,1.61f,5.77f)},.008f,red);
            // Side-wall hose reel is entirely outside the central work zone.
            var reel=Group(p,"HoseReel",new Vector3(5.78f,1.19f,-3.60f),-90f);
            Box(reel,"ReelWallMount",new Vector3(0,0,0),new Vector3(.23f,.46f,.08f),sage,.025f);
            Disk(reel,"ReelRearPlate",new Vector3(0,0,.10f),.31f,.045f,sage,Quaternion.Euler(90,0,0));
            Disk(reel,"ReelCore",new Vector3(0,0,.25f),.15f,.28f,iron,Quaternion.Euler(90,0,0));
            Coil(reel,new Vector3(0,0,.17f),.235f,7,.023f,ochre,Quaternion.Euler(90,0,0));
            Ring(reel,"ReelFrontRim",new Vector3(0,0,.42f),.31f,.022f,sage,Quaternion.Euler(90,0,0));
            for(int i=0;i<5;i++)
            {
                float a=i*2*Mathf.PI/5;
                Tube(reel,"ReelSpoke",new[]{new Vector3(0,0,.42f),new Vector3(Mathf.Cos(a)*.30f,Mathf.Sin(a)*.30f,.42f)},.013f,sage);
            }
            Tube(reel,"HangingHose",new[]{new Vector3(.22f,-.08f,.26f),new Vector3(.35f,-.39f,.28f),new Vector3(.26f,-.69f,.31f),new Vector3(.06f,-.73f,.30f),new Vector3(-.10f,-.59f,.29f)},.023f,ochre);
            Tube(reel,"HoseNozzle",new[]{new Vector3(-.10f,-.59f,.29f),new Vector3(-.16f,-.41f,.29f)},.034f,brass);
            // Electrical trunking, socket, fused isolator: small scale gives the walls working purpose.
            Box(p,"ElectricalConduit",new Vector3(-5.91f,3.22f,.45f),new Vector3(.034f,.034f,9.5f),iron,.012f);
            Box(p,"ElectricalDrop",new Vector3(-5.91f,2.17f,2.32f),new Vector3(.035f,2.10f,.035f),iron,.012f);
            Box(p,"FuseBox",new Vector3(-5.84f,1.86f,2.32f),new Vector3(.18f,.48f,.35f),sage,.025f);
            Box(p,"FuseBoxDoor",new Vector3(-5.735f,1.86f,2.32f),new Vector3(.026f,.42f,.29f),cream,.012f);
            Box(p,"IsolatorSwitch",new Vector3(-5.711f,1.86f,2.32f),new Vector3(.035f,.13f,.07f),red,.007f);
            for(int z=0;z<2;z++)
            {
                Box(p,"WallSocket",new Vector3(-5.90f,1.19f,.10f+z*.18f),new Vector3(.035f,.12f,.12f),cream,.015f);
                for(int k=-1;k<=1;k+=2) Disk(p,"SocketHole",new Vector3(-5.879f,1.19f,.10f+z*.18f+k*.023f),.009f,.003f,rubber,Quaternion.Euler(0,0,90));
            }
            Clock(p,new Vector3(-1.15f,2.95f,5.89f));
            // Fire extinguisher, bracket and hose are recognizable, with no untranslated signage.
            var extinguisher=Group(p,"FireExtinguisher",new Vector3(1.0f,.32f,5.64f));
            Lathe(extinguisher,"ExtinguisherBody",Vector3.zero,new[]{new Vector2(0,0),new Vector2(.13f,.02f),new Vector2(.145f,.10f),new Vector2(.145f,.48f),new Vector2(.11f,.56f),new Vector2(.038f,.59f)},red,24);
            Box(extinguisher,"ExtinguisherLabel",new Vector3(0,.32f,-.145f),new Vector3(.16f,.20f,.006f),cream,.005f);
            Box(extinguisher,"ExtinguisherLever",new Vector3(0,.63f,0),new Vector3(.19f,.025f,.055f),iron,.009f);
            Tube(extinguisher,"ExtinguisherHose",new[]{new Vector3(.04f,.59f,0),new Vector3(.18f,.57f,0),new Vector3(.20f,.12f,0),new Vector3(.18f,.06f,-.04f)},.018f,rubber);
            Ring(extinguisher,"ExtinguisherBracket",new Vector3(0,.14f,0),.15f,.012f,iron,Quaternion.identity);
        }

        static void WindowGarden(Transform p)
        {
            // The sills sit at 1.715m; the plants silhouette against the warm window rather than fill the floor.
            Plant(p,new Vector3(-2.80f,1.715f,-5.80f),.17f,.57f,5);
            Plant(p,new Vector3(.46f,1.715f,-5.80f),.14f,.40f,8);
            Plant(p,new Vector3(5.79f,1.715f,3.40f),.18f,.62f,12);
            Plant(p,new Vector3(-5.17f,0,-4.96f),.30f,1.57f,17);
            Collider(p,"PlantFloorCollision",new Vector3(-5.17f,.35f,-4.96f),new Vector3(.65f,.7f,.65f));
            // Old cast-iron radiator under the south window, with ribbed columns and copper tails.
            var r=Group(p,"Radiator",new Vector3(-1.13f,0,-5.76f));
            for(int i=0;i<22;i++)
            {
                float x=-1.25f+i*.12f;
                Box(r,"RadiatorRib",new Vector3(x,.60f,0),new Vector3(.077f,.88f,.22f),cream,.037f);
                Box(r,"RadiatorRibInset",new Vector3(x,.60f,-.10f),new Vector3(.028f,.72f,.045f),cream,.013f);
            }
            for(int i=0;i<2;i++)
                Tube(r,"RadiatorManifold",new[]{new Vector3(-1.35f,.28f+i*.64f,0),new Vector3(1.39f,.28f+i*.64f,0)},.037f,cream);
            Tube(r,"RadiatorSupply",new[]{new Vector3(1.43f,.28f,0),new Vector3(1.58f,.28f,0),new Vector3(1.58f,.03f,0)},.022f,brass);
            Disk(r,"RadiatorValve",new Vector3(1.57f,.34f,0),.045f,.07f,cream,Quaternion.identity);
            Collider(r,"RadiatorCollision",new Vector3(0,.55f,0),new Vector3(2.96f,1.1f,.37f));
            // Two books and a ceramic mug on the sill give the room a human scale.
            for(int i=0;i<2;i++) Box(p,"SillNotebook",new Vector3(-.37f,1.744f+i*.036f,-5.80f),new Vector3(.29f,.033f,.21f),i==0?sage:paper,.006f);
            Mug(p,new Vector3(-.83f,1.715f,-5.78f),.065f,cream);
        }

        static void SmallStorage(Transform p)
        {
            for(int i=-1;i<=1;i+=2)
                Box(p,"CabinetPull",new Vector3(i*.09f,.65f,-.437f),new Vector3(.025f,.16f,.035f),brass,.01f);
            FoldedCloths(p,new Vector3(-.40f,1.105f,-.03f),.56f,.55f,4);
            Bottle(p,new Vector3(.12f,1.105f,.03f),.09f,.35f,ochre,1);
            Bottle(p,new Vector3(.42f,1.105f,.03f),.08f,.32f,cream,2);
            // Bundled towel bars and delivery cartons occupy only the corner perimeter.
            Carton(p,new Vector3(.07f,.02f,.75f),new Vector3(.88f,.53f,.62f),-6);
            Carton(p,new Vector3(.07f,.55f,.75f),new Vector3(.76f,.38f,.56f),4);
            Collider(p,"CornerCartonsCollision",new Vector3(.07f,.48f,.75f),new Vector3(.95f,.96f,.68f));
        }

        static void WorkNotes(Transform p)
        {
            // Notice frame contains only marks/pictograms, so this introduces no untranslated UI copy.
            var notice=Group(p,"WorkNotes",new Vector3(-5.91f,1.87f,3.84f),90f);
            Box(notice,"NoticeFrame",Vector3.zero,new Vector3(1.20f,.85f,.05f),oak,.015f);
            Box(notice,"Cork",new Vector3(0,0,.031f),new Vector3(1.11f,.76f,.018f),paper,.005f);
            for(int i=0;i<3;i++)
            {
                var note=Group(notice,"PaperNote",new Vector3(-.36f+i*.35f,.03f,.046f)); note.localRotation=Quaternion.Euler(0,0,(i-1)*7);
                Box(note,"Note",Vector3.zero,new Vector3(.27f,.39f,.003f),cream,.001f);
                Disk(note,"Pin",new Vector3(0,.16f,.009f),.015f,.008f,i==1?ochre:brass,Quaternion.Euler(90,0,0));
                for(int line=0;line<4;line++) Box(note,"NoteLine",new Vector3(.01f,.09f-line*.055f,.004f),new Vector3(.16f,.005f,.001f),sage,.0003f);
                Box(note,"NoteCheckbox",new Vector3(-.09f,-.075f,.005f),new Vector3(.025f,.025f,.002f),sage,.002f);
            }
        }

        static void Bottle(Transform p,Vector3 pos,float radius,float height,Material body,int style)
        {
            var g=Group(p,"CleaningBottle",pos,(style*17)%31-15);
            float neck=radius*.44f;
            Lathe(g,"Bottle",Vector3.zero,new[]{new Vector2(0,0),new Vector2(radius*.82f,0),new Vector2(radius,.018f),new Vector2(radius,height*.69f),new Vector2(radius*.93f,height*.77f),new Vector2(neck,height*.87f),new Vector2(neck,height*.98f),new Vector2(0,height*.98f)},body,20);
            // The label wraps around the middle; a tiny pictogram band avoids world-space English text.
            Lathe(g,"BottlePaperBand",new Vector3(0,height*.24f,0),new[]{new Vector2(radius+.001f,0),new Vector2(radius+.001f,height*.31f)},cream,20);
            Box(g,"BottleLabelMark",new Vector3(0,height*.39f,-radius-.003f),new Vector3(radius*.67f,height*.07f,.003f),body,.002f);
            Disk(g,"BottleCap",new Vector3(0,height,0),neck*1.08f,.035f,rubber,Quaternion.identity);
            if(style%3==2)
            {
                Box(g,"SprayPump",new Vector3(0,height+.035f,-radius*.22f),new Vector3(radius*.73f,.055f,radius*1.7f),body,.012f);
                Box(g,"SprayTrigger",new Vector3(0,height-.005f,-radius*.67f),new Vector3(.018f,.07f,.018f),rubber,.006f,Quaternion.Euler(-22,0,0));
                Disk(g,"SprayNozzle",new Vector3(0,height+.034f,-radius*1.12f),.019f,.016f,rubber,Quaternion.Euler(90,0,0));
            }
        }

        static void Can(Transform p,Vector3 pos,float radius,float height,Material body,int pattern)
        {
            var g=Group(p,"SupplyCan",pos);
            Lathe(g,"CanBody",Vector3.zero,new[]{new Vector2(0,0),new Vector2(radius*.91f,0),new Vector2(radius,.018f),new Vector2(radius,height-.018f),new Vector2(radius*.91f,height),new Vector2(0,height)},body,24);
            Ring(g,"CanBottomSeam",new Vector3(0,.018f,0),radius,.006f,chrome,Quaternion.identity);
            Ring(g,"CanTopSeam",new Vector3(0,height-.008f,0),radius,.007f,chrome,Quaternion.identity);
            Lathe(g,"CanLabel",new Vector3(0,height*.2f,0),new[]{new Vector2(radius+.001f,0),new Vector2(radius+.001f,height*.52f)},cream,24);
            Box(g,"CanStripe",new Vector3(0,height*.45f,-radius-.004f),new Vector3(radius*1.25f,height*.09f,.004f),pattern%2==0?sage:ochre,.002f);
            Disk(g,"CanLid",new Vector3(0,height+.004f,0),radius*.93f,.012f,body,Quaternion.identity);
        }

        static void Bucket(Transform p,Vector3 pos,float radius,float height,Material body)
        {
            var g=Group(p,"UtilityPail",pos);
            Lathe(g,"PailBody",Vector3.zero,new[]{new Vector2(0,0),new Vector2(radius*.77f,0),new Vector2(radius*.82f,.02f),new Vector2(radius,height-.01f),new Vector2(radius-.014f,height),new Vector2(radius*.78f,.035f),new Vector2(0,.035f)},body,24);
            Ring(g,"PailRim",new Vector3(0,height,0),radius,.012f,chrome,Quaternion.identity);
            var points=new Vector3[17];
            for(int i=0;i<points.Length;i++){float a=i*Mathf.PI/16;points[i]=new Vector3(Mathf.Cos(a)*radius,height*.79f+Mathf.Sin(a)*radius*.9f,0);}
            Tube(g,"PailHandle",points,.008f,chrome);
            Tube(g,"PailGrip",new[]{new Vector3(-.06f,height*.79f+radius*.90f,0),new Vector3(.06f,height*.79f+radius*.90f,0)},.017f,rubber);
        }

        static void Carton(Transform p,Vector3 pos,Vector3 size,float angle)
        {
            var g=Group(p,"DeliveryCarton",pos,angle);
            Box(g,"Cardboard",new Vector3(0,size.y*.5f,0),size,paper,.012f);
            Box(g,"PackingTape",new Vector3(0,size.y+.0015f,0),new Vector3(.055f,.003f,size.z-.01f),ochre,.001f);
            Box(g,"ShippingLabel",new Vector3(-size.x*.19f,size.y*.55f,-size.z*.5f-.002f),new Vector3(size.x*.33f,size.y*.38f,.003f),cream,.002f);
            for(int i=0;i<8;i++) Box(g,"Barcode",new Vector3(-size.x*.30f+i*size.x*.03f,size.y*.53f,-size.z*.5f-.004f),new Vector3(i%3==0?.009f:.004f,size.y*.16f,.0015f),iron,.0005f);
        }

        static void Crate(Transform p,Vector3 pos,Vector3 size,float angle)
        {
            var g=Group(p,"SlattedCrate",pos,angle);
            Box(g,"CrateBottom",new Vector3(0,.02f,0),new Vector3(size.x,.04f,size.z),oak,.006f);
            for(int x=-1;x<=1;x+=2) for(int z=-1;z<=1;z+=2)
                Box(g,"CrateCorner",new Vector3(x*(size.x*.5f-.025f),size.y*.5f,z*(size.z*.5f-.025f)),new Vector3(.04f,size.y,.04f),oak,.005f);
            for(int level=0;level<3;level++) for(int side=-1;side<=1;side+=2)
            {
                float y=.07f+level*(size.y-.10f)/3;
                Box(g,"CrateSlat",new Vector3(0,y,side*size.z*.5f),new Vector3(size.x,.085f,.025f),oak,.004f);
                Box(g,"CrateEndSlat",new Vector3(side*size.x*.5f,y,0),new Vector3(.025f,.085f,size.z),oak,.004f);
                for(int x=-1;x<=1;x+=2) Disk(g,"CrateNail",new Vector3(x*(size.x*.5f-.045f),y,-size.z*.5f-.014f),.006f,.003f,iron,Quaternion.Euler(90,0,0));
            }
            for(int i=0;i<3;i++)
            {
                var cloth=Disk(g,"RolledCloth",new Vector3(-size.x*.30f+i*size.x*.30f,size.y*.60f,0),size.x*.12f,size.z*.75f,linen,Quaternion.Euler(90,0,0));
                Ring(g,"ClothRollEnd",new Vector3(-size.x*.30f+i*size.x*.30f,size.y*.60f,-size.z*.38f),size.x*.072f,.008f,cream,Quaternion.Euler(90,0,0));
            }
        }

        static void FoldedCloths(Transform p,Vector3 pos,float width,float depth,int count)
        {
            var g=Group(p,"FoldedTowels",pos);
            for(int i=0;i<count;i++)
            {
                float x=Mathf.Sin(i*7)*.018f,z=Mathf.Cos(i*4)*.014f;
                Box(g,"FoldedCloth",new Vector3(x,.022f+i*.045f,z),new Vector3(width,.042f,depth),i%3==0?sage:linen,.017f,Quaternion.Euler(0,i%2==0?2:-2,0));
                Box(g,"ClothHem",new Vector3(x,.022f+i*.045f,z-depth*.5f-.001f),new Vector3(width*.94f,.009f,.003f),cream,.001f);
            }
        }

        static void TaskLamp(Transform p,Vector3 pos)
        {
            var g=Group(p,"ArticulatedTaskLamp",pos);
            Disk(g,"LampBase",new Vector3(0,.018f,0),.125f,.036f,sage,Quaternion.identity);
            Tube(g,"LampLowerArm",new[]{new Vector3(0,.03f,0),new Vector3(-.13f,.42f,.03f)},.016f,iron);
            Tube(g,"LampUpperArm",new[]{new Vector3(-.13f,.42f,.03f),new Vector3(.10f,.66f,.09f)},.016f,iron);
            Disk(g,"LampJoint",new Vector3(-.13f,.42f,.03f),.037f,.048f,brass,Quaternion.Euler(90,0,0));
            var head=Group(g,"LampHead",new Vector3(.13f,.62f,.09f)); head.localRotation=Quaternion.Euler(0,0,-20);
            Lathe(head,"EnamelLampShade",Vector3.zero,new[]{new Vector2(.14f,0),new Vector2(.15f,.012f),new Vector2(.055f,.15f),new Vector2(.025f,.17f),new Vector2(0,.17f)},sage,24);
            Disk(head,"LampInterior",new Vector3(0,.004f,0),.132f,.007f,glow,Quaternion.identity);
            Tube(g,"LampCable",new[]{new Vector3(0,.025f,.04f),new Vector3(-.15f,.02f,.16f),new Vector3(-.29f,.01f,.24f),new Vector3(-.33f,-.45f,.26f)},.005f,rubber);
            var l=new GameObject("WarmWorkLight");l.transform.SetParent(head,false);l.transform.localPosition=new Vector3(0,-.018f,0);l.transform.localRotation=Quaternion.Euler(90,0,0);
            var lightComponent=l.AddComponent<Light>();lightComponent.type=LightType.Spot;lightComponent.color=new Color(1f,.70f,.38f);lightComponent.intensity=1.25f;lightComponent.range=1.6f;lightComponent.spotAngle=90;lightComponent.innerSpotAngle=60;lightComponent.shadows=LightShadows.None;
        }

        static void Clock(Transform p,Vector3 pos)
        {
            var g=Group(p,"WorkshopClock",pos);
            Disk(g,"ClockCasing",Vector3.zero,.24f,.055f,iron,Quaternion.Euler(90,0,0));
            Disk(g,"ClockFace",new Vector3(0,0,-.033f),.215f,.009f,cream,Quaternion.Euler(90,0,0));
            for(int i=0;i<12;i++)
            {
                float a=i*Mathf.PI/6;
                Box(g,"ClockMark",new Vector3(Mathf.Sin(a)*.186f,Mathf.Cos(a)*.186f,-.040f),new Vector3(.009f,i%3==0?.032f:.019f,.004f),iron,.001f,Quaternion.Euler(0,0,-i*30));
            }
            Tube(g,"HourHand",new[]{new Vector3(0,0,-.046f),new Vector3(-.068f,.056f,-.046f)},.006f,iron);
            Tube(g,"MinuteHand",new[]{new Vector3(0,0,-.048f),new Vector3(.105f,.105f,-.048f)},.004f,iron);
            Disk(g,"ClockHub",new Vector3(0,0,-.05f),.016f,.009f,brass,Quaternion.Euler(90,0,0));
        }

        static void Mug(Transform p,Vector3 pos,float radius,Material material)
        {
            var g=Group(p,"CeramicMug",pos);
            Lathe(g,"MugBody",Vector3.zero,new[]{new Vector2(0,0),new Vector2(radius*.85f,0),new Vector2(radius,.02f),new Vector2(radius,radius*1.65f),new Vector2(radius-.009f,radius*1.65f),new Vector2(radius-.009f,.018f),new Vector2(0,.018f)},material,24);
            Ring(g,"MugHandle",new Vector3(radius*1.13f,radius*.85f,0),radius*.57f,.012f,material,Quaternion.Euler(90,0,0));
            Disk(g,"Tea",new Vector3(0,radius*1.38f,0),radius-.01f,.003f,soil,Quaternion.identity);
        }

        static void Plant(Transform p,Vector3 pos,float radius,float foliageHeight,int seed)
        {
            var g=Group(p,"PottedPlant",pos);
            float ph=radius*1.65f;
            Lathe(g,"TerracottaPot",Vector3.zero,new[]{new Vector2(0,0),new Vector2(radius*.67f,0),new Vector2(radius*.72f,.025f),new Vector2(radius,ph*.88f),new Vector2(radius*1.04f,ph*.89f),new Vector2(radius*1.04f,ph),new Vector2(radius*.91f,ph),new Vector2(radius*.85f,ph*.84f),new Vector2(radius*.62f,.03f),new Vector2(0,.03f)},clay,28);
            Disk(g,"PlantSoil",new Vector3(0,ph*.87f,0),radius*.88f,.016f,soil,Quaternion.identity);
            var rng=new System.Random(seed);
            for(int i=0;i<13;i++)
            {
                float a=i*2.39996f;float h=foliageHeight*(.40f+.58f*(float)rng.NextDouble());
                float spread=foliageHeight*(.16f+.21f*(float)rng.NextDouble());
                Vector3 start=new Vector3(Mathf.Cos(a)*radius*.12f,ph*.88f,Mathf.Sin(a)*radius*.12f);
                Vector3 end=start+new Vector3(Mathf.Cos(a)*spread,h,Mathf.Sin(a)*spread);
                Vector3 mid=Vector3.Lerp(start,end,.55f)+Vector3.up*h*.15f;
                Tube(g,"PlantStem",new[]{start,mid,end},radius*.012f,leaf);
                Leaf(g,"CurvedLeaf",start,end,h*.18f,i%3==0?leafLight:leaf);
            }
        }

        static void Leaf(Transform p,string name,Vector3 start,Vector3 end,float width,Material material)
        {
            var vertices=new List<Vector3>();var normals=new List<Vector3>();var uv=new List<Vector2>();var triangles=new List<int>();
            Vector3 axis=end-start;Vector3 side=Vector3.Cross(axis,Vector3.up).normalized;
            if(side.sqrMagnitude<.01f)side=Vector3.right;
            for(int row=0;row<=8;row++)
            {
                float t=row/8f;Vector3 center=Vector3.Lerp(start,end,t)+Vector3.up*(Mathf.Sin(t*Mathf.PI)*axis.magnitude*.13f);
                float w=Mathf.Pow(Mathf.Max(0f,Mathf.Sin(t*Mathf.PI)),.8f)*width;
                for(int col=-1;col<=1;col++) {vertices.Add(center+side*(col*w)+Vector3.up*(Mathf.Abs(col)*w*.22f));uv.Add(new Vector2(col*.5f+.5f,t));}
            }
            for(int row=0;row<8;row++)for(int col=0;col<2;col++)
            {int a=row*3+col,b=a+1,c=a+3,d=c+1;triangles.AddRange(new[]{a,c,b,b,c,d});}
            // Separate back vertices keep normal recalculation from cancelling the front-face normals.
            int frontCount=vertices.Count,frontIndices=triangles.Count;
            for(int i=0;i<frontCount;i++){vertices.Add(vertices[i]);uv.Add(uv[i]);}
            for(int i=0;i<frontIndices;i+=3)triangles.AddRange(new[]{triangles[i]+frontCount,triangles[i+2]+frontCount,triangles[i+1]+frontCount});
            var mesh=new Mesh{name="CurvedPlantLeaf"};mesh.SetVertices(vertices);mesh.SetUVs(0,uv);mesh.SetTriangles(triangles,0);mesh.RecalculateNormals();mesh.RecalculateBounds();TemporaryMeshes.Add(mesh);
            Render(p,name,mesh,Vector3.zero,Quaternion.identity,material);
        }

        // Geometry helpers. Dimensions are metres; bevels are actual geometry, not shader edge highlights.
        static Transform Group(Transform p,string name,Vector3 position,float yaw=0)
        {var go=new GameObject(name);go.transform.SetParent(p,false);go.transform.localPosition=position;go.transform.localRotation=Quaternion.Euler(0,yaw,0);return go.transform;}

        static GameObject Box(Transform p,string name,Vector3 pos,Vector3 size,Material material,float bevel=.008f,Quaternion rotation=default(Quaternion))
        {
            if(rotation==default(Quaternion))rotation=Quaternion.identity;
            string key="Box"+size.ToString("F4")+bevel.ToString("F4",System.Globalization.CultureInfo.InvariantCulture);
            if(!Meshes.TryGetValue(key,out var mesh))
            {
                mesh=RoundedBox(size,Mathf.Min(bevel,Mathf.Min(size.x,Mathf.Min(size.y,size.z))*.46f));
                Meshes[key]=mesh;TemporaryMeshes.Add(mesh);
            }
            return Render(p,name,mesh,pos,rotation,material);
        }

        static Mesh RoundedBox(Vector3 size,float bevel)
        {
            var vertices=new List<Vector3>();var normals=new List<Vector3>();var uv=new List<Vector2>();var triangles=new List<int>();
            Vector3 h=size*.5f,core=h-Vector3.one*bevel;
            Vector3[] ns={Vector3.right,Vector3.left,Vector3.up,Vector3.down,Vector3.forward,Vector3.back};
            Vector3[] us={Vector3.back,Vector3.forward,Vector3.right,Vector3.right,Vector3.right,Vector3.left};
            Vector3[] vs={Vector3.up,Vector3.up,Vector3.back,Vector3.forward,Vector3.up,Vector3.up};
            for(int face=0;face<6;face++)
            {
                Vector3 n=ns[face],u=us[face],v=vs[face];float hn=AbsDot(n,h),hu=AbsDot(u,h),hv=AbsDot(v,h);
                float[] xs={-hu,-hu+bevel,hu-bevel,hu},ys={-hv,-hv+bevel,hv-bevel,hv};int offset=vertices.Count;
                for(int y=0;y<4;y++)for(int x=0;x<4;x++)
                {
                    Vector3 point=n*hn+u*xs[x]+v*ys[y];Vector3 anchor=new Vector3(Mathf.Clamp(point.x,-core.x,core.x),Mathf.Clamp(point.y,-core.y,core.y),Mathf.Clamp(point.z,-core.z,core.z));
                    Vector3 normal=(point-anchor).normalized;vertices.Add(anchor+normal*bevel);normals.Add(normal);uv.Add(new Vector2(xs[x]*2f,ys[y]*2f));
                }
                for(int y=0;y<3;y++)for(int x=0;x<3;x++){int a=offset+y*4+x,b=a+1,c=a+4,d=c+1;triangles.AddRange(new[]{a,b,c,b,d,c});}
            }
            var mesh=new Mesh{name="WorkshopBeveledBox"};mesh.SetVertices(vertices);mesh.SetNormals(normals);mesh.SetUVs(0,uv);mesh.SetTriangles(triangles,0);mesh.RecalculateBounds();return mesh;
        }
        static float AbsDot(Vector3 a,Vector3 b){return Mathf.Abs(a.x)*b.x+Mathf.Abs(a.y)*b.y+Mathf.Abs(a.z)*b.z;}

        static GameObject Disk(Transform p,string name,Vector3 pos,float radius,float height,Material material,Quaternion rotation)
        {
            var profile=new[]{new Vector2(0,-height*.5f),new Vector2(radius*.94f,-height*.5f),new Vector2(radius,-height*.5f+Mathf.Min(height*.2f,.006f)),new Vector2(radius,height*.5f-Mathf.Min(height*.2f,.006f)),new Vector2(radius*.94f,height*.5f),new Vector2(0,height*.5f)};
            var go=Lathe(p,name,pos,profile,material,24);
            if(material==oak)
            {
                // Circular wooden caps are cut from a board: their grain stays parallel across the face.
                // Only cap rows change; the narrow turned edge retains its cylindrical unwrap.
                var mesh=go.GetComponent<MeshFilter>().sharedMesh;
                var vertices=mesh.vertices;var texcoords=mesh.uv;
                for(int i=0;i<vertices.Length;i++)
                    if(Mathf.Abs(vertices[i].y)>=height*.5f-.000001f)
                        texcoords[i]=new Vector2(vertices[i].x*2f,vertices[i].z*2f);
                mesh.uv=texcoords;
            }
            go.transform.localRotation=rotation;return go;
        }
        static GameObject Lathe(Transform p,string name,Vector3 pos,Vector2[] profile,Material material,int sides)
        {
            var verts=new List<Vector3>();var uv=new List<Vector2>();var tris=new List<int>();
            for(int row=0;row<profile.Length;row++)for(int j=0;j<=sides;j++)
            {float a=j*2*Mathf.PI/sides;verts.Add(new Vector3(Mathf.Cos(a)*profile[row].x,profile[row].y,Mathf.Sin(a)*profile[row].x));uv.Add(new Vector2(j/(float)sides,profile[row].y*2));}
            for(int row=0;row<profile.Length-1;row++)for(int j=0;j<sides;j++)
            {int a=row*(sides+1)+j,b=a+1,c=a+sides+1,d=c+1;tris.AddRange(new[]{a,c,b,b,c,d});}
            var mesh=new Mesh{name="WorkshopLathe"};mesh.SetVertices(verts);mesh.SetUVs(0,uv);mesh.SetTriangles(tris,0);mesh.RecalculateNormals();mesh.RecalculateBounds();TemporaryMeshes.Add(mesh);
            return Render(p,name,mesh,pos,Quaternion.identity,material);
        }
        static void Ring(Transform p,string name,Vector3 pos,float radius,float tube,Material material,Quaternion rotation)
        {
            var points=new Vector3[41];for(int i=0;i<=40;i++){float a=i*2*Mathf.PI/40;points[i]=pos+rotation*new Vector3(Mathf.Cos(a)*radius,0,Mathf.Sin(a)*radius);}Tube(p,name,points,tube,material);
        }
        static void Coil(Transform p,Vector3 pos,float radius,int loops,float tube,Material material,Quaternion rotation)
        {
            var points=new Vector3[loops*40+1];for(int i=0;i<points.Length;i++){float t=i/40f,a=t*2*Mathf.PI;points[i]=pos+rotation*new Vector3(Mathf.Cos(a)*radius,(t-loops*.5f)*tube*1.8f,Mathf.Sin(a)*radius);}Tube(p,"CoiledHose",points,tube,material);
        }
        static void Tube(Transform p,string name,Vector3[] path,float radius,Material material)
        {
            const int sides=8;var verts=new List<Vector3>();var normals=new List<Vector3>();var uv=new List<Vector2>();var triangles=new List<int>();float length=0;
            for(int i=0;i<path.Length;i++)
            {
                Vector3 tangent=(path[Mathf.Min(i+1,path.Length-1)]-path[Mathf.Max(0,i-1)]).normalized;
                Vector3 side=Vector3.Cross(tangent,Mathf.Abs(tangent.y)>.94f?Vector3.right:Vector3.up).normalized;Vector3 up=Vector3.Cross(tangent,side).normalized;
                if(i>0)length+=Vector3.Distance(path[i],path[i-1]);
                for(int j=0;j<=sides;j++){float a=j*2*Mathf.PI/sides;Vector3 n=side*Mathf.Cos(a)+up*Mathf.Sin(a);verts.Add(path[i]+n*radius);normals.Add(n);uv.Add(new Vector2(j/(float)sides,length*2));}
            }
            for(int i=0;i<path.Length-1;i++)for(int j=0;j<sides;j++){int a=i*(sides+1)+j,b=a+1,c=a+sides+1,d=c+1;triangles.AddRange(new[]{a,b,c,b,d,c});}
            var mesh=new Mesh{name="WorkshopTube"};mesh.SetVertices(verts);mesh.SetNormals(normals);mesh.SetUVs(0,uv);mesh.SetTriangles(triangles,0);mesh.RecalculateBounds();TemporaryMeshes.Add(mesh);Render(p,name,mesh,Vector3.zero,Quaternion.identity,material);
        }
        static GameObject Render(Transform p,string name,Mesh mesh,Vector3 pos,Quaternion rotation,Material material)
        {
            var go=new GameObject(name);go.transform.SetParent(p,false);go.transform.localPosition=pos;go.transform.localRotation=rotation;
            go.AddComponent<MeshFilter>().sharedMesh=mesh;var r=go.AddComponent<MeshRenderer>();r.sharedMaterial=material;r.shadowCastingMode=ShadowCastingMode.On;r.receiveShadows=true;r.lightProbeUsage=LightProbeUsage.BlendProbes;r.reflectionProbeUsage=ReflectionProbeUsage.BlendProbes;
            return go;
        }
        static void Collider(Transform p,string name,Vector3 center,Vector3 size)
        {var go=new GameObject(name);go.transform.SetParent(p,false);var box=go.AddComponent<BoxCollider>();box.center=center;box.size=size;}

        static void CombineZone(Transform zone)
        {
            var renderers=zone.GetComponentsInChildren<MeshRenderer>();var groups=new Dictionary<Material,List<CombineInstance>>();
            foreach(var r in renderers)
            {
                var source=r.GetComponent<MeshFilter>().sharedMesh;
                foreach(var vertex in source.vertices)
                    if(float.IsNaN(vertex.x)||float.IsInfinity(vertex.x)||float.IsNaN(vertex.y)||float.IsInfinity(vertex.y)||float.IsNaN(vertex.z)||float.IsInfinity(vertex.z))
                        throw new InvalidOperationException("Nonfinite workshop geometry: "+zone.name+"/"+r.name);
                if(!groups.TryGetValue(r.sharedMaterial,out var list)){list=new List<CombineInstance>();groups[r.sharedMaterial]=list;}
                list.Add(new CombineInstance{mesh=source,transform=zone.worldToLocalMatrix*r.transform.localToWorldMatrix});
            }
            foreach(var pair in groups)
            {
                string path=RootDir+"/"+zone.name+"_"+pair.Key.name+".asset";
                var mesh=AssetDatabase.LoadAssetAtPath<Mesh>(path);
                if(mesh==null){mesh=new Mesh{name=zone.name+"_"+pair.Key.name,indexFormat=IndexFormat.UInt32};AssetDatabase.CreateAsset(mesh,path);}else mesh.Clear();
                mesh.indexFormat=IndexFormat.UInt32;mesh.CombineMeshes(pair.Value.ToArray(),true,true,false);mesh.RecalculateBounds();mesh.UploadMeshData(false);EditorUtility.SetDirty(mesh);
                Render(zone,"Baked_"+pair.Key.name,mesh,Vector3.zero,Quaternion.identity,pair.Key);
            }
            foreach(var r in renderers) if(r!=null) Object.DestroyImmediate(r.gameObject);
        }

        static Material Material(string name,Color color,float metallic,float smoothness,int textureStyle)
        {
            string path=MatDir+"/"+name+".mat";var m=AssetDatabase.LoadAssetAtPath<Material>(path);
            if(m==null){m=new Material(Shader.Find("Universal Render Pipeline/Lit")){name=name};AssetDatabase.CreateAsset(m,path);}
            m.SetColor("_BaseColor",color);m.SetFloat("_Metallic",metallic);m.SetFloat("_Smoothness",smoothness);
            m.enableInstancing=true;
            if(textureStyle>0) m.SetTexture("_BaseMap",SurfaceTexture(textureStyle));
            EditorUtility.SetDirty(m);return m;
        }
        public static void ApplyOak(Material material)
        {
            EnsureFolder(MatDir);
            material.SetTexture("_BaseMap",SurfaceTexture(1));
            material.SetTextureScale("_BaseMap",new Vector2(1.5f,1.5f));
            EditorUtility.SetDirty(material);
        }
        static Texture2D SurfaceTexture(int style)
        {
            string path=MatDir+(style==1?"/OakGrain.asset":"/FineSurface.asset");var texture=AssetDatabase.LoadAssetAtPath<Texture2D>(path);if(texture!=null)return texture;
            const int size=256;texture=new Texture2D(size,size,TextureFormat.RGBA32,true,false){name=style==1?"OakGrain":"FineSurface",wrapMode=TextureWrapMode.Repeat,filterMode=FilterMode.Trilinear,anisoLevel=4};
            var colors=new Color[size*size];
            for(int y=0;y<size;y++)for(int x=0;x<size;x++)
            {
                float u=x/(float)size,v=y/(float)size;
                float noise=Mathf.PerlinNoise(u*27+2,v*27+4);
                float value;
                if(style==1)
                {
                    float bend=Mathf.PerlinNoise(u*3+1,v*2+3)*.15f;
                    float stripe=Mathf.Sin((u+bend)*235+Mathf.PerlinNoise(u*7,v*1.4f)*9)*.5f+.5f;
                    float grain=Mathf.PerlinNoise(u*73+9,v*4+1);value=.70f+stripe*.17f+grain*.09f+noise*.04f;
                }
                else value=.90f+noise*.10f;
                colors[y*size+x]=new Color(value,value,value,1);
            }
            texture.SetPixels(colors);texture.Apply(true,false);AssetDatabase.CreateAsset(texture,path);return texture;
        }
        static void EnsureFolder(string path)
        {
            if(AssetDatabase.IsValidFolder(path))return;int slash=path.LastIndexOf('/');EnsureFolder(path.Substring(0,slash));AssetDatabase.CreateFolder(path.Substring(0,slash),path.Substring(slash+1));
        }
    }
}
