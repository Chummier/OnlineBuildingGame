using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace OnlineBuildingGame.Models
{
    public class MapDataModel
    {
        [DatabaseGeneratedAttribute(DatabaseGeneratedOption.Identity)]
        [Key]
        public int Id { get; set; }
        public int MapId { get; set; }
        public int TileId { get; set; }
        public int Layer { get; set; }
        public string TileName { get; set; }
        public int PosY { get; set; }
        public int PosX { get; set; }

        public MapDataModel(int mapId, int tileId, int layer, string tileName, int posY, int posX)
        {
            MapId = mapId;
            TileId = tileId;
            Layer = layer;
            TileName = tileName;
            PosY = posY;
            PosX = posX;
        }
    }
}
