using System;
using System.IO;
using System.Timers;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using OnlineBuildingGame.Models;
using OnlineBuildingGame.Data;

namespace OnlineBuildingGame.Game
{
    public class GameWorld
    {
        private readonly IServiceProvider _serviceProvider;

        private readonly Dictionary<string, TileModel> TileSet; // <TileName, TileModel>
        private readonly Dictionary<string, ItemModel> ItemSet; // <ItemName, ItemModel>
        private Dictionary<int, List<Item>> TileDataSet; // <TileId, List of drops to give when tile is broken>
        private Dictionary<int, TileModel> ItemDataSet; // <ItemId, Tile to place when item is used>

        public Dictionary<int, List<InventoryDataModel>> ActiveInventories = new Dictionary<int, List<InventoryDataModel>>(); // <InventoryId, List<items>>

        public Dictionary<string, PlayerModel> ConnectedPlayers = new Dictionary<string, PlayerModel>();
        public Dictionary<int, Dictionary<string, PlayerLocationModel>> WorldsWithPlayers = new Dictionary<int, Dictionary<string, PlayerLocationModel>>(); 

        private Dictionary<string, DateTime> PlayersLastMoved = new Dictionary<string, DateTime>();

        public List<EntityModel> ActiveEntities = new List<EntityModel>();
        public Dictionary<int, (float, string)> ActivePlants = new Dictionary<int, (float, string)>(); // <NormalizedPosition, (time to grow, tile to become when grown)>

        private Dictionary<string, ChatMsg> ChatMessages = new Dictionary<string, ChatMsg>(); // <PlayerName, ChatMsg>, only 1 message at a time per player
        private double msgLifetime = 3;

        public List<CanvasData> ActiveCanvases = new List<CanvasData>();
        public Dictionary<string, TileInUse> TilesInUse = new Dictionary<string, TileInUse>();

        public readonly Dictionary<string, Func<dynamic, bool>> OnEnterFuncList; // <FuncName, Func<input, output>>

        public MapsDictionary Maps = new MapsDictionary();

        public int MaxStackSize = 30;
        public int InventorySize = 25;
        public int InventoryRows = 5;
        public int InventoryCols = 5;
        public int HotbarSize = 5;

        public int WorldId = 0;
        private readonly int Layers = 3;

        private readonly int rows = 25, cols = 25;

        private static readonly Timer UpdateTimer = new Timer();
        private static readonly Timer SaveTimer = new Timer();
        public readonly int UpdateInterval = 15;
        public readonly int SaveInterval = 5000;

