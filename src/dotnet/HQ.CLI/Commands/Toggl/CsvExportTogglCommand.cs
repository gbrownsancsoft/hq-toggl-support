using HQ.Abstractions.Staff;
using HQ.Abstractions.Times;
using HQ.SDK;

using Spectre.Console;
using Spectre.Console.Cli;

namespace HQ.CLI.Commands.Toggl
{
    internal class CsvExportTogglTimeSettings : HQCommandSettings
    {
        [CommandOption("--from")]
        public DateOnly? From { get; set; }

        [CommandOption("--to")]
        public DateOnly? To { get; set; }
    }

    internal class CsvExportTogglTimeCommand : AsyncCommand<CsvExportTogglTimeSettings>
    {
        private readonly HQServiceV1 _hqService;
        private readonly HQConfig _config;

        public CsvExportTogglTimeCommand(HQConfig config, HQServiceV1 hqService)
        {
            _hqService = hqService;
            _config = config;
        }

        public override async Task<int> ExecuteAsync(CommandContext context, CsvExportTogglTimeSettings settings)
        {
            settings.Output = OutputFormat.CSV;

            if (String.IsNullOrEmpty(_config.TogglUserName) || String.IsNullOrEmpty(_config.TogglPassword))
            {
                AnsiConsole.MarkupLine("[red]Toggl credentials must be entered before importing[/]");
                Console.WriteLine("Run the configure option for the toggl command to enter credentials");

                return 1;
            }

            DateOnly start = DateOnly.FromDateTime(DateTime.Now);
            DateOnly end = DateOnly.FromDateTime(DateTime.Now);

            if (settings.From.HasValue && settings.To.HasValue)
            {
                if (settings.From.HasValue)
                {
                    start = settings.From.Value;
                }

                if (settings.To.HasValue)
                {
                    end = settings.To.Value;
                }
            }
            else
            {
                AnsiConsole.MarkupLine("[yellow3]No dates provided, defaulting to today[/]\n");
            }

            if (_config.StaffId == null || !_config.StaffId.HasValue)
            {
                AnsiConsole.MarkupLine("[red]Unable to determine ID of current user[/]");
                return 1;
            }

            GetStaffV1.Record? staff = (await _hqService.GetStaffV1(new()
            {
                Id = _config.StaffId,
            })).Value?.Records.FirstOrDefault();

            List<TogglRecord> records = (await TogglOperations.GetRecordsAsync(start, end, _config.TogglUserName!, _config.TogglPassword!)) ?? new List<TogglRecord>();

            AnsiConsole.MarkupLine($"Processing [yellow3]{records.Count}[/] records\n");
            List<TogglRecordCsvRow> converted = records.Where(t => t.IsValid()).Select(t => t.ToCsvRow(staff?.FirstName, staff?.LastName)).ToList();

            OutputHelper.Create(converted, converted)
                .WithColumn("DATE", t => t.Date)
                .WithColumn("STAFF", t => t.Staff)
                .WithColumn("CLIENT", t => t.Client)
                .WithColumn("QUOTE", t => t.Quote)
                .WithColumn("HOURS", t => t.Hours)
                .WithColumn("BILLABLE", t => t.Billable)
                .WithColumn("Notes", t => t.Notes)
                .WithColumn("ACTIVITY / TASK", t => t.Activity)
                .Output(settings.Output);

            return 0;
        }
    }
}