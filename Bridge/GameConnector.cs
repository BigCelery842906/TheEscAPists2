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

            try
            {
                pipeGameToBridge = new NamedPipeServerStream("EscAPists2PipeToBridge", PipeDirection.In);
            }
            catch (Exception e)
            {
                // ConsoleLogging.LogWarning(e.ToString());
            }

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

            try
            {
                pipeBridgeToGame = new NamedPipeServerStream("EscAPists2PipeToGame", PipeDirection.Out);
            }
            catch (Exception e)
            {
                // ConsoleLogging.LogWarning(e.ToString());
            }

            ConsoleLogging.LogInfo("(ToGame) Pipe Created, Waiting for connection...");
            pipeBridgeToGame.WaitForConnection();

            ConsoleLogging.LogSuccess("(ToGame) Connection established to game.");
            
            writer = new StreamWriter(pipeBridgeToGame)
            {
                AutoFlush = true
            };
            MessageHandler.instance.SendNoConnection();
            CheckConnections();
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
                        ConsoleLogging.LogWarning("Game disconnected. Please restart both the bridge and the game.");
                        RestartPipes();
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

        void RestartPipes()
        {
            ConsoleLogging.LogDebug("Restarting Pipes due to game disconnect...");
            DisconnectFromGame();
            StartConnectionToGame();
            if (ArchipelagoClient.instance.GetConnectedStatus())
            {
                ConsoleLogging.LogDebug("Resetting the queues for when the game reconnects.");
                ArchipelagoClient.instance.ResetQueues();
            }
        }

        void EndPipe()
        {
            if (pipeBridgeToGame != null)
            {
                pipeBridgeToGame.Disconnect();
                ConsoleLogging.LogSuccess($"Pipe Bridge to game closed. {pipeBridgeToGame.IsConnected}");
                // pipeClientForBridge.Close();
                // pipeBridgeToGame = null;
            }
            if (pipeGameToBridge != null)
            {
                pipeGameToBridge.Disconnect();
                ConsoleLogging.LogSuccess($"Pipe game to bridge closed. {pipeGameToBridge.IsConnected}");
                // pipeClientForGame.Close();
                // pipeGameToBridge = null;
            }

            // writer?.Dispose();
            // writer = null;
            //
            // reader?.Dispose();
            // reader = null;
            //
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


        public void SendConnectionInfo()
        {
            MessageHandler.instance.SendSlotnameToGame();
            MessageHandler.instance.SendConnectionStatus();
            ConsoleLogging.LogDebug("Sending the connection info to the game now.");
        }
    }
}