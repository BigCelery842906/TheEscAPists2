using System;
using System.Diagnostics;
using System.IO;
using System.IO.Pipes;
using System.Threading;
using BC_Dual_Ref_Library;
using BepInEx;
using BepInEx.Logging;
using UnityEngine;

namespace TheEscAPists2;

[BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
[BepInProcess("TheEscapists2.exe")] //Set Specifically for the Escapists 2
public class Plugin : BaseUnityPlugin
{
  
    private Logging logging;

    private Process bridgeProcess;
    private NamedPipeClientStream pipeClientForBridge;
    private NamedPipeClientStream pipeClientForGame;
    private StreamReader reader;
    private StreamWriter writer;

    private static string slotName;
    public static bool isConnected; 
    
    private void Awake()
    {
        logging = new Logging(base.Logger);
        StartBridge();
        StartPipes();
    }

    private void StartBridge()
    {
        if ((bridgeProcess != null && !bridgeProcess.HasExited) || Process.GetProcessesByName("TheEscAPists2Bridge").Length > 0)
        {
            Logger.LogInfo("Bridge already running.");
            return;
        }
        
        
        string pluginDir = Path.GetDirectoryName(Info.Location); 
        string newPluginDir = Directory.GetParent(pluginDir).ToString();
        
        string bridgePath = Path.Combine(newPluginDir, Path.Combine("Bridge", "TheEscAPists2Bridge.exe"));
        Logger.LogError(bridgePath);

        if (!File.Exists(bridgePath))
        {
            Logger.LogError("Bridge exe not found: " + bridgePath);
            return;
        }
        
        bridgeProcess = Process.Start(new ProcessStartInfo{FileName = bridgePath, WorkingDirectory = pluginDir, UseShellExecute = true});

        if (bridgeProcess != null)
        {
            Logger.LogInfo("Bridge running.");
        }
        else
        {
            Logger.LogWarning("Bridge not running.");
        }
        
    }

    void StartPipes()
    {
        StartPipeForBridge();
        StartPipeForGame();
    }

    private void StartPipeForGame()
    {
        if (pipeClientForGame != null && pipeClientForGame.IsConnected)
        {
            return;
        }
        
        pipeClientForGame = new NamedPipeClientStream(".", "EscAPists2PipeToGame", PipeDirection.In);
        
            try
            {
                pipeClientForGame.Connect(500);
            }
            catch (Exception ex)
            {
                Logger.LogError("(ForGame) Failed to connect to bridge.");
                pipeClientForGame = null;
                return;
            }
        
        reader = new StreamReader(pipeClientForGame);
        
        Logger.LogInfo("(ForGame) Connected to bridge.");
        
        Thread thread = new Thread(Listen);
        thread.IsBackground = true;
        thread.Start();
    }

    void StartPipeForBridge()
    {

        if (pipeClientForBridge != null && pipeClientForBridge.IsConnected)
        {
            return;
        }
        
        pipeClientForBridge = new NamedPipeClientStream(".", "EscAPists2PipeToBridge", PipeDirection.Out);
        
            try
            {
                pipeClientForBridge.Connect(500);

                if (pipeClientForBridge.IsConnected)
                {
                    Logger.LogInfo("(ForBridge) Connected to bridge.");
                }
            }
            catch (Exception ex)
            {
                Logger.LogError("(ForBridge) Failed to connect to bridge.");
            }
            
        writer = new StreamWriter(pipeClientForBridge)
        {
            AutoFlush = true
        };
        
        Logger.LogInfo("(ForBridge) Connected to bridge.");

       
    }

    void Listen()
    {
        try
        {
            while (true)
            {
                Logger.LogDebug("Waiting for line....");
                string message = reader.ReadLine();

                if (message == null)
                {
                    Logger.LogWarning("Bridge disconnected. Holding Pipe for reconnect.");
                    return;
                }

                ProcessIncomingPacket(message);
                Logger.LogInfo("Received from bridge -> " + message);
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex.ToString());
        }
        finally
        {
            logging.SendChatMessage("Bridge disconnected. Holding Pipe for reconnect.");
        }

    }

    private void ProcessIncomingPacket(string serialisedMessage)
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
                Packet.ConnectionPacket connectionPacket = (Packet.ConnectionPacket)message;

                slotName = connectionPacket.slotName;
                
                break;
            }
            case PacketType.ChatMessage:
            {
                Packet.ChatMessagePacket chatMessagePacket = (Packet.ChatMessagePacket)message;
                logging.SendChatMessage(chatMessagePacket.chatMessage);
                break;
            }
            case PacketType.Check:
            {
                Packet.CheckPacket checkPacket = (Packet.CheckPacket)message;
                //TODO: Implement goof's code
                break;
            }
            case PacketType.Deathlink:
            {
                Packet.DeathlinkPacket deathlinkPacket = (Packet.DeathlinkPacket)message;
                lastDeathLinkSlot = deathlinkPacket.slotName;
                lastDeathLinkReason = deathlinkPacket.reason;
                pendingDeathLink = true;
                break;
            }
            case PacketType.Goal:
            {
                //TODO: implement goof's code
                break;
            }
            case PacketType.IsConnected:
            {
                Packet.IsConnectedPacket isConnectedPacket = (Packet.IsConnectedPacket)message;
                isConnected = isConnectedPacket.isConnected;
                break;
            }
            default:
            {
                Logger.LogWarning("Unknown message type: " + message.Type);
                break;
            }
        }
    }


    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.F6))
        {
           StartPipes();
        }

        if (Input.GetKeyDown(KeyCode.F2))
        {
            if (logging != null)
            {
               logging.SendChatMessage("Message test");
               SendMessageToBridge("Test message");
            }
        }

        if (Input.GetKeyDown(KeyCode.F3))
        {
            SendAllPacketsToBridge();
            // Logging.BridgeConnect.SetGoalAchieved();
        }
    }

   

    private void OnDestroy()
    {
        KillEverything();
    }

    ~Plugin()
    {
        KillEverything();
    }

    void KillEverything()
    {
        bridgeProcess?.Kill();
        pipeClientForBridge?.Dispose(); 
        pipeClientForBridge = null;
        
        pipeClientForGame?.Dispose();
        pipeClientForGame = null;
            
        writer?.Dispose();
        writer = null;
            
        reader?.Dispose();
        reader = null;
    }

    void SendMessageToBridge(string message)
    {
        //TODO - REPLACE WITH ACTUAL SLOT NAME
        Packet.ChatMessagePacket chatMessagePacket = new Packet.ChatMessagePacket(slotName, message);
            
        // SerialiseAndSend(chatMessagePacket);
        
        // Logger.LogInfo(chatMessagePacket.ToString());
        SerialiseAndSend(chatMessagePacket);
        // writer?.WriteLine(chatMessagePacket.ToString());
    }

    void SendAllPacketsToBridge()
    {
        SerialiseAndSend(new Packet.ConnectionPacket(slotName, "archipelago.gg:56644", ""));
        SerialiseAndSend(new Packet.ChatMessagePacket(slotName, "This is a test message packet"));
        SerialiseAndSend(new Packet.CheckPacket(slotName, "Check Name"));
        SerialiseAndSend(new Packet.DeathlinkPacket(slotName, "Got beaten by dogs"));
        SerialiseAndSend(new Packet.GoalPacket(slotName));
    }
    
    public void SerialiseAndSend(Packet packet)
    {
        string packetJson = packet.ToJson();
        SendToBridge(packetJson);
    }
    
    private void SendToBridge(string message)
    {
        Logger.LogInfo($"Sending to bridge -> {message}");
        writer?.WriteLine(message);
    }
}
