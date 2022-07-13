using System.ComponentModel.DataAnnotations;

namespace OnlineBuildingGame.Models
{
    public class PlayerLocationModel
    {
        [Key]
        public int Id { get; set; }
        public string Player { get; set; }
        public int MapId { get; set; }
        public double PosX { get; set; }
        public double PosY { get; set; }
        public int Layer { get; set; }

        public PlayerLocationModel()
        {
            Player = "";
            MapId = 0;
            PosX = 0;
            PosY = 0;
            Layer = 0;
        }

        public PlayerLocationModel(string player, int mapId, double posX, double posY, int layer)
        {
            Player = player;
            MapId = mapId;
            PosX = posX;
            PosY = posY;
            Layer = layer;
        }
    }
}
