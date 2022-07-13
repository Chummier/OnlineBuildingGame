using System.ComponentModel.DataAnnotations;

namespace OnlineBuildingGame.Models
{
    public class InventoryModel
    {
        [Key]
        public int Id { get; set; }
        public int InvId { get; set; }
        public int Size { get; set; }

        public InventoryModel(int invId, int size)
        {
            InvId = invId;
            Size = size;
        }
    }
}
