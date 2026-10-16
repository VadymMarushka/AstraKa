using System;
using System.Collections.Generic;

namespace AstraKa.Infrastructure;

public partial class UserRole
{
    public byte Id { get; set; }

    public string Name { get; set; } = null!;

    public virtual ICollection<User> Users { get; set; } = new List<User>();
}
