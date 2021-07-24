using System.Collections.Generic;
using System.Linq;

namespace OnlineBuildingGame
{
    public class LoginManager
    {
        private Dictionary<string, int> ConnectedUsers;

        public LoginManager()
        {
            ConnectedUsers = new Dictionary<string, int>();
        }

        public void AddConnection(string Name)
        {
            if (ConnectedUsers.ContainsKey(Name))
            {
                ConnectedUsers[Name]++;
            }
            else
            {
                ConnectedUsers.Add(Name, 1);
            }
        }

        public void RemoveConnection(string Name)
        {
            if (ConnectedUsers.ContainsKey(Name))
            {
                ConnectedUsers[Name]--;
                if (ConnectedUsers[Name] == 0)
                {
                    ConnectedUsers.Remove(Name);
                }
            }
        }

        public List<string> GetConnectedUsers()
        {
            return ConnectedUsers.Keys.ToList();
        }
    }
}
