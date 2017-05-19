using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Data.Entity;
using System.Web;
using System.Web.Mvc;
using System.Web.Script.Serialization;
using Enterprise.Infrastructure;
using Microsoft.AspNet.Identity;
using Microsoft.Owin.Security;
using OptimalSchedulingLogic;

namespace Enterprise.Models
{
	public class PersonalAreaModel
	{
		public IndexViewModel CommonData { get; set; }

		public RegisterViewModel UserInfo { get; set; }
	}

	public class IndexViewModel
	{
		public bool HasPassword { get; set; }
		public IList<UserLoginInfo> Logins { get; set; }
		public string PhoneNumber { get; set; }
		public bool TwoFactor { get; set; }
		public bool BrowserRemembered { get; set; }
	}

    /// <summary>
    /// Represents an algorithm
    /// </summary>
    public class Algorithm
    {
        public int Id { get; set; }

        [Display(Name = "Назва *")]
        [Required(ErrorMessage = "Назва алгоритма - обов'язкове поле.")]
        public string Name { get; set; }

        [Display(Name = "Вихідний код основної процедури формування розкладу *")]
        [Required(ErrorMessage = "Код алгоритма - обов'язкове поле.")]
        [AllowHtml]
        public string Code { get; set; }

        [Display(Name = "Опис")]
        public string Description { get; set; }

        [Display(Name = "Публічний")]
        public bool Published { get; set; }

        public string UserId { get; set; }

        public DateTime DateAdd { get; set; }
    }

    public class Input
    {
        public int Id { get; set; }

        public double Characteristic { get; set; }

        public int AnalyticId { get; set; }

        public int MachineNumber { get; set; }

        public string Tasks { get; set; }

        public string Solution { get; set; }
    }

    public class Analytic
    {
        public int Id { get; set; }

        [Display(Name = "Назва *")]
        [Required(ErrorMessage = "Назва аналітики - обов'язкове поле.")]
        public string Name { get; set; }
        
        [Display(Name = "Вихідний код функції аналітики *")]
        [Required(ErrorMessage = "Код функції аналітики - обов'язкове поле.")]
        [AllowHtml]
        public string FunctionCode { get; set; }

        [Display(Name = "\"Крок\" на графіку *")]
        [Required(ErrorMessage = "Введіть значення кроку на графіку")]
        [DataType(DataType.Duration, ErrorMessage = "Значення кроку на графіку - дійсне додатнє число.")]
        public double Step { get; set; }

        [Display(Name = "Мітка осі X *")]
        [Required(ErrorMessage = "Мітка осі X - обов'язкове поле.")]
        public string XLabel { get; set; }

        [Display(Name = "Мітка осі Y *")]
        [Required(ErrorMessage = "Мітка осі Y - обов'язкове поле.")]
        public string YLabel { get; set; }

        [Display(Name = "Максимальне значення *")]
        [Required(ErrorMessage = "Введіть максимальне значення аналітики")]
        [DataType(DataType.Duration, ErrorMessage = "Максимальне значення аналітики - дійсне додатнє число.")]
        public double MaxValue { get; set; }

        [Display(Name = "Опис")]
        public string Description { get; set; }

        public string UserId { get; set; }

        public DateTime DateAdd { get; set; }
    }

    public class AlgorithmAnalysisModel
    {
        public Algorithm Algorithm { get; set; }

        public List<AlgorithmAnalytic> AlgorithmAnalytics { get; set; }
    }

    public class InputScheduleViewModel
    {
        public int AlgorithmId { get; set; }

        /// <summary>
        /// Tasks file
        /// </summary>
        public HttpPostedFileBase TasksFile { get; set; }

        /// <summary>
        /// Machines file
        /// </summary>
        public HttpPostedFileBase MachinesFile { get; set; }

        /// <summary>
        /// Use branch & bounds method
        /// </summary>
        public bool BBMethod { get; set; }

        /// <summary>
        /// Build diagram
        /// </summary>
        public bool Visualization { get; set; }
    }

    public class ScheduleViewModel
    {
        #region Properties

        public Schedule FastAlgorithmSchedule { get; set; }

        public long FastAlgorithmTime { get; set; }

        public Schedule AccurateAlgorithmSchedule { get; set; }

        public long AccurateAlgorithmTime { get; set; }

        public bool AccurateAlgorithm { get; set; }

        public bool Visualization { get; set; }

        public bool IsOptimal { get; set; }

        public string FileId { get; set; }

        #endregion

