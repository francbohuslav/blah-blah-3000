// Generates the light background music of the tour: a small synthesizer (no samples, no downloads, so no licence to care about).
//
//   node tools/music.ts samples                      4 styles, 26 s each, alone and mixed under the narration -> out/music-samples/
//   node tools/music.ts build <style> <seconds> <file.wav>   one style in full length, ducked under the narration
//
// Styles: calm, piano, lofi, bright.
import { mkdirSync, readFileSync, writeFileSync } from "node:fs";
import { dirname, join } from "node:path";
import { fileURLToPath } from "node:url";
import { NARRATION } from "../src/narration.ts";

const SR = 44100;
const ROOT = join(dirname(fileURLToPath(import.meta.url)), "..");
const TAU = Math.PI * 2;

const hz = (midi: number) => 440 * 2 ** ((midi - 69) / 12);

/** A small deterministic random generator, so the same style always sounds the same. */
function rng(seed: number): () => number {
  let s = seed >>> 0;
  return () => {
    s = (s * 1664525 + 1013904223) >>> 0;
    return s / 4294967296;
  };
}

/** A stereo track the notes are added to at their start times. */
class Track {
  readonly l: Float32Array;
  readonly r: Float32Array;
  constructor(seconds: number) {
    const n = Math.ceil((seconds + 4) * SR);
    this.l = new Float32Array(n);
    this.r = new Float32Array(n);
  }
  add(start: number, samples: Float32Array, gain = 1, pan = 0): void {
    const i0 = Math.floor(start * SR);
    const gl = gain * Math.cos(((pan + 1) * Math.PI) / 4);
    const gr = gain * Math.sin(((pan + 1) * Math.PI) / 4);
    for (let i = 0; i < samples.length && i0 + i < this.l.length; i++) {
      this.l[i0 + i] += samples[i] * gl;
      this.r[i0 + i] += samples[i] * gr;
    }
  }
}

// ---- instruments (each returns a mono note) ----

const triangle = (cycles: number) => 2 * Math.abs(2 * (cycles - Math.floor(cycles + 0.5))) - 1;

/** A soft pad: three slightly detuned triangle waves and a sub octave, slow attack and release, rounded off. */
function pad(midi: number, dur: number): Float32Array {
  const f = hz(midi);
  const att = 0.9;
  const rel = 1.4;
  const out = new Float32Array(Math.floor((dur + rel) * SR));
  const a = 1 - Math.exp((-TAU * 1300) / SR);
  let y = 0;
  for (let i = 0; i < out.length; i++) {
    const t = i / SR;
    const env = Math.min(1, t / att) * (t < dur ? 1 : Math.max(0, 1 - (t - dur) / rel));
    const x = (triangle(f * 0.996 * t) + triangle(f * t) + triangle(f * 1.004 * t)) / 3 + 0.5 * Math.sin(TAU * (f / 2) * t);
    y += a * (x * env - y);
    out[i] = y * 0.5;
  }
  return out;
}

/** A soft plucked note (a piano-like decay). */
function pluck(midi: number, dur = 1.4, bright = 1): Float32Array {
  const f = hz(midi);
  const out = new Float32Array(Math.floor(dur * SR));
  for (let i = 0; i < out.length; i++) {
    const t = i / SR;
    const env = Math.exp(-t / 0.4) * (1 - Math.exp(-t / 0.004));
    out[i] =
      env *
      (Math.sin(TAU * f * t) + 0.35 * bright * Math.sin(TAU * 2 * f * t) * Math.exp(-t / 0.2) + 0.12 * bright * Math.sin(TAU * 3 * f * t) * Math.exp(-t / 0.12)) *
      0.5;
  }
  return out;
}

