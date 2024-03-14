using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Net;

namespace RegistryV2.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class MainController : ControllerBase
    {

        private readonly ILogger<MainController> _logger;
        private readonly AppDbContext _context;

        public MainController(AppDbContext context)
        {
            _context = context;
        }

        /*
        public IActionResult Index()
        {
            var filePath = Path.Combine(Directory.GetCurrentDirectory(), "webpage.html");
            return PhysicalFile(filePath, "text/html");
        }
        */

        private string[] allowedIpAddresses = { "10.0.0.134", "127.0.0.1" }; // Add your allowed IP addresses here
        private bool IsIpAllowed(IPAddress ipAddress, string[] allowedIpAddresses)
        {
            Console.WriteLine(ipAddress.ToString());    
            foreach (var allowedIp in allowedIpAddresses)
            {
                if (IPAddress.Parse(allowedIp)==(ipAddress))
                {
                    return true;
                }
            }
            return false;
        }

        [HttpGet(Name = "GetItems")]
        public async Task<ActionResult<IEnumerable<Petition>>> GetItems()
        {
            var ipAddress = HttpContext.Connection.RemoteIpAddress;

            // Check if the client IP address is allowed
            if (!IsIpAllowed(ipAddress, allowedIpAddresses))
            {
                return StatusCode((int)HttpStatusCode.Forbidden, "Access denied");
            }
            return await _context.Petition.ToListAsync();
        }

        [Route("CreatePetition")]
        [HttpPost]
        public async Task<IActionResult> PostItem(Petition item)
        {


            if (item == null)
            {
                return BadRequest();
            }

            var latestRepair = _context.Petition.OrderByDescending(r => r.Id).FirstOrDefault();
            int latestId = latestRepair?.Id ?? 0;
            item.Id = latestId + 1;
            item.readFlag = false;

            _context.Petition.Add(item);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetItem), new { item.Id }, item);
        }

        [HttpGet("Get{id}")]
        public async Task<ActionResult<Petition>> GetItem(int id)
        {
            var ipAddress = HttpContext.Connection.RemoteIpAddress;

            // Check if the client IP address is allowed
            if (!IsIpAllowed(ipAddress, allowedIpAddresses))
            {
                return StatusCode((int)HttpStatusCode.Forbidden, "Access denied");
            }

            try
            {
                var item = await _context.Petition.FindAsync(id);

                if (item == null)
                {
                    return NotFound(); // Return 404 if the item is not found
                }

                return item;
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        [HttpPut("UpdateReadFlag/{id}")]
        public async Task<IActionResult> UpdateReadFlag(int id)
        {
            var ipAddress = HttpContext.Connection.RemoteIpAddress;

            // Check if the client IP address is allowed
            
            try
            {
                var petition = await _context.Petition.FindAsync(id);

                if (petition == null)
                {
                    return NotFound(); // Return 404 if ID not found
                }

                petition.readFlag = true; // Set readFlag to true
                await _context.SaveChangesAsync();

                return Ok(); // Return 200 OK if successful
            }
            catch (Exception ex)
            {
                // Handle exceptions as needed
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }


        [HttpGet("latest/{n}")]
        public ActionResult<IEnumerable<Petition>> GetLatestNRows(int n)
        {
            var ipAddress = HttpContext.Connection.RemoteIpAddress;

            // Check if the client IP address is allowed
            if (!IsIpAllowed(ipAddress, allowedIpAddresses))
            {
                return StatusCode((int)HttpStatusCode.Forbidden, "Access denied");
            }
            try
            {
                var latestRows = _context.Petition
                    .OrderByDescending(e => e.Id)
                    .Take(n)
                    .ToList();

                return Ok(latestRows);
            }
            catch (Exception ex)
            {
                // Handle exceptions as needed
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        [HttpGet("unread/{n}")]
        public ActionResult<IEnumerable<Petition>> GetLatestNRowsWithFlag(int n)
        {
            var ipAddress = HttpContext.Connection.RemoteIpAddress;

            // Check if the client IP address is allowed
            if (!IsIpAllowed(ipAddress, allowedIpAddresses))
            {
                return StatusCode((int)HttpStatusCode.Forbidden, "Access denied");
            }
            try
            {
                var latestRows = _context.Petition
                    .Where(p => p.readFlag == false) // Filtering where readFlag is false
                    .OrderByDescending(e => e.Id)
                    .Take(n)
                    .ToList();

                return Ok(latestRows);
            }
            catch (Exception ex)
            {
                // Handle exceptions as needed
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }


        [HttpGet("health")]
        public IActionResult CheckHealth()
        {
            // You can perform additional health checks here if needed.
            // For a simple check, just return an "OK" response.
            return Ok("The service is working");
        }



    }
}