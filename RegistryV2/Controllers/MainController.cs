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

        private string[] allowedIpAddresses = { "10.0.0.134", "127.0.0.1", "10.111.111.117" }; // Add your allowed IP addresses here
        private static bool IsIpAllowed(IPAddress ipAddress, string[] allowedIpAddresses)
        {
            Console.WriteLine(ipAddress.ToString());

            // Check if it's an IPv6 formatted IPv4 address, and if so, convert it to IPv4
            if (ipAddress.AddressFamily == System.Net.Sockets.AddressFamily.InterNetworkV6 &&
                ipAddress.IsIPv4MappedToIPv6)
            {
                ipAddress = ipAddress.MapToIPv4();
            }

            foreach (var allowedIp in allowedIpAddresses)
            {
                // Convert each allowed IP to IPAddress format for comparison
                IPAddress allowedIpAddress = IPAddress.Parse(allowedIp);

                // Check if the IP addresses are equal
                if (IPAddress.Equals(allowedIpAddress, ipAddress))
                {
                    return true;
                }
            }
            return false;
        }

        public class PetitionWithFile : Petition
        {
            public IFormFile? File { get; set; }
        }



        [Route("GetItems")]
        [HttpGet]
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
        public async Task<IActionResult> PostItem([FromForm] PetitionWithFile petitionWithFile)
        {
            if (petitionWithFile == null)
            {
                return BadRequest();
            }

            var latestPetition = _context.Petition.OrderByDescending(r => r.Id).FirstOrDefault();
            int latestId = latestPetition?.Id ?? 0;

            string filePath = null;
            string fileName = null;

            if (petitionWithFile.File != null && petitionWithFile.File.Length > 5 * 1024 * 1024) // 5 MB in bytes
            {
                return StatusCode(505);
            }

            if (petitionWithFile.File != null && petitionWithFile.File.Length > 0)
            {
                // Generate a unique file name
                fileName = Guid.NewGuid().ToString() + Path.GetExtension(petitionWithFile.File.FileName);
                var uploads = "uploads";

                // Create the uploads directory if it doesn't exist
                if (!Directory.Exists(uploads))
                {
                    Directory.CreateDirectory(uploads);
                }

                filePath = Path.Combine(uploads, fileName);

                // Copy the file to the uploads directory
                using (var fileStream = new FileStream(filePath, FileMode.Create))
                {
                    await petitionWithFile.File.CopyToAsync(fileStream);
                }

            }

            var item = new Petition
            {
                Id = latestId + 1,
                Name = petitionWithFile.Name,
                PetitionText = petitionWithFile.PetitionText,
                Email = petitionWithFile.Email,
                readFlag = false,
                FilePath = fileName // Store the file path instead of IFormFile
            };

            _context.Petition.Add(item);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetItem), new { item.Id }, item);
        }

        [Route("Get{id}")]
        [HttpGet]
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

        [Route("UpdateReadFlag/{id}")]
        [HttpPut]
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


        [Route("latest/{n}")]
        [HttpGet]
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



        [Route("unread/{n}")]
        [HttpGet]
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

        [Route("files/")]
        [HttpGet]
        public IActionResult GetFile(string filePath)
        {
            // Check if the file exists
            filePath = Path.Combine("uploads", filePath);
            filePath = Path.GetFullPath(filePath);


            if (!System.IO.File.Exists(filePath))
            {
                return NotFound("File not found");
            }

            // Return the file with its original name and extension
            return PhysicalFile(filePath, "application/octet-stream"); // application/octet-stream denotes a generic binary file
        }


        [Route("page/{pageNumber}")]
        [HttpGet]
        public ActionResult<IEnumerable<Petition>> GetNextRows(int pageNumber)
        {
            var ipAddress = HttpContext.Connection.RemoteIpAddress;

            // Check if the client IP address is allowed
            if (!IsIpAllowed(ipAddress, allowedIpAddresses))
            {
                return StatusCode((int)HttpStatusCode.Forbidden, "Access denied");
            }

            try
            {
                // Calculate skip count
                int skipCount = (pageNumber - 1) * 10;

                var nextRows = _context.Petition
                    .OrderBy(e => e.Id)
                    .Skip(skipCount)
                    .Take(10)
                    .ToList();

                return Ok(nextRows);
            }
            catch (Exception ex)
            {
                // Handle exceptions as needed
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        [Route("health")]
        [HttpGet]
        public IActionResult CheckHealth()
        {
            // You can perform additional health checks here if needed.
            // For a simple check, just return an "OK" response.
            return Ok("The service is working");
        }

    }
}