#nullable enable
// ════════════════════════════════════════════════════════════════════════
//  DEMO: HỆ THỐNG QUẢN LÝ KHOA - TRƯỜNG ĐẠI HỌC (C# OOP)
//  Kiến trúc phân tầng: Domain → Repository → Service → Presentation
//  Patterns: Singleton, Repository, Factory, Strategy, Template Method,
//            Mapper (Adapter), Observer, Facade
//  Chạy: dotnet new console → paste → dotnet run
// ════════════════════════════════════════════════════════════════════════
using System;
using System.Collections.Generic;
using System.Linq;

namespace QuanLyKhoaDemo
{
    // ════════════════════════════════════════════════════════════════════
    //  REGION 1 — EXCEPTION LAYER
    //  Mục đích: dùng custom exception thay vì trả null/false → dễ debug,
    //  phân biệt rõ 3 loại lỗi: input sai / không tìm thấy / vi phạm rule.
    // ════════════════════════════════════════════════════════════════════
    #region 1. Exceptions

    /// <summary>Base exception cho mọi lỗi nghiệp vụ của domain.</summary>
    public class DomainException : Exception
    {
        public DomainException(string message) : base(message) { }
    }

    /// <summary>Lỗi do input không hợp lệ (format, rỗng, ngoài khoảng).</summary>
    public class ValidationException : DomainException
    {
        public ValidationException(string m) : base(m) { }
    }

    /// <summary>Lỗi không tìm thấy thực thể (thường map sang HTTP 404).</summary>
    public class NotFoundException : DomainException
    {
        public NotFoundException(string m) : base(m) { }
    }

    /// <summary>Lỗi vi phạm quy tắc nghiệp vụ (trùng, đầy lớp, vượt tín chỉ...).</summary>
    public class BusinessException : DomainException
    {
        public BusinessException(string m) : base(m) { }
    }

    #endregion

    // ════════════════════════════════════════════════════════════════════
    //  REGION 2 — ENUMS
    // ════════════════════════════════════════════════════════════════════
    #region 2. Enums

    /// <summary>Vai trò người dùng — dùng cho phân quyền.</summary>
    public enum VaiTro { Admin, GiangVien, SinhVien, NhanVien }

    /// <summary>Trạng thái học tập của sinh viên.</summary>
    public enum TrangThaiHocTap { DangHoc, BaoLuu, DaTotNghiep, ThoiHoc }

    /// <summary>Học vị của giảng viên.</summary>
    public enum HocVi { CuNhan, ThacSi, TienSi, PGS, GS }

    #endregion

    // ════════════════════════════════════════════════════════════════════
    //  REGION 3 — DOMAIN / MODEL LAYER
    //  Đây là tầng thực thể: KHÔNG chứa SQL, KHÔNG chứa business logic phức tạp.
    //  Áp dụng: Encapsulation, Inheritance, Polymorphism, Abstraction.
    // ════════════════════════════════════════════════════════════════════
    #region 3. Domain

    // ────────────────────────────────────────────────────────────────────
    //  Person — ABSTRACT CLASS (Abstraction + Template Method)
    //  • Abstract: GetVaiTro() buộc lớp con tự định nghĩa.
    //  • Template: GetThongTinCoBan() gọi động GetVaiTro() → polymorphism.
    //  • Encapsulation: field protected + property public có kiểm soát.
    // ────────────────────────────────────────────────────────────────────
    public abstract class Person
    {
        public string Id { get; protected set; }
        public string HoTen { get; set; }
        public DateOnly NgaySinh { get; set; }
        public string GioiTinh { get; set; } = "Khác";
        public string Email { get; set; } = string.Empty;
        public string SoDienThoai { get; set; } = string.Empty;

        // Constructor protected → chỉ lớp con gọi được, không `new Person()`.
        protected Person(string id, string hoTen, DateOnly ngaySinh)
        {
            if (string.IsNullOrWhiteSpace(id))
                throw new ValidationException("ID không được rỗng");
            if (string.IsNullOrWhiteSpace(hoTen))
                throw new ValidationException("Họ tên không được rỗng");
            Id = id;
            HoTen = hoTen;
            NgaySinh = ngaySinh;
        }

        /// <summary>ABSTRACT: lớp con BẮT BUỘC override — trả về tên vai trò.</summary>
        public abstract string GetVaiTro();

        /// <summary>TEMPLATE METHOD: dùng chung — nội suy GetVaiTro() của lớp con.</summary>
        public virtual string GetThongTinCoBan() => $"{Id} - {HoTen} ({GetVaiTro()})";
    }

    // ────────────────────────────────────────────────────────────────────
    //  SinhVien — kế thừa Person (Inheritance)
    //  Thêm thuộc tính riêng + business rule DuocXetHocBong().
    // ────────────────────────────────────────────────────────────────────
    public class SinhVien : Person
    {
        public string MaLop { get; set; } = string.Empty;
        public string MaNganh { get; set; } = string.Empty;
        public int KhoaHoc { get; set; }                 // VD: 2022
        public double Gpa { get; set; }                   // Hệ 4, [0,4]
        public TrangThaiHocTap TrangThai { get; set; } = TrangThaiHocTap.DangHoc;

        public SinhVien(string mssv, string hoTen, DateOnly ngaySinh)
            : base(mssv, hoTen, ngaySinh) { }

