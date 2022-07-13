using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace OnlineBuildingGame.Models
{
    public class ProtectedTileModel
    {
        [DatabaseGeneratedAttribute(DatabaseGeneratedOption.Identity)]
        [Key]
        public int Id { get; set; }
        public int MapId { get; set; }
        public int PosX { get; set; }
        public int PosY { get; set; }
        public int Layer { get; set; }
        public string Owner { get; set; }
        public string CoOwner { get; set; }

        public ProtectedTileModel(int mapId, int posX, int posY, int layer, string owner, string coOwner)
        {
            MapId = mapId;
            PosX = posX;
            PosY = posY;
            Layer = layer;
            Owner = owner;
            CoOwner = coOwner;
        }
    }
}
