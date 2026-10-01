using System.Collections.Generic;
using UnityEngine;

namespace Yokai
{
    /// <summary>
    /// Visual-only mobile world layer. Gameplay collision remains intentionally simple and stable in YokaiWorldBootstrap.
    /// </summary>
    public static class YokaiWorldArt
    {
        static Material moss, earth, stone, wood, darkWood, foliage, foliageDark, roof, crimson, parchment, iron;

        public static void Build(Transform world)
        {
            EnsureMaterials();
            Transform root = new GameObject("YOKAI_ART_WORLD_V14").transform;
            root.SetParent(world, false);

            BuildTerrain(root);
            BuildCedarPass(root);
            BuildForsakenHamlet(root);
            BuildShrineBasin(root);
            BuildOniCourtyard(root);
        }

        static void EnsureMaterials()
        {
            moss = YokaiMaterialLibrary.Get("art_moss", new Color(.075f,.105f,.075f), 0f, .18f);
            earth = YokaiMaterialLibrary.Get("art_earth", new Color(.19f,.135f,.085f), 0f, .12f);
            stone = YokaiMaterialLibrary.Get("art_stone", new Color(.23f,.245f,.25f), 0f, .22f);
            wood = YokaiMaterialLibrary.Get("art_wood", new Color(.20f,.115f,.065f), 0f, .16f);
            darkWood = YokaiMaterialLibrary.Get("art_darkwood", new Color(.085f,.055f,.04f), 0f, .12f);
            foliage = YokaiMaterialLibrary.Get("art_foliage", new Color(.055f,.19f,.09f), 0f, .12f);
            foliageDark = YokaiMaterialLibrary.Get("art_foliage_dark", new Color(.035f,.105f,.06f), 0f, .1f);
            roof = YokaiMaterialLibrary.Get("art_roof", new Color(.13f,.14f,.16f), .04f, .24f);
            crimson = YokaiMaterialLibrary.Get("art_crimson", new Color(.42f,.035f,.025f), .02f, .2f);
            parchment = YokaiMaterialLibrary.Get("art_parchment", new Color(.66f,.57f,.40f), 0f, .12f);
            iron = YokaiMaterialLibrary.Get("art_iron", new Color(.18f,.19f,.21f), .58f, .36f);
        }

        static void BuildTerrain(Transform root)
        {
            // Visible top layer over the simple collision slab.
            for (int i = 0; i < 12; i++)
            {
                float z = -15f + i * 10f;
                GameObject bank = YokaiArtUtility.MeshPart("MossBank_"+i, root,
                    YokaiMeshLibrary.TaperedBox("bank", 34f, 10f, 32f, 10f, .35f),
                    new Vector3(0f,-.05f,z), Vector3.one, Quaternion.identity, moss);
                bank.GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            }

            for (int i = 0; i < 23; i++)
            {
                float z = -17.5f + i * 5f;
                float x = Mathf.Sin(z * .13f) * .28f;
                GameObject road = YokaiArtUtility.MeshPart("Road_"+i, root,
                    YokaiMeshLibrary.TaperedBox("road_strip", 5.8f, 5.25f, 5.5f, 5.25f, .08f),
                    new Vector3(x,.16f,z), Vector3.one, Quaternion.Euler(0f,Mathf.Sin(z*.09f)*1.8f,0f), earth);
                road.GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            }
        }

        static void BuildCedarPass(Transform root)
        {
            Transform zone = new GameObject("ZONE_01_CedarPass").transform;
            zone.SetParent(root, false);
            for (int i = 0; i < 18; i++)
            {
                float z = -18f + i * 2.25f;
                float wobble = Mathf.Sin(i * 1.91f);
                BuildTree(zone, new Vector3(-7.5f - Mathf.Abs(wobble)*2f, 0f, z), .86f + (i%4)*.09f, i*2+1);
                BuildTree(zone, new Vector3(7.4f + Mathf.Abs(Mathf.Cos(i))*2.2f, 0f, z+1.2f), .82f + (i%5)*.075f, i*2+2);
            }
            for (int i = 0; i < 12; i++)
            {
                float z = -12f + i * 3.1f;
                float x = (i % 2 == 0 ? -1f : 1f) * (4.8f + (i%3)*1.2f);
                BuildRock(zone, new Vector3(x,.28f,z), .65f + (i%4)*.12f, 100+i);
            }
        }

