using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.IO;
using System.Linq;

namespace BlueprintAPI.Models {
    public sealed class Vector2I {
        public int X { get; set; } = 0;
        public int Y { get; set; } = 0;

        public Vector2I() { }

        public Vector2I(int x, int y) {
            X = x;
            Y = y;
        }
    }

    public sealed class Blueprint {
        public Guid Id { get; set; }

        public Guid AuthorId { get; set; }
        public User Author { get; set; }

        public string FriendlyName { get; set; }

        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public DateTime Created { get; set; } = DateTime.UtcNow;

        public List<CollectionJoin> Collections { get; set; } = new List<CollectionJoin>();

        public List<BlueprintVersion> Versions { get; set; } = new List<BlueprintVersion>();
    }

    public class BlueprintVersion {
        private BuildingConfig[] _buildingConfiguration = new BuildingConfig[0];
        private byte[] _buildingConfigurationBytes;
        private Vector2I[] _digLocations = new Vector2I[0];
        private byte[] _digLocationsBytes;

        public Guid Id { get; set; }

        public Guid BlueprintId { get; set; }
        public virtual Blueprint Blueprint { get; set; }

        public string Version { get; set; }

        public string Changes { get; set; }

        [NotMapped]
        public BuildingConfig[] BuildingConfiguration {
            get {
                return _buildingConfiguration;
            }

            set {
                _buildingConfiguration = value;

                if (value != null && value.Length > 0) {
                    using MemoryStream memoryStream = new MemoryStream();
                    using BinaryWriter binaryWriter = new BinaryWriter(memoryStream);

                    binaryWriter.Write(value.Length);
                    foreach (BuildingConfig buildingConfig in value) {
                        binaryWriter.Write(buildingConfig.Offset.X);
                        binaryWriter.Write(buildingConfig.Offset.Y);
                        binaryWriter.Write(buildingConfig.BuildingDef);
                        binaryWriter.Write(buildingConfig.Selected_Elements.Length);

                        foreach (int selectedElement in buildingConfig.Selected_Elements) {
                            binaryWriter.Write(selectedElement);
                        }

                        binaryWriter.Write(buildingConfig.Orientation);
                        binaryWriter.Write(buildingConfig.Flags);
                    }

                    _buildingConfigurationBytes = memoryStream.ToArray();
                }
            }
        }

        public byte[] BuildingConfigurationBytes {
            get {
                return _buildingConfigurationBytes;
            }

            set {
                _buildingConfigurationBytes = value;

                if (value != null && value.Length > sizeof(int)) {
                    using MemoryStream memoryStream = new MemoryStream(BuildingConfigurationBytes);
                    using BinaryReader binaryReader = new BinaryReader(memoryStream);

                    int buildingCount = binaryReader.ReadInt32();
                    _buildingConfiguration = new BuildingConfig[buildingCount];

                    for (int i = 0; i < buildingCount; ++i) {
                        BuildingConfig buildingConfig = new BuildingConfig {
                            Offset = new Vector2I(binaryReader.ReadInt32(), binaryReader.ReadInt32()),
                            BuildingDef = binaryReader.ReadString()
                        };

                        int elementCount = binaryReader.ReadInt32();
                        buildingConfig.Selected_Elements = new int[elementCount];

                        for (int j = 0; j < elementCount; ++j) {
                            buildingConfig.Selected_Elements[j] = binaryReader.ReadInt32();
                        }

                        buildingConfig.Orientation = binaryReader.ReadInt32();
                        buildingConfig.Flags = binaryReader.ReadInt32();

                        _buildingConfiguration[i] = buildingConfig;
                    }
                }
            }
        }

        [NotMapped]
        public Vector2I[] DigLocations {
            get {
                return _digLocations;
            }

            set {
                _digLocations = value;

                if (value != null) {
                    using MemoryStream memoryStream = new MemoryStream();
                    using BinaryWriter binaryWriter = new BinaryWriter(memoryStream);

                    binaryWriter.Write(value.Length);
                    foreach (Vector2I digLocation in value) {
                        binaryWriter.Write(digLocation.X);
                        binaryWriter.Write(digLocation.Y);
                    }

                    _digLocationsBytes = memoryStream.ToArray();
                }
            }
        }

        public byte[] DigLocationsBytes { 
            get {
                return _digLocationsBytes;
            }
            
            set {
                _digLocationsBytes = value;

                if (value != null && value.Count() > sizeof(int)) {
                    using MemoryStream memoryStream = new MemoryStream(DigLocationsBytes);
                    using BinaryReader binaryReader = new BinaryReader(memoryStream);

                    int digLocationCount = binaryReader.ReadInt32();

                    _digLocations = new Vector2I[digLocationCount];
                    for (int i = 0; i < digLocationCount; ++i) {
                        _digLocations[i] = new Vector2I(binaryReader.ReadInt32(), binaryReader.ReadInt32());
                    }
                }
            }
        }

        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public DateTime Created { get; set; } = DateTime.UtcNow;
    }

    public class BuildingConfig {
        public Vector2I Offset { get; set; } = new Vector2I(0, 0);

        public string BuildingDef { get; set; }

        public int[] Selected_Elements { get; set; }

        public int Orientation { get; set; } = 0;
        public int Flags { get; set; } = 0;
    }
}
