using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataPersistance.Models
{
    public partial class Button
    {
        public override bool Equals(object? obj)
        {
            if (obj is Button other)
            {
                return other.Id == Id;
            }
            return base.Equals(obj);
        }

        public override int GetHashCode()
        {
            return Id.GetHashCode();
        }
    }
    public partial class ButtonMapping
    {
        public override bool Equals(object? obj)
        {
            if (obj is ButtonMapping other)
            {
                return other.ButtonId == ButtonId && other.ActionId == ActionId;
            }
            return base.Equals(obj);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(ButtonId.GetHashCode(), ActionId.GetHashCode());
        }
    }

    public partial class Feature
    {
        public override bool Equals(object? obj)
        {
            if (obj is Feature other)
            {
                return other.Id == Id;
            }
            return base.Equals(obj);
        }

        public override int GetHashCode()
        {
            return Id.GetHashCode();
        }
    }

    public partial class Led
    {
        public override bool Equals(object? obj)
        {
            if (obj is Led other)
            {
                return other.Id == Id;
            }
            return base.Equals(obj);
        }

        public override int GetHashCode()
        {
            return Id.GetHashCode();
        }
    }

    public partial class LedPreset
    {
        public override bool Equals(object? obj)
        {
            if (obj is LedPreset other)
            {
                return other.Id == Id;
            }
            return base.Equals(obj);
        }

        public override int GetHashCode()
        {
            return Id.GetHashCode();
        }
    }

    public partial class LedPresetValue
    {
        public override bool Equals(object? obj)
        {
            if (obj is LedPresetValue other)
            {
                return other.Id == Id;
            }
            return base.Equals(obj);
        }

        public override int GetHashCode()
        {
            return Id.GetHashCode();
        }
    }

    public partial class Log
    {
        public override bool Equals(object? obj)
        {
            if (obj is Log other)
            {
                return other.Id == Id;
            }
            return base.Equals(obj);
        }

        public override int GetHashCode()
        {
            return Id.GetHashCode();
        }
    }

    public partial class LogConfiguration
    {
        public override bool Equals(object? obj)
        {
            if (obj is LogConfiguration other)
            {
                return other.Id == Id;
            }
            return base.Equals(obj);
        }

        public override int GetHashCode()
        {
            return Id.GetHashCode();
        }
    }

    public partial class Module
    {
        public override bool Equals(object? obj)
        {
            if (obj is Module other)
            {
                return other.Id == Id;
            }
            return base.Equals(obj);
        }

        public override int GetHashCode()
        {
            return Id.GetHashCode();
        }
    }

    public partial class PresetActivation
    {
        public override bool Equals(object? obj)
        {
            if (obj is PresetActivation other)
            {
                return other.Id == Id;
            }
            return base.Equals(obj);
        }

        public override int GetHashCode()
        {
            return Id.GetHashCode();
        }
    }
}
