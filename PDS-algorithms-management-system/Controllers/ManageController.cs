using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using System.Web.WebSockets;
using Enterprise.Infrastructure;
using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.Owin;
using Microsoft.Owin.Security;
using Enterprise.Models;
using Microsoft.AspNet.Identity.EntityFramework;
using System.CodeDom.Compiler;
using ModelingApplication;
using Scheduling;

namespace Enterprise.Controllers
{
	[Authorize]
	public class ManageController : Controller
	{
		#region Managers

		private ApplicationSignInManager _signInManager;
		private ApplicationUserManager _userManager;

		public ManageController()
		{
		}

		public ManageController(ApplicationUserManager userManager, ApplicationSignInManager signInManager)
		{
			UserManager = userManager;
			SignInManager = signInManager;
		}

		public ApplicationSignInManager SignInManager
		{
			get
			{
				return _signInManager ?? HttpContext.GetOwinContext().Get<ApplicationSignInManager>();
			}
			private set
			{
				_signInManager = value;
			}
		}

		public ApplicationUserManager UserManager
		{
			get
			{
				return _userManager ?? HttpContext.GetOwinContext().GetUserManager<ApplicationUserManager>();
			}
			private set
			{
				_userManager = value;
			}
		}

		#endregion

		//
		// GET: /Manage/Index
		[HttpGet]
		public async Task<ActionResult> Index(ManageMessageId? message)
		{
            const int n = 15;
            const int m = 3;
            const int iterNum = 100;
            const int schedulesNum = 100;

            for (var i = 33; i < schedulesNum; i++)
            {
                var currentScale = (i + 1) * 1.0;
                var inputs = Modeler.CalculateOptimalityCriterionEfficiency(n, m, iterNum, currentScale,
                    () => Modeler.NextGamma(5.0, 5.0), () => Modeler.NextExponential(currentScale));

                foreach (var input in inputs)
                {
                    Repository.InsertInput(new Input
                    {
                        AnalyticId = input.AnalyticId,
                        Characteristic = input.Characteristic,
                        MachineNumber = input.MachineNumber,
                        Solution = input.Solution,
                        Tasks = input.Tasks
                    });
                }
            }

            ViewBag.StatusMessage =
			message == ManageMessageId.ChangePasswordSuccess ? "Пароль успішно змінено."
			: message == ManageMessageId.SetPasswordSuccess ? "Пароль успішно збережено."
			: message == ManageMessageId.SetTwoFactorSuccess ? "Your two-factor authentication provider has been set."
			: message == ManageMessageId.Error ? "Виникла помилка. Зверніться до адміністратора."
			: message == ManageMessageId.AddPhoneSuccess ? "Your phone number was added."
			: message == ManageMessageId.RemovePhoneSuccess ? "Your phone number was removed."
			: message == ManageMessageId.ChangeDataSuccess ? "Дані користувача успішно збережені."
			: message == ManageMessageId.ItemsEditedSuccess ? "Інформацію успішно збережено"
			: "";

			var userId = User.Identity.GetUserId();
			var commonData = new IndexViewModel
			{
				HasPassword = HasPassword(),
				PhoneNumber = await UserManager.GetPhoneNumberAsync(userId),
				TwoFactor = await UserManager.GetTwoFactorEnabledAsync(userId),
				Logins = await UserManager.GetLoginsAsync(userId),
				BrowserRemembered = await AuthenticationManager.TwoFactorBrowserRememberedAsync(userId)
			};

			var user = UserManager.FindById(User.Identity.GetUserId());
			var userViewModel = new RegisterViewModel
			{
				BirthDate = user.BirthDate,
				Email = user.Email,
				FullName = user.FullName,
				UserName = user.UserName,
				Password = user.PasswordHash,
				ConfirmPassword = user.PasswordHash
			};
			var model = new PersonalAreaModel { CommonData = commonData, UserInfo = userViewModel };
			return View(model);
		}

		[HttpPost]
		public async Task<ActionResult> Index(RegisterViewModel model)
		{
			ManageMessageId? message = null;
			if (ModelState.IsValid)
			{
				var user = UserManager.FindById(User.Identity.GetUserId());
				user.Email = model.Email;
				user.FullName = model.FullName;
				user.UserName = model.UserName;
				var result = await UserManager.UpdateAsync(user);
				if (!result.Succeeded)
				{
					message = ManageMessageId.Error;
					AddErrors(result);
				}
				else
				{
					message = ManageMessageId.ChangeDataSuccess;
				}
			}

			var actionResult = await Index(message);
			return actionResult;
		}

