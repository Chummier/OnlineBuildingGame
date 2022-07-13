using System;
using System.Linq;
using System.Timers;
using System.IO;
using System.IO.Pipes;
using System.Collections.Generic;
using Microsoft.AspNetCore.SignalR;
using OnlineBuildingGame.Models;
using OnlineBuildingGame.Hubs;

namespace OnlineBuildingGame.Game
{
    public class GameLogic
    {
        private readonly GameWorld _world;
        private readonly IHubContext<GameHub> _hubContext;
        private readonly IServiceProvider _serviceProvider;

        public readonly Dictionary<string, TileModel> TileSet; // <TileName, TileModel>
        public readonly Dictionary<string, ItemModel> ItemSet; // <ItemName, ItemModel>
        public Dictionary<int, List<Item>> TileDataSet; // <TileId, List of drops to give when tile is broken>
        public Dictionary<int, TileModel> ItemDataSet; // <ItemId, Tile to place when item is used>

        public readonly Dictionary<string, Func<dynamic, bool>> FuncList; // <FuncName, Func<input, output>>

        private static readonly Timer updateTimer = new Timer();

        private readonly int rows = 25, cols = 25;

        public GameLogic(GameWorld world, IHubContext<GameHub> hubContext, IServiceProvider serviceProvider)
        {
            _world = world;
            _hubContext = hubContext;
            _serviceProvider = serviceProvider;

            TileSet = _world.GetTileSet();
            ItemSet = _world.GetItemSet();
            TileDataSet = _world.GetTileDataSet();
            ItemDataSet = _world.GetItemDataSet();

            updateTimer.Interval = _world.UpdateInterval;
            updateTimer.Elapsed += OnTimedEvent;
            updateTimer.AutoReset = true;
            updateTimer.Enabled = true;

            var namedPipeServer = new NamedPipeServerStream("C#Pipe", PipeDirection.InOut, 1, PipeTransmissionMode.Byte);
            var streamReader = new StreamReader(namedPipeServer);

            FuncList = new Dictionary<string, Func<dynamic, bool>>()
            {
                {FunctionTypes.Nothing, (dynamic input) => Nothing() },
                {FunctionTypes.Place, (dynamic input) => Place(input) },
                {FunctionTypes.PlaceFloor, (dynamic input) => PlaceFloor(input) },
                {FunctionTypes.PlaceGateway, (dynamic input) => PlaceGateway(input) },
                {FunctionTypes.UseTool, (dynamic input) => UseTool(input) },
                {FunctionTypes.UseWeapon, (dynamic input) => UseWeapon(input) },
                {FunctionTypes.Plant, (dynamic input) => Plant(input) },
                {FunctionTypes.UseCanvas, (dynamic input) => UseCanvas(input) },
            };
        }

        private async void OnTimedEvent(Object source, ElapsedEventArgs e)
        {
            int layers = _world.getNumLayers();
            (int rows, int cols) = _world.getSize();
            string[][][] currentMap = _world.getWorldSprites();
            (string[] playerNames, double[] playerPositionsX, double[] playerPositionsY) = _world.getPlayerLocations(0);
            (string[] entityNames, int[] entityPositionsY, int[] entityPositionsX) = _world.getEntities();
            (int[] canvasP1y, int[] canvasP1x, int[] canvasP2y, int[] canvasP2x, string[] images) = _world.getCanvases();
            string[] msgs = _world.GetChatMsgs();

            await _hubContext.Clients.All.SendAsync("GetWorld", currentMap, layers, rows, cols);
            await _hubContext.Clients.All.SendAsync("GetPlayers", playerNames, playerPositionsY, playerPositionsX, msgs);
            await _hubContext.Clients.All.SendAsync("GetEntities", entityNames, entityPositionsY, entityPositionsX);
            await _hubContext.Clients.All.SendAsync("GetCanvasPositions", canvasP1y, canvasP1x, canvasP2y, canvasP2x, images);

            foreach (PlayerModel p in _world.GetConnectedPlayers())
            {
                (double X, double Y, int layer) = _world.GetPosition(p.Name, 0);
                string direction = _world.GetDirection(p.Name);

                await _hubContext.Clients.Group(p.Name).SendAsync("GetPosition", X, Y);
                await _hubContext.Clients.Group(p.Name).SendAsync("GetDirection", direction);

                var inventory = _world.GetInventory(p.Name);
                var hotbar = _world.GetHotbar(p.Name);

                var amounts = from i in inventory
                              select i.Amount;
                var items = from i in inventory
                            select i.ItemName;
                var positions = from i in inventory
                                select i.Position;
                await _hubContext.Clients.Group(p.Name).SendAsync("GetInventory", amounts.ToArray(), items.ToArray(), positions.ToArray());

                amounts = from i in hotbar
                          select i.Amount;
                items = from i in hotbar
                        select i.ItemName;
                positions = from i in hotbar
                            select i.Position;
                await _hubContext.Clients.Group(p.Name).SendAsync("GetHotbar", amounts.ToArray(), items.ToArray(), positions.ToArray());

                (string type, dynamic data) = _world.getTileInUse(p.Name);
                if (!type.Equals("None"))
                {
                    await _hubContext.Clients.Group(p.Name).SendAsync("GetTileInUse", type, (string)data);
                }
            }            
        }

