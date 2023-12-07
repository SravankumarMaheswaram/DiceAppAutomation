using CsvHelper;
using CsvHelper.Configuration;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Edge;
using OpenQA.Selenium.Support.UI;
using SeleniumExtras.WaitHelpers;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Globalization;
using System.IO;
using System.Linq;

namespace DiceApplicationAutos
{
    class Program
    {
        static IWebDriver driver;

        static void Main(string[] args)
        {


            List<string> Urls;
            List<string> rcmdUrls = new List<string>();
            int prssdurlcnt = 0;

            /*--------------------------------------------------------*/

            //ChromeOptions options = new ChromeOptions();
            //options.AddArguments("--disable-notifications");
            ////System.Environment.SetEnvironmentVariable("webdriver.chrome.driver", ConfigurationManager.AppSettings.Get("driverpath"));
            //WebDriver driver = new ChromeDriver(options);
            //ChromeOptions options = new ChromeOptions();
            //options.AddArgument("--disable-notifications");
            //options.AddArguments("--disable-extensions"); // to disable extension
            //options.AddArguments("--disable-notifications"); // to disable notification
            //options.AddArguments("--disable-application-cache"); // to disable cache
            //options.AddArgument("--safebrowsing-disable-download-protection");
            //options.AddArgument("ignore-certificate-errors");
            //options.AddArgument("--disable-popup-blocking");
            //options.AddArgument("--disable-gpu");
            //options.AddArgument("--incognito");
            //options.AddUserProfilePreference("disable-popup-blocking", "true");
            //driver = new ChromeDriver(ConfigurationManager.AppSettings.Get("driverpath"), options);


            /*--------------------------------------------------------*/
            #region browser driver selection
            if (ConfigurationManager.AppSettings.Get("driver").ToString().Equals("chrome"))
                driver = new ChromeDriver(ConfigurationManager.AppSettings.Get("driverpath"));
            if (ConfigurationManager.AppSettings.Get("driver").ToString().Equals("edge"))
                driver = new EdgeDriver(ConfigurationManager.AppSettings.Get("edgedriverpath"));
            #endregion

            #region url to go and login details
            driver.Url = "https://www.dice.com/dashboard/login/";
            System.Threading.Thread.Sleep(5000);
            driver.Manage().Window.Maximize();

            driver.FindElement(By.Id("email")).SendKeys(ConfigurationManager.AppSettings.Get("email")); // Replace "username" with the actual username field ID
            driver.FindElement(By.Id("password")).SendKeys(ConfigurationManager.AppSettings.Get("password")); // Replace "password" with the actual password field ID
            driver.FindElement(By.TagName("button")).Click(); // Replace "login-button" with the actual login button ID


            System.Threading.Thread.Sleep(2000);


            #endregion


            #region search and increse page count to 100

            string[] searchkeys = ConfigurationManager.AppSettings.Get("searchkeys").ToString().Split("|");
            foreach (string searchkey in searchkeys)
            {

                driver.Url = "https://www.dice.com/jobs";
                System.Threading.Thread.Sleep(2000);
                driver.FindElement(By.Id("typeaheadInput")).Clear();
                driver.FindElement(By.Id("typeaheadInput")).SendKeys(searchkey); // Replace "username" with the actual username field ID
                driver.FindElement(By.Id("submitSearch-button")).Click(); // Replace "password" with the actual password field ID

                System.Threading.Thread.Sleep(5000);

                //select page size to 100
                IWebElement selectElement = driver.FindElement(By.Id("pageSize_2"));
                SelectElement select = new SelectElement(selectElement);
                select.SelectByValue("100");
                System.Threading.Thread.Sleep(5000);
                #endregion

                //get the URLS for .net or c# jobs and process jobs
                Urls = new List<string>();
                
                //List<string> rtnurls = new List<string>();
                // navigation to 100 elements
                int navintValue = 1;
                string filepath = ConfigurationManager.AppSettings.Get("csvfilepath") + ConfigurationManager.AppSettings.Get("filename");
                for (int i = 0; i < 50; i++)
                {
                    try
                    {
                        IWebElement ulElement = driver.FindElement(By.ClassName("pagination"));
                        IReadOnlyCollection<IWebElement> liElements = ulElement.FindElements(By.TagName("li"));
                        foreach (IWebElement liElement in liElements)
                        {
                            IWebElement aElement = liElement.FindElement(By.TagName("a"));
                            string text = aElement.Text;
                            if (int.TryParse(text, out int numericValue) && navintValue == numericValue)
                            {
                                aElement.Click(); // Click the option
                                System.Threading.Thread.Sleep(5000);

                                IReadOnlyCollection<IWebElement> anchorElements = driver.FindElements(By.TagName("a"));
                                List<string> urls = new List<string>();
                                List<string> windowHandles;

                                foreach (IWebElement anchorElement in anchorElements)
                                {
                                    try
                                    {
                                        //string href = anchorElement.GetAttribute("href");
                                        string linkText = anchorElement.Text;
                                        string[] keywords = ConfigurationManager.AppSettings.Get("rcmdkeys").Split(",");
                                        bool keywordexist = false;
                                        foreach (var keyword in keywords)
                                        {
                                            if (((!string.IsNullOrWhiteSpace(linkText) && linkText.ToString().ToUpper().Contains(keyword.ToUpper()))))
                                            {
                                                keywordexist = true;
                                                break;
                                            }
                                            else
                                            {
                                                keywordexist = false;
                                            }
                                        }
                                        if (!keywordexist)
                                            continue;

                                        //clear all tabs except this link


                                        anchorElement.Click();
                                        windowHandles = new List<string>();
                                        windowHandles = driver.WindowHandles.ToList();


                                        driver.SwitchTo().Window(windowHandles[1]);
                                        System.Threading.Thread.Sleep(5000);

                                        // Perform actions in the new tab
                                        prssdurlcnt++;
                                        List<string> prssdUrls = ReadCsv(filepath);
                                        Console.WriteLine("----------------------------------------------------------------------");
                                        Console.WriteLine("Processed Count: " + prssdurlcnt.ToString());
                                        Console.WriteLine("----------------------------------------------------------------------");
                                        //if (prssdUrls != null && prssdUrls.Count > 0)
                                        //{
                                        //    if (prssdUrls.Any(a => a.Equals(driver.Url)))
                                        //        continue;
                                        //}
                                        try
                                        {
                                            System.Threading.Thread.Sleep(5000);

                                            WriteCsv(filepath, driver.Url);
                                            //Easy Apply button
                                            WebDriverWait wait = new WebDriverWait(driver, TimeSpan.FromSeconds(10));
                                            //IWebElement parentButton = driver.FindElement(By.CssSelector("apply-button-wc.ml-4.flex-auto.md\\:flex-initial.hydrated"));
                                            IWebElement parentButton = driver.FindElement(By.Id("applyButton"));

                                            parentButton.Click();
                                            System.Threading.Thread.Sleep(5000);
                                        }
                                        catch (Exception ex)
                                        {
                                            Console.WriteLine("Exception at Easy Apply button: " + ex.Message);
                                            driver.Close();
                                            driver.SwitchTo().Window(windowHandles[0]);
                                            continue;
                                        }

                                        try
                                        {
                                            //next button
                                            IWebElement element = driver.FindElement(By.CssSelector("button.btn.btn-primary.btn-next.btn-block"));
                                            element.Click();
                                            System.Threading.Thread.Sleep(5000);
                                        }
                                        catch (Exception ex)
                                        {
                                            Console.WriteLine("Exception at next button: " + ex.Message);
                                            driver.Close();
                                            try
                                            {
                                                closeTabs();
                                                //driver.SwitchTo().Window(windowHandles[1]);
                                                //System.Threading.Thread.Sleep(500);
                                                //driver.Close();
                                                //driver.SwitchTo().Window(windowHandles[2]);
                                                //System.Threading.Thread.Sleep(500);
                                                //driver.Close();
                                            }
                                            catch { }
                                            driver.SwitchTo().Window(windowHandles[0]);
                                            continue;
                                        }

                                        try
                                        {
                                            //apply button
                                            IWebElement ApplyButton1 = driver.FindElement(By.CssSelector("button.btn.btn-primary.btn-next.btn-split"));
                                            ApplyButton1.Click();
                                            System.Threading.Thread.Sleep(5000);
                                        }
                                        catch (Exception ex)
                                        {
                                            Console.WriteLine("Exception at apply button: " + ex.Message);
                                            driver.Close();
                                            driver.SwitchTo().Window(windowHandles[0]);
                                            continue;
                                        }
                                        // close current tab and Switch back to the original tab
                                        driver.Close();
                                        driver.SwitchTo().Window(windowHandles[0]);
                                    }
                                    catch (Exception ex)
                                    {
                                        Console.WriteLine("Exception at looping: " + ex.Message);
                                        //continue;
                                    }
                                }
                                navintValue++;
                            }
                        }
                    }
                    catch (Exception ex) { Console.WriteLine("Exception at Navigation: " + ex.Message); }
                }
            }
            ////process the urls
            //do
            //{
            //    process(Urls, rcmdUrls);
            //    processcnt++;
            //    Urls = rcmdUrls;

            //    List<string> rcmdUrls1 = ReadCsv(ConfigurationManager.AppSettings.Get("csvfilepath") + ConfigurationManager.AppSettings.Get("rcmdfilename"));
            //    process(rcmdUrls1, rcmdUrls);

            //} while (Urls.Count > 0);
            Console.Read();
        }