        // Override abstract → đa hình khi gọi GetVaiTro() qua Person.
        public override string GetVaiTro() => "SINH VIÊN";

        /// <summary>Business rule: GPA ≥ 3.2 và đang học thì được xét học bổng.</summary>
        public bool DuocXetHocBong() => Gpa >= 3.2 && TrangThai == TrangThaiHocTap.DangHoc;
    }

    // ────────────────────────────────────────────────────────────────────
    //  GiangVien — kế thừa Person
    // ────────────────────────────────────────────────────────────────────
    public class GiangVien : Person
    {
        public string MaBoMon { get; set; } = string.Empty;
        public HocVi HocVi { get; set; } = HocVi.CuNhan;
        public List<string> MonDayDuoc { get; set; } = new();

        public GiangVien(string maGv, string hoTen, DateOnly ngaySinh)
            : base(maGv, hoTen, ngaySinh) { }

        public override string GetVaiTro() => "GIẢNG VIÊN";
    }

    // ────────────────────────────────────────────────────────────────────
    //  NhanVien — kế thừa Person
    // ────────────────────────────────────────────────────────────────────
    public class NhanVien : Person
    {
        public string ChucVu { get; set; } = string.Empty;

        public NhanVien(string maNv, string hoTen, DateOnly ngaySinh)
            : base(maNv, hoTen, ngaySinh) { }

        public override string GetVaiTro() => "NHÂN VIÊN";
    }

    // ────────────────────────────────────────────────────────────────────
    //  Khoa — quản lý tập Bộ môn (composition + encapsulation).
    // ────────────────────────────────────────────────────────────────────
    public class Khoa
    {
        public string MaKhoa { get; set; }
        public string TenKhoa { get; set; }
        public string TruongKhoa { get; set; } = string.Empty;

        // Encapsulation: list private, expose IReadOnlyList → không cho add trực tiếp.
        private readonly List<BoMon> _boMons = new();
        public IReadOnlyList<BoMon> BoMons => _boMons;

        public Khoa(string maKhoa, string tenKhoa)
        {
            MaKhoa = maKhoa;
            TenKhoa = tenKhoa;
        }

        public void ThemBoMon(BoMon bm) => _boMons.Add(bm);
    }

    /// <summary>Bộ môn — trực thuộc Khoa, có thể chứa nhiều Ngành.</summary>
    public class BoMon
    {
        public string MaBoMon { get; set; }
        public string TenBoMon { get; set; }
        public string MaKhoa { get; set; }

        private readonly List<Nganh> _nganhs = new();
        public IReadOnlyList<Nganh> Nganhs => _nganhs;

        public BoMon(string ma, string ten, string maKhoa)
        { MaBoMon = ma; TenBoMon = ten; MaKhoa = maKhoa; }

        public void ThemNganh(Nganh n) => _nganhs.Add(n);
    }

    /// <summary>Ngành đào tạo.</summary>
    public class Nganh
    {
        public string MaNganh { get; set; }
        public string TenNganh { get; set; }
        public string MaBoMon { get; set; }

        public Nganh(string ma, string ten, string maBoMon)
        { MaNganh = ma; TenNganh = ten; MaBoMon = maBoMon; }
    }

    /// <summary>Môn học — đơn vị kiến thức, có số tín chỉ.</summary>
    public class MonHoc
    {
        public string MaMh { get; set; }
        public string TenMh { get; set; }
        public int SoTinChi { get; set; }

        public MonHoc(string ma, string ten, int stc)
        { MaMh = ma; TenMh = ten; SoTinChi = stc; }
    }

    /// <summary>Lớp học phần — một môn được mở trong 1 học kỳ, do 1 GV dạy.</summary>
    public class LopHocPhan
    {
        public string MaLhp { get; set; }
        public string MaMh { get; set; }
        public string MaGv { get; set; }
        public string MaHocKy { get; set; }
        public int SiSoToiDa { get; set; }
        public int SiSoHienTai { get; private set; }     // chỉ tăng/giảm qua method

        public LopHocPhan(string maLhp, string maMh, string maGv, string hocKy, int siSoToiDa)
        {
            MaLhp = maLhp; MaMh = maMh; MaGv = maGv;
            MaHocKy = hocKy; SiSoToiDa = siSoToiDa;
        }

        /// <summary>Lớp đã đầy chưa.</summary>
        public bool IsDay() => SiSoHienTai >= SiSoToiDa;

        /// <summary>Tăng sĩ số khi có SV đăng ký — có kiểm tra tràn.</summary>
        public void TangSiSo()
        {
            if (IsDay()) throw new BusinessException($"Lớp {MaLhp} đã đầy");
            SiSoHienTai++;
        }

        public void GiamSiSo() { if (SiSoHienTai > 0) SiSoHienTai--; }
    }

    /// <summary>Điểm số — tính tổng kết theo trọng số, xếp loại chữ, quy đổi hệ 4.</summary>
    public class Diem
    {
        public double ChuyenCan { get; set; }
        public double GiuaKy { get; set; }
        public double CuoiKy { get; set; }

        /// <summary>Tổng kết = 10% chuyên cần + 30% giữa kỳ + 60% cuối kỳ.</summary>
        public double TongKet => ChuyenCan * 0.1 + GiuaKy * 0.3 + CuoiKy * 0.6;

