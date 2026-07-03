using System;
using System.Collections.Generic;
using System.IO;
using System.Windows;
using iNKORE.UI.WPF.Modern.Controls;
using Newtonsoft.Json;
using Page = iNKORE.UI.WPF.Modern.Controls.Page;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Media;

namespace ExamSettings.Pages
{
    public class TimeSlot : INotifyPropertyChanged
    {
        private string name = string.Empty;
        private string startDate = string.Empty;
        private string startTime = string.Empty;
        private string endDate = string.Empty;
        private string endTime = string.Empty;

        public string Name
        {
            get => name;
            set => SetField(ref name, value);
        }
        public string StartDate
        {
            get => startDate;
            set => SetField(ref startDate, value);
        }
        public string StartTime
        {
            get => startTime;
            set => SetField(ref startTime, value);
        }
        public string EndDate
        {
            get => endDate;
            set => SetField(ref endDate, value);
        }
        public string EndTime
        {
            get => endTime;
            set => SetField(ref endTime, value);
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        protected bool SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
        {
            if (EqualityComparer<T>.Default.Equals(field, value)) return false;
            field = value;
            OnPropertyChanged(propertyName);
            return true;
        }
    }

    public partial class ExamEditer : Page
    {
        private ObservableCollection<TimeSlot>? timeSlots;
        private string? dataFilePath;

        public ExamEditer()
        {
            // Ensure XAML controls are created before any code that accesses them
            InitializeComponent();

            InitializeData();
            LoadData();
        }

        private void InitializeData()
        {
            string appDir = AppDomain.CurrentDomain.BaseDirectory;
            string dataDir = Path.Combine(appDir, "Data");
            if (!Directory.Exists(dataDir))
            {
                Directory.CreateDirectory(dataDir);
            }
            dataFilePath = Path.Combine(dataDir, "Default.json");
            timeSlots = new ObservableCollection<TimeSlot>();
        }

        private void LoadData()
        {
            try
            {
                if (!string.IsNullOrEmpty(dataFilePath) && File.Exists(dataFilePath))
                {
                    string json = File.ReadAllText(dataFilePath);
                    var list = JsonConvert.DeserializeObject<List<TimeSlot>>(json) ?? new List<TimeSlot>();
                    timeSlots = new ObservableCollection<TimeSlot>(list);
                }
                else
                {
                    // 初始化默认数据
                    string today = DateTime.Today.ToString("yyyy-MM-dd");
                    timeSlots = new ObservableCollection<TimeSlot>
                    {
                        new TimeSlot { Name = "新时间段示例", StartDate = today, StartTime = "08:00", EndDate = today, EndTime = "08:45" },
                    };
                    SaveData();
                }

                UpdateDataGrid();
            }
            catch (Exception ex)
            {
                ShowStatus($"加载数据失败: {ex.Message}", false);
                timeSlots = new ObservableCollection<TimeSlot>();
            }
        }

        private void SaveData()
        {
            try
            {
                if (timeSlots != null && dataFilePath != null)
                {
                    // Convert to List for serialization
                    var list = new List<TimeSlot>(timeSlots);
                    string json = JsonConvert.SerializeObject(list, Formatting.Indented);
                    File.WriteAllText(dataFilePath, json);
                    ShowStatus("数据保存成功", true);
                }
            }
            catch (Exception ex)
            {
                ShowStatus($"保存数据失败: {ex.Message}", false);
            }
        }

        private void UpdateDataGrid()
        {
            // Bind the ObservableCollection so UI updates automatically on changes
            if (dgTimeSlots != null)
            {
                dgTimeSlots.ItemsSource = timeSlots;
            }
        }

