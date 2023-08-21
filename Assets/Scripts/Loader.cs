using NVOS.Core.Services;
using NVOS.Core;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using NVOS.Core.Logger.Enums;
using System.Linq;
using System.Reflection;
using NVOS.Core.Modules;
using NVOS.Core.Modules.Extensions;

public class Loader
{
    public const string NVOS_ROOT_PATH = "NVOS_Data";

    [RuntimeInitializeOnLoadMethod]
    static void Init()
    {
        // Disable pesky unity logging
        Application.SetStackTraceLogType(LogType.Log, StackTraceLogType.None);
        Application.SetStackTraceLogType(LogType.Warning, StackTraceLogType.None);

        Debug.Log("===== Loading NVOS =====");
        try
        {
            string data_path = Application.persistentDataPath;
            Debug.Log($"Persistent data path is {data_path}");
            Directory.SetCurrentDirectory(data_path);

            Debug.Log($"Preparing NVOS root directory at {Path.GetFullPath(NVOS_ROOT_PATH)}");
            Directory.CreateDirectory(NVOS_ROOT_PATH);

            Debug.Log("Setting working directory");
            Directory.SetCurrentDirectory(NVOS_ROOT_PATH);

            Debug.Log("NVOS root directory OK");
        }
        catch (Exception ex)
        {
            Debug.LogError($"Failed to prepare NVOS root directory: {ex}");
            return;
        }

        try
        {
            Debug.Log("Calling Bootstrap.Init");
            Bootstrap.Init();
            Debug.Log("Bootstrap OK");
        }
        catch (Exception ex)
        {
            Debug.LogError($"Failed to initialize NVOS: {ex}");
            return;
        }

        if (!Bootstrap.IsInitialized)
        {
            Debug.LogError("NVOS.Core did not set the IsInitialized flag. Perhaps it failed silently?");
            return;
        }

        NVOS.Core.Logger.ILogger logger;
        try
        {
            Debug.Log("Initializing NVOS logging");
            logger = ServiceLocator.Resolve<NVOS.Core.Logger.ILogger>();
            logger.OnLog += Logger_OnLog;
            Debug.Log("Logger OK");
        }
        catch (Exception ex)
        {
            Debug.LogError($"Failed to initialize logger: {ex}");
            return;
        }

        if (Debug.isDebugBuild)
        {
            logger.SetLevel(LogLevel.DEBUG);
            logger.Info("Running a development build, log level set to DEBUG");
        }

        logger.Info("Core bootstrap finished");
        logger.Info("Loading NVOS modules");

        ModuleManager mm;
        try
        {
            mm = ServiceLocator.Resolve<ModuleManager>();
            logger.Info("Module manager OK");
        }
        catch (Exception ex)
        {
            logger.Error($"Failed to get module manager: {ex}");
            return;
        }

        foreach(Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
        {
            if (!assembly.HasModuleManifest())
            {
                logger.Debug($"Assembly {assembly.GetName().Name} is not a module, skipping.");
                continue;
            }

            try
            {
                IModule manifest = assembly.GetModuleManifest();

                // Print module summary
                logger.Info(new string('-', 30));
                logger.Info($"{manifest.Name} {manifest.Version}");
                logger.Info($"by {manifest.Author}");
                logger.Info($"Description: {(manifest.Description != null ? manifest.Description : "None")}");
                logger.Info($"Assembly: {assembly.GetName().Name}");
                logger.Info(new string('-', 30));

                mm.Load(assembly);
                logger.Info("Module load OK");
                logger.Info("");
            }
            catch(Exception ex)
            {
                logger.Info($"Failed to load module {assembly.GetName().Name}: {ex}");
            }
        }

        Debug.Log("===== NVOS loader finished =====");
    }

    private static void Logger_OnLog(object sender, NVOS.Core.Logger.EventArgs.LogEventArgs e)
    {
        switch (e.Level)
        {
            case LogLevel.DEBUG:
                Debug.Log($"[DEBUG] {e.Message}");
                break;
            case LogLevel.INFO:
                Debug.Log($"[INFO] {e.Message}");
                break;
            case LogLevel.WARN:
                Debug.LogWarning(e.Message);
                break;
            case LogLevel.ERROR:
                Debug.LogError(e.Message);
                break;
        }
    }
}
