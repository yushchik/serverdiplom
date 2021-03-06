﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using WebApplication4.Services;
using WebApplication4.Models;
using WebApplication4.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authentication;
using System.Net;
using System.Security.Claims;

namespace WebApplication4.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UserAPIController : Controller
    {
        UserService uS;


        //public UserAPIController(ApplicationDbContext context)
        //{
        //    uS = new UserService(context);

        //}
        private readonly SignInManager<User> _signInManager;
        private readonly ILogger<UserAPIController> _logger;
        private readonly UserManager<User> _userManager;

        public UserAPIController(
            SignInManager<User> signInManager, 
            ILogger<UserAPIController> logger, 
            ApplicationDbContext context, 
            UserManager<User> userManager)
        {
            _signInManager = signInManager;
            _logger = logger;
            uS = new UserService(context);
            _userManager = userManager;
        }

        // GET: api/<controller>
        [HttpGet]
        
        public JsonResult Get()
        {
            return Json(uS.getAllUsers());
        }

        //НАЙТИ ПОЛЬЗОВАТЕЛЯ ПО ЛОГИНУ
        // GET: api/<controller>/id
        [HttpGet("{userLogin}")]
        public JsonResult Get(string userLogin)
        {
            return Json(uS.getUserByLogin(userLogin));
        }

        //Авторизация
        // GET: api/<controller>/password
        [HttpPost("login/")]
        public async Task<JsonResult> Post([FromBody]Login login)
        {
            var result = await _signInManager.PasswordSignInAsync(login.UserName, login.PasswordHash, true, lockoutOnFailure: true);
           
            if (result.Succeeded)
            {
                _logger.LogInformation("User logged in.");
                User user = uS.getUserByLogin(login.UserName);
               
                await _userManager.RemoveAuthenticationTokenAsync(user, "Default", "passwordless-auth");
                var newRefreshToken = _userManager.GenerateUserTokenAsync(user, "Default", "passwordless-auth");
                
                await _userManager.SetAuthenticationTokenAsync(user, "Default", "passwordless-auth", await newRefreshToken);
                return Json(newRefreshToken.Result);
                //return Json(newRefreshToken);
            }
            else
            {
                ModelState.AddModelError(string.Empty, "Invalid login attempt.");
               
                this.HttpContext.Response.StatusCode = 404; 
               
                return Json("Invalid login attempt.");
            }
           

            //if (result.IsLockedOut)
            //{

            //    _logger.LogWarning("User account locked out.");
            //    //return "User account locked out.";
            //    return Json("User account locked out.");
            //}
            //else
            //{
            //    ModelState.AddModelError(string.Empty, "Invalid login attempt.");
            //    //return "Invalid login attempt.";
            //    return Json("Invalid login attempt.");
            //}

            //uS.ChangePassword(password, login);
            //uS.Save();
            //return Json(uS.getUserByLogin(login));
        }

        //Регистрация
        [HttpPost("registration/")]
        public async Task<JsonResult> Post([FromBody]User user)
        {
            if (ModelState.IsValid)
            {
                var newuser = new User
                {
                    UserName = user.Email,
                    Email = user.Email,
                    SURNAME = user.SURNAME,
                    NAME = user.NAME,
                    CITY = user.CITY,
                    BIRTHDAY = user.BIRTHDAY
                };
                var result = await _userManager.CreateAsync(user, user.PasswordHash);
                if (result.Succeeded)
                {
                    _logger.LogInformation("User created a new account with password.");
                     await _signInManager.SignInAsync(user, isPersistent: false);
                    string uId = uS.getUserId(user.UserName);                  
                    var newRefreshToken = _userManager.GenerateUserTokenAsync(user, "Default", "passwordless-auth");
                    await _userManager.SetAuthenticationTokenAsync(user, "Default", "passwordless-auth", await newRefreshToken);
                    return Json(newRefreshToken.Result);
                    //return Json(uId);
                }
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                    this.HttpContext.Response.StatusCode = 404;

                    return Json("Invalid login attempt.");
                }
            }
            this.HttpContext.Response.StatusCode = 405;

            return Json("Invalid user attempt.");
        }
    }
}