using BabyKusto.Server.Contract;
using BabyKusto.Server.Service;
using Microsoft.AspNetCore.Mvc;

namespace Sample.CsvServer.Controllers
{
    [ApiController]
    public class QueryController(
        QueryEndpointHelper queryEndpointHelper,
        QueryV2EndpointHelper queryV2EndpointHelper,
        ILogger<QueryController> logger)
        : ControllerBase
    {
        private readonly QueryEndpointHelper _queryEndpointHelper = queryEndpointHelper ?? throw new ArgumentNullException(nameof(queryEndpointHelper));
        private readonly QueryV2EndpointHelper _queryV2EndpointHelper = queryV2EndpointHelper ?? throw new ArgumentNullException(nameof(queryV2EndpointHelper));
        private readonly ILogger<QueryController> _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        [HttpPost]
        [Route("/v1/rest/query")]
        public IActionResult Execute(KustoApiQueryRequestBody body)
        {
            if (body == null)
            {
                return BadRequest();
            }

            try
            {
                var result = _queryEndpointHelper.Process(body);
                return Ok(result);
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogError(ex, "Error processing query api request.");
                return BadRequest(ex.ToString());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Unexpected error processing query api request, input: {body.Csl}.");
                return BadRequest(ex.ToString());
            }
        }

        [HttpPost]
        [Route("/v2/rest/query")]
        public async Task ExecuteV2(KustoApiQueryRequestBody body)
        {
            if (body == null)
            {
                Response.StatusCode = StatusCodes.Status400BadRequest;
                return;
            }

            try
            {
                Response.StatusCode = StatusCodes.Status200OK;
                await _queryV2EndpointHelper.Process(body, HttpContext);
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogError(ex, "Error processing query api request.");
                if (!Response.HasStarted)
                {
                    Response.StatusCode = StatusCodes.Status400BadRequest;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Unexpected error processing query api request, input: {body.Csl}.");
                if (!Response.HasStarted)
                {
                    Response.StatusCode = StatusCodes.Status400BadRequest;
                    await Response.WriteAsync(ex.ToString());
                }
                return;
            }
        }
    }
}