        /// <summary>Xếp loại điểm chữ (A/B/C/D/F).</summary>
        public string XepLoai => TongKet switch
        {
            >= 8.5 => "A",
            >= 7.0 => "B",
            >= 5.5 => "C",
            >= 4.0 => "D",
            _      => "F"
        };

        /// <summary>Quy đổi sang hệ 4 (quy ước phổ biến).</summary>
        public double DiemHe4 => TongKet switch
        {
            >= 8.5 => 4.0,
            >= 8.0 => 3.5,
            >= 7.0 => 3.0,
            >= 6.5 => 2.5,
            >= 5.5 => 2.0,
            >= 5.0 => 1.5,
            >= 4.0 => 1.0,
            _      => 0.0
        };

        public bool Dat => TongKet >= 4.0;
    }

    /// <summary>Đăng ký học phần — liên kết SinhVien ↔ LopHocPhan, kèm điểm.</summary>
    public class DangKy
    {
        public int Id { get; set; }
        public string Mssv { get; set; }
        public string MaLhp { get; set; }
        public DateTime NgayDangKy { get; set; }
        public Diem? Diem { get; set; }                   // null = chưa nhập

        public DangKy(int id, string mssv, string maLhp, DateTime ngay)
        { Id = id; Mssv = mssv; MaLhp = maLhp; NgayDangKy = ngay; }
    }

    /// <summary>Tài khoản đăng nhập — gắn với 1 Person qua Id.</summary>
    public class TaiKhoan
    {
        public string Username { get; set; }
        public string PasswordHash { get; set; }          // demo: lưu plaintext, thực tế phải hash
        public VaiTro VaiTro { get; set; }
        public bool KichHoat { get; set; } = true;

        public TaiKhoan(string username, string password, VaiTro vaiTro)
        { Username = username; PasswordHash = password; VaiTro = vaiTro; }
    }

    #endregion

    // ════════════════════════════════════════════════════════════════════
    //  REGION 4 — UTIL (Singleton + Logger + Factory)
    // ════════════════════════════════════════════════════════════════════
    #region 4. Util

    /// <summary>
    /// SINGLETON — cấu hình toàn cục.
    /// Dùng Lazy&lt;T&gt; (thread-safe, lazy-init) thay vì if-null-lock thủ công.
    /// </summary>
    public sealed class AppConfig
    {
        private static readonly Lazy<AppConfig> _lazy =
            new(() => new AppConfig(), isThreadSafe: true);

        public static AppConfig Instance => _lazy.Value;

        public string TenUngDung { get; } = "Hệ Thống Quản Lý Khoa - ĐH";
        public string PhienBan   { get; } = "1.0.0";

        // Constructor private → chặn `new AppConfig()` từ ngoài.
        private AppConfig()
        {
            Logger.Info("Khởi tạo AppConfig (Singleton) — chỉ chạy 1 lần duy nhất");
        }
    }

    /// <summary>Logger đơn giản ra console — có prefix level + section.</summary>
    public static class Logger
    {
        public static void Info(string msg)  => Console.WriteLine($"[INFO]  {msg}");
        public static void Warn(string msg)  => Console.WriteLine($"[WARN]  {msg}");
        public static void Error(string msg) => Console.WriteLine($"[ERROR] {msg}");

        /// <summary>In tiêu đề section có khung — cho dễ đọc demo.</summary>
        public static void Section(string title)
        {
            Console.WriteLine();
            Console.WriteLine(new string('═', 64));
            Console.WriteLine($"  {title}");
            Console.WriteLine(new string('═', 64));
        }
    }

    /// <summary>
    /// FACTORY METHOD — tạo Person theo VaiTro mà không cần `new` trực tiếp.
    /// Lợi ích: thêm loại Person mới chỉ sửa switch, caller không đổi.
    /// </summary>
    public static class PersonFactory
    {
        public static Person Create(VaiTro vaiTro, string id, string hoTen, DateOnly ngaySinh)
            => vaiTro switch
            {
                VaiTro.SinhVien  => new SinhVien(id, hoTen, ngaySinh),
                VaiTro.GiangVien => new GiangVien(id, hoTen, ngaySinh),
                VaiTro.NhanVien  => new NhanVien(id, hoTen, ngaySinh),
                _ => throw new ValidationException($"Vai trò không hợp lệ: {vaiTro}")
            };
    }

    #endregion

    // ════════════════════════════════════════════════════════════════════
    //  REGION 5 — DTO & MAPPER (Adapter pattern)
    //  DTO tách khỏi Entity: Controller chỉ thấy DTO, không thấy Entity.
    // ════════════════════════════════════════════════════════════════════
    #region 5. DTO + Mapper

    /// <summary>Data Transfer Object cho SinhVien — không chứa business logic.</summary>
    public class SinhVienDto
    {
        public string Mssv { get; set; } = string.Empty;
        public string HoTen { get; set; } = string.Empty;
        public DateOnly NgaySinh { get; set; }
        public string MaLop { get; set; } = string.Empty;
        public string MaNganh { get; set; } = string.Empty;
        public double Gpa { get; set; }
        public string TrangThai { get; set; } = "DangHoc";

        public override string ToString()
            => $"{Mssv,-10} | {HoTen,-22} | Lớp: {MaLop,-8} | GPA: {Gpa:F2} | {TrangThai}";
    }

