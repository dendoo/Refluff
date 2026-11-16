// Ephemeral filming rig. Run via unity command run_script, outside Assets: no domain reload or production changes.
using System;
using System.IO;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UIElements;
using UnityEditor;
using CarpetCleaning;
using Object = UnityEngine.Object;

namespace ScraperUpgradeFilm
{
    public static class Rig
    {
        static readonly string Root = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
        static readonly string Work = Path.Combine(Root, "SteamStore/_work/tiktok_scraper");
        const string TempCompute = "Assets/CarpetCleaning/Editor/_CaptureOnly/ScuStage.compute";
        static CarpetSimulation sim;
        static CarpetHandover hand;
        static CarpetRenderer view;
        static ToolController tools;
        static DebrisSystem debris;
        static Camera cam;
        static ComputeShader stage;
        static Texture2D picture;
        static Texture2D[] originalPictures;
        static Texture originalPicture;
        static Vector4 originalScale, originalOffset;
        static Color originalCarpetColor;
        static Mesh originalMesh, filmMesh;
        static Vector3 originalCamPos;
        static Quaternion originalCamRot;
        static float originalFov, originalNear;
        static Light sun;
        static Quaternion originalSunRot;
        static float originalSunIntensity, originalSunTemp;
        static LightShadows originalShadows;
        static Color originalAmbient;
        static UIDocument doc;
        static PanelSettings panel;
        static RenderTexture originalPanelTarget;
        static PanelScaleMode originalScaleMode;
        static float originalPanelScale;
        static string originalLanguage;
        static bool installed, cleanupEnabled;
        static float takeStart;
        static int shot;
        static Vector3 fixedPos = new Vector3(0f, 2.85f, -1.12f);
        static Vector3 fixedTarget = new Vector3(0f, 0.01f, 0.02f);
        static float fixedFov = 44f;
        static Action<ScriptableRenderContext, List<Camera>> renderHook;
        static List<Action> undoStyles = new List<Action>();
        static HashSet<VisualElement> styled = new HashSet<VisualElement>();

        public static string Main()
        {
            var ad = AppDomain.CurrentDomain;
            ad.SetData("scu.prepare", (Func<string>)Prepare);
            ad.SetData("scu.probe", (Func<string>)Probe);
            ad.SetData("scu.arrival", (Func<string>)Arrival);
            ad.SetData("scu.weak", (Func<string>)Weak);
            ad.SetData("scu.shop", (Func<string>)Shop);
            ad.SetData("scu.power", (Func<string>)Power);
            ad.SetData("scu.reveal", (Func<string>)Reveal);
            ad.SetData("scu.restore", (Func<string>)Restore);
            ad.SetData("scu.stats", (Func<string>)(() => CarpetDebugApi.StatsJson()));
            ad.SetData("scu.human.stageweak", (Func<string>)HumanStageWeak);
            ad.SetData("scu.human.stagestrong", (Func<string>)HumanStageStrong);
            ad.SetData("scu.human.replayweak", (Func<string>)HumanReplayWeak);
            ad.SetData("scu.human.rebuildweak", (Func<string>)HumanRebuildWeak);
            ad.SetData("scu.human.replaystrong", (Func<string>)HumanReplayStrong);
            ad.SetData("scu.human.shop", (Func<string>)HumanShop);
            ad.SetData("scu.human.reveal", (Func<string>)HumanReveal);
            return "Film rig installed in memory; no project C# imported";
        }

        static void Park()
        {
            tools.ScriptedPointer = true;
            tools.ScriptedUv = new Vector2(-5f, -5f);
            tools.ScriptedDown = false;
            tools.ScriptedRight = false;
        }

        static void RealSteps(int n)
        {
            EditorApplication.isPaused = true;
            Park();
            for (int i = 0; i < n; i++) EditorApplication.Step();
        }

        static void Pose(Vector3 pos, Vector3 target, float fov)
        {
            Vector3 fwd = (target - pos).normalized;
            float w = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(.9f, .999f, Mathf.Abs(fwd.y)));
            cam.transform.SetPositionAndRotation(pos, Quaternion.LookRotation(fwd, Vector3.Slerp(Vector3.up, Vector3.forward, w)));
            cam.fieldOfView = fov;
            cam.nearClipPlane = .01f;
        }

