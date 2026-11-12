namespace BusinessLayer;

public enum PrescriptionType { Injection, Tablets, Diagnostic, Preventive }
public enum StaffRole { Doctor, Nurse }

public sealed class HospitalDepartment
{
    public string Name { get; set; } = "";
    public override string ToString() => Name;
}

public sealed class MedicalStaff
{
    public string FullName { get; set; } = "";
    public StaffRole Role { get; set; }
    public string Specialty { get; set; } = "";
    public override string ToString() => $"{FullName} — {Specialty}";
}

public sealed class Prescription
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public PrescriptionType Type { get; set; }
    public string Name { get; set; } = "";
    public string Details { get; set; } = "";
    public bool IsCompleted { get; set; }
    public DateTime? CompletedAt { get; set; }
    public string? CompletedBy { get; set; }
    public string TypeName => Type switch
    {
        PrescriptionType.Injection => "Инъекция",
        PrescriptionType.Tablets => "Таблетки",
        PrescriptionType.Diagnostic => "Диагностика",
        _ => "Профилактика"
    };
    public override string ToString() => $"[{(IsCompleted ? "Выполнено" : "Ожидает")}] {TypeName}: {Name} ({Details})";
}

public sealed class MedicalRecord
{
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public string AdmissionDiagnosis { get; set; } = "";
    public List<Prescription> Prescriptions { get; set; } = [];
    public string InitialExamination { get; set; } = "";
}

public sealed class Patient
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string FullName { get; set; } = "";
    public DateTime BirthDate { get; set; }
    public string PassportNumber { get; set; } = "";
    public string Phone { get; set; } = "";
    public DateTime AdmissionDate { get; set; } = DateTime.Now;
    public DateTime? DischargeDate { get; set; }
    public string Department { get; set; } = "";
    public string AttendingDoctor { get; set; } = "";
    public bool IsDischarged => DischargeDate.HasValue;
    public MedicalRecord Record { get; set; } = new();
    public override string ToString() => $"{FullName} — {(IsDischarged ? "выписан" : Department)}";
}
