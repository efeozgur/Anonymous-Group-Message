﻿using Agm.Hubs;
using Agm.Models.Class;
using Agm.Models.EntityFramework;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace Agm.Controllers
{
    public class TextMessageController : Controller
    {
        AgmEntities db = new AgmEntities();
        public ActionResult Index(int id)
        {
            var user = Session["User"] as Users;
            if (user == null)
            {
                return RedirectToAction("Login", "Home");
            }
            var group = db.Groups.FirstOrDefault(x => x.groupId == id);
            if (group == null)
            {
                return RedirectToAction("SendMessage", "TextMessage");
            }
            
            return View();
        }
        public JsonResult Get(int id)
        {
            if (id > 0) {
                using (var connection = new SqlConnection(ConfigurationManager.ConnectionStrings["MsgConnection"].ConnectionString))
                {
                    connection.Open();
                    using (SqlCommand command = new SqlCommand(@"SELECT [textId],[textOwner],[textContent],[groupFk], convert(varchar(25), [textDate], 120) as textDate from [dbo].[TextMessage] where groupFk=" + id +"order by textId desc", connection))
                    {

                        command.Notification = null;

                        SqlDependency dependency = new SqlDependency(command);
                        dependency.OnChange += new OnChangeEventHandler(dependency_OnChange);

                        if (connection.State == ConnectionState.Closed)
                            connection.Open();

                        SqlDataReader reader = command.ExecuteReader();

                        var listMsg = reader.Cast<IDataRecord>()
                                .Select(x => new
                                {
                                 
                                    textOwner = (string)x["textOwner"],
                                    textContent = (string)x["textContent"],
                                    textDate = x["textDate"].ToString()

                                }).ToList();

                        return Json(new { listMsg = listMsg }, JsonRequestBehavior.AllowGet);
                    }
                }
            }
            return new JsonResult();
        }
        public void dependency_OnChange(object sender, SqlNotificationEventArgs e)
        {
            MsgHub.Show();
        }
        public ActionResult SendMessage()
        {
            var user = Session["User"] as Users;
            if (user == null)
            {
                return RedirectToAction("Login", "Home");
            }
            var result = db.spUserGroup(user.userId).ToList();
            if (result.Count != 0)
            {
                var groupList = new List<groupsModel>();
                foreach (var group in result)
                {
                    var gModel = new groupsModel();
                    gModel.groupId = group.groupId;
                    gModel.groupName = group.groupName;
                    gModel.groupImageUrl = group.groupImageUrl;
                    groupList.Add(gModel);
                }
                return View(groupList);
            }
            else
            {
                ViewBag.NoResult = "Henüz mesaj yazmak için herhangi bir grubunuz yok.";
            }

            return View();
        }
        public ActionResult Create(int id) {

            var group = db.Groups.FirstOrDefault(x => x.groupId == id);
            ViewBag.GroupImage = group.groupImageUrl;
            ViewBag.GroupName = group.groupName;
            return View();
        }

        [HttpPost]
        public ActionResult Create(int id ,textMessageModel tModel)
        {
            var user = Session["User"] as Users;
            var group = db.Groups.FirstOrDefault(x => x.groupId == id);
            var message = new TextMessage();
            message.groupFk = group.groupId;
            message.textOwner = user.userNameSurname;
            message.groupFk = group.groupId;
            DateTime date = DateTime.Now;
            message.textDate = date;
            message.userFk = user.userId;
            message.textContent = tModel.textContent;
            db.TextMessage.Add(message);
            db.SaveChanges();
            return View("Index",id);
        }
    }
}