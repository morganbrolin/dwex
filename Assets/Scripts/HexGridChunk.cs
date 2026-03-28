using UnityEngine;

/// <summary>
/// Component that manages a single chunk of <see cref="HexGrid"/>.
/// </summary>
public class HexGridChunk : MonoBehaviour
{
	readonly static Color weights1 = new(1f, 0f, 0f);
	readonly static Color weights2 = new(0f, 1f, 0f);
	readonly static Color weights3 = new(0f, 0f, 1f);

	public HexGrid Grid
	{ get; set; }

	[SerializeField]
	HexMesh terrain,  estuaries;

	[SerializeField]
	HexFeatureManager features;

	int[] cellIndices;

	Canvas gridCanvas;

	void Awake()
	{
		gridCanvas = GetComponentInChildren<Canvas>();
		cellIndices = new int[HexMetrics.chunkSizeX * HexMetrics.chunkSizeZ];
	}

	/// <summary>
	/// Add a cell to the chunk.
	/// </summary>
	/// <param name="index">Index of the cell for the chunk.</param>
	/// <param name="cellIndex">Index of the cell to add.</param>
	/// <param name="cellUI">UI root transform of the cell.</param>
	public void AddCell(int index, int cellIndex, RectTransform cellUI)
	{
		cellIndices[index] = cellIndex;
		cellUI.SetParent(gridCanvas.transform, false);
	}

	/// <summary>
	/// Refresh the chunk.
	/// </summary>
	public void Refresh() => enabled = true;

	/// <summary>
	/// Control whether the map UI is visibile or hidden for the chunk.
	/// </summary>
	/// <param name="visible">Whether the UI should be visible.</param>
	public void ShowUI(bool visible) =>
		gridCanvas.gameObject.SetActive(visible);

	void LateUpdate()
	{
		Triangulate();
		enabled = false;
	}

	/// <summary>
	/// Triangulate everything in the chunk.
	/// </summary>
	public void Triangulate()
	{
		terrain.Clear();
		estuaries.Clear();
		features.Clear();
		for (int i = 0; i < cellIndices.Length; i++)
		{
			Triangulate(cellIndices[i]);
		}
		terrain.Apply();
		estuaries.Apply();
		features.Apply();
	}

	void Triangulate(int cellIndex)
	{
		HexCellData cell = Grid.CellData[cellIndex];
		Vector3 cellPosition = Grid.CellPositions[cellIndex];
		for (HexDirection d = HexDirection.NE; d <= HexDirection.NW; d++)
		{
			Triangulate(d, cell, cellIndex, cellPosition);
		}
	}

	void Triangulate(
		HexDirection direction,
		HexCellData cell,
		int cellIndex,
		Vector3 center)
	{
		var e = new EdgeVertices(
			center + HexMetrics.GetFirstSolidCorner(direction),
			center + HexMetrics.GetSecondSolidCorner(direction));

		
		TriangulateWithoutRiver(direction, cell, cellIndex, center, e);
		{
			features.AddFeature(
				cell, (center + e.v1 + e.v5) * (1f / 3f));
		}
	

		if (direction <= HexDirection.SE)
		{
			TriangulateConnection(direction, cell, cellIndex, center.y, e);
		}


	}
	
	void TriangulateWithoutRiver(
		HexDirection direction,
		HexCellData cell,
		int cellIndex,
		Vector3 center,
		EdgeVertices e)
	{
		TriangulateEdgeFan(center, e, cellIndex);
	}

