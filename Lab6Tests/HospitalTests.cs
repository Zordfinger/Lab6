using System.Text;
using System.Text.Json;
using BusinessLayer;
using DataAccess;
using Services;

namespace Lab6Tests;

public static class HospitalTests
{
    public static int Start(bool showResult = true)
    {
        int passed = 0;
        int failed = 0;
        var report = new StringBuilder();

        void Check(string name, Action test)
        {
            try
            {
                test();
                passed++;
                report.AppendLine("OK: " + name);
            }
            catch (Exception ex)
            {
                failed++;
                report.AppendLine("FAIL: " + name);
                report.AppendLine("     " + ex.Message);
            }
        }

        Check("Admit creates a patient and an electronic medical record", AdmitCreatesPatientAndRecord);
        Check("Admit rejects an empty patient name", AdmitRejectsEmptyName);
        Check("Admit rejects an empty passport number", AdmitRejectsEmptyPassport);
        Check("Admit rejects an empty diagnosis", AdmitRejectsEmptyDiagnosis);
        Check("Admit rejects an empty department", AdmitRejectsEmptyDepartment);
        Check("Admit rejects an empty doctor", AdmitRejectsEmptyDoctor);
        Check("Admit rejects a birth date that is not in the past", AdmitRejectsInvalidBirthDate);
        Check("Admit rejects a duplicate active passport", AdmitRejectsDuplicatePassport);
        Check("A passport can be used again after discharge", PassportCanBeUsedAfterDischarge);
        Check("Injection requires a positive whole-number quantity", InjectionRequiresPositiveQuantity);
        Check("Tablets require dosage and duration details", TabletsRequireDetails);
        Check("AddPrescription requires an active patient", PrescriptionRequiresActivePatient);
        Check("Medication can only be completed by a nurse", MedicationRequiresNurse);
        Check("A nurse completion is stored with time and employee", NurseCompletionIsStored);
        Check("Diagnostics can only be completed by a doctor", DiagnosticRequiresDoctor);
        Check("A completed prescription cannot be completed twice", CompletedPrescriptionCannotBeRepeated);
        Check("Discharge builds an epicrisis with patient and treatment data", DischargeBuildsEpicrisis);
        Check("Failed saving does not change the service state", FailedSaveDoesNotChangeServiceState);
        Check("JSON DAO saves and loads hospital data", JsonDaoRoundTripsData);
        Check("JSON DAO reports corrupt files without replacing them", JsonDaoReportsCorruptFile);

        report.AppendLine();
        report.AppendLine($"Passed: {passed}; Failed: {failed}");
        string text = report.ToString();
        Console.WriteLine(text);
        if (showResult)
        {
            MessageBox.Show(text, "Результаты тестов лабораторной 6", MessageBoxButtons.OK,
                failed == 0 ? MessageBoxIcon.Information : MessageBoxIcon.Error);
        }
        return failed == 0 ? 0 : 1;
    }
    private static void Equal<T>(T expected, T actual)
    {
        if (!EqualityComparer<T>.Default.Equals(expected, actual))
            throw new Exception($"Expected: {expected}, but was: {actual}");
    }

    private static void True(bool condition, string message)
    {
        if (!condition) throw new Exception(message);
    }

    private static void Contains(string text, string expected)
    {
        if (!text.Contains(expected, StringComparison.OrdinalIgnoreCase))
            throw new Exception($"Expected text to contain: [{expected}]");
    }

    private static TException Throws<TException>(Action action) where TException : Exception
    {
        try { action(); }
        catch (TException ex) { return ex; }
        throw new Exception($"Expected exception: {typeof(TException).Name}");
    }

    private static (HospitalService Service, MemoryHospitalDao Dao) CreateService()
    {
        var dao = new MemoryHospitalDao();
        return (new HospitalService(dao), dao);
    }

    private static Patient AdmitSample(HospitalService service, string passport = "AB123456") =>
        service.Admit(
            "Иванов Иван Иванович",
            DateTime.Today.AddYears(-30),
            passport,
            "+375291234567",
            "Плановое обследование",
            "Кардиология",
            "Петрова Анна Сергеевна",
            "Первичный осмотр выполнен.");

    private static void AdmitCreatesPatientAndRecord()
    {
        var (service, dao) = CreateService();
        Patient patient = AdmitSample(service);

        Equal(1, service.Patients.Count);
        Equal("Иванов Иван Иванович", patient.FullName);
        Equal("Плановое обследование", patient.Record.AdmissionDiagnosis);
        Equal("Первичный осмотр выполнен.", patient.Record.InitialExamination);
        Equal("AB123456", dao.Data.Patients.Single().PassportNumber);
    }

