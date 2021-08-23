using System;
using System.IO;
using System.Timers;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
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

        public readonly Dictionary<string, TileModel> TileSet;
        private Dictionary<int, dynamic> TileDataSet; // <TileId, List of drops to give when tile is broken>
        public readonly Dictionary<string, ItemModel> ItemSet;
        private Dictionary<int, dynamic> ItemDataSet; // <ItemId, Tile to place when item is used>
        private readonly Dictionary<string, Func<dynamic, bool>> FuncList;

        private Dictionary<int, Inventory> ActiveInventories = new Dictionary<int, Inventory>();
        private Dictionary<int, Inventory> ActiveHotbars = new Dictionary<int, Inventory>();
        private Dictionary<string, PlayerModel> ConnectedPlayers = new Dictionary<string, PlayerModel>();

        private Dictionary<int, List<EntityModel>> ActiveEntities = new Dictionary<int, List<EntityModel>>();

        private Dictionary<string, ChatMsg> ChatMessages = new Dictionary<string, ChatMsg>();
        private double msgLifetime = 3;

        public int MaxStackSize = 30;
        public int InventoryRows = 5;
        public int InventoryCols = 5;
        public int HotbarSize = 5;

        private TileModel[][][] World;
        private readonly int Layers = 3;

        private readonly int rows = 25, cols = 25;

        private static readonly Timer UpdateTimer = new Timer();
        private static readonly Timer SaveTimer = new Timer();
        public readonly int UpdateInterval = 15;
        public readonly int SaveInterval = 1000;

        private int GlobalIdCounter = 0;

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
                {"FlowerItem", new ItemModel(10, "FlowerItem", FunctionTypes.Place, "FlowerItem.png") },
                {"BlankItem", new ItemModel(11, "BlankItem", FunctionTypes.Nothing, "BlankItem.png") },
                {"SaplingItem", new ItemModel(12, "SaplingItem", FunctionTypes.Place, "SaplingItem.png") },
            };

            FuncList = new Dictionary<string, Func<dynamic, bool>>()
            {
                {FunctionTypes.Nothing, (dynamic input) => Nothing() },
                {FunctionTypes.Place, (dynamic input) => Place(input) },
                {FunctionTypes.PlaceFloor, (dynamic input) => PlaceFloor(input) },
                {FunctionTypes.UseTool, (dynamic input) => UseTool(input) },
                {FunctionTypes.UseWeapon, (dynamic input) => UseWeapon(input) },

            };

            TileDataSet = new Dictionary<int, dynamic>()
            {
                {TileSet["Tree"].Id, new List<Item>(){new Item(5, ItemSet["LogWallItem"]), new Item(2, ItemSet["SaplingItem"])} },
                {TileSet["Grass"].Id, new List<Item>(){new Item(1, ItemSet["GrassItem"]) } },
                {TileSet["Dirt"].Id, new List<Item>(){new Item(1, ItemSet["DirtItem"]) } },
                {TileSet["Sand"].Id, new List<Item>(){new Item(1, ItemSet["SandItem"]) } },
                {TileSet["LogWall"].Id, new List<Item>(){new Item(1, ItemSet["LogWallItem"]) } },
                {TileSet["Stone"].Id, new List<Item>(){new Item(5, ItemSet["StoneWallItem"]) } },
                {TileSet["StoneWall"].Id, new List<Item>(){new Item(1, ItemSet["StoneWallItem"]) } },
            };

            ItemDataSet = new Dictionary<int, dynamic>()
            {
                {ItemSet["DirtItem"].Id, TileSet["Dirt"]},
                {ItemSet["GrassItem"].Id, TileSet["Grass"] },
                {ItemSet["SandItem"].Id, TileSet["Sand"] },
                {ItemSet["StoneFloorItem"].Id, TileSet["StoneFloor"] },
                {ItemSet["FlowerItem"].Id, TileSet["Flower"] },
                {ItemSet["LogWallItem"].Id, TileSet["LogWall"] },
                {ItemSet["StoneWallItem"].Id, TileSet["StoneWall"] },
                {ItemSet["SaplingItem"].Id, TileSet["Sapling"] },
            };

            World = new TileModel[Layers][][];
            for (int l = 0; l < Layers; l++)
            {
                World[l] = new TileModel[rows][];
                for (int j = 0; j < rows; j++)
                {
                    World[l][j] = new TileModel[cols];
                    for (int k = 0; k < cols; k++)
                    {
                        World[l][j][k] = new TileModel();
                    }
                }
            }

            LoadWorld();
            //WriteWorld();

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

        private async Task WriteWorld()
        {
            using (StreamWriter file = new("Game\\WorldLayers.txt")) {
                string[] layer = new string[Layers];
                for (int l = 0; l < Layers; l++)
                {
                    for (int i = 0; i < rows; i++)
                    {
                        for (int j = 0; j < cols; j++)
                        {
                            layer[l] += World[l][i][i].DataId + "|" + World[l][i][j].Name + ",";
                        }
                    }
                    await file.WriteLineAsync(layer[l]);
                }
            }
        }

        private void LoadWorld()
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
                        World[l][i][j].Name = DataTile[1];
                        if (TileSet.TryGetValue(DataTile[1], out TileModel value))
                        {
                            World[l][i][j] = value;
                            World[l][i][j].DataId = int.Parse(DataTile[0]);
                        }
                        else
                        {
                            World[l][i][j] = TileSet["Grass"];
                        }

                        if (tileIndex < currentLayer.Length - 1)
                        {
                            tileIndex++;
                        }
                    }
                }
            }
        }

        private void Save(Object source, ElapsedEventArgs e)
        {
            using (var scope = _serviceProvider.CreateScope())
            {
                var _db = scope.ServiceProvider.GetService<GameDbContext>();

                _db.World.FromSqlRaw("DELETE FROM World");
                
                for (int i = 0; i < Layers; i++)
                {
                    var tiles = from x in World[i]
                                from y in x
                                select y.Name;
                    string layer = String.Join(',', tiles);
                    //_db.World.Add(new WorldLayerModel(0, layer));
                }

                _db.SaveChanges();
            }
        }

        private void Update(Object source, ElapsedEventArgs e)
        {
            CheckPositions();
            CheckMsgLifetimes();
        }

        private bool ValidPosition(int PosX, int PosY)
        {
            if (PosX >= 0 && PosY >= 0)
            {
                if (PosX < cols && PosY < rows)
                {
                    for (int l = 0; l < Layers; l++)
                    {
                        if (World[l][PosY][PosX].Type != TileTypes.Open)
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
                int normPos = p.PosY * cols + p.PosX;
                if (ActiveEntities.TryGetValue(normPos, out List<EntityModel> val))
                {
                    List<Item> items = new List<Item>();
                    for (int i = 0; i < val.Count; i++)
                    {
                        items.Add(new Item(1, ItemSet[val[i].ItemName]));
                    }
                    GiveItems(p.Name, items);
                    ActiveEntities.Remove(normPos);
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
            var entities = ActiveEntities.Values;


            var res1 = from x in entities
                       from e in x
                       select e.ItemName;
           var res2 = from x in entities
                      from e in x
                      select e.PosY;
            var res3 = from x in entities
                       from e in x
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

            // Tiles
            for (int l = 0; l < Layers; l++)
            {
                for (int i = 0; i < rows; i++)
                {
                    for (int j = 0; j < cols; j++)
                    {
                        res[l][i][j] = World[l][i][j].Img;
                    }
                }
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

        public void AddPlayer(PlayerModel player, bool isNew)
        {
            if (ConnectedPlayers.TryGetValue(player.Name, out PlayerModel val))
            {
                return;
            }

            ConnectedPlayers.Add(player.Name, player);
            if (isNew)
            {
                ActiveInventories.Add(player.InventoryId, new Inventory(new Item(5, ItemSet["SandItem"]), new Item(1, ItemSet["SwordItem"])));
                for (int i = ActiveInventories[player.InventoryId].Items.Count; i < InventoryRows*InventoryCols; i++)
                {
                    ActiveInventories[player.InventoryId].Items.Add(new Item(1, ItemSet["BlankItem"]));
                }

                ActiveHotbars.Add(player.InventoryId, new Inventory(new Item(1, ItemSet["ShovelItem"]), new Item(1, ItemSet["AxeItem"]), 
                    new Item(25, ItemSet["GrassItem"]), new Item(17, ItemSet["FlowerItem"]), new Item(1, ItemSet["PickaxeItem"])));
                for (int i = ActiveHotbars[player.InventoryId].Items.Count; i < HotbarSize; i++)
                {
                    ActiveHotbars[player.InventoryId].Items.Add(new Item(1, ItemSet["BlankItem"]));
                }

            } else
            {
                // load from database
            }
        }

        public List<PlayerModel> GetConnectedPlayers()
        {
            return ConnectedPlayers.Values.ToList();
        }

        public void AddEntity(string itemName, int posY, int posX)
        {
            int normPos = posY * cols + posX;
            if (ActiveEntities.TryGetValue(normPos, out List<EntityModel> val))
            {
                val.Add(new EntityModel(itemName, 1, posX, posY));
            } else
            {
                ActiveEntities.Add(normPos, new List<EntityModel>() {new EntityModel(itemName, 1, posX, posY)});
            }
        }

        public void SwapItems(string player, string typeA, string typeB, int indexA, int indexB)
        {
            Inventory invA;
            Inventory invB;
            if (typeA == InventoryTypes.Inventory)
            {
                invA = ActiveInventories[ConnectedPlayers[player].InventoryId];
            } else if (typeA == InventoryTypes.Hotbar)
            {
                invA = ActiveHotbars[ConnectedPlayers[player].InventoryId];
            } else
            {
                return;
            }

            if (typeB == InventoryTypes.Inventory)
            {
                invB = ActiveInventories[ConnectedPlayers[player].InventoryId];
            } else if (typeB == InventoryTypes.Hotbar)
            {
                invB = ActiveHotbars[ConnectedPlayers[player].InventoryId];
            } else
            {
                return;
            }

            Item temp = invA.Items[indexA];
            invA.Items[indexA] = invB.Items[indexB];
            invB.Items[indexB] = temp;
        }

        public void GiveItems(string player, List<Item> items)
        {
            var id = ConnectedPlayers[player].InventoryId;
            var inventory = ActiveInventories[id];
            var hotbar = ActiveHotbars[id];

            foreach (var x in items)
            {
                int invIndex = inventory.Items.FindIndex(i => i.Data.Id == x.Data.Id);
                int htbIndex = hotbar.Items.FindIndex(i => i.Data.Id == x.Data.Id);

                int newIndex;

                // Not in inventory
                if (invIndex == -1)
                {
                    // And not in hotbar
                    if (htbIndex == -1)
                    {
                        newIndex = inventory.Items.FindIndex(i => i.Data.Id == ItemSet["BlankItem"].Id);
                        // Then if there's space in the inventory
                        if (newIndex != -1)
                        {
                            inventory.Items[newIndex] = x;
                            // Else if there's space in the hotbar
                        } else
                        {
                            newIndex = hotbar.Items.FindIndex(i => i.Data.Id == ItemSet["BlankItem"].Id);
                            if (newIndex != -1)
                            {
                                hotbar.Items[newIndex] = x;
                            }
                        }
                    } else
                    {
                        hotbar.Items[htbIndex] = new Item(hotbar.Items[htbIndex].Count + x.Count, x.Data);
                    }
                    // Else if in inventory
                } else
                {
                    inventory.Items[invIndex] = new Item(inventory.Items[invIndex].Count + x.Count, x.Data);
                }
                
            }
        }

        public int GetNextId()
        {
            GlobalIdCounter++;
            return GlobalIdCounter;
        }

        public (int X, int Y) GetPosition(string player)
        {
            if (ConnectedPlayers.TryGetValue(player, out PlayerModel val))
            {
                return (val.PosX, val.PosY);
            }
            else
            {
                return (0, 0);
            }
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

        public Item[] GetInventory(string player)
        {
            int id = ConnectedPlayers[player].InventoryId;
            return ActiveInventories[id].Items.ToArray();
        }

        public Item[] GetHotbar(string player)
        {
            int id = ConnectedPlayers[player].InventoryId;
            return ActiveHotbars[id].Items.ToArray();
        }

        public void UseItem(string action, string player, string item, int hotbarIndex, int targetM, int targetN)
        {
            if (action == "Use")
            {
                var res = FuncList[ItemSet[item].UseFunc]((player, item, hotbarIndex, targetM, targetN));
            } else if (action == "Drop")
            {
                if (targetM < 0 || targetM >= rows)
                {
                    return;
                }
                if (targetN < 0 || targetN >= cols)
                {
                    return;
                }

                AddEntity(item, targetM, targetN);
                Inventory playerHotbar = ActiveHotbars[ConnectedPlayers[player].InventoryId];

                int newCount = playerHotbar.Items[hotbarIndex].Count - 1;
                if (newCount <= 0)
                {
                    playerHotbar.Items[hotbarIndex] = new Item(1, ItemSet["BlankItem"]);
                }
                else
                {
                    playerHotbar.Items[hotbarIndex] = new Item(newCount, playerHotbar.Items[hotbarIndex].Data);
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

                if (targetM < 0 || targetM >= rows)
                {
                    return false;
                }
                if (targetN < 0 || targetN >= cols)
                {
                    return false;
                }

                TileModel targetTile = World[0][targetM][targetN];
                Inventory playerHotbar = ActiveHotbars[ConnectedPlayers[player].InventoryId];

                for (int l = 1; l < Layers; l++)
                {
                    if (World[l][targetM][targetN].Id != TileSet["Air"].Id)
                    {
                        return false;
                    }
                }

                if (targetTile.Type == TileTypes.Open)
                {
                    if (targetTile.SubType != TileSubTypes.Air && targetTile.SubType != TileSubTypes.Water)
                    {
                         if (ItemDataSet.TryGetValue(ItemSet[item].Id, out dynamic val))
                        {
                            World[1][targetM][targetN] = val;
                            int newCount = playerHotbar.Items[hotbarIndex].Count - 1;
                            if (newCount <= 0)
                            {
                                playerHotbar.Items[hotbarIndex] = new Item(1, ItemSet["BlankItem"]);
                            }
                            else
                            {
                                playerHotbar.Items[hotbarIndex] = new Item(newCount, playerHotbar.Items[hotbarIndex].Data);
                            }
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

                if (targetM < 0 || targetM >= rows)
                {
                    return false;
                }
                if (targetN < 0 || targetN >= cols)
                {
                    return false;
                }

                TileModel targetTile = World[0][targetM][targetN];
                Inventory playerHotbar = ActiveHotbars[ConnectedPlayers[player].InventoryId];

                for (int l = 1; l < Layers; l++)
                {
                    if (World[l][targetM][targetN].Id != TileSet["Air"].Id)
                    {
                        return false;
                    }
                }

                if (targetTile.Id == TileSet["StoneFloor"].Id)
                {
                    if (ItemSet[item].Id == ItemSet["DirtItem"].Id)
                    {
                        if (ItemDataSet.TryGetValue(ItemSet[item].Id, out dynamic val))
                        {
                            World[0][targetM][targetN] = val;
                            int newCount = playerHotbar.Items[hotbarIndex].Count - 1;
                            if (newCount <= 0)
                            {
                                playerHotbar.Items[hotbarIndex] = new Item(1, ItemSet["BlankItem"]);
                            } else
                            {
                                playerHotbar.Items[hotbarIndex] = new Item(newCount, playerHotbar.Items[hotbarIndex].Data);
                            }
                            
                            return true;
                        }
                    }
                }

                if (targetTile.Id == TileSet["Dirt"].Id)
                {
                    if (ItemDataSet.TryGetValue(ItemSet[item].Id, out dynamic val))
                    {
                        World[0][targetM][targetN] = val;
                        int newCount = playerHotbar.Items[hotbarIndex].Count - 1;
                        if (newCount <= 0)
                        {
                            playerHotbar.Items[hotbarIndex] = new Item(1, ItemSet["BlankItem"]);
                        }
                        else
                        {
                            playerHotbar.Items[hotbarIndex] = new Item(newCount, playerHotbar.Items[hotbarIndex].Data);
                        }
                        return true;
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

                if (item == "AxeItem")
                {
                    for (int l = Layers - 1; l > 0; l--)
                    {
                        TileModel targetTile = World[l][targetM][targetN];
                        if (targetTile.SubType == TileSubTypes.Wood)
                        {
                            World[l][targetM][targetN] = TileSet["Air"];

                            if (TileDataSet.TryGetValue(targetTile.Id, out dynamic val))
                            {
                                GiveItems(player, val);
                            }
                            return true;
                        }
                    }
                } else if (item == "ShovelItem")
                {
                    for (int l = 1; l < Layers; l++)
                    {
                        if (World[l][targetM][targetN].Id != TileSet["Air"].Id)
                        {
                            return false;
                        }
                    }

                    TileModel targetTile = World[0][targetM][targetN];
                    if (targetTile.SubType == TileSubTypes.Soil)
                    {
                        if (targetTile.Id == TileSet["Dirt"].Id)
                        {
                            World[0][targetM][targetN] = TileSet["StoneFloor"];
                        }
                        else
                        {
                            World[0][targetM][targetN] = TileSet["Dirt"];
                        }

                        if (TileDataSet.TryGetValue(targetTile.Id, out dynamic val))
                        {
                            GiveItems(player, val);
                        }
                        return true;
                    }
                } else if (item == "PickaxeItem")
                {
                    for (int l = Layers - 1; l > 0; l--)
                    {
                        TileModel targetTile = World[l][targetM][targetN];
                        if (targetTile.Type == TileTypes.Sturdy && targetTile.SubType == TileSubTypes.Stone)
                        {
                            World[l][targetM][targetN] = TileSet["Air"];

                            if (TileDataSet.TryGetValue(targetTile.Id, out dynamic val))
                            {
                                GiveItems(player, val);
                            }
                            return true;
                        }
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
