using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace MuMech
{
    public class MuUtils
    {
        private static GUISkin _defaultSkin;
        public static GUISkin DefaultSkin
        {
            get
            {
                if (_defaultSkin == null)
                {
                    _defaultSkin = AssetBase.GetGUISkin("KSP window 2");
                }
                return _defaultSkin;
            }
        }

        public static void SelectDefaultSkin(string skin)
        {
            _defaultSkin = AssetBase.GetGUISkin(skin);
        }

        public static string ToSI(double d, int digits = 3, int MinMagnitude = 0, int MaxMagnitude = int.MaxValue)
        {
            float exponent = (float)Math.Log10(Math.Abs(d));
            exponent = Mathf.Clamp(exponent, (float)MinMagnitude, (float)MaxMagnitude);

            if (exponent >= 0)
            {
                switch ((int)Math.Floor(exponent))
                {
                    case 0:
                    case 1:
                    case 2:
                        return d.ToString("F" + digits);
                    case 3:
                    case 4:
                    case 5:
                        return (d / 1e3).ToString("F" + digits) + "k";
                    case 6:
                    case 7:
                    case 8:
                        return (d / 1e6).ToString("F" + digits) + "M";
                    case 9:
                    case 10:
                    case 11:
                        return (d / 1e9).ToString("F" + digits) + "G";
                    case 12:
                    case 13:
                    case 14:
                        return (d / 1e12).ToString("F" + digits) + "T";
                    case 15:
                    case 16:
                    case 17:
                        return (d / 1e15).ToString("F" + digits) + "P";
                    case 18:
                    case 19:
                    case 20:
                        return (d / 1e18).ToString("F" + digits) + "E";
                    case 21:
                    case 22:
                    case 23:
                        return (d / 1e21).ToString("F" + digits) + "Z";
                    default:
                        return (d / 1e24).ToString("F" + digits) + "Y";
                }
            }
            else if (exponent < 0)
            {
                switch ((int)Math.Floor(exponent))
                {
                    case -1:
                    case -2:
                    case -3:
                        return (d * 1e3).ToString("F" + digits) + "m";
                    case -4:
                    case -5:
                    case -6:
                        return (d * 1e6).ToString("F" + digits) + "μ";
                    case -7:
                    case -8:
                    case -9:
                        return (d * 1e9).ToString("F" + digits) + "n";
                    case -10:
                    case -11:
                    case -12:
                        return (d * 1e12).ToString("F" + digits) + "p";
                    case -13:
                    case -14:
                    case -15:
                        return (d * 1e15).ToString("F" + digits) + "f";
                    case -16:
                    case -17:
                    case -18:
                        return (d * 1e18).ToString("F" + digits) + "a";
                    case -19:
                    case -20:
                    case -21:
                        return (d * 1e21).ToString("F" + digits) + "z";
                    default:
                        return (d * 1e24).ToString("F" + digits) + "y";
                }
            }
            else
            {
                return "0";
            }
        }

        public static Vector3d Invert(Vector3d vector)
        {
            return new Vector3d(1 / vector.x, 1 / vector.y, 1 / vector.z);
        }

        public static Vector3d Sign(Vector3d vector)
        {
            return new Vector3d(Math.Sign(vector.x), Math.Sign(vector.y), Math.Sign(vector.z));
        }

        public static Vector3d Reorder(Vector3d vector, int order)
        {
            switch (order)
            {
                case 123:
                    return new Vector3d(vector.x, vector.y, vector.z);
                case 132:
                    return new Vector3d(vector.x, vector.z, vector.y);
                case 213:
                    return new Vector3d(vector.y, vector.x, vector.z);
                case 231:
                    return new Vector3d(vector.y, vector.z, vector.x);
                case 312:
                    return new Vector3d(vector.z, vector.x, vector.y);
                case 321:
                    return new Vector3d(vector.z, vector.y, vector.x);
            }
            throw new ArgumentException("Invalid order", "order");
        }

        public static string DumpObject(object obj, int depth = 2, string pref = "")
        {
            string tmp = "";
            if (depth >= 0)
            {
                foreach (System.ComponentModel.PropertyDescriptor descriptor in System.ComponentModel.TypeDescriptor.GetProperties(obj))
                {
                    try
                    {
                        string name = descriptor.Name;
                        object value = descriptor.GetValue(obj);
                        tmp += pref + ((pref == "") ? "" : ".") + name + " = " + value + "\n";
                        tmp += DumpObject(value, depth - 1, pref + ((pref == "") ? "" : ".") + name);
                    }
                    catch (Exception e)
                    {
                        MonoBehaviour.print("Exception while dumping: " + e.GetType().Name + " - " + e.Message);
                    }
                }
            }
            return tmp;
        }
    }

    public class MovingAverage
    {
        private double[] store;
        private int storeSize;
        private int nextIndex = 0;

        public double value
        {
            get
            {
                double tmp = 0;
                foreach (double i in store)
                {
                    tmp += i;
                }
                return tmp / storeSize;
            }
            set
            {
                store[nextIndex] = value;
                nextIndex = (nextIndex + 1) % storeSize;
            }
        }

        public MovingAverage(int size = 10, double startingValue = 0)
        {
            storeSize = size;
            store = new double[size];
            force(startingValue);
        }

        public void force(double newValue)
        {
            for (int i = 0; i < storeSize; i++)
            {
                store[i] = newValue;
            }
        }

        public static implicit operator double(MovingAverage v)
        {
            return v.value;
        }

        public override string ToString()
        {
            return value.ToString();
        }

        public string ToString(string format)
        {
            return value.ToString(format);
        }
    }
}
