using System;
using System.Collections.Generic;
using System.Linq;

// 1. EXCEPTIONS
public class NotFoundException : Exception { public NotFoundException(string m) : base(m) { } }
public class BusinessException : Exception { public BusinessException(string m) : base(m) { } }

// 2. DOMAIN LAYER - MODELS
public abstract class Person
{
    public string Id { get; set; }
    public string FullName { get; set; }
    
    public Person(string id, string fullName)
    {
        if (string.IsNullOrWhiteSpace(id)) throw new BusinessException("ID không hợp lệ");
        Id = id;
        FullName = fullName;
    }
    
    public abstract string GetRole();
}

public class Student : Person
{
    public string Major { get; set; }
    public Student(string id, string name, string major) : base(id, name) { Major = major; }
    public override string GetRole() { return "Sinh viên"; }
}

public class Lecturer : Person
{
    public string AcademicTitle { get; set; }
    public Lecturer(string id, string name, string title) : base(id, name) { AcademicTitle = title; }
    public override string GetRole() { return "Giảng viên"; }
}

public class Course
{
    public string CourseId { get; set; }
    public string CourseName { get; set; }
    public int Credits { get; set; }
    public Course(string id, string name, int credits) { CourseId = id; CourseName = name; Credits = credits; }
}

public class Semester
{
    public string SemesterId { get; set; }
    public string SemesterName { get; set; }
    public Semester(string id, string name) { SemesterId = id; SemesterName = name; }
}

public class CourseSection
{
    public string SectionId { get; set; }
    public Course Course { get; set; }
    public Lecturer Lecturer { get; set; }
    public Semester Semester { get; set; }
    public int MaxStudents { get; set; }
    public int CurrentStudents { get; private set; }

    public CourseSection(string id, Course course, Lecturer lecturer, Semester semester, int max)
    {
        SectionId = id; Course = course; Lecturer = lecturer; Semester = semester; MaxStudents = max;
    }

    public bool IsFull() { return CurrentStudents >= MaxStudents; }
    public void AddStudent() { if (IsFull()) throw new BusinessException("Lớp đã đầy!"); CurrentStudents++; }
}

// [STRATEGY PATTERN] Tính điểm
public interface IGradeStrategy { double Calculate(double midterm, double final); }
public class StandardGradeStrategy : IGradeStrategy
{
    public double Calculate(double midterm, double final) { return (midterm * 0.4) + (final * 0.6); }
}

public class Grade
{
    public double MidtermScore { get; set; }
    public double FinalScore { get; set; }
    private IGradeStrategy _strategy;

    public Grade(IGradeStrategy strategy) { _strategy = strategy; }
    public double GetFinalGrade() { return _strategy.Calculate(MidtermScore, FinalScore); }
}

public class Enrollment
{
    public int EnrollmentId { get; set; }
    public Student Student { get; set; }
    public CourseSection Section { get; set; }
    public Grade Grade { get; set; }

    public Enrollment(int id, Student student, CourseSection section)
    {
        EnrollmentId = id; Student = student; Section = section;
    }
}

// 3. PATTERNS & UTILS
// [SINGLETON PATTERN]
public sealed class DatabaseConnection
{
    private static readonly Lazy<DatabaseConnection> _instance = new Lazy<DatabaseConnection>(() => new DatabaseConnection());
    public static DatabaseConnection Instance { get { return _instance.Value; } }
    private DatabaseConnection() { Console.WriteLine("[SYSTEM] Đã khởi tạo kết nối Database (Singleton)"); }
}

// [FACTORY PATTERN]
public static class PersonFactory
{
    public static Person Create(string type, string id, string name, string extra)
    {
        if (type.ToLower() == "student") return new Student(id, name, extra);
        if (type.ToLower() == "lecturer") return new Lecturer(id, name, extra);
        throw new BusinessException("Loại Person không hợp lệ");
    }
}

// [OBSERVER PATTERN]
public class NotificationService
{
    public void Update(string message)
    {
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine("\n[THÔNG BÁO] " + message);
        Console.ResetColor();
    }
}

// 4. REPOSITORY LAYER
public class CourseRepository
{
    private List<Course> _courses = new List<Course>();
    public void Add(Course item) { _courses.Add(item); }
    public List<Course> GetAll() { return _courses; }
    public Course GetById(string id) { return _courses.FirstOrDefault(c => c.CourseId == id); }
}

public class PersonRepository
{
    private List<Person> _persons = new List<Person>();
    public void Add(Person item) { _persons.Add(item); }
    public List<Person> GetAll() { return _persons; }
    public Person GetById(string id) { return _persons.FirstOrDefault(p => p.Id == id); }
}

public class SectionRepository
{
    private List<CourseSection> _sections = new List<CourseSection>();
    public void Add(CourseSection item) { _sections.Add(item); }
    public List<CourseSection> GetAll() { return _sections; }
    public CourseSection GetById(string id) { return _sections.FirstOrDefault(s => s.SectionId == id); }
}

