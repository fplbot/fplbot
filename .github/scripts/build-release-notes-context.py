import json, sys, urllib.request

def fetch(url):
    with urllib.request.urlopen(url, timeout=10) as r:
        return json.load(r)

try:
    bootstrap = fetch("https://fantasy.premierleague.com/api/bootstrap-static/")
    events = bootstrap['events']
    cur = next((e for e in events if e['is_current']), None) or next((e for e in reversed(events) if e['finished']), None)
    nxt = next((e for e in events if e['is_next']), None)
    teams = {t['id']: t['name'] for t in bootstrap['teams']}
    players = {p['id']: p for p in bootstrap['elements']}

    lines = ["", "## Live FPL Context (real, current data — use naturally where relevant, don't force it into every entry, and don't fabricate stats not listed here)", ""]
    if cur:
        lines.append(f"Current: GW{cur['id']} ({cur['name']}) - finished: {cur['finished']}, average score: {cur['average_entry_score']}, highest score: {cur['highest_score']}")
    if nxt:
        lines.append(f"Next: GW{nxt['id']} ({nxt['name']}) - deadline: {nxt['deadline_time']}")

    if cur and cur['finished']:
        fixtures = fetch(f"https://fantasy.premierleague.com/api/fixtures/?event={cur['id']}")
        lines.append("")
        lines.append(f"GW{cur['id']} results:")
        for fx in fixtures:
            if not fx['finished']:
                continue
            h, a = teams[fx['team_h']], teams[fx['team_a']]
            score = f"{h} {fx['team_h_score']}-{fx['team_a_score']} {a}"
            scorers = []
            for stat in fx['stats']:
                if stat['identifier'] == 'goals_scored':
                    for side in ('h', 'a'):
                        for g in stat[side]:
                            name = players[g['element']]['web_name']
                            n = f" x{g['value']}" if g['value'] > 1 else ""
                            scorers.append(f"{name}{n}")
            if scorers:
                lines.append(f"- {score} (goals: {', '.join(scorers)})")
            else:
                lines.append(f"- {score}")

        live = fetch(f"https://fantasy.premierleague.com/api/event/{cur['id']}/live/")
        top = sorted(live['elements'], key=lambda e: e['stats']['total_points'], reverse=True)[:3]
        lines.append("")
        lines.append(f"Top GW{cur['id']} FPL scorers:")
        for e in top:
            p = players[e['id']]
            lines.append(f"- {p['web_name']}: {e['stats']['total_points']} pts")

    print('\n'.join(lines))
except Exception as e:
    sys.stderr.write(f"Failed to fetch live FPL context: {e}\n")
