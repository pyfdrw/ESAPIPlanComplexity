using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VMS.TPS.Common.Model.API;
using Complexity.ComplexityMetric;

namespace GetPlanComplexity
{
    public class PlanComplexityValues
    {
        // Plan Infos
        public string PlanId { get; set; }
        public string courseIdorsiteName { get; set; } // Course Id
        public string AlgorithmName { get; set; } // AAA, AcurosXB
        public string PlanTechnicalName { get; set; } // VMAT or IMRT

        public double FractionDose { get; set; }
        public double TotalDose { get; set; }

        // Complexity Metrics
        public double MU { get; set; }
        public double EDGE_Metric { get; set; }
        public double Leaf_Area { get; set; }
        public double Plan_Irregularity { get; set; }
        public double Plan_Modulation { get; set; }
        public double Modulation_Complexity_Score { get; set; }
        public double Small_Aperture_Score_5mm { get; set; }
        public double Small_Aperture_Score_10mm { get; set; }
        public double Small_Aperture_Score_20mm { get; set; }
        public double Mean_Field_Area { get; set; }
        public double Mean_Asymmetry_Distance { get; set; }
        public double Aperture_Area_Ration_Jaw_Area { get; set; }
        public double Aperture_Sub_Regions { get; set; }
        public double Aperture_X_Jaw_Distance { get; set; }
        public double Aperture_Y_Jaw_Distance { get; set; }
        public double Leaf_Gap_Average { get; set; }
        public double Leaf_Gap_Std { get; set; }
        public double Leaf_Travel { get; set; }
        public double Converted_Aperture_Metric { get; set; }
        public double Edge_Area_Metric { get; set; }
        public double Speed_0_4 { get; set; }
        public double Speed_4_8 { get; set; }
        public double Speed_8_12 { get; set; }
        public double Speed_12_16 { get; set; }
        public double Speed_16_20 { get; set; }
        public double Speed_20_25 { get; set; }
        public double Speed_Average { get; set; }
        public double Speed_Std { get; set; }
        public double Acc_0_10 { get; set; }
        public double Acc_10_20 { get; set; }
        public double Acc_20_40 { get; set; }
        public double Acc_40_60 { get; set; }
        public double Acc_Average { get; set; }
        public double Acc_std { get; set; }
        public double SPORT { get; set; }

        // 添加一个显式的无参数构造函数，专门用于反序列化
        public PlanComplexityValues() { }

