using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using OnlineBuildingGame.Models;

namespace OnlineBuildingGame.Game
{
    public static class FunctionTypes
    {
        public const string Nothing = "Nothing";
        public const string Place = "Place";
        public const string PlaceFloor = "PlaceFloor";
        public const string UseTool = "UseTool";
        public const string UseWeapon = "UseWeapon";
    }

    public static class TileTypes
    {
        public const string Open = "Open";
        public const string Sturdy = "Sturdy";
    }

    public static class TileSubTypes
    {
        public const string Wood = "Wood";
        public const string Stone = "Stone";
        public const string Soil = "Soil";
        public const string Loose = "Loose";
        public const string Air = "Air";
        public const string Player = "Player";
        public const string Water = "Water";
    }

    public static class InventoryTypes
    {
        public const string Inventory = "Inventory";
        public const string Hotbar = "Hotbar";
        public const string Chest = "Chest";
    }

    public struct Item
    {
        public int Count { get; set; }
        public ItemModel Data { get; set; }

        public Item(int amt, ItemModel data)
        {
            Count = amt;
            Data = data;
        }
    }

    public class Inventory
    {
        public List<Item> Items;

        public Inventory(params Item[] items)
        {
            Items = new List<Item>();
            for (int i = 0; i < items.Length; i++)
            {
                Items.Add(items[i]);
            }
        }
    }

    public class ChatMsg
    {
        public double Lifetime;
        public string Msg;

        public ChatMsg(double life, string msg)
        {
            Lifetime = life;
            Msg = msg;
        }
    }
    public class GameDataTypes
    {

    }
}
