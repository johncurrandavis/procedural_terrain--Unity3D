using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Threading;

public class MapGenerator : MonoBehaviour {

	public enum DrawMode { NoiseMap, ColorMap, Mesh, FalloffMap };
	public DrawMode drawMode;

	public const int mapChunkSize = 239;	// 241 minus 2 for the border (normals at seams)
	[Range (0,6)] public int editorLOD;
	public float noiseScale;

	public int octaves;
	[Range (0,1)] public float persistance;
	public float lacunarity;
	[Range (0,1)] public float compensator;

	public int seed;
	public Vector2 offset;

	public bool useFalloff;

	public float meshHeightMultiplier;

	public bool autoUpdate;

	public TerrainType[] regions;

	float[,] falloffMap;

	Queue<ThreadInfo<MapData>> mapDataQueue = new Queue<ThreadInfo<MapData>>();
	Queue<ThreadInfo<MeshData>> meshDataQueue = new Queue<ThreadInfo<MeshData>>();

	void Awake() {

		falloffMap = FalloffGenerator.GenerateFalloffMap(mapChunkSize);

	}

	public void DrawMapInEditor() {

		MapData data = GenerateMapData(Vector2.zero);
		MapDisplay display = FindObjectOfType<MapDisplay> ();

		if      (drawMode == DrawMode.NoiseMap)   { display.DrawTexture (TextureGenerator.TextureFromHeightMap (data.heightMap)); }
		else if (drawMode == DrawMode.ColorMap)   { display.DrawTexture (TextureGenerator.TextureFromColourMap (data.colourMap, mapChunkSize)); }
		else if (drawMode == DrawMode.Mesh)       { display.DrawMesh    (MeshGenerator.GenerateTerrainMesh (data.heightMap, meshHeightMultiplier, editorLOD), TextureGenerator.TextureFromColourMap (data.colourMap, mapChunkSize)); }
		else if (drawMode == DrawMode.FalloffMap) { display.DrawTexture (TextureGenerator.TextureFromHeightMap (FalloffGenerator.GenerateFalloffMap(mapChunkSize))); }

	}

	// takes as parameters a location and an action
	public void RequestMapData(Vector2 centre, Action<MapData> callback) {

		print ("requesting map data at" + centre);
		ThreadStart threadStart = delegate { MapDataThread (centre, callback); };
		new Thread (threadStart).Start ();

	}

	// takes as parameters a location and an action
	void MapDataThread(Vector2 centre, Action<MapData> callback) {

		MapData mapData = GenerateMapData (centre);

		// can't call the OnMapDataReceived method directly because it
		// would be processed on this thread (not the main Unity thread)
		// so adds mapData and callback to mapDataThreadInfoQueue
		lock (mapDataQueue) { mapDataQueue.Enqueue (new ThreadInfo<MapData> (callback, mapData)); }

	}

	// takes as parameters map data, an LOD, and an action
	public void RequestMeshData(MapData mapData, int lod, Action<MeshData> callback) {

		print ("requesting mesh data with lod" + lod);
		ThreadStart threadStart = delegate { MeshDataThread (mapData, lod, callback); };
		new Thread (threadStart).Start ();

	}

	// takes as parameters map data, an LOD, and an action
	void MeshDataThread(MapData mapData, int lod, Action<MeshData> callback) {

		MeshData meshData = MeshGenerator.GenerateTerrainMesh (mapData.heightMap, meshHeightMultiplier, lod);
		lock (meshDataQueue) { meshDataQueue.Enqueue (new ThreadInfo<MeshData> (callback, meshData)); }

	}

	void Update() {

		print ("update 1 -- map data thread info queue: " + mapDataQueue.Count);

		if (mapDataQueue.Count > 0) {
			for (int i = 0; i < mapDataQueue.Count; i++) {

				print (mapDataQueue.Count);

				ThreadInfo<MapData> threadInfo = mapDataQueue.Dequeue ();
				threadInfo.callback (threadInfo.parameter);
			}
		}

		print ("update 2 -- mesh data thread info queue: " + meshDataQueue.Count);

		if (meshDataQueue.Count > 0) {
			for (int i = 0; i < meshDataQueue.Count; i++) {

				print (meshDataQueue.Count);

				ThreadInfo<MeshData> threadInfo = meshDataQueue.Dequeue ();
				threadInfo.callback (threadInfo.parameter);
			}
		}

	}

	MapData GenerateMapData(Vector2 centre) {

		float[,] map = Noise.GenerateNoiseMap (mapChunkSize + 2, seed, noiseScale, octaves, persistance, lacunarity, compensator, centre + offset);
 
		Color[] colours = new Color [(mapChunkSize * mapChunkSize)];	// my round brackets

		for (int y = 0; y < mapChunkSize; y++) {
			for (int x = 0; x < mapChunkSize; x++) {

				if (useFalloff) {
					map[x, y] = Mathf.Clamp01(map[x, y] - falloffMap[x, y]);
				}

				float height = map [x, y];

				for (int i = 0; i < regions.Length; i++) {
					if (height >= regions [i].height) { colours [y * mapChunkSize + x] = regions [i].colour; }
					else { break; }
				}

			}
		}

		return new MapData (map, colours);
	}

	void OnValidate() {

		if (lacunarity < 1) { lacunarity = 1; }
		if (octaves < 0) { octaves = 0; }

		falloffMap = FalloffGenerator.GenerateFalloffMap (mapChunkSize) ;

	}

	// uses generic type parameter
	// handles both map data and mesh data
	struct ThreadInfo<T> {

		public readonly Action<T> callback;
		public readonly T parameter;

		// constructor
		public ThreadInfo (Action<T> callback, T parameter)
		{
			this.callback = callback;
			this.parameter = parameter;
		}

	}
	
}

[System.Serializable]		// The Serializable attribute lets you embed a class with sub properties
							// in the inspector. You can use this to display variables in the inspector
							// similar to how a Vector3 shows up in the inspector. The name and a triangle
							// to expand its properties. To do this you need create a class that derives from
							// System.Object and give it the Serializable attribute.
public struct TerrainType {
	public string name;		// not readonly as they
	public float height;	// then would not appear
	public Color colour;	// in the editor
}

public struct MapData {						// at some point rename to TerrainData
	public readonly float[,] heightMap;
	public readonly Color[] colourMap;

	// constructor
	public MapData (float[,] heightMap, Color[] colourMap)
	{
		this.heightMap = heightMap;
		this.colourMap = colourMap;
	}

}
