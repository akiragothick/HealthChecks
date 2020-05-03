using Microsoft.AspNetCore.Mvc;

namespace Client.Monitor.API.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class WeatherForecastController : ControllerBase
    {
        public WeatherForecastController()
        {
        }

        [HttpGet]
        public IActionResult Get() => Ok();
    }
}
