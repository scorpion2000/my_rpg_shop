using Godot;
using System;

public partial class navmeshCleanup : TileMap
{

	/*private bool IsCellTraversible(int cellLayer, Vector2I cell)
	{
		bool isTraversable = true;
		for (int layer = 0; layer <= GetLayersCount(); layer++)
		{
			if (cellLayer == layer)
				break;

			isTraversable = false;
		}

		GD.Print(isTraversable);
		return isTraversable;
	}*/

    public override bool _UseTileDataRuntimeUpdate(int layer, Vector2I coords)
    {
		/*if (!IsCellTraversible(layer, coords))
			return true;
		else
			return false;*/

		int nextLayer = layer + 1;
		if (nextLayer >= GetLayersCount())
			return false;

		NavigationPolygon currentPolygon = GetCellTileData(layer, coords).GetNavigationPolygon(0);
		if (currentPolygon == null)
			return false;

		TileData nextTile = GetCellTileData(nextLayer, coords);
		if (nextTile == null)
			return false;

		NavigationPolygon nextPolygon = nextTile.GetNavigationPolygon(0);
		if (nextPolygon == null)
			return currentPolygon != nextPolygon;

		return true;
    }

    public override void _TileDataRuntimeUpdate(int layer, Vector2I coords, TileData tileData)
    {
		/*GD.Print("WooWee");
		tileData.SetNavigationPolygon(0, new NavigationPolygon());*/

		int nextLayer = layer + 1;
		if (nextLayer >= GetLayersCount())
			return;

		NavigationPolygon currentPolygon = tileData.GetNavigationPolygon(0);
		if (currentPolygon == null)
			return;

		TileData nextTile = GetCellTileData(nextLayer, coords);
		if (nextTile == null)
			return;

		NavigationPolygon nextPolygon = nextTile.GetNavigationPolygon(0);
		if (nextPolygon != null)
		{
			tileData.SetNavigationPolygon(0, nextPolygon);
			return;
		}

		tileData.SetNavigationPolygon(0, new NavigationPolygon());
		return;
    }
}
