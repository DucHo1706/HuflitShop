using HuflitShopCore.Services;
using Microsoft.AspNetCore.Mvc;

namespace HuflitShopCore.Controllers
{
    [ApiController]
    public class GhnWebhookController : ControllerBase
    {
        private readonly ShipmentService _shipmentService;
        private readonly IConfiguration _configuration;

        public GhnWebhookController(ShipmentService shipmentService, IConfiguration configuration)
        {
            _shipmentService = shipmentService;
            _configuration = configuration;
        }

        [HttpPost("api/webhooks/ghn")]
        public async Task<IActionResult> Handle([FromBody] GhnWebhookRequest webhook, [FromQuery] string? token, CancellationToken cancellationToken)
        {
            var configuredToken = _configuration["GHN:WebhookToken"];
            if (!string.IsNullOrWhiteSpace(configuredToken) && !string.Equals(configuredToken, token, StringComparison.Ordinal))
                return Unauthorized();

            await _shipmentService.ApplyWebhookAsync(webhook, cancellationToken);
            return Ok(new { success = true });
        }
    }
}