    private static void AdmitRejectsEmptyName()
    {
        var (service, _) = CreateService();
        Throws<ArgumentException>(() => service.Admit(" ", DateTime.Today.AddYears(-20), "P1", "", "Диагноз", "Кардиология", "Врач", ""));
    }

    private static void AdmitRejectsEmptyPassport()
    {
        var (service, _) = CreateService();
        Throws<ArgumentException>(() => service.Admit("Пациент", DateTime.Today.AddYears(-20), "", "", "Диагноз", "Кардиология", "Врач", ""));
    }

    private static void AdmitRejectsEmptyDiagnosis()
    {
        var (service, _) = CreateService();
        Throws<ArgumentException>(() => service.Admit("Пациент", DateTime.Today.AddYears(-20), "P1", "", "", "Кардиология", "Врач", ""));
    }

    private static void AdmitRejectsEmptyDepartment()
    {
        var (service, _) = CreateService();
        Throws<ArgumentException>(() => service.Admit("Пациент", DateTime.Today.AddYears(-20), "P1", "", "Диагноз", "", "Врач", ""));
    }

    private static void AdmitRejectsEmptyDoctor()
    {
        var (service, _) = CreateService();
        Throws<ArgumentException>(() => service.Admit("Пациент", DateTime.Today.AddYears(-20), "P1", "", "Диагноз", "Кардиология", "", ""));
    }

    private static void AdmitRejectsInvalidBirthDate()
    {
        var (service, _) = CreateService();
        Throws<ArgumentException>(() => service.Admit("Пациент", DateTime.Today, "P1", "", "Диагноз", "Кардиология", "Врач", ""));
    }

    private static void AdmitRejectsDuplicatePassport()
    {
        var (service, _) = CreateService();
        AdmitSample(service);
        Throws<ArgumentException>(() => AdmitSample(service, "ab123456"));
    }

    private static void PassportCanBeUsedAfterDischarge()
    {
        var (service, _) = CreateService();
        var patient = AdmitSample(service);
        service.Discharge(patient.Id);

        Patient readmitted = AdmitSample(service);
        Equal(2, service.Patients.Count);
        True(!readmitted.IsDischarged, "The new admission should be active");
    }

    private static void InjectionRequiresPositiveQuantity()
    {
        var (service, _) = CreateService();
        var patient = AdmitSample(service);
        Throws<ArgumentException>(() => service.AddPrescription(patient.Id, PrescriptionType.Injection, "Антибиотик", "0"));
        Throws<ArgumentException>(() => service.AddPrescription(patient.Id, PrescriptionType.Injection, "Антибиотик", "abc"));
        Throws<ArgumentException>(() => service.AddPrescription(patient.Id, PrescriptionType.Injection, "Антибиотик", "-2"));
    }

    private static void TabletsRequireDetails()
    {
        var (service, _) = CreateService();
        var patient = AdmitSample(service);
        Throws<ArgumentException>(() => service.AddPrescription(patient.Id, PrescriptionType.Tablets, "Витамин", " "));

        var item = service.AddPrescription(patient.Id, PrescriptionType.Tablets, "Витамин", "1 таблетка, 5 дней");
        Equal("1 таблетка, 5 дней", item.Details);
    }

    private static void PrescriptionRequiresActivePatient()
    {
        var (service, _) = CreateService();
        Throws<ArgumentException>(() => service.AddPrescription(Guid.NewGuid(), PrescriptionType.Diagnostic, "Анализ крови", ""));
    }

    private static void MedicationRequiresNurse()
    {
        var (service, _) = CreateService();
        var patient = AdmitSample(service);
        var item = service.AddPrescription(patient.Id, PrescriptionType.Injection, "Укол", "2");

        Throws<ArgumentException>(() => service.CompletePrescription(patient.Id, item.Id, "Петрова Анна Сергеевна"));
        True(!service.Patients.Single().Record.Prescriptions.Single().IsCompleted, "Invalid performer must not complete the prescription");
    }

    private static void NurseCompletionIsStored()
    {
        var (service, _) = CreateService();
        var patient = AdmitSample(service);
        var item = service.AddPrescription(patient.Id, PrescriptionType.Tablets, "Препарат", "1 таблетка, 3 дня");

        service.CompletePrescription(patient.Id, item.Id, "Смирнова Елена Викторовна");
        var saved = service.Patients.Single().Record.Prescriptions.Single();
        True(saved.IsCompleted, "Prescription should be completed");
        True(saved.CompletedAt.HasValue, "Completion time should be stored");
        Equal("Смирнова Елена Викторовна", saved.CompletedBy);
    }

