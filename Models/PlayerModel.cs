using System.ComponentModel.DataAnnotations;


namespace OnlineBuildingGame.Models
{
    public enum Direction
    {
        North = -10,
        South = 10, // multiplied by 10 to differentiate N/S from E/W
        East = 1,
        West = -1, // based off the game canvas coordinates, which start 
                   // at (0, 0) at the top left
    }

    public class PlayerModel
    {
        [Key]
        public int Id { get; set; }
        public string Name { get; set; }
        public int MapId { get; set; }
        public int InventoryId { get; set; }
        public int HotbarId { get; set; }

        public PlayerModel()
        {
            Name = "";
            MapId = 0;
            InventoryId = 0;
            HotbarId = 0;
        }

        public PlayerModel(string name, int mapId, int inventoryId, int hotbarId)
        {
            Name = name;
            MapId = mapId;
            InventoryId = inventoryId;
            HotbarId = hotbarId;
        }
    }
}
