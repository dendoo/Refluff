using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using Object = UnityEngine.Object;

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
            for(int i=0;i<3;i++)
            {
                float z=-4.65f+i*4.65f;
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
            PlaceSpareExtractor(room);
            ConfigureTerminal();
        }

        static void ConfigureLighting(Transform room,Camera camera)
        {
            var sun=GameObject.Find("Directional Light").GetComponent<Light>();
            sun.transform.rotation=Quaternion.Euler(38f,-30f,0f);
            sun.color=new Color(1f,.84f,.62f); sun.intensity=2.8f;
            sun.shadows=LightShadows.Soft; sun.shadowStrength=1f;
            sun.shadowBias=.025f;sun.shadowNormalBias=.15f;
            RenderSettings.sun=sun;
            RenderSettings.ambientMode=AmbientMode.Trilight;
            RenderSettings.ambientSkyColor=new Color(.68f,.71f,.73f);
            RenderSettings.ambientEquatorColor=new Color(.60f,.57f,.50f);
            RenderSettings.ambientGroundColor=new Color(.43f,.40f,.34f);
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
            for(int x=-1;x<=1;x+=2) for(int z=-1;z<=1;z+=2)
            {
                var bounce=new GameObject("IndirectFill"+x+"_"+z);bounce.transform.SetParent(room,false);
                bounce.transform.localPosition=new Vector3(x*3.2f,2.75f,z*3.15f);
                var light=bounce.AddComponent<Light>();light.type=LightType.Point;
                light.color=new Color(1f,.94f,.82f);light.intensity=2.2f;light.range=8f;light.shadows=LightShadows.None;
            }
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
            mat.SetColor("_GroutColor",new Color(.55f,.53f,.47f));
            mat.SetFloat("_CleanGloss",.61f);mat.SetFloat("_CleanSheen",.21f);
            EditorUtility.SetDirty(mat);
        }

        static void PlaceSpareExtractor(Transform room)
        {
            const string path="Assets/CarpetCleaning/Models/Workshop/extractor.glb";
            var source=AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if(source==null)return; // Optional only while authoring; the shipped scene has the imported asset reference.
            var root=new GameObject("SpareExtractor").transform;root.SetParent(room,false);
            var model=(GameObject)PrefabUtility.InstantiatePrefab(source);model.transform.SetParent(root,false);
            var renderers=model.GetComponentsInChildren<Renderer>();
            if(renderers.Length==0)throw new InvalidOperationException("Extractor has no renderer.");
            Bounds bounds=renderers[0].bounds;foreach(var r in renderers)bounds.Encapsulate(r.bounds);
            float scale=1.05f/Mathf.Max(bounds.size.y,.001f);
            model.transform.localScale*=scale;
            model.transform.localPosition=new Vector3(-bounds.center.x,-bounds.min.y,-bounds.center.z)*scale;
            foreach(var r in renderers){r.shadowCastingMode=ShadowCastingMode.On;r.receiveShadows=true;r.reflectionProbeUsage=ReflectionProbeUsage.BlendProbes;}
            var body=root.gameObject.AddComponent<BoxCollider>();body.center=new Vector3(0,.525f,0);body.size=new Vector3(bounds.size.x*scale,1.05f,bounds.size.z*scale);
            root.localPosition=new Vector3(4.62f,-.003f,3.25f);root.localRotation=Quaternion.Euler(0,-145f,0);
        }

        static void ConfigureTerminal()
        {
            const string path=Dir+"/UpgradeDisplay.asset";
            var texture=AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            if(texture==null)
            {
                const int n=256;
                texture=new Texture2D(n,n,TextureFormat.RGBA32,true,false){name="UpgradeDisplay",wrapMode=TextureWrapMode.Clamp,filterMode=FilterMode.Trilinear};
                var pixels=new Color[n*n];
                for(int y=0;y<n;y++)for(int x=0;x<n;x++)
                {
                    float u=(x+.5f)/n,v=(y+.5f)/n;
                    Color c=new Color(.035f,.095f,.09f);
                    float d=Vector2.Distance(new Vector2(u,v),new Vector2(.5f,.59f));
                    bool ring=Mathf.Abs(d-.20f)<.006f;
                    bool stem=Mathf.Abs(u-.5f)<.025f && v>.48f && v<.68f;
                    bool head=v>.60f && v<.71f && Mathf.Abs(Mathf.Abs(u-.5f)-( .71f-v))<.022f;
                    bool frame=(u<.025f || u>.975f || v<.025f || v>.975f);
                    if(frame)c=new Color(.08f,.25f,.22f);
                    if(v>.86f && v<.90f && u>.13f && u<.72f)c=new Color(.30f,.48f,.43f);
                    for(int i=0;i<3;i++) if(Mathf.Abs(v-(.16f+i*.065f))<.012f && u>.15f && u<.85f)
                        c=u<.39f+i*.16f?new Color(.55f,.78f,.59f):new Color(.11f,.22f,.19f);
                    if(ring || stem || head)c=new Color(.71f,.91f,.66f);
                    pixels[y*n+x]=c;
                }
                texture.SetPixels(pixels);texture.Apply(true,false);AssetDatabase.CreateAsset(texture,path);
            }
            var screen=AssetDatabase.LoadAssetAtPath<Material>(Dir+"/Daylight.mat");
            screen.SetColor("_BaseColor",Color.white);screen.SetTexture("_BaseMap",texture);
            screen.SetTexture("_EmissionMap",texture);screen.SetColor("_EmissionColor",Color.white*.75f);
            screen.EnableKeyword("_EMISSION");screen.globalIlluminationFlags=MaterialGlobalIlluminationFlags.RealtimeDirectEmission;
            EditorUtility.SetDirty(screen);
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
            string path=Dir+"/"+name+"_v2.asset";
            var tex=AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            if(tex==null || tex.name!=name+"_v2")
            {
                const int n=256;
                bool isNew=tex==null;
                if(isNew)tex=new Texture2D(n,n,TextureFormat.RGBA32,true,false){wrapMode=TextureWrapMode.Repeat,filterMode=FilterMode.Trilinear,anisoLevel=4};
                tex.name=name+"_v2";
                var colors=new Color[n*n];
                for(int y=0;y<n;y++)for(int x=0;x<n;x++)
                {
                    float u=x/(float)n,v=y/(float)n;
                    // Integer-period waves ensure genuinely seamless textures. Microscopic grain changes smoothness too.
                    float coarse=Mathf.Sin(u*Mathf.PI*6+Mathf.Sin(v*Mathf.PI*4))*.35f+Mathf.Cos(v*Mathf.PI*8+Mathf.Sin(u*Mathf.PI*2))*.25f;
                    float grain=Mathf.Sin(x*2.17f+y*5.37f)*Mathf.Sin(x*.793f-y*1.117f);
                    float a=fine?.97f+grain*.012f:.94f+coarse*.024f+grain*.007f;
                    colors[y*n+x]=new Color(a,a,a,.8f+grain*.1f);
                }
                tex.SetPixels(colors);tex.Apply(true,false);if(isNew)AssetDatabase.CreateAsset(tex,path);else EditorUtility.SetDirty(tex);
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