public class EnrollmentRepository
{
    private List<Enrollment> _enrollments = new List<Enrollment>();
    private int _autoId = 1;
    public void Add(Enrollment item) { item.EnrollmentId = _autoId++; _enrollments.Add(item); }
    public List<Enrollment> GetAll() { return _enrollments; }
    public bool Exists(string studentId, string sectionId) { return _enrollments.Any(e => e.Student.Id == studentId && e.Section.SectionId == sectionId); }
}

// 5. SERVICE LAYER
public class CourseService
{
    private CourseRepository _repo;
    public CourseService(CourseRepository repo) { _repo = repo; }

    public void AddCourse(Course course)
    {
        if (_repo.GetById(course.CourseId) != null) throw new BusinessException("Môn học đã tồn tại!");
        if (course.Credits <= 0) throw new BusinessException("Số tín chỉ phải > 0");
        _repo.Add(course);
    }
    public List<Course> GetAll() { return _repo.GetAll(); }
}

public class EnrollmentService
{
    private EnrollmentRepository _enrollRepo;
    private SectionRepository _sectionRepo;
    private NotificationService _notifier;

    public EnrollmentService(EnrollmentRepository enrollRepo, SectionRepository sectionRepo, NotificationService notifier)
    {
        _enrollRepo = enrollRepo; _sectionRepo = sectionRepo; _notifier = notifier;
    }

    public void Register(Student student, string sectionId)
    {
        var section = _sectionRepo.GetById(sectionId);
        if (section == null) throw new NotFoundException("Không tìm thấy lớp học phần!");
        if (section.IsFull()) throw new BusinessException("Lớp đã đầy sĩ số!");
        if (_enrollRepo.Exists(student.Id, sectionId)) throw new BusinessException("Sinh viên đã đăng ký lớp này rồi!");

        section.AddStudent();
        _enrollRepo.Add(new Enrollment(0, student, section));
        _notifier.Update("Sinh viên " + student.FullName + " đã đăng ký thành công lớp " + sectionId);
    }

    public void InputGrade(string studentId, string sectionId, double mid, double final)
    {
        var enrollment = _enrollRepo.GetAll().FirstOrDefault(e => e.Student.Id == studentId && e.Section.SectionId == sectionId);
        if (enrollment == null) throw new NotFoundException("Không tìm thấy dữ liệu đăng ký của sinh viên này.");

        var grade = new Grade(new StandardGradeStrategy()) { MidtermScore = mid, FinalScore = final };
        enrollment.Grade = grade;
        
        Console.WriteLine("=> Đã nhập điểm. Tổng kết: " + grade.GetFinalGrade().ToString("F2"));
    }
}

// 6. PRESENTATION LAYER
class Program
{
    static DatabaseConnection _db = DatabaseConnection.Instance;
    static CourseRepository _courseRepo = new CourseRepository();
    static PersonRepository _personRepo = new PersonRepository();
    static SectionRepository _sectionRepo = new SectionRepository();
    static EnrollmentRepository _enrollRepo = new EnrollmentRepository();
    
    static NotificationService _notifier = new NotificationService();
    static CourseService _courseService = new CourseService(_courseRepo);
    static EnrollmentService _enrollService = new EnrollmentService(_enrollRepo, _sectionRepo, _notifier);

    static void Main(string[] args)
    {
        Console.OutputEncoding = System.Text.Encoding.UTF8;
        SeedData();

        while (true)
        {
            Console.Clear();
            Console.WriteLine("╔══════════════════════════════════════════════════╗");
            Console.WriteLine("║     HỆ THỐNG QUẢN LÝ KHÓA HỌC ĐẠI HỌC (C#)        ║");
            Console.WriteLine("╠══════════════════════════════════════════════════╣");
            Console.WriteLine("║  1. Quản lý Môn học (Course)                     ║");
            Console.WriteLine("║  2. Quản lý Sinh viên & Giảng viên (Person)      ║");
            Console.WriteLine("║  3. Xem Lớp học phần (CourseSection)             ║");
            Console.WriteLine("║  4. Đăng ký học phần (Enrollment)                ║");
            Console.WriteLine("║  5. Nhập điểm (Grade)                            ║");
            Console.WriteLine("║  0. Thoát                                        ║");
            Console.WriteLine("╚══════════════════════════════════════════════════╝");
            Console.Write("Chọn chức năng (0-5): ");

            string choice = Console.ReadLine();
            try
            {
                switch (choice)
                {
                    case "1": MenuCourse(); break;
                    case "2": MenuPerson(); break;
                    case "3": MenuSection(); break;
                    case "4": MenuEnroll(); break;
                    case "5": MenuGrade(); break;
                    case "0": return;
                    default: Console.WriteLine("Lựa chọn không hợp lệ!"); break;
                }
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("\n[LỖI] " + ex.Message);
                Console.ResetColor();
            }

            Console.WriteLine("\nNhấn Enter để tiếp tục...");
            Console.ReadLine();
        }
    }