        static void BuildForsakenHamlet(Transform root)
        {
            Transform zone = new GameObject("ZONE_02_ForsakenHamlet").transform;
            zone.SetParent(root, false);
            BuildHouse(zone, new Vector3(-8.4f,0f,25f), new Vector3(5.4f,3.4f,4.5f), 9f, false);
            BuildHouse(zone, new Vector3(8.7f,0f,31f), new Vector3(4.8f,3.1f,4.0f), -12f, true);
            BuildHouse(zone, new Vector3(-9.4f,0f,43f), new Vector3(5.8f,3.6f,4.8f), 15f, true);
            BuildHouse(zone, new Vector3(9.2f,0f,46f), new Vector3(4.3f,2.9f,3.8f), -8f, false);

            for (int i=0;i<9;i++)
            {
                float z=22f+i*3.2f;
                BuildFence(zone,new Vector3(-4.2f,0f,z),Quaternion.Euler(0f,4f,0f));
                if(i%2==0) BuildFence(zone,new Vector3(4.2f,0f,z+1.4f),Quaternion.Euler(0f,-5f,0f));
            }
            BuildBrokenCart(zone,new Vector3(5.5f,.15f,39f),Quaternion.Euler(0f,28f,0f));
            BuildLantern(zone,new Vector3(-2.8f,0f,26f));
            BuildLantern(zone,new Vector3(2.8f,0f,37f));
            BuildLantern(zone,new Vector3(-2.8f,0f,48f));
        }

        static void BuildShrineBasin(Transform root)
        {
            Transform zone = new GameObject("ZONE_03_SpiritShrineBasin").transform;
            zone.SetParent(root, false);
            for(int i=0;i<5;i++)
            {
                float w=6.2f+i*.6f;
                YokaiArtUtility.MeshPart("ShrineStep_"+i, zone, YokaiMeshLibrary.TaperedBox("shrine_step",1f,1f,1f,1f,1f),
                    new Vector3(0f,.12f+i*.1f,52f+i*.72f),new Vector3(w,.18f,1.05f),Quaternion.identity,stone);
            }
            BuildToriiArt(zone,new Vector3(0f,0f,58.2f),5.2f,crimson);
            BuildStoneGuardian(zone,new Vector3(-3.3f,0f,60.5f),false);
            BuildStoneGuardian(zone,new Vector3(3.3f,0f,60.5f),true);
            BuildLantern(zone,new Vector3(-2.5f,0f,63f));
            BuildLantern(zone,new Vector3(2.5f,0f,63f));
            for(int i=0;i<8;i++)
            {
                float a=i*Mathf.PI*2f/8f;
                BuildRock(zone,new Vector3(Mathf.Cos(a)*8.5f,.22f,60f+Mathf.Sin(a)*6.5f),.62f+(i%3)*.12f,220+i);
            }
        }

        static void BuildOniCourtyard(Transform root)
        {
            Transform zone = new GameObject("ZONE_04_OniCourtyard").transform;
            zone.SetParent(root, false);
            // Arena ring / broken pillars.
            for (int i=0;i<12;i++)
            {
                float a=i*Mathf.PI*2f/12f;
                Vector3 p=new Vector3(Mathf.Cos(a)*9.2f,0f,78f+Mathf.Sin(a)*9.2f);
                float h=(i%4==0)?3.2f:((i%3==0)?2.1f:1.25f);
                YokaiArtUtility.MeshPart("ArenaPillar_"+i,zone,YokaiMeshLibrary.Frustum("stone_pillar",8,.36f,.29f,1f),
                    p+Vector3.up*h*.5f,new Vector3(1f,h,1f),Quaternion.Euler(0f,i*17f,0f),stone);
            }
            BuildToriiArt(zone,new Vector3(0f,0f,68f),6.4f,crimson);
            BuildTempleFacade(zone,new Vector3(0f,0f,91f));
            BuildBanner(zone,new Vector3(-6.2f,0f,83f));
            BuildBanner(zone,new Vector3(6.2f,0f,83f));
            BuildBanner(zone,new Vector3(-6.2f,0f,88f));
            BuildBanner(zone,new Vector3(6.2f,0f,88f));
        }