/** A glassy bell. */
function bell(midi: number, dur = 3): Float32Array {
  const f = hz(midi);
  const out = new Float32Array(Math.floor(dur * SR));
  for (let i = 0; i < out.length; i++) {
    const t = i / SR;
    const a = 1 - Math.exp(-t / 0.003);
    out[i] = a * (Math.sin(TAU * f * t) * Math.exp(-t / 1.2) + 0.4 * Math.sin(TAU * 2.76 * f * t) * Math.exp(-t / 0.6) + 0.15 * Math.sin(TAU * 5.4 * f * t) * Math.exp(-t / 0.25)) * 0.4;
  }
  return out;
}

/** A warm electric piano (a little frequency modulation with a slow tremolo). */
function epiano(midi: number, dur = 2): Float32Array {
  const f = hz(midi);
  const out = new Float32Array(Math.floor(dur * SR));
  for (let i = 0; i < out.length; i++) {
    const t = i / SR;
    const index = 1.2 * Math.exp(-t / 0.4) + 0.15;
    const env = Math.exp(-t / 0.9) * (1 - Math.exp(-t / 0.003)) * (1 + 0.1 * Math.sin(TAU * 4.5 * t));
    out[i] = env * Math.sin(TAU * f * t + index * Math.sin(TAU * f * t)) * 0.45;
  }
  return out;
}

function bass(midi: number, dur = 1): Float32Array {
  const f = hz(midi);
  const out = new Float32Array(Math.floor(dur * SR));
  for (let i = 0; i < out.length; i++) {
    const t = i / SR;
    out[i] = Math.exp(-t / 0.5) * (1 - Math.exp(-t / 0.012)) * (Math.sin(TAU * f * t) + 0.2 * Math.sin(TAU * 2 * f * t)) * 0.6;
  }
  return out;
}

function kick(): Float32Array {
  const out = new Float32Array(Math.floor(0.35 * SR));
  let phase = 0;
  for (let i = 0; i < out.length; i++) {
    const t = i / SR;
    phase += (TAU * (45 + 80 * Math.exp(-t / 0.04))) / SR;
    out[i] = Math.sin(phase) * Math.exp(-t / 0.13) * 0.8;
  }
  return out;
}

function snare(random: () => number): Float32Array {
  const out = new Float32Array(Math.floor(0.25 * SR));
  let prev = 0;
  for (let i = 0; i < out.length; i++) {
    const t = i / SR;
    const noise = random() * 2 - 1;
    const high = noise - prev * 0.6;
    prev = noise;
    out[i] = (high * 0.5 * Math.exp(-t / 0.07) + Math.sin(TAU * 185 * t) * 0.35 * Math.exp(-t / 0.05)) * 0.7;
  }
  return out;
}

function hat(random: () => number, open = false): Float32Array {
  const out = new Float32Array(Math.floor((open ? 0.2 : 0.06) * SR));
  let prev = 0;
  for (let i = 0; i < out.length; i++) {
    const t = i / SR;
    const noise = random() * 2 - 1;
    const high = noise - prev;
    prev = noise;
    out[i] = high * Math.exp(-t / (open ? 0.07 : 0.015)) * 0.35;
  }
  return out;
}

// ---- reverb: a small Freeverb-like room, so everything sits in one space ----

function reverb(track: Track, wet: number): void {
  const process = (input: Float32Array, offset: number): Float32Array => {
    const out = new Float32Array(input.length);
    for (const base of [1116, 1188, 1277, 1356]) {
      const size = base + offset;
      const buf = new Float32Array(size);
      let idx = 0;
      let store = 0;
      for (let i = 0; i < input.length; i++) {
        const delayed = buf[idx];
        store = delayed * 0.8 + store * 0.2;
        buf[idx] = input[i] * 0.3 + store * 0.84;
        idx = (idx + 1) % size;
        out[i] += delayed;
      }
    }
    for (const base of [556, 441]) {
      const size = base + offset;
      const buf = new Float32Array(size);
      let idx = 0;
      for (let i = 0; i < out.length; i++) {
        const delayed = buf[idx];
        const x = out[i];
        buf[idx] = x + delayed * 0.5;
        out[i] = delayed - x * 0.5;
        idx = (idx + 1) % size;
      }
    }
    return out;
  };
  const mono = new Float32Array(track.l.length);
  for (let i = 0; i < mono.length; i++) mono[i] = (track.l[i] + track.r[i]) / 2;
  const left = process(mono, 0);
  const right = process(mono, 23);
  for (let i = 0; i < mono.length; i++) {
    track.l[i] += left[i] * wet;
    track.r[i] += right[i] * wet;
  }
}

