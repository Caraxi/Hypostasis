using System;
using System.Collections.Generic;
using System.Reflection;
using Dalamud.Game.Command;

namespace Hypostasis.Dalamud;

[AttributeUsage(AttributeTargets.Method)]
public class PluginCommandAttribute : Attribute
{
    public string[] Commands { get; init; }
    public string HelpMessage { get; init; } = string.Empty;
    public bool ShowInHelp { get; init; } = true;
    public PluginCommandAttribute(params string[] commands) => Commands = commands;
}

public class PluginCommandManager : IDisposable
{
    private readonly HashSet<string> pluginCommands = new();

    public PluginCommandManager(object o)
    {
        foreach (var method in o.GetType().GetMethods(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic))
            AddPluginCommandMethod(o, method);
    }

    private void AddPluginCommandMethod(object o, MethodInfo method)
    {
        var attribute = method.GetCustomAttribute<PluginCommandAttribute>();
        if (attribute == null) return;

        var handlerDelegate = (CommandInfo.HandlerDelegate)Delegate.CreateDelegate(typeof(CommandInfo.HandlerDelegate), o, method);
        var commandInfo = new CommandInfo(handlerDelegate)
        {
            HelpMessage = attribute.HelpMessage,
            ShowInHelp = attribute.ShowInHelp,
        };

        foreach (var command in attribute.Commands)
        {
            if (DalamudApi.CommandManager.AddHandler(command, commandInfo))
                pluginCommands.Add(command);
        }
    }

    public void Dispose()
    {
        foreach (var command in pluginCommands)
            DalamudApi.CommandManager.RemoveHandler(command);
        GC.SuppressFinalize(this);
    }
}