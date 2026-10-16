using System;
using System.Collections.Generic;

namespace AstraKa.Infrastructure;

public partial class User
{
    public Guid Id { get; set; }

    public string ExternalId { get; set; } = null!;

    public string Email { get; set; } = null!;

    public string Name { get; set; } = null!;

    public byte RoleId { get; set; }

    public virtual UserRole Role { get; set; } = null!;

    public virtual Starshepherd? Starshepherd { get; set; }

    public virtual Wanderer? Wanderer { get; set; }
}
