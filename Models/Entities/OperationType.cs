using System;
using System.Collections.Generic;

namespace HotelManagementSystem.Models.Entities;

public partial class OperationType
{
    public int OperationTypeId { get; set; }

    public string OperationTypeCode { get; set; } = null!;

    public string OperationTypeName { get; set; } = null!;

    public virtual ICollection<OperationLog> OperationLogs { get; set; } = new List<OperationLog>();
}