        static void BuildTree(Transform parent, Vector3 position, float scale, int seed)
        {
            GameObject tree = new GameObject("Cedar_3D");
            tree.transform.SetParent(parent,false); tree.transform.localPosition=position;
            Renderer trunk = YokaiArtUtility.MeshPart("Trunk",tree.transform,YokaiMeshLibrary.Frustum("cedar_trunk",8,.30f,.19f,1f),
                new Vector3(0f,2.1f,0f),new Vector3(scale,4.2f*scale,scale),Quaternion.Euler(0f,seed*13f,0f),wood).GetComponent<Renderer>();
            var high=new List<Renderer>{trunk};
            for(int i=0;i<3;i++)
            {
                float y=3.7f+i*1.05f;
                float r=(1.72f-i*.28f)*scale;
                Renderer crown=YokaiArtUtility.MeshPart("Crown_H_"+i,tree.transform,YokaiMeshLibrary.Frustum("cedar_crown",9,.62f,0f,1f),
                    new Vector3((i%2==0?-.08f:.12f)*scale,y,0f),new Vector3(r,2.1f*scale,r),Quaternion.Euler(0f,seed*19f+i*21f,0f),i==2?foliageDark:foliage).GetComponent<Renderer>();
                high.Add(crown);
            }
            Renderer lowTrunk=YokaiArtUtility.MeshPart("Trunk_LOW",tree.transform,YokaiMeshLibrary.Frustum("cedar_trunk_low",6,.30f,.18f,1f),
                new Vector3(0f,2.1f,0f),new Vector3(scale,4.2f*scale,scale),Quaternion.identity,wood).GetComponent<Renderer>();
            Renderer low=YokaiArtUtility.MeshPart("Crown_LOW",tree.transform,YokaiMeshLibrary.Frustum("cedar_crown_low",7,.62f,0f,1f),
                new Vector3(0f,4.65f,0f),new Vector3(1.85f*scale,4.0f*scale,1.85f*scale),Quaternion.identity,foliageDark).GetComponent<Renderer>();
            YokaiArtUtility.AddTwoLevelLod(tree,high.ToArray(),new[]{lowTrunk,low},.12f,.022f);
        }

        static void BuildRock(Transform parent, Vector3 p, float scale, int seed)
        {
            GameObject rock=YokaiArtUtility.MeshPart("Rock_3D",parent,YokaiMeshLibrary.Rock("rock_"+(seed%7),seed),p,
                new Vector3(scale*1.35f,scale*.88f,scale),Quaternion.Euler(0f,seed*23f,seed%17-8),stone);
            rock.GetComponent<Renderer>().shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.On;
        }

        static void BuildHouse(Transform parent, Vector3 p, Vector3 size, float yaw, bool ruined)
        {
            GameObject house=new GameObject(ruined?"Ruined_Minka":"Minka");house.transform.SetParent(parent,false);house.transform.localPosition=p;house.transform.localRotation=Quaternion.Euler(0f,yaw,0f);
            float wallH=size.y;
            YokaiArtUtility.MeshPart("Walls",house.transform,YokaiMeshLibrary.TaperedBox("house_body",1f,1f,.96f,.96f,1f),
                new Vector3(0f,wallH*.5f,0f),new Vector3(size.x,wallH,size.z),Quaternion.identity,darkWood,true);
            YokaiArtUtility.MeshPart("Plaster",house.transform,YokaiMeshLibrary.TaperedBox("house_inner",1f,1f,.97f,.97f,1f),
                new Vector3(0f,wallH*.55f,.02f),new Vector3(size.x*.82f,wallH*.72f,size.z*1.01f),Quaternion.identity,parchment);
            GameObject roofGo=YokaiArtUtility.MeshPart("Roof",house.transform,YokaiMeshLibrary.Roof("minka_roof"),
                new Vector3(0f,wallH+.06f,0f),new Vector3(size.x*1.22f,1.35f,size.z*1.25f),Quaternion.identity,roof);
            if(ruined) roofGo.transform.localRotation=Quaternion.Euler(0f,0f,7f);
            for(int i=-1;i<=1;i++)
                YokaiArtUtility.MeshPart("Beam_"+i,house.transform,YokaiMeshLibrary.Frustum("beam",6,.5f,.5f,1f),
                    new Vector3(i*size.x*.34f,wallH*.55f,size.z*.51f),new Vector3(.13f,wallH*.82f,.13f),Quaternion.identity,wood);
        }

