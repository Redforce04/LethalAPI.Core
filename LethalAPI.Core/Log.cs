﻿// -----------------------------------------------------------------------
// <copyright file="Log.cs" company="LethalAPI Modding Community">
// Copyright (c) LethalAPI Modding Community. All rights reserved.
// Licensed under the GPL-3.0 license.
// </copyright>
// -----------------------------------------------------------------------

// ReSharper disable MemberCanBePrivate.Global
namespace LethalAPI.Core;

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Reflection;

using BepInEx.Logging;

#pragma warning disable SA1201 // ordering by correct order.
#pragma warning disable SA1202 // ordering by access.

/// <summary>
/// The logging class for LethalAPI.
/// </summary>
public static class Log
{
    static Log()
    {
        AssemblyNameReplacements = new ConcurrentDictionary<string, string>();
        AssemblyNameReplacements.TryAdd("UnityEngine.CoreModule", "Unity");
    }

    /// <summary>
    /// Gets or sets a value indicating whether logs will show the type and method name of the method calling the logger.
    /// </summary>
    public static bool ShowCallingMethod { get; set; } = false;

    /// <summary>
    /// Gets or sets a value indicating whether logs will show the arguments of the method if <see cref="ShowCallingMethod"/> is enabled.
    /// <seealso cref="ShowCallingMethod"/>
    /// </summary>
    public static bool ShowCallingMethodArgs { get; set; } = false;

    /// <summary>
    /// Gets a dictionary that can be used to specify assembly name replacements.
    /// <code>
    /// Example Entry:
    ///     { "UnityEngine.CoreModule", "Unity" }
    /// Original Log:
    ///     [Time] [LogLevel] [UnityEngine.CoreModule] Message
    /// Updated Log:
    ///     [Time] [LogLevel] [Unity] Message
    /// </code>
    /// </summary>
    /// <param name="originalAssemblyName">The name of the assembly to be replaced.</param>
    /// <param name="newAssemblyName">The new name of the assembly to log.</param>
    public static void AddAssemblyNameReplacement(string originalAssemblyName, string newAssemblyName)
        => AssemblyNameReplacements.TryAdd(originalAssemblyName, newAssemblyName);

    private static ConcurrentDictionary<string, string> AssemblyNameReplacements { get; }

    /// <summary>
    /// A dictionary containing different logging templates.
    /// </summary>
    /// <example>
    /// <code>
    /// {time}   - the time of the log.
    /// {type}   - the type of the log.
    /// {prefix} - the plugins prefix if applicable. Alternatively the calling method if debug info is enabled.
    /// {msg}    - the log message.
    /// {il}     - the il label of an error.
    /// {line}   - the line of an error.
    /// </code>
    /// <code>
    /// Info - the template for info logs.
    /// Debug - the template for debug logs.
    /// Warn - the template for warning logs.
    /// Error - the template for error logs.
    /// LineLocNotFound - Stack trace line name if the line is not found.
    /// LineLocFound - Stack trace line name if the line is found.
    /// </code>
    /// </example>
    public static readonly Dictionary<string, string> Templates = new()
    {
        { "Info", "{time} &7[&b&6{type}&B&7] &7[&b&2{prefix}&B&7]&r {msg}" },
        { "Debug", "{time} &7[&b&5{type}&B&7] &7[&b&2{prefix}&B&7]&r {msg}" },
        { "Warn", "{time} &7[&b&3{type}&B&7] &7[&b&2{prefix}&B&7]&r {msg}" },
        { "Error", "{time} &7[&b&1{type}&B&7] &7[&b&2{prefix}&B&7]&r {msg}" },
        { "LineLocNotFound", "&1Line Unknown&7 &h[&6IL_{il}&h]&7" },
        { "LineLocFound", "&3Line {line}&7 &h[&6IL_{il}&h]&7" },
    };

    /// <inheritdoc cref ="Core.Patches.Fixes.FixBepInExLoggerPrefix.ConsoleText" />
    public static ReadOnlyDictionary<char, ConsoleColor> ColorCodes => Patches.Fixes.FixBepInExLoggerPrefix.ConsoleText;

    private static string GetDateString()
    {
        DateTime now = DateTime.Now;
        return $"[{$"{now:g}",-19} ({$"{now:ss}",-2}.{$"{now.Millisecond:000}",-3}s)]";
    }