        private static void closeTabs()
        {
            List<string> windowHandles = new List<string>();
            windowHandles = driver.WindowHandles.ToList();
            for (int i = 1; i < windowHandles.Count; i++)
            {
                driver.SwitchTo().Window(windowHandles[i]);
                System.Threading.Thread.Sleep(500);
                driver.Close();
            }
        }

        private static void process(List<string> Urls, List<string> rcmdUrls)
        {
            string filepath = ConfigurationManager.AppSettings.Get("csvfilepath") + ConfigurationManager.AppSettings.Get("filename");
            string rcmdfilepath = ConfigurationManager.AppSettings.Get("csvfilepath") + ConfigurationManager.AppSettings.Get("rcmdfilename");
            int prssdurlcnt = 0;
            CreateFile(ConfigurationManager.AppSettings.Get("csvfilepath"), ConfigurationManager.AppSettings.Get("filename"));
            CreateFile(ConfigurationManager.AppSettings.Get("csvfilepath"), ConfigurationManager.AppSettings.Get("rcmdfilename"));

            foreach (var url in Urls)
            {
                prssdurlcnt++;
                List<string> prssdUrls = ReadCsv(filepath);
                Console.WriteLine("----------------------------------------------------------------------");
                Console.WriteLine("Total Count: " + Urls.Count.ToString());
                Console.WriteLine("Processed Count: " + prssdurlcnt.ToString());
                Console.WriteLine("----------------------------------------------------------------------");
                if (prssdUrls != null && prssdUrls.Count > 0)
                {
                    if (prssdUrls.Any(a => a.Equals(url)))
                        continue;
                }

                try
                {
                    driver.Url = url;
                    System.Threading.Thread.Sleep(5000);
                    // get the recomended urls
                    //try
                    //{
                    //    //Get Urls
                    //    List<string> lsturl = new List<string>();
                    //    lsturl = GetPageJobUrls();
                    //    foreach (var item in lsturl)
                    //    {
                    //        rcmdUrls.Add(item);
                    //        WriteCsv(rcmdfilepath, item);
                    //    }
                    //    System.Threading.Thread.Sleep(5000);
                    //}
                    //catch (Exception ex) { Console.WriteLine("Exception at Get Urls method: " + ex.Message); }

                    // to verify the title lable
                    try
                    {
                        IWebElement h1Element = driver.FindElement(By.TagName("h1"));
                        string h1Text = h1Element.Text;
                        string[] keywords = ConfigurationManager.AppSettings.Get("rcmdkeys").Split(",");
                        bool keywordexist = false;
                        foreach (var keyword in keywords)
                        {
                            if (((!string.IsNullOrWhiteSpace(h1Text) && h1Text.ToString().ToUpper().Contains(keyword.ToUpper()))))
                            {
                                keywordexist = true;
                                break;
                            }
                            else
                            {
                                keywordexist = false;
                            }
                        }
                        if (!keywordexist)
                            continue;
                    }
                    catch (Exception ex) { Console.WriteLine("Exception at keyword verification: " + ex.Message); }

                    //if (((!string.IsNullOrWhiteSpace(h1Text) && h1Text.ToString().ToUpper().Contains(".NET"))
                    //    || (!string.IsNullOrWhiteSpace(h1Text) && h1Text.ToString().ToUpper().Contains("C#"))
                    //    || (!string.IsNullOrWhiteSpace(h1Text) && h1Text.ToString().ToUpper().Contains("SSIS"))
                    //    || (!string.IsNullOrWhiteSpace(h1Text) && h1Text.ToString().ToUpper().Contains("SSRS"))
                    //    || (!string.IsNullOrWhiteSpace(h1Text) && h1Text.ToString().ToUpper().Contains("ANGULAR"))))
                    //{
                    //    continue;
                    //}

                    try
                    {
                        WriteCsv(filepath, url);
                        //Easy Apply button
                        WebDriverWait wait = new WebDriverWait(driver, TimeSpan.FromSeconds(10));
                        IWebElement parentButton = driver.FindElement(By.CssSelector("apply-button-wc.ml-4.flex-auto.md\\:flex-initial.hydrated"));
                        parentButton.Click();
                        System.Threading.Thread.Sleep(5000);
                    }
                    catch (Exception ex) { Console.WriteLine("Exception at Easy Apply button: " + ex.Message); continue; }

                    try
                    {
                        //next button
                        IWebElement element = driver.FindElement(By.CssSelector("button.btn.btn-primary.btn-next.btn-block"));
                        element.Click();
                        System.Threading.Thread.Sleep(5000);
                    }
                    catch (Exception ex) { Console.WriteLine("Exception at next button: " + ex.Message); continue; }

                    try
                    {
                        //apply button
                        IWebElement ApplyButton1 = driver.FindElement(By.CssSelector("button.btn.btn-primary.btn-next.btn-split"));
                        ApplyButton1.Click();
                        System.Threading.Thread.Sleep(5000);
                    }
                    catch (Exception ex) { Console.WriteLine("Exception at apply button: " + ex.Message); continue; }

                    // get the recomended urls
                    try
                    {
                        //Get Urls
                        List<string> lsturl1 = new List<string>();
                        lsturl1 = GetPageJobUrls();
                        foreach (var item in lsturl1)
                        {
                            rcmdUrls.Add(item);
                            WriteCsv(rcmdfilepath, item);
                        }
                        System.Threading.Thread.Sleep(5000);
                    }
                    catch (Exception ex) { Console.WriteLine("Exception at Get Urls method: " + ex.Message); }

                }
                catch (Exception ex) { Console.WriteLine("Exception at looping: " + ex.Message); }
                //driver.FindElement(By.Id("submitSearch-button")).Click(); // Replace "password" with the actual password field ID
            }
        }

