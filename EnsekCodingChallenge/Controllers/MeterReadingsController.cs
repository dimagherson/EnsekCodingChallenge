using System;
using System.Threading.Tasks;
using EnsekCodingChallenge.Api.Contracts.Output;
using EnsekCodingChallenge.Application.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace EnsekCodingChallenge.Controllers
{
    [ApiController]
    [Route("/")]
    public class MeterReadingsController : ControllerBase
    {
        private readonly IMeterReadingsService _meterReadingsService;

        public MeterReadingsController(IMeterReadingsService meterReadingsService)
        {
            _meterReadingsService = meterReadingsService ?? throw new ArgumentNullException(nameof(meterReadingsService));
        }

        [HttpPost("meter-reading-uploads")]
        public async Task<IActionResult> UploadMeterReadings([FromQuery] IFormFile file)
        {
            if (file?.Length > 0)
            {
                var stream = file.OpenReadStream();
                var context = await _meterReadingsService.ProcessStream(stream);
                var model = CreateResponseModel(context);

                return Ok(model);
            }

            ModelState.AddModelError(nameof(file), "Cannot be empty.");

            return ValidationProblem();
        }

        private MeterReadingsModel CreateResponseModel(MeterReadingContext context)
        {
            var model = new MeterReadingsModel
            {
                Successes = context.ValidCount,
                Failures = context.InvalidCount
            };

            foreach (var entry in context.InvalidEntryContexts)
            {
                model.Errors.Add(new MeterReadingEntryErrorModel
                {
                    LineNumber = entry.LineNumber,
                    Error = entry.Error
                });
            }

            return model;
        }
    }
}