	    public ActionResult Algorithms()
	    {
	        var availableAlgorithms = Repository.GetAvailableAlgorithms(User.Identity.GetUserId());
	        return View(availableAlgorithms);
	    }

        [HttpGet]
        public ActionResult EditAlgorithm(int id)
        {
            var algorithm = Repository.GetAlgorithm(id);
            var currentUserId = User.Identity.GetUserId();
            if (algorithm == null || string.Compare(currentUserId, algorithm.UserId, StringComparison.InvariantCultureIgnoreCase) != 0)
            {
                return RedirectToAction("NotFound", "Home");
            }

            return View(algorithm);
        }

        [HttpPost]
	    public ActionResult EditAlgorithm(Algorithm algorithm)
	    {
            if (ModelState.IsValid)
            {
                var result = true;
                try
                {
                    var tasks = new List<OptimalSchedulingLogic.Task>
                    {
                        new OptimalSchedulingLogic.Task(1, "", 5, new DateTime(2017, 5, 9, 12, 0, 0)),
                        new OptimalSchedulingLogic.Task(2, "", 7, new DateTime(2017, 5, 9, 12, 2, 0)),
                        new OptimalSchedulingLogic.Task(3, "", 3, new DateTime(2017, 5, 9, 12, 2, 0)),
                        new OptimalSchedulingLogic.Task(4, "", 10, new DateTime(2017, 5, 9, 12, 4, 0)),
                        new OptimalSchedulingLogic.Task(5, "", 8, new DateTime(2017, 5, 9, 12, 5, 0)),
                        new OptimalSchedulingLogic.Task(6, "", 5, new DateTime(2017, 5, 9, 12, 5, 0)),
                        new OptimalSchedulingLogic.Task(7, "", 7, new DateTime(2017, 5, 9, 12, 6, 0))
                    };
                    var methodInfo = ServiceMethods.GetAlgorithmMethod(algorithm.Code);
                    var schedule = ServiceMethods.BuildSchedule(methodInfo, 3, tasks);
                }
                catch (Exception e)
                {
                    ModelState.AddModelError("Code", e.Message);
                    result = false;
                }

                if (result)
                {
                    Repository.UpdateAlgorithm(algorithm);
                    return RedirectToAction("Algorithms");
                }
            }

            return View(algorithm);
	    }

        #region Products

        //[Authorize(Roles = "Technologist")]
        //[HttpGet]
        //public ActionResult EditProducts(string message = null)
        //{
        //	return editItems(new Product(), message);
        //}

        //[Authorize(Roles = "Technologist")]
        //[HttpPost]
        //public ActionResult EditProducts(IEnumerable<Product> products)
        //{
        //	return editItems(new Product(), products);
        //}

        //[Authorize(Roles = "Technologist")]
        //public ActionResult DeleteProduct(int id)
        //{
        //	return deleteItem(new Product(), id);
        //}

        //[Authorize(Roles = "Technologist")]
        //[HttpGet]
        //public ActionResult CreateProduct()
        //{
        //	return createItem(new Product());
        //}

        //[Authorize(Roles = "Technologist")]
        //[HttpPost]
        //public ActionResult CreateProduct(Product product)
        //{
        //	return createItemPost(product);
        //}

        #endregion

        #region Tasks

        //[Authorize(Roles = "Technologist")]
        //[HttpGet]
        //public ActionResult EditTasks(string message = null)
        //{
        //	return editItems(new Models.Task(), message);
        //}

        //[Authorize(Roles = "Technologist")]
        //[HttpPost]
        //public ActionResult EditTasks(IEnumerable<Models.Task> tasks)
        //{
        //	return editItems(new Models.Task(), tasks);
        //}

        //[Authorize(Roles = "Technologist")]
        //public ActionResult DeleteTask(int id)
        //{
        //	return deleteItem(new Models.Task(), id);
        //}

