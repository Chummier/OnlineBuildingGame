using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;

namespace OnlineBuildingGame.Models
{
    public class WorldModel
    {
        [Key]
        public int Id { get; set; }
        public int DimX { get; set; }
        public int DimY { get; set; }
        public string Layer0 { get; set; } // stored as DataId|Name,DataId|Name,etc
        public string Layer1 { get; set; } 
        public string Layer2 { get; set; }
    }
}