	void TriangulateConnection(
		HexDirection direction,
		HexCellData cell,
		int cellIndex,
		float centerY,
		EdgeVertices e1)
	{
		if (!Grid.TryGetCellIndex(
			cell.coordinates.Step(direction), out int neighborIndex))
		{
			return;
		}
		HexCellData neighbor = Grid.CellData[neighborIndex];
		Vector3 bridge = HexMetrics.GetBridge(direction);
		bridge.y = Grid.CellPositions[neighborIndex].y - centerY;
		var e2 = new EdgeVertices(e1.v1 + bridge, e1.v5 + bridge);
		TriangulateEdgeStrip(
			e1, weights1, cellIndex,
			e2, weights2, neighborIndex);
		if (direction <= HexDirection.E &&
			Grid.TryGetCellIndex(
				cell.coordinates.Step(direction.Next()),
				out int nextNeighborIndex))
		{
			HexCellData nextNeighbor = Grid.CellData[nextNeighborIndex];
			Vector3 v5 = e1.v5 + HexMetrics.GetBridge(direction.Next());
			v5.y = Grid.CellPositions[nextNeighborIndex].y;

			if (cell.Elevation <= neighbor.Elevation)
			{
				if (cell.Elevation <= nextNeighbor.Elevation)
				{
					TriangulateCorner(
						e1.v5, cellIndex, cell,
						e2.v5, neighborIndex, neighbor,
						v5, nextNeighborIndex, nextNeighbor);
				}
				else
				{
					TriangulateCorner(
						v5, nextNeighborIndex, nextNeighbor,
						e1.v5, cellIndex, cell,
						e2.v5, neighborIndex, neighbor);
				}
			}
			else if (neighbor.Elevation <= nextNeighbor.Elevation)
			{
				TriangulateCorner(
					e2.v5, neighborIndex, neighbor,
					v5, nextNeighborIndex, nextNeighbor,
					e1.v5, cellIndex, cell);
			}
			else {
				TriangulateCorner(
					v5, nextNeighborIndex, nextNeighbor,
					e1.v5, cellIndex, cell,
					e2.v5, neighborIndex, neighbor);
			}
		}
	}
	

	void TriangulateCorner(
		Vector3 bottom, int bottomCellIndex, HexCellData bottomCell,
		Vector3 left, int leftCellIndex, HexCellData leftCell,
		Vector3 right, int rightCellIndex, HexCellData rightCell)
	{
		HexEdgeType leftEdgeType = bottomCell.GetEdgeType(leftCell);
		HexEdgeType rightEdgeType = bottomCell.GetEdgeType(rightCell);

		if (leftEdgeType == HexEdgeType.Slope)
		{
			if (rightEdgeType == HexEdgeType.Slope)
			{
				TriangulateCornerTerraces(
					bottom, bottomCellIndex,
					left, leftCellIndex,
					right, rightCellIndex);
			}
			else if (rightEdgeType == HexEdgeType.Flat)
			{
				TriangulateCornerTerraces(
					left, leftCellIndex,
					right, rightCellIndex,
					bottom, bottomCellIndex);
			}
			else
			{
				TriangulateCornerTerracesCliff(
					bottom, bottomCellIndex, bottomCell,
					left, leftCellIndex, leftCell,
					right, rightCellIndex, rightCell);
			}
		}
		else if (rightEdgeType == HexEdgeType.Slope)
		{
			if (leftEdgeType == HexEdgeType.Flat)
			{
				TriangulateCornerTerraces(
					right, rightCellIndex,
					bottom, bottomCellIndex,
					left, leftCellIndex);
			}
			else
			{
				TriangulateCornerCliffTerraces(
					bottom, bottomCellIndex, bottomCell,
					left, leftCellIndex, leftCell,
					right, rightCellIndex, rightCell);
			}
		}
		else if (leftCell.GetEdgeType(rightCell) == HexEdgeType.Slope)
		{
			if (leftCell.Elevation < rightCell.Elevation)
			{
				TriangulateCornerCliffTerraces(
					right, rightCellIndex, rightCell,
					bottom, bottomCellIndex, bottomCell,
					left, leftCellIndex, leftCell);
			}
			else
			{
				TriangulateCornerTerracesCliff(
					left, leftCellIndex, leftCell,
					right, rightCellIndex, rightCell,
					bottom, bottomCellIndex, bottomCell);
			}
		}
		else
		{
			terrain.AddTriangle(bottom, left, right);
			Vector3 indices;
			indices.x = bottomCellIndex;
			indices.y = leftCellIndex;
			indices.z = rightCellIndex;
			terrain.AddTriangleCellData(indices, weights1, weights2, weights3);
		}

	}

