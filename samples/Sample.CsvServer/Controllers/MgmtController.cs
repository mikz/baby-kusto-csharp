using BabyKusto.Server.Contract;
using BabyKusto.Server.Service;
using Microsoft.AspNetCore.Mvc;

namespace Sample.CsvServer.Controllers
{
    [ApiController]
    public class MgmtController(ManagementEndpointHelper managementEndpointHelper, ILogger<MgmtController> logger)
        : ControllerBase
    {
        private readonly ManagementEndpointHelper _managementEndpointHelper = managementEndpointHelper ?? throw new ArgumentNullException(nameof(managementEndpointHelper));
        private readonly ILogger<MgmtController> _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        [HttpPost]
        [Route("/v1/rest/mgmt")]
        public IActionResult Execute(KustoApiMgmtRequestBody body)
        {
            if (body == null)
            {
                return BadRequest();
            }

            try
            {
                var result = _managementEndpointHelper.Process(body);
                return Ok(result);
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogError(ex, "Error processing mgmt api request.");
                return BadRequest(ex.ToString());
            }
        }
    }
}