        static void Render(ScriptableRenderContext ctx, List<Camera> cameras)
        {
            if (!installed) return;
            if (cleanupEnabled) CleanCrumbs();
            float t = Mathf.Max(0, Time.time - takeStart);
            if (shot == 1)
            {
                float a = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((t - 1.35f) / 2.2f));
                Pose(Vector3.Lerp(new Vector3(0f, 3.2f, -1.4f), fixedPos, a), Vector3.Lerp(new Vector3(0f,.01f,.12f), fixedTarget,a), Mathf.Lerp(44f,fixedFov,a));
            }
            else if (shot == 5)
            {
                float a = Mathf.SmoothStep(0f,1f,Mathf.Clamp01(t / 2.8f));
                Pose(Vector3.Lerp(fixedPos, new Vector3(0f,3.05f,-.90f),a), fixedTarget, Mathf.Lerp(fixedFov,43f,a));
            }
            else Pose(fixedPos, fixedTarget, fixedFov);
        }

        public static string Prepare()
        {
            if (installed) return "already prepared";
            if (!EditorApplication.isPlaying) throw new Exception("Enter Play first");
            sim = CarpetDebugApi.Sim; hand = CarpetDebugApi.Handover; tools = CarpetDebugApi.Tools;
            debris=Object.FindAnyObjectByType<DebrisSystem>();
            view = Object.FindAnyObjectByType<CarpetRenderer>(); cam = Camera.main;
            originalCamPos=cam.transform.position; originalCamRot=cam.transform.rotation; originalFov=cam.fieldOfView; originalNear=cam.nearClipPlane;
            originalLanguage=Loc.Code;
            CarpetDebugApi.SetLanguage("en", false);
            Park();
            EditorApplication.isPaused=true;
            EconomyDebugApi.ShopOpen(false);
            CarpetDebugApi.SetHighlight(0,false,true); CarpetDebugApi.SetHighlight(1,false,true);
            originalPictures=hand.pictures;
            var mat=view.ShellsRenderer.sharedMaterial;
            originalPicture=mat.GetTexture("_PictureTex"); originalScale=mat.GetVector("_PicScale"); originalOffset=mat.GetVector("_PicOffset");
            originalCarpetColor=mat.GetColor("_CarpetColor");
            picture=new Texture2D(2,2,TextureFormat.RGBA32,false,false) { name="spongebob_film_only", hideFlags=HideFlags.DontUnloadUnusedAsset };
            if(!ImageConversion.LoadImage(picture,File.ReadAllBytes(Path.Combine(Root,"SteamStore/tiktok/scraper_upgrades/spongebob.png")))) throw new Exception("picture decode failed");
            picture.wrapMode=TextureWrapMode.Clamp;
            hand.pictures=new[]{picture};
            var bag=(List<int>)typeof(CarpetHandover).GetField("_picBag",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance).GetValue(hand);bag.Clear();
            typeof(CarpetHandover).GetField("_lastPicture",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance).SetValue(hand,-1);
            mat.SetTexture("_PictureTex",picture); CarpetHandover.PictureFit(picture,out var off,out var scale);mat.SetVector("_PicOffset",off);mat.SetVector("_PicScale",scale);
            string dir=Path.GetDirectoryName(Path.Combine(Root,TempCompute));Directory.CreateDirectory(dir);
            File.Copy(Path.Combine(Root,"Tools/steam_art/tiktok/scraper_upgrades/StageCrust.compute"),Path.Combine(Root,TempCompute),true);
            AssetDatabase.ImportAsset(TempCompute,ImportAssetOptions.ForceSynchronousImport);
            stage=AssetDatabase.LoadAssetAtPath<ComputeShader>(TempCompute);
            if(stage==null)throw new Exception("staging compute not loaded");
            var mf=view.Shells.GetComponent<MeshFilter>();originalMesh=mf.sharedMesh;
            var size=CarpetSimulation.CarpetSizeM;
            filmMesh=ShellMeshBuilder.BuildShells(size.x,size.y,48,view.shellSubdivX,view.shellSubdivZ,view.pileHeightM);
            filmMesh.name="ScuFilmShells";filmMesh.hideFlags=HideFlags.DontUnloadUnusedAsset;mf.sharedMesh=filmMesh;
            sun=GameObject.Find("Directional Light").GetComponent<Light>();
            originalSunRot=sun.transform.rotation;originalSunIntensity=sun.intensity;originalSunTemp=sun.colorTemperature;originalShadows=sun.shadows;originalAmbient=RenderSettings.ambientSkyColor;
            sun.transform.rotation=Quaternion.Euler(40f,30f,0);sun.intensity=4.2f;sun.colorTemperature=4800;sun.shadows=LightShadows.Soft;
            RenderSettings.ambientSkyColor=new Color(.62f,.65f,.72f);
            doc=EconomyDebugApi.Hud.GetComponent<UIDocument>();panel=doc.panelSettings;
            originalPanelTarget=panel.targetTexture;originalScaleMode=panel.scaleMode;originalPanelScale=panel.scale;
            panel.targetTexture=null;panel.scaleMode=PanelScaleMode.ConstantPixelSize;panel.scale=1f;
            SnapshotStyle(doc.rootVisualElement);doc.rootVisualElement.style.width=1080;doc.rootVisualElement.style.height=1920;
            installed=true;
            renderHook=Render;RenderPipelineManager.beginContextRendering+=renderHook;
            StageNew();
            EconomyDebugApi.SetLevel(0,0,0);EconomyDebugApi.SetLevel(0,1,0);EconomyDebugApi.Grant(10000-Economy.Instance.Money);
            EconomyDebugApi.LiveTuning(true);ToolTuning.Set(ToolId.Scraper,.18f,1f);
            CarpetDebugApi.SetTool(0);RealSteps(3);
            PortraitShop();
            return Probe();
        }

        static void StageNew()
        {
            CarpetDebugApi.ResetWithProfile(394,1f,1f,.20f,0f,0f);
            StageDirt(); cleanupEnabled=true;
        }

        static void StageDirt()
        {
            // Kernel bindings filled below from the capture-only shader's documented interface.
            int k=stage.FindKernel("K_Stage");
            stage.SetInts("_Size",sim.Dirt.width,sim.Dirt.height);
            stage.SetInt("_Seed",394);
            stage.SetTexture(k,"_Dirt",sim.Dirt);
            stage.Dispatch(k,(sim.Dirt.width+7)/8,(sim.Dirt.height+7)/8,1);
        }

        static void CleanCrumbs()
        {
            int k=stage.FindKernel("K_CleanCrumbs");
            stage.SetInts("_Size",sim.Dirt.width,sim.Dirt.height);
            stage.SetInt("_Seed",394);
            stage.SetTexture(k,"_Dirt",sim.Dirt);
            stage.Dispatch(k,(sim.Dirt.width+7)/8,(sim.Dirt.height+7)/8,1);
            int p=stage.FindKernel("K_ClearSettledCrumbs");
            stage.SetInt("_ParticleCount",DebrisSystem.ParticleCount);
            stage.SetInt("_CarpetAway",sim.CarpetAway?1:0);
            stage.SetFloat("_PileHeightMm",sim.settings.PILE_H);
            stage.SetBuffer(p,"_Particles",debris.Particles);
            stage.SetTexture(p,"_FiberTex",sim.Fiber);
            stage.Dispatch(p,(DebrisSystem.ParticleCount+63)/64,1,1);
        }

        static string Begin(string name,int mode,bool hud=false)
        {
            Park();shot=mode;
            string result=StoreGifApi.SeqBegin(name,2160,3840,0,0,0,0,hud,1,true,1080,1920,60,true);
            takeStart=Time.time;
            return result;
        }

        public static string Probe()
        {
            Pose(fixedPos,fixedTarget,fixedFov);Park();
            string scene=StoreShotApi.RenderScene(Path.Combine(Work,"probe_scene.png"),1080,1920);
            return scene+" "+CarpetDebugApi.StatsJson();
        }

        public static string Arrival()
        {
            EconomyDebugApi.ShopOpen(false);StageNew();EconomyDebugApi.LiveTuning(true);ToolTuning.Set(ToolId.Scraper,.18f,1f);
            hand.SnapToIdle();hand.Begin(true);hand.Tick(2f);
            string s=Begin("scu_arrival",1);
            StoreGifApi.Away(.3f);
            StoreGifApi.Call(()=>{hand.TakeNewCarpet();sim.ResetCarpet(394,false,CarpetSimulation.RollDirt(394,1f));StageDirt();});
            StoreGifApi.Away(4.7f);
            return s;
        }

        public static string Weak()
        {
            EconomyDebugApi.ShopOpen(false);EconomyDebugApi.LiveTuning(true);ToolTuning.Set(ToolId.Scraper,.18f,1f);
            string s=Begin("scu_weak",2);StoreGifApi.Tool(0);
            StoreGifApi.Hover(-.04f,.17f,.25f);
            StoreGifApi.Stroke(-.02f,.17f,.34f,.17f,1.0f);
            StoreGifApi.Move(.34f,.17f,-.02f,.17f,1f,true);
            StoreGifApi.Move(-.02f,.17f,.34f,.17f,1f,true);
            StoreGifApi.Move(.34f,.17f,-.02f,.17f,1f,true);
            StoreGifApi.Hover(-.02f,.17f,.55f);StoreGifApi.Away(.7f);
            return s;
        }

        public static string Shop()
        {
            string s=Begin("scu_shop",3,true);
            StoreGifApi.Away(.25f);StoreGifApi.Call(()=>EconomyDebugApi.ShopOpen(true));
            StoreGifApi.Away(1.75f);
            for(int i=0;i<6;i++){StoreGifApi.Call(()=>StoreGifApi.ShopBuy(0,0));StoreGifApi.Away(i==5?.25f:.30f);}
            for(int i=0;i<6;i++){StoreGifApi.Call(()=>StoreGifApi.ShopBuy(0,1));StoreGifApi.Away(14f/60f);}
            StoreGifApi.Away(.85f);
            return s;
        }

        public static string Power()
        {
            EconomyDebugApi.ShopOpen(false);EconomyDebugApi.LiveTuning(true);ToolTuning.Set(ToolId.Scraper,20f,5f);
            string s=Begin("scu_power",4);StoreGifApi.Tool(0);
            StoreGifApi.Hover(-.04f,.17f,.20f);
            StoreGifApi.Stroke(-.06f,.17f,1.06f,.17f,.80f);
            StoreGifApi.Move(1.06f,.17f,1.06f,.50f,.10f,false);
            StoreGifApi.Call(()=>tools.SnapHeadingOnNextMove());
            StoreGifApi.Move(1.06f,.50f,-.06f,.50f,.80f,true);
            StoreGifApi.Move(-.06f,.50f,-.06f,.83f,.10f,false);
            StoreGifApi.Call(()=>tools.SnapHeadingOnNextMove());
            StoreGifApi.Move(-.06f,.83f,1.06f,.83f,.80f,true);
            StoreGifApi.Hover(1.06f,.83f,.20f);StoreGifApi.Away(.20f);
            return s;
        }

        public static string Reveal()
        {
            string s=Begin("scu_reveal",5);StoreGifApi.Away(7.1f);return s;
        }

        public static string HumanStageWeak()
        {
            StoreGifApi.SeqAbort();EconomyDebugApi.ShopOpen(false);StageNew();hand.SnapToIdle();
            shot=2;view.ShellsRenderer.sharedMaterial.SetColor("_CarpetColor",new Color(.32f,.20f,.40f));
            EconomyDebugApi.SetLevel(0,0,0);EconomyDebugApi.SetLevel(0,1,0);EconomyDebugApi.LiveTuning(true);
            ToolTuning.Set(ToolId.Scraper,.18f,1f);CarpetDebugApi.SetTool(0);
            sim.Paused=false;sim.UiPaused=false;tools.UiModal=false;tools.Suspended=false;tools.InputEnabled=true;
            Pose(fixedPos,fixedTarget,fixedFov);RealSteps(3);
            return "Weak live stage ready; arm human recorder next";
        }

        public static string HumanStageStrong()
        {
            StoreGifApi.SeqAbort();EconomyDebugApi.ShopOpen(false);shot=4;
            EconomyDebugApi.LiveTuning(true);ToolTuning.Set(ToolId.Scraper,20f,5f);CarpetDebugApi.SetTool(0);
            sim.Paused=false;sim.UiPaused=false;tools.UiModal=false;tools.Suspended=false;tools.InputEnabled=true;
            // The shop arms ToolController's button-release gate when it closes.
            // Consume an unpressed parked frame BEFORE replay's first held sample.
            Pose(fixedPos,fixedTarget,fixedFov);RealSteps(1);tools.SnapHeadingOnNextMove();
            return "Strong live stage ready; same carpet retained";
        }

        static string ReplayPath(string kind)
        {
            return ((Func<string,string>)AppDomain.CurrentDomain.GetData("scu.human.replay"))(kind);
        }

        public static string HumanReplayWeak()
        {
            HumanStageWeak();string s=Begin("scu_weak_human",2);ReplayPath("weak");StoreGifApi.Away(.25f);return s;
        }

        public static string HumanRebuildWeak()
        {
            HumanStageWeak();Park();shot=2;
            string s=StoreGifApi.SeqBegin("scu_rebuild_weak",32,32,0,0,0,0,false,10000,true,1080,1920,60,false);
            takeStart=Time.time;ReplayPath("weak");StoreGifApi.Away(.05f);return s;
        }

        public static string HumanReplayStrong()
        {
            HumanStageStrong();AppDomain.CurrentDomain.SetData("scu.human.initialheading.strong",90f);
            string s=Begin("scu_power_human",4);ReplayPath("strong");StoreGifApi.Away(.3f);return s;
        }

        public static string HumanShop()
        {
            Park();EconomyDebugApi.ShopOpen(false);EconomyDebugApi.SetLevel(0,0,0);EconomyDebugApi.SetLevel(0,1,0);
            string s=Begin("scu_shop_human",3,true);
            StoreGifApi.Away(.25f);StoreGifApi.Call(()=>EconomyDebugApi.ShopOpen(true));StoreGifApi.Away(1.75f);
            for(int i=0;i<6;i++){StoreGifApi.Call(()=>StoreGifApi.ShopBuy(0,0));StoreGifApi.Away(i==5?.25f:.30f);}
            for(int i=0;i<6;i++){StoreGifApi.Call(()=>StoreGifApi.ShopBuy(0,1));StoreGifApi.Away(14f/60f);}
            StoreGifApi.Away(.85f);return s;
        }

        public static string HumanReveal()
        {
            string s=Begin("scu_reveal_human",5);StoreGifApi.Away(7.1f);return s;
        }

        static void Hide(VisualElement e)
        {
            if(e==null)return;var old=e.style.display;undoStyles.Add(()=>e.style.display=old);e.style.display=DisplayStyle.None;
        }

        static void SnapshotStyle(VisualElement e)
        {
            if(e==null||!styled.Add(e))return;
            var s=e.style;
            var width=s.width;var maxWidth=s.maxWidth;var minWidth=s.minWidth;var height=s.height;
            var pt=s.paddingTop;var pb=s.paddingBottom;var pl=s.paddingLeft;var pr=s.paddingRight;
            var mb=s.marginBottom;var mr=s.marginRight;var fs=s.fontSize;var fg=s.flexGrow;var fd=s.flexDirection;var fw=s.flexWrap;
            undoStyles.Add(()=>{var x=e.style;x.width=width;x.maxWidth=maxWidth;x.minWidth=minWidth;x.height=height;
                x.paddingTop=pt;x.paddingBottom=pb;x.paddingLeft=pl;x.paddingRight=pr;x.marginBottom=mb;x.marginRight=mr;
                x.fontSize=fs;x.flexGrow=fg;x.flexDirection=fd;x.flexWrap=fw;});
        }

        static void PortraitShop()
        {
            var root=doc.rootVisualElement;var hud=root.Q<VisualElement>(className:"hud");
            if(hud!=null)foreach(var e in hud.Children())if(e.name!="shop"&&e.name!="fx")Hide(e);
            var p=root.Q<VisualElement>("shop-panel");SnapshotStyle(p);p.style.width=880;p.style.maxWidth=880;
            var body=p.Q<VisualElement>(className:"shop__body");SnapshotStyle(body);body.style.paddingTop=32;body.style.paddingBottom=32;
            Hide(p.Q<VisualElement>(className:"shop__col--floor"));Hide(p.Q<VisualElement>(className:"shop__legend"));Hide(root.Q("shop-close"));
            var list=root.Q("shop-carpet-tools");SnapshotStyle(list);list.style.width=780;list.style.flexDirection=FlexDirection.Column;
            var cards=list.Query<VisualElement>(className:"ucard").ToList();for(int i=1;i<cards.Count;i++)Hide(cards[i]);
            var card=cards[0];SnapshotStyle(card);foreach(var e in card.Query<VisualElement>().ToList())SnapshotStyle(e);
            card.style.width=780;card.style.paddingTop=24;card.style.paddingBottom=24;card.style.paddingLeft=22;card.style.paddingRight=22;
            card.Q<Label>(className:"ucard__name").style.fontSize=40;
            card.Q<VisualElement>(className:"ucard__head").style.marginBottom=22;
            var title=p.Q<Label>(className:"shop__title");SnapshotStyle(title);title.style.fontSize=42;
            foreach(var lbl in p.Query<Label>(className:"caption").ToList()){SnapshotStyle(lbl);lbl.style.fontSize=22;}
            foreach(var row in card.Query<VisualElement>(className:"urow").ToList())
            {
                row.style.height=115;row.style.flexWrap=Wrap.Wrap;row.style.marginBottom=16;
                var cap=row.Q<Label>(className:"urow__caption");cap.style.width=112;cap.style.minWidth=112;cap.style.fontSize=24;
                var icon=row.Q<VisualElement>(className:"urow__icon");icon.style.width=24;icon.style.height=24;
                foreach(var pip in row.Query<VisualElement>(className:"pip").ToList()){pip.style.width=28;pip.style.height=28;pip.style.marginRight=8;}
                var gain=row.Q<Label>(className:"urow__gain");gain.style.fontSize=20;gain.style.flexGrow=0;gain.style.width=150;
                var buy=row.Q<Button>(className:"ubuy");buy.style.height=48;buy.style.minWidth=120;
                row.Q<Label>(className:"ubuy__label").style.fontSize=24;
            }
        }

        public static string Restore()
        {
            StoreGifApi.SeqAbort();
            if(renderHook!=null)RenderPipelineManager.beginContextRendering-=renderHook;renderHook=null;
            cleanupEnabled=false;
            if(!installed)return "not prepared";
            EconomyDebugApi.ShopOpen(false);Park();
            for(int i=undoStyles.Count-1;i>=0;i--)undoStyles[i]();undoStyles.Clear();styled.Clear();
            if(panel!=null){panel.targetTexture=originalPanelTarget;panel.scaleMode=originalScaleMode;panel.scale=originalPanelScale;}
            view.Shells.GetComponent<MeshFilter>().sharedMesh=originalMesh;if(filmMesh!=null)Object.DestroyImmediate(filmMesh);
            hand.pictures=originalPictures;hand.SnapToIdle();
            var mat=view.ShellsRenderer.sharedMaterial;mat.SetTexture("_PictureTex",originalPicture);mat.SetVector("_PicScale",originalScale);mat.SetVector("_PicOffset",originalOffset);
            mat.SetColor("_CarpetColor",originalCarpetColor);
            if(picture!=null)Object.DestroyImmediate(picture);
            sun.transform.rotation=originalSunRot;sun.intensity=originalSunIntensity;sun.colorTemperature=originalSunTemp;sun.shadows=originalShadows;RenderSettings.ambientSkyColor=originalAmbient;
            cam.transform.SetPositionAndRotation(originalCamPos,originalCamRot);cam.fieldOfView=originalFov;cam.nearClipPlane=originalNear;
            CarpetDebugApi.SetLanguage(originalLanguage,false);ToolTuning.Set(ToolId.Scraper,1f,1f);ToolTuning.SetNeutral(false);
            if(stage!=null){stage=null;AssetDatabase.DeleteAsset(TempCompute);AssetDatabase.DeleteAsset("Assets/CarpetCleaning/Editor/_CaptureOnly");}
            tools.ScriptedPointer=false;EditorApplication.isPaused=false;installed=false;
            return "Film camera, lighting, material, UI panel and capture assets restored";
        }
    }
}
