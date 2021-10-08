using System;
using System.IO;
using System.Timers;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using OnlineBuildingGame.Models;
using OnlineBuildingGame.Data;
using OnlineBuildingGame.Hubs;

namespace OnlineBuildingGame.Game
{
    public class GameWorld
    {
        private readonly IHubContext<GameHub> _hubContext;
        private readonly IServiceProvider _serviceProvider;

        public readonly Dictionary<string, TileModel> TileSet; // <TileName, TileModel>
        private Dictionary<int, List<Item>> TileDataSet; // <TileId, List of drops to give when tile is broken>
        public readonly Dictionary<string, ItemModel> ItemSet; // <ItemName, ItemModel>
        private Dictionary<int, TileModel> ItemDataSet; // <ItemId, Tile to place when item is used>
        private readonly Dictionary<string, Func<dynamic, bool>> FuncList; // <FuncName, Func<input, output>>

        private Dictionary<int, List<InventoryDataModel>> ActiveInventories = new Dictionary<int, List<InventoryDataModel>>(); // <InventoryId, List<items>>
        private Dictionary<string, PlayerModel> ConnectedPlayers = new Dictionary<string, PlayerModel>();

        private List<EntityModel> ActiveEntities = new List<EntityModel>();
        private Dictionary<int, (float, string)> ActivePlants = new Dictionary<int, (float, string)>(); // <NormalizedPosition, (time to grow, tile to become when grown)>

        private Dictionary<string, ChatMsg> ChatMessages = new Dictionary<string, ChatMsg>(); // <PlayerName, ChatMsg>, only 1 message at a time per player
        private double msgLifetime = 3;

        public int MaxStackSize = 30;
        public int InventorySize = 25;
        public int InventoryRows = 5;
        public int InventoryCols = 5;
        public int HotbarSize = 5;

        private List<MapDataModel> MainMap;

        private int WorldId = 0;
        private readonly int Layers = 3;

        private readonly int rows = 25, cols = 25;

        private static readonly Timer UpdateTimer = new Timer();
        private static readonly Timer SaveTimer = new Timer();
        public readonly int UpdateInterval = 15;
        public readonly int SaveInterval = 5000;

        public GameWorld(IHubContext<GameHub> hubContext, IServiceProvider serviceProvider)
        {
            _hubContext = hubContext;
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
            };

            FuncList = new Dictionary<string, Func<dynamic, bool>>()
            {
                {FunctionTypes.Nothing, (dynamic input) => Nothing() },
                {FunctionTypes.Place, (dynamic input) => Place(input) },
                {FunctionTypes.PlaceFloor, (dynamic input) => PlaceFloor(input) },
                {FunctionTypes.UseTool, (dynamic input) => UseTool(input) },
                {FunctionTypes.UseWeapon, (dynamic input) => UseWeapon(input) },
                {FunctionTypes.Plant, (dynamic input) => Plant(input) },
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
            };

            MainMap = new List<MapDataModel>();

            LoadWorldFromFile();
            //LoadWorld();

            UpdateTimer.Interval = UpdateInterval;
            UpdateTimer.Elapsed += Update;
            UpdateTimer.AutoReset = true;
            UpdateTimer.Enabled = true;

            SaveTimer.Interval = SaveInterval;
            SaveTimer.Elapsed += Save;
            SaveTimer.AutoReset = true;
            SaveTimer.Enabled = true;
        }

        private void GenerateWorld(int x, int y)
        {

        }

        private void LoadWorldFromFile()
        {
            string[] layers = File.ReadAllLines("Game\\WorldLayers.txt");
            for (int l = 0; l < Layers; l++)
            {
                string[] currentLayer = layers[l].Split(',').Where(s => !string.IsNullOrEmpty(s)).Select(s => s.Trim()).ToArray();
                int tileIndex = 0;
                for (int i = 0; i < rows; i++)
                {
                    for (int j = 0; j < cols; j++)
                    {
                        string[] DataTile = currentLayer[tileIndex].Split('|');
                        MapDataModel temp = new MapDataModel(WorldId, TileSet[DataTile[1]].TileId, l, DataTile[1], i, j);

                        MainMap.Add(temp);

                        if (tileIndex < currentLayer.Length - 1)
                        {
                            tileIndex++;
                        }
                    }
                }
            }         
        }

