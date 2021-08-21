using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;
using OnlineBuildingGame.Game;

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
        public int PosX { get; set; }
        public int PosY { get; set; }
        public Direction Facing { get; set; }
        public int Health { get; set; }
        public int InventoryId { get; set; } // used for hotbar as well

        public PlayerModel()
        {
            Name = "";
            PosX = 0;
            PosY = 0;
            Facing = Direction.South;
            Health = 100;
            InventoryId = 0;
        }

        public PlayerModel(string name, int inventoryId)
        {
            Name = name;
            PosX = 0;
            PosY = 0;
            Facing = Direction.South;
            Health = 100;
            InventoryId = inventoryId;
        }
    }
}
