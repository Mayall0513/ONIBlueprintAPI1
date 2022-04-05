using BlueprintAPI.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using System.Collections.Generic;

namespace BlueprintAPI.Policies {
    public class HandleValidationExceptionAttribute : ActionFilterAttribute {
        public class FieldErrors {
            public string FieldName { get; set; }
            public List<string> Errors { get; } = new List<string>();

            public FieldErrors(string fieldName) {
                FieldName = fieldName;
            }
        }

        public override void OnResultExecuting(ResultExecutingContext context) {
            if (!context.ModelState.IsValid) {
                Dictionary<string, List<string>> errors = new Dictionary<string, List<string>>();
                ModelStateDictionary.ValueEnumerator valueEnumerator = context.ModelState.Values.GetEnumerator();
               
                foreach (string key in context.ModelState.Keys) {
                    valueEnumerator.MoveNext();
                    errors.Add(key, new List<string>());

                    foreach (ModelError error in valueEnumerator.Current.Errors) {
                        errors[key].Add(error.ErrorMessage);
                    }
                }

                context.Result = new JsonResult(new GenericResponseModel("Model(s) failed validation!", errors)) {
                    StatusCode = 400
                };
            }

        }
    }
}
