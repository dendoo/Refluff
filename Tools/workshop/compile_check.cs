using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using System;
using System.IO;
using UnityEditor.SceneManagement;
using Object = UnityEngine.Object;
using System.Collections.Generic;

namespace CarpetCleaning
{
    /// <summary>The walkable workshop, built by CarpetSceneBuilder; simulation coordinates stay unchanged.</summary>
    public static class FirstPersonSceneBuilder
    {
        const string MaterialDir = "Assets/CarpetCleaning/Materials/Workshop";
        const float RoomHalf = 6f;
        const float RoomHeight = 3.8f;

        public static void Build(GameObject carpet, Camera camera)
        {
            if (!AssetDatabase.IsValidFolder(MaterialDir))
                AssetDatabase.CreateFolder("Assets/CarpetCleaning/Materials", "Workshop");
            var old = GameObject.Find("Workshop");
            if (old != null) Object.DestroyImmediate(old);
            var room = new GameObject("Workshop").transform;
            var plaster = Mat("Plaster", new Color(.76f,.69f,.56f), 0, .12f);
            var teal = Mat("Paint", new Color(.24f,.34f,.29f), .05f, .29f);
            var steel = Mat("Steel", new Color(.17f,.20f,.21f), .65f, .42f);
            var wood = Mat("Oak", new Color(.53f,.35f,.19f), 0, .32f);
            var dark = Mat("Rubber", new Color(.055f,.072f,.077f), 0, .2f);
            var glass = Mat("Daylight", new Color(.75f,.88f,.91f), .1f, .65f, 1.2f);
            var windowGlass = Mat("WindowDaylight", new Color(1f,.84f,.58f), 0f, .25f, 3.7f);
            var glow = Mat("Light", new Color(1f,.94f,.77f), 0, .3f, 2.3f);
            var doorPaint = Mat("DoorPaint", new Color(.40f,.51f,.48f), .05f, .35f);
            var doorInset = Mat("DoorInset", new Color(.32f,.42f,.39f), .05f, .30f);
            var brass = Mat("BrushedBrass", new Color(.62f,.51f,.31f), .65f, .4f);

            // The existing FloorSurface still renders all tile; this invisible slab is for walking only.
            var floor = new GameObject("WalkingFloor"); floor.transform.SetParent(room, false);
            floor.transform.localPosition = new Vector3(0,-.103f,0);
            var floorCollider = floor.AddComponent<BoxCollider>(); floorCollider.size = new Vector3(12.16f,.2f,12.16f);
            Cube(room,"NorthWall",new Vector3(0,RoomHeight*.5f,RoomHalf+.08f),new Vector3(12.32f,RoomHeight,.16f),plaster);
            ApertureWall(room,"SouthWall",new Vector3(0,0,-RoomHalf-.08f),Vector3.forward,-1.2f,plaster);
            Cube(room,"WestWall",new Vector3(-RoomHalf-.08f,RoomHeight*.5f,0),new Vector3(.16f,RoomHeight,12f),plaster);
            ApertureWall(room,"EastWall",new Vector3(RoomHalf+.08f,0,0),Vector3.left,1.8f,plaster);
            Cube(room,"NorthWainscot",new Vector3(0,.53f,RoomHalf-.014f),new Vector3(12f,1.06f,.025f),teal,false);
            Cube(room,"SouthWainscot",new Vector3(0,.53f,-RoomHalf+.014f),new Vector3(12f,1.06f,.025f),teal,false);
            Cube(room,"WestWainscot",new Vector3(-RoomHalf+.014f,.53f,0),new Vector3(.025f,1.06f,12f),teal,false);
            Cube(room,"EastWainscot",new Vector3(RoomHalf-.014f,.53f,0),new Vector3(.025f,1.06f,12f),teal,false);
            Cube(room,"Ceiling",new Vector3(0,RoomHeight+.06f,0),new Vector3(12.16f,.12f,12.16f),plaster,false);

            // Actual openings and mullions cast the same sunlight sampled by the participating medium.
            Vector3 sunlight = Quaternion.Euler(38f,-30f,0f) * Vector3.forward;
            WindowBay(room,"SouthWindow",new Vector3(-1.2f,2.65f,-5.94f),Vector3.forward,steel,windowGlass,wood,sunlight);
            WindowBay(room,"EastWindow",new Vector3(5.94f,2.65f,1.8f),Vector3.left,steel,windowGlass,wood,sunlight);
            ClosedDoor(room,doorPaint,doorInset,steel,brass);

            for(int row=0;row<2;row++) for(int i=0;i<3;i++)
            {
                Vector3 p = new Vector3(-3.4f+i*3.4f,RoomHeight-.065f,-2.7f+row*5.4f);
                Cube(room,"CeilingLightHousing"+row+"_"+i,p,new Vector3(.34f,.12f,2.4f),steel,false);
                Cube(room,"CeilingLightDiffuser"+row+"_"+i,p+Vector3.down*.065f,new Vector3(.28f,.025f,2.28f),glow,false);
            }

            var bench = new GameObject("UpgradeWorkbench").transform; bench.SetParent(room,false);
            bench.localPosition = new Vector3(-4.6f,0,.5f);
            var top = Cube(bench,"OakTop",new Vector3(0,.91f,0),new Vector3(.78f,.09f,1.8f),wood);
            Cube(bench,"Cabinet",new Vector3(-.02f,.43f,.38f),new Vector3(.65f,.82f,.75f),teal);
            for(int i=0;i<4;i++)
            {
                Cube(bench,"Drawer"+i,new Vector3(.315f,.2f+i*.17f,.38f),new Vector3(.02f,.145f,.67f),steel,false);
                Cube(bench,"DrawerPull"+i,new Vector3(.344f,.22f+i*.17f,.38f),new Vector3(.035f,.027f,.26f),dark,false);
            }
            for(int i=0;i<2;i++) Cube(bench,"Leg"+i,new Vector3(-.28f+i*.56f,.43f,-.76f),new Vector3(.065f,.86f,.065f),steel);
            Cube(bench,"LowerShelf",new Vector3(0,.18f,-.48f),new Vector3(.65f,.04f,.67f),steel,false);
            Cube(bench,"WorkMat",new Vector3(0,.962f,-.24f),new Vector3(.58f,.014f,.78f),dark,false);
            // Small upgrade terminal: a physical focus for the existing shop, no duplicated text in world space.
            Cube(bench,"TerminalBase",new Vector3(-.12f,.99f,.48f),new Vector3(.3f,.04f,.3f),steel,false);
            Cube(bench,"TerminalStem",new Vector3(-.2f,1.11f,.48f),new Vector3(.045f,.23f,.08f),steel,false);
            var screen = Cube(bench,"Terminal",new Vector3(-.16f,1.27f,.48f),new Vector3(.045f,.32f,.48f),dark);
            screen.transform.localRotation=Quaternion.Euler(0,0,12);
            Cube(screen.transform,"Screen",new Vector3(.52f,0,0),new Vector3(.035f,.83f,.87f),glass,false);
            // Storage stays beyond the enlarged cleaning area and leaves a continuous walking route.
            Cube(room,"StorageCabinet",new Vector3(4.8f,.52f,4.8f),new Vector3(1.4f,1.04f,.8f),teal);
            Cube(room,"StorageTop",new Vector3(4.8f,1.07f,4.8f),new Vector3(1.47f,.06f,.87f),wood,false);
            for(int i=0;i<2;i++) Cube(room,"StorageDoor"+i,new Vector3(4.45f+i*.70f,.55f,4.388f),new Vector3(.665f,.9f,.025f),steel,false);
            // The carpet's existing key/ambient stay intact. Outward-facing fills make vertical furniture readable.
            Fill(room,"WorkbenchFill",new Vector3(-3.35f,2.5f,.5f),new Vector3(-4.6f,.6f,.5f),1.5f,80f);
            Fill(room,"StorageFill",new Vector3(3.4f,2.5f,3.6f),new Vector3(4.8f,.6f,4.8f),1.2f,75f);
            Fill(room,"DoorFill",new Vector3(0f,2.8f,4.2f),new Vector3(0f,1.2f,5.85f),1.1f,65f);
            var playerGo=GameObject.Find("FirstPersonPlayer");
            if(playerGo==null) playerGo=new GameObject("FirstPersonPlayer");
            var cc=Ensure<CharacterController>(playerGo);
            cc.height=1.78f; cc.radius=.22f; cc.center=new Vector3(0,.89f,0); cc.stepOffset=.08f; cc.skinWidth=.015f;
            playerGo.transform.SetPositionAndRotation(new Vector3(0,-.003f,-3.8f),Quaternion.identity);
            camera.transform.SetParent(playerGo.transform,false);
            camera.transform.localPosition=new Vector3(0,1.62f,0);
            camera.transform.localRotation=Quaternion.Euler(28,0,0);
            camera.fieldOfView=68; camera.nearClipPlane=.025f;
            // The generated room has no baked occlusion data. Legacy occlusion can hide its far wall/furniture.
            camera.useOcclusionCulling=false;
            var cameraData = camera.GetComponent<UniversalAdditionalCameraData>();
            if (cameraData != null) cameraData.requiresDepthOption = CameraOverrideOption.On;
            var oldFill=camera.transform.Find("HeldToolFill");
            if(oldFill!=null) Object.DestroyImmediate(oldFill.gameObject);
            var heldFill=new GameObject("HeldToolFill"); heldFill.transform.SetParent(camera.transform,false);
            heldFill.transform.localPosition=new Vector3(-.12f,.05f,.12f);
            var heldLight=heldFill.AddComponent<Light>(); heldLight.type=LightType.Point;
            heldLight.intensity=.8f; heldLight.range=1.4f; heldLight.color=new Color(.88f,.94f,1f);
            heldLight.shadows=LightShadows.None; heldLight.cullingMask=1<<2;
            var player=Ensure<FirstPersonPlayer>(playerGo);
            var tools=carpet.GetComponent<ToolController>();
            player.viewCamera=camera; player.tools=tools; player.sim=carpet.GetComponent<CarpetSimulation>();
            player.initialPitch=28f;
            player.toolReach=FirstPersonPlayer.DefaultToolReach;
            player.movementBounds=new Rect(-5.65f,-5.65f,11.3f,11.3f); tools.player=player;
            var ui=GameObject.Find("CarpetUI");
            var workbench=bench.gameObject.AddComponent<UpgradeWorkbench>();
            workbench.viewCamera=camera; workbench.tools=tools; workbench.hud=ui.GetComponent<EconomyHud>();
            workbench.interactionCollider=top.GetComponent<Collider>();
            var hud=Ensure<FirstPersonHud>(ui); hud.tools=tools; hud.workbench=workbench;
            var held=Ensure<FirstPersonToolView>(camera.gameObject);
            held.viewCamera=camera; held.tools=tools; held.surfaceTools=carpet.GetComponentInChildren<ToolModelView>(true);
            held.generatedTools=FirstPersonModelImporter.EnsureSet();
            WorkshopFinishBuilder.Build(room, camera);
            WorkshopDetailBuilder.Build(room);
            WorkshopAtmosphereBuilder.Configure(room);
            EditorUtility.SetDirty(player); EditorUtility.SetDirty(tools); EditorUtility.SetDirty(camera);
            AssetDatabase.SaveAssets();
        }

