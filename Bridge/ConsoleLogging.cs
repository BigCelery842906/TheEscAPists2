namespace TheEscAPists2Bridge;

public static class ConsoleLogging
{
    public static void LogInfo(string info)
    {
        SetConsoleTextColor(ConsoleColor.White);
        Console.WriteLine(info);
    }

    public enum ErrorCode
    {
        TestError,
        NoServerConnection,
        NoGameConnection,
        ServerRefusedConnection,
        FailToDisconnect,
        
    }
    /// <summary>
    /// 01 - Test Error (Nothing actually wrong).
    /// 02 - The Bridge is not connected to a server.
    /// 03 - The Bridge is not connected to the game.
    /// 04 - Server Refused Connection for some reason, Find more details attached.
    /// 05 - Failed to disconnect from server
    /// </summary>
    /// <param name="errorCode"></param>
    /// 
    public static void LogError(ErrorCode errorCode, string error)
    {
        //Make Text Red - Maybe also flag with surrounding text
        SetConsoleTextColor(ConsoleColor.Red);
        // Console.WriteLine("!! ERROR !!");
        Console.WriteLine("Error Code: " + errorCode + " - " + error);
        // Console.WriteLine("!! END OF ERROR !!");
        SetConsoleTextColor(ConsoleColor.White);
    }

    public static void LogDebug(string debugInfo)
    {
        //Make Text Yellow
        SetConsoleTextColor(ConsoleColor.Cyan);
        Console.WriteLine(debugInfo);
        SetConsoleTextColor(ConsoleColor.White);
    }
    
    public static void LogWarning(string debugInfo)
    {
        //Make Text Yellow
        SetConsoleTextColor(ConsoleColor.Yellow);
        Console.WriteLine(debugInfo);
        SetConsoleTextColor(ConsoleColor.White);
    }

    public static void LogSuccess(string success)
    {
        SetConsoleTextColor(ConsoleColor.Green);
        Console.WriteLine(success);
        SetConsoleTextColor(ConsoleColor.White);
    }

    public static void LogSend(string thingToSend)
    {
        SetConsoleTextColor(ConsoleColor.DarkCyan);
        Console.WriteLine(thingToSend);
        SetConsoleTextColor(ConsoleColor.White);
    }
    
    private static void SetConsoleTextColor(ConsoleColor color)
    {
        Console.ForegroundColor = color;
    }

    public static void LogAllTypes()
    {
        LogError(ErrorCode.TestError, "Test Error");
        LogInfo("This is some information");
        LogDebug("This is some debug");
        LogWarning("This is some warning");
        LogSuccess("Something worked as intended and needs to be relayed to user");
        LogSend("This is some data that has been sent through the bridge");
    }

    public static void LogStatus()
    {
        ConsoleLogging.LogDebug("Running Log status now");
        bool apClientConnected = ArchipelagoClient.instance.GetConnectionStatusToServer();
        if (apClientConnected)
        {
            ConsoleLogging.LogSuccess("Archipelago Bridge Connected to Server.");
            ConnectionInfo connectionInfo = ArchipelagoClient.instance.GetConnectionInfo();
            ConsoleLogging.LogSuccess("Slot Name: " + connectionInfo.slotName);
            ConsoleLogging.LogSuccess("Server Address: " + connectionInfo.serverAddress);
            ConsoleLogging.LogSuccess("Server Password: " + connectionInfo.password);
        }
        else
        {
            ConsoleLogging.LogError(ErrorCode.NoServerConnection, "Archipelago Bridge Not Connected to Server.");
        }

        bool gameClientConnected = GameConnector.instance.GetConnectionStatusBridgeToGame();

        if (gameClientConnected)
        {
            ConsoleLogging.LogSuccess("Bridge to Game Pipe Connected.");
        }
        else
        {
            ConsoleLogging.LogError(ErrorCode.NoGameConnection, "Bridge to Game Pipe not connected.");
        }

        gameClientConnected = GameConnector.instance.GetConnectionStatusGameToBridge();

        if (gameClientConnected)
        {
            ConsoleLogging.LogSuccess("Game to Bridge Pipe Connected.");
        }
        else
        {
            ConsoleLogging.LogError(ErrorCode.NoGameConnection, "Game to Bridge Pipe not connected.");
        }
}
}