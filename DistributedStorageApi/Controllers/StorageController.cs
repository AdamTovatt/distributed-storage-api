using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Sakur.WebApiUtilities.Models;
using StorageCoordinator;
using StorageCoordinator.Models;
using System.Net;
using System.Security.AccessControl;

namespace DistributedStorageApi.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class StorageController : ControllerBase
    {
        [HttpPost("store")]
        [ProducesResponseType(typeof(string), (int)HttpStatusCode.Created)]
        public async Task<ObjectResult> StoreFile(IFormFile file)
        {
            StoreDataResult storeDataResult = await DistributedStorage.Instance.StoreDataAsync(file.FileName, file.OpenReadStream());

            return new ApiResponse(storeDataResult, HttpStatusCode.Created);
        }
    }
}