        static void BuildFence(Transform parent, Vector3 p, Quaternion r)
        {
            GameObject f=new GameObject("BambooFence");f.transform.SetParent(parent,false);f.transform.localPosition=p;f.transform.localRotation=r;
            for(int i=-2;i<=2;i++)
                YokaiArtUtility.MeshPart("Post",f.transform,YokaiMeshLibrary.Frustum("bamboo",6,.5f,.45f,1f),new Vector3(i*.42f,.55f,0f),new Vector3(.08f,1.1f,.08f),Quaternion.identity,wood);
            for(int j=0;j<2;j++)
                YokaiArtUtility.MeshPart("Rail",f.transform,YokaiMeshLibrary.Frustum("bamboo",6,.5f,.45f,1f),new Vector3(0f,.35f+j*.48f,0f),new Vector3(.07f,2.05f,.07f),Quaternion.Euler(0f,0f,90f),wood);
        }

        static void BuildBrokenCart(Transform parent, Vector3 p, Quaternion r)
        {
            GameObject cart=new GameObject("BrokenCart");cart.transform.SetParent(parent,false);cart.transform.localPosition=p;cart.transform.localRotation=r;
            YokaiArtUtility.MeshPart("Bed",cart.transform,YokaiMeshLibrary.TaperedBox("cart_bed",1f,1f,1f,1f,1f),new Vector3(0f,.48f,0f),new Vector3(2f,.18f,1.05f),Quaternion.identity,wood);
            for(int side=-1;side<=1;side+=2)
                YokaiArtUtility.MeshPart("Wheel",cart.transform,YokaiMeshLibrary.Frustum("wheel",10,.5f,.5f,1f),new Vector3(side*1.02f,.42f,0f),new Vector3(.78f,.12f,.78f),Quaternion.Euler(0f,0f,90f),darkWood);
            YokaiArtUtility.MeshPart("Shaft",cart.transform,YokaiMeshLibrary.Frustum("beam",6,.5f,.5f,1f),new Vector3(0f,.42f,1.35f),new Vector3(.1f,2.8f,.1f),Quaternion.Euler(90f,0f,0f),wood);
        }

        static void BuildLantern(Transform parent, Vector3 p)
        {
            GameObject lamp=new GameObject("StoneLantern_3D");lamp.transform.SetParent(parent,false);lamp.transform.localPosition=p;
            YokaiArtUtility.MeshPart("Foot",lamp.transform,YokaiMeshLibrary.TaperedBox("lantern_foot",1f,1f,.8f,.8f,1f),new Vector3(0f,.12f,0f),new Vector3(.65f,.24f,.65f),Quaternion.identity,stone);
            YokaiArtUtility.MeshPart("Stem",lamp.transform,YokaiMeshLibrary.Frustum("lantern_stem",8,.5f,.42f,1f),new Vector3(0f,.72f,0f),new Vector3(.22f,1.05f,.22f),Quaternion.identity,stone);
            YokaiArtUtility.MeshPart("Cap",lamp.transform,YokaiMeshLibrary.Roof("lantern_roof"),new Vector3(0f,1.48f,0f),new Vector3(.82f,.55f,.82f),Quaternion.identity,stone);
            Renderer glow=YokaiArtUtility.MeshPart("Glow",lamp.transform,YokaiMeshLibrary.LowSphere("orb",8,4),new Vector3(0f,1.25f,0f),Vector3.one*.32f,Quaternion.identity,YokaiMaterialLibrary.Emissive("lantern_art",new Color(.9f,.48f,.12f))).GetComponent<Renderer>();
            glow.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;
        }