        public void AddEntity(string itemName, int posY, int posX)
        {
            _world.ActiveEntities.Add(new EntityModel(itemName, 0, posY, posX, 1, 1));
        }

        public void SubtractItem(string player, string item, int hotbarIndex)
        {
            var playerHotbar = _world.ActiveInventories[_world.ConnectedPlayers[player].HotbarId];
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

        public void UseItem(string action, string player, string item, int hotbarIndex, int targetM, int targetN)
        {
            if (action == "Use")
            {
                var res = FuncList[ItemSet[item].UseFunc]((player, item, hotbarIndex, targetM, targetN));
            }
            else if (action == "Drop")
            {
                if (_world.CanPlaceHere(targetM, targetN, 0, true))
                {
                    AddEntity(item, targetM, targetN);
                    SubtractItem(player, item, hotbarIndex);
                }
            }
        }

        public bool UseTile(string player, int targetM, int targetN)
        {
            if (!_world.Maps.TryAccess(_world.WorldId, targetN, targetM, 1, out MapDataModel target))
            {
                return false;
            }

            TileModel tile = TileSet[target.TileName];

            if (tile.SubType == TileSubTypes.Canvas)
            {
                var canvas = _world.ActiveCanvases.Where(c => targetM >= c.Pos1.Item1 && targetM <= c.Pos2.Item1)
                    .Where(c => targetN >= c.Pos1.Item2 && targetN <= c.Pos2.Item2).FirstOrDefault();
                int canvasIndex = _world.ActiveCanvases.IndexOf(canvas);

                if (canvasIndex > -1)
                {
                    var tiles = _world.TilesInUse.Values.Where(t => t.Type == "Canvas");
                    foreach (var t in tiles)
                    {
                        int index;
                        if (Int32.TryParse(t.Data, out index))
                        {
                            if (index == canvasIndex)
                            {
                                // already in use
                                return false;
                            }
                        }
                    }
                    _world.TilesInUse.Add(player, new TileInUse("Canvas", targetM, targetN, canvasIndex.ToString()));
                }
            }

            return false;
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

                if (!_world.CanPlaceHere(targetM, targetN, 1, false))
                {
                    return false;
                }

                if (!_world.Maps.TryAccess(_world.WorldId, targetN, targetM, 1, out MapDataModel target))
                {
                    return false;
                }

                TileModel tile = TileSet[target.TileName];

                if (tile.Type == TileTypes.Open)
                {
                    if (tile.SubType == TileSubTypes.Air)
                    {
                        if (ItemDataSet.TryGetValue(ItemSet[item].Id, out TileModel val))
                        {
                            _world.Maps.Modify(_world.WorldId, new MapDataModel(_world.WorldId, val.TileId, val.Name, target.PosX, target.PosY, target.Layer));
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

                if (!_world.CanPlaceHere(targetM, targetN, 0, false))
                {
                    return false;
                }

                if (!_world.Maps.TryAccess(_world.WorldId, targetN, targetM, 0, out MapDataModel target))
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
                            _world.Maps.Modify(_world.WorldId, new MapDataModel(_world.WorldId, val.TileId, val.Name, target.PosX, target.PosY, target.Layer));
                            SubtractItem(player, item, hotbarIndex);
                            return true;
                        }
                    }
                }

                if (target.TileId == TileSet["Dirt"].TileId)
                {
                    if (ItemSet[item].Id != ItemSet["DirtItem"].Id)
                    {
                        if (ItemDataSet.TryGetValue(ItemSet[item].Id, out TileModel val))
                        {
                            _world.Maps.Modify(_world.WorldId, new MapDataModel(_world.WorldId, val.TileId, val.Name, target.PosX, target.PosY, target.Layer));
                            SubtractItem(player, item, hotbarIndex);
                            return true;
                        }
                    }
                }
            }
            return false;
        }

