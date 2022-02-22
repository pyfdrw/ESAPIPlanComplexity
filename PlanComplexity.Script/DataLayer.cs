using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PlanComplexity.Script
{
    public class DataLayer : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        public void OnPropertyChanged(PropertyChangedEventArgs e)
        {
            PropertyChanged?.Invoke(this, e);
        }


        private ObservableCollection<PatientIdClass> _patientIdList;
        public ObservableCollection<PatientIdClass> PatientIdList
        {
            get { return _patientIdList; }
            set
            {
                _patientIdList = value;
                OnPropertyChanged(new PropertyChangedEventArgs("PatientIdList"));
            }
        }

        private DateTime _beginTime;

        public DateTime BeginTime
        {
            get { return _beginTime; }
            set
            {
                _beginTime = value;
                OnPropertyChanged(new PropertyChangedEventArgs("BeginTime"));
            }
        }

        private DateTime _endTime;

        public DateTime EndTime
        {
            get { return _endTime; }
            set
            {
                _endTime = value;
                OnPropertyChanged(new PropertyChangedEventArgs("EndTime"));
            }
        }


        // 辅助进行显示
        private ObservableCollection<string> _printDateMode;

        public ObservableCollection<string> PrintDateMode
        {
            get { return _printDateMode; }
            set
            {
                _printDateMode = value;
                OnPropertyChanged(new PropertyChangedEventArgs("PrintDateMode"));
            }
        }

        private string _outputFolder;

        public string OutputFolder
        {
            get { return _outputFolder; }
            set
            {
                if (Directory.Exists(value))
                {
                    _outputFolder = value;
                    OnPropertyChanged(new PropertyChangedEventArgs("OutputFolder"));
                }
                else // 不做改变
                {
                    OnPropertyChanged(new PropertyChangedEventArgs("OutputFolder"));
                }
            }
        }

        public DataLayer()
        {
            //PatientIdList = new ObservableCollection<PatientIdClass>()
            //{
            //    new PatientIdClass()
            //    {
            //        PatientId = "1912053C"
            //    }
            //};

            PatientIdList = new ObservableCollection<PatientIdClass>();

            EndTime = DateTime.Today;
            BeginTime = DateTime.Today.AddMonths(-6);  // 减去6个月

            PrintDateMode = new ObservableCollection<string>()
            {
                "计划批准时间","QA时间", "今天"
            };

            var exeName = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().CodeBase.Remove(0, 8));  // Remove file://
 
            exeName = Path.Combine(exeName, "OutputFolder");
            if (Directory.Exists(exeName))
            {
                OutputFolder = exeName;
            }
            else
            {
                Directory.CreateDirectory(exeName);
                OutputFolder = exeName;
            }
        }
    }

    public class PatientIdClass : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        public void OnPropertyChanged(PropertyChangedEventArgs e)
        {
            PropertyChanged?.Invoke(this, e);
        }

        private string _patientId;

        public string PatientId
        {
            get { return _patientId; }
            set
            {
                _patientId = value;
                OnPropertyChanged(new PropertyChangedEventArgs("PatientId"));
            }
        }
    }

    public class RunMessage : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        public void OnPropertyChanged(PropertyChangedEventArgs e)
        {
            PropertyChanged?.Invoke(this, e);
        }

        private string _showMessage = "";

        public string ShowMessage
        {
            get { return _showMessage; }
            set
            {
                _showMessage = value + Environment.NewLine;
                OnPropertyChanged(new PropertyChangedEventArgs("ShowMessage"));
            }
        }

        // 添加需要显示的信息
        public int AddMessage(string messageToAdd)
        {
            ShowMessage = ShowMessage + messageToAdd;

            return 0;
        }
    }
}
