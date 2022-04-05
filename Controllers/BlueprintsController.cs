using System;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using BlueprintAPI.Middlewares;
using BlueprintAPI.Models;
using BlueprintAPI.Models.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BlueprintAPI.Controllers {
    [ApiController]
    [Route("api/blueprint")]
    public class BlueprintsController : Controller {
        private readonly ABlueprintRepository blueprintRepository;
        private readonly AUserRepository userRepository;
        private readonly ACollectionRepository collectionRepository;
        
        public BlueprintsController(ABlueprintRepository blueprintRepository, AUserRepository userRepository, ACollectionRepository collectionRepository) {
            this.blueprintRepository = blueprintRepository;
            this.userRepository = userRepository;
            this.collectionRepository = collectionRepository;
        }




        public class BlueprintGetNoVersionResponseBody {
            public string[] Versions { get; set; }
        }

        public class BlueprintGetResponseBody {
            public string Version { get; set; }
            public object Blueprint { get; set; }
        }

        [HttpGet("{id}", Name = "BlueprintGet")]
        public IActionResult Get([FromRoute] Guid id, string format, string version) {
            Blueprint blueprint = blueprintRepository.BlueprintsContext.Blueprints.Include(x => x.Versions).FirstOrDefault(x => x.Id == id);
            BlueprintVersion blueprintVersion;

            if (blueprint == null) {
                return NotFound(new GenericResponseMessage($"No blueprint with id {id} exists!"));
            }
            
            if (version == null) {
                blueprintVersion = blueprint.Versions.Last();
            }

            else {
                version = version.ToLower();

                blueprintVersion = blueprint.Versions.FirstOrDefault(x => x.Version == version);
                if (blueprintVersion == null) {
                    string[] versions = new string[blueprint.Versions.Count];
                    for (int i = 0; i < versions.Length; ++i) {
                        versions[i] = blueprint.Versions[i].Version;
                    }

                    return NotFound(new GenericResponseModel($"No blueprint version with id {id} and version {version} exists!", new { versions }));
                }
            }
            
            if (format != null && format.ToLower() == "binary") {
                using MemoryStream memoryStream = new MemoryStream();
                using BinaryWriter binaryWriter = new BinaryWriter(memoryStream);

                binaryWriter.Write(blueprint.FriendlyName);
                binaryWriter.Write(blueprintVersion.BuildingConfigurationBytes);
                binaryWriter.Write(blueprintVersion.DigLocationsBytes);

                BlueprintGetResponseBody responseBody = new BlueprintGetResponseBody() {
                    Version = blueprintVersion.Version,
                    Blueprint = System.Text.Encoding.UTF8.GetString(memoryStream.ToArray())
                };

                return Ok(new GenericResponseModel("Selected 1 blueprint.", responseBody));

            }

            else {
                BlueprintGetResponseBody responseBody = new BlueprintGetResponseBody() {
                    Version = blueprintVersion.Version,
                    Blueprint = new { blueprint.FriendlyName, blueprintVersion.BuildingConfiguration, blueprintVersion.DigLocations }
                };

                return Ok(new GenericResponseModel("Selected 1 blueprint.", responseBody));
            }
        }




        public class BlueprintPaginationRequest {

        }

        [HttpGet("/pagination", Name = "BlueprintPagination")]
        public IActionResult Pagination(int start, int count, [FromBody] BlueprintPaginationRequest paginationRequest) {
            return NotFound(); //NOT YET IMPLEMENTED
        }




        public class BlueprintPostRequest {
            [Required]
            [MaxLength(256, ErrorMessage = "Friendly name is too long, maximum length of 256 characters.")]
            public string FriendlyName { get; set; }

            public string Version { get; set; } = "1.0.0";

            public BuildingConfig[] Buildings { get; set; } = new BuildingConfig[0];
            public Vector2I[] DigLocations { get; set; } = new Vector2I[0];

            public Guid? CollectionId { get; set; }
        }

        [HttpPost(Name = "BlueprintPost")]
        [Authorize]
        public async ValueTask<IActionResult> Post([FromBody] BlueprintPostRequest postRequest) {
            User user = HttpContext.GetUser();

            if (postRequest.Buildings.Length == 0 && postRequest.DigLocations.Length == 0) {
                return BadRequest(new GenericResponseModel("Cannot upload empty blueprint!", new { error = "The blueprint lacks buildings and dig commands!" }));
            }

            Blueprint blueprint = new Blueprint() {
                FriendlyName = postRequest.FriendlyName,
                AuthorId = user.Id
            };

            BlueprintVersion blueprintVersion = new BlueprintVersion() {
                Version = postRequest.Version.ToLower(),
                Changes = "Uploaded",
                BuildingConfiguration = postRequest.Buildings,
                DigLocations = postRequest.DigLocations
            };

            blueprintRepository.Create(blueprint);
            blueprintRepository.AddVersion(blueprint, blueprintVersion);
            await blueprintRepository.SaveAsync();

            return Created(Url.Link("BlueprintGet", new { id = blueprint.Id }), new { blueprint.Id });
        }




        [HttpPatch("{id}", Name = "BlueprintPatch")]
        [Authorize]
        public IActionResult Patch(Guid id, [FromBody] string value) {
            return NotFound(); //NOT YET IMPLEMENTED
        }
        



        [HttpDelete("{id}", Name = "BlueprintDelete")]
        [Authorize]
        public async ValueTask<IActionResult> Delete(Guid id) {
            User user = HttpContext.GetUser();
            Blueprint blueprint = await blueprintRepository.GetIDAsync(id);

            if (blueprint == null) {
                return NotFound(new GenericResponseMessage($"No blueprint with id {id} exists!"));
            }

            if (user.AccountType == AccountType.Administrator || user.Id == blueprint.AuthorId) {
                blueprintRepository.Delete(blueprint);
                await blueprintRepository.SaveAsync();

                return Ok(new GenericResponseMessage($"Blueprint with id {id} deleted!"));
            }

            else {
                return Unauthorized(new GenericResponseMessage("You may only delete your own blueprints!"));
            }
        }
    }
}