        static void ClosedDoor(Transform room, Material paint, Material inset, Material frame, Material handle)
        {
            var door = new GameObject("ClosedDoor").transform;
            door.SetParent(room,false);
            door.localPosition = new Vector3(0f,0f,5.94f);
            door.localRotation = Quaternion.LookRotation(Vector3.back,Vector3.up);
            Cube(door,"Frame",new Vector3(0,1.24f,0),new Vector3(1.5f,2.48f,.13f),frame,false);
            // This is intentionally a shut architectural door. Both its leaf and the room wall are solid.
            Cube(door,"ClosedLeaf",new Vector3(0,1.19f,.095f),new Vector3(1.25f,2.32f,.09f),paint);
            Cube(door,"TopPanel",new Vector3(0,1.66f,.147f),new Vector3(1.01f,.85f,.014f),inset,false);
            Cube(door,"BottomPanel",new Vector3(0,.61f,.147f),new Vector3(1.01f,.85f,.014f),inset,false);
            Cube(door,"Threshold",new Vector3(0,.017f,.10f),new Vector3(1.27f,.028f,.23f),frame,false);
            Cube(door,"HandlePlate",new Vector3(.47f,1.11f,.159f),new Vector3(.075f,.20f,.021f),handle,false);
            Cube(door,"HandleSocket",new Vector3(.47f,1.14f,.19f),new Vector3(.038f,.038f,.067f),handle,false);
            Cube(door,"HandleLever",new Vector3(.39f,1.14f,.225f),new Vector3(.20f,.028f,.032f),handle,false);
            for (int i=0;i<3;i++)
                Cube(door,"Hinge"+i,new Vector3(-.637f,.37f+i*.79f,.147f),new Vector3(.032f,.10f,.037f),handle,false);
        }

