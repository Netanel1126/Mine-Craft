using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using UnityEngine;

[Serializable]
class BlockData {
    public Block.BlockType[,,] matrix;

    public BlockData() { }

    public BlockData(Block[,,] b) {
        matrix = new Block.BlockType[World.chunkSize, World.chunkSize, World.chunkSize];
        for (int z = 0; z < World.chunkSize; z++)
            for (int y = 0; y < World.chunkSize; y++)
                for (int x = 0; x < World.chunkSize; x++) {
                    matrix[x, y, z] = b[x, y, z].bType;
                }
    }
}

public class Chunk {

    public enum ChunkStatus { DRAW, DONE, KEEP };

    public Material cubeMaterial;
    public Material fluidMaterial;
    public Block[,,] chunkData;
    public GameObject chunk;
    public GameObject fluid;
    public ChunkStatus status;
    public ChunkMB mb;
    public bool changed;

    bool treesCreated = false;
    BlockData bd;

    public Chunk(Vector3 position, Material c, Material t) {
        chunk = new GameObject(World.BuildChunkName(position));
        chunk.transform.position = position;
        fluid = new GameObject(World.BuildChunkName(position) + "_F");
        fluid.transform.position = position;
        cubeMaterial = c;
        fluidMaterial = t;

        mb = chunk.AddComponent<ChunkMB>();
        mb.SetOwner(this);

        BuildChunk();
    }

    void BuildChunk() {
        int sizeX = World.chunkSize, sizeY = World.chunkSize, sizeZ = World.chunkSize;
        bool dataFromFile = Load();
        chunkData = new Block[sizeX, sizeY, sizeZ];

        for (int z = 0; z < sizeZ; z++) {
            for (int y = 0; y < sizeY; y++) {
                for (int x = 0; x < sizeX; x++) {
                    Vector3 pos = new Vector3(x, y, z);
                    int worldX = (int)(x + chunk.transform.position.x);
                    int worldY = (int)(y + chunk.transform.position.y);
                    int worldZ = (int)(z + chunk.transform.position.z);

                    if (dataFromFile) {
                        chunkData[x, y, z] = new Block(bd.matrix[x, y, z], pos, this);
                        continue;
                    }

                    Block.BlockType blockType;
                    if (0 == worldY) {
                        blockType = Block.BlockType.BEDROCK;
                    } else if (worldY < Utils.GenerateStoneHeight(worldX, worldZ)) {
                        if (Utils.fBM3D(worldX, worldY, worldZ, 0.01f, 2) < 0.4f && worldY < 40) {
                            blockType = Block.BlockType.DIAMOND;
                        } else if (Utils.fBM3D(worldX, worldY, worldZ, 0.03f, 3) < 0.41f && worldY < 20) {
                            blockType = Block.BlockType.REDSTONE;
                        } else {
                            blockType = Block.BlockType.STONE;
                        }
                    } else if (worldY == Utils.GenerateHeight(worldX, worldZ)) {
                        if(Utils.fBM3D(worldX, worldY, worldZ, 0.4f, 2) < 0.4f && worldY > 65) {
                            blockType = Block.BlockType.WOODBASE;
                        }else
                            blockType = Block.BlockType.GRASS;
                    } else if (worldY < Utils.GenerateHeight(worldX, worldZ)) {
                        blockType = Block.BlockType.DIRT;
                    } else if (worldY < 65) {
                        blockType = Block.BlockType.WATER;
                    } else {
                        blockType = Block.BlockType.AIR;
                    }

                    if (blockType != Block.BlockType.WATER && Utils.fBM3D(worldX, worldY, worldZ, 0.1f, 3) < 0.42f) {
                        if (worldY < 65) {
                            blockType = Block.BlockType.WATER;
                        } else
                            blockType = Block.BlockType.AIR;
                    }

                    chunkData[x, y, z] = new Block(blockType, pos, this);
                    status = ChunkStatus.DRAW;
                }
            }
        }
    }

    public void DrawChunk() {
        int sizeX = World.chunkSize, sizeY = World.chunkSize, sizeZ = World.chunkSize;

        if (!treesCreated) {
            for (int z = 0; z < sizeZ; z++) {
                for (int y = 0; y < sizeY; y++) {
                    for (int x = 0; x < sizeX; x++) {
                        this.BuildTrees(chunkData[x, y, z], x, y, z);
                    }
                }
            }
            treesCreated = true;
        }

        for (int z = 0; z < sizeZ; z++) {
            for (int y = 0; y < sizeY; y++) {
                for (int x = 0; x < sizeX; x++) {
                    chunkData[x, y, z].Draw();
                }
            }
        }
        CombineQuads(chunk.gameObject, cubeMaterial);
        MeshCollider collider = chunk.gameObject.AddComponent(typeof(MeshCollider)) as MeshCollider;
        collider.sharedMesh = chunk.gameObject.GetComponent<MeshFilter>().mesh;

        CombineQuads(fluid.gameObject, fluidMaterial);
        fluid.AddComponent<UVScroller>();

        status = ChunkStatus.DONE;
    }


