using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System.Threading.Tasks;

namespace BlueprintAPI.Models {
    public class GenericResponseMessage {
        public string Message { get; set; }

        public GenericResponseMessage(string message) {
            Message = message;
        }
    }

    public class GenericResponseModel {
        public string Message { get; set; }
        public object Data { get; set; }

        public GenericResponseModel(string message, object data) {
            Message = message;
            Data = data;
        }
    }

    public static class GenericResponseExtensions {
        public static async Task WriteGenericResponseAsync(this HttpResponse httpResponse, int statusCode, string message) {
            await WriteGenericResponseAsync(httpResponse, statusCode, new GenericResponseMessage(message));
            return;
        }

        public static async Task WriteGenericResponseAsync(this HttpResponse httpResponse, int statusCode, GenericResponseMessage responseMessage)  {
            httpResponse.ContentType = "application/json";
            httpResponse.StatusCode = statusCode;
            await httpResponse.WriteAsync(JsonConvert.SerializeObject(responseMessage));

            return;
        }

        public static async Task WriteGenericResponseAsync(this HttpResponse httpResponse, int statusCode, string message, object data = null) {
            await WriteGenericResponseAsync(httpResponse, statusCode, new GenericResponseModel(message, data));
            return;
        }

        public static async Task WriteGenericResponseAsync(this HttpResponse httpResponse, int statusCode, GenericResponseModel responseModel) {
            httpResponse.ContentType = "application/json";
            httpResponse.StatusCode = statusCode;
            await httpResponse.WriteAsync(JsonConvert.SerializeObject(responseModel));

            return;
        }
    }
}