        static void WindowBay(Transform room, string name, Vector3 position, Vector3 inward,
            Material frame, Material glass, Material sill, Vector3 travelDirection)
        {
            var bay = new GameObject(name).transform;
            bay.SetParent(room,false); bay.localPosition=position;
            bay.localRotation=Quaternion.LookRotation(inward,Vector3.up);
            const float width=4.5f, height=1.8f, rim=.065f, crossbar=.055f;
            const int columns=3;
            float paneWidth=(width-rim*(columns+1))/columns;
            float paneHeight=(height-rim*2-crossbar)*.5f;
            for(int edge=-1;edge<=1;edge+=2)
                Cube(bay,"HorizontalFrame"+edge,new Vector3(0,edge*(height-rim)*.5f,0),new Vector3(width,rim,.13f),frame,false);
            for(int col=0;col<=columns;col++)
                Cube(bay,"VerticalFrame"+col,new Vector3(-width*.5f+rim*.5f+col*(paneWidth+rim),0,0),new Vector3(rim,height,.13f),frame,false);
            Cube(bay,"Mullion",Vector3.zero,new Vector3(width,crossbar,.14f),frame,false);
            for(int col=0;col<columns;col++)
            {
                float x=-width*.5f+rim+paneWidth*.5f+col*(paneWidth+rim);
                for(int row=0;row<2;row++)
                {
                    float y=(row==0?-1f:1f)*(crossbar*.5f+paneHeight*.5f);
                    var pane=Cube(bay,"Glass"+col+"_"+row,new Vector3(x,y,-.02f),new Vector3(paneWidth,paneHeight,.018f),glass,false);
                    pane.GetComponent<MeshRenderer>().shadowCastingMode=ShadowCastingMode.Off;
                }
                // Each column is one physical volume. The horizontal mullion masks its density in the shader.
                Vector3 portal=bay.TransformPoint(new Vector3(x,0,.079f));
                WindowBeam(room,name+"Beam"+col,portal,bay.right,bay.up,travelDirection,
                    paneWidth*.5f,(height-rim*2)*.5f,crossbar*.5f,7f);
            }
            Cube(bay,"Sill",new Vector3(0,-height*.5f-.035f,.10f),new Vector3(width+.14f,.07f,.32f),sill,false);
        }

        static void WindowBeam(Transform room, string name, Vector3 origin, Vector3 right, Vector3 up,
            Vector3 direction, float halfWidth, float halfHeight, float halfCrossbar, float length)
        {
            var shader=Shader.Find("CarpetCleaning/WindowLightVolume");
            if(shader==null) throw new System.InvalidOperationException("WindowLightVolume shader must be imported before Build Scene.");
            string path=MaterialDir+"/"+name+".mat";
            var material=AssetDatabase.LoadAssetAtPath<Material>(path);
            if(material==null) { material=new Material(shader); AssetDatabase.CreateAsset(material,path); }
            else if(material.shader!=shader) material.shader=shader;
            direction.Normalize(); right.Normalize(); up.Normalize();
            material.SetColor("_Tint",new Color(1f,.96f,.86f,1f));
            material.SetVector("_WindowOrigin",new Vector4(origin.x,origin.y,origin.z,1f));
            material.SetVector("_WindowRight",new Vector4(right.x,right.y,right.z,halfWidth));
            material.SetVector("_WindowUp",new Vector4(up.x,up.y,up.z,halfHeight));
            material.SetVector("_BeamDirection",new Vector4(direction.x,direction.y,direction.z,length));
            material.SetVector("_BeamParams",new Vector4(.046f,.055f,.16f,halfCrossbar));
            material.SetVector("_NoiseParams",new Vector4(1.15f,.18f,.035f,3.5f));
            material.SetFloat("_Anisotropy",.28f);
            EditorUtility.SetDirty(material);

            // The proxy is just an AABB raster bound. Density follows a SHEARED rectangular portal extrusion,
            // so the shaft begins at the real window plane even though the sunlight is oblique to that plane.
            var bounds=new Bounds(origin,Vector3.zero);
            for(int end=0;end<2;end++) for(int x=-1;x<=1;x+=2) for(int y=-1;y<=1;y+=2)
                bounds.Encapsulate(origin+right*(x*halfWidth)+up*(y*halfHeight)+direction*(end*length));
            bounds.Expand(.04f);
            var beam=Cube(room,name,room.InverseTransformPoint(bounds.center),bounds.size,material,false);
            var renderer=beam.GetComponent<MeshRenderer>();
            renderer.shadowCastingMode=ShadowCastingMode.Off;
            renderer.receiveShadows=false;
            renderer.lightProbeUsage=LightProbeUsage.Off;
            renderer.reflectionProbeUsage=ReflectionProbeUsage.Off;
            beam.layer=2; // transparent visual only: never a first-person interaction/cleaning ray target
        }

        static T Ensure<T>(GameObject go) where T:Component
        { var c=go.GetComponent<T>(); return c!=null?c:go.AddComponent<T>(); }

        static void Fill(Transform parent,string name,Vector3 position,Vector3 target,float intensity,float angle)
        {
            var go=new GameObject(name); go.transform.SetParent(parent,false);
            go.transform.localPosition=position; go.transform.LookAt(parent.TransformPoint(target));
            var light=go.AddComponent<Light>(); light.type=LightType.Spot;
            light.color=new Color(1f,.95f,.87f); light.intensity=intensity;
            light.range=3.5f; light.spotAngle=angle; light.innerSpotAngle=angle*.55f;
            light.shadows=LightShadows.None;
        }

        static Material Mat(string name, Color color, float metallic, float smoothness, float emission=0)
        {
            string path=MaterialDir+"/"+name+".mat";
            var m=AssetDatabase.LoadAssetAtPath<Material>(path);
            if(m==null) { m=new Material(Shader.Find("Universal Render Pipeline/Lit")); AssetDatabase.CreateAsset(m,path); }
            m.SetColor("_BaseColor",color); m.SetFloat("_Metallic",metallic); m.SetFloat("_Smoothness",smoothness);
            // URP 17.7 validates _EMISSION from RealtimeDirectEmission, not just the HDR colour.
            // Without this flag its material validator silently removes the keyword after SaveAssets.
            m.globalIlluminationFlags=emission>0f ? MaterialGlobalIlluminationFlags.RealtimeDirectEmission : MaterialGlobalIlluminationFlags.None;
            m.SetColor("_EmissionColor",emission>0f ? color*emission : Color.black);
            if(emission>0f) m.EnableKeyword("_EMISSION"); else m.DisableKeyword("_EMISSION");
            EditorUtility.SetDirty(m); return m;
        }

        static GameObject Cube(Transform parent,string name,Vector3 position,Vector3 size,Material material,bool solid=true)
        {
            var go=GameObject.CreatePrimitive(PrimitiveType.Cube); go.name=name;
            go.transform.SetParent(parent,false); go.transform.localPosition=position; go.transform.localScale=size;
            var renderer=go.GetComponent<MeshRenderer>(); renderer.sharedMaterial=material;
            renderer.shadowCastingMode=ShadowCastingMode.On;
            if(!solid) Object.DestroyImmediate(go.GetComponent<Collider>());
            return go;
        }