    /// <summary>MAPPER — chuyển đổi 2 chiều Entity ↔ DTO.</summary>
    public static class SinhVienMapper
    {
        public static SinhVien ToEntity(SinhVienDto dto) => new(dto.Mssv, dto.HoTen, dto.NgaySinh)
        {
            MaLop    = dto.MaLop,
            MaNganh  = dto.MaNganh,
            Gpa      = dto.Gpa,
            TrangThai = Enum.TryParse<TrangThaiHocTap>(dto.TrangThai, out var st)
                        ? st : TrangThaiHocTap.DangHoc
        };

        public static SinhVienDto ToDto(SinhVien e) => new()
        {
            Mssv      = e.Id,
            HoTen     = e.HoTen,
            NgaySinh  = e.NgaySinh,
            MaLop     = e.MaLop,
            MaNganh   = e.MaNganh,
            Gpa       = e.Gpa,
            TrangThai = e.TrangThai.ToString()
        };
    }

    #endregion

    // ════════════════════════════════════════════════════════════════════
    //  REGION 6 — REPOSITORY LAYER
    //  Data Access: chỉ CRUD, không chứa business logic.
    //  Demo dùng in-memory; production có thể thay bằng JDBC/EF.
    // ════════════════════════════════════════════════════════════════════
    #region 6. Repository

    /// <summary>Interface generic cho mọi Repository (Abstraction).</summary>
    public interface IRepository<T, in TId> where T : class
    {
        T? FindById(TId id);
        IReadOnlyList<T> FindAll();
        void Save(T entity);
        void Update(T entity);
        void DeleteById(TId id);
        bool Exists(TId id);
    }

    /// <summary>Base in-memory — dùng chung logic CRUD cho mọi entity.</summary>
    public abstract class InMemoryRepository<T, TId> : IRepository<T, TId>
        where T : class
    {
        protected readonly Dictionary<TId, T> Store = new();

        /// <summary>Lớp con cung cấp cách lấy khóa từ entity.</summary>
        protected abstract TId GetId(T entity);

        public T? FindById(TId id) => Store.TryGetValue(id, out var e) ? e : null;
        public IReadOnlyList<T> FindAll() => Store.Values.ToList();
        public void Save(T entity) => Store[GetId(entity)] = entity;
        public void Update(T entity) => Store[GetId(entity)] = entity;
        public void DeleteById(TId id) => Store.Remove(id);
        public bool Exists(TId id) => Store.ContainsKey(id);
    }

    // ── Interface đặc thù cho từng entity (thêm method truy vấn riêng) ──
    public interface ISinhVienRepository : IRepository<SinhVien, string>
    {
        List<SinhVien> FindByMaLop(string maLop);
        List<SinhVien> FindByNganh(string maNganh);
    }

    public interface IGiangVienRepository : IRepository<GiangVien, string> { }
    public interface IKhoaRepository     : IRepository<Khoa, string> { }
    public interface IMonHocRepository   : IRepository<MonHoc, string> { }
    public interface ILopHocPhanRepository : IRepository<LopHocPhan, string> { }

    public interface IDangKyRepository : IRepository<DangKy, int>
    {
        List<DangKy> FindByMssv(string mssv);
        bool Exists(string mssv, string maLhp);
    }

    // ── Impl cụ thể ──
    public class SinhVienRepository : InMemoryRepository<SinhVien, string>, ISinhVienRepository
    {
        protected override string GetId(SinhVien e) => e.Id;
        public List<SinhVien> FindByMaLop(string maLop) => Store.Values.Where(s => s.MaLop == maLop).ToList();
        public List<SinhVien> FindByNganh(string maNganh) => Store.Values.Where(s => s.MaNganh == maNganh).ToList();
    }

    public class GiangVienRepository : InMemoryRepository<GiangVien, string>, IGiangVienRepository
    {
        protected override string GetId(GiangVien e) => e.Id;
    }

    public class KhoaRepository : InMemoryRepository<Khoa, string>, IKhoaRepository
    {
        protected override string GetId(Khoa e) => e.MaKhoa;
    }

    public class MonHocRepository : InMemoryRepository<MonHoc, string>, IMonHocRepository
    {
        protected override string GetId(MonHoc e) => e.MaMh;
    }

    public class LopHocPhanRepository : InMemoryRepository<LopHocPhan, string>, ILopHocPhanRepository
    {
        protected override string GetId(LopHocPhan e) => e.MaLhp;
    }

    public class DangKyRepository : InMemoryRepository<DangKy, int>, IDangKyRepository
    {
        private int _autoId = 1;

        protected override int GetId(DangKy e) => e.Id;

        /// <summary>Override Save để tự sinh Id khi thêm mới.</summary>
        public new void Save(DangKy e)
        {
            if (e.Id == 0) e.Id = _autoId++;
            base.Save(e);
        }

        public List<DangKy> FindByMssv(string mssv)
            => Store.Values.Where(d => d.Mssv == mssv).ToList();

        public bool Exists(string mssv, string maLhp)
            => Store.Values.Any(d => d.Mssv == mssv && d.MaLhp == maLhp);
    }

    #endregion

    // ════════════════════════════════════════════════════════════════════
    //  REGION 7 — STRATEGY PATTERN (tính học bổng)
    //  Mỗi loại học bổng = 1 strategy. Thêm loại mới = thêm class, không sửa code cũ.
    // ════════════════════════════════════════════════════════════════════
    #region 7. Strategy

