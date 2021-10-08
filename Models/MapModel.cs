using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;

namespace OnlineBuildingGame.Models
{
    public class MapModel
    {
        [Key]
        public int Id { get; set; }
        public int MapId { get; set; }
        public string Name { get; set; }
        public int DimY { get; set; }
        public int DimX { get; set; }

        public MapModel(int mapId, string name, int dimY, int dimX)
        {
            MapId = mapId;
            Name = name;
            DimY = dimY;
            DimX = dimX;
        }
    }
}