        public string GetJson()
        {
            var data = new List<object>();
            var links = new List<object>();
            var currentId = 3;
            var linkId = 10;

            List<object> fastAlgorithmData;
            List<object> fastAlgorithmLinks;
            DateTime? fastAlgorithmMinStartTime;
            double? fastAlgorithmDuration;
            var commonId = AccurateAlgorithm ? (int?)1 : null;
            getData(FastAlgorithmSchedule, commonId, ref currentId, ref linkId, out fastAlgorithmData, out fastAlgorithmLinks,
                out fastAlgorithmMinStartTime, out fastAlgorithmDuration);
            if (fastAlgorithmMinStartTime != null)
            {
                if (AccurateAlgorithm)
                {
                    var mst = (DateTime)fastAlgorithmMinStartTime;
                    data.Add(new
                    {
                        id = 1,
                        text = string.Format("Fast algorithm schedule (builded in {0} ms)", FastAlgorithmTime),
                        start_date = dateString(mst),
                        duration = string.Format("{0}", fastAlgorithmDuration),
                        progress = 0,
                        open = true
                    });
                }

                data.AddRange(fastAlgorithmData);
                links.AddRange(fastAlgorithmLinks);
            }

            if (AccurateAlgorithm)
            {
                List<object> accAlgorithmData;
                List<object> accAlgorithmLinks;
                DateTime? accAlgorithmMinStartTime;
                double? accAlgorithmDuration;
                getData(AccurateAlgorithmSchedule, 2, ref currentId, ref linkId, out accAlgorithmData, out accAlgorithmLinks,
                    out accAlgorithmMinStartTime, out accAlgorithmDuration);
                if (accAlgorithmMinStartTime != null)
                {
                    var mst = (DateTime)accAlgorithmMinStartTime;
                    data.Add(new
                    {
                        id = 2,
                        text = string.Format("Accurate algorithm schedule (builded in {0} ms)", AccurateAlgorithmTime),
                        start_date = dateString(mst),
                        duration = string.Format("{0}", accAlgorithmDuration),
                        progress = 0,
                        open = true
                    });

                    data.AddRange(accAlgorithmData);
                    links.AddRange(accAlgorithmLinks);
                }
            }

            return new JavaScriptSerializer().Serialize(new { data = data, links = links });
        }

        #region Service functions

        private static void getData(Schedule schedule, int? commonId, ref int startId, ref int startLinkId,
            out List<object> data, out List<object> links, out DateTime? minStartTime, out double? totalDuration)
        {
            data = new List<object>();
            links = new List<object>();

            totalDuration = null;
            minStartTime = null;
            DateTime? maxEndTime = null;
            foreach (var machine in schedule)
            {
                if (machine.Tasks.Count != 0)
                {
                    var parentId = startId++;
                    var startTime = machine.StartTime;
                    var previousId = 0;
                    var duration = 0d;

                    if (minStartTime == null || startTime < minStartTime)
                    {
                        minStartTime = startTime;
                    }

                    foreach (var task in machine.Tasks)
                    {
                        data.Add(new
                        {
                            id = startId,
                            text = string.Format("{0} ({1})", task.Name, task.Id),
                            start_date = dateString(startTime),
                            duration = string.Format("{0}", task.Duration),
                            parent = parentId.ToString(),
                            progress = 0,
                            open = true
                        });
                        startTime = startTime.AddMinutes(task.Duration);

                        if (previousId != 0)
                        {
                            links.Add(new
                            {
                                id = startLinkId.ToString(),
                                source = previousId.ToString(),
                                target = startId.ToString(),
                                type = "0"
                            });
                            ++startLinkId;
                        }
                        previousId = startId;
                        duration += task.Duration;
                        startId++;
                    }

                    data.Add(new
                    {
                        id = parentId,
                        text = string.Format("{0} ({1})", machine.Machine.Name, machine.Machine.Id),
                        start_date = dateString(machine.StartTime),
                        duration = string.Format("{0}", duration),
                        parent = commonId == null ? string.Empty : commonId.ToString(),
                        progress = 1,
                        open = false
                    });

                    var endTime = startTime;
                    if (maxEndTime == null || maxEndTime < endTime)
                    {
                        maxEndTime = endTime;
                    }
                }
            }

            if (minStartTime != null)
            {
                totalDuration = ((DateTime)maxEndTime - (DateTime)minStartTime).TotalMinutes;
            }
        }

        private static string dateString(DateTime date)
        {
            return string.Format("{0:D2}-{1:D2}-{2} {3:D2}:{4:D2}", date.Day, date.Month, date.Year, date.Hour, date.Minute);
        }

        #endregion
    }

