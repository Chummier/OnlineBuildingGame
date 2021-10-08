﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;

namespace OnlineBuildingGame.Models
{
    public class EntityModel
    {
        [Key]
        public int Id { get; set; }
        public string Name { get; set; }
        public int PosY { get; set; }
        public int PosX { get; set; }
        public int Amount { get; set; }

        public EntityModel(string name, int posY, int posX, int amount)
        {
            Name = name;
            PosY = posY;
            PosX = posX;
            Amount = amount;
        }
    }
}