	void TriangulateEdgeTerraces(
		EdgeVertices begin, int beginCellIndex,
		EdgeVertices end, int endCellIndex,
		bool hasRoad)
	{
		EdgeVertices e2 = EdgeVertices.TerraceLerp(begin, end, 1);
		Color w2 = HexMetrics.TerraceLerp(weights1, weights2, 1);
		float i1 = beginCellIndex;
		float i2 = endCellIndex;

		TriangulateEdgeStrip(begin, weights1, i1, e2, w2, i2, hasRoad);

		for (int i = 2; i < HexMetrics.terraceSteps; i++)
		{
			EdgeVertices e1 = e2;
			Color w1 = w2;
			e2 = EdgeVertices.TerraceLerp(begin, end, i);
			w2 = HexMetrics.TerraceLerp(weights1, weights2, i);
			TriangulateEdgeStrip(e1, w1, i1, e2, w2, i2, hasRoad);
		}

		TriangulateEdgeStrip(e2, w2, i1, end, weights2, i2, hasRoad);
	}

	void TriangulateCornerTerraces(
		Vector3 begin, int beginCellIndex,
		Vector3 left, int leftCellIndex,
		Vector3 right, int rightCellIndex)
	{
		Vector3 v3 = HexMetrics.TerraceLerp(begin, left, 1);
		Vector3 v4 = HexMetrics.TerraceLerp(begin, right, 1);
		Color w3 = HexMetrics.TerraceLerp(weights1, weights2, 1);
		Color w4 = HexMetrics.TerraceLerp(weights1, weights3, 1);
		Vector3 indices;
		indices.x = beginCellIndex;
		indices.y = leftCellIndex;
		indices.z = rightCellIndex;

		terrain.AddTriangle(begin, v3, v4);
		terrain.AddTriangleCellData(indices, weights1, w3, w4);

		for (int i = 2; i < HexMetrics.terraceSteps; i++)
		{
			Vector3 v1 = v3;
			Vector3 v2 = v4;
			Color w1 = w3;
			Color w2 = w4;
			v3 = HexMetrics.TerraceLerp(begin, left, i);
			v4 = HexMetrics.TerraceLerp(begin, right, i);
			w3 = HexMetrics.TerraceLerp(weights1, weights2, i);
			w4 = HexMetrics.TerraceLerp(weights1, weights3, i);
			terrain.AddQuad(v1, v2, v3, v4);
			terrain.AddQuadCellData(indices, w1, w2, w3, w4);
		}

		terrain.AddQuad(v3, v4, left, right);
		terrain.AddQuadCellData(indices, w3, w4, weights2, weights3);
	}

	void TriangulateCornerTerracesCliff(
		Vector3 begin, int beginCellIndex, HexCellData beginCell,
		Vector3 left, int leftCellIndex, HexCellData leftCell,
		Vector3 right, int rightCellIndex, HexCellData rightCell)
	{
		float b = 1f / (rightCell.Elevation - beginCell.Elevation);
		if (b < 0)
		{
			b = -b;
		}
		Vector3 boundary = Vector3.Lerp(
			HexMetrics.Perturb(begin), HexMetrics.Perturb(right), b);
		Color boundaryWeights = Color.Lerp(weights1, weights3, b);
		Vector3 indices;
		indices.x = beginCellIndex;
		indices.y = leftCellIndex;
		indices.z = rightCellIndex;

		TriangulateBoundaryTriangle(
			begin, weights1, left, weights2,
			boundary, boundaryWeights, indices);

		if (leftCell.GetEdgeType(rightCell) == HexEdgeType.Slope)
		{
			TriangulateBoundaryTriangle(
				left, weights2, right, weights3,
				boundary, boundaryWeights, indices);
		}
		else
		{
			terrain.AddTriangleUnperturbed(
				HexMetrics.Perturb(left), HexMetrics.Perturb(right), boundary);
			terrain.AddTriangleCellData(
				indices, weights2, weights3, boundaryWeights);
		}
	}

