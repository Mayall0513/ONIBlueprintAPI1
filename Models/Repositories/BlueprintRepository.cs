using BlueprintAPI.DbContexts;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace BlueprintAPI.Models.Repositories {
    public static class ABlueprintRepositoryExtensions {
        public static void AddVersion(this ABlueprintRepository blueprintRepository, Blueprint blueprint, BlueprintVersion blueprintVersion) {
            blueprintRepository.AddVersion(blueprint.Id, blueprintVersion);
        }
    }

    public abstract class ABlueprintRepository : Repository<Blueprint> {
        public ABlueprintRepository(BlueprintsDbContext blueprintsContext) : base(blueprintsContext) {  }

        public abstract void AddVersion(Guid id, BlueprintVersion blueprintVersion);
    }

    public class BlueprintRepository : ABlueprintRepository {
        public BlueprintRepository(BlueprintsDbContext blueprintsContext) : base(blueprintsContext) { }




        //DISCRETE
        public override void AddVersion(Guid id, BlueprintVersion blueprintVersion) {
            blueprintVersion.BlueprintId = id;
            BlueprintsContext.BlueprintVersions.Add(blueprintVersion);
        }
    }
}
