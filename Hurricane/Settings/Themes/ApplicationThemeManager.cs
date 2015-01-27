﻿using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using System.Xml.Serialization;
using MahApps.Metro;

namespace Hurricane.Settings.Themes
{
    [Serializable]
    public class ApplicationThemeManager
    {
        [UserSetting]
        public ThemeBase SelectedColorTheme { get; set; }
        [UserSetting]
        public bool UseCustomSpectrumAnalyzerColor { get; set; }
        [UserSetting]
        public string SpectrumAnalyzerHexColor { get; set; }
        [UserSetting]
        public BaseTheme BaseTheme { get; set; }

        [XmlIgnore]
        public Color SpectrumAnalyzerColor
        {
            get
            {
                if (string.IsNullOrWhiteSpace(SpectrumAnalyzerHexColor)) return Colors.Black;
                return (Color)ColorConverter.ConvertFromString(SpectrumAnalyzerHexColor);
            }
            set
            {
                SpectrumAnalyzerHexColor = string.Format("#{0:X2}{1:X2}{2:X2}{3:X2}", value.A, value.R, value.G, value.B);
            }
        }

        private ObservableCollection<ThemeBase> themes;
        [XmlIgnore]
        public ObservableCollection<ThemeBase> Themes
        {
            get
            {
                if (themes == null)
                {
                    RefreshThemes();
                }
                return themes;
            }
        }

        public void RefreshThemes()
        {
            if (themes == null)
            {
                themes = new ObservableCollection<ThemeBase>();

                foreach (var t in ThemeManager.Accents.Select(a => new AccentColorTheme() { Name = a.Name }).OrderBy((x) => x.TranslatedName))
                {
                    themes.Add(t);
                }
            }
            else
            {
                for (int i = themes.Count -1; i < 0; i++)
                {
                    if (themes[i].GetType() == typeof(CustomColorTheme)) themes.Remove(themes[i]);
                }
            }

            DirectoryInfo themefolder = new DirectoryInfo(HurricaneSettings.Instance.ThemeDirectory);
            if (themefolder.Exists)
            {
                foreach (var file in themefolder.GetFiles("*.xaml"))
                {
                    CustomColorTheme theme = new CustomColorTheme();
                    if (theme.Load(file.Name)) themes.Add(theme);
                }
            }
        }

        public void LoadTheme()
        {
            try
            {
                SelectedColorTheme.ApplyTheme();
            }
            catch (Exception)
            {
                this.SelectedColorTheme = Themes.First(x => x.Name == "Blue");
                SelectedColorTheme.ApplyTheme();
            }
            RefreshSpectrumAnalyzerBrush();
        }

        public void RefreshSpectrumAnalyzerBrush()
        {
            if (UseCustomSpectrumAnalyzerColor)
            {
                Application.Current.Resources["SpectrumAnalyzerBrush"] = new SolidColorBrush((Color)ColorConverter.ConvertFromString(SpectrumAnalyzerHexColor));
            }
            else
            {
                Application.Current.Resources["SpectrumAnalyzerBrush"] = Application.Current.FindResource("AccentColorBrush");
            }
        }

        public void LoadBaseTheme()
        {
            var resource = new ResourceDictionary() { Source = new Uri(string.Format("/Resources/Themes/{0}.xaml", BaseTheme == BaseTheme.Dark ? "BaseDark" : "BaseLight"), UriKind.Relative) };
            Application.Current.Resources.MergedDictionaries.Add(resource);
        }

        public void LoadStandard()
        {
            this.SelectedColorTheme = Themes.First(x => x.Name == "Blue");
            this.UseCustomSpectrumAnalyzerColor = false;
            this.SpectrumAnalyzerHexColor = null;
            this.BaseTheme = BaseTheme.Light;
        }

        public override bool Equals(object obj)
        {
            var other = obj as ApplicationThemeManager;
            if (other == null) return false;
            return this.SelectedColorTheme.Name == other.SelectedColorTheme.Name && this.UseCustomSpectrumAnalyzerColor == other.UseCustomSpectrumAnalyzerColor && this.SpectrumAnalyzerColor == other.SpectrumAnalyzerColor && this.BaseTheme == other.BaseTheme;
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        protected static ResourceDictionary appliedResource;
        public static void RegisterTheme(ResourceDictionary resource)
        {
            if (appliedResource != null) Application.Current.Resources.MergedDictionaries.Remove(appliedResource);
            appliedResource = resource;
        }
    }

    public enum BaseTheme { Light, Dark }
}
