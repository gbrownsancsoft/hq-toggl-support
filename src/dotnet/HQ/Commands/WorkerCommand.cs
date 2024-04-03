﻿using Spectre.Console.Cli;

namespace HQ.Commands;

public class WorkerCommand : AsyncCommand
{
    public override Task<int> ExecuteAsync(CommandContext context)
    {
        var args = context.Remaining.Raw.ToArray();
        var builder = Host.CreateApplicationBuilder(args);
        builder.Services.AddHostedService<Worker>();

        var host = builder.Build();
        host.Run();

        return Task.FromResult(0);
    }
}