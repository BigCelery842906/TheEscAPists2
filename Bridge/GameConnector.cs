using System;
using System.IO.Pipes;
using System.Threading.Tasks;
using BC_Dual_Ref_Library;
using UnityEngine;

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
            ConsoleLogging.LogDebug("Starting to Create Pipe");
            // ConsoleLogging.LogAllTypes();
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

            ConsoleLogging.LogInfo("(ToBridge) Waiting for connection...");
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
            
            ConsoleLogging.LogInfo("(ToGame) Waiting for connection...");
            pipeBridgeToGame.WaitForConnection();

            ConsoleLogging.LogSuccess("(ToGame) Connection established to game.");
            
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
                    ConsoleLogging.LogDebug("Waiting for line....");
                    string message = reader.ReadLine();

                    if (message == null)
                    {
                        ConsoleLogging.LogWarning("Game disconnected. Holding Pipe for reconnect.");
                        return;
                    }

                    MessageHandler.instance.ProcessGameToServer(message);
                    ConsoleLogging.LogInfo("Received from game -> " + message);
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
            ConsoleLogging.LogSend(message);
            writer?.WriteLine(message);
            writer?.Flush();
        }

        private void ReadMessage(string message)
        {
            ArchipelagoClient.instance.SayToServer(message);
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
    }
}