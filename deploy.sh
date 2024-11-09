#!/bin/sh
dotnet publish
scp -r "bin/Release/net8.0/publish" "$SERVER_URL":/usr/local/bin/csrss
ssh "$SERVER_URL" rc-service csrss restart

