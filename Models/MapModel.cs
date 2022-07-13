using System.ComponentModel.DataAnnotations;

namespace OnlineBuildingGame.Models
{
    public class MapModel
    {
        [Key]
        public int Id { get; set; }
        public int MapId { get; set; }
        public string Owner { get; set; }
        public string Name { get; set; }
        public int DimX { get; set; }
        public int DimY { get; set; }
        public int DimZ { get; set; }
        public int SpawnX { get; set; }
        public int SpawnY { get; set; }
        public int SpawnZ { get; set; }

        public MapModel(int mapId, string owner, string name, int dimX, int dimY, int dimZ, int spawnX, int spawnY, int spawnZ)
        {
            MapId = mapId;
            Owner = owner;
            Name = name;
            DimX = dimX;
            DimY = dimY;
            DimZ = dimZ;
            SpawnX = spawnX;
            SpawnY = spawnY;
            SpawnZ = spawnZ;
        }
    }
}
