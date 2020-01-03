 #!/usr/bin/env bash
# Runs Fplbot in docker

 docker run --rm \
 -e fpl__login=$fpl__login \
 -e fpl__password=$fpl__password \
 -e fpl__leagueId=89903 \
 -e Slackbot_SlackApiKey_BotUser=$Slackbot_SlackApiKey_BotUser \ 
 fplbot:heroku