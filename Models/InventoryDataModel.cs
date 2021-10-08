using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace OnlineBuildingGame.Models
{
    public class InventoryDataModel
    {
        [DatabaseGeneratedAttribute(DatabaseGeneratedOption.Identity)]
        [Key]
        public int Id { get; set; }
        public int InvId { get; set; }
        public string ItemName { get; set; }
        public int Amount { get; set; }
        public int Position { get; set; }

        public InventoryDataModel()
        {
            InvId = 0;
            ItemName = "";
            Amount = 0;
            Position = 0;
        }

        public InventoryDataModel(int invId, string itemName, int amount, int position)
        {
            InvId = invId;
            ItemName = itemName;
            Amount = amount;
            Position = position;
        }
    }
}
