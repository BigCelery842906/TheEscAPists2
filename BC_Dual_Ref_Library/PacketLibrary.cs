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
        StealthLink,
        Goal,
        IsConnected,
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
            return PacketConverter.ReadJson(json);
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

        public class IsConnectedPacket : Packet
        {
            public bool isConnected = false;
            
            public  IsConnectedPacket(bool isConnected)
            {
                Type = PacketType.IsConnected;
                this.isConnected = isConnected;
            }
        }

        public class StealthLinkPacket : Packet
        {
            public string slotName = "";

            public StealthLinkPacket()
            {
                Type = PacketType.StealthLink;
            }

            public StealthLinkPacket(string slotName)
            {
                Type = PacketType.StealthLink;
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

                case PacketType.StealthLink:
                {
                    Packet.StealthLinkPacket stealthLinkPacket = (Packet.StealthLinkPacket)packet;
                    string slot = $"Slot: {stealthLinkPacket.slotName}";
                    
                    parts.Add(slot);

                    break;
                }
                case PacketType.Goal:
                {
                    Packet.GoalPacket goalPacket = (Packet.GoalPacket)packet;
                    string slot = $"Slot: {goalPacket.slotName}";
                    
                    parts.Add(slot);
                    
                    break;
                }
                case PacketType.IsConnected:
                {
                    Packet.IsConnectedPacket isConnectedPacket = (Packet.IsConnectedPacket)packet;
                    string isConnected = $"IsConnected: {isConnectedPacket.isConnected}";
                    
                    parts.Add(isConnected);

                    break;
                }
            }
            
            jsonString = string.Join("|", parts.ToArray());
            return jsonString;
        }

        //Return the packet here, convert within each instance via the converter
        //MessageHandler.instance.ProcessGameToServer is where it gets processed bridge side
        //Listen in Plugin.cs - Needs to be set up, currently wont do nish
        public static Packet ReadJson(string jsonString)
        {
            string[] jsonParts = jsonString.Split('|');
            
            for (int i = 0; i < jsonParts.Length; i++)
            {
                Console.WriteLine(jsonParts[i]);
            }
            
            string type = jsonParts[0];
            type = type.Remove(0, 6);
            Console.WriteLine(type);

            PacketType packetType = (PacketType)Enum.Parse(typeof(PacketType), type);

            switch (packetType)
            {
                case PacketType.Connection:
                {
                    string slotName = jsonParts[2].Remove(0, 6);
                    string serverAddress = jsonParts[3].Remove(0, 8);
                    string password = jsonParts[4].Remove(0, 10);
                    
                    Packet.ConnectionPacket packetToReturn = new Packet.ConnectionPacket(slotName, serverAddress, password);
                    
                    return packetToReturn;
                }

                case PacketType.ChatMessage:
                {
                    string slotName = jsonParts[1].Remove(0, 6);
                    string message = jsonParts[2].Remove(0, 9);
                    
                    Packet.ChatMessagePacket packetToReturn = new Packet.ChatMessagePacket(slotName, message);
                    
                    return packetToReturn;
                }

                case PacketType.Check:
                {
                    string slotName = jsonParts[1].Remove(0, 6);
                    string checkName = jsonParts[2].Remove(0, 7);
                    
                    Packet.CheckPacket packetToReturn = new Packet.CheckPacket(slotName, checkName);
                    
                    return packetToReturn;
                }

                case PacketType.Deathlink:
                {
                    string slotName = jsonParts[1].Remove(0, 6);
                    string reason = jsonParts[2].Remove(0, 8);
                    
                    Packet.DeathlinkPacket packetToReturn = new Packet.DeathlinkPacket(slotName, reason);
                    
                    return packetToReturn;
                }

                case PacketType.StealthLink:
                {
                    string slotName = jsonParts[1].Remove(0, 6);
                    
                    Packet.StealthLinkPacket packetToReturn = new Packet.StealthLinkPacket(slotName);

                    return packetToReturn;
                }

                case PacketType.Goal:
                {
                    string slotName = jsonParts[1].Remove(0, 6);
                    
                    Packet.GoalPacket packetToReturn = new Packet.GoalPacket(slotName);
                    
                    return packetToReturn;
                }

                case PacketType.IsConnected:
                {
                    bool isConnected = bool.Parse(jsonParts[1].Remove(0, 13));
                    
                    Packet.IsConnectedPacket isConnectedPacket = new Packet.IsConnectedPacket(isConnected);


                    return isConnectedPacket;
                }
                
                default:
                    return new Packet();
            }
            
        }

    }
}