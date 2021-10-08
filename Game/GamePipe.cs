using System;
using System.Linq;
using System.IO;
using System.IO.Pipes;
using System.Threading.Tasks;

namespace OnlineBuildingGame.Game
{
    public class GamePipe
    {
        public GamePipe()
        {
            /*var namedPipeServer = new NamedPipeServerStream("C#Pipe", PipeDirection.InOut, 1, PipeTransmissionMode.Byte);
            var streamReader = new StreamReader(namedPipeServer);
            namedPipeServer.WaitForConnection();

            var writer = new StreamWriter(namedPipeServer);
            writer.Write("Hello from C#");
            writer.Write((char)0);
            writer.Flush();
            namedPipeServer.WaitForPipeDrain();

            string received = streamReader.ReadLine();
            namedPipeServer.Dispose();*/
        }

        public async Task Communicate()
        {

        }
    }
}
