using System;
using System.Timers;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.Mvc;
using OnlineBuildingGame.Game;

namespace OnlineBuildingGame.Hubs
{
    public class GameHub: Hub
    {
        private readonly GameWorld _world;
        private readonly GameLogic _gameLogic;
        private readonly LoginManager _loginManager;

        public GameHub(GameWorld world, GameLogic gameLogic, LoginManager loginManager)
        {
            _world = world;
            _gameLogic = gameLogic;
            _loginManager = loginManager;
        }

        public override async Task OnConnectedAsync()
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, Context.User.Identity.Name);
            _loginManager.AddConnection(Context.User.Identity.Name);
        }

        public override async Task OnDisconnectedAsync(Exception exception)
        {
            _loginManager.RemoveConnection(Context.User.Identity.Name);
        }

        public async Task GetMovementInput(int Vx, int Vy)
        {
            _world.Move(0, Context.User.Identity.Name, Vx, Vy);
        }

        public async Task GetDirection(string direction)
        { 
            _world.ChangeDirection(Context.User.Identity.Name, direction);
        }

        public async Task GetItemUse(string Action, string Item, int Index, int TargetM, int TargetN)
        {
            _gameLogic.UseItem(Action, Context.User.Identity.Name, Item, Index, TargetM, TargetN);
        }

        public async Task GetTileUse(int TargetM, int TargetN)
        {
            _gameLogic.UseTile(Context.User.Identity.Name, TargetM, TargetN);
        }

        public async Task StopUsingTile()
        {
            _world.removeTileInUse(Context.User.Identity.Name);
        }

        public async Task GetDrawingData(string data, int targetM, int targetN)
        {
            _world.updateCanvasImage(Context.User.Identity.Name, data, targetM, targetN);
        }

        public async Task SwapItems(string typeA, string typeB, int indexA, int indexB)
        {
            _world.SwapItems(Context.User.Identity.Name, typeA, typeB, indexA, indexB);
        }

        public async Task SendMessage(string from, string msg)
        {
            if (!msg.Equals(""))
            {
                _world.AddChatMsg(from, msg);
            }
        }
    }
}
