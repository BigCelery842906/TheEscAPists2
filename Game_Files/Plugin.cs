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
        pipeClientForGame = new NamedPipeClientStream(".", "EscAPists2PipeToGame", PipeDirection.In);
        
            try
            {
                pipeClientForGame.Connect(500);
            }
            catch (Exception ex)
            {
                Logger.LogError("(ForGame) Failed to connect to bridge.");
                return;
            }
        
        reader = new StreamReader(pipeClientForGame);
        
        Logger.LogInfo("(ForGame) Connected to bridge.");
    }

    void StartPipeForBridge()
    {
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

        Thread thread = new Thread(Listen);
        thread.IsBackground = true;
        thread.Start();
    }

    void Listen()
    { 
        try
        {
            Packet packet = null;

            while ((packet = ReadPacket()) != null)
            {

                //Process deserialised packet here
                Logger.LogDebug("Packet Received");
                // ReadMessage(packet);
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex.ToString());
        }
        finally
        {
            logging.LogConsole("Bridge disconnected. Holding Pipe for reconnect.");
        }
        
    }
    
    private Packet ReadPacket()
    {
        string message = null;
        
        message = reader.ReadLine();

        if (message != null)
        {
            ReadMessage(message);
            return Packet.Deserialise(message);
        }
        return null;
    }

    private void ReadMessage(string message)
    {
        logging.SendChatMessage(message);
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
               logging.LogConsole("Message test");
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
        Packet.ChatMessagePacket chatMessagePacket = new Packet.ChatMessagePacket("BC_Escapists", message);
            
        // SerialiseAndSend(chatMessagePacket);
        
        // Logger.LogInfo(chatMessagePacket.ToString());
        SerialiseAndSend(chatMessagePacket);
        // writer?.WriteLine(chatMessagePacket.ToString());
    }

    void SendAllPacketsToBridge()
    {
        SerialiseAndSend(new Packet.ConnectionPacket("BC_Escapists", "archipelago.gg:56644", ""));
        SerialiseAndSend(new Packet.ChatMessagePacket("BC_Escapists", "This is a test message packet"));
        SerialiseAndSend(new Packet.CheckPacket("BC_Escapists", "Check Name"));
        SerialiseAndSend(new Packet.DeathlinkPacket("BC_Escapists", "Got beaten by dogs"));
        SerialiseAndSend(new Packet.GoalPacket("BC_Escapists"));
    }
    
    void SerialiseAndSend(Packet packet)
    {
        string packetJson = packet.ToJson();
        SendToBridge(packetJson);
    }
    
    public void SendToBridge(string message)
    {
        Logger.LogInfo($"Sending to bridge -> {message}");
        writer?.WriteLine(message);
    }
}