        //[Authorize(Roles = "Technologist")]
        //[HttpGet]
        //public ActionResult CreateTask()
        //{
        //	return createItem(new Models.Task());
        //}

        //[Authorize(Roles = "Technologist")]
        //[HttpPost]
        //public ActionResult CreateTask(Models.Task task)
        //{
        //	return createItemPost(task);
        //}

        #endregion

        #region Machines

        //[Authorize(Roles = "Engineer")]
        //[HttpGet]
        //public ActionResult EditMachines(string message = null)
        //{
        //	return editItems(new Machine(), message);
        //}

        //[Authorize(Roles = "Engineer")]
        //[HttpPost]
        //public ActionResult EditMachines(IEnumerable<Machine> machines)
        //{
        //	return editItems(new Machine(), machines);
        //}

        //[Authorize(Roles = "Engineer")]
        //public ActionResult DeleteMachine(int id)
        //{
        //	return deleteItem(new Machine(), id);
        //}

        //[Authorize(Roles = "Engineer")]
        //[HttpGet]
        //public ActionResult CreateMachine()
        //{
        //	return createItem(new Machine());
        //}

        //[Authorize(Roles = "Engineer")]
        //[HttpPost]
        //public ActionResult CreateMachine(Machine machine)
        //{
        //	return createItemPost(machine);
        //}

        #endregion

        #region Departments

        //[Authorize(Roles = "Engineer")]
        //[HttpGet]
        //public ActionResult EditDepartments(string message = null)
        //{
        //	return editItems(new Department(), message);
        //}

        //[Authorize(Roles = "Engineer")]
        //[HttpPost]
        //public ActionResult EditDepartments(IEnumerable<Department> departments)
        //{
        //	return editItems(new Department(), departments);
        //}

        //[Authorize(Roles = "Engineer")]
        //public ActionResult DeleteDepartment(int id)
        //{
        //	return deleteItem(new Department(), id);
        //}

        //[Authorize(Roles = "Engineer")]
        //[HttpGet]
        //public ActionResult CreateDepartment()
        //{
        //	return createItem(new Department());
        //}

        //[Authorize(Roles = "Engineer")]
        //[HttpPost]
        //public ActionResult CreateDepartment(Department department)
        //{
        //	return createItemPost(department);
        //}

        #endregion

        //[Authorize(Roles = "Engineer,Technologist")]
        //public ActionResult Schedule()
        //{
        //    var machines = Repository.GetItems(new Machine());
        //    var scheduleTasks = Repository.GetScheduleTasks();
        //    var schedule = Schedule<MachineSchedule>.BuildSchedule(scheduleTasks, machines.Cast<Machine>());
        //	  return View(schedule);
        //}

        #region Technologies

        //[Authorize(Roles = "Technologist")]
        //[HttpGet]
        //public ActionResult EditTechnologies(int id, string message = null)
        //{
        //	if (!string.IsNullOrEmpty(message))
        //	{
        //		ViewBag.StatusMessage = message;
        //	}

        //	var list = Repository.GetTechnologies(id);
        //	return View(list);
        //}

        //[Authorize(Roles = "Technologist")]
        //[HttpPost]
        //public ActionResult EditTechnologies(Technologies list)
        //{
        //	if (ModelState.IsValid)
        //	{
        //	    var error = false;
        //	    for (var i = 0; i < list.Count; i++)
        //	    {
        //	        if (!(list[i].Duration > 0))
        //	        {
        //                      ModelState.AddModelError(string.Format("[{0}].Duration", i), "Тривалість повинна бути додатньою.");
        //	            error = true;
        //	        }
        //	    }
        //	    if (error)
        //	    {
        //                  return View(list);
        //              }

        //	    Repository.UpdateTechnologies(list);
        //		return RedirectToAction("EditProducts", "Manage", new { message = "Технологічну карту успішно збережено" });
        //	}
        //    list.ProductName = Request.Form.GetValues(0).First();
        //	return View(list);
        //}

        //[Authorize(Roles = "Technologist")]
        //[HttpGet]
        //public ActionResult CreateTechnology(int id)
        //{
        //	var technology = new Technology {ProductId = id};
        //	return View(technology);
        //}

