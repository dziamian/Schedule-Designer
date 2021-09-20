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
}
