using System.ComponentModel.DataAnnotations;
using AcxiomCRM.Domain.Enums;

namespace AcxiomCRM.Application.ViewModels;

public class CustomerListViewModel
{
    public List<CustomerItemViewModel> Customers { get; set; } = new();
    public string? SearchTerm { get; set; }
    public CustomerStatus? StatusFilter { get; set; }
    public string? CityFilter { get; set; }
    public int CurrentPage { get; set; } = 1;
    public int TotalPages { get; set; } = 1;
    public int TotalItems { get; set; }
    public int PageSize { get; set; } = 10;
}

public class CustomerItemViewModel
{
    public int CustomerId { get; set; }
    public string CustomerCode { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string? CompanyName { get; set; }
    public string? City { get; set; }
    public string? State { get; set; }
    public CustomerStatus Status { get; set; }
    public string? AssignedToUserName { get; set; }
    public DateTime CreatedDate { get; set; }
}

public class CustomerCreateEditViewModel
{
    public int CustomerId { get; set; }

    [Display(Name = "Customer Code")]
    public string? CustomerCode { get; set; }

    [Required(ErrorMessage = "Customer Name is required.")]
    [StringLength(150, ErrorMessage = "Customer Name cannot exceed 150 characters.")]
    [Display(Name = "Customer Name")]
    public string CustomerName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Email address is required.")]
    [EmailAddress(ErrorMessage = "Enter a valid email address.")]
    [StringLength(150, ErrorMessage = "Email cannot exceed 150 characters.")]
    [Display(Name = "Email Address")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Phone number is required.")]
    [RegularExpression(@"^(\+?[0-9\s\-]{7,20})$", ErrorMessage = "Enter a valid phone number (digits, optional +, spaces, dashes, 7-20 chars).")]
    [StringLength(20, ErrorMessage = "Phone number cannot exceed 20 characters.")]
    [Display(Name = "Phone Number")]
    public string Phone { get; set; } = string.Empty;

    [StringLength(150, ErrorMessage = "Company Name cannot exceed 150 characters.")]
    [Display(Name = "Company Name")]
    public string? CompanyName { get; set; }

    [StringLength(300, ErrorMessage = "Address cannot exceed 300 characters.")]
    [Display(Name = "Address")]
    public string? Address { get; set; }

    [StringLength(100, ErrorMessage = "City cannot exceed 100 characters.")]
    [Display(Name = "City")]
    public string? City { get; set; }

    [StringLength(100, ErrorMessage = "State cannot exceed 100 characters.")]
    [Display(Name = "State")]
    public string? State { get; set; }

    [Required(ErrorMessage = "Status is required.")]
    [Display(Name = "Status")]
    public CustomerStatus Status { get; set; } = CustomerStatus.Active;

    [Display(Name = "Assigned Sales Executive")]
    public string? AssignedToUserId { get; set; }

    [Display(Name = "Customer Notes")]
    [DataType(DataType.MultilineText)]
    public string? Notes { get; set; }
}

public class CustomerDetailsViewModel
{
    public int CustomerId { get; set; }
    public string CustomerCode { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string? CompanyName { get; set; }
    public string? Address { get; set; }
    public string? City { get; set; }
    public string? State { get; set; }
    public CustomerStatus Status { get; set; }
    public string? Notes { get; set; }
    public DateTime CreatedDate { get; set; }
    public DateTime? ModifiedDate { get; set; }
    public string? CreatedBy { get; set; }
    public string? AssignedToUserName { get; set; }

    public List<OpportunityItemViewModel> Opportunities { get; set; } = new();
    public List<FollowUpItemViewModel> FollowUps { get; set; } = new();
    public List<ActivityItemViewModel> Activities { get; set; } = new();
}
