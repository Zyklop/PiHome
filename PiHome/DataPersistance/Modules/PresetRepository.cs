using System;
using System.Collections.Generic;
using System.Linq;
using DataPersistance.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Internal;

namespace DataPersistance.Modules
{
    public class PresetRepository
    {
        private readonly PiHomeContext context;

        public PresetRepository(PiHomeContext context)
        {
            this.context = context;
        }

        public List<LedValue> GetAllLeds()
        {
            return context.Leds.AsNoTracking().Select(x => new LedValue
            {
                Id = x.Id,
                ModuleId = x.ModuleId,
                X = x.Position.X,
                Y = x.Position.Y,
                Index = x.Index,
                Color = new Color()
            }).ToList();
        }

        public ILookup<Module, LedValue> GetPreset(string name)
        {
            return context.LedPresets.AsNoTracking()
                .Where(x => x.Name == name)
                .SelectMany(x => x.LedPresetValues)
                .Select(x => new
            {
                Value = new LedValue
                {
                    Color = new Color(x.Color),
                    ModuleId = x.Led.ModuleId,
                    Index = x.Led.Index,
                    X = x.Led.Position.X,
                    Y = x.Led.Position.Y,
                    Id = x.LedId
                },
                Module = x.Led.Module
            })
                .ToLookup(x => x.Module, x => x.Value);
        }

        public void UpdatePresetActivation(PresetActivation presetActivation, string presetName)
        {
            var preset = context.LedPresets.Single(x => x.Name == presetName);
            var activation = context.PresetActivations.SingleOrDefault(x => x.Id == presetActivation.Id);
            if (activation == null)
            {
                presetActivation.Preset = preset;
                CalculateNextActivation(ref presetActivation);
                context.PresetActivations.Add(presetActivation);
            }
            else
            {
                activation.Preset = preset;
                activation.ActivationTime = presetActivation.ActivationTime;
                activation.Active = presetActivation.Active;
                activation.DaysOfWeek = presetActivation.DaysOfWeek;
                CalculateNextActivation(ref activation);
            }
            context.SaveChanges();
        }

        public PresetActivation[] GetAllPresetActivations()
        {
            return context.PresetActivations.Include(x => x.Preset).AsNoTracking().ToArray();
        }

        public string? GetPresetToActivate()
        {
            var pa = context.PresetActivations.Include(x => x.Preset)
                .FirstOrDefault(x => x.NextActivationTime < DateTime.Now);
            if (pa != null)
            {
                CalculateNextActivation(ref pa);
                context.SaveChanges();
                return pa.Preset.Name;
            }
            return null;
        }

        private void CalculateNextActivation(ref PresetActivation presetActivation)
        {
            if (!presetActivation.Active)
            {
                presetActivation.NextActivationTime = DateTime.MaxValue;
            }
            var res = DateTime.Today;
            var currentWeekday = res.DayOfWeek;
            for (int i = 1; i <= 8; i++)
            {
                if (i == 8)
                {
                    res = res.AddDays(1);
                    presetActivation.Active = false;
                    break;
                }
                var index = i + (int)currentWeekday;
                if (index >= 7)
                {
                    index -= 7;
                }
                if (presetActivation.DaysOfWeek[index])
                {
                    res = res.AddDays(i);
                    break;
                }
            }
            presetActivation.NextActivationTime = res;
        }

        public Dictionary<int, string> GetAllPresets()
        {
            return context.LedPresets.AsNoTracking().ToDictionary(x => x.Id, x => x.Name);
        }

        public void SavePreset(string name, IEnumerable<LedValue> ledValues, DateTime changeDate)
        {
            var leds = ledValues.ToArray();
            var preset = context.LedPresets.Include(x => x.LedPresetValues).SingleOrDefault(x => x.Name == name);
            if (preset != null && changeDate < preset.ChangeDate)
            {
                return;
            }

            using var trans = context.Database.BeginTransaction();
            if (preset == null)
            {
                preset = new LedPreset { Name = name, ChangeDate = DateTime.SpecifyKind(changeDate, DateTimeKind.Unspecified)};
                context.LedPresets.Add(preset);
            }
            else
            {
                context.LedPresetValues.RemoveRange(preset.LedPresetValues.Where(x => leds.All(y => y.Id != x.LedId)));
            }
            try
            {
                foreach (var led in leds)
                {
                    var value = preset.LedPresetValues.SingleOrDefault(x => x.LedId == led.Id);
                    if (value == null)
                    {
                        value = new LedPresetValue
                        { LedId = led.Id, Preset = preset, Color = led.Color.ToRGBB() };
                        preset.LedPresetValues.Add(value);
                        context.LedPresetValues.Add(value);
                    }
                    else
                    {
                        value.Color = led.Color.ToRGBB();
                    }
                }

                context.SaveChanges();
                trans.Commit();
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                trans.Rollback();
                throw;
            }
        }

        public LedValue[] ToLedValues(IEnumerable<LedPresetDto> dtoValues)
        {
            var modules = context.Modules.AsNoTracking().ToDictionary(x => x.Name, x => x.Id);
            var ledsFromDb = context.Leds.AsTracking().GroupBy(x => x.ModuleId).ToDictionary(x => x.Key, x => x.ToDictionary(y => y.Index, y => y.Id));
            return dtoValues.Select(x =>
            {
                var mId = modules[x.ModuleName];
                return new LedValue
                {
                    Color = x.Color,
                    ModuleId = mId,
                    Id = ledsFromDb[mId][x.Led.Index],
                    Index = x.Led.Index,
                    X = x.Led.X,
                    Y = x.Led.Y
                };
            }).ToArray();
        }

        public void DeletePreset(string name)
        {
            var preset = context.LedPresets.Single(x => x.Name == name);
            context.LedPresets.Remove(preset);
            context.SaveChanges();
        }
    }

    public class LedValue
    {
        public int Index { get; set; }
        public int ModuleId { get; set; }
        public string ModuleName { get; set; }
        public Color Color { get; set; }
        public int Id { get; set; }
        public double X { get; set; }
        public double Y { get; set; }
    }

    public class Color
    {
        public byte R { get; set; }
        public byte G { get; set; }
        public byte B { get; set; }
        public byte Brightness { get; set; }

        public Color()
        {

        }

        public Color(byte[] fromDb)
        {
            Brightness = fromDb[3];
            R = fromDb[0];
            G = fromDb[1];
            B = fromDb[2];
        }

        public byte[] ToRGBB()
        {
            return new[] { R, G, B, Brightness };
        }

        public bool Active => R == 0 && G == 0 && B == 0;
    }

    public class PresetDto
    {
        public string Name { get; set; }
        public DateTime LastChangeDate { get; set; }
        public LedPresetDto[] LedValues { get; set; }
    }

    public class LedPresetDto
    {
        public string ModuleName { get; set; }
        public Color Color { get; set; }
        public LedDto Led { get; set; }
    }
}