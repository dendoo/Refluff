using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

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
            var plaster = Mat("Plaster", new Color(.87f,.83f,.74f), 0, .12f);
            var teal = Mat("Paint", new Color(.37f,.48f,.41f), .05f, .29f);
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
