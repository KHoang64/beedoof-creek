using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace BeaverCreek.Editor
{
    public static class BeaverCreekBuilder
    {
        public const string Root = "Assets/BeaverCreek";
        const string BK = "Assets/BK/PureNature_FantasyForest/Prefabs/";
        const string Ducks = "Assets/Patchmesh/Country Critters - Stylized Hand-Painted Ducks/";
        static System.Random random;
        static readonly Dictionary<Material,Material> materialCopies = new Dictionary<Material,Material>();
        static float R(float min,float max) => Mathf.Lerp(min,max,(float)random.NextDouble());
        static T Load<T>(string path) where T:Object
        {
            var asset=AssetDatabase.LoadAssetAtPath<T>(path);
            if(!asset) throw new InvalidOperationException("Missing required asset: "+path);
            return asset;
        }
        static T Save<T>(T asset,string path) where T:Object
        {
            var existing=AssetDatabase.LoadAssetAtPath<T>(path);
            if(existing) { EditorUtility.CopySerialized(asset,existing); Object.DestroyImmediate(asset); EditorUtility.SetDirty(existing); AssetDatabase.SaveAssetIfDirty(existing); return existing; }
            AssetDatabase.CreateAsset(asset,path); return asset;
        }
        static GameObject Group(string name,Transform parent=null)
        {
            var obj=new GameObject(name); if(parent) obj.transform.SetParent(parent,false); return obj;
        }
        static float Radius(float x,float z) => Mathf.Sqrt(x*x/(34*34)+(z-18)*(z-18)/(29*29));
        public static float Ground(float x,float z)
        {
            float r=Radius(x,z)+(Mathf.PerlinNoise(x*.065f+8,z*.065f+3)-.5f)*.075f;
            float bank=Mathf.SmoothStep(0,1,Mathf.InverseLerp(.82f,1.035f,r));
            return Mathf.Lerp(-1.7f,.43f,bank)+Mathf.Max(0,r-1.05f)*2.3f
                +Mathf.Max(0,x-24)*.09f*bank + (Mathf.PerlinNoise(x*.13f+6,z*.13f+8)-.5f)*.24f*bank;
        }
        static Vector3 Land(float x,float z) => new Vector3(x,Ground(x,z),z);
        static Material Lit(string name,Color color,float smoothness=0)
        {
            var m=new Material(Shader.Find("Universal Render Pipeline/Lit")); m.name=name;
            m.SetColor("_BaseColor",color); m.SetFloat("_Smoothness",smoothness); m.enableInstancing=true;
            return Save(m,Root+"/Materials/"+name+".mat");
        }
        static void Set(Material m,string name,Color color) { if(m.HasProperty(name)) m.SetColor(name,color); }
        static void Set(Material m,string name,float value) { if(m.HasProperty(name)) m.SetFloat(name,value); }
        static Material LocalMaterial(Material source)
        {
            if(materialCopies.TryGetValue(source,out var result)) return result;
            var m=new Material(source); m.name=source.name+"_Creek"; m.enableInstancing=true;
            string shader=m.shader.name;
            if(shader.StartsWith("BK/") && !shader.Contains("Water"))
            {
                Set(m,"_ScatteringColor",new Color(.57f,.65f,.32f));
                Set(m,"_GradientColor",new Color(.38f,.58f,.18f));
                Set(m,"_ColorVariation",new Color(.25f,.46f,.19f));
                Set(m,"_MainColor",new Color(.23f,.42f,.19f));
                Set(m,"_Smoothness",.56f); Set(m,"_SmoothnessPower",.67f);Set(m,"_LayerSmoothnessPower",.43f);
                if(shader.Contains("Trunk")) {Set(m,"_Color",new Color(.66f,.69f,.62f));Set(m,"_2ndColor",new Color(.74f,.88f,.60f));}
                if(shader=="BK/Grass")
                {
                    Set(m,"_Color01",new Color(.23f,.48f,.11f));Set(m,"_Color02",new Color(.16f,.38f,.18f));
                }
            }
            if(source.name.Contains("PineLeaves"))
            {
                Set(m,"_Main_Color",new Color(.29f,.47f,.23f));
                Set(m,"_Second_Color",new Color(.10f,.26f,.17f));
                Set(m,"_SSS_Color",new Color(.30f,.38f,.18f));
                Set(m,"_Gradient_Color",new Color(.15f,.23f,.15f));
                Set(m,"_SSS_Brightness",.35f);Set(m,"_Hue_Variation",.08f);
                Set(m,"_Smoothness",.48f);
            }
            if(source.name.Contains("PineBark")) {Set(m,"_Tint_Color",new Color(.62f,.65f,.59f));Set(m,"_Smoothness_Multiplier",.65f);Set(m,"_Moss_Smoothness",.4f);}
            if(source.name=="DeadLeaves") {Set(m,"_BaseColor",new Color(.48f,.51f,.40f));Set(m,"_Smoothness",.58f);}
            result=Save(m,Root+"/Materials/"+m.name+".mat"); materialCopies[source]=result; return result;
        }
        static GameObject Place(string path,Transform parent,Vector3 position,float scale,float yaw,bool localMaterials=true)
        {
            var obj=(GameObject)PrefabUtility.InstantiatePrefab(Load<GameObject>(path));
            obj.transform.SetParent(parent,false); obj.transform.position=position;
            obj.transform.localScale *= scale; obj.transform.rotation=Quaternion.Euler(0,yaw,0)*obj.transform.rotation;
            if(localMaterials) foreach(var renderer in obj.GetComponentsInChildren<Renderer>(true))
            {
                renderer.sharedMaterials=renderer.sharedMaterials.Select(m=>m ? LocalMaterial(m):null).ToArray();
            }
            return obj;
        }
        static void Terrain(Transform parent)
        {
            const int res=513; const float size=180;
            string dataPath=Root+"/Generated/CreekTerrain.asset";
            var data=AssetDatabase.LoadAssetAtPath<TerrainData>(dataPath);
            if(!data) { data=new TerrainData();AssetDatabase.CreateAsset(data,dataPath); }
            data.heightmapResolution=res;data.size=new Vector3(size,30,size);data.alphamapResolution=512;data.baseMapResolution=1024;
            var heights=new float[res,res];
            for(int z=0;z<res;z++) for(int x=0;x<res;x++) heights[z,x]=(Ground(x*size/(res-1)-90,z*size/(res-1)-65)+5)/30;
            data.SetHeights(0,0,heights);
            string layerRoot="Assets/BK/PureNature_FantasyForest/Textures/Surfaces/TerrainLayers/";
            var layers=new List<TerrainLayer>();
            foreach(string name in new[]{"Grass01","Mud02","Moss01"})
            {
                var layer=Object.Instantiate(Load<TerrainLayer>(layerRoot+name+".terrainlayer"));
                layer.tileSize=new Vector2(5,5); layer.smoothness=name=="Mud02"?.73f:.27f;
                layer.diffuseRemapMax=name=="Mud02"?new Vector4(.55f,.56f,.48f,1):new Vector4(.72f,.85f,.60f,1);
                layer.maskMapRemapMin=new Vector4(0,.65f,0,name=="Mud02"?.48f:.15f);
                layer.maskMapRemapMax=new Vector4(0,1,1,name=="Mud02"?.83f:.4f);
                layers.Add(Save(layer,Root+"/Materials/"+name+".terrainlayer"));
            }
            data.terrainLayers=layers.ToArray();
            var splat=new float[512,512,3];
            for(int z=0;z<512;z++) for(int x=0;x<512;x++)
            {
                float px=x*size/511-90,pz=z*size/511-65,r=Radius(px,pz);
                float path=Mathf.Exp(-Mathf.Pow((px-(14+.27f*(pz+20)))/2.2f,2));
                if(pz>11) path*=Mathf.Clamp01((24-pz)/13);
                float mud=Mathf.Max(path,Mathf.Clamp01((1.055f-r)*12));
                float moss=(1-mud)*(.28f+.23f*Mathf.PerlinNoise(px*.14f+30,pz*.14f+20));
                splat[z,x,0]=1-mud-moss;splat[z,x,1]=mud;splat[z,x,2]=moss;
            }
            data.SetAlphamaps(0,0,splat);EditorUtility.SetDirty(data);AssetDatabase.SaveAssetIfDirty(data);
            var obj=UnityEngine.Terrain.CreateTerrainGameObject(data);obj.name="Sculpted pond basin and woodland path";
            obj.transform.SetParent(parent);obj.transform.position=new Vector3(-90,-5,-65);
            var terrain=obj.GetComponent<UnityEngine.Terrain>();terrain.heightmapPixelError=7;terrain.basemapDistance=160;
            terrain.materialTemplate=Save(new Material(Shader.Find("Universal Render Pipeline/Terrain/Lit")),Root+"/Materials/CreekTerrain.mat");
        }
        static void Pond(Transform parent)
        {
            var water=Place(BK+"Water/Water.prefab",parent,new Vector3(0,0,18),10,0,false);
            var wm=new Material(water.GetComponent<Renderer>().sharedMaterial);
            wm.name="CreekStillWater";
            Set(wm,"_NormalPower",.09f);Set(wm,"_NormalScale",6f);Set(wm,"_NormalSpeed",.12f);
            Set(wm,"_WavesHeight",.013f);Set(wm,"_WavesSpeed",.18f);Set(wm,"_RefractionPower",.8f);
            Set(wm,"_ShallowColor",new Color(.10f,.13f,.07f,.25f));Set(wm,"_DepthColor",new Color(.035f,.052f,.028f,1));
            Set(wm,"_CausticsColor",Color.black);Set(wm,"_SmoothnessPower",.97f);
            water.GetComponent<Renderer>().sharedMaterial=Save(wm,Root+"/Materials/CreekStillWater.mat");
            var mossAsset=Load<GameObject>(BK+"Water/WaterMoss.prefab");
            var moss=new Material(mossAsset.GetComponent<Renderer>().sharedMaterial);moss.name="CreekWaterMoss";
            Set(moss,"_BaseColor",new Color(.63f,.78f,.37f,1));Set(moss,"_Smoothness",.28f);Set(moss,"_Cutoff",.30f);
            moss=Save(moss,Root+"/Materials/CreekWaterMoss.mat");
            int n=0;
            for(float z=-10;z<50;z+=5.2f) for(float x=-36;x<38;x+=5.4f)
            {
                float px=x+R(-2,2),pz=z+R(-2,2);
                if(Radius(px,pz)>1.03f) continue;
                float cove=px*px/(19*19)+(pz+3)*(pz+3)/(10*10);
                if(cove<1.0f) continue;
                var patch=Place(BK+"Water/WaterMoss.prefab",parent,new Vector3(px,.035f+(n++%11)*.0015f,pz),1,0,false);
                patch.name="BK WaterMoss • "+n;patch.transform.rotation=Quaternion.Euler(90,R(0,360),0);
                patch.transform.localScale=new Vector3(R(10,15),R(9,14),1);
                patch.GetComponent<Renderer>().sharedMaterial=moss;
                patch.GetComponent<Renderer>().shadowCastingMode=ShadowCastingMode.Off;
            }
            // Small ragged islands soften the edge of the open duck cove.
            for(int i=0;i<24;i++)
            {
                float a=R(0,Mathf.PI*2),px=Mathf.Cos(a)*R(16,21),pz=-3+Mathf.Sin(a)*R(8,11);
                if(Radius(px,pz)>.98f) continue;
                var patch=Place(BK+"Water/WaterMoss.prefab",parent,new Vector3(px,.07f+i*.0002f,pz),1,0,false);
                patch.transform.rotation=Quaternion.Euler(90,R(0,360),0);patch.transform.localScale=new Vector3(R(2,5),R(2,5),1);
                patch.GetComponent<Renderer>().sharedMaterial=moss;patch.GetComponent<Renderer>().shadowCastingMode=ShadowCastingMode.Off;
            }
        }
        static void Forest(Transform parent)
        {
            var trees=Group("Layered far-bank pines • SoStylized",parent).transform;
            for(int row=0;row<5;row++) for(int i=0;i<28;i++)
            {
                float x=-78+i*5.8f+R(-2,2),z=47+row*9+R(-3,3);
                string path="Assets/SoStylized/Environment/Trees/Pine/Prefabs/P_Pine0"+(1+random.Next(3))+".prefab";
                Place(path,trees,Land(x,z),R(1.35f,2.05f),R(0,360));
            }
            var broad=Group("BK broadleaf canopy and bank trees",parent).transform;
            for(int i=0;i<43;i++)
            {
                float a=R(0,Mathf.PI*2);float x=Mathf.Cos(a)*R(38,54),z=18+Mathf.Sin(a)*R(34,45);
                if(z<-12 && x>-22 && x<25) continue;
                Place(BK+"Trees/BigTree"+(1+random.Next(4))+".prefab",broad,Land(x,z),R(.5f,.81f),R(0,360));
            }
            // Near-bank trees frame the view without covering the ducks or the sign.
            Place(BK+"Trees/BigTree2.prefab",broad,Land(25,-12),.68f,215);
            Place(BK+"Trees/BigTree1.prefab",broad,Land(-25,-11),.69f,40);
            Place(BK+"Trees/BigTree4.prefab",broad,Land(29,5),.76f,280);
            Place(BK+"Trees/BigTree3.prefab",broad,Land(23,-20),.86f,85);
            Place(BK+"Trees/BigTree2.prefab",broad,Land(2,-30),.82f,180);
            Place(BK+"Trees/BigTree1.prefab",broad,Land(22,-9),.6f,120);
            var understory=Group("Ferns, shrubs and meadow grass • BK",parent).transform;
            for(int i=0;i<3300;i++)
            {
                float x=R(-57,57),z=R(-33,69),r=Radius(x,z);
                if(r<1.015f||r>1.62f) continue;
                float pathX=14+.27f*(z+20);
                if(z<18 && Mathf.Abs(x-pathX)<2.1f) continue;
                string asset;float scale;
                int kind=random.Next(12);
                if(kind<5) {asset="Plants/Grass"+(1+random.Next(3));scale=R(.65f,1.3f);}
                else if(kind<8) {asset="Plants/LargeFern"+(1+random.Next(3));scale=R(1.3f,2.5f);}
                else if(kind<10) {asset="Plants/Shrub"+(1+random.Next(3));scale=R(.8f,1.8f);}
                else {asset="Trees/Bush"+(1+random.Next(3));scale=R(.4f,.85f);}
                Place(BK+asset+".prefab",understory,Land(x,z),scale,R(0,360));
            }
            for(int i=0;i<180;i++)
            {
                float a=R(0,Mathf.PI),r=R(1.03f,1.23f),x=Mathf.Cos(a)*34*r,z=18+Mathf.Sin(a)*29*r;
                Place(BK+"Trees/Bush"+(1+random.Next(3))+".prefab",understory,Land(x,z),R(.7f,1.35f),R(0,360));
            }
            for(int i=0;i<260;i++)
            {
                float x=R(-16,26),z=R(-21,-10);
                if(Mathf.Abs(x-(14+.27f*(z+20)))<2.1f || Radius(x,z)<1.03f) continue;
                Place(BK+(i%3==0?"Plants/Shrub2":"Plants/Grass3")+".prefab",understory,Land(x,z),R(.7f,1.25f),R(0,360));
            }
            var details=Group("Leaf litter, roots and shoreline stones",parent).transform;
            for(int i=0;i<180;i++)
            {
                float a=R(0,Mathf.PI*2);float r=R(.99f,1.12f),x=Mathf.Cos(a)*34*r,z=18+Mathf.Sin(a)*29*r;
                string asset=i%4==0 ? "Rocks/MossyRock_"+(1+random.Next(5)):"Plants/DeadLeaves"+(1+random.Next(4));
                Place(BK+asset+".prefab",details,Land(x,z),i%4==0?R(.2f,.65f):R(.8f,1.8f),R(0,360));
            }
            for(int i=0;i<48;i++)
            {
                float z=R(-30,12),x=14+.27f*(z+20)+R(-2,2);
                Place(BK+"Plants/DeadLeaves"+(1+random.Next(4))+".prefab",details,Land(x,z)+Vector3.up*.03f,R(.6f,1.2f),R(0,360));
            }
        }
        static GameObject Primitive(string name,PrimitiveType type,Transform parent,Vector3 pos,Vector3 scale,Material material,bool collider=true)
        {
            var obj=GameObject.CreatePrimitive(type);obj.name=name;obj.transform.SetParent(parent,false);obj.transform.position=pos;obj.transform.localScale=scale;
            obj.GetComponent<Renderer>().sharedMaterial=material;
            if(!collider) Object.DestroyImmediate(obj.GetComponent<Collider>());
            return obj;
        }
        static void Log(Transform parent,string name,Vector3 start,Vector3 end,float radius,Material bark)
        {
            var obj=Primitive(name,PrimitiveType.Cylinder,parent,(start+end)/2,new Vector3(radius*2,Vector3.Distance(start,end)/2,radius*2),bark);
            obj.transform.rotation=Quaternion.FromToRotation(Vector3.up,end-start);
        }
        static AnimatorController DuckController()
        {
            string path=Root+"/Generated/CreekDucks.controller";
            var controller=AssetDatabase.LoadAssetAtPath<AnimatorController>(path);
            if(controller) return controller;
            controller=AnimatorController.CreateAnimatorControllerAtPath(path);
            string[] names={"Idle","Flap","PreenFront","PreenBack","Swim"};
            string[] clips={"Idle 1","Idle 2","Continuous Preening - Front","Continuous Preening - Back","Swimming - Keep"};
            for(int i=0;i<names.Length;i++)
            {
                var state=controller.layers[0].stateMachine.AddState(names[i]);
                state.motion=Load<AnimationClip>(Ducks+"Animations/Duck/Duck_Rig_"+clips[i]+".anim");
                if(i==0) controller.layers[0].stateMachine.defaultState=state;
            }
            EditorUtility.SetDirty(controller);AssetDatabase.SaveAssetIfDirty(controller);return controller;
        }
        static void Wildlife(Transform parent)
        {
            var bark=LocalMaterial(Load<Material>("Assets/BK/PureNature_FantasyForest/Models/Trees/BigTree/BigRootsTrunk.mat"));
            Log(parent,"Part-submerged duck resting log",new Vector3(-9,.04f,-3),new Vector3(5,.18f,-5),.24f,bark);
            Log(parent,"Broken driftwood branch",new Vector3(1,.13f,-4.4f),new Vector3(4,.8f,-2.5f),.09f,bark);
            Log(parent,"Right-bank fallen trunk",new Vector3(20,.16f,1),new Vector3(29,.6f,-8),.22f,bark);
            Log(parent,"Bare leaning branch",new Vector3(23,.1f,-2),new Vector3(20,2.4f,4),.065f,bark);
            for(int i=0;i<9;i++) Place(BK+"Trees/SmallRoot"+(i%2+1)+".prefab",parent,new Vector3(R(18,27),.16f,R(-4,14)),R(.65f,1.4f),R(0,360));
            var controller=DuckController();
            string[] types={"Duck Brown","Duck Brown","Duck Brown","Duck Brown","Duck Brown","Duck Brown","Duck Brown"};
            for(int i=0;i<7;i++)
            {
                bool swimming=i>=5;
                float x=-6.8f+i*2.25f;
                Vector3 pos=swimming ? new Vector3(i==5?-5:4,-.11f,i==5?1:-.4f) : new Vector3(x,.30f+(x+9)/14*.14f,-3-(x+9)/7);
                var duck=Place(Ducks+"Prefabs/"+types[i]+".prefab",parent,pos,.82f,swimming?90:R(130,240),false);
                duck.name=(swimming?"Swimming ":"Log-resting ")+types[i]+" "+(i+1);
                var animator=duck.GetComponentInChildren<Animator>();animator.runtimeAnimatorController=controller;animator.applyRootMotion=false;
                var behavior=duck.AddComponent<CreekDuck>();behavior.animator=animator;behavior.seed=47+i*127;behavior.swimming=swimming;
                var clip=Load<AnimationClip>(Ducks+"Animations/Duck/Duck_Rig_"+(swimming?"Swimming - Keep":"Idle 1")+".anim");
                clip.SampleAnimation(animator.gameObject,0);
                float bottom=float.MaxValue;
                foreach(var skin in duck.GetComponentsInChildren<SkinnedMeshRenderer>())
                {
                    var mesh=new Mesh();skin.BakeMesh(mesh);
                    foreach(var v in mesh.vertices) bottom=Mathf.Min(bottom,skin.transform.TransformPoint(v).y);
                    Object.DestroyImmediate(mesh);
                }
                if(!swimming && bottom<float.MaxValue) duck.transform.position+=Vector3.up*(.27f+(x+9)/14*.14f-bottom);
            }
        }
        static void Sign(Transform parent)
        {
            var group=Group("BeeDoof Creek • trail marker",parent);group.transform.position=Land(17,-15.5f);
            var wood=Lit("Trail marker • weathered umber",new Color(.21f,.145f,.095f));
            var post=Lit("Trail marker • post",new Color(.24f,.25f,.18f));
            var p=group.transform.position;
            Primitive("Timber post",PrimitiveType.Cube,group.transform,p+Vector3.up*.69f,new Vector3(.14f,1.5f,.16f),post);
            Primitive("Rounded-edge sign board",PrimitiveType.Cube,group.transform,p+Vector3.up*1.30f,new Vector3(1.65f,.51f,.1f),wood);
            var label=Group("BeeDoof Creek lettering",group.transform);label.transform.localPosition=new Vector3(0,1.30f,-.057f);
            var text=label.AddComponent<TextMesh>();text.text="BeeDoof Creek";text.fontSize=70;text.characterSize=.034f;text.anchor=TextAnchor.MiddleCenter;text.alignment=TextAlignment.Center;text.color=new Color(.78f,.65f,.34f);
            var screw=Lit("Sign fasteners",new Color(.45f,.44f,.38f),.5f);
            Primitive("Lower sign fastener",PrimitiveType.Sphere,group.transform,p+new Vector3(0,1.12f,-.065f),Vector3.one*.023f,screw,false);
            group.transform.rotation=Quaternion.Euler(0,-18,0);
        }
        static void Lighting()
        {
            RenderSettings.ambientMode=AmbientMode.Trilight;
            RenderSettings.ambientSkyColor=new Color(.59f,.65f,.66f);
            RenderSettings.ambientEquatorColor=new Color(.43f,.47f,.38f);
            RenderSettings.ambientGroundColor=new Color(.26f,.28f,.22f);
            RenderSettings.ambientIntensity=1;
            RenderSettings.fog=true;RenderSettings.fogMode=FogMode.ExponentialSquared;
            RenderSettings.fogColor=new Color(.57f,.66f,.66f);RenderSettings.fogDensity=.0055f;
            var sky=new Material(Shader.Find("BeaverCreek/Fully overcast sky"));
            RenderSettings.skybox=Save(sky,Root+"/Materials/Overcast woodland sky.mat");
            var light=Group("Soft afternoon skylight").AddComponent<Light>();light.type=LightType.Directional;light.color=new Color(1,.97f,.87f);light.intensity=1.6f;
            light.transform.rotation=Quaternion.Euler(52,-55,0);light.shadows=LightShadows.Soft;light.shadowStrength=.58f;light.shadowBias=.035f;RenderSettings.sun=light;
            var profile=ScriptableObject.CreateInstance<VolumeProfile>();
            profile.Add<Tonemapping>().mode.Override(TonemappingMode.ACES);
            var colors=profile.Add<ColorAdjustments>();colors.postExposure.Override(.55f);colors.contrast.Override(3);colors.saturation.Override(7);
            var white=profile.Add<WhiteBalance>();white.temperature.Override(-3);white.tint.Override(-2);
            var vignette=profile.Add<Vignette>();vignette.intensity.Override(.13f);vignette.smoothness.Override(.6f);
            // Embedded subassets are saved explicitly with their profile.
            string path=Root+"/Materials/CreekColorGrade.asset";
            var existing=AssetDatabase.LoadAssetAtPath<VolumeProfile>(path);
            if(!existing)
            {
                AssetDatabase.CreateAsset(profile,path);
                foreach(var component in profile.components) AssetDatabase.AddObjectToAsset(component,profile);
                EditorUtility.SetDirty(profile);AssetDatabase.SaveAssetIfDirty(profile);
            }
            else
            {
                Object.DestroyImmediate(profile);profile=existing;
                if(profile.TryGet<ColorAdjustments>(out var grade)) {grade.postExposure.Override(.55f);grade.contrast.Override(3);grade.saturation.Override(7);EditorUtility.SetDirty(grade);AssetDatabase.SaveAssetIfDirty(grade);}
            }
            var volume=Group("Woodland color grade").AddComponent<Volume>();volume.isGlobal=true;volume.sharedProfile=profile;
            RenderSettings.defaultReflectionMode=DefaultReflectionMode.Skybox;RenderSettings.reflectionIntensity=.65f;
            var probe=Group("Pond canopy reflection • freshly captured").AddComponent<ReflectionProbe>();
            probe.transform.position=new Vector3(0,2,12);probe.size=new Vector3(140,55,145);
            probe.center=new Vector3(0,10,0);probe.resolution=256;probe.nearClipPlane=.2f;probe.farClipPlane=180;
            probe.mode=ReflectionProbeMode.Custom;probe.boxProjection=true;probe.intensity=.8f;
            probe.customBakedTexture=AssetDatabase.LoadAssetAtPath<Cubemap>(Root+"/Generated/PondReflection.exr");
            ApplyOvercastLighting();
        }
        static void ApplyOvercastLighting()
        {
            var sky=Load<Material>(Root+"/Materials/Overcast woodland sky.mat");
            sky.shader=Shader.Find("BeaverCreek/Fully overcast sky");
            sky.SetColor("_CloudLight",new Color(.93f,.94f,.95f));sky.SetColor("_CloudShade",new Color(.35f,.38f,.42f));
            EditorUtility.SetDirty(sky);AssetDatabase.SaveAssetIfDirty(sky);RenderSettings.skybox=sky;
            RenderSettings.ambientMode=AmbientMode.Trilight;
            RenderSettings.ambientSkyColor=new Color(.72f,.74f,.75f);
            RenderSettings.ambientEquatorColor=new Color(.51f,.54f,.50f);
            RenderSettings.ambientGroundColor=new Color(.28f,.30f,.25f);
            RenderSettings.fogColor=new Color(.60f,.64f,.64f);RenderSettings.fogDensity=.0055f;
            RenderSettings.sun=null;
            foreach(var light in SceneManager.GetActiveScene().GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<Light>()))
            {
                if(light.type!=LightType.Directional) continue;
                light.name="Diffuse overcast daylight • no direct sun";light.intensity=.8f;
                light.color=new Color(.94f,.96f,1f);light.shadows=LightShadows.None;
            }
            var profile=Load<VolumeProfile>(Root+"/Materials/CreekColorGrade.asset");
            if(profile.TryGet<ColorAdjustments>(out var grade))
            {
                grade.postExposure.Override(.4f);grade.contrast.Override(-3);grade.saturation.Override(4);
                EditorUtility.SetDirty(grade);AssetDatabase.SaveAssetIfDirty(grade);
            }
        }
        public static void OvercastBrown()
        {
            InAuthoringWorkspace(()=>
            {
                string path=Root+"/Prefabs/BeaverCreekEnvironment.prefab";
                var environment=PrefabUtility.LoadPrefabContents(path);
                try
                {
                    var source=Load<GameObject>(Ducks+"Prefabs/Duck Brown.prefab").GetComponentsInChildren<Renderer>(true).SelectMany(r=>r.sharedMaterials).Distinct().ToArray();
                    var body=source.First(m=>m.name=="Duck Brown");var feathers=source.First(m=>m.name.Contains("Feather"));
                    int index=0;
                    foreach(var duck in environment.GetComponentsInChildren<CreekDuck>(true))
                    {
                        foreach(var renderer in duck.GetComponentsInChildren<Renderer>(true))
                            renderer.sharedMaterials=renderer.sharedMaterials.Select(m=>m&&m.name.Contains("Feather")?feathers:body).ToArray();
                        duck.name=(duck.swimming?"Swimming":"Log-resting")+" Brown duck "+(++index);
                    }
                    PrefabUtility.SaveAsPrefabAsset(environment,path);
                }
                finally {PrefabUtility.UnloadPrefabContents(environment);}
                foreach(string name in new[]{"FirstPerson","Cutscene"})
                {
                    var scene=EditorSceneManager.OpenScene(Root+"/Scenes/BeaverCreek_"+name+".unity",OpenSceneMode.Additive);
                    SceneManager.SetActiveScene(scene);
                    try {ApplyOvercastLighting();EditorSceneManager.SaveScene(scene);}
                    finally {EditorSceneManager.CloseScene(scene,true);}
                }
                ValidateClosed();
            });
        }
        public static void RetouchCloudLight()
        {
            InAuthoringWorkspace(()=>
            {
                foreach(string name in new[]{"FirstPerson","Cutscene"})
                {
                    var scene=EditorSceneManager.OpenScene(Root+"/Scenes/BeaverCreek_"+name+".unity",OpenSceneMode.Additive);
                    SceneManager.SetActiveScene(scene);
                    try {ApplyOvercastLighting();EditorSceneManager.SaveScene(scene);}
                    finally {EditorSceneManager.CloseScene(scene,true);}
                }
                ValidateClosed();
            });
        }
        static Camera CameraAt(string name,Vector3 position,Vector3 target,float fov)
        {
            var camera=Group(name).AddComponent<Camera>();camera.tag="MainCamera";camera.transform.position=position;camera.transform.LookAt(target);camera.fieldOfView=fov;
            camera.nearClipPlane=.06f;camera.farClipPlane=240;camera.allowHDR=true;
            var data=camera.GetUniversalAdditionalCameraData();data.renderPostProcessing=true;data.requiresDepthTexture=true;data.requiresColorTexture=true;
            data.antialiasing=AntialiasingMode.SubpixelMorphologicalAntiAliasing;camera.gameObject.AddComponent<AudioListener>();return camera;
        }
        static CreekCinematic.Shot Shot(string name,Vector3 start,Vector3 end,Vector3 lookStart,Vector3 lookEnd,float duration,float fov)
            => new CreekCinematic.Shot {name=name,start=start,end=end,lookStart=lookStart,lookEnd=lookEnd,duration=duration,fieldOfView=fov};
        static void SceneVersion(bool cinematic)
        {
            var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Additive);SceneManager.SetActiveScene(scene);
            try
            {
                PrefabUtility.InstantiatePrefab(Load<GameObject>(Root+"/Prefabs/BeaverCreekEnvironment.prefab"),scene);Lighting();
                if(cinematic)
                {
                    var cam=CameraAt("Cinematic camera",new Vector3(14,3.1f,-23),new Vector3(4,1,17),53);
                    var director=cam.gameObject.AddComponent<CreekCinematic>();director.view=cam;
                    director.shots=new[]{
                        Shot("01 • Arrival at Beaver Pond",new Vector3(14,3.1f,-23),new Vector3(12,2.8f,-21),new Vector3(4,1,15),new Vector3(2,1,15),12,58),
                        Shot("02 • Resting, preening and flapping",new Vector3(-1,1.15f,-12),new Vector3(1,1.05f,-11),new Vector3(-2,.45f,-3.7f),new Vector3(-1,.45f,-3.8f),16,43),
                        Shot("03 • Across the moss-covered pond",new Vector3(17,2.1f,-10),new Vector3(13,2.2f,-8),new Vector3(-5,3,29),new Vector3(-14,4,34),13,55),
                        Shot("04 • Beneath the woodland canopy",new Vector3(9,2.8f,-18),new Vector3(14,3.1f,-23),new Vector3(0,1.8f,16),new Vector3(0,2,15),11,58)
                    };director.Evaluate(0,0);
                }
                else
                {
                    var player=Group("First person • shoreline explorer");player.transform.position=Land(14,-23)+Vector3.up*.15f;
                    var controller=player.AddComponent<CharacterController>();controller.height=1.75f;controller.radius=.3f;controller.center=Vector3.up*.88f;controller.stepOffset=.28f;controller.slopeLimit=48;
                    var cam=CameraAt("Explorer camera",player.transform.position+Vector3.up*1.62f,new Vector3(4,1,18),60);
                    float yaw=cam.transform.eulerAngles.y;player.transform.rotation=Quaternion.Euler(0,yaw,0);cam.transform.SetParent(player.transform);cam.transform.localRotation=Quaternion.Euler(6,0,0);
                    var fps=player.AddComponent<CreekFirstPerson>();fps.view=cam.transform;
                    // Invisible shoreline protection follows the waterline, keeping the walkable bank accessible.
                    var guard=Group("Pond safety boundary • collision only");
                    for(int i=0;i<48;i++)
                    {
                        float a=i*Mathf.PI*2/48,b=(i+1)*Mathf.PI*2/48;
                        Vector3 p=new Vector3(Mathf.Cos(a)*33.5f,0,18+Mathf.Sin(a)*28.6f),q=new Vector3(Mathf.Cos(b)*33.5f,0,18+Mathf.Sin(b)*28.6f);
                        var wall=Group("Waterline "+i,guard.transform);wall.transform.position=(p+q)/2+Vector3.up;wall.transform.rotation=Quaternion.LookRotation(q-p);
                        wall.AddComponent<BoxCollider>().size=new Vector3(.3f,3,Vector3.Distance(p,q)+.1f);
                    }
                }
                EditorSceneManager.SaveScene(scene,Root+"/Scenes/BeaverCreek_"+(cinematic?"Cutscene":"FirstPerson")+".unity");
            }
            finally { EditorSceneManager.CloseScene(scene,true); }
        }
        [MenuItem("Tools/Beaver Creek/Rebuild both scenes (overwrites generated assets)")]
        public static void RebuildFromMenu()
        {
            if(EditorUtility.DisplayDialog("Rebuild Beaver Creek?","This regenerates the two Beaver Creek scenes, shared environment and generated materials. Preserve manual changes first. Vendor assets are not overwritten.","Rebuild","Cancel")) Build();
        }
        public static void Build()
        {
            InAuthoringWorkspace(BuildClosed);
        }
        static void InAuthoringWorkspace(Action action)
        {
            // Reopen clean generated scenes after rebuilding; never discard an unsaved scene.
            var reopen=new List<string>();string activePath=SceneManager.GetActiveScene().path;
            for(int i=0;i<SceneManager.sceneCount;i++)
            {
                var loaded=SceneManager.GetSceneAt(i);
                if(loaded.path.StartsWith(Root+"/Scenes/"))
                {
                    if(loaded.isDirty) throw new InvalidOperationException("Save your Beaver Creek scene changes before rebuilding.");
                    reopen.Add(loaded.path);
                }
            }
            var holding=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Additive);
            string holdingPath=AssetDatabase.GenerateUniqueAssetPath(Root+"/Generated/AuthoringWorkspace.unity");
            EditorSceneManager.SaveScene(holding,holdingPath);
            foreach(string path in reopen) EditorSceneManager.CloseScene(SceneManager.GetSceneByPath(path),true);
            try { action(); }
            finally
            {
                foreach(string path in reopen) EditorSceneManager.OpenScene(path,OpenSceneMode.Additive);
                var active=SceneManager.GetSceneByPath(activePath);if(active.IsValid()) SceneManager.SetActiveScene(active);
                EditorSceneManager.CloseScene(holding,true);
                AssetDatabase.DeleteAsset(holdingPath);
            }
        }
        static void BuildClosed()
        {
            foreach(string dir in new[]{"Scenes","Prefabs","Materials","Generated","Validation"}) Directory.CreateDirectory(Root+"/"+dir);
            AssetDatabase.Refresh();random=new System.Random(1957);materialCopies.Clear();
            Scene previous=SceneManager.GetActiveScene();var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Additive);SceneManager.SetActiveScene(scene);
            try
            {
                var root=Group("Beaver Creek • shared woodland pond");root.AddComponent<CreekWind>();
                Terrain(root.transform);Pond(Group("Water and BK floating moss",root.transform).transform);
                Forest(root.transform);Wildlife(Group("Patchmesh ducks and driftwood",root.transform).transform);Sign(root.transform);
                Weather(root.transform);
                PrefabUtility.SaveAsPrefabAsset(root,Root+"/Prefabs/BeaverCreekEnvironment.prefab");
            }
            finally { EditorSceneManager.CloseScene(scene,true);if(previous.IsValid()) SceneManager.SetActiveScene(previous); }
            SceneVersion(false);SceneVersion(true);if(previous.IsValid()) SceneManager.SetActiveScene(previous);
            var scenes=EditorBuildSettings.scenes.ToList();
            foreach(string name in new[]{"FirstPerson","Cutscene"})
            {
                string path=Root+"/Scenes/BeaverCreek_"+name+".unity";
                if(!scenes.Any(s=>s.path==path)) scenes.Add(new EditorBuildSettingsScene(path,true));
            }
            EditorBuildSettings.scenes=scenes.ToArray();
            Debug.Log("[BeaverCreek] Built two scenes and shared environment without modifying vendor scenes.");
            ValidateClosed();
        }
        static void Weather(Transform parent)
        {
            var root=Group("After the shower • drizzle, ripples and wet trail",parent);
            var weather=root.AddComponent<CreekWeather>();
            var rain=Place(BK+"Fx/Rain.prefab",root.transform,new Vector3(14,13,-20),1,0,false);
            rain.name="BK rain • light lingering drizzle";rain.transform.rotation=Quaternion.identity;
            var ps=rain.GetComponent<ParticleSystem>();ps.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);
            var main=ps.main;main.loop=true;main.prewarm=false;main.startLifetime=1.8f;main.startSpeed=0;main.startSize=new ParticleSystem.MinMaxCurve(.009f,.017f);
            main.startColor=new Color(.78f,.87f,.89f,.36f);main.maxParticles=360;main.simulationSpace=ParticleSystemSimulationSpace.World;main.gravityModifier=0;
            main.startRotation=0;main.startRotation3D=false;main.startSize3D=false;main.scalingMode=ParticleSystemScalingMode.Hierarchy;
            var emission=ps.emission;emission.enabled=true;emission.rateOverTime=105;emission.rateOverDistance=0;emission.SetBursts(new ParticleSystem.Burst[0]);
            var shape=ps.shape;shape.enabled=true;shape.shapeType=ParticleSystemShapeType.Box;shape.scale=new Vector3(24,.2f,24);shape.position=Vector3.zero;shape.rotation=Vector3.zero;
            var velocity=ps.velocityOverLifetime;velocity.enabled=true;velocity.space=ParticleSystemSimulationSpace.World;velocity.x=-.25f;velocity.y=-7.4f;velocity.z=.15f;
            var collision=ps.collision;collision.enabled=false;
            var noise=ps.noise;noise.enabled=false;
            var size=ps.sizeOverLifetime;size.enabled=false;
            var rotation=ps.rotationOverLifetime;rotation.enabled=false;
            var color=ps.colorOverLifetime;color.enabled=true;
            var fade=new Gradient();fade.SetKeys(new[]{new GradientColorKey(Color.white,0),new GradientColorKey(Color.white,1)},new[]{new GradientAlphaKey(0,0),new GradientAlphaKey(1,.15f),new GradientAlphaKey(.6f,.85f),new GradientAlphaKey(0,1)});color.color=fade;
            var mat=new Material(Shader.Find("BeaverCreek/Drizzle and ripples"));mat.SetColor("_BaseColor",new Color(.76f,.85f,.89f,.7f));mat=Save(mat,Root+"/Materials/LightDrizzle.mat");
            var renderer=ps.GetComponent<ParticleSystemRenderer>();renderer.renderMode=ParticleSystemRenderMode.Stretch;renderer.velocityScale=.028f;renderer.lengthScale=1.8f;renderer.cameraVelocityScale=0;
            renderer.sharedMaterial=mat;renderer.shadowCastingMode=ShadowCastingMode.Off;renderer.receiveShadows=false;
            weather.drizzle=ps;
            var rings=Group("Rain rings on the duck cove",root.transform).AddComponent<ParticleSystem>();rings.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);
            rings.transform.position=new Vector3(0,.025f,-2);
            var rm=rings.main;rm.loop=true;rm.startLifetime=new ParticleSystem.MinMaxCurve(.9f,1.5f);rm.startSpeed=0;rm.startSize=new ParticleSystem.MinMaxCurve(.3f,.65f);rm.startColor=new Color(.71f,.79f,.68f,.28f);rm.maxParticles=60;rm.simulationSpace=ParticleSystemSimulationSpace.World;
            var re=rings.emission;re.rateOverTime=23;
            var rs=rings.shape;rs.shapeType=ParticleSystemShapeType.Box;rs.scale=new Vector3(39,.001f,15);
            var rc=rings.colorOverLifetime;rc.enabled=true;rc.color=fade;
            var rz=rings.sizeOverLifetime;rz.enabled=true;rz.size=new ParticleSystem.MinMaxCurve(1,new AnimationCurve(new Keyframe(0,.08f),new Keyframe(1,1)));
            var ringMat=new Material(mat);ringMat.SetFloat("_Ripple",1);ringMat.SetColor("_BaseColor",new Color(.8f,.88f,.77f,.8f));ringMat=Save(ringMat,Root+"/Materials/PondRainRings.mat");
            var rr=rings.GetComponent<ParticleSystemRenderer>();rr.renderMode=ParticleSystemRenderMode.HorizontalBillboard;rr.sharedMaterial=ringMat;rr.shadowCastingMode=ShadowCastingMode.Off;rr.receiveShadows=false;
            weather.pondRipples=rings;
            var puddle=Lit("Trail puddles • wet silt",new Color(.11f,.13f,.085f),.94f);
            for(int i=0;i<8;i++)
            {
                float z=-27+i*4.6f,x=14+.27f*(z+20)+R(-.65f,.65f);
                var obj=Group("Rain-filled trail hollow "+(i+1),root.transform);obj.transform.position=Land(x,z)+Vector3.up*.016f;
                var mesh=new Mesh {name="Shallow irregular puddle "+i};const int count=40;
                var verts=new Vector3[count+1];var uv=new Vector2[count+1];var tris=new int[count*3];uv[0]=new Vector2(.5f,.5f);
                float width=R(.38f,.85f),length=R(.55f,1.35f);
                for(int j=0;j<count;j++)
                {
                    float a=j*Mathf.PI*2/count;float dx=Mathf.Cos(a)*width*(1+.13f*Mathf.Sin(a*5+i)),dz=Mathf.Sin(a)*length;
                    verts[j+1]=new Vector3(dx,Ground(x+dx,z+dz)-Ground(x,z),dz);uv[j+1]=new Vector2(dx/width*.5f+.5f,dz/length*.5f+.5f);
                    tris[j*3]=0;tris[j*3+1]=(j+1)%count+1;tris[j*3+2]=j+1;
                }
                mesh.vertices=verts;mesh.uv=uv;mesh.triangles=tris;mesh.RecalculateNormals();mesh.RecalculateBounds();mesh=Save(mesh,Root+"/Generated/Puddle_"+i+".asset");
                obj.AddComponent<MeshFilter>().sharedMesh=mesh;obj.AddComponent<MeshRenderer>().sharedMaterial=puddle;
            }
        }
        public static void Capture(Camera camera,string file)
        {
            var rt=new RenderTexture(1440,900,24,RenderTextureFormat.ARGB32);rt.Create();
            var previous=RenderTexture.active;
            try
            {
                camera.aspect=1.6f;
                RenderPipeline.SubmitRenderRequest(camera,new UniversalRenderPipeline.SingleCameraRequest {destination=rt});
                RenderTexture.active=rt;var image=new Texture2D(rt.width,rt.height,TextureFormat.RGB24,false);
                image.ReadPixels(new Rect(0,0,rt.width,rt.height),0,0);image.Apply();File.WriteAllBytes(file,image.EncodeToPNG());Object.DestroyImmediate(image);
            }
            finally { RenderTexture.active=previous;rt.Release();Object.DestroyImmediate(rt); }
        }
        [MenuItem("Tools/Beaver Creek/Run Play mode checks (75 seconds)")]
        public static void Smoke() => BeaverCreekPlayCheck.Start();
        public static void SmokeFirstPerson() => BeaverCreekPlayCheck.Start(1);
        [MenuItem("Tools/Beaver Creek/Validate and capture scenes")]
        public static void Validate()
        {
            InAuthoringWorkspace(ValidateClosed);
        }
        static void ValidateClosed()
        {
            Directory.CreateDirectory(Root+"/Validation");
            var previous=SceneManager.GetActiveScene();
            var oldRoots=Enumerable.Range(0,SceneManager.sceneCount).SelectMany(i=>SceneManager.GetSceneAt(i).GetRootGameObjects()).Where(g=>g.activeSelf).ToArray();
            foreach(var obj in oldRoots) obj.SetActive(false);
            var report=new System.Text.StringBuilder("Beaver Creek serialized scene validation\n"+DateTime.Now+"\n");
            try
            {
                foreach(string name in new[]{"FirstPerson","Cutscene"})
                {
                    var scene=EditorSceneManager.OpenScene(Root+"/Scenes/BeaverCreek_"+name+".unity",OpenSceneMode.Additive);SceneManager.SetActiveScene(scene);
                    try
                    {
                        var roots=scene.GetRootGameObjects();var all=roots.SelectMany(r=>r.GetComponentsInChildren<Transform>(true)).ToArray();
                        int missing=all.Sum(t=>GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject));
                        var renderers=roots.SelectMany(r=>r.GetComponentsInChildren<Renderer>(true));
                        var invalid=renderers.SelectMany(r=>r.sharedMaterials).Where(m=>!m||!m.shader||!m.shader.isSupported).ToArray();
                        var ducks=roots.SelectMany(r=>r.GetComponentsInChildren<CreekDuck>()).ToArray();
                        var cameras=roots.SelectMany(r=>r.GetComponentsInChildren<Camera>()).ToArray();
                        report.AppendLine(name+": objects="+all.Length+", missing scripts="+missing+", unsupported/null materials="+invalid.Length+", ducks="+ducks.Length+", cameras="+cameras.Length);
                        if(missing>0||invalid.Length>0||ducks.Length!=7||cameras.Length!=1) throw new Exception("Scene validation failed: "+name);
                        foreach(var duck in ducks)
                        {
                            var clip=Load<AnimationClip>(Ducks+"Animations/Duck/Duck_Rig_"+(duck.swimming?"Swimming - Keep":"Idle 1")+".anim");clip.SampleAnimation(duck.animator.gameObject,.5f);
                        }
                        var cinematic=cameras[0].GetComponent<CreekCinematic>();
                        var probe=roots.SelectMany(r=>r.GetComponentsInChildren<ReflectionProbe>()).First();
                        string reflection=Root+"/Generated/PondReflection.exr";
                        if(name=="FirstPerson")
                        {
                            var water=renderers.Where(r=>r.sharedMaterials.Any(m=>m&&m.shader.name=="BK/Water")).ToArray();
                            foreach(var renderer in water) renderer.enabled=false;
                            try
                            {
                                probe.mode=ReflectionProbeMode.Baked;
                                if(!Lightmapping.BakeReflectionProbe(probe,reflection)) throw new Exception("Reflection probe capture failed.");
                                AssetDatabase.ImportAsset(reflection,ImportAssetOptions.ForceSynchronousImport);
                            }
                            finally { foreach(var renderer in water) renderer.enabled=true; }
                        }
                        probe.mode=ReflectionProbeMode.Custom;probe.customBakedTexture=Load<Cubemap>(reflection);
                        EditorSceneManager.SaveScene(scene);
                        if(cinematic) for(int i=0;i<cinematic.shots.Length;i++)
                        {
                            cinematic.Evaluate(i,.4f);Capture(cameras[0],Root+"/Validation/Cutscene_"+(i+1)+".png");
                        }
                        else
                        {
                            Capture(cameras[0],Root+"/Validation/FirstPerson.png");
                            var rotation=cameras[0].transform.rotation;
                            cameras[0].transform.rotation=Quaternion.Euler(-35,rotation.eulerAngles.y,0);
                            Capture(cameras[0],Root+"/Validation/CloudSky.png");
                            cameras[0].transform.rotation=rotation;
                        }
                    }
                    finally {EditorSceneManager.CloseScene(scene,true);}
                }
            }
            finally
            {
                foreach(var obj in oldRoots) if(obj) obj.SetActive(true);
                if(previous.IsValid()) SceneManager.SetActiveScene(previous);
                File.WriteAllText(Root+"/Validation/SceneValidation.txt",report.ToString());
            }
        }
    }
}
