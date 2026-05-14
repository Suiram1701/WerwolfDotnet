A Werwolf browser game inspired by [Werwolfonline.eu](https://play.werwolfonline.eu/Werwolf.php). I'm building this to have a more user friendly (and cleaner) Werwolf browser game that can be used in for example community events.

## Architecture:
It uses Svelte(Kit) on the frontend with Bootstrap as a CSS framework and ASP.NET on the backend.
The backend has two main components:
- A small API which is used to create, read and join game sessions
- and a SignalR hub (mostly it's a WebSocket) which is used during the game.

The credentials returned by the API will be used to authenticate against the hub and at this point the whole communication goes through the hub.

This is my very first project I build with Svelte, but I am looking forward to it that it will be good. Also, the first time I build something directly with SignalR (if you exclude Blazor uses SignalR under the hood) but in general I have clear ideas how the implement the game in a good way. 

## Usage
This mainly describes how to use the applications docker image to run it. The image includes the backend and frontend while the frontend is being served using the backend.
ASP.NET can be configured using environment variables instead of the appsettings.json file by replacing nesting with double underscores. Important variables are:
- `Endpoints__Website`: Configures the CORS domain being used by the backend. Has to be the exact domain the service is being served under otherwise the frontend will break.
- `GameLobby__*`: Configures the game lobby screen. See ./WerwolfDotnet.Server/Options/GameLobbyOptions.cs for configuration possibilities.
- `Game__*`: Sets options for things while the game is running. It also contains the default game options. See ./WerwolfDotnet/Options/GameOptions.cs for more details.
- `OTEL_EXPORTER_OTLP_ENDPOINT`: When set the server will push its logs to an external OLTP server. When not set other OLTP variables will be ignored. These sets the URL of the server to push the logs to.
- `OTEL_EXPORTER_OTLP_PROTOCOL`: The protocol being used for the export. For example `http/protobuf`.
- `OTEL_EXPORTER_OTLP_HEADERS`: Can contain key-value-pairs of header being also transmitted. Can be used for example for authentication.