        private bool CheckTimeOverlap(TimeSlot newSlot, TimeSlot? existingSlot = null)
        {
            // 组合日期和时间进行比较
            DateTime newStartDateTime = DateTime.Parse(newSlot.StartDate).Date.Add(TimeSpan.Parse(newSlot.StartTime));
            DateTime newEndDateTime = DateTime.Parse(newSlot.EndDate).Date.Add(TimeSpan.Parse(newSlot.EndTime));

            if (timeSlots != null)
            {
                foreach (var slot in timeSlots)
                {
                    if (slot == existingSlot) continue;

                    DateTime slotStartDateTime = DateTime.Parse(slot.StartDate).Date.Add(TimeSpan.Parse(slot.StartTime));
                    DateTime slotEndDateTime = DateTime.Parse(slot.EndDate).Date.Add(TimeSpan.Parse(slot.EndTime));

                    if ((newStartDateTime >= slotStartDateTime && newStartDateTime < slotEndDateTime) ||
                        (newEndDateTime > slotStartDateTime && newEndDateTime <= slotEndDateTime) ||
                        (newStartDateTime <= slotStartDateTime && newEndDateTime >= slotEndDateTime))
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        private void ShowStatus(string message, bool isSuccess)
        {
            // Prefer InfoBar if available in XAML
            if (InfoBar1 != null)
            {
                // Ensure UI thread
                Dispatcher.Invoke(() =>
                {
                    InfoBar1.Title = isSuccess ? "成功" : "提示";
                    InfoBar1.Message = message;
                    InfoBar1.Severity = isSuccess ? InfoBarSeverity.Success : InfoBarSeverity.Warning;
                    InfoBar1.IsOpen = true;
                });
                return;
            }

            // Fallback to previous TextBlock-based status
            if (txtStatus == null)
            {
                System.Diagnostics.Debug.WriteLine(message);
                return;
            }

            Dispatcher.Invoke(() =>
            {
                txtStatus.Text = message;
                txtStatus.Foreground = isSuccess ? Brushes.Green : Brushes.Red;
            });
        }

        private void btnAdd_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new TimeSlotDialog();
            var result = dialog.ShowAsync();
            result.ContinueWith(task =>
            {
                if (task.Result == ContentDialogResult.Primary && timeSlots != null && dialog.TimeSlot != null)
                {
                    if (CheckTimeOverlap(dialog.TimeSlot))
                    {
                        Dispatcher.Invoke(() =>
                        {
                            ShowStatus("时间段与现有时间段重叠", false);
                        });
                        return;
                    }

                    // Ensure collection modification is done on UI thread
                    Dispatcher.Invoke(() =>
                    {
                        timeSlots.Add(dialog.TimeSlot);
                        UpdateDataGrid();
                        SaveData();
                        ShowStatus("添加时间段成功", true);
                    });
                }
            });
        }

        private void btnEdit_Click(object sender, RoutedEventArgs e)
        {
            if (dgTimeSlots.SelectedItem is not TimeSlot selectedSlot)
            {
                ShowStatus("请选择要编辑的时间段", false);
                return;
            }

            var dialog = new TimeSlotDialog(selectedSlot);
            var result = dialog.ShowAsync();
            result.ContinueWith(task =>
            {
                if (task.Result == ContentDialogResult.Primary && dialog.TimeSlot != null)
                {
                    if (CheckTimeOverlap(dialog.TimeSlot, selectedSlot))
                    {
                        Dispatcher.Invoke(() =>
                        {
                            ShowStatus("时间段与现有时间段重叠", false);
                        });
                        return;
                    }

                    Dispatcher.Invoke(() =>
                    {
                        // Properties on selectedSlot will raise PropertyChanged; just save
                        UpdateDataGrid();
                        SaveData();
                        ShowStatus("编辑时间段成功", true);
                    });
                }
            });
        }

        private void btnDelete_Click(object sender, RoutedEventArgs e)
        {
            if (dgTimeSlots.SelectedItem is not TimeSlot selectedSlot || string.IsNullOrEmpty(selectedSlot.Name))
            {
                ShowStatus("请选择要删除的时间段", false);
                return;
            }

            var dialog = new ContentDialog
            {
                Title = "是否要删除",
                Content = $"确定要删除时间段 '{selectedSlot.Name}' 吗？",
                PrimaryButtonText = "确定",
                SecondaryButtonText = "取消"
            };

            var result = dialog.ShowAsync();
            result.ContinueWith(task =>
            {
                if (task.Result == ContentDialogResult.Primary && timeSlots != null)
                {
                    Dispatcher.Invoke(() =>
                    {
                        timeSlots.Remove(selectedSlot);
                        UpdateDataGrid();
                        SaveData();
                        ShowStatus("删除时间段成功", true);
                    });
                }
            });
        }

        private void btnSave_Click(object sender, RoutedEventArgs e)
        {
            SaveData();
        }

        private void AppBarButton_Click(object? sender, RoutedEventArgs e)
        {
            // 保留原有方法以避免编译错误
        }
    }
}