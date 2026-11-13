using System;
using Microsoft.AspNetCore.Mvc;
using TimeSeriesApi.Data;
using TimeSeriesApi.Models.Api;
using TimeSeriesApi.Services;

namespace TimeSeriesApi.Controllers
{
    [ApiController]
    [Route("api/timeseries")]
    [Produces("application/json")]
    public class TimeSeriesController : ControllerBase
    {
        private readonly ITimeSeriesRepository _repository;
        private readonly ITimeSeriesValidator _validator;

        public TimeSeriesController(ITimeSeriesRepository repository, ITimeSeriesValidator validator)
        {
            _repository = repository;
            _validator = validator;
        }

        /// <summary>Lists time series, optionally filtered by bidding zone, type and time interval.</summary>
        [HttpGet]
        [ProducesResponseType<List<TimeSeriesResponse>>(StatusCodes.Status200OK)]
        [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<List<TimeSeriesResponse>>> GetAll([FromQuery] TimeSeriesQuery query, CancellationToken ct)
        {
            var errors = _validator.Validate(query);
            if (errors.Count > 0)
            {
                return ValidationProblem(new ValidationProblemDetails(errors));
            }

            var series = await _repository.GetAsync(query, ct);
            return Ok(series.Select(TimeSeriesMapper.ToResponse).ToList());
        }

        [HttpGet("{id:guid}")]
        [ProducesResponseType<TimeSeriesResponse>(StatusCodes.Status200OK)]
        [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<TimeSeriesResponse>> GetById(Guid id, CancellationToken ct)
        {
            var series = await _repository.GetByIdAsync(id, ct);
            if (series is null)
            {
                return NotFound();
            }

            return Ok(TimeSeriesMapper.ToResponse(series));
        }

        [HttpPost]
        [ProducesResponseType<TimeSeriesResponse>(StatusCodes.Status201Created)]
        [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<TimeSeriesResponse>> Create(TimeSeriesRequest request, CancellationToken ct)
        {
            var errors = _validator.Validate(request);
            if (errors.Count > 0)
            {
                return ValidationProblem(new ValidationProblemDetails(errors));
            }

            var created = await _repository.AddAsync(TimeSeriesMapper.ToDomain(request), ct);
            var response = TimeSeriesMapper.ToResponse(created);

            return CreatedAtAction(nameof(GetById), new { id = created.Id }, response);
        }

        /// <summary>Replaces an existing time series.</summary>
        [HttpPut("{id:guid}")]
        [ProducesResponseType<TimeSeriesResponse>(StatusCodes.Status200OK)]
        [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
        [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<TimeSeriesResponse>> Update(Guid id, TimeSeriesRequest request, CancellationToken ct)
        {
            var errors = _validator.Validate(request);
            if (errors.Count > 0)
            {
                return ValidationProblem(new ValidationProblemDetails(errors));
            }

            var series = TimeSeriesMapper.ToDomain(request);
            series.Id = id;

            if (!await _repository.UpdateAsync(series, ct))
            {
                return NotFound();
            }

            var updated = await _repository.GetByIdAsync(id, ct);
            return updated is null ? NotFound() : Ok(TimeSeriesMapper.ToResponse(updated));
        }

        [HttpDelete("{id:guid}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
        {
            if (!await _repository.DeleteAsync(id, ct))
            {
                return NotFound();
            }

            return NoContent();
        }
    }
}