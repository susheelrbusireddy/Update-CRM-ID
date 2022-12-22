using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Okta.Sdk.Api;
using Okta.Sdk.Client;
using Okta.Sdk.Model;
using System.Configuration;
using log4net;
using log4net.Config;
using System.Reflection;

namespace UpdateCRM_ID
{
    class Program
    {
        private static readonly string strOktaUrl = ConfigurationManager.AppSettings["oktaUrl"];
        private static readonly string strOktaApiKey = ConfigurationManager.AppSettings["oktaApiKey"];
        private static readonly string csvPath = ConfigurationManager.AppSettings["csvPath"];
        private static readonly string attributeName = ConfigurationManager.AppSettings["attributeName"];
        private static readonly ILog logger = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        static async Task Main(string[] args)
        {
            var logRepository = LogManager.GetRepository(Assembly.GetEntryAssembly());
            XmlConfigurator.Configure(logRepository, new FileInfo("log4net.config"));

            try
            {
                List<string> csvDataList = File.ReadAllLines(@csvPath).Skip(1).ToList();

                Okta.Sdk.Client.Configuration config = new Okta.Sdk.Client.Configuration();
                config.OktaDomain = strOktaUrl;
                config.Token = strOktaApiKey;

                var userApi = new UserApi(config);

                int totalusers = 0;

                foreach (string row in csvDataList)
                {   
                    string[] values = row.Split(",");
                    logger.Info("******************* CRM ID : " + values[0].ToString() + " *******************");
                    int i = 0;
                    var foundUsers = await userApi
                            .ListUsers(search: $"profile." + attributeName + " eq \"" + values[0].ToString() + "\"")
                            .ToArrayAsync();

                    if (foundUsers.Length > 0)
                    {
                        foreach (User user in foundUsers)
                        {
                            user.Profile.AdditionalProperties = new Dictionary<string, object>();
                            user.Profile.AdditionalProperties[attributeName] = values[1];

                            var updateUserRequest = new UpdateUserRequest
                            {
                                Profile = user.Profile
                            };
                            var updatedUser = await userApi.UpdateUserAsync(user.Id, updateUserRequest);
                            i++;
                            logger.Info("CRM ID updated for : " + updatedUser.Profile.Login);
                        }
                    }
                    logger.Info("CRM ID : " + values[0].ToString() + " updated for " + i + " users ");
                    Console.WriteLine("CRM ID : " + values[0].ToString() + " updated for " + i + " users ");
                    totalusers += i;
                    logger.Info("***************************************************");
                }
                logger.Info("Total users updated : " + totalusers);
                Console.WriteLine("Total users updated : " + totalusers);
            }
            catch(Exception ex)
            {
                logger.Error(ex.Message);
                Console.WriteLine(ex.Message);
            }            
        }
    }
}
