using BC_Dual_Ref_Library;

namespace TheEscAPists2Bridge;

public static class ConsoleLogging
{
    public static bool debugMode = false;
    private static List<string> debugStatements = new List<string>();
    public static void LogInfo(string info)
    {
        SetConsoleTextColor(ConsoleColor.White);
        Console.WriteLine(info);
    }

    public static void LogInfoInput(string info)
    {
        SetConsoleTextColor(ConsoleColor.White);
        Console.Write(info);
    }

    public enum ErrorCode
    {
        TestError,
        NoServerConnection,
        NoGameConnection,
        ServerRefusedConnection,
        FailToDisconnect,
        CheckSendFailure,
        LoadFailure,
        ParseError,
    }
    /// <summary>
    /// 01 - Test Error (Nothing actually wrong).
    /// 02 - The Bridge is not connected to a server.
    /// 03 - The Bridge is not connected to the game.
    /// 04 - Server Refused Connection for some reason, Find more details attached.
    /// 05 - Failed to disconnect from server
    /// 06 - Check failed to send
    /// 07 - Failed to Load Save
    /// 08 - Parsing Error
    /// </summary>
    /// <param name="errorCode"></param>
    /// 
    public static void LogError(ErrorCode errorCode, string error)
    {
        debugStatements.Add(error);
        //Make Text Red - Maybe also flag with surrounding text
        SetConsoleTextColor(ConsoleColor.Red);
        // Console.WriteLine("!! ERROR !!");
        Console.WriteLine("Error Code: " + errorCode + " - " + error);
        // Console.WriteLine("!! END OF ERROR !!");
        SetConsoleTextColor(ConsoleColor.White);
    }

    public static void LogDebug(string debugInfo, bool loggingDebugs = false)
    {
        if (!loggingDebugs)
        {
            debugStatements.Add(debugInfo);
        }

        if (!debugMode) { return; }
        
        SetConsoleTextColor(ConsoleColor.Cyan);
        Console.WriteLine(debugInfo);
        SetConsoleTextColor(ConsoleColor.White);
    }
    
    public static void LogWarning(string debugInfo)
    {
        debugStatements.Add(debugInfo);
        SetConsoleTextColor(ConsoleColor.Yellow);
        Console.WriteLine(debugInfo);
        SetConsoleTextColor(ConsoleColor.White);
    }

    public static void LogSuccess(string success)
    {
        debugStatements.Add(success);
        SetConsoleTextColor(ConsoleColor.Green);
        Console.WriteLine(success);
        SetConsoleTextColor(ConsoleColor.White);
    }

    public static void LogSend(string thingToSend)
    {
        debugStatements.Add(thingToSend);
        SetConsoleTextColor(ConsoleColor.DarkCyan);
        Console.WriteLine(thingToSend);
        SetConsoleTextColor(ConsoleColor.White);
    }
    
    public static void LogDebugSend(string thingToSend)
    {
        debugStatements.Add(thingToSend);
        if (!debugMode)
        { return; }
        SetConsoleTextColor(ConsoleColor.DarkCyan);
        Console.WriteLine(thingToSend);
        SetConsoleTextColor(ConsoleColor.White);
    }
    
    private static void SetConsoleTextColor(ConsoleColor color)
    {
        Console.ForegroundColor = color;
    }

    public static void LogStatus()
    {
        LogDebug("Running Log status now");
        bool apClientConnected = ArchipelagoClient.instance.GetConnectionStatusToServer();
        if (apClientConnected)
        {
            LogSuccess("Archipelago Bridge Connected to Server.");
            ConnectionInfo connectionInfo = ArchipelagoClient.instance.GetConnectionInfo();
            LogSuccess("Slot Name: " + connectionInfo.slotName);
            LogSuccess("Server Address: " + connectionInfo.serverAddress);
            LogSuccess("Server Password: " + connectionInfo.password);
        }
        else
        {
            LogError(ErrorCode.NoServerConnection, "Archipelago Bridge Not Connected to Server.");
        }

        bool gameClientConnected = GameConnector.instance.GetConnectionStatusBridgeToGame();

        if (gameClientConnected)
        {
            LogSuccess("Bridge to Game Pipe Connected.");
        }
        else
        {
            LogError(ErrorCode.NoGameConnection, "Bridge to Game Pipe not connected.");
        }

        gameClientConnected = GameConnector.instance.GetConnectionStatusGameToBridge();

        if (gameClientConnected)
        {
            LogSuccess("Game to Bridge Pipe Connected.");
        }
        else
        {
            LogError(ErrorCode.NoGameConnection, "Game to Bridge Pipe not connected.");
        }
}

    public static void LogDebugs()
    {
        List<string> lineToSave = new List<string>();
        ConsoleLogging.LogDebug("SAVING LOG FILE");
        
        string pathToSave = Directory.GetCurrentDirectory();
        pathToSave = pathToSave.Remove(pathToSave.Length - 6);
        pathToSave += "bridgeLog.json";
        ConsoleLogging.LogDebug($"Saving to {pathToSave}");
        
        SetConsoleTextColor(ConsoleColor.Cyan);
        lineToSave.Add($"Mod Version: {ModInfo.PLUGIN_VERSION}");
        Console.WriteLine($"Mod Version: {ModInfo.PLUGIN_VERSION}");
        
        foreach (string debugStatement in debugStatements)
        {
            lineToSave.Add(debugStatement);
            Console.WriteLine(debugStatement);
        }
        SetConsoleTextColor(ConsoleColor.White);
        File.WriteAllLines(pathToSave, lineToSave);
    }
}