    static int ReadInt(string prompt)
    {
        int result;
        Console.Write(prompt);
        while (!int.TryParse(Console.ReadLine(), out result))
        {
            Console.Write("Vui lòng nhập số hợp lệ: ");
        }
        return result;
    }

    static double ReadDouble(string prompt)
    {
        double result;
        Console.Write(prompt);
        while (!double.TryParse(Console.ReadLine(), out result))
        {
            Console.Write("Vui lòng nhập số hợp lệ: ");
        }
        return result;
    }

    static void MenuCourse()
    {
        Console.WriteLine("\n--- THÊM MÔN HỌC MỚI ---");
        Console.Write("Mã môn (VD: CS101): "); string id = Console.ReadLine();
        Console.Write("Tên môn: "); string name = Console.ReadLine();
        int credits = ReadInt("Số tín chỉ: ");

        _courseService.AddCourse(new Course(id, name, credits));
        Console.WriteLine("=> Thêm môn học thành công!");
        
        Console.WriteLine("\n--- DANH SÁCH MÔN HỌC ---");
        foreach (var c in _courseService.GetAll())
        {
            Console.WriteLine(c.CourseId + " | " + c.CourseName + " | " + c.Credits + " TC");
        }
    }

    static void MenuPerson()
    {
        Console.WriteLine("\n--- THÊM NGƯỜI DÙNG ---");
        Console.Write("Loại (student/lecturer): "); string type = Console.ReadLine();
        Console.Write("ID: "); string id = Console.ReadLine();
        Console.Write("Họ tên: "); string name = Console.ReadLine();
        Console.Write("Ngành (nếu là SV) / Học vị (nếu là GV): "); string extra = Console.ReadLine();

        var person = PersonFactory.Create(type, id, name, extra);
        _personRepo.Add(person);
        Console.WriteLine("=> Đã thêm " + person.GetRole() + ": " + person.FullName);
    }

    static void MenuSection()
    {
        Console.WriteLine("\n--- MỞ LỚP HỌC PHẦN MỚI ---");
        Console.Write("Mã lớp (VD: SEC01): "); string secId = Console.ReadLine();
        Console.Write("Mã môn học: "); string courseId = Console.ReadLine();
        Console.Write("Mã giảng viên: "); string lecId = Console.ReadLine();
        int max = ReadInt("Sĩ số tối đa: ");

        var course = _courseRepo.GetById(courseId);
        if (course == null) throw new NotFoundException("Môn học không tồn tại");
        
        var lecturer = _personRepo.GetById(lecId) as Lecturer;
        if (lecturer == null) throw new NotFoundException("Giảng viên không tồn tại");
        
        _sectionRepo.Add(new CourseSection(secId, course, lecturer, new Semester("HK1", "Học kỳ 1"), max));
        Console.WriteLine("=> Mở lớp thành công!");

        Console.WriteLine("\n--- DANH SÁCH LỚP HỌC PHẦN ---");
        foreach (var s in _sectionRepo.GetAll())
        {
            Console.WriteLine(s.SectionId + " | Môn: " + s.Course.CourseName + " | GV: " + s.Lecturer.FullName + " | Sĩ số: " + s.CurrentStudents + "/" + s.MaxStudents);
        }
    }

    static void MenuEnroll()
    {
        Console.WriteLine("\n--- ĐĂNG KÝ HỌC PHẦN ---");
        Console.Write("Mã Sinh viên: "); string svId = Console.ReadLine();
        Console.Write("Mã Lớp học phần: "); string secId = Console.ReadLine();

        var student = _personRepo.GetById(svId) as Student;
        if (student == null) throw new NotFoundException("Không tìm thấy Sinh viên");
        
        _enrollService.Register(student, secId);
    }

    static void MenuGrade()
    {
        Console.WriteLine("\n--- NHẬP ĐIỂM ---");
        Console.Write("Mã Sinh viên: "); string svId = Console.ReadLine();
        Console.Write("Mã Lớp học phần: "); string secId = Console.ReadLine();
        double mid = ReadDouble("Điểm Giữa kỳ: ");
        double final = ReadDouble("Điểm Cuối kỳ: ");

        _enrollService.InputGrade(svId, secId, mid, final);
    }

    static void SeedData()
    {
        _courseRepo.Add(new Course("CS101", "Lập trình C#", 3));
        _personRepo.Add(new Student("SV01", "Nguyễn Văn A", "CNTT"));
        _personRepo.Add(new Lecturer("GV01", "Thầy B", "Thạc sĩ"));
        _sectionRepo.Add(new CourseSection("SEC01", _courseRepo.GetById("CS101"), _personRepo.GetById("GV01") as Lecturer, new Semester("HK1", "Học kỳ 1"), 2));
    }
}