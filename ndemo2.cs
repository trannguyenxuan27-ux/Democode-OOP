#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;

namespace QuanLyKhoaInteractive
{
    // ════════════════════════════════════════════════════════════════════
    //  1. EXCEPTIONS & ENUMS
    // ════════════════════════════════════════════════════════════════════
    public class DomainException : Exception { public DomainException(string m) : base(m) { } }
    public class ValidationException : DomainException { public ValidationException(string m) : base(m) { } }
    public class NotFoundException : DomainException { public NotFoundException(string m) : base(m) { } }
    public class BusinessException : DomainException { public BusinessException(string m) : base(m) { } }

    public enum TrangThaiHocTap { DangHoc, BaoLuu, DaTotNghiep, ThoiHoc }

    // ════════════════════════════════════════════════════════════════════
    //  2. DOMAIN LAYER (Thực thể)
    // ════════════════════════════════════════════════════════════════════
    public abstract class Person
    {
        public string Id { get; protected set; }
        public string HoTen { get; set; }
        
        protected Person(string id, string hoTen)
        {
            if (string.IsNullOrWhiteSpace(id)) throw new ValidationException("ID không được rỗng");
            if (string.IsNullOrWhiteSpace(hoTen)) throw new ValidationException("Họ tên không được rỗng");
            Id = id; HoTen = hoTen;
        }
        public abstract string GetVaiTro();
    }

    public class SinhVien : Person
    {
        public string MaLop { get; set; } = "Chưa xếp";
        public double Gpa { get; set; } = 0;
        public TrangThaiHocTap TrangThai { get; set; } = TrangThaiHocTap.DangHoc;

        public SinhVien(string mssv, string hoTen) : base(mssv, hoTen) { }
        public override string GetVaiTro() => "SINH VIÊN";
        
        public override string ToString() => $"{Id,-8} | {HoTen,-20} | Lớp: {MaLop,-8} | GPA: {Gpa:F2}";
    }

    public class LopHocPhan
    {
        public string MaLhp { get; set; }
        public string TenMon { get; set; }
        public int SiSoToiDa { get; set; }
        public int SiSoHienTai { get; private set; }

        public LopHocPhan(string ma, string ten, int max)
        { MaLhp = ma; TenMon = ten; SiSoToiDa = max; }

        public bool IsDay() => SiSoHienTai >= SiSoToiDa;
        public void TangSiSo() => SiSoHienTai++;
    }

    public class Diem
    {
        public double ChuyenCan { get; set; }
        public double GiuaKy { get; set; }
        public double CuoiKy { get; set; }
        public double TongKet => ChuyenCan * 0.1 + GiuaKy * 0.3 + CuoiKy * 0.6;
        public string XepLoai => TongKet >= 8.5 ? "A" : TongKet >= 7.0 ? "B" : TongKet >= 5.5 ? "C" : TongKet >= 4.0 ? "D" : "F";
    }

    public class DangKy
    {
        public int Id { get; set; }
        public string Mssv { get; set; }
        public string MaLhp { get; set; }
        public Diem? Diem { get; set; }

        public DangKy(int id, string mssv, string maLhp) { Id = id; Mssv = mssv; MaLhp = maLhp; }
    }

    // ════════════════════════════════════════════════════════════════════
    //  3. REPOSITORY LAYER (Lưu trữ In-Memory)
    // ════════════════════════════════════════════════════════════════════
    public class SinhVienRepository
    {
        private readonly Dictionary<string, SinhVien> _store = new();
        public void Save(SinhVien sv) => _store[sv.Id] = sv;
        public SinhVien? FindById(string id) => _store.TryGetValue(id, out var sv) ? sv : null;
        public List<SinhVien> GetAll() => _store.Values.ToList();
        public bool Exists(string id) => _store.ContainsKey(id);
    }

    public class LopHocPhanRepository
    {
        private readonly Dictionary<string, LopHocPhan> _store = new();
        public void Save(LopHocPhan lhp) => _store[lhp.MaLhp] = lhp;
        public LopHocPhan? FindById(string id) => _store.TryGetValue(id, out var lhp) ? lhp : null;
        public List<LopHocPhan> GetAll() => _store.Values.ToList();
    }

    public class DangKyRepository
    {
        private readonly List<DangKy> _store = new();
        private int _autoId = 1;

