using System;
using System.Diagnostics;
using System.IO;
using System.IO.Pipes;
using System.Threading;
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
    private NamedPipeClientStream pipeClient;
    private StreamReader reader;
    private StreamWriter writer;
    private void Awake()
    {
        logging = new Logging(base.Logger);
        StartBridge();
        ConnectToBridge();
    }

    private void StartBridge()
    {
        if ((bridgeProcess != null && !bridgeProcess.HasExited) || Process.GetProcessesByName("TheEscAPists2Bridge").Length > 0)
        {
            Logger.LogInfo("Bridge already running.");
            return;
        }
        
        
        string pluginDir = Path.GetDirectoryName(Info.Location);
        string bridgePath = Path.Combine(pluginDir, "TheEscAPists2Bridge.exe");
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

    void ConnectToBridge()
    {
        pipeClient = new NamedPipeClientStream(".", "EscAPists2Pipe", PipeDirection.InOut);

        const int maxAttempts = 40;

        for (int i = 0; i < maxAttempts; i++)
        {
            try
            {
                pipeClient.Connect(500);

                if (pipeClient.IsConnected)
                {
                    Logger.LogInfo("Connected to bridge.");
                    break;
                }
            }
            catch (Exception ex)
            {
                Logger.LogWarning(
                    $"Bridge not ready yet ({i + 1}/{maxAttempts})"
                );
            }

            Thread.Sleep(250);
        }

        if (!pipeClient.IsConnected)
        {
            Logger.LogError("Failed to connect to bridge.");
            return;
        }
        
        reader = new StreamReader(pipeClient);
        writer = new StreamWriter(pipeClient)
        {
            AutoFlush = true
        };
        
        Logger.LogInfo("Connected to bridge.");

        Thread thread = new Thread(Listen);
    }

    void Listen()
    {
        while (true)
        {
                
            string message = reader.ReadLine();

            if (message == null)
            {
                logging.LogConsole("Game disconnected. Holding Pipe for reconnect.");
                break;
            }

            ReadMessage(message);
            // logging.LogInfo("Sending to Game -> " + message);
                
            writer.WriteLine(message);
                
        }
    }

    private void ReadMessage(string message)
    {
        logging.SendChatMessage(message);
    }


    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.F6))
        {
            ConnectToBridge();
        }

        if (Input.GetKeyDown(KeyCode.F2))
        {
            if (logging != null)
            {
               logging.LogConsole("Message test");
               writer?.WriteLine("Testing the Chat box. This could be a check");
            }
        }

        if (Input.GetKeyDown(KeyCode.F3))
        {
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
        pipeClient?.Dispose(); 
        pipeClient = null;
            
        writer?.Dispose();
        writer = null;
            
        reader?.Dispose();
        reader = null;
    }
}
