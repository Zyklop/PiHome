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
		private ModuleFactory mf = new ModuleFactory();

		public PresetRepository()
		{
		}

		public List<LedValue> GetAllLeds()
		{
			using (var context = new PiHomeContext())
			{
				return context.Led.AsNoTracking().Select(x => new LedValue
				{
					Id = x.Id, ModuleId = x.ModuleId, X = x.Position.X, Y = x.Position.Y, Index = x.Index,
					Color = new Color()
				}).ToList();
			}
		}

		public Dictionary<Module, LedValue[]> GetPreset(string name)
		{
			using (var context = new PiHomeContext())
			{
				return context.LedPreset.AsNoTracking().Where(x => x.Name == name).SelectMany(x => x.LedPresetValues).GroupBy(x => x.Led.Module, x =>
					new LedValue
					{
						Color = new Color(x.Color),
						ModuleId = x.Led.ModuleId,
						Index = x.Led.Index,
						X = x.Led.Position.X,
						Y = x.Led.Position.Y,
						Id = x.LedId
					}).ToDictionary(x => x.Key, x => x.ToArray());
			}
		}

		public PresetDto GetPresetDto(string name)
		{
			using (var context = new PiHomeContext())
			{
				return context.LedPreset.AsNoTracking().Where(x => x.Name == name).Include(x => x.LedPresetValues).ThenInclude(x => x.Led).Select(x => new PresetDto
				{
					Name = x.Name, LastChangeDate = x.ChangeDate,
					LedValues = x.LedPresetValues.Select(y => new LedPresetDto
					{
						ModuleName = y.Led.Module.Name,
						Led = new LedDto {X = y.Led.Position.X, Index = y.Led.Index, Y = y.Led.Position.Y},
						Color = new Color(y.Color)
					}).ToArray()
				}).SingleOrDefault();
			}
		}

		public void UpdatePresetActivation(PresetActivation presetActivation)
		{
			using (var context = new PiHomeContext())
			{
				var preset = context.PresetActivation.SingleOrDefault(x => x.Id == presetActivation.Id);
				if (preset == null)
				{
                    CalculateNextActivation(ref presetActivation);
					context.PresetActivation.Add(presetActivation);
				}
				else
				{
					preset.ActivationTime = presetActivation.ActivationTime;
					preset.Active = presetActivation.Active;
					preset.DaysOfWeek = presetActivation.DaysOfWeek;
					preset.PresetId = presetActivation.PresetId;
				    CalculateNextActivation(ref preset);
                }
				context.SaveChanges();
			}
		}

	    public string GetPresetToActivate()
	    {
	        using (var context = new PiHomeContext())
	        {
	            var pa = context.PresetActivation.Include(x => x.Preset)
	                .FirstOrDefault(x => x.NextActivationTime < DateTime.UtcNow);
	            if (pa != null)
	            {
	                CalculateNextActivation(ref pa);
	                context.SaveChanges();
	                return pa.Preset.Name;
	            }
                return null;
	        }
	    }

	    private void CalculateNextActivation(ref PresetActivation presetActivation)
	    {
	        if (!presetActivation.Active[0])
	        {
	            presetActivation.NextActivationTime = DateTime.MaxValue;
	        }
            var res = DateTime.Today;
	        res += presetActivation.ActivationTime;
	        var currentWeekday = res.DayOfWeek;
	        for (int i = 1; i <= 8; i++)
	        {
	            if (i == 8)
	            {
	                res = res.AddDays(1);
	                presetActivation.Active[0] = false;
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

	    public DateTime GetPresetChangeDate(string name)
		{
			using (var context = new PiHomeContext())
			{
				return context.LedPreset.AsNoTracking().SingleOrDefault(x => x.Name == name)?.ChangeDate ?? DateTime.MinValue;
			}
		}

		public List<string> GetAllPresets()
		{
			using (var context = new PiHomeContext())
			{
				return context.LedPreset.AsNoTracking().Select(x => x.Name).ToList();
			}
		}

		public void SavePreset(string name, IEnumerable<LedValue> ledValues, DateTime changeDate)
		{
			var leds = ledValues.ToArray();
			using (var context = new PiHomeContext())
			{
				var preset = context.LedPreset.Include(x => x.LedPresetValues).SingleOrDefault(x => x.Name == name);
				if (preset != null && changeDate < preset.ChangeDate)
				{
					return;
				}
				using (var trans = context.Database.BeginTransaction())
				{
					if (preset == null)
					{
						preset = new LedPreset { Name = name, ChangeDate = changeDate };
						context.LedPreset.Add(preset);
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
								value = new LedPresetValues
									{LedId = led.Id, Preset = preset, Color = led.Color.ToRGBB()};
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
			}
		}

		public LedValue[] ToLedValues(IEnumerable<LedPresetDto> dtoValues)
		{
			using (var context = new PiHomeContext())
			{
				var modules = context.Module.AsNoTracking().ToDictionary(x => x.Name, x => x.Id);
				var ledsFromDb = context.Led.AsTracking().GroupBy(x => x.ModuleId).ToDictionary(x => x.Key, x => x.ToDictionary(y => y.Index, y => y.Id));
				return dtoValues.Select(x =>
				{
					var mId = modules[x.ModuleName];
					return new LedValue
					{
						Color = x.Color, ModuleId = mId, Id = ledsFromDb[mId][x.Led.Index], Index = x.Led.Index,
						X = x.Led.X, Y = x.Led.Y
					};
				}).ToArray();
			}
		}

		public void DeletePreset(string name)
		{
			using (var context = new PiHomeContext())
			{
				var preset = context.LedPreset.Single(x => x.Name == name);
				context.LedPreset.Remove(preset);
				context.SaveChanges();
			}
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
			return new[] {R, G, B, Brightness};
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