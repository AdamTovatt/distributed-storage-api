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

        [HttpGet("retrieve")]
        [ProducesResponseType(typeof(FileStreamResult), (int)HttpStatusCode.OK)]
        public async Task RetrieveFile(string fileName)
        {
            #pragma warning disable ASP0019 // Suggest using IHeaderDictionary.Append or the indexer
            Response.Headers.Add("Content-Type", "byte/event-stream");
            Response.Headers.Add("Cache-Control", "no-cache");
            Response.Headers.Add("Connection", "keep-alive");
            #pragma warning restore ASP0019 // Suggest using IHeaderDictionary.Append or the indexer

            RetrieveDataResult result = await DistributedStorage.Instance.RetrieveDataAsync(fileName, Response.Body, Response.HttpContext.RequestAborted);
        }
    }
}
