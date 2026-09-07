#!/usr/bin/env python3
"""
Generates release notes ourselves instead of using github/copilot-release-notes.
That action bakes in its own base prompt and a rigid one-entry-per-PR JSON
schema/renderer we can't control, which caused most of the formatting bugs
we hit (doubled headings, duplicate PR numbers, dropped entries, stock
phrases). Calling the copilot CLI directly gives full control over the
prompt and the exact markdown that comes back.

Usage: generate-release-notes.py <base-ref> [head-ref]
Requires: GITHUB_TOKEN (gh PR lookups), COPILOT_GITHUB_TOKEN (copilot CLI auth)
Prints the final release notes markdown to stdout.
"""
import json
import os
import re
import subprocess
import sys
import urllib.request

ROOT = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))


def sh(*args):
    return subprocess.run(args, capture_output=True, text=True, check=True).stdout


def fetch_json(url):
    with urllib.request.urlopen(url, timeout=10) as r:
        return json.load(r)


def build_live_context():
    """Real, current FPL gameweek/fixture/scorer data to ground the
    commentary in actual events instead of invented ones."""
    try:
        bootstrap = fetch_json("https://fantasy.premierleague.com/api/bootstrap-static/")
        events = bootstrap["events"]
        cur = next((e for e in events if e["is_current"]), None) or next(
            (e for e in reversed(events) if e["finished"]), None
        )
        nxt = next((e for e in events if e["is_next"]), None)
        teams = {t["id"]: t["name"] for t in bootstrap["teams"]}
        players = {p["id"]: p for p in bootstrap["elements"]}

        lines = [
            "",
            "## Live FPL Context (real, current data — use naturally where "
            "relevant, don't force it into every entry, and don't fabricate "
            "stats not listed here)",
            "",
        ]
        if cur:
            lines.append(
                f"Current: GW{cur['id']} ({cur['name']}) - finished: {cur['finished']}, "
                f"average score: {cur['average_entry_score']}, highest score: {cur['highest_score']}"
            )
        if nxt:
            lines.append(f"Next: GW{nxt['id']} ({nxt['name']}) - deadline: {nxt['deadline_time']}")

        if cur and cur["finished"]:
            fixtures = fetch_json(f"https://fantasy.premierleague.com/api/fixtures/?event={cur['id']}")
            lines.append("")
            lines.append(f"GW{cur['id']} results:")
            for fx in fixtures:
                if not fx["finished"]:
                    continue
                h, a = teams[fx["team_h"]], teams[fx["team_a"]]
                score = f"{h} {fx['team_h_score']}-{fx['team_a_score']} {a}"
                scorers = []
                for stat in fx["stats"]:
                    if stat["identifier"] == "goals_scored":
                        for side in ("h", "a"):
                            for g in stat[side]:
                                name = players[g["element"]]["web_name"]
                                n = f" x{g['value']}" if g["value"] > 1 else ""
                                scorers.append(f"{name}{n}")
                lines.append(f"- {score} (goals: {', '.join(scorers)})" if scorers else f"- {score}")

            live = fetch_json(f"https://fantasy.premierleague.com/api/event/{cur['id']}/live/")
            top = sorted(live["elements"], key=lambda e: e["stats"]["total_points"], reverse=True)[:3]
            lines.append("")
            lines.append(f"Top GW{cur['id']} FPL scorers:")
            for e in top:
                p = players[e["id"]]
                lines.append(f"- {p['web_name']}: {e['stats']['total_points']} pts")

        return "\n".join(lines)
    except Exception as e:
        sys.stderr.write(f"Failed to fetch live FPL context: {e}\n")
        return ""


def find_pr_numbers(base_ref, head_ref):
    numbers = set()
    merges = sh("git", "log", "--merges", "--oneline", f"{base_ref}..{head_ref}")
    for line in merges.splitlines():
        m = re.search(r"Merge pull request #(\d+) from", line)
        if m:
            numbers.add(int(m.group(1)))
    non_merges = sh("git", "log", "--no-merges", "--oneline", f"{base_ref}..{head_ref}")
    for line in non_merges.splitlines():
        m = re.search(r"\(#(\d+)\)$", line)
        if m:
            numbers.add(int(m.group(1)))
    return sorted(numbers)


def fetch_pr(repo, number):
    out = sh(
        "gh", "api", f"repos/{repo}/pulls/{number}",
        "--jq", '{number, title, body, author: .user.login}',
    )
    return json.loads(out)


def build_prompt(instructions_template, prs, live_context):
    pr_blocks = []
    for pr in prs:
        body = (pr["body"] or "").strip()
        if len(body) > 1500:
            body = body[:1500] + "\n... (truncated)"
        pr_blocks.append(
            f"### PR #{pr['number']}: {pr['title']}\n"
            f"Author: @{pr['author']}\n\n{body}"
        )
    pr_section = "\n\n".join(pr_blocks)

    return (
        instructions_template
        .replace("{{PR_DATA}}", pr_section)
        .replace("{{LIVE_CONTEXT}}", live_context)
    )


COPILOT_TIMEOUT_SECONDS = 5 * 60


def build_copilot_env():
    """Minimal environment for the copilot subprocess — never inherit the
    full workflow environment, which would leak unrelated secrets (Heroku,
    Docker Hub, etc.) to the model. Mirrors github/copilot-release-notes'
    own buildCopilotEnv()."""
    env = {}
    for key in ("PATH", "HOME", "RUNNER_TEMP", "NODE_PATH", "NODE_OPTIONS"):
        if os.environ.get(key):
            env[key] = os.environ[key]
    env["GITHUB_TOKEN"] = os.environ.get("COPILOT_GITHUB_TOKEN") or os.environ.get("GITHUB_TOKEN", "")
    return env


def sanitize(text):
    """Neutralize leading '::' so untrusted PR content can't inject
    GitHub Actions workflow commands into the log output."""
    return re.sub(r"^::", "  ::", text, flags=re.MULTILINE)


def run_copilot(prompt, model):
    try:
        result = subprocess.run(
            ["copilot", "--model", model, "--deny-tool", "shell", "-p", prompt],
            capture_output=True, text=True, stdin=subprocess.DEVNULL,
            env=build_copilot_env(), timeout=COPILOT_TIMEOUT_SECONDS,
        )
    except subprocess.TimeoutExpired:
        sys.stderr.write(f"Copilot CLI timed out after {COPILOT_TIMEOUT_SECONDS}s\n")
        raise SystemExit(1)
    if result.returncode != 0:
        sys.stderr.write(sanitize(result.stderr))
        raise SystemExit(1)
    return sanitize(result.stdout.strip())


def main():
    base_ref = sys.argv[1]
    head_ref = sys.argv[2] if len(sys.argv) > 2 else "HEAD"
    repo = os.environ.get("GITHUB_REPOSITORY", "fplbot/fplbot")
    model = os.environ.get("COPILOT_MODEL", "claude-sonnet-5")

    pr_numbers = find_pr_numbers(base_ref, head_ref)
    if not pr_numbers:
        return

    prs = [fetch_pr(repo, n) for n in pr_numbers]

    instructions_path = os.path.join(ROOT, ".github", "release-notes-instructions.md")
    instructions_template = open(instructions_path).read()

    live_context = build_live_context()

    prompt = build_prompt(instructions_template, prs, live_context)
    notes = run_copilot(prompt, model)
    print(notes)


if __name__ == "__main__":
    main()
