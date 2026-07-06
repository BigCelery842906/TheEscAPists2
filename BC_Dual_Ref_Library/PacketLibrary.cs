using System;
using System.Collections.Generic;

namespace BC_Dual_Ref_Library
{
    public enum PacketType
    {
        None,
        Connection,
        ChatMessage,
        Check,
        Deathlink,
        Goal,
    }

    public class Packet
    {
        public PacketType Type { get; set; }

        public Packet()
        {
            Type = PacketType.None;
        }

        public string ToJson()
        {
            return PacketConverter.WriteJson(this);
        }

        public static Packet Deserialise(string json)
        {
            //TODO: DESERIALISATION

            return new Packet();
        }

        public class ConnectionPacket : Packet
        {
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

        public class ChatMessagePacket : Packet
        {
            public string slotName = "";
            public string chatMessage = "";

            public ChatMessagePacket()
            {
                Type = PacketType.ChatMessage;
            }

            public ChatMessagePacket(string slotname, string message)
            {
                Type = PacketType.ChatMessage;
                this.chatMessage = message;
                this.slotName = slotname;
            }
        }

        public class CheckPacket : Packet
        {
            public string slotName = "";
            public string checkName = "";

            public CheckPacket()
            {
                Type = PacketType.Check;
            }

            public CheckPacket(string slotName, string checkName)
            {
                Type = PacketType.Check;
                this.slotName = slotName;
                this.checkName = checkName;
            }
        }

        public class DeathlinkPacket : Packet
        {
            public string slotName = "";
            public string reason = "";
            public DeathlinkPacket()
            {
                Type = PacketType.Deathlink;
            }
            public DeathlinkPacket(string slotName, string reason)
            {
                Type = PacketType.Deathlink;
                this.slotName = slotName;
                this.reason = reason;
            }
        }

        public class GoalPacket : Packet
        {
            public string slotName = "";
            public GoalPacket()
            {
                Type = PacketType.Goal;
            }
            public GoalPacket(string slotName)
            {
                Type = PacketType.Goal;
                this.slotName = slotName;
            }
        }
    }

    public static class PacketConverter 
    {
        public static string WriteJson(Packet packet)
        {
            string jsonString = "";
            PacketType packetType = packet.Type;
            string type = $"Type: {packetType.ToString()}";

            List<string> parts = new List<string>();
            
            parts.Add(type);
            switch (packetType)
            {
                case PacketType.Connection:
                {
                    Packet.ConnectionPacket connectionPacket = (Packet.ConnectionPacket)packet;
                    
                    // game, slot, serveraddress, password, version
                    string game = $"Game: {connectionPacket.game}";
                    string slot = $"Slot: {connectionPacket.slotName}";
                    string server = $"Server: {connectionPacket.serverAddress}";
                    string password = $"Password: {connectionPacket.password}";
                    string version = $"Version: {connectionPacket.version}";

                    parts.Add(game);
                    parts.Add(slot);
                    parts.Add(server);
                    parts.Add(password);
                    parts.Add(version);
                    
                    break;
                }
                case PacketType.ChatMessage:
                {
                    Packet.ChatMessagePacket chatMessagePacket = (Packet.ChatMessagePacket)packet;
                    
                    string slot = $"Slot: {chatMessagePacket.slotName}";
                    string message =  $"Message: {chatMessagePacket.chatMessage}";
                    
                    parts.Add(slot);
                    parts.Add(message);
                    
                    break;
                }
                case PacketType.Check:
                {
                    Packet.CheckPacket checkPacket = (Packet.CheckPacket)packet;
                    
                    string slot = $"Slot: {checkPacket.slotName}";
                    string check =  $"Check: {checkPacket.checkName}";
                    
                    parts.Add(slot);
                    parts.Add(check);
                    break;
                }
                case PacketType.Deathlink:
                {
                    Packet.DeathlinkPacket deathlinkPacket = (Packet.DeathlinkPacket)packet;
                    string slot = $"Slot: {deathlinkPacket.slotName}";
                    string reason = $"Reason: {deathlinkPacket.reason}";
                    
                    parts.Add(slot);
                    parts.Add(reason);
                    
                    break;
                }
                case PacketType.Goal:
                {
                    Packet.GoalPacket goalPacket = (Packet.GoalPacket)packet;
                    string slot = $"Slot: {goalPacket.slotName}";
                    
                    parts.Add(slot);
                    
                    break;
                }
                
            }
            
            jsonString = string.Join("|", parts.ToArray());
            return jsonString;
        }

        // public override Packet ReadJson(JsonReader reader, Type objectType, Packet existingValue, bool hasExistingValue,
        //     JsonSerializer serializer)
        // {
        //     JObject jsonObject = JObject.Load(reader);
        //
        //     JToken TypeToken = jsonObject["Type"];
        //
        //     if (TypeToken == null)
        //     {
        //         throw new JsonSerializationException("Packet does not contain a type property.");
        //     }
        //     
        //     switch (TypeToken.ToObject<PacketType>())
        //     {
        //         case PacketType.Connection:
        //         {
        //             Console.WriteLine("Deserializing connection packet");
        //             return jsonObject.ToObject<Packet.ConnectionPacket>(serializer);
        //             break;
        //         }
        //         case PacketType.ChatMessage:
        //         {
        //             Console.WriteLine("Deserializing chat message packet");
        //             return jsonObject.ToObject<Packet.ChatMessagePacket>(serializer);
        //             break;
        //         }
        //         case PacketType.Check:
        //         {
        //             Console.WriteLine("Deserializing check packet");
        //             return jsonObject.ToObject<Packet.CheckPacket>(serializer);
        //             break;
        //         }
        //         case PacketType.Deathlink:
        //         {
        //             Console.WriteLine("Deserializing deathlink packet");
        //             return jsonObject.ToObject<Packet.DeathlinkPacket>(serializer);
        //             break;
        //         }
        //         case PacketType.Goal:
        //         {
        //             Console.WriteLine("Deserializing goal packet");
        //             return jsonObject.ToObject<Packet.GoalPacket>(serializer);
        //             break;
        //         }
        //     }
        //
        //     throw new JsonException("Unknown Type");
        // }

    }
}