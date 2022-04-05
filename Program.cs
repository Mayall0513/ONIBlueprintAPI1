using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;

namespace BlueprintAPI {
    public class Program {
        public static void Main(string[] arguments) {
            Host.CreateDefaultBuilder(arguments).ConfigureWebHostDefaults(webBuilder => webBuilder.UseStartup<Startup>()).Build().Run();
        }
    }
}
