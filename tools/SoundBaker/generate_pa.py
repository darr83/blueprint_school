"""Makes the tannoy announcements with ElevenLabs text to speech.

    python3 tools/SoundBaker/generate_pa.py --list              # write pa_lines.json and count characters (free)
    ELEVENLABS_API_KEY=... python3 tools/SoundBaker/generate_pa.py [--voices f,m] [key ...]

Every line is recorded once per announcer voice (f = female, m = male) into takes/pa/<key>_<voice>.mp3
(not committed), then converted to game/Audio/Sounds/pa/<key>_<voice>.ogg with silence trimmed and
loudness evened out. The tannoy speaker sound is added in the game (Announcements bus), not baked in.
Existing takes are skipped, so re-running never spends credits twice. Needs ffmpeg.

The keys match the clip names the simulation sends (SchoolSimulation.Announcements.cs): gen_<name>
for the lines at set times of the school day, and to_office_<role> for staff called to the school office.
"""

import json
import os
import subprocess
import sys

import requests

HERE = os.path.dirname(os.path.abspath(__file__))
ROOT = os.path.dirname(os.path.dirname(HERE))
TAKES = os.path.join(HERE, "takes", "pa")
OUT = os.path.join(ROOT, "game", "Audio", "Sounds", "pa")
LINES_FILE = os.path.join(HERE, "pa_lines.json")

# ElevenLabs premade British voices: calm, clear announcers.
VOICES = {
    "f": "Xb7hH8MSUJpSbSDYk0k2",  # Alice: clear, engaging British female
    "m": "onwK4e9ZLuTAKqWW03F9",  # Daniel: steady British male newsreader
}
MODEL = "eleven_multilingual_v2"

GENERAL = {
    "gen_fire_test": "This is a test of the fire alarm system. No action is required.",
    "gen_no_smoking": "This is a no smoking site, including the car park and entrances.",
    "gen_morning": "Good morning everyone. Please make your way to your tutor rooms.",
    "gen_lunch_open": "The canteen is now open for lunch. Year seven, you may go first.",
    "gen_library_lunch": "The library is open at lunchtime for quiet reading and homework.",
    "gen_end_of_lunch": "Lunch is nearly over. Please make your way to your next lesson.",
    "gen_buses": "The school buses are waiting at the front. Please make your way there now.",
    "gen_clubs": "After-school clubs are starting. Please go straight to your club room.",
    "gen_lost_property": "Lost property is kept at the school office. Please collect anything with your name on.",
    "gen_walk_corridors": "Please remember to walk, not run, in the corridors.",
}

# Staff called to the school office.
TO_OFFICE = {
    "to_office_teacher": "Would the duty teacher please come to the school office.",
    "to_office_site": "Would a member of the site team please come to the school office.",
    "to_office_first_aider": "Would a first aider please come to the school office.",
    "to_office_head_of_year": "Would the Head of Year seven please come to the school office.",
}


def build_lines():
    lines = {}
    lines.update(GENERAL)
    lines.update(TO_OFFICE)
    return lines


def convert(src, dst):
    os.makedirs(os.path.dirname(dst), exist_ok=True)
    trim = "silenceremove=start_periods=1:start_threshold=-50dB,areverse,silenceremove=start_periods=1:start_threshold=-50dB,areverse"
    subprocess.run(["ffmpeg", "-y", "-loglevel", "error", "-i", src, "-af", f"{trim},loudnorm=I=-18:TP=-1.5:LRA=11",
                    "-ar", "44100", "-ac", "1", "-c:a", "libvorbis", "-q:a", "5", dst], check=True)


def main():
    lines = build_lines()
    with open(LINES_FILE, "w") as f:
        json.dump(lines, f, indent=1, ensure_ascii=False)
    chars = sum(len(t) for t in lines.values())
    if "--list" in sys.argv:
        print(f"{len(lines)} lines, {chars} characters per voice")
        return 0

    voices = list(VOICES)
    args = [a for a in sys.argv[1:] if not a.startswith("--")]
    if "--voices" in sys.argv:
        voices = sys.argv[sys.argv.index("--voices") + 1].split(",")
        args = [a for a in args if a != sys.argv[sys.argv.index("--voices") + 1]]
    wanted = set(args)
    key = os.environ["ELEVENLABS_API_KEY"]
    os.makedirs(TAKES, exist_ok=True)
    spent = 0
    for name, text in lines.items():
        if wanted and name not in wanted:
            continue
        for v in voices:
            take = os.path.join(TAKES, f"{name}_{v}.mp3")
            if not os.path.exists(take):
                r = requests.post(
                    f"https://api.elevenlabs.io/v1/text-to-speech/{VOICES[v]}",
                    headers={"xi-api-key": key},
                    params={"output_format": "mp3_44100_128"},
                    json={"text": text, "model_id": MODEL,
                          "voice_settings": {"stability": 0.75, "similarity_boost": 0.75, "style": 0.0, "speed": 0.95}},
                    timeout=120)
                if r.status_code != 200:
                    print(f"FAILED {name}_{v}: {r.status_code} {r.text[:300]}")
                    return 1
                with open(take, "wb") as out:
                    out.write(r.content)
                cost = r.headers.get("character-cost") or r.headers.get("x-character-count") or "?"
                if cost.isdigit():
                    spent += int(cost)
                print(f"{name}_{v}  credits {cost}", flush=True)
            convert(take, os.path.join(OUT, f"{name}_{v}.ogg"))
    print(f"Credits reported this run: {spent}")
    return 0


if __name__ == "__main__":
    sys.exit(main())
