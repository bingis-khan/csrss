#!/bin/sh
dotnet publish
rsync -r "bin/Release/net8.0/publish" "$SERVER_URL":/usr/local/bin/csrss  # remember to install rsync in the remote server also.
ssh "$SERVER_URL" rc-service csrss restart

