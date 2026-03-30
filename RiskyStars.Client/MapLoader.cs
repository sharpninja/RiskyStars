using Microsoft.Xna.Framework;

namespace RiskyStars.Client;

public class MapLoader
{
    public static MapData CreateSampleMap()
    {
        var mapData = new MapData();

        var homeSystem1 = new StarSystemData
        {
            Id = "home_1",
            Name = "Home System 1",
            Type = StarSystemType.Home,
            Position = new Vector2(-400, -200)
        };

        var body1 = new StellarBodyData
        {
            Id = "body_1",
            Name = "Planet 5432",
            StarSystemId = homeSystem1.Id,
            Type = StellarBodyType.RockyPlanet,
            Position = homeSystem1.Position + new Vector2(-30, -30)
        };
        body1.Regions.Add(new RegionData
        {
            Id = "body_1_region_1",
            Name = "Continent 1",
            StellarBodyId = body1.Id,
            Position = body1.Position + new Vector2(-10, -5)
        });
        body1.Regions.Add(new RegionData
        {
            Id = "body_1_region_2",
            Name = "Continent 2",
            StellarBodyId = body1.Id,
            Position = body1.Position + new Vector2(10, -5)
        });
        body1.Regions.Add(new RegionData
        {
            Id = "body_1_region_3",
            Name = "Continent 3",
            StellarBodyId = body1.Id,
            Position = body1.Position + new Vector2(-10, 5)
        });
        body1.Regions.Add(new RegionData
        {
            Id = "body_1_region_4",
            Name = "Continent 4",
            StellarBodyId = body1.Id,
            Position = body1.Position + new Vector2(10, 5)
        });
        homeSystem1.StellarBodies.Add(body1);

        var body2 = new StellarBodyData
        {
            Id = "body_2",
            Name = "Gas Giant 7821",
            StarSystemId = homeSystem1.Id,
            Type = StellarBodyType.GasGiant,
            Position = homeSystem1.Position + new Vector2(40, 20)
        };
        body2.Regions.Add(new RegionData
        {
            Id = "body_2_region_1",
            Name = "Surface",
            StellarBodyId = body2.Id,
            Position = body2.Position
        });
        homeSystem1.StellarBodies.Add(body2);

        var body3 = new StellarBodyData
        {
            Id = "body_3",
            Name = "Planetoid 3142",
            StarSystemId = homeSystem1.Id,
            Type = StellarBodyType.Planetoid,
            Position = homeSystem1.Position + new Vector2(20, -40)
        };
        body3.Regions.Add(new RegionData
        {
            Id = "body_3_region_1",
            Name = "Surface",
            StellarBodyId = body3.Id,
            Position = body3.Position
        });
        homeSystem1.StellarBodies.Add(body3);

        var homeSystem2 = new StarSystemData
        {
            Id = "home_2",
            Name = "Home System 2",
            Type = StarSystemType.Home,
            Position = new Vector2(400, 200)
        };

        var body5 = new StellarBodyData
        {
            Id = "body_5",
            Name = "Planet 2345",
            StarSystemId = homeSystem2.Id,
            Type = StellarBodyType.RockyPlanet,
            Position = homeSystem2.Position + new Vector2(-25, -25)
        };
        body5.Regions.Add(new RegionData
        {
            Id = "body_5_region_1",
            Name = "Continent 1",
            StellarBodyId = body5.Id,
            Position = body5.Position + new Vector2(-12, 0)
        });
        body5.Regions.Add(new RegionData
        {
            Id = "body_5_region_2",
            Name = "Continent 2",
            StellarBodyId = body5.Id,
            Position = body5.Position + new Vector2(0, -8)
        });
        body5.Regions.Add(new RegionData
        {
            Id = "body_5_region_3",
            Name = "Continent 3",
            StellarBodyId = body5.Id,
            Position = body5.Position + new Vector2(12, 0)
        });
        body5.Regions.Add(new RegionData
        {
            Id = "body_5_region_4",
            Name = "Continent 4",
            StellarBodyId = body5.Id,
            Position = body5.Position + new Vector2(0, 8)
        });
        body5.Regions.Add(new RegionData
        {
            Id = "body_5_region_5",
            Name = "Continent 5",
            StellarBodyId = body5.Id,
            Position = body5.Position
        });
        homeSystem2.StellarBodies.Add(body5);

        var body6 = new StellarBodyData
        {
            Id = "body_6",
            Name = "Comet 4567",
            StarSystemId = homeSystem2.Id,
            Type = StellarBodyType.Comet,
            Position = homeSystem2.Position + new Vector2(50, -10)
        };
        body6.Regions.Add(new RegionData
        {
            Id = "body_6_region_1",
            Name = "Surface",
            StellarBodyId = body6.Id,
            Position = body6.Position
        });
        homeSystem2.StellarBodies.Add(body6);

        var featuredSystem = new StarSystemData
        {
            Id = "featured",
            Name = "Featured System",
            Type = StarSystemType.Featured,
            Position = new Vector2(0, 0)
        };

        var body8 = new StellarBodyData
        {
            Id = "body_8",
            Name = "Planet 1234",
            StarSystemId = featuredSystem.Id,
            Type = StellarBodyType.RockyPlanet,
            Position = featuredSystem.Position + new Vector2(-40, 0)
        };
        for (int i = 0; i < 8; i++)
        {
            float angle = (float)(i * Math.PI / 4);
            body8.Regions.Add(new RegionData
            {
                Id = $"body_8_region_{i + 1}",
                Name = $"Continent {i + 1}",
                StellarBodyId = body8.Id,
                Position = body8.Position + new Vector2(
                    (float)Math.Cos(angle) * 12,
                    (float)Math.Sin(angle) * 12
                )
            });
        }
        featuredSystem.StellarBodies.Add(body8);

        var body9 = new StellarBodyData
        {
            Id = "body_9",
            Name = "Gas Giant 5678",
            StarSystemId = featuredSystem.Id,
            Type = StellarBodyType.GasGiant,
            Position = featuredSystem.Position + new Vector2(40, 20)
        };
        body9.Regions.Add(new RegionData
        {
            Id = "body_9_region_1",
            Name = "Surface",
            StellarBodyId = body9.Id,
            Position = body9.Position
        });
        featuredSystem.StellarBodies.Add(body9);

        mapData.StarSystems.Add(homeSystem1);
        mapData.StarSystems.Add(homeSystem2);
        mapData.StarSystems.Add(featuredSystem);

        var lane1 = new HyperspaceLaneData
        {
            Id = "lane_1",
            Name = "Home System 1 - Featured System",
            StarSystemAId = homeSystem1.Id,
            StarSystemBId = featuredSystem.Id,
            MouthAId = "lane_1_mouth_a",
            MouthBId = "lane_1_mouth_b",
            MouthAPosition = homeSystem1.Position + new Vector2(60, 30),
            MouthBPosition = featuredSystem.Position + new Vector2(-50, -30)
        };
        mapData.HyperspaceLanes.Add(lane1);

        var lane2 = new HyperspaceLaneData
        {
            Id = "lane_2",
            Name = "Home System 2 - Featured System",
            StarSystemAId = homeSystem2.Id,
            StarSystemBId = featuredSystem.Id,
            MouthAId = "lane_2_mouth_a",
            MouthBId = "lane_2_mouth_b",
            MouthAPosition = homeSystem2.Position + new Vector2(-60, -30),
            MouthBPosition = featuredSystem.Position + new Vector2(50, 30)
        };
        mapData.HyperspaceLanes.Add(lane2);

        return mapData;
    }
}
