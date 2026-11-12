using System.ComponentModel;
using BusinessLayer;
using Services;

namespace Presentation;

public sealed class MainForm : Form
{
    private readonly HospitalService _service;
    private readonly ListBox _patients = new() { Dock = DockStyle.Fill };
    private readonly ListBox _prescriptions = new() { Dock = DockStyle.Fill };
    private readonly TextBox _name = new(), _passport = new(), _phone = new(), _diagnosis = new(), _examination = new(), _rxName = new(), _rxDetails = new();
    private readonly ComboBox _department = new() { DropDownStyle = ComboBoxStyle.DropDownList }, _doctor = new() { DropDownStyle = ComboBoxStyle.DropDownList }, _rxType = new() { DropDownStyle = ComboBoxStyle.DropDownList }, _staff = new() { DropDownStyle = ComboBoxStyle.DropDownList };
    private readonly DateTimePicker _birth = new() { Format = DateTimePickerFormat.Short, MaxDate = DateTime.Today.AddDays(-1), Value = DateTime.Today.AddYears(-30) };
    private readonly Label _record = new() { Dock = DockStyle.Fill, AutoSize = false };

    public MainForm(HospitalService service)
    {
        _service = service; Text = "Больница — учет пациентов"; Width = 1100; Height = 760; MinimumSize = new Size(1000, 700); WindowState = FormWindowState.Maximized; StartPosition = FormStartPosition.CenterScreen;
        BuildUi(); RefreshPatients();
    }