        static void BuildToriiArt(Transform parent, Vector3 p, float width, Material material)
        {
            GameObject t=new GameObject("Torii_Art");t.transform.SetParent(parent,false);t.transform.localPosition=p;
            for(int side=-1;side<=1;side+=2)
            {
                YokaiArtUtility.MeshPart("Post",t.transform,YokaiMeshLibrary.Frustum("torii_post",8,.5f,.42f,1f),new Vector3(side*width*.38f,1.65f,0f),new Vector3(.34f,3.3f,.34f),Quaternion.identity,material);
                YokaiArtUtility.MeshPart("Foot",t.transform,YokaiMeshLibrary.TaperedBox("torii_foot",1f,1f,.75f,.75f,1f),new Vector3(side*width*.38f,.12f,0f),new Vector3(.72f,.24f,.72f),Quaternion.identity,stone);
            }
            YokaiArtUtility.MeshPart("Lintel",t.transform,YokaiMeshLibrary.TaperedBox("torii_beam",1f,1f,.92f,.92f,1f),new Vector3(0f,3.1f,0f),new Vector3(width,.28f,.38f),Quaternion.identity,material);
            YokaiArtUtility.MeshPart("Top",t.transform,YokaiMeshLibrary.TaperedBox("torii_top",1f,1f,.94f,.94f,1f),new Vector3(0f,3.42f,0f),new Vector3(width*1.14f,.22f,.44f),Quaternion.identity,darkWood);
        }

        static void BuildStoneGuardian(Transform parent, Vector3 p, bool mirrored)
        {
            GameObject g=new GameObject("Komainu_Stone");g.transform.SetParent(parent,false);g.transform.localPosition=p;g.transform.localRotation=Quaternion.Euler(0f,mirrored?205f:155f,0f);
            YokaiArtUtility.MeshPart("Body",g.transform,YokaiMeshLibrary.LowSphere("guardian_body",8,4),new Vector3(0f,.7f,0f),new Vector3(.82f,1.05f,.72f),Quaternion.identity,stone);
            YokaiArtUtility.MeshPart("Head",g.transform,YokaiMeshLibrary.LowSphere("guardian_head",8,4),new Vector3(0f,1.35f,.18f),new Vector3(.62f,.62f,.62f),Quaternion.identity,stone);
            YokaiArtUtility.MeshPart("Muzzle",g.transform,YokaiMeshLibrary.TaperedBox("guardian_muzzle",1f,1f,.7f,.7f,1f),new Vector3(0f,1.27f,.53f),new Vector3(.36f,.25f,.38f),Quaternion.identity,stone);
        }

        static void BuildTempleFacade(Transform parent, Vector3 p)
        {
            GameObject temple=new GameObject("OniTempleFacade");temple.transform.SetParent(parent,false);temple.transform.localPosition=p;
            YokaiArtUtility.MeshPart("Main",temple.transform,YokaiMeshLibrary.TaperedBox("temple_body",1f,1f,.94f,.94f,1f),new Vector3(0f,2.25f,0f),new Vector3(12f,4.5f,4.5f),Quaternion.identity,darkWood);
            YokaiArtUtility.MeshPart("Roof",temple.transform,YokaiMeshLibrary.Roof("temple_roof"),new Vector3(0f,4.55f,0f),new Vector3(14.8f,2.1f,6.3f),Quaternion.identity,roof);
            for(int i=-2;i<=2;i++)
                YokaiArtUtility.MeshPart("Pillar_"+i,temple.transform,YokaiMeshLibrary.Frustum("temple_pillar",8,.5f,.45f,1f),new Vector3(i*2f,2f,-2.4f),new Vector3(.34f,4f,.34f),Quaternion.identity,crimson);
        }

        static void BuildBanner(Transform parent, Vector3 p)
        {
            GameObject b=new GameObject("OniBanner");b.transform.SetParent(parent,false);b.transform.localPosition=p;
            YokaiArtUtility.MeshPart("Pole",b.transform,YokaiMeshLibrary.Frustum("banner_pole",8,.5f,.44f,1f),new Vector3(0f,1.8f,0f),new Vector3(.10f,3.6f,.10f),Quaternion.identity,iron);
            YokaiArtUtility.MeshPart("Cloth",b.transform,YokaiMeshLibrary.TaperedBox("banner_cloth",1f,1f,.86f,.92f,1f),new Vector3(.48f,2.6f,0f),new Vector3(.9f,1.55f,.055f),Quaternion.Euler(0f,0f,-4f),crimson);
        }
    }
}
