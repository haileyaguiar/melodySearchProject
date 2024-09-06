using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;


namespace melodySearchProject.Models;

public partial class MeiFile
{
    [Key]
    public int file_id { get; set; }

    public string? file_name { get; set; }

    public string? file_content { get; set; }
}