    private void BuildTrees(Block trunk, int x, int y, int z) {
        if (trunk.bType != Block.BlockType.WOODBASE) return;

        Block t = trunk.GetBlock(x, y + 1, z);
        if (t != null){
            t.SetType(Block.BlockType.WOOD);
            Block t1 = t.GetBlock(x, y + 2, z);
            if(t1 != null) {
                t1.SetType(Block.BlockType.WOOD);

                for (int i = -1; i <= 1; i++)
                    for (int j = -1; j <= 1; j++)
                        for (int k = 3; k <= 4; k++) {
                            Block t2 = trunk.GetBlock(x + i, y + k, z + j);

                            if (t2 != null) {
                                t2.SetType(Block.BlockType.LEAVES);
                            } else {
                                return;
                            }
                        }
                Block t3 = t1.GetBlock(x, y + 5, z);
                if (t3 != null) {
                    t3.SetType(Block.BlockType.LEAVES);
                }
            }
        }
    }

    public void UpdateChunk() {
        for (int z = 0; z < World.chunkSize; z++) {
            for (int y = 0; y < World.chunkSize; y++) {
                for (int x = 0; x < World.chunkSize; x++) {
                    Block block = chunkData[x, y, z];
                    if (block.bType == Block.BlockType.SAND) {
                        mb.StartCoroutine(mb.Drop(block,Block.BlockType.SAND,20));
                    }
                }
            }
        }
    }


    public void Redraw() {
        GameObject.DestroyImmediate(chunk.GetComponent<MeshFilter>());
        GameObject.DestroyImmediate(chunk.GetComponent<MeshRenderer>());
        GameObject.DestroyImmediate(chunk.GetComponent<Collider>());

        GameObject.DestroyImmediate(fluid.GetComponent<MeshFilter>());
        GameObject.DestroyImmediate(fluid.GetComponent<MeshRenderer>());
        //GameObject.DestroyImmediate(fluid.GetComponent<Collider>());

        DrawChunk();
    }

    void CombineQuads(GameObject g, Material m) {
        //1. Combine all children meshes
        MeshFilter[] meshFilters = g.GetComponentsInChildren<MeshFilter>();
        CombineInstance[] combine = new CombineInstance[meshFilters.Length];
        int i = 0;
        while (i < meshFilters.Length) {
            combine[i].mesh = meshFilters[i].sharedMesh;
            combine[i].transform = meshFilters[i].transform.localToWorldMatrix;
            i++;
        }

        //2. Create a new mesh on the parent object
        MeshFilter mf = (MeshFilter)g.gameObject.AddComponent(typeof(MeshFilter));
        mf.mesh = new Mesh();

        //3. Add combined meshes on children as the parent's mesh
        mf.mesh.CombineMeshes(combine);

        //4. Create a renderer for the parent
        MeshRenderer renderer = g.gameObject.AddComponent(typeof(MeshRenderer)) as MeshRenderer;
        renderer.material = m;

        //5. Delete all uncombined children
        foreach (Transform quad in g.transform) {
            GameObject.Destroy(quad.gameObject);
        }

    }

    string BuildChunkFileName(Vector3 v) {
        return Application.persistentDataPath + "/savedata/Chunk_" +
                                (int)v.x + "_" +
                                    (int)v.y + "_" +
                                        (int)v.z +
                                        "_" + World.chunkSize +
                                        "_" + World.radius +
                                        ".dat";
    }

    bool Load() //read data from file
    {
        string chunkFile = BuildChunkFileName(chunk.transform.position);
        if (File.Exists(chunkFile)) {
            BinaryFormatter bf = new BinaryFormatter();
            FileStream file = File.Open(chunkFile, FileMode.Open);
            bd = new BlockData();
            bd = (BlockData)bf.Deserialize(file);
            file.Close();
            return true;
        }
        return false;
    }

    public void Save() //write data to file
    {
        string chunkFile = BuildChunkFileName(chunk.transform.position);

        if (!File.Exists(chunkFile)) {
            Directory.CreateDirectory(Path.GetDirectoryName(chunkFile));
        }
        BinaryFormatter bf = new BinaryFormatter();
        FileStream file = File.Open(chunkFile, FileMode.OpenOrCreate);
        bd = new BlockData(chunkData);
        bf.Serialize(file, bd);
        file.Close();
    }

}
