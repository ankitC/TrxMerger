using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;
using System.IO;

namespace TRXMerge
{
    public class TrxMerger
    {

        private const string SectionResults = @"/Results";
        private const string SectionTestDefinitions = @"/TestDefinitions";
        private const string SectionTestEntries = @"/TestEntries";

        private const string ResultsPathTemplate = "{0}\\FinalResults.trx";

        static int Main(string[] args)
        {
            string[] fileNames = TrxMerger.FindTrxFiles(args[0]);
#if DEBUG
            foreach (string fileName in fileNames)
            {
                Console.WriteLine(fileName);
            }
            Console.WriteLine(fileNames.Length);
#endif
      
            if (fileNames.Length >= 1)
            {
                System.Xml.XmlDocument oDocFirst = new XmlDocument();
                oDocFirst.Load(MakeCompatXML(fileNames[0]));

                for (int i = 1; i < fileNames.Length; i++)
                {
                    System.Xml.XmlDocument oDocSecond = new XmlDocument();
                    oDocSecond.Load(MakeCompatXML(fileNames[i]));

                    ////locate sections in first and append data from second...
                    MergeSection(out oDocFirst, oDocSecond, TrxMerger.SectionTestDefinitions);
                    MergeSection(out oDocFirst, oDocSecond, TrxMerger.SectionResults);
                    MergeSection(out oDocFirst, oDocSecond, TrxMerger.SectionTestEntries);
                }

                string resultsFilePath = string.Format(TrxMerger.ResultsPathTemplate, args[0]);
                if (File.Exists(resultsFilePath))
                {
                    File.Delete(resultsFilePath);
                }
                oDocFirst.Save(resultsFilePath);

                SetSummary(resultsFilePath);
            }

            return 0;
        }

        private static string[] FindTrxFiles(string path)
        {
            string resultsFilePath = string.Format(TrxMerger.ResultsPathTemplate, path);
            if (File.Exists(resultsFilePath))
            {
                File.Delete(resultsFilePath);
            }

            List<string> resultsFileList = new List<string>();
            string[] trxFilesList = Directory.GetFiles(path, "*.trx", SearchOption.TopDirectoryOnly);
            return trxFilesList;
        }

        private static void MergeSection(System.Xml.XmlDocument oDocFirst, System.Xml.XmlDocument oDocSecond, string sectionName)
        {
            XmlNode oNodeWhereInsert = oDocFirst.SelectSingleNode(sectionName);
            int i = 0;
            while (oDocSecond.SelectSingleNode(sectionName).ChildNodes.Count != i)
            {
                oNodeWhereInsert.AppendChild(oDocFirst.ImportNode(oDocSecond.SelectSingleNode(sectionName).ChildNodes[i], true));
                i++;
            }
        }

        public static summary GetSummary(XmlDocument doc)
        {
            summary s = new summary();
            s.total = -1;
            s.executed = -1;
            s.passed = -1;

            //XmlElement ele = doc.DocumentElement;

            XmlNode nTotal = doc.SelectNodes("//Counters/@total").Item(0);
            s.total = Convert.ToInt32(nTotal.InnerText);
            XmlNode nPass = doc.SelectNodes("//Counters/@passed").Item(0);
            s.passed = Convert.ToInt32(nPass.InnerText);
            XmlNode nExecuted = doc.SelectNodes("//Counters/@executed").Item(0);
            s.executed = Convert.ToInt32(nExecuted.InnerText);
            DateTime start;
            DateTime end0;
            XmlNode nStart = doc.SelectNodes("//Times/@start ").Item(0);
            start = Convert.ToDateTime(nStart.InnerText);
            XmlNode nEnd = doc.SelectNodes("//Times/@finish").Item(0);
            end0 = Convert.ToDateTime(nEnd.InnerText);
            //s.time = Math.Round((double)(end0 - start)/10000000.0,2); //ticks are in 100-nanoseconds
            s.time = ((end0 - start).TotalSeconds);

            return s;
        }

        public static string MakeCompatXML(string input)
        {
            //change the first tag to have no attributes - VSTS2008
            StringBuilder newFile = new StringBuilder();
            string temp = "";
            string nStr = "";
            string[] file = File.ReadAllLines(input);

            foreach (string line in file)
            {
                if (line.Contains("<TestRun id"))
                {
                    nStr = line.Substring(0, 8) + ">";
                    temp = line.Replace(line.ToString(), nStr);
                    newFile.Append(temp + "\r");
                    continue;
                }

                newFile.Append(line + "\r");
            }

            File.WriteAllText(input, newFile.ToString());

            return input;
        }

        public static bool IfTestExists(XmlDocument doc, string testName)
        {
            int i = 0;
            while (doc.SelectSingleNode("//TestDefinitions").ChildNodes.Count != i)
            {
                if (doc.SelectSingleNode("//TestDefinitions").ChildNodes[i].Attributes["name"].Value == testName)
                {
                    return true;
                }
                i++;
            }
            return false;
        }

        public static bool IfResultExists(XmlDocument doc, string testName, out XmlNode oldNode)
        {
            int i = 0;
            while (doc.SelectSingleNode("//Results").ChildNodes.Count != i)
            {
                if (doc.SelectSingleNode("//Results").ChildNodes[i].Attributes["testName"].Value == testName)
                {
                    oldNode = doc.SelectSingleNode("//Results").ChildNodes[i];
                    return true;
                }
                i++;
            }
            oldNode = null;
            return false;
        }

        public static void SetSummary(string fileName)
        {
            System.Xml.XmlDocument oDoc = new XmlDocument();
            oDoc.Load(fileName);

            XmlNode master = oDoc.SelectSingleNode("//ResultSummary/Counters");

            summary oSummary;
            oSummary.passed = 0;

            //count the number of test cases for total
            oSummary.total = oDoc.SelectSingleNode("//TestDefinitions").ChildNodes.Count;

            //count the number of test cases executed from count of test results
            oSummary.executed = oDoc.SelectSingleNode("//Results").ChildNodes.Count;

            //count the number of passed test cases from results
            int i = 0;
            while (oDoc.SelectSingleNode("//Results").ChildNodes.Count != i)
            {
                if (oDoc.SelectSingleNode("//Results").ChildNodes[i].Attributes["outcome"].Value == "Passed")
                {
                    oSummary.passed++;
                }
                i++;
            }

            ////update summary with new numbers
            master.Attributes["total"].Value = oSummary.total.ToString();
            master.Attributes["executed"].Value = oSummary.executed.ToString();
            master.Attributes["passed"].Value = oSummary.passed.ToString();

            ////locate and update times
            XmlNode oTimes = oDoc.SelectSingleNode("//Times");
            oDoc.Save(fileName);

        }

        public static double CalculateSummaryTime(XmlDocument doc)
        {
            DateTime sTime;
            DateTime eTime;
            double sDiff = 0;

            int i = 0;

            while (doc.SelectSingleNode("//Results").ChildNodes.Count != i)
            {
                sTime = Convert.ToDateTime(doc.SelectSingleNode("//Results").ChildNodes[i].Attributes["startTime"].Value);
                eTime = Convert.ToDateTime(doc.SelectSingleNode("//Results").ChildNodes[i].Attributes["endTime"].Value);
                //// calculate timespan for each test case and add them to calculate total time
                sDiff += (eTime - sTime).TotalSeconds;
                i++;
            }
            return sDiff;
        }
    }  

    public struct summary
    {
        public int total;
        public int executed;
        public int passed;
        public double time;
    }
}
