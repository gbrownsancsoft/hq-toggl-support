using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging;

using Spectre.Console;
using Spectre.Console.Cli;

namespace HQ.CLI.Commands;

internal class LayoutTogglCommand : AsyncCommand<HQCommandSettings>
{
    public LayoutTogglCommand() { }

    public override Task<int> ExecuteAsync(CommandContext context, HQCommandSettings settings)
    {
        Console.WriteLine("For accurate parsing of Toggl entries, structure them with the following:\n");

        AnsiConsole.MarkupLine("[yellow3]Description:[/] Notes about the entry (activites and tasks can be entered for parsing as shown below)");
        AnsiConsole.MarkupLine("     [darkorange]Activity:[/] Stored in the description with the syntax [bold][[[[activity name]]]][/]");
        AnsiConsole.MarkupLine("     [darkorange]Task:[/] Stored in the description with the syntax [bold]((task))[/]\n");
        AnsiConsole.MarkupLine("[yellow3]Client:[/] Client that the project is associated with");
        AnsiConsole.MarkupLine("[yellow3]Project:[/] Toggl project should be setup with the syntax \"[bold]Project Name - Quote Number[/]\"\n");

        return Task.FromResult(0);
    }
}