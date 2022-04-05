using BlueprintAPI.Models;
using BlueprintAPI.Models.Repositories;
using Microsoft.AspNetCore.Http;
using System.Net;
using System.Threading.Tasks;

namespace BlueprintAPI.Middlewares {
    public sealed class CheckAccountMiddleware {
        public const string UserItem = "user";

        private readonly RequestDelegate nextDelegate;

        public CheckAccountMiddleware(RequestDelegate nextDelegate) {
            this.nextDelegate = nextDelegate;
        }

        public async Task Invoke(HttpContext httpContext, AUserRepository userRepository) {
            if (httpContext.User.Identity.IsAuthenticated) {
                User user = await userRepository.GetUserAsync(httpContext.User);

                if (user == null) {
                    await httpContext.Response.WriteGenericResponseAsync((int) HttpStatusCode.InternalServerError, "Error when getting account used to authenticate!");
                    return;
                }

                if (user.IsBanned()) {
                    await httpContext.Response.WriteGenericResponseAsync((int) HttpStatusCode.Unauthorized, "Account is banned!");
                    return;
                }

                if (user.IsUnverified()) {
                    await httpContext.Response.WriteGenericResponseAsync((int) HttpStatusCode.Unauthorized, "Account requires email verification!");
                    return;
                }

                httpContext.Items[UserItem] = user;
            }

            await nextDelegate(httpContext);
        }
    }

    public static class CheckAccountMiddlewareExtensions {
        public static User GetUser(this HttpContext httpContext) {
            return httpContext.Items[CheckAccountMiddleware.UserItem] as User;
        }
    }
}