        public GameWorld(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;

            TileSet = new Dictionary<string, TileModel>()
            {
                {"Tree", new TileModel(0, "Tree", TileTypes.Sturdy, TileSubTypes.Wood, FunctionTypes.Nothing , "Tree.png", 0) },
                {"Grass", new TileModel(1, "Grass", TileTypes.Open, TileSubTypes.Soil, FunctionTypes.Nothing, "Grass.png", 0) },
                {"Air", new TileModel(2, "Air", TileTypes.Open, TileSubTypes.Air, FunctionTypes.Nothing, "Air.png", 0) },
                {"Player", new TileModel(3, "Player", TileTypes.Open, TileSubTypes.Player, FunctionTypes.Nothing, "Player.png", 0) },
                {"Dirt", new TileModel(4, "Dirt", TileTypes.Open, TileSubTypes.Soil, FunctionTypes.Nothing, "Dirt.png", 0) },
                {"LogWall", new TileModel(5, "LogWall", TileTypes.Sturdy, TileSubTypes.Wood, FunctionTypes.Nothing, "LogWall.png", 0) },
                {"Stone", new TileModel(6, "Stone", TileTypes.Sturdy, TileSubTypes.Stone, FunctionTypes.Nothing, "Stone.png", 0) },
                {"StoneFloor", new TileModel(7, "StoneFloor", TileTypes.Open, TileSubTypes.Stone, FunctionTypes.Nothing, "StoneFloor.png", 0) },
                {"StoneWall", new TileModel(8, "StoneWall", TileTypes.Sturdy, TileSubTypes.Stone, FunctionTypes.Nothing, "StoneWall.png", 0) },
                {"Water", new TileModel(9, "Water", TileTypes.Open, TileSubTypes.Water, "StartSwimming", "Water.png", 0) },
                {"Sand", new TileModel(10, "Sand", TileTypes.Open, TileSubTypes.Soil, FunctionTypes.Nothing, "Sand.png", 0) },
                {"Flower", new TileModel(11, "Flower", TileTypes.Open, TileSubTypes.Loose, FunctionTypes.Nothing, "Flower.png", 0) },
                {"Sapling", new TileModel(12, "Sapling", TileTypes.Open, TileSubTypes.Loose, FunctionTypes.Nothing, "Sapling.png", 0) },
                {"Sunflower", new TileModel(13, "Sunflower", TileTypes.Open, TileSubTypes.Loose, FunctionTypes.Nothing, "Sunflower.png", 0) },
                {"Chest", new TileModel(14, "Chest", TileTypes.Sturdy, TileSubTypes.Wood, FunctionTypes.Nothing, "Chest.png", 0) },
                {"CanvasTopLeft", new TileModel(15, "CanvasTopLeft", TileTypes.Sturdy, TileSubTypes.Canvas, FunctionTypes.Nothing, "CanvasTopLeft.png", 0) },
                {"CanvasTopMiddle", new TileModel(16, "CanvasTopMiddle", TileTypes.Sturdy, TileSubTypes.Canvas, FunctionTypes.Nothing, "CanvasTopMiddle.png", 0) },
                {"CanvasTopRight", new TileModel(17, "CanvasTopRight", TileTypes.Sturdy, TileSubTypes.Canvas, FunctionTypes.Nothing, "CanvasTopRight.png", 0) },
                {"CanvasMiddleLeft", new TileModel(18, "CanvasMiddleLeft", TileTypes.Sturdy, TileSubTypes.Canvas, FunctionTypes.Nothing, "CanvasMiddleLeft.png", 0) },
                {"CanvasMiddle", new TileModel(19, "CanvasMiddle", TileTypes.Sturdy, TileSubTypes.Canvas, FunctionTypes.Nothing, "CanvasMiddle.png", 0) },
                {"CanvasMiddleRight", new TileModel(20, "CanvasMiddleRight", TileTypes.Sturdy, TileSubTypes.Canvas, FunctionTypes.Nothing, "CanvasMiddleRight.png", 0) },
                {"CanvasSingle", new TileModel(21, "CanvasSingle", TileTypes.Sturdy, TileSubTypes.Canvas, FunctionTypes.Nothing, "CanvasSingle.png", 0) },
                {"CanvasSingleMiddle", new TileModel(22, "CanvasSingleMiddle", TileTypes.Sturdy, TileSubTypes.Canvas, FunctionTypes.Nothing, "CanvasSingleMiddle.png", 0) },
                {"PlankDoor", new TileModel(23, "PlankDoor", TileTypes.Open, TileSubTypes.Gateway, FunctionTypes.Nothing, "PlankDoor.png", 0) },
                {"Stairs", new TileModel(24, "Stairs", TileTypes.Open, TileSubTypes.Gateway, FunctionTypes.GoUpstairs, "Stairs.png", 0) },
                {"StairsDown", new TileModel(25, "StairsDown", TileTypes.Open, TileSubTypes.Gateway, FunctionTypes.GoDownstairs, "StairsDown.png", 0) },
            };

            ItemSet = new Dictionary<string, ItemModel>()
            {
                {"DirtItem", new ItemModel(0, "DirtItem", FunctionTypes.PlaceFloor, "DirtItem.png") },
                {"GrassItem", new ItemModel(1, "GrassItem", FunctionTypes.PlaceFloor, "GrassItem.png") },
                {"LogWallItem", new ItemModel(2, "LogWallItem", FunctionTypes.Place, "LogWallItem.png") },
                {"SandItem", new ItemModel(3, "SandItem", FunctionTypes.PlaceFloor, "SandItem.png") },
                {"StoneFloorItem", new ItemModel(4, "StoneFloorItem", FunctionTypes.PlaceFloor, "StoneFloorItem.png") },
                {"StoneWallItem", new ItemModel(5, "StoneWallItem", FunctionTypes.Place, "StoneWallItem.png") },
                {"AxeItem", new ItemModel(6, "AxeItem", FunctionTypes.UseTool, "AxeItem.png") },
                {"PickaxeItem", new ItemModel(7, "PickaxeItem", FunctionTypes.UseTool, "PickaxeItem.png") },
                {"ShovelItem", new ItemModel(8, "ShovelItem", FunctionTypes.UseTool, "ShovelItem.png") },
                {"SwordItem", new ItemModel(9, "SwordItem", FunctionTypes.UseWeapon, "SwordItem.png") },
                {"FlowerItem", new ItemModel(10, "FlowerItem", FunctionTypes.Plant, "FlowerItem.png") },
                {"BlankItem", new ItemModel(11, "BlankItem", FunctionTypes.Nothing, "BlankItem.png") },
                {"SaplingItem", new ItemModel(12, "SaplingItem", FunctionTypes.Plant, "SaplingItem.png") },
                {"SunflowerItem", new ItemModel(13, "SunflowerItem", FunctionTypes.Plant, "SunflowerItem.png") },
                {"ChestItem", new ItemModel(14, "ChestItem", FunctionTypes.Place, "ChestItem.png") },
                {"GlovesItem", new ItemModel(15, "GlovesItem", FunctionTypes.UseTool, "GlovesItem.png") },
                {"CanvasItem", new ItemModel(16, "CanvasItem", FunctionTypes.UseCanvas, "CanvasItem.png") },
                {"PlankDoorItem", new ItemModel(17, "PlankDoorItem", FunctionTypes.PlaceGateway, "PlankDoorItem.png") },
                {"StairsItem", new ItemModel(18, "StairsItem", FunctionTypes.PlaceGateway, "StairsItem.png") },
                {"StairsDownItem", new ItemModel(19, "StairsDownItem", FunctionTypes.PlaceGateway, "StairsDownItem.png") },
            };

            TileDataSet = new Dictionary<int, List<Item>>()
            {
                {TileSet["Tree"].TileId, new List<Item>(){new Item(5, ItemSet["LogWallItem"]), new Item(2, ItemSet["SaplingItem"])} },
                {TileSet["Grass"].TileId, new List<Item>(){new Item(1, ItemSet["GrassItem"]) } },
                {TileSet["Dirt"].TileId, new List<Item>(){new Item(1, ItemSet["DirtItem"]) } },
                {TileSet["Sand"].TileId, new List<Item>(){new Item(1, ItemSet["SandItem"]) } },
                {TileSet["LogWall"].TileId, new List<Item>(){new Item(1, ItemSet["LogWallItem"]) } },
                {TileSet["Stone"].TileId, new List<Item>(){new Item(5, ItemSet["StoneWallItem"]) } },
                {TileSet["StoneWall"].TileId, new List<Item>(){new Item(1, ItemSet["StoneWallItem"]) } },
                {TileSet["Flower"].TileId, new List<Item>(){new Item(1, ItemSet["FlowerItem"]) } },
                {TileSet["Sunflower"].TileId, new List<Item>(){new Item(1, ItemSet["SunflowerItem"]) } },
                {TileSet["Chest"].TileId, new List<Item>(){new Item(1, ItemSet["ChestItem"]) } },
                {TileSet["PlankDoor"].TileId, new List<Item>(){new Item(1, ItemSet["PlankDoorItem"])} },
                {TileSet["Stairs"].TileId, new List<Item>(){new Item(1, ItemSet["StairsItem"]) } },
                {TileSet["StairsDown"].TileId, new List<Item>(){new Item(1, ItemSet["StairsDownItem"]) } },
            };

            ItemDataSet = new Dictionary<int, TileModel>()
            {
                {ItemSet["DirtItem"].Id, TileSet["Dirt"]},
                {ItemSet["GrassItem"].Id, TileSet["Grass"] },
                {ItemSet["SandItem"].Id, TileSet["Sand"] },
                {ItemSet["StoneFloorItem"].Id, TileSet["StoneFloor"] },
                {ItemSet["FlowerItem"].Id, TileSet["Flower"] },
                {ItemSet["LogWallItem"].Id, TileSet["LogWall"] },
                {ItemSet["StoneWallItem"].Id, TileSet["StoneWall"] },
                {ItemSet["SaplingItem"].Id, TileSet["Sapling"] },
                {ItemSet["SunflowerItem"].Id, TileSet["Sunflower"] },
                {ItemSet["ChestItem"].Id, TileSet["Chest"] },
                {ItemSet["PlankDoorItem"].Id, TileSet["PlankDoor"] },
                {ItemSet["StairsItem"].Id, TileSet["Stairs"] },
                {ItemSet["StairsDownItem"].Id, TileSet["StairsDown"] },
            };

            OnEnterFuncList = new Dictionary<string, Func<dynamic, bool>>()
            {
                {FunctionTypes.GoUpstairs, (dynamic input) => GoUpstairs(input) },
                {FunctionTypes.GoDownstairs, (dynamic input) => GoDownstairs(input) },
            };

            UpdateTimer.Interval = UpdateInterval;
            UpdateTimer.Elapsed += Update;
            UpdateTimer.AutoReset = true;
            UpdateTimer.Enabled = true;

            SaveTimer.Interval = SaveInterval;
            SaveTimer.Elapsed += Save;
            SaveTimer.AutoReset = true;
            SaveTimer.Enabled = true;

            LoadWorldFromFile();
            //LoadWorld();
        }