    public interface IHocBongStrategy
    {
        string TenLoai { get; }
        bool DuDieuKien(SinhVien sv);
        decimal TinhTienThuong(SinhVien sv);
    }

    /// <summary>Học bổng Xuất sắc — GPA ≥ 3.6.</summary>
    public class HocBongXuatSacStrategy : IHocBongStrategy
    {
        public string TenLoai => "Học bổng Xuất sắc";
        public bool DuDieuKien(SinhVien sv)
            => sv.Gpa >= 3.6 && sv.TrangThai == TrangThaiHocTap.DangHoc;
        public decimal TinhTienThuong(SinhVien sv) => 10_000_000m;
    }

    /// <summary>Học bổng Giỏi — 3.2 ≤ GPA &lt; 3.6.</summary>
    public class HocBongGioiStrategy : IHocBongStrategy
    {
        public string TenLoai => "Học bổng Giỏi";
        public bool DuDieuKien(SinhVien sv)
            => sv.Gpa >= 3.2 && sv.Gpa < 3.6 && sv.TrangThai == TrangThaiHocTap.DangHoc;
        public decimal TinhTienThuong(SinhVien sv) => 5_000_000m;
    }

    #endregion

    // ════════════════════════════════════════════════════════════════════
    //  REGION 8 — OBSERVER PATTERN (thông báo sự kiện)
    //  Service phát sự kiện → mọi observer nhận được (VD: gửi email khi đăng ký).
    // ════════════════════════════════════════════════════════════════════
    #region 8. Observer

    public interface IThongBaoObserver
    {
        void NhanThongBao(string noiDung);
    }

    public interface IThongBaoSubject
    {
        void Subscribe(IThongBaoObserver o);
        void Unsubscribe(IThongBaoObserver o);
        void PhatThongBao(string noiDung);
    }

    /// <summary>Subject trung tâm — quản lý danh sách observer.</summary>
    public class ThongBaoService : IThongBaoSubject
    {
        private readonly List<IThongBaoObserver> _observers = new();
        public void Subscribe(IThongBaoObserver o)   => _observers.Add(o);
        public void Unsubscribe(IThongBaoObserver o) => _observers.Remove(o);
        public void PhatThongBao(string noiDung)     => _observers.ForEach(o => o.NhanThongBao(noiDung));
    }

    /// <summary>Observer cụ thể — in thông báo ra console (thực tế: gửi email/SMS).</summary>
    public class ConsoleObserver : IThongBaoObserver
    {
        public void NhanThongBao(string noiDung) => Logger.Info($"[THÔNG BÁO] {noiDung}");
    }

    #endregion

    // ════════════════════════════════════════════════════════════════════
    //  REGION 9 — SERVICE LAYER (BUSINESS LOGIC)
    //  Đây là nơi chứa quy tắc nghiệp vụ. Phụ thuộc vào interface của repo.
    // ════════════════════════════════════════════════════════════════════
    #region 9. Service

    public interface ISinhVienService
    {
        SinhVienDto GetSinhVien(string mssv);
        List<SinhVienDto> GetAll();
        void Them(SinhVienDto dto);
        void CapNhat(SinhVienDto dto);
        void Xoa(string mssv);
        List<(string LoaiHocBong, decimal TienThuong, SinhVienDto Sv)> DanhSachHocBong();
    }

    public interface IDangKyService
    {
        DangKy DangKyHocPhan(string mssv, string maLhp);
        List<DangKy> GetBySinhVien(string mssv);
    }

    public interface IDiemService
    {
        void NhapDiem(int dangKyId, double chuyenCan, double giuaKy, double cuoiKy);
        List<(string MaLhp, string TenMon, Diem Diem)> GetBangDiem(string mssv);
    }

    // ────────────────────────────────────────────────────────────────────
    //  SinhVienService — Constructor Injection (DI thủ công).
    //  Áp dụng: SRP (chỉ lo nghiệp vụ SV), DIP (phụ thuộc interface).
    // ────────────────────────────────────────────────────────────────────
    public class SinhVienService : ISinhVienService
    {
        private readonly ISinhVienRepository _repo;
        private readonly IReadOnlyList<IHocBongStrategy> _strategies;
        private readonly IThongBaoSubject _thongBao;

        public SinhVienService(
            ISinhVienRepository repo,
            IEnumerable<IHocBongStrategy> strategies,
            IThongBaoSubject thongBao)
        {
            _repo       = repo;
            _strategies = strategies.ToList();
            _thongBao   = thongBao;
        }

        public SinhVienDto GetSinhVien(string mssv)
        {
            var e = _repo.FindById(mssv)
                ?? throw new NotFoundException($"Không tìm thấy SV: {mssv}");
            return SinhVienMapper.ToDto(e);
        }

        public List<SinhVienDto> GetAll()
            => _repo.FindAll().Select(SinhVienMapper.ToDto).ToList();

        /// <summary>Validate 2 lớp: format cơ bản + business rule (trùng mã).</summary>
        public void Them(SinhVienDto dto)
        {
            // 1. Validate format
            if (string.IsNullOrWhiteSpace(dto.HoTen))
                throw new ValidationException("Tên sinh viên không được rỗng");
            if (dto.Gpa < 0 || dto.Gpa > 4)
                throw new ValidationException("GPA phải nằm trong [0, 4]");

            // 2. Validate nghiệp vụ
            if (_repo.Exists(dto.Mssv))
                throw new BusinessException($"MSSV '{dto.Mssv}' đã tồn tại");

            _repo.Save(SinhVienMapper.ToEntity(dto));
            _thongBao.PhatThongBao($"Đã thêm sinh viên {dto.Mssv} - {dto.HoTen}");
        }

