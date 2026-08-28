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
            while (true)
            {
                //Subscribe to different events which fires off in here for handlers

                // ProcessChatQueue();
            }
        }

        //Chat Messages

        public void ProcessChatQueue()
        {
            //TODO: Change this to all packets that need to be sent.
            lock (ArchipelagoClient.instance.incomingChatQueue)
            {
                while (ArchipelagoClient.instance.incomingChatQueue.Count > 0)
                {
                    string msg = ArchipelagoClient.instance.incomingChatQueue.Dequeue();
                    SendChatToGame(msg);
                }
            }
        }
        
        private void SendChatToGame(string message)
        {
            ConsoleLogging.LogSend(message);
            Packet.ChatMessagePacket chatMessagePacket = new Packet.ChatMessagePacket(ArchipelagoClient.instance.GetConnectionInfo().slotName, message);
            SerialiseAndSend(chatMessagePacket);
        }
        //Checks - Try Chat colour changes
        
        //Receive Deathlink
        public void SendDeathlinkToGame(DeathLink deathLinkObject)
        {
            if (deathLinkObject.Source != ArchipelagoClient.instance.GetConnectionInfo().slotName)
            {
                ConsoleLogging.LogSend($"DeathLink Received - {deathLinkObject.Cause}");
                Packet.DeathLinkPacket deathLinkPacket = new Packet.DeathLinkPacket(deathLinkObject.Source, deathLinkObject.Cause);
                SerialiseAndSend(deathLinkPacket);
                return;
            }
        }

        public void SendStealthLinkToGame(string slotResponsible)
        {
            if (slotResponsible != ArchipelagoClient.instance.GetConnectionInfo().slotName)
            {
                ConsoleLogging.LogSend($"StealthLink Received From: {slotResponsible}");
                Packet.StealthLinkPacket stealthLinkPacket = new Packet.StealthLinkPacket(slotResponsible);

                SerialiseAndSend(stealthLinkPacket);
                return;
            }
        }

        public void SendConnectionStatus()
        {
            Packet.IsConnectedPacket isConnectedPacket = new Packet.IsConnectedPacket(ArchipelagoClient.instance.GetConnectedStatus());
            SerialiseAndSend(isConnectedPacket);
        }
        
        public void ScoutHintToGame(string location, string item)
        {
            Packet.HintPacket hintPacket = new Packet.HintPacket(location, Packet.HintPacket.TypeOfHint.Scout, item);
            ConsoleLogging.LogSend($"Scouted Hint: {location} contains {item}");
            SerialiseAndSend(hintPacket);
        }

        public void SendItemToGame(string itemName, bool isID)
        {
            Packet.ItemPacket itemPacket = new Packet.ItemPacket(itemName, isID);
            SerialiseAndSend(itemPacket);
        }
        
        //Eventually going to need to get the items and feed it through


        void SerialiseAndSend(Packet packet)
        {
            string packetJson = packet.ToJson();
            GameConnector.instance.SendToGame(packetJson);
        }
        
        #endregion
        
        #region GameToServer
        
        //Game -> Server
        //Don't need to serialise into the server, as the server has its own functions.
        //I do need to de-serialise though, as the game will serialise anything it sends to the bridge. 

        public void ProcessGameToServer(string serialisedMessage)
        {
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
                    SendCheckToServer(checkPacket.checkName, checkPacket.isID);
                    break;
                }
                case PacketType.DeathLink:
                {
                    Packet.DeathLinkPacket deathLinkPacket = (Packet.DeathLinkPacket)message;
                    SendDeathLinkToServer(deathLinkPacket.slotName, deathLinkPacket.reason);
                    break;
                }
                case PacketType.StealthLink:
                {
                    Packet.StealthLinkPacket stealthLinkPacket = (Packet.StealthLinkPacket)message;
                    SendStealthLinkToServer(stealthLinkPacket.slotName);
                    break;
                }
                case PacketType.Goal:
                {
                    SendGoalToServer();
                    break;
                }
                case PacketType.Hint:
                {
                    Packet.HintPacket hintPacket = (Packet.HintPacket)message;

                    switch (hintPacket.hintType)
                    {
                        case Packet.HintPacket.TypeOfHint.Normal:
                        {
                            SendChatToServer($"!hint {hintPacket.locationName}");
                            break;
                        }
                        case Packet.HintPacket.TypeOfHint.Scout:
                        {
                            ScoutHintToServer(hintPacket.locationName);
                            break;
                        }
                    }
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
            ConsoleLogging.LogSend("Chat Send: " + ChatMessage);
            ArchipelagoClient.instance.SayToServer(ChatMessage);
        }
        
        //Checks
        void SendCheckToServer(string CheckName, bool isID)
        {
            //TODO: SEND CHECK VALUES
            ArchipelagoClient.instance.SendCheck(CheckName, isID);
        }
        
        //Send Deathlink
        void SendDeathLinkToServer(string slot, string reason)
        {
            ConsoleLogging.LogSend("Deathlink Send: " + reason);
            ArchipelagoClient.instance.SendDeathLinkToServer(slot, reason);
        }

        //Send StealthLink
        void SendStealthLinkToServer(string slot)
        {
            ConsoleLogging.LogSend("StealhLink Send: " + slot);
            ArchipelagoClient.instance.SendStealthLink(slot);
        }
        
        //Goal
        void SendGoalToServer()
        {
            //TODO: TEST
            ConsoleLogging.LogSend("Sending Goal to server.");
            ArchipelagoClient.instance.SendGoalToServer();
        }

        //Send the Slot name to the game, for returning packets
        public void SendSlotnameToGame()
        {
            ConnectionInfo connectionInfo = ArchipelagoClient.instance.GetConnectionInfo();
            Packet.ConnectionPacket connectionPacket = new Packet.ConnectionPacket(connectionInfo.slotName, connectionInfo.serverAddress, connectionInfo.password);
            
            SerialiseAndSend(connectionPacket);
        }
        
        //Scout Hint
        private void ScoutHintToServer(string hintPacketHintName)
        {
            ArchipelagoClient.instance.ScoutHint(hintPacketHintName);
        }
        
        #endregion
    }
}