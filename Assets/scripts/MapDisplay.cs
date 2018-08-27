using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MapDisplay : MonoBehaviour {

	public Renderer textureRender;         // https://docs.unity3d.com/ScriptReference/Renderer.html
	public MeshFilter meshFilter;          // https://docs.unity3d.com/ScriptReference/MeshFilter.html
                                           // https://docs.unity3d.com/Manual/class-MeshFilter.html
                                           // The Mesh Filter takes a mesh from your assets and passes it
                                           // to the Mesh Renderer for rendering on the screen.
	public MeshRenderer meshRenderer;

	public void DrawTexture(Texture2D texture) {
		
		textureRender.sharedMaterial.mainTexture = texture;       // https://docs.unity3d.com/ScriptReference/Renderer-sharedMaterial.html
		textureRender.transform.localScale = new Vector3 (texture.width, 1, texture.height);

	}

	public void DrawMesh (MeshData meshData, Texture2D texture) {

		meshFilter.sharedMesh = meshData.CreateMesh ();		// shared because mesh might be
		meshRenderer.sharedMaterial.mainTexture = texture;	// generated outside game mode

	}

}