    #region Old

    //   public abstract class Item
    //{
    //	public abstract string InheritorName { get; }

    //	public abstract string InheritorNameUrk { get; }

    //	public abstract string Title { get; }


    //	public int Id { get; set; }

    //	[Display(Name="Назва*")]
    //	[Required(ErrorMessage = "Назва - обов'язкове поле.")]
    //	public string Name { get; set; }

    //	[Display(Name = "Опис")]
    //	public string Description { get; set; }

    //	public DateTime CreationDate { get; set; }

    //	public string CreationUserId { get; set; }

    //	public string CreationUserLogin { get; set; }


    //	protected Item()
    //	{
    //		Id = 0;
    //		Name = string.Empty;
    //		Description = string.Empty;
    //		CreationDate = DateTime.MinValue;
    //		CreationUserId = string.Empty;
    //		CreationUserLogin = string.Empty;
    //	}

    //	protected Item(int id, string name, string description, DateTime creationDate, string creationUserId, string creationUserLogin)
    //	{
    //		Id = id;
    //		Name = name;
    //		Description = description;
    //		CreationDate = creationDate;
    //		CreationUserId = creationUserId;
    //		CreationUserLogin = creationUserLogin;
    //	}

    //	public abstract Item Create(int id, string name, string description, DateTime creationDate, string creationUserId, string creationUserLogin);
    //}

    //public class Product : Item
    //{
    //	public override string InheritorName
    //	{
    //		get { return "Product"; }
    //	}

    //	public override string InheritorNameUrk
    //	{
    //		get { return "Продукт"; }
    //	}

    //	public override string Title
    //	{
    //		get { return "Редагування продуктів"; }
    //	}

    //       [Required(ErrorMessage = "Введить дедлайн")]
    //       [Display(Name = "Дедлайн*")]
    //       [DataType(DataType.DateTime, ErrorMessage = "Дедлайн - значення типу дата-час.")]
    //       public DateTime? Deadline { get; set; }


    //       public override Item Create(int id, string name, string description, DateTime creationDate, string creationUserId, string creationUserLogin)
    //	{
    //		return new Product(id, name, null, description, creationDate, creationUserId, creationUserLogin);
    //	}


    //	public Product()
    //	{
    //	    Deadline = null;
    //	}

    //	public Product(int id, string name, DateTime? deadline, string description, DateTime creationDate, string creationUserId, string creationUserLogin)
    //	: base(id, name, description, creationDate, creationUserId, creationUserLogin)
    //	{
    //	    Deadline = deadline;
    //	}
    //}

    //public class Task : Item
    //{
    //	public override string InheritorName
    //	{
    //		get { return "Task"; }
    //	}

    //	public override string InheritorNameUrk
    //	{
    //		get { return "Роботу"; }
    //	}

    //	public override string Title
    //	{
    //		get { return "Редагування робіт"; }
    //	}

    //	public override Item Create(int id, string name, string description, DateTime creationDate, string creationUserId, string creationUserLogin)
    //	{
    //		return new Task(id, name, description, creationDate, creationUserId, creationUserLogin);
    //	}


    //	public Task()
    //	{
    //	}

    //	public Task(int id, string name, string description, DateTime creationDate, string creationUserId, string creationUserLogin)
    //	: base(id, name, description, creationDate, creationUserId, creationUserLogin)
    //	{
    //	}
    //}

    //public class Machine : Item
    //{
    //	[Display(Name = "Дільниця")]
    //	public int DepartmentId { get; set; }

    //	public string DepartmentName { get; set; }


    //	public override string InheritorName
    //	{
    //		get { return "Machine"; }
    //	}

    //	public override string InheritorNameUrk
    //	{
    //		get { return "Прилад"; }
    //	}

    //	public override string Title
    //	{
    //		get { return "Редагування приладів"; }
    //	}

    //	public override Item Create(int id, string name, string description, DateTime creationDate, string creationUserId, string creationUserLogin)
    //	{
    //		return new Machine(id, name, 0, string.Empty, description, creationDate, creationUserId, creationUserLogin);
    //	}


    //	public Machine()
    //	{
    //		DepartmentId = 0;
    //		DepartmentName = string.Empty;
    //	}

    //	public Machine(int id, string name, int departmentId, string departmentName, string description, DateTime creationDate, string creationUserId, string creationUserLogin)
    //	: base(id, name, description, creationDate, creationUserId, creationUserLogin)
    //	{
    //		DepartmentId = departmentId;
    //		DepartmentName = departmentName;
    //	}
    //}

