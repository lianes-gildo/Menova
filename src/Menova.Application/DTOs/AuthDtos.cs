using FluentValidation;

namespace Menova.Application.DTOs;

public class LoginDto
{
    public string NomeUtilizador { get; set; } = string.Empty;
    public string PalavraPasse { get; set; } = string.Empty;
    public bool LembrarMe { get; set; } = false;
}

public class LoginDtoValidator : AbstractValidator<LoginDto>
{
    public LoginDtoValidator()
    {
        RuleFor(x => x.NomeUtilizador)
            .NotEmpty().WithMessage("O nome de utilizador é obrigatório.")
            .MaximumLength(50).WithMessage("O nome de utilizador não pode exceder 50 carateres.");

        RuleFor(x => x.PalavraPasse)
            .NotEmpty().WithMessage("A palavra-passe é obrigatória.");
    }
}

public class AlterarPalavraPasseDto
{
    public string PalavraPasseAtual { get; set; } = string.Empty;
    public string NovaPalavraPasse { get; set; } = string.Empty;
    public string ConfirmarPalavraPasse { get; set; } = string.Empty;
}

public class AlterarPalavraPasseDtoValidator : AbstractValidator<AlterarPalavraPasseDto>
{
    public AlterarPalavraPasseDtoValidator()
    {
        RuleFor(x => x.PalavraPasseAtual)
            .NotEmpty().WithMessage("A palavra-passe atual é obrigatória.");

        RuleFor(x => x.NovaPalavraPasse)
            .NotEmpty().WithMessage("A nova palavra-passe é obrigatória.")
            .MinimumLength(10).WithMessage("A nova palavra-passe deve ter pelo menos 10 carateres.")
            .Matches(@"[A-Z]").WithMessage("A nova palavra-passe deve conter pelo menos uma letra maiúscula.")
            .Matches(@"[a-z]").WithMessage("A nova palavra-passe deve conter pelo menos uma letra minúscula.")
            .Matches(@"[0-9]").WithMessage("A nova palavra-passe deve conter pelo menos um dígito numérico.")
            .Matches(@"[\W_]").WithMessage("A nova palavra-passe deve conter pelo menos um caráter especial.");

        RuleFor(x => x.ConfirmarPalavraPasse)
            .NotEmpty().WithMessage("A confirmação da palavra-passe é obrigatória.")
            .Equal(x => x.NovaPalavraPasse).WithMessage("A confirmação não coincide com a nova palavra-passe.");
    }
}

public class UtilizadorResumoDto
{
    public Guid Id { get; set; }
    public string NomeCompleto { get; set; } = string.Empty;
    public string NomeUtilizador { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? Telefone { get; set; }
    public string Foto { get; set; } = "default.jpg";
    public bool Ativo { get; set; }
    public string PerfilNome { get; set; } = string.Empty;
    public string PerfilCodigo { get; set; } = string.Empty;
    public DateTime? UltimoLoginEm { get; set; }
    public bool Bloqueado { get; set; }
}
