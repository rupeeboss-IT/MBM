using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RB_Website_API.Referrals.Models;

/// <summary>
/// DB2.employee_master (assumed existing).
/// </summary>
[Table("Employee_Master")]
public sealed class EmployeeMaster
{
    [Key]
    public int EmpId { get; set; }

    [MaxLength(200)]
    public string Emp_Name { get; set; } = "";

    [MaxLength(50)]
    public string Emp_Code { get; set; } = "";

    public bool Is_Active { get; set; }
}

