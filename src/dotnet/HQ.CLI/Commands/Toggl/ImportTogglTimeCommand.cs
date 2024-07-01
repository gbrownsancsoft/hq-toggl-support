using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

using FluentResults;

using HQ.Abstractions.ChargeCodes;
using HQ.Abstractions.Times;
using HQ.Abstractions.Voltron;
using HQ.SDK;

using Spectre.Console;
using Spectre.Console.Cli;
using Spectre.Console.Json;

namespace HQ.CLI.Commands.ChargeCode
{
    internal class ImportTogglTimeSettings : HQCommandSettings
    {
        [CommandOption("--from")]
        public DateOnly From { get; set; }

        [CommandOption("--to")]
        public DateOnly To { get; set; }
    }

    internal class ImportTogglTimeCommand : AsyncCommand<ImportTogglTimeSettings>
    {
        private readonly HQServiceV1 _hqService;
        private readonly HQConfig _config;

        public ImportTogglTimeCommand(HQConfig config, HQServiceV1 hqService)
        {
            _hqService = hqService;
            _config = config;
        }

        public override async Task<int> ExecuteAsync(CommandContext context, ImportTogglTimeSettings settings)
        {
            if (String.IsNullOrEmpty(_config.TogglUserName) || String.IsNullOrEmpty(_config.TogglPassword))
            {
                AnsiConsole.MarkupLine("[red]Toggl credentials must be entered before importing[/]");
                Console.WriteLine("Run the configure option for the toggl command to enter credentials");

                return 1;
            }

            List<TogglRecord> records = (await GetRecordsAsync(settings.From, settings.To, _config.TogglUserName!, _config.TogglPassword!)) ?? new List<TogglRecord>();
            records = records.Where(t => t.Start >= settings.From.ToDateTime(TimeOnly.MinValue) && t.Start <= settings.To.ToDateTime(TimeOnly.MaxValue)).ToList();

            Console.WriteLine($"Processing {records.Count} records");
            foreach (TogglRecord record in records)
            {
                UpsertTimeV1.Request request = new UpsertTimeV1.Request()
                {
                    Date = DateOnly.FromDateTime(record.Start),
                    Hours = (decimal?)record.Duration,
                    Notes = record.Description,
                    ChargeCode = record.Quote
                };

                var response = await _hqService.UpsertTimeEntryV1(request);

                if (!response.IsSuccess || response.Value == null)
                {
                    ErrorHelper.Display(response);
                }
            }

            Console.WriteLine("Import Complete, Verify Entries in HQ Before Submitting");

            return 0;
        }

        async static Task<List<TogglRecord>?> GetRecordsAsync(DateOnly start, DateOnly end, string userName, string pass)
        {
            string url = $"https://api.track.toggl.com/api/v9/me/time_entries?meta=true&start_date={start.ToString("yyyy-MM-dd")}&end_date={end.AddDays(1).ToString("yyyy-MM-dd")}";

            List<TogglRecord>? records;
            using (HttpClient client = new HttpClient())
            {
                HttpRequestMessage message = new HttpRequestMessage(HttpMethod.Get, new Uri(url));
                message.Headers.Authorization = new AuthenticationHeaderValue("Basic", System.Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes($"{userName}:{pass}")));
                message.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                HttpResponseMessage response = await client.SendAsync(message);
                string json = await response.Content.ReadAsStringAsync();

                records = System.Text.Json.JsonSerializer.Deserialize<List<TogglRecord>>(json, new System.Text.Json.JsonSerializerOptions()
                {
                    PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.SnakeCaseLower
                });
            }

            if (records != null)
            {
                List<TogglRecord> distinct = records.DistinctBy(t => t.Description).ToList();
                foreach (TogglRecord record in distinct)
                {
                    records.Remove(record);
                }

                foreach (TogglRecord record in records)
                {
                    TogglRecord? matching = distinct.Find(t => t.Description == record.Description && t.Quote == record.Quote);

                    if (matching != null)
                    {
                        distinct[distinct.FindIndex(t => t == matching)].Duration = (matching.Duration + record.Duration) * 3600;
                    }
                }

                records = distinct;
            }

            return records;
        }
    }

    internal class TogglRecord
    {
        private string? _project_name { get; set; }
        public int _duration { get; private set; }

        public string? Quote
        {
            get
            {
                if (String.IsNullOrEmpty(_project_name) || !_project_name.Contains(" - "))
                {
                    return null;
                }

                return _project_name.Split(" - ")[1];
            }
        }
        public double Duration
        {
            set
            {
                _duration = (int)value;
            }

            get
            {
                TimeSpan duration = TimeSpan.FromSeconds(_duration);

                if (duration.TotalSeconds < 900)
                {
                    duration = TimeSpan.FromMinutes(15);
                }

                double temp;
                if ((temp = duration.TotalMinutes / 15) % 1 != 0)
                {
                    duration = TimeSpan.FromMinutes(15 * Math.Round(temp + 0.04));
                }

                return duration.TotalHours;
            }
        }
        public string? Description { get; set; }
        public string? ClientName { get; set; }
        public string? ProjectName
        {
            set
            {
                _project_name = value;
            }

            get
            {
                if (String.IsNullOrEmpty(_project_name))
                {
                    return null;
                }

                if (_project_name.Contains(" - "))
                {
                    return _project_name.Split(" - ")[0];
                }

                return _project_name;
            }
        }
        public string? _Billable { get; set; } = null;
        public DateTime Start { get; set; }

        public TogglRecord() { }
    }
}