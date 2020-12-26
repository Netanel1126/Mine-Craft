using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Block {

	enum Cubeside {BOTTOM, TOP, LEFT, RIGHT, FRONT, BACK};
	public enum BlockType { GRASS, DIRT, WATER, STONE, LEAVES, WOOD, WOODBASE, SAND, GOLD, BEDROCK, REDSTONE,
							DIAMOND, NOCRACK, CRACK1, CRACK2, CRACK3, CRACK4, AIR
	};
	public bool isSolid;

	public BlockType bType;
	GameObject parent;
	public Vector3 position;
	public Chunk owner;

	public BlockType health = BlockType.NOCRACK;
	public int currentHealth;
	int[] blockHealthMax = { 3, 3, 10, 4, 2, 4, 4, 2, 3, -1, 4, 4, 0, 0, 0, 0, 0, 0 };

	Vector2[,] blockUVs = { 
		/*GRASS TOP*/		{new Vector2( 0.125f, 0.375f ), new Vector2( 0.1875f, 0.375f),
								new Vector2( 0.125f, 0.4375f ),new Vector2( 0.1875f, 0.4375f )},
		/*GRASS SIDE*/		{new Vector2( 0.1875f, 0.9375f ), new Vector2( 0.25f, 0.9375f),
								new Vector2( 0.1875f, 1.0f ),new Vector2( 0.25f, 1.0f )},
		/*DIRT*/			{new Vector2( 0.125f, 0.9375f ), new Vector2( 0.1875f, 0.9375f),
								new Vector2( 0.125f, 1.0f ),new Vector2( 0.1875f, 1.0f )},
		/*WATER*/			{ new Vector2(0.875f,0.125f),  new Vector2(0.9375f,0.125f),
 								new Vector2(0.875f,0.1875f), new Vector2(0.9375f,0.1875f)},
		/*STONE*/			{new Vector2( 0, 0.875f ), new Vector2( 0.0625f, 0.875f),
								new Vector2( 0, 0.9375f ),new Vector2( 0.0625f, 0.9375f )},
		/*LEAVES*/			{ new Vector2(0.0625f,0.375f),  new Vector2(0.125f,0.375f),
 								new Vector2(0.0625f,0.4375f), new Vector2(0.125f,0.4375f)},
 		/*WOOD*/			{ new Vector2(0.375f,0.625f),  new Vector2(0.4375f,0.625f),
 								new Vector2(0.375f,0.6875f), new Vector2(0.4375f,0.6875f)},
 		/*WOODBASE*/		{ new Vector2(0.375f,0.625f),  new Vector2(0.4375f,0.625f),
 								new Vector2(0.375f,0.6875f), new Vector2(0.4375f,0.6875f)},	    
		/*SAND*/			{ new Vector2(0.125f,0.875f),  new Vector2(0.1875f,0.875f),
 								new Vector2(0.125f,0.9375f), new Vector2(0.1875f,0.9375f)},
 		/*GOLD*/			{ new Vector2(0f,0.8125f),  new Vector2(0.0625f,0.8125f),
 								new Vector2(0f,0.875f), new Vector2(0.0625f,0.875f)},
		/*BEDROCK*/			{new Vector2( 0.3125f, 0.8125f ), new Vector2( 0.375f, 0.8125f),
								new Vector2( 0.3125f, 0.875f ),new Vector2( 0.375f, 0.875f )},
		/*REDSTONE*/		{new Vector2( 0.1875f, 0.75f ), new Vector2( 0.25f, 0.75f),
								new Vector2( 0.1875f, 0.8125f ),new Vector2( 0.25f, 0.8125f )},
		/*DIAMOND*/			{new Vector2( 0.125f, 0.75f ), new Vector2( 0.1875f, 0.75f),
								new Vector2( 0.125f, 0.8125f ),new Vector2( 0.1875f, 0.8125f )},
		/*NOCRACK*/			{new Vector2( 0.6875f, 0f ), new Vector2( 0.75f, 0f),
								new Vector2( 0.6875f, 0.0625f ),new Vector2( 0.75f, 0.0625f )},
		/*CRACK1*/			{ new Vector2(0f,0f),  new Vector2(0.0625f,0f),
 								new Vector2(0f,0.0625f), new Vector2(0.0625f,0.0625f)},
 		/*CRACK2*/			{ new Vector2(0.0625f,0f),  new Vector2(0.125f,0f),
 								new Vector2(0.0625f,0.0625f), new Vector2(0.125f,0.0625f)},
 		/*CRACK3*/			{ new Vector2(0.125f,0f),  new Vector2(0.1875f,0f),
 								new Vector2(0.125f,0.0625f), new Vector2(0.1875f,0.0625f)},
 		/*CRACK4*/			{ new Vector2(0.1875f,0f),  new Vector2(0.25f,0f),
 								new Vector2(0.1875f,0.0625f), new Vector2(0.25f,0.0625f)}
						};


	public Block(BlockType b, Vector3 pos, Chunk o)
	{
		position = pos;
		owner = o;
		SetType(b);
	}

	public bool BuildBlock(BlockType b) {
		if (b == BlockType.WATER) {
			ChunkMB mb = owner.mb;
			int strength = this.blockHealthMax[(int)b];
			mb.StartCoroutine(mb.Flow(this, BlockType.WATER, strength, 5));
		} else if (b == BlockType.SAND) {
			ChunkMB mb = owner.mb;
			mb.StartCoroutine(mb.Drop(this, BlockType.SAND, 20));
		} else {
			SetType(b);
			owner.Redraw();
		}
		return true;
	}

	public void SetType(BlockType b) {
		this.bType = b;
		this.isSolid = bType != BlockType.AIR && bType != BlockType.WATER;
		this.parent = bType != BlockType.WATER ? owner.chunk.gameObject : owner.fluid.gameObject;

		health = BlockType.NOCRACK;
		currentHealth = blockHealthMax[(int)bType];
    }

	public bool HitBlock() {
		if (currentHealth == -1) return false;
		currentHealth--;
		health++;

		if (currentHealth == (blockHealthMax[(int)bType] - 1)) {
			owner.mb.StartCoroutine(owner.mb.HealBlock(position));
		}

		if (currentHealth <= 0) {
			bType = BlockType.AIR;
			isSolid = false;
			health = BlockType.AIR;
			owner.Redraw();
			owner.UpdateChunk();
			return true;
        }
		owner.Redraw();
		return false;
	}

	public void Reset() {
		health = BlockType.NOCRACK;
		currentHealth = blockHealthMax[(int)bType];
		owner.Redraw();
	}

	void CreateQuad(Cubeside side)
	{
		Mesh mesh = new Mesh();
	    mesh.name = "ScriptedMesh" + side.ToString(); 

		Vector3[] vertices = new Vector3[4];
		Vector3[] normals = new Vector3[4];
		Vector2[] uvs = new Vector2[4];
        List<Vector2> suvs = new List<Vector2>();
        int[] triangles = new int[6];

		//all possible UVs
		Vector2 uv00;
		Vector2 uv10;
		Vector2 uv01;
		Vector2 uv11;

		if(bType == BlockType.GRASS && side == Cubeside.TOP)
		{
			uv00 = blockUVs[0,0];
			uv10 = blockUVs[0,1];
			uv01 = blockUVs[0,2];
			uv11 = blockUVs[0,3];
		}
		else if(bType == BlockType.GRASS && side == Cubeside.BOTTOM)
		{
			uv00 = blockUVs[(int)(BlockType.DIRT+1),0];
			uv10 = blockUVs[(int)(BlockType.DIRT+1),1];
			uv01 = blockUVs[(int)(BlockType.DIRT+1),2];
			uv11 = blockUVs[(int)(BlockType.DIRT+1),3];
		}
		else
		{
			uv00 = blockUVs[(int)(bType+1),0];
			uv10 = blockUVs[(int)(bType+1),1];
			uv01 = blockUVs[(int)(bType+1),2];
			uv11 = blockUVs[(int)(bType+1),3];
		}

        //set cracks
        suvs.Add(blockUVs[(int)(health + 1), 3]);
        suvs.Add(blockUVs[(int)(health + 1), 2]);
        suvs.Add(blockUVs[(int)(health + 1), 0]);
        suvs.Add(blockUVs[(int)(health + 1), 1]);

        //all possible vertices 
        Vector3 p0 = new Vector3( -0.5f,  -0.5f,  0.5f );
		Vector3 p1 = new Vector3(  0.5f,  -0.5f,  0.5f );
		Vector3 p2 = new Vector3(  0.5f,  -0.5f, -0.5f );
		Vector3 p3 = new Vector3( -0.5f,  -0.5f, -0.5f );		 
		Vector3 p4 = new Vector3( -0.5f,   0.5f,  0.5f );
		Vector3 p5 = new Vector3(  0.5f,   0.5f,  0.5f );
		Vector3 p6 = new Vector3(  0.5f,   0.5f, -0.5f );
		Vector3 p7 = new Vector3( -0.5f,   0.5f, -0.5f );

		switch(side)
		{
			case Cubeside.BOTTOM:
				vertices = new Vector3[] {p0, p1, p2, p3};
				normals = new Vector3[] {Vector3.down, Vector3.down, 
											Vector3.down, Vector3.down};
				uvs = new Vector2[] {uv11, uv01, uv00, uv10};
				triangles = new int[] { 3, 1, 0, 3, 2, 1};
			break;
			case Cubeside.TOP:
				vertices = new Vector3[] {p7, p6, p5, p4};
				normals = new Vector3[] {Vector3.up, Vector3.up, 
											Vector3.up, Vector3.up};
				uvs = new Vector2[] {uv11, uv01, uv00, uv10};
				triangles = new int[] {3, 1, 0, 3, 2, 1};
			break;
			case Cubeside.LEFT:
				vertices = new Vector3[] {p7, p4, p0, p3};
				normals = new Vector3[] {Vector3.left, Vector3.left, 
											Vector3.left, Vector3.left};
				uvs = new Vector2[] {uv11, uv01, uv00, uv10};
				triangles = new int[] {3, 1, 0, 3, 2, 1};
			break;
			case Cubeside.RIGHT:
				vertices = new Vector3[] {p5, p6, p2, p1};
				normals = new Vector3[] {Vector3.right, Vector3.right, 
											Vector3.right, Vector3.right};
				uvs = new Vector2[] {uv11, uv01, uv00, uv10};
				triangles = new int[] {3, 1, 0, 3, 2, 1};
			break;
			case Cubeside.FRONT:
				vertices = new Vector3[] {p4, p5, p1, p0};
				normals = new Vector3[] {Vector3.forward, Vector3.forward, 
											Vector3.forward, Vector3.forward};
				uvs = new Vector2[] {uv11, uv01, uv00, uv10};
				triangles = new int[] {3, 1, 0, 3, 2, 1};
			break;
			case Cubeside.BACK:
				vertices = new Vector3[] {p6, p7, p3, p2};
				normals = new Vector3[] {Vector3.back, Vector3.back, 
											Vector3.back, Vector3.back};
				uvs = new Vector2[] {uv11, uv01, uv00, uv10};
				triangles = new int[] {3, 1, 0, 3, 2, 1};
			break;
		}

		mesh.vertices = vertices;
		mesh.normals = normals;
		mesh.uv = uvs;
        mesh.SetUVs(1, suvs);
        mesh.triangles = triangles;
		 
		mesh.RecalculateBounds();
		
		GameObject quad = new GameObject("Quad");
		quad.transform.position = position;
	    quad.transform.parent = parent.transform;

     	MeshFilter meshFilter = (MeshFilter) quad.AddComponent(typeof(MeshFilter));
		meshFilter.mesh = mesh;

		//MeshRenderer renderer = quad.AddComponent(typeof(MeshRenderer)) as MeshRenderer;
	}

	public bool HasSolidNeighbour(int x, int y, int z) {

		try {
			Block b = GetBlock(x, y, z);
			if (b != null) {
				return b.isSolid || b.bType == bType;
			}
		} catch (System.IndexOutOfRangeException) { 
		}
		return false;
	}

	int ConvertBlockIndexToLocal(int i) {
		if (i <= -1)
			i = World.chunkSize + i;
		else if (i >= World.chunkSize)
			i = i - World.chunkSize;
		return i;
	}

	public void Draw()
	{
		if(bType == BlockType.AIR) {
			return;
        }
		if (!HasSolidNeighbour((int)position.x, (int)position.y, (int)position.z + 1))
			CreateQuad(Cubeside.FRONT);
		if (!HasSolidNeighbour((int)position.x, (int)position.y, (int)position.z - 1))
			CreateQuad(Cubeside.BACK);
		if (!HasSolidNeighbour((int)position.x, (int)position.y + 1, (int)position.z))
			CreateQuad(Cubeside.TOP);
		if (!HasSolidNeighbour((int)position.x, (int)position.y - 1, (int)position.z))
			CreateQuad(Cubeside.BOTTOM);
		if (!HasSolidNeighbour((int)position.x - 1, (int)position.y, (int)position.z))
			CreateQuad(Cubeside.LEFT);
		if (!HasSolidNeighbour((int)position.x + 1, (int)position.y, (int)position.z))
			CreateQuad(Cubeside.RIGHT);
	}

	public Block GetBlock(int x, int y, int z) {
		Block[,,] chunks;

		if (x < 0 || x >= World.chunkSize
		 || y < 0 || y >= World.chunkSize
		 || z < 0 || z >= World.chunkSize) {
			//Block is in neighboring chunk
			int newX = 0, newY = 0, newZ = 0;

			if (x < 0 || x >= World.chunkSize) {
				newX = (x - (int)position.x) * World.chunkSize;
			}
			if (y < 0 || y >= World.chunkSize) {
				newY = (y - (int)position.y) * World.chunkSize;
			}
			if (z < 0 || z >= World.chunkSize) {
				newZ = (z - (int)position.z) * World.chunkSize;
			}
			Vector3 neighbourChunkPos = this.parent.transform.position +
										   new Vector3(newX, newY, newZ);


			string nName = World.BuildChunkName(neighbourChunkPos);

			x = ConvertBlockIndexToLocal(x);
			y = ConvertBlockIndexToLocal(y);
			z = ConvertBlockIndexToLocal(z);

			Chunk nChunk;
			if (World.chunks.TryGetValue(nName, out nChunk)) {
				chunks = nChunk.chunkData;
			} else {
				return null;
			}
		} else {
			chunks = owner.chunkData;
		}

		return chunks[x, y, z];
	}
}
