﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;
using OnlineBuildingGame.Game;

namespace OnlineBuildingGame.Models
{
    public class ItemModel
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string UseFunc { get; set; }
        public string Img { get; set; }

        public ItemModel(int id, string name, string useFunc, string img)
        {
            Id = id;
            Name = name;
            UseFunc = useFunc;
            Img = img;
        }
    }
}
