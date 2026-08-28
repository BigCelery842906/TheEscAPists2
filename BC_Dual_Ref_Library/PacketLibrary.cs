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
        DeathLink,
        StealthLink,
        Goal,
        IsConnected,
        Hint,
        Item,
        SlotData,
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
            public bool isID = false;

            public CheckPacket()
            {
                Type = PacketType.Check;
            }

            public CheckPacket(string slotName, string checkName, bool isID = false)
            {
                Type = PacketType.Check;
                this.slotName = slotName;
                this.checkName = checkName;
                this.isID = isID;
            }
        }

        public class DeathLinkPacket : Packet
        {
            public string slotName = "";
            public string reason = "";
            public DeathLinkPacket()
            {
                Type = PacketType.DeathLink;
            }
            public DeathLinkPacket(string slotName, string reason)
            {
                Type = PacketType.DeathLink;
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

        public class HintPacket : Packet
        {
            public enum TypeOfHint { Normal, Scout}

            public TypeOfHint hintType = TypeOfHint.Normal;
            // public string slotName = "";
            
            public string locationName = "";
            public string itemName = "";
            public HintPacket()
            {
                hintType = TypeOfHint.Normal;
                Type = PacketType.Hint;
            }

            public HintPacket(string locationName, TypeOfHint hintType = TypeOfHint.Normal, string itemName = "")
            {
                Type = PacketType.Hint;
                this.hintType = hintType;
                this.locationName = locationName;
                this.itemName = itemName;
            }
        }

        public class ItemPacket : Packet
        {
            public string itemName;
            public bool isItemID = false;

            public ItemPacket(string itemName, bool isItemID = false)
            {
                Type = PacketType.Item;
                this.itemName = itemName;
                this.isItemID = isItemID;
            }
        }

        public class SlotDataPacket : Packet
        {
            public int strength_step;
            public int stamina_step;
            public int intellect_step;

            public SlotDataPacket(int strengthStep, int staminaStep, int intellectStep)
            {
                Type = PacketType.SlotData;
                strength_step = strengthStep;
                stamina_step = staminaStep;
                intellect_step = intellectStep;
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
                    string isID = $"isID: {checkPacket.isID}";
                    
                    parts.Add(slot);
                    parts.Add(check);
                    parts.Add(isID);
                    
                    break;
                }
                case PacketType.DeathLink:
                {
                    Packet.DeathLinkPacket deathLinkPacket = (Packet.DeathLinkPacket)packet;
                    string slot = $"Slot: {deathLinkPacket.slotName}";
                    string reason = $"Reason: {deathLinkPacket.reason}";
                    
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

                case PacketType.Hint:
                {
                    Packet.HintPacket hintPacket = (Packet.HintPacket)packet;
                    string hintType = $"HintType: {hintPacket.hintType}";
                    string hintName = $"HintName: {hintPacket.locationName}";
                    
                    parts.Add(hintType);
                    parts.Add(hintName);

                    break;
                }
                case PacketType.Item:
                {
                    Packet.ItemPacket itemPacket = (Packet.ItemPacket)packet;
                    string itemName = $"ItemName: {itemPacket.itemName}";
                    string isItemID = $"IsItemID: {itemPacket.isItemID}";
                    
                    parts.Add(itemName);
                    parts.Add(isItemID);

                    break;
                }

                case PacketType.SlotData:
                {
                    Packet.SlotDataPacket slotDataPacket = (Packet.SlotDataPacket)packet;

                    string strengthStep = $"StrengthStep: {slotDataPacket.strength_step}";
                    string staminaStep =  $"StaminaStep: {slotDataPacket.stamina_step}";
                    string intellectStep = $"IntellectStep: {slotDataPacket.intellect_step}";
                    
                    parts.Add(strengthStep);
                    parts.Add(staminaStep);
                    parts.Add(intellectStep);
                    
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
            
            string type = jsonParts[0];
            type = type.Remove(0, 6);

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
                    bool isID = bool.Parse(jsonParts[3].Remove(0, 6));
                    
                    Packet.CheckPacket packetToReturn = new Packet.CheckPacket(slotName, checkName, isID);
                    
                    return packetToReturn;
                }

                case PacketType.DeathLink:
                {
                    string slotName = jsonParts[1].Remove(0, 6);
                    string reason = jsonParts[2].Remove(0, 8);
                    
                    Packet.DeathLinkPacket packetToReturn = new Packet.DeathLinkPacket(slotName, reason);
                    
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

                case PacketType.Hint:
                {
                    string hintType = jsonParts[1].Remove(0, 10);
                    Packet.HintPacket.TypeOfHint hintTyp = (Packet.HintPacket.TypeOfHint)int.Parse(hintType);
                    string hintName = jsonParts[2].Remove(0, 10);
                    
                    Packet.HintPacket hintPacket = new Packet.HintPacket(hintName, hintTyp);

                    return hintPacket;
                }

                case PacketType.Item:
                {
                    string itemName = jsonParts[1].Remove(0, 10);
                    bool isItemID = Convert.ToBoolean(jsonParts[2].Remove(0, 10));
                    
                    Packet.ItemPacket itemPacket = new Packet.ItemPacket(itemName, isItemID);
                    
                    return itemPacket;
                }

                case PacketType.SlotData:
                {
                    int strengthStep = int.Parse(jsonParts[1].Remove(0, 14));
                    int staminaStep = int.Parse(jsonParts[2].Remove(0, 13));
                    int intellectStep = int.Parse(jsonParts[3].Remove(0, 15));
                    
                    Packet.SlotDataPacket slotDataPacket = new Packet.SlotDataPacket(strengthStep, staminaStep, intellectStep);

                    return slotDataPacket;
                }
                default:
                    return new Packet();
            }
            
        }

    }
}