        private void GenerateWorld(int id, string owner, string name)
        {
            string[] layers = File.ReadAllLines("Game\\WorldLayers.txt");
            List<MapDataModel> Map = new List<MapDataModel>();
            for (int l = 0; l < Layers; l++)
            {
                string[] currentLayer = layers[l].Split(',').Where(s => !string.IsNullOrEmpty(s)).Select(s => s.Trim()).ToArray();
                int tileIndex = 0;
                for (int i = 0; i < rows; i++)
                {
                    for (int j = 0; j < cols; j++)
                    {
                        string[] DataTile = currentLayer[tileIndex].Split('|');
                        MapDataModel temp = new MapDataModel(id, TileSet[DataTile[1]].TileId, DataTile[1], j, i, 1);

                        Map.Add(temp);

                        if (tileIndex < currentLayer.Length - 1)
                        {
                            tileIndex++;
                        }
                    }
                }
            }
            MapModel mapModel = new MapModel(id, owner, name, rows, cols, 3, 0, 0, 1);
            Maps.AddMap(id, Map);
        }

        private void LoadWorldFromFile()
        {
            string[] layers = File.ReadAllLines("Game\\WorldLayers.txt");
            List<MapDataModel> MainMap = new List<MapDataModel>();
            for (int l = 0; l < Layers; l++)
            {
                string[] currentLayer = layers[l].Split(',').Where(s => !string.IsNullOrEmpty(s)).Select(s => s.Trim()).ToArray();
                int tileIndex = 0;
                for (int i = 0; i < rows; i++)
                {
                    for (int j = 0; j < cols; j++)
                    {
                        string[] DataTile = currentLayer[tileIndex].Split('|');
                        MapDataModel temp = new MapDataModel(WorldId, TileSet[DataTile[1]].TileId, DataTile[1], j, i, l);

                        MainMap.Add(temp);

                        if (tileIndex < currentLayer.Length - 1)
                        {
                            tileIndex++;
                        }
                    }
                }
            }
            Maps.AddMap(WorldId, MainMap);
        }

        private void LoadWorld()
        {
            using (var scope = _serviceProvider.CreateScope())
            {
                List<MapDataModel> MainMap = new List<MapDataModel>();
                var _db = scope.ServiceProvider.GetService<GameDbContext>();

                var map = _db.MapData.Where(d => d.MapId == WorldId).OrderBy(t => t.Layer).ThenBy(t => t.PosY).ThenBy(t => t.PosX);
                MainMap.AddRange(map);

                foreach (var d in _db.MapData)
                {
                    //Maps.Add(new Tuple<int, int, int>(d.PosX, d.PosY, d.Layer), d);
                }

                ActiveEntities.AddRange(_db.Entities);
            }
        }

