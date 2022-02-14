using Microsoft.AspNet.OData;
using Microsoft.AspNet.OData.Query;
using Microsoft.AspNet.OData.Query.Validators;
using Microsoft.AspNetCore.Http;
using Microsoft.OData;
using ScheduleDesigner.Authentication;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace ScheduleDesigner.Attributes
{
    /// <summary>
    /// Klasa atrybutu pozwalająca na porównywanie wartości dwóch właściwości obiektu.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false)]
    sealed public class GreaterThan : ValidationAttribute
    {
        /// <summary>
        /// Nazwa właściwości obiektu, do której należy porównać wartości.
        /// </summary>
        private readonly string _otherPropertyName;

        /// <summary>
        /// Konstruktor atrybutu.
        /// </summary>
        /// <param name="otherPropertyName">Nazwa właściwości obiektu, do której należy porównać wartości</param>
        public GreaterThan(string otherPropertyName)
        {
            _otherPropertyName = otherPropertyName;
        }

        /// <summary>
        /// Funkcja sprawdzająca poprawność wartości właściwości obiektu.
        /// </summary>
        /// <param name="value">Wartość do zweryfikowania</param>
        /// <param name="validationContext">Kontekst operacji weryfikującej</param>
        /// <returns>Rezultat weryfikacji</returns>
        protected override ValidationResult IsValid(object value, ValidationContext validationContext)
        {
            var otherProperty = validationContext.ObjectType.GetProperty(_otherPropertyName);

            if (otherProperty == null)
            {
                return new ValidationResult(
                    string.Format("Unknown property name: {0}", _otherPropertyName)
                );
            }

            var otherPropertyValue = (TimeSpan)otherProperty.GetValue(validationContext.ObjectInstance, null);
            var propertyValue = (TimeSpan)value;

            if (propertyValue <= otherPropertyValue)
            {
                return new ValidationResult(
                    string.Format("Value need to be greater than '{0}'.", _otherPropertyName)
                );
            }

            return null;
        }
    }

    /// <summary>
    /// Klasa atrybutu zabezpieczająca dostęp do tokenów dostępu i ich sekretów.
    /// </summary>
    public class CustomEnableQueryAttribute : EnableQueryAttribute
    {
        /// <summary>
        /// Metoda weryfikująca poprawność zapytania OData.
        /// </summary>
        /// <param name="request">Reprezentacja żądania HTTP</param>
        /// <param name="queryOpts">Wykorzystane opcje w zapytaniu OData</param>
        /// <exception cref="InvalidOperationException">Zapytanie OData próbuje uzyskać dostęp do zabezpieczonego zasobu</exception>
        public override void ValidateQuery(HttpRequest request, ODataQueryOptions queryOpts)
        {
            if (queryOpts.SelectExpand != null
                && queryOpts.SelectExpand.RawExpand != null
                && queryOpts.SelectExpand.RawExpand.Contains("Authorization"))
            {
                throw new InvalidOperationException();
            }

            base.ValidateQuery(request, queryOpts);
        }
    }
}