        public void CapNhat(SinhVienDto dto)
        {
            if (!_repo.Exists(dto.Mssv))
                throw new NotFoundException($"Không tìm thấy SV: {dto.Mssv}");
            _repo.Update(SinhVienMapper.ToEntity(dto));
            _thongBao.PhatThongBao($"Đã cập nhật SV {dto.Mssv}");
        }

        public void Xoa(string mssv)
        {
            if (!_repo.Exists(mssv))
                throw new NotFoundException($"Không tìm thấy SV: {mssv}");
            _repo.DeleteById(mssv);
            _thongBao.PhatThongBao($"Đã xóa SV {mssv}");
        }

        /// <summary>
        /// Xét học bổng — duyệt tập sinh viên, thử từng strategy.
        /// Strategy pattern: thêm loại mới không sửa method này.
        /// </summary>
        public List<(string LoaiHocBong, decimal TienThuong, SinhVienDto Sv)> DanhSachHocBong()
        {
            var ketQua = new List<(string, decimal, SinhVienDto)>();
            foreach (var sv in _repo.FindAll())
            {
                // Duyệt strategy từ cao xuống thấp → SV chỉ nhận loại cao nhất đủ điều kiện.
                var strategy = _strategies.FirstOrDefault(s => s.DuDieuKien(sv));
                if (strategy is null) continue;
                ketQua.Add((strategy.TenLoai, strategy.TinhTienThuong(sv), SinhVienMapper.ToDto(sv)));
            }
            return ketQua;
        }
    }

    // ────────────────────────────────────────────────────────────────────
    //  DangKyService — nghiệp vụ phức tạp nhất (nhiều rule).
    // ────────────────────────────────────────────────────────────────────
    public class DangKyService : IDangKyService
    {
        private readonly IDangKyRepository _dkRepo;
        private readonly ILopHocPhanRepository _lhpRepo;
        private readonly ISinhVienRepository _svRepo;
        private readonly IThongBaoSubject _thongBao;

        public DangKyService(
            IDangKyRepository dkRepo,
            ILopHocPhanRepository lhpRepo,
            ISinhVienRepository svRepo,
            IThongBaoSubject thongBao)
        {
            _dkRepo   = dkRepo;
            _lhpRepo  = lhpRepo;
            _svRepo   = svRepo;
            _thongBao = thongBao;
        }

        /// <summary>
        /// Đăng ký học phần — đủ 4 business rule:
        ///   1) SV tồn tại     2) Lớp tồn tại
        ///   3) Lớp chưa đầy   4) SV chưa đăng ký lớp này.
        /// </summary>
        public DangKy DangKyHocPhan(string mssv, string maLhp)
        {
            var sv = _svRepo.FindById(mssv)
                ?? throw new NotFoundException($"Sinh viên không tồn tại: {mssv}");
            var lhp = _lhpRepo.FindById(maLhp)
                ?? throw new NotFoundException($"Lớp học phần không tồn tại: {maLhp}");

            if (lhp.IsDay())
                throw new BusinessException($"Lớp '{maLhp}' đã đầy ({lhp.SiSoHienTai}/{lhp.SiSoToiDa})");
            if (_dkRepo.Exists(mssv, maLhp))
                throw new BusinessException($"SV {mssv} đã đăng ký lớp {maLhp} rồi");

            var dk = new DangKy(0, mssv, maLhp, DateTime.Now);
            _dkRepo.Save(dk);

            lhp.TangSiSo();
            _lhpRepo.Update(lhp);

            _thongBao.PhatThongBao(
                $"SV {sv.HoTen} ({mssv}) vừa đăng ký lớp {maLhp} — sĩ số {lhp.SiSoHienTai}/{lhp.SiSoToiDa}");
            return dk;
        }

        public List<DangKy> GetBySinhVien(string mssv) => _dkRepo.FindByMssv(mssv);
    }

    // ────────────────────────────────────────────────────────────────────
    //  DiemService — nhập điểm + xuất bảng điểm.
    // ────────────────────────────────────────────────────────────────────
    public class DiemService : IDiemService
    {
        private readonly IDangKyRepository _dkRepo;
        private readonly ILopHocPhanRepository _lhpRepo;
        private readonly IMonHocRepository _mhRepo;

        public DiemService(
            IDangKyRepository dkRepo,
            ILopHocPhanRepository lhpRepo,
            IMonHocRepository mhRepo)
        {
            _dkRepo  = dkRepo;
            _lhpRepo = lhpRepo;
            _mhRepo  = mhRepo;
        }

        /// <summary>Nhập/cập nhật điểm cho 1 đăng ký — có validate khoảng [0,10].</summary>
        public void NhapDiem(int dangKyId, double chuyenCan, double giuaKy, double cuoiKy)
        {
            var dk = _dkRepo.FindById(dangKyId)
                ?? throw new NotFoundException($"Không tìm thấy đăng ký Id={dangKyId}");

            static void CheckDiem(double d, string ten)
            {
                if (d < 0 || d > 10) throw new ValidationException($"{ten} phải trong [0,10]");
            }
            CheckDiem(chuyenCan, "Điểm chuyên cần");
            CheckDiem(giuaKy,    "Điểm giữa kỳ");
            CheckDiem(cuoiKy,    "Điểm cuối kỳ");

            dk.Diem = new Diem { ChuyenCan = chuyenCan, GiuaKy = giuaKy, CuoiKy = cuoiKy };
            _dkRepo.Update(dk);
        }

