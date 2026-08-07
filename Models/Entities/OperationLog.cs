using System;
using System.Collections.Generic;

namespace HotelManagementSystem.Models.Entities;

public partial class OperationLog
{
    public int OperationLogId { get; set; }

    public int TargetBranchId { get; set; }

    public DateTime OperatedAt { get; set; }

    public string OperatorEmployeeNumber { get; set; } = null!;

    public int OperationTypeId { get; set; }

    public string TargetType { get; set; } = null!;

    public string TargetIdentifier { get; set; } = null!;

    public string Description { get; set; } = null!;

    public virtual OperationType OperationType { get; set; } = null!;

    public virtual Employee OperatorEmployeeNumberNavigation { get; set; } = null!;

    public virtual Branch TargetBranch { get; set; } = null!;
}
