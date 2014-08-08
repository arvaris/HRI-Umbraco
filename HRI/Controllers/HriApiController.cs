﻿using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Web;
using System.Web.Mvc;
using System.Web.Script.Serialization;
using Umbraco.Web.Models;
using Umbraco.Web.WebApi;
using System.Net.Http;
using Umbraco.Core.Models;

namespace HRI.Controllers
{
    public class HriApiController : UmbracoApiController
    {
        /// <summary>
        /// Checks to see if a username is available using the HRI API
        /// </summary>
        /// <param name="username"></param>
        /// <returns></returns>
        [System.Web.Http.AcceptVerbs("GET", "POST")]
        public bool IsUserNameAvailable(string username)
        {
            // Get ahold of the root/home node
            IPublishedContent root = Umbraco.ContentAtRoot().First();
            // Get the API uri
            string apiUri = root.GetProperty("apiUri").Value.ToString();
            // Apend the command to determine user availability
            string userNameCheckApiString = apiUri + "/Registration?isUserNameAvailable=" + username;
            // Create a JSON object to hold the response 
            JObject json;
            // Create a web client to access the API
            using(var client = new WebClient())
            {
                // Set the format to JSON
                client.Headers[HttpRequestHeader.ContentType] = "application/json";
                // Execute a GET and get the response as a JSON object
                json = JObject.Parse(client.DownloadString(userNameCheckApiString));
            }
            // Return whether or not it is available
            return Convert.ToBoolean(json["isAvailable"]);
        }

        public bool GetRegisteredUserByUsername(string username, out RegisterModel registerModel)
        {
            // Get ahold of the root/home node
            IPublishedContent root = Umbraco.ContentAtRoot().First();
            // Get the API uri
            string apiUri = root.GetProperty("apiUri").Value.ToString();
            // Apend the command to determine user exists
            string userNameCheckApiString = apiUri + "/Registration?userName=" + username;
            string response;
            JObject json;

            using (var client = new WebClient())
            {
                // Set the format to JSON
                client.Headers[HttpRequestHeader.ContentType] = "application/json";
                try
                {
                    response = client.DownloadString(userNameCheckApiString);
                    json = JObject.Parse(response);
                }
                catch (WebException ex)
                {
                    registerModel = null;
                    return false;
                }
            }
            registerModel = null;
            return true;
        }

        /// <summary>
        /// Registers a user with the HRI web API
        /// </summary>
        /// <param name="username"></param>
        /// <returns></returns>
        [System.Web.Http.AcceptVerbs("GET", "POST")]
        public bool RegisterUser(string userName)
        {
            // Get an instance of the member
            var member = Services.MemberService.GetByUsername(userName);
            // Create a dictionary of values that we will convert to JSON and send
            Dictionary<string, string> jsonData = new Dictionary<string, string>();
            jsonData.Add("RegId", null);
            jsonData.Add("RegDate", DateTime.Now.ToString());
            jsonData.Add("MemberId", null);
            jsonData.Add("UserName", member.Username);
            jsonData.Add("FirstName", member.GetValue("firstName").ToString());
            jsonData.Add("LastName", member.GetValue("lastName").ToString());
            jsonData.Add("Ssn", member.GetValue("ssn").ToString());
            jsonData.Add("EMail", member.Email);
            jsonData.Add("ZipCode", member.GetValue("zipCode").ToString());
            jsonData.Add("PhoneNumber", member.GetValue("phoneNumber").ToString());
            jsonData.Add("RegVerified", "true");
            // Convert the dictionary to JSON
            string myJsonString = (new JavaScriptSerializer()).Serialize(jsonData);

            // Get ahold of the root/home node
            IPublishedContent root = Umbraco.ContentAtRoot().First();
            // Get the API uri
            string apiUri = root.GetProperty("apiUri").Value.ToString();
            // Apend the command to invoke the register function
            string registerUserApi = apiUri + "/Registration";                                   
            
            // Create a JSON object to hold the response
            JObject json;
            string response;
            // Create a webclient object to post the user data
            using(var client = new WebClient())
            {
                // Set the format to JSON
                client.Headers[HttpRequestHeader.ContentType] = "application/json";
                // Execute a GET and get the response as a JSON object
                
                // Get the response when posting the member
                try
                {
                    json = JObject.Parse(client.UploadString(registerUserApi, myJsonString));
                }
                catch(WebException ex)
                {                    
                    return false;
                }
            }

            // If the user was created
            if (json["MemberId"] != null)
            {
                // Assign this user their member id
                var temp = member.GetValue("memberId");
                member.SetValue("memberId", json["RegId"]);                
                // Assign their Morneau Shapell Y Number
                Services.MemberService.GetByUsername(member.Username).SetValue("yNumber", json["MemberId"]);
                // Return successful registration
                return true;
            }

            // Member was not registered with HRI; return false
            return false;
        }
    }
}