        /// <summary>Xuất bảng điểm: ghép Đăng ký + Lớp + Môn.</summary>
        public List<(string MaLhp, string TenMon, Diem Diem)> GetBangDiem(string mssv)
        {
            var kq = new List<(string, string, Diem)>();
            foreach (var dk in _dkRepo.FindByMssv(mssv))
            {
                if (dk.Diem is null) continue;
                var lhp = _lhpRepo.FindById(dk.MaLhp); if (lhp is null) continue;
                var mh  = _mhRepo.FindById(lhp.MaMh);  if (mh  is null) continue;
                kq.Add((lhp.MaLhp, mh.TenMh, dk.Diem));
            }
            return kq;
        }
    }

    #endregion

    // ════════════════════════════════════════════════════════════════════
    //  REGION 10 — PRESENTATION LAYER (DEMO SCRIPT)
    //  Trong production sẽ là Controller / REST. Ở demo này, ta đóng vai
    //  "client" gọi Service, chứng minh toàn bộ luồng end-to-end.
    // ════════════════════════════════════════════════════════════════════
    #region 10. Program (Main)

    internal static class Program
    {
        // ── BƯỚC 0: điểm vào chương trình ──
        private static void Main()
        {
            Console.OutputEncoding = System.Text.Encoding.UTF8;

            Logger.Section("DEMO HỆ THỐNG QUẢN LÝ KHOA - TRƯỜNG ĐẠI HỌC (C# OOP)");

            // 1. Khởi tạo hạ tầng theo mô hình DI thủ công
            var (svc, dk, diem, svRepo) = KhoiTaoHeThong();

            // 2. Nạp dữ liệu mẫu
            SeedDuLieu(svRepo);

            // 3. Chạy kịch bản demo
            ChayKichBan(svc, dk, diem, svRepo);
        }

        // ── DI thủ công: "wire" toàn bộ dependency trong 1 hàm ──
        private static (ISinhVienService, IDangKyService, IDiemService, ISinhVienRepository)
            KhoiTaoHeThong()
        {
            // Util: Singleton cấu hình
            _ = AppConfig.Instance;
            Logger.Info("Đang khởi tạo Container (DI thủ công)...");

            // Repo
            ISinhVienRepository   svRepo  = new SinhVienRepository();
            ILopHocPhanRepository lhpRepo = new LopHocPhanRepository();
            IDangKyRepository     dkRepo  = new DangKyRepository();
            IMonHocRepository     mhRepo  = new MonHocRepository();

            // Subject + Observer
            var thongBao = new ThongBaoService();
            thongBao.Subscribe(new ConsoleObserver());

            // Service — inject dependency qua constructor
            var svSvc = new SinhVienService(
                svRepo,
                new IHocBongStrategy[] { new HocBongXuatSacStrategy(), new HocBongGioiStrategy() },
                thongBao);
            var dkSvc  = new DangKyService(dkRepo, lhpRepo, svRepo, thongBao);
            var diemSvc = new DiemService(dkRepo, lhpRepo, mhRepo);

            // Lưu seed data qua static (đơn giản hóa cho demo)
            SeedRepository.LhpRepo = lhpRepo;
            SeedRepository.MhRepo  = mhRepo;

            return (svSvc, dkSvc, diemSvc, svRepo);
        }

        // ── Nạp dữ liệu mẫu — chỉ để demo có dữ liệu ──
        private static void SeedDuLieu(ISinhVienRepository svRepo)
        {
            Logger.Section("SEED DỮ LIỆU MẪU");

            // 3 sinh viên
            svRepo.Save(new SinhVien("SV001", "Nguyễn Văn An",  new DateOnly(2004, 1, 15))
                        { MaLop = "CNTT01", MaNganh = "N01", KhoaHoc = 2022, Gpa = 3.8 });
            svRepo.Save(new SinhVien("SV002", "Trần Thị Bình",  new DateOnly(2004, 5, 20))
                        { MaLop = "CNTT01", MaNganh = "N01", KhoaHoc = 2022, Gpa = 3.3 });
            svRepo.Save(new SinhVien("SV003", "Lê Hoàng Cường", new DateOnly(2004, 9, 10))
                        { MaLop = "CNTT02", MaNganh = "N02", KhoaHoc = 2022, Gpa = 2.9 });

            // 2 môn học
            SeedRepository.MhRepo.Save(new MonHoc("MH01", "Lập trình hướng đối tượng", 3));
            SeedRepository.MhRepo.Save(new MonHoc("MH02", "Cơ sở dữ liệu", 4));

            // 2 lớp học phần
            SeedRepository.LhpRepo.Save(new LopHocPhan("LHP01", "MH01", "GV01", "HK1-2024", 2));
            SeedRepository.LhpRepo.Save(new LopHocPhan("LHP02", "MH02", "GV02", "HK1-2024", 40));

            Logger.Info("Đã nạp 3 SV, 2 môn, 2 lớp học phần");
        }

