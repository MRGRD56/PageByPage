using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PageByPage.Models
{
    /// <summary>
    /// Object validity condition.
    /// </summary>
    /// <returns>Is the object valid.</returns>
    public delegate bool ValidationPredicate();

    /// <summary>
    /// The action to be taken after the object has been validated.
    /// </summary>
    /// <param name="isValid">Is the object valid.</param>
    public delegate void ValidationCallback(bool isValid);

    public static class Validation
    {
        public const string NoError = "";

        /// <summary>
        /// Returns the first error encountered during validation, or returns an empty error if no errors occurred. Returns an empty error immediately if <paramref name="commonPredicate"/> meets the condition.
        /// </summary>
        /// <param name="commonPredicate"></param>
        /// <param name="validationConditions"></param>
        /// <param name="callback">Action to be taken after all the validations are complete.</param>
        /// <returns></returns>
        public static string Validate(ValidationPredicate commonPredicate, IEnumerable<ValidationCondition> validationConditions, ValidationCallback callback = null)
        {
            var result = NoError;
            if (commonPredicate?.Invoke() != true)
            {
                foreach (var validation in validationConditions)
                {
                    var isValid = validation.Predicate();
                    validation.Callback?.Invoke(isValid);

                    if (!isValid)
                    {
                        result = validation.ErrorText;
                        break;
                    }
                }
            }

            callback?.Invoke(result == NoError);
            return result;
        }

        public static bool IsValid(this IDataErrorInfo dataErrorInfo, string columnName) =>
            dataErrorInfo[columnName] == NoError;
    }
}