        private static List<string> GetPageJobUrls()
        {
            System.Threading.Thread.Sleep(5000);

            IReadOnlyCollection<IWebElement> anchorElements = driver.FindElements(By.TagName("a"));
            List<string> urls = new List<string>();

            foreach (IWebElement anchorElement in anchorElements)
            {
                try
                {
                    string href = anchorElement.GetAttribute("href");
                    string linkText = anchorElement.Text;
                    //if ((!string.IsNullOrWhiteSpace(linkText) && linkText.ToString().ToUpper().Contains(".NET"))
                    //    || (!string.IsNullOrWhiteSpace(linkText) && linkText.ToString().ToUpper().Contains("C#")))
                    //{
                    if (!string.IsNullOrEmpty(href))
                    {
                        urls.Add(href);
                    }
                    //}
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Exception at looping: " + ex.Message);
                    continue;
                }
            }

            foreach (var url in urls)
            {
                Console.WriteLine(url);
            }

            return urls;
        }


        static void WriteCsv(string filePath, string data)
        {
            using (FileStream fs = new FileStream(filePath, FileMode.OpenOrCreate, FileAccess.Write))
            {
                fs.Seek(0, SeekOrigin.End);
                using (StreamWriter writer = new StreamWriter(fs))
                {
                    // To append, update the stream's position to the end of the file
                    writer.WriteLine(data);
                }
            }
        }

        // Read data from a CSV file
        static List<string> ReadCsv(string filePath)
        {
            List<string> lnlst = new List<string>();
            using (StreamReader file = new StreamReader(filePath))
            {
                string ln;
                while ((ln = file.ReadLine()) != null)
                {
                    lnlst.Add(ln);
                }
                file.Close();
            }
            return lnlst;
        }

        static void CreateFile(string filePath, string filename)
        {
            // Check if the file exists
            if (!File.Exists(filePath + filename))
            {
                // Create the file if it doesn't exist
                using (FileStream fs = File.Create(filePath + filename))
                {
                    Console.WriteLine("File created successfully.");
                }
            }
            else
            {
                Console.WriteLine("File already exists.");
            }
        }


    }
}