        public bool PlaceGateway(dynamic input)
        {
            if (input is (string, string, int, int, int))
            {
                string player = input.Item1;
                string item = input.Item2;
                int hotbarIndex = input.Item3;
                int targetM = input.Item4;
                int targetN = input.Item5;

                if (!_world.CanPlaceHere(targetM, targetN, 1, false))
                {
                    return false;
                }

                if (!_world.Maps.TryAccess(_world.WorldId, targetN, targetM, 1, out MapDataModel target))
                {
                    return false;
                }

                TileModel tile = TileSet[target.TileName];

                if (tile.Type == TileTypes.Open)
                {
                    if (tile.SubType == TileSubTypes.Air)
                    {
                        if (ItemDataSet.TryGetValue(ItemSet[item].Id, out TileModel val))
                        {
                            _world.Maps.Modify(_world.WorldId, new MapDataModel(_world.WorldId, val.TileId, val.Name, target.PosX, target.PosY, target.Layer));
                            SubtractItem(player, item, hotbarIndex);
                            return true;
                        }
                    }
                }
            }
            return false;
        }

        public bool PlaceCanvas((int, int) pos1, (int, int) pos2, int canvasIndex)
        {
            if (Math.Abs(pos1.Item1 - pos2.Item1) <= 2)
            {
                if (Math.Abs(pos1.Item2 - pos2.Item2) <= 3)
                {
                    (int, int) topLeft, bottomRight;

                    if (pos2.Item1 < pos1.Item1)
                    {
                        if (pos2.Item2 < pos1.Item2)
                        {
                            topLeft = pos2;
                            bottomRight = pos1;
                        }
                        else
                        {
                            topLeft = (pos2.Item1, pos1.Item2);
                            bottomRight = (pos1.Item1, pos2.Item2);
                        }
                    }
                    else
                    {
                        if (pos1.Item2 < pos2.Item2)
                        {
                            topLeft = pos1;
                            bottomRight = pos2;
                        }
                        else
                        {
                            topLeft = (pos1.Item1, pos2.Item2);
                            bottomRight = (pos2.Item1, pos1.Item2);
                        }
                    }

                    for (int i = topLeft.Item1; i <= bottomRight.Item1; i++)
                    {
                        for (int j = topLeft.Item2; j <= bottomRight.Item2; j++)
                        {
                            if (!_world.CanPlaceHere(i, j, 1, false))
                            {
                                return false;
                            }

                        }
                    }

                    _world.ActiveCanvases[canvasIndex].Pos1 = topLeft;
                    _world.ActiveCanvases[canvasIndex].Pos2 = bottomRight;

                    for (int i = topLeft.Item1; i <= bottomRight.Item1; i++)
                    {
                        for (int j = topLeft.Item2; j <= bottomRight.Item2; j++)
                        {
                            if (_world.Maps.TryAccess(_world.WorldId, j, i, 1, out MapDataModel tile))
                            {
                                _world.Maps.Modify(_world.WorldId, new MapDataModel(_world.WorldId, TileSet["CanvasMiddle"].TileId, TileSet["CanvasMiddle"].Name, j, i, 1));
                            }
                        }
                    }
                    return true;
                }
            }
            return false;
        }

        public bool RemoveCanvas(int targetM, int targetN)
        {
            var canvas = _world.ActiveCanvases.Where(c => targetM >= c.Pos1.Item1 && targetM <= c.Pos2.Item1)
                                        .Where(c => targetN >= c.Pos1.Item2 && targetN <= c.Pos2.Item2).FirstOrDefault();
            int canvasIndex = _world.ActiveCanvases.IndexOf(canvas);

            if (canvasIndex < 0)
            {
                return false;
            }

            for (int i = canvas.Pos1.Item1; i <= canvas.Pos2.Item1; i++)
            {
                for (int j = canvas.Pos1.Item2; j <= canvas.Pos2.Item2; j++)
                {
                    if (_world.Maps.TryAccess(_world.WorldId, j, i, 1, out MapDataModel tile))
                    {
                        _world.Maps.Modify(_world.WorldId, new MapDataModel(_world.WorldId, TileSet["Air"].TileId, TileSet["Air"].Name, j, i, 1));
                    }
                    else
                    {
                        return false;
                    }
                }
            }

            _world.ActiveCanvases.Remove(canvas);
            return true;
        }