        // The continuous collider keeps the walking boundary solid; four visible wall pieces leave a real light aperture.
        static void ApertureWall(Transform room,string name,Vector3 origin,Vector3 inward,float centre,Material material)
        {
            var wall=new GameObject(name); wall.transform.SetParent(room,false);
            wall.transform.localPosition=origin; wall.transform.localRotation=Quaternion.LookRotation(inward,Vector3.up);
            var collider=wall.AddComponent<BoxCollider>(); collider.center=new Vector3(0,RoomHeight*.5f,0); collider.size=new Vector3(12.32f,RoomHeight,.16f);
            const float lo=1.75f, hi=3.55f, half=2.25f;
            float left=centre-half,right=centre+half;
            Cube(wall.transform,"BelowWindow",new Vector3(0,lo*.5f,0),new Vector3(12.32f,lo,.16f),material,false);
            Cube(wall.transform,"AboveWindow",new Vector3(0,(hi+RoomHeight)*.5f,0),new Vector3(12.32f,RoomHeight-hi,.16f),material,false);
            Cube(wall.transform,"LeftPier",new Vector3((-6.16f+left)*.5f,(lo+hi)*.5f,0),new Vector3(left+6.16f,hi-lo,.16f),material,false);
            Cube(wall.transform,"RightPier",new Vector3((right+6.16f)*.5f,(lo+hi)*.5f,0),new Vector3(6.16f-right,hi-lo,.16f),material,false);
        }
    }
}


namespace CarpetCleaning
{
    /// <summary>Warm workshop art direction. All mutations are restricted to the environment and lighting.</summary>
    public static class WorkshopFinishBuilder
    {
        const string Dir = "Assets/CarpetCleaning/Materials/Workshop";

        [MenuItem("CarpetCleaning/Workshop/Rebuild environment only")]
        public static void Rebuild()
        {
            if (Application.isPlaying) throw new InvalidOperationException("Stop Play before rebuilding the workshop.");
            var carpet = GameObject.Find("CarpetRoot");
            var camera = Camera.main;
            if (carpet == null || camera == null) throw new InvalidOperationException("Build the main scene first.");
            FirstPersonSceneBuilder.Build(carpet,camera);
            var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            Debug.Log("[Workshop] Environment rebuilt; carpet, simulation and save assets preserved.");
        }

        public static void Build(Transform room, Camera camera)
        {
            var plaster=AssetDatabase.LoadAssetAtPath<Material>(Dir+"/Plaster.mat");
            var paint=AssetDatabase.LoadAssetAtPath<Material>(Dir+"/Paint.mat");
            ApplyFinish(plaster,"Limewash",false);
            ApplyFinish(paint,"Enamel",true);
            var oak=Material("CeilingTimber",new Color(.24f,.15f,.085f),.22f,0f);
            var trim=Material("DadoTrim",new Color(.14f,.23f,.20f),.38f,.08f);
            var stone=Material("SkirtingStone",new Color(.29f,.28f,.245f),.25f,0f);
            var cream=Material("CeilingIvory",new Color(.69f,.65f,.54f),.10f,0f);
            ApplyFinish(cream,"Limewash",false);
            var ceiling=room.Find("Ceiling").GetComponent<MeshRenderer>();
            ceiling.sharedMaterial=cream;
            var trimRoot=new GameObject("ArchitecturalFinish").transform; trimRoot.SetParent(room,false);
            for(int axis=0;axis<2;axis++) for(int side=-1;side<=1;side+=2)
            {
                Vector3 p=axis==0?new Vector3(side*5.956f,.07f,0):new Vector3(0,.07f,side*5.956f);
                Vector3 size=axis==0?new Vector3(.075f,.14f,12f):new Vector3(12f,.14f,.075f);
                Box(trimRoot,"StoneSkirting",p,size,stone);
                p.y=1.075f; size.y=.05f;
                Box(trimRoot,"DadoCap",p,size,trim);
                p.y=3.67f;size.y=.16f;
                Box(trimRoot,"Cornice",p,size,cream);
                p.y=3.57f;size.y=.025f;
                Box(trimRoot,"CorniceShadowLine",p,size,oak);
            }
            // Slender structural members organize the broad ceiling, without lowering the clear working volume.
            for(int i=0;i<4;i++)
            {
                float z=-4.9f+i*3.26f;
                Box(trimRoot,"CeilingBeam"+i,new Vector3(0,3.64f,z),new Vector3(11.9f,.23f,.13f),oak);
                for(int side=-1;side<=1;side+=2)
                    Box(trimRoot,"BeamBracket",new Vector3(side*5.84f,3.50f,z),new Vector3(.16f,.30f,.19f),trim);
            }
            // Real scale UVs on plaster and paint. Keep each wall face's broad mottling distinct from the small enamel grain.
            foreach(var mr in room.GetComponentsInChildren<MeshRenderer>())
            {
                if(mr.sharedMaterial!=plaster && mr.sharedMaterial!=paint && mr.sharedMaterial!=cream) continue;
                Vector3 s=mr.bounds.size;
                float horizontal=Mathf.Max(s.x,s.z),vertical=s.y>.3f?s.y:Mathf.Min(s.x,s.z);
                var block=new MaterialPropertyBlock(); mr.GetPropertyBlock(block);
                block.SetVector("_BaseMap_ST",new Vector4(horizontal*.55f,vertical*.55f,0,0));
                mr.SetPropertyBlock(block);
            }
            ConfigureLighting(room,camera);
            ConfigureFloor();
        }

