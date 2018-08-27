using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class TextureGenerator {

	public static Texture2D TextureFromColourMap(Color[] colourMap, int size) {
		
		Texture2D texture = new Texture2D (size, size);

		texture.filterMode = FilterMode.Point;
		texture.wrapMode = TextureWrapMode.Clamp;

		texture.SetPixels (colourMap);
		texture.Apply ();

		return texture;

	}

	public static Texture2D TextureFromHeightMap(float[,] heightMap) {
		
		int size = heightMap.GetLength (0);		// square, so only one dimension needed

		// one dimensional array to hold colour values
		Color[] colourMap = new Color[size * size];

		for (int y = 0; y < size; y++) {
			for (int x = 0; x < size; x++) {

				colourMap [y * size + x] = Color.Lerp (Color.black, Color.white, heightMap [x, y]);
				
			}
		}

		return TextureFromColourMap (colourMap, size);

	}

}
