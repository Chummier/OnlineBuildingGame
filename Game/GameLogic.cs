using System;
using System.Linq;
using System.Timers;
using System.IO;
using System.IO.Pipes;
using System.Threading.Tasks;
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

        private static readonly Timer updateTimer = new Timer();

        public GameLogic(GameWorld world, IHubContext<GameHub> hubContext, IServiceProvider serviceProvider)
        {
            _world = world;
            _hubContext = hubContext;
            _serviceProvider = serviceProvider;

            updateTimer.Interval = _world.UpdateInterval;
            updateTimer.Elapsed += OnTimedEvent;
            updateTimer.AutoReset = true;
            updateTimer.Enabled = true;

            var namedPipeServer = new NamedPipeServerStream("C#Pipe", PipeDirection.InOut, 1, PipeTransmissionMode.Byte);
            var streamReader = new StreamReader(namedPipeServer);
            
        }

        private async void OnTimedEvent(Object source, ElapsedEventArgs e)
        {
            int layers = _world.getNumLayers();
            (int rows, int cols) = _world.getSize();
            string[][][] currentMap = _world.getWorldSprites();
            (string[] playerNames, int[] playerPositionsY, int[] playerPositionsX) = _world.getPlayerLocations();
            (string[] entityNames, int[] entityPositionsY, int[] entityPositionsX) = _world.getEntities();
            string[] msgs = _world.GetChatMsgs();

            await _hubContext.Clients.All.SendAsync("GetWorld", currentMap, layers, rows, cols);
            await _hubContext.Clients.All.SendAsync("GetPlayers", playerNames, playerPositionsY, playerPositionsX, msgs);
            await _hubContext.Clients.All.SendAsync("GetEntities", entityNames, entityPositionsY, entityPositionsX);

            foreach (PlayerModel p in _world.GetConnectedPlayers())
            {
                (int X, int Y) = _world.GetPosition(p.Name);
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
            }            
        }
    }
}
