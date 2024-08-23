VERSION=$(dotnet-gitversion | jq -r '.FullSemVer')
INFOVERSION=$(dotnet-gitversion | jq -r '.InformationalVersion')
echo "Version: $VERSION"
echo "Informational Version: $INFOVERSION"
docker build -t registry.heroku.com/blank-fplbot-test/web --build-arg INFOVERSION=$INFOVERSION --build-arg VERSION=$VERSION -f ./src/Dockerfile.web ./src 
docker push registry.heroku.com/blank-fplbot-test/web
heroku container:release web --app blank-fplbot-test
