using System;
using Microsoft.Maui.Media;

namespace ScriptingMaui
{
	public class TTS
	{
		static Dictionary<string, Locale> Locales = new Dictionary<string, Locale>();
		static Locale? DefaultLocale;

        public static float Pitch { get; set; } = 1.5f; // 0.0 - 2.0
        public static float Volume { get; set; } = .75f; // 0.0 - 1.0

        public static async Task InitLocales()
		{
			var locales = await TextToSpeech.GetLocalesAsync();
			foreach (var locale in locales)
			{
				//Console.WriteLine("LOCALE:" + locale.Language + " - " + locale.Name + " - " + locale.Id + " - " + locale.Country);
				Locales[locale.Language.ToLower()] = locale;
            }
			DefaultLocale = locales.FirstOrDefault();
        }
		public static async Task<Locale?> GetLocale(string voice)
		{
			if (Locales.Count == 0)
			{
				await InitLocales();
            }
			//Locale? locale = DefaultLocale;
			var key = voice.Replace("_", "-").ToLower();
			if (!Locales.TryGetValue(key, out Locale? locale))
			{
				return DefaultLocale;
			}
			return locale;
        }

		public static async Task Speak(string text, string voice = "en_US", bool force = false)
		{
			if (!force && !SettingsPage.Sound)
			{
				return;
			}
			var locale = await GetLocale(voice);
            SpeechOptions options = new SpeechOptions()
            {
                Pitch = Pitch,   
                Volume = Volume, 
                Locale = locale
            };
            await TextToSpeech.Default.SpeakAsync(text, options);
        }
	}
}

