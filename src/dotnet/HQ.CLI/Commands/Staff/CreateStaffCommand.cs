﻿using FluentResults;
using HQ.Abstractions.Staff;
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

namespace HQ.CLI.Commands.Staff
{
    internal class CreateStaffettings : HQCommandSettings
    {
    }

    internal class CreateStaffCommand : AsyncCommand<CreateStaffettings>
    {
        private readonly HQServiceV1 _hqService;

        public CreateStaffCommand(HQServiceV1 hqService)
        {
            _hqService = hqService;
        }

        public override async Task<int> ExecuteAsync(CommandContext context, CreateStaffettings settings)
        {
            var model = new UpsertStaffV1.Request();

            var Createor = new YAMLEditor<UpsertStaffV1.Request>(model, async (value) =>
            {
                return await _hqService.UpsertStaffV1(value);
            });

            var rc = await Createor.Launch();
            return rc;
        }
    }
}