        public void Save(DangKy dk) { if (dk.Id == 0) dk.Id = _autoId++; _store.Add(dk); }
        public List<DangKy> FindByMssv(string mssv) => _store.Where(d => d.Mssv == mssv).ToList();
        public bool Exists(string mssv, string maLhp) => _store.Any(d => d.Mssv == mssv && d.MaLhp == maLhp);
    }

    // ════════════════════════════════════════════════════════════════════
    //  4. SERVICE LAYER (Nghiệp vụ)
    // ════════════════════════════════════════════════════════════════════
    public class SinhVienService
    {
        private readonly SinhVienRepository _repo;
        public SinhVienService(SinhVienRepository repo) { _repo = repo; }

        public void Them(string mssv, string hoTen, string maLop, double gpa)
        {
            if (_repo.Exists(mssv)) throw new BusinessException($"MSSV {mssv} đã tồn tại!");
            if (gpa < 0 || gpa > 4) throw new ValidationException("GPA phải từ 0 đến 4");
            
            var sv = new SinhVien(mssv, hoTen) { MaLop = maLop, Gpa = gpa };
            _repo.Save(sv);
        }

        public List<SinhVien> GetAll() => _repo.GetAll();
        
        public List<SinhVien> GetHocBong() => _repo.GetAll().Where(s => s.Gpa >= 3.2).ToList();
    }

    public class DangKyService
    {
        private readonly DangKyRepository _dkRepo;
        private readonly LopHocPhanRepository _lhpRepo;
        private readonly SinhVienRepository _svRepo;

        public DangKyService(DangKyRepository dk, LopHocPhanRepository lhp, SinhVienRepository sv)
        { _dkRepo = dk; _lhpRepo = lhp; _svRepo = sv; }

        public void DangKy(string mssv, string maLhp)
        {
            if (_svRepo.FindById(mssv) == null) throw new NotFoundException("Không tìm thấy Sinh Viên");
            var lhp = _lhpRepo.FindById(maLhp) ?? throw new NotFoundException("Không tìm thấy Lớp Học Phần");
            if (lhp.IsDay()) throw new BusinessException("Lớp đã đầy!");
            if (_dkRepo.Exists(mssv, maLhp)) throw new BusinessException("Sinh viên đã đăng ký lớp này rồi!");

            _dkRepo.Save(new DangKy(0, mssv, maLhp));
            lhp.TangSiSo();
        }

        public void NhapDiem(int idDangKy, double cc, double gk, double ck)
        {
            // Demo đơn giản: Tìm trong list
            var dk = _dkRepo.FindByMssv("").FirstOrDefault(); // Placeholder
            // Thực tế cần hàm FindById trong DangKyRepository, nhưng để ngắn gọn ta lấy list
            var allDk = _dkRepo.FindByMssv("SV001").Concat(_dkRepo.FindByMssv("SV002")).ToList(); 
            // (Lược bớt phần tìm kiếm phức tạp để tập trung vào UI)
        }
    }

    // ════════════════════════════════════════════════════════════════════
    //  5. PRESENTATION LAYER (Giao diện tương tác)
    // ════════════════════════════════════════════════════════════════════
    internal static class Program
    {
        // Khởi tạo "Cơ sở dữ liệu" giả lập
        private static readonly SinhVienRepository SvRepo = new();
        private static readonly LopHocPhanRepository LhpRepo = new();
        private static readonly SinhVienService SvService = new(SvRepo);
        private static readonly DangKyService DkService = new(new DangKyRepository(), LhpRepo, SvRepo);