    private static string GetCallingPlugin(MethodBase method, string input, bool includeMethod)
    {
        try
        {
            if (!string.IsNullOrEmpty(input))
            {
                return input;
            }

            if (method.DeclaringType?.Assembly is null)
            {
                return "Unknown";
            }

            Type type = method.DeclaringType!;
            Assembly assembly = method.DeclaringType.Assembly;

            BepInEx.PluginInfo? plugin = null;
            if (BepInEx.Bootstrap.Chainloader.PluginInfos is not null)
            {
                // FirstOrDefault keeps throwing a NullReferenceException. This doesnt throw an exception so we will use it.
                foreach (KeyValuePair<string, BepInEx.PluginInfo> x in BepInEx.Bootstrap.Chainloader.PluginInfos)
                {
                    if (x.Value?.Instance is null)
                    {
                        continue;
                    }

                    if (x.Value.Instance.GetType().Assembly != assembly)
                    {
                        continue;
                    }

                    plugin = x.Value;
                    break;
                }
            }

            if (plugin is null)
            {
                input = assembly.GetName().Name;
                if (input is "" or null)
                    return "Unknown";

                if (AssemblyNameReplacements.ContainsKey(input))
                    AssemblyNameReplacements.TryGetValue(input, out input);
            }
            else
            {
                input = plugin.Metadata.Name;
            }

            if (!includeMethod)
                return input!;

            string args = string.Empty;

            if (ShowCallingMethodArgs)
            {
                foreach (ParameterInfo x in method.GetParameters())
                {
                    args += $"&g{x.ParameterType.Name} &h{x.Name}, ";
                }

                if (args != string.Empty)
                {
                    args = args.Substring(0, args.Length - 2) + "&7";
                }
            }

            input += $"&h::{type.FullName}.&6{method.Name}&7({args})";
            return input;
        }
        catch (Exception e)
        {
            return $"Unknown {e}";
        }
    }

    /// <summary>
    /// Logs information to the console.
    /// </summary>
    /// <param name="message">The message to log.</param>
    /// <param name="callingPlugin">Displays a custom message for the plugin name. This will be automatically inferred.</param>
    public static void Info(string message, string callingPlugin = "")
    {
        callingPlugin = GetCallingPlugin(GetCallingMethod(), callingPlugin, ShowCallingMethod);

        // &7[&b&6{type}&B&7] &7[&b&2{prefix}&B&7]&r
        Raw(Templates["Info"].Replace("{time}", GetDateString()).Replace("{prefix}", $"{callingPlugin,-5}").Replace("{msg}", message).Replace("{type}", "Info"));
    }

    /// <summary>
    /// Logs debugging information to the console.
    /// </summary>
    /// <param name="message">The message to log.</param>
    /// <param name="canLog">Can be used to prevent the log from being logged. Essentially an integrated if statement.
    /// <code>
    /// if(canLog)
    ///     Log.Debug();
    /// </code></param>
    /// <param name="callingPlugin">Displays a custom message for the plugin name. This will be automatically inferred.</param>
    public static void Debug(string message, bool canLog = true, string callingPlugin = "")
    {
        if (!canLog)
        {
            return;
        }

        callingPlugin = GetCallingPlugin(GetCallingMethod(), callingPlugin, ShowCallingMethod);

        // &7[&b&5{type}&B&7] &7[&b&2{prefix}&B&7]&r
        Raw(Templates["Debug"].Replace("{time}", GetDateString()).Replace("{prefix}", callingPlugin).Replace("{msg}", message).Replace("{type}", "Debug"));
    }

    /// <summary>
    /// Logs warning information to the console.
    /// </summary>
    /// <param name="message">The message to log.</param>
    /// <param name="callingPlugin">Displays a custom message for the plugin name. This will be automatically inferred.</param>
    public static void Warn(string message, string callingPlugin = "")
    {
        callingPlugin = GetCallingPlugin(GetCallingMethod(), callingPlugin, ShowCallingMethod);

        // &7[&b&3{type}&B&7] &7[&b&2{prefix}&B&7]&r
        Raw(Templates["Warn"].Replace("{time}", GetDateString()).Replace("{prefix}", callingPlugin).Replace("{msg}", message).Replace("{type}", "Warn"));
    }

    /// <summary>
    /// Logs error information to the console.
    /// </summary>
    /// <param name="message">The message to log.</param>
    /// <param name="callingPlugin">Displays a custom message for the plugin name. This will be automatically inferred.</param>
    public static void Error(string message, string callingPlugin = "")
    {
        callingPlugin = GetCallingPlugin(GetCallingMethod(), callingPlugin, ShowCallingMethod);

        // &7[&b&1{type}&B&7] &7[&b&2{prefix}&B&7]&r
        Raw(Templates["Error"].Replace("{time}", GetDateString()).Replace("{prefix}", callingPlugin).Replace("{msg}", message).Replace("{type}", "Error"));
    }

