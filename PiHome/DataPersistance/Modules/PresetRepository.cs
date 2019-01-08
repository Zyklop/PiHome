using System;
using System.Collections.Generic;
using System.Linq;
using DataPersistance.Models;
using Microsoft.EntityFrameworkCore;

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
				return context.Led.Select(x => new LedValue
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
				return context.LedPreset.Where(x => x.Name == name).SelectMany(x => x.LedPresetValues).GroupBy(x => x.Led.Module, x =>
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

		public List<string> GetAllPresets()
		{
			using (var context = new PiHomeContext())
			{
				return context.LedPreset.Select(x => x.Name).ToList();
			}
		}

		public void SavePreset(string name, IEnumerable<LedValue> leds)
		{
			using (var context = new PiHomeContext())
			{
				var preset = context.LedPreset.Include(x => x.LedPresetValues).SingleOrDefault(x => x.Name == name);
				using (var trans = context.Database.BeginTransaction())
				{
					if (preset == null)
					{
						preset = new LedPreset { Name = name, ChangeDate = DateTime.UtcNow };
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
}