namespace BusinessLayer;

public sealed class HospitalData
{
    public List<Patient> Patients { get; set; } = [];
    public List<HospitalDepartment> Departments { get; set; } = [];
    public List<MedicalStaff> Staff { get; set; } = [];
}
