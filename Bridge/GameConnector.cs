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

        private NamedPipeServerStream pipeServerToBridge;
        private NamedPipeServerStream pipeServerToGame;
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
            Task.Run(StartPipeForBridge);
            Task.Run(StartPipeForGame);
        }

        public void DisconnectFromGame()
        {
            EndPipe();
        }

        private void StartPipeForBridge()
        {
            if (pipeServerToBridge != null && pipeServerToBridge.IsConnected)
            {
                return;
            }

            pipeServerToBridge = new NamedPipeServerStream("EscAPists2PipeToBridge", PipeDirection.In);

            ConsoleLogging.LogInfo("(ToBridge) Waiting for connection...");
            pipeServerToBridge.WaitForConnection();

            ConsoleLogging.LogSuccess("(ToBridge) Connection established to game.");

            reader = new StreamReader(pipeServerToBridge);
            
            Listen();
        }

        private void StartPipeForGame()
        {
            if (pipeServerToGame != null && pipeServerToGame.IsConnected)
            {
                return;
            }
            
            pipeServerToGame = new NamedPipeServerStream("EscAPists2PipeToGame", PipeDirection.Out);
            
            ConsoleLogging.LogInfo("(ToGame) Waiting for connection...");
            pipeServerToGame.WaitForConnection();

            ConsoleLogging.LogSuccess("(ToGame) Connection established to game.");
            
            writer = new StreamWriter(pipeServerToGame)
            {
                AutoFlush = true
            };
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
            pipeServerToBridge?.Dispose();
            pipeServerToBridge = null;
            
            pipeServerToGame?.Dispose();
            pipeServerToGame = null;

            writer?.Dispose();
            writer = null;

            reader?.Dispose();
            reader = null;

            instance = null;
        }

        public void SendToGame(string message)
        {
            writer?.WriteLine(message);
            writer?.Flush();
        }

        private void ReadMessage(string message)
        {
            ArchipelagoClient.instance.SayToServer(message);
        }

        public bool GetConnectionStatus()
        {
            if (pipeServerToBridge.IsConnected)
            {
                return true;
            }

            return false;
        }
    }
}