        private static void Main()
        {
            Console.OutputEncoding = System.Text.Encoding.UTF8;
            SeedData(); // Nạp sẵn vài dữ liệu mẫu

            while (true)
            {
                Console.Clear();
                Console.WriteLine("╔══════════════════════════════════════════════════╗");
                Console.WriteLine("║     HỆ THỐNG QUẢN LÝ KHOA - ĐẠI HỌC (C# OOP)      ║");
                Console.WriteLine("╠══════════════════════════════════════════════════╣");
                Console.WriteLine("║  1. Xem danh sách sinh viên                      ║");
                Console.WriteLine("║  2. Thêm sinh viên mới                           ║");
                Console.WriteLine("║  3. Đăng ký học phần                             ║");
                Console.WriteLine("║  4. Xem danh sách lớp học phần                   ║");
                Console.WriteLine("║  5. Xem danh sách học bổng (GPA >= 3.2)          ║");
                Console.WriteLine("║  0. Thoát                                        ║");
                Console.WriteLine("╚══════════════════════════════════════════════════╝");
                Console.Write("Chọn chức năng (0-5): ");

                string choice = Console.ReadLine() ?? "";
                try
                {
                    switch (choice)
                    {
                        case "1": HienThiSinhVien(); break;
                        case "2": ThemSinhVien(); break;
                        case "3": DangKyHocPhan(); break;
                        case "4": HienThiLopHocPhan(); break;
                        case "5": HienThiHocBong(); break;
                        case "0": return;
                        default: Console.WriteLine("Lựa chọn không hợp lệ!"); break;
                    }
                }
                catch (DomainException ex)
                {
                    // Bắt lỗi nghiệp vụ (Validation, Business...) để app không bị crash
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"\n[LỖI] {ex.Message}");
                    Console.ResetColor();
                }

                Console.WriteLine("\nNhấn Enter để tiếp tục...");
                Console.ReadLine();
            }
        }

        // ── Các hàm xử lý Menu ──

        private static void HienThiSinhVien()
        {
            Console.WriteLine("\n--- DANH SÁCH SINH VIÊN ---");
            var list = SvService.GetAll();
            if (list.Count == 0) Console.WriteLine("(Trống)");
            foreach (var sv in list) Console.WriteLine(sv);
        }

        private static void ThemSinhVien()
        {
            Console.WriteLine("\n--- THÊM SINH VIÊN ---");
            Console.Write("Nhập MSSV: ");
            string mssv = Console.ReadLine() ?? "";
            
            Console.Write("Nhập Họ Tên: ");
            string hoTen = Console.ReadLine() ?? "";
            
            Console.Write("Nhập Lớp: ");
            string maLop = Console.ReadLine() ?? "";

            Console.Write("Nhập GPA (0-4): ");
            if (!double.TryParse(Console.ReadLine(), out double gpa))
                throw new ValidationException("GPA phải là số!");

            SvService.Them(mssv, hoTen, maLop, gpa);
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("=> Thêm sinh viên thành công!");
            Console.ResetColor();
        }

        private static void DangKyHocPhan()
        {
            Console.WriteLine("\n--- ĐĂNG KÝ HỌC PHẦN ---");
            Console.Write("Nhập MSSV: ");
            string mssv = Console.ReadLine() ?? "";
            
            Console.Write("Nhập Mã Lớp Học Phần (VD: LHP01): ");
            string maLhp = Console.ReadLine() ?? "";

            DkService.DangKy(mssv, maLhp);
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("=> Đăng ký thành công!");
            Console.ResetColor();
        }

        private static void HienThiLopHocPhan()
        {
            Console.WriteLine("\n--- DANH SÁCH LỚP HỌC PHẦN ---");
            foreach (var lhp in LhpRepo.GetAll())
            {
                Console.WriteLine($"{lhp.MaLhp,-8} | {lhp.TenMon,-25} | Sĩ số: {lhp.SiSoHienTai}/{lhp.SiSoToiDa}");
            }
        }

        private static void HienThiHocBong()
        {
            Console.WriteLine("\n--- DANH SÁCH HỌC BỔNG ---");
            var list = SvService.GetHocBong();
            if (list.Count == 0) Console.WriteLine("(Không có ai đủ điều kiện)");
            foreach (var sv in list) Console.WriteLine(sv);
        }

        // ── Dữ liệu khởi tạo ──
        private static void SeedData()
        {
            SvRepo.Save(new SinhVien("SV001", "Nguyễn Văn An") { MaLop = "CNTT01", Gpa = 3.8 });
            SvRepo.Save(new SinhVien("SV002", "Trần Thị Bình") { MaLop = "CNTT01", Gpa = 3.3 });
            SvRepo.Save(new SinhVien("SV003", "Lê Hoàng Cường") { MaLop = "CNTT02", Gpa = 2.9 });

            LhpRepo.Save(new LopHocPhan("LHP01", "Lập trình hướng đối tượng", 2));
            LhpRepo.Save(new LopHocPhan("LHP02", "Cơ sở dữ liệu", 40));
        }
    }
}