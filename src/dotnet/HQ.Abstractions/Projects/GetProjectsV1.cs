﻿using HQ.Abstractions.Common;
using HQ.Abstractions.Enumerations;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HQ.Abstractions.Projects;

public class GetProjectsV1
{
    public class Request : PagedRequestV1
    {
        public string? Search { get; set; }
        public Guid? Id { get; set; }

        public SortColumn SortBy { get; set; } = SortColumn.Name;
        public SortDirection SortDirection { get; set; } = SortDirection.Asc;
    }

    public enum SortColumn
    {
        CreatedAt,
        Name
    }

    public class Response : PagedResponseV1<Record>;
    public class Record
    {
        public Guid Id { get; set; }
        public int ProjectNumber { get; set; }
        public Guid ClientId { get; set; }
        public string ClientName { get; set; } = null!;
        public Guid? ProjectManagerId { get; set; }
        public string? ProjectManagerName { get; set; }
        public string Name { get; set; } = null!;
        public Guid? QuoteId { get; set; }
        public int? QuoteNumber { get; set; }
        public string? ChargeCode { get; set; } = null!;
        // Letter of engagement
        public decimal HourlyRate { get; set; }
        public decimal BookingHours { get; set; }
        public Period BookingPeriod { get; set; }
        public DateOnly? StartDate { get; set; }
        public DateOnly? EndDate { get; set; }
    }
}
