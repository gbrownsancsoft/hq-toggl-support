
using System.Globalization;
using System.Net.Http.Headers;
using System.Xml;

using CsvHelper;
using CsvHelper.Configuration;

using HQ.Abstractions.Times;

using Spectre.Console;

namespace HQ.CLI.Commands.Toggl
{
    public class TogglRecord
    {
        private bool? _billable { get; set; }
        private string? _project_name { get; set; }
        public int _duration { get; private set; }
        private DateTime _startTime { get; set; }
        public DateTime Start
        {
            get
            {
                return TimeZoneInfo.ConvertTimeFromUtc(_startTime, TimeZoneInfo.FindSystemTimeZoneById("Eastern Standard Time"));
            }
            set
            {
                _startTime = new DateTime(value.Ticks, DateTimeKind.Utc);
            }
        }

        public DateOnly StartDate
        {
            get
            {
                return DateOnly.FromDateTime(TimeZoneInfo.ConvertTimeFromUtc(_startTime, TimeZoneInfo.FindSystemTimeZoneById("Eastern Standard Time")));
            }
        }

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

        public string? Billable
        {
            get
            {
                if (Quote == null)
                {
                    return null;
                }

                return Quote.ToLower()[0] == 'q' ? "Yes" : "No";
            }
        }

        public TogglRecord() { }

        public UpsertTimeV1.Request ToUpsertTimeV1Request(Guid id)
        {
            Dictionary<string, string?> parsedDescription = ParseDescription();

            return new UpsertTimeV1.Request()
            {
                Date = DateOnly.FromDateTime(Start),
                Hours = (decimal?)Duration,
                Notes = parsedDescription["description"],
                ChargeCode = Quote,
                Task = parsedDescription["task"],
                ActivityName = parsedDescription["activity"],
                StaffId = id
            };
        }

        public TogglRecordCsvRow ToCsvRow(string? firstName, string? lastName)
        {
            string staff = "unknown";

            if (firstName != null && lastName != null)
            {
                staff = $"{firstName.ToLower()[0]}{lastName.ToLower()}";
            }

            Dictionary<string, string?> parsedDescription = ParseDescription();

            return new TogglRecordCsvRow()
            {
                Date = DateOnly.FromDateTime(Start).ToString(),
                Hours = ((decimal?)Duration)?.ToString() ?? "0.00",
                Notes = parsedDescription["description"] ?? "",
                Quote = Quote ?? "",
                Activity = (parsedDescription["activity"] ?? parsedDescription["task"]) ?? "",
                Staff = staff,
                Client = ClientName ?? "",
                Project = ProjectName ?? "",
                Billable = Billable ?? ""
            };
        }

        public bool IsValid()
        {
            return _duration > 2;
        }

        private Dictionary<string, string?> ParseDescription()
        {
            Dictionary<string, string?> returnValue = new Dictionary<string, string?>()
            {
                { "description",  null },
                { "activity", null },
                { "task", null }
            };

            if (String.IsNullOrEmpty(Description))
            {
                return returnValue;
            }

            string description = Description;
            if (description.Contains("[[") && description.Contains("]]"))
            {
                string activity = description.Substring(description.IndexOf("[[") + 2, description.IndexOf("]]") - description.IndexOf("[[") - 2);
                returnValue["activity"] = activity;

                description = description.Replace($"[[{activity}]]", "");
            }

            if (description.Contains("((") && description.Contains("))"))
            {
                string task = description.Substring(description.IndexOf("((") + 2, description.IndexOf("))") - description.IndexOf("((") - 2);
                returnValue["task"] = task;

                description = description.Replace($"(({task}))", "");
            }

            while (description.Contains("  "))
            {
                description = description.Replace("  ", " ");
            }

            returnValue["description"] = description.Trim();
            return returnValue;
        }
    }

    public class TogglRecordCsvRow
    {
        public string Date { get; set; } = null!;
        public string Staff { get; set; } = null!;
        public string Client { get; set; } = null!;
        public string Project { get; set; } = null!;
        public string Quote { get; set; } = null!;
        public string Hours { get; set; } = null!;
        public string Billable { get; set; } = null!;
        public string Notes { get; set; } = null!;
        public string Activity { get; set; } = null!;
    }

    public static class TogglOperations
    {
        public async static Task<List<TogglRecord>?> GetRecordsAsync(DateOnly start, DateOnly end, string userName, string pass)
        {
            DateTime startDateTime = TimeZoneInfo.ConvertTimeToUtc(start.ToDateTime(TimeOnly.MinValue), TimeZoneInfo.FindSystemTimeZoneById("Eastern Standard Time"));
            DateTime endDateTime = TimeZoneInfo.ConvertTimeToUtc(end.AddDays(1).ToDateTime(TimeOnly.MinValue), TimeZoneInfo.FindSystemTimeZoneById("Eastern Standard Time"));

            AnsiConsole.MarkupLine($"Getting Records Between [blue]{startDateTime.ToShortDateString()} {startDateTime.ToShortTimeString()}[/] and [blue]{endDateTime.ToShortDateString()} {endDateTime.ToShortTimeString()}[/]\n");
            string url = $"https://api.track.toggl.com/api/v9/me/time_entries?meta=true&start_date={XmlConvert.ToString(startDateTime, XmlDateTimeSerializationMode.Utc)}&end_date={XmlConvert.ToString(endDateTime, XmlDateTimeSerializationMode.Utc)}";

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
                records = records.Where(t => t.IsValid()).ToList();
                List<TogglRecord> distinct = records.GroupBy(t => new { t.Description, t.StartDate }).Select(t => t.First()).ToList();
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
}