        //[Authorize(Roles = "Technologist")]
        //[HttpPost]
        //public ActionResult CreateTechnology(Technology technology)
        //{
        //	if (ModelState.IsValid)
        //	{
        //	    if (!(technology.Duration > 0))
        //              {
        //                  ModelState.AddModelError("Duration", "Тривалість повинна бути додатньою.");
        //                  return View(technology);
        //              }
        //	    Repository.CreateTechnology(technology);
        //		return RedirectToAction("EditTechnologies", "Manage", new { id = technology.ProductId, message = "Операцію успішно додано до технологічної карти." });
        //	}
        //	return View(technology);
        //}

        #endregion

        //   [Authorize(Roles = "Engineer")]
        //   public ActionResult Compatibilities(int id)
        //   {
        //       var department = Repository.GetDepartment(id);
        //       if (department == null)
        //       {
        //           return RedirectToAction("NotFound", "Home");
        //       }

        //       var model = new Compatibilities(Repository.GetCompatibilities(id)) {Department = department};

        //       return View(model);
        //}

        //   [Authorize(Roles = "Engineer")]
        //   public ActionResult ChangeCompatibility(int dp, int task, bool value)
        //   {
        //       Repository.ChangeCompatibility(dp, task, value);
        //       return RedirectToAction("Compatibilities", "Manage", new {id = dp});
        //   }

        #region Common actions

        //      private ActionResult editItems(Item instance, string message = null)
        //{
        //	ViewBag.StatusMessage = message;

        //	ViewBag.Instance = instance;
        //	var list = Repository.GetItems(instance);

        //	return View("EditItems", list);
        //}

        //private ActionResult editItems(Item instance, IEnumerable<Item> items)
        //{
        //	if (ModelState.IsValid)
        //	{
        //		Repository.UpdateItems(items);
        //		return RedirectToAction("Index", new {message = ManageMessageId.ItemsEditedSuccess});
        //	}

        //	ViewBag.Instance = instance;
        //	return View("EditItems", items);
        //}

        //private ActionResult deleteItem(Item instance, int id)
        //{
        //	var message = string.Empty;
        //	try
        //	{
        //		Repository.DeleteItem(id, instance.InheritorName);
        //		message = string.Format("{0} було успішно видалено.", instance.InheritorNameUrk);
        //	}
        //	catch (Exception e)
        //	{
        //		message = string.Format("Є дані, що писилаються на цей запис. {0} не було видалено.", instance.InheritorNameUrk);
        //	}
        //	return RedirectToAction(string.Format("Edit{0}s", instance.InheritorName), new { message = message });
        //}

        //private ActionResult createItem(Item instance)
        //{
        //	return View("CreateItem", instance);
        //}

        //private ActionResult createItemPost(Item instance)
        //{
        //	if (ModelState.IsValid)
        //	{
        //		Repository.CreateItem(instance, User.Identity.GetUserId());
        //		return RedirectToAction(string.Format("Edit{0}s", instance.InheritorName),
        //			new { message = string.Format("{0} успішно збережено.", instance.InheritorNameUrk) });
        //	}
        //	return View("CreateItem", instance);
        //}

        #endregion

        #region Third-party tools