        private void Save(Object source, ElapsedEventArgs e)
        {
            using (var scope = _serviceProvider.CreateScope())
            {
                var _db = scope.ServiceProvider.GetService<GameDbContext>();
                string query;

                // Save Maps
                query = "DELETE FROM dbo.Maps WHERE MapId = " + WorldId;
                _db.Database.ExecuteSqlRaw(query);
                _db.Maps.Add(new MapModel(WorldId, "Admin", "Main", rows, cols, 3, 0, 0, 1));

                var map = _db.MapData.Where(d => d.MapId == WorldId).OrderBy(t => t.Layer).ThenBy(t => t.PosY).ThenBy(t => t.PosX).ToList();
                var MainMap = Maps.ToList(WorldId);
                for (int i = 0; i < map.Count; i++)
                {
                    map[i].TileId = MainMap[i].TileId;
                    map[i].TileName = MainMap[i].TileName;

                    //map[i] = Maps.Access(WorldId, map[i].PosX, map[i].PosY, map[i].Layer);
                }
                _db.MapData.UpdateRange(map);

                // Save Entities
                _db.Database.ExecuteSqlRaw("DELETE FROM dbo.Entities");
                _db.Entities.AddRange(ActiveEntities);

                // Save Player Data
                _db.Players.UpdateRange(ConnectedPlayers.Values);


                // Save Player Inventory/Hotbar Data
                foreach (int key in ActiveInventories.Keys)
                {
                    query = "DELETE FROM dbo.InventoryData WHERE InvId = " + key;
                    _db.Database.ExecuteSqlRaw(query);
                    foreach (InventoryDataModel m in ActiveInventories[key])
                    {
                        _db.InventoryData.Add(new InventoryDataModel(m.InvId, m.ItemName, m.Amount, m.Position));
                    }
                }

                _db.SaveChanges();
            }
        }

        public Dictionary<string, TileModel> GetTileSet()
        {
            return TileSet;
        }

        public Dictionary<string, ItemModel> GetItemSet()
        {
            return ItemSet;
        }

        public Dictionary<int, List<Item>> GetTileDataSet()
        {
            return TileDataSet;
        }

        public Dictionary<int, TileModel> GetItemDataSet()
        {
            return ItemDataSet;
        }

        public string[] GetPlayerWorldNames(string player)
        {
            using (var scope = _serviceProvider.CreateScope())
            {
                var _db = scope.ServiceProvider.GetService<GameDbContext>();
                string[] worldNames = _db.Maps.Where(d => d.Owner == player).OrderBy(d => d.MapId).Select(d => d.Name).ToArray();
                return worldNames;
            }
        }

        private void Update(Object source, ElapsedEventArgs e)
        {
            CheckPositions();
            CheckMsgLifetimes();
            CheckPlants();
        }

        private int NextFreeIndex(List<int> indices, int end)
        {
            int start = indices.Min();
            int candidate = start + 1;

            foreach (int i in indices)
            {
                if (i == candidate)
                {
                    candidate = i + 1;
                }
            }

            if (candidate >= end)
            {
                return -1;
            } else
            {
                return candidate;
            }
        }

        private bool ValidPosition(int PosX, int PosY, int Layer)
        {
            if (PosX >= 0 && PosY >= 0)
            {
                if (PosX < cols && PosY < rows)
                {
                    string tile = Maps.Access(WorldId, PosX, PosY, Layer).TileName;

                    if (TileSet.TryGetValue(tile, out TileModel val))
                    {
                        if (val.Type != TileTypes.Open)
                        {
                            return false;
                        }
                    }
                    return true;
                }
            }
            return false;
        }

        private bool GoUpstairs(dynamic input)
        {
            if (input is (int, string))
            {
                int mapId = input.Item1;
                string player = input.Item2;

                if (WorldsWithPlayers.TryGetValue(mapId, out Dictionary<string, PlayerLocationModel> players))
                {
                    if (players.TryGetValue(player, out PlayerLocationModel pData))
                    {
                        MoveLayer(mapId, player, pData.Layer + 1);
                        SetPlayerLocation(mapId, player, (int)pData.PosX, (int)pData.PosY - 1);
                        return true;
                    }
                }
            }
            return false;
        }

        private bool GoDownstairs(dynamic input)
        {
            if (input is (int, string))
            {
                int mapId = input.Item1;
                string player = input.Item2;

                if (WorldsWithPlayers.TryGetValue(mapId, out Dictionary<string, PlayerLocationModel> players))
                {
                    if (players.TryGetValue(player, out PlayerLocationModel pData))
                    {
                        MoveLayer(mapId, player, pData.Layer - 1);
                        SetPlayerLocation(mapId, player, (int)pData.PosX, (int)pData.PosY + 1);
                        return true;
                    }
                }
            }
            return false;
        }

