using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WorldDecomposer : MonoBehaviour {
    private Vector3 targetPosition;
    private Vector3 mousePosition;
	private int [,] worldData;

	[SerializeField] private GameObject ObjectForPlace;
	private float nodeSize = 0.25f;
	private int terrainWidth = 80;
	private int terrainLength = 80;


	private int rows;
	private int cols;
	private List<RaycastHit> previousHits = new List<RaycastHit>();

    // Getters to use the world data in my movementfile (formerly kinematicarrive.cs)
    public int[,] GetWorldData() { return worldData; }
    public float GetNodeSize() { return nodeSize; }
    public int GetRows() { return rows; }
    public int GetCols() { return cols; } 

	private void Start () {

		terrainWidth = 80; // Plane size is 80x80
		terrainLength = 80;

		nodeSize = .4f; // using very small node size because my character is small

        rows = Mathf.FloorToInt(terrainWidth / nodeSize);
        cols = Mathf.FloorToInt(terrainLength / nodeSize);

		worldData = new int[rows, cols];

		DecomposeWorld ();
	}

    // Update is called once per frame
    void Update()
    {

    }
	void DecomposeWorld () {
		float startX = -40; 
		float startZ = -40;

		float nodeCenterOffset = nodeSize / 2f;


		for (int row = 0; row < rows; row++) {

			for (int col = 0; col < cols; col++) {

				float x = startX + nodeCenterOffset + (nodeSize * col);
				float z = startZ + nodeCenterOffset + (nodeSize * row);

				Vector3 startPos = new Vector3 (x, 3f, z);

				

				// Does our raycast hit anything at this point in the map
				RaycastHit hit;

				// Bit shift the index of the layer (11) to get a bit mask
				int layerMask = (1 << 11) | (1 << 14);

				// This would cast rays only against colliders in layer 11.
				// But instead we want to collide against everything except layer 11. The ~ operator does this, it inverts a bitmask.
				layerMask = ~layerMask;

				// Does the ray intersect any objects excluding the player layer
				if (Physics.Raycast (startPos, Vector3.down, out hit, Mathf.Infinity, layerMask)) {
					previousHits.Add(hit); 
					// print ("Hit something at row: " + row + " col: " + col); // for debugging
					Debug.DrawRay (startPos, Vector3.down * 3, Color.red, 50000);
					worldData [row, col] = 1;

				} else {
					Debug.DrawRay (startPos, Vector3.down * 3, Color.green, 50000);
					worldData [row, col] = 0;
				}
			}
		}

	}
}
