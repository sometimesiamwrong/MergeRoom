using Microsoft.AspNetCore.Mvc;
using Prometheus;
using System.Text;

namespace DiscordMergeRoomBotCsharpEdition.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class AppController : ControllerBase
    {
        [HttpGet("logs")]
        public async Task<IActionResult> GetLogs()
        {
            var date = DateTimeOffset.UtcNow.Date.ToString("yyyyMMdd");
            var logFilePath = Path.Combine(Directory.GetCurrentDirectory(), "logs", $"log{date}.txt");

            if (!System.IO.File.Exists(logFilePath))
            {
                return NotFound("Log file not found");
            }

            // Создание временной копии файла
            var tempFilePath = Path.Combine(Path.GetTempPath(), $"log{date}_copy.txt");

            try
            {
                System.IO.File.Copy(logFilePath, tempFilePath, true);
                var logContent = await System.IO.File.ReadAllTextAsync(tempFilePath);
                return Ok(logContent);
            }
            catch (IOException ex)
            {
                return StatusCode(500, $"Error reading log file: {ex.Message}");
            }
            finally
            {
                // Удаление временного файла
                if (System.IO.File.Exists(tempFilePath))
                {
                    System.IO.File.Delete(tempFilePath);
                }
            }
        }

        [HttpGet("logs/full")]
        public async Task<IActionResult> GetFullLogs()
        {
            var date = DateTimeOffset.UtcNow.Date.ToString("yyyyMMdd");
            var logFilePath = Path.Combine(Directory.GetCurrentDirectory(), "logs", $"fullLog{date}.txt");

            if (!System.IO.File.Exists(logFilePath))
            {
                return NotFound("Log file not found");
            }

            // Создание временной копии файла
            var tempFilePath = Path.Combine(Path.GetTempPath(), $"fullLog{date}_copy.txt");

            try
            {
                System.IO.File.Copy(logFilePath, tempFilePath, true);
                var logContent = await System.IO.File.ReadAllTextAsync(tempFilePath);
                return Ok(logContent);
            }
            catch (IOException ex)
            {
                return StatusCode(500, $"Error reading log file: {ex.Message}");
            }
            finally
            {
                // Удаление временного файла
                if (System.IO.File.Exists(tempFilePath))
                {
                    System.IO.File.Delete(tempFilePath);
                }
            }
        }

        [HttpGet("logs/w_e")]
        public async Task<IActionResult> GetMinWaringLogs()
        {
            var date = DateTimeOffset.UtcNow.Date.ToString("yyyyMMdd");
            var logFilePath = Path.Combine(Directory.GetCurrentDirectory(), "logs", $"errorLog{date}.txt");

            if (!System.IO.File.Exists(logFilePath))
            {
                return NotFound("Error Log file not found");
            }

            // Создание временной копии файла
            var tempFilePath = Path.Combine(Path.GetTempPath(), $"errorLog{date}_copy.txt");

            try
            {
                System.IO.File.Copy(logFilePath, tempFilePath, true);
                var logContent = await System.IO.File.ReadAllTextAsync(tempFilePath);
                return Ok(logContent);
            }
            catch (IOException ex)
            {
                return StatusCode(500, $"Error reading log file: {ex.Message}");
            }
            finally
            {
                // Удаление временного файла
                if (System.IO.File.Exists(tempFilePath))
                {
                    System.IO.File.Delete(tempFilePath);
                }
            }
        }

        [HttpGet("health")]
        public Task<IActionResult> HealthCheck()
        {
            return Task.FromResult<IActionResult>(Ok("OK"));
        }

        [HttpGet("metrics")]
        public async Task<ContentResult> GetMetrics()
        {
            var metricsSnapshot = new StringBuilder();
            using (var stream = new MemoryStream())
            {
                await Metrics.DefaultRegistry.CollectAndExportAsTextAsync(stream);
                stream.Position = 0;
                using (var reader = new StreamReader(stream))
                {
                    metricsSnapshot.Append(await reader.ReadToEndAsync());
                }
            }

            return Content(metricsSnapshot.ToString(), "text/plain");
        }
    }
}
