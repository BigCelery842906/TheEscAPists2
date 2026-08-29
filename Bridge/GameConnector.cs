using System.IO.Pipes;

namespace TheEscAPists2Bridge
{
    public class GameConnector
    {
        public static GameConnector instance;

        private NamedPipeServerStream pipeGameToBridge;
        private NamedPipeServerStream pipeBridgeToGame;
        private StreamReader reader;
        private StreamWriter writer;


        public void ReceiveConnectionValues(string game, string slotName, string serverAddress, string password,
            Version gameVersion)
        {
            ArchipelagoClient.instance.SetConnectionValues(game, slotName, serverAddress, password, gameVersion);
        }

        public GameConnector()
        {
            if (instance != null && instance != this)
            {
                ConsoleLogging.LogInfo("Game Connector instance already running.");
                return;
            }

            if (instance == null)
            {
                instance = this;
                ConsoleLogging.LogSuccess("Game Connector instance created.");
            }
            
            StartConnectionToGame();
        }

        ~GameConnector()
        {
            EndPipe();
        }

        public void StartConnectionToGame()
        {
            if (!GetConnectionStatusGameToBridge())
            {
                Task.Run(StartPipeForBridge);
            }

            if (!GetConnectionStatusBridgeToGame())
            {
                Task.Run(StartPipeForGame);
            }
        }

        public void DisconnectFromGame()
        {
            EndPipe();
        }

        private void StartPipeForBridge()
        {
            if (pipeGameToBridge != null && pipeGameToBridge.IsConnected)
            {
                return;
            }

            pipeGameToBridge = new NamedPipeServerStream("EscAPists2PipeToBridge", PipeDirection.In);

            ConsoleLogging.LogInfo("(ToBridge) Pipe Created, Waiting for connection...");
            pipeGameToBridge.WaitForConnection();

            ConsoleLogging.LogSuccess("(ToBridge) Connection established to game.");

            reader = new StreamReader(pipeGameToBridge);
            
            Listen();
        }

        private void StartPipeForGame()
        {
            if (pipeBridgeToGame != null && pipeBridgeToGame.IsConnected)
            {
                return;
            }
            
            pipeBridgeToGame = new NamedPipeServerStream("EscAPists2PipeToGame", PipeDirection.Out);
            
            ConsoleLogging.LogInfo("(ToGame) Pipe Created, Waiting for connection...");
            pipeBridgeToGame.WaitForConnection();

            ConsoleLogging.LogSuccess("(ToGame) Connection established to game.");
            
            CheckConnections();
            writer = new StreamWriter(pipeBridgeToGame)
            {
                AutoFlush = true
            };
            
            MessageHandler.instance.SendConnectionStatus();
        }

        void Listen()
        {
            try
            {
                while (true)
                {
                    string message = reader.ReadLine();

                    if (message == null)
                    {
                        ConsoleLogging.LogWarning("Game disconnected. Holding Pipe for reconnect.");
                        return;
                    }

                    MessageHandler.instance.ProcessGameToServer(message);
                }
            }
            catch (Exception e)
            {
                ConsoleLogging.LogError(ConsoleLogging.ErrorCode.NoGameConnection, e.ToString());
            }
        }

        void EndPipe()
        {
            pipeGameToBridge?.Dispose();
            pipeGameToBridge = null;
            
            pipeBridgeToGame?.Dispose();
            pipeBridgeToGame = null;

            writer?.Dispose();
            writer = null;

            reader?.Dispose();
            reader = null;

            instance = null;
        }

        public void SendToGame(string message)
        {
            // ConsoleLogging.LogSend(message);
            writer?.WriteLine(message);
            writer?.Flush();
        }

        public bool GetConnectionStatusBridgeToGame()
        {
            if (pipeGameToBridge != null && pipeGameToBridge.IsConnected)
            {
                return true;
            }

            return false;
        }
        
        public bool GetConnectionStatusGameToBridge()
        {
            if (pipeBridgeToGame != null && pipeBridgeToGame.IsConnected)
            {
                return true;
            }
            
            return false;
        }

        void CheckConnections()
        {
            if (GetConnectionStatusBridgeToGame() && GetConnectionStatusGameToBridge())
            {
                Program.StartConnectionInfo();
            }
        }
    }
}