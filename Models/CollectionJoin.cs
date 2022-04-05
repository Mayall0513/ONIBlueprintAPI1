using System;

namespace BlueprintAPI.Models {
    public sealed class CollectionJoin {
        public Guid Id { get; set; }

        public Guid CollectionId { get; set; }
        public Collection Collection { get; set; }

        public Guid BlueprintId { get; set; }
        public Blueprint Blueprint { get; set; }

        public Guid AuthorId { get; set; }
        public User Author { get; set; }
    }
}