        static void ConfigureLighting(Transform room,Camera camera)
        {
            var sun=GameObject.Find("Directional Light").GetComponent<Light>();
            sun.transform.rotation=Quaternion.Euler(38f,-30f,0f);
            sun.color=new Color(1f,.84f,.62f); sun.intensity=2.8f;
            sun.shadows=LightShadows.Soft; sun.shadowStrength=1f;
            sun.shadowBias=.025f;sun.shadowNormalBias=.15f;
            sun.shadowResolution=LightShadowResolution.VeryHigh;
            RenderSettings.sun=sun;
            RenderSettings.ambientMode=AmbientMode.Trilight;
            RenderSettings.ambientSkyColor=new Color(.46f,.49f,.53f);
            RenderSettings.ambientEquatorColor=new Color(.34f,.32f,.27f);
            RenderSettings.ambientGroundColor=new Color(.19f,.18f,.145f);
            RenderSettings.reflectionIntensity=.65f;
            RenderSettings.fog=false;
            var volume=GameObject.Find("Global Volume").GetComponent<Volume>();
            var profile=volume.sharedProfile;
            if(profile.TryGet<ColorAdjustments>(out var ca)) {ca.saturation.Override(7f);ca.postExposure.Override(0f);}
            if(profile.TryGet<Bloom>(out var bloom)) {bloom.threshold.Override(1.15f);bloom.intensity.Override(.18f);bloom.scatter.Override(.65f);}
            if(profile.TryGet<Vignette>(out var vig)) vig.intensity.Override(.16f);
            EditorUtility.SetDirty(profile);
            // Practical lighting is deliberately local: it reveals the working stations while the diagonal sun stays dominant.
            Spot(room,"ShelfWarmBounce",new Vector3(-3.5f,3.25f,4.35f),new Vector3(-3.5f,1.4f,5.55f),new Color(1f,.76f,.47f),3.2f,4.4f,100f);
            Spot(room,"SinkWarmBounce",new Vector3(2.8f,3.05f,4.5f),new Vector3(2.8f,.8f,5.5f),new Color(1f,.80f,.55f),3f,4f,100f);
            Spot(room,"BenchTaskLight",new Vector3(-4.65f,2.6f,-.1f),new Vector3(-4.5f,.9f,.5f),new Color(1f,.78f,.49f),3f,3.4f,90f);
            Spot(room,"SouthWindowBounce",new Vector3(-1.2f,2.8f,-5.68f),new Vector3(-1.5f,1f,-3.9f),new Color(1f,.88f,.67f),2f,4.7f,125f);
            Spot(room,"EastWindowBounce",new Vector3(5.7f,2.8f,1.8f),new Vector3(3.9f,1f,1.8f),new Color(1f,.88f,.67f),2f,4.7f,125f);
            // Six understated ceiling lamps give soft overhead bounce, matching the visible fixtures.
            for(int row=0;row<2;row++) for(int i=0;i<3;i++)
                Spot(room,"CeilingBounce"+row+"_"+i,new Vector3(-3.4f+i*3.4f,3.53f,-2.7f+row*5.4f),
                    new Vector3(-3.4f+i*3.4f,0,-2.7f+row*5.4f),new Color(1f,.88f,.67f),.65f,5f,120f);
            var probeGo=new GameObject("WorkshopReflection"); probeGo.transform.SetParent(room,false);probeGo.transform.localPosition=new Vector3(0,1.8f,0);
            var probe=probeGo.AddComponent<ReflectionProbe>(); probe.mode=ReflectionProbeMode.Realtime;
            probe.refreshMode=ReflectionProbeRefreshMode.OnAwake; probe.timeSlicingMode=ReflectionProbeTimeSlicingMode.AllFacesAtOnce;
            probe.resolution=128;probe.hdr=true;probe.boxProjection=true;probe.size=new Vector3(12,3.8f,12);probe.center=Vector3.zero;
            probe.nearClipPlane=.1f;probe.farClipPlane=20f;probe.intensity=.65f;probe.cullingMask=~(1<<2);
            probe.clearFlags=ReflectionProbeClearFlags.SolidColor; probe.backgroundColor=new Color(.25f,.23f,.19f);
            camera.allowHDR=true; camera.allowMSAA=true;
            var urp=GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset;
            if(urp!=null)
            {
                urp.shadowDistance=24f;urp.shadowCascadeCount=4;
                urp.cascade4Split=new Vector3(.10f,.26f,.52f);urp.supportsCameraDepthTexture=true;
                // Serialized resolution keeps this compatible with URP's read-only public resolution API.
                var so=new SerializedObject(urp);
                var resolution=so.FindProperty("m_MainLightShadowmapResolution");if(resolution!=null)resolution.intValue=4096;
                so.ApplyModifiedPropertiesWithoutUndo();EditorUtility.SetDirty(urp);
            }
            EditorUtility.SetDirty(sun);
        }

        static void ConfigureFloor()
        {
            var mat=AssetDatabase.LoadAssetAtPath<Material>("Assets/CarpetCleaning/Materials/FloorSurface.mat");
            if(mat==null)return;
            mat.SetColor("_TileColor",new Color(.67f,.65f,.57f));
            mat.SetColor("_GroutColor",new Color(.40f,.40f,.35f));
            mat.SetFloat("_CleanGloss",.61f);mat.SetFloat("_CleanSheen",.21f);
            EditorUtility.SetDirty(mat);
        }

        static void Spot(Transform room,string name,Vector3 p,Vector3 target,Color color,float power,float range,float cone)
        {
            var go=new GameObject(name);go.transform.SetParent(room,false);go.transform.localPosition=p;go.transform.LookAt(target);
            var light=go.AddComponent<Light>();light.type=LightType.Spot;light.color=color;light.intensity=power;light.range=range;
            light.spotAngle=cone;light.innerSpotAngle=cone*.55f;light.shadows=LightShadows.None;
        }

        static Material Material(string name,Color color,float smooth,float metal)
        {
            string path=Dir+"/"+name+".mat";
            var m=AssetDatabase.LoadAssetAtPath<Material>(path);
            if(m==null){m=new Material(Shader.Find("Universal Render Pipeline/Lit"));AssetDatabase.CreateAsset(m,path);}
            m.SetColor("_BaseColor",color);m.SetFloat("_Smoothness",smooth);m.SetFloat("_Metallic",metal);
            EditorUtility.SetDirty(m);return m;
        }

        static void ApplyFinish(Material mat,string name,bool fine)
        {
            string path=Dir+"/"+name+".asset";
            var tex=AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            if(tex==null)
            {
                const int n=256;
                tex=new Texture2D(n,n,TextureFormat.RGBA32,true,false){name=name,wrapMode=TextureWrapMode.Repeat,filterMode=FilterMode.Trilinear,anisoLevel=4};
                var colors=new Color[n*n];
                for(int y=0;y<n;y++)for(int x=0;x<n;x++)
                {
                    float u=x/(float)n,v=y/(float)n;
                    // Integer-period waves ensure genuinely seamless textures. Microscopic grain changes smoothness too.
                    float coarse=Mathf.Sin(u*Mathf.PI*6+Mathf.Sin(v*Mathf.PI*4))*.35f+Mathf.Cos(v*Mathf.PI*8+Mathf.Sin(u*Mathf.PI*2))*.25f;
                    float grain=Mathf.Sin(x*2.17f+y*5.37f)*Mathf.Sin(x*.793f-y*1.117f);
                    float a=fine?.96f+grain*.035f:.92f+coarse*.07f+grain*.025f;
                    colors[y*n+x]=new Color(a,a,a,.8f+grain*.1f);
                }
                tex.SetPixels(colors);tex.Apply(true,false);AssetDatabase.CreateAsset(tex,path);
            }
            mat.SetTexture("_BaseMap",tex);EditorUtility.SetDirty(mat);
        }

