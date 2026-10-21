## Configuration

ASP.NET Server can be configured either through an appsettings.json mounted at /app/appsettings.json or by using environment variables.
The JSON structure translates to ENV-vars by separating the descending key with a double underscore.

The only **REQUIRED** config key is `Endpoints:Website` which has to contain the exact domain the service is being served under in order for the frontend to work properly.

Other configuration section: 

- `AdminAccess` enables and specifies users for a simple admin authentication to manage games.
- `GameLobby` contains options for the lobby search screen.
- `Game` contains options for the game lobby (when the game has not yet started).
- `Game:DefaultOptions` contains the options which are set by default for each session.
- `Cleanup` contains config for the automated session cleanup.

Detailed description can be accessed by reviewing the property comments in the option classes (WerwolfDotnet.Server/Options).
Additionally, log levels can be configured through `Logging:LogLevel:{fully qualified class name}` set to minimum level.

A complete configuration and also the default one of the game server looks like this:

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning",
      "WerwolfDotnet.GameContext": "Trace"
    }
  },
  "AllowedHosts": "*",
  "AdminAccess": {
    "Enabled": false,
    "Users": {
      "Admin": "e5XGw9wCu5ay2X4UrbAG"
    }
  },
  "GameLobby": {
    "AllowViewAll": false,
    "MinPlayers": 3,
    "MaxPlayers": 32,
    "PlayerNameMinLength": 3,
    "PlayerNameMaxLength": 16,
    "PlayerNameAllowNumbers": true,
    "PlayerNameForbiddenWords": [
      "Amor",
      "Bärenführer",
      "Heiler",
      "Beschützer",
      "Jäger",
      "Seher",
      "Seherlehrling",
      "Brüder",
      "Schwestern",
      "Urwolf",
      "Dorfmatratze",
      "Dorfbewohner",
      "Werwolf",
      "Weißerwolf",
      "Bürgermeister"
    ],
    "PlayerNameForbiddenWordsLists": [
      "https://raw.githubusercontent.com/LDNOOBW/List-of-Dirty-Naughty-Obscene-and-Otherwise-Bad-Words/refs/heads/master/de",
      "https://raw.githubusercontent.com/LDNOOBW/List-of-Dirty-Naughty-Obscene-and-Otherwise-Bad-Words/refs/heads/master/en"
    ]
  },
  "Game": {
    "CanStartWhenNotReady": true,
    "GameMasterSkipAllowed": true,
    "DefaultOptions": {
      "AmountOfRoles": {
        "-1": 1,
        // Werwolf
        "2": 1,
        // Seer
        "4": 1
        // Witch
      },
      "SeerSeesRole": true,
      "ExplodingWitchHome": false,
      "HunterMustKill": false,
      "MayorDecidesNextMayor": true,
      "DefaultVotingTime": 60,
      "DiscussionTime": 300,
      "RevealRoleForCauses": []
    }
  },
  "Cleanup": {
    "Intervall": "00:10:00",
    "AfterInactivity": "12:00:00",
    "AfterEmptySession": "00:30:00"
  }
}
```

NOTE: Enum values have to be specified in the integer values (this affects **AmountOfRoles** and **RevealRoleForCauses**).

### Admin access

When admin access is configured it can be used with HTTP Basic authentication. Normal players authenticate using a Bearer token while admins use Basic. 
Using this method admins can access every session's endpoints except establishing a SignalR connection.

### OLTP

The server is beable to export its logs to and OTLP server. 
This can be achieved using environment variables:

- `OTEL_EXPORTER_OTLP_ENDPOINT`: When set the server will push its logs to an external OLTP server. When not set other OLTP variables will be ignored. These sets the URL of the server to push the logs to.
- `OTEL_EXPORTER_OTLP_PROTOCOL`: The protocol being used for the export. For example `http/protobuf`.
- `OTEL_EXPORTER_OTLP_HEADERS`: Can contain key-value-pairs of header being also transmitted. Can be used for example for authentication.