        // 从 PlanSetup 对象中提取复杂性值的构造函数
        public PlanComplexityValues(PlanSetup ps) 
        {
            PlanId = ps.Id;
            courseIdorsiteName = ps.Course.Id;
            AlgorithmName = ps.PhotonCalculationModel;
            FractionDose = ps.DosePerFraction.Dose;
            TotalDose = ps.TotalDose.Dose;

            string tech = (from beam in ps.Beams
             where (!beam.IsSetupField && beam.MLC != null)
             select beam.Technique).FirstOrDefault().ToString().ToUpper();
            if (tech.Contains("ARC") || tech.Contains("STATIC"))
            {
                // 无论IMRT或者VAMT都需要计算的指标
                MU = (from beam in ps.Beams
                      where (!beam.IsSetupField && beam.MLC != null)
                      select beam.Meterset.Value).ToArray().Sum();

                var patient = ps.Course.Patient;

                EdgeMetric edgeMetricObj = new EdgeMetric();
                double planEdgeMetric = edgeMetricObj.CalculateForPlan(patient, ps);
                EDGE_Metric = planEdgeMetric;

                LeafArea planLeafAreaObj = new LeafArea();
                double planLeafArea = planLeafAreaObj.CalculateForPlan(patient, ps);
                Leaf_Area = planLeafArea;

                PlanIrregularity planIrregularityObj = new PlanIrregularity();
                double planIrregularity = planIrregularityObj.CalculateForPlan(patient, ps);
                Plan_Irregularity = planIrregularity;

                PlanModulation planModulationObj = new PlanModulation();
                double planModulation = planModulationObj.CalculateForPlan(patient, ps);
                Plan_Modulation = planModulation;

                ModulationComplexityScore planMCSObj = new ModulationComplexityScore();
                double planMCS = planMCSObj.CalculateForPlan(patient, ps);
                Modulation_Complexity_Score = planMCS;

                SmallApertureScore planSmallApertureScoreObj = new SmallApertureScore();
                double planSmallApertureScore5mm = planSmallApertureScoreObj.CalculateForPlan(patient, ps, 5.0);
                double planSmallApertureScore10mm = planSmallApertureScoreObj.CalculateForPlan(patient, ps, 10.0);
                double planSmallApertureScore20mm = planSmallApertureScoreObj.CalculateForPlan(patient, ps, 20.0);
                Small_Aperture_Score_5mm = planSmallApertureScore5mm;
                Small_Aperture_Score_10mm = planSmallApertureScore10mm;
                Small_Aperture_Score_20mm = planSmallApertureScore20mm;

                MeanFieldArea planMFDObj = new MeanFieldArea();
                double planMFD = planMFDObj.CalculateForPlan(patient, ps);
                Mean_Field_Area = planMFD;

                MeanAsymmetryDistance planMSDObj = new MeanAsymmetryDistance();
                double planMSD = planMSDObj.CalculateForPlan(patient, ps);
                Mean_Asymmetry_Distance = planMSD;

                ApertureAreaRationJawArea planApertureAreaRatioJawAreaObj = new ApertureAreaRationJawArea();
                double planApertureAreaRatioJawArea = planApertureAreaRatioJawAreaObj.CalculateForPlan(patient, ps);
                Aperture_Area_Ration_Jaw_Area = planApertureAreaRatioJawArea;

                ApertureSubRegions planApertureSubRegionsObj = new ApertureSubRegions();
                double planApertureSubRegions = planApertureSubRegionsObj.CalculateForPlan(patient, ps);
                Aperture_Sub_Regions = planApertureSubRegions;

                ApertureXJawDistance planApertureXJawDistanceObj = new ApertureXJawDistance();
                double planApertureXJawDistance = planApertureXJawDistanceObj.CalculateForPlan(patient, ps);
                Aperture_X_Jaw_Distance = planApertureXJawDistance;

                ApertureYJawDistance planApertureYJawDistanceObj = new ApertureYJawDistance();
                double planApertureYJawDistance = planApertureYJawDistanceObj.CalculateForPlan(patient, ps);
                Aperture_Y_Jaw_Distance = planApertureYJawDistance;

                LeafGap planLeafGapObj = new LeafGap();
                Dictionary<string, double> planLeafGap = planLeafGapObj.CalculateForPlanDictionary(patient, ps);
                Leaf_Gap_Average = planLeafGap["Average"];
                Leaf_Gap_Std = planLeafGap["Std"];

                LeafTravel planLeafTravelObj = new LeafTravel();
                double planLeafTravel = planLeafTravelObj.CalculateForPlan(patient, ps);
                Leaf_Travel = planLeafTravel;

                ConvertedApertureMetric camObj = new ConvertedApertureMetric();
                double planCam = camObj.CalculateForPlan(patient, ps);
                Converted_Aperture_Metric = planCam;

                EdgeAreaMetric eamObj = new EdgeAreaMetric();
                double planEam = eamObj.CalculateForPlan(patient, ps);
                Edge_Area_Metric = planEam;

                if (tech.Contains("ARC")) // VMAT
                {
                    // VMAT
                    PlanTechnicalName = "VMAT";
                                       
                    ProportionMLCSpeed planMLCSpeedObj = new ProportionMLCSpeed();
                    Dictionary<string, double> planMLCSpeed = planMLCSpeedObj.CalculateForPlanDictionary(patient, ps);
                    Speed_0_4 = planMLCSpeed["Speed (0, 4)"];
                    Speed_4_8 = planMLCSpeed["Speed (4, 8)"];
                    Speed_8_12 = planMLCSpeed["Speed (8, 12)"];
                    Speed_12_16 = planMLCSpeed["Speed (12, 16)"];
                    Speed_16_20 = planMLCSpeed["Speed (16, 20)"];
                    Speed_20_25 = planMLCSpeed["Speed (20, 25)"];
                    Speed_Average = planMLCSpeed["Speed Average"];
                    Speed_Std = planMLCSpeed["Speed Std"];

                    ProportionMLCAccelerate planMLCAccelerateObj = new ProportionMLCAccelerate();
                    Dictionary<string, double> planMLCAcc = planMLCAccelerateObj.CalculateForPlanDictionary(patient, ps);
                    Acc_0_10 = planMLCAcc["Acc (0, 10)"];
                    Acc_10_20 = planMLCAcc["Acc (10, 20)"];
                    Acc_20_40 = planMLCAcc["Acc (20, 40)"];
                    Acc_40_60 = planMLCAcc["Acc (40, 60)"];
                    Acc_Average = planMLCAcc["Acc Average"];
                    Acc_std = planMLCAcc["Acc Std"];

                    StationParameterOptimizedRadiationTherapy miSPORTObj = new StationParameterOptimizedRadiationTherapy();
                    double planMiSPORT = miSPORTObj.CalculateForPlan(patient, ps);
                    SPORT = planMiSPORT;
                }
                else
                {
                    // IMRT
                    PlanTechnicalName = "IMRT";
                }
            }
            else
            {
                // TOTAL HDTSE
                // Do Noting
                return;
            }
        }

        public string ToJson() 
        {
            // Output to json string
            // using Newtonsoft.Json
            return Newtonsoft.Json.JsonConvert.SerializeObject(this);
        }
    }
}
