using System.ComponentModel.DataAnnotations;

namespace WebApplication1.DTOs;

public class ClientCreateDTO
{
    [Required]
    [StringLength(120)]
    public String FirstName { get; set; }
    
    [Required]
    [StringLength(120)]
    public String LastName { get; set; }
    
    [Required]
    [EmailAddress]
    public String Email { get; set; }
    
    [Required]
    [Phone]
    public String Telephone { get; set; }
    
    [Required]
    [RegularExpression(@"^\d{11}$", ErrorMessage = "PESEL musi mieć 11 cyfr.")]
    public String Pesel { get; set; }
}