    /// <summary>
    /// Logs an exception and any relevant information to the console.
    /// </summary>
    /// <param name="exception">The exception being logged.</param>
    /// <param name="callingPlugin">Displays a custom message for the plugin name. This will be automatically inferred.</param>
    public static void Exception(Exception exception, string callingPlugin = "")
    {
        string message = $"An error has occured. {exception.Message}. Information: \n";
        Raw(Templates["Error"].Replace("{time}", GetDateString()).Replace("{prefix}", callingPlugin).Replace("{msg}", message).Replace("{type}", "Error"));
        for (Exception? e = exception; e != null; e = e.InnerException)
        {
            string msg1 = "Exception Information";
            string name = e.GetType().FullName!;
            if (name.Length > msg1.Length)
                msg1 = msg1.PadBoth(name.Length);
            else
                name = name.PadBoth(msg1.Length);
            string msg2 = $"&h[&b {msg1} &h]&a".PadBoth(100, '-');
            string name1 = $"&h[&1 {name} &h]&a".PadBoth(100);
            Raw($"&h[&a{msg2}&h]");
            Raw($" {name1} ");
            Raw("&h" + e.Message + "\n&h" + e.StackTrace);
            if (e is ReflectionTypeLoadException typeLoadException)
            {
                for (int index = 0; index < typeLoadException.Types.Length; ++index)
                    Raw("&7ReflectionTypeLoadException.Types[&3" + index + "&7]: &6" + typeLoadException.Types[index]);
                for (int index = 0; index < typeLoadException.LoaderExceptions.Length; ++index)
                    Exception(typeLoadException.LoaderExceptions[index]); // (tag + (tag == null ? "" : ", ") + "rtle:" + index.ToString());
            }

            if (e is TypeLoadException)
                Raw("TypeLoadException.TypeName: " + ((TypeLoadException)e).TypeName);
            if (e is BadImageFormatException)
                Raw("BadImageFormatException.FileName: " + ((BadImageFormatException)e).FileName);
        }
    }

    /// <summary>
    /// Logs an exception and the respective information to the console.
    /// </summary>
    /// <param name="e">The exception to log.</param>
    /// <param name="callingPlugin">The name of the calling plugin.</param>
    public static void Error(Exception e, string callingPlugin = "")
    {
        string errorMsg = $"{e}";
        Raw(Templates["Error"].Replace("{time}", GetDateString()).Replace("{prefix}", callingPlugin).Replace("{msg}", errorMsg).Replace("{type}", "Error"));
    }

    /// <summary>
    /// Used to rewrite a BepInEx log into the new method of logging, but skip several stack trace iterations.
    /// </summary>
    /// <param name="message">The previous message.</param>
    /// <param name="level">The log level.</param>
    internal static void Skip(string message, LogLevel level)
    {
        string callingPlugin = GetCallingPlugin(GetCallingMethod(6), string.Empty, ShowCallingMethod);
        string template = level switch
        {
            LogLevel.Info => "Info",
            LogLevel.Message => "Info",
            LogLevel.Debug => "Debug",
            LogLevel.Warning => "Warn",
            LogLevel.Error => "Error",
            LogLevel.Fatal => "Error",
            _ => "Info",
        };

        Raw(Templates[template].Replace("{time}", GetDateString()).Replace("{prefix}", callingPlugin).Replace("{msg}", message).Replace("{type}", template));
    }

    /// <summary>
    /// Logs information to the console, without adding any text to the message.
    /// </summary>
    /// <param name="message">The message to log.</param>
    // ReSharper disable once Unity.PerformanceCriticalCodeInvocation
    public static void Raw(string message) => LogMessage.Invoke(message);

    /// <summary>
    /// Called on a message log.
    /// </summary>
    public static event Action<string> LogMessage = null!;

    private static MethodBase GetCallingMethod(int skip = 0)
    {
        StackTrace stack = new (2 + skip);

        return stack.GetFrame(0).GetMethod();
    }

    private static string PadBoth(this string source, int length, char padChar = ' ')
    {
        int spaces = length - source.Length;
        int padLeft = (spaces / 2) + source.Length;
        return source.PadLeft(padLeft, padChar).PadRight(length, padChar);
    }
}