// ---- the styles ----

// Chords as MIDI notes (low to high).
const CMAJ7 = [48, 52, 55, 59];
const AM7 = [45, 48, 52, 55];
const FMAJ7 = [41, 45, 48, 52];
const G6 = [43, 47, 50, 52];
const C = [48, 52, 55];
const G = [43, 47, 50];
const AM = [45, 48, 52];
const F = [41, 45, 48];

type Style = "calm" | "piano" | "lofi" | "bright";

function compose(style: Style, seconds: number): Track {
  const track = new Track(seconds);
  const random = rng(style.length * 7919 + 13);
  const PENTATONIC = [72, 74, 76, 79, 81, 84];

  if (style === "calm") {
    // Slow pads on I - vi - IV - V with a few glassy bells above them.
    const chords = [CMAJ7, AM7, FMAJ7, G6];
    const len = 4.5;
    for (let t = 0, c = 0; t < seconds + len; t += len, c++) {
      const chord = chords[c % chords.length];
      chord.forEach((n, i) => track.add(t, pad(n + 12, len + 0.6), 0.7, (i - 1.5) * 0.25));
      track.add(t, pad(chord[0], len + 0.6), 0.7, 0);
    }
    for (let t = 1.2; t < seconds; t += 2.2 + random() * 2.4) {
      track.add(t, bell(PENTATONIC[Math.floor(random() * PENTATONIC.length)]), 0.35, random() * 1.6 - 0.8);
    }
    reverb(track, 0.55);
  } else if (style === "piano") {
    // A gentle arpeggio over a quiet pad: 84 bpm, eighth notes.
    const chords = [C, G, AM, F];
    const beat = 60 / 84;
    const barLen = beat * 4;
    const pattern = [0, 1, 2, 1, 2, 1, 0, 1];
    for (let bar = 0; bar * barLen < seconds + barLen; bar++) {
      const chord = chords[bar % chords.length];
      const t0 = bar * barLen;
      pattern.forEach((p, i) => track.add(t0 + (i * beat) / 2, pluck(chord[p] + 24 + (i % 4 === 3 ? 12 : 0), 1.6), 0.5, (i % 2 ? 0.25 : -0.25)));
      track.add(t0, bass(chord[0], beat * 3), 0.6, 0);
      if (bar % 2 === 0) chord.forEach((n) => track.add(t0, pad(n + 12, barLen * 2), 0.28, 0));
    }
    reverb(track, 0.32);
  } else if (style === "lofi") {
    // Warm electric piano chords with a lazy swung beat, 76 bpm.
    const chords = [CMAJ7, AM7, FMAJ7, G6];
    const beat = 60 / 76;
    const barLen = beat * 4;
    for (let bar = 0; bar * barLen < seconds + barLen; bar++) {
      const chord = chords[bar % chords.length];
      const t0 = bar * barLen;
      const hit = (at: number, gain: number) => chord.forEach((n, i) => track.add(t0 + at + i * 0.012, epiano(n + 12, 2.4), gain, (i - 1.5) * 0.2));
      hit(0, 0.55);
      hit(beat * 2.5, 0.4);
      track.add(t0, bass(chord[0] - 12, beat * 2), 0.7, 0);
      track.add(t0 + beat * 2.5, bass(chord[0] - 12, beat), 0.5, 0);
      track.add(t0, kick(), 0.7);
      track.add(t0 + beat * 2, kick(), 0.55);
      track.add(t0 + beat * 1, snare(random), 0.45);
      track.add(t0 + beat * 3, snare(random), 0.45);
      for (let i = 0; i < 8; i++) {
        const swing = i % 2 === 1 ? 0.08 * beat : 0;
        track.add(t0 + (i * beat) / 2 + swing, hat(random, i === 7), i % 2 === 0 ? 0.4 : 0.25, 0.3);
      }
    }
    reverb(track, 0.22);
  } else {
    // Bright and light: a bouncy arpeggio, a soft pulse and quiet percussion, 100 bpm, I - V - vi - IV.
    const chords = [C, G, AM, F];
    const beat = 0.6;
    const barLen = beat * 4;
    const pattern = [0, 1, 2, 1, 2, 1, 0, 2, 1, 2, 1, 0, 2, 1, 2, 1];
    for (let bar = 0; bar * barLen < seconds + barLen; bar++) {
      const chord = chords[bar % chords.length];
      const t0 = bar * barLen;
      pattern.forEach((p, i) => track.add(t0 + (i * beat) / 4, pluck(chord[p] + 24, 0.5, 1.4), 0.38, (i % 2 ? 0.35 : -0.35)));
      for (let i = 0; i < 4; i++) track.add(t0 + i * beat + beat / 2, bass(chord[0], beat * 0.5), 0.45, 0);
      for (let i = 0; i < 4; i++) track.add(t0 + i * beat, kick(), 0.4);
      for (let i = 0; i < 4; i++) track.add(t0 + i * beat + beat / 2, hat(random), 0.35, 0.3);
      chord.forEach((n) => track.add(t0, pad(n + 12, barLen), 0.25, 0));
    }
    reverb(track, 0.18);
  }
  return track;
}

