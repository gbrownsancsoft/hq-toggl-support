using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging;

using Spectre.Console;
using Spectre.Console.Cli;

namespace HQ.CLI.Commands;

internal class ConfigureTogglCommand : AsyncCommand<HQCommandSettings>
{
    private readonly HQConfig _config;

    public ConfigureTogglCommand(HQConfig config)
    {
        _config = config;
    }

    public override Task<int> ExecuteAsync(CommandContext context, HQCommandSettings settings)
    {
        _config.TogglUserName = AnsiConsole.Prompt(
            new TextPrompt<string>("Enter Toggl Username:")
        );

        _config.TogglPassword = AnsiConsole.Prompt(
            new TextPrompt<string>("Enter Toggl Password:")
        );

        Console.WriteLine("Username and password successfully saved, it is a good idea to execute the 'layout' Toggl command to see how the Toggl entries should be structured");

        return Task.FromResult(0);
    }
}