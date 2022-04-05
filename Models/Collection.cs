using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace BlueprintAPI.Models {
    public sealed class Collection {
        public Guid Id { get; set; }

        public string Name { get; set; }

        public string Description { get; set; }

        public Guid AuthorId { get; set; }
        public User Author { get; set; }

        public List<CollectionJoin> Blueprints { get; set; } = new List<CollectionJoin>();

        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public DateTime Created { get; set; } = DateTime.UtcNow;
    }
}