        static void Box(Transform root,string name,Vector3 p,Vector3 size,Material material)
        {
            var go=GameObject.CreatePrimitive(PrimitiveType.Cube);go.name=name;go.transform.SetParent(root,false);go.transform.localPosition=p;go.transform.localScale=size;
            go.GetComponent<MeshRenderer>().sharedMaterial=material;Object.DestroyImmediate(go.GetComponent<Collider>());
        }
    }
}


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
            ArchitecturalTrim(Group(root,"ArchitecturalDetails",Vector3.zero));

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
            Box(p,"SinkCounter",new Vector3(0,.91f,0),new Vector3(1.91f,.07f,.86f),oak,.014f);
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

        static void ArchitecturalTrim(Transform p)
        {
            // A consistent base, rail and crown breaks the four empty planes into human-scaled surfaces.
            for(int wall=0;wall<4;wall++)
            {
                var frame=Group(p,"WallTrim"+wall,Vector3.zero,wall*90);
                Box(frame,"Baseboard",new Vector3(0,.065f,5.965f),new Vector3(11.96f,.13f,.042f),iron,.005f);
                Box(frame,"WainscotCap",new Vector3(0,1.075f,5.955f),new Vector3(11.96f,.055f,.055f),oak,.008f);
                Box(frame,"CrownMoulding",new Vector3(0,3.71f,5.94f),new Vector3(11.96f,.12f,.09f),cream,.012f);
                for(int j=0;j<10;j++)
                {
                    float x=-5.45f+j*1.21f;
                    if(wall==0 && Mathf.Abs(x)<.8f) continue;
                    Box(frame,"WainscotBatten",new Vector3(x,.59f,5.963f),new Vector3(.035f,.90f,.025f),sage,.004f);
                }
            }
            // Ceiling structure casts long soft marks and makes the room feel built rather than boxed.
            for(int i=0;i<4;i++)
            {
                Box(p,"CeilingJoist",new Vector3(-4.5f+i*3,3.69f,0),new Vector3(.16f,.23f,11.99f),oak,.018f);
                for(int j=-1;j<=1;j+=2) Box(p,"JoistBracket",new Vector3(-4.5f+i*3,3.65f,j*5.83f),new Vector3(.24f,.29f,.21f),iron,.01f);
            }
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
                float w=Mathf.Pow(Mathf.Sin(t*Mathf.PI),.8f)*width;
                for(int col=-1;col<=1;col++) {vertices.Add(center+side*(col*w)+Vector3.up*(Mathf.Abs(col)*w*.22f));uv.Add(new Vector2(col*.5f+.5f,t));}
            }
            for(int row=0;row<8;row++)for(int col=0;col<2;col++)
            {int a=row*3+col,b=a+1,c=a+3,d=c+1;triangles.AddRange(new[]{a,c,b,b,c,d,b,c,a,d,c,b});}
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
            var go=Lathe(p,name,pos,profile,material,24);go.transform.localRotation=rotation;return go;
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
                if(!groups.TryGetValue(r.sharedMaterial,out var list)){list=new List<CombineInstance>();groups[r.sharedMaterial]=list;}
                list.Add(new CombineInstance{mesh=r.GetComponent<MeshFilter>().sharedMesh,transform=zone.worldToLocalMatrix*r.transform.localToWorldMatrix});
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


namespace CarpetCleaning
{
    /// <summary>Atmosphere assets and scene wiring. Invoked by the idempotent workshop builder.</summary>
    public static class WorkshopAtmosphereBuilder
    {
        const string Directory = "Assets/CarpetCleaning/Materials/Workshop";

        public static void Configure(Transform room)
        {
            Texture3D noise = DensityTexture();
            Material dustMaterial = DustMaterial();
            var shafts = new List<Renderer>();
            var motes = new List<ParticleSystem>();
            var foliage = new List<Transform>();
            var restRotations = new List<Quaternion>();
            foreach (var renderer in room.GetComponentsInChildren<MeshRenderer>())
            {
                Material material = renderer.sharedMaterial;
                if (material == null || material.shader == null
                    || material.shader.name != "CarpetCleaning/WindowLightVolume") continue;
                material.SetTexture("_DensityNoise", noise);
                material.SetColor("_Tint", new Color(1f, .86f, .62f, 1f));
                material.SetVector("_NoiseParams", new Vector4(.16f, .92f, .035f, 3.9f));
                material.SetFloat("_Anisotropy", .32f);
                material.SetFloat("_ShadowWeight", 1f);
                material.SetVector("_AtmosphereClock", Vector4.zero);
                var parameters = material.GetVector("_BeamParams");
                parameters.x = .042f;
                parameters.y = .065f;
                parameters.z = .18f;
                material.SetVector("_BeamParams", parameters);
                EditorUtility.SetDirty(material);
                renderer.shadowCastingMode = ShadowCastingMode.Off;
                shafts.Add(renderer);
                if (renderer.name.EndsWith("Beam1"))
                    Foliage(room, renderer, foliage, restRotations);

                var existing = room.Find(renderer.name + "Dust");
                if (existing != null) Object.DestroyImmediate(existing.gameObject);
                var go = new GameObject(renderer.name + "Dust");
                go.transform.SetParent(room, false);
                go.layer = 2;
                var system = go.AddComponent<ParticleSystem>();
                system.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                var main = system.main;
                main.playOnAwake = false;
                main.loop = false;
                main.maxParticles = 32;
                main.simulationSpace = ParticleSystemSimulationSpace.World;
                main.simulationSpeed = 0f;
                main.startSpeed = 0f;
                main.startLifetime = 100f;
                var emission = system.emission; emission.enabled = false;
                var shape = system.shape; shape.enabled = false;
                var particleRenderer = system.GetComponent<ParticleSystemRenderer>();
                particleRenderer.sharedMaterial = dustMaterial;
                particleRenderer.renderMode = ParticleSystemRenderMode.Billboard;
                particleRenderer.shadowCastingMode = ShadowCastingMode.Off;
                particleRenderer.receiveShadows = false;
                particleRenderer.lightProbeUsage = LightProbeUsage.Off;
                particleRenderer.reflectionProbeUsage = ReflectionProbeUsage.Off;
                particleRenderer.minParticleSize = 0f;
                particleRenderer.maxParticleSize = .018f;
                motes.Add(system);
            }
            var atmosphere = room.GetComponent<WorkshopAtmosphere>();
            if (atmosphere == null) atmosphere = room.gameObject.AddComponent<WorkshopAtmosphere>();
            atmosphere.shafts = shafts.ToArray();
            atmosphere.motes = motes.ToArray();
            atmosphere.foliage = foliage.ToArray();
            atmosphere.foliageRestRotations = restRotations.ToArray();
            EditorUtility.SetDirty(atmosphere);
        }

        static void Foliage(Transform room, Renderer shaft, List<Transform> foliage, List<Quaternion> rotations)
        {
            const string materialPath = Directory + "/AtmosphereFoliage.mat";
            Material material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
            if (material == null)
            {
                material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                material.SetColor("_BaseColor", new Color(.09f, .15f, .07f));
                AssetDatabase.CreateAsset(material, materialPath);
            }
            Vector3 origin = shaft.sharedMaterial.GetVector("_WindowOrigin");
            Vector3 direction = shaft.sharedMaterial.GetVector("_BeamDirection");
            Vector3 right = shaft.sharedMaterial.GetVector("_WindowRight");
            Vector3 up = shaft.sharedMaterial.GetVector("_WindowUp");
            for (int layer = 0; layer < 2; layer++)
            {
                string name = shaft.name + "ExteriorFoliage" + layer;
                var old = room.Find(name);
                if (old != null) Object.DestroyImmediate(old.gameObject);
                var go = new GameObject(name);
                go.transform.SetParent(room, false);
                go.transform.position = origin - direction * (1.1f + layer * .37f)
                    + up * (.16f + layer * .18f) + right * (layer == 0 ? -.3f : .45f);
                go.transform.rotation = Quaternion.LookRotation(Vector3.Cross(right, up), up)
                    * Quaternion.Euler(0f, 0f, layer == 0 ? -12f : 158f);
                go.AddComponent<MeshFilter>().sharedMesh = FoliageMesh(layer);
                var renderer = go.AddComponent<MeshRenderer>();
                renderer.sharedMaterial = material;
                renderer.shadowCastingMode = ShadowCastingMode.ShadowsOnly;
                renderer.receiveShadows = false;
                renderer.lightProbeUsage = LightProbeUsage.Off;
                renderer.reflectionProbeUsage = ReflectionProbeUsage.Off;
                foliage.Add(go.transform);
                rotations.Add(go.transform.localRotation);
            }
        }