        // ── KỊCH BẢN DEMO: chạy tuần tự, show hết nghiệp vụ ──
        private static void ChayKichBan(
            ISinhVienService svc,
            IDangKyService dk,
            IDiemService diem,
            ISinhVienRepository svRepo)
        {
            // [BƯỚC 1] Xem danh sách
            Logger.Section("[1] XEM DANH SÁCH SINH VIÊN");
            svc.GetAll().ForEach(s => Console.WriteLine("  " + s));

            // [BƯỚC 2] Thêm SV mới — happy path
            Logger.Section("[2] THÊM SINH VIÊN MỚI (happy path)");
            svc.Them(new SinhVienDto
            {
                Mssv = "SV004", HoTen = "Phạm Minh Dũng",
                NgaySinh = new DateOnly(2004, 3, 3),
                MaLop = "CNTT02", MaNganh = "N02", Gpa = 3.65
            });
            Logger.Info($"Tổng số SV hiện tại: {svc.GetAll().Count}");

            // [BƯỚC 3] Test validation — trùng MSSV
            Logger.Section("[3] TEST VALIDATION — thêm trùng MSSV");
            try
            {
                svc.Them(new SinhVienDto { Mssv = "SV001", HoTen = "Ai Đó", Gpa = 3.0 });
            }
            catch (DomainException ex)
            {
                Logger.Error($"Bắt lỗi đúng loại: {ex.GetType().Name} → {ex.Message}");
            }

            // [BƯỚC 4] Đăng ký học phần — happy path
            Logger.Section("[4] ĐĂNG KÝ HỌC PHẦN (happy path)");
            var dk1 = dk.DangKyHocPhan("SV001", "LHP01");
            var dk2 = dk.DangKyHocPhan("SV002", "LHP01");
            Logger.Info($"SV001 đăng ký thành công, mã ĐK = {dk1.Id}");
            Logger.Info($"SV002 đăng ký thành công, mã ĐK = {dk2.Id}");

            // [BƯỚC 5] Test business rule — lớp đã đầy
            Logger.Section("[5] TEST BUSINESS RULE — lớp đã đầy");
            try
            {
                dk.DangKyHocPhan("SV003", "LHP01");   // LHP01 sĩ số tối đa = 2
            }
            catch (DomainException ex)
            {
                Logger.Error($"Bắt lỗi đúng loại: {ex.GetType().Name} → {ex.Message}");
            }

            // [BƯỚC 6] Nhập điểm
            Logger.Section("[6] NHẬP ĐIỂM");
            diem.NhapDiem(dk1.Id, chuyenCan: 9.0, giuaKy: 8.5, cuoiKy: 9.5);
            diem.NhapDiem(dk2.Id, chuyenCan: 8.0, giuaKy: 7.5, cuoiKy: 8.0);
            Logger.Info("Đã nhập điểm cho SV001 và SV002");

            // [BƯỚC 7] Xem bảng điểm
            Logger.Section("[7] BẢNG ĐIỂM CỦA SV001");
            foreach (var (maLhp, tenMon, d) in diem.GetBangDiem("SV001"))
            {
                Console.WriteLine($"  {maLhp} | {tenMon,-30} | " +
                                  $"CC={d.ChuyenCan:F1} GK={d.GiuaKy:F1} CK={d.CuoiKy:F1} " +
                                  $"→ Tổng kết = {d.TongKet:F2} ({d.XepLoai}, hệ 4: {d.DiemHe4:F1})");
            }

            // [BƯỚC 8] Xét học bổng — dùng Strategy
            Logger.Section("[8] DANH SÁCH HỌC BỔNG (Strategy Pattern)");
            foreach (var (loai, tien, sv) in svc.DanhSachHocBong())
            {
                Console.WriteLine($"  {sv.Mssv,-8} | {sv.HoTen,-22} | {loai,-22} | " +
                                  $"Thưởng: {tien:N0} VNĐ");
            }

            // [BƯỚC 9] Demo đa hình (Polymorphism) qua abstract class Person
            Logger.Section("[9] DEMO POLYMORPHISM — List<Person>");
            List<Person> dsPerson = new()
            {
                svRepo.FindById("SV001")!,
                new GiangVien("GV01", "TS. Hoàng Văn Em", new DateOnly(1980, 6, 1))
                {
                    HocVi = HocVi.TienSi, MaBoMon = "BM01"
                },
                new NhanVien("NV01", "Nguyễn Thị Phương", new DateOnly(1990, 4, 4))
                {
                    ChucVu = "Kế toán"
                }
            };
            // GetThongTinCoBan() gọi động GetVaiTro() của từng lớp con.
            dsPerson.ForEach(p => Console.WriteLine("  " + p.GetThongTinCoBan()));

            Logger.Section("HOÀN THÀNH DEMO — TẤT CẢ NGHIỆP VỤ ĐÃ CHẠY ĐÚNG");
        }
    }

    /// <summary>
    /// Holder tạm cho seed data — để Main ngắn gọn, không truyền 6 repo lỉnh kỉnh.
    /// (Trong dự án thật, dùng DI Container như Microsoft.Extensions.DependencyInjection.)
    /// </summary>
    internal static class SeedRepository
    {
        public static ILopHocPhanRepository LhpRepo { get; set; } = null!;
        public static IMonHocRepository MhRepo { get; set; } = null!;
    }

    #endregion
}