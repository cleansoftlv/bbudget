using System.ComponentModel.DataAnnotations;

namespace LMApp.Models.Categories;

public class CategoryDisplayForEdit
{
    public long Id { get; set; }
    
    [Required(ErrorMessage = "Category name is required")]
    [StringLength(40,
        ErrorMessage = "Category name must be between 1 and 40 characters")]
    public string Name { get; set; }
    
    [StringLength(140, ErrorMessage = "Description cannot exceed 140 characters")]
    public string Description { get; set; }
    
    public bool IsArchived { get; set; }
    public bool ExcludeFromBudget { get; set; }
    public bool ExcludeFromTotals { get; set; }
    public bool IsIncome { get; set; }

    // UI state properties
    public bool IsEditing { get; set; }
    public bool IsNew { get; set; }
    public bool IsSaving { get; set; }
    public string SaveError { get; set; }

    // Original values for canceling edits
    public string OriginalName { get; set; }
    public string OriginalDescription { get; set; }
    public bool OriginalIsArchived { get; set; }
    public bool OriginalExcludeFromBudget { get; set; }
    public bool OriginalExcludeFromTotals { get; set; }
    public bool OriginalIsIncome { get; set; }

    public void TrimAll()
    {
        if (Name != null) Name = Name.Trim();
        if (Description != null) Description = Description.Trim();
    }
}
