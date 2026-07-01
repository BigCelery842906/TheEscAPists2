using System.Text.Json;
using Archipelago.MultiClient.Net.BounceFeatures.DeathLink;
using BC_Dual_Ref_Library;

namespace TheEscAPists2Bridge
{
    public class MessageHandler
    {
        
        #region Class Initialisation
        
        public static MessageHandler instance;

        public MessageHandler()
        {
            if (instance != null && instance != this)
            {
                ConsoleLogging.LogInfo("Message Handler instance already running.");
                return;
            }

            if (instance == null)
            {
                instance = this;
                ConsoleLogging.LogSuccess("Message Handler instance created.");
            }

            Task.Run(ProcessServerToGame);
        }

        ~MessageHandler()
        {
            instance = null;
        }
        
        #endregion
        
        #region ServerToGame
        
        //Server -> Game
        //Then I need to process it game side too, based on incoming packets.
        //Some of the outgoing stuff has already been set up, and arguably should be easier to sort because it's not 2 2-way systems

        private void ProcessServerToGame()
        {
            //Subscribe to different events which fires off in here for handlers

            ProcessChatQueue();
        }
        
        //Chat Messages

        private void ProcessChatQueue()
        {
            lock (ArchipelagoClient.instance.incomingChatQueue)
            {
                while (ArchipelagoClient.instance.incomingChatQueue.Count > 0)
                {
                    string msg = ArchipelagoClient.instance.incomingChatQueue.Dequeue();
                    SendChatToServer(msg);
                }
            }
        }
        
        public void SendChatToGame(string message)
        {
            Packet.ChatMessagePacket chatMessagePacket = new Packet.ChatMessagePacket(ArchipelagoClient.instance.GetConnectionInfo().slotName, message);
            ConsoleLogging.LogSend(chatMessagePacket.ToString());
            SerialiseAndSend(chatMessagePacket);
        }
        //Checks - Try Chat colour changes
        
        //Receive Deathlink
        public void SendDeathlinkToGame(DeathLink deathLinkObject)
        {
            if (deathLinkObject.Source != ArchipelagoClient.instance.GetConnectionInfo().slotName)
            {
                Packet.DeathlinkPacket deathlinkPacket = new Packet.DeathlinkPacket(deathLinkObject.Source, deathLinkObject.Cause);
                ConsoleLogging.LogSend(deathlinkPacket.ToString());
                SerialiseAndSend(deathlinkPacket);
            }
        }
        
        //Eventually going to need to get the items and feed it through


        void SerialiseAndSend(Packet packet)
        {
            string packetJson = packet.ToJson();
            ConsoleLogging.LogDebug(packetJson);
            GameConnector.instance.SendToGame(packetJson);
        }
        
        #endregion
        
        #region GameToServer
        
        //Game -> Server
        //Don't need to serialise into the server, as the server has its own functions.
        //I do need to de-serialise though, as the game will serialise anything it sends to the bridge. 

        public void ProcessGameToServer(string serialisedMessage)
        {
            ConsoleLogging.LogDebug(serialisedMessage);
            if (serialisedMessage == null)
            {
                return;
            }
            
            Packet? message = Packet.Deserialise(serialisedMessage);
            
            switch (message.Type)
            {
                case PacketType.Connection:
                {
                    TryConnectToServer((Packet.ConnectionPacket)message);
                    break;
                }
                case PacketType.ChatMessage:
                {
                    Packet.ChatMessagePacket chatMessagePacket = (Packet.ChatMessagePacket)message;
                    SendChatToServer(chatMessagePacket.chatMessage);
                    break;
                }
                case PacketType.Check:
                {
                    Packet.CheckPacket checkPacket = (Packet.CheckPacket)message;
                    SendCheckToServer(checkPacket.checkName);
                    break;
                }
                case PacketType.Deathlink:
                {
                    Packet.DeathlinkPacket deathlinkPacket = (Packet.DeathlinkPacket)message;
                    SendDeathlinkToServer(deathlinkPacket.slot, deathlinkPacket.reason);
                    break;
                }
                case PacketType.Goal:
                {
                    SendGoalToServer();
                    break;
                }
                default:
                {
                    ConsoleLogging.LogWarning("Unknown message type: " + message.Type);
                    break;
                }
            }
        }
        
        //Connection
        void TryConnectToServer(Packet.ConnectionPacket Connection)
        {
            //TODO: SET VALUES CORRECTLY
            ArchipelagoClient.instance.SetConnectionValues(Connection.game, Connection.slotName, Connection.serverAddress, Connection.password, Connection.version);
            ConsoleLogging.LogSend(ArchipelagoClient.instance.GetConnectionInfo().ToString());
            ArchipelagoClient.instance.TryToConnect();
        }
        
        //Say
        void SendChatToServer(string ChatMessage)
        {
            //TODO: FEED CHAT MESSAGE TO SERVER
            ConsoleLogging.LogSend("Chat Send: " + ChatMessage);
            ArchipelagoClient.instance.SayToServer(ChatMessage);
        }
        
        //Checks
        void SendCheckToServer(string CheckName)
        {
            //TODO: SEND CHECK VALUES
            ConsoleLogging.LogSend("Check Send: " + CheckName);
            ArchipelagoClient.instance.SendCheck(CheckName);
        }
        
        //Send Deathlink
        void SendDeathlinkToServer(string slot, string reason)
        {
            ConsoleLogging.LogSend("Deathlink Send: " + reason);
            ArchipelagoClient.instance.SendDeathlinkToServer(slot, reason);
        }
        
        //Goal
        void SendGoalToServer()
        {
            ConsoleLogging.LogSend("Sending Goal to server.");
            ArchipelagoClient.instance.SendGoalToServer();
        }
        //
        //
        
        #endregion

        
    }
}