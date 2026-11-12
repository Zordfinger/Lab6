using System.Text.Json;
using BusinessLayer;

namespace DataAccess;

public interface IHospitalDao
{
    HospitalData Load();
    void Save(HospitalData data);
}

public sealed class JsonHospitalDao : IHospitalDao
{
    private readonly string _filePath;
    private readonly JsonSerializerOptions _options = new() { WriteIndented = true };

    public JsonHospitalDao(string? filePath = null)
    {
        _filePath = filePath ?? Path.Combine(AppContext.BaseDirectory, "hospital-data.json");
    }

    public HospitalData Load()
    {
        if (!File.Exists(_filePath)) return CreateInitialData();
        try
        {
            return JsonSerializer.Deserialize<HospitalData>(File.ReadAllText(_filePath), _options)
                ?? throw new InvalidDataException("JSON-файл больницы пуст или содержит null.");
        }
        catch (JsonException ex)
        {
            throw new InvalidDataException($"Не удалось прочитать файл данных '{_filePath}'. Исходный файл оставлен без изменений.", ex);
        }
    }

    public void Save(HospitalData data)
    {
        var directory = Path.GetDirectoryName(_filePath);
        if (!string.IsNullOrEmpty(directory)) Directory.CreateDirectory(directory);

        var tempPath = _filePath + "." + Guid.NewGuid().ToString("N") + ".tmp";
        try
        {
            using (var stream = new FileStream(tempPath, FileMode.CreateNew, FileAccess.Write, FileShare.None))
            {
                JsonSerializer.Serialize(stream, data, _options);
                stream.Flush(flushToDisk: true);
            }
            File.Move(tempPath, _filePath, overwrite: true);
        }
        finally
        {
            if (File.Exists(tempPath)) File.Delete(tempPath);
        }
    }

    private static HospitalData CreateInitialData() => new()
    {
        Departments = [new() { Name = "Кардиология" }, new() { Name = "Хирургия" }, new() { Name = "Неврология" }],
        Staff = [
            new() { FullName = "Иванов Иван Иванович", Role = StaffRole.Doctor, Specialty = "Дежурный врач" },
            new() { FullName = "Петрова Анна Сергеевна", Role = StaffRole.Doctor, Specialty = "Кардиолог" },
            new() { FullName = "Сидоров Павел Олегович", Role = StaffRole.Doctor, Specialty = "Хирург" },
            new() { FullName = "Козлова Мария Игоревна", Role = StaffRole.Doctor, Specialty = "Невролог" },
            new() { FullName = "Смирнова Елена Викторовна", Role = StaffRole.Nurse, Specialty = "Медсестра" },
            new() { FullName = "Волкова Ольга Андреевна", Role = StaffRole.Nurse, Specialty = "Медсестра" }
        ]
    };
}