        static Mesh FoliageMesh(int variant)
        {
            string path = Directory + "/AtmosphereFoliage" + variant + ".asset";
            var existing = AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if (existing != null) return existing;
            var vertices = new List<Vector3>();
            var triangles = new List<int>();
            // A sparse branch silhouette: one combined, double-sided mesh per moving layer.
            for (int branch = 0; branch < 4; branch++)
            {
                Vector3 start = new Vector3(-1.7f + branch * .7f, -.13f + branch * .04f, 0f);
                Vector3 end = start + new Vector3(.8f, .4f + .11f * (branch % 2), .02f);
                Vector3 tangent = (end - start).normalized;
                Vector3 side = new Vector3(-tangent.y, tangent.x, 0f);
                Quad(vertices, triangles, start - side * .012f, end - side * .004f,
                    end + side * .004f, start + side * .012f);
                for (int leaf = 0; leaf < 7; leaf++)
                {
                    float t = .15f + leaf * .12f;
                    float sign = ((leaf + variant) & 1) == 0 ? 1f : -1f;
                    Vector3 stem = Vector3.Lerp(start, end, t);
                    Vector3 axis = (tangent * .5f + side * sign).normalized;
                    Vector3 leafSide = new Vector3(-axis.y, axis.x, 0f);
                    float length = .23f + .04f * ((branch + leaf + variant) % 3);
                    float width = .055f + .009f * (leaf % 3);
                    int first = vertices.Count;
                    vertices.Add(stem + axis * length * .47f);
                    const int segments = 8;
                    for (int i = 0; i < segments; i++)
                    {
                        float angle = i * Mathf.PI * 2f / segments;
                        float along = (Mathf.Cos(angle) + 1f) * .5f;
                        Vector3 p = stem + axis * (along * length)
                            + leafSide * (Mathf.Sin(angle) * width);
                        p.z += Mathf.Sin(angle) * .018f;
                        vertices.Add(p);
                    }
                    for (int i = 0; i < segments; i++)
                        DoubleTriangle(triangles, first, first + 1 + i, first + 1 + (i + 1) % segments);
                }
            }
            var mesh = new Mesh { name = "AtmosphereFoliage" + variant };
            mesh.SetVertices(vertices);
            mesh.SetTriangles(triangles, 0);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            AssetDatabase.CreateAsset(mesh, path);
            return mesh;
        }

        static void Quad(List<Vector3> vertices, List<int> triangles, Vector3 a, Vector3 b, Vector3 c, Vector3 d)
        {
            int first = vertices.Count;
            vertices.Add(a); vertices.Add(b); vertices.Add(c); vertices.Add(d);
            DoubleTriangle(triangles, first, first + 1, first + 2);
            DoubleTriangle(triangles, first, first + 2, first + 3);
        }

        static void DoubleTriangle(List<int> triangles, int a, int b, int c)
        {
            triangles.Add(a); triangles.Add(b); triangles.Add(c);
            triangles.Add(c); triangles.Add(b); triangles.Add(a);
        }

        static Material DustMaterial()
        {
            const string texturePath = Directory + "/AtmosphereMote.asset";
            Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath);
            if (texture == null)
            {
                const int size = 32;
                texture = new Texture2D(size, size, TextureFormat.RGBA32, false, true);
                texture.name = "AtmosphereMote";
                texture.wrapMode = TextureWrapMode.Clamp;
                var pixels = new Color32[size * size];
                for (int y = 0; y < size; y++) for (int x = 0; x < size; x++)
                {
                    float u = ((x + .5f) / size - .5f) * 2f;
                    float v = ((y + .5f) / size - .5f) * 2f;
                    float radius = u * u + v * v;
                    float alpha = Mathf.Exp(-radius * 5f) * Mathf.Clamp01((1f - radius) * 3f);
                    pixels[y * size + x] = new Color(1f, 1f, 1f, alpha);
                }
                texture.SetPixels32(pixels);
                texture.Apply(false, true);
                AssetDatabase.CreateAsset(texture, texturePath);
            }
            const string path = Directory + "/AtmosphereDust.mat";
            Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
            Shader shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
            if (material == null)
            {
                material = new Material(shader);
                AssetDatabase.CreateAsset(material, path);
            }
            material.shader = shader;
            material.SetTexture("_BaseMap", texture);
            material.SetColor("_BaseColor", new Color(1.7f, 1.5f, 1.15f, 1f));
            material.SetFloat("_Surface", 1f);
            material.SetFloat("_Blend", 0f);
            material.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
            material.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
            material.SetFloat("_SrcBlendAlpha", (float)BlendMode.One);
            material.SetFloat("_DstBlendAlpha", (float)BlendMode.OneMinusSrcAlpha);
            material.SetFloat("_ZWrite", 0f);
            material.SetFloat("_Cull", (float)CullMode.Off);
            material.SetFloat("_SoftParticlesEnabled", 1f);
            material.SetVector("_SoftParticleFadeParams", new Vector4(0f, 6.67f, 0f, 0f));
            material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            material.EnableKeyword("_SOFTPARTICLES_ON");
            material.renderQueue = 3030;
            material.SetOverrideTag("RenderType", "Transparent");
            EditorUtility.SetDirty(material);
            return material;
        }

        static Texture3D DensityTexture()
        {
            const string path = Directory + "/AtmosphereDensity.asset";
            var existing = AssetDatabase.LoadAssetAtPath<Texture3D>(path);
            if (existing != null) return existing;
            const int size = 48;
            var values = new byte[size * size * size];
            for (int z = 0; z < size; z++) for (int y = 0; y < size; y++) for (int x = 0; x < size; x++)
            {
                Vector3 p = new Vector3(x, y, z) / size;
                float n = Noise(p * 4f, 4) * .75f + Noise(p * 8f, 8) * .25f;
                // Stretch softly around the mean: shafts retain organic pockets after ray integration.
                n = Mathf.Clamp01((n - .5f) * 1.55f + .5f);
                values[x + size * (y + size * z)] = (byte)Mathf.RoundToInt(n * 255f);
            }
            var texture = new Texture3D(size, size, size, TextureFormat.R8, false)
            {
                name = "AtmosphereDensity", wrapMode = TextureWrapMode.Repeat,
                filterMode = FilterMode.Bilinear
            };
            texture.SetPixelData(values, 0);
            texture.Apply(false, true);
            AssetDatabase.CreateAsset(texture, path);
            return texture;
        }