        //
        // POST: /Manage/RemoveLogin
        [HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<ActionResult> RemoveLogin(string loginProvider, string providerKey)
		{
			ManageMessageId? message;
			var result = await UserManager.RemoveLoginAsync(User.Identity.GetUserId(), new UserLoginInfo(loginProvider, providerKey));
			if (result.Succeeded)
			{
				var user = await UserManager.FindByIdAsync(User.Identity.GetUserId());
				if (user != null)
				{
					await SignInManager.SignInAsync(user, isPersistent: false, rememberBrowser: false);
				}
				message = ManageMessageId.RemoveLoginSuccess;
			}
			else
			{
				message = ManageMessageId.Error;
			}
			return RedirectToAction("ManageLogins", new { Message = message });
		}

		//
		// GET: /Manage/AddPhoneNumber
		public ActionResult AddPhoneNumber()
		{
			return View();
		}

		//
		// POST: /Manage/AddPhoneNumber
		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<ActionResult> AddPhoneNumber(AddPhoneNumberViewModel model)
		{
			if (!ModelState.IsValid)
			{
				return View(model);
			}
			// Generate the token and send it
			var code = await UserManager.GenerateChangePhoneNumberTokenAsync(User.Identity.GetUserId(), model.Number);
			if (UserManager.SmsService != null)
			{
				var message = new IdentityMessage
				{
					Destination = model.Number,
					Body = "Your security code is: " + code
				};
				await UserManager.SmsService.SendAsync(message);
			}
			return RedirectToAction("VerifyPhoneNumber", new { PhoneNumber = model.Number });
		}

		//
		// POST: /Manage/EnableTwoFactorAuthentication
		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<ActionResult> EnableTwoFactorAuthentication()
		{
			await UserManager.SetTwoFactorEnabledAsync(User.Identity.GetUserId(), true);
			var user = await UserManager.FindByIdAsync(User.Identity.GetUserId());
			if (user != null)
			{
				await SignInManager.SignInAsync(user, isPersistent: false, rememberBrowser: false);
			}
			return RedirectToAction("Index", "Manage");
		}

		//
		// POST: /Manage/DisableTwoFactorAuthentication
		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<ActionResult> DisableTwoFactorAuthentication()
		{
			await UserManager.SetTwoFactorEnabledAsync(User.Identity.GetUserId(), false);
			var user = await UserManager.FindByIdAsync(User.Identity.GetUserId());
			if (user != null)
			{
				await SignInManager.SignInAsync(user, isPersistent: false, rememberBrowser: false);
			}
			return RedirectToAction("Index", "Manage");
		}

		//
		// GET: /Manage/VerifyPhoneNumber
		public async Task<ActionResult> VerifyPhoneNumber(string phoneNumber)
		{
			var code = await UserManager.GenerateChangePhoneNumberTokenAsync(User.Identity.GetUserId(), phoneNumber);
			// Send an SMS through the SMS provider to verify the phone number
			return phoneNumber == null ? View("Error") : View(new VerifyPhoneNumberViewModel { PhoneNumber = phoneNumber });
		}

		//
		// POST: /Manage/VerifyPhoneNumber
		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<ActionResult> VerifyPhoneNumber(VerifyPhoneNumberViewModel model)
		{
			if (!ModelState.IsValid)
			{
				return View(model);
			}
			var result = await UserManager.ChangePhoneNumberAsync(User.Identity.GetUserId(), model.PhoneNumber, model.Code);
			if (result.Succeeded)
			{
				var user = await UserManager.FindByIdAsync(User.Identity.GetUserId());
				if (user != null)
				{
					await SignInManager.SignInAsync(user, isPersistent: false, rememberBrowser: false);
				}
				return RedirectToAction("Index", new { Message = ManageMessageId.AddPhoneSuccess });
			}
			// If we got this far, something failed, redisplay form
			ModelState.AddModelError("", "Failed to verify phone");
			return View(model);
		}

		//
		// POST: /Manage/RemovePhoneNumber
		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<ActionResult> RemovePhoneNumber()
		{
			var result = await UserManager.SetPhoneNumberAsync(User.Identity.GetUserId(), null);
			if (!result.Succeeded)
			{
				return RedirectToAction("Index", new { Message = ManageMessageId.Error });
			}
			var user = await UserManager.FindByIdAsync(User.Identity.GetUserId());
			if (user != null)
			{
				await SignInManager.SignInAsync(user, isPersistent: false, rememberBrowser: false);
			}
			return RedirectToAction("Index", new { Message = ManageMessageId.RemovePhoneSuccess });
		}

		//
		// GET: /Manage/ChangePassword
		public ActionResult ChangePassword()
		{
			return View();
		}

		//
		// POST: /Manage/ChangePassword
		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<ActionResult> ChangePassword(ChangePasswordViewModel model)
		{
			if (!ModelState.IsValid)
			{
				return View(model);
			}

			var result = await UserManager.ChangePasswordAsync(User.Identity.GetUserId(), model.OldPassword, model.NewPassword);
			if (result.Succeeded)
			{
				var user = await UserManager.FindByIdAsync(User.Identity.GetUserId());
				if (user != null)
				{
					await SignInManager.SignInAsync(user, isPersistent: false, rememberBrowser: false);
				}
				return RedirectToAction("Index", new { Message = ManageMessageId.ChangePasswordSuccess });
			}

			ModelState.AddModelError("OldPassword", "Невірний пароль");
			return View(model);
		}

		//
		// GET: /Manage/SetPassword
		public ActionResult SetPassword()
		{
			return View();
		}

		//
		// POST: /Manage/SetPassword
		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<ActionResult> SetPassword(SetPasswordViewModel model)
		{
			if (ModelState.IsValid)
			{
				var result = await UserManager.AddPasswordAsync(User.Identity.GetUserId(), model.NewPassword);
				if (result.Succeeded)
				{
					var user = await UserManager.FindByIdAsync(User.Identity.GetUserId());
					if (user != null)
					{
						await SignInManager.SignInAsync(user, isPersistent: false, rememberBrowser: false);
					}
					return RedirectToAction("Index", new { Message = ManageMessageId.SetPasswordSuccess });
				}
				AddErrors(result);
			}

			// If we got this far, something failed, redisplay form
			return View(model);
		}

		//
		// GET: /Manage/ManageLogins
		public async Task<ActionResult> ManageLogins(ManageMessageId? message)
		{
			ViewBag.StatusMessage =
			message == ManageMessageId.RemoveLoginSuccess ? "The external login was removed."
			: message == ManageMessageId.Error ? "An error has occurred."
			: "";
			var user = await UserManager.FindByIdAsync(User.Identity.GetUserId());
			if (user == null)
			{
				return View("Error");
			}
			var userLogins = await UserManager.GetLoginsAsync(User.Identity.GetUserId());
			var otherLogins = AuthenticationManager.GetExternalAuthenticationTypes().Where(auth => userLogins.All(ul => auth.AuthenticationType != ul.LoginProvider)).ToList();
			ViewBag.ShowRemoveButton = user.PasswordHash != null || userLogins.Count > 1;
			return View(new ManageLoginsViewModel
			{
				CurrentLogins = userLogins,
				OtherLogins = otherLogins
			});
		}

		//
		// POST: /Manage/LinkLogin
		[HttpPost]
		[ValidateAntiForgeryToken]
		public ActionResult LinkLogin(string provider)
		{
			// Request a redirect to the external login provider to link a login for the current user
			return new AccountController.ChallengeResult(provider, Url.Action("LinkLoginCallback", "Manage"), User.Identity.GetUserId());
		}

		//
		// GET: /Manage/LinkLoginCallback
		public async Task<ActionResult> LinkLoginCallback()
		{
			var loginInfo = await AuthenticationManager.GetExternalLoginInfoAsync(XsrfKey, User.Identity.GetUserId());
			if (loginInfo == null)
			{
				return RedirectToAction("ManageLogins", new { Message = ManageMessageId.Error });
			}
			var result = await UserManager.AddLoginAsync(User.Identity.GetUserId(), loginInfo.Login);
			return result.Succeeded ? RedirectToAction("ManageLogins") : RedirectToAction("ManageLogins", new { Message = ManageMessageId.Error });
		}

		protected override void Dispose(bool disposing)
		{
			if (disposing && _userManager != null)
			{
				_userManager.Dispose();
				_userManager = null;
			}

			base.Dispose(disposing);
		}

		#endregion

		#region Helpers

		// Used for XSRF protection when adding external logins
		private const string XsrfKey = "XsrfId";

		private IAuthenticationManager AuthenticationManager
		{
			get
			{
				return HttpContext.GetOwinContext().Authentication;
			}
		}

		private void AddErrors(IdentityResult result)
		{
			foreach (var error in result.Errors)
			{
				ModelState.AddModelError("", error);
			}
		}

		private bool HasPassword()
		{
			var user = UserManager.FindById(User.Identity.GetUserId());
			if (user != null)
			{
				return user.PasswordHash != null;
			}
			return false;
		}

		private bool HasPhoneNumber()
		{
			var user = UserManager.FindById(User.Identity.GetUserId());
			if (user != null)
			{
				return user.PhoneNumber != null;
			}
			return false;
		}

		public enum ManageMessageId
		{
			AddPhoneSuccess,
			ChangePasswordSuccess,
			SetTwoFactorSuccess,
			SetPasswordSuccess,
			RemoveLoginSuccess,
			RemovePhoneSuccess,
			Error,
			ChangeDataSuccess,
			ItemsEditedSuccess
		}

		#endregion
	}
}