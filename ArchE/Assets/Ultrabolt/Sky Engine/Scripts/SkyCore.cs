using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

namespace Ultrabolt.SkyEngine
{
	[RequireComponent(typeof(WorldTimeEvent))]
	public class SkyCore : MonoBehaviour
	{
		public enum Weather { Clear, LowCloud, HighCloud, Rain }

		#region Variables
		public Text statsText;

		// Stars
		public GameObject starDome;
		public float starSpeed = 2f;

		// Celestial System
		public GameObject celestialRotation, moon, sun;

		// Weather
		public Weather weather;
		public GameObject lowClouds, highClouds;
		public ParticleSystem rainFx;
		public float weatherSpeed = 0.3f;

		// Lights & Colors
		public float lightFadeSpeed = 0.3f;
		public Light sunLight, moonLight;
		public Gradient skyTop, skyBottom;

		// Fog
		public bool enableFog;
		public float dayFogDensity = 0.01f;
		public float nightFogDensity = 0.1f;

		// Time
		public float timeSpeed = 1f;
		public float dayLength = 120f;

		// Info
		public string dayState;
		public int dayCount = 0;
		[Range(0, 1)] public float timeOfDay = 0f;

		// Cahching variables.
		private Material starMat, lowCloudMat, highCloudMat;
		private WorldTimeEvent timeEvents;
		private Camera cam;

		private Color lowCloudCol, highCloudCol;
		private float lowCloudDensity, highCloudDensity, lightValue;

		private float sunIntensity, moonIntensity;
		private GameTime lastTimeState;
		#endregion

		public void SetWeather(int value) => weather = (Weather)value;

		private void Start()
		{
			timeEvents = GetComponent<WorldTimeEvent>();

			RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
			cam = Camera.main;

			lowCloudMat = lowClouds.GetComponent<MeshRenderer>().material;
			highCloudMat = highClouds.GetComponent<MeshRenderer>().material;

			starMat = starDome.GetComponentInChildren<Renderer>().material;

			sunIntensity = sunLight.intensity;
			moonIntensity = moonLight.intensity;
		}

		private string GetWorldTime()
		{
			float totalSecondsInDay = 24f * 60f * 60f;
			float currentSeconds = timeOfDay * totalSecondsInDay;

			currentSeconds += 21600f;

			if (currentSeconds >= totalSecondsInDay)
				currentSeconds -= totalSecondsInDay;

			int hours = Mathf.FloorToInt(currentSeconds / 3600f);
			int minutes = Mathf.FloorToInt((currentSeconds % 3600f) / 60f);

			return $"{hours:D2}:{minutes:D2}";
		}

		private void Update()
		{
			if (statsText != null)
				statsText.text = $"Day: {dayCount}\n{dayState}\n{GetWorldTime()}\nWeather: {weather}";

			// Days calculator.
			timeOfDay += (Time.deltaTime / dayLength) * timeSpeed;
			if (timeOfDay >= 1f)
			{
				timeOfDay = 0;
				dayCount++;
			}

			// Fog
			RenderSettings.fog = enableFog;
			if (enableFog)
			{
				RenderSettings.fogColor = RenderSettings.skybox.GetColor("_SkyGradientTop");
				RenderSettings.fogDensity = Mathf.MoveTowards(RenderSettings.fogDensity, (dayState == "Night") ?
					nightFogDensity : dayFogDensity, Time.deltaTime * lightFadeSpeed * timeSpeed);
			}

			// Day Time
			GameTime state = GetTimeState(timeOfDay);
			dayState = state.ToString();

			UpdateWeather();
			UpdateLighting();
			ApplyAmbient();
		}

		private void UpdateWeather()
		{
			Color lightGray20 = new Color(1f, 1f, 1f, 0.2f);
			Color lightGray40 = new Color(1f, 1f, 1f, 0.4f);
			Color darkGray50 = new Color(0.4f, 0.4f, 0.4f, 0.5f);

			switch (weather)
			{
				case Weather.Clear: ApplyWeather(false, 2f, 2f, lightGray20, lightGray20); break;
				case Weather.LowCloud: ApplyWeather(false, 1f, 2f, lightGray20, lightGray20); break;
				case Weather.HighCloud: ApplyWeather(false, 0.4f, 0f, lightGray20, lightGray40); break;
				case Weather.Rain: ApplyWeather(true, 0.2f, 0f, darkGray50, darkGray50); break;
			}

			// Cloud density
			lowCloudMat.SetColor("_CloudColor", lowCloudCol);
			lowCloudMat.SetFloat("_Density", lowCloudDensity);
			highCloudMat.SetColor("_CloudColor", highCloudCol);
			highCloudMat.SetFloat("_Density", highCloudDensity);
		}

