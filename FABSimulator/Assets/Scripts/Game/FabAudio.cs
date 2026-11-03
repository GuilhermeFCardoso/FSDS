using UnityEngine;

namespace FABSim.Game
{
    public sealed class FabAudio : MonoBehaviour
    {
        const int Rate = 44100;
        AudioSource engine, wind, gun, warn, lockTone, oneShot;
        AudioClip boom, launch, hit, click, deny, flare, blip;
        FlightController f;

        delegate float Gen(float t, float k);

        static AudioClip Make(string name, float secs, Gen g)
        {
            int n = (int)(secs * Rate);
            var d = new float[n];
            for (int i = 0; i < n; i++) d[i] = g(i / (float)Rate, i / (float)n);
            var c = AudioClip.Create(name, n, 1, Rate, false);
            c.SetData(d, 0);
            return c;
        }

        static float low;
        static float Noise(float cut) { low += (Random.value * 2 - 1 - low) * cut; return low; }

        AudioSource Src(AudioClip c, bool loop)
        {
            var s = gameObject.AddComponent<AudioSource>();
            s.clip = c; s.loop = loop; s.spatialBlend = 0; s.playOnAwake = false;
            return s;
        }

        void Awake()
        {
            if (System.Environment.GetEnvironmentVariable("FAB_AUTOTEST") != null) AudioListener.volume = 0;
            engine = Src(Make("motor", 2, (t, k) => Fade(k) * (Noise(0.04f) * 2.2f + 0.12f * Mathf.Sin(t * 2 * Mathf.PI * 75))), true);
            wind = Src(Make("vento", 2, (t, k) => Fade(k) * Noise(0.2f) * 1.1f), true);
            gun = Src(Make("canhao", 0.5f, (t, k) => Fade(k) * (Noise(0.5f) * 0.8f * (0.5f + 0.5f * Mathf.Sign(Mathf.Sin(t * 2 * Mathf.PI * 28))))), true);
            warn = Src(Make("alerta", 0.5f, (t, k) => (k < 0.5f ? 0.25f : 0) * Mathf.Sign(Mathf.Sin(t * 2 * Mathf.PI * 1200))), true);
            lockTone = Src(Make("trava", 0.25f, (t, k) => 0.1f * Mathf.Sin(t * 2 * Mathf.PI * 1560)), true);
            oneShot = Src(null, false);
            boom = Make("boom", 2.6f, (t, k) => Noise(0.05f) * 3.2f * Mathf.Exp(-k * 5));
            launch = Make("lanc", 1.6f, (t, k) => Noise(0.3f) * 0.9f * Mathf.Exp(-k * 2.5f));
            hit = Make("impacto", 0.3f, (t, k) => Noise(0.6f) * Mathf.Exp(-k * 7));
            click = Make("clique", 0.05f, (t, k) => 0.2f * Mathf.Sin(t * 2 * Mathf.PI * 1800) * (1 - k));
            deny = Make("nega", 0.25f, (t, k) => 0.2f * Mathf.Sign(Mathf.Sin(t * 2 * Mathf.PI * 220)));
            flare = Make("flare", 0.25f, (t, k) => Noise(0.8f) * 0.5f * (1 - k));
            blip = Make("radio", 0.28f, (t, k) => Noise(0.8f) * 0.1f * (1 - k) + 0.08f * Mathf.Sin(t * 2 * Mathf.PI * 1000) * (k < 0.3f ? 1 : 0));
        }

        static float Fade(float k) => Mathf.Clamp01(Mathf.Min(k, 1 - k) * 40);

        public void Bind(FlightController fc)
        {
            f = fc;
            engine.Play(); wind.Play();
        }

        void Update()
        {
            if (f == null || f.S == null) return;
            var s = f.S;
            float n = (float)s.N;
            engine.volume = s.Crashed.Length > 0 ? 0 : Mathf.Clamp01(0.08f + n * 0.5f + (float)s.Ab * 0.3f) * (f.Cockpit ? 0.55f : 1);
            engine.pitch = 0.6f + n * 0.9f;
            float v = (float)s.Diag.Tas;
            wind.volume = Mathf.Clamp01(v / 350) * (f.Cockpit ? 0.2f : 0.5f);
            wind.pitch = 0.7f + v / 500;
        }

        public void Warn(bool missile, bool locked, bool firing)
        {
            Toggle(warn, missile); Toggle(lockTone, locked && !missile); Toggle(gun, firing);
        }

        static void Toggle(AudioSource s, bool on) { if (on && !s.isPlaying) s.Play(); else if (!on && s.isPlaying) s.Stop(); }

        public void Boom(float vol) => oneShot.PlayOneShot(boom, vol);
        public void Launch() => oneShot.PlayOneShot(launch, 0.8f);
        public void Hit() => oneShot.PlayOneShot(hit, 0.9f);
        public void Click() => oneShot.PlayOneShot(click, 0.7f);
        public void Deny() => oneShot.PlayOneShot(deny, 0.7f);
        public void Flare() => oneShot.PlayOneShot(flare, 0.8f);
        public void RadioBlip() => oneShot.PlayOneShot(blip, 0.8f);
    }
}
