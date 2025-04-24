using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;

public class GhanaGPSAddressAttribute : ValidationAttribute, IClientModelValidator
{
    protected override ValidationResult IsValid(object value, ValidationContext validationContext)
    {
        var gpsAddress = value as string;

        if (string.IsNullOrEmpty(gpsAddress))
        {
            return new ValidationResult("GPS Address is required.");
        }

        var regex = new Regex(@"^[A-Z]{2}-\d{3}-\d{4}$");
        if (!regex.IsMatch(gpsAddress))
        {
            return new ValidationResult("The GPS Address must be in the format XX-XXX-XXXX.");
        }

        return ValidationResult.Success;
    }

    public void AddValidation(ClientModelValidationContext context)
    {
        MergeAttribute(context.Attributes, "data-val", "true");
        MergeAttribute(context.Attributes, "data-val-ghana-gpsaddress", "The GPS Address must be in the format XX-XXX-XXXX.");
    }

    private bool MergeAttribute(IDictionary<string, string> attributes, string key, string value)
    {
        if (attributes.ContainsKey(key))
        {
            return false;
        }

        attributes.Add(key, value);
        return true;
    }
}