	void TriangulateCornerCliffTerraces(
		Vector3 begin, int beginCellIndex, HexCellData beginCell,
		Vector3 left, int leftCellIndex, HexCellData leftCell,
		Vector3 right, int rightCellIndex, HexCellData rightCell)
	{
		float b = 1f / (leftCell.Elevation - beginCell.Elevation);
		if (b < 0)
		{
			b = -b;
		}
		Vector3 boundary = Vector3.Lerp(
			HexMetrics.Perturb(begin), HexMetrics.Perturb(left), b);
		Color boundaryWeights = Color.Lerp(weights1, weights2, b);
		Vector3 indices;
		indices.x = beginCellIndex;
		indices.y = leftCellIndex;
		indices.z = rightCellIndex;

		TriangulateBoundaryTriangle(
			right, weights3, begin, weights1,
			boundary, boundaryWeights, indices);

		if (leftCell.GetEdgeType(rightCell) == HexEdgeType.Slope)
		{
			TriangulateBoundaryTriangle(
				left, weights2, right, weights3,
				boundary, boundaryWeights, indices);
		}
		else
		{
			terrain.AddTriangleUnperturbed(
				HexMetrics.Perturb(left), HexMetrics.Perturb(right), boundary);
			terrain.AddTriangleCellData(
				indices, weights2, weights3, boundaryWeights);
		}
	}

	void TriangulateBoundaryTriangle(
		Vector3 begin, Color beginWeights,
		Vector3 left, Color leftWeights,
		Vector3 boundary, Color boundaryWeights, Vector3 indices)
	{
		Vector3 v2 = HexMetrics.Perturb(HexMetrics.TerraceLerp(begin, left, 1));
		Color w2 = HexMetrics.TerraceLerp(beginWeights, leftWeights, 1);

		terrain.AddTriangleUnperturbed(HexMetrics.Perturb(begin), v2, boundary);
		terrain.AddTriangleCellData(indices, beginWeights, w2, boundaryWeights);

		for (int i = 2; i < HexMetrics.terraceSteps; i++)
		{
			Vector3 v1 = v2;
			Color w1 = w2;
			v2 = HexMetrics.Perturb(HexMetrics.TerraceLerp(begin, left, i));
			w2 = HexMetrics.TerraceLerp(beginWeights, leftWeights, i);
			terrain.AddTriangleUnperturbed(v1, v2, boundary);
			terrain.AddTriangleCellData(indices, w1, w2, boundaryWeights);
		}

		terrain.AddTriangleUnperturbed(v2, HexMetrics.Perturb(left), boundary);
		terrain.AddTriangleCellData(indices, w2, leftWeights, boundaryWeights);
	}

	void TriangulateEdgeFan(Vector3 center, EdgeVertices edge, float index)
	{
		terrain.AddTriangle(center, edge.v1, edge.v2);
		terrain.AddTriangle(center, edge.v2, edge.v3);
		terrain.AddTriangle(center, edge.v3, edge.v4);
		terrain.AddTriangle(center, edge.v4, edge.v5);

		Vector3 indices;
		indices.x = indices.y = indices.z = index;
		terrain.AddTriangleCellData(indices, weights1);
		terrain.AddTriangleCellData(indices, weights1);
		terrain.AddTriangleCellData(indices, weights1);
		terrain.AddTriangleCellData(indices, weights1);
	}

	void TriangulateEdgeStrip(
		EdgeVertices e1, Color w1, float index1,
		EdgeVertices e2, Color w2, float index2,
		bool hasRoad = false)
	{
		terrain.AddQuad(e1.v1, e1.v2, e2.v1, e2.v2);
		terrain.AddQuad(e1.v2, e1.v3, e2.v2, e2.v3);
		terrain.AddQuad(e1.v3, e1.v4, e2.v3, e2.v4);
		terrain.AddQuad(e1.v4, e1.v5, e2.v4, e2.v5);

		Vector3 indices;
		indices.x = indices.z = index1;
		indices.y = index2;
		terrain.AddQuadCellData(indices, w1, w2);
		terrain.AddQuadCellData(indices, w1, w2);
		terrain.AddQuadCellData(indices, w1, w2);
		terrain.AddQuadCellData(indices, w1, w2);
	}


}