    private void BuildUi()
    {
        var tabs = new TabControl { Dock = DockStyle.Fill };
        var admission = new TabPage("Госпитализация"); var treatment = new TabPage("Лечение и ЭМК");
        tabs.TabPages.Add(admission); tabs.TabPages.Add(treatment); Controls.Add(tabs);
        var form = new TableLayoutPanel { Dock = DockStyle.Top, AutoSize = true, ColumnCount = 2, RowCount = 0, Padding = new Padding(14) };
        form.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 190)); form.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        AddRow(form, "ФИО пациента", _name); AddRow(form, "Дата рождения", _birth); AddRow(form, "Паспорт", _passport); AddRow(form, "Телефон", _phone);
        _department.Items.AddRange(_service.Departments.Cast<object>().ToArray()); _doctor.Items.AddRange(_service.Doctors.Cast<object>().ToArray());
        AddRow(form, "Отделение", _department); AddRow(form, "Лечащий врач", _doctor); AddRow(form, "Диагноз при поступлении", _diagnosis); AddRow(form, "Первичный осмотр", _examination);
        var admitButton = new Button { Text = "Госпитализировать", AutoSize = true }; admitButton.Click += (_, _) => Run(() => { _service.Admit(_name.Text, _birth.Value, _passport.Text, _phone.Text, _diagnosis.Text, _department.Text, _doctor.Text, _examination.Text); _name.Clear(); _passport.Clear(); _phone.Clear(); _diagnosis.Clear(); _examination.Clear(); RefreshPatients(); MessageBox.Show("Пациент госпитализирован, ЭМК создана."); });
        form.Controls.Add(new Label()); form.Controls.Add(admitButton); admission.Controls.Add(form);

        var split = new SplitContainer { Dock = DockStyle.Fill, SplitterDistance = 330, Padding = new Padding(8) }; treatment.Controls.Add(split);
        split.Panel1.Controls.Add(_patients); _patients.SelectedIndexChanged += (_, _) => RefreshRecord();
        var right = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 5, Padding = new Padding(8) };
        right.RowStyles.Add(new RowStyle(SizeType.Percent, 38)); right.RowStyles.Add(new RowStyle(SizeType.Absolute, 52)); right.RowStyles.Add(new RowStyle(SizeType.Percent, 32)); right.RowStyles.Add(new RowStyle(SizeType.Absolute, 70)); right.RowStyles.Add(new RowStyle(SizeType.Percent, 30));
        _record.Padding = new Padding(5); _record.Text = "Выберите пациента, чтобы открыть ЭМК."; _record.BorderStyle = BorderStyle.FixedSingle; right.Controls.Add(_record, 0, 0);
        var rxPanel = new FlowLayoutPanel { Dock = DockStyle.Fill, AutoScroll = true };
        _rxType.Items.AddRange(new object[] { "Инъекция", "Таблетки", "Диагностика", "Профилактика" }); _rxType.SelectedIndex = 0;
        _rxName.Width = 150; _rxDetails.Width = 180; _rxName.PlaceholderText = "Название"; _rxDetails.PlaceholderText = "Кол-во / доза, дни";
        rxPanel.Controls.Add(_rxType); rxPanel.Controls.Add(_rxName); rxPanel.Controls.Add(_rxDetails);
        var addRx = new Button { Text = "Добавить назначение", AutoSize = true }; addRx.Click += (_, _) => Run(() => { var p = SelectedPatient(); _service.AddPrescription(p.Id, (PrescriptionType)_rxType.SelectedIndex, _rxName.Text, _rxDetails.Text); _rxName.Clear(); _rxDetails.Clear(); RefreshPatients(); RefreshRecord(); }); rxPanel.Controls.Add(addRx); right.Controls.Add(rxPanel, 0, 1);
        right.Controls.Add(_prescriptions, 0, 2);
        var executePanel = new FlowLayoutPanel { Dock = DockStyle.Fill, AutoScroll = true, WrapContents = false }; _staff.Width = 440; _staff.DropDownWidth = 560; _staff.MaxDropDownItems = 12; _staff.Items.AddRange(_service.Nurses.Cast<object>().Concat(_service.Doctors).ToArray()); executePanel.Controls.Add(_staff);
        var complete = new Button { Text = "Отметить выполненным", AutoSize = true }; complete.Click += (_, _) => Run(() => { var patient = SelectedPatient(); var rx = _prescriptions.SelectedItem as Prescription ?? throw new ArgumentException("Выберите назначение."); _service.CompletePrescription(patient.Id, rx.Id, _staff.Text); RefreshPatients(); RefreshRecord(); }); executePanel.Controls.Add(complete); right.Controls.Add(executePanel, 0, 3);
        var bottom = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.TopDown, AutoScroll = true }; var discharge = new Button { Text = "Выписать и сформировать эпикриз", AutoSize = true };
        discharge.Click += (_, _) => Run(() => { var text = _service.Discharge(SelectedPatient().Id); RefreshPatients(); RefreshRecord(); MessageBox.Show(text, "Выписной эпикриз", MessageBoxButtons.OK, MessageBoxIcon.Information); });
        bottom.Controls.Add(new Label { Text = "Список назначений пациента:", AutoSize = true }); bottom.Controls.Add(discharge); right.Controls.Add(bottom, 0, 4); split.Panel2.Controls.Add(right);
    }

    private static void AddRow(TableLayoutPanel panel, string title, Control control)
    {
        int row = panel.RowCount++; panel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        var label = new Label { Text = title, AutoSize = true, Anchor = AnchorStyles.Left, Padding = new Padding(0, 7, 0, 0) };
        control.Dock = DockStyle.Top; if (control is TextBox text) text.PlaceholderText = title;
        panel.Controls.Add(label, 0, row); panel.Controls.Add(control, 1, row);
    }
    private Patient SelectedPatient() => _patients.SelectedItem as Patient ?? throw new ArgumentException("Сначала выберите пациента во вкладке лечения.");
    private void RefreshPatients()
    {
        var selected = _patients.SelectedItem as Patient;
        _patients.DataSource = null; _patients.DataSource = new BindingList<Patient>(_service.Patients.ToList());
        if (selected is not null) _patients.SelectedItem = _service.Patients.FirstOrDefault(p => p.Id == selected.Id);
    }
    private void RefreshRecord()
    {
        if (_patients.SelectedItem is not Patient p) { _record.Text = "Выберите пациента, чтобы открыть ЭМК."; _prescriptions.DataSource = null; return; }
        _record.Text = $"ЭЛЕКТРОННАЯ МЕДИЦИНСКАЯ КАРТА\nПациент: {p.FullName}\nПоступил: {p.AdmissionDate:g}\nОтделение: {p.Department}\nЛечащий врач: {p.AttendingDoctor}\nДиагноз: {p.Record.AdmissionDiagnosis}\nПервичный осмотр: {p.Record.InitialExamination}\nСтатус: {(p.IsDischarged ? $"выписан {p.DischargeDate:g}" : "на лечении")}";
        _prescriptions.DataSource = null; _prescriptions.DataSource = p.Record.Prescriptions.ToList();
    }
    private static void Run(Action action) { try { action(); } catch (Exception ex) { MessageBox.Show(ex.Message, "Не удалось выполнить действие", MessageBoxButtons.OK, MessageBoxIcon.Warning); } }
}


