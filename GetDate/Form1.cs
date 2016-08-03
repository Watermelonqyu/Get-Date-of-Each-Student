using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

using CSALMongo;
using MongoDB.Bson;
using MongoDB.Driver;


namespace GetDate
{
    public partial class Form1 : Form
    {
        //should change the name of the database
        public const string DB_URL = "mongodb://localhost:27017/csaldata";
        //protected MongoDatabase testDB = null;
        List<String> students = new List<string>();
        List<String> studentsInToronto = new List<string>();

        // String Tags = "Record ID\t" + "City\t" + "Class ID\t" + "Teacher\t" + "Date\t" + "User ID\t" + "StartTime\t" + "EndTime\t" + "Duration\t" + "No. of lessons\t" + "lessons\t" + "attempts\t" + "completion\t" + "\n";

        public Form1()
        {
            InitializeComponent();

            // Start 
            try
            {
                //query from the database
                var db = new CSALDatabase(DB_URL);
                var classes = db.FindClasses();
                var students1 = db.FindStudents();
                // this.richTextBox1.Text = Tags;

                // lai | marietta
                // aec | kingwilliam | main | tlp
                string[] pilot1_class = {"pilot1_aecn", "pilot1_tlp", "pilot1_ptp1", "pilot1_ptp2", 
                                         "pilot1_marietta1", "pilot1_marietta2", "pilot1_lai1", "pilot1_lai2"};

                Dictionary<string, List<string>> allstudents = new Dictionary<string, List<string>>();
                
                // find the target classes
                foreach (var oneClass in classes)
                {
                    foreach (string cls in pilot1_class)
                    {
                        string id_location = "";
                        List<string> studentsInOne = new List<string>();
                        if (oneClass.ClassID == cls)
                        {
                            foreach (String student in oneClass.Students)
                            {
                                if (!String.IsNullOrWhiteSpace(student) && !String.IsNullOrWhiteSpace(oneClass.ClassID))
                                {
                                    id_location = oneClass.ClassID + ':' + oneClass.Location;
                                    studentsInOne.Add(student);
                                }
                            }
                        }
                        if (allstudents.ContainsKey(id_location) == false && id_location != "")
                        {
                            allstudents.Add(id_location, studentsInOne);
                        }
                    }
                }

                string[] lines = System.IO.File.ReadAllLines(@"D:\CSAL\Tools\GetDate\GetDate\Lessons.txt");
                List<string> lessonNames = new List<string>();
                List<string> lessonActualNames = new List<string>();

                foreach (string line in lines)
                {
                    string lesson = line.Split(new char[] { '_' })[0];
                    string lessonN = line.Split(new char[] { '_' })[1];
                    lessonNames.Add(lesson);
                    lessonActualNames.Add(lessonN);
                }
    
                List<List<string>> results = new List<List<string>>();
                foreach (KeyValuePair<string, List<string>> studentInOne in allstudents)
                {
                    // get ClassID and Location
                    string[] clstu = studentInOne.Key.Split(new Char[] { ':' });
                    string classId = clstu[0];
                    string studentId = clstu[1];
                    List<string> allRecordsOneRaw = new List<string>();
                    // write all records in all the lessons into one List
                    foreach (string lessonId in lessonNames)
                    {
                        // get record
                        string recordsInOneLesson = getPerLesson(studentInOne.Value, lessonId);
                        allRecordsOneRaw.Add(classId + "\t" + studentId + "\t" + recordsInOneLesson + "\n");
                    }
                    results.Add(allRecordsOneRaw);
                }

                int recordCount = 1;

                foreach (List<string> record in results)
                {
                    foreach (string re in record)
                    {
                        this.richTextBox1.AppendText(recordCount.ToString() + "\t" + lessonActualNames[(recordCount - 1) % 35]+ "\t" + re);
                        recordCount++;
                    }
                }
            }
            catch (Exception e)
            {
                e.GetBaseException();
                e.GetType();
            }
        }

        public string getPerLesson(List<string> studentIds, string lessonID)
        {
            string records = "";
            var min_dura = new TimeSpan(2,0,0);
            var max_dura = new TimeSpan(0,0,0);
            string date = "";
            var db = new CSALDatabase(DB_URL);
            foreach (string studentId in studentIds)
            {
                var oneTurn = db.FindTurns(lessonID, studentId);
                // student didn't do the lesson
                if (oneTurn == null || oneTurn.Count < 1 || oneTurn[0].Turns.Count < 1)
                {
                    continue;
                }
                else
                {
                    var startDt = new DateTime();
                    foreach (var turn in oneTurn[0].Turns)
                    {
                        if (turn.TurnID == 1)
                        {
                            startDt = new DateTime(2010, 1, 1, 0, 0, 0, 0).AddSeconds(Math.Round(turn.DBTimestamp / 1000)).ToLocalTime();
                        }

                        foreach (var tran in turn.Transitions)
                        {
                            if (tran.RuleID == "End")
                            {
                                var Enddt = new DateTime(2010, 1, 1, 0, 0, 0, 0).AddSeconds(Math.Round(turn.DBTimestamp / 1000)).ToLocalTime();
                                TimeSpan dura = Enddt.Subtract(startDt);
                                if (dura < min_dura)
                                {
                                    min_dura = dura;
                                    date = startDt.Date.ToString();
                                }
                                if (dura > max_dura)
                                {
                                    max_dura = dura;
                                }
                            }
                        }
                    }
                }
            }

            records = date + "\t" + min_dura.TotalMinutes.ToString() + "\t" + max_dura.TotalMinutes.ToString();
            return records;
        }
 
    }
}
