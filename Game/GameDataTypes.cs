using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;
using OnlineBuildingGame.Models;

namespace OnlineBuildingGame.Game
{
    public static class FunctionTypes
    {
        public const string Nothing = "Nothing";
        public const string GoUpstairs = "GoUpstairs";
        public const string GoDownstairs = "GoDownstairs";
        public const string Place = "Place";
        public const string PlaceFloor = "PlaceFloor";
        public const string PlaceGateway = "PlaceGateway";
        public const string Plant = "Plant";
        public const string UseTool = "UseTool";
        public const string UseWeapon = "UseWeapon";
        public const string UseCanvas = "UseCanvas";
    }

    public static class TileTypes
    {
        public const string Open = "Open";
        public const string Sturdy = "Sturdy";
    }

    public static class TileSubTypes
    {
        public const string Wood = "Wood";
        public const string Canvas = "Canvas";
        public const string Gateway = "Gateway";
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

    public class MapDictionary
    {
        private ConcurrentDictionary<Tuple<int, int, int>, MapDataModel> Map;

        public MapDictionary()
        {
            Map = new ConcurrentDictionary<Tuple<int, int, int>, MapDataModel>();
        }

        public void Modify(MapDataModel newTile)
        {
            Map[new Tuple<int, int, int>(newTile.PosX, newTile.PosY, newTile.Layer)] = newTile;
        }

        public void AddMap(List<MapDataModel> map)
        {
            foreach (MapDataModel d in map)
            {
                Map.TryAdd(new Tuple<int, int, int>(d.PosX, d.PosY, d.Layer), d);
            }
        }

        public MapDataModel Access(int x, int y, int layer)
        {
            return Map[new Tuple<int, int, int>(x, y, layer)];
        }

        public bool TryAccess(int x, int y, int layer, out MapDataModel data)
        {
            if (Map.TryGetValue(new Tuple<int, int, int>(x, y, layer), out MapDataModel val))
            {
                data = val;
                return true;
            }

            data = null;
            return false;
        }

        public List<MapDataModel> ToList()
        {
            return Map.Values.ToList();
        }
    }

    public class MapsDictionary
    {
        private ConcurrentDictionary<int, MapDictionary> Maps;

        public MapsDictionary()
        {
            Maps = new ConcurrentDictionary<int, MapDictionary>();
        }

        public void AddMap(int mapId, List<MapDataModel> map)
        {
            if (Maps.TryAdd(mapId, new MapDictionary()))
            {
                Maps[mapId].AddMap(map);
            }
        }

        public void RemoveMap(int mapId)
        {
            if (Maps.TryRemove(mapId, out MapDictionary map))
            {

            }
        }

        public MapDataModel Access(int mapId, int x, int y, int layer)
        {
            return Maps[mapId].Access(x, y, layer);
        }

        public bool TryAccess(int mapId, int x, int y, int layer, out MapDataModel data)
        {
            if (Maps[mapId].TryAccess(x, y, layer, out data))
            {
                return true;
            }

            return false;
        }

        public List<MapDataModel> AccessGroup(int mapId, int x, int y)
        {
            List<MapDataModel> tiles = new List<MapDataModel>();

            for (int i = 0; i < 5; i++)
            {
                if (Maps[mapId].TryAccess(x, y, i, out MapDataModel data))
                {
                    tiles.Add(data);
                }
            }

            return tiles;
        }

        public void Modify(int mapId, MapDataModel newTile)
        {
            Maps[mapId].Modify(newTile);
        }

        public List<MapDataModel> ToList(int mapId)
        {
            return Maps[mapId].ToList();
        }
    }

    public class CanvasData
    {
        public string Owner { get; set; }
        public bool Placed { get; set; }
        public (int, int) Pos1 { get; set; }
        public (int, int) Pos2 { get; set; }
        public int Layer { get; set; }
        public string Image { get; set; }

        public CanvasData()
        {
            Placed = false;
            Image = "0";
        }

        public CanvasData(string owner, (int, int) pos1, int layer)
        {
            Owner = owner;
            Placed = false;
            Pos1 = pos1;
            Layer = layer;
            Image = "0";
        }
    }

    public class GatewayData
    {
        public int Id { get; set; }
        public int LinkId { get; set; }
        public string Type { get; set; }
        public int PosX { get; set; }
        public int PosY { get; set; }
        public int Layer { get; set; }

        public GatewayData()
        {
            Id = -1;
            LinkId = -1;
        }
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

    public class TileInUse
    {
        public string Type { get; set; }
        public int PosY { get; set; }
        public int PosX { get; set; }
        public string Data { get; set; }

        public TileInUse(string type, int posY, int posX, string data)
        {
            Type = type;
            PosY = posY;
            PosX = posX;
            Data = data;
        }
    }

    public class GameDataTypes
    {

    }
}
