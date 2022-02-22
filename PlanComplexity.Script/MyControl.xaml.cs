using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.ComponentModel;
using System.Windows.Forms;
using System.IO;
using VMS.TPS.Common.Model.API;
using VMS.TPS.Common.Model.Types;
using Complexity.Script;
using Path = System.IO.Path;

namespace PlanComplexity.Script
{
    /// <summary>
    /// MyControl.xaml 的交互逻辑
    /// </summary>
    public partial class MyControl : Window
    {
        public DataLayer dataLayer = new DataLayer();
        RunMessage theRunMessage = new RunMessage();

        VMS.TPS.Common.Model.API.Application app = null;

        private BackgroundWorker bcWorker;
        public MyControl(VMS.TPS.Common.Model.API.Application app)
        {
            InitializeComponent();

            this.app = app;
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

            ParametersGrid.DataContext = dataLayer;
            RunMessageTextBox.DataContext = theRunMessage;
            bcWorker = (BackgroundWorker)this.FindResource("bkWorker");
        }

        public MyControl()
        {
            InitializeComponent();
        }

        private void BackgroundWorker_DoWork(object sender, System.ComponentModel.DoWorkEventArgs e)
        {
            if (bcWorker.CancellationPending)
            {
                e.Cancel = true;
                return;
            }

            if (app == null)
            {
                return;
            }
            int totalPatient = dataLayer.PatientIdList.Count;
            if (0 == totalPatient)
            {
                theRunMessage.AddMessage("未指定患者ID，查找指定日期内的所有患者信息");
            }
            List<string> patientList = app.PatientSummaries.Where(x => x.CreationDateTime >= dataLayer.BeginTime && x.CreationDateTime <= dataLayer.EndTime).Select(x => x.Id).ToList();

            if (patientList.Count > 0)
            {
                totalPatient = patientList.Count;
                theRunMessage.AddMessage($"所有患者数量为 {totalPatient}");
            }

            theRunMessage.AddMessage($"输出文件夹: {dataLayer.OutputFolder}");

            // 查找到的所有患者
            foreach (var item in patientList)
            {
                dataLayer.PatientIdList.Add(new PatientIdClass() { PatientId = item });
            }

            float finishedNum = 0;
            // 对每一个患者进行计算

            String vmat_filename = Path.Combine(dataLayer.OutputFolder, "patient_vmat_analysis.csv");
            theRunMessage.AddMessage($"输出文件: {vmat_filename}");
            StreamWriter vmat_patient_file = new StreamWriter(vmat_filename, true, Encoding.GetEncoding("gb2312"));

            vmat_patient_file.WriteLine("ID, Name, Sites, Doctor, Physicist, PlanID, MachineID, Calculation_Model, Prescribed_Dose, MU, " +
                "EDGE_Metric, Leaf_Area, Plan_Irregularity, Plan_Modulation, Modulation_Complexity_Score, " +
                "Small_Aperture_Score_5mm, Small_Aperture_Score_10mm, Small_Aperture_Score_20mm, Mean_Field_Area, " +
                "Mean_Asymmetry_Distance, Aperture_Area_Ration_Jaw_Area, Aperture_Sub_Regions, Aperture_X_Jaw_Distance, " +
                "Aperture_Y_Jaw_Distance, Leaf_Gap_Average, Leaf_Gap_Std, Leaf_Travel, Converted_Aperture_Metric, Edge_Area_Metric, " +
                "Speed_0_4, Speed_4_8, Speed_8_12, Speed_12_16, Speed_16_20, Speed_20_25, Speed_Average, Speed_Std, " +
                "Acc_0_10, Acc_10_20, Acc_20_40, Acc_40_60, Acc_Average, Acc_std, SPORT");

            String imrt_filename = Path.Combine(dataLayer.OutputFolder, "patient_imrt_analysis.csv");
            theRunMessage.AddMessage($"输出文件: {imrt_filename}");
            StreamWriter imrt_patient_file = new StreamWriter(imrt_filename, true, Encoding.GetEncoding("gb2312"));
            imrt_patient_file.WriteLine("ID, Name, Sites, Doctor, Physicist, PlanID, MachineID, Calculation_Model, Prescribed_Dose, MU, " +
                                        "EDGE_Metric, Leaf_Area, Plan_Irregularity, Plan_Modulation, Modulation_Complexity_Score, " +
                                        "Small_Aperture_Score_5mm, Small_Aperture_Score_10mm, Small_Aperture_Score_20mm, Mean_Field_Area, " +
                                        "Mean_Asymmetry_Distance, Aperture_Area_Ration_Jaw_Area, Aperture_Sub_Regions, Aperture_X_Jaw_Distance, " +
                                        "Aperture_Y_Jaw_Distance, Leaf_Gap_Average, Leaf_Gap_Std, Leaf_Travel, Converted_Aperture_Metric, Edge_Area_Metric");

            foreach (var patientIdInstance in dataLayer.PatientIdList)
            {
                try
                {
                    Patient patient = app.OpenPatientById(patientIdInstance.PatientId);
                    if (patient == null)
                    {
                        patient = app.OpenPatientById(patientIdInstance.PatientId.ToLower());
                    }

                    theRunMessage.AddMessage($"处理: {patient.Id}");

                    if (patient != null)
                    {
                        Console.WriteLine($"Open Patient by Id = {patient.Id}");

#if (DEBUG136)
                        // Only choose DMLC/IMRT and VMAT plan to analysis
                        var approvedPlans = from Course c in patient.Courses
                                            where c != null
                                            from PlanSetup ps in c.PlanSetups

                                            where ps.ApprovalStatus == PlanSetupApprovalStatus.TreatmentApproved && ps.IsTreated == true
                                            select new
                                            {
                                                Patient = patient,
                                                Cource = c,
                                                Plan = ps
                                            };
#endif
#if (!DEBUG136)

                // Only choose DMLC/IMRT and VMAT plan to analysis
                        var approvedPlans = from Course c in patient.Courses
                                            where c != null
                                            from PlanSetup ps in c.PlanSetups

                                            where ps.ApprovalStatus == PlanSetupApprovalStatus.TreatmentApproved && ps.PlanIntent.ToLower() != "verification"
                                            select new
                                            {
                                                Patient = patient,
                                                Cource = c,
                                                Plan = ps
                                            };
#endif


                        theRunMessage.AddMessage($"该患者下符合条件的计划数量: {approvedPlans.Count()}");
                        if (approvedPlans.Any())
                        {
                            foreach (var p in approvedPlans)
                            {
                                PlanSetup ps = p.Plan;
                                Console.WriteLine($"Open Course by Id = {p.Cource.Id}, Open Plan by ID = {ps.Id}");

                                if (PlanUtilities.getTreatmentTechnique(ps).ToUpper().Equals("STATIC"))
                                {
                                    string[] patient_infos = new String[] { patient.Id, patient.Name, "", "", "" };
                                    IMRT.Execute(p.Patient, ps, imrt_patient_file, patient_infos);
                                }
                                else if (PlanUtilities.getTreatmentTechnique(ps).ToUpper().Equals("ARC"))
                                {
                                    string[] patient_infos = new String[] { patient.Id, patient.Name, "", "", "" };
                                    VMAT.Execute(p.Patient, ps, vmat_patient_file, patient_infos);
                                }
                                theRunMessage.AddMessage($"{patient.Id} {patient.Name} Course:{ps.Course.Id} Plan:{ps.Id} 计算完成");
                            }
                            theRunMessage.AddMessage($"该患者处理完成");

                        }
                    }
                    app.ClosePatient();

                    finishedNum++;
                    bcWorker.ReportProgress((int)Math.Round(finishedNum / totalPatient * 100));
                }
                catch (Exception exception)
                {
                    Console.WriteLine(exception);
                    theRunMessage.AddMessage(exception.Message);
                    // throw;
                }
            }
            imrt_patient_file.Close();
            vmat_patient_file.Close();
        }

