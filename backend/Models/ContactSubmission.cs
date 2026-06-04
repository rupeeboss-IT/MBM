using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RB_Website_API.Models;

[Table("ContactSubmissions", Schema = "dbo")]
public sealed class ContactSubmission
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    [MaxLength(160)]
    public string FullName { get; set; } = "";

    [MaxLength(10)]
    public string Phone { get; set; } = "";

    [MaxLength(508)]
    public string Email { get; set; } = "";

    public int SubjectId { get; set; }

    [MaxLength(4000)]
    public string Message { get; set; } = "";

    public bool ConsentAccepted { get; set; }

    public DateTime? ConsentAcceptedAt { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? ConfirmationEmailSentAt { get; set; }
}
