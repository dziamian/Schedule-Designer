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
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false)]
    sealed public class GreaterThan : ValidationAttribute
    {
        private readonly string _otherPropertyName;
        
        public GreaterThan(string otherPropertyName)
        {
            _otherPropertyName = otherPropertyName;
        }

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

    public class CustomEnableQueryAttribute : EnableQueryAttribute
    {
        private readonly DefaultQuerySettings defaultQuerySettings;
        public CustomEnableQueryAttribute()
        {
            this.defaultQuerySettings = new DefaultQuerySettings
            {
                EnableExpand = true,
                EnableSelect = true
            };
        }
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