        private void CheckPositions()
        {
            foreach (int mapId in WorldsWithPlayers.Keys)
            {
                foreach (string player in WorldsWithPlayers[mapId].Keys)
                {
                    PlayerLocationModel playerLocation = WorldsWithPlayers[mapId][player];

                    var entities = ActiveEntities.Where(e => e.PosX == playerLocation.PosX && e.PosY == playerLocation.PosY);
                    List<Item> items = new List<Item>();

                    foreach (EntityModel e in entities)
                    {
                        items.Add(new Item(e.Amount, ItemSet[e.Name]));
                    }

                    GiveItems(playerLocation.Player, items);
                    ActiveEntities.RemoveAll(e => entities.Contains(e));

                    if (Maps.TryAccess(mapId, (int)playerLocation.PosX, (int)playerLocation.PosY, playerLocation.Layer, out MapDataModel currentTile))
                    {
                        if (currentTile.TileId == TileSet["Stairs"].TileId)
                        {
                            var res = OnEnterFuncList[TileSet["Stairs"].OnEnterFunc]((mapId, player));
                        }

                        if (currentTile.TileId == TileSet["StairsDown"].TileId)
                        {
                            var res = OnEnterFuncList[TileSet["StairsDown"].OnEnterFunc]((mapId, player));
                        }
                    }

                    if (!ValidPosition((int)playerLocation.PosX, (int)playerLocation.PosY, playerLocation.Layer) ||
                    !ValidPosition((int)Math.Ceiling(playerLocation.PosX), (int)Math.Ceiling(playerLocation.PosY), playerLocation.Layer) ||
                    !ValidPosition((int)playerLocation.PosX, (int)Math.Ceiling(playerLocation.PosY), playerLocation.Layer) ||
                    !ValidPosition((int)Math.Ceiling(playerLocation.PosX), (int)playerLocation.PosY, playerLocation.Layer))
                    {
                        for (int y = (int)playerLocation.PosY - 1; y <= (int)playerLocation.PosY + 1; y++)
                        {
                            for (int x = (int)playerLocation.PosX - 1; x <= (int)playerLocation.PosX + 1; x++)
                            {
                                if (ValidPosition(x, y, playerLocation.Layer))
                                {
                                    WorldsWithPlayers[mapId][player].PosX = x;
                                    WorldsWithPlayers[mapId][player].PosY = y;
                                    return;
                                }
                            }
                        }
                    }
                }
            }
        }

        private void CheckMsgLifetimes()
        {
            foreach (string key in ChatMessages.Keys)
            {
                if (ChatMessages[key].Lifetime <= 0)
                {
                    ChatMessages.Remove(key);
                } else
                {
                    ChatMessages[key].Lifetime -= 0.01f;
                }
            }
        }

        private void CheckPlants()
        {
            foreach (int key in ActivePlants.Keys)
            {
                if (ActivePlants[key].Item1 <= 0)
                {
                    int m = key / cols;
                    int n = key % cols;
                    if (TileSet.TryGetValue(ActivePlants[key].Item2, out TileModel val))
                    {
                        //World[1][m][n] = val;
                    }
                    ActivePlants.Remove(key);
                } else
                {
                    ActivePlants[key] = (ActivePlants[key].Item1 - 0.01f, ActivePlants[key].Item2);
                }
            }
        }

        public (int, int) getSize()
        {
            return (rows, cols);
        }

        public int getNumLayers()
        {
            return Layers;
        }

        public (string, dynamic) getTileInUse(string player)
        {
            string type = "None";
            dynamic data = null;
            if (TilesInUse.TryGetValue(player, out TileInUse val))
            {
                type = val.Type;
                if (val.Type == "Canvas")
                {
                    data = null;
                }
            }
            return (type, data);
        }

        public void removeTileInUse(string player)
        {
            TilesInUse.Remove(player);
        }
        public (int[], int[], int[], int[], string[]) getCanvases()
        {
            var res1 = from c in ActiveCanvases
                       where c.Placed == true
                       select c.Pos1.Item1;
            var res2 = from c in ActiveCanvases
                       where c.Placed == true
                       select c.Pos1.Item2;
            var res3 = from c in ActiveCanvases
                       where c.Placed == true
                       select c.Pos2.Item1;
            var res4 = from c in ActiveCanvases
                       where c.Placed == true
                       select c.Pos2.Item2;
            var res5 = from c in ActiveCanvases
                       where c.Placed == true
                       select c.Image;
            return (res1.ToArray(), res2.ToArray(), res3.ToArray(), res4.ToArray(), res5.ToArray());
        }

        public void updateCanvasImage(string player, string image, int targetM, int targetN)
        {
            var canvas = ActiveCanvases.Where(c => targetM >= c.Pos1.Item1 && targetM <= c.Pos2.Item1)
                    .Where(c => targetN >= c.Pos1.Item2 && targetN <= c.Pos2.Item2).FirstOrDefault();
            int canvasIndex = ActiveCanvases.IndexOf(canvas);

            if (canvasIndex < 0)
            {
                return;
            }

            string path = @"C:\Users\jrs99_000\source\repos\OnlineBuildingGame\wwwroot\images\canvases\";
            string fileName = ActiveCanvases[canvasIndex].Pos1.Item1.ToString() + "-" + ActiveCanvases[canvasIndex].Pos1.Item2.ToString() + ".png";

            //string fileNameWithPath = path + DateTime.Now.ToString().Replace("/", "-").Replace(" ", "- ").Replace(":", "") + ".png";

            using (FileStream fs = new FileStream(path + fileName, FileMode.Create))
            {
                using (BinaryWriter bw = new BinaryWriter(fs))
                {
                    byte[] data = Convert.FromBase64String(image);
                    bw.Write(data);
                    bw.Close();
                }
            }
            ActiveCanvases[canvasIndex].Image = fileName;
        }

