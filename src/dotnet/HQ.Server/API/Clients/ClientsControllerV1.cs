﻿using Asp.Versioning;
using FluentResults.Extensions.AspNetCore;
using HQ.Abstractions.Clients;
using HQ.Abstractions.Common;
using HQ.API;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HQ.Server.API.Clients
{
    [Authorize]
    [ApiController]
    [ApiVersion(1.0)]
    [Tags("Clients")]
    [Route("v{version:apiVersion}/Clients")]
    [ProducesResponseType<List<ErrorSummaryV1>>(StatusCodes.Status400BadRequest)]
    public class ClientsControllerV1 : ControllerBase
    {
        private readonly ClientServiceV1 _clientService;

        public ClientsControllerV1(ClientServiceV1 clientService)
        {
            _clientService = clientService;
        }

        [HttpPost(nameof(GetClientsV1))]
        [ProducesResponseType<GetClientsV1.Response>(StatusCodes.Status200OK)]
        public Task<ActionResult> GetClientsV1([FromBody] GetClientsV1.Request request, CancellationToken ct = default) => 
            _clientService.GetClientsV1(request, ct)
            .ToActionResult(new HQResultEndpointProfile());

        [HttpPost(nameof(CreateClientV1))]
        [ProducesResponseType<CreateClientV1.Response>(StatusCodes.Status201Created)]
        public Task<ActionResult> CreateClientV1([FromBody] CreateClientV1.Request request, CancellationToken ct = default) => 
            _clientService.CreateClientV1(request, ct)
            .ToActionResult(new HQResultEndpointProfile());

        [HttpPost(nameof(UpdateClientV1))]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public Task<ActionResult> UpdateClientV1([FromBody] UpdateClientV1.Request request, CancellationToken ct = default) =>
            _clientService.UpdateClientV1(request, ct)
            .ToActionResult(new HQResultEndpointProfile());

        [HttpPost(nameof(DeleteClientV1))]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public Task<ActionResult> DeleteClientV1([FromBody] DeleteClientV1.Request request, CancellationToken ct = default) =>
            _clientService.DeleteClientV1(request, ct)
            .ToActionResult(new HQResultEndpointProfile());
    }
}