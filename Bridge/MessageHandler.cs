using System.Text.Json;
using System.Text.Json.Serialization;
using TheEscAPists2Bridge.PacketLibrary;

namespace TheEscAPists2Bridge
{
    namespace PacketLibrary
    {
        public enum PacketType
        {
            Connection,
            ChatMessage,
            Check,
            Deathlink,
            Goal,
        }

        public class Packet
        {
            public PacketType Type { get; set; }

            public string ToJson()
            {
                var options = new JsonSerializerOptions
                {
                    Converters = { new PacketConverter() },
                    IncludeFields = true,
                };
                
                return JsonSerializer.Serialize(this, options);
            }

            public static Packet? Deserialise(string json)
            {
                var options = new JsonSerializerOptions
                {
                    Converters = { new PacketConverter() },
                    IncludeFields = true,
                };
                
                return JsonSerializer.Deserialize<Packet>(json, options);
            }

            public class ConnectionPacket : Packet
            {
                // game, slotName, serverAddress, password, gameVersion
                public string game = ModInfo.GameName;
                public string slotName;
                public string serverAddress;
                public string password = "";
                public Version version = ModInfo.GameVersion;
                
                
                public ConnectionPacket()
                {
                    Type = PacketType.Connection;
                }

                public ConnectionPacket(string slotName, string serverAddress, string password = "")
                {
                    Type = PacketType.Connection;
                    this.slotName = slotName;
                    this.serverAddress = serverAddress;
                    this.password = password;
                }
            }

            public class MessagePacket : Packet
            {
                public string chatMessage = "";
                
                public MessagePacket()
                {
                    Type = PacketType.ChatMessage;
                }

                public MessagePacket(string message)
                {
                    Type = PacketType.ChatMessage;
                    this.chatMessage = message;
                }
            }

            public class CheckPacket : Packet
            {
                public string checkName = "";

                public CheckPacket()
                {
                    Type = PacketType.Check;
                }

                public CheckPacket(string checkName)
                {
                    Type = PacketType.Check;
                    this.checkName = checkName;
                }
            }

            public class DeathlinkPacket : Packet
            {
                public DeathlinkPacket()
                {
                    Type = PacketType.Deathlink;
                }
            }
        }

        public class PacketConverter : JsonConverter<Packet>
        {
            public override Packet? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
            {
                throw new NotImplementedException();
            }

            public override void Write(Utf8JsonWriter writer, Packet value, JsonSerializerOptions options)
            {
                throw new NotImplementedException();
            }
        }
    }
   
    
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
            Packet.MessagePacket messagePacket = new Packet.MessagePacket(message);
            
            SerialiseAndSend(messagePacket);
        }
        //Checks - Try Chat colour changes
        
        //Receive Deathlink
        public void SendDeathlinkToGame()
        {
            Packet.DeathlinkPacket deathlinkPacket = new Packet.DeathlinkPacket();
            
            SerialiseAndSend(deathlinkPacket);
        }
        
        //Eventually going to need to get the items and feed it through


        void SerialiseAndSend(Packet packet)
        {
            string packetJson = JsonSerializer.Serialize(packet);
            
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
                    Packet.MessagePacket messagePacket = (Packet.MessagePacket)message;
                    SendChatToServer(messagePacket.chatMessage);
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
                    SendDeathlinkToServer();
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
            ArchipelagoClient.instance.TryToConnect();
        }
        
        //Say
        void SendChatToServer(string ChatMessage)
        {
            //TODO: FEED CHAT MESSAGE TO SERVER
            ArchipelagoClient.instance.SayThis(ChatMessage);
        }
        
        //Checks
        void SendCheckToServer(string CheckName)
        {
            //TODO: SEND CHECK VALUES
            ArchipelagoClient.instance.SendCheck(CheckName);
        }
        
        //Send Deathlink
        void SendDeathlinkToServer()
        {
            ArchipelagoClient.instance.SendDeathlink();
        }
        
        //Goal
        void SendGoalToServer()
        {
            ArchipelagoClient.instance.SendGoal();
        }
        //
        //
        
        #endregion
        
    }
}