        public (string[], int[], int[]) getEntities()
        {
            var res1 = from e in ActiveEntities
                       select e.Name;
            var res2 = from e in ActiveEntities
                       select e.PosY;
            var res3 = from e in ActiveEntities
                       select e.PosX;
            return (res1.ToArray(), res2.ToArray(), res3.ToArray());
        }

        public (string[], double[], double[]) getPlayerLocations(int mapId)
        {
            List<PlayerLocationModel> locations;
            if (WorldsWithPlayers.ContainsKey(mapId))
            {
                locations = WorldsWithPlayers[mapId].Values.ToList();
            } else
            {
                Console.WriteLine("GameWorld\\getPlayerLocations: WorldsWithPlayers didn't have key: " + mapId);
                return (new string[0], new double[0], new double[0]);
            }

            var players = from x in locations
                       select x.Player;
            var xPositions = from x in locations
                      select x.PosX;
            var yPositions = from x in locations
                       select x.PosX;
            return (players.ToArray(), xPositions.ToArray(), yPositions.ToArray());
        }

        public string[][][] getWorldSprites()
        {
            string[][][] res = new string[Layers][][];
            for (int l = 0; l < Layers; l++)
            {
                res[l] = new string[rows][];
                for (int i = 0; i < rows; i++)
                {
                    res[l][i] = new string[cols];
                }
            }

            foreach (MapDataModel d in Maps.ToList(WorldId))
            {
                res[d.Layer][d.PosY][d.PosX] = TileSet[d.TileName].Img;
            }

            return res;
        }

        public string[] GetTileImages()
        {
            var res = from x in TileSet
                      select x.Value.Img;
            return res.ToArray();
        }

        public string[] GetItemImages()
        {
            var res = from x in ItemSet
                      select x.Value.Img;
            return res.ToArray();
        }

        public string[] GetCanvasImages()
        {
            var res = from x in ActiveCanvases
                      select x.Image;
            return res.ToArray();
        }

        public void AddPlayerToWorld(string player, string levelName)
        {
            using (var scope = _serviceProvider.CreateScope())
            {
                var _db = scope.ServiceProvider.GetService<GameDbContext>();
                var map = _db.Maps.Where(m => m.Name == levelName);

                if (map.Count() == 1)
                {
                    if (!WorldsWithPlayers.ContainsKey(map.First().MapId))
                    {
                        WorldsWithPlayers.Add(map.First().MapId, new Dictionary<string, PlayerLocationModel>());   
                    }

                    if (!WorldsWithPlayers[map.First().MapId].TryAdd(player, new PlayerLocationModel(player,
                            map.First().MapId, map.First().SpawnX, map.First().SpawnY, map.First().SpawnZ)))
                    {
                        Console.WriteLine("GameWorld\\AddPlayerToWorld: TryAdd on WorldsWithPlayers failed for player: " + player + " with level: " + levelName);
                    }
                } else
                {
                    Console.WriteLine("GameWorld\\AddPlayerToWorld: found nothing in _db.Maps where name == " + levelName);
                }
            }
        }

        public void LoginPlayer(string player)
        {
            // If already connected
            if (ConnectedPlayers.ContainsKey(player))
            {
                return;
            }

            using (var scope = _serviceProvider.CreateScope())
            {
                var _db = scope.ServiceProvider.GetService<GameDbContext>();

                var res = from p in _db.Players
                          where p.Name == player
                          select p;

                // If is a new player
                if (res.Count() == 0)
                {
                    var invIds = _db.Players.OrderBy(p => p.InventoryId).Select(p => p.InventoryId);
                    var hotbarIds = _db.Players.OrderBy(p => p.HotbarId).Select(p => p.HotbarId);

                    int invId, hotbarId;
                    if (_db.Players.Count() == 0)
                    {
                        invId = 0;
                        hotbarId = 1;
                    } else
                    {
                        invId = invIds.LastOrDefault() + 2;
                        hotbarId = hotbarIds.LastOrDefault() + 2;
                    }

                    PlayerModel p = new PlayerModel(player, WorldId, invId, hotbarId);

                    ActiveInventories.Add(invId, new List<InventoryDataModel>());
                    ActiveInventories.Add(hotbarId, new List<InventoryDataModel>());

                    _db.Players.Add(p);

                    _db.Inventories.Add(new InventoryModel(invId, InventorySize));
                    _db.Inventories.Add(new InventoryModel(hotbarId, HotbarSize));
                    _db.SaveChanges();

                    ConnectedPlayers.Add(player, p);
                    PlayersLastMoved.Add(player, DateTime.Now);
                }
                else
                {
                    int invId = res.First().InventoryId;
                    int hotbarId = res.First().HotbarId;

                    var inv = _db.InventoryData.Where(i => i.InvId == invId);
                    var hotbar = _db.InventoryData.Where(h => h.InvId == hotbarId);

                    ActiveInventories.Add(invId, new List<InventoryDataModel>());
                    ActiveInventories.Add(hotbarId, new List<InventoryDataModel>());

                    foreach (InventoryDataModel d in inv)
                    {
                        ActiveInventories[invId].Add(new InventoryDataModel(d.InvId, d.ItemName, d.Amount, d.Position));                        
                    }

                    foreach (InventoryDataModel d in hotbar)
                    {
                        ActiveInventories[hotbarId].Add(new InventoryDataModel(d.InvId, d.ItemName, d.Amount, d.Position));
                    }

                    ConnectedPlayers.Add(player, res.First());
                    PlayersLastMoved.Add(player, DateTime.Now);
                }

                GiveItems(player, new List<Item>() { new Item(5, ItemSet["StairsItem"]) });
                GiveItems(player, new List<Item>() { new Item(5, ItemSet["ChestItem"]) });
                GiveItems(player, new List<Item>() { new Item(1, ItemSet["GlovesItem"]), new Item(1, ItemSet["AxeItem"]), new Item(1, ItemSet["ShovelItem"])});
                GiveItems(player, new List<Item>() { new Item(1, ItemSet["PickaxeItem"]), new Item(1, ItemSet["SwordItem"]) });
                GiveItems(player, new List<Item>() { new Item(1, ItemSet["CanvasItem"]) });
                GiveItems(player, new List<Item>() { new Item(1, ItemSet["PlankDoorItem"]) });
            }
        }

