﻿using StereoKit;
using System;
using System.Collections.Generic;
using System.Text;

class Terrain
{
    enum LodMode
    {
        Center,
        Head,
        Constant
    }
    struct Chunk 
    {
        public Vec3   centerOffset;
        public Matrix transform;
        public int    lod;
    }
    Chunk[] chunks;
    Vec3    chunkCenter;
    float   chunkSize;

    float    terrainHeight = 1;
    Vec4     heightSize;
    Vec4     colorSize;
    Material terrainMaterial;
    Mesh[]   meshLods = new Mesh[4];
    LodMode  lodMode  = LodMode.Constant;

    public Vec3  Center { 
        get{ return chunkCenter; } 
        set{ chunkCenter = value; UpdateChunks(); } }
    public float Height { 
        get{ return terrainHeight; } 
        set{ terrainHeight = value; if (terrainMaterial != null) terrainMaterial["world_height"] = terrainHeight; } }
    public Material Material => terrainMaterial;

    public Terrain(int chunkDetail, float chunkSize, int chunkGrid)
    {
        this.chunkSize = chunkSize;
        chunkCenter = Vec3.Zero;

        terrainMaterial = new Material(Shader.FromFile(@"terrain.hlsl"));
        
        int subdivisions = chunkDetail;

        for (int i = 0; i < meshLods.Length; i++)
        {
            meshLods[i]  = Mesh.GeneratePlane(Vec2.One*chunkSize, subdivisions);
            subdivisions = subdivisions / 2;
        }

        if (chunkGrid %2 != 1)
            chunkGrid += 1;

        chunks = new Chunk[chunkGrid * chunkGrid];
        float half = (int)(chunkGrid/2.0f);
        for (int y = 0; y < chunkGrid; y++)
        {
            for (int x = 0; x < chunkGrid; x++)
            {
                Vec3 pos = new Vec3(x - half, 0, y - half) * chunkSize;
                chunks[x+y*chunkGrid].centerOffset = pos;

            }
        }
        UpdateChunks();
    }

    public void SetHeightData(Tex heightData, Vec3 heightDimensions, Vec2 heightCenter)
    {
        heightSize.XY = heightCenter - heightDimensions.XZ / 2;
        heightSize.ZW = heightDimensions.XZ;
        terrainMaterial["world"       ] = heightData;
        terrainMaterial["world_size"  ] = heightSize;
        terrainMaterial["world_height"] = heightDimensions.y;
    }

    public void SetColorData(Tex colorData, Vec2 colorDimensions, Vec2 colorCenter)
    {
        colorSize.XY = colorCenter - colorDimensions / 2;
        colorSize.ZW = colorDimensions;
        terrainMaterial["world_color"] = colorData;
        terrainMaterial["color_size" ] = colorSize;
    }

    void UpdateChunks() 
    {
        for (int i = 0; i < chunks.Length; i++)
            chunks[i].transform = Matrix.T(chunks[i].centerOffset+chunkCenter);
        UpdateLod();
    }

    void UpdateLod()
    {
        switch (lodMode) {
            case LodMode.Constant:
            {
                for (int i = 0; i < chunks.Length; i++)
                    chunks[i].lod = 0;
            }break;
            case LodMode.Center:
            {
                for (int i = 0; i < chunks.Length; i++)
                    chunks[i].lod = (int)Math.Min(meshLods.Length - 1, ((chunks[i].centerOffset.Magnitude / chunkSize)));
            } break;
            case LodMode.Head:
            {
                Vec3 headRel = Input.Head.position - chunkCenter;
                for (int i = 0; i < chunks.Length; i++)
                    chunks[i].lod = (int)Math.Min(meshLods.Length - 1, (((chunks[i].centerOffset-headRel).Magnitude / chunkSize)));
            } break;
        }
    }

    public void Update()
    {
        Vec3 offset = Input.Head.position - chunkCenter;
        bool update = false;
        if      (offset.x > chunkSize*0.4f ) { chunkCenter.x += chunkSize*0.5f; update = true; }
        else if (offset.x < chunkSize*-0.4f) { chunkCenter.x -= chunkSize*0.5f; update = true; }
        if      (offset.z > chunkSize*0.4f ) { chunkCenter.z += chunkSize*0.5f; update = true; }
        else if (offset.z < chunkSize*-0.4f) { chunkCenter.z -= chunkSize*0.5f; update = true; }
        if (update) UpdateChunks();

        if (lodMode == LodMode.Head)
            UpdateLod();

        for (int i = 0; i < chunks.Length; i++)
        {
            meshLods[chunks[i].lod].Draw(terrainMaterial, chunks[i].transform);

            Color c     = Color.HSV(chunks[i].lod * 0.25f, 1, 1);
            Vec3  start = chunks[i].centerOffset + chunkCenter;
            /*Lines.Add(
                start + new Vec3(chunkSize*0.45f, 0.2f, chunkSize*0.45f),
                start + new Vec3(chunkSize*0.45f, 0.2f, chunkSize*-0.45f), c, 0.01f);
            Lines.Add(
                start + new Vec3(chunkSize * -0.45f, 0.2f, chunkSize * 0.45f),
                start + new Vec3(chunkSize * -0.45f, 0.2f, chunkSize * -0.45f), c, 0.01f);
            Lines.Add(
                start + new Vec3(chunkSize *  0.45f, 0.2f, chunkSize * -0.45f),
                start + new Vec3(chunkSize * -0.45f, 0.2f, chunkSize * -0.45f), c, 0.01f);
            Lines.Add(
                start + new Vec3(chunkSize *  0.45f, 0.2f, chunkSize * 0.45f),
                start + new Vec3(chunkSize * -0.45f, 0.2f, chunkSize * 0.45f), c, 0.01f);*/
        }
    }
}