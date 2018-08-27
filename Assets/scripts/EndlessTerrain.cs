using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EndlessTerrain : MonoBehaviour {

	const float scale = 3f;

	const float moveThresholdForUpdate = 25f;
	const float sqrMoveThresholdForUpdate = moveThresholdForUpdate * moveThresholdForUpdate;	// to avoid sqrt operation

	public LODInfo[] detailLevels;
	public static float maxViewDistance;

	public Transform viewer;
	public Material mapMaterial;

	public static Vector2 viewerPosition;
	Vector2 viewerPositionOld;
	static MapGenerator mapGenerator;
	int chunkSize;
	int chunksVisibleInDistance;

	Dictionary<Vector2, TerrainChunk> terrainChunkDictionary = new Dictionary<Vector2, TerrainChunk>();
	static List <TerrainChunk> chunksVisibleLastUpdate = new List <TerrainChunk>();

	void Start() {

		mapGenerator = FindObjectOfType<MapGenerator>();

		maxViewDistance = detailLevels [detailLevels.Length - 1].visibleDistanceThreshold;

		chunkSize = MapGenerator.mapChunkSize - 1;
		chunksVisibleInDistance = Mathf.RoundToInt (maxViewDistance / chunkSize);
		print("chunks visible: " + chunksVisibleInDistance);

		updateVisibleChunks ();

	}

	void Update() {

		viewerPosition = new Vector2 (viewer.position.x, viewer.position.z) / scale;

		if ((viewerPositionOld - viewerPosition).sqrMagnitude > sqrMoveThresholdForUpdate) {
			viewerPositionOld = viewerPosition;
			updateVisibleChunks ();
		}

	}

	void updateVisibleChunks () {

		for (int i = 0; i < chunksVisibleLastUpdate.Count; i++) { chunksVisibleLastUpdate [i].SetVisible (false); }
		chunksVisibleLastUpdate.Clear ();

		int currentCoordX = Mathf.RoundToInt (viewerPosition.x / chunkSize);
		int currentCoordY = Mathf.RoundToInt (viewerPosition.y / chunkSize);

		for (int yOffset = -chunksVisibleInDistance; yOffset <= chunksVisibleInDistance; yOffset++) {
			for (int xOffset = -chunksVisibleInDistance; xOffset <= chunksVisibleInDistance; xOffset++) {

				Vector2 viewedChunkCoord = new Vector2 (currentCoordX + xOffset, currentCoordY + yOffset);

				if (terrainChunkDictionary.ContainsKey (viewedChunkCoord)) { terrainChunkDictionary [viewedChunkCoord].UpdateTerrainChunk (); }
				else { terrainChunkDictionary.Add (viewedChunkCoord, new TerrainChunk (viewedChunkCoord, chunkSize, detailLevels, transform, mapMaterial)); }

			}
		}

	}

	public class TerrainChunk {

		GameObject meshObject;
		Vector2 position;
		Bounds bounds;

		MapData mapData;

		MeshRenderer meshRenderer;
		MeshFilter meshFilter;

		LODInfo[] detailLevels;
		LODMesh[] lodMeshes;

		MapData mapdata;
		bool mapDataReceived;
		int previousLODIndex = -1;	// not zero the first time around, and so must update

		public TerrainChunk(Vector2 coord, int size, LODInfo[] detailLevels, Transform parent, Material material) {

			this.detailLevels = detailLevels;

			position = coord * size;
			bounds = new Bounds(position, Vector2.one * size);
			Vector3 positionV3 = new Vector3 (position.x, 0, position.y);

			meshObject = new GameObject ("Terrain Chunk");
			meshRenderer = meshObject.AddComponent<MeshRenderer>();
			meshFilter = meshObject.AddComponent<MeshFilter>();
			meshRenderer.material = material;

			meshObject.transform.position = positionV3 * scale;
			meshObject.transform.parent = parent;
			meshObject.transform.localScale = Vector3.one * scale;

			SetVisible (false);		// default state

			lodMeshes = new LODMesh [detailLevels.Length];
			for (int i = 0; i < detailLevels.Length; i++) {
				lodMeshes[i] = new LODMesh(detailLevels[i].lod, UpdateTerrainChunk);
			}

			mapGenerator.RequestMapData (position, OnMapDataReceived);	// the OnMapDataReceived method is the callback variable
																		// referred to by RequestMapData(Action<MapData> callback)
		}

		void OnMapDataReceived(MapData mapData) {
			this.mapData = mapData;
			mapDataReceived = true;

			Texture2D texture = TextureGenerator.TextureFromColourMap (mapData.colourMap, MapGenerator.mapChunkSize);
			meshRenderer.material.mainTexture = texture;

			UpdateTerrainChunk ();
		}

		public void UpdateTerrainChunk() {

			if (mapDataReceived) {

				float viewerDistanceFromNearestEdge = Mathf.Sqrt (bounds.SqrDistance (viewerPosition));
				bool visible = viewerDistanceFromNearestEdge <= maxViewDistance;

				if (visible) {
					int lodIndex = 0;

					for (int i=0; i < detailLevels.Length - 1; i++) {
						if (viewerDistanceFromNearestEdge > detailLevels[i].visibleDistanceThreshold) { lodIndex = i + 1; }
						else { break; }		// not visible (viewerDistanceFromNearestEdge > maxViewDistance)
					}

					if (lodIndex != previousLODIndex) {		// if LOD is to be changed
						LODMesh lodMesh = lodMeshes [lodIndex];
						if (lodMesh.hasMesh) { previousLODIndex = lodIndex; meshFilter.mesh = lodMesh.mesh; }
						else if (!lodMesh.hasRequestedMesh) { lodMesh.RequestMesh (mapData); }
					}

					chunksVisibleLastUpdate.Add (this);

				}

				SetVisible (visible);
			}

		}

		public void SetVisible(bool visible) {
			meshObject.SetActive (visible);
		}

		public bool isVisible() {
			return meshObject.activeSelf;
		}

	}

	class LODMesh {

		public Mesh mesh;
		public bool hasRequestedMesh;
		public bool hasMesh;
		int lod;
		System.Action updateCallback;

		public LODMesh(int lod, System.Action updateCallback) {

			this.lod = lod;
			this.updateCallback = updateCallback;

		}

		void OnMeshDataReceived(MeshData meshData) {
			mesh = meshData.CreateMesh ();
			hasMesh = true;

			updateCallback ();

		}

		public void RequestMesh(MapData mapData) {
			
			hasRequestedMesh = true;
			mapGenerator.RequestMeshData(mapData, lod, OnMeshDataReceived);

		}

	}

	[System.Serializable]
	public struct LODInfo {
		public int lod;
		public float visibleDistanceThreshold;
	}

}