        public List<PlayerModel> GetConnectedPlayers()
        {
            return ConnectedPlayers.Values.ToList();
        }

        public void SwapItems(string player, string typeA, string typeB, int indexA, int indexB)
        {
            int idA, idB;
            PlayerModel p = ConnectedPlayers[player];

            if (typeA == InventoryTypes.Inventory)
            {
                idA = p.InventoryId;
            } else if (typeA == InventoryTypes.Hotbar)
            {
                idA = p.HotbarId;
            } else
            {
                return;
            }

            if (typeB == InventoryTypes.Inventory)
            {
                idB = p.InventoryId;
            } else if (typeB == InventoryTypes.Hotbar)
            {
                idB = p.HotbarId;
            } else
            {
                return;
            }

            InventoryDataModel itemA = ActiveInventories[idA].Where(i => i.Position == indexA).FirstOrDefault();
            InventoryDataModel itemB = ActiveInventories[idB].Where(i => i.Position == indexB).FirstOrDefault();
            int iA = ActiveInventories[idA].IndexOf(itemA);
            int iB = ActiveInventories[idB].IndexOf(itemB);

            if (iA == -1 & iB == -1)
            {
                return;
            }

            if (iA == -1)
            {
                ActiveInventories[idB].Remove(itemB);
                ActiveInventories[idA].Add(new InventoryDataModel(idA, itemB.ItemName, itemB.Amount, indexA));
            } else if (iB == -1)
            {
                ActiveInventories[idA].Remove(itemA);
                ActiveInventories[idB].Add(new InventoryDataModel(idB, itemA.ItemName, itemA.Amount, indexB));
            } else if (itemA.Id == itemB.Id) 
            {
                ActiveInventories[idA].Remove(itemA);
                ActiveInventories[idB][iB] = new InventoryDataModel(idB, itemA.ItemName, itemA.Amount + itemB.Amount, itemB.Position);
            } else
            {
                ActiveInventories[idA][iA] = new InventoryDataModel(idA, itemB.ItemName, itemB.Amount, itemA.Position);
                ActiveInventories[idB][iB] = new InventoryDataModel(idB, itemA.ItemName, itemA.Amount, itemB.Position);
            }
        }

        public void GiveItems(string player, List<Item> items)
        {
            int invId = ConnectedPlayers[player].InventoryId;
            int hotbarId = ConnectedPlayers[player].HotbarId;

            foreach (var x in items)
            {
                var inventory = ActiveInventories[invId];
                var hotbar = ActiveInventories[hotbarId];

                int invIndex = ActiveInventories[invId].FindIndex(i => i.ItemName == x.Data.Name);
                int htbIndex = ActiveInventories[hotbarId].FindIndex(i => i.ItemName == x.Data.Name);

                int newIndex;

                // Not in inventory
                if (invIndex == -1)
                {
                    // And not in hotbar
                    if (htbIndex == -1)
                    {
                        if (inventory.Count == 0)
                        {
                            newIndex = 0;
                        } else
                        {
                            newIndex = NextFreeIndex(inventory.Select(i => i.Position).ToList(), InventorySize);
                        }
                        
                        // Then if there's space in the inventory
                        if (newIndex != -1)
                        {
                            inventory.Add(new InventoryDataModel(invId, x.Data.Name, x.Count, newIndex));
                            // Else if there's space in the hotbar
                        } else
                        {
                            if (hotbar.Count == 0)
                            {
                                newIndex = 0;
                            } else
                            {
                                newIndex = NextFreeIndex(hotbar.Select(i => i.Position).ToList(), HotbarSize);
                            }
                            
                            if (newIndex != -1)
                            {
                                hotbar.Add(new InventoryDataModel(hotbarId, x.Data.Name, x.Count, newIndex));
                            }
                        }
                    } else
                    {
                        int pos = hotbar[htbIndex].Position;
                        hotbar[htbIndex] = new InventoryDataModel(hotbarId, x.Data.Name, hotbar[htbIndex].Amount + x.Count, pos);
                    }
                    // Else if in inventory
                } else
                {
                    int pos = inventory[invIndex].Position;
                    inventory[invIndex] = new InventoryDataModel(invId, x.Data.Name, inventory[invIndex].Amount + x.Count, pos);
                }
                
            }
        }

        public (double X, double Y, int L) GetPosition(string player, int mapId)
        {
            if (WorldsWithPlayers.TryGetValue(mapId, out Dictionary<string, PlayerLocationModel> world))
            {
                if (world.TryGetValue(player, out PlayerLocationModel playerLocation))
                {
                    return (playerLocation.PosX, playerLocation.PosY, playerLocation.Layer);
                }
            }

            return (0, 0, 1);
        }
         