    //public class Department : Item
    //{
    //	public override string InheritorName
    //	{
    //		get { return "Department"; }
    //	}

    //	public override string InheritorNameUrk
    //	{
    //		get { return "Дільницю"; }
    //	}

    //	public override string Title
    //	{
    //		get { return "Редагування дільниць"; }
    //	}

    //	public override Item Create(int id, string name, string description, DateTime creationDate, string creationUserId, string creationUserLogin)
    //	{
    //		return new Department(id, name, description, creationDate, creationUserId, creationUserLogin);
    //	}


    //	public Department()
    //	{
    //	}

    //	public Department(int id, string name, string description, DateTime creationDate, string creationUserId, string creationUserLogin)
    //	: base(id, name, description, creationDate, creationUserId, creationUserLogin)
    //	{
    //	}
    //}

    //public class Technology
    //{
    //	public int Id { get; set; }

    //	public int ProductId { get; set; }

    //	[Required(ErrorMessage = "Виберіть операцію")]
    //	[Display(Name="Операція*")]
    //	public int TaskId { get; set; }

    //	[Required(ErrorMessage = "Введіть тривалість")]
    //	[Display(Name = "Тривалість, с*")]
    //	[DataType(DataType.Duration, ErrorMessage = "Тривалість - дійсне додатнє число.")]
    //	public double Duration { get; set; }

    //	[Display(Name = "Опис")]
    //	public string Description { get; set; }
    //}

    //public class Technologies : List<Technology>
    //{
    //	public int ProductId { get; set; }

    //	public string ProductName { get; set; }
    //}

    //   public class Compatibility
    //   {
    //       public Task Task { get; set; }

    //       public bool IsCompatible { get; set; }
    //   }

    //   public class Compatibilities : List<Compatibility>
    //   {
    //       public Department Department { get; set; }


    //       public Compatibilities()
    //       {
    //       }

    //       public Compatibilities(IEnumerable<Compatibility> compatibilities) : base(compatibilities)
    //       {
    //       }
    //   }

    #endregion

    #region Third-party models

    public class ManageLoginsViewModel
	{
		public IList<UserLoginInfo> CurrentLogins { get; set; }
		public IList<AuthenticationDescription> OtherLogins { get; set; }
	}

	public class FactorViewModel
	{
		public string Purpose { get; set; }
	}

	public class SetPasswordViewModel
	{
		[Required(ErrorMessage = "Пароль - обов'язкове поле")]
		[StringLength(100, ErrorMessage = "{0} повинен містити хоча б {2} символів.", MinimumLength = 6)]
		[DataType(DataType.Password)]
		[Display(Name = "Пароль*")]
		public string NewPassword { get; set; }

		[Required(ErrorMessage = "Введіть підтвердження пароля")]
		[DataType(DataType.Password)]
		[Display(Name = "Підтвердження пароля*")]
		[System.ComponentModel.DataAnnotations.Compare("NewPassword", ErrorMessage = "Пароль і підтвердження не співпадають.")]
		public string ConfirmPassword { get; set; }
	}

	public class ChangePasswordViewModel
	{
		[Required(ErrorMessage = "Введіть поточний пароль")]
		[DataType(DataType.Password)]
		[Display(Name = "Поточний пароль*")]
		public string OldPassword { get; set; }

		[Required(ErrorMessage = "Введіть новий пароль")]
		[StringLength(100, ErrorMessage = "{0} повинен містити хоча б {2} символів.", MinimumLength = 6)]
		[DataType(DataType.Password)]
		[Display(Name = "Новий пароль*")]
		public string NewPassword { get; set; }

		[Required(ErrorMessage = "Введіть підтвердження пароля")]
		[DataType(DataType.Password)]
		[Display(Name = "Підтвердження пароля*")]
		[System.ComponentModel.DataAnnotations.Compare("NewPassword", ErrorMessage = "Пароль і підтвердження не співпадають.")]
		public string ConfirmPassword { get; set; }
	}

	public class AddPhoneNumberViewModel
	{
		[Required]
		[Phone]
		[Display(Name = "Phone Number")]
		public string Number { get; set; }
	}

	public class VerifyPhoneNumberViewModel
	{
		[Required]
		[Display(Name = "Code")]
		public string Code { get; set; }

		[Required]
		[Phone]
		[Display(Name = "Phone Number")]
		public string PhoneNumber { get; set; }
	}

	public class ConfigureTwoFactorViewModel
	{
		public string SelectedProvider { get; set; }
		public ICollection<System.Web.Mvc.SelectListItem> Providers { get; set; }
	}

	#endregion
}