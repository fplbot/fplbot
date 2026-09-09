#!/usr/bin/env python3
"""
Creates a fresh FplBot dev Slack app and prints the credentials to paste into appsettings.json.

Prerequisites:
  - Slack CLI installed: curl -fsSL https://downloads.slack-edge.com/slack-cli/install.sh | bash
  - Authenticated as a workspace owner: slack login

Usage:
  python3 src/create-slack-dev-app.py          # bot app  → CLIENT_ID / CLIENT_SECRET / SIGNING_SECRET / SlackAppId
  python3 src/create-slack-dev-app.py --admin  # admin app → admin:SlackClientId / admin:SlackClientSecret

Reads available workspaces from `slack auth list`. Picks automatically if only one is
logged in, otherwise prompts for a selection.

Note: event_subscriptions are intentionally omitted from the bot manifest — localhost has no
public URL. To test Slack events locally, expose port 1337 via ngrok and configure the
Request URL in the app dashboard at https://api.slack.com/apps/<APP_ID>/event-subscriptions
"""

import json
import re
import shutil
import subprocess
import urllib.request
import urllib.parse
import sys

BOT_MANIFEST_FILE   = 'src/slack-app-manifest.json'
ADMIN_MANIFEST_FILE = 'src/slack-admin-app-manifest.json'


def list_teams():
    slack = shutil.which('slack')
    if not slack:
        print("ERROR: Slack CLI not found. Install: curl -fsSL https://downloads.slack-edge.com/slack-cli/install.sh | bash", file=sys.stderr)
        sys.exit(1)
    out = subprocess.check_output([slack, 'auth', 'list', '--no-color'], text=True)
    return re.findall(r'^(.+?)\s+\(Team ID:\s+(T[A-Z0-9]+)\)', out, re.MULTILINE)


def pick_team(teams):
    if not teams:
        print("ERROR: No teams logged in. Run: slack login", file=sys.stderr)
        sys.exit(1)
    if len(teams) == 1:
        return teams[0]
    print("Select a workspace:")
    for i, (name, tid) in enumerate(teams, 1):
        print(f"  {i}) {name} ({tid})")
    while True:
        try:
            choice = int(input("Enter number: "))
            if 1 <= choice <= len(teams):
                return teams[choice - 1]
        except (ValueError, EOFError):
            pass


def get_token(team_id):
    creds_file = __import__('os').path.expanduser('~/.slack/credentials.json')
    try:
        with open(creds_file) as f:
            return json.load(f)[team_id]['token']
    except (FileNotFoundError, KeyError):
        print(f"ERROR: no stored token for {team_id}. Run: slack login", file=sys.stderr)
        sys.exit(1)


def create_app(token, manifest_file):
    with open(manifest_file) as f:
        manifest = json.load(f)
    manifest.pop('_comment', None)

    data = urllib.parse.urlencode({'manifest': json.dumps(manifest)}).encode()
    req = urllib.request.Request(
        'https://slack.com/api/apps.manifest.create',
        data=data,
        headers={'Authorization': f'Bearer {token}', 'Content-Type': 'application/x-www-form-urlencoded'}
    )
    resp = json.loads(urllib.request.urlopen(req).read())
    if not resp.get('ok'):
        print("ERROR:", json.dumps(resp, indent=2), file=sys.stderr)
        sys.exit(1)
    return resp


def main():
    admin = '--admin' in sys.argv

    teams = list_teams()
    _, team_id = pick_team(teams)
    token = get_token(team_id)

    if admin:
        resp = create_app(token, ADMIN_MANIFEST_FILE)
        c = resp.get('credentials', {})
        print(f"Paste into appsettings.json under \"admin\": {{}}")
        print(f"  SlackClientId:     {c.get('client_id')}")
        print(f"  SlackClientSecret: {c.get('client_secret')}")
        print(f"  (App ID: {resp.get('app_id')})")
    else:
        resp = create_app(token, BOT_MANIFEST_FILE)
        c = resp.get('credentials', {})
        print(f"Paste into appsettings.json (root level):")
        print(f"  CLIENT_ID:          {c.get('client_id')}")
        print(f"  CLIENT_SECRET:      {c.get('client_secret')}")
        print(f"  CLIENT_SIGNING_SECRET: {c.get('signing_secret')}")
        print(f"  SlackAppId:         {resp.get('app_id')}")


if __name__ == '__main__':
    main()
