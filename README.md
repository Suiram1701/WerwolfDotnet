A Werwolf browser game inspired by [Werwolfonline.eu](https://play.werwolfonline.eu/Werwolf.php). I'm building this to have a more user-friendly (and cleaner) Werwolf browser game that can be used in for example community events.

## Architecture:
It uses Svelte(Kit) on the frontend with Bootstrap as a CSS framework and ASP.NET on the backend.
The backend has two main components:
- A small API which is used to create, read and join game sessions
- and a SignalR hub (mostly it's a WebSocket) which is used during the game.

The credentials returned by the API will be used to authenticate against the hub and at this point the whole communication goes through the hub.

This is my very first project I build with Svelte, but I am looking forward to it that it will be good. Also, the first time I build something directly with SignalR (if you exclude Blazor uses SignalR under the hood) but in general I have clear ideas how the implement the game in a good way. 

## Usage
It's pretty simple and straight forward. Just pull and run the image `ghcr.io/suiram1701/werwolfdotnet:latest`. Expose port :8080 for HTTP.
By default, HTTPS is not enabled or configured, but it can also done using [this](https://learn.microsoft.com/en-us/aspnet/core/security/docker-https?view=aspnetcore-10.0#running-pre-built-container-images-with-https) Microsoft guide.

Due to the fact that the server by itself does not provide any form or rate limiting or bot protection I recommend using a reverse proxy like traefik to do this.

Everything else can be configured using the described configurations: [CONFIG.md](./CONFIG.md)