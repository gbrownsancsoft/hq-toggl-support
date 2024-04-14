﻿using FluentResults;
using HQ.Abstractions.Clients;
using HQ.SDK;
using Spectre.Console;
using Spectre.Console.Cli;
using Spectre.Console.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace HQ.CLI.Commands.Clients
{
    internal class CreateClientSettings : HQCommandSettings
    {
    }

    internal class CreateClientCommand : AsyncCommand<CreateClientSettings>
    {
        private readonly HQServiceV1 _hqService;

        public CreateClientCommand(HQServiceV1 hqService)
        {
            _hqService = hqService;
        }

        public override async Task<int> ExecuteAsync(CommandContext context, CreateClientSettings settings)
        {
            var model = new CreateClientV1.Request();

            var Createor = new YAMLEditor<CreateClientV1.Request>(model, async (value) =>
            {
                return await _hqService.CreateClientV1(value);
            });

            var rc = await Createor.Launch();
            return rc;
        }
    }
}
