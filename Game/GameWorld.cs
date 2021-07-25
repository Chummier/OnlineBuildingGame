using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace OnlineBuildingGame.Game
{
    public struct TileProperties
    {
        bool isBreakable;
        bool canEnter;
        bool canMove;
        string img;
        
        public TileProperties(bool breakable, bool enterable, bool movable, string srcimg)
        {
            isBreakable = breakable;
            canEnter = enterable;
            canMove = movable;
            img = srcimg;
        }
    }

    public struct GameTile{
        public string name { get; set; }
        public TileProperties data { get; }

        public GameTile(string Name, TileProperties Data)
        {
            name = Name;
            data = Data;
        }
    }

    public class GameWorld
    {
        private readonly Dictionary<string, TileProperties> TileSet;

        private GameTile[][] World;
        private readonly int dim = 25;

        public GameWorld()
        {
            TileSet = new Dictionary<string, TileProperties>()
            {
                {"Tree", new TileProperties(true, false, false, "tree") },
            };

            World = new GameTile[dim][];
            for (int i = 0; i < dim; i++)
            {
                World[i] = new GameTile[dim];
            }

            string[] TileNames = File.ReadAllLines("Game\\CleanWorld.txt");
            int tileIndex = 0;

            for (int i = 0; i < dim; i++)
            {
                for (int j = 0; j < dim; j++)
                {
                    World[i][j].name = TileNames[tileIndex];
                    if (tileIndex < TileNames.Length-1)
                    {
                        tileIndex++;
                    }
                }
            }
        }

        public int getSize()
        {
            return dim;
        }

        public GameTile[][] getWorld()
        {
            return World;
        }

        public void updateTile(int x, int y, GameTile newTile)
        {
            World[x][y] = newTile;
        }

        public GameTile getTile(int x, int y)
        {
            return World[x][y];
        }
    }
}
