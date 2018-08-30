using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class Noise {

	// method to generate a grid of values
	public static float[,] GenerateNoiseMap(int mapSize, int seed, float scale, int octaves, float persistance, float lacunarity, float compensator, Vector2 offset) {
		
		float[,] noiseMap = new float[mapSize, mapSize];

		System.Random prng = new System.Random (seed);
		Vector2[] octaveOffsets = new Vector2[octaves];

		float amplitude = 1;
		float frequency = 1;

		for (int i = 0; i < octaves; i++) {
			float offsetX = prng.Next (-100000, 100000) + offset.x;
			float offsetY = prng.Next (-100000, 100000) - offset.y;
			octaveOffsets [i] = new Vector2 (offsetX, offsetY);

			amplitude *= persistance;
		}

		if (scale < 0.001f) { scale = 0.001f; }		// avoid division by zero

		float halfSize = mapSize / 2f;

		for (int y = 0; y < mapSize; y++) {
			for (int x = 0; x < mapSize; x++) {

				amplitude = 1;
				frequency = 1;
				float noiseHeight = 0;

				for (int i = 0; i < octaves; i++) {

					float sampleX = (x - halfSize + octaveOffsets[i].x) / scale * frequency;
					float sampleY = (y - halfSize + octaveOffsets[i].y) / scale * frequency;

					float perlinValue = Mathf.PerlinNoise (sampleX, sampleY);

					noiseHeight += perlinValue * amplitude;
					amplitude *= persistance;
					frequency *= lacunarity;
				}

				noiseMap [x, y] = noiseHeight * compensator;

			}
		}

		return noiseMap;

	}

}
