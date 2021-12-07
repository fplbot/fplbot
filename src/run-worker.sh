docker run \
--rm \
-e DOTNET_ENVIRONMENT=Development \
-e NSB_LICENSE="$NSB_LICENSE" \
-e ASB_CONNECTIONSTRING=$ASB_CONNECTIONSTRING \
-e REDIS_URL=$REDIS_URL \
-e fpl__login=$fpl__login \
-e fpl__password=$fpl__password \
fplbot-worker:heroku
