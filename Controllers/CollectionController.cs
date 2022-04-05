using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using BlueprintAPI.Middlewares;
using BlueprintAPI.Models;
using BlueprintAPI.Models.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BlueprintAPI.Controllers {
    [ApiController]
    [Route("api/collection")]
    public class CollectionController : Controller {
        private readonly ACollectionRepository collectionRepository;

        public CollectionController(ACollectionRepository collectionRepository) {
            this.collectionRepository = collectionRepository;
        }


        

        [HttpGet("{id}", Name = "CollectionGet")]
        public async ValueTask<IActionResult> Get([FromRoute] Guid id) {
            Collection collection = await collectionRepository.GetIDAsync(id);

            if (collection == null) {
                return NotFound(new GenericResponseMessage($"No collection with id {id} exists!"));
            }

            return Ok(new GenericResponseModel("Selected 1 collection.", new { blueprints = collection.Blueprints.Select(x => x.BlueprintId) }));
        }




        public class CollectionPaginationRequest {

        }

        [HttpGet("/pagination", Name = "CollectionPagination")]
        public IActionResult Pagination(int start, int count, [FromBody] CollectionPaginationRequest paginationRequest) {
            return NotFound(); //NOT YET IMPLEMENTED
        }




        public class CollectionPostRequest {
            [Required]
            public string Name { get; set; }
            
            [Required]
            public string Description { get; set; }
        }

        [HttpPost(Name = "CollectionPost")]
        [Authorize]
        public async ValueTask<IActionResult> Post([FromBody] CollectionPostRequest postRequest) {
            User user = HttpContext.GetUser();

            Collection collection = new Collection() {
                Name = postRequest.Name,
                Description = postRequest.Description,
                AuthorId = user.Id
            };

            collectionRepository.Create(collection);
            await collectionRepository.SaveAsync();

            return Created(Url.Link("CollectionGet", new { id = collection.Id }), new { collection.Id });
        }




        [HttpPatch("{id}", Name = "CollectionPatch")]
        [Authorize]
        public IActionResult Patch(Guid id, [FromBody] string value) {
            return NotFound(); //NOT YET IMPLEMENTED
        }




        [HttpDelete("{id}", Name = "CollectionDelete")]
        [Authorize]
        public async ValueTask<IActionResult> Delete(Guid id) {
            User user = HttpContext.GetUser();
            Collection toDelete = await collectionRepository.GetIDAsync(id);

            if (toDelete == null) {
                return NotFound(new GenericResponseMessage($"No collection with id {id} exists!"));
            }

            if (user.AccountType == AccountType.Administrator || user.Id == toDelete.AuthorId) {
                collectionRepository.Delete(toDelete);
                await collectionRepository.SaveAsync();

                return Ok(new GenericResponseModel($"Collection with id {id} deleted!", new { deletedat = DateTime.UtcNow }));
            }

            else {
                return Unauthorized(new GenericResponseMessage("You may only delete your own collections!"));
            }
        }
    }
}
