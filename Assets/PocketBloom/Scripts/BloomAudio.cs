using UnityEngine;

namespace PocketBloom
{
    public sealed class BloomAudio : MonoBehaviour
    {
        AudioSource effects, music;
        AudioClip tap, plant, clear, win, ambient;
        public void Setup()
        {
            effects = gameObject.AddComponent<AudioSource>(); music = gameObject.AddComponent<AudioSource>();
            tap = Tone(new[] { 523.25f }, .065f); plant = Tone(new[] { 659.25f, 783.99f }, .13f);
            clear = Tone(new[] { 523.25f, 659.25f, 783.99f, 1046.5f }, .38f);
            win = Tone(new[] { 523.25f, 659.25f, 783.99f, 1046.5f, 1318.5f }, .7f);
            ambient = Tone(new[] { 130.81f, 164.81f, 196f, 261.63f, 196f, 164.81f, 146.83f, 196f }, 12, true);
            music.clip = ambient; music.loop = true; music.volume = .12f; music.Play();
        }
        public void Apply(BloomProfile profile) { effects.mute = !profile.sound; music.mute = !profile.music; }
        public void Play(int type) { effects.PlayOneShot(type == 3 ? win : type == 2 ? clear : type == 1 ? plant : tap, .28f); }
        static AudioClip Tone(float[] notes, float duration, bool soft = false)
        {
            const int rate = 22050; var samples = new float[Mathf.CeilToInt(rate * duration)];
            float step = duration / notes.Length;
            for (int i = 0; i < samples.Length; i++)
            {
                float t = i / (float)rate, local = t % step;
                int note = Mathf.Min(notes.Length - 1, (int)(t / step));
                float envelope = Mathf.Sin(Mathf.PI * local / step) * Mathf.Exp(-local * (soft ? 1 : 8));
                samples[i] = (Mathf.Sin(2 * Mathf.PI * notes[note] * t) + .2f * Mathf.Sin(4 * Mathf.PI * notes[note] * t)) * envelope * .35f;
            }
            var clip = AudioClip.Create("BloomOriginal", samples.Length, 1, rate, false); clip.SetData(samples, 0); return clip;
        }
        void OnDestroy()
        { foreach (var clip in new[] { tap, plant, clear, win, ambient }) if (clip) Destroy(clip); }
    }
}
