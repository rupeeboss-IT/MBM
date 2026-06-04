using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RB_Website_API.Models;

[Table("MemberIdSequences")]
public sealed class MemberIdSequence
{
    [Key]
    public int Year { get; set; }

    public int LastNumber { get; set; }
}
