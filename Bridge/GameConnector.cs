using System;
using System.IO.Pipes;
using System.Threading.Tasks;
using UnityEngine;

namespace TheEscAPists2Bridge
{
    public class GameConnector
    {
        public static GameConnector instance;

        private NamedPipeServerStream pipeServer;
        private StreamReader reader;
        private StreamWriter writer;
        
        
        public void ReceiveConnectionValues(string game, string slotName, string serverAddress, string password, Version gameVersion)
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
            Task.Run(StartPipe);
        }

        public void DisconnectFromGame()
        {
            EndPipe();
        }

        private void StartPipe()
        {
            if (pipeServer != null && pipeServer.IsConnected)
            {
                return;
            }
            pipeServer = new NamedPipeServerStream("EscAPists2Pipe", PipeDirection.InOut);
            
            ConsoleLogging.LogInfo("Waiting for connection...");
            pipeServer.WaitForConnection();
            
            ConsoleLogging.LogSuccess("Connection established to game.");
            
            reader = new StreamReader(pipeServer);
            writer = new StreamWriter(pipeServer)
            {
                AutoFlush = true
            };

            // Task.Run(Listen);
            Listen();
        }

        void Listen()
        {
            while (true)
            {
                
                string message = reader.ReadLine();

                if (message == null)
                {
                    ConsoleLogging.LogWarning("Game disconnected. Holding Pipe for reconnect.");
                    break;
                }

                MessageHandler.instance.ProcessGameToServer(message);
                ConsoleLogging.LogInfo("Sending to Server -> " + message);
                
                writer.WriteLine(message);
                
            }
        }

        void EndPipe()
        {
            pipeServer?.Dispose();
            pipeServer = null;
            
            writer?.Dispose();
            writer = null;
            
            reader?.Dispose();
            reader = null;

            instance = null;
        }

        public void SendToGame(string message)
        {
            writer?.WriteLine(message);
        }

        private void ReadMessage(string message)
        {
            ArchipelagoClient.instance.SayToServer(message);
        }

        public bool GetConnectionStatus()
        {
            if (pipeServer.IsConnected)
            {
                return true;
            }

            return false;
        }
    }
}