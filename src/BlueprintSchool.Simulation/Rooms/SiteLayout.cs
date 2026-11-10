using BlueprintSchool.Simulation.World;

namespace BlueprintSchool.Simulation.Rooms;

/// <summary>Turns the site and its rooms into the walkability grid people find their way around.</summary>
public static class SiteLayout
{
    /// <summary>Builds the walkability grid: paths and floors walkable, walls blocking except at doors and entrances.</summary>
    public static NavGrid BuildNavGrid(SiteMap site, IEnumerable<Room> spaces)
    {
        var all = spaces as IReadOnlyCollection<Room> ?? spaces.ToList();
        var grid = new NavGrid(site.Width, site.GridLength, site.FloorStride);
        foreach (var tile in site.Bounds.Tiles())
        {
            grid.SetCost(tile, site[tile] switch
            {
                TileKind.BuildingFloor => 1,
                TileKind.Paving => 1,
                TileKind.Parking => 2,
                TileKind.Road => 3,
                TileKind.Grass => 4,
                _ => 1,
            });
        }

        foreach (var building in site.Buildings)
        {
            foreach (var (inside, outside, side) in building.OuterEdges())
            {
                if (!building.IsOpening(inside, side))
                {
                    grid.BlockEdge(inside, outside);
                }
            }
        }

        // Upper floors: the building's outline, walled all round (there are no street doors upstairs).
        for (var floor = 1; floor < site.FloorCount; floor++)
        {
            foreach (var building in site.Buildings)
            {
                foreach (var tile in building.Areas.SelectMany(a => a.Tiles()))
                {
                    grid.SetCost(site.OnFloor(tile, floor), 1);
                }

                foreach (var (inside, outside, _) in building.OuterEdges())
                {
                    grid.BlockEdge(site.OnFloor(inside, floor), site.OnFloor(outside, floor));
                }
            }
        }

        foreach (var space in all.Where(s => s.HasWalls))
        {
            BlockPerimeter(grid, space.Rect, space.IsDoor);
        }

        VerticalTransport.AddLinks(grid, site, all);
        return grid;
    }

    private static void BlockPerimeter(NavGrid grid, GridRect rect, Func<GridCoord, WallSide, bool> isOpening)
    {
        for (var x = rect.X; x < rect.Right; x++)
        {
            var north = new GridCoord(x, rect.Y);
            if (!isOpening(north, WallSide.North))
            {
                grid.BlockEdge(north, new GridCoord(x, rect.Y - 1));
            }

            var south = new GridCoord(x, rect.Bottom - 1);
            if (!isOpening(south, WallSide.South))
            {
                grid.BlockEdge(south, new GridCoord(x, rect.Bottom));
            }
        }

        for (var y = rect.Y; y < rect.Bottom; y++)
        {
            var west = new GridCoord(rect.X, y);
            if (!isOpening(west, WallSide.West))
            {
                grid.BlockEdge(west, new GridCoord(rect.X - 1, y));
            }

            var east = new GridCoord(rect.Right - 1, y);
            if (!isOpening(east, WallSide.East))
            {
                grid.BlockEdge(east, new GridCoord(rect.Right, y));
            }
        }
    }
}
