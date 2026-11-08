namespace FactionColonies.Events;

/// <summary>
/// Tribute Demand counts a hostile settlement as nearby only when it is on the same planet layer as the
/// Empire settlement: the distance the check feeds to <see cref="EventConditionChecker.WithinTributeProximity"/>
/// is infinite across layers. Measuring across layers used to read one layer's tile id on the other
/// layer, which gave false positives (or an out-of-range tile error). The cross-layer case needs the
/// Odyssey orbit layer; without Odyssey it is not part of the suite.
/// </summary>
public static class TributeProximityLayerTests
{
    private const float TributeRange = 50f;

    [EmpireTest("Events.Conditions", GameState = TestGameState.Any)]
    public static void Proximity_InfiniteDistance_False()
    {
        TestAssert.IsFalse(EventConditionChecker.WithinTributeProximity(float.PositiveInfinity, TributeRange),
            "an infinite (cross-layer) distance should never count as nearby");
    }

    [EmpireTest("Events.Conditions", RequiresPackageId = ModContentPack.OdysseyModPackageId)]
    public static void Proximity_SameTileIdOnOtherLayer_NotNearby()
    {
        WorldGrid grid = Find.WorldGrid;
        PlanetLayer orbit = TestAssert.NotNull(grid.Orbit, "Odyssey is active but the world has no orbit layer");
        TestAssert.IsFalse(orbit == grid.Surface, "the orbit layer must differ from the surface layer");

        // The same tile id on both layers: a layer-blind measurement reads this pair as distance 0.
        PlanetTile surfaceTile = new(0, grid.Surface);
        PlanetTile orbitTile = new(0, orbit);

        float crossLayer = surfaceTile.ApproxDistanceSameLayer(orbitTile);
        TestAssert.IsTrue(float.IsPositiveInfinity(crossLayer), $"a cross-layer pair should be infinitely far, got {crossLayer}");
        TestAssert.IsFalse(EventConditionChecker.WithinTributeProximity(crossLayer, TributeRange),
            "a hostile on another layer should never trigger Tribute Demand");
        TestAssert.IsFalse(EventConditionChecker.WithinTributeProximity(orbitTile.ApproxDistanceSameLayer(surfaceTile), TributeRange),
            "the check should be symmetric: orbit to surface is also never nearby");

        float sameLayer = surfaceTile.ApproxDistanceSameLayer(new PlanetTile(0, grid.Surface));
        TestAssert.IsTrue(EventConditionChecker.WithinTributeProximity(sameLayer, TributeRange),
            "a same-layer pair on the same tile should still count as nearby");
    }
}