        public string GetDirection(string player)
        {
            return "West";
        }

        public void AddChatMsg(string user, string msg)
        {
            ChatMessages.Remove(user);
            ChatMessages.Add(user, new ChatMsg(msgLifetime, msg));
        }

        public string[] GetChatMsgs()
        {
            var res = from x in ConnectedPlayers.Values
                        select x.Name;
            string[] names = res.ToArray();
            string[] msgs = new string[names.Count()];

            for (int i = 0; i < names.Length; i++)
            {
                if (ChatMessages.TryGetValue(names[i], out ChatMsg val))
                {
                    msgs[i] = val.Msg;
                } else
                {
                    msgs[i] = "";
                }
            }

            return msgs.ToArray();
        }
        
        public void ChangeDirection(string name, string direction)
        {
            
        }

        public void MoveLayer(int mapId, string name, int newLayer)
        {
            if (WorldsWithPlayers.TryGetValue(mapId, out Dictionary<string, PlayerLocationModel> players))
            {
                if (players.TryGetValue(name, out PlayerLocationModel player))
                {
                    double normalizedX = Math.Round(player.PosX, MidpointRounding.ToEven);
                    double normalizedY = Math.Round(player.PosY, MidpointRounding.ToEven);

                    if (ValidPosition((int)normalizedX, (int)normalizedY, newLayer))
                    {
                        WorldsWithPlayers[mapId][name].Layer = newLayer;
                    }
                } 
                else
                {
                    Console.WriteLine("GameWorld\\MoveLayer: WorldsWithPlayers[" + mapId + "] didn't contain key: " + name);
                }
            } 
            else
            {
                Console.WriteLine("GameWorld\\MoveLayer: WorldsWithPlayers didn't contain key: " + mapId);
            }
        }

        public void Move(int mapId, string name, double Vx, double Vy)
        {
            if (WorldsWithPlayers.TryGetValue(mapId, out Dictionary<string, PlayerLocationModel> players))
            {
                if (players.TryGetValue(name, out PlayerLocationModel player))
                {
                    double normalizedX = Math.Round(player.PosX, MidpointRounding.ToEven);
                    double normalizedY = Math.Round(player.PosY, MidpointRounding.ToEven);

                    double newX = player.PosX + Vx / 32;
                    double newY = player.PosY + Vy / 32;

                    if (ValidPosition((int)Math.Ceiling(newX), (int)Math.Ceiling(newY), player.Layer) &&
                        ValidPosition((int)newX, (int)newY, player.Layer) &&
                        ValidPosition((int)Math.Ceiling(newX), (int)newY, player.Layer) &&
                        ValidPosition((int)newX, (int)Math.Ceiling(newY), player.Layer))
                    {
                        WorldsWithPlayers[mapId][name].PosX += Vx / 32;
                        WorldsWithPlayers[mapId][name].PosY += Vy / 32;
                        PlayersLastMoved[name] = DateTime.Now;
                    }
                    else
                    {
                        if (Math.Abs(normalizedX - player.PosX) <= 0.4)
                        {
                            WorldsWithPlayers[mapId][name].PosX = normalizedX;
                        }

                        if (Math.Abs(normalizedY - player.PosY) <= 0.4)
                        {
                            WorldsWithPlayers[mapId][name].PosY = normalizedY;
                        }
                    }
                }
                else
                {
                    Console.WriteLine("GameWorld\\Move: WorldsWithPlayers[" + mapId + "] didn't contain key: " + name);
                }
            }
            else
            {
                Console.WriteLine("GameWorld\\Move: WorldsWithPlayers didn't contain key: " + mapId);
            }
        }

        public void SetPlayerLocation(int mapId, string name, int newX, int newY)
        {
            if (WorldsWithPlayers.TryGetValue(mapId, out Dictionary<string, PlayerLocationModel> players))
            {
                if (players.TryGetValue(name, out PlayerLocationModel player))
                {
                    if (ValidPosition(newX, newY, player.Layer))
                    {
                        WorldsWithPlayers[mapId][name].PosX = newX;
                        WorldsWithPlayers[mapId][name].PosY = newY;
                    }
                } else
                {
                    Console.WriteLine("GameWorld\\SetPlayerLocation: WorldsWithPlayers[" + mapId + "] didn't contain key: " + name);
                }
            } else
            {
                Console.WriteLine("GameWorld\\SetPlayerLocation: WorldsWithPlayers didn't contain key: " + mapId);
            }
        }

        public InventoryDataModel[] GetInventory(string player)
        {
            int id = ConnectedPlayers[player].InventoryId;
            return ActiveInventories[id].ToArray();
        }

        public InventoryDataModel[] GetHotbar(string player)
        {
            int id = ConnectedPlayers[player].HotbarId;
            return ActiveInventories[id].ToArray();
        }

        public bool CanPlaceHere(int targetM, int targetN, int layer, bool isDrop)
        {
            if (targetM < 0 || targetM >= rows)
            {
                return false;
            }
            if (targetN < 0 || targetN >= cols)
            {
                return false;
            }

            if (isDrop)
            {
                return true;
            }

            var tiles = Maps.AccessGroup(WorldId, targetN, targetM);

            if (tiles.Count == 0)
            {
                return false;
            }

            foreach (MapDataModel d in tiles)
            {
                if (d.Layer > layer)
                {
                    if (d.TileId != TileSet["Air"].TileId)
                    {
                        return false;
                    }
                }
            }

            return true;
        }
    }
}
