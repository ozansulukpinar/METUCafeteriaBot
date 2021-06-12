using System;
using System.Net;
using System.Collections.Generic;
using System.Net.Mail;
using SlackBotMessages;
using SlackBotMessages.Models;

namespace METUCafeteriaBot
{
    class Program
    {
        private static string WebHookUrl => "webhookurl";

        static void Main(string[] args)
        {
            WebClient client = new WebClient();
            
            DateTime today = DateTime.Today;
            string day, month, year;

            day = today.Day.ToString();
            month = today.Month.ToString();
            year = today.Year.ToString();

            if(today.Day < 10)
                day = "0" + day;
            
            if(today.Month < 10)
                month = "0" + month;

            string date = day + "-" + month + "-" + year;
            string pageSource = client.DownloadString("https://kafeterya.metu.edu.tr/tarih/" + date);

            List<string> mealList = GetMeals(pageSource);

            if(mealList.Count != 0){
                // Gmail less secure app access setting should be on
                SendEmail(mealList);

                SendMessage(WebHookUrl, mealList);
            }
            else{
               // At weekends and holidays there is no meal
               Console.WriteLine("No meal in today!");
           }
        }

        private static List<string> GetMeals(string pageSource){
            List<string> pList = new List<string>();
            string startString, endString, keyString;
            startString = "<p>";
            endString = "</p>";
            keyString = "<div class=\"yemek\">";
            bool isItValid = false;

            isItValid = pageSource.Contains(keyString);

            if(isItValid){                
                int startIndex, endIndex;

                for(int i = 0; i < 11; i++){
                    startIndex = pageSource.IndexOf(startString, 0) + startString.Length;
                    endIndex = pageSource.IndexOf(endString, startIndex);

                    string meal = pageSource.Substring(startIndex, endIndex - startIndex);
                    pList.Add(meal);

                    pageSource = pageSource.Remove(0, startIndex);
                }

            int[] irrelevantItemIndexes = {0, 4, 8};

            foreach(int item in irrelevantItemIndexes){
                pList.RemoveAt(item);
            }
           }

            return pList;
        }
        
        private static void SendEmail(List<string> mealList)
        {
            string from, password, to, subject, content, style;
            from = "sender_email_address";
            password = "sender_password";
            to = "recipient_email_address";
            subject = "Today's Menu"+ " - " + DateTime.Today.ToString("dd-MM-yyyy");
            content = "<table style=\"border-collapse:collapse; text-align:center;\" ><tr>";

            style = " style=\" border-color:#000000; border-style:solid; border-width:thin; padding: 5px;\"";

            for(int i = 0; i < 8; i++){
                if(i == 0)
                content += "<td" + style + ">Afternoon</td>";

                if(i == 4){
                content += "</tr><tr>";
                content += "<td" + style + ">Evening</td>";
                }

                content += "<td" + style + ">"+ mealList[i] +"</td>";
            }

            content += "</tr></table>";

            MailMessage mail = new MailMessage();
            mail.From = new MailAddress(from);
            mail.To.Add(to);
            mail.Subject = subject;
            mail.Body = content;
            mail.IsBodyHtml = true;

            SmtpClient smtp = new SmtpClient("smtp.gmail.com", 587);
            smtp.Credentials = new NetworkCredential(from, password);
            smtp.EnableSsl = true;
            smtp.Send(mail);
        }

        private static void SendMessage(string webHookURL, List<string> mealList)
        {
            var client = new SbmClient(webHookURL);
            var message = new Message
            {
                // An app as named this username should be in Slack workspace
                Username = "METUCafeteriaBot",
                IconUrl = "https://www.google.com/images/branding/googlelogo/2x/googlelogo_color_92x30dp.png",
                Attachments = new List<SlackBotMessages.Models.Attachment>
                    {
                        new SlackBotMessages.Models.Attachment
                        {
                        Color = "good",
                        Fields = new List<Field>
                            {
                                new Field
                                {
                                 Title = "Today's Menu" + " - " + DateTime.Now.ToString("dd-MM-yyyy") + " " + Emoji.Spaghetti,
                                 Value = "Afternoon" + "\r\n" +mealList[0] + "\r\n" + mealList[1] + "\r\n" + mealList[2] + "\r\n" + mealList[3] + "\r\n" + "Evening" + "\r\n" + mealList[4] + "\r\n" + mealList[5] + "\r\n" + mealList[6] + "\r\n" + mealList[7]
                                }
                            }
                        }
                    }
            };

            client.Send(message);
        }
    }
}