        public bool UseCanvas(dynamic input)
        {
            if (input is (string, string, int, int, int))
            {
                string player = input.Item1;
                string item = input.Item2;
                int hotbarIndex = input.Item3;
                int targetM = input.Item4;
                int targetN = input.Item5;

                if (!_world.CanPlaceHere(targetM, targetN, 0, false))
                {
                    return false;
                }

                var canvas = _world.ActiveCanvases.Where(c => c.Placed == false && c.Owner == player).FirstOrDefault();
                if (canvas == null)
                {
                    _world.ActiveCanvases.Add(new CanvasData(player, (targetM, targetN), 1));
                }
                else
                {
                    int index = _world.ActiveCanvases.IndexOf(canvas);
                    if (PlaceCanvas(_world.ActiveCanvases[index].Pos1, (targetM, targetN), index))
                    {
                        _world.ActiveCanvases[index].Placed = true;
                    }
                    else
                    {

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

                if (!_world.CanPlaceHere(targetM, targetN, 0, false))
                {
                    return false;
                }

                if (!_world.Maps.TryAccess(_world.WorldId, targetN, targetM, 1, out MapDataModel target))
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
                            _world.Maps.Modify(_world.WorldId, new MapDataModel(_world.WorldId, val.TileId, val.Name, target.PosX, target.PosY, target.Layer));
                            SubtractItem(player, item, hotbarIndex);

                            if (item == "FlowerItem")
                            {

                            }
                            else if (item == "SaplingItem")
                            {
                                _world.ActivePlants.Add(targetM * cols + targetN, (3f, "Tree"));
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

                var targetTiles = _world.Maps.AccessGroup(_world.WorldId, targetN, targetM);

                if (item == "AxeItem")
                {
                    var targetTile = targetTiles.Where(t => t.Layer == 1).First();

                    TileModel tile = TileSet[targetTile.TileName];

                    if (tile.SubType == TileSubTypes.Wood)
                    {
                        _world.Maps.Modify(_world.WorldId, new MapDataModel(_world.WorldId, TileSet["Air"].TileId, TileSet["Air"].Name, targetN, targetM, 1));
                        if (TileDataSet.TryGetValue(tile.TileId, out List<Item> val))
                        {
                            _world.GiveItems(player, val);
                        }
                        return true;
                    }
                    else if (tile.SubType == TileSubTypes.Canvas)
                    {
                        return RemoveCanvas(targetM, targetN);
                    }
                }
                else if (item == "ShovelItem")
                {
                    if (targetTiles.Where(t => t.Layer > 0).Where(t => t.TileId != TileSet["Air"].TileId).Count() != 0)
                    {
                        return false;
                    }

                    var targetTile = targetTiles.Where(t => t.Layer == 0).First();

                    TileModel tile = TileSet[targetTile.TileName];

                    if (tile.SubType == TileSubTypes.Soil)
                    {
                        if (tile.TileId == TileSet["Dirt"].TileId)
                        {
                            _world.Maps.Modify(_world.WorldId, new MapDataModel(_world.WorldId, TileSet["StoneFloor"].TileId, TileSet["StoneFloor"].Name, targetN, targetM, 0));
                        }
                        else
                        {
                            _world.Maps.Modify(_world.WorldId, new MapDataModel(_world.WorldId, TileSet["Dirt"].TileId, TileSet["Dirt"].Name, targetN, targetM, 0));
                        }

                        if (TileDataSet.TryGetValue(tile.TileId, out List<Item> val))
                        {
                            _world.GiveItems(player, val);
                        }
                        return true;
                    }
                }
                else if (item == "PickaxeItem")
                {
                    var targetTile = targetTiles.Where(t => t.Layer == 1).First();

                    TileModel tile = TileSet[targetTile.TileName];

                    if (tile.Type == TileTypes.Sturdy && tile.SubType == TileSubTypes.Stone)
                    {
                        _world.Maps.Modify(_world.WorldId, new MapDataModel(_world.WorldId, TileSet["Air"].TileId, TileSet["Air"].Name, targetN, targetM, 1));

                        if (TileDataSet.TryGetValue(tile.TileId, out List<Item> val))
                        {
                            _world.GiveItems(player, val);
                        }
                        return true;
                    }
                }
                else if (item == "GlovesItem")
                {
                    var targetTile = targetTiles.Where(t => t.Layer == 1).First();

                    TileModel tile = TileSet[targetTile.TileName];

                    if (tile.SubType == TileSubTypes.Loose)
                    {
                        _world.Maps.Modify(_world.WorldId, new MapDataModel(_world.WorldId, TileSet["Air"].TileId, TileSet["Air"].Name, targetN, targetM, 1));

                        if (TileDataSet.TryGetValue(tile.TileId, out List<Item> val))
                        {
                            _world.GiveItems(player, val);
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

                /*Direction facing = _world.ConnectedPlayers[player].Facing;
                int Vx = 0;
                int Vy = 0;

                if (facing == Direction.North)
                {
                    Vy = -1;
                }
                else if (facing == Direction.South)
                {
                    Vy = 1;
                }
                else if (facing == Direction.East)
                {
                    Vx = 1;
                }
                else if (facing == Direction.West)
                {
                    Vx = -1;
                }

                if (item == "SwordItem")
                {
                    foreach (PlayerModel p in _world.ConnectedPlayers.Values)
                    {
                        if (p.PosY == targetM && p.PosX == targetN)
                        {
                            _world.Move(p.Name, Vx, Vy);
                        }
                    }
                }*/
            }
            return false;
        }
    }
}
