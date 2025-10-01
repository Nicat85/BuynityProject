using FluentValidation;
using OnlineShppingSystem.Application.DTOs.AuthDtos;
using System.Text.RegularExpressions;

namespace OnlineShppingSystem.Application.Validations.AuthValidations
{
    public class EditProfileDtoValidator : AbstractValidator<EditProfileDto>
    {
        public EditProfileDtoValidator()
        {
            
            RuleFor(x => x.FullName)
                .Cascade(CascadeMode.Stop)
                .NotEmpty().When(x => x.FullName is not null).WithMessage("Tam ad boş ola bilməz.")
                .MinimumLength(2).When(x => !string.IsNullOrWhiteSpace(x.FullName)).WithMessage("Tam ad ən azı 2 simvol olmalıdır.")
                .MaximumLength(80).When(x => !string.IsNullOrWhiteSpace(x.FullName)).WithMessage("Tam ad 80 simvoldan çox ola bilməz.");

          
            RuleFor(x => x.UserName)
                .Cascade(CascadeMode.Stop)
                .NotEmpty().When(x => x.UserName is not null).WithMessage("İstifadəçi adı boş ola bilməz.")
                .Matches("^[a-zA-Z0-9._-]{3,32}$")
                    .When(x => !string.IsNullOrWhiteSpace(x.UserName))
                    .WithMessage("İstifadəçi adı 3-32 simvol, yalnız hərf, rəqəm, '.', '_' və '-' ola bilər.");

         
            RuleFor(x => x.Email)
                .Cascade(CascadeMode.Stop)
                .NotEmpty().When(x => x.Email is not null).WithMessage("Email boş ola bilməz.")
                .EmailAddress().When(x => !string.IsNullOrWhiteSpace(x.Email))
                .WithMessage("Email formatı yanlışdır.");

          
            RuleFor(x => x.PhoneNumber)
                .Cascade(CascadeMode.Stop)
                .NotEmpty().When(x => x.PhoneNumber is not null).WithMessage("Telefon nömrəsi boş ola bilməz.")
                .Must(BeValidAzPhone)
                    .When(x => !string.IsNullOrWhiteSpace(x.PhoneNumber))
                    .WithMessage("Telefon formatı yanlışdır. Nümunə: +9945XXXXXXXX və ya 05XXXXXXXX.");

           
            RuleFor(x => x.Bio)
                .MaximumLength(500)
                .When(x => !string.IsNullOrWhiteSpace(x.Bio))
                .WithMessage("Bio 500 simvoldan çox ola bilməz.");

      
            RuleFor(x => x.Address)
                .MaximumLength(200)
                .When(x => !string.IsNullOrWhiteSpace(x.Address))
                .WithMessage("Ünvan 200 simvoldan çox ola bilməz.");

          
            RuleFor(x => x.FinCode)
                .Cascade(CascadeMode.Stop)
              
                .Must(v => IsValidFinCodeOrEmpty(v))
                .WithMessage("FIN kod 7 simvol olmalı və yalnız [A-Z0-9] qəbul edilir.");
        }

        private static bool BeValidAzPhone(string? input)
        {
            if (string.IsNullOrWhiteSpace(input)) return true;

           
            var digits = Regex.Replace(input, "[^0-9+]", "");

            
            return Regex.IsMatch(digits, @"^(\+994\d{9}|0\d{9})$");
        }

        private static bool IsValidFinCodeOrEmpty(string? fin)
        {
            if (string.IsNullOrWhiteSpace(fin)) return true; 

            var cleaned = fin.Trim().ToUpperInvariant();
            
            return Regex.IsMatch(cleaned, "^[A-Z0-9]{7}$");

            
        }
    }
}
