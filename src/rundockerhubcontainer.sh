 #!/usr/bin/env bash
# Runs Fplbot in docker

 docker run --rm \
 -e fpl__login=$fpl__login \
 -e fpl__password=$fpl__password \
 -e Slackbot_SlackApiKey_SlackApp=$Slackbot_SlackApiKey_SlackApp \
 -e Slackbot_SlackApiKey_BotUser=$Slackbot_SlackApiKey_BotUser \
 -e leagueId=89903 \
 fplbot/fplbot:latest