        private void BackgroundWorker_ProgressChanged(object sender, System.ComponentModel.ProgressChangedEventArgs e)
        {
            donePatientBar.Value = e.ProgressPercentage;
        }

        private void BackgroundWorker_RunWorkerCompleted(object sender,
            System.ComponentModel.RunWorkerCompletedEventArgs e)
        {
            System.Windows.MessageBox.Show("任务完成");
        }

        private void ChangeFolder_Button_Click(object sender, RoutedEventArgs e)
        {
            FolderBrowserDialog newBrowserDialog = new FolderBrowserDialog();
            newBrowserDialog.ShowNewFolderButton = true;
            newBrowserDialog.ShowDialog();
            if (Directory.Exists(newBrowserDialog.SelectedPath))
            {
                Directory.GetAccessControl(newBrowserDialog.SelectedPath);  // 获取读写权限
                dataLayer.OutputFolder = newBrowserDialog.SelectedPath;
            }
            else
            {
                System.Windows.MessageBox.Show($"目录{newBrowserDialog.SelectedPath}不存在");
            }
        }

        // 开始运行
        private void DoAnalyse_Button_Click(object sender, RoutedEventArgs e)
        {
            if (app == null)
            {
                return;
            }
            int totalPatient = dataLayer.PatientIdList.Count;
            if (0 == totalPatient)
            {
                theRunMessage.AddMessage("未指定患者ID，查找指定日期内的所有患者信息");
            }
            List<string> patientList = app.PatientSummaries.Where(x => x.CreationDateTime >= dataLayer.BeginTime && x.CreationDateTime <= dataLayer.EndTime).Select(x => x.Id).ToList();

            if (patientList.Count > 0)
            {
                totalPatient = patientList.Count;
                theRunMessage.AddMessage($"所有患者数量为 {totalPatient}");
            }

            theRunMessage.AddMessage($"输出文件夹: {dataLayer.OutputFolder}");

            // 查找到的所有患者
            foreach (var item in patientList)
            {
                dataLayer.PatientIdList.Add(new PatientIdClass() { PatientId = item });
            }

            float finishedNum = 0;
            // 对每一个患者进行计算

            String vmat_filename = Path.Combine(dataLayer.OutputFolder, "patient_vmat_analysis.csv");
            theRunMessage.AddMessage($"输出文件: {vmat_filename}");
            StreamWriter vmat_patient_file = new StreamWriter(vmat_filename, true, Encoding.GetEncoding("gb2312"));

            vmat_patient_file.WriteLine("ID, Name, Sites, Doctor, Physicist, PlanID, MachineID, Calculation_Model, Prescribed_Dose, MU, " +
                "EDGE_Metric, Leaf_Area, Plan_Irregularity, Plan_Modulation, Modulation_Complexity_Score, " +
                "Small_Aperture_Score_5mm, Small_Aperture_Score_10mm, Small_Aperture_Score_20mm, Mean_Field_Area, " +
                "Mean_Asymmetry_Distance, Aperture_Area_Ration_Jaw_Area, Aperture_Sub_Regions, Aperture_X_Jaw_Distance, " +
                "Aperture_Y_Jaw_Distance, Leaf_Gap_Average, Leaf_Gap_Std, Leaf_Travel, Converted_Aperture_Metric, Edge_Area_Metric, " +
                "Speed_0_4, Speed_4_8, Speed_8_12, Speed_12_16, Speed_16_20, Speed_20_25, Speed_Average, Speed_Std, " +
                "Acc_0_10, Acc_10_20, Acc_20_40, Acc_40_60, Acc_Average, Acc_std, SPORT");

            String imrt_filename = Path.Combine(dataLayer.OutputFolder, "patient_imrt_analysis.csv");
            theRunMessage.AddMessage($"输出文件: {imrt_filename}");
            StreamWriter imrt_patient_file = new StreamWriter(imrt_filename, true, Encoding.GetEncoding("gb2312"));
            imrt_patient_file.WriteLine("ID, Name, Sites, Doctor, Physicist, PlanID, MachineID, Calculation_Model, Prescribed_Dose, MU, " +
                                        "EDGE_Metric, Leaf_Area, Plan_Irregularity, Plan_Modulation, Modulation_Complexity_Score, " +
                                        "Small_Aperture_Score_5mm, Small_Aperture_Score_10mm, Small_Aperture_Score_20mm, Mean_Field_Area, " +
                                        "Mean_Asymmetry_Distance, Aperture_Area_Ration_Jaw_Area, Aperture_Sub_Regions, Aperture_X_Jaw_Distance, " +
                                        "Aperture_Y_Jaw_Distance, Leaf_Gap_Average, Leaf_Gap_Std, Leaf_Travel, Converted_Aperture_Metric, Edge_Area_Metric");

            foreach (var patientIdInstance in dataLayer.PatientIdList)
            {
                try
                {
                    Patient patient = app.OpenPatientById(patientIdInstance.PatientId);
                    if (patient == null)
                    {
                        patient = app.OpenPatientById(patientIdInstance.PatientId.ToLower());
                    }

                    theRunMessage.AddMessage($"处理: {patient.Id}");

                    if (patient != null)
                    {
                        Console.WriteLine($"Open Patient by Id = {patient.Id}");

#if (DEBUG136)
                        // Only choose DMLC/IMRT and VMAT plan to analysis
                        var approvedPlans = from Course c in patient.Courses
                                            where c != null
                                            from PlanSetup ps in c.PlanSetups

                                            where ps.ApprovalStatus == PlanSetupApprovalStatus.TreatmentApproved && ps.IsTreated == true
                                            select new
                                            {
                                                Patient = patient,
                                                Cource = c,
                                                Plan = ps
                                            };
#endif
#if (!DEBUG136)

                // Only choose DMLC/IMRT and VMAT plan to analysis
                        var approvedPlans = from Course c in patient.Courses
                                            where c != null
                                            from PlanSetup ps in c.PlanSetups

                                            where ps.ApprovalStatus == PlanSetupApprovalStatus.TreatmentApproved && ps.PlanIntent.ToLower() != "verification"
                                            select new
                                            {
                                                Patient = patient,
                                                Cource = c,
                                                Plan = ps
                                            };
#endif


                        theRunMessage.AddMessage($"该患者下符合条件的计划数量: {approvedPlans.Count()}");
                        if (approvedPlans.Any())
                        {
                            foreach (var p in approvedPlans)
                            {
                                PlanSetup ps = p.Plan;
                                Console.WriteLine($"Open Course by Id = {p.Cource.Id}, Open Plan by ID = {ps.Id}");

                                if (PlanUtilities.getTreatmentTechnique(ps).ToUpper().Equals("STATIC"))
                                {
                                    string[] patient_infos = new String[] { patient.Id, patient.Name, "", "", "" };
                                    IMRT.Execute(p.Patient, ps, imrt_patient_file, patient_infos);
                                }
                                else if (PlanUtilities.getTreatmentTechnique(ps).ToUpper().Equals("ARC"))
                                {
                                    string[] patient_infos = new String[] { patient.Id, patient.Name, "", "", "" };
                                    VMAT.Execute(p.Patient, ps, vmat_patient_file, patient_infos);
                                }
                                theRunMessage.AddMessage($"{patient.Id} {patient.Name} Course:{ps.Course.Id} Plan:{ps.Id} 计算完成");
                            }
                            theRunMessage.AddMessage($"该患者处理完成");

                        }
                    }
                    app.ClosePatient();

                    finishedNum++;
                    // bcWorker.ReportProgress((int)Math.Round(finishedNum / totalPatient * 100));
                }
                catch (Exception exception)
                {
                    Console.WriteLine(exception);
                    theRunMessage.AddMessage(exception.Message);

                    app.ClosePatient();
                    // throw;
                }
            }
            imrt_patient_file.Close();
            vmat_patient_file.Close();


            //if (bcWorker != null && !bcWorker.IsBusy)
            //{
            //    bcWorker.RunWorkerAsync();
            //}
        }

        // 取消
        private void Cancel_Button_Click(object sender, RoutedEventArgs e)
        {
            if (bcWorker != null)
            {
                bcWorker.CancelAsync();
            }
        }
    }
}
