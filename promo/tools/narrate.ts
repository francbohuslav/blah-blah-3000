// Generates the narration of the tour with the local VoiceStudio (OmniVoice) and checks its timing.
//
//   node tools/narrate.ts              generate the lines that do not have a file yet
//   node tools/narrate.ts --force      generate all lines again
//   node tools/narrate.ts --only ai-key,ptt-hold   generate just these lines again
//   node tools/narrate.ts --check      transcribe the files with VoiceStudio's Whisper and compare them with the text
//
// VoiceStudio must be running (docker compose up -d in C:\Projekty\Bogan\Media\VoiceStudio). 127.0.0.1 is used on purpose:
// "localhost" resolves to IPv6 there and times out.
import { existsSync, mkdirSync, readFileSync, writeFileSync } from "node:fs";
import { dirname, join } from "node:path";
import { fileURLToPath } from "node:url";
import { NARRATION, startOf, VOICE } from "../src/narration.ts";
import { TOTAL_DURATION } from "../src/schedule.ts";

const API = "http://127.0.0.1:3900";
const OUT = join(dirname(fileURLToPath(import.meta.url)), "..", "public", "narration");
const args = process.argv.slice(2);
const force = args.includes("--force");
const MIN_GAP = 1.0; // seconds of silence between two lines: the viewer needs time to take in the picture
const check = args.includes("--check");
const only = args.includes("--only") ? new Set(args[args.indexOf("--only") + 1].split(",")) : null;

/** Length in seconds of a PCM WAV file. */
function wavSeconds(buf: Buffer): number {
  const rate = buf.readUInt32LE(24);
  const channels = buf.readUInt16LE(22);
  const bits = buf.readUInt16LE(34);
  const dataAt = buf.indexOf("data");
  const size = Math.min(buf.readUInt32LE(dataAt + 4), buf.length - dataAt - 8);
  return size / (rate * channels * (bits / 8));
}

async function speak(text: string, speed: number): Promise<Buffer> {
  const res = await fetch(`${API}/v1/audio/speech`, {
    method: "POST",
    headers: { "content-type": "application/json" },
    body: JSON.stringify({
      model: "tts-1",
      voice: VOICE.profile,
      input: text,
      language: VOICE.language,
      response_format: "wav",
      speed,
      num_step: 32,
      seed: VOICE.seed,
    }),
  });
  if (!res.ok) throw new Error(`HTTP ${res.status}: ${(await res.text()).slice(0, 300)}`);
  return Buffer.from(await res.arrayBuffer());
}

async function transcribe(buf: Buffer): Promise<string> {
  const form = new FormData();
  form.append("file", new Blob([buf], { type: "audio/wav" }), "line.wav");
  form.append("model", "whisper-1");
  form.append("language", VOICE.language);
  const res = await fetch(`${API}/v1/audio/transcriptions`, { method: "POST", body: form });
  if (!res.ok) throw new Error(`HTTP ${res.status}: ${(await res.text()).slice(0, 300)}`);
  const json = (await res.json()) as { text?: string };
  return json.text ?? "";
}

mkdirSync(OUT, { recursive: true });
const lengths = new Map<string, number>();

for (const line of NARRATION) {
  const file = join(OUT, `${line.id}.wav`);
  const exists = existsSync(file);
  const selected = !only || only.has(line.id);
  let buf: Buffer;
  if (!check && selected && (force || only || !exists)) {
    const started = Date.now();
    buf = await speak(line.text, line.speed ?? 1);
    writeFileSync(file, buf);
    console.log(`generated ${line.id} (${((Date.now() - started) / 1000).toFixed(1)} s)`);
  } else if (exists) {
    buf = readFileSync(file);
  } else {
    console.log(`missing ${line.id}`);
    continue;
  }
  lengths.set(line.id, wavSeconds(buf));
  if (check) {
    const heard = await transcribe(buf);
    console.log(`${line.id}\n  said:  ${line.text}\n  heard: ${heard.trim()}`);
  }
}

// Does every line end before the next one starts?
console.log("\nid".padEnd(20) + "start".padStart(8) + "length".padStart(8) + "ends".padStart(8) + "next at".padStart(9) + "  gap");
let problems = 0;
NARRATION.forEach((line, i) => {
  const length = lengths.get(line.id);
  if (length === undefined) return;
  const start = startOf(line);
  const next = NARRATION[i + 1] ? startOf(NARRATION[i + 1]) : undefined;
  const end = start + length;
  const gap = next === undefined ? Infinity : next - end;
  if (gap < MIN_GAP) problems++;
  console.log(
    line.id.padEnd(19) + start.toFixed(1).padStart(8) + length.toFixed(2).padStart(8) + end.toFixed(1).padStart(8) +
      (next === undefined ? "-" : next.toFixed(1)).padStart(9) + (gap === Infinity ? "" : `  ${gap.toFixed(2)}${gap < MIN_GAP ? "  <-- too tight" : ""}`),
  );
});
if (problems > 0) console.log(`\n${problems} line(s) end too close to the next one: shorten the text or set "speed" in src/narration.ts.`);