    private static void DiagnosticRequiresDoctor()
    {
        var (service, _) = CreateService();
        var patient = AdmitSample(service);
        var item = service.AddPrescription(patient.Id, PrescriptionType.Diagnostic, "УЗИ", "Брюшной полости");

        Throws<ArgumentException>(() => service.CompletePrescription(patient.Id, item.Id, "Смирнова Елена Викторовна"));
        service.CompletePrescription(patient.Id, item.Id, "Петрова Анна Сергеевна");
        True(service.Patients.Single().Record.Prescriptions.Single().IsCompleted, "Doctor should complete diagnostics");
    }

    private static void CompletedPrescriptionCannotBeRepeated()
    {
        var (service, _) = CreateService();
        var patient = AdmitSample(service);
        var item = service.AddPrescription(patient.Id, PrescriptionType.Diagnostic, "КТ", "Грудной клетки");
        service.CompletePrescription(patient.Id, item.Id, "Петрова Анна Сергеевна");

        Throws<ArgumentException>(() => service.CompletePrescription(patient.Id, item.Id, "Петрова Анна Сергеевна"));
    }

    private static void DischargeBuildsEpicrisis()
    {
        var (service, _) = CreateService();
        var patient = AdmitSample(service);
        service.AddPrescription(patient.Id, PrescriptionType.Diagnostic, "УЗИ", "Сердца");

        string epicrisis = service.Discharge(patient.Id);
        Contains(epicrisis, "ВЫПИСНОЙ ЭПИКРИЗ");
        Contains(epicrisis, patient.FullName);
        Contains(epicrisis, patient.Record.AdmissionDiagnosis);
        Contains(epicrisis, "УЗИ");
        True(service.Patients.Single().IsDischarged, "Patient should be marked as discharged");
    }

    private static void FailedSaveDoesNotChangeServiceState()
    {
        var (service, dao) = CreateService();
        dao.FailOnSave = true;

        Throws<IOException>(() => AdmitSample(service));
        Equal(0, service.Patients.Count);
        Equal(0, dao.Data.Patients.Count);
    }

    private static void JsonDaoRoundTripsData()
    {
        string directory = Path.Combine(Path.GetTempPath(), "Lab6Tests_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        try
        {
            string path = Path.Combine(directory, "hospital-data.json");
            var dao = new JsonHospitalDao(path);
            var source = new HospitalData
            {
                Departments = [new HospitalDepartment { Name = "Кардиология" }],
                Staff = [new MedicalStaff { FullName = "Тестовый врач", Role = StaffRole.Doctor, Specialty = "Кардиолог" }],
                Patients = [new Patient { FullName = "Тестовый пациент", PassportNumber = "T1", Record = new MedicalRecord { AdmissionDiagnosis = "Тестовый диагноз" } }]
            };

            dao.Save(source);
            HospitalData loaded = dao.Load();
            Equal("Тестовый пациент", loaded.Patients.Single().FullName);
            Equal("Тестовый диагноз", loaded.Patients.Single().Record.AdmissionDiagnosis);
            Equal("Кардиология", loaded.Departments.Single().Name);
        }
        finally
        {
            if (Directory.Exists(directory)) Directory.Delete(directory, recursive: true);
        }
    }

    private static void JsonDaoReportsCorruptFile()
    {
        string directory = Path.Combine(Path.GetTempPath(), "Lab6Tests_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        try
        {
            string path = Path.Combine(directory, "hospital-data.json");
            const string badJson = "{ invalid json";
            File.WriteAllText(path, badJson);

            Throws<InvalidDataException>(() => new JsonHospitalDao(path).Load());
            Equal(badJson, File.ReadAllText(path));
        }
        finally
        {
            if (Directory.Exists(directory)) Directory.Delete(directory, recursive: true);
        }
    }

    private sealed class MemoryHospitalDao : IHospitalDao
    {
        private HospitalData _data = new()
        {
            Departments = [new HospitalDepartment { Name = "Кардиология" }],
            Staff = [
                new MedicalStaff { FullName = "Петрова Анна Сергеевна", Role = StaffRole.Doctor, Specialty = "Кардиолог" },
                new MedicalStaff { FullName = "Смирнова Елена Викторовна", Role = StaffRole.Nurse, Specialty = "Медсестра" }
            ]
        };

        public bool FailOnSave { get; set; }
        public HospitalData Data => _data;

        public HospitalData Load() => Copy(_data);

        public void Save(HospitalData data)
        {
            if (FailOnSave) throw new IOException("Искусственная ошибка сохранения для теста.");
            _data = Copy(data);
        }

        private static HospitalData Copy(HospitalData data)
        {
            var json = JsonSerializer.Serialize(data);
            return JsonSerializer.Deserialize<HospitalData>(json)!;
        }
    }
}

