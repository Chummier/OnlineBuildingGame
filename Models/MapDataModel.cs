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
        public string TileName { get; set; }
        public int PosX { get; set; }
        public int PosY { get; set; }
        public int Layer { get; set; }

        public MapDataModel(int mapId, int tileId, string tileName, int posX, int posY, int layer)
        {
            MapId = mapId;
            TileId = tileId;
            TileName = tileName;
            PosX = posX;
            PosY = posY;
            Layer = layer;
        }
    }
}
