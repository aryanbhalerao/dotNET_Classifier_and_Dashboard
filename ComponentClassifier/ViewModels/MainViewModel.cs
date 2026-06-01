using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ComponentClassifier.Models;
using LiveCharts;
using LiveCharts.Wpf;
using Newtonsoft.Json;
using System;
using System.Globalization;

namespace ComponentClassifier.ViewModels
{
    public partial class MainViewModel : ObservableObject
    {
        [ObservableProperty]
        private string _currentFilePath = "Readings.json";

        [ObservableProperty]
        private ObservableCollection<Reading> _classifiedReadings = new();

        [ObservableProperty] private int _totalComponentsFed;
        [ObservableProperty] private int _totalFaultsDetected;
        [ObservableProperty] private int _needInspection;
        [ObservableProperty] private int _faulty;
        [ObservableProperty] private string _passPercentage = string.Empty;
        [ObservableProperty] private string _inspectPercentage = string.Empty;
        [ObservableProperty] private string _failPercentage = string.Empty;

        [ObservableProperty] private int _nutsFed;
        [ObservableProperty] private int _fastenersFed;
        [ObservableProperty] private int _faultsInNuts;
        [ObservableProperty] private int _faultsInFasteners;

        [ObservableProperty] private SeriesCollection _resultPieChartSeries = new();
        [ObservableProperty] private SeriesCollection _typeBarChartSeries = new();
        [ObservableProperty] private string[] _typeBarChartLabels = Array.Empty<string>();
        [ObservableProperty] private SeriesCollection _faultsLineChartSeries = new();
        [ObservableProperty] private string[] _faultsLineChartLabels = Array.Empty<string>();

        public MainViewModel()
        {
            LoadAndProcessData(CurrentFilePath);
        }

        [RelayCommand]
        private void BrowseFile()
        {
            var dialog = new Microsoft.Win32.OpenFileDialog
            {
                Filter = "JSON files (*.json)|*.json|All files (*.*)|*.*",
                Title = "Select Readings JSON file"
            };
            if (dialog.ShowDialog() == true)
            {
                CurrentFilePath = dialog.FileName;
                LoadAndProcessData(CurrentFilePath);
            }
        }

        private void LoadAndProcessData(string filePath)
        {
            var rawReadings = new List<Reading>();
            try
            {
                var jsonText = File.ReadAllText(filePath);
                rawReadings = JsonConvert.DeserializeObject<List<Reading>>(jsonText) ?? new List<Reading>();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error reading or parsing {Path.GetFileName(filePath)}: {ex.Message}");
                return;
            }

            var nuts = new HashSet<string> { "HexNut", "CapNut", "WingNut", "AcornNut" };
            var fasteners = new HashSet<string> { "HexBolt", "MachineScrew", "FlatScrew", "WoodScrew" };
            var processedReadings = new List<Reading>();

            foreach (var reading in rawReadings)
            {
                if (reading.Fault < 7) reading.Result = "PASS";
                else if (reading.Fault >= 7 && reading.Fault <= 30) reading.Result = "INSPECT";
                else reading.Result = "FAIL";

                if (nuts.Contains(reading.Component)) reading.Type = "Nuts";
                else if (fasteners.Contains(reading.Component)) reading.Type = "Fasteners";
                else reading.Type = "Unknown";

                processedReadings.Add(reading);
            }

            processedReadings = processedReadings
                .OrderBy(r => DateTime.ParseExact(r.TimeStamp, "ddMMyyyy-HHmmss", CultureInfo.InvariantCulture))
                .ToList();

            ClassifiedReadings = new ObservableCollection<Reading>(processedReadings);
            CalculateDashboardMetrics(processedReadings);
            PrepareChartData(processedReadings);
        }

        private void CalculateDashboardMetrics(List<Reading> readings)
        {
            TotalComponentsFed = readings.Count;

            var passCount = readings.Count(r => r.Result == "PASS");
            NeedInspection = readings.Count(r => r.Result == "INSPECT");
            Faulty = readings.Count(r => r.Result == "FAIL");
            TotalFaultsDetected = NeedInspection + Faulty;

            if (TotalComponentsFed > 0)
            {
                PassPercentage = $"{(double)passCount / TotalComponentsFed:P2}";
                InspectPercentage = $"{(double)NeedInspection / TotalComponentsFed:P2}";
                FailPercentage = $"{(double)Faulty / TotalComponentsFed:P2}";
            }
            else
            {
                PassPercentage = "0.00%";
                InspectPercentage = "0.00%";
                FailPercentage = "0.00%";
            }

            NutsFed = readings.Count(r => r.Type == "Nuts");
            FastenersFed = readings.Count(r => r.Type == "Fasteners");
            FaultsInNuts = readings.Count(r => r.Type == "Nuts" && r.Result != "PASS");
            FaultsInFasteners = readings.Count(r => r.Type == "Fasteners" && r.Result != "PASS");
        }

        private void PrepareChartData(List<Reading> readings)
        {
            ResultPieChartSeries = new SeriesCollection
            {
                new PieSeries { Title = "PASS", Values = new ChartValues<int> { readings.Count(r => r.Result == "PASS") }, DataLabels = true },
                new PieSeries { Title = "INSPECT", Values = new ChartValues<int> { readings.Count(r => r.Result == "INSPECT") }, DataLabels = true },
                new PieSeries { Title = "FAIL", Values = new ChartValues<int> { readings.Count(r => r.Result == "FAIL") }, DataLabels = true }
            };

            TypeBarChartLabels = new[] { "Nuts", "Fasteners" };
            TypeBarChartSeries = new SeriesCollection
            {
                new ColumnSeries
                {
                    Title = "Faulty Components",
                    Values = new ChartValues<int>
                    {
                        readings.Count(r => r.Type == "Nuts" && r.Result != "PASS"),
                        readings.Count(r => r.Type == "Fasteners" && r.Result != "PASS")
                    }
                }
            };

            FaultsLineChartSeries = new SeriesCollection
            {
                new LineSeries
                {
                    Title = "Fault %",
                    Values = new ChartValues<double>(readings.Select(r => r.Fault))
                }
            };
            FaultsLineChartLabels = readings
                .Select(r => DateTime.ParseExact(r.TimeStamp, "ddMMyyyy-HHmmss", CultureInfo.InvariantCulture).ToString("g"))
                .ToArray();
        }
    }
}
