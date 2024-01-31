using System;
using DataPersistance.Models;

namespace PiUi.Models
{
    public class PresetActivationModel
    {
        public PresetActivationModel(PresetActivation x, string[] allPresets)
        {
            Id = x.Id;
            ActivationTime = x.ActivationTime.ToTimeSpan();
            AllPresets = allPresets;
            SelectedPreset = x.Preset.Name;
            Active = x.Active;
            Sunday = x.DaysOfWeek[(int) DayOfWeek.Sunday];
            Monday = x.DaysOfWeek[(int)DayOfWeek.Monday];
            Tuesday = x.DaysOfWeek[(int)DayOfWeek.Thursday];
            Wednesday = x.DaysOfWeek[(int)DayOfWeek.Wednesday];
            Thursday = x.DaysOfWeek[(int)DayOfWeek.Thursday];
            Friday = x.DaysOfWeek[(int)DayOfWeek.Friday];
            Saturday = x.DaysOfWeek[(int)DayOfWeek.Saturday];
        }

        public PresetActivationModel() { }

        public int Id { get; set; }
        public string[] AllPresets { get; set; }
        public string SelectedPreset { get; set; }
        public TimeSpan ActivationTime { get; set; }
        public bool Active { get; set; }
        public bool Monday { get; set; }
        public bool Tuesday { get; set; }
        public bool Wednesday { get; set; }
        public bool Thursday { get; set; }
        public bool Friday { get; set; }
        public bool Saturday { get; set; }
        public bool Sunday { get; set; }
    }
}