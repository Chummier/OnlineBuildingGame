using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;

namespace OnlineBuildingGame.Models
{
    public class InventoryModel
    {
        [Key]
        public int DbId { get; set; }
        public int Id { get; set; }
        public int Size { get; set; }
        public string Items { get; set; } // Amt|ItemName, Amt|ItemName
                                        // 0|whatever if empty slot

        public InventoryModel(int id, int size, string items)
        {
            Id = id;
            Size = size;
            Items = items;
        }
    }
}