        private void LoadWorld()
        {
            using (var scope = _serviceProvider.CreateScope())
            {
                var _db = scope.ServiceProvider.GetService<GameDbContext>();

                var map = _db.MapData.Where(d => d.MapId == WorldId).OrderBy(t => t.Layer).ThenBy(t => t.PosY).ThenBy(t => t.PosX);
                MainMap.AddRange(map);

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
                _db.Maps.Add(new MapModel(WorldId, "Main", rows, cols));

                var map = _db.MapData.Where(d => d.MapId == WorldId).OrderBy(t => t.Layer).ThenBy(t => t.PosY).ThenBy(t => t.PosX).ToList();
                for (int i = 0; i < map.Count; i++)
                {
                    map[i].TileId = MainMap[i].TileId;
                    map[i].TileName = MainMap[i].TileName;
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

        private bool ValidPosition(int PosX, int PosY)
        {
            if (PosX >= 0 && PosY >= 0)
            {
                if (PosX < cols && PosY < rows)
                {
                    for (int l = 0; l < Layers; l++)
                    {
                        string tile = MainMap.Where(t => t.PosY == PosY && t.PosX == PosX)
                                              .Where(t => t.Layer == l).First().TileName;
                        if (TileSet[tile].Type != TileTypes.Open)
                        {
                            return false;
                        }
                    }
                    return true;
                }
            }

            return false;
        }

        private void CheckPositions()
        {
            foreach (PlayerModel p in ConnectedPlayers.Values)
            {
                var entities = ActiveEntities.Where(e => e.PosY == p.PosY && e.PosX == p.PosX);
                List<Item> items = new List<Item>();

                foreach (EntityModel e in entities)
                {
                    items.Add(new Item(e.Amount, ItemSet[e.Name]));
                }

                GiveItems(p.Name, items);
                ActiveEntities.RemoveAll(e => entities.Contains(e));
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

        public (string[], int[], int[]) getPlayerLocations()
        {
            var res1 = from x in ConnectedPlayers.Values
                       select x.Name;
            var res2 = from x in ConnectedPlayers.Values
                      select x.PosY;
            var res3 = from x in ConnectedPlayers.Values
                       select x.PosX;
            return (res1.ToArray(), res2.ToArray(), res3.ToArray());
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

            foreach (MapDataModel d in MainMap.ToArray())
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

                    PlayerModel p = new PlayerModel(player, invId, hotbarId);

                    ActiveInventories.Add(invId, new List<InventoryDataModel>());
                    ActiveInventories.Add(hotbarId, new List<InventoryDataModel>());

                    _db.Players.Add(p);

                    _db.Inventories.Add(new InventoryModel(invId, InventorySize));
                    _db.Inventories.Add(new InventoryModel(hotbarId, HotbarSize));
                    _db.SaveChanges();

                    ConnectedPlayers.Add(player, p);
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
                }

                //GiveItems(player, new List<Item>() { new Item(5, ItemSet["SunflowerItem"]) });
                //GiveItems(player, new List<Item>() { new Item(5, ItemSet["ChestItem"]) });
                //GiveItems(player, new List<Item>() { new Item(1, ItemSet["GlovesItem"]), new Item(1, ItemSet["AxeItem"]), new Item(1, ItemSet["ShovelItem"])});
                //GiveItems(player, new List<Item>() { new Item(1, ItemSet["PickaxeItem"]), new Item(1, ItemSet["SwordItem"]) });
            }
        }

        public List<PlayerModel> GetConnectedPlayers()
        {
            return ConnectedPlayers.Values.ToList();
        }

        public void AddEntity(string itemName, int posY, int posX)
        {
            ActiveEntities.Add(new EntityModel(itemName, posY, posX, 1));
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

        public (int X, int Y) GetPosition(string player)
        {
            return (ConnectedPlayers[player].PosX, ConnectedPlayers[player].PosY);
        }

        public string GetDirection(string player)
        {
            Direction dir = ConnectedPlayers[player].Facing;
            if (dir == Direction.North)
            {
                return "North";
            }
            if (dir == Direction.South)
            {
                return "South";
            }
            if (dir == Direction.East)
            {
                return "East";
            }
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
            

        public void Move(string name, int Vx, int Vy)
        {
            (int currentX, int currentY) = (ConnectedPlayers[name].PosX, ConnectedPlayers[name].PosY);

            Direction temp = ConnectedPlayers[name].Facing;

            if (Vx != 0)
            {
                ConnectedPlayers[name].Facing = (Direction)Vx;
            }

            if (Vy != 0)
            {
                ConnectedPlayers[name].Facing = (Direction)(Vy * 10);
            }

            if (ConnectedPlayers[name].Facing != temp)
            {
                return;
            }

            if (ValidPosition(currentX + Vx, currentY + Vy))
            {
                ConnectedPlayers[name].PosX += Vx;
                ConnectedPlayers[name].PosY += Vy;
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

        public void SubtractItem(string player, string item, int hotbarIndex)
        {
            var playerHotbar = ActiveInventories[ConnectedPlayers[player].HotbarId];
            var itemModel = playerHotbar.Where(i => i.Position == hotbarIndex).First();
            int itemIndex = playerHotbar.IndexOf(itemModel);

            if (itemIndex == -1)
            {
                return;
            }

            int newCount = itemModel.Amount - 1;
            if (newCount <= 0)
            {
                playerHotbar[itemIndex] = new InventoryDataModel(0, "BlankItem", 1, hotbarIndex);
            }
            else
            {
                playerHotbar[itemIndex] = new InventoryDataModel(itemModel.Id, itemModel.ItemName, newCount, hotbarIndex);
            }
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

            var targetTiles = MainMap.Where(t => t.PosY == targetM && t.PosX == targetN);
            var target = targetTiles.Where(t => t.Layer == layer).FirstOrDefault();

            if (target == null)
            {
                return false;
            }

            foreach (MapDataModel d in targetTiles)
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

        public void AlterTile(int targetM, int targetN, int layer, TileModel newTile)
        {

        }

        public void UseItem(string action, string player, string item, int hotbarIndex, int targetM, int targetN)
        {
            if (action == "Use")
            {
                var res = FuncList[ItemSet[item].UseFunc]((player, item, hotbarIndex, targetM, targetN));
            } else if (action == "Drop")
            {
                if (CanPlaceHere(targetM, targetN, 0, true))
                {
                    AddEntity(item, targetM, targetN);
                    SubtractItem(player, item, hotbarIndex);
                } 
            }
        }

        public bool Nothing()
        {
            return false;
        }

        public bool Place(dynamic input)
        {
            if (input is (string, string, int, int, int))
            {
                string player = input.Item1;
                string item = input.Item2;
                int hotbarIndex = input.Item3;
                int targetM = input.Item4;
                int targetN = input.Item5;

                if (!CanPlaceHere(targetM, targetN, 1, false))
                {
                    return false;
                }    

                var target = MainMap.Where(t => t.PosY == targetM && t.PosX == targetN).Where(t => t.Layer == 1).First();
                int targetIndex = MainMap.IndexOf(target);

                if (target == null)
                {
                    return false;
                }

                TileModel tile = TileSet[target.TileName];

                if (tile.Type == TileTypes.Open)
                {
                    if (tile.SubType != TileSubTypes.Air && tile.SubType != TileSubTypes.Water)
                    {
                         if (ItemDataSet.TryGetValue(ItemSet[item].Id, out TileModel val))
                        {
                            MainMap[targetIndex] = new MapDataModel(WorldId, val.TileId, target.Layer, val.Name, target.PosY, target.PosX);

                            SubtractItem(player, item, hotbarIndex);
                            return true;
                        }
                    }
                }
            }
            return false;
        }

        public bool PlaceFloor(dynamic input)
        {
            if (input is (string, string, int, int, int))
            {
                string player = input.Item1;
                string item = input.Item2;
                int hotbarIndex = input.Item3;
                int targetM = input.Item4;
                int targetN = input.Item5;

                if (!CanPlaceHere(targetM, targetN, 0, false))
                {
                    return false;
                }

                var target = MainMap.Where(t => t.PosY == targetM && t.PosX == targetN).Where(t => t.Layer == 0).First();
                int targetIndex = MainMap.IndexOf(target);

                if (target == null)
                {
                    return false;
                }

                TileModel tile = TileSet[target.TileName];

                if (tile.TileId == TileSet["StoneFloor"].TileId)
                {
                    if (ItemSet[item].Id == ItemSet["DirtItem"].Id)
                    {
                        if (ItemDataSet.TryGetValue(ItemSet[item].Id, out TileModel val))
                        {
                            MainMap[targetIndex] = new MapDataModel(WorldId, val.TileId, target.Layer, val.Name, target.PosY, target.PosX);

                            SubtractItem(player, item, hotbarIndex);
                            return true;
                        }
                    }
                }

                if (target.TileId == TileSet["Dirt"].TileId)
                {
                    if (ItemDataSet.TryGetValue(ItemSet[item].Id, out TileModel val))
                    {
                        MainMap[targetIndex] = new MapDataModel(WorldId, val.TileId, target.Layer, val.Name, target.PosY, target.PosX);

                        SubtractItem(player, item, hotbarIndex);
                        return true;
                    }
                }
            }
            return false;
        }

        public bool Plant(dynamic input)
        {
            if (input is (string, string, int, int, int))
            {
                string player = input.Item1;
                string item = input.Item2;
                int hotbarIndex = input.Item3;
                int targetM = input.Item4;
                int targetN = input.Item5;

                if (!CanPlaceHere(targetM, targetN, 0, false))
                {
                    return false;
                }

                var target = MainMap.Where(t => t.PosY == targetM && t.PosX == targetN).Where(t => t.Layer == 0).First();
                int targetIndex = MainMap.IndexOf(target);

                if (target == null)
                {
                    return false;
                }

                TileModel tile = TileSet[target.TileName];

                if (tile.Type == TileTypes.Open)
                {
                    if (tile.SubType == TileSubTypes.Soil)
                    {
                        if (ItemDataSet.TryGetValue(ItemSet[item].Id, out TileModel val))
                        {
                            MainMap[targetIndex] = new MapDataModel(WorldId, val.TileId, target.Layer, val.Name, target.PosY, target.PosX);

                            SubtractItem(player, item, hotbarIndex);

                            if (item == "FlowerItem")
                            {

                            } else if (item == "SaplingItem")
                            {
                                ActivePlants.Add(targetM * cols + targetN, (3f, "Tree"));
                            }
                            return true;
                        }
                    }
                }
            }
            return false;
        }

        public bool UseTool(dynamic input)
        {
            if (input is (string, string, int, int, int))
            {
                string player = input.Item1;
                string item = input.Item2;
                int hotbarIndex = input.Item3;
                int targetM = input.Item4;
                int targetN = input.Item5;

                if (targetM < 0 || targetM >= rows)
                {
                    return false;
                }
                if (targetN < 0 || targetN >= cols)
                {
                    return false;
                }

                var targetTiles = MainMap.Where(t => t.PosY == targetM && t.PosX == targetN);

                if (item == "AxeItem")
                {
                    var targetTile = targetTiles.Where(t => t.Layer == 1).First();
                    int tileIndex = MainMap.IndexOf(targetTile);

                    TileModel tile = TileSet[targetTile.TileName];

                    if (tile.SubType == TileSubTypes.Wood)
                    {
                        MainMap[tileIndex] = new MapDataModel(WorldId, TileSet["Air"].TileId, 1, TileSet["Air"].Name, targetM, targetN);

                        if (TileDataSet.TryGetValue(tile.TileId, out List<Item> val))
                        {
                            GiveItems(player, val);
                        }
                        return true;
                    }
                } else if (item == "ShovelItem")
                {
                    var targetTile = targetTiles.Where(t => t.Layer == 0).First();
                    int tileIndex = MainMap.IndexOf(targetTile);

                    TileModel tile = TileSet[targetTile.TileName];

                    if (tile.SubType == TileSubTypes.Soil)
                    {
                        if (tile.TileId == TileSet["Dirt"].TileId)
                        {
                            MainMap[tileIndex] = new MapDataModel(WorldId, TileSet["StoneFloor"].TileId, 0, TileSet["StoneFloor"].Name, targetM, targetN);
                        } else
                        {
                            MainMap[tileIndex] = new MapDataModel(WorldId, TileSet["Dirt"].TileId, 0, TileSet["Dirt"].Name, targetM, targetN);
                        }

                        if (TileDataSet.TryGetValue(tile.TileId, out List<Item> val))
                        {
                            GiveItems(player, val);
                        }
                        return true;
                    }
                } else if (item == "PickaxeItem")
                {
                    var targetTile = targetTiles.Where(t => t.Layer == 1).First();
                    int tileIndex = MainMap.IndexOf(targetTile);

                    TileModel tile = TileSet[targetTile.TileName];

                    if (tile.Type == TileTypes.Sturdy && tile.SubType == TileSubTypes.Stone)
                    {
                        MainMap[tileIndex] = new MapDataModel(WorldId, TileSet["Air"].TileId, 1, TileSet["Air"].Name, targetM, targetN);

                        if (TileDataSet.TryGetValue(tile.TileId, out List<Item> val))
                        {
                            GiveItems(player, val);
                        }
                        return true;
                    }
                } else if (item == "GlovesItem")
                {
                    var targetTile = targetTiles.Where(t => t.Layer == 1).First();
                    int tileIndex = MainMap.IndexOf(targetTile);

                    TileModel tile = TileSet[targetTile.TileName];

                    if (tile.SubType == TileSubTypes.Loose)
                    {
                        MainMap[tileIndex] = new MapDataModel(WorldId, TileSet["Air"].TileId, 1, TileSet["Air"].Name, targetM, targetN);

                        if (TileDataSet.TryGetValue(tile.TileId, out List<Item> val))
                        {
                            GiveItems(player, val);
                        }
                        return true;
                    }
                }
            }
            return false;
        }

        public bool UseWeapon(dynamic input)
        {
            if (input is (string, string, int, int, int))
            {
                string player = input.Item1;
                string item = input.Item2;
                int hotbarIndex = input.Item3;
                int targetM = input.Item4;
                int targetN = input.Item5;

                if (targetM < 0 || targetM >= rows)
                {
                    return false;
                }
                if (targetN < 0 || targetN >= cols)
                {
                    return false;
                }

                Direction facing = ConnectedPlayers[player].Facing;
                int Vx = 0;
                int Vy = 0;

                if (facing == Direction.North)
                {
                    Vy = -1;
                } else if (facing == Direction.South)
                {
                    Vy = 1;
                } else if (facing == Direction.East)
                {
                    Vx = 1;
                } else if (facing == Direction.West)
                {
                    Vx = -1;
                }            

                if (item == "SwordItem")
                {
                    foreach (PlayerModel p in ConnectedPlayers.Values)
                    {
                        if (p.PosY == targetM && p.PosX == targetN)
                        {
                            Move(p.Name, Vx, Vy);
                        }
                    }
                }
            }
            return false;
        }
    }
}