// ---- mixing and files ----

function readWav(file: string): Float32Array {
  const buf = readFileSync(file);
  const rate = buf.readUInt32LE(24);
  const at = buf.indexOf("data") + 8;
  const n = (buf.length - at) / 2;
  const src = new Float32Array(n);
  for (let i = 0; i < n; i++) src[i] = buf.readInt16LE(at + i * 2) / 32768;
  const ratio = rate / SR;
  const out = new Float32Array(Math.floor(n / ratio));
  for (let i = 0; i < out.length; i++) {
    const x = i * ratio;
    const i0 = Math.floor(x);
    out[i] = src[i0] + (src[Math.min(n - 1, i0 + 1)] - src[i0]) * (x - i0);
  }
  return out;
}

function writeWav(file: string, l: Float32Array, r: Float32Array): void {
  const n = l.length;
  const buf = Buffer.alloc(44 + n * 4);
  buf.write("RIFF", 0);
  buf.writeUInt32LE(36 + n * 4, 4);
  buf.write("WAVEfmt ", 8);
  buf.writeUInt32LE(16, 16);
  buf.writeUInt16LE(1, 20);
  buf.writeUInt16LE(2, 22);
  buf.writeUInt32LE(SR, 24);
  buf.writeUInt32LE(SR * 4, 28);
  buf.writeUInt16LE(4, 32);
  buf.writeUInt16LE(16, 34);
  buf.write("data", 36);
  buf.writeUInt32LE(n * 4, 40);
  for (let i = 0; i < n; i++) {
    buf.writeInt16LE(Math.round(Math.max(-1, Math.min(1, l[i])) * 32767), 44 + i * 4);
    buf.writeInt16LE(Math.round(Math.max(-1, Math.min(1, r[i])) * 32767), 46 + i * 4);
  }
  mkdirSync(dirname(file), { recursive: true });
  writeFileSync(file, buf);
}

const rms = (a: Float32Array, b?: Float32Array) => {
  let s = 0;
  for (let i = 0; i < a.length; i++) s += a[i] * a[i] + (b ? b[i] * b[i] : 0);
  return Math.sqrt(s / (a.length * (b ? 2 : 1)));
};

