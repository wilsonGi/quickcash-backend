//using Microsoft.AspNetCore.Identity;
//using QuickCashJobAPI.Models;
//using System.ComponentModel.DataAnnotations;
//using System.Text.RegularExpressions;

//public class GhanaNationalIdAttribute : ValidationAttribute
//{
//    protected override ValidationResult IsValid(object value, ValidationContext validationContext)
//    {
//        var nationalIdNo = value as string;

//        if (string.IsNullOrEmpty(nationalIdNo))
//        {
//            return new ValidationResult("The National ID Number field is required.");
//        }

//        var regex = new Regex(@"^GHA-\d{12}$");
//        if (!regex.IsMatch(nationalIdNo))
//        {
//            return new ValidationResult("The National ID Number must be in the format GHA-XXXXXXXXXXXX where X is a digit.");
//        }

//        // Check if the National ID Number is already in use
//        var userManager = validationContext.GetService(typeof(UserManager<ApplicationUser>)) as UserManager<ApplicationUser>;
//        var user = userManager.Users.FirstOrDefault(u => u.NationalIdNo == nationalIdNo);

//        if (user != null)
//        {
//            return new ValidationResult("The National ID Number is already in use.");
//        }

//        return ValidationResult.Success;
//    }
//}
