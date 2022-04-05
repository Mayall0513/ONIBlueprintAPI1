using BlueprintAPI.DbContexts;
using System;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace BlueprintAPI.Models.Repositories {
    public sealed class LoginResponse {
        public bool Successful { get; set; } = false;
        public User User { get; set; } = null;
    }

    public abstract class AUserRepository : Repository<User> {
        public AUserRepository(BlueprintsDbContext blueprintsContext) : base(blueprintsContext) {  }

        public abstract void RegisterAsync(User user);
        public abstract ValueTask<LoginResponse> LoginAsync(string credential, string password);

        public abstract ValueTask<User> GetUserAsync(ClaimsPrincipal user);
    }

    public class UserRepository : AUserRepository {
        public UserRepository(BlueprintsDbContext blueprintsContext) : base(blueprintsContext) {  }




        //CONVENIENCE METHODS
        public override async void RegisterAsync(User user) {
            if (user == null) {
                return;
            }

            Create(user);
            await SaveAsync(default);
        }

        public override async ValueTask<LoginResponse> LoginAsync(string credential, string password) {
            if (credential == null || password == null) {
                throw new ArgumentNullException();
            }

            User userFound = null;

            if (new EmailAddressAttribute().IsValid(credential)) {
                userFound = await GetAsync(x => x.EmailAddress == credential, default);
            }

            else {
                userFound = await GetAsync(x => x.Username == credential, default);
            }

            if (userFound != null && userFound.VerifyPassword(password)) {
                return new LoginResponse() {
                    Successful = true,
                    User = userFound
                };
            }
            
            return new LoginResponse();
        }

        public override async ValueTask<User> GetUserAsync(ClaimsPrincipal user) {
            if (user.HasClaim(x => x.Type.ToLower() == "id")) {
                Guid id = new Guid(user.Claims.First(x => x.Type.ToLower() == "id").Value);
                return await GetIDAsync(id, default);
            }

            return null;
        }
    }
}
