using System.Text.Json;
using BusinessLayer;
using DataAccess;

namespace Services;

public sealed class HospitalService
{
    private readonly IHospitalDao _dao;
    private HospitalData _data;
    private readonly JsonSerializerOptions _cloneOptions = new();

    public HospitalService(IHospitalDao dao) { _dao = dao; _data = dao.Load(); }
    public IReadOnlyList<Patient> Patients => _data.Patients;
    public IReadOnlyList<HospitalDepartment> Departments => _data.Departments;
    public IReadOnlyList<MedicalStaff> Doctors => _data.Staff.Where(x => x.Role == StaffRole.Doctor).ToList();
    public IReadOnlyList<MedicalStaff> Nurses => _data.Staff.Where(x => x.Role == StaffRole.Nurse).ToList();

    public Patient Admit(string fullName, DateTime birthDate, string passport, string phone, string diagnosis, string department, string doctor, string examination)
    {
        Required(fullName, "Введите ФИО пациента."); Required(passport, "Введите номер паспорта.");
        Required(diagnosis, "Укажите диагноз при поступлении."); Required(department, "Выберите отделение.");
        Required(doctor, "Выберите лечащего врача.");
        if (birthDate.Date >= DateTime.Today) throw new ArgumentException("Дата рождения должна быть в прошлом.");
        if (_data.Patients.Any(p => p.PassportNumber.Equals(passport.Trim(), StringComparison.OrdinalIgnoreCase) && !p.IsDischarged))
            throw new ArgumentException("Пациент с таким паспортом уже находится в стационаре.");

        var next = Clone(_data);
        var patient = new Patient { FullName = fullName.Trim(), BirthDate = birthDate.Date, PassportNumber = passport.Trim(), Phone = phone.Trim(), AdmissionDate = DateTime.Now, Department = department, AttendingDoctor = doctor, Record = new MedicalRecord { AdmissionDiagnosis = diagnosis.Trim(), InitialExamination = examination.Trim() } };
        next.Patients.Add(patient);
        Commit(next);
        return patient;
    }

    public Prescription AddPrescription(Guid patientId, PrescriptionType type, string name, string details)
    {
        Required(name, "Укажите название назначения.");
        if (type == PrescriptionType.Injection && (!int.TryParse(details, out var count) || count <= 0))
            throw new ArgumentException("Для инъекции укажите положительное количество.");
        if (type == PrescriptionType.Tablets && string.IsNullOrWhiteSpace(details))
            throw new ArgumentException("Для таблеток укажите дозировку и число дней.");

        var next = Clone(_data);
        var patient = ActivePatient(next, patientId);
        var prescription = new Prescription { Type = type, Name = name.Trim(), Details = details.Trim() };
        patient.Record.Prescriptions.Add(prescription);
        Commit(next);
        return prescription;
    }

    public void CompletePrescription(Guid patientId, Guid prescriptionId, string staffName)
    {
        var currentPatient = ActivePatient(_data, patientId);
        var currentItem = currentPatient.Record.Prescriptions.FirstOrDefault(p => p.Id == prescriptionId)
            ?? throw new ArgumentException("Назначение не найдено.");
        if (currentItem.IsCompleted) throw new ArgumentException("Назначение уже выполнено.");
        var isMedication = currentItem.Type is PrescriptionType.Injection or PrescriptionType.Tablets;
        var staff = _data.Staff.FirstOrDefault(s => s.FullName == staffName)
            ?? throw new ArgumentException("Сотрудник не найден.");
        if (isMedication && staff.Role != StaffRole.Nurse) throw new ArgumentException("Медикаментозное назначение выполняет медсестра.");
        if (!isMedication && staff.Role != StaffRole.Doctor) throw new ArgumentException("Диагностические и профилактические назначения выполняет врач.");

        var next = Clone(_data);
        var item = ActivePatient(next, patientId).Record.Prescriptions.First(p => p.Id == prescriptionId);
        item.IsCompleted = true; item.CompletedAt = DateTime.Now; item.CompletedBy = staffName;
        Commit(next);
    }

    public string Discharge(Guid patientId)
    {
        var next = Clone(_data);
        var patient = ActivePatient(next, patientId);
        patient.DischargeDate = DateTime.Now;
        Commit(next);
        var lines = patient.Record.Prescriptions.Count == 0 ? "Назначений не было." : string.Join(Environment.NewLine, patient.Record.Prescriptions.Select(p => $"- {p.TypeName}: {p.Name} — {p.Details}; {(p.IsCompleted ? $"выполнено {p.CompletedAt:g}, {p.CompletedBy}" : "не выполнено")}"));
        return $"ВЫПИСНОЙ ЭПИКРИЗ\n\nПациент: {patient.FullName}\nДата рождения: {patient.BirthDate:dd.MM.yyyy}\nПаспорт: {patient.PassportNumber}\nПериод пребывания: {patient.AdmissionDate:g} — {patient.DischargeDate:g}\nОтделение: {patient.Department}\nЛечащий врач: {patient.AttendingDoctor}\nДиагноз при поступлении: {patient.Record.AdmissionDiagnosis}\n\nНазначения:\n{lines}";
    }

    private void Commit(HospitalData next)
    {
        _dao.Save(next);
        _data = next;
    }
    private HospitalData Clone(HospitalData data) => JsonSerializer.Deserialize<HospitalData>(JsonSerializer.Serialize(data, _cloneOptions), _cloneOptions)!;
    private static Patient ActivePatient(HospitalData data, Guid id) => data.Patients.FirstOrDefault(p => p.Id == id && !p.IsDischarged) ?? throw new ArgumentException("Активный пациент не найден.");
    private static void Required(string value, string message) { if (string.IsNullOrWhiteSpace(value)) throw new ArgumentException(message); }
}
