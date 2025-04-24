using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;

public class GhanaPhoneNumberAttribute : ValidationAttribute, IClientModelValidator
{
    private int v;

    public GhanaPhoneNumberAttribute(int v)
    {
        this.v = v;
    }

    protected override ValidationResult IsValid(object value, ValidationContext validationContext)
    {
        var phoneNumber = value as string;

        if (string.IsNullOrEmpty(phoneNumber))
        {
            return new ValidationResult("Phone number is required.");
        }

        var regex = new Regex(@"^0[235][0245679]\d{7}$");
        if (!regex.IsMatch(phoneNumber))
        {
            return new ValidationResult("The Phone Number must be a valid Ghanaian phone number.");
        }

        return ValidationResult.Success;
    }

    public void AddValidation(ClientModelValidationContext context)
    {
        MergeAttribute(context.Attributes, "data-val", "true");
        MergeAttribute(context.Attributes, "data-val-ghana-phonenumber", "The Phone Number must be a valid Ghanaian phone number.");
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
