using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace OnlineBuildingGame.Models
{
    public class EntityModel
    {
        public string ItemName { get; set; }
        public int Amount { get; set; }
        public int PosX { get; set; }
        public int PosY { get; set; }

        public EntityModel(string itemName, int amount, int posX, int posY)
        {
            ItemName = itemName;
            Amount = amount;
            PosX = posX;
            PosY = posY;
        }
    }
}