/** The finished music: cut to the length, faded in and out, and set to the given loudness (RMS). */
function render(style: Style, seconds: number, level: number): [Float32Array, Float32Array] {
  const track = compose(style, seconds);
  const n = Math.floor(seconds * SR);
  const l = track.l.slice(0, n);
  const r = track.r.slice(0, n);
  const gain = level / rms(l, r);
  for (let i = 0; i < n; i++) {
    const t = i / SR;
    const fade = Math.min(1, t / 1.5) * Math.min(1, (seconds - t) / 2.5);
    l[i] *= gain * fade;
    r[i] *= gain * fade;
  }
  return [l, r];
}

/** Narration lines that fit into the first `seconds` of the video, on a track at the music's sample rate. */
function voiceTrack(seconds: number): Float32Array {
  const out = new Float32Array(Math.floor(seconds * SR));
  for (const line of NARRATION) {
    const samples = readWav(join(ROOT, "public", "narration", `${line.id}.wav`));
    if (line.at + samples.length / SR > seconds) continue;
    const i0 = Math.floor(line.at * SR);
    for (let i = 0; i < samples.length; i++) out[i0 + i] += samples[i];
  }
  return out;
}

/** The music gets quieter while the voice speaks (attack 30 ms, release 450 ms). */
function duck(voice: Float32Array, depth: number): Float32Array {
  const gain = new Float32Array(voice.length);
  let env = 0;
  const up = 1 - Math.exp(-1 / (0.03 * SR));
  const down = 1 - Math.exp(-1 / (0.45 * SR));
  for (let i = 0; i < voice.length; i++) {
    const x = Math.abs(voice[i]);
    env += (x > env ? up : down) * (x - env);
    gain[i] = 1 - depth * Math.min(1, env / 0.02);
  }
  return gain;
}

const STYLES: Style[] = ["calm", "piano", "lofi", "bright"];
const [command, arg1, arg2, arg3] = process.argv.slice(2);

if (command === "samples") {
  const seconds = 26;
  const voice = voiceTrack(seconds);
  const voiceLevel = rms(voice) * Math.sqrt(voice.length / voice.filter((v) => Math.abs(v) > 0.003).length);
  console.log(`voice loudness while speaking: ${voiceLevel.toFixed(3)} RMS`);
  const outDir = join(ROOT, "out", "music-samples");
  for (const style of STYLES) {
    // Alone: loud enough to judge. Under the voice: a background level, ducked while it speaks.
    const [al, ar] = render(style, seconds, 0.1);
    writeWav(join(outDir, `${style}.wav`), al, ar);
    const [ml, mr] = render(style, seconds, voiceLevel * 0.3);
    const gain = duck(voice, 0.5);
    for (let i = 0; i < voice.length; i++) {
      ml[i] = ml[i] * gain[i] + voice[i];
      mr[i] = mr[i] * gain[i] + voice[i];
    }
    writeWav(join(outDir, `${style}-with-voice.wav`), ml, mr);
    let peak = 0;
    for (let i = 0; i < ml.length; i++) peak = Math.max(peak, Math.abs(ml[i]), Math.abs(mr[i]));
    console.log(`${style.padEnd(7)} alone ${rms(al, ar).toFixed(3)} RMS, mix peak ${peak.toFixed(2)}`);
  }
  console.log(`\nSamples are in ${outDir}`);
} else if (command === "build" && STYLES.includes(arg1 as Style) && arg2 && arg3) {
  const seconds = Number(arg2);
  // About half of the voice (amplitude) in the pauses, and half of that while the narration speaks.
  const [l, r] = render(arg1 as Style, seconds, 0.04);
  const gain = duck(voiceTrack(seconds), 0.5);
  for (let i = 0; i < l.length; i++) {
    l[i] *= gain[i];
    r[i] *= gain[i];
  }
  writeWav(arg3, l, r);
  console.log(`${arg1}: ${arg2} s, ducked under the narration -> ${arg3}`);
} else {
  console.log("usage: node tools/music.ts samples | build <calm|piano|lofi|bright> <seconds> <file>");
}
