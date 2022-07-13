using System.ComponentModel.DataAnnotations;

namespace OnlineBuildingGame.Models
{
    public class EntityModel
    {
        [Key]
        public int Id { get; set; }
        public string Name { get; set; }
        public int MapId { get; set; }
        public int PosX { get; set; }
        public int PosY { get; set; }
        public int Layer { get; set; }
        public int Amount { get; set; }

        public EntityModel(string name, int mapId, int posY, int posX, int layer, int amount)
        {
            Name = name;
            MapId = mapId;
            PosY = posY;
            PosX = posX;
            Layer = layer;
            Amount = amount;
        }
    }
}
