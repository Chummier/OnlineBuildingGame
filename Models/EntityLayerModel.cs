using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;

namespace OnlineBuildingGame.Models
{
    public class EntityLayerModel
    {
        [Key]
        public int Id { get; set; }
        public string Entities { get; set; } 
        public string NormalizedPositions { get; set; }
        public string Amounts { get; set; }
    }
}