        static float Noise(Vector3 p, int period)
        {
            int x = Mathf.FloorToInt(p.x), y = Mathf.FloorToInt(p.y), z = Mathf.FloorToInt(p.z);
            Vector3 f = new Vector3(p.x - x, p.y - y, p.z - z);
            f = new Vector3(Smooth(f.x), Smooth(f.y), Smooth(f.z));
            float value = 0f;
            for (int i = 0; i < 8; i++)
            {
                int dx = i & 1, dy = (i >> 1) & 1, dz = (i >> 2) & 1;
                uint h = unchecked((uint)((x + dx) % period) * 1597334677u
                    ^ (uint)((y + dy) % period) * 3812015801u ^ (uint)((z + dz) % period) * 2798796415u);
                h = unchecked((h ^ (h >> 16)) * 2246822519u);
                h ^= h >> 13;
                float n = (h & 0x00ffffffu) / 16777216f;
                value += n * (dx == 0 ? 1f - f.x : f.x)
                    * (dy == 0 ? 1f - f.y : f.y) * (dz == 0 ? 1f - f.z : f.z);
            }
            return value;
        }

        static float Smooth(float t) => t * t * t * (t * (t * 6f - 15f) + 10f);
    }
}


namespace CarpetCleaning
{
    /// <summary>Reproducible atmospheric motion, with dust restricted to the physical window portals.</summary>
    [DefaultExecutionOrder(190)]
    public sealed class WorkshopAtmosphere : MonoBehaviour
    {
        public Renderer[] shafts;
        public ParticleSystem[] motes;
        public Transform[] foliage;
        public Quaternion[] foliageRestRotations;
        [Min(0f)] public float motionSpeed = 1f;
        [Range(0f, 1f)] public float moteOpacity = .27f;
        const int MotesPerShaft = 32;
        static readonly int ClockId = Shader.PropertyToID("_AtmosphereClock");
        MaterialPropertyBlock block;
        ParticleSystem.Particle[][] particles;
        float previewTime = -1f;

        void Awake()
        {
            // No domain or scene reload in this project: recreate all private runtime caches.
            block = new MaterialPropertyBlock();
            particles = null;
            previewTime = -1f;
            if (motes != null) foreach (var system in motes)
            {
                if (system == null) continue;
                system.Play(false);
                system.Pause(false);
            }
            Apply(0f);
        }

        void LateUpdate() => Apply(previewTime >= 0f ? previewTime : Time.time * motionSpeed);

        public void SetPreviewTime(float seconds)
        {
            previewTime = Mathf.Max(0f, seconds);
            Apply(previewTime);
        }

        public void ResumeTime() => previewTime = -1f;

        void Apply(float seconds)
        {
            if (foliage != null && foliageRestRotations != null)
                for (int i = 0; i < Mathf.Min(foliage.Length, foliageRestRotations.Length); i++)
                {
                    if (foliage[i] == null) continue;
                    float sign = (i & 1) == 0 ? 1f : -1f;
                    float phase = seconds * .17f + i * 1.91f;
                    foliage[i].localRotation = foliageRestRotations[i] * Quaternion.Euler(
                        Mathf.Sin(phase * .71f) * 2.2f, Mathf.Sin(phase * .83f) * 3f,
                        sign * Mathf.Sin(phase) * 5.5f);
                }
            if (shafts == null) return;
            if (block == null) block = new MaterialPropertyBlock();
            if (particles == null || particles.Length != shafts.Length)
                particles = new ParticleSystem.Particle[shafts.Length][];
            for (int s = 0; s < shafts.Length; s++)
            {
                Renderer shaft = shafts[s];
                if (shaft == null || shaft.sharedMaterial == null) continue;
                shaft.GetPropertyBlock(block);
                block.SetVector(ClockId, new Vector4(seconds, 1f, 0f, 0f));
                shaft.SetPropertyBlock(block);
                if (motes == null || s >= motes.Length || motes[s] == null) continue;
                if (particles[s] == null) particles[s] = new ParticleSystem.Particle[MotesPerShaft];
                Material material = shaft.sharedMaterial;
                Vector3 origin = material.GetVector("_WindowOrigin");
                Vector4 right = material.GetVector("_WindowRight");
                Vector4 up = material.GetVector("_WindowUp");
                Vector4 direction = material.GetVector("_BeamDirection");
                for (int i = 0; i < MotesPerShaft; i++)
                {
                    int seed = 31 + s * 53 + i * 7;
                    float travel = Mathf.Repeat(Random01(seed) + seconds * .004f, 1f);
                    float x = (Random01(seed + 1) * 2f - 1f) * right.w * .84f;
                    float y = (Random01(seed + 2) * 2f - 1f) * up.w * .84f;
                    // Keep the particles out of the mullion's unlit strip.
                    y = Mathf.Sign(y) * Mathf.Max(Mathf.Abs(y), .085f);
                    Vector3 position = origin + (Vector3)right * x + (Vector3)up * y
                        + (Vector3)direction * (travel * direction.w);
                    position += new Vector3(Mathf.Sin(seconds * .23f + seed),
                        Mathf.Sin(seconds * .17f + seed * .8f), Mathf.Cos(seconds * .19f + seed)) * .016f;
                    float fade = Mathf.SmoothStep(0f, 1f, travel * 8f)
                        * Mathf.SmoothStep(0f, 1f, (1f - travel) * 8f)
                        * Mathf.SmoothStep(0f, 1f, (position.y - .03f) * 4f);
                    var particle = new ParticleSystem.Particle();
                    particle.position = position;
                    particle.startSize = Mathf.Lerp(.004f, .012f, Random01(seed + 3));
                    particle.startColor = new Color(1f, .91f, .72f, fade * moteOpacity);
                    particle.startLifetime = 100f;
                    particle.remainingLifetime = 100f;
                    particle.randomSeed = (uint)seed;
                    particles[s][i] = particle;
                }
                motes[s].SetParticles(particles[s], MotesPerShaft);
            }
        }

        void OnDisable()
        {
            if (shafts == null) return;
            if (block == null) block = new MaterialPropertyBlock();
            foreach (var shaft in shafts)
            {
                if (shaft == null) continue;
                shaft.GetPropertyBlock(block);
                block.SetVector(ClockId, Vector4.zero);
                shaft.SetPropertyBlock(block);
            }
            if (motes != null) foreach (var system in motes)
                if (system != null) system.Clear();
            if (foliage != null && foliageRestRotations != null)
                for (int i = 0; i < Mathf.Min(foliage.Length, foliageRestRotations.Length); i++)
                    if (foliage[i] != null) foliage[i].localRotation = foliageRestRotations[i];
        }

        static float Random01(int seed)
        {
            uint h = unchecked((uint)seed * 747796405u + 2891336453u);
            h = ((h >> (int)((h >> 28) + 4)) ^ h) * 277803737u;
            h = (h >> 22) ^ h;
            return (h & 0x00ffffffu) / 16777216f;
        }
    }
}

