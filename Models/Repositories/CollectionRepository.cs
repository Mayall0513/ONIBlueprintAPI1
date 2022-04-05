using BlueprintAPI.DbContexts;

namespace BlueprintAPI.Models.Repositories {
    public abstract class ACollectionRepository : Repository<Collection> { //ONLY TO MATCH PATTERN, UNUSED - FOR NOW.
        public ACollectionRepository(BlueprintsDbContext blueprintsContext) : base(blueprintsContext) { }
    }

    public class CollectionRepository : ACollectionRepository {
        public CollectionRepository(BlueprintsDbContext blueprintsContext) : base(blueprintsContext) {  }
    }
}