		private void ApplyWeather(bool raining, float lowDensity, float highDensity, Color lowCol, Color highCol)
		{
			float t = Time.deltaTime * weatherSpeed * timeSpeed;

			// Rain
			rainFx.transform.position = cam.transform.position;
			if (!rainFx.isPlaying && raining && highCloudDensity < 0.1f) rainFx.Play();
			else if (rainFx.isPlaying && !raining && highCloudDensity > 0.1f) rainFx.Stop();

			// Cloud Colors
			lowCloudCol = Color.Lerp(lowCloudCol, lowCol, t);
			highCloudCol = Color.Lerp(highCloudCol, highCol, t);
			lowCloudDensity = Mathf.Lerp(lowCloudDensity, lowDensity, t);
			highCloudDensity = Mathf.Lerp(highCloudDensity, highDensity, t);
		}

		private void UpdateLighting()
		{
			//Moon Rise
			if (timeOfDay > 0.45f && timeOfDay < 1f)
			{
				moonLight.intensity = Mathf.MoveTowards(moonLight.intensity, moonIntensity, Time.deltaTime * lightFadeSpeed * timeSpeed);
				sunLight.intensity = Mathf.MoveTowards(sunLight.intensity, 0f, Time.deltaTime * lightFadeSpeed * timeSpeed);
			}
			else // Sun Rise
			{
				moonLight.intensity = Mathf.MoveTowards(moonLight.intensity, 0f, Time.deltaTime * lightFadeSpeed * timeSpeed);
				sunLight.intensity = Mathf.MoveTowards(sunLight.intensity, sunIntensity, Time.deltaTime * lightFadeSpeed * timeSpeed);
			}

			// Sun & Moon
			moon.transform.LookAt(cam.transform);
			sun.transform.LookAt(cam.transform);

			// Skybox colors
			RenderSettings.skybox.SetColor("_SkyGradientTop", skyTop.Evaluate(timeOfDay));
			RenderSettings.skybox.SetColor("_SkyGradientBottom", skyBottom.Evaluate(timeOfDay));


			// Star Dome
			float t = Time.deltaTime * timeSpeed;
			starDome.transform.Rotate(Vector3.up * (starSpeed * t));
			celestialRotation.transform.localRotation = Quaternion.Euler(timeOfDay * 360f, 0, 0);

			float nightFactor = (dayState == "Night" || dayState == "Midnight") ? 1 : 0;
			starMat.color = new Color(1, 1, 1, Mathf.Lerp(starMat.color.a, nightFactor, t));
		}

		GameTime GetTimeState(float t)
		{
			if (t < 0.1f) lastTimeState = GameTime.Morning;
			else if (t < 0.4f) lastTimeState = GameTime.MidNoon;
			else if (t < 0.6f) lastTimeState = GameTime.Evening;
			else lastTimeState = GameTime.Night;

			timeEvents.TriggerEventsFor(dayCount, lastTimeState);
			return lastTimeState;
		}

		private void ApplyAmbient()
		{
			if (RenderSettings.skybox == null)
				return;

			float t = Time.deltaTime * lightFadeSpeed * timeSpeed;

			Color top = skyTop.Evaluate(timeOfDay);
			Color bottom = skyBottom.Evaluate(timeOfDay);
			Color mid = Color.Lerp(top, bottom, 0.5f);

			RenderSettings.ambientSkyColor = Color.Lerp(RenderSettings.ambientSkyColor, top, t);
			RenderSettings.ambientEquatorColor = Color.Lerp(RenderSettings.ambientEquatorColor, mid, t);
			RenderSettings.ambientGroundColor = Color.Lerp(RenderSettings.ambientGroundColor, bottom, t);
		}
	}

	public enum GameTime { Morning, MidNoon, Evening, Night }
}
