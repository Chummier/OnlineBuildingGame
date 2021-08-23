using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;

namespace OnlineBuildingGame.Models
{
    public class WorldLayerModel
    {
        [Key]
        public int DbId { get; set; }
        public int Layer { get; set; }
        public string Tiles { get; set; }

        public WorldLayerModel(int layer, string tiles)
        {
            Layer = layer;
            Tiles = tiles;
        }
    }
}
