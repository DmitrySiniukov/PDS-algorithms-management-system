using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using Enterprise.Models;
using Microsoft.AspNet.Identity.EntityFramework;

namespace Enterprise.Infrastructure
{
    /// <summary>
    /// 
    /// </summary>
    public class Repository
    {
        private static string _connectionString;

        public static string DefaultConnection
        {
            get
            {
                if (string.IsNullOrEmpty(_connectionString))
                {
                    _connectionString = ConfigurationManager.ConnectionStrings["DefaultConnection"].ConnectionString;
                }
                return _connectionString;
            }
        }

        public static List<IdentityRole<string, IdentityUserRole>> GetAvailableRoles()
        {
            var result = new List<IdentityRole<string, IdentityUserRole>>();

            using (var connection = new SqlConnection(DefaultConnection))
            {
                connection.Open();
                using (var cmd = new SqlCommand())
                {
                    cmd.CommandText = @"select Id, Name from AspNetRoles";
                    cmd.Connection = connection;
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            result.Add(new IdentityRole()
                            {
                                Id = reader.GetFieldValue<string>(0),
                                Name = reader.GetFieldValue<string>(1)
                            });
                        }
                    }
                }
                connection.Close();
            }
            return result;
        }

        public static List<Item> GetItems(Item instance)
        {
            var result = new List<Item>();

            using (var connection = new SqlConnection(DefaultConnection))
            {
                connection.Open();
                using (var cmd = new SqlCommand())
                {
                    var text = instance is Machine
                        ? @"select p.Id, p.Name, p.Description, p.C_Date, p.C_User, u.UserName, d.Id, d.Name from {0}s p
							left join AspNetUsers u on p.C_User = u.Id left join Departments d on d.Id = p.DepartmentId"
                        : instance is Product
                            ? @"select p.Id, p.Name, p.Description, p.C_Date, p.C_User, u.UserName, p.Deadline from {0}s p
							left join AspNetUsers u on p.C_User = u.Id"
                            : @"select p.Id, p.Name, p.Description, p.C_Date, p.C_User, u.UserName from {0}s p
							left join AspNetUsers u on p.C_User = u.Id";
                    cmd.CommandText = string.Format(text, instance.InheritorName);

                    cmd.Connection = connection;
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            var item = instance.Create(
                                reader.GetFieldValue<int>(0),
                                reader.GetFieldValue<string>(1),
                                reader.GetFieldValue<string>(2),
                                reader.IsDBNull(3) ? DateTime.MinValue : reader.GetFieldValue<DateTime>(3),
                                reader.IsDBNull(4) ? string.Empty : reader.GetFieldValue<string>(4),
                                reader.IsDBNull(5) ? string.Empty : reader.GetFieldValue<string>(5)
                                );
                            if (instance is Machine && !reader.IsDBNull(6))
                            {
                                var machine = (Machine)item;
                                machine.DepartmentId = reader.GetFieldValue<int>(6);
                                machine.DepartmentName = reader.GetFieldValue<string>(7);
                            }
                            if (instance is Product && !reader.IsDBNull(6))
                            {
                                var product = (Product)item;
                                product.Deadline = reader.GetFieldValue<DateTime>(6);
                            }
                            result.Add(item);
                        }
                    }
                }
                connection.Close();
            }
            return result;
        }

        public static bool DeleteItem(int id, string inheritorName)
        {
            var result = false;
            using (var connection = new SqlConnection(DefaultConnection))
            {
                connection.Open();
                using (var cmd = new SqlCommand())
                {
                    cmd.CommandText = string.Format(@"delete {0}s where Id = @idParam", inheritorName);
                    cmd.Connection = connection;
                    cmd.Parameters.AddWithValue("idParam", id);
                    if (cmd.ExecuteNonQuery() > 0)
                    {
                        result = true;
                    }
                }
                connection.Close();
            }
            return result;
        }

        public static void UpdateItems(IEnumerable<Item> items)
        {
            var itemsArray = (items as List<Item>) ?? items.ToList();
            if (itemsArray.Count == 0)
            {
                return;
            }

            var instance = itemsArray[0];
            var isMachine = instance is Machine;
            var isProduct = instance is Product;
            var stringBuilder = new StringBuilder();

            using (var connection = new SqlConnection(DefaultConnection))
            {
                connection.Open();
                using (var cmd = new SqlCommand())
                {
                    stringBuilder.AppendLine("BEGIN TRANSACTION UpdateItems");
                    var i = 0;
                    foreach (var p in itemsArray)
                    {
                        ++i;
                        var nameParam = string.Format("nameParam{0}", i);
                        var descriptionParam = string.Format("descriptionParam{0}", i);
                        var idParam = string.Format("idParam{0}", i);
                        var departmentParam = string.Empty;
                        if (isMachine)
                        {
                            departmentParam = string.Format("departmentParam{0}", i);
                            var id = ((Machine)p).DepartmentId;
                            cmd.Parameters.AddWithValue(departmentParam, id == 0 ? (object)DBNull.Value : id);
                        }
                        var deadlineParam = string.Empty;
                        if (isProduct)
                        {
                            deadlineParam = string.Format("deadlineParam{0}", i);
                            var deadline = ((Product) p).Deadline;
                            cmd.Parameters.AddWithValue(deadlineParam, deadline ?? (object) DBNull.Value);
                        }

                        stringBuilder.AppendLine(
                            string.Format("update {0}s set Name = @{1}, Description = @{2}{3} where Id = @{4}",
                                instance.InheritorName,
                                nameParam, descriptionParam,
                                isMachine
                                    ? string.Format(", DepartmentId = @{0}", departmentParam)
                                    : isProduct
                                        ? string.Format(", Deadline = @{0}", deadlineParam)
                                        : string.Empty,
                                idParam));
                        cmd.Parameters.AddRange(new[]
                        {
                            new SqlParameter(nameParam, p.Name ?? string.Empty),
                            new SqlParameter(descriptionParam, p.Description ?? string.Empty),
                            new SqlParameter(idParam, p.Id)
                        });
                    }
                    stringBuilder.AppendLine("COMMIT TRANSACTION UpdateItems");
                    cmd.CommandText = stringBuilder.ToString();
                    cmd.Connection = connection;
                    cmd.ExecuteNonQuery();
                }
                connection.Close();
            }
        }

        public static void CreateItem(Item item, string userId)
        {
            var machine = item as Machine;
            var product = item as Product;
            using (var connection = new SqlConnection(DefaultConnection))
            {
                connection.Open();
                using (var cmd = new SqlCommand())
                {
                    cmd.CommandText = string.Format(
                        @"insert {0}s (Name, Description, C_Date, C_User{1}) values (@nameParam, @descrParam, @dateParam, @userParam{2})",
                        item.InheritorName,
                        machine != null ? ", DepartmentId"
                        : product != null ? ", Deadline"
                        : string.Empty,
                        machine != null ? ", @departmentParam"
                        : product != null ? ", @deadlineParam"
                        : string.Empty);
                    cmd.Connection = connection;
                    cmd.Parameters.AddWithValue("nameParam", item.Name);
                    cmd.Parameters.AddWithValue("descrParam", item.Description ?? string.Empty);
                    cmd.Parameters.AddWithValue("dateParam", DateTime.Now);
                    cmd.Parameters.AddWithValue("userParam", userId);

                    if (machine != null)
                    {
                        cmd.Parameters.AddWithValue("departmentParam", machine.DepartmentId == 0 ? (object)DBNull.Value : machine.DepartmentId);
                    }
                    if (product != null)
                    {
                        cmd.Parameters.AddWithValue("deadlineParam", product.Deadline ?? (object) DBNull.Value);
                    }
                    cmd.ExecuteNonQuery();
                }
                connection.Close();
            }
        }

        public static List<ScheduleTask> GetScheduleTasks()
        {
            var result = new List<ScheduleTask>();

            using (var connection = new SqlConnection(DefaultConnection))
            {
                connection.Open();
                using (var cmd = new SqlCommand())
                {
                    cmd.CommandText =
                        @"select tc.Id, tc.Duration, p.Id, p.Name, p.Deadline, t.Id, t.Name, tc.Description, c.DepartmentId
                            from Technologies tc
                            inner join Tasks t on tc.TaskId = t.Id
                            inner join Products p on p.Id = tc.ProductId
                            inner join Compatibilities c on c.TaskId = tc.TaskId";

                    cmd.Connection = connection;
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            var technologyId = reader.GetFieldValue<int>(0);
                            var item = result.Find(x => x.TechnologyId == technologyId);
                            if (item == null)
                            {
                                item = new ScheduleTask(technologyId,
                                    reader.GetFieldValue<double>(1),
                                    reader.GetFieldValue<int>(2),
                                    reader.GetFieldValue<string>(3),
                                    reader.GetFieldValue<DateTime>(4),
                                    reader.GetFieldValue<int>(5),
                                    reader.GetFieldValue<string>(6),
                                    reader.GetFieldValue<string>(7)
                                    );
                                result.Add(item);
                            }
                            item.CompatibleDepartments.Add(reader.GetFieldValue<int>(8));
                        }
                    }
                }
                connection.Close();
            }
            return result;
        }

        public static Technologies GetTechnologies(int productId)
        {
            var result = new Technologies();
            result.ProductId = productId;
            using (var connection = new SqlConnection(DefaultConnection))
            {
                connection.Open();
                using (var cmd = new SqlCommand())
                {
                    cmd.CommandText = string.Format(
                        @"select Id, TaskId, Description, Duration from Technologies where ProductId = @idParam");
                    cmd.Parameters.AddWithValue("idParam", productId);
                    cmd.Connection = connection;
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            result.Add(new Technology()
                            {
                                Id = reader.GetFieldValue<int>(0),
                                TaskId = reader.GetFieldValue<int>(1),
                                Description = reader.GetFieldValue<string>(2),
                                Duration = reader.GetFieldValue<double>(3),
                                ProductId = productId
                            });
                        }
                    }
                }
                using (var cmd = new SqlCommand())
                {
                    cmd.CommandText = @"select Name from Products where Id = @idParam";
                    cmd.Parameters.AddWithValue("idParam", productId);
                    cmd.Connection = connection;
                    using (var reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            result.ProductName = reader.GetFieldValue<string>(0);
                        }
                    }
                }
                connection.Close();
            }
            return result;
        }

        public static void UpdateTechnologies(IEnumerable<Technology> technologies)
        {
            var stringBuilder = new StringBuilder();
            using (var connection = new SqlConnection(DefaultConnection))
            {
                connection.Open();
                using (var cmd = new SqlCommand())
                {
                    stringBuilder.AppendLine("BEGIN TRANSACTION UpdateTechnologies");
                    var i = 0;
                    foreach (var t in technologies)
                    {
                        ++i;
                        var taskParam = string.Format("taskParam{0}", i);
                        var descriptionParam = string.Format("descriptionParam{0}", i);
                        var durationParam = string.Format("durationParam{0}", i);
                        var idParam = string.Format("idParam{0}", i);

                        stringBuilder.AppendLine(string.Format("update Technologies set TaskId = @{0}, Description = @{1}, Duration = @{2} where Id = @{3}",
                            taskParam, descriptionParam, durationParam, idParam));
                        cmd.Parameters.AddRange(new[]
                        {
                            new SqlParameter(taskParam, t.TaskId),
                            new SqlParameter(descriptionParam, t.Description ?? string.Empty),
                            new SqlParameter(idParam, t.Id),
                            new SqlParameter(durationParam, t.Duration)
                        });
                    }
                    stringBuilder.AppendLine("COMMIT TRANSACTION UpdateTechnologies");
                    cmd.CommandText = stringBuilder.ToString();
                    cmd.Connection = connection;
                    cmd.ExecuteNonQuery();
                }
                connection.Close();
            }
        }

        public static void CreateTechnology(Technology technology)
        {
            using (var connection = new SqlConnection(DefaultConnection))
            {
                connection.Open();
                using (var cmd = new SqlCommand())
                {
                    cmd.CommandText = @"insert Technologies (ProductId, TaskId, Description, Duration) values (@productParam, @taskParam, @descrParam, @durParam)";
                    cmd.Connection = connection;
                    cmd.Parameters.AddWithValue("productParam", technology.ProductId);
                    cmd.Parameters.AddWithValue("taskParam", technology.TaskId);
                    cmd.Parameters.AddWithValue("descrParam", technology.Description ?? string.Empty);
                    cmd.Parameters.AddWithValue("durParam", technology.Duration);
                    cmd.ExecuteNonQuery();
                }
                connection.Close();
            }
        }

        public static bool DeleteTechnology(int id)
        {
            var result = false;
            using (var connection = new SqlConnection(DefaultConnection))
            {
                connection.Open();
                using (var cmd = new SqlCommand())
                {
                    cmd.CommandText = "delete Technologies where Id = @idParam";
                    cmd.Connection = connection;
                    cmd.Parameters.AddWithValue("idParam", id);
                    if (cmd.ExecuteNonQuery() > 0)
                    {
                        result = true;
                    }
                }
                connection.Close();
            }
            return result;
        }

        public static List<Compatibility> GetCompatibilities(int departmentId)
        {
            var result = new List<Compatibility>();
            using (var connection = new SqlConnection(DefaultConnection))
            {
                connection.Open();
                using (var cmd = new SqlCommand())
                {
                    cmd.CommandText = @"select t.Id, t.Name, t.Description, t.C_Date, t.C_User, u.UserName, c.Id from Tasks t
                                            left join AspNetUsers u on t.C_User = u.Id
                                            left join Compatibilities c on c.TaskId = t.Id and c.DepartmentId = @departmentParam";
                    cmd.Parameters.AddWithValue("departmentParam", departmentId);
                    cmd.Connection = connection;
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            var task = new Task(reader.GetFieldValue<int>(0), reader.GetFieldValue<string>(1),
                                reader.GetFieldValue<string>(2), reader.GetFieldValue<DateTime>(3),
                                null, string.Empty);
                            if (!reader.IsDBNull(4))
                            {
                                task.CreationUserId = reader.GetFieldValue<string>(4);
                                task.CreationUserLogin = reader.GetFieldValue<string>(5);
                            }
                            result.Add(new Compatibility
                            {
                                Task = task,
                                IsCompatible = !reader.IsDBNull(6)
                            });
                        }
                    }
                }
                connection.Close();
            }
            return result;
        }

        public static void ChangeCompatibility(int departmentId, int taskId, bool compatible)
        {
            using (var connection = new SqlConnection(DefaultConnection))
            {
                connection.Open();
                using (var cmd = new SqlCommand())
                {
                    cmd.CommandText = compatible
                        ? "insert Compatibilities (DepartmentId, TaskId) values (@departmentParam, @taskParam)"
                        : "delete Compatibilities where DepartmentId = @departmentParam and TaskId = @taskParam";
                    cmd.Connection = connection;
                    cmd.Parameters.AddWithValue("departmentParam", departmentId);
                    cmd.Parameters.AddWithValue("taskParam", taskId);
                    cmd.ExecuteNonQuery();
                }
                connection.Close();
            }
        }

        public static Department GetDepartment(int id)
        {
            Department result = null;
            using (var connection = new SqlConnection(DefaultConnection))
            {
                connection.Open();
                using (var cmd = new SqlCommand())
                {
                    cmd.CommandText = @"select d.Name, d.Description, d.C_Date, d.C_User, u.UserName from Departments d
                                            left join AspNetUsers u on d.C_User = u.Id where d.Id = @idParam";
                    cmd.Parameters.AddWithValue("idParam", id);
                    cmd.Connection = connection;
                    using (var reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            result = new Department(id, reader.GetFieldValue<string>(0), reader.GetFieldValue<string>(1),
                                reader.GetFieldValue<DateTime>(2), null, string.Empty);
                            if (!reader.IsDBNull(3))
                            {
                                result.CreationUserId = reader.GetFieldValue<string>(3);
                                result.CreationUserLogin = reader.GetFieldValue<string>(4);
                            }
                        }
                    }
                }
                connection.Close();
            }
            return result;
        }
    }
}