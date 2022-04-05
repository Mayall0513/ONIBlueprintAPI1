using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace BlueprintAPI.Models {
    public sealed class User {
        private static readonly PasswordHasher<User> passwordHasher = new PasswordHasher<User>();

        public Guid Id { get; set; }

        public string Username { get; set; }
        public string PasswordHash { get; set; }
        public string EmailAddress { get; set; }

        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public DateTime Created { get; set; } = DateTime.UtcNow;

        public AccountType AccountType { get; set; }

        public Guid? EmailVerificationToken { get; set; }
        public DateTime? EmailVerificationTokenDate { get; set; }
        public DateTime? EmailVerificationTokenExpiration { get; set; }

        public List<Blueprint> Blueprints { get; set; } = new List<Blueprint>();
        public List<Collection> Collections { get; set; } = new List<Collection>();
        public List<CollectionJoin> CollectionJoins { get; set; } = new List<CollectionJoin>();

        public void HashPassword(string password) {
            PasswordHash = passwordHasher.HashPassword(this, password);
        }

        public bool VerifyPassword(string password) {
            PasswordVerificationResult hashResult = passwordHasher.VerifyHashedPassword(this, PasswordHash, password);
            if (hashResult == PasswordVerificationResult.SuccessRehashNeeded) {
                PasswordHash = passwordHasher.HashPassword(this, password);
                return true;
            }

            return hashResult == PasswordVerificationResult.Success;
        }

        public bool IsBanned() {
            return AccountType == AccountType.Banned;
        }

        public bool IsUnverified() {
            return AccountType == AccountType.Unverified;
        }
    }

    public enum AccountType : byte {
        Banned,
        Unverified,
        User,
        Administrator
    }
}
