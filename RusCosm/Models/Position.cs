using System;
using System.Collections.Generic;
using RusCosm.Model;

namespace RusCosm.Model;

public partial class Position
{
    public int PositionId { get; set; }

    public string PositionName { get; set; } = null!;

    public virtual ICollection<Employee> Employees { get; set; } = new List<Employee>();
}