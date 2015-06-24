using System;
using Microsoft.AspNet.Mvc;
using System.Net;
using System.IO;
using System.Collections.Generic;
using Newtonsoft.Json;
using System.Threading;
using wqa.Models;
using wqa.Helpers;

namespace wqa.Controllers
{
    public class HomeController : Controller
    {
        // see http://www.ibm.com/smarterplanet/us/en/ibmwatson/developercloud/doc/qaapi/corpora.html#travel for question types
        QAWatson qaw = new QAWatson { name = "Anonymous", question = "Is there a ferry at Hyannis?", answer = " " };

        public ActionResult Index()
        {
            return View(qaw);
        }

        public ActionResult QAWatsonError(string error)
        {
            QAWatsonError err = new QAWatsonError();
            err.Error = error;
            return View(err);
        }

        [HttpPost]
        public ActionResult QAWatsonRedirect(string question)
        {
            qaw.question = question;
            Console.WriteLine("QAWatsonRedirect");
            WatsonQAService ws = ProcessVCAP_Sevices();
            if (ws == null)
            {
                string errorString = "Sorry, Unable to get the IBM Watson QA service!";
                Console.WriteLine(errorString);
                return RedirectToAction("QAWatsonError", new { error = errorString });
            }

            try
            {
                System.Net.ServicePointManager.ServerCertificateValidationCallback += (s, ce, ca, p) => true;

                Console.WriteLine("URL:: " + ws.url);
                RequestManager rm = new RequestManager();
                HttpWebResponse rep = rm.SendPOSTRequest(ws.url, rm.GetJsonString(qaw.question), ws.userid, ws.password, false);
                if (rep.StatusCode != HttpStatusCode.OK)
                {
                    Console.WriteLine("HTTP Status: " + rep.StatusCode);
                    return RedirectToAction("QAWatsonError", new { error = rep.StatusDescription });
                }

                StreamReader reader = new StreamReader(rep.GetResponseStream());
                qaw.answer = reader.ReadToEnd();

                List<WatsonQA> list = JsonConvert.DeserializeObject<List<WatsonQA>>(qaw.answer);
                WatsonQA q3 = list[0];
                Console.WriteLine("Response status: " + q3.question.status);

                if (q3.question.status == "Accepted")
                {
                    int cnt = 0;
                    int ind = q3.question.links.self.LastIndexOf('/');
                    string instresp = q3.question.links.self.Substring(ind);

                    while (q3.question.status != "Complete" && cnt++ != 5)
                    {
                        Console.WriteLine("Response URI --> " + ws.url + instresp);
                        Thread.Sleep(100);

                        HttpWebResponse rep1 = rm.SendGETRequest(ws.url + instresp, ws.userid, ws.password, false);
                        StreamReader reader1 = new StreamReader(rep1.GetResponseStream());
                        qaw.answer = reader1.ReadToEnd();
                        q3 = JsonConvert.DeserializeObject<WatsonQA>(qaw.answer);
                        Console.WriteLine("Trying " + cnt + ", Response: status - " + q3.question.status + ", Answers: " + q3.question.answers.Length);
                    }
                }

                if (q3.question.status == "Complete")
                {
                    qaw.id = new int[q3.question.answers.Length];
                    qaw.text = new string[q3.question.answers.Length];
                    qaw.confidence = new float[q3.question.answers.Length];

                    for (int i = 0; i < q3.question.answers.Length; i++)
                    {
                        qaw.id[i] = q3.question.answers[i].id;
                        qaw.text[i] = q3.question.evidencelist[i].text;
                        qaw.confidence[i] = q3.question.answers[i].confidence;
                    }
                }
                else
                {
                    string errorString = "Sorry, Failed to get data from Watson service. Status : " + q3.question.status;
                    Console.WriteLine(errorString);

                    return RedirectToAction("QAWatsonError", new { error = errorString });
                }
            }
            catch (Exception Ex)
            {
                string errorString = "Sorry, Failed to get data from Watson service. Status : " + Ex;
                Console.WriteLine(errorString);

                return RedirectToAction("QAWatsonError", new { error = errorString });
            }

            return View(qaw);
        }

        public class WatsonQA
        {
            public question question { get; set; }
        }

        public class links
        {
            public string self { get; set; }
            public string feedback { get; set; }
        }

        public class evidenceRequest
        {
            public int items { get; set; }
            public string profile { get; set; }
        }

        public class evidencelist
        {
            public string copyright { get; set; }
            public string document { get; set; }
            public string id { get; set; }
            public string termsOfUse { get; set; }
            public string text { get; set; }
            public string title { get; set; }
            public float value { get; set; }
        }

        public class answers
        {
            public int id { get; set; }
            public string text { get; set; }
            public string pipeline { get; set; }
            public float confidence { get; set; }
        }

        public class question
        {
            public links links { get; set; }
            public string id { get; set; }
            public bool formattedAnswer { get; set; }
            public string questionText { get; set; }
            public string status { get; set; }
            public int items { get; set; }
            public string passthru { get; set; }
            public evidenceRequest evidenceRequest { get; set; }
            public evidencelist[] evidencelist { get; set; }
            public answers[] answers { get; set; }
            public bool inferQuestion { get; set; }
        }

        public class WatsonQAService
        {
            public string url { get; set; }
            public string userid { get; set; }
            public string password { get; set; }
        }

        public class wobject
        {
            public WatsonQAAPI[] WatsonQAAPI { get; set; }
        }

        public class WatsonQAAPI
        {
            public string name { get; set; }
            public string label { get; set; }
            public string[] tags { get; set; }
            public string plan { get; set; }
            public credentials credentials { get; set; }
        }

        public class credentials
        {
            public string url { get; set; }
            public string username { get; set; }
            public string password { get; set; }
        }

        private static WatsonQAService ProcessVCAP_Sevices()
        {
            WatsonQAService ws = new WatsonQAService();

            string envStr = System.Environment.GetEnvironmentVariable("VCAP_SERVICES");
            if (envStr == null)
            {
                Console.WriteLine("VCAP_SERVICES environment variable not set.");
                return null;
            }

            string newStr = envStr.Replace("question_and_answer", "WatsonQAAPI");

            var wo = JsonConvert.DeserializeObject<wobject>(newStr);
            if (wo != null && wo.WatsonQAAPI != null)
            {
                ws.url = wo.WatsonQAAPI[0].credentials.url + "/v1/question/travel";
                ws.userid = wo.WatsonQAAPI[0].credentials.username;
                ws.password = wo.WatsonQAAPI[0].credentials.password;
                Console.WriteLine("ProcessVCAP_Services: URL: " + ws.url);
            }
            else
            {
                Console.WriteLine("Unable to get Watson QA service details from VCAP_SERVICES");
                return null;
            }

            return ws;
        }
    }
}
