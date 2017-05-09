using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Data.Entity;
using System.Web.Mvc;
using Microsoft.AspNet.Identity;
using Microsoft.Owin.Security;

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

	public abstract class Item
	{
		public abstract string InheritorName { get; }

		public abstract string InheritorNameUrk { get; }

		public abstract string Title { get; }


		public int Id { get; set; }

		[Display(Name="Назва*")]
		[Required(ErrorMessage = "Назва - обов'язкове поле.")]
		public string Name { get; set; }

		[Display(Name = "Опис")]
		public string Description { get; set; }
		
		public DateTime CreationDate { get; set; }

		public string CreationUserId { get; set; }

		public string CreationUserLogin { get; set; }


		protected Item()
		{
			Id = 0;
			Name = string.Empty;
			Description = string.Empty;
			CreationDate = DateTime.MinValue;
			CreationUserId = string.Empty;
			CreationUserLogin = string.Empty;
		}

		protected Item(int id, string name, string description, DateTime creationDate, string creationUserId, string creationUserLogin)
		{
			Id = id;
			Name = name;
			Description = description;
			CreationDate = creationDate;
			CreationUserId = creationUserId;
			CreationUserLogin = creationUserLogin;
		}

		public abstract Item Create(int id, string name, string description, DateTime creationDate, string creationUserId, string creationUserLogin);
	}

	public class Product : Item
	{
		public override string InheritorName
		{
			get { return "Product"; }
		}

		public override string InheritorNameUrk
		{
			get { return "Продукт"; }
		}

		public override string Title
		{
			get { return "Редагування продуктів"; }
		}

        [Required(ErrorMessage = "Введить дедлайн")]
        [Display(Name = "Дедлайн*")]
        [DataType(DataType.DateTime, ErrorMessage = "Дедлайн - значення типу дата-час.")]
        public DateTime? Deadline { get; set; }


        public override Item Create(int id, string name, string description, DateTime creationDate, string creationUserId, string creationUserLogin)
		{
			return new Product(id, name, null, description, creationDate, creationUserId, creationUserLogin);
		}


		public Product()
		{
		    Deadline = null;
		}

		public Product(int id, string name, DateTime? deadline, string description, DateTime creationDate, string creationUserId, string creationUserLogin)
		: base(id, name, description, creationDate, creationUserId, creationUserLogin)
		{
		    Deadline = deadline;
		}
	}

	public class Task : Item
	{
		public override string InheritorName
		{
			get { return "Task"; }
		}

		public override string InheritorNameUrk
		{
			get { return "Роботу"; }
		}

		public override string Title
		{
			get { return "Редагування робіт"; }
		}

		public override Item Create(int id, string name, string description, DateTime creationDate, string creationUserId, string creationUserLogin)
		{
			return new Task(id, name, description, creationDate, creationUserId, creationUserLogin);
		}


		public Task()
		{
		}

		public Task(int id, string name, string description, DateTime creationDate, string creationUserId, string creationUserLogin)
		: base(id, name, description, creationDate, creationUserId, creationUserLogin)
		{
		}
	}

	public class Machine : Item
	{
		[Display(Name = "Дільниця")]
		public int DepartmentId { get; set; }

		public string DepartmentName { get; set; }


		public override string InheritorName
		{
			get { return "Machine"; }
		}

		public override string InheritorNameUrk
		{
			get { return "Прилад"; }
		}

		public override string Title
		{
			get { return "Редагування приладів"; }
		}

		public override Item Create(int id, string name, string description, DateTime creationDate, string creationUserId, string creationUserLogin)
		{
			return new Machine(id, name, 0, string.Empty, description, creationDate, creationUserId, creationUserLogin);
		}


		public Machine()
		{
			DepartmentId = 0;
			DepartmentName = string.Empty;
		}

		public Machine(int id, string name, int departmentId, string departmentName, string description, DateTime creationDate, string creationUserId, string creationUserLogin)
		: base(id, name, description, creationDate, creationUserId, creationUserLogin)
		{
			DepartmentId = departmentId;
			DepartmentName = departmentName;
		}
	}

	public class Department : Item
	{
		public override string InheritorName
		{
			get { return "Department"; }
		}

		public override string InheritorNameUrk
		{
			get { return "Дільницю"; }
		}

		public override string Title
		{
			get { return "Редагування дільниць"; }
		}

		public override Item Create(int id, string name, string description, DateTime creationDate, string creationUserId, string creationUserLogin)
		{
			return new Department(id, name, description, creationDate, creationUserId, creationUserLogin);
		}


		public Department()
		{
		}

		public Department(int id, string name, string description, DateTime creationDate, string creationUserId, string creationUserLogin)
		: base(id, name, description, creationDate, creationUserId, creationUserLogin)
		{
		}
	}

	public class Technology
	{
		public int Id { get; set; }
		
		public int ProductId { get; set; }

		[Required(ErrorMessage = "Виберіть операцію")]
		[Display(Name="Операція*")]
		public int TaskId { get; set; }
		
		[Required(ErrorMessage = "Введіть тривалість")]
		[Display(Name = "Тривалість, с*")]
		[DataType(DataType.Duration, ErrorMessage = "Тривалість - дійсне додатнє число.")]
		public double Duration { get; set; }

		[Display(Name = "Опис")]
		public string Description { get; set; }
	}

	public class Technologies : List<Technology>
	{
		public int ProductId { get; set; }

		public string ProductName { get; set; }
	}

    public class Compatibility
    {
        public Task Task { get; set; }

        public bool IsCompatible { get; set; }
    }

    public class Compatibilities : List<Compatibility>
    {
        public Department Department { get; set; }


        public Compatibilities()
        {
        }

        public Compatibilities(IEnumerable<Compatibility> compatibilities) : base(compatibilities)
        {
        }
    }

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