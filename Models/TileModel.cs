using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;
using OnlineBuildingGame.Game;

namespace OnlineBuildingGame.Models
{
    public class TileModel
    {
        [Key]
        public int DbId { get; set; }
        public int Id { get; set; }
        public string Name { get; set; }
        public string Type { get; set; }
        public string SubType { get; set; }
        public string OnEnterFunc { get; set; }
        public string Img { get; set; }
        public int DataId { get; set; }

        public TileModel()
        {
            Id = 0;
            Name = "";
            Type = TileTypes.Open;
            SubType = TileSubTypes.Soil;
            OnEnterFunc = "Nothing";
            Img = "Air.png";
            DataId = 0;
        }

        public TileModel(int id, string _name, string type, string subType, string enterFunc, string srcimg, int dataId)
        {
            Id = id;
            Name = _name;
            Type = type;
            SubType = subType;
            OnEnterFunc = enterFunc;
            Img = srcimg;
            DataId = dataId;
        }

    }
}
